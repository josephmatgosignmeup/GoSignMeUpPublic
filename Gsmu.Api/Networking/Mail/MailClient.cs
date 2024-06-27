using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net.Mime;
using Dimac.JMail;
using EASendMail;
using Gsmu.Api.Data.School.Entities;
using System.Net;

namespace Gsmu.Api.Networking.Mail
{
    /// <summary>
    /// This class and the methods can be uniformly extended to accept attachments.
    /// </summary>
    public static class MailClient
    {
        public static void SendEmail(string from, string to, string subject, string body, bool isHtml, MailHandler? mailHandler = null, string Attachment = null, string BCC = null, EmailAuditTrail EmailEntity = null)
        {
            var handler = !mailHandler.HasValue ? MailSettings.MailHandler : mailHandler.Value;

            switch (handler)
            {
                case MailHandler.DotNet:
                    SendDotNetEmail(from, to, subject, body, isHtml,BCC, EmailEntity);
                    break;
                case MailHandler.JMail:
                    SendDotNetEmail(from, to, subject, body, isHtml,BCC, EmailEntity);
                    //SendDimacJmailEmail(from, to, subject, body, isHtml, EmailEntity);
                   
                    break;
                default:
                    //SendDotNetEmail(from, to, subject, body, isHtml);
                    SendEAMailArchitect(from, to, subject, body, isHtml, Attachment, BCC, EmailEntity);
                    break;

            }
        }

        public static void SendDimacJmailEmail(string from, string to, string subject, string body, bool isHtml, EmailAuditTrail EmailEntity)
        {
            Message message = new Message();
            message.From = from;
            message.To.Add(to);
            message.Subject = subject;
            message.BodyText = body;
            if (isHtml)
            {
                message.BodyHtml = body;
            }
            try
            {

                Smtp.Send(message, MailSettings.MailServer);
            }
            catch (Exception e)
            {
                if (EmailEntity != null)
                {
                    EmailEntity.Pending = 1;
                    EmailEntity.ErrorInfo = e.Message;

                }
            }
            finally
            {
                EmailEntity.EmailFrom = from;
                if (!string.IsNullOrEmpty(EmailEntity.EmailBCC))
                {
                    if (EmailEntity.EmailBCC.Count() >= 1000)
                    {
                        EmailEntity.EmailBCC = EmailEntity.EmailBCC.Substring(0, 999);
                    }
                }
                using (var db = new SchoolEntities())
                {
                    db.EmailAuditTrails.Add(EmailEntity);
                    db.SaveChanges();

                }
            }
        }

        public static void SendDotNetEmail(string from, string to, string subject, string body, bool isHtml,string BCC,EmailAuditTrail EmailEntity)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            string useNewEncryptionProtocol = System.Configuration.ConfigurationManager.AppSettings["UseNewEncryptionProtocol"];
            if (!string.IsNullOrEmpty(useNewEncryptionProtocol) && bool.Parse(useNewEncryptionProtocol) == true)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }

            MailMessage message = new MailMessage(from, to, subject, body);
            string fromName = string.Empty;
            using (var db = new SchoolEntities())
            {
                fromName = db.PDFHeaderFooterInfoes.FirstOrDefault().Header1; // get the first record
            }
            if (!string.IsNullOrEmpty(fromName)) 
            {
                message.From = new System.Net.Mail.MailAddress(from, fromName); //overwrites teh from email address to attach from name
            }
            if (BCC != null)
            {
                foreach (var BCCs in BCC.Split(';'))
                {
                    if (BCCs != "")
                    {
                        try
                        {
                            message.Bcc.Add(BCCs);
                        }
                        catch { }
                    }
                }
            }
            if (EmailEntity.AttachmentName != null)
            {
                foreach (string file in EmailEntity.AttachmentName.Split('|'))
                {
                    if (file != "")
                    {
                        message.Attachments.Add(new System.Net.Mail.Attachment(file, MediaTypeNames.Application.Octet));
                    }
                }
            }
            if (EmailEntity.AttachmentNameMemo != null)
            {
                foreach (string file in EmailEntity.AttachmentNameMemo.Split('|'))
                {
                    if (file != "" && message.Attachments.Where(a => a.Name.Contains(file)).ToList().Count == 0) // make sure that no files gets attached more than once
                    {
                        message.Attachments.Add(new System.Net.Mail.Attachment(file, MediaTypeNames.Application.Octet));
                    }
                }
            }
            if (isHtml)
            {
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(body, new ContentType("text/html")));
            }
            try
            {

                string smtpIP = "";
                int smtpPort = 25;
                string smtpSendType = "";
                var client = new System.Net.Mail.SmtpClient(MailSettings.MailServer);
                var SMTPServerInfo = MailSettings.MailServer.Split(':');
                if (MailSettings.MailServer.IndexOf(":") > 0)
                {
                    smtpIP = SMTPServerInfo[0];
                    smtpPort = int.Parse(SMTPServerInfo[1]);
                    if (SMTPServerInfo.Length == 3)
                    {
                        smtpSendType = SMTPServerInfo[2];
                    }
                    client = new System.Net.Mail.SmtpClient(smtpIP);
                    client.Port = smtpPort;
                    client.EnableSsl = true;
                }
                
                var SMTPEmailAccount = MailSettings.SMTPEmailAccount.Replace("%@%@%", "|").Split('|');
                var User = SMTPEmailAccount[0];
                var password = SMTPEmailAccount[1];
                if (!string.IsNullOrEmpty(User))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(User, password);
                }
                client.Send(message);
            }
            catch (Exception e)
            {
                if (EmailEntity != null)
                {
                    EmailEntity.Pending = 1;
                    EmailEntity.ErrorInfo = e.Message;

                }
            }
            finally
            {
                EmailEntity.EmailFrom = from;
                if (EmailEntity != null && !string.IsNullOrEmpty(from))
                {
                    EmailEntity.EmailFrom = fromName + " (" + from + ")";
                }
                if (!string.IsNullOrEmpty(EmailEntity.EmailBCC))
                {
                    if (EmailEntity.EmailBCC.Count() >= 1000)
                    {
                        EmailEntity.EmailBCC = EmailEntity.EmailBCC.Substring(0, 999);
                    }
                }
                using (var db = new SchoolEntities())
                {
                    if (Authorization.AuthorizationHelper.CurrentUser != null)
                    {
                        EmailEntity.LoggedInUser = Authorization.AuthorizationHelper.CurrentUser.LoggedInUsername;
                    }
                    db.EmailAuditTrails.Add(EmailEntity);
                    db.SaveChanges();

                }
            }
        }

        public static void SendEAMailArchitect(string from, string to, string subject, string body, bool isHtml, string Attachment, string BCC, EmailAuditTrail EmailEntity)
        {
            string password = "";
            string User = "";
            string licensekey = "TryIt";
            int smtpPort = 25;
            string smtpIP = "";
            string smtpSendType = "";
            string useNewEncryptionProtocol = System.Configuration.ConfigurationManager.AppSettings["UseNewEncryptionProtocol"];
            if (!string.IsNullOrEmpty(useNewEncryptionProtocol) && bool.Parse(useNewEncryptionProtocol) == true)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }

            try
            {
                var SMTPEmailAccount = MailSettings.SMTPEmailAccount.Replace("%@%@%", "|").Split('|');
                User = SMTPEmailAccount[0];
                password = SMTPEmailAccount[1];
                licensekey = "ES-C1407722592-00840-C5915V91976268V7-14TAE2UCT589DF94"; //SMTPEmailAccount[2];
            }
            catch (Exception e)
            {
            }
            string GlobalFromEmailAddress = Gsmu.Api.Data.Settings.Instance.GetMasterInfo().FromEmailAddress;
            EASendMail.SmtpMail oMail = new EASendMail.SmtpMail(licensekey);
            string fromName = string.Empty;
            using (var db = new SchoolEntities())
            {
                fromName = db.PDFHeaderFooterInfoes.FirstOrDefault().Header1; // get the first record
            }
            if (!string.IsNullOrEmpty(fromName))
            {
                oMail.From = new EASendMail.MailAddress(fromName, GlobalFromEmailAddress);  //overwrites teh from email address to attach from name
            }
            else 
            {
                oMail.From = GlobalFromEmailAddress; //new MailAddress("Tester", "test@adminsystem.com");;
            }
            oMail.ReplyTo = from;
            if (to.Contains(';'))
            {
                foreach (var address in to.Split(';'))
                {
                    if (address != "")
                    {
                        oMail.To.Add(address);
                    }
                }
            }
            else
            {
                oMail.To = to;
            }
            if (EmailEntity.AttachmentName != null)
            {
                foreach (string file in EmailEntity.AttachmentName.Split('|'))
                {
                    if (file != "")
                    {
                        oMail.AddAttachment(file);
                    }
                }

            }

            if (EmailEntity.AttachmentNameMemo != null)
            {
                foreach (string file in EmailEntity.AttachmentNameMemo.Split('|'))
                {
                    if (file != "")
                    {
                        oMail.AddAttachment(file);
                    }
                }
            }
            if (!string.IsNullOrEmpty(BCC))
            {
                //foreach (var BCCs in BCC.Split(';'))
                //{
                //    if (BCCs != "")
                //    {
                //        oMail.Bcc.Add(BCCs);
                //    }
                //}
                oMail.Bcc = BCC;
            }
            if (EmailEntity.EmailCC != null)
            {
                oMail.Cc = EmailEntity.EmailCC;
            }

            oMail.Subject = subject;
            //may need to implement UseHTML option on the email system config.
            oMail.HtmlBody =body;
         
            var SMTPServerInfo = MailSettings.MailServer.Split(':');
            if (MailSettings.MailServer.IndexOf(":") > 0)
            {
                smtpIP = SMTPServerInfo[0];
                smtpPort = int.Parse(SMTPServerInfo[1]);
                if (SMTPServerInfo.Length == 3)
                {
                    smtpSendType = SMTPServerInfo[2];
                }
            }
            else
            {
                smtpIP = MailSettings.MailServer;
            }
            EASendMail.SmtpServer oServer = new EASendMail.SmtpServer(smtpIP);
            oServer.Port = smtpPort;
            if (smtpSendType == "tls")
            {
                oServer.ConnectType = EASendMail.SmtpConnectType.ConnectTryTLS;
            }
            else if (smtpSendType == "ssl")
            {
                oServer.ConnectType = EASendMail.SmtpConnectType.ConnectSSLAuto;
            }
            
            if (User != "")
            {
                oServer.User = User;
                oServer.Password = password;
            }
            try
            {
                EASendMail.SmtpClient oSmtp = new EASendMail.SmtpClient();
                oSmtp.SendMail(oServer, oMail);
                if (EmailEntity != null)
                {

                    EmailEntity.Pending = 0;
                }
            }
            catch (Exception ep)
            {
                if (EmailEntity != null)
                {
                    EmailEntity.Pending = 1;
                    EmailEntity.ErrorInfo = ep.Message;
                   
                }
                Gsmu.Api.Data.School.Supervisor.SupervisorHelper.SendingEmailStatus = EmailEntity.Pending.ToString();
            }
            finally
            {

                EmailEntity.EmailFrom = from;
                if (EmailEntity != null && !string.IsNullOrEmpty(from))
                {
                    EmailEntity.EmailFrom = fromName + " (" + from + ")";
                }
                if (!string.IsNullOrEmpty(EmailEntity.EmailBCC))
                {
                    if (EmailEntity.EmailBCC.Count() >= 1000)
                    {
                        EmailEntity.EmailBCC = EmailEntity.EmailBCC.Substring(0, 999);
                    }
                }
                using (var db = new SchoolEntities())
                {
                        if (Authorization.AuthorizationHelper.CurrentUser != null)
                        {
                            EmailEntity.LoggedInUser = Authorization.AuthorizationHelper.CurrentUser.LoggedInUsername;
                        }
                        db.EmailAuditTrails.Add(EmailEntity);
                        db.SaveChanges();
                    

                }
            }
        }

    }
}

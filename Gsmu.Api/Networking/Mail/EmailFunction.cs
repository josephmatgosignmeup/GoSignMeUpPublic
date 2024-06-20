
using Dimac.JMail;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Data.School.CourseRoster;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.Survey;
using Gsmu.Api.Data.Survey.CourseSurvey;
using Gsmu.Api.Commerce.ShoppingCart;
using Gsmu.Api.Data.School.Terminology;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.RegularExpressions;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Authorization;
using System.Threading;

namespace Gsmu.Api.Networking.Mail
{
    public class EmailFunction
    {
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        public static bool IncludeIcs
        {
            get
            {
                return Settings.Instance.GetMasterInfo3().EmailIncludeICS.HasValue && Settings.Instance.GetMasterInfo3().EmailIncludeICS.Value == 1;
            }
        }
        public static SendCalendarEventViaEmail SendCalendarEventViaEmail
        {
            get
            {
                int value = Settings.Instance.GetMasterInfo2().SendCalendarEventViaEmail;
                return (SendCalendarEventViaEmail)value;
            }
        }

        public EmailReturnModel SendConfirmationEmail(string payment_gross, string orderNumber, string surveypage, string callfrom = "", MailSettings.NetworkingEmailResendingModel resendingModel = null)
        {
            //TODO: Check noregemail setting to see if we should send confirmation email.
            //      Not sure what to do now, since all courses are sent in one email for the order.
            //      It would be confusing for a student to get a confirmation email for a whole
            //      order that doesn't show each course, if the setting is turned off for one
            //      or more courses within the order.
            //if (AuthorizationHelper.CurrentAdminUser != null)
            //{
            //    using (var db = new SchoolEntities())
            //    {
            //        var kmultiplemasterorder = (from masterorder in db.Course_Rosters where masterorder.MasterOrderNumber == orderNumber || masterorder.OrderNumber == orderNumber select masterorder.OrderNumber).Distinct().ToList();
         

            //        foreach (var orderroster in kmultiplemasterorder)
            //        {
            //            SendConfirmationEmailFromAdminEnroll(payment_gross, orderroster, surveypage, callfrom, resendingModel);
            //        }
            //    }
            //    return null;
            //}
            const int NO_CONFIRMATION_EMAIL = 2;

            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            EmailFunction EmailFunction = new EmailFunction();
            try 
            {
                OrderModel CourseRosterModel = new OrderModel(orderNumber);
                string Stname = "";
               // string cName = "";
                string ConfirmationBody = Settings.Instance.GetMasterInfo3().EmailConfirmationContent;
                string ConfirmationSubject = Settings.Instance.GetMasterInfo3().EmailConfirmationSubject;
                int studentid = CourseRosterModel.Student.STUDENTID;
                string coursids = "";
                string AllCourseConfirmationBody = "";
                string checkoutComments = "";
                string PubDateFormat = Settings.Instance.GetPubDateFormat();
                var isPaidinFull = 0;
                int waitlistcoursecount = 0;
                int rosteritemnumber = 0;
                string cName = "";
                resendingModel = resendingModel == null ? new MailSettings.NetworkingEmailResendingModel() : resendingModel;
                /*string eventcontent = "";
                foreach (Course_Roster rosteritem in CourseRosterModel.CourseRosters.DistinctBy(a => a.eventid).Where(x => x.Course.COURSENUM != "~ZZZZZZ~" && (x.Course.coursetype == 0 || x.Course.coursetype == null) && (x.Course.eventid != 0 || x.Course.eventid != null)))
                {
                    using (var db = new SchoolEntities())
                    {

                        var eventdetails = (from _events in db.Courses where _events.COURSEID == rosteritem.eventid select new { name = _events.COURSENAME, coursenum = _events.COURSENUM }).FirstOrDefault();
                        if (eventdetails != null)
                        {
                            eventcontent = eventcontent + "Event Information:" + eventdetails.coursenum + " - " + eventdetails.name + "<br />";
                        }
                        foreach (var session in CourseRosterModel.CourseRosters.Where(x => x.Course.eventid == rosteritem.Course.eventid).DistinctBy(a => a.Course.sessionid))
                        {
                            var sessiondetails = (from _events in db.Courses where _events.COURSEID == session.Course.sessionid select new { name = _events.COURSENAME, coursenum = _events.COURSENUM }).FirstOrDefault();
                            if (sessiondetails != null)
                            {
                                eventcontent = eventcontent + "                 Session Information:" + sessiondetails.coursenum + " - " + sessiondetails.name + "<br />";
                            }
                            foreach (var courses in CourseRosterModel.CourseRosters.Where(x => x.Course.sessionid == session.Course.sessionid).DistinctBy(a => a.Course.COURSEID))
                            {
                                CourseModel CourseModel = new CourseModel(Int32.Parse(courses.COURSEID.ToString()));
                                eventcontent = eventcontent + "                        " + "<br>Course Number: " + courses.Course.COURSENUM + "<br> Course Name: " + CourseModel.Course.COURSENAME + "<br>Course Date: " + CourseModel.CourseStart.COURSEDATE.Value.ToString(PubDateFormat) + "";
                                eventcontent = eventcontent + "<br />";
                            }
                        }
                    }

                }

                ConfirmationBody = ConfirmationBody.Replace("{EventDetailsNew}", eventcontent);
                bool iseventappended = false;*/
                foreach (Course_Roster rosteritem in CourseRosterModel.CourseRosters.Where(x => (x.Course.coursetype==0 || x.Course.coursetype==null)))
                {

                    try
                    {
                        if (Directory.Exists(System.Web.HttpContext.Current.Server.MapPath("~") + "/admin/CitrixG2W") == true)
                        {
                            // create registration
                            //wpostURL = ClientURLName & "/CitrixG2W/create-registrant.asp?webinarid=" & aParameters(23) & "&studid=" & iStudentID & "&rosterid=" & cRID
                            //http.open "POST", wpostURL , true
                            //http.SetRequestHeader "Content-Type", "application/x-www-form-urlencoded"
                            //http.send()
                            //http.WaitForResponse
                            string wpostURL = "";
                            wpostURL = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "/CitrixG2W/create-registrant.asp?rubyrequest=1&webinarid=&studid=" + rosteritem.STUDENTID + "&rosterid=" + rosteritem.RosterID;
                            System.Net.ServicePointManager.Expect100Continue = true;
                            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                            System.Net.HttpWebRequest wrequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(wpostURL);
                            wrequest.ContentType = "application/json; charset=utf-8";
                            wrequest.Method = "POST";
                            using (var streamWriter = new StreamWriter(wrequest.GetRequestStream()))
                            {
                                streamWriter.Write("");
                            }

                            System.Net.HttpWebResponse responseStream = wrequest.GetResponse() as System.Net.HttpWebResponse;
                            StreamReader reader = new StreamReader(responseStream.GetResponseStream(), System.Text.Encoding.UTF8);
                            string datastream = reader.ReadToEnd();
                        }
                    }

                    catch { }
                    ConfirmationBody = Settings.Instance.GetMasterInfo3().EmailConfirmationContent;
                    isPaidinFull = rosteritem.PaidInFull;
                    CourseModel CourseModel = new CourseModel(Int32.Parse(rosteritem.COURSEID.ToString()));
                    if (CourseModel.Course.NoRegEmail == NO_CONFIRMATION_EMAIL || CourseModel.Course.NoRegEmail == 3)
                    {
                        continue;
                    }

                    if (rosteritem.Course.COURSENUM != "~ZZZZZZ~")
                    {

                        cName = cName + "<br>Course Number: " + CourseModel.Course.COURSENUM + "<br> Course Name: " + CourseModel.Course.COURSENAME + "<br>Course Date: " + CourseModel.CourseStart.COURSEDATE.Value.ToString(PubDateFormat) + "";
                    }
                    else
                    {
                        cName = cName + "<br>Course Name: "+CourseModel.Course.COURSENAME;
                    }
                    if (rosteritemnumber > 0)                    
                    //if (rosteritem.Course.eventid != 0 && rosteritem.Course.eventid != null)
                    {
                    //    if (!iseventappended)
                    //    {
                    //        iseventappended = true;
                    //    }
                    //}
                    //else
                    //{
                    //    cName = cName + "<br>Course Number: " + CourseModel.Course.COURSENUM + "<br> Course Name: " + CourseModel.Course.COURSENAME + "<br>Course Date: " + CourseModel.CourseStart.COURSEDATE.Value.ToString(PubDateFormat) + "";
                    //}
                    //if (rosteritemnumber > 0)
                    //{
                        int startindex = 0;
                        int endindex = 0;
                        startindex = ConfirmationBody.IndexOf("{Start Tokens}");
                        startindex = ((startindex >= 0) ? startindex : 0);
                        endindex = ConfirmationBody.IndexOf("{End Tokens}");
                        endindex = ((endindex >= 0) ? endindex : ConfirmationBody.Length - 1);

                        if ((endindex - startindex) > 0)
                        {
                            ConfirmationBody = ConfirmationBody.Substring(startindex, endindex - startindex).Replace("{courseinfoappend}", "");
                        }


                        int appendindex = 0;
                        appendindex = AllCourseConfirmationBody.IndexOf("{courseinfoappend}");
                        appendindex = ((appendindex >= 0) ? appendindex : 0);
                        AllCourseConfirmationBody = AllCourseConfirmationBody.Insert(appendindex, EmailFunction.ReplaceToken(payment_gross, ConfirmationBody, rosteritem, surveypage));
                        //if (rosteritem.Course.eventid != 0 && rosteritem.Course.eventid != null)
                        //{
                        //    AllCourseConfirmationBody = AllCourseConfirmationBody.Insert(appendindex, EmailFunction.ReplaceToken(payment_gross, ConfirmationBody, rosteritem, surveypage));
                        //}
                    }
                    else
                    {
                        int endindex = 0;
                        endindex = ConfirmationBody.IndexOf("{End Tokens}");
                        //endindex = ((endindex >= 0) ? endindex : ConfirmationBody.Length - 1);
                        endindex = ((endindex >= 0) ? endindex : ConfirmationBody.Length);
                        if (endindex < 0)
                        {
                            endindex = 0;
                        }
                        if (endindex == ConfirmationBody.Length)
                        {
                            ConfirmationBody = ConfirmationBody+" <br />{courseinfoappend}";
                        }
                        else
                        {
                            ConfirmationBody = ConfirmationBody.Insert(endindex, "<br />{courseinfoappend}");
                        }
                        AllCourseConfirmationBody = AllCourseConfirmationBody + EmailFunction.ReplaceToken(payment_gross, ConfirmationBody, rosteritem, surveypage);
                    }

                    rosteritemnumber++;
                    var student = Student.GetStudent(Int32.Parse(rosteritem.STUDENTID.ToString()));
                    EmailEntity.EmailTo = student.EMAIL;
                    Stname = student.FIRST + " " + student.LAST;
                    if (!string.IsNullOrEmpty(student.additionalemail)) {
                        if (student.additionalemail.Contains(','))
                        {
                            EmailEntity.EmailCC = student.additionalemail; // format should be comma separated
                        }
                        else
                        {
                            try
                            {
                                MailAddress m = new MailAddress(student.additionalemail);
                                try
                                {
                                    if (student.EMAIL.ToLower() != student.additionalemail.ToLower())
                                        EmailEntity.EmailCC = student.additionalemail;
                                }
                                catch { }
                            }

                            catch
                            {
                            }
                        }
                    }
                    if (rosteritem.CourseExtraParticipants.Count > 0)
                    {
                        foreach (var cep in rosteritem.CourseExtraParticipants)
                        {
                            if (!string.IsNullOrEmpty(cep.StudentEmail))
                            {
                                EmailEntity.EmailTo += ";" + cep.StudentEmail;
                            }
                        }
                    }
                    EmailEntity.EmailBCC += BuildBCCList(Int32.Parse(rosteritem.STUDENTID.ToString()), "enroll", Int32.Parse(rosteritem.COURSEID.ToString())) +";";
                    foreach (string file in Gsmu.Api.Data.School.Course.CourseFilesHelper.GetCourseFileList(rosteritem.COURSEID.Value))
                    {
                        var fi = new FileInfo(file);
                        if (fi.Name.StartsWith(rosteritem.COURSEID.ToString()))
                        {
                            EmailEntity.AttachmentName = EmailEntity.AttachmentName + "|" + file;
                        }
                    }

                    checkoutComments = rosteritem.CheckoutComments;

                    if (rosteritem.IsWaiting)
                    {
                        waitlistcoursecount++;
                    }

                    coursids += rosteritem.COURSEID.ToString() + ",";
                    if (EmailFunction.SendCalendarEventViaEmail != Mail.SendCalendarEventViaEmail.No)
                    {
                        var attachments = MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, "", Convert.ToInt32(rosteritem.COURSEID), 1);

                        if (EmailFunction.IncludeIcs)
                        {
                            //Email include Calendar
                            foreach (string cal_files in attachments)
                            {
                                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
                            }
                        }
                        else
                        {
                            string CalendarEmailCOnfirmSubject = Settings.Instance.GetMasterInfo2().CalendarEmailCOnfirmSubject;
                            string CalendarEmailConfirmbody = Settings.Instance.GetMasterInfo2().CalendarEmailConfirmbody;

                            EmailEntity.EmailSubject = EmailFunction.ReplaceToken(payment_gross, CalendarEmailCOnfirmSubject, rosteritem, surveypage);
                            EmailEntity.EmailBody = EmailFunction.ReplaceToken(payment_gross, CalendarEmailConfirmbody, rosteritem, surveypage);

                            SendIcsEmail(EmailEntity, attachments, CourseModel.Course.COURSENAME);
                        }
                    }
                }
                //EmailEntity.EmailTo = "test.test@test.com";
                EmailEntity.EmailSubject = !resendingModel.SendEmailUsingPreview ? ConfirmationSubject : resendingModel.Subject;
                //append to subject if one or more courses is on waitlist
                if (WebConfiguration.EnrollToWaitList)
                {
                    EmailEntity.EmailSubject += ((waitlistcoursecount > 0) ? " (At least one class is pending " + TerminologyHelper.Instance.GetTermLower(TermsEnum.Supervisor) + "'s approval)" : string.Empty);
                }
                else
                {
                    EmailEntity.EmailSubject += ((waitlistcoursecount > 0) ?" ("+ WebConfiguration.WaitListVerbiage +")" : string.Empty);
                }
                EmailEntity.AuditProcess = "Public Registration";
                if (AuthorizationHelper.CurrentAdminUser != null)
                {
                    EmailEntity.AuditProcess = "Admin Enroll";
                }                
                string EnrollConfirm = ((waitlistcoursecount > 0) ? Settings.Instance.GetMasterInfo2().AltCourseConfirmation : string.Empty);
                AllCourseConfirmationBody = EnrollConfirm + AllCourseConfirmationBody;
                EmailEntity.EmailBody = !resendingModel.SendEmailUsingPreview ? AllCourseConfirmationBody.Replace("{courseinfoappend}", "") : System.Uri.UnescapeDataString(resendingModel.Body);
                EmailEntity.AuditDate = DateTime.Now;

                //BCC Distinct Email and Remove space
                string[] BBCarr = EmailEntity.EmailBCC.Split(';').Where(x => x != string.Empty).Distinct().ToArray();
                EmailEntity.EmailBCC = string.Join(";", BBCarr);

                /*foreach (Course_Roster rosteritem in CourseRosterModel.CourseRosters)
                {
                    if (EmailFunction.SendCalendarEventViaEmail != Mail.SendCalendarEventViaEmail.No)
                    {
                        var attachments = MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, "", Convert.ToInt32(rosteritem.COURSEID), 1);

                        if (EmailFunction.IncludeIcs)
                        {
                            //Email include Calendar
                            foreach (string cal_files in attachments)
                            {
                                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
                            }
                        }
                        else
                        {
                            SendIcsEmail(EmailEntity, attachments);
                        }
                    }

                }*/


                /// WARNING need to change this when applying multiple enrollment.
                if (EmailEntity.EmailTo != null)
                {
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        if (Gsmu.Api.Data.School.Supervisor.SupervisorHelper.ValidateStudentSupervisor(CourseRosterModel.Student.STUDENTID))
                        {
                            if (AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 1 || AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 3)
                            {
                                if (EmailEntity.EmailCC != null)
                                {
                                    EmailEntity.EmailCC = EmailEntity.EmailCC + "," + AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                                }
                                else
                                {
                                    EmailEntity.EmailCC = AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                                }
                            }
                        }
                    }
                    if (callfrom == "cashnetconfirmation")
                    {
                        //for some reason cashnet always posted twice.                        
                        using (var db = new SchoolEntities())
                        {
                            var lookupOrder = (from orderinProgress in db.OrderInProgresses where orderinProgress.OrderNumber == orderNumber || orderinProgress.MasterOrderNumber == orderNumber select orderinProgress).FirstOrDefault();
                            if (lookupOrder.OrderMethod == "CashnetConfirmed")
                            {
                                callfrom = "donotsendconfirm";
                            } else {
                                var orderinpProgress = (from orderinProgress in db.OrderInProgresses where orderinProgress.OrderNumber == orderNumber select orderinProgress).ToList();
                                if (orderinpProgress != null)
                                {
                                    orderinpProgress.ForEach(setcashnetsent => setcashnetsent.OrderMethod = "CashnetConfirmed");
                                    db.SaveChanges();
                                }
                            }   
                        }
                    }
                    if (((isPaidinFull != 1) && (isPaidinFull != -1)) || (callfrom != "donotsendconfirm"))
                    {
                        EmailFunction.SendEmail(EmailEntity, studentid, coursids);
                        if (!String.IsNullOrWhiteSpace(checkoutComments))
                        {
                            checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments;
                            //checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments + "<br><br>** This email has also been sent to the course Instructor(s)";
                            SendCheckOutCommentEmail(checkoutComments, EmailEntity.EmailCC);
                        }
                    }

                    //if ((callfrom == "paypalconfirmation"))
                    //{
                    //    //special code has been removed. Lang has Paypal use the one above
                    //    EmailFunction.SendEmail(EmailEntity, studentid, coursids);
                    //    if (!String.IsNullOrWhiteSpace(checkoutComments))
                    //    {
                    //        checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments;
                    //        //checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments + "<br><br>** This email has also been sent to the course Instructor(s)";
                    //        SendCheckOutCommentEmail(checkoutComments, EmailEntity.EmailCC);
                    //    }
                    //}
                }
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "success",
                    sentStatus = true
                };
            }
            catch (Exception ex)
            {
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
            }

        }

        public EmailReturnModel SendConfirmationEmailFromAdminEnroll(string payment_gross, string orderNumber, string surveypage, string callfrom = "", MailSettings.NetworkingEmailResendingModel resendingModel = null)
        {
            //TODO: Check noregemail setting to see if we should send confirmation email.
            //      Not sure what to do now, since all courses are sent in one email for the order.
            //      It would be confusing for a student to get a confirmation email for a whole
            //      order that doesn't show each course, if the setting is turned off for one
            //      or more courses within the order.
            const int NO_CONFIRMATION_EMAIL = 2;
   
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            EmailFunction EmailFunction = new EmailFunction();
            try
            {
                OrderModel CourseRosterModel = new OrderModel(orderNumber);
                string Stname = "";
                string cName = "";
                string ConfirmationBody = Settings.Instance.GetMasterInfo3().EmailConfirmationContent;
                string ConfirmationSubject = Settings.Instance.GetMasterInfo3().EmailConfirmationSubject;

                int studentid = CourseRosterModel.Student.STUDENTID;

                string coursids = "";
                string AllCourseConfirmationBody = "";
                string checkoutComments = "";
                string PubDateFormat = Settings.Instance.GetPubDateFormat();
                var isPaidinFull = 0;
                int waitlistcoursecount = 0;
                int rosteritemnumber = 0;

                resendingModel = resendingModel == null ? new MailSettings.NetworkingEmailResendingModel() : resendingModel;

                foreach (Course_Roster rosteritem in CourseRosterModel.CourseRosters.Where(x => x.Course.COURSENUM != "~ZZZZZZ~" && (x.Course.coursetype == 0 || x.Course.coursetype == null)))
                {
                    isPaidinFull = rosteritem.PaidInFull;
                    CourseModel CourseModel = new CourseModel(Int32.Parse(rosteritem.COURSEID.ToString()));
                    if (CourseModel.Course.NoRegEmail == NO_CONFIRMATION_EMAIL || CourseModel.Course.NoRegEmail == 3)
                    {
                        continue;
                    }

                    cName = cName + "<br>Course Number: " + CourseModel.Course.COURSENUM + "<br> Course Name: " + CourseModel.Course.COURSENAME + "<br>Course Date: " + CourseModel.CourseStart.COURSEDATE.Value.ToString(PubDateFormat) + "";
                    if (rosteritemnumber > 0)
                    {
                        int startindex = 0;
                        int endindex = 0;
                        startindex = ConfirmationBody.IndexOf("{Start Tokens}");
                        startindex = ((startindex >= 0) ? startindex : 0);
                        endindex = ConfirmationBody.IndexOf("{End Tokens}");
                        endindex = ((endindex >= 0) ? endindex : ConfirmationBody.Length - 1);

                        if ((endindex - startindex) > 0)
                        {
                            ConfirmationBody = ConfirmationBody.Substring(startindex, endindex - startindex).Replace("{courseinfoappend}", "");
                        }

                        int appendindex = 0;
                        appendindex = AllCourseConfirmationBody.IndexOf("{courseinfoappend}");
                        appendindex = ((appendindex >= 0) ? appendindex : 0);
                        AllCourseConfirmationBody = AllCourseConfirmationBody.Insert(appendindex, EmailFunction.ReplaceToken(payment_gross, ConfirmationBody, rosteritem, surveypage));
                    }
                    else
                    {
                        int endindex = 0;
                        endindex = ConfirmationBody.IndexOf("{End Tokens}");
                        endindex = ((endindex >= 0) ? endindex : ConfirmationBody.Length);
                        if (endindex < 0)
                        {
                            endindex = 0;
                        }
                        if (endindex == ConfirmationBody.Length)
                        {
                            ConfirmationBody = ConfirmationBody + " <br />{courseinfoappend}";
                        }
                        else
                        {
                            ConfirmationBody = ConfirmationBody.Insert(endindex, "<br />{courseinfoappend}");
                        }
                      
                        AllCourseConfirmationBody = AllCourseConfirmationBody + EmailFunction.ReplaceToken(payment_gross, ConfirmationBody, rosteritem, surveypage);
                    }

                    rosteritemnumber++;
                    var student = Student.GetStudent(Int32.Parse(rosteritem.STUDENTID.ToString()));
                    EmailEntity.EmailTo = student.EMAIL;
                    Stname = student.FIRST + " " + student.LAST;
                    if (!string.IsNullOrEmpty(student.additionalemail))
                    {
                        EmailEntity.EmailCC = student.additionalemail; // format should be comma separated
                    }
                    if (rosteritem.CourseExtraParticipants.Count > 0)
                    {
                        foreach (var cep in rosteritem.CourseExtraParticipants)
                        {
                            if (!string.IsNullOrEmpty(cep.StudentEmail))
                            {
                                EmailEntity.EmailTo += ";" + cep.StudentEmail;
                            }
                        }
                    }

                    EmailEntity.EmailBCC = BuildBCCList(Int32.Parse(rosteritem.STUDENTID.ToString()), "enroll", Int32.Parse(rosteritem.COURSEID.ToString()));
                    EmailEntity.EmailBCC.Replace(student.EMAIL, "");
                    foreach (string file in Gsmu.Api.Data.School.Course.CourseFilesHelper.GetCourseFileList(rosteritem.COURSEID.Value))
                    {
                        var fi = new FileInfo(file);
                        if (fi.Name.StartsWith(rosteritem.COURSEID.ToString()))
                        {
                            EmailEntity.AttachmentName = EmailEntity.AttachmentName + "|" + file;
                        }
                    }

                    checkoutComments = rosteritem.CheckoutComments;

                    if (rosteritem.IsWaiting)
                    {
                        waitlistcoursecount++;
                    }

                    coursids += rosteritem.COURSEID.ToString() + ",";
                    if (EmailFunction.SendCalendarEventViaEmail != Mail.SendCalendarEventViaEmail.No)
                    {
                        var attachments = MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, "", Convert.ToInt32(rosteritem.COURSEID), 1);

                        if (EmailFunction.IncludeIcs)
                        {
                            //Email include Calendar
                            foreach (string cal_files in attachments)
                            {
                                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
                            }
                        }
                        else
                        {
                            string CalendarEmailCOnfirmSubject = Settings.Instance.GetMasterInfo2().CalendarEmailCOnfirmSubject;
                            string CalendarEmailConfirmbody = Settings.Instance.GetMasterInfo2().CalendarEmailConfirmbody;

                            EmailEntity.EmailSubject = EmailFunction.ReplaceToken(payment_gross, CalendarEmailCOnfirmSubject, rosteritem, surveypage);
                            EmailEntity.EmailBody = EmailFunction.ReplaceToken(payment_gross, CalendarEmailConfirmbody, rosteritem, surveypage);

                            SendIcsEmail(EmailEntity, attachments, CourseModel.Course.COURSENAME);
                        }
                    }
                }
                //EmailEntity.EmailTo = "test.test@test.com";
                EmailEntity.EmailSubject = !resendingModel.SendEmailUsingPreview ? ConfirmationSubject : resendingModel.Subject;
                //append to subject if one or more courses is on waitlist
                if (WebConfiguration.EnrollToWaitList)
                {
                    EmailEntity.EmailSubject += ((waitlistcoursecount > 0) ? " (At least one class is pending " + TerminologyHelper.Instance.GetTermLower(TermsEnum.Supervisor) + "'s approval)" : string.Empty);
                }
                else
                {
                    EmailEntity.EmailSubject += ((waitlistcoursecount > 0) ? " (" + WebConfiguration.WaitListVerbiage + ")" : string.Empty);
                }
               
                if (AuthorizationHelper.CurrentAdminUser != null)
                {
                    EmailEntity.AuditProcess = "Admin Enroll";
                }
                else
                {
                    EmailEntity.AuditProcess = "Public Registration(Admin)";
                }
                string EnrollConfirm = ((waitlistcoursecount > 0) ? Settings.Instance.GetMasterInfo2().AltCourseConfirmation : string.Empty);
                AllCourseConfirmationBody = EnrollConfirm + AllCourseConfirmationBody;
                EmailEntity.EmailBody = !resendingModel.SendEmailUsingPreview ? AllCourseConfirmationBody.Replace("{courseinfoappend}", "") : System.Uri.UnescapeDataString(resendingModel.Body);
                EmailEntity.AuditDate = DateTime.Now;

                /*foreach (Course_Roster rosteritem in CourseRosterModel.CourseRosters)
                {
                    if (EmailFunction.SendCalendarEventViaEmail != Mail.SendCalendarEventViaEmail.No)
                    {
                        var attachments = MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, "", Convert.ToInt32(rosteritem.COURSEID), 1);

                        if (EmailFunction.IncludeIcs)
                        {
                            //Email include Calendar
                            foreach (string cal_files in attachments)
                            {
                                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
                            }
                        }
                        else
                        {
                            SendIcsEmail(EmailEntity, attachments,"");
                        }
                    }

                }*/


                /// WARNING need to change this when applying multiple enrollment.
                if (EmailEntity.EmailTo != null)
                {
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        if (AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 1 || AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 3)
                        {
                            if (EmailEntity.EmailCC != null)
                            {
                                EmailEntity.EmailCC = EmailEntity.EmailCC + "," + AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                            }
                            else
                            {
                                EmailEntity.EmailCC = AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                            }
                        }
                    }

                    EmailEntity.EmailCC.Replace(EmailEntity.EmailTo, "");
                    if (((isPaidinFull != 1) && (isPaidinFull != -1)) || (callfrom != "paypalconfirmation"))
                    {
                        EmailFunction.SendEmail(EmailEntity, studentid, coursids);
                        if (!String.IsNullOrWhiteSpace(checkoutComments))
                        {
                            checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments;
                            //checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments + "<br><br>** This email has also been sent to the course Instructor(s)";
                            SendCheckOutCommentEmail(checkoutComments, EmailEntity.EmailCC);
                        }
                    }

                    if ((callfrom == "paypalconfirmation"))
                    {
                        EmailFunction.SendEmail(EmailEntity, studentid, coursids);
                        if (!String.IsNullOrWhiteSpace(checkoutComments))
                        {
                            checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments;
                            //checkoutComments = "<br>Student Name: " + Stname + "<br>Email: " + EmailEntity.EmailTo + "<br>Course Details:" + cName + "<br>Checkout Comments:" + checkoutComments + "<br><br>** This email has also been sent to the course Instructor(s)";
                            SendCheckOutCommentEmail(checkoutComments, EmailEntity.EmailCC);
                        }
                    }
                }
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "success",
                    sentStatus = true
                };
            }
            catch (Exception ex)
            {

                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
            }

        }

        private void SendIcsEmail(EmailAuditTrail fromEntity, List<string> attachments, string icscoursename)
        {
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            EmailEntity.EmailBody = fromEntity.EmailBody;
            EmailEntity.EmailTo = fromEntity.EmailTo;
            EmailEntity.EmailFrom = fromEntity.EmailFrom;
            EmailEntity.EmailSubject = fromEntity.EmailSubject;
            EmailEntity.AuditDate = DateTime.Now;
            EmailEntity.AuditProcess = "Public Registration ICS";

            //Email include Calendar
            foreach (string cal_files in attachments)
            {
                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
            }
            
            EmailFunction.SendEmail(EmailEntity);

        }

        public EmailReturnModel SendCancellationEmail(int RosterId, MailSettings.NetworkingEmailResendingModel resendingModel = null)
        {
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            try
            {
                const int NO_CANCELLATION_EMAIL = 4;
                bool SendCancellationEmail = true;
                EmailFunction EmailFunction = new EmailFunction();

                string CancellationBody = Settings.Instance.GetMasterInfo().EmailCancelBody;
                string CancellationSubject = Settings.Instance.GetMasterInfo().EmailCancelSubject;
                int studentId = 0;
                string coursids = "";

                using (var db = new SchoolEntities())
                {
                    Course_Roster rosteritem = db.Course_Rosters.Where(x => (x.RosterID == RosterId)).First();
                    if (resendingModel != null)
                    {
                        CancellationBody = !resendingModel.SendEmailUsingPreview ? EmailFunction.ReplaceToken("0", CancellationBody, rosteritem, "") : resendingModel.Body;
                        CancellationSubject = !resendingModel.SendEmailUsingPreview ? EmailFunction.ReplaceToken("0", CancellationSubject, rosteritem, "") : resendingModel.Subject;
                    }
                    else 
                    {
                        CancellationBody = EmailFunction.ReplaceToken("0", CancellationBody,rosteritem,"");
                        CancellationSubject = EmailFunction.ReplaceToken("0", CancellationSubject, rosteritem, "");
                    }
                    Student Student = Student.GetStudent(Int32.Parse(rosteritem.STUDENTID.ToString()));
                    EmailEntity.EmailTo = Student.EMAIL;
                    if (!string.IsNullOrEmpty(Student.additionalemail))
                    {
                         EmailEntity.EmailCC = Student.additionalemail; // format should be comma separated
                    }
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        if (Gsmu.Api.Data.School.Supervisor.SupervisorHelper.ValidateStudentSupervisor(Student.STUDENTID))
                        {
                            if (AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 2 || AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 3)
                            {
                                if (EmailEntity.EmailCC != null)
                                {
                                    EmailEntity.EmailCC = EmailEntity.EmailCC + "," + AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                                }
                                else
                                {

                                    EmailEntity.EmailCC = AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                                }
                            }
                        }
                    }
                    // Get course email options
                    int courseid = (int)(rosteritem.COURSEID.HasValue ? rosteritem.COURSEID : 0);
                    CourseModel CourseModel = new CourseModel(courseid);
                    if (CourseModel.Course.NoRegEmail == NO_CANCELLATION_EMAIL || CourseModel.Course.NoRegEmail == 3)
                    {
                        SendCancellationEmail = false;
                    }
                    studentId = Int32.Parse(rosteritem.STUDENTID.ToString());
                    coursids += courseid.ToString() + ",";
                    EmailEntity.EmailBCC = BuildBCCList(Int32.Parse(rosteritem.STUDENTID.ToString()), "cancellation", Int32.Parse(rosteritem.COURSEID.ToString()));
                    if (EmailFunction.SendCalendarEventViaEmail != Mail.SendCalendarEventViaEmail.No)
                    {
                        var attachments = MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, "", Convert.ToInt32(rosteritem.COURSEID), 2);

                        if (EmailFunction.IncludeIcs)
                        {
                            //Email include Calendar
                            foreach (string cal_files in attachments)
                            {
                                EmailEntity.AttachmentNameMemo = EmailEntity.AttachmentNameMemo + "|" + cal_files;
                            }
                        }
                        else
                        {
                            string CalendarEmailCOnfirmSubject = Settings.Instance.GetMasterInfo2().CalendarEmailCOnfirmSubject;
                            string CalendarEmailConfirmbody = Settings.Instance.GetMasterInfo2().CalendarEmailConfirmbody;

                            EmailEntity.EmailSubject = EmailFunction.ReplaceToken("0", CalendarEmailCOnfirmSubject, rosteritem, "");
                            EmailEntity.EmailBody = EmailFunction.ReplaceToken("0", CalendarEmailConfirmbody, rosteritem, "");

                            SendIcsEmail(EmailEntity, attachments,"");
                        }
                    }

                }

                EmailEntity.EmailSubject = CancellationSubject;
                EmailEntity.AuditProcess = "Public Cancellation";
                EmailEntity.EmailBody = CancellationBody;
                EmailEntity.AuditDate = DateTime.Now;
                if (SendCancellationEmail)
                {
                    EmailFunction.SendEmail(EmailEntity, studentId, coursids);
                }
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "success",
                    sentStatus = true
                };
            }
            catch (Exception ex)
            {
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
                HttpContext.Current.Response.Write("<script>console.log('error_message': '" + ex.Message + "')</script>");
            }
        }

        public EmailReturnModel SendWaitListToEnrollEMail(Course_Roster roster, MailSettings.NetworkingEmailResendingModel resendingModel = null) 
        {
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            try
            {
                resendingModel = resendingModel == null ? new MailSettings.NetworkingEmailResendingModel() : resendingModel;
                string waitlistemailSubject = !resendingModel.SendEmailUsingPreview ? Settings.Instance.GetMasterInfo().EnrolledFromWaitingSubject : resendingModel.Subject;
                string waitlistemailBody = !resendingModel.SendEmailUsingPreview ? Settings.Instance.GetMasterInfo().EnrolledFromWaitingBody : resendingModel.Body;
                string emailBody = ReplaceToken("0.00", waitlistemailBody, roster, "");

                if (!string.IsNullOrEmpty(resendingModel.EmailAddressTo))
                {
                    EmailEntity.EmailTo = roster.Student.EMAIL;
                    EmailEntity.EmailCC = roster.Student.additionalemail;
                }
                else 
                {
                    EmailEntity.EmailTo = resendingModel.EmailAddressTo;
                }
                EmailEntity.EmailBCC = BuildBCCList(Int32.Parse(roster.STUDENTID.ToString()), "enroll", Int32.Parse(roster.COURSEID.ToString()));
                EmailEntity.EmailFrom = Settings.Instance.GetMasterInfo().EmailAddress;
                EmailEntity.EmailSubject = waitlistemailSubject;
                EmailEntity.EmailBody = emailBody;
                EmailEntity.EmailTo = roster.Student.EMAIL;
                EmailEntity.AuditDate = DateTime.Now;
                EmailEntity.AuditProcess = "Public Waitlist to Enroll";
                EmailFunction.SendEmail(EmailEntity);
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "success",
                    sentStatus = true
                };
            }catch(Exception ex)
            {
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
            }

        }
        public EmailReturnModel SendWaitListToEnrollEMail(int RosterId, MailSettings.NetworkingEmailResendingModel resendingModel = null) 
        {
            using (var db = new SchoolEntities())
            {
                var roster = db.Course_Rosters.Where(r => r.RosterID == RosterId).SingleOrDefault();
                return SendWaitListToEnrollEMail(roster, resendingModel);
            }
        }
        public EmailReturnModel SendWaitListToApproveEmail(Course_Roster roster) {
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            try
            {
                string subject = "You have been approved";
                string body = @"Dear {StudentName},
                                <br /><br />
                                This message is to let you know that you have been approved for {CourseName} on {CourseDates} . <br /> 
                                If space becomes available then you will be put in the course and you will be notified at that time.
                                <br /><br />
                                Thank You";
                string emailBody = ReplaceToken("0.00", body, roster, "");

  
                EmailEntity.EmailBCC = GetBCCAdmin();
                EmailEntity.EmailFrom = Settings.Instance.GetMasterInfo().EmailAddress;
                EmailEntity.EmailSubject = subject;
                EmailEntity.EmailBody = emailBody;
                EmailEntity.EmailTo = roster.Student.EMAIL;
                EmailEntity.AuditDate = DateTime.Now;
                EmailEntity.AuditProcess = "Public Waitlist to Approve";
                EmailFunction.SendEmail(EmailEntity);
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "success",
                    sentStatus = true
                };
            }
            catch (Exception ex)
            {
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
            }
        }

        private void SendCheckOutCommentEmail(string CheckoutComment, string instructorEmail)
        {
            if (Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo2().EmailCOComment))
            {
                EmailAuditTrail EmailEntity = new EmailAuditTrail();
                EmailEntity.EmailBody = Settings.Instance.GetMasterInfo2().EmailCOCommentText +"<br>" +CheckoutComment;
                EmailEntity.EmailTo = Settings.Instance.GetMasterInfo2().EmailCOCommentTo;
                EmailEntity.EmailSubject = Settings.Instance.GetMasterInfo2().CheckoutCommentsLabel;
                EmailEntity.AuditDate = DateTime.Now;
                EmailEntity.AuditProcess = "Public Registration";
                if (AuthorizationHelper.CurrentAdminUser != null)
                {
                    EmailEntity.AuditProcess = "Admin Enroll";
                }
                if (Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo2().EmailCOCommentCC))
                {
                    EmailEntity.EmailCC = instructorEmail;
                }

                EmailFunction.SendEmail(EmailEntity);
            }
        }

        public EmailReturnModel SendAutomaticCreditToCardEmail(int RosterId, string CreditAmt)
        {
            int studentId = 0;
            string coursids = "";
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
             try
            {

                using (var db = new SchoolEntities())
                {
                    Course_Roster rosteritem = db.Course_Rosters.Where(x => (x.RosterID == RosterId)).First();
                    Student Student = Student.GetStudent(Int32.Parse(rosteritem.STUDENTID.ToString()));
                    EmailEntity.EmailTo = Student.EMAIL;
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        if (AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 2 || AuthorizationHelper.CurrentSupervisorUser.NOTIFY == 3)
                        {
                            if (EmailEntity.EmailCC != null)
                            {
                                EmailEntity.EmailCC = EmailEntity.EmailCC + "," + AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                            }
                            else
                            {
                                EmailEntity.EmailCC = AuthorizationHelper.CurrentSupervisorUser.EMAIL;
                            }
                        }
                    }
                    // Get course email options
                    int courseid = (int)(rosteritem.COURSEID.HasValue ? rosteritem.COURSEID : 0);
                    studentId = Int32.Parse(rosteritem.STUDENTID.ToString());
                    coursids += courseid.ToString() + ",";


                   String emailbody = "------------------------------------------------------------------------<br>";
                    emailbody += "A credit has been applied to your credit card in the amount of: "+ CreditAmt +"<br>";
                    emailbody += "CourseID = " + coursids + "<br>";
                    emailbody += "StudentID = " + studentId + "<br>";
                    emailbody += "------------------------------------------------------------------------<br>";

                    EmailEntity.EmailBody = emailbody;
                    //EmailEntity.EmailTo = EmailTo;
                    EmailEntity.EmailSubject = "Credit Applied";
                    EmailEntity.AuditDate = DateTime.Now;
                    EmailEntity.AuditProcess = "Credit Applied";
                    EmailFunction.SendEmail(EmailEntity);

                    return new EmailReturnModel()
                    {
                        emailAuditTrailInfo = EmailEntity,
                        status = "success",
                        sentStatus = true
                    };
                }
             }
            catch (Exception ex)
            {
                return new EmailReturnModel()
                {
                    emailAuditTrailInfo = EmailEntity,
                    status = "failed",
                    sentStatus = false,
                    errorMessage = ex.Message
                };
            }

        }
        private string GetCourseBCCInstructor(int courseId = 0)
        {
            CourseModel CourseModel = new CourseModel(courseId);
            string result = "";
            if (CourseModel.Course.SendConfirmationEmailtoInstructor != 0)
            {
                // Send email to instructor 1
                if (CourseModel.Course.INSTRUCTORID != null && CourseModel.Course.INSTRUCTORID > 0)
                {
                    InstructorModel InstructorModel = new InstructorModel(int.Parse(CourseModel.Course.INSTRUCTORID.ToString()));
                    if (InstructorModel.Instructor != null)
                    {
                        result += (InstructorModel.Instructor.EMAIL.Length > 0 ? InstructorModel.Instructor.EMAIL : "");
                    }
                }

                // Send email to instructor 2, seperate by ; if instructor 1 exists
                if (CourseModel.Course.INSTRUCTORID2 != null && CourseModel.Course.INSTRUCTORID2 > 0)
                {
                    InstructorModel InstructorModel = new InstructorModel(int.Parse(CourseModel.Course.INSTRUCTORID2.ToString()));
                    if (InstructorModel.Instructor != null)
                    {
                        if (result.Length > 0) result += ";";
                        result += (InstructorModel.Instructor.EMAIL.Length > 0 ? InstructorModel.Instructor.EMAIL : "");
                    }
                }

                // Send email to instructor 3, seperate by ; if instructor 1 or 2 exist
                if (CourseModel.Course.INSTRUCTORID3 != null && CourseModel.Course.INSTRUCTORID3 > 0)
                {
                    InstructorModel InstructorModel = new InstructorModel(int.Parse(CourseModel.Course.INSTRUCTORID3.ToString()));
                    if (InstructorModel.Instructor != null)
                    {
                        if (result.Length > 0) result += ";";
                        result += (InstructorModel.Instructor.EMAIL.Length > 0 ? InstructorModel.Instructor.EMAIL : "");
                    }
                }
            }
            return result;
        }

        private string GetBCCAdmin()
        {
            string result = "";
            //0 = off, 1 = simple, 2 = complex
            if (Settings.Instance.GetMasterInfo().BCCEmailAdmin > 0)
            {
                if (Settings.Instance.GetMasterInfo().PublicEmailAddress != null && Settings.Instance.GetMasterInfo().PublicEmailAddress.Length > 0)
                {
                    result += Settings.Instance.GetMasterInfo().PublicEmailAddress;
                }
            }
            return result;
        }

        private string BuildBCCList(int studentid,string action,int courseId = 0)
        {
            string bccList = string.Empty;
            //get admin email
            string adminList = GetBCCAdmin();
            //get instructor emails
            if (action != "cancellation")
            {
                string instructorList = GetCourseBCCInstructor(courseId);
                //concat instructor and admin emails
                bccList += instructorList;
            }


            if (adminList.Length > 0)
            {
                bccList += ((bccList.Length > 0) ? ";" : string.Empty);
                bccList += adminList;
            }

            string supervisorlist = GetStudentSupervisor(studentid,action);
            if (supervisorlist.Length > 0)
            {
                bccList += ((bccList.Length > 0) ? ";" : string.Empty);
                bccList += supervisorlist;
            }
            return bccList;
        }

        private string GetStudentSupervisor(int studentid,string action)
        {
            string supervisoremail = "";
            Student Student = Student.GetStudent(studentid);

            var Supervisors = Student.GetStudentSupervisors(Student.SCHOOL, Student.DISTRICT, studentid);
            if (Supervisors != null)
            {
                foreach (Supervisor sup in Supervisors)
                {

                    if ((action == "cancellation" && (sup.NOTIFY == 2 || sup.NOTIFY == 3)) || (action == "enroll" && (sup.NOTIFY == 1 || sup.NOTIFY == 3)))
                    {
                        if (supervisoremail.Length > 0)
                        {
                            supervisoremail = supervisoremail + ";";
                        }
                        supervisoremail = supervisoremail + sup.EMAIL;

                        if (sup.AdditionalEmailAddresses != "")
                        {
                            supervisoremail = supervisoremail + ";" + sup.AdditionalEmailAddresses;
                        }

                    }
                }
            }

            return supervisoremail;


        }

        public string GetStudentSupervisorEmails(int studentid, string action)
        {
            return GetStudentSupervisor(studentid, action);
        }

        public string ReplaceToken(string payment_gross, string EmailBody, Course_Roster Roster, string serverurl)
        {
            string PubDateFormat = Settings.Instance.GetPubDateFormat();
            try
            {
                CourseModel CourseModel = new CourseModel(Int32.Parse(Roster.COURSEID.ToString()));
                Student Student = Student.GetStudent(Int32.Parse(Roster.STUDENTID.ToString()));
                Parent Parent = Student.GetParent(Int32.Parse(Roster.STUDENTID.ToString()));
                decimal gross = decimal.Parse(payment_gross);
                string enrollStatus = ((Roster.WAITING == 0) ? TerminologyHelper.Instance.GetTermCapital(TermsEnum.Enrolled) : "On Waiting List");
                string resetLink = "";
                string DotNetSiteRootUrl = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl;
                var newLine = Environment.NewLine;
                var Context = new SchoolEntities();

                if (EmailBody.Contains("{resetLink}"))
                {

                    string curToken = "pw" + UserDashQueries.GenRandomKey().ToLower();
                    resetLink = DotNetSiteRootUrl + "/Public/User/AccountResetPass?stype=S&username=" + Student.USERNAME + "&resettoken=" + curToken + "&misc=" + DateTime.Now;
                    Gsmu.Api.Data.School.Entities.Student student = Context.Students.First(st => st.USERNAME == Student.USERNAME);
                    
                    student.resetPasswordHash = curToken;
                    student.resetPasswordDate = DateTime.Now;
                    Context.SaveChanges();
                }
                EmailBody = EmailBody.Replace("{RosterId}",Roster.RosterID.ToString());
                EmailBody = EmailBody.Replace("{TotalAmount}", gross.ToString("C"));
                EmailBody = EmailBody.Replace("{CardNumber}", Roster.CardNumber ?? "");
                EmailBody = EmailBody.Replace("{Date}", DateTime.Now.ToString(PubDateFormat));
                EmailBody = EmailBody.Replace("{OrderNumber}", Roster.OrderNumber ?? "");
                EmailBody = EmailBody.Replace("{SalesTax}", Roster.CourseSalesTaxPaid.ToString());
                EmailBody = EmailBody.Replace("{OtherPayNumber}", Roster.payNumber ?? "");
                EmailBody = EmailBody.Replace("{OrderNumber}", Roster.OrderNumber ?? "");
                EmailBody = EmailBody.Replace("{CancellationNumber}", Roster.CancelNumber ?? "");
                EmailBody = EmailBody.Replace("{CancellationDate}", (Roster.CancelDate.HasValue ? Roster.CancelDate.Value.ToShortDateString() : ""));
                EmailBody = EmailBody.Replace("{CouponCode}", Roster.CouponCode ?? "");
                EmailBody = EmailBody.Replace("{PaymentMethod}", Roster.PAYMETHOD ?? "");

                //AMOUNT DUE
                if (EmailBody.Contains("{AmountDue}"))
                {
                    string courseCostRaw = string.IsNullOrEmpty(Roster.CourseCost) ? "0" : Roster.CourseCost.Replace("$", "");
                    decimal courseCost = Decimal.Parse(courseCostRaw);
                    decimal amountDue = 0;
                    if (Roster.PaidInFull == 0)
                    {
                        amountDue = new OrderModel(Roster.OrderNumber, "usermodel").OrderTotalOptimized;
                    }
                    EmailBody = EmailBody.Replace("{AmountDue}", amountDue.ToString("C") ?? "0.00");
                }
                

                //StudentChoiceCourse
                if (EmailBody.Contains("{ShowCourseType}"))
                {
                    string courseChoiceInfo = string.Empty;
                    int ? course_choice_id = Roster.StudentChoiceCourse;
                    if (course_choice_id != null)
                    {
                        Gsmu.Api.Data.School.Entities.CourseChoice courseChoice = Context.CourseChoices.FirstOrDefault(cc => cc.CourseChoiceId == course_choice_id);
                        if (courseChoice != null)
                        {
                            courseChoiceInfo = courseChoice.CourseChoice1;
                        }
                    }
                    EmailBody = EmailBody.Replace("{ShowCourseType}", courseChoiceInfo ?? "");
                }

                // COURSE EXTRA PARTICIPANTS / HOUSEHOLD
                if (EmailBody.Contains("{HouseHold}"))
                {
                    var houseHold = Context.CourseExtraParticipants.Where(ce => ce.RosterId == Roster.RosterID).ToList();
                    string houseHoldNames = "No household included.";
                    if (houseHold.Count() > 0)
                    {
                        houseHoldNames = string.Empty;
                        foreach(var houseHoldData in houseHold)
                        {
                            string firstname = houseHoldData.StudentFirst;
                            string lastname = houseHoldData.StudentLast;
                            houseHoldNames += firstname + " " + lastname + " <br />";
                        }
                    }
                    EmailBody = EmailBody.Replace("{HouseHold}", houseHoldNames);
                }

                if (Roster.CouponDiscount != null)
                {
                    EmailBody = EmailBody.Replace("{CouponDiscount}", Roster.CouponDiscount.ToString());
                }
                EmailBody = EmailBody.Replace("{UserName}", Student.USERNAME ?? "");
                EmailBody = EmailBody.Replace("{resetLink}", resetLink ?? "");
                //TODO: show parent name, not id
                if (Parent != null)
                {
                    EmailBody = EmailBody.Replace("{ParentsName}", Parent.ParentsFirstName + " " + Parent.ParentsLastName);
                }
                EmailBody = EmailBody.Replace("{FirstName}", Student.FIRST ?? "");
                EmailBody = EmailBody.Replace("{LastName}", Student.LAST ?? "");
                EmailBody = EmailBody.Replace("{StudentName}", Student.FIRST + " " + Student.LAST); //added to match old site
                EmailBody = EmailBody.Replace("{Address}", Student.ADDRESS ?? "");
                EmailBody = EmailBody.Replace("{City}", Student.CITY ?? "");
                EmailBody = EmailBody.Replace("{State}", Student.STATE ?? "");
                EmailBody = EmailBody.Replace("{Zip}", Student.ZIP ?? "");
                EmailBody = EmailBody.Replace("{Phone}", Student.HOMEPHONE ?? "");
                EmailBody = EmailBody.Replace("{Email}", Student.EMAIL ?? "");
                EmailBody = EmailBody.Replace("{CourseName}", CourseModel.Course.COURSENAME ?? "");


                if (CourseModel.Course.COURSENUM != "~ZZZZZZ~")
                {
                    EmailBody = EmailBody.Replace("{CourseNumber}", CourseModel.Course.COURSENUM ?? "");
                }
                else
                {
                    EmailBody = EmailBody.Replace("{CourseNumber}","");
                }

                EmailBody = EmailBody.Replace("{studregfield1}", Student.StudRegField1 ?? "");
                EmailBody = EmailBody.Replace("{studregfield2}", Student.StudRegField2 ?? "");
                EmailBody = EmailBody.Replace("{studregfield3}", Student.StudRegField3 ?? "");
                EmailBody = EmailBody.Replace("{studregfield4}", Student.StudRegField4 ?? "");
                EmailBody = EmailBody.Replace("{studregfield5}", Student.StudRegField5 ?? "");
                EmailBody = EmailBody.Replace("{studregfield6}", Student.StudRegField6 ?? "");
                EmailBody = EmailBody.Replace("{studregfield7}", Student.StudRegField7 ?? "");
                EmailBody = EmailBody.Replace("{studregfield8}", Student.StudRegField8 ?? "");
                EmailBody = EmailBody.Replace("{studregfield9}", Student.StudRegField9 ?? "");
                EmailBody = EmailBody.Replace("{studregfield10}", Student.StudRegField10 ?? "");

                try
                {
                    if (Student.DISTRICT != null)
                    {
                        string myDistrictToken = "{" + Settings.Instance.GetMasterInfo().Field3Name.ToString() + "}";
                        if (!string.IsNullOrEmpty(Student.DISTRICT.ToString()))
                        {
                            EmailBody = EmailBody.Replace("{" + Settings.Instance.GetMasterInfo().Field3Name + "}", Gsmu.Api.Data.School.District.Queries.GetDistrictById(Int32.Parse(Student.DISTRICT.ToString())).DISTRICT1.ToString());
                        }
                    }
                    else
                    {
                        EmailBody = EmailBody.Replace("{" + Settings.Instance.GetMasterInfo().Field3Name + "}", "");
                    }
                    if (Student.SCHOOL.HasValue)
                    {
                        EmailBody = EmailBody.Replace("{SchoolName}", Gsmu.Api.Data.School.School.Queries.GetSchoolById(Int32.Parse(Student.SCHOOL.ToString())).LOCATION.ToString());
                    }
                    else
                    {
                        EmailBody = EmailBody.Replace("{SchoolName}", "");
                    }
                    if ((Student.GRADE != null) && (Student.GRADE != 0))
                    {
                        EmailBody = EmailBody.Replace("{" + Settings.Instance.GetMasterInfo().Field1Name + "}", Gsmu.Api.Data.School.Grade.Queries.GetGradeById(Int32.Parse(Student.GRADE.ToString())).GRADE);
                    }
                    else
                    {
                        EmailBody = EmailBody.Replace("{" + Settings.Instance.GetMasterInfo().Field1Name + "}", "");
                    }
                }
                catch
                {

                }

                //Course Specific Tokens:
                //EmailBody = EmailBody.Replace("{EventDetails}", CourseModel.Course.EVENTNUM ?? "");
                //EmailBody = EmailBody.Replace("{EnrollStatus} ", CourseModel.Course.MAXENROLL.ToString());
                EmailBody = EmailBody.Replace("{ExtraParticipant}", Roster.ExtraParticipant ?? "");
                EmailBody = EmailBody.Replace("{EnrollStatus}", enrollStatus ?? "");
                EmailBody = EmailBody.Replace("{CourseName}", CourseModel.Course.COURSENAME ?? "");


                if (CourseModel.Course.COURSENUM != "~ZZZZZZ~")
                {
                    EmailBody = EmailBody.Replace("{CourseNum}", CourseModel.Course.COURSENUM ?? "");
                    EmailBody = EmailBody.Replace("{CourseNumber}", CourseModel.Course.COURSENUM ?? ""); //added to match old site
                }
                else
                {
                    EmailBody = EmailBody.Replace("{CourseNumber}", "");
                    EmailBody = EmailBody.Replace("{CourseNumber}",""); //added to match old site
                    EmailBody = EmailBody.Replace("{CourseDateTime}", "");
                    EmailBody = EmailBody.Replace("{CourseDates}", ""); //added to match old site
                    EmailBody = EmailBody.Replace("{CourseStartDate}", "");
                    EmailBody = EmailBody.Replace("{Location}","");
                    EmailBody = EmailBody.Replace("{locationAddtionalInfo}","");
                    EmailBody = EmailBody.Replace("{CourseLocationAddress}","");
                    EmailBody = EmailBody.Replace("{LocationURL}","");
                    EmailBody = EmailBody.Replace("{RoomBookInfo}", "");
                    EmailBody = EmailBody.Replace("{RoomInfo}", "");
                }

                EmailBody = EmailBody.Replace("{CourseId}", CourseModel.Course.COURSEID.ToString());
                if (CourseModel.Course.sessionid != null)
                {
                    EmailBody = EmailBody.Replace("{EventDetails}", GetEventSessionDetails( CourseModel.Course.sessionid.Value, true));
                }
                else
                {
                    EmailBody = EmailBody.Replace("{EventDetails}", "");
                }
                string DateandTime = "";
                string StartDate = "";
                string semicoln = "";
                var timeIndex = 0;
                foreach (var datetime in CourseModel.CourseTimes)
                {
                    timeIndex++;


                    if (CourseModel.Course.IsOnlineCourse && CourseModel.Course.coursetype == 0)
                    {
                        if (timeIndex == 1)
                        {
                            DateandTime = "Online from " + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.STARTTIME.Value.ToShortTimeString();
                            StartDate = datetime.COURSEDATE.Value.ToString(PubDateFormat);
                        }
                        else
                        {
                                DateandTime += " until " + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.FINISHTIME.Value.ToShortTimeString();
                                
                        }
                    }
                    else
                    {

                        DateandTime = DateandTime + semicoln + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.STARTTIME.Value.ToShortTimeString() + " - " + datetime.FINISHTIME.Value.ToShortTimeString() + "";
                        semicoln = "; ";
                        if (string.IsNullOrWhiteSpace(StartDate))
                        {
                            StartDate = datetime.COURSEDATE.Value.ToString(PubDateFormat);
                        }
                    }

                }
                if (CourseModel.Course.StartEndTimeDisplay != "" && CourseModel.Course.StartEndTimeDisplay != null)
                {
                    EmailBody = EmailBody.Replace("{CourseDateTime}", CourseModel.Course.StartEndTimeDisplay);
                    EmailBody = EmailBody.Replace("{CourseDates}", CourseModel.Course.StartEndTimeDisplay); //added to match old site
                    EmailBody = EmailBody.Replace("{CourseStartDate}", CourseModel.Course.StartEndTimeDisplay);
                }
                else
                {
                    EmailBody = EmailBody.Replace("{CourseDateTime}", DateandTime);
                    EmailBody = EmailBody.Replace("{CourseDates}", DateandTime); //added to match old site
                    EmailBody = EmailBody.Replace("{CourseStartDate}", StartDate);
                }
                try
                {
                    EmailBody = EmailBody.Replace("{CourseCost}", Decimal.Parse(Roster.CourseCost).ToString("C"));
                }
                catch (Exception)
                {
                    EmailBody = EmailBody.Replace("{CourseCost}", Decimal.Parse("0").ToString());
                }

                if (CourseModel.Course.LOCATION != null)
                {
                    EmailBody = EmailBody.Replace("{Location}", CourseModel.Course.LOCATION);
                }
                if (CourseModel.Course.LocationAdditionalInfo != null)
                {
                    EmailBody = EmailBody.Replace("{locationAddtionalInfo}", CourseModel.Course.LocationAdditionalInfo);
                }
                EmailBody = EmailBody.Replace("{CourseLocationAddress}", (CourseModel.Course.STREET + ", " + CourseModel.Course.CITY + ", " + CourseModel.Course.STATE + " " + CourseModel.Course.ZIP));
                if (CourseModel.Course.ContactName != null)
                {
                    EmailBody = EmailBody.Replace("{ContactName}", CourseModel.Course.ContactName);
                }

                if (CourseModel.Course.LOCATIONURL != "" && CourseModel.Course.LOCATIONURL != null)
                {
                    EmailBody = EmailBody.Replace("{LocationURL}", CourseModel.Course.LOCATIONURL.Replace(" ", "%20"));
                }
                else
                {
                    if (!string.IsNullOrEmpty(CourseModel.Course.STREET) || !string.IsNullOrEmpty(CourseModel.Course.CITY) || !string.IsNullOrEmpty(CourseModel.Course.STATE))
                        EmailBody = EmailBody.Replace("{LocationURL}", "https://maps.google.com/?q=" + CourseModel.Course.STREET.Replace(" ", "%20") + "%20" + CourseModel.Course.CITY.Replace(" ", "%20") + "%20" + CourseModel.Course.STATE);
                    else
                    {
                        EmailBody = EmailBody.Replace(@"href=""{LocationURL}""", " style=display:none");
                        EmailBody = EmailBody.Replace("{LocationURL}", "");
                    }
                }



                EmailBody = EmailBody.Replace("{ContactPhone}", CourseModel.Course.ContactPhone ?? "");
                if (CourseModel.Course.RoomDirection != null)
                {
                    EmailBody = EmailBody.Replace("{RoomDirectionsInfo}", CourseModel.Course.RoomDirection.RoomDirectionsTitle + ' ' + CourseModel.Course.RoomDirection.RoomDirectionsInfo);
                }
                else
                {
                    EmailBody = EmailBody.Replace("{RoomDirectionsInfo}", "");
                }
                if (CourseModel.Course.ROOM != null)
                {
                    EmailBody = EmailBody.Replace("{RoomBookInfo}", CourseModel.Course.ROOM);
                    EmailBody = EmailBody.Replace("{RoomInfo}", CourseModel.Course.ROOM);
                    
                }
                else
                {
                    EmailBody = EmailBody.Replace("{RoomBookInfo}", "");
                    EmailBody = EmailBody.Replace("{RoomInfo}","");
                }


                string strSiteUrl = Settings.Instance.GetMasterInfo().SiteURL ?? "";
                CourseSurvey CourseSurvey = new CourseSurvey(int.Parse(Roster.COURSEID.ToString()));
                string strSurveyLink = "";
                try
                {
                    Survey Survey = new Survey(int.Parse(CourseSurvey.CourseSurveyModel.BeforeCourseSurveyId.ToString()));
                    if (Settings.Instance.GetMasterInfo().RequireSSL == -1)
                    {
                        strSurveyLink = "<a href='https://" + serverurl + "/public/survey/showsurvey?studid=" + Roster.STUDENTID.ToString() + "&sid=" + CourseSurvey.CourseSurveyModel.BeforeCourseSurveyId + "&cid=" + Roster.COURSEID.ToString() + "&misc=" + DateTime.Now + "'>" + Survey.SurveyModel.Name + "</a>";
                    }
                    else
                    {
                        if (Survey.SurveyModel.SurveyLoginType == 1)
                        {
                            strSurveyLink = "<a href='http://" + serverurl + "/public/survey/showsurvey?studid=" + 0 + "&sid=" + CourseSurvey.CourseSurveyModel.BeforeCourseSurveyId + "&cid=" + Roster.COURSEID.ToString() + "&misc=" + DateTime.Now + "'>" + Survey.SurveyModel.Name + "</a>";

                        }
                        else
                        {
                            strSurveyLink = "<a href='http://" + serverurl + "/public/survey/showsurvey?studid=" + Roster.STUDENTID.ToString() + "&sid=" + CourseSurvey.CourseSurveyModel.BeforeCourseSurveyId + "&cid=" + Roster.COURSEID.ToString() + "&misc=" + DateTime.Now + "'>" + Survey.SurveyModel.Name + "</a>";
                        }
                    }

                }
                catch
                {
                }
                EmailBody = EmailBody.Replace("{BeforeCourseSurvey}", strSurveyLink ?? "");
                EmailBody = EmailBody.Replace("{CourseExtraText}", CourseModel.Course.CourseConfirmationEmailExtraText ?? "");
                if (CourseModel.Course.coursetype != null)
                {
                    EmailBody = EmailBody.Replace("{ShowCourseType}", CourseModel.Course.coursetype.ToString());
                }
                EmailBody = EmailBody.Replace("{G2WJoinURL}", "");
                if (CourseModel.Course.ROOM != null)
                {
                    EmailBody = EmailBody.Replace("{Room}", CourseModel.Course.ROOM.ToString());
                }
                //EmailBody = EmailBody.Replace("{EnrollStatus}", "");

                //EMAIL INSTRUCTORS
                string instructor_names = string.Empty;
                foreach (var instructor_data in CourseModel.Instructors) 
                {
                    instructor_names = instructor_names + (instructor_data.FIRST + " " + instructor_data.LAST) + "<br />";
                }
                EmailBody = EmailBody.Replace("{Instructors}", instructor_names);
                EmailBody = EmailBody.Replace("{instructors}", instructor_names);

                //EMAIL WAITLIST PART
                string allDates = string.Empty;
                StringBuilder details = new StringBuilder();
                int dateCounter = 0;
                foreach (var datetime in CourseModel.CourseTimes)
                {
                    if (CourseModel.Course.OnlineCourse.HasValue && CourseModel.Course.OnlineCourse.Value != 0)
                    {
                        if (dateCounter > 0)
                        {
                            allDates = allDates + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.FINISHTIME.Value.ToShortTimeString() + "<br />";
                        }
                        else 
                        {
                            allDates = allDates + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.STARTTIME.Value.ToShortTimeString() + "<br />";
                        }
                    }
                    else 
                    { 
                        allDates = allDates + datetime.COURSEDATE.Value.ToString(PubDateFormat) + " " + datetime.STARTTIME.Value.ToShortTimeString() + " - " + datetime.FINISHTIME.Value.ToShortTimeString() + "<br />";
                    }
                    dateCounter++;
                }

                details.Append("<br />" + newLine + " Student : " + Student.FIRST + " " + Student.LAST + "<br />" + newLine);
                details.Append("Course : " + CourseModel.Course.COURSENUM + " " + CourseModel.Course.COURSENAME + "<br />" + newLine);
                details.Append("(Date(s) : " + allDates + ") <br />" + newLine + newLine);
                details.Append("Location : " + CourseModel.Course.LOCATION + "<br />" + newLine);
                details.Append("Room : " + CourseModel.Course.ROOM + "<br />" + newLine);
                details.Append("Course Extra Text : " + CourseModel.Course.CourseConfirmationEmailExtraText + "<br />" + newLine);

                EmailBody = EmailBody.Replace("{Details}", details.ToString());

                var materialsText = GenerateMaterialsInfo(Roster);
                EmailBody = EmailBody.Replace("{Materials}", materialsText);
                var creditarea = GenerateCreditInfo(Roster);
                EmailBody = EmailBody.Replace("{CreditHoursArea}", creditarea);
                EmailBody = EmailBody.Replace("{CourseStartDate}", "");
                EmailBody = EmailBody.Replace("{ParentName}", "");
                EmailBody = EmailBody.Replace("{Start Tokens}", "");
                EmailBody = EmailBody.Replace("{End Tokens}", "");

                
            }
            catch
            {
                EmailBody = EmailBody.Replace("{Materials}", "");
                EmailBody = EmailBody.Replace("{CreditHoursArea}", "");
                EmailBody = EmailBody.Replace("{CourseStartDate}", "");
                EmailBody = EmailBody.Replace("{ParentName}", "");
                EmailBody = EmailBody.Replace("{Start Tokens}", "");
                EmailBody = EmailBody.Replace("{End Tokens}", "");
            }
            EmailBody = EmailBody.Replace("{Start Tokens}", "");
            EmailBody = EmailBody.Replace("{End Tokens}", "");
            return EmailBody;
        }

        private string GetEventSessionDetails(int sessionId,bool getAllDetails)
        {
            string PubDateFormat = Settings.Instance.GetPubDateFormat();
            using (var db = new SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.AutoDetectChangesEnabled = false;
                var sessiondetails = (from sessiondetail in db.Courses.AsNoTracking() where sessiondetail.COURSEID == sessionId select sessiondetail).FirstOrDefault();
                if (sessiondetails != null)
                {
                    var eventdetails = (from eventdetail in db.Courses.AsNoTracking() where eventdetail.COURSEID == sessiondetails.eventid select eventdetail).FirstOrDefault();

                    if (eventdetails != null)
                    {
                        if (getAllDetails)
                        {
                            string details = "(<u>Event</u>: " + eventdetails.COURSENUM + " - " + eventdetails.COURSENAME + " at " + eventdetails.LOCATION + " " + eventdetails.STREET + " " + eventdetails.CITY + " " + eventdetails.STATE + " " + eventdetails.ZIP + "<br />";
                            string datedetails = "";
                            var courseDate = (from cd in db.Course_Times.AsNoTracking() where cd.COURSEID == sessiondetails.eventid orderby cd.COURSEDATE select cd).ToList();
                            int startdatepouplated = 0;
                            foreach (var dates in courseDate)
                            {
                                if (startdatepouplated == 0)
                                {
                                    datedetails = dates.COURSEDATE.Value.ToString(PubDateFormat) + " " + dates.STARTTIME.Value.ToShortTimeString();
                                }
                                else
                                {
                                    datedetails = datedetails + " to "  + dates.COURSEDATE.Value.ToString(PubDateFormat) + " " + dates.STARTTIME.Value.ToShortTimeString();
                                }
                                startdatepouplated = 1;
                            }

                            details = details + datedetails +"<br /><u>Session</u>: " + sessiondetails.COURSENUM + " - " + sessiondetails.COURSENAME + ")";

                            return details;

                        }
                    }
                }
                return String.Empty;
            }
        }
        private string GenerateCreditInfo(Course_Roster roster)
        {
            string creditinfos = "";
            try
            {
           
                if (roster.HOURS > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Credit) + ":" + roster.Course.CREDITHOURS.ToString() + Environment.NewLine;
                }
                if (roster.InserviceHours > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.InService) + ":" + roster.Course.InserviceHours.ToString() + Environment.NewLine;
                }
                if (roster.CustomCreditHours > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Custom) + ":" + roster.Course.CustomCreditHours.ToString() + Environment.NewLine;
                }
                if (roster.graduatecredit > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Graduate) + ":" + roster.Course.GraduateCredit.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours1 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional1) + ":" + roster.Course.Optionalcredithours1.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours2 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional2) + ":" + roster.Course.Optionalcredithours2.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours3 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional3) + ":" + roster.Course.Optionalcredithours3.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours4 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional4) + ":" + roster.Course.Optionalcredithours4.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours5 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional5) + ":" + roster.Course.Optionalcredithours5.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours6 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional6) + ":" + roster.Course.Optionalcredithours6.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours7 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional7) + ":" + roster.Course.Optionalcredithours7.ToString() + Environment.NewLine;
                }
                if (roster.Optionalcredithours8 > 0)
                {
                    creditinfos = creditinfos + CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional8) + ":" + roster.Course.Optionalcredithours8.ToString() + Environment.NewLine;
                }
                if (creditinfos == "" && !string.IsNullOrWhiteSpace(roster.CourseHoursType.ToString()))
                {
                    if (roster.CourseHoursType.ToString() == "CH")
                    {
                        creditinfos = CourseCreditHelper.GetCreditLabel(CourseCreditType.Credit);
                    }
                    else if (roster.CourseHoursType.ToString() == "ISH")
                    {
                        creditinfos = CourseCreditHelper.GetCreditLabel(CourseCreditType.InService);
                    }
                    else if (roster.CourseHoursType.ToString() == "CCH")
                    {
                        creditinfos = CourseCreditHelper.GetCreditLabel(CourseCreditType.Custom);
                    }
                    else if (roster.CourseHoursType.ToString() == "BOTH")
                    {

                    }
                }
            }
            catch { }
            return creditinfos;
        }
        private string GenerateMaterialsInfo(Course_Roster roster)
        {
            var materials = roster.Materials;
            if (materials.Count == 0)
            {
                return string.Empty;
            }

            var result = new System.Text.StringBuilder();
            foreach (var material in materials)
            {
                if (material.hidematerialprice != 0) 
                {
                    var line = string.Format("<strong>{0}</strong> (" + TerminologyHelper.Instance.GetTermCapital(TermsEnum.Material) + ")<br/>", material.product_name);
                    result.Append(line);
                }
                else 
                {
                    var line = string.Format("<strong>{0}</strong> (" + TerminologyHelper.Instance.GetTermCapital(TermsEnum.Material) + ") - {1:c}<br/>", material.product_name, material.EffectivePrice);
                    result.Append(line);
                }
                
            }
            return result.ToString();
        }

        public static void SendEmail(EmailAuditTrail EmailEntity)
        {
            Gsmu.Api.Data.School.Supervisor.SupervisorHelper.SendingEmailStatus = "Sent"; //Initially set the status to sent, and replace the value if error encountered later on.
            int value = Settings.Instance.GetMasterInfo().MailHandler;
            string emailfrom = Settings.Instance.GetMasterInfo().PublicEmailAddress;
            // SendEmailUseDotNetBuiltIn(EmailEntity, emailfrom, Settings.Instance.GetMasterInfo().Mailserver);
            EmailEntity.EmailBody = EmailEntity.EmailBody.Replace("&nbsp;", "").Replace("&nbsp", "");
            MailClient.SendEmail(emailfrom, EmailEntity.EmailTo, EmailEntity.EmailSubject, EmailEntity.EmailBody, true, null, EmailEntity.AttachmentNameMemo, EmailEntity.EmailBCC, EmailEntity);

           
        }

        public static void SendEmail(EmailAuditTrail EmailEntity, int StudentId, string courseids)
        {
            EmailEntity.EmailBody = EmailEntity.EmailBody + emailBodyTrail(StudentId, courseids);
            SendEmail(EmailEntity); 
        }

        public void SendEmail(string to, string subject, string body, string bcc) 
        {
            EmailAuditTrail emailAuditrail = new EmailAuditTrail();
            emailAuditrail.EmailTo = to;
            emailAuditrail.EmailSubject = subject;
            emailAuditrail.EmailBody = body;
            emailAuditrail.AuditDate = DateTime.Now;
            emailAuditrail.AuditProcess = "Public Cancellation/Populate Waiting/Credit Hours";
            emailAuditrail.EmailBCC = bcc;
            EmailFunction.SendEmail(emailAuditrail);

        }

        public static string emailBodyTrail(int studentid, string courseids)
        {
            return " </p></p><font color='white'>[studentid:" + studentid.ToString() + ",courseids:" + courseids + "]</font>";
        }


        private static void UseJmail(EmailAuditTrail EmailEntity, string emailFrom, string emailServer)
        {
            try
            {

                Message message = new Message();
                if (EmailEntity.AttachmentNameMemo != null)
                {
                    foreach (string file in EmailEntity.AttachmentNameMemo.Split('|'))
                    {
                        if ((file != "") & (file != null))
                        {
                            message.Attachments.Add(file);
                        }
                    }
                }

                message.From = emailFrom;
                if (EmailEntity.EmailTo.Contains(";"))
                {
                    foreach (var receiver in EmailEntity.EmailTo.Split(';'))
                    {
                        message.To.Add(receiver);
                    }
                }
                else
                {
                    message.To.Add(EmailEntity.EmailTo);
                }
                if (EmailEntity.EmailCC != null)
                {
                    foreach (string ccemails in EmailEntity.EmailCC.Split(','))
                    {
                        message.Cc.Add(ccemails);
                    }
                }


                message.Subject = EmailEntity.EmailSubject;
                message.BodyHtml = EmailEntity.EmailBody;
                try
                {
                    System.Net.IPHostEntry ip = System.Net.Dns.GetHostEntry(System.Net.IPAddress.Parse(emailServer));
                    string hostname = ip.HostName;
                    Smtp.Send(message, hostname);
                }
                catch (Exception)
                {
                    Smtp.Send(message, emailServer);

                }
            }
            catch (Exception e)
            {
                EmailEntity.ErrorInfo = e.Message;
                EmailEntity.Pending = 1;
            }
            finally
            {
                EmailEntity.EmailFrom = emailFrom;
                using (var db = new SchoolEntities())
                {
                    db.EmailAuditTrails.Add(EmailEntity);
                    db.SaveChanges();

                }
            }
        }

        private static void SendEmailUseDotNetBuiltIn(EmailAuditTrail EmailEntity, string emailFrom, string emailServer)
        {
            int port =0;
            SmtpClient clientvar = new SmtpClient(emailServer);
            if (emailServer.Contains(":"))
            {
               port =  int.Parse(emailServer.Split(':')[1]);
               emailServer =emailServer.Split(':')[0];
               clientvar = new SmtpClient(emailServer,port);
            }
            try
            {
                using (SmtpClient client = clientvar)
                {
                    string raw_cred = string.Empty;
                    string email_cred = string.Empty;
                    string pass_cred = string.Empty;

                    if (!string.IsNullOrEmpty(Settings.Instance.GetMasterInfo().SMTPEmailAccount) && !string.IsNullOrEmpty(MailSettings.SmtpAccountInfo.Username))
                    {
                        client.Credentials = new System.Net.NetworkCredential(MailSettings.SmtpAccountInfo.Username, MailSettings.SmtpAccountInfo.Password);
                    }
                    else
                    {
                        client.UseDefaultCredentials = true;
                    }

                    using (MailMessage message = new MailMessage())
                    {
                        MailAddress from = new MailAddress(emailFrom, emailFrom);
                        message.From = from;
                        if (EmailEntity.EmailTo.Contains(";"))
                        {
                            foreach (var receiver in EmailEntity.EmailTo.Split(';'))
                            {
                                message.To.Add(receiver);
                            }
                        }
                        else
                        {
                            message.To.Add(EmailEntity.EmailTo);
                        }
                        message.Subject = EmailEntity.EmailSubject;
                        if (EmailEntity.AttachmentNameMemo != null)
                        {
                            foreach (string file in EmailEntity.AttachmentNameMemo.Split('|'))
                            {
                                if ((file != "") & (file != null))
                                {
                                    System.Net.Mail.Attachment data = new System.Net.Mail.Attachment(file, MediaTypeNames.Application.Octet);
                                    ContentDisposition disposition = data.ContentDisposition;
                                    disposition.CreationDate = System.IO.File.GetCreationTime(file);
                                    disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
                                    disposition.ReadDate = System.IO.File.GetLastAccessTime(file);
                                    message.Attachments.Add(data);
                                }
                            }
                        }
                        message.Body = EmailEntity.EmailBody;
                        if ((EmailEntity.EmailBCC != null) && (EmailEntity.EmailBCC != "") && EmailEntity.EmailBCC.Contains(';'))
                        {
                            string[] BccArray = EmailEntity.EmailBCC.Split(';');
                            foreach (string email in BccArray.Distinct().ToArray())
                            {
                                if ((email != "") && (email != null))
                                {
                                    message.Bcc.Add(email);
                                }
                            }
                        }

                        else
                        {
                            if ((EmailEntity.EmailBCC != null) && (EmailEntity.EmailBCC != ""))
                            {
                                message.Bcc.Add(EmailEntity.EmailBCC.Replace(';', ' '));
                            }
                        }
                        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(EmailEntity.EmailBody, new ContentType("text/html")));
                        client.Send(message);
                        Gsmu.Api.Data.School.Supervisor.SupervisorHelper.SendingEmailStatus = "Sent";
                        EmailEntity.Pending = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                EmailEntity.ErrorInfo = ex.Message;
                EmailEntity.Pending = 1;
                Gsmu.Api.Data.School.Supervisor.SupervisorHelper.SendingEmailStatus = EmailEntity.Pending.ToString();
            }
            finally
            {
                
                EmailEntity.EmailFrom = emailFrom;
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
        public List<string> MakeICalendarAttachment(string FromAddress, string Routine, int CourseId, int Method)
        {
            List<string> filenames = new List<string>();
            string timezone_string = string.Empty;
            string icsvalidTZstring = string.Empty;
            string timezonesAbv = string.Empty;
            string timezonesT = string.Empty;
            string TZDaylight = string.Empty;
            string temp_file_name = string.Empty;
            string randomuniquetext = string.Empty;
            string course_name = string.Empty;
            string ics_location = string.Empty;
            string ics_room = string.Empty;
            string initial_dir = string.Empty;
            string initial_dirVbs = string.Empty;
            if (Gsmu.Api.Data.WebConfiguration.GenerateUniqueICS)
            {
                randomuniquetext = DateTime.Now.ToString("yyyy-MM-dd T HH-mm-ss");
                try
                {
                    string[] files = Directory.GetFiles(HttpContext.Current.Server.MapPath("/") + "Temp\\");

                    foreach (string file in files)
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.LastAccessTime < DateTime.Now.AddMonths(-2))
                            fi.Delete();
                    }
                }
                catch(Exception e) { }
            }
            using (var db = new SchoolEntities())
            {
                var course = (from c in db.Courses
                              join ct in db.Course_Times on c.COURSEID equals ct.COURSEID
                              where c.COURSEID == CourseId
                              orderby ct.COURSEDATE, ct.STARTTIME, ct.FINISHTIME
                              select new
                              {
                                  CourseName = c.COURSENAME,
                                  CourseNum = c.COURSENUM,
                                  CourseDate = ct.COURSEDATE,
                                  StartTime = ct.STARTTIME,
                                  Location = c.LOCATION,
                                  LocationURL = c.LOCATIONURL,
                                  LocationAdditionalInfo = c.LocationAdditionalInfo,
                                  FinishTime = ct.FINISHTIME,
                                  ContactName = c.ContactName,
                                  ContactPhone = c.ContactPhone,
                                  Street = c.STREET,
                                  City = c.CITY,
                                  State = c.STATE,
                                  ZIP = c.ZIP,
                                  Room = c.ROOM,
                                  CourseExtraText = c.CourseConfirmationEmailExtraText,
                                  Description = c.DESCRIPTION

                              }).FirstOrDefault();


                var course_datetime = (from c_t in db.Course_Times
                                       where c_t.COURSEID == CourseId
                                       select new
                                       {
                                           Cdate = c_t.COURSEDATE,
                                           Cstartime = c_t.STARTTIME,
                                           Cendtime = c_t.FINISHTIME
                                       }
                                      ).ToList();

                string ICSExtraDescription = Settings.Instance.GetMasterInfo3().ICSExtraDescription;
                ICSExtraDescription = ICSExtraDescription + (string.IsNullOrWhiteSpace(ICSExtraDescription) ? "" : "\n");

                ICSExtraDescription = ICSExtraDescription.Replace("{Location}", course.Location ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{LocationURL}", course.LocationURL ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{LocationAdditionalInfo}", course.LocationAdditionalInfo ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{ContactName}", course.ContactName ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{ContactPhone}", course.ContactPhone ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{Street}", course.Street ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{City}", course.City ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{State}", course.State ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{Zip}", course.ZIP ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{Room}", course.Room ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{CourseExtraText}", course.CourseExtraText ?? "");
                ICSExtraDescription = ICSExtraDescription.Replace("{CourseDescription}", course.Description.Replace("<br>", "\n") ?? "");

                ICSExtraDescription = ICSExtraDescription.Replace("<br>", " \n");
                ICSExtraDescription = ICSExtraDescription.Replace("<br/>", " \n");
                ICSExtraDescription = ICSExtraDescription.Replace("<p>", " \n");
                ICSExtraDescription = ICSExtraDescription.Replace("</p>", " \n");

                //new line using [enter] in systemconfig_mailserver
                ICSExtraDescription = ICSExtraDescription.Replace("&#10;", "");
                ICSExtraDescription = ICSExtraDescription.Replace("&#13;", "");

                ICSExtraDescription = ICSExtraDescription.Replace("\n", " \\n");
                ICSExtraDescription = ICSExtraDescription.Replace("\r", String.Empty);
                ICSExtraDescription = ICSExtraDescription.Replace("\t", String.Empty);
                int time_zone = Settings.Instance.GetMasterInfo3().system_timezone_hour.Value;
                switch (time_zone)
                {
                    case 1:
                        timezone_string = "America/Denver"; //MST or US/Mountain
                        icsvalidTZstring = "Mountain Time";
                        timezonesAbv = "MST";
                        timezonesT = "-0700";
                        TZDaylight = "-0600";
                        break;
                    case 2:
                        timezone_string = "America/Chicago"; //CST or US/Central
                        icsvalidTZstring = "Central Time";
                        timezonesAbv = "CT";
                        timezonesT = "-0600";
                        TZDaylight = "-0500";
                        break;
                    case 3:
                        timezone_string = "America/New_York"; //EST or US/Eastern
                        icsvalidTZstring = "Eastern Time";
                        timezonesAbv = "ET";
                        timezonesT = "-0500";
                        TZDaylight = "-0400";
                        break;
                    //for approval not yet implemented 
                    case 4:
                        timezone_string = "America/Arizona"; //Using Daylight Saving Time (DST) - MST(Arizona) or US/Arizona
                        icsvalidTZstring = "Mountain Time";
                        timezonesAbv = "MT";
                        timezonesT = "-0700";
                        TZDaylight = "-0600";
                        break;
                    case 5:
                        timezone_string = "America/Arizona"; //Daylight Saving Time (DST) Not Observed - US/Arizona
                        icsvalidTZstring = "Mountain Time";
                        timezonesAbv = "MT";
                        timezonesT = "-0700";
                        TZDaylight = "-0700";
                        break;
                    case 6:
                        timezone_string = "Dubai - United Arab Emirates"; //Dubai UAE time zone
                        icsvalidTZstring = "Gulf Standard Time";
                        timezonesAbv = "UAE";
                        timezonesT = "+0400";
                        TZDaylight = "+0400";
                        break;
                    default:
                        timezone_string = "America/Los_Angeles"; //PST or US/Pacific
                        icsvalidTZstring = "Pacific Time";
                        timezonesAbv = "PT";
                        timezonesT = "-0800";
                        TZDaylight = "-0700";
                        break;
                }

                if (course != null && course.CourseNum != "~ZZZZZZ~")
                {
                    initial_dir = HttpContext.Current.Server.MapPath("/") + "Temp\\";
                    initial_dirVbs = HttpContext.Current.Server.MapPath("/") + "\\admin\\";
                    if (!Directory.Exists(initial_dir))
                    {
                        Directory.CreateDirectory(initial_dir);
                    }
                    if (Routine.ToLower().Contains(".vbs") == true)
                    {
                        //temp_file_name = Environment.CurrentDirectory.ToString().ToLower() + @"\Temp\" + CourseId + "-" + FilterUploadName(course.CourseName + "-");
                        temp_file_name = initial_dirVbs + @"\Temp\" + CourseId + "-" + FilterUploadName(course.CourseName + "-");
                    }
                    else
                    {
                        temp_file_name = initial_dir + @"\" + CourseId + "-" + FilterUploadName(course.CourseName + "-");
                    }


                    string path = temp_file_name + randomuniquetext+".ICS";

                    for (int icl = 1; icl <= 2; icl = icl + 1)
                    {

                        if (icl == 2)
                        {
                            path = temp_file_name + randomuniquetext+ ".ical";
                        }
                        // if (!File.Exists(path))
                        // {
                        // Create a file to write to. 
                        _readWriteLock.EnterWriteLock();
                        try
                        {
                            using (StreamWriter sw = File.CreateText(path))
                            {
                                sw.AutoFlush = true;
                                sw.WriteLine("BEGIN:VCALENDAR");
                                sw.WriteLine("PRODID:-//GoSignMeUp, Inc//Gosignmeup.com//EN");
                                sw.WriteLine("VERSION:2.0");
                                if (Method == 2)
                                {
                                    sw.WriteLine("METHOD:CANCEL");
                                }
                                else
                                {
                                    sw.WriteLine("METHOD:REQUEST");
                                }

                                if (course.Location != null && course.Location != string.Empty)
                                {
                                    ics_location = course.Location;
                                }
                                if (course.Room != null && course.Room != string.Empty)
                                {
                                    ics_room = course.Room;
                                }

                                String DTSTARTstr = course.CourseDate.Value.ToShortDateString() + " " + course.StartTime.Value.ToShortTimeString();
                                DateTime DTSTART = DateTime.Parse(DTSTARTstr);
                                String DTENDstr = course.CourseDate.Value.ToShortDateString() + " " + course.FinishTime.Value.ToShortTimeString();
                                DateTime DTEND = DateTime.Parse(DTENDstr);

                                var tz = TimeZone.CurrentTimeZone;
                                bool is_dst = tz.IsDaylightSavingTime(DTSTART);
                                if (is_dst == true && time_zone != 3 && time_zone != 2 && time_zone != 1)
                                {
                                    TZDaylight = "-0700";
                                }

                                sw.WriteLine("BEGIN:VTIMEZONE");
                                sw.WriteLine("TZID:" + icsvalidTZstring);
                                sw.WriteLine("BEGIN:STANDARD");
                                sw.WriteLine("DTSTART:19701101T020000");
                                sw.WriteLine("RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU");
                                sw.WriteLine("TZOFFSETFROM:" + TZDaylight);
                                sw.WriteLine("TZOFFSETTO:" + timezonesT);
                                sw.WriteLine("TZNAME:" + icsvalidTZstring);
                                sw.WriteLine("END:STANDARD");
                                sw.WriteLine("BEGIN:DAYLIGHT");
                                sw.WriteLine("DTSTART:19700308T020000");
                                sw.WriteLine("RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU");
                                sw.WriteLine("TZOFFSETFROM:" + timezonesT);
                                sw.WriteLine("TZOFFSETTO:" + TZDaylight);
                                sw.WriteLine("TZNAME:Daylight Savings Time");
                                sw.WriteLine("END:DAYLIGHT");
                                sw.WriteLine("END:VTIMEZONE");



                                if (course_datetime == null)
                                {
                                    sw.WriteLine("BEGIN:VEVENT");
                                    sw.WriteLine("ATTENDEE;CN=\"\";ROLE=REQ-PARTICIPANT;RSVP=TRUE:MAILTO:" + FromAddress);
                                    sw.WriteLine("ORGANIZER;CN=\"MAILTO:" + FromAddress + "\":MAILTO:" + FromAddress);

                                    sw.WriteLine("DTSTART;TZID=\"" + icsvalidTZstring + "\":" + ProvideFloatingDateTime(DTSTART, DTSTART));
                                    sw.WriteLine("DTEND;TZID=\"" + icsvalidTZstring + "\":" + ProvideFloatingDateTime(DTEND, DTEND));

                                    if (ics_location.Trim() == "" && ics_room.Trim() == "")
                                    {
                                        sw.WriteLine("LOCATION:See Confirmation");
                                    }
                                    else if (ics_location.Trim() != string.Empty && ics_room.Trim() != string.Empty)
                                    {
                                        sw.WriteLine("LOCATION: " + ics_location + " - Room :" + ics_room);
                                    }
                                    else if (ics_location.Trim() != string.Empty && ics_room.Trim() == string.Empty)
                                    {
                                        sw.WriteLine("LOCATION: " + ics_location);
                                    }
                                    else if (ics_location.Trim() == string.Empty && ics_room.Trim() != string.Empty)
                                    {
                                        sw.WriteLine("LOCATION: " + ics_room);
                                    }

                                    sw.WriteLine("TRANSP:OPAQUE");
                                    sw.WriteLine("SEQUENCE:0");
                                    sw.WriteLine("UID:" + CourseId + "-Gosignmeup_Generated");
                                    sw.WriteLine("DTSTAMP:" + ProvideFloatingDateTime(DateTime.Now.Date.ToUniversalTime(), DateTime.Now) + "Z");

                                    sw.WriteLine("DESCRIPTION: " + ICSExtraDescription);
                                    sw.WriteLine("SUMMARY:" + course.CourseName.ToString() + " (" + course.CourseDate.Value.ToString("MM/dd/yyyy") + " " + course.StartTime.Value.TimeOfDay + " - " + course.FinishTime.Value.TimeOfDay + " " + timezonesAbv + ")");
                                    sw.WriteLine("PRIORITY:0");
                                    sw.WriteLine("CLASS:PUBLIC");

                                    sw.WriteLine("END:VEVENT");

                                }
                                else
                                {
                                        foreach(var caltime in course_datetime)
                                    {
                                        DTSTARTstr = caltime.Cdate.Value.ToShortDateString() + " " + caltime.Cstartime.Value.ToShortTimeString();
                                        DTSTART = DateTime.Parse(DTSTARTstr);
                                        DTENDstr = caltime.Cdate.Value.ToShortDateString() + " " + caltime.Cendtime.Value.ToShortTimeString();
                                        DTEND = DateTime.Parse(DTENDstr);
                                        sw.WriteLine("BEGIN:VEVENT");
                                        sw.WriteLine("ATTENDEE;CN=\"\";ROLE=REQ-PARTICIPANT;RSVP=TRUE:MAILTO:" + FromAddress);
                                        sw.WriteLine("ORGANIZER;CN=\"MAILTO:" + FromAddress + "\":MAILTO:" + FromAddress);

                                        sw.WriteLine("DTSTART;TZID=\"" + icsvalidTZstring + "\":" + ProvideFloatingDateTime(DTSTART, DTSTART));
                                        sw.WriteLine("DTEND;TZID=\"" + icsvalidTZstring + "\":" + ProvideFloatingDateTime(DTEND, DTEND));

                                        if (ics_location.Trim() == "" && ics_room.Trim() == "")
                                        {
                                            sw.WriteLine("LOCATION:See Confirmation");
                                        }
                                        else if (ics_location.Trim() != string.Empty && ics_room.Trim() != string.Empty)
                                        {
                                            sw.WriteLine("LOCATION: " + ics_location + " - Room :" + ics_room);
                                        }
                                        else if (ics_location.Trim() != string.Empty && ics_room.Trim() == string.Empty)
                                        {
                                            sw.WriteLine("LOCATION: " + ics_location);
                                        }
                                        else if (ics_location.Trim() == string.Empty && ics_room.Trim() != string.Empty)
                                        {
                                            sw.WriteLine("LOCATION: " + ics_room);
                                        }

                                        sw.WriteLine("TRANSP:OPAQUE");
                                        sw.WriteLine("SEQUENCE:0");
                                        sw.WriteLine("UID:" + CourseId + "-Gosignmeup_Generated" + ProvideFloatingDateTime(DTSTART, DTSTART));
                                        sw.WriteLine("DTSTAMP:" + ProvideFloatingDateTime(DateTime.Now.Date.ToUniversalTime(), DateTime.Now) + "Z");

                                        sw.WriteLine("DESCRIPTION: " + ICSExtraDescription);
                                        sw.WriteLine("SUMMARY:" + course.CourseName.ToString() + " (" + caltime.Cdate.Value.ToString("MM/dd/yyyy") + " " + caltime.Cstartime.Value.TimeOfDay + " - " + caltime.Cendtime.Value.TimeOfDay + " " + timezonesAbv + ")");
                                        sw.WriteLine("PRIORITY:0");
                                        sw.WriteLine("CLASS:PUBLIC");

                                        sw.WriteLine("END:VEVENT");
                                    }

                                }




                                sw.WriteLine("END:VCALENDAR");
                                sw.Flush();
                                sw.Dispose();
                                sw.Close();

                             //   filenames.Add(path);
                            }
                        }
                        finally
                        {
                            _readWriteLock.ExitWriteLock();
                        }
                      //  }
                       // else
                      //  {
                            filenames.Add(path);
                       // }
                    }
                }


            }
            return filenames;
        }
        public string FilterUploadName(string file_name)
        {
            file_name = file_name.ToLower();
            string pattern = "[\\~#%&*{}/<>?|\"-]";
            Regex regEx = new Regex(pattern);
            string sanitized_file_name = regEx.Replace(file_name, "");
            sanitized_file_name = sanitized_file_name.Replace(" ", "_").Replace(":", "_");
            return sanitized_file_name;
        }
        public string ProvideFloatingDateTime(DateTime DateIn, DateTime TimeIn)
        {
            string FloatingDate = DateIn.ToString("yyyyMMdd") + "T" + TimeIn.ToString("HHmmss") + "";
            return FloatingDate;
        }
        public List<string> GetConfirmationEmailAttachments(int courseid) 
        {
            List<string> files = new List<string>();
            foreach (string file in Gsmu.Api.Data.School.Course.CourseFilesHelper.GetCourseFileList(courseid))
            {
                var fi = new FileInfo(file);
                if (fi.Name.StartsWith(courseid.ToString()))
                {
                    files.Add(file);
                }
            }
            return files;
        }
        //can be used on the above sending email routines
        //This is the function for Cancelation and Wait List To Confirmation//
        public EmailContentModel GetEmailPreview(int type, bool replaceToken = false, string OrderNumber = "", int RosterId = 0) 
        {
            string body = string.Empty;
            string subject = string.Empty;
            if (type == 0)
            {
                body = Settings.Instance.GetMasterInfo3().EmailConfirmationContent;
                subject = Settings.Instance.GetMasterInfo3().EmailConfirmationSubject;
            }
            else if (type == 1)
            {
                body = Settings.Instance.GetMasterInfo().EmailCancelBody;
                subject = Settings.Instance.GetMasterInfo().EmailCancelSubject;
            }
            else if (type == 2)
            {
                body = Settings.Instance.GetMasterInfo().EnrolledFromWaitingBody;
                subject = Settings.Instance.GetMasterInfo().EnrolledFromWaitingSubject;
            }
            using(var db = new SchoolEntities())
            {
                var roster = db.Course_Rosters.Where(r => RosterId > 0 ? r.RosterID == RosterId : r.OrderNumber == OrderNumber).FirstOrDefault();
                subject = replaceToken ? ReplaceToken("0.0", subject, roster, "") : subject;
                body = replaceToken ? ReplaceToken("0.0", body, roster, "") : body;
            }
            return new EmailContentModel()
            {
                subject = subject,
                body = body
            };
        }

        public void SendBlackboardEmailConfirmation(Course course, Student user)
        {
            var Subject = Settings.Instance.GetMasterInfo3().BBStudentRegEmailSubject;
            var TextBody = Settings.Instance.GetMasterInfo3().BBStudentRegEmailText;
            Subject=Subject.Replace("{course}", course.COURSENAME);
            TextBody= TextBody.Replace("{course}", course.COURSENAME).Replace(" {username}",user.USERNAME).Replace(" {resetLink}", "");
            EmailAuditTrail EmailEntity = new EmailAuditTrail();
            EmailEntity.EmailTo = user.EMAIL;
            EmailEntity.EmailSubject = Subject;
            EmailEntity.EmailBody = TextBody;
            EmailEntity.AuditDate = DateTime.Now;
            EmailEntity.AuditProcess = "Public BB_Reg";

            EmailFunction.SendEmail(EmailEntity);
        }

        #region EMAIL RETURN MODEL
        public class EmailReturnModel
        {
            public string status { get; set; }
            public bool sentStatus { get; set; }
            public EmailAuditTrail emailAuditTrailInfo { get; set; }
            public string errorMessage { get; set; }
        }
        #endregion
        #region EMAIL CONTENT MODEL
        public class EmailContentModel 
        {
            public string body { get; set; }
            public string subject { get; set; }
        }
        #endregion

        #region EmailcomtentListModel
        public class EmailContentList
        {
            public int? CourseId { set; get; }
            public int? RosterId { get; set; }
            public string Body { get; set; }
            public string subject { get; set; }
            public string EmailBody { get; set; }
        }

        public class EmailContentListModel
        {
            public List<EmailContentList> CategorymodelList = new List<EmailContentList>();
        }
        #endregion

        
    }
}

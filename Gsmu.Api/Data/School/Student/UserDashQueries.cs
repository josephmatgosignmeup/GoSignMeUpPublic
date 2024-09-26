using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Authorization;
using entities = Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.School.Category;
using Gsmu.Api.Networking.Mail;
using System.Web;
using Gsmu.Api.Integration.Blackboard;
using blackboard = Gsmu.Api.Integration.Blackboard;
using canvas = Gsmu.Api.Integration.Canvas;
using Gsmu.Api.Data.ViewModels.Grid;
using BlackBoardAPI;
using static BlackBoardAPI.BlackBoardAPIModel;
using System.Web.Script.Serialization;

namespace Gsmu.Api.Data.School.Student
{
    public class UserDashQueries
    {


        public static String SendCheckRecover(string username, string firstname, string email, string recovrtyp, String logintype)
        {
            if (Settings.Instance.GetMasterInfo2().CommonPublicLogin == 1 && Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1)
            {
                logintype = "S";
            }
            String subject = "";
            String emailBody = "";
            String curToken = "";
            Gsmu.Api.Data.School.Entities.Student Student = new Gsmu.Api.Data.School.Entities.Student();
            Gsmu.Api.Data.School.Entities.Supervisor Supervisor = new Gsmu.Api.Data.School.Entities.Supervisor();
            Gsmu.Api.Data.School.Entities.Instructor Instructor = new Gsmu.Api.Data.School.Entities.Instructor();
            Gsmu.Api.Data.School.Entities.EmailAuditTrail EmailEntity = new entities.EmailAuditTrail();
            String ParentLevelTitle = Settings.Instance.GetMasterInfo2().ParentLevelTitle;

            try
            {
                using (var db = new SchoolEntities())
                {
                    if (recovrtyp == "pass")
                    {
                        if (logintype == "S")
                        {
                            Student = (from s in db.Students where s.USERNAME == username && s.EMAIL == email select s).First();
                        }
                        else if(logintype == "Sup")
                        {
                            Supervisor = (from pt in db.Supervisors where pt.UserName == username && pt.EMAIL == email select pt).First();
                        }
                        else if (logintype == "I")
                        {
                            Instructor = (from pt in db.Instructors where pt.USERNAME == username && pt.EMAIL == email select pt).First();
                        }
                    }
                    else
                    {
                        if (logintype == "S")
                        {
                            Student = (from s in db.Students where s.FIRST == firstname && s.EMAIL == email select s).First();
                        }
                        else if (logintype == "Sup")
                        {
                            Supervisor = (from pt in db.Supervisors where pt.FIRST == firstname && pt.EMAIL == email select pt).First();
                        }
                        else if (logintype == "I")
                        {
                            Instructor = (from pt in db.Instructors where pt.FIRST == firstname && pt.EMAIL == email select pt).First();
                        }
                    }
                }
            }
            catch
            {
                if (recovrtyp == "pass"){
                    return "{success:false, msg:'No account found with the combination of that username and email address.'}";
                }else{
                    return "{success:false, msg:'No account found with the combination of that first name and email address.'}";
                }
            }

            curToken = "pw" + GenRandomKey().ToLower();
            String curURL = "<a href='" + HttpContext.Current.Request.Url.AbsoluteUri.ToLower();
            if (curURL.Contains("?") == false) { curURL = curURL + "?"; }
            curURL = curURL + "&stype=" + logintype + "&username=" + username + "&resettoken=" + curToken + "&misc=" + DateTime.Now + "'>Reset your password.</a>";
            curURL = curURL.Replace("SubmitRecover", "AccountResetPass");
            curURL = curURL.Replace("submitrecover", "AccountResetPass");
            curURL = curURL.Replace("cart/addnewstudentforenrollment", "User/AccountResetPass");

            if (recovrtyp == "pass")
            {
                subject = Settings.Instance.GetMasterInfo().ForgotPasswordEmailSubject;
                emailBody = Settings.Instance.GetMasterInfo().ForgotPasswordEmailBody;
            }
            else
            {
                emailBody = Settings.Instance.GetMasterInfo().ForgotUsernameEmailBody;
                subject = Settings.Instance.GetMasterInfo().ForgotUsernameEmailSubject;
            }


            if (!String.IsNullOrEmpty(emailBody))
            {
                if (logintype == "S")
                {
                    emailBody = emailBody.Replace("{first}", Student.FIRST);
                    emailBody = emailBody.Replace("{last}", Student.LAST);
                    emailBody = emailBody.Replace("{username}", Student.USERNAME);
                }
                else if (logintype == "Sup")
                {
                    emailBody = emailBody.Replace("{first}", Supervisor.FIRST);
                    emailBody = emailBody.Replace("{last}", Supervisor.LAST);
                    emailBody = emailBody.Replace("{username}", Supervisor.UserName);
                }
                else if (logintype == "I")
                {
                    emailBody = emailBody.Replace("{first}", Instructor.FIRST);
                    emailBody = emailBody.Replace("{last}", Instructor.LAST);
                    emailBody = emailBody.Replace("{username}", Instructor.USERNAME);
                }
                emailBody = emailBody.Replace("{resetLink}", curURL);
            }
            else
            {
               

                if (recovrtyp == "pass")
                {
                    if (logintype == "S")
                    {
                        emailBody = "Dear " + Student.FIRST + " " + Student.LAST + "<br>";
                        emailBody = emailBody + "<a href='" + curURL + "'>";
                        emailBody = emailBody + "Click this link to reset your student password</a><br>";
                        emailBody = emailBody + "<b>* Notice:</b> This link only works within 24 hours from the time you made the request.";
                    }
                    else if (logintype == "Sup")
                    {
                        emailBody = "Dear " + Supervisor.FIRST + " " + Supervisor.LAST + "<br>";
                        emailBody = emailBody + "<a href='" + curURL + "'>";
                        emailBody = emailBody + "Click this link to reset your " + ParentLevelTitle + " password</a>";
                    }
                    else if (logintype == "I")
                    {
                        emailBody = "Dear " + Instructor.FIRST + " " + Instructor.LAST + "<br>";
                        emailBody = emailBody + "<a href='" + curURL + "'>";
                        emailBody = emailBody + "Click this link to reset your " + ParentLevelTitle + " password</a>";
                    }
                }
                else
                {
                    if (logintype == "S")
                    {
                        emailBody = emailBody + "Here is your username for the system: " + username;
                    }
                    else if (logintype == "Sup")
                    {
                        emailBody = emailBody + "Here is your " + ParentLevelTitle + " username for the system: " + username;
                    }
                    else if (logintype == "Sup")
                    {
                        emailBody = emailBody + "Here is your " +"Instructor " + " username for the system: " + username;
                    }
                }
            }


            if (String.IsNullOrEmpty(subject))
            {
                if (recovrtyp == "pass")
                {
                    if (logintype == "S")
                    {
                        subject = "Your Gosignmeup Student Login";
                    }
                    else if (logintype == "Sup")
                    {
                        subject = "Your Gosignmeup " + ParentLevelTitle + " Login";
                    }
                    else if (logintype == "I")
                    {
                        subject = "Your Gosignmeup " + "Instructor" + " Login";
                    }
                }
                else
                {
                    if (logintype == "S")
                    {
                        subject = "Your Gosignmeup Student Username";
                    }
                    else if (logintype == "Sup")
                    {
                        subject = "Your Gosignmeup " + ParentLevelTitle + " Username";
                    }
                }
            }


            if (recovrtyp == "pass")
            {
                if (logintype == "S")
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Student student = Context.Students.First(st => st.USERNAME == username && st.EMAIL == email);
                    student.resetPasswordHash = curToken;
                    student.resetPasswordDate = DateTime.Now;
                    Context.SaveChanges();
                }
                else if (logintype == "Sup")
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Supervisor parent = Context.Supervisors.First(pt => pt.UserName == username && pt.EMAIL == email);
                    parent.resetPasswordHash = curToken;
                    parent.resetPasswordDate = DateTime.Now;
                    Context.SaveChanges();
                }
                else if (logintype == "I")
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Instructor teacher = Context.Instructors.First(pt => pt.USERNAME == username && pt.EMAIL == email);
                    teacher.resetPasswordHash = curToken;
                    teacher.resetPasswordDate = DateTime.Now;
                    Context.SaveChanges();
                }
            }

            EmailFunction EmailFunction = new EmailFunction();
            EmailEntity.EmailBody = emailBody;
            EmailEntity.EmailTo = email;
            if (WebConfiguration.sendRequestUsernamePass2AdditionalEmail =="yes" && logintype == "S" && !string.IsNullOrEmpty(Student.additionalemail))
            {
                EmailEntity.EmailCC = Student.additionalemail.ToString();
            }
            EmailEntity.EmailSubject = subject;
            EmailEntity.AuditProcess = "Public Reset Pass";
            EmailEntity.AuditDate = DateTime.Now;
            EmailFunction.SendEmail(EmailEntity);

            String msg = "";
            if (recovrtyp == "pass")
            {
                msg = "<h2>Your reset password link has been sent to " + email + " <br> Any prior request will be expired.</h2>";
            }
            else
            {
                msg = "<h2>We have sent your username to this email address: " + email + "</h2>";
            }


            return "{success:true, msg:'" + msg + "'}";
        }

        public static String CheckValidResettoken(string username, string resettoken, string logintype)
        {
            if (Settings.Instance.GetMasterInfo2().CommonPublicLogin == 1 && Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1)
            {
                logintype = "S";
            }
            Gsmu.Api.Data.School.Entities.Student Student = new Gsmu.Api.Data.School.Entities.Student();
            Gsmu.Api.Data.School.Entities.Supervisor Supervisor = new Gsmu.Api.Data.School.Entities.Supervisor();
            Gsmu.Api.Data.School.Entities.Instructor Instructor = new Gsmu.Api.Data.School.Entities.Instructor();
            try
            {
                using (var db = new SchoolEntities())
                {
                    if (logintype == "S")
                    {
                        Student = (from s in db.Students where s.USERNAME == username && s.resetPasswordHash == resettoken select s).First();
                    }
                    else if (logintype == "Sup")
                    {
                        Supervisor = (from p in db.Supervisors where p.UserName == username && p.resetPasswordHash == resettoken select p).First();
                    }
                    else if (logintype == "I")
                    {
                        Instructor = (from p in db.Instructors where p.USERNAME == username && p.resetPasswordHash == resettoken select p).First();
                    }
                }
            }
            catch
            {
                return "invalidresettoken";
            }

            DateTime TokenValidDate = DateTime.Now.AddDays(-1);
            if (logintype == "S")
            {
                if (Student.resetPasswordDate > TokenValidDate)
                {
                    return "validtoken";
                }
            }
            else if (logintype == "Sup")
            {
                if (Supervisor.resetPasswordDate > TokenValidDate)
                {
                    return "validtoken";
                }
            }
            else if (logintype == "I")
            {
                if (Instructor.resetPasswordDate > TokenValidDate)
                {
                    return "validtoken";
                }
            }
            return "validtokendate";
        }

        public static String SendResetPass(string username, string resettoken, string firstpassword, string logintype)
        {
            Gsmu.Api.Data.School.Entities.Student Student = new Gsmu.Api.Data.School.Entities.Student();
            bool commonlogin = false;
            if (Settings.Instance.GetMasterInfo2().CommonPublicLogin == 1 && Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1)
            {
                commonlogin = true;
            }
            if ((logintype == "S")||commonlogin)
            {
                try
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Student student = Context.Students.First(st => st.USERNAME == username && st.resetPasswordHash == resettoken);
                    if (string.IsNullOrEmpty(firstpassword))
                    {
                        return "failed";
                    }
                    else
                    {
                        if (commonlogin)
                        {
                            Gsmu.Api.Data.School.Entities.Instructor instructor = Context.Instructors.FirstOrDefault(inst => inst.USERNAME == username);
                            if (instructor != null)
                            {
                                instructor.PASSWORD = firstpassword;
                            }
                            Gsmu.Api.Data.School.Entities.Supervisor supervisor = Context.Supervisors.FirstOrDefault(inst => inst.UserName == username);
                            if (supervisor != null)
                            {
                                supervisor.PASSWORD = firstpassword;
                            }
                        }
                        student.STUDNUM = firstpassword;
                        Context.SaveChanges();

                        if (canvas.Configuration.Instance.EnableOAuth2Authentication == true && canvas.Configuration.Instance.UserSynchronizationInDashboard == true)
                        {
                            //update user password.
                            if (student.canvas_user_id != 0 && student.canvas_user_id != null && canvas.Configuration.Instance.enableGSMUMasterAuthentication == true && !string.IsNullOrEmpty(student.STUDNUM.ToString()))
                            {
                                try
                                {
                                    if (student.STUDNUM.ToString().IndexOf("canvas") == -1)
                                    {
                                        canvas.Entities.User user = new canvas.Entities.User();
                                        user.Id = student.canvas_user_id.Value;
                                        canvas.Response loginresp;
                                        user.cpass = student.STUDNUM.ToString();
                                        loginresp = canvas.Clients.UserClient.UpdateUserPass(user);
                                        return "successupdate";
                                    }
                                }
                                catch
                                {
                                  if(  student.canvas_user_id == 0 || student.canvas_user_id == null)
                                    {
                                        return "successupdate";
                                    }
                                    else {
                                        return "failed";
                                    }
                                }
                            }
                        }
                        var bbConfig = Configuration.Instance;
                        if (bbConfig.BlackboardRealtimeStudentSyncEnabled)
                        {
                            if (Configuration.Instance.BlackboardUseAPI)
                            {
                                BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                                var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                                var user = handelr.GetUserDetails(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, student.USERNAME, "", "", jsonToken);

                                if (user.userName != null)
                                {
                                    BBUser user_update = new BBUser();
                                    user_update.password = firstpassword;
                                    BBRespUserProfile updateduser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, user.userName, "", "", jsonToken, "");
                                    if (updateduser.responseMessage.IndexOf("True") >= 0)
                                    {
                                        return "successupdate";
                                    }
                                    else
                                    {
                                        return "successupdatepartial";
                                    }
                                }
                            }
                            else
                            { 
                                var checkUserExist = blackboard.Connector.UserConnector.SeachUserByFields("", username);
                                if (checkUserExist.IsSuccess)
                                {
                                    var userResult = blackboard.Connector.UserConnector.UpdateBBUserAccount(username, firstpassword, "", "", "", "", 0, 0, 0);
                                    if (userResult.IsSuccess)
                                    {
                                        return "successupdate";
                                    }
                                    else
                                    {
                                        return "successupdatepartial";
                                    }
                                }
                                else
                                {
                                    return "successupdatepartial";
                                }
                            }
                        }
                        else
                        {
                            return "successupdate";
                        }

                        
                    }
                }
                catch
                {
                    return "failed";
                }
            }
            if (logintype == "Sup")
            {
                try
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Supervisor supervisor = Context.Supervisors.First(pt => pt.UserName == username && pt.resetPasswordHash == resettoken);
                    if (string.IsNullOrEmpty(firstpassword))
                    {
                        return "failed";
                    }
                    else
                    {
                        supervisor.PASSWORD = firstpassword;
                        Context.SaveChanges();
                        return "successupdate";
                    }
                }
                catch
                {
                    return "failed";
                }
            }
            else if (logintype == "I")
            {
                try
                {
                    var Context = new SchoolEntities();
                    Gsmu.Api.Data.School.Entities.Instructor instructor = Context.Instructors.First(pt => pt.USERNAME == username && pt.resetPasswordHash == resettoken);
                    if (string.IsNullOrEmpty(firstpassword))
                    {
                        return "failed";
                    }
                    else
                    {
                        instructor.PASSWORD = firstpassword;
                        Context.SaveChanges();
                        return "successupdate";
                    }
                }
                catch
                {
                    return "failed";
                }
            }
            else
            {
                return "failed";
            }
        }


        public static String GenRandomKey()
        {
            // random key using CharSet
            string CharSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            Random m_Rand = new Random();
            int length = m_Rand.Next(7, 7 + 1);
            StringBuilder key = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                key.Append(CharSet[m_Rand.Next(0, CharSet.Length)]);
            }

            // random key using datetime
            string ticks = DateTime.Now.Ticks.ToString();
            ticks = ticks.Substring(ticks.Length - 7);

            string RandomKeyStr = key.ToString() + ticks;

            return RandomKeyStr;
        }


        public static bool ProcessCheckStudentUsernameExist(string username)
        {
            var exist = false;
            var SkipMe = false;
            using (var db = new SchoolEntities())
            {
                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    if (AuthorizationHelper.CurrentStudentUser.LoggedInUsername != username)
                    {
                        if (WebConfiguration.AllowCreateNewAccountOverInactive == "true")
                        {
                            exist = (from op in db.Students where op.USERNAME == username && op.InActive == 0 select op).Count() > 0;
                        }
                        else
                        {
                            exist = (from op in db.Students where op.USERNAME == username select op).Count() > 0;
                        }

                        if (!exist)
                        {
                            exist = (from sup in db.Supervisors where sup.UserName == username select sup).Count() > 0;
                        }
                    }
                    else
                    {
                        SkipMe = true;
                    }
                }

                else
                {
                    if (WebConfiguration.AllowCreateNewAccountOverInactive == "true")
                    {
                        exist = (from op in db.Students where op.USERNAME == username && op.InActive == 0 select op).Count() > 0;
                    }
                    else
                    {
                        exist = (from op in db.Students where op.USERNAME == username select op).Count() > 0;
                    }
                    if (!exist)
                    {
                        exist = (from sup in db.Supervisors where sup.UserName == username select sup).Count() > 0;
                    }
                }
            }

            if (!SkipMe)
            {
                var bbConfig = Configuration.Instance;
                if (!exist && bbConfig.BlackboardSsoEnabled)
                {
                    if (!Configuration.Instance.BlackboardUseAPI)
                    {
                        var userResult = blackboard.Connector.UserConnector.SeachUserByFields("", username);
                        exist = userResult.IsSuccess;
                    }
                }

                if (!exist && canvas.Configuration.Instance.EnableOAuth2Authentication)
                {
                    var lookUpUserInCanvas = canvas.Clients.UserClient.SearchCanvasUserByUserName(username);
                    if (lookUpUserInCanvas == null && Gsmu.Api.Integration.Canvas.Configuration.Instance.autoMapCanvasAccount && canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField != "username")
                    {
                        if (!string.IsNullOrEmpty(username))
                        {
                            var Context = new SchoolEntities();
                            Gsmu.Api.Data.School.Entities.Student student = Context.Students.FirstOrDefault(st => st.USERNAME == username);
                            if (student != null)
                            {
                                string currentCustomIDField = "nothing here";
                                if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField == "studregfield1" && !string.IsNullOrEmpty(student.StudRegField1))
                                {
                                    currentCustomIDField = student.StudRegField1;
                                }
                                else if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField == "studregfield2")
                                {
                                    currentCustomIDField = student.StudRegField2;
                                }
                                else if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField == "studregfield3")
                                {
                                    currentCustomIDField = student.StudRegField3;
                                }
                                else if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField == "studregfield4")
                                {
                                    currentCustomIDField = student.StudRegField4;
                                }
                                else if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField == "studregfield5")
                                {
                                    currentCustomIDField = student.StudRegField5;
                                }
                                lookUpUserInCanvas = Gsmu.Api.Integration.Canvas.Clients.UserClient.SearchCanvasUserBySisUserID(currentCustomIDField);
                            }
                        }
                    }
                    //null mean no matching (per function catch return
                    if (lookUpUserInCanvas != null)
                    {
                        int existingCanvasUserID = int.Parse(lookUpUserInCanvas.User.Id.ToString());
                        if (existingCanvasUserID != 0) { exist = true; }
                    }
                    else
                    {
                        exist = false;
                    }
                }
            }
            return exist;
        }

        public static bool ProcessCheckInstructorUsernameExist(string username)
        {
            using (var db = new SchoolEntities())
            {
                var exist = (from op in db.Instructors where op.USERNAME == username select op).Count() > 0;
                return exist;
            }

        }

        public static GridModel<OtherUserEnrolledModel> GetOtherUserEnrolledCourses(QueryState state)
        {
            List<OtherUserEnrolledModel> OtherUserEnrolledModel = new List<OtherUserEnrolledModel>();

            using (var db = new SchoolEntities())
            {
                var temporders = (from otheruserorders in db.Course_Rosters
                                  where otheruserorders.EnrollMaster == AuthorizationHelper.CurrentStudentUser.STUDENTID && otheruserorders.STUDENTID != AuthorizationHelper.CurrentStudentUser.STUDENTID && otheruserorders.MasterOrderNumber != "" && otheruserorders.MasterOrderNumber != null && otheruserorders.Cancel == 0

                                  select new OtherUserEnrolledModel
                                  {
                                      OrderNumber = otheruserorders.OrderNumber,
                                      MasterOrderNumber = otheruserorders.MasterOrderNumber,
                                      StudentName = (from student in db.Students where student.STUDENTID == otheruserorders.STUDENTID select student.FIRST + " " + student.LAST).FirstOrDefault(),
                                      courseid = otheruserorders.COURSEID,
                                      courseName = (from course in db.Courses where course.COURSEID == otheruserorders.COURSEID select course.COURSENAME).FirstOrDefault(),
                                      CourseDate = (from coursedate in db.Course_Times where coursedate.COURSEID == otheruserorders.COURSEID select coursedate.COURSEDATE).FirstOrDefault(),
                                      TotalPaid = otheruserorders.TotalPaid,
                                      Rosterid = otheruserorders.RosterID
                                  });

                if (state.Filters != null)
                {
                    if (state.Filters.ContainsKey("keyword"))
                    {
                        var keyword = state.Filters["keyword"];
                        temporders = temporders.Where(orders => orders.courseName.Contains(keyword) || orders.OrderNumber.Contains(keyword) || orders.MasterOrderNumber.Contains(keyword) || orders.StudentName.Contains(keyword));
                    }
                }
                temporders = temporders.OrderBy(order => order.MasterOrderNumber);
                if (temporders.Count() > 0)
                {
                    var model = new GridModel<OtherUserEnrolledModel>(temporders.Count(), state);
                    temporders = model.Paginate(temporders);
                    foreach (var userorder in temporders)
                    {
                        userorder.FormatedDate = userorder.CourseDate.Value.ToString(Settings.Instance.GetPubDateFormat());
                        if (userorder.TotalPaid != null)
                        {
                            userorder.FormattedTotalPaid = String.Format("{0:C}", userorder.TotalPaid);
                        }
                        else
                        {
                            userorder.FormattedTotalPaid = "$0.00";
                        }
                        OtherUserEnrolledModel.Add(userorder);
                    }
                    model.Result = OtherUserEnrolledModel;
                    return model;
                }
                else
                {
                    return null;
                }

            }

        }
        public static List<FieldSpec> GetCustomRegistrationField()
        {
            using (var db = new SchoolEntities())
            {
                var registrationfields = (from regfield in db.FieldSpecs where regfield.TableName == "Students" && regfield.ShowinMultipleEnroll == 1 orderby regfield.FieldDisplaySortOrder ascending select regfield).ToList();
                foreach (var field in registrationfields.Where(fieldinmultiple => fieldinmultiple.ShowinMultipleEnroll == 1))
                {
                    if (field.FieldName == "studregfield1")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField1Name;
                    }
                    else if (field.FieldName == "studregfield2")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField2Name;
                    }
                    else if (field.FieldName == "studregfield3")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField3Name;
                    }
                    else if (field.FieldName == "studregfield4")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField4Name;
                    }
                    else if (field.FieldName == "studregfield5")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField5Name;
                    }
                    else if (field.FieldName == "studregfield6")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField6Name;
                    }
                    else if (field.FieldName == "studregfield7")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField7Name;
                    }
                    else if (field.FieldName == "studregfield8")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField8Name;
                    }
                    else if (field.FieldName == "studregfield9")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField9Name;
                    }
                    else if (field.FieldName == "studregfield10")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField10Name;
                    }
                    else if (field.FieldName == "studregfield11")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField11Name;
                    }
                    else if (field.FieldName == "studregfield12")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField12Name;
                    }
                    else if (field.FieldName == "studregfield13")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField13Name;
                    }
                    else if (field.FieldName == "studregfield14")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField14Name;
                    }
                    else if (field.FieldName == "studregfield15")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField15Name;
                    }
                    else if (field.FieldName == "studregfield16")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField16Name;
                    }
                    else if (field.FieldName == "studregfield17")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField17Name;
                    }
                    else if (field.FieldName == "studregfield18")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField18Name;
                    }
                    else if (field.FieldName == "studregfield19")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField19Name;
                    }
                    else if (field.FieldName == "studregfield20")
                    {
                        field.FieldLabel = Settings.Instance.GetMasterInfo().StudRegField20Name;
                    }
                }
                return registrationfields;
            }
        }


    }
}

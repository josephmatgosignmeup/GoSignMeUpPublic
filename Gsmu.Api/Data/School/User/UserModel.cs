using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.School.CourseRoster;
using Gsmu.Api.Data.Survey.Entities;
using Gsmu.Api.Data.ViewModels.Grid;
using entities = Gsmu.Api.Data.School.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using blackboard = Gsmu.Api.Integration.Blackboard;
using haiku = Gsmu.Api.Integration.Haiku;
using canvas = Gsmu.Api.Integration.Canvas;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Integration.Google;
using System.Data.Entity.Core.Objects;
using System.Data.Entity;
using Gsmu.Api.Commerce.ShoppingCart;
using Gsmu.Api.Integration.Blackboard;
using BlackBoardAPI;
using static BlackBoardAPI.BlackBoardAPIModel;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Gsmu.Api.Data.School.User
{
    public class UserModel
    {

        public static LoggedInUserType UserGroupToLoggedInUserType(string groupdId)
        {
            groupdId = groupdId.ToUpper();

            switch (groupdId)
            {
                case "ST":
                case "STUDENT":
                    return LoggedInUserType.Student;

                case "SP":
                case "SUPERVISOR":
                    return LoggedInUserType.Supervisor;

                case "IT":
                case "INSTRUCTORS":
                    return LoggedInUserType.Instructor;

                case "SA":
                case "MANAGER":
                case "SUBADMIN":
                    return LoggedInUserType.SubAdmin;


                case "AD":
                case "ADMIN":
                    return LoggedInUserType.Admin;

                default:
                    throw new NotImplementedException("Usergroup not implemented: " + groupdId);

            }
        }

        public static string LoggedInUserTypeToUserGroupPrefix(LoggedInUserType type)
        {
            switch (type)
            {
                case LoggedInUserType.Admin:
                    return "AD";

                case LoggedInUserType.Instructor:
                    return "IT";

                case LoggedInUserType.Student:
                    return "ST";

                case LoggedInUserType.SubAdmin:
                    return "SA";

                case LoggedInUserType.Supervisor:
                    return "SP";

                default:
                    throw new NotImplementedException("User type is not implemented: " + type.ToString());
            }
        }

        public UserModel() { }

        /// <summary>
        /// Query with Common Fields in all users (STUDENT,SUPERVISOR,INSTRUCTOR,MANAGER/SUBADMIN,ADMIN)
        /// </summary>
        /// <param name="userid">
        /// STUDENTID for STUDENTS; 
        /// SUPERVISORID for SUPERVISORS; 
        /// INSTRUCTORID for INSTRUCTORS; 
        /// MANAGERID for MANAGERS/SUBADMINS; 
        /// ADMINID for ADMINS; 
        /// </param>
        /// <param name="usergroup">
        /// ST or STUDENT = STUDENTS; 
        /// SP or SUPERVISOR = SUPERVISORS; 
        /// IT or INSTRUCTOR  = INSTRUCTORS; 
        /// SA or MANAGER or SUBADMIN = MANAGERS/SUBADMINS; 
        /// AD or ADMIN = ADMINS;  
        /// </param>

        public UserModel(string usergroup = "ST", string cmd = "addnew", int userid = 0, string process = "default")
        {
            string UserDashboardMode = HttpContext.Current.Session["UserDashboardMode"] as string;

            if (UserDashboardMode == "AdminEditMode")
            {
                userid = 0;
                cmd = "addnew";
            }
            else if (UserDashboardMode == "AdminView")
            {
                userid = Int32.Parse(HttpContext.Current.Session["UserDashboardUserid"] as string);
                usergroup = HttpContext.Current.Session["UserDashboardAbv"] as string;
            }
            else if (UserDashboardMode == "UserView")
            {

                if (!Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.IsLoggedIn && cmd != "addnew")
                {
                    throw new System.Web.HttpException(500, "You are required to login to access this area.");
                }

                if (Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.LoggedInUserType.ToString() == "Instructor")
                {
                    userid = cmd == "addnew" ? 0 : Gsmu.Api.Authorization.AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID;
                    usergroup = "IT";
                }
                else if (Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.LoggedInUserType.ToString() == "Student")
                {
                    userid = cmd == "addnew" ? 0 : Gsmu.Api.Authorization.AuthorizationHelper.CurrentStudentUser.STUDENTID;
                    usergroup = "ST";
                }
                else
                {
                    userid = 0;
                    usergroup = "ST";
                }
            }


            //throw new System.Web.HttpException(500, "UserDashboardMode:" + UserDashboardMode + " cmd:" + cmd + " usergroup:" + usergroup + " userid:" + userid);


            using (var db = new SchoolEntities())
            {
                if (usergroup == "ST" || usergroup == "STUDENT")
                {
                    try
                    {
                        //userid = cmd == "addnew" ? 0 : Gsmu.Api.Authorization.AuthorizationHelper.CurrentStudentUser.STUDENTID;
                        if (userid != 0)
                        {
                            var STdta = (from st in db.Students where st.STUDENTID == userid select st).First();
                            InitST(db, STdta, userid, process);
                        }
                    }
                    catch (Exception e)
                    {
                        if (userid != 0)
                        {
                            var STdta = new Entities.Student();
                            InitST(db, STdta, userid);
                        }
                    }
                }
                else if (usergroup == "SP" || usergroup.ToUpper() == "SUPERVISOR")
                {
                    try
                    {
                        var SPdta = (from sp in db.Supervisors where sp.SUPERVISORID == userid select sp).First();
                        InitSP(db, SPdta, userid);
                    }
                    catch
                    {
                        var SPdta = new Entities.Supervisor();
                        InitSP(db, SPdta, userid);
                    }
                }
                else if (usergroup == "IT" || usergroup.ToUpper() == "INSTRUCTORS")
                {
                    try
                    {
                        //userid = cmd == "addnew" ? 0 : Gsmu.Api.Authorization.AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID;
                        var ITdta = (from it in db.Instructors where it.INSTRUCTORID == userid select it).First();
                        InitIT(db, ITdta, userid);
                    }
                    catch
                    {
                        var ITdta = new Entities.Instructor();
                        InitIT(db, ITdta, userid);
                    }
                }
                else if (usergroup == "SA" || usergroup.ToUpper() == "MANAGER" || usergroup.ToUpper() == "SUBADMIN")
                {
                    userid = 0;
                    try
                    {
                        var SAdta = (from sa in db.Managers where sa.AUTOINDEX == userid select sa).First();
                        InitSA(db, SAdta, userid);
                    }
                    catch
                    {
                        var SAdta = new Entities.Manager();
                        InitSA(db, SAdta, userid);
                    }
                }
                else if (usergroup == "AD" || usergroup.ToUpper() == "ADMIN")
                {
                    userid = 0;
                    try
                    {
                        var ADdta = (from ad in db.adminpasses where ad.AdminID == userid select ad).First();
                        InitAD(db, ADdta, userid);
                    }
                    catch
                    {
                        var ADdta = new Entities.adminpass();
                        InitAD(db, ADdta, userid);
                    }
                }
                else
                {
                    throw new NotImplementedException("Error in usergroup variable: " + usergroup);
                }
            }
        }

        // for STUDENT
        private void InitST(SchoolEntities db, Entities.Student st, int userid, string process = "default")
        {
            int? system_time_zone = Gsmu.Api.Data.Settings.Instance.GetMasterInfo3().system_timezone_hour;
            int timezonesT = 0;

            double startDays = Settings.Instance.GetMasterInfo().CourseCancelDays == null ? 0.00 : Convert.ToDouble(Settings.Instance.GetMasterInfo().CourseCancelDays);
            switch (system_time_zone)
            {
                case 0:
                    timezonesT = 0; // -8
                    break;
                case 1:
                    timezonesT = -1; //-7
                    break;
                case 2:
                    timezonesT = -2; //-6
                    break;
                case 3:
                    timezonesT = -3; //-5
                    break;
                default:
                    timezonesT = 0; // -8
                    break;
            };
            Student = st;
            var dbs = new SurveyEntities();
            string prevOrderNo = "";
            UserInfo ui = new UserInfo();
            ui.usergroup = "STUDENT";
            ui.usergroupAbv = "ST";
            ui.userid = st.STUDENTID;
            ui.username = st.USERNAME;
            ui.first = st.FIRST;
            ui.last = st.LAST;
            ui.password = st.STUDNUM;
            ui.email = st.EMAIL;
            ui.additionalemail = st.additionalemail;
            ui.usertype = 0;
            ui.createdby = st.CreatedBy;
            ui.createdbyname = (from stu in db.Students
                                where stu.STUDENTID == st.CreatedBy
                                select stu.FIRST + " " + stu.LAST).FirstOrDefault();
            ui.createdname = (from stu in db.Students
                              where stu.CreatedBy == st.STUDENTID
                              select new UserInfo()
                              {
                                  first = stu.FIRST,
                                  last = stu.LAST
                              }).ToList();
            ui.school = st.SCHOOL;
            ui.district = st.DISTRICT;
            ui.grade = st.GRADE;

            ui.schoolName = (from sch in db.Schools where sch.locationid == st.SCHOOL select sch.LOCATION).FirstOrDefault();
            ui.districtName = (from dst in db.Districts where dst.DISTID == st.DISTRICT select dst.DISTRICT1).FirstOrDefault();
            ui.gradeName = (from grd in db.Grade_Levels where grd.GRADEID == st.GRADE select grd.GRADE).FirstOrDefault();

            ui.homephone = st.HOMEPHONE;
            ui.workphone = st.WORKPHONE;
            ui.fax = st.FAX;

            ui.address = st.ADDRESS;

            ui.courses = (from cr in db.Course_Rosters
                          join c in db.Courses on cr.COURSEID equals c.COURSEID
                          where cr.STUDENTID == st.STUDENTID
                          orderby cr.OrderNumber
                          select new
                          {
                              paidremainderamount = cr.paidremainderamount,
                              SelectedCredit = ((cr.Optionalcredithours8 > 0 || cr.Optionalcredithours8 < 0) ? "o8 " + cr.Optionalcredithours8.ToString() + "<br />" : "") + ((cr.Optionalcredithours7 > 0 || cr.Optionalcredithours7 < 0) ? "o7 " + cr.Optionalcredithours7.ToString() + "<br />" : "") + ((cr.Optionalcredithours6 > 0 || cr.Optionalcredithours6 < 0) ? "o6 " + cr.Optionalcredithours6.ToString() + "<br />" : "") + ((cr.Optionalcredithours5 > 0 || cr.Optionalcredithours5 < 0) ? "o5 " + cr.Optionalcredithours5.ToString() + "<br />" : "") + ((cr.Optionalcredithours4 > 0 || cr.Optionalcredithours4 < 0) ? "o4 " + cr.Optionalcredithours4.ToString() + "<br />" : "") + ((cr.Optionalcredithours3 > 0 || cr.Optionalcredithours3 < 0) ? "o1 " + cr.Optionalcredithours3.ToString() + "<br />" : "") + ((cr.Optionalcredithours2 > 0 || cr.Optionalcredithours2 < 0) ? "o2 " + cr.Optionalcredithours2.ToString() + "<br />" : "") + ((cr.Optionalcredithours1 > 0 || cr.Optionalcredithours1 < 0) ? "o1 " + cr.Optionalcredithours1.ToString() + "<br />" : "") + ((cr.InserviceHours > 0 || cr.InserviceHours < 0) ? "inservice " + cr.InserviceHours.ToString() + "<br />" : "") + ((cr.CustomCreditHours > 0 || cr.CustomCreditHours < 0) ? "cch " + cr.CustomCreditHours.ToString() + "<br />" : "") + ((cr.HOURS > 0 || cr.HOURS < 0) ? "credithours " + cr.HOURS.ToString() + "<br />" : ""),
                              COURSEID = cr.COURSEID,
                              Cancel = cr.Cancel,
                              WAITING = cr.WAITING,
                              ATTENDED = cr.ATTENDED,
                              TotalPaid = cr.TotalPaid.HasValue ? cr.TotalPaid.Value : 0,
                              COURSENUM = c.COURSENUM,
                              COURSENAME = c.COURSENAME,
                              OrderNumber = cr.OrderNumber,
                              MasterOrderNumber = cr.MasterOrderNumber,
                              Rosterid = cr.RosterID,
                              Coursetype = c.sessionid,
                              CourseCost = cr.CourseCost ?? "0",
                              CRPartialPaymentPaidAmount = cr.CRPartialPaymentPaidAmount.HasValue ? cr.CRPartialPaymentPaidAmount.Value : 0,
                              PaidInFullBool = (cr.PaidInFull == -1 || cr.PaidInFull == 1) ? true : false,
                              MinDate = (from ct in db.Course_Times where ct.COURSEID == c.COURSEID select ct.COURSEDATE).Min(),
                              MaxDate = (from ct in db.Course_Times where ct.COURSEID == c.COURSEID select ct.COURSEDATE).Max(),
                              CourseStartDateTime = db.Course_Times.Where(ct => ct.COURSEID == c.COURSEID).FirstOrDefault().STARTTIME, // start time of the first date in this course
                              CourseEndDateTime = db.Course_Times.Where(ct => ct.COURSEID == c.COURSEID).OrderByDescending(t => t.COURSEDATE).FirstOrDefault().FINISHTIME, // finish time of the last date in this course
                              Transcriptcount = (from tr in db.Transcripts
                                                 where tr.CourseId == c.COURSEID && tr.STUDENTID == st.STUDENTID
                                                 select tr.CourseId).Max(),
                              RosterMaterialTotal = (from rm in db.rostermaterials where rm.priceincluded == 0 && rm.RosterID == cr.RosterID select rm).Sum(rm => rm.price.HasValue ? rm.price.Value : 0),
                              RegisteredDate = cr.DATEADDED.Value
                          })
                          .Select(n => new UserCourses()
                          {
                              paidremainderamount = n.paidremainderamount,
                              SelectedCredit = n.SelectedCredit,
                              COURSEID = n.COURSEID,
                              Cancel = n.Cancel,
                              WAITING = n.WAITING,
                              ATTENDED = n.ATTENDED,
                              TotalPaid = n.TotalPaid,
                              COURSENUM = n.COURSENUM,
                              COURSENAME = n.COURSENAME,
                              OrderNumber = n.OrderNumber,
                              MasterOrderNumber = n.MasterOrderNumber,
                              Rosterid = n.Rosterid,
                              CourseCost = n.CourseCost,
                              CRPartialPaymentPaidAmount = n.CRPartialPaymentPaidAmount,
                              PaidInFullBool = n.PaidInFullBool,
                              MinDate = n.MinDate,
                              MaxDate = n.MaxDate,
                              CourseEvent = n.Coursetype.ToString(),
                              CourseStartDateTime = system_time_zone == 999 || system_time_zone == 0 ? n.CourseStartDateTime : DbFunctions.AddHours(n.CourseStartDateTime.Value, timezonesT),
                              CourseEndDateTime = system_time_zone == 999 || system_time_zone == 0 ? n.CourseEndDateTime : DbFunctions.AddHours(n.CourseEndDateTime.Value, timezonesT),
                              Transcriptcount = n.Transcriptcount,
                              RosterMaterialTotal = n.RosterMaterialTotal,
                              RegisteredDate = n.RegisteredDate
                          })
                          .ToList();


            foreach (var record in ui.courses)
            {
                try
                {
                    string coursetype = "enrolled";
                    if (record.Transcriptcount > 0 && record.Cancel == 0) { coursetype = "transcripted"; }
                    else if (record.Cancel == 1 || record.Cancel == -1 || record.Cancel == 2 || record.Cancel == 3 || record.Cancel == 4) { coursetype = "cancelled"; }
                    else if ((record.MaxDate >= DateTime.Now || record.MaxDate == null) && record.Cancel == 0 && record.WAITING == 0) { coursetype = "enrolled"; } //&& record.ATTENDED == 0 - commented section for now
                    else if ((record.MaxDate >= DateTime.Now || record.MaxDate == null) && record.Cancel == 0 && record.WAITING != 0) { coursetype = "waiting"; }
                    else if (record.MaxDate < DateTime.Now && record.Cancel == 0) { coursetype = "past"; }
                    //overrides the course type when the number of days prior to start is -1 to enable cancelling
                    if (startDays == -1 && coursetype == "past")
                    {
                        DateTime courseStartDateTime = Convert.ToDateTime(record.MinDate.Value.ToString("yyyy-MM-dd") + " " + record.CourseStartDateTime.Value.ToString("HH:mm:ss"));
                        if (courseStartDateTime > DateTime.Now)
                        {
                            coursetype = "enrolled";
                        }
                    }
                    if (coursetype != "past" && record.PaidInFullBool == true && record.WAITING == 0 && record.Transcriptcount == 0)
                    {
                        coursetype = "enrolled";
                    }
                    //if (ui.createdby != 0) { coursetype = "multiple"; }
                    record.CourseType = coursetype;

                    //  if (record.OrderNumber != prevOrderNo)
                    // {
                    // if (process != "default")
                    //{
                    var tempOrder = new OrderModel(record.OrderNumber, "usermodel");
                    record.TotalCourseCostDecimalbyOrder = tempOrder.OrderTotalOptimized;

                    // }
                    //  else
                    // {
                    //     record.TotalCourseCostDecimalbyOrder = 0;
                    // }
                    //  }

                    if (record.CourseCost == null)
                    {
                        record.CourseCostDecimal = 0;
                    }
                    else
                    {
                        record.CourseCostDecimal = decimal.Parse(record.CourseCost, System.Globalization.NumberStyles.AllowCurrencySymbol | System.Globalization.NumberStyles.Number);
                    }

                    record.RosterMaterialTotalDecimal = record.RosterMaterialTotal.HasValue ? decimal.Parse(record.RosterMaterialTotal.Value.ToString()) : 0;
                    if (record.PaidInFullBool && record.TotalPaid > 0)
                    {
                        //if paid in full or by order PaidPerCourse computed by ProRate
                        if (record.TotalCourseCostDecimalbyOrder == 0)
                        {
                            record.PaidPerCourse = 0;
                        }
                        else
                        {
                            record.PaidPerCourse = (((record.CourseCostDecimal + record.RosterMaterialTotalDecimal) / record.TotalCourseCostDecimalbyOrder) * record.TotalPaid);
                        }
                    }
                    else
                    {
                        record.PaidPerCourse = record.CRPartialPaymentPaidAmount.HasValue ? decimal.Parse(record.CRPartialPaymentPaidAmount.Value.ToString()) : 0;
                    }
                    if (record.SelectedCredit != "")
                    {
                        record.SelectedCredit = "Selected Credit(s) <br />" + record.SelectedCredit;
                        record.SelectedCredit = record.SelectedCredit.Replace("inservice", CourseCreditHelper.GetCreditLabel(CourseCreditType.InService) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("credithours", CourseCreditHelper.GetCreditLabel(CourseCreditType.Credit) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("cch", CourseCreditHelper.GetCreditLabel(CourseCreditType.Custom) + " ");

                        record.SelectedCredit = record.SelectedCredit.Replace("o1", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional1) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o2", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional2) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o3", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional3) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o4", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional4) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o5", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional5) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o6", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional6) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o7", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional7) + " ");
                        record.SelectedCredit = record.SelectedCredit.Replace("o8", CourseCreditHelper.GetCreditLabel(CourseCreditType.Optional8) + " ");


                    }
                }
                catch { }

                prevOrderNo = record.OrderNumber;
            }



            ui.certifications = (from cs in db.CertificationsStudents
                                 join crt in db.Certifications on cs.CertificationsId equals crt.CertificationsId
                                 where cs.StudentId == st.STUDENTID
                                 orderby crt.CertificationsId
                                 select new UserCertifications()
                                 {
                                     ExpireDate = cs.ExpireDate,
                                     CertificationsStudentId = cs.CertificationsStudentId,
                                     CertificationsId = cs.CertificationsId,
                                     CertificationsTitle = crt.CertificationsTitle,
                                 }).ToList();

            ui.certificationsCompleteds = (from cs in db.CertificationsStudentCompleteds
                                           join crt in db.Certifications on cs.CertificationsId equals crt.CertificationsId
                                           where cs.StudentId == st.STUDENTID
                                           orderby crt.CertificationsId
                                           select new UserCertificationsCompleteds()
                                           {
                                               CertificationsStudentId = cs.CertificationsStudentId,
                                               CertificationsId = cs.CertificationsId,
                                               CertificationsTitle = crt.CertificationsTitle,
                                               CompletionDate = cs.CompletionDate,
                                               CertificateID = crt.CertificationsCustomCertId
                                           }).ToList();


            ui.usersurveyCompleteds = (from u in dbs.Users
                                       where u.GSMU_UserID == st.STUDENTID
                                       select new UserSurveyCompleted()
                                       {
                                           CourseID = u.CourseID,
                                           DateTaken = u.DateInserted,
                                           SurveyID = u.SurveyID,
                                       }).ToList();

            ui.CertificatesReceived = (from emltr in db.EmailAuditTrails
                                       where (emltr.AuditProcess.Contains("reports_certificate"))
                                          && (emltr.EmailToUserIDs.Contains("studentid\":" + st.STUDENTID) || emltr.EmailTo == (st.EMAIL.ToString()))
                                       select new UserCertificatesReceived()
                                       {
                                           RecvdDate = emltr.AuditDate,
                                           RecvdSubj = emltr.EmailSubject,
                                           Attachment = emltr.AttachmentNameMemo,
                                       }).ToList();

            ui.certCompletedsNotCourseCert = GetCertStudent(st.STUDENTID);


            ui.certificatesCompleteds = (from tran in db.Transcripts
                                         join c in db.Courses on tran.CourseId equals c.COURSEID
                                         join cr in db.Course_Rosters on tran.STUDENTID equals cr.STUDENTID
                                         where tran.STUDENTID.Value == st.STUDENTID
                                         && cr.COURSEID == tran.CourseId
                                         && (c.coursecertificate >= 0) // at somepoint -1 was default but not anymore || c.coursecertificate == -1)  //show only 0 (use default) and greater than zero
                                         && tran.ATTENDED != 0
                                         select new UserCertificatesCompleteds()
                                         {
                                             CourseId = tran.CourseId,
                                             CourseNum = tran.CourseNum,
                                             CourseName = tran.CourseName,
                                             StartDate = (from ct in db.Course_Times where ct.COURSEID == tran.CourseId select ct.COURSEDATE).Min(),
                                             CompletionDate = tran.CourseCompletionDate,
                                             DateAutoCertSent = tran.DateAutoCertSent,
                                             CertType = "CourseCert",
                                             CertNum = tran.TranscriptID
                                         }).ToList();


            //this will remove Course with Survey ( show only completed )
            var certcompltd = ui.certificatesCompleteds;
            try
            {
                foreach (var certcomp in certcompltd)
                {

                    var CourseWithSurvey = (from csurvey in dbs.CourseSurveys where csurvey.CourseID == certcomp.CourseId select csurvey.SurveyID).FirstOrDefault();
                    if (CourseWithSurvey != 0 && !certcomp.DateAutoCertSent.HasValue)
                    {
                        ui.certificatesCompleteds.RemoveAll(d => d.CourseId == certcomp.CourseId);
                    }
                }

            }
            catch
            {
            }




            foreach (var certnotcors in ui.certCompletedsNotCourseCert)
            {
                try
                {
                    ui.certificatesCompleteds.Add(certnotcors);
                }
                catch
                {
                }

            }




            //this will add all Completed Survey or Course with Survey
            var surveyselected = (from survey in dbs.Surveys select survey.AfterSurveyCertificate).FirstOrDefault();
            foreach (var survycomp in ui.usersurveyCompleteds)
            {
                try
                {
                    surveyselected = (from survey in dbs.Surveys where survey.SurveyID == survycomp.SurveyID select survey.AfterSurveyCertificate).FirstOrDefault();
                    if (surveyselected != null && surveyselected != "")
                    {
                        ui.certificatesCompleteds.Add(
                          new UserCertificatesCompleteds()
                          {
                              CourseId = survycomp.CourseID,
                              CourseNum = (from c in db.Courses where c.COURSEID == survycomp.CourseID select c.COURSENUM).First(),
                              CourseName = (from c in db.Courses where c.COURSEID == survycomp.CourseID select c.COURSENAME).First(),
                              StartDate = (from ct in db.Course_Times where ct.COURSEID == survycomp.CourseID select ct.COURSEDATE).Min(),
                              CompletionDate = survycomp.DateTaken,
                              CertType = "SurveyCert",
                              CertNum = survycomp.SurveyID
                          });
                    }
                }
                catch (Exception e)
                {
                }
            }
            //}

            ui.surveylists = (from cs in dbs.CourseSurveys
                              join s in dbs.Surveys on cs.SurveyID equals s.SurveyID
                              where s.SurveyLoginType == 0 && s.SurveyRequired != 0
                              select new UserSurveys()
                              {
                                  SurveyID = cs.SurveyID,
                                  CourseID = cs.CourseID,
                                  Name = s.Name,
                                  SurveyRequired = s.SurveyRequired,
                                  SurveyRequiredText = (s.SurveyRequired == 1 ? "(Required - click to take)" : " "),
                              }).ToList();

            ui.usersurveys = (from cs in ui.surveylists
                              join c in ui.courses on cs.CourseID equals c.COURSEID
                              where c.Cancel == 0 && c.ATTENDED != 0 && c.MaxDate <= DateTime.Now.Date
                              select new UserSurveys()
                              {
                                  SurveyID = cs.SurveyID,
                                  Name = cs.Name,
                                  SurveyRequired = cs.SurveyRequired,
                                  SurveyRequiredText = (cs.SurveyRequired == 1 ? "(Required - click to take)" : " "),
                              }).ToList();


            ui.ProfileImage = st.ProfileImage;
            ui.TempProfileImage = st.TempProfileImage;

            CommonUserInfo = ui;
        }

        // for SUPERVISOR
        private void InitSP(SchoolEntities db, Entities.Supervisor sp, int userid)
        {
            Supervisor = sp;

            UserInfo ui = new UserInfo();
            ui.usergroup = "SUPERVISOR";
            ui.usergroupAbv = "SP";
            ui.userid = sp.SUPERVISORID;
            ui.username = sp.UserName;
            ui.first = sp.FIRST;
            ui.last = sp.LAST;
            ui.password = sp.PASSWORD;
            ui.email = sp.EMAIL;
            ui.usertype = 10;

            ui.school = sp.SCHOOL;
            ui.district = sp.DISTRICT;
            ui.grade = sp.GRADE;

            ui.schoolName = (from sch in db.Schools where sch.locationid == sp.SCHOOL select sch.LOCATION).FirstOrDefault();
            ui.districtName = (from dst in db.Districts where dst.DISTID == sp.DISTRICT select dst.DISTRICT1).FirstOrDefault();
            ui.gradeName = (from grd in db.Grade_Levels where grd.GRADEID == sp.GRADE select grd.GRADE).FirstOrDefault();

            ui.homephone = "n/a";
            ui.workphone = sp.PHONE;
            ui.fax = sp.FAX;

            CommonUserInfo = ui;
        }

        // for INSTRUCTOR
        private void InitIT(SchoolEntities db, Entities.Instructor it, int userid)
        {
            Instructor = it;

            UserInfo ui = new UserInfo();
            ui.usergroup = "INSTRUCTOR";
            ui.usergroupAbv = "IT";
            ui.userid = it.INSTRUCTORID;
            ui.username = it.USERNAME;
            ui.first = it.FIRST;
            ui.last = it.LAST;
            ui.password = it.PASSWORD;
            ui.email = it.EMAIL;
            ui.usertype = 20;

            ui.school = it.SCHOOL;
            ui.district = it.DISTRICT;

            int categoryID = 0;
            string strGRADELEVEL = string.IsNullOrEmpty(it.GRADELEVEL) ? "0" : it.GRADELEVEL;
            int gradeid = int.TryParse(strGRADELEVEL, out categoryID) ? int.Parse(strGRADELEVEL) : 0;
            ui.grade = gradeid;

            ui.schoolName = (from sch in db.Schools where sch.locationid == it.SCHOOL select sch.LOCATION).FirstOrDefault();
            ui.districtName = (from dst in db.Districts where dst.DISTID == it.DISTRICT select dst.DISTRICT1).FirstOrDefault();
            ui.gradeName = (from grd in db.Grade_Levels where grd.GRADEID == gradeid select grd.GRADE).FirstOrDefault();

            ui.homephone = it.HOMEPHONE;
            ui.workphone = it.WORKPHONE;
            ui.fax = it.FAX;
            //ui.courses      = null;

            ui.ProfileImage = it.PhotoImage;
            ui.TempProfileImage = it.TempProfileImage;

            CommonUserInfo = ui;
        }

        // for MANAGER/SUBADMIN
        private void InitSA(SchoolEntities db, Entities.Manager sa, int userid)
        {
            Manager = sa;

            UserInfo ui = new UserInfo();
            ui.usergroup = "SUBADMIN";
            ui.usergroupAbv = "SA";
            ui.userid = sa.AUTOINDEX;
            ui.username = sa.MANAGERID;
            ui.first = sa.FIRST;
            ui.last = sa.LAST;
            ui.password = sa.PASSWORD;
            ui.email = sa.EMAIL;
            ui.usertype = 30;

            ui.schoolid = 0;
            ui.districtid = 0;
            ui.gradeid = 0;

            ui.schoolName = "n/a";
            ui.districtName = "n/a";
            ui.gradeName = "n/a";

            ui.homephone = "n/a";
            ui.workphone = "n/a";
            ui.fax = "n/a";

            CommonUserInfo = ui;
        }

        // for ADMIN
        private void InitAD(SchoolEntities db, Entities.adminpass ad, int userid)
        {
            Admin = ad;

            UserInfo ui = new UserInfo();
            ui.usergroup = "ADMIN";
            ui.usergroupAbv = "AD";
            ui.userid = ad.AdminID;
            ui.username = ad.username;
            ui.first = "";
            ui.last = "";
            ui.password = ad.userpass;
            ui.email = ad.email;
            ui.usertype = 40;

            ui.schoolid = 0;
            ui.districtid = 0;
            ui.gradeid = 0;

            ui.schoolName = "n/a";
            ui.districtName = "n/a";
            ui.gradeName = "n/a";

            ui.homephone = "n/a";
            ui.workphone = "n/a";
            ui.fax = "n/a";

            CommonUserInfo = ui;
        }

        /// <summary>
        /// Common field name for all users (STUDENT,SUPERVISOR,INSTRUCTOR,MANAGER/SUBADMIN,ADMIN)
        /// </summary>
        public UserInfo CommonUserInfo { get; set; }

        /// <summary>
        /// STUDENT data using its original field names
        /// </summary>
        public Entities.Student Student { get; set; }

        /// <summary>
        /// SUPERVISOR data using its original field names
        /// </summary>
        public Entities.Supervisor Supervisor { get; set; }

        /// <summary>
        /// INSTRUCTOR data using its original field names
        /// </summary>
        public Entities.Instructor Instructor { get; set; }

        /// <summary>
        /// MANAGER/SUBADMIN data using its original field names
        /// </summary>
        public Entities.Manager Manager { get; set; }

        /// <summary>
        /// ADMIN data using its original field names
        /// </summary>
        public Entities.adminpass Admin { get; set; }

        /// <summary>
        /// Submit User Info for Update or Add New. example id = ST1234
        /// </summary>
        /// <param name="ui">
        /// UserInfo model from Gsmu.Api.Data.School.User
        /// </param>
        /// <param name="id">
        /// Must combination of Prefx group and userid
        /// If userid = zero(0) then it is Add new. Example: ST0, add new to student
        /// Prefix:
        /// ST = STUDENTS; 
        /// SP = SUPERVISORS; 
        /// IT = INSTRUCTORS; 
        /// SA = MANAGERS/SUBADMINS; 
        /// AD = ADMINS; 
        /// </param>
        public UserInfo SumbitUserInfo(UserInfo ui, string usergroup = "ST")
        {

            if (ui.password == "**********" || string.IsNullOrEmpty(ui.password)) { ui.password = null; }


            try
            {
                if (usergroup == "ST" || usergroup == "STUDENT")
                {
                    return NewOrUpdateST(ui);
                }
                else if (usergroup == "SP" || usergroup.ToUpper() == "SUPERVISOR")
                {
                    return NewOrUpdateSP(ui);
                }
                else if (usergroup == "IT" || usergroup.ToUpper() == "INSTRUCTORS")
                {
                    return NewOrUpdateIT(ui);
                }
                else if (usergroup == "SA" || usergroup.ToUpper() == "MANAGER" || usergroup.ToUpper() == "SUBADMIN")
                {
                    return NewOrUpdateSA(ui);
                }
                else if (usergroup == "AD" || usergroup.ToUpper() == "ADMIN")
                {
                    return NewOrUpdateAD(ui);
                }
            }
            catch (Exception e)
            {
                var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
                if (TurnOnDebugTracingMode != null)
                {
                    if (TurnOnDebugTracingMode.ToLower() == "on")
                    {
                        Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                        Audittrail.TableName = "User";
                        Audittrail.AuditDate = DateTime.Now;
                        Audittrail.RoutineName = "User - SumbitUserInfo" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                        Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                        try
                        {
                            Audittrail.AuditAction = "Error Detail:" + e.Message;
                        }
                        catch { }
                        Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                        LogManager.LogSiteActivity(Audittrail);
                    }
                }
                throw new Exception(string.Format("Error in usergroup variable: {0}, Exception: {1}", usergroup, e));
            }

            return new UserInfo();

        }



        public UserInfo NewOrUpdateST(UserInfo ui)
        {
            var Context = new SchoolEntities();
            string tempUsername = null;
            int isBlacboardOwned = 0;
            int fountExisting = 0;
            int IsGoogleUser = 0;
            int usernameMaskNum = Settings.Instance.GetFieldMasks("username", "Students").DefaultMaskNumber;
            bool haikuUsernameReal = haiku.Configuration.Instance.HaikuAuthenticationEnabled;
            bool isShibbolethUser = false;
            Entities.Student st = new Entities.Student();
            // if (ui.userid != 0) { st = Context.Students.First(s => s.STUDENTID == ui.userid); }
            if (ui.userid != 0)
            {
                st = (from s in Context.Students where s.STUDENTID == ui.userid select s).FirstOrDefault();
                if (st.google_user != null && st.google_user != 0)
                {
                    IsGoogleUser = 1;
                }
                if ((Settings.Instance.GetMasterInfo4().shibboleth_sso_enabled == 1) || (Settings.Instance.GetMasterInfo4().shibboleth_sso_enabled == 2))
                {
                    if (st.STUDNUM.IndexOf("hibboleth Assign") > 0 || (!string.IsNullOrEmpty(ui.password) && ui.password.IndexOf("hibboleth Assign") > 0))
                    {
                        isShibbolethUser = true;
                    }
                }
            }
            if (Gsmu.Api.Integration.Haiku.Configuration.Instance.HaikuAuthenticationEnabled)
            {
                if (!string.IsNullOrEmpty(ui.username) && IsGoogleUser == 0 && (ui.username.Contains('@') || ui.username.Contains('_') || ui.username.Contains('-')))
                {
                    throw new NotImplementedException("Invalid username: " + ui.username, new Exception("Invalid username: " + ui.username));

                }
            }
            if (st != null)
            {

                if (st.date_imported_from_bb != null && blackboard.Configuration.Instance.BlackboardSsoEnabled)
                {
                    //if detault value is 1990-01-01, it means the account isnt a BB import.
                    if (DateTime.Parse(st.date_imported_from_bb.ToString()).Year.ToString() != "1990")
                    {
                        isBlacboardOwned = 1;
                    }
                }
                if (usernameMaskNum == 97 && !haikuUsernameReal && isBlacboardOwned == 0 && isShibbolethUser == false)
                {

                    tempUsername = ui.email;
                    ui.username = ui.email;
                    st.USERNAME = ui.email;

                }
                else if (usernameMaskNum == 97 && isShibbolethUser)
                {
                    //st.USERNAME = ui.username;
                }
                else if (usernameMaskNum == 6 && !haikuUsernameReal)
                {
                    //auto create - last + period + first name + appending #
                    //starting 0
                    if ((ui.last != "") && (ui.first != "") && (ui.last != null) && ui.first != null)
                    {
                        tempUsername = ui.last + "." + ui.first;
                        fountExisting = (from se in Context.Students where se.USERNAME.Contains(tempUsername) select se).Count();
                        if (fountExisting >= 1)
                        {
                            //found - assuming client never delete user and instead archive or deactivate until implement hide option                     
                            tempUsername = string.Concat(tempUsername, fountExisting.ToString());
                            st.USERNAME = tempUsername;
                            ui.username = tempUsername;
                        }
                        else
                        {
                            //none exist
                            st.USERNAME = tempUsername;
                            ui.username = tempUsername;

                        }
                    }
                    else
                    {
                        if (AuthorizationHelper.CurrentUser.IsLoggedIn)
                        {

                            ui.username = AuthorizationHelper.CurrentStudentUser.USERNAME;
                            ui.password = AuthorizationHelper.CurrentStudentUser.STUDNUM;
                            tempUsername = AuthorizationHelper.CurrentStudentUser.USERNAME;
                        }
                    }
                }
                else
                {
                    if (ui.username != null)
                    {
                        st.USERNAME = ui.username;
                        tempUsername = ui.username;

                    }

                }
                if (ui.first != null) { st.FIRST = ui.first; }
                if (ui.last != null) { st.LAST = ui.last; }
                st.ResetPassword = 0;

                var bbConfig = blackboard.Configuration.Instance;
                if (ui.password != null)
                {
                    st.STUDNUM = ui.password;

                    try
                    {
                        bool commonlogin = false;
                        if (Settings.Instance.GetMasterInfo2().CommonPublicLogin == 1 && Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1)
                        {
                            if (commonlogin)
                            {
                                Gsmu.Api.Data.School.Entities.Instructor instructor = Context.Instructors.FirstOrDefault(inst => inst.USERNAME == ui.username);
                                if (instructor != null)
                                {
                                    instructor.PASSWORD = ui.password;
                                }
                                Gsmu.Api.Data.School.Entities.Supervisor supervisor = Context.Supervisors.FirstOrDefault(inst => inst.UserName == ui.username);
                                if (supervisor != null)
                                {
                                    supervisor.PASSWORD = ui.password;
                                }
                            }
                        }
                    }

                    catch
                    {

                    }

                    if (bbConfig.BlackboardRealtimeStudentSyncEnabled)
                    {

                        if (Configuration.Instance.BlackboardUseAPI && Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardMembershipIntegrationEnabled)
                        {
                            BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            var user = handelr.GetUserDetails(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, st.USERNAME,"", "", jsonToken);

                            if (user.userName != null)
                            {
                                BBUser user_update = new BBUser();
                                if (Settings.Instance.GetMasterInfo4().blackboard_students_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_students_dsk != null)
                                {
                                    string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_students_dsk;
                                    if (!string.IsNullOrEmpty(tempDSK))
                                    {
                                        int countbar = tempDSK.Count(f => f == '_');
                                        //if (tempDSK.IndexOf("_") < 0)
                                        if (countbar != 2)
                                        {
                                            var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, Gsmu.Api.Integration.Blackboard.Configuration.Instance.StudentDsk, "dsk", "", jsonToken);
                                            datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                            string actualDSK = globaldatasource.id;

                                            user_update.dataSourceId = actualDSK;
                                        }
                                        else
                                        {
                                            user_update.dataSourceId = tempDSK;
                                        }
                                    }
                                }
                                user_update.password = st.STUDNUM;

                                user_update.contact = new ProfileContactObj();
                                user_update.contact.email = ui.email;
                                user_update.name = new ProfileNameObj();
                                user_update.name.given = st.FIRST;
                                user_update.name.family = st.LAST;

                                BBRespUserProfile updateduser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, user.userName, "", "", jsonToken, "");
                            }
                            else
                            {
                                BBUser user_update = new BBUser();
                                if (Settings.Instance.GetMasterInfo4().blackboard_students_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_students_dsk != null)
                                {
                                    string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_students_dsk;
                                    if (!string.IsNullOrEmpty(tempDSK))
                                    {
                                        int countbar = tempDSK.Count(f => f == '_');
                                        //if (tempDSK.IndexOf("_") < 0)
                                        if (countbar != 2)
                                        {
                                            var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, Gsmu.Api.Integration.Blackboard.Configuration.Instance.StudentDsk, "dsk", "", jsonToken);
                                            datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                            string actualDSK = globaldatasource.id;

                                            user_update.dataSourceId = actualDSK;
                                        }
                                        else
                                        {
                                            user_update.dataSourceId = tempDSK;
                                        }
                                    }                                    
                                }
                                user_update.userName = ui.username;
                                user_update.password = ui.password;

                                user_update.contact = new ProfileContactObj();
                                user_update.contact.email = ui.email;
                                user_update.name = new ProfileNameObj();
                                user_update.name.given = ui.first;
                                user_update.name.family = ui.last;

                              //  string[] bbSystemRole = new string[1];
                                string[] bbInstitutionRole = new string[1];
                                //bbSystemRole[0] = Configuration.Instance.BlackboardSystemRole;
                                bbInstitutionRole[0] =Configuration.Instance.BlackboardInstitutionalRole;
                                //user_update.systemRoleIds = bbSystemRole;
                                user_update.institutionRoleIds = bbInstitutionRole;

                                BBRespUserProfile updateduser = handelr.CreateNewUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update,"", jsonToken,"", Configuration.Instance.StudentInstitutionalHierarchyNodeId);
                                st.Blackboard_user_UUID = updateduser.uuid;   
                            }
                        }
                        else
                        {
                            var checkUserExist = blackboard.Connector.UserConnector.SeachUserByFields("", st.USERNAME);
                            if (checkUserExist.IsSuccess && st.USERNAME != "")
                            {
                                var userResult = blackboard.Connector.UserConnector.UpdateBBUserAccount(st.USERNAME, ui.password, ui.first, ui.last, ui.email, "",ui.district,ui.school,ui.grade);

                            }
                            else
                            {
                                if (st.USERNAME != "")
                                {

                                    var userResult = blackboard.Connector.UserConnector.InsertBBUserAccount(st.USERNAME, ui.password, ui.first, ui.last, ui.email, ui.district, ui.school, ui.grade);
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (bbConfig.BlackboardRealtimeStudentSyncEnabled)
                    {
                        if (Configuration.Instance.BlackboardUseAPI && Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardMembershipIntegrationEnabled)
                        {
                            BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            BBUser user_update = new BBUser();

                            if (Settings.Instance.GetMasterInfo4().blackboard_students_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_students_dsk != null)
                            {
                                string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_students_dsk;
                                if (!string.IsNullOrEmpty(tempDSK))
                                {
                                    if (tempDSK.IndexOf("_") < 0)
                                    {
                                        var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, Gsmu.Api.Integration.Blackboard.Configuration.Instance.StudentDsk, "dsk", "", jsonToken);
                                        datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                        string actualDSK = globaldatasource.id;

                                        user_update.dataSourceId = actualDSK;
                                    }
                                    else
                                    {
                                        user_update.dataSourceId = tempDSK;
                                    }
                                }
                            }

                            //user_update.userName = Request["username"];
                            user_update.password = st.STUDNUM;
                            //user_update.userName = st.Blackboard_user_UUID;
                            user_update.contact = new ProfileContactObj();
                            user_update.contact.email = ui.email;
                            user_update.name = new ProfileNameObj();
                            user_update.name.given = st.FIRST;
                            user_update.name.family = st.LAST;

                            BBRespUserProfile updateduser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, st.Blackboard_user_UUID, "uuid", "", jsonToken, "");
                        }
                        else
                        {
                            var checkUserExist = blackboard.Connector.UserConnector.SeachUserByFields("", st.USERNAME);
                            if (checkUserExist.IsSuccess && st.USERNAME != "")
                            {
                                var userResult = blackboard.Connector.UserConnector.UpdateBBUserAccount(st.USERNAME, ui.password, ui.first, ui.last, ui.email, "",ui.district,ui.school,ui.grade);
                            }
                        }
                    }

                }
                if (ui.email != null) { st.EMAIL = ui.email; }
                if (ui.school != null && ui.school != 0) { st.SCHOOL = ui.school; }
                if (ui.LocationID2 != null) { st.LocationID2 = ui.LocationID2.Value; }
                if (ui.LocationID3 != null) { st.LocationID3 = ui.LocationID3.Value; }
                if (ui.district != null && ui.district != 0) { st.DISTRICT = ui.district; }
                if (ui.grade != null && ui.grade != 0) { st.GRADE = ui.grade; }
                if (ui.homephone != null) { st.HOMEPHONE = ui.homephone; }
                if (ui.workphone != null) { st.WORKPHONE = ui.workphone; }
                if (ui.fax != null) { st.FAX = ui.fax; }
                if (Settings.Instance.GetMasterInfo3().allowCrossUserUpdate == 2 && AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    if (AuthorizationHelper.CurrentSupervisorUser.ADDRESS != null) { st.ADDRESS = AuthorizationHelper.CurrentSupervisorUser.ADDRESS; }
                    if (AuthorizationHelper.CurrentSupervisorUser.CITY != null) { st.CITY = AuthorizationHelper.CurrentSupervisorUser.CITY; }
                    if (AuthorizationHelper.CurrentSupervisorUser.STATE != null) { st.STATE = AuthorizationHelper.CurrentSupervisorUser.STATE; }
                    if (AuthorizationHelper.CurrentSupervisorUser.ZIP != null) { st.ZIP = AuthorizationHelper.CurrentSupervisorUser.ZIP; }
                    if (AuthorizationHelper.CurrentSupervisorUser.PHONE != null) { st.HOMEPHONE = AuthorizationHelper.CurrentSupervisorUser.PHONE; }
                }
                else
                {
                    if (ui.address != null) { st.ADDRESS = ui.address; }
                    if (ui.city != null) { st.CITY = ui.city; }
                    if (ui.state != null) { st.STATE = ui.state; }
                    if (ui.zip != null) { st.ZIP = ui.zip; }
                    if (ui.country != null) { st.COUNTRY = ui.country; }
                }
                if (ui.distemployee.ToString() != null) { st.DISTEMPLOYEE = ui.distemployee; }
                if (ui.ss != null && ui.ss != "************") { st.SS = "enc." + Gsmu.Api.Authorization.AuthorizationHelper.RC2Encrytion(ui.ss); }
                //if (ui.ss != null) { st.SS = ui.ss; }
                if (ui.HiddenStudRegField1 != null) { st.HiddenStudRegField1 = ui.HiddenStudRegField1; }
                if (ui.HiddenStudRegField2 != null) { st.HiddenStudRegField2 = ui.HiddenStudRegField2; }
                if (ui.HiddenStudRegField3 != null) { st.HiddenStudRegField3 = ui.HiddenStudRegField3; }
                if (ui.HiddenStudRegField4 != null) { st.HiddenStudRegField4 = ui.HiddenStudRegField4; }
                if (ui.ReadOnlyStudRegField1 != null) { st.ReadOnlyStudRegField1 = ui.ReadOnlyStudRegField1; }
                if (ui.ReadOnlyStudRegField2 != null) { st.ReadOnlyStudRegField2 = ui.ReadOnlyStudRegField2; }
                if (ui.ReadOnlyStudRegField3 != null) { st.ReadOnlyStudRegField3 = ui.ReadOnlyStudRegField3; }
                if (ui.ReadOnlyStudRegField4 != null) { st.ReadOnlyStudRegField4 = ui.ReadOnlyStudRegField4; }
                if (ui.StudRegField1 != null) { st.StudRegField1 = ui.StudRegField1; }
                if (ui.StudRegField2 != null) { st.StudRegField2 = ui.StudRegField2; }
                if (ui.StudRegField3 != null) { st.StudRegField3 = ui.StudRegField3; }
                if (ui.StudRegField4 != null) { st.StudRegField4 = ui.StudRegField4; }
                if (ui.StudRegField5 != null) { st.StudRegField5 = ui.StudRegField5; }
                if (ui.StudRegField6 != null) { st.StudRegField6 = ui.StudRegField6; }
                if (ui.StudRegField7 != null) { st.StudRegField7 = ui.StudRegField7; }
                if (ui.StudRegField8 != null) { st.StudRegField8 = ui.StudRegField8; }
                if (ui.StudRegField9 != null) { st.StudRegField9 = ui.StudRegField9; }
                if (ui.StudRegField10 != null) { st.StudRegField10 = ui.StudRegField10; }
                if (ui.StudRegField11 != null) { st.StudRegField11 = ui.StudRegField11; }
                if (ui.StudRegField12 != null) { st.StudRegField12 = ui.StudRegField12; }
                if (ui.StudRegField13 != null) { st.StudRegField13 = ui.StudRegField13; }
                if (ui.StudRegField14 != null) { st.StudRegField14 = ui.StudRegField14; }
                if (ui.StudRegField15 != null) { st.StudRegField15 = ui.StudRegField15; }
                if (ui.StudRegField16 != null) { st.StudRegField16 = ui.StudRegField16; }
                if (ui.StudRegField17 != null) { st.StudRegField17 = ui.StudRegField17; }
                if (ui.StudRegField18 != null) { st.StudRegField18 = ui.StudRegField18; }
                if (ui.StudRegField19 != null) { st.StudRegField19 = ui.StudRegField19; }
                if (ui.StudRegField20 != null) { st.StudRegField20 = ui.StudRegField20; }
                if (ui.additionalemail != null) { st.additionalemail = ui.additionalemail; }
                //Added default value on registration
                if (st.loginTally == null) { st.loginTally = 0; };
                if (st.DISTRICT == null) { st.DISTRICT = 0; };
                if (st.SCHOOL == null) { st.SCHOOL = 0; };
                if (st.GRADE == null) { st.GRADE = 0; };
                if (st.AuthFromLDAP == null) { st.AuthFromLDAP = 0; };
                DateTime defaultdate = DateTime.Parse("1990-01-01 00:00:00.000");
                if (st.LastUpdateTime == null) { st.LastUpdateTime = defaultdate; };
                if (st.LastTimeSAPSync == null) { st.LastTimeSAPSync = defaultdate; };
                if (st.date_modified == null) { st.date_modified = defaultdate; };
                if (st.date_bb_integrated == null) { st.date_bb_integrated = defaultdate; };
                if (st.ProfileViewedDateTime == null) { st.ProfileViewedDateTime = defaultdate; };
                if (st.date_imported_from_bb == null) { st.date_imported_from_bb = defaultdate; };
                if (st.lastlogin == null) { st.lastlogin = defaultdate; };
                if (st.membershipexpiredate == null) { st.membershipexpiredate = defaultdate; };
                if (st.resetPasswordDate == null) { st.resetPasswordDate = defaultdate; };
                if (st.ResetPassword == null) { st.ResetPassword = 0; };
                if (!AuthorizationHelper.CurrentUser.IsLoggedIn || ui.userid == 0)
                {
                    st.DateAdded = System.DateTime.Now;

                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        st.CreatedBy = AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID;
                    }

                }
                else if (AuthorizationHelper.CurrentUser.IsLoggedIn || ui.userid != 0)
                {
                    st.date_modified = System.DateTime.Now;
                }
                if (ui.ProfileImage != null && ui.ProfileImage != st.ProfileImage)
                {
                    string OldProfileImage = st.ProfileImage;
                    st.ProfileImage = ui.ProfileImage;
                    st.TempProfileImage = "";
                    if (System.IO.File.Exists(HttpContext.Current.Request.MapPath(UploadImage.ItemUploadFolderPath + OldProfileImage)))
                    {
                        System.IO.File.Delete(HttpContext.Current.Request.MapPath(UploadImage.ItemUploadFolderPath + OldProfileImage));
                    }

                }
                if (st.USERNAME == null && ui.username == null)
                {

                    if (usernameMaskNum == 97 && !haikuUsernameReal && isBlacboardOwned == 0)
                    {
                        if (st.EMAIL != null)
                        {
                            tempUsername = st.EMAIL;
                            ui.username = st.EMAIL;
                            st.USERNAME = st.EMAIL;
                        }

                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(ui.email) && (canvas.Configuration.Instance.ExportUserAfterRegistration || canvas.Configuration.Instance.UserSynchronizationInDashboard))
                        {
                            st.USERNAME = ui.email;
                            ui.username = st.USERNAME;
                        }
                        else
                        {
                            Random random = new Random();
                            int randomNumber = random.Next(0, 1000);
                            st.USERNAME = "temp" + randomNumber + usernameMaskNum + ui.email;
                            ui.username = st.USERNAME;
                        }
                    }
                }
                if (ui.userid == 0)
                {
                    st.DISTEMPLOYEE = Data.School.Student.StudentHelper.NewMemberDistemployeeValue;
                    if (st.DISTRICT != 0 && st.DISTRICT != null)
                    {
                        var districtmembershipflag = (from s in Context.Districts where s.DISTID == st.DISTRICT select s).FirstOrDefault();
                        if (districtmembershipflag != null)
                        {
                            if (districtmembershipflag.MembershipFlag != null)
                            {
                                st.DISTEMPLOYEE = short.Parse(districtmembershipflag.MembershipFlag.Value.ToString());
                            }
                        }
                    }
                    var studentcheckdups = (from s in Context.Students where s.USERNAME == ui.username select s).FirstOrDefault();
                    if (WebConfiguration.AllowCreateNewAccountOverInactive == "true")
                    {
                        studentcheckdups = (from s in Context.Students where s.USERNAME == ui.username && s.InActive == 0 select s).FirstOrDefault();
                    }
                    var supervisorstudentcheckdups = (from s in Context.Supervisors where s.UserName == ui.username select s).FirstOrDefault();
                    if ((studentcheckdups != null) || (supervisorstudentcheckdups != null))
                    {
                        if (studentcheckdups != null)
                        {
                            throw new NotImplementedException("Username already exist: " + studentcheckdups.USERNAME, new Exception("Username already exist: " + studentcheckdups.USERNAME));
                        }
                        else
                        {
                            throw new NotImplementedException("Username already exist: " + supervisorstudentcheckdups.UserName, new Exception("Username already exist: " + supervisorstudentcheckdups.UserName));
                        }
                    }
                    else
                    {
                        st.USERNAME = ui.username;
                        Context.Students.Add(st);
                        if (AuthorizationHelper.CurrentSupervisorUser != null)
                        {
                            ui.supervisor = AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID.ToString() + ",";
                        }
                        else
                        {
                            st.UserSessionId = Guid.NewGuid();
                        }
                    }
                }

                Data.School.Student.MembershipHelper.CalculateMembership(Context, st);
                if (Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.GetConfig().useClubPilates == 1)
                {
                    if (st.clubready_student_id == null)
                    {
                        Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper ClubPilatesHelper = new Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper();
                        ClubPilatesHelper.InsertUserToClubPilates(st);
                    }

                }
                AuthorizationHelper.UpdateCurrentUser(st);

                Context.SaveChanges();
                // return student id

                if (ui.userid == 0 && haiku.Configuration.Instance.ExportUserToHaikuAfterRegistration && haiku.Configuration.Instance.HaikuUserSynchronizationEnabled)
                {
                    var response = haiku.HaikuExport.SynchronizeStudent(st, Context);
                }

                if ((ui.userid == 0 && canvas.Configuration.Instance.ExportUserAfterRegistration || canvas.Configuration.Instance.UserSynchronizationInDashboard) && st.USERNAME != null && st.EMAIL != "" && st.FIRST != null && st.LAST != null)
                {
                    if (WebConfiguration.CanvasSkipSyncUserAccount == "false")
                    {
                        var response = canvas.CanvasExport.SynchronizeStudent(st, Context);
                    }
                }
                Context.SaveChanges();

                ui.userid = st.STUDENTID;
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent = ui.userid;
                }
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    //deal with update student method only
                    var SupStudRelExist = (from gm in Context.SupervisorStudents
                                           where (gm.studentid == st.STUDENTID) && (gm.SupervisorID == AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID)
                                           select gm).ToList();
                    if (SupStudRelExist.Count > 0)
                    {
                        try
                        {
                            var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
                            if (TurnOnDebugTracingMode != null)
                            {
                                if (TurnOnDebugTracingMode.ToLower() == "on")
                                {
                                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                                    Audittrail.TableName = "User";
                                    Audittrail.AuditDate = DateTime.Now;
                                    Audittrail.RoutineName = "User/Sup - Relation Export" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                                    try
                                    {
                                        Audittrail.AuditAction = "Info Detail: SupID: " + AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID + "StudID:" + int.Parse(st.STUDENTID.ToString()) + " StudCanvasID: " + int.Parse(st.canvas_user_id.ToString());
                                    }
                                    catch { }
                                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                                    LogManager.LogSiteActivity(Audittrail);
                                }
                            }
                            canvas.CanvasExport.ExportSupervisorStudentRelation2Canvas(null, AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID, int.Parse(st.STUDENTID.ToString()), int.Parse(st.canvas_user_id.ToString()));
                        }
                        catch { }
                    }

                }
                if ((ui.supervisor != null) && (ui.supervisor != ""))
                {

                    SupervisorStudent supervisorentity = new SupervisorStudent();

                    var removeFromGroup = (from gm in Context.SupervisorStudents
                                           where gm.studentid == st.STUDENTID
                                           select gm).ToList();
                    foreach (var item in removeFromGroup)
                    {
                        Context.SupervisorStudents.Remove(item);
                        Context.SaveChanges();
                    }
                    try
                    {
                        if (ui.supervisor.Contains(','))
                        {

                            foreach (var supervisorid in ui.supervisor.Split(','))
                            {
                                if (supervisorid != "")
                                {
                                    supervisorentity = new SupervisorStudent();
                                    supervisorentity.studentid = st.STUDENTID;
                                    supervisorentity.SupervisorID = int.Parse(supervisorid);

                                    Context.SupervisorStudents.Add(supervisorentity);
                                    Context.SaveChanges();
                                    canvas.CanvasExport.ExportSupervisorStudentRelation2Canvas(null, int.Parse(supervisorid), int.Parse(st.STUDENTID.ToString()), int.Parse(st.canvas_user_id.ToString()));
                                }
                            }
                        }
                        else
                        {
                            if (ui.supervisor != "")
                            {
                                int supervisorid = int.Parse(ui.supervisor);
                                supervisorentity = new SupervisorStudent();
                                supervisorentity.studentid = st.STUDENTID;
                                supervisorentity.SupervisorID = supervisorid;

                                Context.SupervisorStudents.Add(supervisorentity);
                                Context.SaveChanges();
                                canvas.CanvasExport.ExportSupervisorStudentRelation2Canvas(null, supervisorid, int.Parse(st.STUDENTID.ToString()), int.Parse(st.canvas_user_id.ToString()));

                            }
                        }
                    }
                    catch (Exception e) { }
                }
            }
            //update instructor and supervisor if common login is on, populate password, basic data (email, address, phone....).
            if (Settings.Instance.GetMasterInfo2().CommonPublicLogin == 1)
            {
                //cross update password
                if (Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1 || Settings.Instance.GetMasterInfo3().allowCrossUserUpdate == 1)
                {
                    //find sup
                    var ContextSP = new SchoolEntities();
                    Entities.Supervisor sp = new Entities.Supervisor();
                    // if (ui.username.Length > 0) { sp = ContextSP.Supervisors.First(sv => sv.UserName == ui.username); }
                    if (tempUsername == null) { tempUsername = st.USERNAME; }
                    if (tempUsername.Length > 0) { sp = (from sv in ContextSP.Supervisors where sv.UserName == tempUsername select sv).FirstOrDefault(); }
                    //find inst
                    var ContextInst = new SchoolEntities();
                    Entities.Instructor inst = new Entities.Instructor();
                    // if (ui.username.Length > 0) { inst = ContextInst.Instructors.First(si => si.USERNAME == ui.username); }                    
                    if (tempUsername.Length > 0) { inst = (from si in ContextInst.Instructors where si.USERNAME == tempUsername select si).FirstOrDefault(); }

                    if (Settings.Instance.GetMasterInfo3().AutoPopulatePassword4CommonLogin == 1)
                    {
                        if (ui.password != null)
                        {
                            if (sp != null) { sp.PASSWORD = ui.password; }
                            if (inst != null) { inst.PASSWORD = ui.password; }
                        }
                    }
                    // update basic info
                    if (Settings.Instance.GetMasterInfo3().allowCrossUserUpdate == 1)
                    {
                        if (sp != null)
                        {
                            if (ui.first != null) { sp.FIRST = ui.first; }
                            if (ui.last != null) { sp.LAST = ui.last; }
                            if (ui.email != null) { sp.EMAIL = ui.email; }
                            if (ui.homephone != null) { sp.PHONE = ui.homephone; }
                            if (ui.workphone != null) { sp.FAX = ui.workphone; }
                            if (ui.address != null) { sp.ADDRESS = ui.address; }
                            if (ui.city != null) { sp.CITY = ui.city; }
                            if (ui.state != null) { sp.STATE = ui.state; }
                            if (ui.zip != null) { sp.ZIP = ui.zip; }
                        }
                        if (inst != null)
                        {
                            if (ui.first != null) { inst.FIRST = ui.first; }
                            if (ui.last != null) { inst.LAST = ui.last; }
                            if (ui.email != null) { inst.EMAIL = ui.email; }
                            if (ui.homephone != null) { inst.HOMEPHONE = ui.homephone; }
                            if (ui.workphone != null) { inst.WORKPHONE = ui.workphone; }
                            if (ui.address != null) { inst.ADDRESS = ui.address; }
                            if (ui.city != null) { inst.CITY = ui.city; }
                            if (ui.state != null) { inst.STATE = ui.state; }
                            if (ui.zip != null) { inst.ZIP = ui.zip; }
                        }
                    }


                    // may need to check for record availability before save 
                    if (sp != null)
                    {
                        ContextSP.SaveChanges();
                    }
                    if (inst != null)
                    {
                        ContextInst.SaveChanges();
                    }
                }
            }
            return ui;
        }

        public UserInfo NewOrUpdateSP(UserInfo ui)
        {
            var Context = new SchoolEntities();
            Entities.Supervisor sp = new Entities.Supervisor();
            if (ui.userid != 0) { sp = Context.Supervisors.First(s => s.SUPERVISORID == ui.userid); }

            DataLists dlists = new DataLists();
            var fm = new FieldMask();
            try { fm = dlists.FieldMasks.Where(f => f.FieldName == "UserName" && f.TableName == "Parents").FirstOrDefault(); }
            catch { }
            if (fm != null)
            {
                if (fm.DefaultMaskNumber == 97)
                {
                    if (ui.email != null) { sp.UserName = ui.email; ui.username = ui.email; }
                }
                else
                {
                    if (ui.username != null) { sp.UserName = ui.username; }
                }
            }
            else
            {
                if (ui.username != null) { sp.UserName = ui.username; }
            }
            if (ui.userid == 0)
            {
                if (ui.username == "" || ui.username == null)
                {
                    throw new Exception("Invalid Username");
                }
                int fountExisting = (from se in Context.Supervisors where se.UserName.Contains(ui.username) select se).Count();
                if (fountExisting >= 1)
                {
                    throw new Exception("Username already exist");
                }
            }
            if (ui.ParentsFirstName != null) { sp.FIRST = ui.ParentsFirstName; }
            if (ui.ParentsLastName != null) { sp.LAST = ui.ParentsLastName; }
            if (ui.password != null) { sp.PASSWORD = ui.password; }
            if (ui.email != null) { sp.EMAIL = ui.email; }
            if (ui.schoolid != null) { sp.SCHOOL = int.Parse(ui.schoolid.ToString()); }
            if (ui.districtid != null) { sp.DISTRICT = ui.districtid; }
            if (ui.school != null) { sp.SCHOOL = int.Parse(ui.school.ToString()); }
            if (ui.district != null) { sp.DISTRICT = ui.district; }
            if (ui.gradeid != null) { sp.GRADE = ui.gradeid; }
            //if (ui.workphone != null) { sp.PHONE = ui.workphone; }
            if (ui.homephone != null) { sp.PHONE = ui.homephone; }
            if (ui.fax != null) { sp.FAX = ui.fax; }
            if (ui.address != null) { sp.ADDRESS = ui.address; }
            if (ui.city != null) { sp.CITY = ui.city; }
            if (ui.state != null) { sp.STATE = ui.state; }
            if (ui.address != null) { sp.ZIP = ui.zip; }
            if (ui.supervisornum != null) { sp.SUPERVISORNUM = ui.supervisornum; }
            if (ui.title != null) { sp.TITLE = ui.title; }
            if (ui.notify != null) { sp.NOTIFY = ui.notify; }

            if (ui.advanceoptionsstr != null) { sp.AdvanceOptions = (ui.advanceoptionsstr == "on" || ui.advanceoptionsstr == "1" || ui.advanceoptionsstr == "-1" || ui.advanceoptionsstr == "true" ? -1 : 0); }
            if (ui.additionalemailaddresses != null) { sp.AdditionalEmailAddresses = ui.additionalemailaddresses; }
            sp.DateAdded = System.DateTime.Now;
            sp.ACTIVE = 1;

            if (ui.userid == 0) { Context.Supervisors.Add(sp); }
            Context.SaveChanges();

            if ((ui.userid == 0 && canvas.Configuration.Instance.ExportUserAfterRegistration || canvas.Configuration.Instance.UserSynchronizationInDashboard) && canvas.Configuration.Instance.allowSupervisorIntegration == true && sp.UserName != null && sp.EMAIL != "" && sp.FIRST != null && sp.LAST != null)
            {
                var response = canvas.CanvasExport.SynchronizeSupervisor(sp, Context);
            }

            ui.userid = sp.SUPERVISORID;
            return ui;
        }

        public UserInfo NewOrUpdateIT(UserInfo ui)
        {
            var Context = new SchoolEntities();
            Entities.Instructor it = new Entities.Instructor();
            if (ui.userid != 0) { it = Context.Instructors.First(s => s.INSTRUCTORID == ui.userid); }

            if (ui.username != null) { it.USERNAME = ui.username; }
            if (ui.first != null) { it.FIRST = ui.first; }
            if (ui.last != null) { it.LAST = ui.last; }
            if (ui.password != null) { it.PASSWORD = ui.password; }
            if (ui.email != null) { it.EMAIL = ui.email; }
            if (ui.school != null) { it.SCHOOL = ui.school; }
            if (ui.district != null) { it.DISTRICT = ui.district; }
            if (ui.grade != null) { it.GRADELEVEL = ui.grade.ToString(); }
            if (ui.homephone != null) { it.HOMEPHONE = ui.homephone; }
            if (ui.workphone != null) { it.WORKPHONE = ui.workphone; }
            if (ui.fax != null) { it.FAX = ui.fax; }
            if (ui.address != null) { it.ADDRESS = ui.address; }

            if (ui.InstructorRegField1 != null) { it.InstructorRegField1 = ui.InstructorRegField1; }
            if (ui.InstructorRegField2 != null) { it.InstructorRegField2 = ui.InstructorRegField2; }
            if (ui.InstructorRegField3 != null) { it.InstructorRegField3 = ui.InstructorRegField3; }
            if (ui.InstructorRegField4 != null) { it.InstructorRegField4 = ui.InstructorRegField4; }
            if (ui.InstructorRegField5 != null) { it.InstructorRegField5 = ui.InstructorRegField5; }
            if (ui.InstructorRegField6 != null) { it.InstructorRegField6 = ui.InstructorRegField6; }
            if (ui.InstructorRegField7 != null) { it.InstructorRegField7 = ui.InstructorRegField7; }
            if (ui.InstructorRegField8 != null) { it.InstructorRegField8 = ui.InstructorRegField8; }
            if (ui.InstructorRegField9 != null) { it.InstructorRegField9 = ui.InstructorRegField9; }
            if (ui.InstructorRegField10 != null) { it.InstructorRegField10 = ui.InstructorRegField10; }


            if (ui.ProfileImage != null && ui.ProfileImage != it.PhotoImage)
            {
                string OldProfileImage = it.PhotoImage;
                it.PhotoImage = ui.ProfileImage;
                //it.TempProfileImage = "";
                if (System.IO.File.Exists(HttpContext.Current.Request.MapPath(UploadImage.ItemUploadFolderPath + OldProfileImage)))
                {
                    System.IO.File.Delete(HttpContext.Current.Request.MapPath(UploadImage.ItemUploadFolderPath + OldProfileImage));
                }

            }



            if (Configuration.Instance.BlackboardUseAPI && Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardMembershipIntegrationEnabled)
            {
                BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                var user = handelr.GetUserDetails(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, it.USERNAME, "", "", jsonToken);

                if (user.userName != null)
                {
                    BBUser user_update = new BBUser();
                    user_update.dataSourceId = Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk;
                    user_update.password =it.PASSWORD;

                    user_update.contact = new ProfileContactObj();
                    user_update.contact.email = ui.email;
                    user_update.name = new ProfileNameObj();
                    user_update.name.given = it.FIRST;
                    user_update.name.family = it.LAST;

                    BBRespUserProfile updateduser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, user.userName, "", "", jsonToken, "");


                }
                else
                {
                    BBUser user_update = new BBUser();

                    if (Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk != null)
                    {
                        string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk;
                        if (!string.IsNullOrEmpty(tempDSK))
                        {
                            if (tempDSK.IndexOf("_") < 0)
                            {
                                var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, Gsmu.Api.Integration.Blackboard.Configuration.Instance.InstructorsDsk, "dsk", "", jsonToken);
                                datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                string actualDSK = globaldatasource.id;

                                user_update.dataSourceId = actualDSK;
                            }
                            else
                            {
                                user_update.dataSourceId = tempDSK;
                            }
                        }
                    }
                    
                    user_update.userName = ui.username;
                    user_update.password = ui.password;

                    user_update.contact = new ProfileContactObj();
                    user_update.contact.email = ui.email;
                    user_update.name = new ProfileNameObj();
                    user_update.name.given = ui.first;
                    user_update.name.family = ui.last;

                    string[] bbSystemRole = new string[1];
                    string[] bbInstitutionRole = new string[1];
                    //bbSystemRole[0] = Configuration.Instance.BlackboardSystemRole;
                    bbInstitutionRole[0] = Configuration.Instance.BlackboardInstructorRole;

                   // user_update.systemRoleIds = bbSystemRole;
                    user_update.institutionRoleIds = bbInstitutionRole;
                    BBRespUserProfile updateduser = handelr.CreateNewUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, "", jsonToken, "", Configuration.Instance.StudentInstitutionalHierarchyNodeId);
                    it.Blackboard_user_UUID = updateduser.uuid;
                }
            }




            if (ui.userid == 0) { Context.Instructors.Add(it); }
            Context.SaveChanges();

            if (ui.userid == 0 && haiku.Configuration.Instance.ExportUserToHaikuAfterRegistration && haiku.Configuration.Instance.HaikuUserSynchronizationEnabled)
            {
                var response = haiku.HaikuExport.SynchronizeInstructor(it, Context);
            }
            if (WebConfiguration.CanvasforceSyncInstructor == "false" && canvas.Configuration.Instance.UserSynchronizationInDashboard && it.USERNAME != null && it.EMAIL != "" && it.FIRST != null && it.LAST != null)
            {
                canvas.CanvasExport.SynchronizeInstructor(it, Context);
            }
            ui.userid = it.INSTRUCTORID;
            return ui;
        }

        public UserInfo NewOrUpdateSA(UserInfo ui)
        {
            var Context = new SchoolEntities();
            Entities.Manager sa = new Entities.Manager();
            if (ui.userid != 0) { sa = Context.Managers.First(s => s.AUTOINDEX == ui.userid); }

            //if (ui.username != null)    { sa.LAST = ui.username; }
            if (ui.first != null) { sa.FIRST = ui.first; }
            if (ui.last != null) { sa.LAST = ui.last; }
            if (ui.password != null) { sa.PASSWORD = ui.password; }
            if (ui.email != null) { sa.EMAIL = ui.email; }

            if (ui.userid == 0) { Context.Managers.Add(sa); }
            Context.SaveChanges();
            ui.userid = sa.AUTOINDEX;
            return ui;
        }

        public UserInfo NewOrUpdateAD(UserInfo ui)
        {
            var Context = new SchoolEntities();
            Entities.adminpass ad = new Entities.adminpass();
            if (ui.userid != 0) { ad = Context.adminpasses.First(s => s.AdminID == ui.userid); }

            if (ui.username != null) { ad.username = ui.username; }
            if (ui.password != null) { ad.userpass = ui.password; }
            if (ui.email != null) { ad.email = ui.email; }

            if (ui.userid == 0) { Context.adminpasses.Add(ad); }
            Context.SaveChanges();
            ui.userid = ad.AdminID;
            return ui;
        }


        public List<UserCertificatesCompleteds> GetCertStudent(int studentid)
        {

            string query = "select Max(distinct t.CourseId) as FirstCourse , min(CT.CourseDate) as StartCourseDate,";
            query += " Count(distinct t.CourseId) as CompletedClasses, C1.CertificationsId, C1.CertificationsTitle,";
            query += " S.STUDENTID, S.Last, S.First, S.email, T.CertificateIssueDate,";
            query += " isnull(CC2.CustomCertId, 0) as CertificationsCustomCertId, isnull(CC2.certtitle, '') as certtitle";
            query += " from((((Certifications C1";
            query += " inner join CertificationsStudent CS on CS.CertificationsId = C1.CertificationsId";
            query += " inner join Students S on S.STUDENTID = CS.StudentId)";
            query += " inner join(select distinct courseid, Cc.CertificationsId from Courses C2";
            query += " inner join CertificationsCourse CC on CC.Coursenum = C2.Coursenum or CC.Coursenum = C2.CustomCourseField5";
            query += " union select distinct courseid, CourseCertificationsId from Courses c3) as C on C.CertificationsId = C1.CertificationsId)";
            query += " inner join[course times] CT on CT.courseId = C.CourseId)";
            query += " inner join transcripts T on T.CourseId = C.courseid and T.studentId = S.studentId)";
            query += " left join customcetificate CC2 on CC2.CustomCertId = C1.CertificationsCustomCertId";
            query += " where T.attended <> 0 and S.STUDENTID = " + studentid;
            query += " group by S.studentid, C1.CertificationsId, C1.CertificationsTitle,S.Last, S.First, S.email , CC2.CustomCertId, CC2.certtitle,T.CertificateIssueDate";
            query += " having Count(distinct T.CourseId) >= max(C1.CertificationsHowManyCoursesRequired)";
            query += " order by 2,s.last,s.first";

            var certcomps = new List<UserCertificatesCompleteds>();

            try
            {
                var mainConnection = Connections.GetSchoolConnection();
                mainConnection.Open();

                var mainCommand = mainConnection.CreateCommand();
                mainCommand.CommandText = query;

                using (var Reader = mainCommand.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        int intCourseId = 0;
                        int.TryParse(Reader.GetValue(0).ToString(), out intCourseId);
                        var certcomp = new UserCertificatesCompleteds()
                        {
                            CourseId = intCourseId,
                            CourseNum = "",
                            CourseName = Reader.GetValue(4).ToString(),
                            StartDate = DateTime.Parse(Reader.GetValue(1).ToString()),
                            CompletionDate = DateTime.Parse(Reader.GetValue(9).ToString()),
                            CertType = "CourseCert",
                            CertNum = int.Parse(Reader.GetValue(3).ToString()),
                        };
                        certcomps.Add(certcomp);
                    }
                }
                mainConnection.Close();
            }
            catch
            {
            }

            return certcomps;
        }


        public string GetFieldStrValue(int userid, string fieldname = null, string tablename = "Students")
        {
            string query = "SELECT Top 1 [" + fieldname + "] FROM [" + tablename + "] WHERE STUDENTID=" + userid;
            if (tablename == "Instructors")
            {
                query = "SELECT Top 1 [" + fieldname + "] FROM [" + tablename + "] WHERE INSTRUCTORID=" + userid;
            }

            if (fieldname == "supervisor" && tablename == "Students")
            {
                query = "SELECT  [" + "SupervisorID" + "] FROM [" + "SupervisorStudents" + "] WHERE STUDENTID=" + userid;
                if ((AuthorizationHelper.CurrentSupervisorUser != null) && (userid == 0))
                {
                    return AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID.ToString();
                }
            }
            if (tablename == "Supervisors")
            {
                if (fieldname == "advanceoptionsstr") { fieldname = "advanceoptions"; };
                query = "SELECT Top 1 [" + fieldname + "] FROM [" + tablename + "] WHERE SUPERVISORID=" + userid;
            }

            try
            {
                var mainConnection = Connections.GetSchoolConnection();
                mainConnection.Open();

                string dtring = "";

                var mainCommand = mainConnection.CreateCommand();
                mainCommand.CommandText = query;

                using (var Reader = mainCommand.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        if (fieldname == "supervisor")
                        {
                            dtring = dtring + "," + Reader.GetValue(0).ToString();
                        }
                        else
                        {
                            dtring = Reader.GetValue(0).ToString();
                        }

                        if (tablename == "Students" && fieldname.ToLower() == "ss")
                        {
                            if (String.IsNullOrEmpty(Reader.GetValue(0).ToString()))
                            {
                                dtring = "";
                            }
                            else
                            {
                                dtring = "************";
                            }

                        }
                    }
                }
                mainConnection.Close();
                return dtring;
            }
            catch
            {
                return "Invalid SQL: " + query;
            }
        }



        public UserWidget SumbitUserWidget(string adminmode, UserWidget ui)
        {

            string strobj = Json.Encode(ui);

            using (var db = new SchoolEntities())
            {

                foreach (var mi4 in db.masterinfo4)
                {

                    if (adminmode == "StudentsDashViewEdit")
                    {
                        mi4.StudentsDashViewEdit = strobj;
                    }
                    else if (adminmode == "StudentsDashAddnew")
                    {
                        mi4.StudentsDashAddnew = strobj;
                    }
                    else if (adminmode == "StudentsDashAdmin")
                    {
                        mi4.StudentsDashAdmin = strobj;

                    }
                    else if (adminmode == "InstructorsDashViewEdit")
                    {
                        mi4.InstructorsDashViewEdit = strobj;
                    }
                    else if (adminmode == "InstructorsDashAddnew")
                    {
                        mi4.InstructorsDashAddnew = strobj;
                    }
                    else if (adminmode == "InstructorsDashAdmin")
                    {
                        mi4.InstructorsDashAdmin = strobj;
                    }


                }
                db.SaveChanges();
            }
            Settings.Instance.Refresh();

            return ui;
        }


        public bool SumbitUserWidgetProp(string adminmode, List<UserRegFieldSpecs> list)
        {
            using (var db = new SchoolEntities())
            {

                foreach (var item in list)
                {

                    var FieldName = item.FieldName;
                    var FieldLabel = item.FieldLabel;
                    var MaskTxt = item.MaskTxt;
                    var TblFieldName = item.TblFieldName;
                    var BoolFieldRequired = item.BoolFieldRequired;
                    var FieldReadOnly = item.FieldReadOnly;
                    var FieldReadOnlyVal = (FieldReadOnly == true ? 1 : 0);
                    var FieldRequired = (BoolFieldRequired == true ? 1 : 0);
                    var FieldRequiredMasterInfo = (BoolFieldRequired == true ? -1 : 0);
                    var ConfirmRequiredVal = (item.ConfirmRequired == true ? 1 : 0);
                    int MaskNum = 0;
                    int FieldListType = 0;
                    if (item.BoolFieldRequiredAll == true)
                    {
                        FieldRequired = 2;
                        FieldRequiredMasterInfo = 2;
                    }

                    if (adminmode == "StudentsDashAddnew" || adminmode == "StudentsDashViewEdit" || adminmode == "StudentsDashAdmin")
                    {

                        //default, try to save all prop to FieldSpecs and FieldMask

                        if (MaskTxt == "2LetterStateAbbrev") { MaskNum = 1; }
                        else if (MaskTxt == "(###) ###-####") { MaskNum = 1; }
                        else if (MaskTxt == "###-##-####") { MaskNum = 1; }
                        else if (MaskTxt == "####") { MaskNum = 2; }
                        else if (MaskTxt == "emailusername") { MaskNum = 97; }

                        else if (MaskTxt == "YYYY/MM/DD") { MaskNum = 1; }
                        else if (MaskTxt == "MM/DD/YYYY") { MaskNum = 2; }
                        else if (MaskTxt == "Gender") { MaskNum = 20; }
                        else if (MaskTxt == "Ethnicity 1") { MaskNum = 21; }
                        else if (MaskTxt == "Department") { MaskNum = 22; }
                        else if (MaskTxt == "Yes/No") { MaskNum = 23; }
                        else if (MaskTxt == "Ethnicity 2") { MaskNum = 24; }
                        else if (MaskTxt == "Race") { MaskNum = 25; }
                        else if (MaskTxt == "SelectionCheckbox") { MaskNum = 26; FieldListType = 0; }
                        else if (MaskTxt == "SelectionListSingleSelect") { MaskNum = 26; FieldListType = 1; }
                        else if (MaskTxt == "SelectionListMultiSelect") { MaskNum = 26; FieldListType = 2; }

                        try
                        {
                            FieldSpec fs = db.FieldSpecs.FirstOrDefault(f => f.FieldName == FieldName && f.TableName == "Students");
                            if (fs == null)
                            {
                                FieldSpec newfs = new FieldSpec();
                                newfs.FieldName = FieldName;
                                newfs.FieldLabel = FieldLabel;
                                newfs.FieldReadOnly = FieldReadOnlyVal;
                                newfs.FieldRequired = FieldRequired;
                                newfs.ConfirmRequired = ConfirmRequiredVal;
                                newfs.FieldListType = FieldListType;
                                newfs.TableName = "Students";
                                db.FieldSpecs.Add(newfs);
                            }
                            else
                            {
                                fs.FieldLabel = FieldLabel;
                                fs.FieldReadOnly = FieldReadOnlyVal;
                                fs.FieldRequired = FieldRequired;
                                fs.ConfirmRequired = ConfirmRequiredVal;
                                fs.FieldListType = FieldListType;
                            }
                            db.SaveChanges();
                        }
                        catch
                        {

                        }

                        try
                        {
                            FieldMask fm = db.FieldMasks.First(f => f.FieldName == FieldName && f.TableName == "Students");
                            fm.DefaultMaskNumber = MaskNum;
                            db.SaveChanges();
                        }
                        catch { }


                        //Save to MasterInfo label and required

                        if (item.FieldGrp == "presetfield" || item.FieldGrp == "affiliaton")
                        {
                            try
                            {
                                string query = "UPDATE MasterInfo SET [ReqStudent" + FieldName + "] = " + FieldRequiredMasterInfo;
                                db.Database.ExecuteSqlCommand(query);
                            }
                            catch { }
                        }

                        if (item.FieldGrp == "customfield")
                        {
                            try
                            {
                                string query = "UPDATE MasterInfo SET [" + FieldName + "name] = '" + FieldLabel + "', [" + FieldName + "Required] = " + FieldRequired;
                                db.Database.ExecuteSqlCommand(query);
                            }
                            catch { }
                        }

                        if (item.FieldGrp == "affiliaton")
                        {
                            foreach (var mi in db.MasterInfoes)
                            {
                                if (FieldName == "district") { mi.Field3Name = FieldLabel; }
                                else if (FieldName == "school") { mi.Field2Name = FieldLabel; }
                                else if (FieldName == "grade") { mi.Field1Name = FieldLabel; }
                            }
                            db.SaveChanges();
                        }

                        //special cases
                        if (FieldName == "address")
                        {
                            foreach (var mi in db.MasterInfo2)
                            {
                                if (FieldName == "address") { mi.PublicAddressLabel = FieldLabel; }
                            }
                            db.SaveChanges();
                        }

                    }
                }
            }
            Settings.Instance.Refresh();
            return true;
        }




        public string InitializeUserWidgetSettings(string resetcmd = "NONE")
        {
            string resetedflds = "";
            int lenStudentsDashAddnew = Settings.Instance.GetMasterInfo4().StudentsDashAddnew.Length;
            int lenStudentsDashViewEdit = Settings.Instance.GetMasterInfo4().StudentsDashViewEdit.Length;
            int lenStudentsDashAdmin = Settings.Instance.GetMasterInfo4().StudentsDashAdmin.Length;

            int lenInstructorsDashAddnew = Settings.Instance.GetMasterInfo4().InstructorsDashAddnew.Length;
            int lenInstructorsDashViewEdit = Settings.Instance.GetMasterInfo4().InstructorsDashViewEdit.Length;
            int lenInstructorsDashAdmin = Settings.Instance.GetMasterInfo4().InstructorsDashAdmin.Length;


            for (int istud = 1; istud <= 6; istud++)
            {
                int mincharrest = 30;
                int col1flex = 1; int col1width = 50;
                int col2flex = 1; int col2width = 50;
                int col3flex = 0; int col3width = 0;

                bool runreset = false;
                switch (istud)
                {
                    case 1:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenStudentsDashAddnew <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "StudentsDashAddnew" ? true : false);
                        }
                        break;
                    case 2:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenStudentsDashViewEdit <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "StudentsDashViewEdit" ? true : false);
                        }
                        break;
                    case 3:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenStudentsDashAdmin <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "StudentsDashAdmin" ? true : false);
                        }

                        col1flex = 2; col1width = 33;
                        col2flex = 3; col2width = 50;
                        col3flex = 1; col3width = 16;
                        break;

                    case 4:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenInstructorsDashAddnew <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "InstructorsDashAddnew" ? true : false);
                        }
                        break;
                    case 5:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenInstructorsDashViewEdit <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "InstructorsDashViewEdit" ? true : false);
                        }
                        break;
                    case 6:
                        if (resetcmd == "NONE")
                        {
                            runreset = (lenInstructorsDashAdmin <= mincharrest ? true : false);
                        }
                        else
                        {
                            runreset = (resetcmd == "InstructorsDashAdmin" ? true : false);
                        }

                        col1flex = 2; col1width = 33;
                        col2flex = 3; col2width = 50;
                        col3flex = 1; col3width = 16;
                        break;
                }

                if (resetcmd == "resetall")
                {
                    runreset = true;
                }


                if (runreset)
                {
                    UserWidget uw = new UserWidget();
                    var widgetitemlist = new List<WidgetItemList>();
                    var widgetinfo = new List<WidgetInfo>();
                    var widgetcolumn = new List<WidgetColumn>();

                    var idf = 0;
                    DataLists dlists = new DataLists();

                    //COLUMNS    ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //addnew: IDENTITY,AFFILIATION column
                    //editview: IDENTITY, DEMOGRAPHIC, AFFILIATION, CERTIFICATE& CERTIFICATION and SURVEYS column
                    if (col1flex > 0)
                    {
                        widgetcolumn.Add(new WidgetColumn() { ID = 1, ColFlex = col1flex, WidthPer = col1width, DispSort = 1 });
                    }
                    //addnew: DEMOGRAPHIC column
                    //editview: COURSES, RECIEVED EMAILS column
                    if (col2flex > 0)
                    {
                        widgetcolumn.Add(new WidgetColumn() { ID = 2, ColFlex = col2flex, WidthPer = col2width, DispSort = 2 });
                    }
                    //admin: ACTION / REPORT column
                    if (col3flex > 0)
                    {
                        widgetcolumn.Add(new WidgetColumn() { ID = 3, ColFlex = col3flex, WidthPer = col3width, DispSort = 3 });
                    }


                    //WIDGETS      ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    if (istud == 3 || istud == 6)
                    {
                        //student admin view
                        if (col1flex > 0)
                        {
                            //IDENTITY widget
                            widgetinfo.Add(new WidgetInfo() { ID = 1, ColID = 1, DispSort = 1, Title = "Identity", WithProfileImage = true });
                            //DEMOGRAPHIC widget
                            widgetinfo.Add(new WidgetInfo() { ID = 3, ColID = 1, DispSort = 2, Title = "Demographic" });
                            //AFFILIATION widget
                            widgetinfo.Add(new WidgetInfo() { ID = 2, ColID = 1, DispSort = 3, Title = "Affiliation" });
                            //Role PRESET widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 4,
                                ColID = 1,
                                DispSort = 4,
                                Title = "Role",
                                WidgetType = "preset",
                                Name = "role",
                                Url = "UserRoles?cmd=addnew",
                                PanelID = "UserDashboardRoles"
                            });
                        }
                        if (col2flex > 0)
                        {
                            //courses PRESET widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 5,
                                ColID = 2,
                                DispSort = 1,
                                Title = "Courses",
                                WidgetType = "preset",
                                Name = "courses",
                                Url = "UserCourses?cmd=addnew",
                                PanelID = "UserDashboardCourses"
                            });

                            //Recieved Email PRESET widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 6,
                                ColID = 2,
                                DispSort = 2,
                                Title = "Received Emails",
                                WidgetType = "preset",
                                Name = "recievdemails",
                                Url = "UserEmails?cmd=addnew",
                                PanelID = "UserDashboardReceivedEmail"
                            });
                            if (Settings.Instance.GetMasterInfo3().UsePurchaseCredit == 1)
                            {
                                widgetinfo.Add(new WidgetInfo()
                                {
                                    ID = 9,
                                    ColID = 2,
                                    DispSort = 3,
                                    Title = Settings.Instance.GetMasterInfo2().CreditHoursName + " Transactions",
                                    WidgetType = "preset",
                                    Name = "coursehours",
                                    Url = "UserReportsCourseTransactions?cmd=addnew",
                                    PanelID = "UserDashCourseHoursTransactions"
                                });
                            }
                            if (Settings.Instance.GetMasterInfo2().AllowStudentMultiEnroll == 1)
                            {
                                widgetinfo.Add(new WidgetInfo()
                                {
                                    ID = 10,
                                    ColID = 2,
                                    DispSort = 3,
                                    Title = "User Enrolled Courses for Other Students",
                                    WidgetType = "preset",
                                    Name = "otherusercourses",
                                    Url = "UserEnrolledOtherStudents?cmd=addnew",
                                    PanelID = "OtherUserDashCourse"
                                });
                            }
                        }
                        if (col3flex > 0)
                        {
                            //IDENTITY widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 7,
                                ColID = 3,
                                DispSort = 1,
                                Title = "Actions",
                                WidgetType = "preset",
                                Name = "actions",
                                Url = "UserActions?cmd=addnew",
                                PanelID = "UserDashboardActions"
                            });

                            //AFFILIATION widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 8,
                                ColID = 3,
                                DispSort = 2,
                                Title = "Reports",
                                WidgetType = "preset",
                                Name = "reports",
                                Url = "UserReports?cmd=addnew",
                                PanelID = "UserDashboardReports"
                            });
                        }

                    }
                    else if (istud == 2 || istud == 5)
                    {
                        //student inst edit and view
                        if (col1flex > 0)
                        {
                            //IDENTITY widget
                            widgetinfo.Add(new WidgetInfo() { ID = 1, ColID = 1, DispSort = 1, Title = "Identity", WithProfileImage = true });

                            //DEMOGRAPHIC widget
                            widgetinfo.Add(new WidgetInfo() { ID = 3, ColID = 1, DispSort = 2, Title = "Demographic" });

                            //AFFILIATION widget
                            widgetinfo.Add(new WidgetInfo() { ID = 2, ColID = 1, DispSort = 3, Title = "Affiliation" });

                            //CERTIFICATE& CERTIFICATION preset widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 4,
                                ColID = 1,
                                DispSort = 4,
                                Title = "Certificate and Certification",
                                WidgetType = "preset",
                                Name = "certandcertifcation",
                                Url = "UserCertificates?cmd=addnew",
                                PanelID = "UserDashboardCertificates"
                            });

                            //SURVEYS preset widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 5,
                                ColID = 1,
                                DispSort = 5,
                                Title = "Surveys",
                                WidgetType = "preset",
                                Name = "surveys",
                                Url = "UserSurveys?cmd=addnew",
                                PanelID = "UserDashboardSurveys"
                            });

                        }
                        if (col2flex > 0)
                        {
                            //COURSES preset widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 6,
                                ColID = 2,
                                DispSort = 1,
                                Title = "Courses",
                                WidgetType = "preset",
                                Name = "courses",
                                Url = "UserCourses?cmd=addnew",
                                PanelID = "UserDashboardCourses"
                            });

                            //RECIEVED EMAILS preset widget
                            widgetinfo.Add(new WidgetInfo()
                            {
                                ID = 7,
                                ColID = 2,
                                DispSort = 2,
                                Title = "Received Emails",
                                WidgetType = "preset",
                                Name = "recievdemails",
                                Url = "UserEmails?cmd=addnew",
                                PanelID = "UserDashboardReceivedEmail"
                            });

                            if (Settings.Instance.GetMasterInfo3().UsePurchaseCredit == 1)
                            {
                                widgetinfo.Add(new WidgetInfo()
                                {
                                    ID = 9,
                                    ColID = 2,
                                    DispSort = 3,
                                    Title = Settings.Instance.GetMasterInfo2().CreditHoursName + " Transactions",
                                    WidgetType = "preset",
                                    Name = "coursehours",
                                    Url = "UserReportsCourseTransactions?cmd=addnew",
                                    PanelID = "UserDashCourseHoursTransactions"
                                });
                            }

                            if (Settings.Instance.GetMasterInfo2().AllowStudentMultiEnroll == 1)
                            {
                                widgetinfo.Add(new WidgetInfo()
                                {
                                    ID = 10,
                                    ColID = 2,
                                    DispSort = 3,
                                    Title = "User Enrolled Courses for Other Students",
                                    WidgetType = "preset",
                                    Name = "otherusercourses",
                                    Url = "UserEnrolledOtherStudents?cmd=addnew",
                                    PanelID = "OtherUserDashCourse"
                                });
                            }
                        }

                    }
                    else
                    {
                        //student inst addnew
                        if (col1flex > 0)
                        {
                            //IDENTITY widget
                            widgetinfo.Add(new WidgetInfo() { ID = 1, ColID = 1, DispSort = 1, Title = "Identity", WithProfileImage = true });

                            //AFFILIATION widget
                            widgetinfo.Add(new WidgetInfo() { ID = 2, ColID = 1, DispSort = 2, Title = "Affiliation" });
                        }
                        if (col2flex > 0)
                        {
                            //DEMOGRAPHIC widget
                            widgetinfo.Add(new WidgetInfo() { ID = 3, ColID = 2, DispSort = 1, Title = "Demographic" });
                        }

                    }



                    //FIELDS       ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                    //IDENTITY Fields
                    widgetitemlist.Add(new WidgetItemList() { FieldName = "last", ID = 1, DispSort = 1, WidgetID = 1 });
                    widgetitemlist.Add(new WidgetItemList() { FieldName = "first", ID = 2, DispSort = 2, WidgetID = 1 });
                    widgetitemlist.Add(new WidgetItemList() { FieldName = "username", ID = 3, DispSort = 3, WidgetID = 1 });
                    if (istud == 1 || istud == 2 || istud == 3) //students
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "studnum", ID = 4, DispSort = 4, WidgetID = 1 });
                    }
                    else
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "instructornum", ID = 4, DispSort = 4, WidgetID = 1 });
                    }
                    widgetitemlist.Add(new WidgetItemList() { FieldName = "email", ID = 5, DispSort = 5, WidgetID = 1 });
                    widgetitemlist.Add(new WidgetItemList() { FieldName = "additionalemail", ID = 10, DispSort = 10, WidgetID = 1 });

                    //AFFILIATION Fields
                    if (Settings.Instance.GetMasterInfo().VisibleStudentDISTRICT != 0)
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "district", ID = 6, DispSort = 1, WidgetID = 2 });
                    }

                    if (Settings.Instance.GetMasterInfo().VisibleStudentSCHOOL != 0)
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "school", ID = 7, DispSort = 2, WidgetID = 2 });
                    }

                    if (Settings.Instance.GetMasterInfo().VisibleStudentGRADE != 0)
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "grade", ID = 8, DispSort = 3, WidgetID = 2 });
                    }
                    if (Settings.Instance.GetMasterInfo3().AssignSup2StudVisible == 1)
                    {
                        widgetitemlist.Add(new WidgetItemList() { FieldName = "supervisor", ID = 9, DispSort = 3, WidgetID = 2 });

                    }

                    //DEMOGRAPHIC Fields
                    idf = 9;
                    if (istud == 1 || istud == 2 || istud == 3) //students
                    {
                        foreach (var item in dlists.AllStudentUserFields.Where(f => f.FieldGrp == "customfield" && f.FieldVisible == true))
                        {
                            idf = idf + 1;
                            widgetitemlist.Add(new WidgetItemList()
                            {
                                FieldName = item.FieldName,
                                ID = idf,
                                DispSort = idf,
                                WidgetID = 3
                            });
                        }
                    }


                    uw.Column = widgetcolumn;
                    uw.Widgets = widgetinfo;
                    uw.WidgetItems = widgetitemlist;


                    using (var db = new SchoolEntities())
                    {
                        foreach (var mi4 in db.masterinfo4)
                        {
                            if (istud == 1)
                            {
                                mi4.StudentsDashAddnew = Json.Encode(uw);
                                resetedflds = resetedflds + "StudentsDashAddnew, ";
                            }
                            else if (istud == 2)
                            {
                                mi4.StudentsDashViewEdit = Json.Encode(uw);
                                resetedflds = resetedflds + "StudentsDashViewEdit, ";
                            }
                            else if (istud == 3)
                            {
                                mi4.StudentsDashAdmin = Json.Encode(uw);
                                resetedflds = resetedflds + "StudentsDashAdmin, ";
                            }

                            else if (istud == 4)
                            {
                                mi4.InstructorsDashAddnew = Json.Encode(uw);
                                resetedflds = resetedflds + "InstructorsDashAddnew, ";
                            }
                            else if (istud == 5)
                            {
                                mi4.InstructorsDashViewEdit = Json.Encode(uw);
                                resetedflds = resetedflds + "InstructorsDashViewEdit, ";
                            }
                            else if (istud == 6)
                            {
                                mi4.InstructorsDashAdmin = Json.Encode(uw);
                                resetedflds = resetedflds + "InstructorsDashAdmin, ";
                            }

                        }
                        db.SaveChanges();
                    }

                }

                Settings.Instance.Refresh();



            }

            if (resetcmd == "NONE")
            {
                if (string.IsNullOrEmpty(resetedflds))
                {
                    return "empty";
                }
                else
                {
                    return "initialized";
                }
            }
            else
            {
                return "Reseted: " + resetedflds;
            }
        }


        public bool? ActivateOrDeactivateUserInBB(int userid,string available)
        {
            var bbConfig = blackboard.Configuration.Instance;
            if (bbConfig.BlackboardRealtimeStudentSyncEnabled)
            {
                var Context = new SchoolEntities();
                var st = Context.Students.Where(s => s.STUDENTID == userid).SingleOrDefault();
                if (st != null)
                {
                    if (available == "No")
                        st.InActive = 1;
                    else
                        st.InActive = 0;

                    Context.SaveChanges();
                }

                if (Configuration.Instance.BlackboardUseAPI && Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardMembershipIntegrationEnabled)
                {
                    try
                    {

                        if (userid != 0)
                        {
                            BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            BBUser user_update = new BBUser();
                            ProfileAvailabilityObj availability = new ProfileAvailabilityObj();
                            availability.available = available;
                            user_update.availability = availability;


                            BBRespUserProfile updateduser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user_update, st.Blackboard_user_UUID, "uuid", "", jsonToken, "");

                            return true;
                        }
                        return null;

                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                }

                else
                {
                    if (available == "No")
                        available = "false";
                    else
                    {
                        available = "true";
                    }
                    var checkUserExist = blackboard.Connector.UserConnector.SeachUserByFields("", st.USERNAME);
                    if (checkUserExist.IsSuccess && st.USERNAME != "")
                    {
                        var userResult = blackboard.Connector.UserConnector.UpdateBBUserAccount(st.USERNAME, "", st.FIRST,st.LAST, st.EMAIL, available,st.DISTRICT,st.SCHOOL,st.GRADE);
                        return true ;

                    }
                    else
                    {
                        return null; 
                    }
                }

                return null;


                
            }
            else
            {
                return null;
            }

        }
            
    }
}

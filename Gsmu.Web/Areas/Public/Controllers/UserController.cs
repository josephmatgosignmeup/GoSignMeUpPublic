using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.EmailAuditTrail;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Data.School.User;
using Gsmu.Api.Export;
using System;
using System.IO;
using web = Gsmu.Api.Web;
using System.Web;
using System.Web.Mvc;
using System.Web.Helpers;
using System.Collections.Generic;
using Gsmu.Api.Web;
using System.Configuration;
using System.Linq;
using blackboard = Gsmu.Api.Integration.Blackboard;
using Gsmu.Api.Data.School.Transcripts;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Export.GradeCertificate;
using Gsmu.Api.Data.Survey;
//using Gsmu.Api.Data.Survey.CourseSurvey;
using Gsmu.Api.Data.School.Terminology;
using System.Web.Script.Serialization;
using Gsmu.Api.Networking.Mail;
using System.Text.RegularExpressions;

namespace Gsmu.Web.Areas.Public.Controllers
{
    //temporary disabled for testing and debugging
    //[GsmuAuthorization(LoggedInUserType.Student, LoggedInUserType.Instructor, LoggedInUserType.Supervisor, AllowedActions = new string[] { "RegisterUser", "UserProfileImage", "SubmitUserInfo", "CheckStudentUsernameAvailable", "RegisterUserAcknowledgement", "Disclaimer", "AccountRecovery", "SubmitRecover", "AccountResetPass", "ResetPass", "DashboardEdit", "SumbitUserWidget", "UserWidgetEdit", "UserWidgetAdminMode", "UserWidget", "UserWidgetStores","UserWidgetStores", "InitializeUserWidgetSettings","UserWidgetStores","UserWidgetStores","UserWidgetStores","UserRoles" })]
    public class UserController : Controller
    {
        public JavaScriptSerializer json;
        [HttpPost]
        public ActionResult SumbitUserWidget(UserWidget ui,  string adminmode = "none")
        {
            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err702.");
            }
            UserWidget resultui = new UserModel().SumbitUserWidget(adminmode, ui);

            JsonResult result = new JsonResult();
            result.Data = new
            {
                success = true,
            };
            return result;
        }

        [HttpPost]
        public ActionResult SumbitUserWidgetProp(List<UserRegFieldSpecs> list, string adminmode = "none")
        {

            bool resultui = new UserModel().SumbitUserWidgetProp(adminmode, list);

            JsonResult result = new JsonResult();
            result.Data = new
            {
                success = resultui,
            };
            return result;
        }


        public String InitializeUserWidgetSettings(string resetcmd = "NONE")
        {
            string resultui = new UserModel().InitializeUserWidgetSettings(resetcmd);

            return resultui;
        }



        public ActionResult UserWidgetStores(string cmd = "view", string usergroup = "ST")
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }
        [HttpGet]
        public ActionResult WidgetFilterSchool(string filter = null)
        {
            var result = new JsonResult();
            int? filtrDistrict = 0;
            string filterText = "";

            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            if (filter != null && filter.Contains("district") && !filter.Contains("undefined"))
            {
                var converter = new JavaScriptSerializer();
                var parseResult = converter.Deserialize<dynamic[]>(filter);
                foreach (var filters in parseResult)
                {
                    try
                    {
                        if ((filters["property"] == "district"))
                        {
                            filtrDistrict = int.Parse(filters["value"]);
                        }

                        if ((filters["property"] == "txt"))
                        {
                            filterText = filters["value"];
                        }
                    }
                    catch { }
                }
            }
            Gsmu.Api.Data.School.DataLists dlists = new Gsmu.Api.Data.School.DataLists();

            var list = dlists.Schools.Where(p => p.District == filtrDistrict && p.LOCATION.ToLower().Contains(filterText));
            result.Data = new
            {
                success = true,
                data = list
                .OrderBy(s => s.SortOrder)
                .Select(i => new { txt = i.LOCATION, vlu = i.locationid.ToString(), district = i.District.ToString() })
            };

           
            return result;
        }
        [HttpGet]
        public ActionResult WidgetFilterGrade(string filter = null)
        {
            var result = new JsonResult();
            int? filtrGrade = 0;
            string filterText = "";

            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            if (filter != null && filter.Contains("schoolid") && !filter.Contains("undefined"))
            {
                var converter = new JavaScriptSerializer();
                var parseResult = converter.Deserialize<dynamic[]>(filter);
                foreach (var filters in parseResult)
                {
                    try
                    {
                        if ((filters["property"] == "schoolid"))
                        {
                            filtrGrade = int.Parse( filters["value"]);
                        }

                        if ((filters["property"] == "txt"))
                        {
                            filterText = filters["value"];
                        }
                    }
                    catch { }
                }
            }
            Gsmu.Api.Data.School.DataLists dlists = new Gsmu.Api.Data.School.DataLists();
            var list = dlists.GradeLevelsForFilter.Where(p => p.SCHOOLID == filtrGrade && p.GRADE.ToLower().Contains(filterText));
            result.Data = new
            {
                success = true,
                data = list
                .OrderBy(a => a.GradeSortOrder).ThenBy(a => a.GRADE)
                .Select(i => new { txt = i.GRADE, vlu = i.GRADEID.ToString(), schoolid = i.SCHOOLID.ToString() })

            };
            return result;
        }

        [CustomSecurityforCrossSiteForgeryAttributes]
        public ActionResult UserWidget(UserWidget userwidget, WidgetInfo widgetmodel, string txtuserwidget = "", string txtfld = "", string txtwidgetmodel = "", string cmode = "obj", string cmd = "view", string usergroup = "ST", int sid=0)
        {
            ViewBag.cmode = cmode;
            ViewBag.cmd = cmd;
            ViewBag.widgetmodel = widgetmodel;
            ViewBag.userwidget = userwidget;
            ViewBag.txtwidgetmodel = txtwidgetmodel;
            ViewBag.txtuserwidget = txtuserwidget;
            ViewBag.txtfld = txtfld;
            ViewBag.usergroup = usergroup;
            ViewBag.SelectedUserid = sid;

            UserModel ui = new UserModel(usergroup, cmd, sid);
            return PartialView(ui);

        }


        //[RequireAdminMode(true)]
        public ActionResult UserWidgetAdminMode()
        {
            return View();
        }

        public ActionResult UserWidgetEdit(string txtwidgetmodel = "", string txtuserwidget = "", string txtfld = "", string adminmode = "none", string cmd = "addnew", string usergroup = "ST")
        {
            ViewBag.txtwidgetmodel = txtwidgetmodel;
            ViewBag.txtuserwidget = txtuserwidget;
            ViewBag.txtfld = txtfld;
            ViewBag.adminmode = adminmode;
            ViewBag.usergroup = usergroup;

            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserRoles(string cmd = "view", string usergroup = "ST")
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserEmails(string cmd = "view", string usergroup = "ST", int userid =0)
        {
            ViewBag.cmd = cmd;
            ViewBag.userid = userid;
            UserModel ui = new UserModel(usergroup, cmd, userid);
            return PartialView(ui);
        }

        [HttpPost]
        public ActionResult UploadProfileImage(string abv, int userid, HttpPostedFileBase file)
        {
            //int userid = Gsmu.Api.Authorization.AuthorizationHelper.CurrentStudentUser.STUDENTID;
            string result = UploadImage.UploadTempImage(file, abv, userid);

            bool success = false; 
            string filename = "error";

            if (result != "false")
            {
                success = true;
                filename = result;
            }

            JsonResult jsn = new JsonResult();
            jsn.ContentType = "text/html";
            jsn.Data = new
            {
                success = success,
                filename = filename
            };
            return jsn;
        } 

        public ActionResult UserProfileImage(string cmd, string usergroup)
        {
            ViewBag.cmd = cmd;
            ViewBag.V3InstructorImage = ConfigurationManager.AppSettings["V3InstructorImage"];
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserDetails(string cmd = "view", string usergroup = "ST", string activetab = "0")
        {
            ViewBag.cmd = cmd;
            ViewBag.activetab = activetab;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserAffiliation(string cmd = "view", string usergroup = "ST")
        {
            
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserDemographic(string cmd = "view", string usergroup = "ST")
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserCourses(string cmd = "view", string usergroup = "ST",int userid=0)
        {
            int userid_fromSupervisor = 0;
            string SupervisorRequest = "false";
            if (AuthorizationHelper.CurrentSupervisorUser != null)
            {
                userid_fromSupervisor = userid;
                SupervisorRequest = "true";
            }
            string SupervisorConfiguration = Settings.Instance.GetMasterInfo4().SupervisorConfiguration;
            if (SupervisorConfiguration == "" || SupervisorConfiguration == null)
            {
                SupervisorConfiguration = "{}";
            }
            json = new JavaScriptSerializer();
            dynamic supervisorConfiguration = json.Deserialize(SupervisorConfiguration, typeof(object));
            int allowStudentMoveWaittoEnroll = 0;
            if (supervisorConfiguration.ContainsKey("allowstudentmovewaittoenroll"))
            {
                allowStudentMoveWaittoEnroll = int.Parse(supervisorConfiguration["allowstudentmovewaittoenroll"]);
            }
            ViewBag.AllowStudentMoveWaittoEnroll = allowStudentMoveWaittoEnroll;
            ViewBag.SupervisorRequest = SupervisorRequest;
            ViewBag.DaysBeforeCourseStart = Settings.Instance.GetMasterInfo().CourseCancelDays == null ? 0.00 : Convert.ToDouble(Settings.Instance.GetMasterInfo().CourseCancelDays);
            ViewBag.AllowCourseCancellation = Settings.Instance.GetMasterInfo().AllowCancel;
            ViewBag.NoCancelMessage = Settings.Instance.GetMasterInfo3().PublicNoCancelMessage;
            ViewBag.cmd = cmd;
            ViewBag.AllowMultiEnroll = Settings.Instance.GetMasterInfo2().AllowStudentMultiEnroll;
            ViewBag.MultipleSignUp = Settings.Instance.GetMasterInfo3().restrictStudentMultiSignup;
            ViewBag.CancelCourseDateReached = true;
            ViewBag.AllowCourseCancelOnPaymentStatus = Settings.Instance.GetMasterInfo3().allowcancelOnlyNotPaid;
            UserModel ui = new UserModel(usergroup, cmd, userid_fromSupervisor);

            return PartialView(ui);
        }

        public ActionResult UserMemberships(string cmd = "view", string usergroup = "ST", int userid = 0)
        {
            UserModel ui = new UserModel(usergroup, cmd);

            var usr = new UserInfo();
            usr = ui.CommonUserInfo;

            
            return PartialView(ui);
        }

        public ActionResult UserCertificates(string cmd = "view", string usergroup = "ST", int userid = 0)
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd, userid);
            return PartialView(ui);
        }

        public ActionResult UserActions(string cmd = "view", string usergroup = "ST")
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }

        public ActionResult UserReportsCourseTransactions(string cmd = "view", string usergroup = "ST", int userid = 0)
        {
            int userid_fromSupervisor = 0;
            if (AuthorizationHelper.CurrentSupervisorUser != null)
            {
                userid_fromSupervisor = userid;
            }
            ViewBag.cmd = cmd;
            ViewBag.Label = Settings.Instance.GetMasterInfo2().CustomCreditTypeName + " and " + TerminologyHelper.Instance.GetTermCapital(TermsEnum.Transcript); ;
            UserModel ui = new UserModel(usergroup, cmd, userid_fromSupervisor);
            return PartialView(ui);
        }
        public ActionResult UserEnrolledOtherStudents(string cmd = "view", string usergroup = "ST", int userid = 0)
        {
            return PartialView();
        }        
        public ActionResult UserReports(string cmd = "view", string usergroup = "ST")
        {
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd);
            return PartialView(ui);
        }
        public ActionResult UserSurveys(string cmd = "view", string usergroup = "ST", int userid = 0)
        {
            int sid = 0;
            if (AuthorizationHelper.CurrentSupervisorUser != null)
            {
                sid = userid;
            }
            Gsmu.Api.Data.Survey.SurveyFunction surveyf = new Api.Data.Survey.SurveyFunction();
            if(userid==0)
            {
                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    sid = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                }
            }
            if (sid != 0)
            {
                ViewBag.presurvey = surveyf.GetPreSurveyForStudent(sid);
                ViewBag.postsurvey = surveyf.GetPostSurveyForStudent(sid);
            }
            else
            {
                ViewBag.postsurvey = new List<Gsmu.Api.Data.Survey.SurveyFunction.SurveyObject>();
                ViewBag.presurvey = new List<Gsmu.Api.Data.Survey.SurveyFunction.SurveyObject>();
            }
            ViewBag.cmd = cmd;
           // UserModel ui = new UserModel(usergroup, cmd);
            UserModel ui = null;
            return PartialView(ui);
        }

        public ActionResult UserSurveyList(string cmd = "view", string usergroup = "ST",int userid =0)
        {
 
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(usergroup, cmd,userid);
            return PartialView(ui);
        }

        public ActionResult MembershipStatus()
        {
            return PartialView("~/Areas/Public/Views/Shared/_MembershipStatus.cshtml");
        }

        [HttpPost]
        public ActionResult SubmitUserInfo(UserInfo ui)
        {
            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err701.");
            }
            JsonResult result = new JsonResult();
            try
            {
                if ((Gsmu.Api.Data.Settings.Instance.GetMasterInfo2().disallownewuser == -1) && AuthorizationHelper.CurrentStudentUser == null && AuthorizationHelper.CurrentAdminUser == null)
                {
                    if ((AuthorizationHelper.CurrentSupervisorUser == null) && (AuthorizationHelper.CurrentInstructorUser == null) && ui.usergroupAbv != "SP" && AuthorizationHelper.CurrentUser.LoggedInUserType != LoggedInUserType.Admin && AuthorizationHelper.CurrentUser.LoggedInUserType != LoggedInUserType.SubAdmin)
                    {


                        var error = "Account creation process has been disabled due to session timed out or partially timed out. Please logout and login again. UErr105AC";
                        result.Data = new
                        {
                            success = false,
                            error = error + " "
                        };

                        return result;

                    }
                }
                if ((Gsmu.Api.Data.Settings.Instance.GetMasterInfo2().DisallowUserPublicEdit == 1) && AuthorizationHelper.CurrentStudentUser != null && AuthorizationHelper.CurrentAdminUser == null)
                {
                    var error = "Account modification is not allowed. Please contact your administrator. UErr105AE";
                    if (Gsmu.Api.Data.Settings.Instance.GetMasterInfo2().DisallowUserPublicEditText != "")
                    {
                        error = Gsmu.Api.Data.Settings.Instance.GetMasterInfo2().DisallowUserPublicEditText;
                    }
                    result.Data = new
                    {
                        success = false,
                        error = error + " Invalid Session"
                    };

                    return result;
                }


                UserInfo resultui = new UserModel().SumbitUserInfo(ui, ui.usergroupAbv);

                if (ui.usergroupAbv == "SP")
                {
                    AuthorizationHelper.LoginSupervisor(resultui.username, resultui.password);
                    AuthorizationHelper.UpdateCurrentLoginSupervisor(resultui.userid);
                }
                else
                {

                    var cookie = Request.Cookies["post-registration-action"];
                    if (ui.username != null && cookie != null && cookie.Value == "checkout" && UserModel.UserGroupToLoggedInUserType(ui.usergroupAbv) == LoggedInUserType.Student)
                    {
                        if ((Gsmu.Api.Authorization.AuthorizationHelper.CurrentSupervisorUser == null) && (Gsmu.Api.Authorization.AuthorizationHelper.CurrentInstructorUser == null) && Gsmu.Api.Authorization.AuthorizationHelper.CurrentAdminUser == null)
                        {
                            //AuthorizationHelper.LoginStudent(ui.username);
                            AuthorizationHelper.UpdateCurrentLoginStudent(resultui.userid);
                        }
                        else
                        {
                            Gsmu.Web.Areas.Adm.Controllers.AdministratorEnrollmentController.SetPrincipalStudent(resultui.userid);
                        }
                    }

                    if (cookie != null && cookie.Value == "Dashboard" && resultui.username != null)
                    {
                        if ((Gsmu.Api.Authorization.AuthorizationHelper.CurrentSupervisorUser == null) && (Gsmu.Api.Authorization.AuthorizationHelper.CurrentInstructorUser == null) && Gsmu.Api.Authorization.AuthorizationHelper.CurrentAdminUser == null && Gsmu.Api.Authorization.AuthorizationHelper.CurrentSubAdminUser == null)
                        {
                            // AuthorizationHelper.LoginStudent(resultui.username, resultui.password);
                            AuthorizationHelper.UpdateCurrentLoginStudent(resultui.userid);
                        }
                        else
                        {
                            Gsmu.Web.Areas.Adm.Controllers.AdministratorEnrollmentController.SetPrincipalStudent(resultui.userid);
                        }
                    }
                }

             
                result.Data = new
                {
                    success = true,
                    userid = resultui.userid,
                    usergroupAbv = ui.usergroupAbv,
                    username = ui.username
                };
            }
            catch (Exception e)
            {

                var error = "Unable to Process the account. Please contact your administrator. UErr105" +e.InnerException +e.Message;
              if(e.Message.Contains("Username already exist"))
              {
                  error= "Username already exists.";
              }
                result.Data = new
                {
                    success = false,
                    error = error
                };
            }


            return result;
        }

 

        public ActionResult Dashboard(string MissingReqFields = "0")
        {
            if (Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.LoggedInUserType == LoggedInUserType.Supervisor)
            {
               // return RedirectToAction("Index", "Supervisor");
            }

            if (Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.IsLoggedIn)
            {
                System.Web.HttpContext.Current.Session["UserDashboardUserid"] = "0";
                System.Web.HttpContext.Current.Session["UserDashboardMode"] = "UserView";
                string cmd = "view";
                ViewBag.cmd = cmd;
                ViewBag.MissingReqFields = MissingReqFields;
                UserModel ui = new UserModel("ANY", cmd);
                return View(ui);
            }
            return RedirectToAction("Index", "Home");
        }

        [UserLoginSecuredAttributes]
        public ActionResult DashboardViewAdmin(string abv = "ST", string SearchText = "", int userid = 0)
        {
            if ((abv == "ST" && Gsmu.Api.Authorization.AuthorizationHelper.CurrentSupervisorUser != null) ||(Gsmu.Api.Authorization.AuthorizationHelper.CurrentStudentUser!=null))
            {
                Gsmu.Api.Authorization.AuthorizationHelper.Logout();
            }
            System.Web.HttpContext.Current.Session["UserDashboardUserid"] = userid.ToString();
            System.Web.HttpContext.Current.Session["UserDashboardAbv"] = abv;
            System.Web.HttpContext.Current.Session["UserDashboardMode"] = "AdminView";

            string cmd = "view";
            ViewBag.userid = userid;
            ViewBag.abv = abv;
            ViewBag.cmd = cmd;
            ViewBag.SearchText = SearchText;
            UserModel ui = new UserModel(abv, cmd, userid);
            return View(ui);
        }

        public ActionResult DashboardAdmin(string abv = "ST", int userid = 0)
        {
            if (abv == "ST" && Gsmu.Api.Authorization.AuthorizationHelper.CurrentSupervisorUser != null)
            {
                Gsmu.Api.Authorization.AuthorizationHelper.Logout();
            }
            System.Web.HttpContext.Current.Session["UserDashboardUserid"] = userid.ToString();
            System.Web.HttpContext.Current.Session["UserDashboardAbv"] = abv;
            System.Web.HttpContext.Current.Session["UserDashboardMode"] = "AdminView";

            string cmd = "view";
            ViewBag.userid = userid;
            ViewBag.abv = abv;
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel(abv, cmd, userid);
            return PartialView(ui);
        }

        public ActionResult SetAdminSessionOnSearch(string abv = "ST", int userid = 0)
        {

            System.Web.HttpContext.Current.Session["UserDashboardUserid"] = userid.ToString();
            System.Web.HttpContext.Current.Session["UserDashboardAbv"] = abv;
            System.Web.HttpContext.Current.Session["UserDashboardMode"] = "AdminView";
            JsonResult result = new JsonResult();
            result.Data = new{success = true};
            return result;

        }



        public ActionResult DashboardEdit(bool partialview = false, string mode = "none")
        {
            System.Web.HttpContext.Current.Session["UserDashboardMode"] = "AdminEditMode";
            string cmd = "addnew";
            string usergroup = "ST";
            if (mode == "InstructorsDashViewEdit" || mode == "InstructorsDashAddnew" || mode == "InstructorsDashAdmin")
            {usergroup = "IT";}

            ViewBag.usergroup = usergroup;
            ViewBag.cmd = cmd;
            ViewBag.adminmode = mode;

            UserModel ui = new UserModel(usergroup, cmd);
            if (partialview == true)
            {
                return PartialView(ui);
            }
            else
            {
                return View(ui);
            }
        }




        public ActionResult RegisterUser()
        {
            string cmd = "addnew";
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel("ST", cmd);
            ViewBag.PrivacyReq = Settings.Instance.GetMasterInfo().PrivacyReq;
            ViewBag.PrivacyInFormDisp = Settings.Instance.GetMasterInfo().PrivacyInFormDisp;
            ViewBag.TermsLabel = Settings.Instance.GetMasterInfo().TermsLabel;
            ViewBag.PrivacyText = Settings.Instance.GetMasterInfo().PrivacyText;
            ViewBag.SupervisorAccess = 0;
            if ((AuthorizationHelper.CurrentSupervisorUser != null) || (AuthorizationHelper.CurrentInstructorUser != null))
            {

                    ViewBag.SupervisorAccess = 1;
            }
            return View(ui);
        }

        public ActionResult RegisterSupervisor()
        {
            ViewBag.BGColorInfolist=new List<string>();
            string cmd = "addnew";
            ViewBag.cmd = cmd;
            UserModel ui = new UserModel("SP", cmd);
            ViewBag.PrivacyReq = Settings.Instance.GetMasterInfo().PrivacyReq;
            ViewBag.PrivacyInFormDisp = Settings.Instance.GetMasterInfo().PrivacyInFormDisp;
            ViewBag.TermsLabel = Settings.Instance.GetMasterInfo().TermsLabel;
            ViewBag.PrivacyText = Settings.Instance.GetMasterInfo().PrivacyText;
            return View(ui);
        }

        //[RequireAdminMode(true)]
        public ActionResult RegisterUserAdminView(string abv)
        {
            string adminusersessionid = Request["sessionid"];
            int CompleteSupAccountToSupervisor = 0;
            try
            {
                if (Settings.Instance.GetMasterInfo4().CompleteSupAccountToSupervisor != null)
                {
                    CompleteSupAccountToSupervisor = Settings.Instance.GetMasterInfo4().CompleteSupAccountToSupervisor.Value;
                }
            }

            catch { }
            string username = Request["username"];
            AuthorizationHelper.ConfigurePortalAdministratorLogin(username, adminusersessionid);
            UserModel ui = new UserModel(abv, "addnew");
            ViewBag.abv = abv;
            ViewBag.CompleteSupAccountToSupervisor = CompleteSupAccountToSupervisor;

            ViewBag.NewStudentLink = "/public/user/RegisterUserAdminView?adminmode=false&abv=ST&userid=0&sessionid="+ adminusersessionid + "&username="+ username;
            return View(ui);
        }

        public ActionResult Disclaimer()
        {
            return View();
        }

        public ActionResult RegisterUserAcknowledgement()
        {
            ViewBag.requsername = Request["username"];
            ViewBag.usergroup = Request["usergroup"];
            return PartialView();
        }

        public ActionResult SendStudentEmail(string Subject, string To, string Message)
        {
            var result = new JsonResult();
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                EmailAuditTrail email = new EmailAuditTrail();
                email.EmailTo = To;
                email.EmailSubject = Subject;
                email.EmailBody = HttpUtility.HtmlDecode(Message);
                email.AuditProcess = "Supervisor Dash";
                email.AuditDate = DateTime.Now;
                EmailFunction.SendEmail(email);
                if (Gsmu.Api.Data.School.Supervisor.SupervisorHelper.SendingEmailStatus == "Sent")
                {
                    result.Data = "Message Sent.";
                }
                else
                {
                    result.Data = "Message Not Sent.";
                }
            }
            catch (Exception e)
            {
                result.Data = e.Message;
            }
            return result;
        }

        public ActionResult GetStudentRecord(int studentId)
        {
            var result = new JsonResult();
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                result.Data = Gsmu.Api.Data.School.Supervisor.SupervisorHelper.GetStudent(studentId);
            }
            catch (Exception)
            {
            }
            return result;
        }


        public ActionResult AccountRecovery(string usertype)
        {
            var layoutConfig = Gsmu.Api.Data.ViewModels.Layout.LayoutManager.PublicLayoutConfiguration;
            var hideSupervisorLogin = Settings.Instance.GetMasterInfo3().HideSupervisorLogin;
            var hideInstructorLogin = Settings.Instance.GetMasterInfo3().hideInstructorLogin;
            String ParentLevelTitle =  TerminologyHelper.Instance.GetTermCapital(TermsEnum.Supervisor);
            String InstructorLevelTitle =  TerminologyHelper.Instance.GetTermCapital(TermsEnum.Instructor);
            String StudentTitle =  TerminologyHelper.Instance.GetTermCapital(TermsEnum.Student);
            if (usertype == null || usertype == "")
            {
                if (layoutConfig.HideStudentLogin == 1 && hideInstructorLogin == 1)
                {
                    usertype = "supervisor";
                }

                if (@layoutConfig.HideStudentLogin == 1 && (@hideSupervisorLogin == 1 || @hideSupervisorLogin == 2))
                {
                    usertype = "instructor";
                }

                if (@layoutConfig.HideStudentLogin == 0)
                {
                    usertype = "";
                }
                else
                {
                    usertype = "instructor";
                }
            }
            ViewBag.userType = usertype;
            if (usertype == "supervisor")
            {
                ViewBag.Type_Text = ParentLevelTitle;
            }
            else if (usertype == "instructor")
            {
                ViewBag.Type_Text = InstructorLevelTitle;
            }
            else
            {
                ViewBag.Type_Text = StudentTitle;
            }
            if (Settings.Instance.GetMasterInfo2().HideForgotPassword == 1)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        public String SubmitRecover(string usertype)
        {
            String reply = "";
            String username = Request["username"];
            String firstname = Request["first"];
            String email = Request["email"];
            String recovrtyp = Request["recovrtyp"];
            String logintype = "S";
            if (usertype == "supervisor")
            {
                logintype = "Sup";
            }
            else if (usertype == "instructor")
            {
                logintype = "I";
            }
            
            reply = UserDashQueries.SendCheckRecover(username, firstname, email, recovrtyp, logintype);
            return reply;
        }


        public ActionResult AccountResetPass(string usertype)
        {
            String ParentLevelTitle = TerminologyHelper.Instance.GetTermCapital(TermsEnum.Supervisor);
            String InstructorLevelTitle = TerminologyHelper.Instance.GetTermCapital(TermsEnum.Instructor);
            String StudentTitle = TerminologyHelper.Instance.GetTermCapital(TermsEnum.Student);
            if (usertype == "supervisor")
            {
                ViewBag.Type_Text = ParentLevelTitle;
            }
            else if (usertype == "instructor")
            {
                ViewBag.Type_Text = InstructorLevelTitle;
            }
            else
            {
                ViewBag.Type_Text = StudentTitle;
            }
            String tokenstatus = ResetPass("init");
            ViewBag.tokenstatus = tokenstatus;
            return View();
        }


        public String ResetPass(string cmd)
        {
            String reply = "";
            String resetstatus = "";
            String msg = "";
            String username = Request["username"];
            String resettoken = Request["resettoken"];
            String firstpassword = Request["firstpassword"];
            String logintype = Request["stype"];

            String tokenstatus = UserDashQueries.CheckValidResettoken(username, resettoken, logintype);
            if (tokenstatus == "validtoken")
            {
                resetstatus = UserDashQueries.SendResetPass(username, resettoken, firstpassword, logintype);
                // Make sure the password is not empty
                // This is to make sure that the password wont be updated if the system did not have the new password set
                if (resetstatus == "failed")
                {
                    if (string.IsNullOrEmpty(firstpassword))
                    {
                        msg = "New Password Missing. Please make sure that the password is not empty.";
                        reply = "{success:false, msg:'" + msg + "'}";
                    }
                    else 
                    { 
                        msg = "Invalid reset token. Make a request again or contact your administrator.";
                        reply = "{success:false, msg:'" + msg + "'}";
                    }
                }
                else if (resetstatus == "successupdatepartial")
                {
                    msg = ("'Password has only been reset in Gosignmeup. Please login with your new password'");
                    reply = "{success:true, msg:" + msg + "}";
                }
                else
                {
                    msg = ("'Password has been reset. Please login with your new password'");
                    reply = "{success:true, msg:" + msg + "}";
                }
            }
            else
            {
                if (tokenstatus == "validtokendate")
                {
                    msg = "Your request has expired. Make a request again or contact your administrator.";
                    reply = "{success:false, msg:'" + msg + "'}";
                }
                else
                {
                    msg = "Invalid reset token. Make a request again or contact your administrator.";
                    reply = "{success:false, msg:'" + msg + "'}";
                }
            }

            if (cmd == "init")
            {
                if (tokenstatus == "validtoken") { return tokenstatus; } else { return msg; }
            }
            else
            {
                return reply;
                //return "resettoken: " + resettoken + " username: " + username + " logintype: " + logintype + " tokenstatus: " + tokenstatus;
            }

        }
   
 
        public String CheckStudentUsernameAvailable()
        {
            string username = Request["Username"];
            username = Regex.Replace(username, @"\s+", "");
            var bbConfig = blackboard.Configuration.Instance;
            if(UserDashQueries.ProcessCheckStudentUsernameExist(username)){
                if (bbConfig.BlackboardSsoEnabled)
                {
                    return "notavailableBB";
                }
                else if (Gsmu.Api.Integration.Canvas.Configuration.Instance.EnableOAuth2Authentication && Gsmu.Api.Integration.Canvas.Configuration.Instance.HideLoginFormIfUserInCanvas)
                {
                    return "notavailableCanvas";
                }
                else if (Gsmu.Api.Integration.Canvas.Configuration.Instance.EnableOAuth2Authentication)
                {
                    return "notavailableGSMUorCanvas";
                }
                else
                {
                    return "notavailable";
                }
                
            }else{
                if (Gsmu.Api.Integration.Haiku.Configuration.Instance.HaikuAuthenticationEnabled)
                {
                    if (username.Contains('@') || username.Contains('_') || username.Contains('-'))
                    {

                        return "invalidhaiku";
                    }
                    else
                    {
                        return "available";
                    }
                }
                else
                {
                    return "available";
                }
            }
        }

        public String CheckInstructorUsernameAvailable()
        {
            string username = Request["Username"];
            if (UserDashQueries.ProcessCheckInstructorUsernameExist(username))
            {
                return "notavailable";
            }
            else
            {
                return "available";
            }
        }

        [UserLoginSecuredAttributes]
        public ActionResult StudentListQuery(int start = 0, int limit = 5, string query = null, string sort = null)
        {
            var result = new JsonResult();
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(sort);

                var queryState = new QueryState(start, limit)
                {
                    OrderByDirection = sorterResult.Value,
                    OrderFieldString = sorterResult.Key,
                    Query = query
                };
                result.Data = Gsmu.Api.Data.School.User.Queries.GetStudentListQuery(queryState);
            }
            catch
            {
            }
            return result;
        }

        [UserLoginSecuredAttributes]
        public ActionResult InstructorListQuery(int start = 0, int limit = 5, string query = null, string sort = null)
        {
            var result = new JsonResult();
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(sort);

                var queryState = new QueryState(start, limit)
                {
                    OrderByDirection = sorterResult.Value,
                    OrderFieldString = sorterResult.Key,
                    Query = query
                };
                result.Data = Gsmu.Api.Data.School.User.Queries.GetInstructorListQuery(queryState);
            }
            catch
            {
            }
            return result;
        }
        [UserLoginSecuredAttributes]
        public ActionResult SupervisorListQuery(int start = 0, int limit = 5, string query = null, string sort = null)
        {
            var result = new JsonResult();
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
                var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(sort);

                var queryState = new QueryState(start, limit)
                {
                    OrderByDirection = sorterResult.Value,
                    OrderFieldString = sorterResult.Key,
                    Query = query
                };
                result.Data = Gsmu.Api.Data.School.User.Queries.GetSupervisorListQuery(queryState);
            }
            catch
            {
            }
            return result;
        }


        public ActionResult Email(int start = 0, int limit = 5, int userid = 0, string filter = null, string sort = null)
        {
            var result = new JsonResult();
            result.MaxJsonLength = 2147483644;
            try
            {
                result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

                var filterResult = ExtJsDataStoreHelper.ParseFilter(filter);
                var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(sort);

                var queryState = new QueryState(start, limit)
                {
                    OrderByDirection = sorterResult.Value,
                    OrderFieldString = sorterResult.Key,
                    Filters = filterResult
                };
                if (userid == 0)
                {
                    if (AuthorizationHelper.CurrentUser != null)
                    {
                        if (AuthorizationHelper.CurrentSupervisorUser != null)
                        {
                            userid = AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID;
                        }

                    }
                }
                result.Data = Gsmu.Api.Data.School.EmailAuditTrail.Queries.GetUserEmails(queryState, userid);
            }
            catch (Exception)
            {
            }
            return result;
        }


        public String CheckCertAvailabilty(string filename)
        {

            if (System.IO.File.Exists(Server.MapPath("~/admin/temp/" + filename)))
            {
                return ("available");
            }
            else
            {
                return ("notavailable");
            }

        }


        public ActionResult userCertificate(int courseId = 0, int studentid = 0, string certtype = "CourseCert", int certnum = 0, string cmd = "")
        {
            if (AuthorizationHelper.CurrentSupervisorUser == null )
            {
                if (AuthorizationHelper.CurrentAdminUser == null && AuthorizationHelper.CurrentSubAdminUser == null)
                {
                    if (studentid != AuthorizationHelper.CurrentStudentUser.STUDENTID)
                    {
                        throw new UnauthorizedAccessException("Not authorized to view the certificate. Please login first.");
                    }
                }
            }
            
            //For Course Cert
            Transcripts tran = new Transcripts();
            Transcript transcript = tran.StudentTranscriptedCourse(studentid, courseId);
            Course_Roster roster = tran.StudentTranscriptedRoster(studentid, courseId);
            CourseModel model = new CourseModel(courseId);
            
            //use default course cert
            var db = new SchoolEntities();
            int defltcoursecertificate = 0;
            if (certtype != "Certification")
            {
                if (model.Course.coursecertificate == 0)
                {
                    defltcoursecertificate = (from crt in db.customcetificates
                                              where crt.defaultcert == 1
                                              select crt.customcertid).FirstOrDefault();
                    model.Course.coursecertificate = defltcoursecertificate;
                }

                if (certtype == "SurveyCert")
                {
                    SurveyFunction surveyfunc = new SurveyFunction();
                    Gsmu.Api.Data.Survey.Survey surveyapi = new Gsmu.Api.Data.Survey.Survey(certnum);
                    int val = 0;
                    if (surveyapi.SurveyModel != null)
                    {
                        if (int.TryParse(surveyapi.SurveyModel.AfterSurveyCertificate.Replace("~", string.Empty), out val))
                        {
                            model.Course.coursecertificate = int.Parse(surveyapi.SurveyModel.AfterSurveyCertificate.Replace("~", string.Empty));
                        }
                        else
                        {
                            model.Course.coursecertificate = 0;
                        }
                    }
                    else
                    {
                        model.Course.coursecertificate = certnum;
                    }
                    int courseHasSurveyID = -1;
                    using (var dbsurvey = new Gsmu.Api.Data.Survey.Entities.SurveyEntities())
                    {
                        var courseHasSurvey = (from cs in dbsurvey.CourseSurveys where cs.CourseID == courseId select cs).FirstOrDefault();
                        courseHasSurveyID = courseHasSurvey.SurveyID;
                    }

                    if (surveyapi.CourseSurvey != null)
                    {
                        var surveycoursecheck = surveyapi.CourseSurvey.Where(surveyc => surveyc.SurveyID == certnum && surveyc.CourseID == courseId).FirstOrDefault();
                        if (surveycoursecheck == null && courseHasSurveyID == -1)
                        {
                            return Content("The Certificate is no longer available to print. Please contact Administrator.");
                            //return Redirect("~/Landing/ErrorPage/");
                        }
                    }
                    else
                    {
                        return Content("The Certificate is no longer available to print. Please contact Administrator.");
                        //return Redirect("~/Landing/ErrorPage/");

                    }

                }

                PdfGradeCertificate IndivCert = new PdfGradeCertificate(model.Course, roster, transcript);
                if (IndivCert.CustomCertificate != null)
                {
                    IndivCert.Execute();
                }
                if (IndivCert.PdfFileName != "")
                {
                    if (!System.IO.File.Exists(Server.MapPath("~/temp/" + IndivCert.PdfFileName)))
                    {
                        return Content("The Certificate is no longer available to print. Please contact Administrator.");
                    }
                    else
                    {
                        if (cmd == "getfilename")
                        {
                            return Content("~/temp/" + IndivCert.PdfFileName);
                        }
                        else
                        {
                            return Redirect("~/temp/" + IndivCert.PdfFileName);
                        }
                    }
                }
                else
                {
                    return Content("The Certificate is no longer available to print. Please contact Administrator.");
                    //return Redirect("~/Landing/ErrorPage/");
                }
            }

            else
            {
                PdfGradeCertificate IndivCert = new PdfGradeCertificate(certnum);
                if (IndivCert.CustomCertificate != null)
                {
                    IndivCert.ExecuteCertification();
                }
                if (IndivCert.PdfFileName != "")
                {
                    if (!System.IO.File.Exists(Server.MapPath("~/temp/" + IndivCert.PdfFileName)))
                    {
                        return Content("The Certificate is no longer available to print. Please contact Administrator.");
                    }
                    else
                    {
                        if (cmd == "getfilename")
                        {
                            return Content("~/temp/" + IndivCert.PdfFileName);
                        }
                        else
                        {
                            return Redirect("~/temp/" + IndivCert.PdfFileName);
                        }
                    }
                }
                else
                {
                    return Content("The Certificate is no longer available to print. Please contact Administrator.");
                    //return Redirect("~/Landing/ErrorPage/");
                }
                return Redirect("~/temp/" + IndivCert.PdfFileName);
            }

           



            ////For Cert by Survey
            //SurveyFunction surveyfunc = new SurveyFunction();
            //int SurveyID = surveyfunc.GetSurveyId(courseId);
            //Gsmu.Api.Data.Survey.Survey surveyapi = new Gsmu.Api.Data.Survey.Survey(SurveyID);

            //Transcripts tran = new Transcripts();
            //CourseModel model = new CourseModel(courseId);
            //Course_Roster roster = tran.StudentTranscriptedRoster(studentid, courseId);
            //Transcript transcript = tran.StudentTranscriptedCourse(studentid, courseId);
            //string tst = surveyapi.SurveyModel.AfterSurveyCertificate.Replace("~", string.Empty);
            //model.Course.coursecertificate = int.Parse(surveyapi.SurveyModel.AfterSurveyCertificate.Replace("~", string.Empty));
            //PdfGradeCertificate IndivCert = new PdfGradeCertificate(model.Course, roster, transcript);
            //IndivCert.Execute();
        }
        public ActionResult OrderConfirmation(string order, string print, int? sid=0)
        {
            if (Settings.Instance.GetMasterInfo2().HidePaymentInfo == 1)
            {
                ViewBag.HidePaymentInfo = "True";
            }
            else
            {
                ViewBag.HidePaymentInfo = "False";
            }

            var model = new Gsmu.Api.Data.School.CourseRoster.OrderModel(order, sid);
            if (model.SingleRoster == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if ((AuthorizationHelper.CurrentStudentUser != null) || (AuthorizationHelper.CurrentSupervisorUser != null) || (AuthorizationHelper.CurrentAdminUser != null || AuthorizationHelper.CurrentSubAdminUser != null || Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode))
            {
                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    if (model.Student.STUDENTID != AuthorizationHelper.CurrentStudentUser.STUDENTID && model.SingleRoster.EnrollMaster != AuthorizationHelper.CurrentStudentUser.STUDENTID)
                    {

                        throw new UnauthorizedAccessException("The order number provided is not yours.");
                    }
                }

            }
            else
            {
               
                return RedirectToAction("Index", "Home");

            }
            if (print != null)
            {
                if (print.Contains("paypal"))
                {
                    ViewBag.IsPayPalAdvance = true;
                }
            }
            var result = View("OrderConfirmation", print == null ? "_Layout" : "_PrintLayout", model);
            return result;
        }
        [CustomSecurityforCrossSiteForgeryAttributes]
        public string UserCoursesinPDF(string usergroup = "ST",string coursecategory ="enrolled",int sid=0)
        {
            int userid = AuthorizationHelper.CurrentUser.SiteUserId;
            PDFReporting PdfReporting = new PDFReporting();
            string fileName = userid +coursecategory+ "Courses.pdf";
            string fileNameinPath = Server.MapPath("~/Temp/"+fileName);
            if (!Directory.Exists(Server.MapPath("~/Temp" ))){
                Directory.CreateDirectory(Server.MapPath("~/Temp"));
            }
            UserModel ui = new UserModel(usergroup,"view",sid);
            if (coursecategory == "transcripted")
            {
                PdfReporting.GenerateStudentTranscript(fileNameinPath, ui, coursecategory);
            }
            else
            {
            PdfReporting.GenerateStudentRosterReport(fileNameinPath, ui, coursecategory);
            }
            return fileName;
        }


        public string CheckReqMissingFields()
        {
            Gsmu.Api.Data.School.DataLists dlists = new Gsmu.Api.Data.School.DataLists();
            try
            {
                String resultui = dlists.CheckReqMissingFields(AuthorizationHelper.CurrentStudentUser.STUDENTID, "ST");

                return resultui;
            }
            catch (Exception e)
            {
                var resultnullcheck = "";
                if (AuthorizationHelper.CurrentStudentUser == null)
                {
                    resultnullcheck = "Current Login Student is null";
                }
                return (e.Data.Count.ToString() + "::::" + e.InnerException + ":::::::" + e.Message + ";;;;;" + resultnullcheck);
            }

        }
        public ActionResult UserClockhoursPurchaseReceipt(int transcriptid)
        {

                Transcripts trans = new Transcripts();
                ViewBag.TranscriptDetails = trans.GetStudentCourseHoursPurchasedDetails(transcriptid);
                ViewBag.CreditLabel = CourseCreditHelper.GetCreditLabel(CourseCreditType.Custom);
            
            return View();
        }

        public ActionResult OtherUserEnrolledCourses(int start = 0, int limit = 5, int userid = 0, string filter = null, string sort = null)
        {
            if (Gsmu.Api.Authorization.AuthorizationHelper.CurrentStudentUser == null)
            {
                return null;
            }


            var filterResult = ExtJsDataStoreHelper.ParseFilter(filter);
            var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(sort);

            var queryState = new QueryState(start, limit)
            {
                OrderByDirection = sorterResult.Value,
                OrderFieldString = sorterResult.Key,
                Filters = filterResult
            };
            var result = new JsonResult();

            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = UserDashQueries.GetOtherUserEnrolledCourses(queryState);

            return result;
        }
        public ActionResult CheckEmailRestriction(string domain, string email)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            Gsmu.Web.Areas.Adm.Controllers.UserSettingsController admFunction = new Gsmu.Web.Areas.Adm.Controllers.UserSettingsController();
            EmailRestriction jsonObj = admFunction.GetEmailRestrictions();

            var Whitelist = new List<EmailRestrictionData>();
            var Blacklist = new List<EmailRestrictionData>();
            var validemail = false;
            int CntWhitelist = 0;
            int CntBlacklist = 0;
            int CntValidWhitelist = 0;
            int CntInvalidBlacklist = 0;

            //only query the DB when it's set to operate.
            if (jsonObj.OnOff != 0)
            {
                Whitelist = (from n in jsonObj.Data
                             where n.grp.Contains("whitelist")
                             select n).ToList();

                Blacklist = (from n in jsonObj.Data
                             where n.grp.Contains("blacklist")
                             select n).ToList();

                CntWhitelist = Whitelist.Count();

                CntBlacklist = Blacklist.Count();

                CntValidWhitelist = (from n in Whitelist
                                     where n.email.Contains(domain)
                                     select n).Count();

                CntInvalidBlacklist = (from n in Blacklist
                                       where n.email.Contains(domain)
                                       select n).Count();

                validemail = false;

                if (CntWhitelist == 0 && CntBlacklist == 0)
                {
                    validemail = true;
                }

                if (CntWhitelist > 0 && CntBlacklist == 0 && CntValidWhitelist > 0)
                {
                    validemail = true;
                }

                if (CntWhitelist == 0 && CntBlacklist > 0 && CntInvalidBlacklist == 0)
                {
                    validemail = true;
                }

                if (CntWhitelist > 0 && CntBlacklist > 0 && CntValidWhitelist > 0 && CntInvalidBlacklist == 0)
                {
                    validemail = true;
                }
                if (jsonObj.OnOff == 0)
                {
                    validemail = true;
                }
                try
                {
                    if (Settings.Instance.GetFieldMasks("username", "Students").DefaultMaskNumber == 97)
                    {
                        var Context = new SchoolEntities();
                        if (UserDashQueries.ProcessCheckStudentUsernameExist(email))
                        {

                            validemail = false;
                            jsonObj.EmailNotification = "Username already exists";
                        }

                    }
                }
                catch
                {
                    validemail = true;
                }
            }
            else
            {
                validemail = true;
            }

            result.Data = new
            {
                success = true,
                OnOff = jsonObj.OnOff,
                valid = validemail,
                EmailNotification = jsonObj.EmailNotification,
                Whitelist = Whitelist,
                Blacklist = Blacklist,
                CntWhitelist = CntWhitelist,
                CntBlacklist = CntBlacklist,
                CntValidWhitelist = CntValidWhitelist,
                CntInvalidBlacklist = CntInvalidBlacklist
            };
            return result;
        }
        public ActionResult GetCustomRegistrationField()
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = UserDashQueries.GetCustomRegistrationField();
            return result;
        }

        public bool ValidateUrlReferrer()
        {
            if (HttpContext.Request.UrlReferrer.Authority.ToString().ToLower() != Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl.ToLower().Replace("/", "").Replace("https:", ""))
            {
                return false;
            }

            return true;
        }
    }
}


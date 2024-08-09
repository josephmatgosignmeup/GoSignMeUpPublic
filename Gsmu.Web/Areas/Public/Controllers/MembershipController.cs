using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using System.Web.Script.Serialization;
using Gsmu.Api.Data.Survey;

namespace Gsmu.Web.Areas.Public.Controllers
{
    public class MembershipController : Controller
    {
        //
        // GET: /Public/Membership/

        public ActionResult Index()
        {
            if (AuthorizationHelper.CurrentUser.IsLoggedIn)
            {
                return RedirectToAction("Manage");
            }
            ViewBag.CASConfigLoginGsmu = GetCASConfig();
            return RedirectToAction("Login");
        }

        public ActionResult Login(String cmd)
        {




            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err705.");
            }
            ViewBag.CASConfigLoginGsmu = GetCASConfig();
            ViewBag.hide_SupervisorLogin_onCartCheckOut = 0;
            SurveyInfo.Instance.SetSurvey(0, 0, 0);
            if (cmd == "partialview")
            {
                return PartialView();
            }
            else if (cmd == "cartcheckoutlogin")
            {
                ViewBag.hide_InstructorLogin_onCartCheckOut = 1;
                return PartialView();
            }
           
            return View();
        }
        public static string GetCASConfig()
        {
            if((Settings.Instance.GetMasterInfo2().useCASAuth == 1) || (Settings.Instance.GetMasterInfo2().useCASAuth == -1))
            {
                string cas_allow_gsmu_auth = "1";
                string casconfig = Settings.Instance.GetMasterInfo4().CASAuthConfiguration;
                if ((casconfig != "") && (casconfig != null))
                {
                    JavaScriptSerializer j = new JavaScriptSerializer();
                    dynamic settingsconfig = j.Deserialize(casconfig, typeof(object));
                    cas_allow_gsmu_auth = settingsconfig["allowgsmuauth"];
                    return cas_allow_gsmu_auth;
                }
            }
            else
            {
                return "1";
            }
            return "1";
        }

        [HttpPost]
        public ActionResult LoginStudent(string username, string password)
        {

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err706.");
            }
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Student Log In";
                    Audittrail.UserName = username;

                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            var result = new JsonResult();
            try
            {
                var messages = AuthorizationHelper.LoginStudent(username, password);
                var ForceUpdate = Settings.Instance.GetMasterInfo3().ForceAccountUpdate;
                var StrtupPage = Settings.Instance.GetMasterInfo3().StrtupPage;
                String OtherStrtupPage = Settings.Instance.GetMasterInfo3().OtherStrtupPage;
                string studentSessionId = "";
                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    if (AuthorizationHelper.CurrentStudentUser.UserSessionId != null)
                    {
                        studentSessionId = AuthorizationHelper.CurrentStudentUser.UserSessionId.ToString();
                    }
                }

                result.Data = new
                {
                    success = true,
                    ForceUpdate = ForceUpdate,
                    StrtupPage = StrtupPage,
                    OtherStrtupPage = OtherStrtupPage,
                    messages = messages,
                    //studentkey = "guidid : " + studentSessionId,
                    studentkey = "studentguid : " + studentSessionId, //This is the session key for API request.
                    cartItemCount = Gsmu.Api.Commerce.ShoppingCart.CourseShoppingCart.Instance.Count
                };
            }
            catch (Exception e)
            {
                result.Data = new
                {
                    success = false,
                    error = e.Message
                };
            }
            return result;
        }

        [HttpPost]
        public ActionResult LoginSupervisor(string username, string password)
        {

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err707.");
            }
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Supervisor Log In";
                    Audittrail.UserName = username;

                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            var result= new JsonResult();
            try {
                var messages = AuthorizationHelper.LoginSupervisor(username, password);

                result.Data = new
                {
                    success = true,
                    messages = messages
                };

            } catch (Exception e) {
                result.Data = new {
                    success = false,
                    error = e.Message
                };
            }
            return result;
        }

        [HttpPost]
        public ActionResult LoginInstructor(string username, string password)
        {

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err708.");
            }
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off")&& (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Instructor Log In";
                    Audittrail.UserName = username;

                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            var result = new JsonResult();
            try
            {
                var messages = AuthorizationHelper.LoginInstructor(username, password);

                result.Data = new
                {
                    success = true,
                    messages = messages
                };

            }
            catch (Exception e)
            {
                result.Data = new
                {
                    success = false,
                    error = e.Message
                };
            }
            return result;
        }
        [HttpPost]
        public ActionResult LoginAdministrator(string username, string password){

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err710.");
            }
            var result = new JsonResult();
            try
            {
                var messages = AuthorizationHelper.LoginAdministrator(username, password);

                result.Data = new
                {
                    success = true,
                    messages = messages
                };

            }
            catch (Exception e)
            {
                result.Data = new
                {
                    success = false,
                    error = e.Message
                };
            }
            return result;
        }
        public ActionResult Logout()
        {
            JsonResult result = new JsonResult() {
                 JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
            var activeUser = "";
            if (AuthorizationHelper.CurrentSupervisorUser != null)
            {
                activeUser = "supervisor";
                Gsmu.Api.Commerce.ShoppingCart.CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent = 0;
            }
            if (AuthorizationHelper.CurrentInstructorUser != null)
            {
                activeUser = "instructor";
                Gsmu.Api.Commerce.ShoppingCart.CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent = 0;
            }
            var messages = AuthorizationHelper.Logout();
            Session.Clear();
            Session.Abandon();
            System.Web.HttpContext.Current.Session.Timeout = WebConfiguration.LoggedOutSessionTimeout;
            result.Data = new
            {
                success = true,
                messages = messages,
                activeUser = activeUser
            };
            return result;
        }

        public bool ValidateUrlReferrer()
        {

            try
            {
                if(!Gsmu.Api.Data.WebConfiguration.RequiredReferrerCheck)
                {
                    return true;
                }
                if (HttpContext.Request.UrlReferrer.Authority.ToString().ToLower() != Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl.ToLower().Replace("/", "").Replace("https:", ""))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;

using Gsmu.Api.Data;
using Gsmu.Api.Integration.Haiku;
using canvas = Gsmu.Api.Integration.Canvas;
using Gsmu.Api.Authorization;
using Gsmu.Api.Export;
using Gsmu.Api.Web;
using Gsmu.Api.Data.School.CourseRoster;
using System.Collections.Specialized;
using Gsmu.Api.Data.School.CustomTranscriptModel;
using Gsmu.Api.Commerce.ShoppingCart;
using System.Configuration;
using System.Web.Configuration;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Data.School.Attendance;
using Gsmu.Api.Data.School.CourseSettings;
using Gsmu.Service.Interface;
using Gsmu.Service.BusinessLogic.Admin.Reports;
using Gsmu.Service.Models.Admin.Reports;
using Gsmu.Service.Interface.Admin.Reports;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Gsmu.Web.Areas.Adm.Controllers;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Commerce;
using spreedlyGW = Gsmu.Api.Integration.Spreedly;
using System.Web.Script.Serialization;
using Gsmu.Api.Data.ViewModels;
using System.Xml;
using System.Net;
using Gsmu.Api.Integration.Blackboard.API;
using Gsmu.Api.Data.School.User;
using BlackBoardAPI;
using static BlackBoardAPI.BlackBoardAPIModel;

namespace Gsmu.Web.Controllers
{
    public class ApplicationController : Controller
    {
        [HttpPost]
        public ActionResult SaveApplicationUrl(string url)
        {
            //Settings.Instance.SetMasterinfoValue(4, "DotNetSiteRootUrl", url);
            return null;
        }

        public ActionResult CurrentInstructorId()
        {
            return new JsonResult()
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = AuthorizationHelper.CurrentUser.LoggedInUserType == LoggedInUserType.Instructor ? AuthorizationHelper.CurrentUser.SiteUserId : 0
            };
        }

        public ActionResult AdminPing()
        {
            var result = new JavaScriptResult();
            result.Script = Request["callback"] + "();";
            return result;
        }

        public ActionResult IsAdmin()
        {
            return new JsonResult()
            {
                Data = new
                {
                    IsAdmin = Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode
                },
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }


        public ActionResult AdminFunction(string call, int? courseId,string Key="")
        {
            string CallerRequestHash = Request["myHash"];
            string LMSCallHashKey = System.Configuration.ConfigurationManager.AppSettings["LMSCallHashKey"];
            int isCanvasSection = 0;
            if (!string.IsNullOrEmpty(Request["isCanvasSection"]) && Request["isCanvasSection"] == "1")
            {
                isCanvasSection = 1;
            }

            if (string.IsNullOrEmpty(LMSCallHashKey)) { LMSCallHashKey = "Not used"; }
            string pureHash = "haiku" + DateTime.Now.ToString("M/d/yyyy") + ' ' + DateTime.Now.ToString("HH:mm") + ' ' + LMSCallHashKey;
            string ServerHash = Gsmu.Api.Encryption.HmacSha1.Encode("haiku" + DateTime.Now.ToString("M/d/yyyy") + ' ' + DateTime.Now.ToString("HH:mm"), LMSCallHashKey);
            object callResult = null;
            //if ((ServerHash == CallerRequestHash) || AuthorizationHelper.CurrentInstructorUser != null)
            if (Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode || (ServerHash == CallerRequestHash) || AuthorizationHelper.CurrentInstructorUser != null)
            {
                //try
                //{
                Api.Networking.Mail.EmailFunction emailFunction = new Api.Networking.Mail.EmailFunction();
                switch (call)
                {
                    case "haikuClassImport":
                        callResult = HaikuImport.SynchronizeCourse(courseId.Value);
                        break;

                    case "haikuRosterImport":
                        callResult = HaikuImport.SynchronizeRoster(courseId.Value);
                        break;

                    case "haikuCourseExport":
                        callResult = HaikuExport.SynchronizeCourse(courseId.Value).Item1;
                        break;

                    case "haikuCourseRosterExport":
                        callResult = HaikuExport.SynchronizeRoster(courseId.Value);
                        break;

                    case "haikuCourseList":
                        callResult = HaikuImport.ListClasses();
                        break;

                    case "canvasCourseList":
                        NameValueCollection query = new NameValueCollection();
                        if ((!String.IsNullOrEmpty(Request.QueryString["sort"])))
                        {
                            query.Add("sort", Request.QueryString["sort"]);
                        }
                        if ((!String.IsNullOrEmpty(Request.QueryString["page"])))
                        {
                            query.Add("page", Request.QueryString["page"]);
                        }
                        else
                        {
                            query.Add("page", "1");
                        }
                        if ((!String.IsNullOrEmpty(Request.QueryString["filter"])))
                        {
                            var filter = Request.QueryString["filter"];
                            var filterResult = ExtJsDataStoreHelper.ParseFilter(filter);
                            if (filterResult.ContainsKey("keyword"))
                            {
                                if (filterResult["keyword"] != "")
                                {
                                    query.Add("search_term", filterResult["keyword"]);
                                    // query.Add("completed", "false");
                                }
                            }
                            if (filterResult.ContainsKey("published"))
                            {
                                if (filterResult["published"] != "")
                                {
                                    query.Add("published", filterResult["published"]);

                                }
                                else
                                {
                                    query.Add("published", "true");
                                }

                            }

                            else
                            {
                                query.Add("published", "true");
                            }


                        }
                        else
                        {
                            query.Add("published", "true");
                        }

                        var response = canvas.Clients.CourseClient.ListCourses(query);
                        Gsmu.Api.Integration.Canvas.Canvas_Object_result resultdata = new Gsmu.Api.Integration.Canvas.Canvas_Object_result();
                        resultdata.Courses = response.Courses;
                        resultdata.totalCount = response.CourseNoOfpages;
                        callResult = resultdata;
                        break;
                    case "canvasGetCourseSectionsList":
                        var SectionsResponse = canvas.Clients.CourseClient.GetCourseSectionsList(courseId.Value);
                        callResult = SectionsResponse.Sections;

                        break;
                    case "canvasCourseRosterExport":
                        canvas.CanvasExport.ExportCourseWithRoster(courseId.Value);
                        break;
                    case "canvasCancelRoster":
                        string canvasRosterID = Request.QueryString["crid"];
                        canvas.CanvasExport.DeleteEnrollment(courseId.Value, int.Parse(canvasRosterID));
                        break;
                    case "ExportSupervisorStudentRelation2Canvas":
                        string supID = Request["supID"];
                        string studID = Request["studID"];
                        string studCanvasID = Request["studCanvasID"];
                        canvas.CanvasExport.ExportSupervisorStudentRelation2Canvas(null, int.Parse(supID), int.Parse(studID), int.Parse(studCanvasID.ToString()));
                        break;
                    case "AdminRequestUser":
                        if (Request.QueryString["reqType"] == "99")
                        {
                            callResult = canvas.Clients.UserClient.GetUser(courseId.Value);
                        }
                        else if (Request.QueryString["reqType"] == "999")
                        {
                            callResult = canvas.Clients.UserClient.GetSupervisorObservee(courseId.Value);
                        }

                        foreach (var propertyInfo in callResult.GetType().GetProperties())
                        {
                            string resType = propertyInfo.PropertyType.ToString();
                            if (resType == "Gsmu.Api.Integration.Canvas.Entities.User")
                            {
                                var result1 = JsonConvert.SerializeObject(propertyInfo.GetValue(callResult, null));
                                return new ContentResult()
                                {
                                    Content = result1,
                                    ContentType = "application/json"
                                };
                            }
                        }
                        break;
                    case "canvasCourseImport":
                        callResult = canvas.CanvasImport.SynchronizeCourse(courseId.Value, isCanvasSection);
                        break;

                    case "canvasCourseAndEnrollmentImport":
                        callResult = canvas.CanvasImport.SyncronizeCourseAndEnrollment(courseId.Value, isCanvasSection);
                        break;
                    case "canvasGetMainAccount":
                        var accountsResponse = canvas.Clients.AccountClient.GetListMainAccounts;
                        //JToken jObject = JObject.Parse(accountsResponse.Accounts.ToString());
                        dynamic accountObj = JsonConvert.DeserializeObject(SerializationHelper.SerializeEntity(accountsResponse.Accounts));
                        var GotAccountID = "";
                        var GotAccountName = "";
                        foreach (var singleAccount in accountObj)
                        {
                            GotAccountID = singleAccount.id;
                            GotAccountName = singleAccount.name;
                        }

                        callResult = accountsResponse.Accounts;
                        break;
                    case "canvasGetSubAccount":
                        var tempAccountID = 0;
                        var tempRecursivemode = 0;
                        if (Request.QueryString["CanvasSubAccountID"] != "") {
                            tempAccountID = int.Parse(Request.QueryString["CanvasSubAccountID"]);
                        }
                        if (Request.QueryString["RecursiveMode"] == "1")
                        {
                            tempRecursivemode = int.Parse(Request.QueryString["RecursiveMode"]);
                        }
                        var accountsSubResponse = canvas.Clients.AccountClient.GetListSubAccounts(tempAccountID, tempRecursivemode);
                        callResult = accountsSubResponse.Accounts;
                        break;
                    case "canvasPullCourseObj":
                        var gradeObjResponse = canvas.Clients.EnrollmentClient.ListCourseEnrollments(courseId.Value);
                        callResult = gradeObjResponse.Enrollments;
                        break;
                    case "portal-signinsheet":
                        string fileName = "document_" + System.Guid.NewGuid().ToString() + "_Signinsheet.pdf";
                        string fileNameinPath = Server.MapPath("~/Temp/" + fileName);
                        if (!Directory.Exists(Server.MapPath("~/Temp")))
                        {
                            Directory.CreateDirectory(Server.MapPath("~/Temp"));
                        }
                        string cid = Request.QueryString["courseId"];
                        if (cid == null)
                        {
                            cid = courseId.ToString();
                        }
                        PDFSigninSheet.GenerateSigninSheet(fileNameinPath, int.Parse(cid), Request);
                        callResult = fileName;
                        break;

                    case "portal-attendance":
                        return RedirectToAction("PortalAttendance", "Attendance", new { area = "adm", courseId = courseId.Value });

                    case "go-admin":
                        Response.Cache.SetCacheability(HttpCacheability.NoCache);
                        Response.Cache.SetExpires(DateTime.Now.AddSeconds(-1));
                        Response.Cache.SetNoStore();
                        Response.AppendHeader("pragma", "no-cache");
                        return View("AdminPortalLogin");

                    case "portal-revieworder":
                        string useNewReviewOrderView = System.Configuration.ConfigurationManager.AppSettings["UseNewReportView"];
                        if (!string.IsNullOrEmpty(useNewReviewOrderView))
                        {
                            if (bool.Parse(useNewReviewOrderView) == true)
                            {
                                callResult = ReviewOrders.GenerateReviewOrderReport(ReviewOrders.BuildRequestQuery(Request), Server.MapPath("/Temp/"));
                            }
                            else
                            {
                                callResult = ReviewOrders.ReviewOrdersList(ReviewOrders.BuildRequestQuery(Request));
                            }
                        }
                        else
                        {
                            callResult = ReviewOrders.ReviewOrdersList(ReviewOrders.BuildRequestQuery(Request));
                        }
                        break;
                    case "portal-revieworderinprogress":
                        {
                            callResult = ReviewOrders.GenerateReviewOrderInProgressDetails(Request["id"]);
                            break;
                        }
                    case "portal-process-revieworderinprogress":
                        {
                            ReviewOrders.GenerateProcessReviewOrderInProgresstoRoster(Request["id"]);
                            callResult = true;
                            break;
                        }
                    case "portal-classlist":
                        int pstart = 0;
                        int cancel = 0;
                        var sort = "[{'property':'DateTimeAdded','direction':'DESC'}]";
                        if ((!String.IsNullOrEmpty(Request.QueryString["start"])))
                        {
                            pstart = int.Parse(Request.QueryString["start"]);
                        }
                        if ((!String.IsNullOrEmpty(Request.QueryString["cancel"])))
                        {
                            cancel = int.Parse(Request.QueryString["cancel"]);
                        }
                        if (!String.IsNullOrEmpty(Request.QueryString["sort"]))
                        {
                            sort = Request.QueryString["sort"];
                        }
                        if (Request.QueryString["waiting"] == "1")
                        {
                            sort = null;
                        }
                        if (!String.IsNullOrEmpty(Request.QueryString["rosterId"]))
                        {
                            callResult = ClassList.UpdateRosterDetails(int.Parse(Request.QueryString["rosterId"]), Request.QueryString["SpecialNeeds"], Request.QueryString["Notes"], Request.QueryString["Invoice"], Request.QueryString["Invoicedate"], Request.QueryString["records"]);
                        }
                        else
                        {
                            callResult = ClassList.GetClasslist(courseId, pstart, 10, Request.QueryString["filter"], sort, Request.QueryString["waiting"], cancel);
                        }
                        break;

                    case "portal-cancelindividual":
                        {
                            break;
                        }
                    case "portal-rostereport-export":

                        callResult = Gsmu.Api.Data.School.CourseRoster.RosterReport.GenerateRosterReport(Request, true);
                        break;
                    case "portal-rostereport":
                        string useNewView = System.Configuration.ConfigurationManager.AppSettings["UseNewReportView"];
                        if (!string.IsNullOrEmpty(useNewView))
                        {
                            if (bool.Parse(useNewView) == true)
                            {
                                IRosterReport RosterReport = new Gsmu.Service.BusinessLogic.Admin.Reports.RosterReport();
                                callResult = RosterReport.GenerateRosterReport(Gsmu.Api.Data.School.CourseRoster.RosterReport.BuildRequestQuery(Request));
                            }
                            else
                            {
                                callResult = Gsmu.Api.Data.School.CourseRoster.RosterReport.GenerateRosterReport(Request, false);
                            }

                        }
                        else
                        {
                            callResult = Gsmu.Api.Data.School.CourseRoster.RosterReport.GenerateRosterReport(Request, false);
                        }
                        break;
                    case "portal-canvassectioncourselist":

                        Gsmu.Service.Interface.Courses.ICourseGrid CourseGrid = new Gsmu.Service.BusinessLogic.Courses.CourseGridManager();

                        callResult = CourseGrid.GetAdminCanvasSectionCourses(int.Parse(Request["canvasId"]));
                        break;
                    case "portal-deactivatestudent":
                        //try
                        //{
                        // Gsmu.Service.Interface.Students.IStudentManager studentManagerA = new Gsmu.Service.BusinessLogic.Students.StudentManager();
                        // studentManagerA.DeactivateStudents(int.Parse(Request["userid"].Replace("ST", "")));

                        // Gsmu.Service.Interface.Courses.ICourseGrid studentManager = new Gsmu.Service.BusinessLogic.Courses.CourseGridManager();
                        //studentManager.DeactivateStudents(119844);
                        UserModel userModel = new UserModel();

                        callResult = userModel.ActivateOrDeactivateUserInBB(int.Parse(Request["userid"].Replace("ST", "")), "No");
                        //}
                        //catch
                        //{
                        //}

                        break;
                    case "portal-reactivatestudent":
                        //try
                        //{
                        //Gsmu.Service.Interface.Students.IStudentManager studentManager = new Gsmu.Service.BusinessLogic.Students.StudentManager();
                        //studentManager.DeactivateStudents(int.Parse(Request["userid"].Replace("ST", "")));

                        Gsmu.Service.Interface.Courses.ICourseGrid reactstudentManager = new Gsmu.Service.BusinessLogic.Courses.CourseGridManager();
                        reactstudentManager.ReactivateStudents(int.Parse(Request["userid"].Replace("ST", "")));
                        UserModel userModelA = new UserModel();
                        callResult = userModelA.ActivateOrDeactivateUserInBB(int.Parse(Request["userid"].Replace("ST", "")), "Yes");
                        //}
                        //catch
                        //{
                        //}

                        break;
                    case "portal-credithoursreport":
                        CreditHoursPurchaseParamenterModel CreditHoursPurchaseParamenterModel = new CreditHoursPurchaseParamenterModel();
                        int pagestart = 0;
                        if ((!String.IsNullOrEmpty(Request.QueryString["page"])))
                        {
                            CreditHoursPurchaseParamenterModel.pagestart = int.Parse(Request.QueryString["page"]);
                        }
                        var datafilter = Request.QueryString["filter"];
                        var limit = int.Parse(Request.QueryString["limit"]);
                        var DatafilterResult = ExtJsDataStoreHelper.ParseFilter(datafilter);
                        var sorterResult = ExtJsDataStoreHelper.ParseSorterUnique(null);
                        var queryState = new QueryState(pagestart, limit)
                        {
                            OrderByDirection = sorterResult.Value,
                            OrderFieldString = sorterResult.Key,
                            Filters = DatafilterResult
                        };
                        if (queryState.Filters != null)
                        {
                            if (queryState.Filters.ContainsKey("search_keyword"))
                            {
                                CreditHoursPurchaseParamenterModel.Keyword = queryState.Filters["search_keyword"];
                            }
                            if (queryState.Filters.ContainsKey("studdistrict"))
                            {
                                CreditHoursPurchaseParamenterModel.studentDistrict = queryState.Filters["studdistrict"];
                            }
                            if (queryState.Filters.ContainsKey("coursefromdate"))
                            {
                                CreditHoursPurchaseParamenterModel.StartDate = queryState.Filters["coursefromdate"];
                            }
                            else
                            {
                                CreditHoursPurchaseParamenterModel.StartDate = DateTime.Now.AddDays(-30).ToString();
                            }
                            if (queryState.Filters.ContainsKey("coursetodate"))
                            {
                                CreditHoursPurchaseParamenterModel.EndDate = queryState.Filters["coursetodate"];
                            }
                            else
                            {
                                CreditHoursPurchaseParamenterModel.EndDate = DateTime.Now.AddDays(30).ToString();
                            }
                            if (queryState.Filters.ContainsKey("datefilter"))
                            {
                                CreditHoursPurchaseParamenterModel.datefilter = queryState.Filters["datefilter"];
                            }
                            else
                            {
                                CreditHoursPurchaseParamenterModel.datefilter = "DatePurchase";
                            }
                        }
                        else
                        {
                            CreditHoursPurchaseParamenterModel.Keyword = "";
                            CreditHoursPurchaseParamenterModel.datefilter = "DatePurchase";
                            CreditHoursPurchaseParamenterModel.StartDate = DateTime.Now.AddDays(-30).ToString();
                            CreditHoursPurchaseParamenterModel.EndDate = DateTime.Now.AddDays(30).ToString();
                        }

                        ICreditHoursPurchase CreditHoursPurchase = new CreditHoursPurchaseReport();
                        callResult = CreditHoursPurchase.GetStudentCourseHoursPurchased(CreditHoursPurchaseParamenterModel, queryState);
                        break;

                    case "instructor-sendsurvey":
                        Gsmu.Web.Areas.Public.Controllers.InstructorController instructorController = new Areas.Public.Controllers.InstructorController();
                        callResult = instructorController.SendStudentSurvey(courseId);
                        break;
                    case "get-CustomCourseField5-MasterCourseIDList":
                        string keyword = "";
                        if ((!String.IsNullOrEmpty(Request.QueryString["filter"])))
                        {
                            keyword = Request.QueryString["filter"];
                        }
                        //callResult = CustomCourseField5_MasterCourseIDList.GetAllMasterCourseId(keyword);
                        break;
                    case "update-CustomCourseField5-MasterCourseIDList":
                        string old_value = "";
                        string new_value = "";
                        if ((!String.IsNullOrEmpty(Request.QueryString["records"])))
                        {
                            new_value = Request.QueryString["records"];
                        }
                        if ((!String.IsNullOrEmpty(Request.QueryString["StringMasterCourseId_value"])))
                        {
                            old_value = Request.QueryString["StringMasterCourseId_value"];
                        }
                        //callResult = CustomCourseField5_MasterCourseIDList.UpdateMasterCourseId(old_value, new_value.Replace("StringMasterCourseId=",""));
                        break;
                    case "delete-CustomCourseField5-MasterCourseIDList":
                        string old_value_ = "";
                        if ((!String.IsNullOrEmpty(Request.QueryString["records"])))
                        {
                            new_value = Request.QueryString["records"];
                        }
                        if ((!String.IsNullOrEmpty(Request.QueryString["StringMasterCourseId_value"])))
                        {
                            old_value_ = Request.QueryString["StringMasterCourseId_value"];
                        }
                        callResult = CustomCourseField5_MasterCourseIDList.UpdateMasterCourseId(old_value_, "");
                        break;
                    case "SpreedlyViewAvailbleGateway":
                        StreamReader gatewayList = new StreamReader(spreedlyGW.SpreedlyHelper.SpreedlyGetGatewayList("", false), System.Text.Encoding.UTF8);
                        string jsonGatewayList = gatewayList.ReadToEnd();
                        if (jsonGatewayList != null)
                        {
                            JavaScriptSerializer j = new JavaScriptSerializer();
                            string gatewayObject = j.Serialize(jsonGatewayList);
                            callResult = gatewayObject;
                        }
                        break;
                    case "SpreedlyAddGateway":
                        string SpreedlyNewGateWayToken = "No Post Data";
                        if (Request.QueryString["gwrequest"] != "")
                        {
                            SpreedlyNewGateWayToken = spreedlyGW.SpreedlyHelper.SpreedlyAddGateway(Request.QueryString["gwrequest"]);
                        }
                        callResult = SpreedlyNewGateWayToken;
                        break;
                    case "portal-attendance-report":
                        callResult = AttendanceModel.AddAttendanceReport(Request, false);
                        break;

                    case "portal-attendance-report-export":
                        callResult = AttendanceModel.AddAttendanceReport(Request, true);
                        break;
                    //paypal refund triggered from admin side - partial or full
                    case "PPcbref":
                        CreditCardPayments payment = new CreditCardPayments();
                        string PPRosterid = "";
                        string PPAmount = "";
                        string responseMessage = "";
                        bool success = false;
                        if ((!String.IsNullOrEmpty(Request.QueryString["PPOrdernumber"])) && Request.QueryString["PPOrdernumber"].Length <= 15)
                        {
                            if ((!String.IsNullOrEmpty(Request.QueryString["PPRosterid"])))
                            {
                                PPRosterid = Request.QueryString["PPRosterid"];
                            }
                            else
                            {
                                PPRosterid = "0";
                            }
                            if ((!String.IsNullOrEmpty(Request.QueryString["PPAmount"])))
                            {
                                PPAmount = Request.QueryString["PPAmount"];
                            }
                            else
                            {
                                PPAmount = "0.00";
                            }
                            responseMessage = payment.RefundPaypal(0, 0, Request.QueryString["PPOrdernumber"], int.Parse(PPRosterid), double.Parse(PPAmount));
                            success = responseMessage.ToLower() == "approved";
                        }
                        dynamic dynPPRefResponse = new System.Dynamic.ExpandoObject();
                        dynPPRefResponse.Success = success;
                        dynPPRefResponse.ResponseMessage = responseMessage;
                        return new JsonResult()
                        {
                            Data = Newtonsoft.Json.JsonConvert.SerializeObject(dynPPRefResponse),
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    case "portal-coupons-report":
                        callResult = CouponsModel.GenerateCouponReport(Request, false);
                        break;

                    case "portal-coupons-options":
                        callResult = CouponsModel.GetCouponOptions();
                        break;

                    case "update-coupons-options":

                        callResult = CouponsModel.UpdateCouponOptions(Request);
                        break;

                    case "delete-coupons":
                        callResult = CouponsModel.DeleteCoupon(Request);
                        break;

                    case "portal-coupons-course":
                        callResult = CouponsModel.GenerateCouponCourseList(Request);
                        break;

                    case "portal-assigned-coupons-course":
                        callResult = CouponsModel.GenerateAssignedCouponCourseList(Request);
                        break;

                    case "update-coupons":
                        callResult = CouponsModel.UpdateCoupon(Request);
                        break;

                    case "portal-course-assigned-coupons":
                        callResult = CouponsModel.GetCourseAssignedCouponList(Request);
                        break;

                    case "remove-assign-coupon-to-course":
                        callResult = CouponsModel.DeleteAssignCouponToCourse(Request);
                        break;

                    case "assign-coupon-to-course":
                        callResult = CouponsModel.AssignCouponToCourse(Request);
                        break;

                    case "portal-getcouponlist":
                        callResult = CouponsModel.GetCouponList(Request);
                        break;
                    case "portal-course-dashboard-bundle":
                        return View("~/Areas/Adm/Views/Widgets/CourseBundle.cshtml");
                    case "portal-course-dashboard-bundle-data":
                        var course = new CourseModel(courseId.Value);
                        return new JsonResult()
                        {
                            Data = course.BundledCourses.Select(bc => new CourseBundleViewModel() {
                                CourseID = bc.COURSEID,
                                CourseName = bc.COURSENAME,
                                CourseNumber = bc.COURSENUM
                            }).ToList(),
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    case "course-max-enrollchange":
                        dynamic dynResponse = new System.Dynamic.ExpandoObject();
                        var courseid = Request.QueryString["courseid"];

                        if ((!string.IsNullOrEmpty(courseid)))
                        {
                            if (WebConfiguration.EnrollToWaitList)
                            {
                                Gsmu.Api.Data.School.Student.EnrollmentFunction enrollmentFunction = new Gsmu.Api.Data.School.Student.EnrollmentFunction();
                                int movedRosterId = enrollmentFunction.PopulateWaitingPeople(int.Parse(courseid), true);
                                dynResponse.Success = true;
                                dynResponse.Moved = true;
                                dynResponse.MovedRosterId = movedRosterId;
                            }
                            else
                            {
                                dynResponse.Success = true;
                                dynResponse.Moved = false;
                                dynResponse.MovedRosterId = 0;
                            }
                            return new JsonResult()
                            {
                                Data = Newtonsoft.Json.JsonConvert.SerializeObject(dynResponse),
                                JsonRequestBehavior = JsonRequestBehavior.AllowGet
                            };
                        }
                        break;
                    case "get-supervisor-emails-by-student":
                        dynamic supervisorResponse = new System.Dynamic.ExpandoObject();
                        var studentId = Request.QueryString["studentId"];
                        string supervisorEmails = string.Empty;
                        if ((!string.IsNullOrEmpty(studentId)))
                        {
                            supervisorEmails = emailFunction.GetStudentSupervisorEmails(int.Parse(studentId), "enroll");
                            supervisorResponse.Success = true;
                            supervisorResponse.SupervisorEmails = supervisorEmails;
                        }
                        return new JsonResult()
                        {
                            Data = Newtonsoft.Json.JsonConvert.SerializeObject(supervisorResponse),
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                        };
                    case "getReminderICSTemplate":
                        int remindercourseid = courseId ?? 0;
                        if (remindercourseid != 0)
                        {
                            List<string> fileNamesCreated = emailFunction.MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, ".vbs", remindercourseid, 1);
                            return new JsonResult()
                            {
                                Data = new
                                {
                                    Status = "Success",
                                    IcsFileNames = fileNamesCreated
                                },
                                JsonRequestBehavior = JsonRequestBehavior.AllowGet
                            };
                        }

                        break;
                    case "get-bb-api-courses":
                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            //BBToken BBToken = new BBToken();
                            //BBToken = handelr.GenerateAccessToken(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl);
                            //var jsonToken = new JavaScriptSerializer().Serialize(BBToken);
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            //  BBEnrollment myEnrollmentData = new BBEnrollment();
                            // myEnrollmentData.courseRoleId = "Student";
                            //  var Enrollment =    handelr.CreateNewEnrollment(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, myEnrollmentData, "71599ad1c93942b29d8bcafd6e86f78d", "5ec540cccf5e4e53a914da2af7dbc208", "uuid", "uuid", "", jsonToken);

                            var bbcourses = handelr.GetCourseDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", "", jsonToken);
                            dynamic json = JsonConvert.DeserializeObject(bbcourses);

                            callResult = json;
                        }
                        break;
                    case "verify-bb=courseid-exist":

                        
                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
                            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
                            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
                            //BBToken BBToken = new BBToken();
                            //BBToken = handelr.GenerateAccessToken(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl);
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            //var jsonToken = new JavaScriptSerializer().Serialize(BBToken);
                            var bbcourses = handelr.GetCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, Key, "courseId", "", jsonToken);

                            dynamic json = JsonConvert.DeserializeObject(bbcourses);

                            if (json != null)
                            {

                                BBCourse obj_course = JsonConvert.DeserializeObject<BBCourse>(bbcourses);
                                var datasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, obj_course.dataSourceId, "dsk", "", jsonToken);
                                datasource datasource = JsonConvert.DeserializeObject<datasource>(datasourceKeyDetails);
                                string globaldskDesc = "";
                                string globaldskName = "";
                                string globaldskId = "";
                                if (obj_course.dataSourceId != Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk)
                                {
                                    var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk, "dsk", "", jsonToken);
                                    datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                    if (globaldatasource != null)
                                    {
                                        globaldskDesc = globaldatasource.description;
                                        globaldskName = globaldatasource.externalId;
                                        globaldskId = globaldatasource.id;
                                    }
                                }
                                if(globaldskName == "")
                                {
                                    globaldskName = Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk;
                                }
                                callResult = obj_course.uuid + "|" + obj_course.name +"|"+ obj_course.dataSourceId + "|" + datasource.externalId + "|" + datasource.description + "|" + globaldskId + "|" + globaldskName + "|" + globaldskDesc;
                            }
                            else
                            {
                                callResult = null;
                            }
                        }
                        break;
                    case "get-bb-course-details":


                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            var bbcourses = handelr.GetCourseDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, Key, "courseId", "", jsonToken);
                            dynamic json = JsonConvert.DeserializeObject(bbcourses);

                            if (json != null)
                            {
                                callResult = json;
                            }
                            else
                            {
                                callResult = null;
                            }
                        }
                        break;
                    case "create-new-bb-course":
                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            var _course = new CourseModel(courseId.Value);
                            if (_course.Course.blackboard_api_uuid == "" || _course.Course.blackboard_api_uuid == null)
                            {
                                BBCourse NewBBcourse = new BBCourse();
                                using (var db = new SchoolEntities())
                                {

                                    NewBBcourse.name = _course.Course.COURSENAME;
                                    NewBBcourse.externalId = _course.Course.CustomCourseField1.ToString()+ _course.Course.COURSEID.ToString();
                                    NewBBcourse.courseId = _course.Course.CustomCourseField1.ToString();
                                    NewBBcourse.description = _course.Course.DESCRIPTION;
                                    NewBBcourse.ultraStatus = "Classic";
                                    NewBBcourse.availability = new availability();
                                    NewBBcourse.availability.available = "Yes";
                                    NewBBcourse.dataSourceId = "_2_1";

                                    BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                                    var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                                    var bbcourse = handelr.AddNewBlackBoardCourse(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, NewBBcourse, "", jsonToken, "");

                                    var gsmu_course_updated = (from gsmucourse in db.Courses where gsmucourse.COURSEID == _course.Course.COURSEID select gsmucourse).FirstOrDefault();
                                    if (gsmu_course_updated!=null) {
                                        gsmu_course_updated.blackboard_api_uuid = bbcourse.uuid;

                                        db.Entry(gsmu_course_updated).State = System.Data.Entity.EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }
                        break;
                    case "sync-bb-api-grades":
                        
                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
                            var _course = new CourseModel(courseId.Value);
                            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
                            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
                            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
                            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            string curCourse_identify = "";
                            string curCourse_id_field = "";
                            if (!string.IsNullOrEmpty(_course.Course.blackboard_api_uuid)) {
                                curCourse_identify = _course.Course.blackboard_api_uuid;
                                curCourse_id_field = "uuid";
                            } else
                            {
                                curCourse_identify = _course.Course.CustomCourseField1;
                                curCourse_id_field = "courseId";
                            }
                            //looking for final column only
                            int GetFinalColumn = 1;
                            var bbcolumns = handelr.GetCourseGradeColumnDetails(BB_sec_key, BB_app_key, "", bb_connection_url, curCourse_identify, curCourse_id_field, GetFinalColumn, "", jsonToken);
                            dynamic jsonColumns = JsonConvert.DeserializeObject(bbcolumns);
                            dynamic jsonGrade = null;
                            string columnId = "";
                            var gradeholder = "";
                            string FinalGradeColumn = Settings.Instance.GetMasterInfo3().BlackboardGradeCenterColumnName.ToLower();

                            if (jsonColumns != null)
                            { 
                                try
                                {
                                    //if (columns["externalGrade"] != null )
                                    string GradeBookFinalColumn = jsonColumns["name"].ToString().ToLower();
                                    if (GradeBookFinalColumn == FinalGradeColumn)
                                    {
                                        if (TurnOnDebugTracingMode != null)
                                        {
                                            if (TurnOnDebugTracingMode.ToLower() == "on")
                                            {
                                                Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                                                Audittrail.TableName = "Debug";
                                                Audittrail.AuditDate = DateTime.Now;
                                                Audittrail.RoutineName = "SyncGrade--Admin";
                                                Audittrail.UserName = "Grade Routine";
                                                Audittrail.DetailDescription = "1a) Final Column Found: " + GradeBookFinalColumn;
                                                try
                                                {
                                                    Audittrail.AuditAction = "Info json " + jsonColumns;
                                                }
                                                catch { }
                                                Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                                                LogManager.LogSiteActivity(Audittrail);
                                            }
                                        }
                                        using (var db1= new SchoolEntities())
                                        {
                                            db1.Configuration.LazyLoadingEnabled = false;
                                            db1.Configuration.ProxyCreationEnabled = false;
                                            db1.Configuration.AutoDetectChangesEnabled = false;
                                            var rosterDetails = (from roster in db1.Course_Rosters where roster.ATTENDED == 0 && roster.COURSEID == courseId.Value && roster.PaidInFull != 0 && roster.Cancel == 0 && (roster.canvas_skip == 0 || roster.canvas_skip == null) select roster).ToList();
                                            columnId = jsonColumns["id"];
                                            Student stud = new Student();
                                            foreach (var roster in rosterDetails)
                                            {
                                                stud = (from _student in db1.Students where _student.STUDENTID == roster.STUDENTID.Value && _student.InActive == 0 select _student).FirstOrDefault();
                                                string studIdentity = "";
                                                string studIdentityField = "";
                                                string gsmustudentid = stud.STUDENTID.ToString();
                                                if (string.IsNullOrEmpty(stud.Blackboard_user_UUID))
                                                {
                                                    studIdentity = stud.USERNAME;
                                                    studIdentityField = "userName";
                                                }
                                                else
                                                {
                                                    studIdentity = stud.Blackboard_user_UUID;
                                                    studIdentityField = "uuid";
                                                }
                                                string courseIdentity = "";
                                                string courseIdentityField = "";
                                                if (string.IsNullOrEmpty(_course.Course.blackboard_api_uuid))
                                                {
                                                    courseIdentity = _course.Course.CustomCourseField1;
                                                    courseIdentityField = "courseId";
                                                }
                                                else
                                                {
                                                    courseIdentity = _course.Course.blackboard_api_uuid;
                                                    courseIdentityField = "uuid";
                                                }
                                                if (stud != null) {
                                                    gradeholder = handelr.GetCourseGradeValue(BB_sec_key, BB_app_key, "", bb_connection_url, courseIdentity, courseIdentityField, columnId, studIdentity, studIdentityField, "", jsonToken);
                                                    jsonGrade = JsonConvert.DeserializeObject(gradeholder);

                                                    //grade from  BB is available 
                                                    if (jsonGrade != null)
                                                    {
                                                        if (TurnOnDebugTracingMode != null)
                                                        {
                                                            if (TurnOnDebugTracingMode.ToLower() == "on")
                                                            {
                                                                Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                                                                Audittrail.TableName = "Debug";
                                                                Audittrail.AuditDate = DateTime.Now;
                                                                Audittrail.RoutineName = "SyncGrade--Admin";
                                                                Audittrail.UserName = "Grade Routine";
                                                                Audittrail.DetailDescription = "2a) Found Grade json: " + jsonGrade;
                                                                Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                                                                LogManager.LogSiteActivity(Audittrail);
                                                            }
                                                        }
                                                        string myBBGrade = "";
                                                        float myBBScore = 0;
                                                        float transid;
                                                        var finalBBGradeScore = "";
                                                        string finalBBGradeScoreType = "";
                                                        //if (finalBBGradeScoreType != null)
                                                        if (jsonGrade.ContainsKey("displayGrade") == true)
                                                        {
                                                            finalBBGradeScoreType = jsonGrade["displayGrade"]["scaleType"].ToString();
                                                            //organize course type
                                                            if (finalBBGradeScoreType == "Score")
                                                            {
                                                                finalBBGradeScore = jsonGrade["displayGrade"]["score"].ToString("0.00");
                                                                var isNumericValue = float.TryParse(finalBBGradeScore, out transid);
                                                                if (isNumericValue)
                                                                {
                                                                    //foundPassingGrade = 0;
                                                                }
                                                            }
                                                            else if (finalBBGradeScoreType == "Percent")
                                                            {
                                                                finalBBGradeScore = jsonGrade["displayGrade"]["score"].ToString();
                                                            }
                                                            else if (finalBBGradeScoreType == "Letter" || finalBBGradeScoreType == "CompleteIncomplete")
                                                            {
                                                                finalBBGradeScore = jsonGrade["displayGrade"]["text"].ToString();
                                                            }
                                                            roster.StudentGrade = finalBBGradeScore.ToString();
                                                            roster.bb_graded_date = DateTime.Now.Date;
                                                            int foundPassingGrade = 0;

                                                            if (TurnOnDebugTracingMode != null)
                                                            {
                                                                if (TurnOnDebugTracingMode.ToLower() == "on")
                                                                {
                                                                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail1 = new Gsmu.Api.Data.School.Entities.AuditTrail();
                                                                    Audittrail1.TableName = "Debug";
                                                                    Audittrail1.AuditDate = DateTime.Now;
                                                                    Audittrail1.RoutineName = "SyncGrade--Admin";
                                                                    Audittrail1.UserName = "Grade Routine";
                                                                    Audittrail1.DetailDescription = "3b) Grade Type: " + finalBBGradeScoreType;
                                                                    try
                                                                    {
                                                                        Audittrail1.AuditAction = "Final Grade " + finalBBGradeScore;
                                                                    }
                                                                    catch { }
                                                                    Gsmu.Api.Logging.LogManagerDispossable LogManager1 = new Api.Logging.LogManagerDispossable();
                                                                    LogManager1.LogSiteActivity(Audittrail1);
                                                                }
                                                            }

                                                            //cycle through declared passing grade
                                                            foreach (var grade_value in Settings.Instance.GetMasterInfo3().BlackboardGradeCenterColumnValue.Split('@'))
                                                            {
                                                                if (finalBBGradeScore == grade_value && foundPassingGrade == 0)
                                                                {
                                                                    /*var coursedates = (from course_date in db.Course_Times where course_date.COURSEID == roster.COURSEID select course_date).ToList();
                                                                    var attendancedate = (from attendance_date in db.AttendanceDetails where attendance_date.RosterId == roster.RosterID select attendance_date).ToList();
                                                                    AttendanceDetail AttendanceDetail = new AttendanceDetail();
                                                                    foreach (var coursedate in coursedates)
                                                                    {
                                                                        if (attendancedate.Count == 0)
                                                                        {
                                                                            AttendanceDetail = new AttendanceDetail();
                                                                            AttendanceDetail.RosterId = roster.RosterID;
                                                                            AttendanceDetail.CourseID = roster.COURSEID.Value;
                                                                            AttendanceDetail.CourseDate = coursedate.COURSEDATE.Value;
                                                                            AttendanceDetail.Attended = 1;
                                                                            db.AttendanceDetails.Add(AttendanceDetail);
                                                                        }
                                                                        else
                                                                        {
                                                                            foreach(var att in attendancedate)
                                                                            {
                                                                                att.Attended = 1;
                                                                            }
                                                                        }
                                                                    }*/
                                                                    roster.ATTENDED = -1;
                                                                    roster.canvas_skip = 99; // for bb need to process survey/cert
                                                                    roster.DIDNTATTEND = 0;

                                                                    //insert into grade log
                                                                    StudentsGradeLog myBBStudGrade = new StudentsGradeLog();
                                                                    myBBStudGrade.bbcoursename = _course.Course.COURSENAME;
                                                                    myBBStudGrade.bbcoursenum = _course.Course.COURSENUM; //curCourse_identify;
                                                                    myBBStudGrade.bbusername = stud.USERNAME; //studIdentity;
                                                                    myBBStudGrade.gsmucourseid = courseId.Value.ToString();
                                                                    myBBStudGrade.gsmustudentid = roster.STUDENTID.Value.ToString();
                                                                    myBBStudGrade.bbgrade = finalBBGradeScore.ToString();
                                                                    myBBStudGrade.dateadded = DateTime.Now;
                                                                    db1.StudentsGradeLogs.Add(myBBStudGrade);

                                                                    foundPassingGrade = 1;
                                                                } //matching grade with declared passing grade
                                                            } //cycle through all declared passing grade
                                                            db1.Entry(roster).State = System.Data.Entity.EntityState.Modified;
                                                            Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                                                            Audittrail.TableName = Request.UserHostName;
                                                            Audittrail.DetailDescription = "Blakcboard - Grade Pull API - ColID: " + columnId;
                                                            Audittrail.AuditDate = DateTime.Now;
                                                            Audittrail.StudentID = int.Parse(gsmustudentid);
                                                            Audittrail.CourseID = courseId.Value;
                                                            Audittrail.RoutineName = "SyncGrade--Admin";
                                                            Audittrail.UserName = "Grade Routine";
                                                            Audittrail.AuditAction = "info found BB Grade Record: " + gradeholder.ToString();
                                                            Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                                                            LogManager.LogSiteActivity(Audittrail);
                                                        } //finalbbgrade ScoreType

                                                    }
                                                    db1.SaveChanges();
                                                } //stud != null
                                            } //rosterDetails
                                        } //SchoolEntities
                                    } //GradeBookFinalColumn
                                }
                                catch(Exception e) {
                                    string errMsg = " - " + e.Message;
                                }              
                            } //jsonColumns
                            callResult = gradeholder;
                        }
                        break;

                    case "get-bb-api-system-version":
                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                        {
                            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                            var bbcourses = handelr.GetBBAPISystemVersion(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", "", jsonToken);

                            callResult ="Major="+ bbcourses.learn.major+" Minor="+ bbcourses.learn.minor;
                        }
                        break;
                    case "roster_individual_gsmu_to_bb":
                        {
                            //********** WARNING
                            //**********courseId.Value here is actually RosterID
                            //********** WARNING
                            //basic call: /application/adminfunction?callback=jsonp1704915394366&call=roster_individual_gsmu_to_bb&courseId=<roster ID>
                            using (var db = new SchoolEntities())
                            {
                                Api.Data.School.Student.EnrollmentFunction enrollmentFunction = new Api.Data.School.Student.EnrollmentFunction();
                                var rosterDetails = (from roster in db.Course_Rosters where roster.RosterID == courseId.Value && roster.PaidInFull != 0 && roster.Cancel == 0 && roster.blackboard_roster_id == null select roster).ToList();
                                callResult = callResult + "<br />Initiating Manual Blackboard Enrollment.";
                                //Student stud = new Student();
                                foreach (var roster in rosterDetails)
                                {
                                    var _course = new CourseModel(roster.COURSEID.Value);
                                    if (_course != null && _course.Course.blackboard_api_uuid != null)
                                    {
                                        callResult = callResult + "<br />Processing enrollments." + roster.RosterID;
                                        //stud = (from _student in db.Students where _student.STUDENTID == roster.STUDENTID.Value select _student).FirstOrDefault();
                                        enrollmentFunction.EnrollUserToBlackBoardApi(roster.COURSEID.Value, roster.STUDENTID.Value, 0, "student", roster.RosterID);
                                    }
                                    else
                                    {
                                        callResult = callResult + "<br />Course of the enrollment is not in Blackboard.";
                                    }
                                }           
                            }
                            break;
                        }

                    case "roster_gsmu_to_bb":
                        {
                            // FYI
                            //this is a fail-safe function to pickup all enrollments from admin/ftp/webservice import or classic/webservice enrollments
                            ////basic call: /application/adminfunction?callback=jsonp1704915394366&call=roster_gsmu_to_bb
                            using (var db = new SchoolEntities())
                            {
                                Api.Data.School.Student.EnrollmentFunction enrollmentFunction = new Api.Data.School.Student.EnrollmentFunction();
                                var course_ids = (from c in db.Courses where c.BBCourseCloned == 1 && c.blackboard_api_uuid != null select c.COURSEID).ToList();
                                var baselineDate = DateTime.Now.AddHours(-48);
                                callResult = callResult + "<br />Initiating Manual Blackboard all course Enrollment.";
                                foreach (var _cid in course_ids)
                                {                                    
                                    //var roster_ids = (from roster in db.Course_Rosters where roster.COURSEID == _cid select roster.STUDENTID).ToList();

                                    var roster_ids = (from roster in db.Course_Rosters where roster.DATEADDED >= baselineDate && roster.COURSEID == _cid && roster.PaidInFull != 0 && roster.Cancel == 0 && roster.blackboard_roster_id == null select roster).ToList();
                                    foreach (var roster in roster_ids)
                                    {
                                        callResult = callResult + "<br />Processing courses: " + _cid;
                                        enrollmentFunction.EnrollUserToBlackBoardApi(_cid, roster.STUDENTID.Value, 0, "student", roster.RosterID);
                                    }
                                }
                            }
                            break;
                        }
                    case "update-bb-course-datasourceid":
                        {
                            if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                            {
                                BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                                string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
                                string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
                                string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
                                var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                                var bbcourses = handelr.GetCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, Key, "courseId", "", jsonToken);
                                dynamic json = JsonConvert.DeserializeObject(bbcourses);

                                if (json != null)
                                {
                                    BBCourse obj_course = JsonConvert.DeserializeObject<BBCourse>(bbcourses);

                                    if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != "" && Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != null)
                                    {
                                        string tempDSK = Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk;
                                        if (tempDSK.IndexOf("_") < 0)
                                        {
                                            var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, tempDSK, "dsk", "", jsonToken);
                                            datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                            string actualDSK = globaldatasource.id;

                                            obj_course.dataSourceId = actualDSK;
                                        }
                                        else
                                        {
                                            obj_course.dataSourceId = tempDSK;
                                        }
                                    }

                                    var updated_course = handelr.UpdateBlackbooardCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, obj_course, Key, "", jsonToken);

                                    callResult = updated_course;
                                }
                                else
                                {
                                    callResult = null;
                                }
                            }
                                break;
                        }
                }

                //}
                //catch (Exception e)
                //{
                //    string ErrorMessage = e.ToString();
                //    var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
                //    if (TurnOnDebugTracingMode != null)
                //    {
                //        if (TurnOnDebugTracingMode.ToLower() == "off")
                //        {
                //            ErrorMessage = "An error has occurred. Please try again or contact administrator. MErr100";
                //        }
                //    }
                //    if (call.StartsWith("haiku"))
                //    {
                //        var response = new Response();
                //        response.Exceptions.Add(e);
                //        callResult = response;
                //    }
                //    else
                //    {

                //        callResult = new
                //        {
                //            Error = ErrorMessage
                //        };
                //    }
                //}
            }
            var result = new JavaScriptResult();
            string arguments = SerializationHelper.SerializeEntity(callResult);
            if (call.ToLower() == "canvasgetcoursesectionslist")
            {
                result.Script = arguments;
            }
            else
            {
                result.Script = Request["callback"] + "(" + arguments + ");";
            }
            return result;
        }


        public ActionResult AdminFunctionTEST(string call, int? courseId)
        {
            string CallerRequestHash = Request["myHash"];
            string LMSCallHashKey = System.Configuration.ConfigurationManager.AppSettings["LMSCallHashKey"];
            int isCanvasSection = 0;
            if (!string.IsNullOrEmpty(Request["isCanvasSection"]) && Request["isCanvasSection"] == "1")
            {
                isCanvasSection = 1;
            }

            if (string.IsNullOrEmpty(LMSCallHashKey)) { LMSCallHashKey = "Not used"; }
            string pureHash = "haiku" + DateTime.Now.ToString("M/d/yyyy") + ' ' + DateTime.Now.ToString("HH:mm") + ' ' + LMSCallHashKey;
            string ServerHash = Gsmu.Api.Encryption.HmacSha1.Encode("haiku" + DateTime.Now.ToString("M/d/yyyy") + ' ' + DateTime.Now.ToString("HH:mm"), LMSCallHashKey);
            object callResult = null;

            //if (Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode || (ServerHash == CallerRequestHash) || AuthorizationHelper.CurrentInstructorUser != null)
            //{
            //if (Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode || (ServerHash == CallerRequestHash) || AuthorizationHelper.CurrentInstructorUser != null)
            //{
            //    //try
            //{
            Api.Networking.Mail.EmailFunction emailFunction = new Api.Networking.Mail.EmailFunction();
            switch (call)
            {
                case "getReminderICSTemplate":
                    int remindercourseid = courseId ?? 0;
                    if (remindercourseid != 0)
                    {
                        List<string> fileNamesCreated = emailFunction.MakeICalendarAttachment(Settings.Instance.GetMasterInfo().PublicEmailAddress, ".vbs", remindercourseid, 1);
                        return new JsonResult()
                        {
                            Data = new
                            {
                                IsAdminMode = Gsmu.Api.Web.RequireAdminModeAttribute.IsAdminMode,
                                CallerRequestHash = CallerRequestHash,
                                ServerHash = ServerHash,
                                CurrentInstructorUser = AuthorizationHelper.CurrentInstructorUser,
                                Status = "Success",
                                IcsFileNames = fileNamesCreated
                            },
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }
                    break;
            }

            //}
            //catch (Exception e)
            //{
            //    string ErrorMessage = e.ToString();
            //    var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
            //    if (TurnOnDebugTracingMode != null)
            //    {
            //        if (TurnOnDebugTracingMode.ToLower() == "off")
            //        {
            //            ErrorMessage = "An error has occurred. Please try again or contact administrator. MErr100";
            //        }
            //    }
            //    if (call.StartsWith("haiku"))
            //    {
            //        var response = new Response();
            //        response.Exceptions.Add(e);
            //        callResult = response;
            //    }
            //    else
            //    {

            //        callResult = new
            //        {
            //            Error = ErrorMessage
            //        };
            //    }
            //}
            //}
            var result = new JavaScriptResult();
            string arguments = SerializationHelper.SerializeEntity(callResult);
            if (call.ToLower() == "canvasgetcoursesectionslist")
            {
                result.Script = arguments;
            }
            else
            {
                result.Script = Request["callback"] + "(" + arguments + ");";
            }
            return result;
        }


        public ActionResult LtiConfiguration(string name)
        {
            var filename = "~/App_Data/Lti." + name + ".";

#if DEBUG
            filename += "Debug";
#else
            filename += "Release";
#endif
            filename += ".xml";

            var path = Server.MapPath(filename);
            return new FileStreamResult(new FileStream(path, FileMode.Open), "application/xml");
        }
        public ActionResult ExternalPayment(int TranscriptId, int cid)
        {
            Gsmu.Api.Data.School.Transcripts.Transcripts transcript = new Api.Data.School.Transcripts.Transcripts();
            var SingleTranscript = transcript.GetSingleTranscriptById(TranscriptId);

            ViewBag.Amount = String.Format("{0:0.00}", SingleTranscript.Amount); ;
            ViewBag.OrderNo = SingleTranscript.OrderNumber;
            ViewBag.TranscriptId = SingleTranscript.TranscriptID;
            ViewBag.cid = cid;
            ViewBag.showotherpayment = "true";
            ViewBag.credithourspayment = "true";
            ViewBag.returnsite = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "courses_attendance_detail.asp?cid=" + cid;
            transcript.ClockHourPurchaseOrderinProgress(SingleTranscript.OrderNumber, ViewBag.Amount, ViewBag.TranscriptId);
            return View();
        }
        public ActionResult AdminOrderPayment(string orderNumber, string amount, string paidinfull)
        {
            ViewBag.Amount = amount;
            ViewBag.OrderNo = orderNumber;
            ViewBag.returnsite = Settings.Instance.GetMasterInfo4().AspSiteRootUrl;
            var chkout = CheckoutInfo.Instance;
            chkout.ReturnLink = Settings.Instance.GetMasterInfo4().AspSiteRootUrl;
            chkout.PaymentCaller = "checkout";
            chkout.TotalPaid = decimal.Parse(amount);
            chkout.OrderNumber = orderNumber;
            ViewBag.showotherpayment = "false";
            ViewBag.credithourspayment = "false";
            return View("ExternalPayment");
        }

        public void TestEventDetails()
        {
            IEventDetails EventDetails = new Gsmu.Service.BusinessLogic.Events.EventDetails();
            Gsmu.Service.Models.Events.EventDetailsModel eventDetails = EventDetails.GetEventDetails(8600);
        }

        public string TestCertificate()
        {
            Gsmu.Api.Data.Survey.Survey surveyapi = new Gsmu.Api.Data.Survey.Survey(21);
            Gsmu.Api.Data.School.Transcripts.Transcripts tran = new Gsmu.Api.Data.School.Transcripts.Transcripts();
            Transcript StudentTranscript = tran.StudentTranscriptedCourse(117271, 2588);
            Course_Roster StudentRoster = tran.StudentTranscriptedRoster(117271, 2588);

            CourseModel cmodel = new CourseModel(2588);
            cmodel.Course.coursecertificate = int.Parse(surveyapi.SurveyModel.AfterSurveyCertificate.Replace("~", string.Empty));
            Gsmu.Api.Export.GradeCertificate.PdfGradeCertificate certificate = new Gsmu.Api.Export.GradeCertificate.PdfGradeCertificate(cmodel.Course, StudentRoster, StudentTranscript);
            certificate.Execute();
            EmailAuditTrail emailentity = new EmailAuditTrail();
            return Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl + "Temp/" + certificate.PdfFileName;
        }

        public void TestPaygovInterface()
        {
            Gsmu.Api.Commerce.CreditCardPayments CreditCardPayments = new Gsmu.Api.Commerce.CreditCardPayments();
            CreditCardPayments.AuthorizePayGovPaymentThruService();
        }


        public XmlDocument TestPaygovSoap()
        {


            var pymodel = new CreditCardPaymentModel();
            Gsmu.Api.Commerce.CreditCardPayments CreditCardPayments = new Gsmu.Api.Commerce.CreditCardPayments();
            pymodel.CardNumber = "4111111111111111";
            pymodel.CCV = "999";
            pymodel.ExpiryMonth = "12";
            pymodel.ExpiryYear = "2023";
            pymodel.FirstName = "Test name";
            pymodel.LastName = "Surname";
            pymodel.OrderNumber = "213456789";


            CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
            CreditCardPayments payment = new CreditCardPayments();
            HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
            var request = context.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            var baseUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl);

            pymodel.CurrentUrl = new string[2];
            pymodel.CurrentUrl[0] = baseUrl;
            pymodel.CurrentUrl[1] = baseUrl + request.FilePath; ;


            var reslt = CreditCardPayments.ProcessPaygovTEST(pymodel, "100");
            return reslt;
        }

        public string CallSoapWebService()
        {
            var _url = "http://xxxxxxxxx/Service1.asmx";
            var _action = "http://xxxxxxxx/Service1.asmx?op=HelloWorld";

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope();
            HttpWebRequest webRequest = CreateWebRequest(_url, _action);
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            // begin async call to web request.
            IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);

            // suspend this thread until call is complete. You might want to
            // do something usefull here like update your UI.
            asyncResult.AsyncWaitHandle.WaitOne();

            // get the response from the completed web request.
            string soapResult;
            using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
            {
                using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                {
                    soapResult = rd.ReadToEnd();
                }
                return soapResult;
            }
        }

        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope()
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml(
            @"<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:xsi=""http://www.w3.org/1999/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/1999/XMLSchema"">
        <SOAP-ENV:Body>
            <HelloWorld xmlns=""http://tempuri.org/"" 
                SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"">
                <int1 xsi:type=""xsd:integer"">12</int1>
                <int2 xsi:type=""xsd:integer"">32</int2>
            </HelloWorld>
        </SOAP-ENV:Body>
    </SOAP-ENV:Envelope>");
            return soapEnvelopeDocument;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }


        public BBHierarchies TestBB()
        {
            if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
            {
                BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                var datasources = handelr.GetBBAPIHierarchiess(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl,"", "", "", jsonToken);

                return datasources;
            }

            return null;
        }

        public bool TestLog()
        {
            var uname = Request.QueryString["uname"];
            var pw = Request.QueryString["pw"];

            if (uname == "jm" && pw == "pass")
                return true;
            else
                return false;
        }



        public string TestMark()
        {
            return "Done marking.";
        }
        public ActionResult TestBB_Courses()
        {
            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
            //  BBEnrollment myEnrollmentData = new BBEnrollment();
            // myEnrollmentData.courseRoleId = "Student";
            //  var Enrollment =    handelr.CreateNewEnrollment(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, myEnrollmentData, "71599ad1c93942b29d8bcafd6e86f78d", "5ec540cccf5e4e53a914da2af7dbc208", "uuid", "uuid", "", jsonToken);

            var bbcourses = handelr.GetCourseDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", "", jsonToken);
            dynamic json = JsonConvert.DeserializeObject(bbcourses);



            var result = new JavaScriptResult();
            string arguments = SerializationHelper.SerializeEntity(json);

                result.Script = Request["callback"] + "(" + arguments + ");";
            
            return result;

        }

        public string DebugBlackBoard(string callType, string url, string AppKey, string AppSec, string query, string jsonparams)
        {
            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            string currURL = "";
            string currAppKey = "";
            string currAppSec = "";
            if (callType == "") { callType = "GET"; }
            if (!string.IsNullOrEmpty(url))
            {
                currURL = url;
            }
            else
            {
                currURL = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
            }
            if (!string.IsNullOrEmpty(AppKey)) {
                currAppKey = AppKey;
            } else
            {
                currAppKey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
            }
            if (!string.IsNullOrEmpty(AppSec))
            {
                currAppSec = AppSec;
            }
            else
            {
                currAppSec = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
            }
            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
            Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
            Audittrail.TableName = Request.UserHostName;
            Audittrail.DetailDescription = "Blakcboard Debug";
            Audittrail.AuditDate = DateTime.Now;
            Audittrail.RoutineName = "DebugService" + AuthorizationHelper.CurrentUser.LoggedInUserType + " -Admin";
            Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
            Audittrail.AuditAction = "info" + jsonToken + " x " + currURL;
            Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
            LogManager.LogSiteActivity(Audittrail);

            var bbresult = handelr.DebugBlackBoard(callType, currAppSec, currAppKey, "", currURL, jsonToken, currURL, query,jsonparams);

            return bbresult;
        }
        public string UpdateCourseInfo()
        {
            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
            string result = "";
            int tempGSMUCourseID = 0;

            if (!string.IsNullOrEmpty(Request["courseid"]))
            {
                tempGSMUCourseID = Convert.ToInt32(Request["courseid"]);
            }
            if (tempGSMUCourseID != 0)
            {


                using (var db = new SchoolEntities())
                {
                    var courseDetails = (from course in db.Courses
                                         where course.COURSEID == tempGSMUCourseID && course.BBCourseCloned != 0 && (course.blackboard_api_uuid != null || course.blackboard_api_uuid != "") && course.CustomCourseField1 != "" && course.CustomCourseField2 != "" && course.CustomCourseField1 != null && course.CustomCourseField2 != null
                                         select course).FirstOrDefault();

                    string curBB_uuid = courseDetails.blackboard_api_uuid;

                    var bbaddedcourses = handelr.GetCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, courseDetails.CustomCourseField1, "courseId", "", jsonToken);
                    BBCourse obj_course = JsonConvert.DeserializeObject<BBCourse>(bbaddedcourses);

                    obj_course.name = courseDetails.COURSENAME;

                    if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != "" && Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != null)
                    {
                        string tempDSK = Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk;
                        if (tempDSK.IndexOf("_") < 0)
                        {
                            var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, tempDSK, "dsk", "", jsonToken);
                            datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                            string actualDSK = globaldatasource.id;

                            obj_course.dataSourceId = actualDSK;
                        }
                        else
                        {
                            obj_course.dataSourceId = tempDSK;
                        }
                    }
                    var updated_course = handelr.UpdateBlackbooardCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, obj_course, courseDetails.CustomCourseField1, "", jsonToken);
                    result = result + "source uuid: " + curBB_uuid + "<br>| Message:" + bbaddedcourses + "<br> Updated Course:" + updated_course;
                }
            } else
            {
                result = "Unable to find course to update";
            }
            return result;
        }

        public string UpdateCourseEnrollmentInfo()
        {
            //no normalization yet
            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
            Gsmu.Api.Data.School.Student.EnrollmentFunction enrollmentFunction = new Gsmu.Api.Data.School.Student.EnrollmentFunction();
            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
            string result = "";
            int tempGSMUCourseID = 0;
            int tempGSMURosterID = 0;

            if (!string.IsNullOrEmpty(Request["courseid"]))
            {
                tempGSMUCourseID = Convert.ToInt32(Request["courseid"]);
            }
            if (!string.IsNullOrEmpty(Request["rosterid"]))
            {
                tempGSMURosterID = Convert.ToInt32(Request["rosterid"]);
            }
            if (tempGSMUCourseID != 0 && tempGSMURosterID == 0)
            {
                using (var db2 = new SchoolEntities())
                {
                    foreach (var _roster in (from roster in db2.Course_Rosters where roster.COURSEID == tempGSMUCourseID && roster.PaidInFull != 0 && roster.Cancel == 0 select roster).ToList())
                    {
                        enrollmentFunction.EnrollUserToBlackBoardApi(tempGSMUCourseID, _roster.STUDENTID.Value, 0, "student", _roster.RosterID);
                    }
                }
            }
            else if (tempGSMURosterID != 0)
            {
                using (var db2 = new SchoolEntities())
                {
                    foreach (var _roster in (from roster in db2.Course_Rosters where roster.RosterID == tempGSMURosterID && roster.PaidInFull != 0 && roster.Cancel == 0 select roster).ToList())
                    {
                        enrollmentFunction.EnrollUserToBlackBoardApi(tempGSMUCourseID, _roster.STUDENTID.Value, 0, "student", _roster.RosterID);
                    }
                }
            }
            return result;
        }

        public string CopyandCreateNewCourse()
        {
            Gsmu.Api.Data.School.Student.EnrollmentFunction enrollmentFunction = new Gsmu.Api.Data.School.Student.EnrollmentFunction();
            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
            BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
            string result = "";

            using (var db = new SchoolEntities())
            {
                Course courseforUpdate = null;
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;
                db.Configuration.AutoDetectChangesEnabled = false;
                var courseDetails = (from course in db.Courses
                                     where course.BBCourseCloned == 0 && (course.blackboard_api_uuid == null || course.blackboard_api_uuid == "") && course.CustomCourseField1 != "" && course.CustomCourseField2 != "" && course.CustomCourseField1 !=null && course.CustomCourseField2!=null
                                     select course).ToList();

                string SourceCourseId = "";
                copyCourse copyCourse = new copyCourse();
                copyCourse.targetCourse = new targetCourse();
                copyCourse.targetCourse.courseId = "";
                copyCourse.copy = new copy();

                BBCourse bb_updated_course = new BBCourse();

                foreach (var course in courseDetails)
                {
                    SourceCourseId = course.CustomCourseField2;
                    copyCourse = new copyCourse();
                    copyCourse.targetCourse = new targetCourse();
                    copyCourse.targetCourse.courseId = course.CustomCourseField1;
                    copyCourse.copy = new copy();
                    copyCourse.copy.adaptiveReleaseRules = true;
                    copyCourse.copy.announcements = true;
                    copyCourse.copy.assessments = true;
                    copyCourse.copy.blogs = true;
                    copyCourse.copy.calendar = true;
                    copyCourse.copy.contacts = true;
                    copyCourse.copy.contentAlignments = true;
                    copyCourse.copy.contentAreas = true;
                    copyCourse.copy.discussions = "None";
                    copyCourse.copy.glossary = true;
                    copyCourse.copy.gradebook = true;
                    copyCourse.copy.groupSettings = true;
                    copyCourse.copy.journals = true;
                    copyCourse.copy.retentionRules = true;
                    copyCourse.copy.rubrics = true;
                    copyCourse.copy.settings = new settings();
                    copyCourse.copy.settings.availability = true;
                    copyCourse.copy.settings.bannerImage = true;
                    copyCourse.copy.settings.duration = true;
                    copyCourse.copy.settings.enrollmentOptions = true;
                    copyCourse.copy.settings.guestAccess = true;
                    copyCourse.copy.settings.languagePack = true;
                    copyCourse.copy.settings.navigationSettings = true;
                    copyCourse.copy.settings.observerAccess = true;

                    copyCourse.copy.wikis = true;
                    copyCourse.copy.tasks = true;
                    result = result+ "New ID: " + course.CustomCourseField1;
                    result = result + " <br> Source '" + SourceCourseId+"' | ";
                    var bbcourses = handelr.GetCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, SourceCourseId, "courseId", "", jsonToken);
                    dynamic json = JsonConvert.DeserializeObject(bbcourses);
                    string uuid = "";
                    if (json != null)
                    {
                        BBCourse obj_course = JsonConvert.DeserializeObject<BBCourse>(bbcourses);
                        uuid = obj_course.uuid;
                    }

                    if (uuid != "")
                    {
                        var bbNewCourse= handelr.CopyandCreateNewCourse(BB_sec_key, BB_app_key, "", bb_connection_url, copyCourse, course.CustomCourseField2, jsonToken, "");
                        var bbaddedcourses = handelr.GetCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, course.CustomCourseField1, "courseId", "", jsonToken);
                        BBCourse obj_course = JsonConvert.DeserializeObject<BBCourse>(bbaddedcourses);

                        obj_course.name = course.COURSENAME;                        

                        if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != "" && Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk != null)
                        {
                            string tempDSK = Gsmu.Api.Integration.Blackboard.Configuration.Instance.CoursesDsk;
                            if (tempDSK.IndexOf("_") < 0)
                            {
                                var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, tempDSK, "dsk", "", jsonToken);
                                datasource globaldatasource = JsonConvert.DeserializeObject<datasource>(globaldatasourceKeyDetails);
                                string actualDSK = globaldatasource.id;

                                obj_course.dataSourceId = actualDSK;
                            }
                            else
                            {
                                obj_course.dataSourceId = tempDSK;
                            }                            
                        }
                        //var updated_course = handelr.UpdateBlackbooardCourseDetails(BB_sec_key, BB_app_key, "", bb_connection_url, obj_course,course.CustomCourseField1, "", jsonToken);
                        var updated_course = handelr.UpdateBlackbooardCourseDetails(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, obj_course, course.CustomCourseField1, "", jsonToken);
                        result = result + "source uuid: " + uuid + "<br>json: " + json + "<br>| Message:" + bbaddedcourses + "<br> Updated Course:"+updated_course;

                        try
                        {
                            bb_updated_course = JsonConvert.DeserializeObject<BBCourse>(updated_course);
                        }
                        catch { }

                        using (var db2 = new SchoolEntities())
                        {
                            courseforUpdate = (from _course in db2.Courses where _course.COURSEID == course.COURSEID select _course).FirstOrDefault();
                            courseforUpdate.blackboard_api_uuid = bb_updated_course.uuid;
                            courseforUpdate.BBCourseCloned = 1;
                            courseforUpdate.bb_last_integration_date = DateTime.Now;
                            result = result + "<br>|new course uuid: " + bb_updated_course.uuid;

                            db2.SaveChanges();

                            foreach (var _roster in (from roster in db2.Course_Rosters where roster.COURSEID == course.COURSEID && roster.PaidInFull != 0 && roster.Cancel == 0 select roster).ToList())
                            {
                                enrollmentFunction.EnrollUserToBlackBoardApi(course.COURSEID, _roster.STUDENTID.Value, 0, "student", _roster.RosterID);
                            }
                            foreach (var _instructor in (from instructor in db2.Instructors where instructor.INSTRUCTORID == course.INSTRUCTORID && instructor.DISABLED == 0 select instructor).ToList())
                            {
                                enrollmentFunction.EnrollUserToBlackBoardApi(course.COURSEID, 0, _instructor.INSTRUCTORID, "instructor",0);
                            }
                        }
                    }
                    else
                    {
                        result = result + " Course Id not exist.";
                    }
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.DetailDescription = "Blakcboard Copy/Create Course";
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "CreateCourse-" + AuthorizationHelper.CurrentUser.LoggedInUserType + " -Admin";
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    Audittrail.AuditAction = "info" + result + " x ";
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }

            }
            return result;
        }
    }

    public class TestList
    {
        public int id { get; set; }
        public string Title { get; set; }
        public string userID { get; set; }
        public string Descriptiom { get; set; }
    }

    public class bbCoursedropdown{
        public int uuid { get; set; }
        public string  name{ get;set;}
        }
}

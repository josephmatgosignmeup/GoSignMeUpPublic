using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Gsmu.Api.Commerce;
using Gsmu.Api.Commerce.ShoppingCart;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Data.ViewModels;
using Gsmu.Api.Networking.Mail;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using haiku = Gsmu.Api.Integration.Haiku;
using canvas = Gsmu.Api.Integration.Canvas;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Configuration;
using Gsmu.Api.Web;
using Gsmu.Api.Data.School.Transcripts;
using System.Web.Script.Serialization;
using Square;
using Square.Apis;
using Square.Models;
using Square.Exceptions;

//using Paymentech;

using System.IO.Compression;
using Gsmu.Web.WebReference;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Xml;

namespace Gsmu.Web.Areas.Public.Controllers
{
    /// <summary>
    /// Handles interfacing with the cart.
    /// </summary>
    public class CartController : Controller
    {
        /// <summary>
        /// This is based on the original ASP site, complete replicate.
        /// </summary>
        /// <returns></returns>
        /// 
        string storeOrderNumSession = string.Empty;
        string amountSession = string.Empty;
        public ActionResult MiniDisplay()
        {
            CourseShoppingCart.Instance.Refresh();
            return PartialView(CourseShoppingCart.Instance);
        }

        public ActionResult Login()
        {
            return PartialView();
        }

        public ActionResult ValidateCart()
        {
            var messages = CourseShoppingCart.Instance.Validate();
            return Json(messages, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CheckOut(string sort = "0")
        {
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Cart Checkout" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    try
                    {
                        foreach (var item in CourseShoppingCart.Instance.Items)
                        {

                            Audittrail.AuditAction = item.Course.COURSENAME + " " + item.LineTotal + ";";
                        }

                    }
                    catch { }
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            CourseShoppingCart.Instance.StartCheckout();
            if (CheckoutInfo.Instance.HasPartialPayment == null)
            {
                CheckoutInfo.Instance.HasPartialPayment = false;
            }
            //IEnumerable<Material> Material = GetListofMaterialsForIndividualCourse(2187);
            //DeleteMaterialsFromSpeciedCourse(2225,"1015");
            //GetCheckOutQuestion();
            //GetPreRequisitesSetting(7438);
            //GetCourseMaterialAmountAndShipping(7438).Keys("price");
            //ViewBag.CheckOutMessage = GetCheckOutMessageSetting["ischeckoutcommentenabled"];
            Dictionary<string, string> CheckoutComment = GetCheckoutCommentSettings();
            if (CheckoutComment.ContainsKey("ischeckoutcommentenabled"))
            { ViewBag.ischeckoutcommentenabled = CheckoutComment["ischeckoutcommentenabled"]; }
            else
            { ViewBag.ischeckoutcommentenabled = "0"; }

            if (CheckoutComment.ContainsKey("checkoutcommentlabel"))
            { ViewBag.checkoutcommentlabel = CheckoutComment["checkoutcommentlabel"]; }
            else
            { ViewBag.checkoutcommentlabel = ""; }

            if (CheckoutComment.ContainsKey("checkoutquestionenabled"))
            { ViewBag.checkoutquestionenabled = CheckoutComment["checkoutquestionenabled"]; }
            else
            { ViewBag.checkoutquestionenabled = "False"; }

            if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 1)
            {
                ViewBag.AllowCouponPerOrder = "True";
            }
            else
            {
                ViewBag.AllowCouponPerOrder = "False";
            }

            if (Settings.Instance.GetMasterInfo3().HideCoupons == 1)
            {
                ViewBag.HideCoupons = "True";
            }
            else
            {
                ViewBag.HideCoupons = "False";
            }
            if (Settings.Instance.GetMasterInfo2().HidePaymentInfo == 1)
            {
                ViewBag.HidePaymentInfo = "True";
            }
            else
            {
                ViewBag.HidePaymentInfo = "False";
            }
            Dictionary<string, string> CheckOutMessage = GetCheckOutMessageSetting();
            if (CheckOutMessage.ContainsKey("hidecheckoutapproval"))
            { ViewBag.hidecheckoutapproval = CheckOutMessage["hidecheckoutapproval"]; }
            else
            { ViewBag.hidecheckoutapproval = "0"; }

            if (CheckOutMessage.ContainsKey("showslternatecheckoutmessage"))
            { ViewBag.showslternatecheckoutmessage = CheckOutMessage["showslternatecheckoutmessage"]; }
            else
            { ViewBag.showslternatecheckoutmessage = "0"; }

            if (CheckOutMessage.ContainsKey("alternatecheckoutmessage"))
            { ViewBag.alternatecheckoutmessage = CheckOutMessage["alternatecheckoutmessage"]; }
            else
            { ViewBag.alternatecheckoutmessage = ""; }

            var CheckOutQuestion = GetParseCheckOutQuestion();
            SelectList list = new SelectList(CheckOutQuestion);
            ViewBag.CheckOutQuestion = list;
            ViewBag.checkoutquestionlabel = Settings.Instance.GetMasterInfo2().CheckoutQuestionText;

            //Check Outomatic Coupon Application
            EnrollmentFunction enrollment = new EnrollmentFunction();
            // already filtered by start/exp date - so if coupon is automatic then auto populate to coupon box.

            var automaticCoupon = enrollment.PrimaryAutomaticCoupon;
            string couponCode = string.Empty;
            if (automaticCoupon != null)
            {
                couponCode = automaticCoupon.CouponCode;
            }
            ViewBag.CouponCode = couponCode;
            var test = CourseShoppingCart.Instance.MultipleStudentCourses;
            ViewBag.Sortby = sort;
            ViewBag.CountMembership = CourseShoppingCart.Instance.CountMembership;
            return PartialView(CourseShoppingCart.Instance);

        }


        public String SubmitCheckout(string OrderId)
        {
            var chkout = CheckoutInfo.Instance;
            if (AuthorizationHelper.CurrentUser.LoggedInUserType == LoggedInUserType.Guest && chkout.PaymentCaller != "paynowuserdash")
            {
                return "Your session has been timed out. Please login and try again.";
            }
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Submit Checkout" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    try
                    {
                        foreach (var item in CourseShoppingCart.Instance.Items)
                        {

                            Audittrail.AuditAction = item.Course.COURSENAME + " " + item.LineTotal + ";";
                        }

                    }
                    catch { }
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            String OrderNumber = "";

            if (CourseShoppingCart.Instance.MultipleStudentCourses.Count == 0)
            {
                EnrollmentFunction enroll = new EnrollmentFunction();
                Course_Roster courseRoster = new Course_Roster(true);
                courseRoster.STUDENTID = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                courseRoster.CheckoutComments = Request["CheckoutComments"];
                courseRoster.CheckoutComments2 = Request["CheckOutQuestion"];
                courseRoster.CouponCode = Request["discountcode"];
                try
                {
                    var PaidInFull = short.Parse(Request["MarkedAsPaidInFull"]);
                    if (PaidInFull == 1)
                    {
                        CourseShoppingCart.Instance.PaidInfull = true;
                    }
                    else
                    {
                        CourseShoppingCart.Instance.PaidInfull = false;
                    }
                 }
                catch {
                    CourseShoppingCart.Instance.PaidInfull = false;
                }
                float val = 0;
                if (float.TryParse(Request["coupondiscount"], out val))
                {
                    courseRoster.CouponDiscount = float.Parse(Request["coupondiscount"]);
                }
                else
                {
                    courseRoster.CouponDiscount = 0;
                }
                decimal value = 0;
                if (Decimal.TryParse(Request["SalesTaxTotal"], out value))
                {
                    courseRoster.CourseSalesTaxPaid = decimal.Parse(Request["SalesTaxTotal"]);
                }
                else
                {
                    courseRoster.CourseSalesTaxPaid = 0;
                }
                string MaterialList = Request["MaterialList"];
                if (string.IsNullOrWhiteSpace(MaterialList))
                {
                    MaterialList = "";
                }
                string coursematlist = SaveMaterialtoRoster(MaterialList);
                OrderNumber = enroll.CarttoCourseRoster(courseRoster, coursematlist);
            }

            else
            {
                EnrollmentFunction enroll = new EnrollmentFunction();
                string strMasterOrderNumber = "";
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    strMasterOrderNumber = enroll.CheckGenerateOrderID(AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID);
                }
                else if (AuthorizationHelper.CurrentInstructorUser != null)
                {
                    strMasterOrderNumber = enroll.CheckGenerateOrderID(AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID);
                }
                else if (AuthorizationHelper.CurrentAdminUser != null)
                {
                    strMasterOrderNumber = enroll.CheckGenerateOrderID(AuthorizationHelper.CurrentAdminUser.AdminID);
                }
                else if (AuthorizationHelper.CurrentSubAdminUser != null)
                {
                    strMasterOrderNumber = enroll.CheckGenerateOrderID(AuthorizationHelper.CurrentSubAdminUser.InstructorId);
                }
                else if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    strMasterOrderNumber = enroll.CheckGenerateOrderID(AuthorizationHelper.CurrentStudentUser.STUDENTID);
                }
                foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses)
                {

                    Course_Roster courseRoster = new Course_Roster(true);
                    courseRoster.WAITING = st.IsWaiting;


                    courseRoster.STUDENTID = st.StudentId;
                    courseRoster.COURSEID = st.CourseId;
                    courseRoster.OrderNumber = st.OrderNumber;
                    courseRoster.CheckoutComments = Request["CheckoutComments"];
                    courseRoster.CheckoutComments2 = Request["CheckOutQuestion"];
                    courseRoster.CouponCode = Request["discountcode"];
                    try
                    {
                        var PaidInFull = short.Parse(Request["MarkedAsPaidInFull"]);
                        if (PaidInFull == 1)
                        {
                            CourseShoppingCart.Instance.PaidInfull = true;
                        }
                        else
                        {
                            CourseShoppingCart.Instance.PaidInfull = false;
                        }
                    }
                    catch
                    {
                        CourseShoppingCart.Instance.PaidInfull = false;
                    }
                    float val = 0;
                    if (float.TryParse(Request["coupondiscount"], out val))
                    {
                        courseRoster.CouponDiscount = float.Parse(Request["coupondiscount"]);
                    }
                    else
                    {
                        courseRoster.CouponDiscount = 0;
                    }
                    string MaterialList = Request["MaterialList"];
                    decimal value = 0;
                    if (Decimal.TryParse(Request["SalesTaxTotal"], out value))
                    {
                        courseRoster.CourseSalesTaxPaid = decimal.Parse(Request["SalesTaxTotal"]);
                    }
                    else
                    {
                        courseRoster.CourseSalesTaxPaid = 0;
                    }
                    string coursematlist = SaveMaterialtoRoster(MaterialList);
                    OrderNumber = enroll.CarttoCourseRoster(courseRoster, coursematlist, strMasterOrderNumber);
                }
            }




            return OrderNumber;
        }

        public ActionResult Payment()
        {

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err703.");
            }
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Cart Checkout Payment Page" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    try
                    {
                        foreach (var item in CourseShoppingCart.Instance.Items)
                        {

                            Audittrail.AuditAction = item.Course.COURSEID.ToString() + " " + item.LineTotal + ";";
                        }

                    }
                    catch { }
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            var chkout = CheckoutInfo.Instance;

            string OrderNumber = Request["OrderNumber"];
            chkout.TranscriptID = Request["TranscriptID"];
            chkout.OrderNumber = OrderNumber;
            decimal OrderTotal = chkout.OrderTotal;
            decimal PaymentTotal = 0;
            string showotherpaymentFromadminside = Request["showotherpayment"];
            if (Request["showotherpayment"] == null)
            {
                showotherpaymentFromadminside = "";
            }
            if ((Request["credithourspayment"] != "true") || (Request["credithourspayment"] == null) || (Request["fromadmin"] == "true"))
            {
                try
                {
                    PaymentTotal = Request["PaymentTotal"].Trim().Length == 0 ? 0 : decimal.Parse(Request["PaymentTotal"]);
                }
                catch
                {
                    PaymentTotal = Request["OrderTotal"].Trim().Length == 0 ? 0 : decimal.Parse(Request["OrderTotal"]);
                }

                chkout.PaymentCaller = Request["PaymentCaller"];
                chkout.PaymentTotal = PaymentTotal;

            }

            else
            {
                OrderTotal = decimal.Parse((Request["OrderTotal"]));
                PaymentTotal = OrderTotal;
                chkout.PaymentTotal = OrderTotal;
            }

            if (chkout.OrderinProgressTotal > chkout.PaymentTotal && !chkout.HasPartialPayment)
            {
                if ((Request["credithourspayment"] != "true") || (Request["credithourspayment"] == null))
                {
                    OrderTotal = chkout.OrderinProgressTotal;
                    PaymentTotal = chkout.OrderinProgressTotal;
                    chkout.PaymentTotal = chkout.OrderinProgressTotal;
                }
            }

            var credithourspayment = false;
            ViewBag.TranscriptId = "";
            if (Request["credithourspayment"] != null)
            {
                if (Request["credithourspayment"] == "true")
                {
                    credithourspayment = true;
                    ViewBag.TranscriptId = Request["TranscriptID"];
                    ViewBag.FromUserType = "from Admin";
                }
            }
            if (chkout.PaymentCaller == "payclockhours")
            {
                credithourspayment = true;
                ViewBag.TranscriptId = Request["TranscriptID"];
                ViewBag.FromUserType = "from Public";
            }
            ViewBag.IsPayPalAdvance = CreditCardPaymentHelper.UsePayPalAdvance;
            if (CreditCardPaymentHelper.UsePayPalAdvance)
            {
                ViewBag.OrderButtonLabel = "Continue";
            }
            else
            {
                ViewBag.OrderButtonLabel = "Place Order Now";
            }

            EnrollmentFunction enroll = new EnrollmentFunction();
            ViewBag.OrderID = OrderNumber;
            ViewBag.RequiredFieldSetUp = enroll.GetCCVandRequiredFieldSetUp()["requiredsettings"];
            ViewBag.ccvon = enroll.GetCCVandRequiredFieldSetUp()["ccvon"];
            if (AuthorizationHelper.CurrentStudentUser != null)
            {
                ViewBag.StudentFirstName = AuthorizationHelper.CurrentStudentUser.FIRST;
                ViewBag.StudentLastName = AuthorizationHelper.CurrentStudentUser.LAST;
                ViewBag.StudentAddress1 = AuthorizationHelper.CurrentStudentUser.ADDRESS;
                ViewBag.StudentCity = AuthorizationHelper.CurrentStudentUser.CITY;
                ViewBag.StudentState = AuthorizationHelper.CurrentStudentUser.STATE;
                ViewBag.StudentZip = AuthorizationHelper.CurrentStudentUser.ZIP;
                ViewBag.StudentCountry = AuthorizationHelper.CurrentStudentUser.COUNTRY;
            }
            else if (AuthorizationHelper.CurrentSupervisorUser != null)
            {
                ViewBag.StudentFirstName = AuthorizationHelper.CurrentSupervisorUser.FIRST;
                ViewBag.StudentLastName = AuthorizationHelper.CurrentSupervisorUser.LAST;
                ViewBag.StudentAddress1 = AuthorizationHelper.CurrentSupervisorUser.ADDRESS;
                ViewBag.StudentCity = AuthorizationHelper.CurrentSupervisorUser.CITY;
                ViewBag.StudentState = AuthorizationHelper.CurrentSupervisorUser.STATE;
                ViewBag.StudentZip = AuthorizationHelper.CurrentSupervisorUser.ZIP;
                //ViewBag.StudentCountry = AuthorizationHelper.CurrentSupervisorUser.;
            }

            ViewBag.ActiveCCProcessing = CreditCardPaymentHelper.ActiveCCProcessing;
            ViewBag.OrderTotal = OrderTotal;
            ViewBag.OterPaymentLabel = enroll.GetOtherPaymentLabel();
            ViewBag.IsAuthorized = AuthorizationHelper.CurrentUser.IsLoggedIn ? "authorized" : "noauthorzied";
            ViewBag.ShowOtherPayments = enroll.ShowOtherPayments();
            ViewBag.IsPaygov = CreditCardPaymentHelper.UsePaygov;
            ViewBag.IsPaygovTCS = CreditCardPaymentHelper.UsePaygovTCS;
            ViewBag.IsAuthorizeNet = CreditCardPaymentHelper.UseAuthorizeNet;
            ViewBag.IsUseClubPilates = CreditCardPaymentHelper.UseClubPilates;
            ViewBag.IsSquare = CreditCardPaymentHelper.UseSquare;
            ViewBag.IsCybersource = CreditCardPaymentHelper.UseCybersource;
            ViewBag.IsFirstData = CreditCardPaymentHelper.UseFirstData;
            ViewBag.IsAdyen = CreditCardPaymentHelper.UseAdyen;
            ViewBag.IsAuthorizedRedirect = CreditCardPaymentHelper.UseAuthorizeNetRedirect;
            ViewBag.IsTouchnetRedirect = CreditCardPaymentHelper.UseTouchnetRedirect;
            ViewBag.IsChasePayment = CreditCardPaymentHelper.UseChasePayment;
            ViewBag.hostedSecureID = CreditCardPaymentHelper.ChasePaymentMerchantId;
            ViewBag.ChasePaymentServer = CreditCardPaymentHelper.ChasePaymentServer;
            ViewBag.IsConverge = CreditCardPaymentHelper.UseElavon;
            ViewBag.UseCashNetRedirect = CreditCardPaymentHelper.UseCashNetRedirect;
            ViewBag.UseSpreedly = CreditCardPaymentHelper.UseSpreedly;

            // ViewBag.UseRevTrak = CreditCardPaymentHelper.UseRevtrak;
            string image_path = Server.MapPath("/Areas/Public/Images/Layout/allcardsbig_new.jpg");
            ViewBag.PaymentImageExisted = System.IO.File.Exists(image_path);

            decimal subtotal = PaymentTotal;
            if (subtotal > 0)
            {
                CreditCardPayments payment = new CreditCardPayments();
                var acceptedCreditCard = payment.GetAcceptedCreditCards();
                var countryList = payment.GetCountryList();
                var OtherPayments = payment.GetAllPaymentTypes();
                if (AuthorizationHelper.CurrentAdminUser != null || AuthorizationHelper.CurrentSubAdminUser != null)
                {
                    OtherPayments = payment.GetAllPaymentTypesforAdmin();
                }
                if (showotherpaymentFromadminside == "true")
                {
                    OtherPayments = payment.GetAllPaymentTypesforAdmin();
                }
                if ((OtherPayments.Count() <= 1) || (showotherpaymentFromadminside == "false"))
                {
                    ViewBag.ShowOtherPayments = false;
                }
                if (chkout.PaymentCaller == "payclockhours") // this is for ruby. Payingclock hours should not allow other payment types. Only credit card.
                {
                    ViewBag.ShowOtherPayments = false;
                }

                var showcreditcard = Settings.Instance.GetMasterInfo().ShowCreditCard;
                if (showcreditcard > 0)
                {
                    ViewBag.ShowCreditCard = true;

                }
                else
                {
                    ViewBag.ShowCreditCard = false;
                }
                if (chkout.PaymentCaller == "paynowuserdash")
                {
                    OtherPayments = OtherPayments.Where(o => o.allowPublicPayPending == 0);
                    enroll.UpdateSessionOfPayNowOrder(OrderNumber);

                }
                ViewBag.credithourspayment = credithourspayment;
                ViewBag.item1List = new SelectList(acceptedCreditCard, "PaymentType", "PaymentType", "Visa");
                ViewBag.itemCountryList = new SelectList(countryList, "countrycode", "countryname", "United States");
                ViewBag.itemOtherPayments = new SelectList(OtherPayments, "PaymentType", "PaymentType", "Select Payment Type");
                ViewBag.subtotal = String.Format("{0:0.00}", subtotal);
                ViewBag.paynowUserDash = chkout.PaymentCaller == "paynowuserdash" ? 1 : 0;

                if (credithourspayment)
                {
                    if ((CreditCardPaymentHelper.UseAuthorizeNetRedirect))
                    {
                        HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
                        var request = context.Request;
                        var appUrl = HttpRuntime.AppDomainAppVirtualPath;
                        ViewBag.AuthorizeRedirectLink = CreditCardPaymentHelper.ANRedirectSubmissionLink;
                        ViewBag.ANLogin = CreditCardPaymentHelper.ANLogin;
                        ViewBag.ANTesting = CreditCardPaymentHelper.ANTesting.ToString();
                        Random r = new Random();
                        string sequence = (r.Next(0, 1000)).ToString();
                        string timestamp = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
                        ViewBag.TimeStamp = timestamp;
                        if (CreditCardPaymentHelper.ANRedirectSubmissionLink.IndexOf("cbresourcecenter") != -1)
                        {
                            // for Coldwellbanker only.
                            ViewBag.Invoice = "618GSMU";
                        }
                        else
                        {
                            ViewBag.Invoice = Request["TranscriptID"];
                        }

                        PaymentsController pay = new PaymentsController();
                        //byte[] payloadInBytes = Encoding.UTF8.GetBytes(CreditCardPaymentHelper.ANLogin + "^" + sequence + "^" + timestamp + "^"+ strAmount + "^");
                        // var md5 = new HMACMD5(pay.StringToByteArray(CreditCardPaymentHelper.ANTranKey));
                        //  byte[] hash = md5.ComputeHash(payloadInBytes);
                        // ViewBag.Fingerprint = BitConverter.ToString(hash).Replace("-", string.Empty);
                        string strPaymentTotal = String.Format("{0:0.00}", PaymentTotal);
                        string payload = CreditCardPaymentHelper.ANLogin + "^" + sequence + "^" + timestamp + "^" + strPaymentTotal + "^";
                        ViewBag.Fingerprint = pay.HMAC_MD5(CreditCardPaymentHelper.ANTranKey, payload);
                        ViewBag.AuthorizeRedirectSequnce = sequence;
                        //Confirmation Number is being use on Capturing ad Approving Order on Silent Post (Authorize.Net Redirect). Any changes on format will affect the Silent Pot behavior. 
                        //Please consider checking SilentFormPost if Confirmation Number is changed format

                        ViewBag.ConfirmationNumber = OrderNumber + " - " + ListOfCourseNumbersPerOrderNumber(OrderNumber);
                        ViewBag.Amount = String.Format("{0:0.00}", PaymentTotal + 0);

                        string t_HREFSUCCESS = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/user/Dashboard?useACresTok=1&OrderTotal=" + OrderTotal + "&PaymentType=CC&OrderNo=" + OrderTotal;
                        //string t_HREFSUCCESS = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/course/browse";
                        if ((Request["credithourspayment"] == "true") && (Request["fromadmin"] == "true"))
                        {
                            t_HREFSUCCESS = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "/courses_attendance_detail.asp?cid=" + Request["Courseid"] + "&coursetype=0";
                        }
                        ViewBag.ReceiptLink = t_HREFSUCCESS;
                        //  ViewBag.RelayLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "admin/anetconfirmed3.asp";
                        ViewBag.RelayLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/AuthorizeRedirectSilentPost";

                    }

                }


                ViewBag.UseIpay = CreditCardPaymentHelper.UseIpay;
                ViewBag.UseAdyen = CreditCardPaymentHelper.UseAdyen;



                if (CreditCardPaymentHelper.UsePaygov & !enroll.ShowOtherPayments())
                {
                    return RedirectToAction("Confirmation", new { OrderNumber = OrderNumber, OrderAmount = subtotal, PayType = "Credit Card" });
                }
                else
                {
                    return PartialView();
                }
            }
            else
            {
                return RedirectToAction("Confirmation", new { OrderNumber = OrderNumber });
            }
        }

        public ActionResult TouchNetTLinkConfirmation()
        {
            try
            {
                Empty(); // clear cart

                ViewBag.Result = "000";
                string session_identifier = Request["session_identifier"];

                ViewBag.OrderTotal = Request["pmt_amt"];
                var chkout = CheckoutInfo.Instance;
                chkout.PaymentTotal = decimal.Parse(Request["pmt_amt"]);
                chkout.PaymentCaller = "checkout";
                ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS
                PaymentsController payment = new PaymentsController();
                ViewBag.OrderNo = payment.AuthorizeTransactions_TouchNetTLink(session_identifier);
                // ViewBag.OrderNo = Request["oid"];  //Hacked for Touchnet Development
                chkout.OrderNumber = ViewBag.OrderNo;
                if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
                {
                    //  ViewBag.Result = "Session Expired.";
                    EnrollmentFunction enrollment = new EnrollmentFunction();
                    AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(ViewBag.OrderNo));
                }
                PostBackDateCapture(ViewBag.OrderNo);

                return View();
            }
            catch (Exception e)
            {
                dynamic soapEx = e;
                string message = "Error on Payment : " + e.Message + " " + e.InnerException + soapEx.Detail.InnerText;
                return RedirectToAction("DisplayCartError", "Cart", new { message = message });
            }
        }

        public ActionResult PayGovConfirmation()
        {
            //ViewBag.OrderTotal = Request["OrderTotal"];
            ViewBag.OrderNo = Request["OrderNo"];
            string token = Request["token"];
            ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS
            PaymentsController payment = new PaymentsController();
            //PayGovTransactionDetails = pgTotalAmount + "|" + pgCCName + "|" + pgAuthCode + "|" + pgProcessDate + "|" + pgProcessTime;
            string tempTxDetails = payment.UpdatePaygovOrder(token);
            string[] pgTxDetails = tempTxDetails.Split('|');
            ViewBag.OrderTotal = pgTxDetails[0];
            ViewBag.CardName = pgTxDetails[1];
            ViewBag.AuthNum = pgTxDetails[2] + '-' + pgTxDetails[3] + '-' + pgTxDetails[4];
            if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
            {
                //  ViewBag.Result = "Session Expired.";
                EnrollmentFunction enrollment = new EnrollmentFunction();
                AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(ViewBag.OrderNo));
            }
            PostBackDateCapture(ViewBag.OrderNo);
            return View();
        }


        public ActionResult SpreedlyConfirmation()
        {
            string SpreedlyToken = Request.QueryString["payment_method_token"];
            ViewBag.OrderNo = Request["OrderNo"];
            CreditCardPaymentModel CreditCardPaymentModelValues = new CreditCardPaymentModel();
            CreditCardPaymentModelValues.RefNumber = SpreedlyToken;
            CreditCardPaymentModelValues.OrderNumber = Request["OrderNo"];

             string Amount =  CheckoutInfo.Instance.PaymentTotal.ToString();
             PaymentsController PaymentsController = new Controllers.PaymentsController();
             if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
             {
                 ViewBag.output = "Your session is expired.";
             }
             else if (CheckoutInfo.Instance == null)
             {
                 ViewBag.output = "You have nothing on your cart. Please checkout again.";
             }
             else
             {
                 ViewBag.output = PaymentsController.ProcessAndSelectPaymentMerchant(CreditCardPaymentModelValues, Amount);
             }
            return View();
        }

        public ActionResult SpreedlyIFrame()
        {
            CheckoutInfo chk =  CheckoutInfo.Instance;
            ViewBag.OrderID = chk.OrderNumber;
            ViewBag.OrderToPay = chk.PaymentTotal;
            return View();
        }
        public ActionResult NelnetRedirectConfirmation()
        {
            //TAKE OUT OF THE IFRAME
            if (Request["top"] == null)
            {
                var url = Request.RawUrl + "&top=1";
                return View(
                    viewName: "_JavaScriptTopRedirect",
                    model: url
                );
            }
            double total = double.Parse(Request["transactionTotalAmount"]) / 100;
            ViewBag.OrderTotal = total;
            ViewBag.OrderNo = Request["orderNumber"];
            CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
            CreditCardPaymentModel.PaymentType = Request["transactionAcountType"];

            string transactionid = Request["transactionId"];
            if (transactionid == "" || transactionid == null)
                transactionid = Request.Form["transactionId"];
            if (transactionid == "" || transactionid == null)
                transactionid = Request.Headers["transactionId"];

            CreditCardPaymentModel.PaymentNumber = transactionid;
            CreditCardPaymentModel.LongOrderId = transactionid;
            string transactionStatus = Request["transactionStatus"];
            ViewBag.transactionid = transactionid;
            CreditCardPaymentModel.ActiveCCPayMethod = "Nelnet";
            ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS
            CreditCardPaymentModel.TotalPaid = total;
            if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
            {
                //  ViewBag.Result = "Session Expired.";
                EnrollmentFunction enrollment = new EnrollmentFunction();
                AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(ViewBag.OrderNo));
            }
            PostBackDateCapture(ViewBag.OrderNo);
            if (transactionStatus.Trim() == "1")
            {
                //enrollment.ApproveEnrollment(CreditCardPaymentModel, Request["orderNumber"]);
                ViewBag.ApproveNelNet = "1";
                Empty(); // clear cart
            }
            else
            {
                ViewBag.ApproveNelNet = "0";
                Empty(); // clear cart
            }
            return View();
        }

        public ActionResult AuthorizeRedirectSilentPost()
        {
            //Repost Anet result

            ViewBag.x_trans_id = Request["x_trans_id"];
            ViewBag.x_auth_code = Request["x_auth_code"];
            ViewBag.x_description = Request["x_description"];
            ViewBag.x_invoice_num = Request["x_invoice_num"];
            ViewBag.x_amount = Request["x_amount"];
            ViewBag.x_response_code = Request["x_response_code"];
            ViewBag.x_response_reason_code = Request["x_response_reason_code"];
            ViewBag.x_response_reason_text = Request["x_response_reason_text"];

            HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
            var request = context.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            string referrer = HttpContext.Request.RawUrl;
            string message = "";
            if (referrer != null)
            {
                if ((referrer.ToString().ToLower().Contains("secure.authorize.net")) || referrer.ToString().ToLower().Contains("test.authorize.net") || referrer.ToString().ToLower().Contains("/public/cart/authorizeredirectsilentpost") || referrer.ToString().ToLower().Contains("/public/cart/authorizeredirectsilentpost"))
                {
                    ViewBag.PostUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/AuthorizeRedirectConfirmation";
                    message = "Successful Payment.";
                }
                else
                {
                    ViewBag.PostUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "/Public/Course/Browse?referrer=" + referrer;
                    message = "Invalid referrer" + referrer.ToString();

                }
            }
            else
            {
                ViewBag.PostUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "/Public/Course/Browse?referrer=" + referrer;
                message = "Invalid referrer( Direct link access)";
            }

            var log = new AuditTrail();
            log.RoutineName = "Cart AuthorizeNet Redirect Payment-" + ViewBag.x_invoice_num;
            log.AuditDate = DateTime.Now;
            log.ShortDescription = message;
            log.DetailDescription = "Amount: " + Request["x_amount"] + " - " + Request["x_description"];
            log.ATErrorMsg = "" + Request["x_trans_id"];
            log.AuditAction = "";
            Gsmu.Api.Logging.LogManager.GenericLogToAuditTrail(log);
            return PartialView();
        }

        [PaymentCustomSecurityforCrossSiteForgeryAttributes]
        public ActionResult AuthorizeRedirectConfirmation()
        {
            var a = AuthorizationHelper.CurrentUser;
            ViewBag.IsTranscript = false;
            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.DetailDescription = Request["x_trans_id"];
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Cart Checkout Confirmation Page" + AuthorizationHelper.CurrentUser.LoggedInUserType + " -ANET Redirect Payment";
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    try
                    {
                        foreach (var item in CourseShoppingCart.Instance.Items)
                        {

                            Audittrail.AuditAction = item.Course.COURSEID.ToString() + " " + item.LineTotal + ";";
                        }

                    }
                    catch { }
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }
            //Sample Link for testing.
            //Public/Cart/AuthorizeRedirectSilentPost?x_description=description:C1BESU21N573E0H&x_trans_id=0000&x_auth_code=909090&x_amount=20&x_response_code=1
            //Values: Status:  x TxID: 0000 x AuthNum: 909090 x Desc: description:C1BESU21N573E0H x Amount: 20

            //This is using ANET Server Integration Method (SIM) with Receipt Option of Relay Response
            //more info http://developer.authorize.net/api/howitworks/sim/
            //Relay Response page 53 http://www.authorize.net/support/SIM_guide.pdf
            //note: Domain should be registered in Response/Receipt URLs page in the Merchant Interface for whitelisting ex. http://dev250.gosignmeup.com
            //      only Domain port 80 and 443 will be accepted

            var chkout = CheckoutInfo.Instance;

            string payment_status = Request["x_response_code"];
            string transactionID = Request["x_trans_id"];
            string authnum = Request["x_auth_code"];
            string desc = Request["x_description"];
            string invoice_num = Request["x_invoice_num"];
            string payment_gross = Request["x_amount"];
            string response_code = Request["x_response_code"];
            string response_reason_code = Request["x_response_reason_code"];
            string response_reason_text = Request["x_response_reason_text"];

            chkout.HasActivePaymentProcess = true;
            EnrollmentFunction enrollment = new EnrollmentFunction();
            AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(invoice_num));

            ViewBag.Info = "-rc:" + Request["rc"] +
                "-x_trans_id:" + Request["x_trans_id"] +
                "-x_auth_code:" + Request["x_auth_code"] +
                "-x_amount:" + Request["x_amount"] +
                "-x_invoice_num:" + Request["x_invoice_num"] +
                "-x_response_code:" + Request["x_response_code"] +
                "-x_response_reason_code:" + Request["x_response_reason_code"] +
                "-x_receipt_link_url:" + Request["x_receipt_link_url"] +
                "-x_description:" + Request["x_description"] +
                "-desc:" + Request["desc"];
            //throw new Exception(ViewBag.Info);

            HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
            var request = context.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            string t_HREFSUCCESS = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ShowConfirmationReceipt?OrderNumber=" + invoice_num;

            ViewBag.AnetReceiptLink = t_HREFSUCCESS;
            ViewBag.response_code = response_code;
            ViewBag.response_reason_code = response_reason_code;
            ViewBag.response_reason_text = response_reason_text;


            //tracking order before GSMU process Silent post
            AuditTrail Anetaudit = new AuditTrail();
            Anetaudit.AuditDate = DateTime.Now;
            Anetaudit.UserName = "PreSilent_PaymentTracker";
            string QString = "";
            foreach (String key in Request.QueryString.AllKeys)
            {
                QString = QString + "Key: " + key + " Value: " + Request.QueryString[key];
            }
            Anetaudit.AuditAction = QString;
            Anetaudit.DetailDescription = "Values: Status: " + payment_status + " x TxID: " + transactionID + " x AuthNum: " + authnum + " x Desc: " + desc + " x Amount: " + payment_gross;
            Anetaudit.ShortDescription = "response code: " + response_code;
            Anetaudit.RoutineName = "CartController.cs";
            using (var db = new SchoolEntities())
            {

                try
                {
                    db.AuditTrails.Add(Anetaudit);
                    db.SaveChanges();
                }

                catch { }
            }

            if ((desc != null) && (desc.Trim() != "") && (response_code == "1" || response_code == "4"))
            {

                String OrderNumber = invoice_num;
                String RespMsg = "completed";
                if (response_code == "4")
                {
                    RespMsg = "pending";
                }

                chkout.PaymentTotal = decimal.Parse(payment_gross);
                CreditCardPaymentModel CCpaymodel = new CreditCardPaymentModel();

                CCpaymodel.ActiveCCPayMethod = "AuthorizeNetRedirect";
                CCpaymodel.PaymentType = "CC";
                CCpaymodel.PaymentNumber = transactionID;
                CCpaymodel.LongOrderId = authnum;
                CCpaymodel.TotalPaid = Double.Parse(payment_gross);
                CCpaymodel.RespMsg = RespMsg;
                CCpaymodel.Result = RespMsg;
                CCpaymodel.AuthNum = authnum;
                CCpaymodel.RefNumber = authnum;

                if (chkout.IsValidOrderNumber(OrderNumber))
                {

                    ViewBag.IsTranscript = false;
                    enrollment.ApproveEnrollment(CCpaymodel, OrderNumber.Trim());
                    EmailFunction EmailFunction = new EmailFunction();
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {

                        using (var db = new SchoolEntities())
                        {
                            var rosteritems = db.Course_Rosters.Where(x => (x.MasterOrderNumber == OrderNumber)).DistinctBy(ordernum => ordernum.OrderNumber).ToList();
                            foreach (var st in rosteritems)
                            {

                                EmailFunction.SendConfirmationEmail(payment_gross, st.OrderNumber, Request.Url.Host, "anetsupconfirmation");
                            }
                        }
                    }
                    else
                    {

                        EmailFunction.SendConfirmationEmail(payment_gross, OrderNumber, Request.Url.Host, "anetregconfirmation");
                    }
                    var itemData = CourseShoppingCart.Instance.Items.LastOrDefault();
                    if (itemData != null)
                    {
                        var checkOutInfo = CheckoutInfo.Instance;
                        bool isPartial = itemData.IsPartialPayment;

                        if (isPartial == false || checkOutInfo.PaymentCaller == "paynowuserdash")
                        {
                            Empty();
                        }
                    }

                    //tracking order during GSMU process Silent post
                    AuditTrail Anetaudit1 = new AuditTrail();
                    Anetaudit1.AuditDate = DateTime.Now;
                    Anetaudit1.UserName = "Process_PaymentTracker";
                    Anetaudit1.AuditAction = "Card Payment";
                    Anetaudit1.DetailDescription = "Values: Ordernum: " + OrderNumber + " x TxID: " + transactionID + " x AuthNum: " + authnum + " x Desc: " + desc + " x Amount: " + Double.Parse(payment_gross);
                    Anetaudit1.ShortDescription = "response code: " + response_code;
                    Anetaudit1.RoutineName = "CartController.cs";
                    using (var db = new SchoolEntities())
                    {
                        try
                        {
                            var orderinprogress = (from order in db.OrderInProgresses where order.OrderNumber == OrderNumber || order.MasterOrderNumber== OrderNumber select order).FirstOrDefault();
                            if (orderinprogress != null)
                            {
                                orderinprogress.postbackdate = DateTime.Now;
                            }
                            db.AuditTrails.Add(Anetaudit1);
                            db.SaveChanges();
                        }
                        catch { }
                    }

                    if (haiku.Configuration.Instance.ExportRosterToHaikuAfterCheckout)
                    {
                        try
                        {
                            haiku.HaikuExport.SynchronizeOrder(OrderNumber);
                        }
                        catch (Exception e)
                        {
                            ObjectHelper.AddRequestMessage(this, string.Format(
                                "Was trying to synchronize the order with Learning for exporting the roster. Got error: {0}", e.Message
                            ));
                            Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Haiku for exporting the roster.", e);
                        }
                    }

                    if (canvas.Configuration.Instance.ExportEnrollmentAfterCheckout)
                    {
                        try
                        {
                            canvas.CanvasExport.SynchronizeOrder(OrderNumber);
                        }
                        catch (Exception e)
                        {
                            ObjectHelper.AddRequestMessage(this, string.Format(
                                "Was trying to synchronize the order with Canvas for exporting the roster. Got error: {0}", e.Message
                            ));
                            Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Canvas for exporting the roster at ANET confirmation", e);
                        }
                    }
                }
                else
                {
                    // Transcript Payment
                    ViewBag.IsTranscript = true;
                    int courseid = enrollment.ApproveTranscriptPayment(CCpaymodel, OrderNumber.Trim());
                    if (AuthorizationHelper.CurrentStudentUser == null)
                    {
                        ViewBag.FromAdmin = true;
                        ViewBag.AdminLink = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "/courses_attendance_detail.asp?cid=" + courseid.ToString() + "&coursetype=0";
                    }
                    else
                    {
                        if (desc.Contains("(from Admin)"))
                        {
                            ViewBag.FromAdmin = true;
                            ViewBag.AdminLink = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "/courses_attendance_detail.asp?cid=" + courseid.ToString() + "&coursetype=0";

                        }
                        else
                        {
                            ViewBag.FromAdmin = false;
                        }
                    }
                }
                //throw new Exception("ViewBag.IsTranscript " + ViewBag.IsTranscript.ToString() + chkout.IsValidOrderNumber(OrderNumber).ToString());

            }
            return View();
        }


        public ActionResult PaypalRedirectConfirmation()
        {

            try
            {
                if (Request["top"] == null)
                {
                    var url = Request.RawUrl + "&top=1";
                    return View(
                        viewName: "_JavaScriptTopRedirect",
                        model: url
                    );
                }

                ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS
                CreditCardPaymentModel creditCardPaymentModel = new CreditCardPaymentModel();
                EnrollmentFunction enrollment = new EnrollmentFunction();

                string amount = Request.QueryString["AMT"] == null ? "0.00" : Request.QueryString["AMT"];
                string token = Session["token"] != null ? Session["token"].ToString() : Request.QueryString["SECURETOKENID"];
                string orderNumber = string.Empty;

                if ((Request.QueryString["RESULT"] != null && Request.QueryString["RESULT"] == "0") && Request.QueryString["RESPMSG"] != null)
                {
                    //CHECK IF THE AMOUNT IS PAID IN FULL
                    token = string.IsNullOrEmpty(token.Replace("MySecTokenID-", "")) ? "" : token.Replace("MySecTokenID-", "");
                    orderNumber = Session["strOrderNo"] != null ? (string)Session["strOrderNo"] : token;
                    creditCardPaymentModel.TotalPaid = double.Parse(amount);
                    if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
                        AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(orderNumber));
                    PostBackDateCapture(orderNumber);
                    creditCardPaymentModel.FirstName = (Request.QueryString["FIRSTNAME"] != null ? Request.QueryString["FIRSTNAME"] : "");
                    creditCardPaymentModel.LastName = (Request.QueryString["LASTNAME"] != null ? Request.QueryString["LASTNAME"] : "");
                    creditCardPaymentModel.Address = (Request.QueryString["BILLTOSTREET"] != null ? Request.QueryString["BILLTOSTREET"] : "") + ", "
                        + (Request.QueryString["BILLTOCITY"] != null ? Request.QueryString["BILLTOCITY"] : "") + ", "
                        + (Request.QueryString["BILLTOSTATE"] != null ? Request.QueryString["BILLTOSTATE"] : "") + ", "
                        + (Request.QueryString["BILLTOZIP"] != null ? Request.QueryString["BILLTOZIP"] : "") + ", "
                        + (Request.QueryString["BILLTOCOUNTRY"] != null ? Request.QueryString["BILLTOCOUNTRY"] : "");

                    if (!string.IsNullOrEmpty(Request.QueryString["EXPDATE"]))
                    {
                        creditCardPaymentModel.ExpiryMonth = Request.QueryString["EXPDATE"].ToString().Substring(0, 2);
                        creditCardPaymentModel.ExpiryYear = Request.QueryString["EXPDATE"].ToString().Substring(2, 2);
                    }

                    string paytype = "PayPal",
                    authCode = Request.QueryString["AUTHCODE"],
                    refNumber = Request.QueryString["PNREF"],
                    resultResponseMsg = Request.QueryString["RESPMSG"],
                    paymentNumber = Request.QueryString["PPREF"] != null ? Request.QueryString["PPREF"] : "";

                    //make sure that these information are saved if paypal is used on checkout
                    //these are important fields
                    //ref. ticket no. 19716
                    creditCardPaymentModel.PaymentType = paytype;
                    creditCardPaymentModel.LongOrderId = token;
                    creditCardPaymentModel.AuthNum = authCode;
                    creditCardPaymentModel.RefNumber = refNumber;
                    creditCardPaymentModel.RespMsg = resultResponseMsg;
                    creditCardPaymentModel.Result = resultResponseMsg;
                    creditCardPaymentModel.PaymentNumber = paymentNumber;
                    CheckoutInfo.Instance.PaypalTempData = null;
                    if ((CheckoutInfo.Instance.PaypalTempData == null) || (CheckoutInfo.Instance.PaypalTempData != Request.QueryString["SECURETOKENID"]))
                    {

                        if (AuthorizationHelper.CurrentUser.IsLoggedIn)
                        {
                            EmailFunction PaypalEmailFunction = new EmailFunction();
                            enrollment.ApproveEnrollment(creditCardPaymentModel, orderNumber);
                            
                            if(CheckoutInfo.Instance.PaymentCaller != "paynowuserdash")
                                PaypalEmailFunction.SendConfirmationEmail(amount, orderNumber, Request.Url.Host, "paypalconfirmation");

                            if (haiku.Configuration.Instance.ExportRosterToHaikuAfterCheckout)
                            {
                                try
                                {
                                    haiku.HaikuExport.SynchronizeOrder(orderNumber);
                                }
                                catch (Exception e)
                                {
                                    ObjectHelper.AddRequestMessage(this, string.Format(
                                        "Was trying to synchronize the order with Learning for exporting the roster. Got error: {0}", e.Message
                                    ));
                                    Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Haiku for exporting the roster.", e);
                                }
                            }

                            if (canvas.Configuration.Instance.ExportEnrollmentAfterCheckout)
                            {
                                try
                                {
                                    canvas.CanvasExport.SynchronizeOrder(orderNumber);
                                }
                                catch (Exception e)
                                {
                                    ObjectHelper.AddRequestMessage(this, string.Format(
                                        "Was trying to synchronize the order with Canvas for exporting the roster. Got error: {0}", e.Message
                                    ));
                                    Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Canvas for exporting the roster at ANET confirmation", e);
                                }
                            }
                        }

                        var checkOutInfo = CheckoutInfo.Instance;
                        var itemData = CourseShoppingCart.Instance.Items.LastOrDefault();

                        if (itemData != null && checkOutInfo != null)
                        {
                            bool isPartial = itemData.IsPartialPayment;
                            decimal lineTotal = itemData.LineTotal;
                            if (isPartial == false || checkOutInfo.PaymentCaller == "paynowuserdash")
                            {
                                Empty();
                            }
                        }
                        CheckoutInfo.Instance.PaypalTempData = orderNumber;
                    }
                    else
                    {
                        ViewBag.ResponseMessage = "Invalid POST.";
                        return RedirectToAction("Browse", "Course", new
                        {
                            area = "Public",
                            paypalRedirect = "1",
                            orderNumberEmpty = "1"
                        });
                    }
                    //END CHECK
                }
                else if (!string.IsNullOrEmpty(Request.QueryString["RESULT"]) && Request.QueryString["RESULT"] != "0")
                {
                    //LOG THE ERROR RECEIVED OR THE RESPONSE TO AUDITTRAIL
                    token = string.IsNullOrEmpty(token.Replace("MySecTokenID-", "")) ? "" : token.Replace("MySecTokenID-", "");
                    orderNumber = Session["strOrderNo"] != null ? (string)Session["strOrderNo"] : token;
                    AuditTrail trail = new AuditTrail()
                    {
                        RoutineName = "CartController.cs-" + orderNumber,
                        ShortDescription = "Error Occured in PaypalRedirectConfirmation - REJECTED ",
                        DetailDescription = "Rejection Error Code : " + Request.QueryString["RESULT"] + ", Message : " + Request.QueryString["RESPMSG"],
                        AuditDate = System.DateTime.Now,
                        CourseID = 0,
                        StudentID = 0
                    };
                    CartAuditLogger(trail);
                }

                if (!string.IsNullOrEmpty(orderNumber))
                {
                    ViewBag.Amount = String.Format("{0:0.00}", double.Parse(amount));
                    ViewBag.orderno = orderNumber;
                    ViewBag.Url = System.Web.HttpContext.Current.Request.Url.AbsoluteUri;

                    var model = new Gsmu.Api.Data.School.CourseRoster.OrderModel(orderNumber);
                    ViewBag.ResponseMessage = Request.QueryString["RESPMSG"];
                    return View("PaypalRedirectConfirmation", model);
                }
                else
                {
                    AuditTrail trail = new AuditTrail()
                    {
                        RoutineName = "CartController.cs",
                        ShortDescription = "Error Occured in PaypalRedirectConfirmation.",
                        DetailDescription = "Error Occured in PaypalRedirectConfirmation, ordernumber is empty.",
                        AuditDate = System.DateTime.Now,
                        CourseID = 0,
                        StudentID = 0
                    };
                    CartAuditLogger(trail);
                    return RedirectToAction("Browse", "Course", new
                    {
                        area = "Public",
                        paypalRedirect = "1",
                        orderNumberEmpty = "1"
                    });
                }
            }
            catch (Exception ex)
            {
                //LOG THE INFORMATION
                var DetailDescription = "";
                string orderNumber = ", Order Number : " + (Session["strOrderNo"] == null ? "Ordernumber is Nulll " : (string)Session["strOrderNo"]);
                if (ex.InnerException != null)
                {
                    DetailDescription = "Error Stack Trace : " + (string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message) + " - Message : " + ex.InnerException.Message;

                }
                else
                {
                    DetailDescription = ex.Message;
                }
                AuditTrail trail = new AuditTrail()
                {
                    RoutineName = "CartController.cs-" + orderNumber,
                    ShortDescription = "Error Occured in PaypalRedirectConfirmation",

                    DetailDescription = DetailDescription, // "Error Stack Trace : " + (string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message) + " - Message : " + ex.InnerException.Message,
                    AuditDate = System.DateTime.Now,
                    ATErrorMsg = "", // LOG IF ERROR AND SEE IF THERE ARE FIELDS THAT ARE EMPTY
                    CourseID = 0,
                    StudentID = 0
                };
                CartAuditLogger(trail);
                return RedirectToAction("Browse", "Course", new
                {
                    area = "Public",
                    paypalRedirect = "1",
                    serverErrorOccured = "1"
                });
            }
        }
        [AcceptVerbs(WebRequestMethods.Http.Post)]
        public ActionResult ConvergeRedirectConfirmation()
        {
            string error_code = Request["errorCode"];
            string errorMessage = Request["errorMessage"];
            string errorName = Request["errorName"];
            string result = Request["ssl_result"];
            string message = Request["ssl_result_message"];
            string invoice_number = Request["ssl_invoice_number"];
            EnrollmentFunction enrollment = new EnrollmentFunction();
            if (string.IsNullOrEmpty(error_code) && result == "0")
            {
                string amount = Request["ssl_amount"];
                string approval_code = Request["ssl_approval_code"];
                string card_number = Request["ssl_card_number"];
                string transaction_id = Request["ssl_txn_id"];

                ViewBag.OrderNumber = invoice_number;
                ViewBag.OrderTotal = amount;
                ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS

                CreditCardPaymentModel creditCardPaymentModel = new CreditCardPaymentModel();

                creditCardPaymentModel.PaymentType = "Converge";
                creditCardPaymentModel.TotalPaid = double.Parse(amount);
                creditCardPaymentModel.RespMsg = message;
                creditCardPaymentModel.Result = result;
                creditCardPaymentModel.CardNumber = card_number;
                creditCardPaymentModel.OrderNumber = invoice_number;
                creditCardPaymentModel.RefNumber = transaction_id;
                creditCardPaymentModel.LongOrderId = transaction_id;
                creditCardPaymentModel.AuthNum = approval_code;
                //approve enrollment
                enrollment.ApproveEnrollment(creditCardPaymentModel, invoice_number);
                if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
                    AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(invoice_number));
                PostBackDateCapture(invoice_number);
                //clear cart
                Empty();

                //audit
                string QString = "";
                foreach (string key in Request.QueryString.AllKeys)
                {
                    QString = QString + "Key: " + key + " Value: " + Request.QueryString[key];
                }
                AuditTrail convergeAuditTrail = new AuditTrail()
                {
                    UserName = "ResponseData_PaymentTracker",
                    AuditAction = QString,
                    DetailDescription = "Message : " + message,
                    ShortDescription = "response code: " + approval_code,
                    RoutineName = "CartController.cs"
                };
                CartAuditLogger(convergeAuditTrail);
                ViewBag.IsConverge = CreditCardPaymentHelper.UseElavon;
                return RedirectToAction("ShowConfirmationReceipt", new { OrderNumber = invoice_number }); ;
            }
            return RedirectToAction("ShowConfirmationReceipt", new { OrderNumber = invoice_number, ResponseMessage = message });
        }
        public ActionResult CashNetRedirectConfirmation()
        {
            string result = Request["result"];
            string invoice_number = Request["gl1"];
            string amount = Request["amount1"];
            string message = Request["respmessage"];
            string pmtcode = Request["tx"];
            string authnum = Request["tx"];
            try
            {
                if (invoice_number == "")
                {
                    invoice_number = CheckoutInfo.Instance.OrderNumber;
                }

            }
            catch { }
            EnrollmentFunction enrollment = new EnrollmentFunction();
            if (result == "0")
            {
                CreditCardPaymentModel creditCardPaymentModel = new CreditCardPaymentModel();

                creditCardPaymentModel.PaymentType = "CashNet";
                creditCardPaymentModel.TotalPaid = double.Parse(amount);
                creditCardPaymentModel.RespMsg = message;
                creditCardPaymentModel.Result = result;
                creditCardPaymentModel.OrderNumber = invoice_number;
                creditCardPaymentModel.PaymentNumber = pmtcode;
                creditCardPaymentModel.LongOrderId = pmtcode;
                creditCardPaymentModel.AuthNum = authnum;
                //approve enrollment
                if (!AuthorizationHelper.CurrentUser.IsLoggedIn)
                    AuthorizationHelper.LoginUserBySessionID(enrollment.GetSessionFromOrder(invoice_number));
                PostBackDateCapture(invoice_number);
                enrollment.ApproveCashnetEnrollmentFromSilentPost(creditCardPaymentModel, invoice_number);
                //enrollment.ApproveEnrollment(creditCardPaymentModel, invoice_number); //comment out, this is just a duplicate of cashnet approving order
                EmailFunction CashnetEmailFunction = new EmailFunction();
                CashnetEmailFunction.SendConfirmationEmail(amount, invoice_number, Request.Url.Host, "cashnetconfirmation");
            }
            return RedirectToAction("ShowConfirmationReceipt", new { OrderNumber = invoice_number }); ;
        }
        public ActionResult CashNetSilentPost()
        {
            string result = Request["result"];
            string invoice_number = Request["gl1"];
            string amount = Request["amount1"];
            string message = Request["respmessage"];
            string pmtcode = Request["pmtcode"];
            try
            {
                if (invoice_number == "")
                {
                    invoice_number = CheckoutInfo.Instance.OrderNumber;
                }

            }
            catch { }
            EnrollmentFunction enrollment = new EnrollmentFunction();
            if (result == "0")
            {
                CreditCardPaymentModel creditCardPaymentModel = new CreditCardPaymentModel();

                creditCardPaymentModel.PaymentType = "CashNet";
                creditCardPaymentModel.TotalPaid = double.Parse(amount);
                creditCardPaymentModel.RespMsg = message;
                creditCardPaymentModel.Result = result;
                creditCardPaymentModel.OrderNumber = invoice_number;
                creditCardPaymentModel.RefNumber = pmtcode;
                creditCardPaymentModel.LongOrderId = pmtcode;
                creditCardPaymentModel.AuthNum = pmtcode;
                //approve enrollment
                enrollment.ApproveCashnetEnrollmentFromSilentPost(creditCardPaymentModel, invoice_number);

            }
            return RedirectToAction("ShowConfirmationReceipt", new { OrderNumber = invoice_number }); ;
        }
        public ActionResult Confirmation(string OrderNumber = "", string OrderAmount = "0", string PayType = "")
        {

            if (!ValidateUrlReferrer())
            {
                return Content("Invalid Request. Err724.");
            }
            var chkout = CheckoutInfo.Instance;
            if (AuthorizationHelper.CurrentUser.LoggedInUserType == LoggedInUserType.Guest && chkout.PaymentCaller != "paynowuserdash")
            {
                return Content("Your session has been timed out. Please login and try again.");
            }
            ViewBag.IsBBPayGateRedirect = false;
            string strDiscount = "0.00";
            string strOrderNo = OrderNumber.ToString();
            string strPaytype = PayType;
            string otherPaymentType = string.Empty;
            string AuthNumber = "";
            string RefNumber = "";
            string CardName = "";
            string PaymentNumber = "";
            string Result = "";
            if (OrderNumber == "0" || OrderNumber == "")
            {
                strOrderNo = Request["OrderNo"];
                strDiscount = Request["totalDiscount"];
                strPaytype = Request["PaymentType"];
                otherPaymentType = Request["OtherPayment"];

                Result = Request["ChaseResult"];
                AuthNumber = Request["ChaseAuthNum"];
                PaymentNumber = Request["ChasePaymentNumber"];
                RefNumber = Request["ChaseRefNumber"];
                if (CreditCardPaymentHelper.UseChasePayment) { chkout.OrderNumber = strOrderNo; }
            }

            if((chkout.PaymentTotal>chkout.OrderTotal || strPaytype=="" || strPaytype is null) && chkout.OrderTotal!=0)
            {
                return Content("Invalid process.1");
            }
            else
            {
                var roster = new Gsmu.Api.Data.School.CourseRoster.OrderModel(strOrderNo);
                if (AuthorizationHelper.CurrentStudentUser != null && Settings.Instance.GetMasterInfo2().AllowStudentMultiEnroll!=1)
                {
                    if (roster.Student != null)
                    {
                        if (roster.Student.STUDENTID != AuthorizationHelper.CurrentStudentUser.STUDENTID)
                        {
                            return Content("Invalid process.2");
                        }
                    }
                }
            }

            string strAmount = chkout.PaymentTotal.ToString();
            string PaymentStatus = "";
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();

            if (CreditCardPaymentHelper.UseSquare && (otherPaymentType == "CC" || otherPaymentType == "Credit Card" || otherPaymentType == "SquarePayment"))
            {
                return RedirectToAction("SquarePayment");
            }

            try
            {
                if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = Request.UserHostName;
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.RoutineName = "Cart Checkout Confirmation Page" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                    try
                    {
                        foreach (var item in CourseShoppingCart.Instance.Items)
                        {

                            Audittrail.AuditAction = OrderNumber + "|" + strPaytype + "|" + otherPaymentType;
                        }

                    }
                    catch { }
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                }
            }
            catch
            {
            }

            if (double.Parse(strAmount) > 0)
            {
                PaymentsController PaymentsController = new Controllers.PaymentsController();
                CreditCardPaymentModel.PaymentType = Request["PaymentType"];
                CreditCardPaymentModel.PaymentNumber = Request["Paynumber"];
                CreditCardPaymentModel.OrderNumber = strOrderNo;
               
                if (strPaytype == "Credit Card" || strPaytype == "PayPal" || otherPaymentType == "PayPal" || strPaytype == "NelNet")
                {
                    CreditCardPaymentModel.CardNumber = Request["CardNumber"];
                    CreditCardPaymentModel.CardType = Request["CardType"];
                    CreditCardPaymentModel.ExpiryMonth = Request["ExpiryMonth"];
                    CreditCardPaymentModel.ExpiryYear = Request["ExpiryYear"];
                    CreditCardPaymentModel.Address = Request["Address"];
                    CreditCardPaymentModel.State = Request["State"];
                    CreditCardPaymentModel.Zip = Request["Zip"];
                    CreditCardPaymentModel.City = Request["City"];
                    CreditCardPaymentModel.Country = Request["Country"];
                    CreditCardPaymentModel.FirstName = Request["FirstName"];
                    CreditCardPaymentModel.LastName = Request["LastName"];
                    CreditCardPaymentModel.CCV = Request["ccv"];

                    ViewBag.Amount = String.Format("{0:0.00}", double.Parse(strAmount));

                    //Process Payments and get status
                    if (CreditCardPaymentHelper.UsePaygov)
                    {
                        PaymentStatus = PaymentsController.ProcessAndSelectPaymentMerchant(CreditCardPaymentModel, strAmount);
                    }
                    else if (CreditCardPaymentHelper.UsePayPalAdvance || strPaytype == "PayPal" || otherPaymentType == "PayPal")
                    {
                        CreditCardPaymentModel.Email = Request["Email"];
                        PaymentStatus = PaymentPaypal(CreditCardPaymentModel, strAmount);
                    }
                    else
                    {
                        CreditCardPaymentModel.Email = Request["Email"];
                        try
                        {
                            PaymentStatus = PaymentsController.ProcessAndSelectPaymentMerchant(CreditCardPaymentModel, strAmount);

                            ViewBag.Statz = PaymentStatus;
                        }
                        catch (Exception e)
                        {
                            ViewBag.Statz = e.Message;
                            ObjectHelper.AddRequestMessage(this, string.Format(
                                        "Got error: {0}", e.Message
                                    ));
                        }
                    }


                }

                else if (strPaytype == "CC" || strPaytype == "Check")
                {
                    EnrollmentFunction.OrderInprogressToRoster(null, strOrderNo);
                    var roster = new Gsmu.Api.Data.School.CourseRoster.OrderModel(strOrderNo);
                    double ccfee = double.Parse(strAmount) - double.Parse(roster.OrderTotal.ToString());
                    if (CreditCardPaymentHelper.UsePaygov)
                    {
                        //if (roster.SingleRoster.CouponDiscount != null)
                        //{
                        //    ccfee = ccfee + roster.SingleRoster.CouponDiscount.Value;
                        //}
                        ccfee = double.Parse(OrderAmount) - double.Parse(strAmount);

                        CreditCardPaymentModel.AuthNum = Request["pgAuthNum"];
                        CreditCardPaymentModel.FirstName = Request["pgCardName"];
                        chkout.TotalPaid = decimal.Parse(OrderAmount);
                        CreditCardPaymentModel.TotalPaid = double.Parse(OrderAmount);
                        chkout.PaymentTotal = decimal.Parse(OrderAmount);
                    }
                    else if (CreditCardPaymentHelper.UseChasePayment)
                    {
                        CreditCardPaymentModel.PaymentType = strPaytype;
                    }
                    else
                    {
                        ccfee = 0;
                        CreditCardPaymentModel.AuthNum = chkout.transactionid;
                    }
                    CreditCardPaymentModel.CreditCardFee = ccfee;
                    CreditCardPaymentModel.PaymentNumber = chkout.transactionid;
                    CreditCardPaymentModel.RespMsg = chkout.responsemessage;
                    CreditCardPaymentModel.Result = chkout.responsecode;

                    ViewBag.Result = strPaytype;
                    PaymentStatus = "Approved";

                }

                else
                {
                    CreditCardPaymentModel.CreditCardFee = 0;
                    ViewBag.Result = strPaytype;
                    PaymentStatus = "Approved";
                }


            }

            //if OrderAmount = 0
            else
            {
                if (CreditCardPaymentHelper.UseChasePayment)
                {
                    CreditCardPaymentModel.PaymentType = strPaytype;
                }
                PaymentStatus = "Approved";
            }

            //Paypal
            if (PaymentStatus.Contains("iframe"))
            {
                if (strOrderNo != "")
                {
                    Session["strOrderNo"] = strOrderNo;
                }

                ViewBag.Result = "Paypal Ok";
                ViewBag.PaypalIFrame = PaymentStatus;
            }


            //Other Approved Status
            if (PaymentStatus.Contains("Approved"))
            {
                if (PaymentStatus.Contains("-"))
                {
                    CreditCardPaymentModel.LongOrderId = PaymentStatus.Replace("Approved.-", "");
                }
                else
                {
                    CreditCardPaymentModel.LongOrderId = "";
                }


                CreditCardPaymentModel.TotalPaid = double.Parse(strAmount);
                chkout.PaymentResult = PaymentStatus;

                EnrollmentFunction.ApproveEnrollment(CreditCardPaymentModel, strOrderNo);
                EmailFunction EmailFunction = new EmailFunction();
                int sentcounter = 0;
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                    {
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(x => x.OrderNumber).Distinct().ToList())
                        {
                            EmailFunction.SendConfirmationEmail(strAmount, st, Request.Url.Host, "supervisorconfirmation");
                        }
                    }
                }
                else if (AuthorizationHelper.CurrentInstructorUser != null)
                {
                    foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(x => x.OrderNumber).Distinct().ToList())
                    {
                        EmailFunction.SendConfirmationEmail(strAmount, st, Request.Url.Host, "supervisorconfirmation");
                    }
                }
                else if (AuthorizationHelper.CurrentAdminUser != null || AuthorizationHelper.CurrentSubAdminUser!=null)
                {
                   
                    if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                    {
                   
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(x => x.OrderNumber).Distinct().ToList())
                        {
                            EmailFunction.SendConfirmationEmail(strAmount, st, Request.Url.Host, "supervisorconfirmation");
                        }
                    }
                    else
                    {

                        EmailFunction.SendConfirmationEmail(strAmount, strOrderNo, Request.Url.Host, "supervisorconfirmation");
                    }

                }
                else
                {
                    if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                    {
                        if (CourseShoppingCart.Instance.MultipleStudentCourses.Count > 1)
                        {
                            foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(x => x.OrderNumber).Distinct().ToList())
                            {
                                EmailFunction.SendConfirmationEmail(strAmount, st, Request.Url.Host, "studentmultipleconfirmation");
                                sentcounter = sentcounter + 1;
                            }
                        }
                        else
                        {
                            EmailFunction.SendConfirmationEmail(strAmount, strOrderNo, Request.Url.Host, "regularconfirmation");
                        }

                        if (sentcounter > 0)
                        {
                            EmailFunction.SendConfirmationEmail(strAmount, strOrderNo, Request.Url.Host, "regularconfirmation");
                        }
                    }
                    else
                    {
                        EmailFunction.SendConfirmationEmail(strAmount, strOrderNo, Request.Url.Host, "regularconfirmation");
                    }
                }

            }
            //Remove for Ticket 16776: PayPal Incomplete and Cancel
            // else
            // {
            //Redirect payment methods will put temporary the Order to Cancel Mode then after confirmation it will updated to Enrolled 
            //Payment from PayNow Userdash will Not put to Order to Cancelled if not successfull or in the process of redirect  payment  
            // if (chkout.PaymentCaller == "checkout")
            // {
            // EnrollmentFunction.CancelEnrollment(strOrderNo);
            // }
            //}

            // For redirect payments
            HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
            var request = context.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            ViewBag.IsPaygovpayment = CreditCardPaymentHelper.UsePaygov;
            ViewBag.IsAuthorizedRedirect = CreditCardPaymentHelper.UseAuthorizeNetRedirect;
            ViewBag.IsTouchnetRedirect = CreditCardPaymentHelper.UseTouchnetRedirect;
            ViewBag.IsChasePayment = CreditCardPaymentHelper.UseChasePayment;
            ViewBag.IsNelNet = CreditCardPaymentHelper.UseNelNet;
            ViewBag.hostedSecureID = CreditCardPaymentHelper.ChasePaymentMerchantId;
            ViewBag.ChasePaymentServer = CreditCardPaymentHelper.ChasePaymentServer;
            ViewBag.UseCashNetRedirect = CreditCardPaymentHelper.UseCashNetRedirect;
            //ViewBag.UseRevTrak = CreditCardPaymentHelper.UseRevtrak;
            ViewBag.ChaseTemplateUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "ChasePayment_Template.aspx";
            ViewBag.ChaseReturnUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ChaseHPPConfirmation";
            ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GOOGLE ANALYTICS
            ViewBag.ConfirmationNumber = strOrderNo;
            ViewBag.IsSpreedly = CreditCardPaymentHelper.UseSpreedly;
            //Area for Ipay Codes
            ViewBag.UseIpay = CreditCardPaymentHelper.UseIpay;

            //end iPay

            if (CreditCardPaymentHelper.UseNelNet || strPaytype == "Nelnet")
            {

                if (PaymentStatus.Contains("Approved"))
                {
                }
                else
                {
                    PaymentsController PaymentsController = new Controllers.PaymentsController();
                    PaymentStatus = PaymentsController.PaymentNelNet(CreditCardPaymentModel, strAmount);
                    ViewBag.NelNetSource = PaymentStatus;
                }
            }

            if (CreditCardPaymentHelper.UseTouchnetRedirect)
            {
                if (CreditCardPaymentHelper.UseTouchnetTlink)
                {
                    PaymentsController PaymentsController = new Controllers.PaymentsController();
                    ViewBag.TicketName = strOrderNo;
                    ViewBag.Ticket = PaymentStatus;
                }
                ViewBag.touchnetsiteid = Settings.Instance.GetMasterInfo4().touchnetsiteid;
                ViewBag.touchnetserver = Settings.Instance.GetMasterInfo4().TouchnetServer;
                if (AuthorizationHelper.CurrentStudentUser == null)
                {
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        ViewBag.billingname = AuthorizationHelper.CurrentSupervisorUser.FIRST + " " + AuthorizationHelper.CurrentSupervisorUser.LAST;
                    }

                    if (AuthorizationHelper.CurrentInstructorUser != null)
                    {
                        ViewBag.billingname = AuthorizationHelper.CurrentInstructorUser.FIRST + " " + AuthorizationHelper.CurrentInstructorUser.LAST;
                    }
                }
                else
                {
                    ViewBag.billingname = AuthorizationHelper.CurrentStudentUser.FIRST + " " + AuthorizationHelper.CurrentStudentUser.LAST;
                    ViewBag.url = new Uri(System.Web.HttpContext.Current.Request.Url.AbsoluteUri);
                }
            }
            if (CreditCardPaymentHelper.UseBBPaygate)
            {
                if (PaymentStatus == "Redirect")
                {
                    PaymentsController PaymentsController = new Controllers.PaymentsController();
                    ViewBag.BBPaygateTredirectURL = PaymentsController.BuildBlackBoardHPPURL(CreditCardPaymentModel, strAmount);
                    ViewBag.IsBBPayGateRedirect = true;
                }
            }
            if (CreditCardPaymentHelper.UseAuthorizeNetRedirect)
            {
                ViewBag.AuthorizeRedirectLink = CreditCardPaymentHelper.ANRedirectSubmissionLink;
                ViewBag.ANLogin = CreditCardPaymentHelper.ANLogin;
                ViewBag.ANTesting = CreditCardPaymentHelper.ANTesting.ToString();
                Random r = new Random();
                string sequence = (r.Next(0, 1000)).ToString();
                string timestamp = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
                ViewBag.TimeStamp = timestamp;
                if (CreditCardPaymentHelper.ANRedirectSubmissionLink.IndexOf("cbresourcecenter") != -1)
                {
                    // for Coldwellbanker only.
                    ViewBag.Invoice = "618GSMU";
                }
                else
                {
                    ViewBag.Invoice = chkout.OrderNumber;
                }

                if (PaymentStatus == "Redirect")
                {
                    decimal ccfee = EnrollmentFunction.ComputeConvenienceFee(decimal.Parse(strAmount));
                    strAmount = (ccfee + decimal.Parse(strAmount)).ToString();
                }
                PaymentsController pay = new PaymentsController();
                ViewBag.Description = pay.GetDescriptionForAnet();
                strAmount = String.Format("{0:0.00}", double.Parse(strAmount));
                string payload = CreditCardPaymentHelper.ANLogin + "^" + sequence + "^" + timestamp + "^" + strAmount + "^";
                ViewBag.Fingerprint = pay.HMAC_MD5(CreditCardPaymentHelper.ANTranKey, payload);
                ViewBag.AuthorizeRedirectSequnce = sequence;
                //Confirmation Number is being use on Capturing ad Approving Order on Silent Post (Authorize.Net Redirect). Any changes on format will affect the Silent Pot behavior. 
                //Please consider checking SilentFormPost if Confirmation Number is changed format
                ViewBag.x_logo_url = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "admin/images/" + Settings.Instance.GetMasterInfo().AuthorizeNetHeaderLogoImgName;
                ViewBag.ConfirmationNumber = strOrderNo + " - " + ListOfCourseNumbersPerOrderNumber(strOrderNo);
                ViewBag.Amount = strAmount;

                string t_HREFSUCCESS = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/AuthorizeRedirectConfirmation?useACresTok=1&OrderTotal=" + strAmount + "&PaymentType=CC&OrderNo=" + strOrderNo;
                ViewBag.ReceiptLink = t_HREFSUCCESS;
                //ViewBag.RelayLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "admin/anetconfirmed3.asp";
                ViewBag.RelayLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/AuthorizeRedirectSilentPost";
                chkout.AnetReceiptLink = t_HREFSUCCESS;

                //PassUserAddressToANET
                string passUser = ConfigurationManager.AppSettings["PassUserAddressToANET"];
                if (!string.IsNullOrEmpty(passUser))
                {
                    if (bool.Parse(passUser) == true)
                    {

                        var roster = new Gsmu.Api.Data.School.CourseRoster.OrderModel(strOrderNo);
                        ViewBag.passUser = true;
                        ViewBag.x_address = string.IsNullOrEmpty(Request["Address"]) ? "" : Request["Address"];
                        ViewBag.x_city = string.IsNullOrEmpty(Request["State"]) ? "" : Request["State"];
                        ViewBag.x_state = string.IsNullOrEmpty(Request["Zip"]) ? "" : Request["Zip"];
                        ViewBag.x_zip = string.IsNullOrEmpty(Request["City"]) ? "" : Request["City"];
                        ViewBag.x_country = string.IsNullOrEmpty(Request["Country"]) ? "" : Request["Country"];
                    }
                }
                //
            }
            if (CreditCardPaymentHelper.UseCashNetRedirect)
            {
                ViewBag.CashNetOperator = CreditCardPaymentHelper.CashNetOperator;
                ViewBag.CashNetPassword = CreditCardPaymentHelper.CashNetPassword;
                ViewBag.cashnetCustomerCode = CreditCardPaymentHelper.cashnetCustomerCode;
                ViewBag.cashnetItemCode = CreditCardPaymentHelper.cashnetItemCode;
                ViewBag.cashnetItemCodeDesc = CreditCardPaymentHelper.cashnetItemCodeDesc;
                ViewBag.cashnetserver = CreditCardPaymentHelper.cashnetserver;
                ViewBag.FullName = CreditCardPaymentModel.FirstName + ' ' + CreditCardPaymentModel.LastName;
                ViewBag.FirstName = CreditCardPaymentModel.FirstName;
                ViewBag.LastName = CreditCardPaymentModel.LastName;
                ViewBag.Address = CreditCardPaymentModel.Address;
                ViewBag.City = CreditCardPaymentModel.City;
                ViewBag.State = CreditCardPaymentModel.State;
                ViewBag.Zip = CreditCardPaymentModel.Zip;
                ViewBag.emailaddress = CreditCardPaymentModel.Email;
                ViewBag.RedirectLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/CashNetRedirectConfirmation";
            }
            //CONVERGE
            ViewBag.IsConverge = CreditCardPaymentHelper.UseElavon;
            if (CreditCardPaymentHelper.UseElavon)
            {
                string receiptLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ShowConfirmationReceipt?OrderNumber=" + strOrderNo;
                ViewBag.ConvergePaymentServer = CreditCardPaymentHelper.ElavonServer; //"https://demo.myvirtualmerchant.com/VirtualMerchantDemo/process.do";
                ViewBag.Amount = String.Format("{0:0.00}", double.Parse(strAmount));
                ViewBag.RedirectLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ConvergeRedirectConfirmation";
                ViewBag.ConvergeMerchantID = CreditCardPaymentHelper.ElavonMerchantId; //"008132"
                ViewBag.ConvergeUserID = CreditCardPaymentHelper.ElavonUserId;// "008132"; 
                ViewBag.ConvergePIN = CreditCardPaymentHelper.ElavonPin; //"DHLHKT";
                ViewBag.InvoiceNumber = strOrderNo;
                ViewBag.FirstName = CreditCardPaymentModel.FirstName;
                ViewBag.LastName = CreditCardPaymentModel.LastName;
                ViewBag.Address = CreditCardPaymentModel.Address;
                ViewBag.City = CreditCardPaymentModel.City;
                ViewBag.State = CreditCardPaymentModel.State;
                ViewBag.Zip = CreditCardPaymentModel.Zip;
                ViewBag.Country = CreditCardPaymentModel.Country;
                ViewBag.CardNumber = CreditCardPaymentModel.CardNumber;
                ViewBag.CCV = CreditCardPaymentModel.CCV;
                ViewBag.Expire = CreditCardPaymentModel.ExpiryMonth + CreditCardPaymentModel.ExpiryYear;
                ViewBag.ReceiptLink = receiptLink;
                //PaymentConverge(CreditCardPaymentModel, strAmount);
            }
            ViewBag.orderno = strOrderNo;
            var model = new Gsmu.Api.Data.School.CourseRoster.OrderModel(strOrderNo);



            if ((!CreditCardPaymentHelper.UsePayPalAdvance || PaymentStatus.Contains("confirmation")) && strPaytype != "NelNet")
            {
                ViewBag.Result = PaymentStatus;
            }
            if (PaymentStatus.Contains("Approved") && PaymentStatus.Contains("confirmation") == false)
            {
                ViewBag.Result = "Approved";
            }
            //else
            //{
            //    ViewBag.Result = PaymentStatus;
            //}

            if (Settings.Instance.GetMasterInfo2().HidePaymentInfo == 1)
            {
                ViewBag.HidePaymentInfo = "True";
            }
            else
            {
                ViewBag.HidePaymentInfo = "False";
            }



            if (haiku.Configuration.Instance.ExportRosterToHaikuAfterCheckout)
            {
                try
                {
                    haiku.HaikuExport.SynchronizeOrder(strOrderNo);
                }
                catch (Exception e)
                {
                    ObjectHelper.AddRequestMessage(this, string.Format(
                        "Was trying to synchronize the order with Learning for exporting the roster. Got error: {0}", e.Message
                    ));
                    Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Haiku for exporting the roster.", e);
                }
            }

            if (canvas.Configuration.Instance.ExportEnrollmentAfterCheckout)
            {
                try
                {
                    canvas.CanvasExport.SynchronizeOrder(strOrderNo);
                }
                catch (Exception e)
                {
                    ObjectHelper.AddRequestMessage(this, string.Format(
                        "Was trying to synchronize the order with Canvas for exporting the roster. Got error: {0}", e.Message
                    ));
                    Gsmu.Api.Logging.LogManager.LogException("CartController", "Was trying to synchronize the order with Canvas for exporting the roster.", e);
                }
            }
            /*if (Directory.Exists(Server.MapPath("~") + "/admin/CitrixG2W") == true)
            {
                // create registration
				//wpostURL = ClientURLName & "/CitrixG2W/create-registrant.asp?webinarid=" & aParameters(23) & "&studid=" & iStudentID & "&rosterid=" & cRID
				//http.open "POST", wpostURL , true
				//http.SetRequestHeader "Content-Type", "application/x-www-form-urlencoded"
				//http.send()
				//http.WaitForResponse
                string wpostURL = "";
                wpostURL = Settings.Instance.GetMasterInfo4().AspSiteRootUrl + "/CitrixG2W/create-registrant.asp?webinarid=&studid=" + iStudentID + "&rosterid=" + cRID;
                System.Net.ServicePointManager.Expect100Continue = true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                System.Net.HttpWebRequest wrequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(wpostURL);
                wrequest.ContentType = "application/json; charset=utf-8";
                wrequest.Method = "POST";
                using (var streamWriter = new StreamWriter(wrequest.GetRequestStream()))
                {
                    streamWriter.Write("");
                }

                System.Net.HttpWebResponse response = wrequest.GetResponse() as System.Net.HttpWebResponse;

            }*/
            return PartialView("PartialConfirmation", model);
        }

        public ActionResult ShowConfirmationReceipt(string OrderNumber = "", string ResponseMessage = "")
        {

            int? GoogleTracker_status = Settings.Instance.GetMasterInfo3().GoogleTracker_status;

            if (GoogleTracker_status == 1)
                ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GA SCRIPT;
            else
                ViewBag.GoogleAnalyticsSetUpScript = string.Empty; // IF status is not 1 then dont load the GA

            ViewBag.OrderNumber = Request["OrderNumber"];
            ViewBag.Error = 0;
            ViewBag.Message = "";

            if (AuthorizationHelper.CurrentStudentUser == null && AuthorizationHelper.CurrentSupervisorUser == null && AuthorizationHelper.CurrentInstructorUser == null && AuthorizationHelper.CurrentAdminUser == null)
            {
                ViewBag.Error = 1;
                ViewBag.Message = "* Unable to validate your authenticity to display the confirmation content. Please contact Administrator. ErrC214";
            }
            var roster = new Gsmu.Api.Data.School.CourseRoster.OrderModel(OrderNumber);
            if (roster.CourseRosters.Count <= 0)
            {
                ViewBag.Error = 1;

                ViewBag.Message = "* Unable to validate your authenticity to display the confirmation content. Please contact Administrator. ErrC215";
            }
            else
            {
                if ((roster.CourseRosters.Where(cr => cr.Cancel == 1 || cr.Cancel == -1).FirstOrDefault() != null))
                {
                    ViewBag.Error = 1;

                    ViewBag.Message = "* Unable to validate your authenticity to display the confirmation content. Please contact Administrator. ErrC216";
                }
            }
            if (!string.IsNullOrEmpty(ResponseMessage))
            {
                ViewBag.Error = 1;
                ViewBag.Message = "* Unable to process your request, transaction declined by the provider. Please contact Administrator. ErrC217 - " + ResponseMessage;
            }
            return View();
        }

        public ActionResult ConfirmationReceipt(string OrderNumber = "")
        {
            
            var model = new Gsmu.Api.Data.School.CourseRoster.OrderModel(OrderNumber);
            ViewBag.Result = "Approved";
            ViewBag.IsTouchnetRedirect = false;
            ViewBag.IsPaygovpayment = false;
            ViewBag.IsAuthorizedRedirect = false;
            ViewBag.IsChasePayment = false;
            ViewBag.IsNelNet = false;
            ViewBag.IsBBPayGateRedirect = false;

            return PartialView("PartialConfirmation", model);
        }

        public string SaveMaterialtoRoster(string MaterialList)
        {

            foreach (CourseShoppingCartItem item in CourseShoppingCart.Instance.Items)
            {
                if (item.Materials != null)
                {
                    foreach (Material material in item.Materials)
                    {
                        // if (MaterialList.Contains(item.Course.COURSEID + "|" + material.productID.ToString()) == false) // ASSUME THAT THE NEWLY ADDED/UPDATED MATERIAL DATA SHOULD NOT EXIST FROM THE PREVIOUS INFORMATION
                        //  {
                        MaterialList = MaterialList + "," + item.Course.COURSEID + "|" + material.productID.ToString() + "|" + material.QuantityPurchased.ToString();
                        //  }
                    }
                }
            }
            string[] MixedMaterials = MaterialList.Split(',');
            string strDeletedMaterials = "";
            foreach (string material in MixedMaterials)
            {
                if (material.Contains('-'))
                {
                    strDeletedMaterials = strDeletedMaterials + "," + material;
                    int i = MaterialList.IndexOf(material);
                    if (i > 0)
                        MaterialList = MaterialList.Remove(i, material.Length).Insert(i, "");

                    int j = MaterialList.IndexOf(material.Replace("-", ""));
                    if (j > 0)
                        MaterialList = MaterialList.Remove(j, material.Replace("-", "").Length).Insert(j, "");
                }

            }

            return MaterialList;
        }

        /// <summary>
        /// Removes items from the cart.
        /// </summary>
        /// <returns></returns>
        public ActionResult Empty()
        {
            CourseShoppingCart.Instance.Empty();
            CourseShoppingCart.Instance.MultipleStudentCourses = new List<CourseMultipleStudentItem>();
            CourseShoppingCart.Instance.CurrentOrderNumber = null;
            //CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent = 0;
            //Session.Remove("quantity_array");
            var result = new JsonResult()
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };

            result.Data = new
            {
                status = CourseShoppingCart.Instance.Status,
                success = true
            };

            return result;
        }

        public ActionResult AddMembership(int MembershipID)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                MembershipShoppingCart.Instance.AddMembership(MembershipID);
                result.Data = new
                {
                    status = MembershipShoppingCart.Instance.Status,
                    success = true
                };

            }
            catch { }

            return result;
        }


        public ActionResult RemoveEvents(int eventParent = 0)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            List<int> EventIdsToDel = new List<int>();

            if (CourseShoppingCart.Instance.GetCourseItem(eventParent) != null && CourseShoppingCart.Instance.GetCourseItem(eventParent).EventParent > 0)
            {
                foreach (CourseShoppingCartItem item in CourseShoppingCart.Instance.Items.Where(p => p.EventParent == eventParent))
                {
                    EventIdsToDel.Add(item.CourseId);
                }
                foreach (int evntIds in EventIdsToDel)
                {
                    RemoveCourse(evntIds);
                }
            }
            result.Data = new
            {
                status = CourseShoppingCart.Instance.Status,
                success = true,
                EventIdsToDel = EventIdsToDel.ToList()
            };
            return result;
        }



        /// <summary>
        /// This is based on the original ASP site, complete replicate.
        /// </summary>
        public ActionResult AddCourse(int courseId, int? coursePricingOptionId, int? courseChoiceId, List<int> materialsIds = null, int eventParent = 0, List<int> eventIds = null, List<string> selectedcredits = null, string accessCode = null, List<CourseExtraParticipant> extraParticipants = null, int studentId = 0, List<int> qty = null, bool? enrollStudentOnHouseholdRequired = null, bool? passrequirements = false)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                //Check Principal Student for Multiple Order purposes. Need to set studentId if Supervisor's login.
                if ((studentId == 0) && (CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent != 0) && (CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent != null))
                {
                    studentId = CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent;

                    //  coursePricingOptionId = CourseShoppingCart.Instance.GetCourseItem(courseId).PricingModel.CoursePricingOption.CoursePricingOptionId;

                }
                if (CourseShoppingCart.Instance.GetCourseItem(courseId) != null)
                {
                    accessCode = CourseShoppingCart.Instance.GetCourseItem(courseId).Course.courseinternalaccesscode;
                    if (CourseShoppingCart.Instance.GetCourseItem(courseId).PricingModel != null)
                    {
                        if (CourseShoppingCart.Instance.GetCourseItem(courseId).PricingModel.CoursePricingOption != null)
                        {
                            coursePricingOptionId = CourseShoppingCart.Instance.GetCourseItem(courseId).PricingModel.CoursePricingOption.CoursePricingOptionId;
                        }
                        else
                        {
                            coursePricingOptionId = 0;
                        }
                    }
                }

                CourseEnrollmentStatistics stats = null;
                var course = new CourseModel(courseId);
                stats = course.Course.EnrollmentStatistics;

                int spaceAvailable = stats.SpaceAvailable < 0 ? 0 : stats.SpaceAvailable;
                int waitSpace = stats.WaitSpaceAvailable < 0 ? 0 : stats.WaitSpaceAvailable;

                if (eventParent == 0) //do not evaluate events
                {
                    if (enrollStudentOnHouseholdRequired == null && (spaceAvailable + waitSpace <= 1) && (coursePricingOptionId.HasValue && coursePricingOptionId > 0))
                    {
                        result.Data = new
                        {
                            status = "household pending",
                        };
                        return result;
                    }
                }

                if (course.Course.CourseColorGrouping != null && course.Course.CourseColorGrouping != 0)
                {
                    int studentid_forgroup = 0;
                    if (AuthorizationHelper.CurrentStudentUser != null && studentId == 0)
                    {
                        studentid_forgroup = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                    }
                    CourseColorGroupingHelper CourseColorGroupingHelper = new CourseColorGroupingHelper();
                    var groupcheck = CourseColorGroupingHelper.GetColorGroupingLimitRegistration(course.Course.CourseColorGrouping);
                    if (groupcheck != null)
                    {
                        if (groupcheck.IsLimitedtoOneRegistration)
                        {
                            if (CourseShoppingCart.Instance.Items.Where(cartitem => groupcheck.Courses.Contains(cartitem.CourseId)).Count() > 0)
                            {
                                result.Data = new
                                {
                                    status = "Color_Groupings_Duplicate",
                                    message = Settings.Instance.GetMasterInfo3().CourseGroupAlreadyInCartMessage.Replace("[Course Name]", groupcheck.GroupName),
                                };

                                return result;
                            }
                            if (CourseColorGroupingHelper.IsEnrolledCoursesinSameGroupings(course.Course.CourseColorGrouping, studentid_forgroup))
                            {
                                result.Data = new
                                {
                                    status = "Color_Groupings_Duplicate",
                                    message = Settings.Instance.GetMasterInfo3().CourseGroupAlreadyEnrolled.Replace("[Course Name]", groupcheck.GroupName),
                                };

                                return result;
                            }
                        }
                    }
                }


                CourseShoppingCart.Instance.AddCourse(courseId, coursePricingOptionId, courseChoiceId, materialsIds, eventParent, eventIds, selectedcredits, accessCode, false, false, null, extraParticipants, studentId, qty, passrequirements);
                Session["quantity_array"] = qty;
                result.Data = new
                {
                    status = CourseShoppingCart.Instance.Status,
                    success = true,
                    extraParticipantRequired = false,
                    AdminCheckAccessCode = CourseShoppingCart.Instance.AdminCheckAccessCode,
                    AdminCheckCourseRequirements = CourseShoppingCart.Instance.AdminCheckCourseRequirements,
                };
            }
            catch (Exception e)
            {
                string ExtraParticipantRequiredFields = "";
                CourseEnrollmentStatistics stats = null;
                if (e is CourseExtraParticipantMissingException)
                {
                    var course = new CourseModel(courseId);
                    stats = course.Course.EnrollmentStatistics;
                    try
                    {
                        ExtraParticipantRequiredFields = System.Configuration.ConfigurationManager.AppSettings["HouseHoldRequiredField"];
                        if (ExtraParticipantRequiredFields == null)
                        {
                            ExtraParticipantRequiredFields = "all";
                        }
                    }
                    catch
                    {
                        ExtraParticipantRequiredFields = "all";
                    }
                }

                result.Data = new
                {
                    error = e.Message,
                    success = false,
                    extraParticipantRequired = e is CourseExtraParticipantMissingException,
                    ExtraParticipantRequiredFields = ExtraParticipantRequiredFields,
                    statistics = stats
                };
            }
            return result;
        }

        /// <summary>
        /// This is based on the original ASP site, complete replicate.
        /// </summary>
        public ActionResult EnrollmentCheckSettings(int course_id)
        {
            bool allowed = true;
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            int? student_id = AuthorizationHelper.CurrentStudentUser == null ? 0 : AuthorizationHelper.CurrentStudentUser.STUDENTID;
            int? enrollmentCheck = Settings.Instance.GetMasterInfo3().EnrollmentCheck;
            if (enrollmentCheck != null && enrollmentCheck > 0)
            {
                using (var db = new SchoolEntities())
                {
                    var course_data = (from c in db.Courses where c.COURSEID == course_id select c).SingleOrDefault();

                    string course_name = course_data.COURSENAME;
                    string course_number = course_data.COURSENUM;
                    bool DisableEnrollmentCheckingSpecific = (course_data.CourseSpecificEnrollmentCheck == 1 ? true : false);

                    foreach (var data in CourseShoppingCart.Instance.Items)
                    {
                        if (enrollmentCheck == 1)
                        { // FILTER BY COURSE NAME
                            if (course_name == data.Course.COURSENAME || course_name.Replace(" ", "") == data.Course.COURSENAME.Replace(" ", ""))
                                allowed = false;
                        }
                        else if (enrollmentCheck == 2)
                        { // FILTER BY COURSE NUMBER
                            if (course_number == data.Course.COURSENUM || course_number.Replace(" ", "") == data.Course.COURSENUM.Replace(" ", ""))
                                allowed = false;
                        }
                        else if (enrollmentCheck == 3)
                        { // FILTER BY COURSE NUMBER && COURSE NAME
                            if ((course_name == data.Course.COURSENAME || course_name.Replace(" ", "") == data.Course.COURSENAME.Replace(" ", "")) && (course_number == data.Course.COURSENUM || course_number.Replace(" ", "") == data.Course.COURSENUM.Replace(" ", "")))
                                allowed = false;
                        }
                    }
                    // IF LOGGED IN
                    if (student_id > 0)
                    {
                        // GET STUDENT ROSTER INFORMATION AND GET ALL COURSES
                        var course_roster_data = (from cr in db.Course_Rosters
                                                  join c in db.Courses on cr.COURSEID equals c.COURSEID
                                                  where cr.STUDENTID == student_id && cr.Cancel == 0
                                                  select c);
                        foreach (var data in course_roster_data)
                        {
                            if (enrollmentCheck == 1)
                            { // FILTER BY COURSE NAME
                                if (course_name == data.COURSENAME || course_name.Replace(" ", "") == (data.COURSENAME.Replace(" ", "")))
                                    allowed = false;
                            }
                            else if (enrollmentCheck == 2)
                            { // FILTER BY COURSE NUMBER
                                if (course_number == data.COURSENUM || course_number.Replace(" ", "") == (data.COURSENUM.Replace(" ", "")))
                                    allowed = false;
                            }
                            else if (enrollmentCheck == 3)
                            { // FILTER BY COURSE NUMBER && COURSE NAME
                                if ((course_name == data.COURSENAME || course_name.Replace(" ", "") == (data.COURSENAME.Replace(" ", ""))) && (course_number == data.COURSENUM || course_number.Replace(" ", "") == (data.COURSENUM.Replace(" ", ""))))
                                    allowed = false;
                            }
                        }
                    }

                    if (DisableEnrollmentCheckingSpecific) { allowed = true; }
                }
            }
            else
            {
                enrollmentCheck = 0;
            }

            result.Data = new
            {
                error = "",
                success = true,
                allowed = allowed,
                enrollmentCheckSetting = enrollmentCheck
            };
            return result;
        }

        /// <summary>
        /// CHECKER IF THE GOOGLE ANALYTICS IS USED OR GOOGLE STATUS.
        /// </summary>

        public ActionResult CheckGoogleAnalyticsUsed()
        {
            var result = new JsonResult();
            bool ga_global_used = false;
            bool ga_order_used = false;
            string ga_script = Settings.Instance.GetMasterInfo3().GoogleTracker_code;
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            ga_global_used = Settings.Instance.GetMasterInfo3().GoogleTracker_status == 1 ? true : false; // OVERRIDES the GA ORDER USED SETTING
            ga_order_used = Settings.Instance.GetMasterInfo3().GoogleTrackOrder_status == 1 ? true : false;

            if (ga_order_used == true)
            {
                if (string.IsNullOrEmpty(ga_script))
                {
                    ga_order_used = false;
                }
                else
                {
                    ga_order_used = true;
                }
            }
            result.Data = new
            {
                error = "",
                success = true,
                ga_used = ga_global_used == false ? false : ga_order_used
            };
            return result;
        }

        /// <summary>
        /// Removes a course from the cart.
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public ActionResult RemoveCourse(int courseId)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                CourseShoppingCart.Instance.RemoveCourse(courseId);
                result.Data = new
                {
                    status = CourseShoppingCart.Instance.Status,
                    success = true
                };
            }
            catch (Exception e)
            {
                result.Data = new
                {
                    error = e.Message,
                    success = false
                };
            }
            return result;
        }
        public ActionResult RemoveStudentinCheckoutMultiple(int courseid, int studentid)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                CourseShoppingCart.Instance.RemoveStudentinCheckoutMultiple(courseid, studentid);

                result.Data = new
                {
                    status = CourseShoppingCart.Instance.Status,
                    success = true,
                    courseenrolleecount = CourseShoppingCart.Instance.MultipleStudentCourses.Where(_c => _c.CourseId == courseid).Count(),
                };
            }
            catch (Exception e)
            {
                result.Data = new
                {
                    error = e.Message,
                    success = false
                };
            }
            return result;
        }
        /// <summary>
        /// Gets the alert text to display when removing a course from the shopping cart.
        /// Needs to be different is course is a single course vs. a parent bundled course vs. a child bundled course.
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        /*
        public ActionResult GetRemoveCourseAlertText(int courseId)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                string alertText = CourseShoppingCart.Instance.GetRemoveCourseAlertText(courseId);
                result.Data = new
                {
                    status = CourseShoppingCart.Instance.Status,
                    success = true,
                    alerttext = alertText
                };
            }
            catch (Exception e)
            {
                result.Data = new
                {
                    error = e.Message,
                    success = false
                };
            }
            return result;
        }
        */

        /// <summary>
        /// Check if PayNumber is Required on Given Payment Type
        /// </summary>
        /// <returns>string</returns>
        public string CheckRequiredPaynumber()
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            return EnrollmentFunction.CheckRequiredPaynumberData(Request["PaymentType"].ToString()).ToString();
        }

        /// <summary>
        /// Get the list of materials on specified course.
        /// </summary>
        /// <returns>list</returns>
        public IEnumerable<Material> GetListofMaterialsForIndividualCourse(int intCourseId)
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            //string strListOfMaterials = "~1015~, ~1016~, ~1040~, ~1018~, ~1038~";
            string strListOfMaterials = EnrollmentFunction.GetMaterialListFromCourse(intCourseId);
            try
            {
                if (strListOfMaterials.Equals(null) || strListOfMaterials.Equals(""))
                {
                    strListOfMaterials = "0";
                }
            }
            catch (Exception)
            {
                strListOfMaterials = "0";
            }
            return EnrollmentFunction.GetAllMaterialsById(strListOfMaterials);
        }
        /// <summary>
        /// Get the list of materials on specified course.
        /// </summary>
        /// <returns>List</returns>
        /// <summary>
        /// Get the list of materials on specified course.
        /// </summary>
        /// <returns>List</returns>
        /// 
        public Material GetMaterialInformation(int intMaterialId)
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            return EnrollmentFunction.GetMaterialDetails(intMaterialId);
        }

        /// <summary>
        /// Delete Materials from The Course
        /// </summary>
        /// <returns>List</returns>
        /// 
        public void DeleteMaterialsFromSpeciedCourse(int intCourseId, string strMaterialId)
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            string strListOfMaterials = EnrollmentFunction.GetMaterialListFromCourse(intCourseId);
            string strNewValue = "";
            foreach (string material in strListOfMaterials.Split(','))
            {
                if (material.Replace("~", "") != strMaterialId.Replace("~", ""))
                {
                    if (strNewValue == "")
                    {
                        strNewValue = strNewValue + material;
                    }
                    else
                    {
                        strNewValue = strNewValue + "," + material;
                    }
                }
            }
        }
        /// <summary>
        ///Add material to roster table.
        /// </summary>
        /// <param name="intRosterId"></param>
        /// <param name="strListOfProductId"></param>
        /// <returns></returns>
        /// 
        public void AddMaterialsToRoster(int intRosterId, string strListOfProductId)
        {
            strListOfProductId = strListOfProductId + ",";
            Material material = new Material();
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            foreach (string materialid in strListOfProductId.Split(','))
            {
                if (materialid.Trim() != "")
                {
                    material = EnrollmentFunction.GetMaterialDetails(int.Parse(materialid));
                    EnrollmentFunction.SaveRosterMaterial(intRosterId, material, 1);
                }
            }

        }
        /// <summary>
        ///Get the amount and shipping cost for the materials, can be use in updating te payment total.
        /// </summary>
        /// <returns>Dictionary</returns>
        /// 
        public Dictionary<string, string> GetCourseMaterialAmountAndShipping(int intProductId)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            Material Material = new Material();
            Material = EnrollmentFunction.GetMaterialDetails(intProductId);
            try
            {
                dictionary.Add("price", Material.price.ToString());
                dictionary.Add("shippingcost", Material.shipping_cost.ToString());
                dictionary.Add("istaxable", Material.taxable.ToString());
            }
            catch
            {
                dictionary.Add("price", "0");
                dictionary.Add("shippingcost", "0");
                dictionary.Add("istaxable", "0");
            }
            return dictionary;
        }
        /// <summary>
        ///Get the list of questions for checkout.
        /// </summary>
        /// <returns>List</returns>
        /// 
        public List<string> GetParseCheckOutQuestion()
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            List<string> listQuestions = new List<string>();
            string x = EnrollmentFunction.GetCheckOutQuestions();
            //listQuestions.Add("Tell us how you found out about this courses?");
            listQuestions.Add("");
            foreach (string questionitem in EnrollmentFunction.GetCheckOutQuestions().Replace("~~", "~").Split('~'))
            {
                string[] strArray = questionitem.Replace("%%", "%").Split('%');
                try
                {
                    if (strArray.Length > 0)
                    {
                        listQuestions.Add(strArray[1]);
                    }
                }
                catch (Exception)
                {
                }
            }

            return listQuestions;
        }

        /// <summary>
        ///Get the setting of CheckoutComment.
        /// </summary>
        /// <returns>Dictionary</returns>
        /// 
        public Dictionary<string, string> GetCheckoutCommentSettings()
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            return EnrollmentFunction.GetCheckoutCommentandQuestionSetUp();
        }

        /// <summary>
        ///Get the Prerequisite setting of the course.
        /// </summary>
        /// <param name="intCourseId"></param>
        /// <returns>Dictionary</returns>
        /// 
        public Dictionary<string, string> GetPreRequisitesSetting(int intCourseId)
        {

            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            CourseModel CourseModel = new CourseModel();
            CourseModel = EnrollmentFunction.GetPrerequisitesSetup(intCourseId);
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            try
            {
                dictionary.Add("showprerequisite", CourseModel.Course.ShowPrerequisite.ToString());
                dictionary.Add("prerequisiteInfo", CourseModel.Course.PrerequisiteInfo.ToString());
            }
            catch
            {
                dictionary.Add("showprerequisite", "0");
                dictionary.Add("prerequisiteInfo", "");
            }
            return dictionary;
        }
        /// <summary>
        ///Get the CheckOut Message setting.
        /// </summary>
        /// <param name="intCourseId"></param>
        /// <returns>Dictionary</returns>
        /// 
        public Dictionary<string, string> GetCheckOutMessageSetting()
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            return EnrollmentFunction.GetCheckOutMessageSetUp();
        }

        /// <summary>
        ///Partial view for material per course in cart.
        /// </summary>
        /// [ChildActionOnly]
        public ActionResult PartialViewMaterials(int intCourseId, List<Material> listMaterials, int intStudentId = 0)
        {
            List<string> materialids = new List<string>();
            List<int> qty = new List<int>();
            foreach (Material material in listMaterials)
            {
                materialids.Add(material.productID.ToString());
            }
            //set the materials list.
            ViewBag.materials = materialids;
            ViewBag.intCourseId = intCourseId.ToString();
            ViewBag.QuantitiesPerMaterial = Session["quantity_array"];
            int disabledstudentmaterials = 0;
            //For multiple Student, only principal student is editable.
            if (intStudentId != 0)
            {
                if (CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent != intStudentId)
                {
                    disabledstudentmaterials = 1;
                }
            }
            ViewBag.MaterialRequiredVal = CourseShoppingCart.Instance.GetCourseItem(intCourseId).Course.MaterialsRequired;
            ViewBag.sid = intStudentId;
            ViewBag.IsDisbledMaterials = disabledstudentmaterials;
            ViewBag.allowMaterialQuantity = Settings.Instance.GetMasterInfo4().allow_quantity_purchase;
            return PartialView(GetListofMaterialsForIndividualCourse(intCourseId));
        }


        public ActionResult PartialViewConfirmedMaterials(int intCourseId, string OrderNumber)
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            int intRosterId = EnrollmentFunction.GetRosterIdFromCourseIdandOrderNumber(intCourseId, OrderNumber);
            return PartialView(EnrollmentFunction.GetConfirmedMaterials(intRosterId));
        }

        /// <summary>
        ///use in script to check Authorization 
        /// </summary>
        public ActionResult CheckAuthorization()
        {
            return new JsonResult()
            {
                JsonRequestBehavior = System.Web.Mvc.JsonRequestBehavior.AllowGet,
                Data = new
                {
                    UserType = AuthorizationHelper.CurrentUser.LoggedInUserType.ToString(),
                    IsLoggedIn = AuthorizationHelper.CurrentUser.IsLoggedIn
                }
            };
        }

        public ActionResult PartialViewCourseTimes(int intCourseId = 0)
        {
            CourseModel course = null;
            ViewBag.CourseTimeList = Gsmu.Api.Data.School.Entities.Course.GetCourTimesList(intCourseId);
            ViewBag.DateDisplay = Gsmu.Api.Data.School.Entities.Course.GetCourseDetails(intCourseId).StartEndTimeDisplay;
            return PartialView(course);
        }

        public ActionResult updatePartialPayment(int intCourseId = 0, bool ppval = false)
        {
            CourseShoppingCart.Instance.updatePartialPayment(intCourseId, ppval);

            CheckoutInfo.Instance.HasPartialPayment = ppval;
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            result.Data = new
            {
                success = true,
                items = CourseShoppingCart.Instance.Items
            };
            return result;
        }

        public ActionResult updateCRExtraParticipant(int intCourseId = 0, string val = "")
        {
            CourseShoppingCart.Instance.UpdateCRExtraParticipant(intCourseId, val);

            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            result.Data = new
            {
                success = true,
                items = CourseShoppingCart.Instance.Items
            };
            return result;
        }

        public ActionResult GetCartList()
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            result.Data = new
            {
                success = true,
                items = CourseShoppingCart.Instance.Items,
                mutiplestudent = CourseShoppingCart.Instance.MultipleStudentCourses,
                taxpercent = CourseShoppingCart.Instance.SalesTaxPercentage,
            };

            return result;
        }

        public String GetCartCourseIDList()
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            return EnrollmentFunction.getCartCIDList();
        }

        public String GetCartCourseSubTotal()
        {
            return CourseShoppingCart.Instance.SubTotal.ToString();
        }

        public ActionResult ApplyCouponDiscount()
        {
            string courseidliststack = "0";
            string couponidliststack = "0";
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 1)
            {
                var autocoupons = EnrollmentFunction.GetAllAutomaticCouponDiscounts();
                foreach (var coupon in autocoupons)
                {
                    string additionalids = coupon.additionalcourseid;
                    foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
                    {
                        if ((coupon.CouponCourseId == cartitem.Course.COURSEID) || (additionalids.Contains(cartitem.Course.COURSEID.ToString())))
                        {
                            if (coupon.CouponOrderCourses <= CourseShoppingCart.Instance.Count)
                            {
                                if (coupon.CouponOneTimeUse != 1)
                                {
                                    courseidliststack = courseidliststack + "|" + cartitem.Course.COURSEID.ToString();
                                    couponidliststack = couponidliststack + "|" + coupon.CouponCode;
                                }
                                else
                                {
                                    if (!couponidliststack.Contains(coupon.CouponCode))
                                    {
                                        courseidliststack = courseidliststack + "|" + cartitem.Course.COURSEID.ToString();
                                        couponidliststack = couponidliststack + "|" + coupon.CouponCode;
                                    }
                                }
                            }

                        }
                    }

                }
                var JsonResult = new JsonResult();
                JsonResult.Data = new
                {
                    status = "Stack",
                    courseidlist = courseidliststack,
                    couponidlist = couponidliststack


                };
                return JsonResult;
            }
            else
            {
                string CouponCode = Request["couponcode"];
                string result = "approved";
                Coupon couponDetails = EnrollmentFunction.GetCouponDiscountDetails(CouponCode);
                var course_in_cart_not_free_count = CourseShoppingCart.Instance.Items.Where(i => i.LineTotal > 0).Count();
                if (couponDetails.CouponCourseId != 0 && couponDetails.CouponCourseId != null)
                {
                    couponDetails.additionalcourseid = couponDetails.additionalcourseid + "," + couponDetails.CouponCourseId.Value.ToString();
                }
                if (couponDetails.CouponCode == "notexist")
                {
                    result = "Coupon Not Found.";
                }

                else if (couponDetails.CouponExpireDate < DateTime.Now)
                {
                    result = "Coupon Expired.";
                }

                else if (couponDetails.CouponStartDate > DateTime.Now)
                {
                    result = "Coupon Expired.";
                }

                else if ((couponDetails.additionalcourseid != null) && (couponDetails.additionalcourseid != ""))
                {
                    string[] courseidlist = couponDetails.additionalcourseid.Split(',');
                    result = "";
                    string courseList = "";
                    foreach (string coursedid in courseidlist)
                    {
                        if (coursedid != "0" && coursedid != "")
                        {
                            try
                            {
                                CourseModel c = new CourseModel(int.Parse(coursedid));
                                courseList = courseList + c.Course.COURSENAME + ", ";
                            }
                            catch
                            {
                                courseList = courseList + coursedid + ", ";
                            }

                        }
                        foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
                        {
                            if (cartitem.Course.CustomCourseField5 == null)
                            {
                                cartitem.Course.CustomCourseField5 = "";
                            }
                            if (coursedid == cartitem.Course.COURSEID.ToString())
                            {
                                result = "approved";
                            }
                            else if (coursedid == cartitem.Course.CustomCourseField5.Replace(" ", ""))
                            {
                                if (coursedid != "")
                                {
                                    result = "approved";
                                }
                            }
                        }
                    }
                    if (result == "")
                    {
                        result = "This Coupon Code needs this course in your cart:" + courseList;
                    }
                }

                if (couponDetails.LimitCouponUsageinSystem == 1)
                {
                    List<string> rosterids = Gsmu.Api.Data.School.CourseRoster.Queries.GetRosterByCouponCode(CouponCode);
                    if (AuthorizationHelper.CurrentAdminUser == null)
                    {
                        if (rosterids.Count() >= couponDetails.NoOfCouponAvailable)
                        {

                            result = "Coupon Already Used.";
                        }
                    }

                }

                if (couponDetails.CouponOneTimeUse == 1)
                {
                    if (AuthorizationHelper.CurrentStudentUser != null)
                    {
                        if (AuthorizationHelper.CurrentAdminUser == null)
                        {
                            List<string> rosterids = Gsmu.Api.Data.School.CourseRoster.Queries.GetRosterByCouponCode(CouponCode, AuthorizationHelper.CurrentStudentUser.STUDENTID);
                            if (rosterids.Count() >= 1)
                            {
                                result = "Coupon Already Used.";
                            }
                        }
                    }
                    else
                    {
                        if (AuthorizationHelper.CurrentAdminUser == null)
                        {
                            foreach (var students in CourseShoppingCart.Instance.MultipleStudentCourses)
                            {
                                List<string> rosterids = Gsmu.Api.Data.School.CourseRoster.Queries.GetRosterByCouponCode(CouponCode, students.StudentId);
                                if (rosterids.Count() >= 1)
                                {
                                    result = "Coupon Already Used.";
                                }
                            }
                        }
                    }

                }
                if (couponDetails.CouponOrderCourses > CourseShoppingCart.Instance.Items.Count() || couponDetails.CouponOrderCourses > course_in_cart_not_free_count)
                {
                    result = "Need to have at least " + couponDetails.CouponOrderCourses + " items with more than zero price on your cart.";
                }

                if (couponDetails.CouponPerCourseAmount > 0)
                {
                    var chkout = CheckoutInfo.Instance;
                    if ((chkout.OrderTotal > decimal.Parse(couponDetails.CouponPerCourseAmount.ToString())))
                    {
                        result = "Order total must be less than $" + couponDetails.CouponPerCourseAmount.ToString() + ".";
                    }
                }

                var JsonResult = new JsonResult();
                if (result == "approved")
                {
                    var countUser = 1;
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        countUser = CourseShoppingCart.Instance.MultipleStudentCourses.Select(cr => cr.OrderNumber).Distinct().Count();
                    }
                    JsonResult.Data = new
                    {
                        status = "approved",
                        percentdiscount = couponDetails.CouponPercentAmount.ToString(),
                        //dollardiscount = (couponDetails.CouponDollarAmount * countUser).ToString(),
                        // dollor discount does not count number of user
                        dollardiscount = (couponDetails.CouponDollarAmount).ToString(),
                        materialdicounted = couponDetails.CouponMaterialsDiscounted.ToString()

                    };
                }
                else
                {
                    JsonResult.Data = new
                    {
                        status = result

                    };
                }
                return JsonResult;
            }

        }
        public void ApplyRoomMateRequest()
        {
            int CourseId = int.Parse(Request["cid"]);
            string roommatename = Request["roommatename"];
            string roommategender = Request["roommategender"];
            string roommatecommute = Request["roommatecommute"];
            foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
            {
                if (CourseId == cartitem.Course.COURSEID)
                {

                    cartitem.RoommateName = roommatename;
                    cartitem.RoommateGender = roommategender;
                    cartitem.RoommateQuestion = roommatecommute;
                }
            }
        }

        public decimal UpdateAdminPricing()
        {
            var studentid = int.Parse(Request["studentid"]); ;
            var courseid = int.Parse(Request["courseid"]);
            var newprice = decimal.Parse(Request["newprice"]); 
            if (AuthorizationHelper.CurrentAdminUser != null)
            {
                if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                {
                    foreach (var item in CourseShoppingCart.Instance.MultipleStudentCourses)
                    {
                        if (item.StudentId == studentid && item.CourseId == courseid)
                        {
                            item.CourseTotal = newprice;
                            item.useAdminPricing = 1;
                            break;
                        }
                    }
                }
                foreach (var crtitem in CourseShoppingCart.Instance.Items)
                {
                    if (crtitem.CourseId == courseid)
                    {
                        crtitem.AdminSetCourseTotal = newprice;
                        break;
                    }
                }
            }

            return newprice;

        }
        
        public ActionResult ApplyCouponDiscountPerCourse()
        {
            var studentid = 0;
            if (AuthorizationHelper.CurrentStudentUser != null)
            {
                studentid = AuthorizationHelper.CurrentStudentUser.STUDENTID;
            }
            else
            {
                try
                {
                    studentid = int.Parse(Request["studentid"]);

                }
                catch
                {
                    studentid = CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent;
                }
            }
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            string CouponCode = Request["couponcode"];
            int sid = 0;
            int CourseId = int.Parse(Request["courseid"]);
            string result = "approved";
            Coupon couponDetails = EnrollmentFunction.GetCouponDiscountDetails(CouponCode);
            if (couponDetails.CouponCourseId != 0 && couponDetails.CouponCourseId != null)
            {
                couponDetails.additionalcourseid = couponDetails.additionalcourseid + "," + couponDetails.CouponCourseId.Value.ToString();
            }
            if (couponDetails.CouponCode == "notexist")
            {
                result = "Coupon Not Found.";
                CouponCode = "";
            }
            else
            {
                if ((couponDetails.additionalcourseid != null) && (couponDetails.additionalcourseid != ""))
                {
                    string[] courseidlist = couponDetails.additionalcourseid.Split(',');
                    result = "";
                    foreach (string coursedid in courseidlist)
                    {

                        // foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
                        //{
                        if (coursedid == CourseId.ToString())
                        {
                            result = "approved";
                        }
                        // }
                        try
                        {
                            if (coursedid != "")
                            {
                                if (coursedid == CourseShoppingCart.Instance.Items.Where(course => course.Course.COURSEID == CourseId).FirstOrDefault().Course.CustomCourseField5.Replace(" ", ""))
                                {
                                    result = "approved";
                                }
                            }
                        }
                        catch { }
                    }
                    if (result == "")
                    {
                        result = "This Coupon Code is only  for Course(s):" + couponDetails.additionalcourseid;
                    }
                }
                if (couponDetails.CouponExpireDate < DateTime.Now)
                {
                    result = "Coupon Expired.";
                }

                if (couponDetails.CouponStartDate > DateTime.Now)
                {
                    result = "Coupon Expired.";
                }



                if (couponDetails.LimitCouponUsageinSystem == 1)
                {
                    List<string> rosterids = Gsmu.Api.Data.School.CourseRoster.Queries.GetRosterByCouponCode(CouponCode);
                    int CouponCountonCart = CourseShoppingCart.Instance.Items.Where(cart => cart.DiscountCouponPerCourse == CouponCode).ToList().Count();
                    if (rosterids.Count() + CouponCountonCart >= couponDetails.NoOfCouponAvailable)
                    {
                        result = "Coupon Already Used.";
                    }

                }

                if (couponDetails.CouponOneTimeUse == 1)
                {

                    List<string> rosterids = Gsmu.Api.Data.School.CourseRoster.Queries.GetRosterByCouponCode(CouponCode, studentid);
                    if (rosterids.Count() >= 1)
                    {
                        result = "Coupon Already Used.";
                    }
                    //checking the shopping cart if the coupone has been used.
                    if (AuthorizationHelper.CurrentSupervisorUser == null)
                    {
                        foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
                        {
                            if (cartitem.DiscountCouponPerCourse == CouponCode)
                            {
                                result = "Coupon Already Used.";
                            }
                        }
                    }

                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                        {
                            foreach (var item in CourseShoppingCart.Instance.MultipleStudentCourses)
                            {
                                if (item.StudentId == studentid && item.DiscountCouponPerCourse == CouponCode)
                                {
                                    result = "Coupon Already Used.";
                                }
                            }
                        }
                    }
                }

                if (couponDetails.CouponOrderCourses > CourseShoppingCart.Instance.Items.Count())
                {
                    result = "Need to have at least " + couponDetails.CouponOrderCourses + " items on your cart.";
                }
            }
            var JsonResult = new JsonResult();
            if ((result == "approved") || (CouponCode == ""))
            {
                foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
                {
                    if (CourseId == cartitem.Course.COURSEID)
                    {

                        cartitem.DiscountCouponPerCourse = CouponCode;
                    }
                }
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
                    {
                        foreach (var item in CourseShoppingCart.Instance.MultipleStudentCourses)
                        {
                            if (item.CourseId == CourseId && item.StudentId == studentid)
                            {
                                item.DiscountCouponPerCourse = CouponCode;
                            }
                        }
                    }
                }
                if (CouponCode != "")
                {
                    JsonResult.Data = new
                    {
                        status = "approved",
                        percentdiscount = couponDetails.CouponPercentAmount.ToString(),
                        dollardiscount = couponDetails.CouponDollarAmount.ToString(),
                        materialdicounted = couponDetails.CouponMaterialsDiscounted.ToString(),

                    };
                }
                else
                {
                    JsonResult.Data = new
                    {
                        status = result

                    };
                }
            }
            else
            {
                JsonResult.Data = new
                {
                    status = result

                };
            }
            return JsonResult;
        }
        public ActionResult SetDiscountAmountPercourse()
        {
            var JsonResult = new JsonResult();
            int CourseId = int.Parse(Request["courseid"]);
            var studentid = 0;
            if (AuthorizationHelper.CurrentStudentUser != null)
            {
                studentid = AuthorizationHelper.CurrentStudentUser.STUDENTID;
            }
            else
            {
                try
                {
                    studentid = int.Parse(Request["studentid"]);

                }
                catch
                {
                    studentid = CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent;
                }
            }
            string DiscountAmount = Request["discountamount"];
            string status = "";
            foreach (CourseShoppingCartItem cartitem in CourseShoppingCart.Instance.Items)
            {
                status = cartitem.StudentId.ToString();
                if ((CourseId == cartitem.Course.COURSEID) && CourseShoppingCart.Instance.MultipleStudentCourses.Where(stud => stud.StudentId == studentid && stud.CourseId == CourseId).Count() > 0)
                {
                    var studentinMultipleEnroll = CourseShoppingCart.Instance.MultipleStudentCourses.Where(stud => stud.StudentId == studentid && stud.CourseId == CourseId).FirstOrDefault();
                    if (studentinMultipleEnroll != null)
                    {
                        studentinMultipleEnroll.DiscountAmountPerCourse = DiscountAmount;
                    }
                    else
                    {
                       
                        cartitem.DiscountAmountPerCourse = DiscountAmount;
                    }
                    status = "approved";

                }
                else if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    if (CourseId == cartitem.Course.COURSEID)
                    {
                        cartitem.DiscountAmountPerCourse = DiscountAmount;
                        status = "approved";
                    }
                }
            }
            JsonResult.Data = new
            {
                status = status + studentid.ToString(),

            };
            return JsonResult;
        }
        public ActionResult ValidateMaxEnrollment()
        {
            var result = new JsonResult();
            int noofremovecourse = 0;
            int currentCourseinCart = CourseShoppingCart.Instance.Items.Count();
            if (AuthorizationHelper.CurrentSupervisorUser != null && currentCourseinCart == 0)
            {
                currentCourseinCart = currentCourseinCart + 1; //this is to cath a paylater function, which is cart has no course selected.
            }
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            try
            {
                foreach (var item in CourseShoppingCart.Instance.Items)
                {
                    if ((item.Course.MAXWAIT + item.Course.MAXENROLL) <= (CourseEnrollmentStatistics.GetTotalEnrolledinRoster(item.CourseId)))
                    {
                        RemoveCourse(item.CourseId);
                        noofremovecourse = noofremovecourse + 1;
                    }
                    else if (item.Course.MAXENROLL <= (CourseEnrollmentStatistics.GetTotalEnrolledinRosterNoWaiting(item.CourseId)))
                    {
                        if (!item.IsWaiting)
                        {
                            RemoveCourse(item.CourseId);
                        }
                    }
                }
            }
            catch { }

            result.Data = new
            {
                noofcourse = currentCourseinCart,
                success = true,
                noofremovecourse = noofremovecourse
            };
            return result;
        }
        public ActionResult ValidateAccessCode(int courseId, string accessCode = null)
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            try
            {
                var model = new CourseModel(courseId);
                bool resulttext = false;
                if (!model.Course.IsAccessCodeValid(accessCode))
                {
                    resulttext = false;
                }
                else
                {
                    resulttext = true;
                }
                result.Data = new
                {
                    resulttext = resulttext,
                    success = true
                };
            }
            catch (Exception e)
            {
                result.Data = new
                {
                    error = e.Message,
                    success = false
                };
            }
            return result;
        }


        public ActionResult ProcessCreditHoursPayment(string OrderNumber = "", string OrderAmount = "0", string PayType = "")
        {

            string strAmount = OrderAmount;
            string strDiscount = "0.00";
            string strOrderNo = OrderNumber.ToString();
            string strPaytype = PayType;
            string otherPaymentType = string.Empty;
            string ptype = "";
            int TranscriptID = 0;
            if (OrderNumber == "0" || OrderNumber == "")
            {
                strOrderNo = Request["OrderNo"];
                strAmount = Request["OrderTotal"];
                strDiscount = Request["totalDiscount"];
                strPaytype = Request["PaymentType"];
                otherPaymentType = Request["OtherPayment"];
                ptype = Request["Paynumber"];
                try
                {
                    TranscriptID = int.Parse(Request["TranscriptID"]);
                }
                catch
                {
                }
            }
            if (TranscriptID > 0)
            {
                otherPaymentType = Request["OtherPayment"];
                ptype = Request["Paynumber"];
            }
            string PaymentStatus = "";
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();


            if (double.Parse(strAmount) > 0)
            {
                CreditCardPaymentModel.PaymentType = Request["PaymentType"];
                CreditCardPaymentModel.PaymentNumber = Request["Paynumber"];
                CreditCardPaymentModel.OrderNumber = strOrderNo;

                if (strPaytype == "Credit Card" || strPaytype == "PayPal" || otherPaymentType == "PayPal")
                {
                    CreditCardPaymentModel.CardNumber = Request["CardNumber"];
                    CreditCardPaymentModel.CardType = Request["CardType"];
                    CreditCardPaymentModel.ExpiryMonth = Request["ExpiryMonth"];
                    CreditCardPaymentModel.ExpiryYear = Request["ExpiryYear"];
                    CreditCardPaymentModel.Address = Request["Address"];
                    CreditCardPaymentModel.State = Request["State"];
                    CreditCardPaymentModel.Zip = Request["Zip"];
                    CreditCardPaymentModel.City = Request["City"];
                    CreditCardPaymentModel.Country = Request["Country"];
                    CreditCardPaymentModel.FirstName = Request["FirstName"];
                    CreditCardPaymentModel.LastName = Request["LastName"];
                    CreditCardPaymentModel.CCV = Request["ccv"];

                    ViewBag.Amount = String.Format("{0:0.00}", double.Parse(strAmount));
                    PaymentsController PaymentsController = new Controllers.PaymentsController();
                    if (CreditCardPaymentHelper.UsePaygov)
                    {
                        PaymentStatus = PaymentsController.ProcessAndSelectPaymentMerchant(CreditCardPaymentModel, strAmount);
                    }
                    else if (CreditCardPaymentHelper.UsePayPal || strPaytype == "PayPal" || otherPaymentType == "PayPal")
                    {
                        CreditCardPaymentModel.Email = Request["Email"];
                        PaymentStatus = PaymentPaypal(CreditCardPaymentModel, strAmount);
                    }

                    else
                    {
                        CreditCardPaymentModel.Email = Request["Email"];
                        try
                        {
                            PaymentStatus = PaymentsController.ProcessAndSelectPaymentMerchant(CreditCardPaymentModel, strAmount);
                        }
                        catch (Exception e)
                        {
                            ObjectHelper.AddRequestMessage(this, string.Format(
                                        "Got error: {0}", e.Message
                                    ));
                        }
                    }


                }
                else
                {
                    PaymentStatus = "Approved";
                }


            }
            //PaymentStatus = "Approved";
            bool resultvalue = false;
            if (PaymentStatus.Contains("Approved"))
            {
                Transcripts trans = new Transcripts();
                trans.ApprovedCreditHoursPurchase(strOrderNo, TranscriptID, otherPaymentType, double.Parse(strAmount));
                resultvalue = true;
                try
                {
                    AuditTrail audit = new AuditTrail();
                    audit.RoutineName = "Credit Hours Purchase";
                    audit.ShortDescription = "Transcript ID: " + TranscriptID + "; Order Number:" + strOrderNo;
                    audit.DetailDescription = "Payment Type:" + strPaytype + " " + otherPaymentType + "Amount:" + strAmount;
                    CartAuditLogger(audit);
                }
                catch { }
            }
            var result = new JsonResult();
            result.Data = new
            {
                messages = PaymentStatus,
                success = resultvalue
            };
            return result;
        }

        public ActionResult ThankYou()
        {
            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            string query = "SELECT TOP 1 ThankYouMessage FROM MasterInfo2";
            string thankyou_message = string.Empty;
            using (var connection = Connections.GetSchoolConnection())
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.Connection = connection;
                cmd.CommandText = query;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var index = reader.GetOrdinal("ThankYouMessage");
                        thankyou_message = reader.GetValue(index).ToString();
                    }
                    reader.Close();
                }

                connection.Close();
            }
            ViewBag.ThankYouMessage = thankyou_message == "" || thankyou_message == null ? "Thank You!" : thankyou_message;
            int? GoogleTracker_status = Settings.Instance.GetMasterInfo3().GoogleTracker_status;

            if (GoogleTracker_status == 1)
                ViewBag.GoogleAnalyticsSetUpScript = Settings.Instance.GetMasterInfo3().GoogleTracker_code; // GA SCRIPT;
            else
                ViewBag.GoogleAnalyticsSetUpScript = string.Empty; // IF status is not 1 then dont load the GA
            return View();
        }


        public string GetAllMultipleStudentIdinCart(int cid)
        {
            string sids = "";
            if (CourseShoppingCart.Instance.MultipleStudentCourses != null)
            {
                if (CourseShoppingCart.Instance.MultipleStudentCourses.Count > 0)
                {
                    foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses)
                    {
                        if (st.CourseId == cid)
                        {
                            if (CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent != st.StudentId)
                            {
                                sids = sids + st.StudentId.ToString() + ",";
                            }
                        }
                    }
                }
            }
            sids = sids.TrimEnd(',');
            return sids;
        }
        public void SetReservationExpiredTime(string reservationexpiredtime)
        {
            CourseShoppingCart.Instance.ReservationExpired = reservationexpiredtime;
        }
        public string GetReservationExpiredTime()
        {
            if (CourseShoppingCart.Instance.Items.Count() > 0)
            {
                return CourseShoppingCart.Instance.ReservationExpired;
            }
            else
            {
                return "0";
            }
        }
        public string AddNewStudentForEnrollment(string FirstName, string LastName, string Email, int? cid = 0)
        {
            EnrollmentFunction EnrollmentFunction = new EnrollmentFunction();
            Student NewStudent = new Student();
            NewStudent.StudRegField1 = Request["dynamicfieldstudregfield1"];
            NewStudent.StudRegField2 = Request["dynamicfieldstudregfield2"];
            NewStudent.StudRegField3 = Request["dynamicfieldstudregfield3"];
            NewStudent.StudRegField4 = Request["dynamicfieldstudregfield4"];
            NewStudent.StudRegField5 = Request["dynamicfieldstudregfield5"];
            NewStudent.StudRegField6 = Request["dynamicfieldstudregfield6"];
            NewStudent.StudRegField7 = Request["dynamicfieldstudregfield7"];
            NewStudent.StudRegField8 = Request["dynamicfieldstudregfield8"];
            NewStudent.StudRegField9 = Request["dynamicfieldstudregfield9"];
            NewStudent.StudRegField10 = Request["dynamicfieldstudregfield10"];
            NewStudent.StudRegField11 = Request["dynamicfieldstudregfield11"];
            NewStudent.StudRegField12 = Request["dynamicfieldstudregfield12"];
            NewStudent.StudRegField13 = Request["dynamicfieldstudregfield13"];
            NewStudent.StudRegField14 = Request["dynamicfieldstudregfield14"];
            NewStudent.StudRegField15 = Request["dynamicfieldstudregfield15"];
            NewStudent.StudRegField16 = Request["dynamicfieldstudregfield16"];
            NewStudent.StudRegField17 = Request["dynamicfieldstudregfield17"];
            NewStudent.StudRegField18 = Request["dynamicfieldstudregfield18"];
            NewStudent.StudRegField19 = Request["dynamicfieldstudregfield19"];
            NewStudent.StudRegField20 = Request["dynamicfieldstudregfield20"];
            return EnrollmentFunction.AddNewStudentForEnrollment(FirstName, LastName, Email, NewStudent, cid);
        }

        #region PaygovTCS IMPLEMENTATION
        public String TestPaygov()
        {
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;

            //X509Store store = new X509Store(StoreLocation.CurrentUser);
            //store.Open(OpenFlags.ReadOnly);
            //X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindBySubjectName, "QADOEWAPATCERT0", true);
            string certName = "C:\\websites\\lang2\\Gsmu.Web\\Certificate\\paygov.pfx";
            string password = "masters3";


            //Making the Web Request
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(@"https://qa.tcs.pay.gov/services/TCSOnlineService/2.0/");
            //HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(@"http://www.dataaccess.com/webservicesserver/numberconversion.wso");


            //Content Type
            Req.ContentType = "text/xml;charset=utf-8";
            Req.Accept = "text/xml";

            //HTTP Method
            Req.Method = "POST";

            //SOAP Action
            // Req.Headers.Add("SOAPAction", "\"SOAPAction:urn#LstSbr\"");

            //NetworkCredential creds = new NetworkCredential("username", "pass");
            //Req.Credentials = creds;

            //Req.Headers.Add("MessageID", "1"); //Adding the MessageID in the header


            //Build the XML
            string xmlRequest = "";

            string sMsg = " ";
            sMsg += "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tcs=\"http://fms.treas.gov/services/tcsonline\">";
            sMsg += "<soapenv:Header/>";
            sMsg += "<soapenv:Body>";
            sMsg += "<tcs:startOnlineCollection>";
            sMsg += "<startOnlineCollectionRequest>";
            sMsg += "<tcs_app_id>TCSDOEWAPATC</tcs_app_id>";
            sMsg += "<agency_tracking_id>1501dfghj</agency_tracking_id>";
            sMsg += "<transaction_type>Sale</transaction_type>";
            sMsg += "<transaction_amount>90</transaction_amount>";
            sMsg += "<language>en</language>";
            sMsg += "<url_success>https://dev252.gosignmeup.com/</url_success>";
            sMsg += "<url_cancel>https://dev252.gosignmeup.com/</url_cancel>";
            sMsg += "</startOnlineCollectionRequest>";
            sMsg += "</tcs:startOnlineCollection>";
            sMsg += "</soapenv:Body>";
            sMsg += "</soapenv:Envelope>";
            xmlRequest = sMsg;


            //Pull Request into UTF-8 Byte Array
            byte[] reqBytes = new UTF8Encoding().GetBytes(xmlRequest);

            //Set the content length
            Req.ContentLength = reqBytes.Length;
            Req.ClientCertificates.Add(new X509Certificate2(certName, password, X509KeyStorageFlags.UserKeySet));
            //Req.ClientCertificates = collection;


            //Write the XML to Request Stream
            Stream sr = null;
            XmlTextReader xml;
            XmlDocument xmldoc;
            HttpWebResponse resp = null;

            try
            {
                using (Stream reqStream = Req.GetRequestStream())
                {
                    reqStream.Write(reqBytes, 0, reqBytes.Length);
                }
                resp = (HttpWebResponse)Req.GetResponse();
            }
            catch (WebException ex)
            {
                //WebResponse errResp = ex.Response;
                //using (Stream errsr = errResp.GetResponseStream())
                //{
                //    xml = new XmlTextReader(errsr);
                //    xmldoc = new XmlDocument();
                //    xmldoc.Load(xml);
                //    //StreamReader reader = new StreamReader(errsr);
                //    //string text = reader.ReadToEnd();
                //    var return_code = xmldoc.GetElementsByTagName("return_code")[0].InnerText;
                //    var return_detail = xmldoc.GetElementsByTagName("return_detail")[0].InnerText;
                //}
                //return "Exception of type " + ex.GetType().Name + " " + ex.Message;
                WebResponse errResp = ex.Response;
                using (Stream errsr = errResp.GetResponseStream())
                {
                    xml = new XmlTextReader(errsr);
                    xmldoc = new XmlDocument();
                    xmldoc.Load(xml);
                    var ErrCode = xmldoc.GetElementsByTagName("return_code")[0].InnerText;
                    var ErrDetail = xmldoc.GetElementsByTagName("return_detail")[0].InnerText;
                    return "Error " + ErrCode + " " + ErrDetail;
                }

            }


            string token = "";
            XmlNodeList listNodes;
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                sr = resp.GetResponseStream();
                xml = new XmlTextReader(sr);
                xmldoc = new XmlDocument();
                xmldoc.Load(xml);

                listNodes = xmldoc.GetElementsByTagName("token");
                foreach (XmlNode node in listNodes)
                {

                    token = node.InnerText;
                }

                xml.Close();
            }

            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
            string rhtml = "";
            rhtml += "<p>token: " + token + "</p>";
            var url = "https://qa.pay.gov/tcsonline/payment.do?token=" + token + "&tcsAppID=TCSDOEWAPATC";
            rhtml += "<a href=\"" + url + "\">Redirect: " + url + "</a>";
            rhtml += "<p>" + baseUrl;


            return rhtml;


        }
        public String TestPaygovX()
        {
            //string certName = @"C:\certs\paygov.pfx";

            //X509Certificate2 cert = new X509Certificate2(certName, "masters3", X509KeyStorageFlags.MachineKeySet);
            //X509Certificate2Collection certcoll = new X509Certificate2Collection();
            //certcoll.Add(cert);
            //X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
            //store.Open(OpenFlags.ReadWrite);
            ////store.Remove(cert);
            //store.Add(cert);



            Stream sr = null;
            XmlTextReader xml;
            XmlDocument xmldoc;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;


            //List<X509Certificate2> certificates = new List<X509Certificate2>();
            //X509Certificate2 certificate = new X509Certificate2();
            //X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
            //store.Open(OpenFlags.ReadOnly);
            //X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindBySubjectName, "QADOEWAPATCERT0", true);

            //foreach (X509Certificate2 cert in collection)
            //{
            //    certificates.Add(cert);
            //}
            //certificate = certificates[0];
            //string certName = @"~/Certificate/paygov.pfx";

            //string certName = @"E:\websites\eptc.gosignmeup.com\Certificate\paygov.pfx";
            //string certName = Server.MapPath("~") + "/Certificate/paygov.pfx";
            string certName = System.Configuration.ConfigurationManager.AppSettings["PaygovTCSCertLoc"];
            //string certName = "C:\\websites\\lang2\\Gsmu.Web\\Certificate\\paygov.pfx";

            //var crt = certificates;
            string password = "masters3";

            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(@"https://qa.tcs.pay.gov/services/TCSOnlineService/2.0/");
            Req.ContentType = "text/xml;charset=utf-8";
            Req.Method = "POST";

            string xmlReq = "";
            xmlReq += "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tcs=\"http://fms.treas.gov/services/tcsonline\">";
            xmlReq += "<soapenv:Header/>";
            xmlReq += "<soapenv:Body>";
            xmlReq += "<tcs:startOnlineCollection>";
            xmlReq += "<startOnlineCollectionRequest>";
            xmlReq += "<tcs_app_id>TCSDOEWAPATC</tcs_app_id>";
            xmlReq += "<agency_tracking_id>1501ASDhghjfgfgS</agency_tracking_id>";
            xmlReq += "<transaction_type>Sale</transaction_type>";
            xmlReq += "<transaction_amount>400</transaction_amount>";
            xmlReq += "<language>en</language>";
            xmlReq += "<url_success>https://dev252.gosignmeup.com/</url_success>";
            xmlReq += "<url_cancel>https://dev252.gosignmeup.com/</url_cancel>";
            xmlReq += "</startOnlineCollectionRequest>";
            xmlReq += "</tcs:startOnlineCollection>";
            xmlReq += "</soapenv:Body>";
            xmlReq += "</soapenv:Envelope>";


            //Pull Request into UTF-8 Byte Array
            byte[] reqBytes = new UTF8Encoding().GetBytes(xmlReq);

            Req.ContentLength = reqBytes.Length;
            //Req.ClientCertificates.Add(new X509Certificate2(certName, password, X509KeyStorageFlags.MachineKeySet));
            Req.ClientCertificates.Add(new X509Certificate2(certName, password));
            //Req.ClientCertificates.Add(certificate.GetRawCertData);
            HttpWebResponse resp = null;

            try
            {
                using (Stream reqStream = Req.GetRequestStream())
                {
                    reqStream.Write(reqBytes, 0, reqBytes.Length);
                }
                resp = (HttpWebResponse)Req.GetResponse();
            }
            catch (WebException ex)
            {
                //WebResponse errResp = ex.Response;
                //using (Stream errsr = errResp.GetResponseStream())
                //{
                //    xml = new XmlTextReader(errsr);
                //    xmldoc = new XmlDocument();
                //    xmldoc.Load(xml);
                //    //StreamReader reader = new StreamReader(errsr);
                //    //string text = reader.ReadToEnd();
                //    var return_code = xmldoc.GetElementsByTagName("return_code")[0].InnerText;
                //    var return_detail = xmldoc.GetElementsByTagName("return_detail")[0].InnerText;
                //}
                //return "Exception of type " + ex.GetType().Name + " " + ex.Message;
                WebResponse errResp = ex.Response;
                using (Stream errsr = errResp.GetResponseStream())
                {
                    xml = new XmlTextReader(errsr);
                    xmldoc = new XmlDocument();
                    xmldoc.Load(xml);
                    var ErrCode = xmldoc.GetElementsByTagName("return_code")[0].InnerText;
                    var ErrDetail = xmldoc.GetElementsByTagName("return_detail")[0].InnerText;
                    return "Error " + ErrCode + " " + ErrDetail;
                }

            }


            string token = "";
            XmlNodeList listNodes;
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                sr = resp.GetResponseStream();
                xml = new XmlTextReader(sr);
                xmldoc = new XmlDocument();
                xmldoc.Load(xml);

                listNodes = xmldoc.GetElementsByTagName("token");
                foreach (XmlNode node in listNodes)
                {

                    token = node.InnerText;
                }

                xml.Close();
            }

            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
            string rhtml = "";
            rhtml += "<p>token: " + token + "</p>";
            var url = "https://qa.pay.gov/tcsonline/payment.do?token=" + token + "&tcsAppID=TCSDOEWAPATC";
            rhtml += "<a href=\"" + url + "\">Redirect: " + url + "</a>";
            rhtml += "<p>" + baseUrl;


            return rhtml;
            //return xmlResponse;

        }


        public ActionResult PaygovTCSPayment()
        {
            var chkout = CheckoutInfo.Instance;
            string PaymentTotalStr = String.Format("{0:0.00}", chkout.PaymentTotal);
            string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
            //PaymentTotalStr = "400.00";

            Random r = new Random();
            string sequence = (r.Next(1000, 9999)).ToString();
            string PayGovTCSAgencyId = chkout.OrderNumber + "-" + sequence;

            string xmlReq = "";
            xmlReq += "<tcs:startOnlineCollection>";
            xmlReq += "<startOnlineCollectionRequest>";
            xmlReq += "<tcs_app_id>{PayGovTCSAppId}</tcs_app_id>";
            xmlReq += "<agency_tracking_id>" + PayGovTCSAgencyId + "</agency_tracking_id>";
            xmlReq += "<transaction_type>Sale</transaction_type>";
            xmlReq += "<transaction_amount>"+ PaymentTotalStr + "</transaction_amount>";
            xmlReq += "<language>en</language>";
            xmlReq += "<url_success>"+ baseUrl + "public/cart/PaygovTCSSuccess</url_success>";
            xmlReq += "<url_cancel>" + baseUrl + "</url_cancel>";
            xmlReq += "<custom_fields>";
            xmlReq += "<custom_field_2>" + chkout.GatherStudentNameInCart(chkout.OrderNumber) + "</custom_field_2>";
            xmlReq += "</custom_fields>";
            xmlReq += "</startOnlineCollectionRequest>";
            xmlReq += "</tcs:startOnlineCollection>";

            return PaygovTCS(xmlReq, "payment");

        }
        public ActionResult PaygovTCSSuccess()
        {
            string token = Request["token"];
            string xmlReq = "";
            xmlReq += "<tcs:completeOnlineCollectionWithDetails>";
            xmlReq += "<completeOnlineCollectionWithDetailsRequest>";
            xmlReq += "<tcs_app_id>{PayGovTCSAppId}</tcs_app_id>";
            xmlReq += "<token>"+ token +"</token>";
            xmlReq += "</completeOnlineCollectionWithDetailsRequest>";
            xmlReq += "</tcs:completeOnlineCollectionWithDetails>";

            return PaygovTCS(xmlReq, "success");

        }
        public ActionResult PaygovTCS(string SoapBody, string cmd)
        {
            Stream sr = null;
            XmlTextReader xml;
            XmlDocument xmldoc;
            string OrderNumber = "";
            string certPassword = "masters3";
            int usePayGovTCS = Convert.ToInt32(Settings.Instance.GetMasterInfo3().usePayGovTCS.ToString().ToLower());
            int PayGovTCSProd = Convert.ToInt32(Settings.Instance.GetMasterInfo3().PayGovTCSProd.ToString().ToLower());
            string PayGovTCSAppId = Settings.Instance.GetMasterInfo3().PayGovTCSAppId;
            string PayGovTCSAgencyId = Settings.Instance.GetMasterInfo3().PayGovTCSAgencyId;
            bool usePayGovTCSProd = false;
            if (PayGovTCSProd == 1) { usePayGovTCSProd = true; }

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;



            //string certName = Server.MapPath("~") + "/Certificate/paygov.pfx";
            string certName = System.Configuration.ConfigurationManager.AppSettings["PaygovTCSCertLoc"];
            if (System.IO.File.Exists(certName) == false) { throw new Exception("Cannot find paygov certificate: "+ certName); }

          


            string endpoint = @"https://qa.tcs.pay.gov/services/TCSOnlineService/2.0/";
            if (usePayGovTCSProd) { endpoint = @"https://tcs.pay.gov/services/TCSOnlineService/2.0/"; }

            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(endpoint);
            Req.ContentType = "text/xml;charset=utf-8";
            Req.Method = "POST";

            string xmlReq = "";
            xmlReq += "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tcs=\"http://fms.treas.gov/services/tcsonline\">";
            xmlReq += "<soapenv:Header/>";
            xmlReq += "<soapenv:Body>";
            xmlReq += SoapBody.Replace("{PayGovTCSAppId}", PayGovTCSAppId);
            xmlReq += "</soapenv:Body>";
            xmlReq += "</soapenv:Envelope>";

            //Pull Request into UTF-8 Byte Array
            byte[] reqBytes = new UTF8Encoding().GetBytes(xmlReq);

            Req.ContentLength = reqBytes.Length;
            //Req.ClientCertificates.Add(new X509Certificate2(certName, certPassword, X509KeyStorageFlags.MachineKeySet));
            Req.ClientCertificates.Add(new X509Certificate2(certName, certPassword, X509KeyStorageFlags.UserKeySet));
            HttpWebResponse resp = null;
         
            try
            {
                using (Stream reqStream = Req.GetRequestStream())
                {
                    reqStream.Write(reqBytes, 0, reqBytes.Length);
                }
                resp = (HttpWebResponse)Req.GetResponse();
            }
            catch (WebException ex)
            {
                try
                {
                    WebResponse errResp = ex.Response;
                    using (Stream errsr = errResp.GetResponseStream())
                    {
                        xml = new XmlTextReader(errsr);
                        xmldoc = new XmlDocument();
                        xmldoc.Load(xml);
                        ViewBag.ErrCode = xmldoc.GetElementsByTagName("return_code")[0].InnerText;
                        ViewBag.ErrDetail = xmldoc.GetElementsByTagName("return_detail")[0].InnerText;
                        ViewBag.Cmd = "error";
                        return View();
                    }
                }
                catch
                {
                    ViewBag.ErrCode = ex.GetType().Name;
                    ViewBag.ErrDetail = ex.Message;
                    ViewBag.Cmd = "error";
                    return View();

                }
                throw new Exception("Exception of type " + ex.GetType().Name + " " + ex.Message);
            }

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                sr = resp.GetResponseStream();
                xml = new XmlTextReader(sr);
                xmldoc = new XmlDocument();
                xmldoc.Load(xml);


                if(cmd == "payment")
                {

                    var token = xmldoc.GetElementsByTagName("token")[0].InnerText;
                  
                    var urlPayment = "https://qa.pay.gov/tcsonline/payment.do?token=" + token + "&tcsAppID=" + PayGovTCSAppId;
                    if (usePayGovTCSProd) { urlPayment = "https://www.pay.gov/tcsonline/payment.do?token=" + token + "&tcsAppID=" + PayGovTCSAppId; }
                    return Redirect(urlPayment);

                    //var rhtml = "<a href=\"" + urlPayment + "\">Redirect: " + urlPayment + "</a>" + Request.Url.Host;
                    //return rhtml;
                }

                if(cmd == "success")
                {
                    var chkout = CheckoutInfo.Instance;
                    var paygov_tracking_id = xmldoc.GetElementsByTagName("paygov_tracking_id")[0].InnerText;
                    var agency_tracking_id = xmldoc.GetElementsByTagName("agency_tracking_id")[0].InnerText;
                    var transaction_amount = xmldoc.GetElementsByTagName("transaction_amount")[0].InnerText;
                    var payment_type = xmldoc.GetElementsByTagName("payment_type")[0].InnerText;
                    OrderNumber = chkout.OrderNumber;

                    chkout.PaymentResult = "Approved";
                    CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                    CreditCardPaymentModel.PaymentType = "Credit Card";
                    CreditCardPaymentModel.PaymentNumber = paygov_tracking_id;
                    CreditCardPaymentModel.AuthNum = agency_tracking_id;
                    CreditCardPaymentModel.ActiveCCPayMethod = "PagovTCS";
                    CreditCardPaymentModel.RespMsg = "Approved";
                    CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                    CreditCardPaymentModel.TotalPaid = Convert.ToDouble(transaction_amount);
                    EnrollmentFunction enrollment = new EnrollmentFunction();
                    enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                    EmailFunction EmailFunction = new EmailFunction();
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                        {
                            EmailFunction.SendConfirmationEmail(transaction_amount, st, Request.Url.Host, "paygovtcsconfirmation");
                        }
                    }
                        else
                        {

                            EmailFunction.SendConfirmationEmail(transaction_amount, chkout.OrderNumber, Request.Url.Host, "paygovtcsconfirmation");
                        }
                    }


                    xml.Close();

                //return ShowConfirmationReceipt(OrderNumber, "");
                ViewBag.Cmd = "success";
                ViewBag.OrderNumber = OrderNumber;
                return View();
            }


            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = new
            {
                message = "Error command: "+ cmd,
                success = false
            };
            return result;
        }



        #endregion

        #region PAYPAL IMPLEMENTATION

        public string PaymentPaypal(CreditCardPaymentModel CreditCardPaymentModelValues, string Amount)
        {
            string output = "";
            string partner = Settings.Instance.GetMasterInfo().PFPartner.ToString();
            string vendor = Settings.Instance.GetMasterInfo().PFVendor.ToString();
            string login = Settings.Instance.GetMasterInfo().PFUser.ToString();
            string password = Settings.Instance.GetMasterInfo().PFPWD.ToString();
            int usepaypal_payflow = Convert.ToInt32(Settings.Instance.GetMasterInfo().UsePayflow.ToString().ToLower());
            int PayPalOperateMode = -1;
            string PaypalHostAddress = Settings.Instance.GetMasterInfo().PFHostAddress.ToString();

            var itemData = CourseShoppingCart.Instance.Items.ToList();
            string courseName = string.Empty;
            if (itemData != null && itemData.Count() > 0)
            {
                courseName = string.Join(",", Array.ConvertAll(itemData.ToArray(), l => l.Course.COURSENAME));
            }
            if (courseName.Count() > 50)
            {
                courseName = courseName.Substring(0, 50);
            }
            string dotNetUrl = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl;
            if (PaypalHostAddress.ToLower().IndexOf("pilot") == -1)
            {
                PayPalOperateMode = 1;
            }

            Gsmu.Api.Data.School.Student.EnrollmentFunction checkingoutLog = new Gsmu.Api.Data.School.Student.EnrollmentFunction();
            checkingoutLog.LogAuthorizeNetTransaction(CreditCardPaymentModelValues, CreditCardPaymentModelValues.TotalPaid.ToString(), "", "", "CreditCardPayment.cs-");

            if (httpRequestVariables()["RESULT"] != null)
            {
                System.Web.HttpContext.Current.Session["payflowresponse"] = httpRequestVariables();
            }

            var payflowresponse = System.Web.HttpContext.Current.Session["payflowresponse"] as NameValueCollection;
            if (payflowresponse != null)
            {
                System.Web.HttpContext.Current.Session["payflowresponse"] = null;

                bool success = payflowresponse["RESULT"] == "0";
                if (success)
                {
                    output = "Paypal Transaction Approved!";
                }
                else
                {
                    output = "Paypal Transaction Failed!";
                    try
                    {
                        checkingoutLog.LogAuthorizeNetTransaction(CreditCardPaymentModelValues, CreditCardPaymentModelValues.TotalPaid.ToString(), output + payflowresponse["RESULT"], "", "CreditCardPayment.cs-");
                    }
                    catch{ }

                }


            }
            else
            {

                HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
                var request = context.Request;
                var appUrl = HttpRuntime.AppDomainAppVirtualPath;
                string return_url = Request.Url.GetLeftPart(UriPartial.Authority) + "/Public/Cart/PaypalRedirectConfirmation";
                Random rand = new Random(DateTime.Now.Millisecond);
                string gen_token = "MySecTokenID-" + rand.Next() + "-" + CreditCardPaymentModelValues.OrderNumber; //genId();
                Session["token"] = gen_token;


                //if (usepaypal_payflow == 1) //LIVE
                //{
                //partner = Settings.Instance.GetMasterInfo().PFPartner.ToString();
                //vendor = Settings.Instance.GetMasterInfo().PFVendor.ToString();
                //login = Settings.Instance.GetMasterInfo().PFUser.ToString();
                //password = Settings.Instance.GetMasterInfo().PFPWD.ToString();
                //}
                //- A = Authorization
                //- B = Balance Inquiry
                //- C = Credit (Refund)
                //- D = Delayed Capture
                //- F = Voice Authorization
                //- I = Inquiry
                //- K = Rate Lookup
                //- L = Data Upload
                //- N = Duplicate Transaction
                string category = "";
                string coursenum = dotNetUrl;
                if (Settings.Instance.GetMasterInfo().PFIncludeCourseNumber == "~1~" || Settings.Instance.GetMasterInfo().PFFieldsToInclude == "~1~")
                {
                    coursenum = "";
                    try
                    {
                        using (var db = new SchoolEntities())
                        {
                            foreach (CourseShoppingCartItem item in CourseShoppingCart.Instance.Items)
                            {
                                coursenum = coursenum + "|" + item.Course.COURSENUM;
                                category = category + "|" + (from cr in db.MainCategories
                                                             where cr.CourseID == item.CourseId
                                                             select cr).FirstOrDefault().MainCategory1;
                            }

                        }
                    }
                    catch
                    {
                    }
                }
                string suffix = gen_token.Split('-')[1].ToString();
                NameValueCollection requestArray = new NameValueCollection()
                {
                    {"PARTNER", partner },
                    {"VENDOR", vendor },
                    {"USER", login },
                    {"PWD", password },
                    {"TRXTYPE", "S"},
                    //{"TRXTYPE", "C"},
                    {"BUTTONSOURCE","GoSignMeUp_SP"},
                    {"AMT", decimal.Parse(Amount).ToString ("0.##")},
                    {"CURRENCY", "USD"},
                    {"CREATESECURETOKEN", "Y"},
                    {"SECURETOKENID", gen_token},
                    //{"INVNUM", CreditCardPaymentModelValues.OrderNumber + suffix}, // no longer use md5 string as token. ordernumber is good enough.
                    {"INVNUM", CreditCardPaymentModelValues.OrderNumber +"-"+ rand.Next() },
                    {"VERBOSITY", "HIGH"},
                    //This should be generated and unique, never used before
                    {"RETURNURL", return_url},  //Note how this simple exa mple merely returns back to itself, rather than having a seperate Return.aspx
                    {"CANCELURL", return_url},
                    {"ERRORURL", return_url},
                    //COMMENT PART WHERE ADDITIONAL INFO IS ADDED
                    {"COMMENT1", category},
                    {"COMMENT2", coursenum},
                    // In practice you'd collect billing and shipping information with your own form,
                    // then afterwards be doing this request for a secure token to display the payment iframe.
                    // (For visuals, see page 7 of https://cms.paypal.com/cms_content/US/en_US/files/developer/Embedded_Checkout_Design_Guide.pdf )
                    // This example uses hardcoded address values for simplicity.
                    {"BILLTOEMAIL", CreditCardPaymentModelValues.Email},
                    {"BILLTOFIRSTNAME", CreditCardPaymentModelValues.FirstName},
                    {"BILLTOLASTNAME", CreditCardPaymentModelValues.LastName},
                    {"BILLTOSTREET", CreditCardPaymentModelValues.Address},
                    {"BILLTOCITY", CreditCardPaymentModelValues.City},
                    {"BILLTOSTATE", CreditCardPaymentModelValues.State},
                    {"BILLTOZIP", CreditCardPaymentModelValues.Zip},
                    {"BILLTOCOUNTRY", CreditCardPaymentModelValues.Country}
                };


                NameValueCollection resp = run_payflow_call(requestArray, PayPalOperateMode);
                if (resp["RESULT"] != "0")
                {
                    output = "Payflow call failed";
                }
                else
                {
                    string mode = string.Empty;
                    //if (environment == "pilot" || environment == "test" || environment == "sandbox") mode = "TEST"; else mode = "LIVE";
                    if (PayPalOperateMode == -1) //test
                    {
                        mode = "TEST";
                    }
                    else if (PayPalOperateMode == 1) //live
                    {
                        mode = "LIVE";
                    }
                    else if (PayPalOperateMode == 0) //depends on the current environment or host //if staging, live or local
                    {
                        string host_environment = System.Web.HttpContext.Current.Request.Url.Host;
                        if (host_environment.ToLower() == "localhost" || host_environment.ToLower() == "server")
                        {
                            mode = "TEST";
                        }
                    }
                    // can put in style: overflow:scroll; if need scroll
                    output = "<iframe id='paypal_iframe_source' src='https://payflowlink.paypal.com?SECURETOKEN=" + resp["SECURETOKEN"] + "&SECURETOKENID=" + resp["SECURETOKENID"] + "&MODE=" + mode + "' height='500' border='0' frameborder='0' scrolling='no' allowtransparency='true' style=\" width:100%; text-align:center; display:block;\">\n</iframe>";
                }
            }



            return output;
        }

        protected NameValueCollection httpRequestVariables()
        {
            var post = Request.Form;       // $_POST
            var get = Request.QueryString; // $_GET
            return Merge(post, get);
        }

        protected string genId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 16)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());
            return "MySecTokenID-" + result; //add a prefix to avoid confusion with the "SECURETOKEN"
        }

        protected NameValueCollection run_payflow_call(NameValueCollection requestArray, int mode)
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
            String nvpstring = "";
            foreach (string key in requestArray)
            {
                //format:  "PARAMETERNAME[lengthofvalue]=VALUE&".  Never URL encode.
                var val = requestArray[key];
                if (val == null)
                {
                    val = string.Empty;
                }
                nvpstring += key + "[" + val.Length + "]=" + val + "&";
            }
            //nvpstring = nvpstring.TrimEnd('&');

            string urlEndpoint = string.Empty;
            //if (environment == "pilot" || environment == "test" || environment == "sandbox")
            if (mode == -1)
            {
                urlEndpoint = "https://pilot-payflowpro.paypal.com/";
            }
            else if (mode == 1)
            {
                urlEndpoint = "https://payflowpro.paypal.com";
            }
            else if (mode == 0) //depends on the current environment or host //if staging, live or local
            {
                string host_environment = System.Web.HttpContext.Current.Request.Url.Host;
                if (host_environment.ToLower() == "localhost" || host_environment.ToLower() == "server")
                {
                    urlEndpoint = "https://pilot-payflowpro.paypal.com/";
                }
            }

            //send request to Payflow
            HttpWebRequest payReq = (HttpWebRequest)WebRequest.Create(urlEndpoint);
            payReq.Method = "POST";
            payReq.ContentLength = nvpstring.Length;
            payReq.ContentType = "application/x-www-form-urlencoded";

            StreamWriter sw = new StreamWriter(payReq.GetRequestStream());
            sw.Write(nvpstring);
            sw.Close();

            //get Payflow response
            HttpWebResponse payResp = (HttpWebResponse)payReq.GetResponse();
            StreamReader sr = new StreamReader(payResp.GetResponseStream());
            string response = sr.ReadToEnd();
            sr.Close();

            //parse string into array and return
            NameValueCollection dict = new NameValueCollection();
            foreach (string nvp in response.Split('&'))
            {
                string[] keys = nvp.Split('=');
                dict.Add(keys[0], keys[1]);
            }
            return dict;
        }

        // merges two NVCs
        public static NameValueCollection Merge(NameValueCollection first, NameValueCollection second)
        {
            if (first == null && second == null)
                return null;
            else if (first != null && second == null)
                return new NameValueCollection(first);
            else if (first == null && second != null)
                return new NameValueCollection(second);

            NameValueCollection result = new NameValueCollection(first);
            for (int i = 0; i < second.Count; i++)
                result.Set(second.GetKey(i), second.Get(i));
            return result;
        }

        #endregion
        #region NELNET IMPLEMENTATION
        protected string run_nelnet_calls(NameValueCollection requestArray)
        {
            string output = "";
            string nvpstring = "";
            foreach (string key in requestArray)
            {
                //format:  "PARAMETERNAME[lengthofvalue]=VALUE&".  Never URL encode.
                var val = requestArray[key];
                if (val == null)
                {
                    val = string.Empty;
                }
                nvpstring += key + "[" + val.Length + "]=" + val + "&";
            }

            string baseUrl = "https://quikpayasp.com/cwru2/commerce_manager/payer.do";
            //string urlEndpoint = CreditCardPaymentHelper.CashNetServer;
            //HttpWebRequest payReq = (HttpWebRequest)WebRequest.Create(baseUrl);
            //payReq.Method = "POST";
            //payReq.ContentLength = nvpstring.Length;
            //payReq.ContentType = "application/x-www-form-urlencoded";

            //StreamWriter sw = new StreamWriter(payReq.GetRequestStream());
            //sw.Write(nvpstring);
            //sw.Close();


            //HttpWebResponse payResp = (HttpWebResponse)payReq.GetResponse();
            //StreamReader sr = new StreamReader(payResp.GetResponseStream());
            //string response = sr.ReadToEnd();
            //sr.Close();

            ////parse string into array and return
            //NameValueCollection dict = new NameValueCollection();
            //foreach (string nvp in response.Split('&'))
            //{
            //    string[] keys = nvp.Split('=');
            //    dict.Add(keys[0], keys[1]);
            //}
            //return dict;
            return "";
        }
        #endregion
        #region CASHNET IMPLEMENTATION
        //public string PaymentCashNet(CreditCardPaymentModel CreditCardPaymentModelValues, string Amount)
        //{
        //    string output = "";
        //    int count = 1;
        //    NameValueCollection requestArray = new NameValueCollection();
        //    requestArray.Add("itemcode1", CreditCardPaymentModelValues.OrderNumber);
        //    requestArray.Add("amount1", Amount);
        //    requestArray.Add("amount1", Amount);
        //    output = run_cashnet_call(requestArray);
        //    return output;
        //}
        //protected string run_cashnet_call(NameValueCollection requestArray)
        //{
        //    string nvpstring = "";
        //    foreach (string key in requestArray)
        //    {
        //        //format:  "PARAMETERNAME[lengthofvalue]=VALUE&".  Never URL encode.
        //        var val = requestArray[key];
        //        if (val == null)
        //        {
        //            val = string.Empty;
        //        }
        //        nvpstring += key + "[" + val.Length + "]=" + val + "&";
        //    }

        //    //string urlEndpoint = "http://commerce.cashnet.com/ualrpay";
        //    string urlEndpoint = CreditCardPaymentHelper.CashNetServer;
        //    ControllerContext.HttpContext.Response.Redirect(urlEndpoint);
        //    //send request to Payflow
        //    //HttpWebRequest payReq = (HttpWebRequest)WebRequest.Create(urlEndpoint);
        //    //payReq.Method = "POST";
        //    //payReq.ContentLength = nvpstring.Length;
        //    //payReq.ContentType = "application/x-www-form-urlencoded";

        //    //StreamWriter sw = new StreamWriter(payReq.GetRequestStream());
        //    //sw.Write(nvpstring);
        //    //sw.Close();

        //    ////get Payflow response
        //    //HttpWebResponse payResp = (HttpWebResponse)payReq.GetResponse();
        //    //StreamReader sr = new StreamReader(payResp.GetResponseStream());
        //    //string response = sr.ReadToEnd();
        //    //sr.Close();
        //    return "";
        //}
        #endregion

        #region IPAY IMPLEMENTATION

        public ActionResult iPayConfirmation()
        {
            string token = Request["token"];

            string ipayconfig = Settings.Instance.GetMasterInfo4().iPayGatewayConfiguration;
            if ((ipayconfig != "") && (ipayconfig != null))
            {
                CreditCardPayments payment = new CreditCardPayments();
                string timestamp_ipay = DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss");
                ViewBag.timestamp = timestamp_ipay;


                JavaScriptSerializer j = new JavaScriptSerializer();
                dynamic settingsconfig = j.Deserialize(ipayconfig, typeof(object));
                string serverurl = settingsconfig["serverurl"];
                string siteid = settingsconfig["siteid"];
                string accounts = settingsconfig["accounts"];
                string cfoapal = settingsconfig["cfoapal"];
                string sendkeyascii = settingsconfig["sendkeyascii"];
                ViewBag.IpayServerUrl = serverurl;
                string iPayAccounts = accounts;
                string[] CFOAPALs = (cfoapal).Split('|');
                string chart1 = "";
                string fund1 = "";
                string org1 = "";
                string account1 = "";
                string program1 = "";
                try
                {
                    if (CFOAPALs.Length > 0)
                    {
                        chart1 = CFOAPALs[0];
                        fund1 = CFOAPALs[1];
                        org1 = CFOAPALs[2];
                        account1 = CFOAPALs[3];
                        program1 = CFOAPALs[4];

                    }
                }
                catch { }
                var chkout = CheckoutInfo.Instance;
                string total = String.Format("{0:0.00}", chkout.OrderTotal);
                byte[] encodedcertificate = new ASCIIEncoding().GetBytes(token + "|" + total + "|" + timestamp_ipay + "|" + iPayAccounts + "|" + chart1 + "|" + fund1 + "|" + org1 + "|" + account1 + "|" + program1 + "|" + total);
                HMACSHA1 hmacsha1 = new HMACSHA1(new ASCIIEncoding().GetBytes(sendkeyascii));
                string certificatevalue = BitConverter.ToString(hmacsha1.ComputeHash(encodedcertificate)).Replace("-", "").ToLower();
                ViewBag.CertificateValue = certificatevalue;
                string postString = string.Format("action=captureccpayment&token=" + token + "&amount=" + total + "&numaccounts={0}&chart1={1}&fund1={2}&org1={3}&account1={4}&program1={5}&timestamp={6}&certification={7}&amount1=" + total, iPayAccounts, chart1, fund1, org1, account1, program1, timestamp_ipay, certificatevalue);


                string Result = WebRequestForIpay(timestamp_ipay, certificatevalue, postString, serverurl);
                string[] stringSeparators = new string[] { "\n" };
                string[] response_lines = Result.Split(stringSeparators, StringSplitOptions.None);
                string responsecode = "1";
                string responsemessage = "";
                string transactionid = "";
                foreach (var values in response_lines)
                {
                    if (values.ToLower().Contains("responsecode"))
                    {
                        string[] Pair_parts = values.Split('=');
                        responsecode = Pair_parts[1];
                    }

                    if (values.ToLower().Contains("responsemessage"))
                    {
                        string[] Pair_parts = values.Split('=');
                        responsemessage = Pair_parts[1];
                    }


                }

                ViewBag.OrderNo = chkout.OrderNumber;
                ViewBag.transactionid = chkout.transactionid;
                ViewBag.OrderTotal = chkout.OrderTotal;
                ViewBag.Result = responsecode;
                ViewBag.All = Result;
                ViewBag.responsemessage = responsemessage;
                chkout.responsecode = responsecode;
                chkout.responsemessage = responsemessage;

            }
            return View();





        }
        public ActionResult SquarePayment()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            string useNewEncryptionProtocol = System.Configuration.ConfigurationManager.AppSettings["UseNewEncryptionProtocol"];
            if (!string.IsNullOrEmpty(useNewEncryptionProtocol) && bool.Parse(useNewEncryptionProtocol) == true)
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }
            //TransactionApi _transactionApi = new TransactionApi();
            var chkout = CheckoutInfo.Instance;
            ViewBag.OrderNo = chkout.OrderNumber;
            ViewBag.SquareAppID = Settings.Instance.GetMasterInfo().SquareAppID.Trim();
            string PaymentTotal = chkout.PaymentTotal.ToString();
            ViewBag.PaymentTotalLbl = String.Format("$ {0:0.00}", PaymentTotal);


            string _appID = Gsmu.Api.Data.Settings.Instance.GetMasterInfo().SquareAppID.Trim();
            string _accessToken = Gsmu.Api.Data.Settings.Instance.GetMasterInfo().SquareAccessToken.Trim();
            var envi = Square.Environment.Production;
            if (_appID.Contains("sandbox")) { envi = Square.Environment.Sandbox; }

            SquareClient client = new SquareClient.Builder()
                .Environment(envi)
                .AccessToken(_accessToken)
                .Build();

            var rspnse = client.LocationsApi.ListLocationsAsync();

            if (rspnse.Result == null)
            {
                throw new Exception("Error: "+ rspnse.Status.ToString());
            }
            var _locationId = rspnse.Result.Locations[0].Id.ToString();
            ViewBag.LocationId = _locationId;

            return View();

        }



        public async System.Threading.Tasks.Task<string> SquarePaymentChargeAsync(string nonce, string firstname, string lastname, string sourceId )
        {
            string useNewEncryptionProtocol = System.Configuration.ConfigurationManager.AppSettings["UseNewEncryptionProtocol"];
            if (!string.IsNullOrEmpty(useNewEncryptionProtocol) && bool.Parse(useNewEncryptionProtocol) == true)
            {
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            else
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }

            string _accessToken = Gsmu.Api.Data.Settings.Instance.GetMasterInfo().SquareAccessToken.Trim();
            string _appID = Gsmu.Api.Data.Settings.Instance.GetMasterInfo().SquareAppID.Trim();
            //string _accessToken = "EAAAEJVyeQDncA5YqwJamJjE82W1sZ74rptw8Znm4r_Ji_o_cmyuRy6HJ7NKVKi_";
            //string _appID = "sandbox-sq0idb-aMkIejKc8RnlvWaeuR51EQ";
            string uuid = SquarePaymentNewIdempotencyKey();
            var chkout = CheckoutInfo.Instance;
            //test
            //_accessToken = "EAAAEOAA_if82m-T6QabpHPRciv09JV5fAYBUwhRUXKZyUL3zl5SDk3xVz4bkQTU";
            //chkout.PaymentTotal = 123;
            //chkout.OrderNumber = "TESTORDERID123";

            var PaymentTotal = Convert.ToDecimal(chkout.PaymentTotal.ToString());
            //FROM API v2 document :
            //The amount field is always in the smallest denomination of the currency indicated by currency_code. When currency_code is USD (US dollars), amount is in cents. 
            long SqrPaymentTotal = Convert.ToInt64(PaymentTotal * 100);

            Money amount = new Money.Builder()
                .Amount(SqrPaymentTotal)
                .Currency("USD")
                .Build();

            var billingAddress = new Address.Builder()
              .FirstName(firstname)
              .LastName(lastname)
              .Build();

            var envi = Square.Environment.Production;
            if (_appID.Contains("sandbox")) { envi = Square.Environment.Sandbox; }

            SquareClient client = new SquareClient.Builder()
                .Environment(envi)
                .AccessToken(_accessToken)
                .Build();

            var rspnse = client.LocationsApi.ListLocationsAsync();
           
            if (rspnse.Result == null)
            {
                return rspnse.ToString();
                //return rspnse.Status.ToString();
            }
            var _locationId = rspnse.Result.Locations[0].Id.ToString();
            IPaymentsApi paymentsApi = client.PaymentsApi;

            int crscnt = 0;
            string crses = "";
            string fld3 = "";
            string cma = "";
            string dsh = " - ";
            foreach (var item in CourseShoppingCart.Instance.Items)
            {
                crscnt += 1;
                crses += cma + item.Course.COURSENAME;
                if (crscnt <= 3 && !string.IsNullOrEmpty(item.Course.CustomCourseField3))
                {
                    fld3 += dsh + item.Course.CustomCourseField3;
                }
                cma = ",";
            }

            var clientdata = new CreateCustomerRequest.Builder()
                .GivenName(lastname)
                .FamilyName(firstname)
                .ReferenceId(chkout.OrderNumber)
                .Build();


            try
            {

                //var clientresp = await client.CustomersApi.CreateCustomerAsync(body: clientdata);
                var clientresp = await client.CustomersApi.CreateCustomerAsync(body: clientdata);

                var body = new CreatePaymentRequest.Builder(nonce, uuid, amount)
                   .LocationId(_locationId)
                   .ReferenceId(chkout.OrderNumber + fld3)
                   .BillingAddress(billingAddress)
                   .CustomerId(clientresp.Customer.Id)
                   .Note(crses)
                   .SourceId(sourceId)
                   .Build();

            CreatePaymentResponse response = new CreatePaymentResponse();
            response = await paymentsApi.CreatePaymentAsync(body);

            if (response.Errors == null)
            {

                // dynamic rspns = response.ToJson();
                chkout.PaymentResult = "Approved";
                CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                CreditCardPaymentModel.PaymentType = "Credit Card";
                CreditCardPaymentModel.PaymentNumber = response.Payment.Id;
                CreditCardPaymentModel.AuthNum = response.Payment.OrderId;
                CreditCardPaymentModel.ActiveCCPayMethod = response.Payment.CardDetails.Card.CardBrand.ToString();
                CreditCardPaymentModel.RespMsg = "Approved";
                CreditCardPaymentModel.ReceiptUrl = response.Payment.ReceiptUrl;
                CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                CreditCardPaymentModel.TotalPaid = Convert.ToDouble(PaymentTotal);
                EnrollmentFunction enrollment = new EnrollmentFunction();
                enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                EmailFunction EmailFunction = new EmailFunction();
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                    {

                        EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), st, Request.Url.Host, "squaresupconfirmation");
                    }
                }
                else
                {

                    EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), chkout.OrderNumber, Request.Url.Host, "squareregconfirmation");
                }

                return "";
            }
            else
            {
                string allerrs = string.Join(",", response.Errors);
                return allerrs;
            }
            }
            catch (ApiException e)
            {
                return e.Message.ToString();
            }
        }


        public static string SquarePaymentNewIdempotencyKey()
        {
            return Guid.NewGuid().ToString();
        }




        public ActionResult CyberSourcePayment()
        {
            var chkout = CheckoutInfo.Instance;
            string reason_code = Request["reason_code"];
            long PaymentTotal = Convert.ToInt64(Convert.ToDecimal(chkout.PaymentTotal.ToString()));

            IDictionary<string, string> parameters = new Dictionary<string, string>();
            string transaction_uuid = System.Guid.NewGuid().ToString();
            DateTime time = DateTime.Now.ToUniversalTime();
            string signed_date_time = time.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            string reference_number = chkout.OrderNumber;

            string profile_id = Settings.Instance.GetMasterInfo().CybersourceMerchantID;
            string access_key = Settings.Instance.GetMasterInfo().CybersourceAccessKey;
            string secret_key = Settings.Instance.GetMasterInfo().CybersourceSecretKey;

            string amount = PaymentTotal.ToString();
            string signature = "";
            string message = "";
            string signdflds = "";
            bool UseCyberSourceItemized = WebConfiguration.UseCyberSourceItemized;

            if (String.IsNullOrWhiteSpace(reason_code))
            {
                var cnti = 0;
                string flds = "";
                string courselst = "";
                string comma = "";
                decimal multipleTotal = 0;
                foreach (var item in CourseShoppingCart.Instance.Items)
                {
                   
                    if (UseCyberSourceItemized)
                    {
                        flds += ",item_" + cnti + "_code," + "item_" + cnti + "_name," + "item_" + cnti + "_sku," + "item_" + cnti + "_quantity," + "item_" + cnti + "_unit_price";
                        parameters.Add("item_" + cnti + "_code", item.Course.COURSENUM);
                        parameters.Add("item_" + cnti + "_name", item.Course.COURSENAME);
                        parameters.Add("item_" + cnti + "_sku", item.Course.COURSENUM);
                        parameters.Add("item_" + cnti + "_quantity", "1");

                        if (CourseShoppingCart.Instance.MultipleStudentCourses != null && CourseShoppingCart.Instance.MultipleStudentCourses.Count() > 1)
                        {
                            foreach(var multiitem in CourseShoppingCart.Instance.MultipleStudentCourses ){
                                multipleTotal = multipleTotal + multiitem.CourseTotal;
                            }
                            parameters.Add("item_" + cnti + "_unit_price", multipleTotal.ToString("0.##"));
                            multipleTotal = 0;
                        }
                        else
                        {
                            parameters.Add("item_" + cnti + "_unit_price", item.LineTotal.ToString("0.##"));
                        }
                        cnti += 1;
                    }
                    else
                    {
                        courselst += comma + item.Course.COURSENUM + " - " + item.Course.COURSENAME;
                        comma = "; ";
                    }
                }

                parameters.Add("access_key", access_key);
                parameters.Add("profile_id", profile_id);
                parameters.Add("transaction_uuid", transaction_uuid);
                if (UseCyberSourceItemized == false)
                {
                    parameters.Add("merchant_secure_data4", courselst);
                    parameters.Add("amount", amount);
                    flds += ",amount,merchant_secure_data4";
                }
                else if (chkout.PaymentCaller == "paynowuserdash")
                {
                    parameters.Add("merchant_secure_data4", "Gosignmeup - Pay Remainder Balance");
                    parameters.Add("amount", amount);
                    flds += ",amount,merchant_secure_data4";
                }
                else
                {
                    parameters.Add("line_item_count", cnti.ToString());
                    flds += ",line_item_count";
                }
                signdflds = "access_key,profile_id,transaction_uuid,signed_field_names,unsigned_field_names,signed_date_time,locale,transaction_type,reference_number,currency" + flds;
                parameters.Add("signed_field_names", signdflds);
                parameters.Add("unsigned_field_names", "");

                parameters.Add("signed_date_time", signed_date_time);
                parameters.Add("transaction_type", "sale");
                parameters.Add("reference_number", reference_number);
                parameters.Add("currency", "USD");
                parameters.Add("locale", "en");

                signature = signCyberSource(buildDataToSignCyberSource(parameters), secret_key);
            }
            else
            {
                message = Request["message"];
                if (reason_code == "100")
                {


                    chkout.PaymentResult = "Approved";
                    CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                    CreditCardPaymentModel.PaymentType = "Credit Card";
                    CreditCardPaymentModel.PaymentNumber = Request["transaction_id"];
                    CreditCardPaymentModel.AuthNum = Request["auth_trans_ref_no"];
                    CreditCardPaymentModel.ActiveCCPayMethod = "Cybersource";
                    CreditCardPaymentModel.RespMsg = "Approved";
                    CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                    CreditCardPaymentModel.TotalPaid = PaymentTotal;
                    EnrollmentFunction enrollment = new EnrollmentFunction();
                    enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                    EmailFunction EmailFunction = new EmailFunction();
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                        {

                            EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), st, Request.Url.Host, "squaresupconfirmation");
                        }
                    }
                    else
                    {

                        EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), chkout.OrderNumber, Request.Url.Host, "squareregconfirmation");
                    }





                }
            }

            ViewBag.signdflds = signdflds;
            ViewBag.signature = signature;
            ViewBag.signed_date_time = signed_date_time;
            ViewBag.transaction_uuid = transaction_uuid;
            ViewBag.reference_number = reference_number;
            ViewBag.access_key = access_key;
            ViewBag.profile_id = profile_id;
            ViewBag.amount = amount;
            ViewBag.message = StripHtmlTags.Strip(message);
            ViewBag.reason_code = reason_code;
            ViewBag.PaymentCaller = chkout.PaymentCaller;
            ViewBag.OrderNo = chkout.OrderNumber;
            ViewBag.SquareAppID = Settings.Instance.GetMasterInfo().SquareAppID.Trim();
            ViewBag.PaymentTotalLbl = String.Format("$ {0:0.00}", chkout.PaymentTotal.ToString());
            return View();
        }


        private static String signCyberSource(String data, String secretKey)
        {
            UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secretKey);

            HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);
            byte[] messageBytes = encoding.GetBytes(data);
            return Convert.ToBase64String(hmacsha256.ComputeHash(messageBytes));
        }

        private static String buildDataToSignCyberSource(IDictionary<string, string> paramsArray)
        {
            String[] signedFieldNames = paramsArray["signed_field_names"].Split(',');
            IList<string> dataToSign = new List<string>();

            foreach (String signedFieldName in signedFieldNames)
            {
                dataToSign.Add(signedFieldName + "=" + paramsArray[signedFieldName]);
            }

            return commaSeparateCyberSource(dataToSign);
        }

        private static String commaSeparateCyberSource(IList<string> dataToSign)
        {
            return String.Join(",", dataToSign);
        }

        public ActionResult ChaseHPPPayment()
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
            var chkout = CheckoutInfo.Instance;
            long PaymentTotal = Convert.ToInt64(Convert.ToDecimal(chkout.PaymentTotal.ToString()));


            string PaymentTotalStr = String.Format("{0:0.00}", chkout.PaymentTotal);
            decimal PaymentTotalDec = Convert.ToDecimal(PaymentTotalStr);
            try
            {
                HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
                var url = CreditCardPaymentHelper.ChasePaymentAbstractionAddress;
                var request = context.Request;
                var appUrl = HttpRuntime.AppDomainAppVirtualPath;
                var ChaseTemplateUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "ChasePayment_Template.aspx";
                var ChaseReturnUrl = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ChaseHPPConfirmation";
                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["hostedSecureID"] = CreditCardPaymentHelper.ChasePaymentMerchantId;
                    data["hostedSecureAPIToken"] = CreditCardPaymentHelper.ChasePaymentPassword;
                    data["content_template_url"] = ChaseTemplateUrl;
                    data["return_url"] = ChaseReturnUrl;
                    data["trans_type"] = "auth_capture";
                    data["total_amt"] = PaymentTotalDec.ToString();
                    data["formtype"] = "1";
                    data["order_id"] = chkout.OrderNumber;
                    data["allowed_types"] = "MasterCard|Visa|American Express|Discover";
                    data["required"] = "all";
                    data["order_desc"] = "Gosignmeup Registration";
                    data["collectAddress"] = "2";

                    var response = wb.UploadValues(url, "POST", data);
                    string responseInString = Encoding.UTF8.GetString(response);

                    ViewBag.OrderNo = chkout.OrderNumber;
                    ViewBag.PaymentTotalLbl = PaymentTotalStr;
                    ViewBag.UrlSubmit = CreditCardPaymentHelper.ChasePaymentServer + "?" + responseInString;
                    try
                    {
                        if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                        {
                            Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                            Audittrail.TableName = Request.UserHostName;
                            Audittrail.AuditDate = DateTime.Now;
                            Audittrail.RoutineName = "HPP Payment";
                            Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                            try
                            {
                                Audittrail.AuditAction = ViewBag.UrlSubmit;

                            }
                            catch { }
                            Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                            LogManager.LogSiteActivity(Audittrail);
                        }
                    }
                    catch
                    {
                    }
                    return View();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public ActionResult ChaseHPPConfirmation()
        {
            var chkout = CheckoutInfo.Instance;
            long PaymentTotal = Convert.ToInt64(Convert.ToDecimal(chkout.PaymentTotal.ToString()));
            string code = Request["code"];
            string message = Request["message"];
            string uID = Request["ProcTxnID"] + "*" + Request["transId"] + "*" + Request["uID"];

            if (code == "000" && message == "Success")
            {

                chkout.PaymentResult = "Approved";
                CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                CreditCardPaymentModel.PaymentType = "Credit Card";
                CreditCardPaymentModel.PaymentNumber = uID;
                CreditCardPaymentModel.AuthNum = uID;
                CreditCardPaymentModel.ActiveCCPayMethod = "ChaseHPP";
                CreditCardPaymentModel.RespMsg = "Approved";
                CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                CreditCardPaymentModel.TotalPaid = PaymentTotal;
                EnrollmentFunction enrollment = new EnrollmentFunction();
                enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                EmailFunction EmailFunction = new EmailFunction();
                if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                    {

                        EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), st, Request.Url.Host, "ChaseHPPConfirmation");
                    }
                }
                else
                {

                    EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), chkout.OrderNumber, Request.Url.Host, "ChaseHPPConfirmation");
                }

                return RedirectToAction("ShowConfirmationReceipt", new { OrderNumber = chkout.OrderNumber });

            }
            else
            {
                return Content(code + ":" + message);
            }

        }

        public ActionResult FirstDataPayment()
        {
            var chkout = CheckoutInfo.Instance;
            string PaymentTotalStr = String.Format("{0:0.00}", chkout.PaymentTotal);
            ViewBag.OrderNo = chkout.OrderNumber;
            ViewBag.PaymentTotalLbl = PaymentTotalStr;


            return View();
        }
        public String FirstDataPaymentProcess(String CardNumber, String ExpDateMM, String ExpDateYY, String CCV, String Name, String Address1, String City, String State, String Zip, String Country)
        {

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

            var chkout = CheckoutInfo.Instance;
            string PaymentTotalStr = String.Format("{0:0.00}", chkout.PaymentTotal);
            decimal PaymentTotalDec = Convert.ToDecimal(PaymentTotalStr);
            double PaymentTotal = Convert.ToDouble(PaymentTotalStr);

            // Initialize Service Object 
            FDGGWSApiOrderService oFDGGWSApiOrderService = new FDGGWSApiOrderService();
            //oFDGGWSApiOrderService.Url = @"https://ws.merchanttest.firstdataglobalgateway.com/fdggwsapi/services/order.wsdl";
            //oFDGGWSApiOrderService.ClientCertificates.Add(X509Certificate.CreateFromCertFile("C:/FDGGWSClient/WS1909526596._.1.pem"));
            string WebServiceAPI = Settings.Instance.GetMasterInfo3().FirstDataGatewayAPIHostName;
            string CertLoc = Settings.Instance.GetMasterInfo3().FirstDataGatewayURL;
            oFDGGWSApiOrderService.Url = @WebServiceAPI + "/order.wsdl";
            oFDGGWSApiOrderService.ClientCertificates.Add(X509Certificate.CreateFromCertFile(CertLoc));

            // Set the Authentication Credentials 
            //NetworkCredential nc = new NetworkCredential("WS1909526596._.1", "mgYjnkxj");
            string WSAuthUser = Settings.Instance.GetMasterInfo3().FirstDataGatewayUserID;
            string WSAuthPass = Settings.Instance.GetMasterInfo3().FirstDataGatewayStoreNumber;
            NetworkCredential nc = new NetworkCredential(WSAuthUser, WSAuthPass);
            oFDGGWSApiOrderService.Credentials = nc;

            // Create Sale Transaction 
            FDGGWSApiOrderRequest oOrderRequest = new FDGGWSApiOrderRequest();
            Gsmu.Web.WebReference.Transaction oTransaction = new Gsmu.Web.WebReference.Transaction();
            CreditCardTxType oCreditCardTxType = new CreditCardTxType();
            oCreditCardTxType.Type = CreditCardTxTypeType.sale;
            TransactionDetails oTransactionDetails = new TransactionDetails();
            CreditCardData oCreditCardData = new CreditCardData();

            oCreditCardData.ItemsElementName = new ItemsChoiceType[] { 
            //    ItemsChoiceType.CardCodeValue,
            //    ItemsChoiceType.CardCodeIndicator,
                ItemsChoiceType.CardNumber,
                ItemsChoiceType.ExpMonth,
                ItemsChoiceType.ExpYear

            };
            ////oCreditCardData.Items = new string[] { 
            ////    "4111111111111111", 
            ////    "12", 
            ////    "18",
            ////};

            oCreditCardData.Items = new object[] { 
            //    "5984",
            //    CreditCardDataCardCodeIndicator.PROVIDED,
                CardNumber,
                ExpDateMM,
                ExpDateYY
            };


            Billing BillAddr = new Billing();
            BillAddr.Name = Name;
            BillAddr.Address1 = Address1;
            BillAddr.City = City;
            BillAddr.State = State;
            BillAddr.Zip = Zip;
            BillAddr.Country = Country;


            oTransactionDetails.InvoiceNumber = chkout.OrderNumber;

            oTransaction.Items = new object[] {
                oCreditCardTxType,
                oCreditCardData
            };
            WebReference.Payment oPayment = new WebReference.Payment();
            oPayment.ChargeTotal = PaymentTotalDec;
            oTransaction.Payment = oPayment;
            oTransaction.Billing = BillAddr;
            oTransaction.TransactionDetails = oTransactionDetails;
            oOrderRequest.Item = oTransaction;

            // Get the Response 
            FDGGWSApiOrderResponse oReponse = null;
            try
            {
                oReponse = oFDGGWSApiOrderService.FDGGWSApiOrder(oOrderRequest);
                string sApprovalCode = oReponse.TransactionResult;

                if (sApprovalCode == "APPROVED")
                {

                    chkout.PaymentResult = "Approved";
                    CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                    CreditCardPaymentModel.PaymentType = "Credit Card";
                    CreditCardPaymentModel.PaymentNumber = oReponse.OrderId;
                    CreditCardPaymentModel.AuthNum = oReponse.ApprovalCode;
                    CreditCardPaymentModel.ActiveCCPayMethod = "FirstData";
                    CreditCardPaymentModel.RespMsg = "Approved";
                    CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                    CreditCardPaymentModel.TotalPaid = PaymentTotal;
                    EnrollmentFunction enrollment = new EnrollmentFunction();
                    enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                    EmailFunction EmailFunction = new EmailFunction();
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                        {

                            EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), st, Request.Url.Host, "FirstDataConfirmation");
                        }
                    }
                    else
                    {

                        EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), chkout.OrderNumber, Request.Url.Host, "FirstDataConfirmation");
                    }


                }
                return sApprovalCode + ":" + oReponse.ErrorMessage;


            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                // Catch the Exception 
                return "error";

            }


        }


        public ActionResult AdyenPayment()
        {

            var chkout = CheckoutInfo.Instance;
            string reason_code = Request["authResult"];
            string PaymentTotalStr = String.Format("{0:0.00}", chkout.PaymentTotal);
            double PaymentTotal = Convert.ToDouble(PaymentTotalStr);
            PaymentTotal = PaymentTotal * 100;

            //string hppUrl = "https://test.adyen.com/hpp/pay.shtml";
            //string hppUrl = "https://ca-test.adyen.com/ca/ca/skin/checkhmac.shtml";
            //string hmacKey = "9607CE2DAA3751B2836168C7E926C37762CA86087790957BE71C184EC9265337";
            //chkout.PaymentTotal = 123 * 100;
            //PaymentTotal = 123 * 100;
            //chkout.OrderNumber = "TestOrderNumber";
            string hmacKey = string.Empty;
            string hppUrl = string.Empty;
            string skinCode = string.Empty;
            string merchantAccount = string.Empty;

            string AdyenPaymentGateway = Settings.Instance.GetMasterInfo().AdyenPaymentGateway;
            if ((AdyenPaymentGateway != "") && (AdyenPaymentGateway != null))
            {
                if (AdyenPaymentGateway.Length > 10)
                {
                    JavaScriptSerializer j = new JavaScriptSerializer();
                    dynamic settingsconfig = j.Deserialize(AdyenPaymentGateway, typeof(object));
                    hmacKey = settingsconfig["AdyenHmacKey"];
                    hppUrl = settingsconfig["AdyenHppUrl"];
                    skinCode = settingsconfig["AdyenSkinCode"];
                    merchantAccount = settingsconfig["AdyenMerchantAccount"];
                }
            }


            string merchantReference = chkout.OrderNumber + "-" + DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            string paymentAmount = PaymentTotal.ToString();
            string currencyCode = "USD";
            string shipBeforeDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
            string sessionValidity = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssK");
            string shopperLocale = "en_US";
            string orderData = AdyenCompressString(chkout.OrderNumber);
            string countryCode = "US";
            string shopperEmail = "";
            string shopperReference = "";
            string allowedMethods = "";
            string blockedMethods = "";
            string offset = "";

            Dictionary<string, string> paramlist = new Dictionary<string, string>();

            paramlist.Add("currencyCode", currencyCode);
            paramlist.Add("merchantAccount", merchantAccount);
            paramlist.Add("merchantReference", merchantReference);
            paramlist.Add("paymentAmount", paymentAmount);
            paramlist.Add("sessionValidity", sessionValidity);
            paramlist.Add("shipBeforeDate", shipBeforeDate);
            paramlist.Add("shopperLocale", shopperLocale);
            paramlist.Add("orderData", orderData);
            paramlist.Add("skinCode", skinCode);
            paramlist.Add("countryCode", countryCode);
            paramlist.Add("shopperEmail", shopperEmail);
            paramlist.Add("shopperReference", shopperReference);
            paramlist.Add("allowedMethods", allowedMethods);
            paramlist.Add("blockedMethods", blockedMethods);
            paramlist.Add("offset", offset);

            string signingString = AdyenBuildSigningString(paramlist);
            string merchantSig = AdyenCalculateHMAC(hmacKey, signingString);


            if (String.IsNullOrWhiteSpace(reason_code))
            {


            }
            else
            {
                if (reason_code == "AUTHORISED")
                {

                    chkout.PaymentResult = "Approved";
                    CreditCardPaymentModel CreditCardPaymentModel = new CreditCardPaymentModel();
                    CreditCardPaymentModel.PaymentType = "Credit Card";
                    CreditCardPaymentModel.PaymentNumber = Request["merchantReference"];
                    CreditCardPaymentModel.AuthNum = Request["pspReference"];
                    CreditCardPaymentModel.ActiveCCPayMethod = "Adyen";
                    CreditCardPaymentModel.RespMsg = "Approved";
                    CreditCardPaymentModel.LongOrderId = chkout.OrderNumber;

                    CreditCardPaymentModel.TotalPaid = PaymentTotal;
                    EnrollmentFunction enrollment = new EnrollmentFunction();
                    enrollment.ApproveEnrollment(CreditCardPaymentModel, chkout.OrderNumber);

                    EmailFunction EmailFunction = new EmailFunction();
                    if (AuthorizationHelper.CurrentSupervisorUser != null)
                    {
                        foreach (var st in CourseShoppingCart.Instance.MultipleStudentCourses.Select(a => a.OrderNumber).Distinct().ToList())
                        {

                            EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), st, Request.Url.Host, "adyenconfirmation");
                        }
                    }
                    else
                    {

                        EmailFunction.SendConfirmationEmail(PaymentTotal.ToString(), chkout.OrderNumber, Request.Url.Host, "adyenconfirmation");
                    }


                }
                else
                {


                }


            }

            ViewBag.hppUrl = hppUrl;
            ViewBag.merchantReference = merchantReference;
            ViewBag.paymentAmount = paymentAmount;
            ViewBag.currencyCode = currencyCode;
            ViewBag.shipBeforeDate = shipBeforeDate;
            ViewBag.skinCode = skinCode;
            ViewBag.merchantAccount = merchantAccount;
            ViewBag.sessionValidity = sessionValidity;
            ViewBag.shopperLocale = shopperLocale;
            ViewBag.orderData = orderData;
            ViewBag.countryCode = countryCode;
            ViewBag.shopperEmail = shopperEmail;
            ViewBag.shopperReference = shopperReference;
            ViewBag.allowedMethods = allowedMethods;
            ViewBag.blockedMethods = blockedMethods;
            ViewBag.offset = offset;
            ViewBag.merchantSig = merchantSig;

            ViewBag.reason_code = reason_code;

            ViewBag.OrderNo = chkout.OrderNumber;
            ViewBag.PaymentTotalLbl = PaymentTotalStr;
            return View();
        }

        string AdyenCompressString(string input)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(input);

            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                    gzip.Close();
                    return Convert.ToBase64String(output.ToArray());
                }
            }
        }

        static string AdyenEscapeVal(string val)
        {
            if (val == null)
            {
                return string.Empty;
            }

            val = val.Replace(@"\", @"\\");
            val = val.Replace(":", @"\:");
            return val;
        }

        static string AdyenBuildSigningString(IDictionary<string, string> dict)
        {
            //Dictionary<string, string> signDict = dict.Where(x => x.Value != "").OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            Dictionary<string, string> signDict = dict.OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            string keystring = string.Join(":", signDict.Keys);
            string valuestring = string.Join(":", signDict.Values.Select(AdyenEscapeVal));


            return string.Format("{0}:{1}", keystring, valuestring);
        }

        // Computes the Base64 encoded signature using the HMAC algorithm with the HMACSHA256 hashing function.
        string AdyenCalculateHMAC(string hmacKey, string signingstring)
        {
            byte[] key = AdyenPackH(hmacKey);
            byte[] data = Encoding.UTF8.GetBytes(signingstring);

            try
            {
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    // Compute the hmac on input data bytes
                    byte[] rawHmac = hmac.ComputeHash(data);

                    // Base64-encode the hmac
                    return Convert.ToBase64String(rawHmac);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to generate HMAC : " + e.Message);
            }
        }

        byte[] AdyenPackH(string hex)
        {
            if ((hex.Length % 2) == 1)
            {
                hex += '0';
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }



        public ActionResult IPayGeneratePaymentLink()
        {
            string ipayconfig = Settings.Instance.GetMasterInfo4().iPayGatewayConfiguration;
            if ((ipayconfig != "") && (ipayconfig != null))
            {
                JavaScriptSerializer j = new JavaScriptSerializer();
                dynamic settingsconfig = j.Deserialize(ipayconfig, typeof(object));
                string serverurl = settingsconfig["serverurl"];
                string siteid = settingsconfig["siteid"];
                string accounts = settingsconfig["accounts"];
                string cfoapal = settingsconfig["cfoapal"];
                string sendkeyascii = settingsconfig["sendkeyascii"];
                CreditCardPayments payment = new CreditCardPayments();
                string timestamp_ipay = DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss");
                ViewBag.timestamp = timestamp_ipay;
                ViewBag.IpayServerUrl = serverurl;
                var chkout = CheckoutInfo.Instance;
                string total = String.Format("{0:0.00}", chkout.OrderTotal);
                byte[] encodedcertificate = new ASCIIEncoding().GetBytes(total + "|" + siteid + "|" + timestamp_ipay);
                HMACSHA1 hmacsha1 = new HMACSHA1(new ASCIIEncoding().GetBytes(sendkeyascii));
                string certificatevalue = BitConverter.ToString(hmacsha1.ComputeHash(encodedcertificate)).Replace("-", "").ToLower();
                ViewBag.CertificateValue = certificatevalue;
                string postString = string.Format("action=registerccpayment&siteid={0}&amount=" + total + "&market=retail&referenceid1={1}&timestamp={2}&certification={3}", siteid, chkout.OrderNumber, timestamp_ipay, certificatevalue);
                string Result = WebRequestForIpay(timestamp_ipay, certificatevalue, postString, serverurl);
                string[] stringSeparators = new string[] { "\n" };
                string[] response_lines = Result.Split(stringSeparators, StringSplitOptions.None);
                string token = "";
                string url_redirect = "";
                string transactionid = "";
                foreach (var values in response_lines)
                {
                    if (values.ToLower().Contains("token"))
                    {
                        string[] Pair_parts = values.Split('=');
                        token = Pair_parts[1];
                    }
                    if (values.ToLower().Contains("redirect"))
                    {
                        string[] Pair_parts = values.Split('=');
                        url_redirect = Pair_parts[1];
                    }
                    if (values.ToLower().Contains("transactionid"))
                    {
                        string[] Pair_parts = values.Split('=');
                        transactionid = Pair_parts[1];
                    }

                }
                chkout.transactionid = transactionid;
                ViewBag.Token = token;
                ViewBag.redirect = url_redirect;
                ViewBag.urlToken = url_redirect + "?token=" + token;
                ViewBag.All = Result;
            }
            return View();
        }

        public string WebRequestForIpay(string timestamp, string certification, string postString, string serverurl)
        {
            var chkout = CheckoutInfo.Instance;
            string total = String.Format("{0:0.00}", chkout.OrderTotal);
            string URLAuth = serverurl;

            const string contentType = "application/x-www-form-urlencoded";
            System.Net.ServicePointManager.Expect100Continue = false;

            CookieContainer cookies = new CookieContainer();
            HttpWebRequest webRequest = WebRequest.Create(URLAuth) as HttpWebRequest;
            webRequest.Method = "POST";
            webRequest.ContentType = contentType;
            webRequest.CookieContainer = cookies;
            webRequest.ContentLength = postString.Length;
            webRequest.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.9.0.1) Gecko/2008070208 Firefox/3.0.1";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            webRequest.Referer = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl; //  "https://dev250.gosignmeup.com"

            StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
            requestWriter.Write(postString);
            requestWriter.Close();

            StreamReader responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
            string responseData = responseReader.ReadToEnd();

            responseReader.Close();
            webRequest.GetResponse().Close();
            return responseData;
        }

        public ActionResult DisplayCartError(string message)
        {
            ViewBag.Message = message;
            return View();
        }

        public bool GetPreRequisiteConfig()
        {
            return WebConfiguration.IsAdvance == 0;
        }
        #endregion

        #region CONVERGE IMPLEMENTATION
        public string PaymentConverge(CreditCardPaymentModel CreditCardPaymentModelValues, string Amount)
        {
            HttpContextWrapper context = new HttpContextWrapper(System.Web.HttpContext.Current);
            var request = context.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;
            //string convergePaymentServer = "https://demo.myvirtualmerchant.com/VirtualMerchantDemo/process.do"; //CreditCardPaymentHelper.ConvergeServer; //"https://api.demo.convergepay.com/VirtualMerchantDemo/process.do"; 
            string convergePaymentServer = CreditCardPaymentHelper.ElavonServer.ToString();
            string amount = String.Format("{0:0.00}", double.Parse(Amount));
            string redirectLink = string.Format("{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl) + "Public/Cart/ConvergeRedirect"; ;
            string convergeMerchantID = CreditCardPaymentHelper.ElavonMerchantId;
            string convergeUserID = CreditCardPaymentHelper.ElavonUserId;
            string convergePIN = CreditCardPaymentHelper.ElavonPin;
            string invoiceOrderNumber = CreditCardPaymentModelValues.OrderNumber;

            NameValueCollection requestArray = new NameValueCollection()
            {
                {"ssl_amount", amount },
                {"ssl_merchant_id", convergeMerchantID },
                {"ssl_user_id", convergeUserID },
                {"ssl_pin", convergePIN },
                {"ssl_receipt_link_url", redirectLink },
                {"ssl_invoice_number", invoiceOrderNumber},
                {"ssl_invoice_number", "Gosignmeup Registration"},
                {"ssl_transaction_type", "ccsale"},
                {"ssl_show_form", "true"},
                {"ssl_result_format", "HTML"},
                {"ssl_cvv2cvc2_indicator", "1"}
            };

            string nvpstring = "";
            foreach (string key in requestArray)
            {
                //format:  "PARAMETERNAME[lengthofvalue]=VALUE&".  Never URL encode.
                var val = requestArray[key];
                if (val == null)
                {
                    val = string.Empty;
                }
                nvpstring += key + "[" + val.Length + "]=" + val + "&";
            }
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
            HttpWebRequest payReq = (HttpWebRequest)WebRequest.Create(convergePaymentServer);
            payReq.Method = "POST";
            payReq.ContentLength = nvpstring.Length;
            payReq.ContentType = "application/x-www-form-urlencoded";

            StreamWriter sw = new StreamWriter(payReq.GetRequestStream());
            sw.Write(nvpstring);
            sw.Close();

            //get Payflow response
            HttpWebResponse payResp = (HttpWebResponse)payReq.GetResponse();
            StreamReader sr = new StreamReader(payResp.GetResponseStream());
            string response = sr.ReadToEnd();
            sr.Close();

            //parse string into array and return
            //NameValueCollection dict = new NameValueCollection();
            //foreach (string nvp in response.Split('&'))
            //{
            //    string[] keys = nvp.Split('=');
            //    dict.Add(keys[0], keys[1]);
            //}
            return response;
        }



        #endregion

        #region Helpers
        /// <summary>
        ///List of commma separated course numbers via Order Number (ListOfCourseNumbersPerOrderNumber)
        /// </summary>
        public string ListOfCourseNumbersPerOrderNumber(string OrderNumber)
        {
            string course_numbers = string.Empty;
            using (var db = new SchoolEntities())
            {
                var listOfCourses = (from cr in db.Course_Rosters
                                     where cr.OrderNumber == OrderNumber
                                     select cr.COURSEID).ToList();
                foreach (int course_id in listOfCourses)
                {
                    course_numbers = course_numbers + (from c in db.Courses where c.COURSEID == course_id select c.COURSENUM).First() + ", ";
                }
                try
                {
                    course_numbers = course_numbers.Remove(course_numbers.Length - 2);
                }
                catch
                {
                    course_numbers = course_numbers + "";
                }
                return course_numbers;
            }
        }
        private void CartAuditLogger(AuditTrail audit)
        {
            using (var db = new SchoolEntities())
            {
                AuditTrail trail = new AuditTrail()
                {
                    RoutineName = audit.RoutineName,
                    ShortDescription = audit.ShortDescription,
                    DetailDescription = audit.DetailDescription,
                    AuditDate = System.DateTime.Now
                };
                db.AuditTrails.Add(trail);
                db.SaveChanges();
            }
        }


        public void PostBackDateCapture(string OrderNumber)
        {
            using (var db = new SchoolEntities())
            {
                try
                {
                    var orderinprogress = (from order in db.OrderInProgresses where order.OrderNumber == OrderNumber || order.MasterOrderNumber == OrderNumber select order).FirstOrDefault();
                    if (orderinprogress != null)
                    {
                        orderinprogress.postbackdate = DateTime.Now;
                    }
                    db.SaveChanges();
                }
                catch { }
            }
        }

        public bool ValidateUrlReferrer()
        {
            var test = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl.ToLower().Replace("/", "").Replace("https:", "");
            if (HttpContext.Request.UrlReferrer.Authority.ToString().ToLower()!= Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl.ToLower().Replace("/","").Replace("https:",""))
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}

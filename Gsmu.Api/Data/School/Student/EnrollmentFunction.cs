using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Threading.Tasks;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Commerce;
using Gsmu.Api.Commerce.ShoppingCart;
using System.Security.Cryptography;
using Gsmu.Api.Data.ViewModels;
using Gsmu.Api.Data.School.Course;
using Gsmu.Api.Data.School.CourseRoster;
using Gsmu.Api.Authorization;
using Gsmu.Api.Networking.Mail;
using haiku = Gsmu.Api.Integration.Haiku;
using canvas = Gsmu.Api.Integration.Canvas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.Entity.Validation;
using System.Dynamic;
using System.ComponentModel.DataAnnotations;
using BlackBoardAPI;
using static BlackBoardAPI.BlackBoardAPIModel;
using System.Web.Script.Serialization;

namespace Gsmu.Api.Data.School.Student
{
    public class EnrollmentFunction
    {

        public string CarttoCourseRoster(Course_Roster courseRoster, string MaterialList, string MasterOrderNumber)
        {
            var coursedetails = new JObject();
            var userdetails = new JObject();
            using (var db = new SchoolEntities())
            {
                var checkmultiplemasterorder = (from m in db.Course_Rosters where m.OrderNumber == courseRoster.OrderNumber select m.MasterOrderNumber).FirstOrDefault();
                if ((checkmultiplemasterorder != null) && (checkmultiplemasterorder != ""))
                {
                    //return checkmultiplemasterorder;
                    MasterOrderNumber = checkmultiplemasterorder;
                }
            }
            foreach (var item in CourseShoppingCart.Instance.Items)
            {
                if (item.CourseId == courseRoster.COURSEID)                // must see warning note at bellow in this function
                {
                    Course_Roster cr = new Course_Roster(true);
                    OrderInProgress orderinprogress = new OrderInProgress();
                    cr.CourseSalesTaxPaid = courseRoster.CourseSalesTaxPaid;
                    coursedetails.Add("CourseSalesTaxPaid", cr.CourseSalesTaxPaid);
                    if (AuthorizationHelper.CurrentStudentUser != null)
                    {
                        cr.EnrollMaster = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                    }
                    else
                    {
                        cr.EnrollMaster = courseRoster.STUDENTID;
                    }
                    cr.eventid = item.Course.eventid;
                    coursedetails.Add("eventid", item.Course.eventid);
                    cr.COURSEID = item.Course.COURSEID;
                    coursedetails.Add("cid", item.Course.COURSEID);
                    cr.STUDENTID = courseRoster.STUDENTID;
                    userdetails.Add("sid", courseRoster.STUDENTID);
                    cr.MasterOrderNumber = MasterOrderNumber;
                    orderinprogress.MasterOrderNumber = MasterOrderNumber;
                    //cr.CustomSignInNotes = "test";
                    if (courseRoster.WAITING != 1)
                    {
                        cr.WAITING = (short)(item.IsWaiting ? 1 : 0);
                        coursedetails.Add("waiting", cr.WAITING);
                    }
                    else
                    {
                        cr.WAITING = courseRoster.WAITING;
                        coursedetails.Add("waiting", cr.WAITING);
                    }
                    cr.WaitOrder = 10000;

                    orderinprogress.OrderPrice = CourseShoppingCart.Instance.SubTotal.ToString();
                    if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                    {

                        cr.CourseCost = "0.00";
                        coursedetails.Add("coursecost", "$0.00");
                        if (Settings.Instance.GetMasterInfo3().AutoApproveZeroOrder == 1)
                        {
                            if (CourseShoppingCart.Instance.SubTotal > 0)
                            {
                                if (CourseShoppingCart.Instance.PaidInfull)
                                {
                                    cr.PaidInFull = 1;
                                    coursedetails.Add("paidinfull", 1);
                                }
                                else
                                {
                                    cr.PaidInFull = 0;
                                    coursedetails.Add("paidinfull", 0);
                                }
                            }
                            else
                            {
                                cr.PaidInFull = -1;
                                coursedetails.Add("paidinfull", -1);
                            }
                        }
                        else
                        {
                            if (CourseShoppingCart.Instance.PaidInfull)
                            {
                                cr.PaidInFull = 1;
                                coursedetails.Add("paidinfull", 1);
                            }
                            else
                            {
                                cr.PaidInFull = 0;
                                coursedetails.Add("paidinfull", 0);
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            
                            cr.CourseCost = (from course in CourseShoppingCart.Instance.MultipleStudentCourses where course.StudentId == cr.STUDENTID && course.CourseId == cr.COURSEID select course.CourseTotal).FirstOrDefault().ToString();
                            coursedetails.Add("coursecost", cr.CourseCost);
                        }
                        catch
                        {
                            cr.CourseCost = "0.00";
                            coursedetails.Add("coursecost", cr.CourseCost);
                        }


                        cr.PaidInFull = 0;
                        coursedetails.Add("paidinfull", 0);


                            coursedetails.Add("partialpayment", CheckoutInfo.Instance.HasPartialPayment);
                        

                    }

                    if (CourseShoppingCart.Instance.PaidInfull)
                    {
                        try
                        {
                            cr.PaidInFull = 1;
                            coursedetails.Add("paidinfull", 1);
                        }
                        catch { }
                    }
                    cr.OrderNumber = courseRoster.OrderNumber;
                    orderinprogress.OrderNumber = courseRoster.OrderNumber;
                    cr.ATTENDED = 0;
                    cr.DIDNTATTEND = 0;
                    cr.CancelStatus = RosterCancelStatus.WaitingForPayment;
                    //cr.EnrollMaster = courseRoster.STUDENTID;
                    cr.RosterFrom = 3; // set Ruby to 3. Admin is 2, classic student is 1.

                    cr.PricingMember = 0;

                    CourseModel cmodel = new CourseModel(cr.COURSEID.Value);
                    if ((cmodel.CreditOption == 2) || (cmodel.CreditOption == 1))
                    {
                        if (item.SelectedCredits != null)
                        {
                            if (item.SelectedCredits.Count > 0)
                            {
                                foreach (var credit in item.SelectedCredits)
                                {
                                    if (credit.CourseCreditType == CourseCreditType.Credit)
                                    {
                                        cr.HOURS = item.Course.CREDITHOURS;
                                        cr.CourseHoursType = "CH";

                                        coursedetails.Add("hours", item.Course.CREDITHOURS);
                                    }
                                    else
                                    {
                                        if ((cr.HOURS == null))
                                        {
                                            cr.HOURS = 0;
                                            coursedetails.Add("hours", 0);
                                        }
                                    }

                                    if (credit.CourseCreditType == CourseCreditType.Ceu)
                                    {
                                        cr.ceucredit = float.Parse(item.Course.CEUCredit.ToString());
                                        coursedetails.Add("ceucredit", item.Course.CEUCredit.ToString());
                                    }
                                    else
                                    {
                                        if (cr.ceucredit == null)
                                        {
                                            cr.ceucredit = 0;
                                            coursedetails.Add("ceucredit", 0);
                                        }
                                    }

                                    if (credit.CourseCreditType == CourseCreditType.Graduate)
                                    {
                                        cr.graduatecredit = float.Parse(item.Course.GraduateCredit.ToString());
                                        coursedetails.Add("graduatecredit", item.Course.GraduateCredit.ToString());
                                    }

                                    else
                                    {
                                        if (cr.graduatecredit == null)
                                            cr.graduatecredit = 0;
                                        coursedetails.Add("graduatecredit", 0);
                                    }

                                    if (credit.CourseCreditType == CourseCreditType.Custom)
                                    {
                                        cr.CustomCreditHours = float.Parse(item.Course.CustomCreditHours.ToString());
                                        cr.CourseHoursType = "CCH";
                                        coursedetails.Add("CustomCreditHours", item.Course.CustomCreditHours.ToString());
                                    }

                                    else
                                    {
                                        if (cr.CustomCreditHours == null)
                                            cr.CustomCreditHours = 0;
                                        coursedetails.Add("CustomCreditHours", 0);
                                    }

                                    if (credit.CourseCreditType == CourseCreditType.InService)
                                    {

                                        cr.InserviceHours = item.Course.InserviceHours;
                                        cr.CourseHoursType = "ISH";
                                        coursedetails.Add("InserviceHours", item.Course.InserviceHours);
                                    }

                                    else
                                    {
                                        if (cr.InserviceHours == null)
                                            cr.InserviceHours = 0;
                                        coursedetails.Add("InserviceHours", 0);
                                    }

                                    if (credit.CourseCreditType == CourseCreditType.Optional1)
                                    {
                                        cr.Optionalcredithours1 = item.Course.Optionalcredithours1;
                                        coursedetails.Add("Optionalcredithours1", item.Course.Optionalcredithours1);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours1 == null)
                                            cr.Optionalcredithours1 = 0;
                                        coursedetails.Add("Optionalcredithours1", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional2)
                                    {
                                        cr.Optionalcredithours2 = item.Course.Optionalcredithours2;
                                        coursedetails.Add("Optionalcredithours2", item.Course.Optionalcredithours2);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours2 == null)
                                            cr.Optionalcredithours2 = 0;
                                        coursedetails.Add("Optionalcredithours2", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional3)
                                    {
                                        cr.Optionalcredithours3 = item.Course.Optionalcredithours3;
                                        coursedetails.Add("Optionalcredithours3", item.Course.Optionalcredithours3);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours3 == null)
                                            cr.Optionalcredithours3 = 0;
                                        coursedetails.Add("Optionalcredithours3", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional4)
                                    {
                                        cr.Optionalcredithours4 = item.Course.Optionalcredithours4;
                                        coursedetails.Add("Optionalcredithours4", item.Course.Optionalcredithours4);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours4 == null)
                                            cr.Optionalcredithours4 = 0;
                                        coursedetails.Add("Optionalcredithours4", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional5)
                                    {
                                        cr.Optionalcredithours5 = item.Course.Optionalcredithours5;
                                        coursedetails.Add("Optionalcredithours5", item.Course.Optionalcredithours5);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours5 == null)
                                            cr.Optionalcredithours5 = 0;
                                        coursedetails.Add("Optionalcredithours5", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional6)
                                    {
                                        cr.Optionalcredithours6 = item.Course.Optionalcredithours6;
                                        coursedetails.Add("Optionalcredithours6", item.Course.Optionalcredithours6);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours6 == null)
                                            cr.Optionalcredithours6 = 0;
                                        coursedetails.Add("Optionalcredithours6", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional7)
                                    {
                                        cr.Optionalcredithours7 = item.Course.Optionalcredithours7;
                                        coursedetails.Add("Optionalcredithours7", item.Course.Optionalcredithours7);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours7 == null)
                                            cr.Optionalcredithours7 = 0;
                                        coursedetails.Add("Optionalcredithours7", 0);
                                    }
                                    if (credit.CourseCreditType == CourseCreditType.Optional8)
                                    {
                                        cr.Optionalcredithours8 = item.Course.Optionalcredithours8;
                                        coursedetails.Add("Optionalcredithours8", item.Course.Optionalcredithours8);
                                    }

                                    else
                                    {
                                        if (cr.Optionalcredithours8 == null)
                                            cr.Optionalcredithours8 = 0;
                                        coursedetails.Add("Optionalcredithours8", 0);
                                    }

                                }
                                if (Settings.Instance.GetMasterInfo2().PricingHourType == 1)
                                {
                                    if (item.SelectedCredits.Count == 2)
                                    {
                                        cr.CourseHoursType = "CUST";
                                    }
                                    if (item.SelectedCredits.Count == 3)
                                    {
                                        cr.CourseHoursType = "BOTH";
                                    }
                                    if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                                    {
                                        cr.CourseHoursType = "BOTH";
                                    }


                                    if ((cr.CourseHoursType == "") || (cr.CourseHoursType == null))
                                    {
                                        cr.CourseHoursType = "NONE";
                                    }
                                }
                                else
                                {
                                    if (cr.CourseHoursType == "")
                                    {
                                        if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                                        {
                                            cr.CourseHoursType = "BOTH";
                                        }
                                        else
                                        {
                                            cr.CourseHoursType = "NONE";
                                        }
                                    }
                                    else
                                    {
                                        cr.CourseHoursType = cr.CourseHoursType;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        cr.HOURS = item.Course.CREDITHOURS;
                        coursedetails.Add("HOURS", item.Course.CREDITHOURS);
                        cr.InserviceHours = item.Course.InserviceHours;
                        coursedetails.Add("InserviceHours", cr.InserviceHours);
                        cr.CustomCreditHours = item.Course.CustomCreditHours;
                        coursedetails.Add("CustomCreditHours", item.Course.CustomCreditHours);
                        cr.Optionalcredithours1 = 0;
                        cr.ceucredit = 0;
                        cr.graduatecredit = 0;
                        int isMultipleCredits = 0;
                        if (cr.InserviceHours > 0)
                        {
                            isMultipleCredits = isMultipleCredits + 1;
                        }
                        if (cr.CustomCreditHours > 0)
                        {
                            isMultipleCredits = isMultipleCredits + 1;
                        }
                        if (cr.HOURS > 0)
                        {
                            isMultipleCredits = isMultipleCredits + 1;
                        }

                        if (isMultipleCredits >= 1)
                        {
                            cr.CourseHoursType = "BOTH";
                        }

                    }
                    cr.BBCancelled = 0;
                    cr.CreditedInFull = 0;
                    cr.CourseSurveySent = 0;
                    cr.Expire = 0;
                    cr.AttendanceStatusId = 0;
                    cr.ExternalDBUpdated = 0;
                    cr.OptionalInfo1 = 0;
                    cr.OptionalInfo2A = 0;
                    cr.OptionalInfo2B = 0;
                    cr.OptionalInfo2C = 0;
                    cr.OptionalInfo2D = 0;
                    cr.OptionalInfo2E = 0;
                    cr.OptionalInfo2F = 0;
                    cr.OptionalInfo2G = 0;
                    cr.Rebill = 0;
                    cr.CreditedInFull = 0;
                    cr.CustomCreditHours = 0;
                    cr.CRPartialPaymentMade = 0;
                    cr.CRPrimaryTotalPaid = 0;
                    cr.ExtraParticipant = item.CRExtraParticipant;

                    //get the coupon code from the cart 
                    cr.CouponCode = courseRoster.CouponCode;
                    coursedetails.Add("CouponCode", courseRoster.CouponCode);
                    cr.originalstudentid = 0;
                    cr.SAPSyncCount = 0;
                    cr.Membership = 0;
                    cr.StudentChoiceCourse = (item.CourseChoice != null ? item.CourseChoice.CourseChoiceId : 0);
                    // ----------------------------------------------------------------------------------------------------------------warning
                    // variable will be change when programming is completed.
                    // ----------------------------------------------------------------------------------------------------------------warning
                    cr.HasPartialPayment = 0;
                    cr.paidremainderamount = 0;
                    cr.creditcardfee = 0;
                    cr.shippingfee = Convert.ToDecimal(0);
                    cr.preAuthAmount = "0";
                    cr.FTCourseRosterId = courseRoster.RosterID;
                    cr.IsHoursPaid = 0;
                    cr.SubSiteId = 0;
                    cr.AllowModifyMultiEnroll = 0;
                    cr.Parking = 0;





                    cr.CheckoutComments = courseRoster.CheckoutComments;
                    coursedetails.Add("CheckoutComments", courseRoster.CheckoutComments);
                    cr.CheckoutComments2 = courseRoster.CheckoutComments2;
                    coursedetails.Add("CheckoutComments2", courseRoster.CheckoutComments2);

                    if (courseRoster.STUDENTID == item.StudentId)
                    {
                        cr.SingleRosterDiscountCoupon = item.DiscountCouponPerCourse;
                        coursedetails.Add("SingleRosterDiscountCoupon", item.DiscountCouponPerCourse);
                        coursedetails.Add("RoommateGender", item.RoommateGender);
                        coursedetails.Add("RoommateName", item.RoommateName);
                        coursedetails.Add("RoommateQuestion", item.RoommateQuestion);
                        try
                        {
                            cr.SingleRosterDiscountAmount = float.Parse(item.DiscountAmountPerCourse);
                            coursedetails.Add("SingleRosterDiscountAmount", item.DiscountAmountPerCourse);
                        }
                        catch (Exception)
                        {
                            cr.SingleRosterDiscountAmount = 0;
                            coursedetails.Add("SingleRosterDiscountAmount", 0);
                        }
                    }
                    else
                    {
                        var multiplestudentincart = CourseShoppingCart.Instance.MultipleStudentCourses.Where(student => student.StudentId == courseRoster.STUDENTID && student.CourseId == courseRoster.COURSEID).FirstOrDefault();
                        if (multiplestudentincart.StudentId == courseRoster.STUDENTID)
                        {
                            try
                            {
                                cr.SingleRosterDiscountAmount = float.Parse(multiplestudentincart.DiscountAmountPerCourse);
                                coursedetails.Add("SingleRosterDiscountAmount", multiplestudentincart.DiscountAmountPerCourse);
                                cr.SingleRosterDiscountCoupon = item.DiscountCouponPerCourse;
                                coursedetails.Add("SingleRosterDiscountCoupon", item.DiscountCouponPerCourse);
                            }
                            catch (Exception)
                            {
                                cr.SingleRosterDiscountAmount = 0;
                                coursedetails.Add("SingleRosterDiscountAmount", 0);
                            }
                        }
                        else
                        {
                            cr.SingleRosterDiscountAmount = 0;
                            coursedetails.Add("SingleRosterDiscountAmount", 0);
                        }
                    }
                    var countlistmasterstudent = (from a in CourseShoppingCart.Instance.MultipleStudentCourses group a by a.OrderNumber into g select new { a = g.Key }).Count();

                    if (countlistmasterstudent > 1)
                    {
                        var MultipleStudentCourses = (from a in CourseShoppingCart.Instance.MultipleStudentCourses where a.StudentId == cr.STUDENTID select a).ToList();
                        if (MultipleStudentCourses.Count > 0)
                        {
                            decimal subTotal = 0;
                            decimal discounttotal = 0;
                            Coupon couponDetails = GetCouponDiscountDetails(cr.CouponCode);
                            if ((couponDetails != null) && (couponDetails.CouponCode != "notexist") && (courseRoster.CouponDiscount > 0))
                            {
                                foreach (var student in MultipleStudentCourses)
                                {
                                    if (student.CourseTotal != null)
                                    {
                                        subTotal = subTotal + student.CourseTotal + CourseShoppingCart.Instance.GetCourseItem(student.CourseId).MateriaTotal;
                                    }
                                }


                                var coupoun_dollar = couponDetails.CouponDollarAmount;
                                var coupon_percent = couponDetails.CouponPercentAmount;

                                try
                                {
                                    if (coupoun_dollar > 0)
                                    {
                                        discounttotal = decimal.Parse(coupoun_dollar.ToString());
                                    }
                                    if (coupon_percent > 0)
                                    {
                                        if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 0)
                                        {
                                            discounttotal = CourseShoppingCart.Instance.SubTotal * (decimal.Parse(coupon_percent.ToString()) / 100);
                                        }
                                        else
                                        {
                                            discounttotal = discounttotal + subTotal * (decimal.Parse(coupon_percent.ToString()) / 100);
                                        }
                                    }
                                    cr.CouponDiscount = float.Parse(discounttotal.ToString());
                                    coursedetails.Add("CouponDiscount", cr.CouponDiscount);
                                }
                                catch
                                {
                                    cr.CouponDiscount = 0;
                                    coursedetails.Add("CouponDiscount", cr.CouponDiscount);
                                }
                            }
                            else
                            {
                                cr.CouponDiscount = 0;
                                coursedetails.Add("CouponDiscount", cr.CouponDiscount);
                            }

                        }
                    }
                    else
                    {
                        cr.CouponDiscount = courseRoster.CouponDiscount / countlistmasterstudent;
                        coursedetails.Add("CouponDiscount", cr.CouponDiscount);
                    }

                    if (cr.CouponDiscount <= 0)
                    {
                        try
                        {
                            cr.CouponDiscount = float.Parse(cr.SingleRosterDiscountAmount.ToString());
                            coursedetails.Add("CouponDiscount", cr.SingleRosterDiscountAmount);
                        }
                        catch
                        {
                        }
                    }
                    if (item.PricingModel != null && item.PricingModel.PricingOption != null)
                    {
                        cr.PricingOption = item.PricingModel.PricingOption.PriceTypedesc;

                        if (item.PricingModel.PricingOption.inservicehour == 1)
                        {
                            cr.CourseHoursType = "ISH";
                        }
                        else if (item.PricingModel.PricingOption.credithour == 1)
                        {
                            cr.CourseHoursType = "CH";
                        }
                    }

                    if (Settings.Instance.GetMasterInfo2().PricingHourType == 1)
                    {
                        if (item.SelectedCredits.Count == 2)
                        {
                            cr.CourseHoursType = "CUST";
                        }
                        if (item.SelectedCredits.Count == 3)
                        {
                            cr.CourseHoursType = "BOTH";
                        }
                        if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                        {
                            cr.CourseHoursType = "BOTH";
                        }


                        if ((cr.CourseHoursType == "") || (cr.CourseHoursType == null))
                        {
                            cr.CourseHoursType = "NONE";
                        }
                    }
                    else
                    {
                        if (cr.CourseHoursType == "")
                        {
                            if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                            {
                                cr.CourseHoursType = "BOTH";
                            }
                            else
                            {
                                cr.CourseHoursType = "NONE";
                            }
                        }
                        else
                        {
                            cr.CourseHoursType = cr.CourseHoursType;
                        }
                    }
                    cr.CRInitialAuditInfo = "0  NONE  NONE  0  0  0  0  0  200  1000";
                    if (Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.GetConfig().useClubPilates == 1 && item.Course.clubready_package_id > 0)
                    {
                        coursedetails.Add("clubpilatespackageinstallmentdetails", Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.BuildRosterPackageInstallmentDetails(item.Course.clubready_package_id.Value, item.PricingModel.CoursePricingOption.CoursePricingOptionId));
                        cr.clubpilatespackageinstallmentdetails = Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.BuildRosterPackageInstallmentDetails(item.Course.clubready_package_id.Value, item.PricingModel.CoursePricingOption.CoursePricingOptionId);
                    }
                    coursedetails.Add("aCRInitialAuditInfo", cr.CRInitialAuditInfo);
                    coursedetails.Add("CourseHoursType", cr.CourseHoursType);

                    if (item.Course.coursetype != 1)
                    {
                        using (var db = new SchoolEntities())
                        {
                            var checkduplicates = (from a in db.Course_Rosters where a.COURSEID == cr.COURSEID && a.STUDENTID == cr.STUDENTID && a.OrderNumber == cr.OrderNumber select a).FirstOrDefault();
                            if (checkduplicates == null)
                            {
                                var checkmultipleenrollment = (from roster_ in db.Course_Rosters where roster_.COURSEID == cr.COURSEID && roster_.STUDENTID == cr.STUDENTID && roster_.Cancel == 0 select roster_).FirstOrDefault();
                                if (checkmultipleenrollment == null)
                                {
                                    if ((item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0))
                                    {
                                        if (CourseShoppingCart.Instance.SubTotal == 0)
                                        {
                                            cr.CourseCost = "0";
                                            db.Course_Rosters.Add(cr);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                checkduplicates.CouponDiscount = cr.CouponDiscount;
                                checkduplicates.CouponCode = cr.CouponCode;
                            }
                            string CourseExtraParticipants = "";
                            if (item.HasExtraParticipants)
                            {
                                CourseExtraParticipants = Newtonsoft.Json.JsonConvert.SerializeObject(item.ExtraParticipants);
                                foreach (var cep in item.ExtraParticipants)
                                {
                                    if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                                    {
                                        if (CourseShoppingCart.Instance.SubTotal == 0)
                                        {
                                            db.CourseExtraParticipants.Add(cep);
                                            cr.CourseExtraParticipants.Add(cep);
                                        }
                                    }
                                }
                            }
                            coursedetails.Add("MaterialList", MaterialList);
                            coursedetails.Add("CourseExtraParticipants", CourseExtraParticipants);
                            orderinprogress.coursedetails = coursedetails.ToString(Newtonsoft.Json.Formatting.None);
                            orderinprogress.userlogindetails = userdetails.ToString(Newtonsoft.Json.Formatting.None);
                            orderinprogress.OrderCurStatus = "Pending";
                            orderinprogress.orderdate = DateTime.Now;

                            if (AuthorizationHelper.CurrentStudentUser != null)
                                orderinprogress.UserSessionid = (from stud in db.Students where stud.STUDENTID == AuthorizationHelper.CurrentStudentUser.STUDENTID select stud.UserSessionId).FirstOrDefault();
                            if (AuthorizationHelper.CurrentAdminUser != null)
                                orderinprogress.UserSessionid = (from stud in db.adminpasses where stud.AdminID == AuthorizationHelper.CurrentAdminUser.AdminID select stud.UserSessionId).FirstOrDefault();
                            if (AuthorizationHelper.CurrentSupervisorUser != null)
                                orderinprogress.UserSessionid = (from stud in db.Supervisors where stud.SUPERVISORID == AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID select stud.UserSessionId).FirstOrDefault();
                            if (AuthorizationHelper.CurrentInstructorUser != null)
                                orderinprogress.UserSessionid = (from stud in db.Instructors where stud.INSTRUCTORID == AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID select stud.UserSessionId).FirstOrDefault();

                            db.OrderInProgresses.Add(orderinprogress);
                            db.SaveChanges();

                            //membership
                            if (item.Course.COURSENUM == "~ZZZZZZ~")
                            {
                                UpdateStudentInfo(cr.STUDENTID, "approvemembership");
                            }
                            int rosterid = cr.RosterID;
                            if (rosterid > 0)
                            {
                                RoommateRequest rmrequest = new RoommateRequest();
                                rmrequest.rosterid = rosterid;
                                rmrequest.reqRMName = item.RoommateName;
                                rmrequest.reqRMEmail = item.RoommateGender;
                                rmrequest.reqRMAddress = item.RoommateQuestion;
                                rmrequest.courseid = item.Course.COURSEID;
                                rmrequest.reqRMApproved = 0;
                                rmrequest.reqdate = DateTime.Now;
                                if (!String.IsNullOrEmpty(rmrequest.reqRMName))
                                {
                                    SaveRoomMateRequest(rmrequest);
                                }
                            }
                            // please keep the errors sent to the browser instead of hiding with
                            // try {} catch { empty? } 
                            if (!string.IsNullOrWhiteSpace(MaterialList))
                            {
                                foreach (string materialids in MaterialList.Split(','))
                                {
                                    if (materialids.Contains('|'))
                                    {
                                        try
                                        {
                                            if (int.Parse(materialids.Split('|')[0]) == cr.COURSEID)
                                            {
                                                if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                                                {
                                                    if (CourseShoppingCart.Instance.SubTotal == 0)
                                                    {
                                                        SaveRosterMaterial(rosterid, GetMaterialDetails(int.Parse(materialids.Split('|')[1])), int.Parse(materialids.Split('|')[2]));
                                                    }
                                                }
                                            }
                                        }
                                        finally
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            coursedetails = new JObject();
            userdetails = new JObject();

            return MasterOrderNumber;
        }
        /// <summary>
        /// cart must not empty
        /// </summary>
        public string CarttoCourseRoster(Course_Roster courseRoster, string MaterialList)
        {
            string strOrderNumber = "";
            if (CourseShoppingCart.Instance.CurrentOrderNumber != null && CourseShoppingCart.Instance.CurrentOrderNumber != "")
            {
                strOrderNumber = CourseShoppingCart.Instance.CurrentOrderNumber;
                using (var db = new SchoolEntities())
                {
                    (from orderinprog_ in db.OrderInProgresses where orderinprog_.OrderNumber == strOrderNumber select orderinprog_).ToList().ForEach(f => f.OrderCurStatus = "Duplicate");
                    db.SaveChanges();
                }
            }
            else
            {
                strOrderNumber = CheckGenerateOrderID(int.Parse(courseRoster.STUDENTID.ToString()));
            }
            CourseShoppingCart.Instance.CurrentOrderNumber = strOrderNumber;
            string aCRInitialAuditInfo = "";
            var coursedetails = new JObject();
            var userdetails = new JObject();
            foreach (var item in CourseShoppingCart.Instance.Items)
            {
                // must see warning note at bellow in this function
                Course_Roster cr = new Course_Roster(true);
                OrderInProgress orderinprogress = new OrderInProgress();
                cr.CourseSalesTaxPaid = courseRoster.CourseSalesTaxPaid;
                coursedetails.Add("CourseSalesTaxPaid", cr.CourseSalesTaxPaid);
                cr.eventid = item.Course.eventid;
                coursedetails.Add("eventid", item.Course.eventid);
                cr.COURSEID = item.Course.COURSEID;
                coursedetails.Add("cid", item.Course.COURSEID);
                cr.STUDENTID = courseRoster.STUDENTID;
                userdetails.Add("sid", courseRoster.STUDENTID);
                userdetails.Add("membership", AuthorizationHelper.CurrentUser.MembershipType.ToString());
                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    userdetails.Add("email", AuthorizationHelper.CurrentStudentUser.EMAIL);
                }
                //cr.CustomSignInNotes = "test";
                cr.WAITING = (short)(item.IsWaiting ? 1 : 0);
                coursedetails.Add("waiting", cr.WAITING);
                cr.WaitOrder = 10000;
                orderinprogress.OrderPrice = CourseShoppingCart.Instance.SubTotal.ToString();
                if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                {
                    cr.CourseCost = "$0.00";
                    coursedetails.Add("coursecost", "$0.00");
                    if (Settings.Instance.GetMasterInfo3().AutoApproveZeroOrder == 1)
                    {
                        if (CourseShoppingCart.Instance.SubTotal > 0)
                        {
                            cr.PaidInFull = 0;
                            coursedetails.Add("paidinfull", 0);
                            coursedetails.Add("partialpayment", CheckoutInfo.Instance.HasPartialPayment);
                        }
                        else
                        {
                            cr.PaidInFull = -1;
                            coursedetails.Add("paidinfull", -1);
                        }
                    }
                    else
                    {
                        cr.PaidInFull = 0;
                        coursedetails.Add("paidinfull", 0);
                        coursedetails.Add("partialpayment", CheckoutInfo.Instance.HasPartialPayment);

                    }
                    aCRInitialAuditInfo = "0";
                }
                else
                {
                    cr.CourseCost = item.CourseTotal.ToString();
                    coursedetails.Add("coursecost", item.CourseTotal.ToString());
                    cr.PaidInFull = 0;
                    coursedetails.Add("paidinfull", 0);

                    
                        coursedetails.Add("partialpayment", CheckoutInfo.Instance.HasPartialPayment);
                    
                    aCRInitialAuditInfo = item.CourseTotal.ToString();
                }
                cr.OrderNumber = strOrderNumber;
                orderinprogress.OrderNumber = strOrderNumber;
                cr.ATTENDED = 0;
                cr.DIDNTATTEND = 0;
                cr.CancelStatus = RosterCancelStatus.WaitingForPayment;
                cr.EnrollMaster = courseRoster.STUDENTID;
                cr.RosterFrom = 3; // set Ruby to 3. Admin is 2, classic student is 1.

                cr.PricingMember = 0;

                CourseModel cmodel = new CourseModel(cr.COURSEID.Value);
                if ((cmodel.CreditOption == 2) || (cmodel.CreditOption == 1))
                {
                    if (item.SelectedCredits != null)
                    {
                        if (item.SelectedCredits.Count > 0)
                        {
                            foreach (var credit in item.SelectedCredits)
                            {
                                if (credit.CourseCreditType == CourseCreditType.Credit)
                                {
                                    cr.HOURS = item.Course.CREDITHOURS;
                                    cr.CourseHoursType = "CH";
                                    if (coursedetails["hours"] == null)
                                    {
                                        coursedetails.Add("hours", item.Course.CREDITHOURS);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("hours");
                                        coursedetails.Add("hours", item.Course.CREDITHOURS);
                                    }

                                }
                                else
                                {
                                    if ((cr.HOURS == null))
                                    {
                                        cr.HOURS = 0;
                                        if (coursedetails["hours"] == null)
                                        {

                                            coursedetails.Add("hours", 0);
                                        }
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.Ceu)
                                {
                                    cr.ceucredit = float.Parse(item.Course.CEUCredit.ToString());
                                    if (coursedetails["ceucredit"] == null)
                                    {
                                        coursedetails.Add("ceucredit", item.Course.CEUCredit.ToString());
                                    }
                                    else
                                    {
                                        coursedetails.Remove("ceucredit");
                                        coursedetails.Add("ceucredit", item.Course.CEUCredit.ToString());
                                    }
                                }
                                else
                                {
                                    if (cr.ceucredit == null)
                                    {
                                        cr.ceucredit = 0;
                                        if (coursedetails["ceucredit"] == null)
                                        {
                                            coursedetails.Add("ceucredit", 0);
                                        }
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.Graduate)
                                {
                                    cr.graduatecredit = float.Parse(item.Course.GraduateCredit.ToString());
                                    if (coursedetails["graduatecredit"] == null)
                                    {
                                        coursedetails.Add("graduatecredit", item.Course.GraduateCredit.ToString());
                                    }
                                    else
                                    {
                                        coursedetails.Remove("graduatecredit");
                                        coursedetails.Add("graduatecredit", item.Course.GraduateCredit.ToString());
                                    }
                                }

                                else
                                {
                                    if (cr.graduatecredit == null)
                                        cr.graduatecredit = 0;
                                    if (coursedetails["graduatecredit"] == null)
                                    {
                                        coursedetails.Add("graduatecredit", 0);
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.Custom)
                                {
                                    cr.CustomCreditHours = float.Parse(item.Course.CustomCreditHours.ToString());
                                    cr.CourseHoursType = "CCH";
                                    if (coursedetails["CustomCreditHours"] == null)
                                    {
                                        coursedetails.Add("CustomCreditHours", item.Course.CustomCreditHours.ToString());
                                    }
                                    else
                                    {
                                        coursedetails.Remove("CustomCreditHours");
                                        coursedetails.Add("CustomCreditHours", item.Course.CustomCreditHours.ToString());
                                    }

                                }

                                else
                                {
                                    if (cr.CustomCreditHours == null)
                                        cr.CustomCreditHours = 0;
                                    if (coursedetails["CustomCreditHours"] == null)
                                    {
                                        coursedetails.Add("CustomCreditHours", 0);
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.InService)
                                {

                                    cr.InserviceHours = item.Course.InserviceHours;
                                    cr.CourseHoursType = "ISH";
                                    if (coursedetails["InserviceHours"] == null)
                                    {
                                        coursedetails.Add("InserviceHours", item.Course.InserviceHours);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("InserviceHours");
                                        coursedetails.Add("InserviceHours", item.Course.InserviceHours);
                                    }

                                }

                                else
                                {
                                    if (cr.InserviceHours == null)
                                        cr.InserviceHours = 0;
                                    if (coursedetails["InserviceHours"] == null)
                                    {
                                        coursedetails.Add("InserviceHours", 0);
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.Optional1)
                                {
                                    cr.Optionalcredithours1 = item.Course.Optionalcredithours1;
                                    if (coursedetails["Optionalcredithours1"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours1", item.Course.Optionalcredithours1);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours1");
                                        coursedetails.Add("Optionalcredithours1", item.Course.Optionalcredithours1);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours1 == null)
                                        cr.Optionalcredithours1 = 0;
                                    if (coursedetails["Optionalcredithours1"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours1", 0);
                                    }
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional2)
                                {
                                    cr.Optionalcredithours2 = item.Course.Optionalcredithours2;
                                    if (coursedetails["Optionalcredithours2"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours2", item.Course.Optionalcredithours2);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours2");
                                        coursedetails.Add("Optionalcredithours2", item.Course.Optionalcredithours2);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours2 == null)
                                        cr.Optionalcredithours2 = 0;
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional3)
                                {
                                    cr.Optionalcredithours3 = item.Course.Optionalcredithours3;
                                    if (coursedetails["Optionalcredithours3"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours3", item.Course.Optionalcredithours3);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours3");
                                        coursedetails.Add("Optionalcredithours3", item.Course.Optionalcredithours3);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours3 == null)
                                        cr.Optionalcredithours3 = 0;
                                    if (coursedetails["Optionalcredithours3"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours3", 0);
                                    }
                                }

                                if (credit.CourseCreditType == CourseCreditType.Optional4)
                                {
                                    cr.Optionalcredithours4 = item.Course.Optionalcredithours4;
                                    if (coursedetails["Optionalcredithours4"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours4", item.Course.Optionalcredithours4);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours4");
                                        coursedetails.Add("Optionalcredithours4", item.Course.Optionalcredithours4);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours4 == null)
                                        cr.Optionalcredithours4 = 0;
                                    if (coursedetails["Optionalcredithours4"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours4", 0);
                                    }
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional5)
                                {
                                    cr.Optionalcredithours5 = item.Course.Optionalcredithours5;
                                    if (coursedetails["Optionalcredithours5"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours5", item.Course.Optionalcredithours5);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours5");
                                        coursedetails.Add("Optionalcredithours5", item.Course.Optionalcredithours5);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours5 == null)
                                        cr.Optionalcredithours5 = 0;
                                    if (coursedetails["Optionalcredithours5"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours5", 0);
                                    }
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional6)
                                {
                                    cr.Optionalcredithours6 = item.Course.Optionalcredithours6;
                                    if (coursedetails["Optionalcredithours6"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours6", item.Course.Optionalcredithours6);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours6");
                                        coursedetails.Add("Optionalcredithours6", item.Course.Optionalcredithours6);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours6 == null)
                                        cr.Optionalcredithours6 = 0;
                                    if (coursedetails["Optionalcredithours6"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours6", 0);
                                    }
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional7)
                                {
                                    cr.Optionalcredithours7 = item.Course.Optionalcredithours7;
                                    if (coursedetails["Optionalcredithours7"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours7", item.Course.Optionalcredithours7);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours7");
                                        coursedetails.Add("Optionalcredithours7", item.Course.Optionalcredithours7);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours7 == null)
                                        cr.Optionalcredithours7 = 0;
                                    if (coursedetails["Optionalcredithours7"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours7", 0);
                                    }
                                }
                                if (credit.CourseCreditType == CourseCreditType.Optional8)
                                {
                                    cr.Optionalcredithours8 = item.Course.Optionalcredithours8;
                                    if (coursedetails["Optionalcredithours8"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours8", item.Course.Optionalcredithours8);
                                    }
                                    else
                                    {
                                        coursedetails.Remove("Optionalcredithours8");
                                        coursedetails.Add("Optionalcredithours8", item.Course.Optionalcredithours8);
                                    }
                                }

                                else
                                {
                                    if (cr.Optionalcredithours8 == null)
                                        cr.Optionalcredithours8 = 0;
                                    if (coursedetails["Optionalcredithours8"] == null)
                                    {
                                        coursedetails.Add("Optionalcredithours8", 0);
                                    }
                                }

                            }
                            if (Settings.Instance.GetMasterInfo2().PricingHourType == 1)
                            {
                                if (item.SelectedCredits.Count == 2)
                                {
                                    cr.CourseHoursType = "CUST";
                                }
                                if (item.SelectedCredits.Count == 3)
                                {
                                    cr.CourseHoursType = "BOTH";
                                }
                                if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                                {
                                    cr.CourseHoursType = "BOTH";
                                }


                                if ((cr.CourseHoursType == "") || (cr.CourseHoursType == null))
                                {
                                    cr.CourseHoursType = "NONE";
                                }
                            }
                            else
                            {
                                if (cr.CourseHoursType == "")
                                {
                                    if ((cr.InserviceHours > 0) && (cr.CustomCreditHours > 0) && (cr.HOURS > 0))
                                    {
                                        cr.CourseHoursType = "BOTH";
                                    }
                                    else
                                    {
                                        cr.CourseHoursType = "NONE";
                                    }
                                }
                                else
                                {
                                    cr.CourseHoursType = cr.CourseHoursType;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (item.Course.CREDITHOURS != null)
                    {
                        cr.HOURS = item.Course.CREDITHOURS;
                        coursedetails.Add("HOURS", item.Course.CREDITHOURS);
                    }
                    else
                    {
                        cr.HOURS = 0;
                        coursedetails.Add("HOURS", 0);
                    }
                    cr.InserviceHours = item.Course.InserviceHours;

                    if (item.Course.CustomCreditHours != null)
                    {
                        cr.CustomCreditHours = item.Course.CustomCreditHours;
                        coursedetails.Add("CustomCreditHours", item.Course.CustomCreditHours);
                    }
                    else
                    {
                        cr.CustomCreditHours = 0;
                        coursedetails.Add("CustomCreditHours", 0);
                    }
                    cr.Optionalcredithours1 = 0;
                    cr.ceucredit = 0;
                    cr.graduatecredit = 0;
                    int isMultipleCredits = 0;
                    if (cr.InserviceHours > 0)
                    {
                        isMultipleCredits = isMultipleCredits + 1;
                    }
                    if (cr.CustomCreditHours > 0)
                    {
                        isMultipleCredits = isMultipleCredits + 1;
                    }
                    if (cr.HOURS > 0)
                    {
                        isMultipleCredits = isMultipleCredits + 1;
                    }

                    if (isMultipleCredits >= 1)
                    {
                        cr.CourseHoursType = "BOTH";
                    }

                }


                cr.BBCancelled = 0;
                cr.CreditedInFull = 0;
                cr.CourseSurveySent = 0;
                cr.Expire = 0;
                cr.AttendanceStatusId = 0;
                cr.ExternalDBUpdated = 0;

                cr.OptionalInfo1 = 0;
                cr.OptionalInfo2A = 0;
                cr.OptionalInfo2B = 0;
                cr.OptionalInfo2C = 0;
                cr.OptionalInfo2D = 0;
                cr.OptionalInfo2E = 0;
                cr.OptionalInfo2F = 0;
                cr.OptionalInfo2G = 0;
                cr.Rebill = 0;
                cr.CreditedInFull = 0;
                // cr.CustomCreditHours = 0;
                cr.CRPartialPaymentMade = 0;
                cr.CRPrimaryTotalPaid = 0;
                cr.ExtraParticipant = item.CRExtraParticipant;
                coursedetails.Add("ExtraParticipant", item.CRExtraParticipant);
                //get the coupon code from the cart 
                cr.CouponCode = courseRoster.CouponCode;
                coursedetails.Add("CouponCode", courseRoster.CouponCode);
                cr.originalstudentid = 0;
                cr.SAPSyncCount = 0;
                cr.Membership = 0;
                cr.StudentChoiceCourse = (item.CourseChoice != null ? item.CourseChoice.CourseChoiceId : 0);
                coursedetails.Add("StudentChoiceCourse", cr.StudentChoiceCourse);
                // ----------------------------------------------------------------------------------------------------------------warning
                // variable will be change when programming is completed.
                // ----------------------------------------------------------------------------------------------------------------warning
                cr.HasPartialPayment = 0;
                cr.paidremainderamount = 0;
                cr.creditcardfee = 0;
                cr.shippingfee = Convert.ToDecimal(0);
                cr.preAuthAmount = "0";
                cr.FTCourseRosterId = courseRoster.RosterID;
                cr.IsHoursPaid = 0;
                cr.SubSiteId = 0;
                cr.AllowModifyMultiEnroll = 0;
                cr.Parking = 0;

                cr.CheckoutComments = courseRoster.CheckoutComments;
                coursedetails.Add("CheckoutComments", courseRoster.CheckoutComments);
                cr.CheckoutComments2 = courseRoster.CheckoutComments2;
                coursedetails.Add("CheckoutComments2", courseRoster.CheckoutComments2);
                cr.SingleRosterDiscountCoupon = item.DiscountCouponPerCourse;
                coursedetails.Add("SingleRosterDiscountCoupon", item.DiscountCouponPerCourse);
                coursedetails.Add("RoommateGender", item.RoommateGender);
                coursedetails.Add("RoommateName", item.RoommateName);
                coursedetails.Add("RoommateQuestion", item.RoommateQuestion);
                try
                {
                    cr.SingleRosterDiscountAmount = float.Parse(item.DiscountAmountPerCourse);
                    coursedetails.Add("SingleRosterDiscountAmount", item.DiscountAmountPerCourse);
                }
                catch (Exception)
                {
                    cr.SingleRosterDiscountAmount = 0;
                    coursedetails.Add("SingleRosterDiscountAmount", 0);
                }
                cr.CouponDiscount = courseRoster.CouponDiscount;

                coursedetails.Add("CouponDiscount", courseRoster.CouponDiscount);
                if (item.PricingModel != null && item.PricingModel.PricingOption != null)
                {
                    cr.PricingOption = item.PricingModel.PricingOption.PriceTypedesc;
                    coursedetails.Add("PricingOption", item.PricingModel.PricingOption.PriceTypedesc);
                    aCRInitialAuditInfo = aCRInitialAuditInfo + "  " + item.PricingModel.PricingOption.PriceTypedesc; // pricing option
                    if (Settings.Instance.GetMasterInfo2().PricingHourType == 1)
                    {
                        //Credit binding with pricing option
                        if (item.PricingModel.PricingOption.inservicehour == 1)
                        {
                            cr.CourseHoursType = "ISH";
                            aCRInitialAuditInfo = aCRInitialAuditInfo + "  ISH";
                        }
                        else if (item.PricingModel.PricingOption.credithour == 1)
                        {
                            cr.CourseHoursType = "CH";
                            aCRInitialAuditInfo = aCRInitialAuditInfo + "  CH";
                        }
                        /// missing both
                        else
                        {
                            aCRInitialAuditInfo = aCRInitialAuditInfo + "  NONE"; //coursehourtype
                        }
                    }
                    else
                    {
                        //binding selected credit type - for now Default as None.
                        // will need to add the credit type to checkout process
                        if ((cr.CourseHoursType == "") || (cr.CourseHoursType == null))
                        {
                            cr.CourseHoursType = "NONE";
                        }
                        aCRInitialAuditInfo = aCRInitialAuditInfo + "  NONE  NONE";
                    }
                }
                // adding AUdit info
                aCRInitialAuditInfo = aCRInitialAuditInfo + "  0"; // parking
                aCRInitialAuditInfo = aCRInitialAuditInfo + "  0"; // pricing member
                aCRInitialAuditInfo = aCRInitialAuditInfo + "  0"; // member type

                //membership
                if (item.Course.COURSENUM == "~ZZZZZZ~")
                {
                    UpdateStudentInfo(cr.STUDENTID, "approvemembership");
                }

                using (var db = new SchoolEntities())
                {
                    var rosterList = db.Course_Rosters.Where(u => u.COURSEID == item.Course.COURSEID).ToList();
                    if (rosterList.Count > 0)
                    {
                        aCRInitialAuditInfo = aCRInitialAuditInfo + "  " + rosterList.Count.ToString(); // enroll count        
                    }
                    else
                    {
                        aCRInitialAuditInfo = aCRInitialAuditInfo + "  0"; // enroll count
                    }
                }

                aCRInitialAuditInfo = aCRInitialAuditInfo + "  0"; // waiting counter
                aCRInitialAuditInfo = aCRInitialAuditInfo + "  " + item.Course.MAXENROLL.ToString(); // max enroll
                aCRInitialAuditInfo = aCRInitialAuditInfo + "  " + item.Course.MAXWAIT.ToString(); // max wait
                //temporary not use - do not remove
                //if (courseRoster.CouponDiscount > 0)
                //{
                //    DateTime currentDateTime = DateTime.Now;
                //    aCRInitialAuditInfo = aCRInitialAuditInfo + "<br>" + currentDateTime.ToString() + " Discount Changed when checkout,Current Discount:" + courseRoster.CouponDiscount + ",Notes:";// coupon discount info
                //}

                cr.CRInitialAuditInfo = aCRInitialAuditInfo;

                coursedetails.Add("aCRInitialAuditInfo", aCRInitialAuditInfo);
                coursedetails.Add("CourseHoursType", cr.CourseHoursType);
                if (Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.GetConfig().useClubPilates == 1 && item.Course.clubready_package_id > 0)
                {
                    coursedetails.Add("clubpilatespackageinstallmentdetails", Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.BuildRosterPackageInstallmentDetails(item.Course.clubready_package_id.Value, item.PricingModel.CoursePricingOption.CoursePricingOptionId));
                    cr.clubpilatespackageinstallmentdetails = Gsmu.Api.Integration.ClubPilates.ClubPilatesHelper.BuildRosterPackageInstallmentDetails(item.Course.clubready_package_id.Value, item.PricingModel.CoursePricingOption.CoursePricingOptionId);
                }
                if (item.Course.coursetype != 1)
                {
                    using (var db = new SchoolEntities())
                    {
                        var checkduplicates = (from a in db.Course_Rosters where a.COURSEID == cr.COURSEID && a.STUDENTID == cr.STUDENTID && a.OrderNumber == cr.OrderNumber select a).FirstOrDefault();
                        if (checkduplicates == null)
                        {
                            var checkmultipleenrollment = (from roster_ in db.Course_Rosters where roster_.COURSEID == cr.COURSEID && roster_.STUDENTID == cr.STUDENTID && roster_.Cancel == 0 select roster_).FirstOrDefault();
                            if (checkmultipleenrollment == null)
                            {
                                if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                                {
                                    if (CourseShoppingCart.Instance.SubTotal == 0)
                                    {
                                        var chkout = CheckoutInfo.Instance;

                                        cr.paidremainderamount = float.Parse(chkout.RemainderAmount.ToString());
                                        //  cr.PaidInFull = 0;

                                        db.Course_Rosters.Add(cr);
                                    }
                                }
                            }
                        }

                        string CourseExtraParticipants = "";
                        if (item.HasExtraParticipants)
                        {
                            CourseExtraParticipants = Newtonsoft.Json.JsonConvert.SerializeObject(item.ExtraParticipants);
                            foreach (var cep in item.ExtraParticipants)
                            {
                                if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                                {
                                    if (CourseShoppingCart.Instance.SubTotal == 0)
                                    {
                                        db.CourseExtraParticipants.Add(cep);
                                        cr.CourseExtraParticipants.Add(cep);
                                    }
                                }
                            }
                        }
                        coursedetails.Add("MaterialList", MaterialList);
                        coursedetails.Add("CourseExtraParticipants", CourseExtraParticipants);
                        orderinprogress.coursedetails = coursedetails.ToString(Newtonsoft.Json.Formatting.None);
                        orderinprogress.userlogindetails = userdetails.ToString(Newtonsoft.Json.Formatting.None);
                        orderinprogress.OrderCurStatus = "Pending";
                        orderinprogress.orderdate = DateTime.Now;
                        if(AuthorizationHelper.CurrentStudentUser!=null)
                             orderinprogress.UserSessionid =(from stud in db.Students where stud.STUDENTID ==  AuthorizationHelper.CurrentStudentUser.STUDENTID select stud.UserSessionId).FirstOrDefault();
                        if (AuthorizationHelper.CurrentAdminUser != null)
                            orderinprogress.UserSessionid = (from stud in db.adminpasses where stud.AdminID == AuthorizationHelper.CurrentAdminUser.AdminID select stud.UserSessionId).FirstOrDefault();
                        if (AuthorizationHelper.CurrentSupervisorUser != null)
                            orderinprogress.UserSessionid = (from stud in db.Supervisors where stud.SUPERVISORID == AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID select stud.UserSessionId).FirstOrDefault();
                        if (AuthorizationHelper.CurrentInstructorUser != null)
                            orderinprogress.UserSessionid = (from stud in db.Instructors where stud.INSTRUCTORID == AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID select stud.UserSessionId).FirstOrDefault();

                        db.OrderInProgresses.Add(orderinprogress);

                        db.SaveChanges();

                        int rosterid = cr.RosterID;
                        if (rosterid > 0)
                        {
                            RoommateRequest rmrequest = new RoommateRequest();
                            rmrequest.rosterid = rosterid;
                            rmrequest.reqRMName = item.RoommateName;
                            rmrequest.reqRMEmail = item.RoommateGender;
                            rmrequest.reqRMAddress = item.RoommateQuestion;
                            rmrequest.reqRMApproved = 0;
                            rmrequest.courseid = item.Course.COURSEID;
                            rmrequest.reqdate = DateTime.Now;
                            if (!String.IsNullOrEmpty(rmrequest.reqRMName))
                            {
                                SaveRoomMateRequest(rmrequest);
                            }
                        }
                        // please keep the errors sent to the browser instead of hiding with
                        // try {} catch { empty? } 
                        if (!string.IsNullOrWhiteSpace(MaterialList))
                        {

                            foreach (string materialids in MaterialList.Split(','))
                            {
                                if (materialids.Contains('|'))
                                {
                                    if (int.Parse(materialids.Split('|')[0]) == cr.COURSEID)
                                    {
                                        if (item.LineTotal.ToString() == "0" || item.LineTotal.ToString() == "0.00" || item.LineTotal.ToString() == "0.0000" || item.LineTotal == 0)
                                        {
                                            if (CourseShoppingCart.Instance.SubTotal == 0)
                                            {
                                                SaveRosterMaterial(rosterid, GetMaterialDetails(int.Parse(materialids.Split('|')[1])), int.Parse(materialids.Split('|')[2]));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                coursedetails = new JObject();
                userdetails = new JObject();
            }

            return strOrderNumber;

        }

        public static bool CheckOrderInprogressItems(string orderno)
        {
            try
            {
                using (var db = new SchoolEntities())
                {
                    var orderlist = db.OrderInProgresses.Where(u => u.OrderNumber == orderno || u.MasterOrderNumber == orderno).ToList();
                    var rosterList = db.Course_Rosters.Where(u => u.OrderNumber == orderno || u.MasterOrderNumber == orderno).ToList();

                    return (orderlist.Count + rosterList.Count) > 0;
                }
            }

            catch {

                return false;
            }
        }
        public void OrderInprogressToRoster(CreditCardPaymentModel PaymentModel,string OrderNumber)
        {
            try
            {
                using (var db = new SchoolEntities())
                {
                    var orderlist = db.OrderInProgresses.Where(u => (u.OrderNumber == OrderNumber || u.MasterOrderNumber == OrderNumber) && u.OrderCurStatus != "Duplicate").ToList();
                    var rosterList = db.Course_Rosters.Where(u => u.OrderNumber == OrderNumber || u.MasterOrderNumber == OrderNumber).ToList();
                    var extraparticipant = "";
                    string MaterialList = "";
                    if (rosterList.Count == 0)
                    {
                        Course_Roster cr = new Course_Roster(true);
                        foreach (var order in orderlist)
                        {
                            dynamic coursedetails = JsonConvert.DeserializeObject(order.coursedetails);
                            dynamic userdetails = JsonConvert.DeserializeObject(order.userlogindetails);
                            cr.CourseSalesTaxPaid = coursedetails.CourseSalesTaxPaid;
                            cr.OrderNumber = order.OrderNumber;
                            cr.MasterOrderNumber = order.MasterOrderNumber;
                            if (cr.MasterOrderNumber != "")
                            {
                                if (AuthorizationHelper.CurrentStudentUser != null)
                                {
                                    cr.EnrollMaster = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                                }
                                else
                                {
                                    cr.EnrollMaster = cr.STUDENTID;
                                }
                            }
                            else
                            {
                                cr.EnrollMaster = cr.STUDENTID;
                            }

                            if (PaymentModel is null)
                            {
                                cr.TIMEADDED = DateTime.Parse(order.orderdate.Value.ToString("yyyy-MM-dd hh:mm"));
                            }
                            cr.eventid = coursedetails.eventid;
                            cr.COURSEID = coursedetails.cid;
                            cr.STUDENTID = userdetails.sid;
                            cr.WAITING = coursedetails.waiting;
                            cr.WaitOrder = 10000;
                            cr.CourseCost = coursedetails.coursecost;
                            cr.PaidInFull = coursedetails.paidinfull;

                            CourseShoppingCart.Instance.CurrentOrderPaymntMode = coursedetails.partialpayment;
                            cr.ATTENDED = 0;
                            cr.DIDNTATTEND = 0;
                            cr.CancelStatus = RosterCancelStatus.WaitingForPayment;
                            //cr.EnrollMaster = cr.STUDENTID;
                            cr.RosterFrom = 3; // set Ruby to 3. Admin is 2, classic student is 1.

                            cr.PricingMember = 0;

                            cr.HOURS = coursedetails.hours;

                            cr.ceucredit = coursedetails.ceucredit;
                            cr.graduatecredit = coursedetails.graduatecredit;
                            cr.CustomCreditHours = coursedetails.CustomCreditHours;
                            cr.InserviceHours = coursedetails.InserviceHours;
                            cr.Optionalcredithours1 = coursedetails.Optionalcredithours1;
                            cr.Optionalcredithours2 = coursedetails.Optionalcredithours2;
                            cr.Optionalcredithours3 = coursedetails.Optionalcredithours3;
                            cr.Optionalcredithours4 = coursedetails.Optionalcredithours4;
                            cr.Optionalcredithours5 = coursedetails.Optionalcredithours5;
                            cr.Optionalcredithours6 = coursedetails.Optionalcredithours6;
                            cr.Optionalcredithours7 = coursedetails.Optionalcredithours7;
                            cr.Optionalcredithours8 = coursedetails.Optionalcredithours8;

                            cr.BBCancelled = 0;
                            cr.CreditedInFull = 0;
                            cr.CourseSurveySent = 0;
                            cr.Expire = 0;
                            cr.AttendanceStatusId = 0;
                            cr.ExternalDBUpdated = 0;

                            cr.OptionalInfo1 = 0;
                            cr.OptionalInfo2A = 0;
                            cr.OptionalInfo2B = 0;
                            cr.OptionalInfo2C = 0;
                            cr.OptionalInfo2D = 0;
                            cr.OptionalInfo2E = 0;
                            cr.OptionalInfo2F = 0;
                            cr.OptionalInfo2G = 0;
                            cr.Rebill = 0;
                            cr.CreditedInFull = 0;
                            // cr.CustomCreditHours = 0;
                            cr.CRPartialPaymentMade = 0;
                            cr.CRPrimaryTotalPaid = 0;
                            cr.ExtraParticipant = coursedetails.ExtraParticipant;
                            //get the coupon code from the cart 
                            cr.CouponCode = coursedetails.CouponCode;
                            cr.originalstudentid = 0;
                            cr.SAPSyncCount = 0;
                            cr.Membership = 0;
                            cr.StudentChoiceCourse = coursedetails.StudentChoiceCourse;
                            cr.HasPartialPayment = 0;
                            cr.paidremainderamount = 0;
                            cr.creditcardfee = 0;
                            cr.shippingfee = Convert.ToDecimal(0);
                            cr.preAuthAmount = "0";
                            cr.FTCourseRosterId = 0;
                            cr.IsHoursPaid = 0;
                            cr.SubSiteId = 0;
                            cr.AllowModifyMultiEnroll = 0;
                            cr.Parking = 0;

                            cr.CheckoutComments = coursedetails.CheckoutComments;
                            cr.CheckoutComments2 = coursedetails.CheckoutComments2;
                            cr.SingleRosterDiscountCoupon = coursedetails.SingleRosterDiscountCoupon;
                            try
                            {
                                cr.SingleRosterDiscountAmount = coursedetails.SingleRosterDiscountAmount;
                            }
                            catch (Exception)
                            {
                                cr.SingleRosterDiscountAmount = 0;

                            }
                            cr.PricingOption = coursedetails.PricingOption;
                            cr.CouponDiscount = coursedetails.CouponDiscount;
                            if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 0)
                            {
                                cr.CouponDiscount = coursedetails.CouponDiscount;
                            }
                            else
                            {
                            }
                            if ((cr.SingleRosterDiscountAmount != 0) && (cr.SingleRosterDiscountAmount != null))
                            {
                                cr.CouponDiscount = float.Parse(cr.SingleRosterDiscountAmount.ToString());
                            }
                            cr.CRInitialAuditInfo = coursedetails.aCRInitialAuditInfo;
                            cr.CourseHoursType = coursedetails.CourseHoursType;
                            var checkduplicates = (from a in db.Course_Rosters where a.COURSEID == cr.COURSEID && a.STUDENTID == cr.STUDENTID && a.OrderNumber == cr.OrderNumber select a).FirstOrDefault();
                            if (checkduplicates == null)
                            {
                                cr.Cancel = 0;
                                db.Course_Rosters.Add(cr);
                            }


                            OrderInProgress orderinprog = db.OrderInProgresses.First(op => op.OrderInProgressId == order.OrderInProgressId);
                            orderinprog.OrderCurStatus = "Successfully Paid";
                            if (PaymentModel != null)
                            {
                                orderinprog.paymentdetails = JsonConvert.SerializeObject(PaymentModel);
                            }
                            extraparticipant = coursedetails.CourseExtraParticipants;
                            if ((!string.IsNullOrWhiteSpace(extraparticipant)))
                            {
                                var CourseExtraParticipants = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CourseExtraParticipant>>(extraparticipant);
                                foreach (var cep in CourseExtraParticipants)
                                {


                                    db.CourseExtraParticipants.Add(cep);
                                    cr.CourseExtraParticipants.Add(cep);

                                }
                            }



                            db.SaveChanges();

                            if (cr.RosterID > 0)
                            {
                                RoommateRequest rmrequest = new RoommateRequest();
                                rmrequest.rosterid = cr.RosterID;
                                rmrequest.courseid = cr.COURSEID.Value;
                                rmrequest.reqRMName = coursedetails.RoommateName;
                                rmrequest.reqRMEmail = coursedetails.RoommateGender;
                                rmrequest.reqRMAddress = coursedetails.RoommateQuestion;
                                rmrequest.reqRMApproved = 0;
                                rmrequest.reqdate = DateTime.Now;
                                if (!String.IsNullOrEmpty(rmrequest.reqRMName))
                                {
                                    SaveRoomMateRequest(rmrequest);
                                }
                            }
                            MaterialList = coursedetails.MaterialList;

                            if (!string.IsNullOrWhiteSpace(MaterialList))
                            {

                                foreach (string materialids in MaterialList.Split(','))
                                {
                                    if (materialids.Contains('|'))
                                    {
                                        if (int.Parse(materialids.Split('|')[0]) == cr.COURSEID)
                                        {
                                            SaveRosterMaterial(cr.RosterID, GetMaterialDetails(int.Parse(materialids.Split('|')[1])), int.Parse(materialids.Split('|')[2]));

                                        }
                                    }
                                }
                            }
                            cr = new Course_Roster(true);
                        }
                    }
                }
            }
            catch
            {
                using (var db = new SchoolEntities())
                {
                    try
                    {
                        int Transcriptid = int.Parse(OrderNumber);
                        var transcript = (from tran in db.Transcripts where tran.TranscriptID == Transcriptid select tran).FirstOrDefault();
                        var orderinprog = (from order in db.OrderInProgresses where order.OrderNumber == OrderNumber select order).FirstOrDefault();
                        orderinprog.OrderCurStatus = "Successfully Paid";
                        transcript.IsHoursPaid = 1;
                        db.SaveChanges();
                    }
                    catch
                    { }
                }
            }
        }
        public decimal ComputeConvenienceFee(decimal total)
        {
            if (total > 0)
            {
                if (Settings.Instance.GetMasterInfo4().GlobalConfiguration != "" && Settings.Instance.GetMasterInfo4().GlobalConfiguration != null)
                {
                    decimal percentagefee = 0;
                    System.Web.Script.Serialization.JavaScriptSerializer JSSerializeObj = new System.Web.Script.Serialization.JavaScriptSerializer();
                    dynamic globalconfiguration = JSSerializeObj.Deserialize(Settings.Instance.GetMasterInfo4().GlobalConfiguration, typeof(object));
                    try
                    {
                        percentagefee = decimal.Parse(globalconfiguration["ConnvenienceFee"]);
                    }
                    catch { }
                    if (percentagefee > 0)
                    {
                        return decimal.Round((total / 100) * percentagefee,2);
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }

        public decimal GetOriginalPriceLessConvenienceFee(decimal total)
        {
            if (total > 0)
            {
                if (Settings.Instance.GetMasterInfo4().GlobalConfiguration != "" && Settings.Instance.GetMasterInfo4().GlobalConfiguration != null)
                {
                    decimal percentagefee = 0;
                    System.Web.Script.Serialization.JavaScriptSerializer JSSerializeObj = new System.Web.Script.Serialization.JavaScriptSerializer();
                    dynamic globalconfiguration = JSSerializeObj.Deserialize(Settings.Instance.GetMasterInfo4().GlobalConfiguration, typeof(object));
                    try
                    {
                        percentagefee = decimal.Parse(globalconfiguration["ConnvenienceFee"]);
                    }
                    catch { }
                    if (percentagefee > 0)
                    {
                        return (total / (1 + (percentagefee / 100)));
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            return 0;
        }
        public void ApproveCashnetEnrollmentFromSilentPost(CreditCardPaymentModel PaymentModel, string OrderNumber)
        {
            OrderInprogressToRoster(PaymentModel, OrderNumber);
            using (var db = new SchoolEntities())
            {
                var rosterList = db.Course_Rosters.Where(u => u.OrderNumber == OrderNumber).ToList();
                if (rosterList.Count == 0)
                {
                    rosterList = db.Course_Rosters.Where(u => u.MasterOrderNumber == OrderNumber).ToList();
                }
                foreach (var rosters in rosterList)
                {
                    var Context = new SchoolEntities();
                    Course_Roster roster = Context.Course_Rosters.First(cr => cr.RosterID == rosters.RosterID);
                    roster.Cancel = 0;
                    roster.TotalPaid = decimal.Parse(PaymentModel.TotalPaid.ToString());
                    try
                    {
                        var chkout = CheckoutInfo.Instance;
                        chkout.RemainderAmount = decimal.Subtract(chkout.OrderTotal, roster.TotalPaid.Value);

                        if (chkout.PaymentMode == "partialpayment")
                        {
                            roster.PaidInFull = 0;
                        }
                        else
                        {
                            roster.PaidInFull =-1;
                        }
                    }
                    catch { }
                    roster.ChargeDate = DateTime.Now;
                    roster.paidremainderamount = 0;
                    
                    roster.RefNumber = PaymentModel.RefNumber;
                    roster.AuthNum = PaymentModel.AuthNum;
                    roster.payNumber = PaymentModel.PaymentNumber;
                    roster.PAYMETHOD = PaymentModel.PaymentType;
                    Context.SaveChanges();
                }
            }

        }
        public void ApproveEnrollment(CreditCardPaymentModel PaymentModel, string OrderNumber)
        {

            var chkout = CheckoutInfo.Instance;
            OrderInprogressToRoster(PaymentModel,OrderNumber);
            double totalDiscount=0;

            using (var db = new SchoolEntities())
            {
                var rosterList = db.Course_Rosters.Where(u => u.OrderNumber == OrderNumber).ToList();
                if (rosterList.Count == 0)
                {
                    rosterList = db.Course_Rosters.Where(u => u.MasterOrderNumber == OrderNumber).ToList();
                }
                AuditTrail audit = new AuditTrail();
                bool process_all_course_payment = false; //use this flag to check if the bundle course main, or at least one course has pricing. If there is, this will be set to true and update all roster in the order.
                foreach (var rosters in rosterList)
                {
                    try
                    {
                        var Context = new SchoolEntities();
                        Course_Roster roster = Context.Course_Rosters.First(cr => cr.RosterID == rosters.RosterID);
                        try
                        {
                            if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder != 0)
                            {
                                if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                                {
                                    totalDiscount = Context.Course_Rosters.Where(cr => cr.OrderNumber == roster.OrderNumber).Sum(cr => cr.SingleRosterDiscountAmount).Value;
                                    roster.CouponDiscount = float.Parse(totalDiscount.ToString());
                                }
                            }
                        }
                        catch { }
                        if (WebConfiguration.LogAuthorizeNetTransaction)
                        {                            
                            EnrollmentAuditingandTracking(roster, "[Step 03a]", PaymentModel.PaymentType);
                        }
                        if (chkout.PaymentCaller != "paynowuserdash")
                        {
                            roster.CancelStatus = RosterCancelStatus.Valid;
                        }
                        if (PaymentModel.PaymentType == "Credit Card")
                        {
                            if (Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo().UseCCAbbrev))
                            {
                                if (PaymentModel.CardNumber != null)
                                {
                                    roster.CardNumber = "****-****-****-" + PaymentModel.CardNumber.Substring(PaymentModel.CardNumber.Length - Math.Min(4, PaymentModel.CardNumber.Length));
                                }
                                else
                                {
                                    roster.CardNumber = PaymentModel.CardNumber;
                                }
                            }
                            else
                            {
                                roster.CardNumber = PaymentModel.CardNumber;
                            }
                            roster.CardName = PaymentModel.FirstName + " " + PaymentModel.LastName;
                            roster.CardExp = PaymentModel.ExpiryMonth + "/" + PaymentModel.ExpiryYear;
                            roster.CardAddress = PaymentModel.Address;
                        }

                        if (PaymentModel.PaymentType == "SquarePayment")
                        {
                            roster.PAYMETHOD = PaymentModel.PaymentType;
                            roster.payNumber = PaymentModel.PaymentNumber;
                            roster.AuthNum = PaymentModel.AuthNum;
                            roster.RespMsg = PaymentModel.RespMsg;
                            roster.Result = PaymentModel.RespMsg;
                            roster.enrollmentnote = PaymentModel.ReceiptUrl;
                            roster.ChargeDate = DateTime.Now;
                        }

                        if (CreditCardPaymentHelper.UsePaygov && PaymentModel.PaymentType == "CC")
                        {
                            roster.AuthNum = PaymentModel.AuthNum;
                            roster.CardName = PaymentModel.FirstName;
                            roster.payNumber = PaymentModel.PaymentNumber;
                            roster.creditcardfee = decimal.Parse(PaymentModel.CreditCardFee.ToString());
                           
                            roster.RespMsg = PaymentModel.RespMsg;
                            roster.Result = PaymentModel.Result;
                        }
                        //if ((CreditCardPaymentHelper.UseChasePayment) && (PaymentModel.PaymentType == "Credit Card" || PaymentModel.PaymentType == "CC"))
                        //{
                        //    return;
                        //}
                        //else
                        //{
                            if (roster.PAYMETHOD != "Credit Card") //this is to check touchlink process transaction.
                            {
                                roster.PAYMETHOD = PaymentModel.PaymentType;
                                roster.payNumber = PaymentModel.PaymentNumber;
                                roster.RefNumber = PaymentModel.RefNumber;
                                roster.AuthNum = PaymentModel.AuthNum;
                            }
                            roster.RespMsg = PaymentModel.RespMsg;
                            roster.Result = PaymentModel.Result;

                       // }
                        if (PaymentModel.LongOrderId != "" && PaymentModel.LongOrderId.Length < 50)
                        {
                            if ((roster.AuthNum == "") || (roster.AuthNum == null))
                            {
                                roster.AuthNum = PaymentModel.LongOrderId;
                            }

                        }

                        if (String.IsNullOrEmpty(PaymentModel.LongOrderId) == false)
                        {
                            if (PaymentModel.LongOrderId.Length < 25)
                            {
                                roster.RefNumber = PaymentModel.LongOrderId;
                            }
                        }
                        if ((roster.CardNumber != "") && (roster.CardNumber != null))
                        {
                            roster.ChargeDate = DateTime.Now;
                        }
                        if (WebConfiguration.LogAuthorizeNetTransaction)
                        {
                            if (WebConfiguration.LogAuthorizeNetTransaction)
                            {
                                EnrollmentAuditingandTracking(roster, "[Step 04a]-longorderID:", PaymentModel.LongOrderId);
                            }
                        }
                        roster.creditcardfee = decimal.Parse((PaymentModel.CreditCardFee.ToString()));



                        if (PaymentModel.ActiveCCPayMethod == "AuthorizeNetRedirect" || PaymentModel.ActiveCCPayMethod == "Nelnet")
                        {
                            roster.PAYMETHOD = PaymentModel.PaymentType;
                            roster.payNumber = PaymentModel.PaymentNumber;
                            roster.RespMsg = PaymentModel.RespMsg;
                            roster.Result = PaymentModel.Result;
                            roster.AuthNum = PaymentModel.LongOrderId;
                            roster.RefNumber = PaymentModel.LongOrderId;
                            roster.CardNumber = PaymentModel.CardNumber;
                            if (WebConfiguration.LogAuthorizeNetTransaction)
                            {
                                EnrollmentAuditingandTracking(roster, "[Step 05a]-respmsg:", PaymentModel.RespMsg);
                            }
                        }
                        if (PaymentModel.PaymentType == "PayPal" || (PaymentModel.PaymentType == "Credit Card" && CreditCardPaymentHelper.UsePayPalAdvance))
                        {
                            roster.CardExp = PaymentModel.ExpiryMonth + "/" + PaymentModel.ExpiryYear;
                            roster.CardAddress = PaymentModel.Address;
                            if (roster.PAYMETHOD != "Credit Card") //this is to check touchlink process transaction.
                            {
                                roster.PAYMETHOD = PaymentModel.PaymentType;
                                roster.AuthNum = PaymentModel.AuthNum;
                                roster.payNumber = PaymentModel.PaymentNumber;
                            }
                            if (PaymentModel.PaymentType == "PayPal")
                            {
                                //make sure that these information are saved if paypal is used on checkout
                                //these are important fields
                                //ref. ticket no. 19716
                                roster.PAYMETHOD = PaymentModel.PaymentType;
                                roster.AuthNum = PaymentModel.AuthNum;
                                roster.RefNumber = PaymentModel.RefNumber;
                                roster.payNumber = PaymentModel.PaymentNumber;
                                if (PaymentModel.TotalPaid != null)
                                {
                                    chkout.PaymentTotal = decimal.Parse(PaymentModel.TotalPaid.ToString());
                                }
                            }
                            roster.RespMsg = PaymentModel.RespMsg;
                            roster.Result = PaymentModel.Result;

                            roster.RefNumber = PaymentModel.RefNumber;
                            roster.ChargeDate = DateTime.Now;
                            roster.CardNumber = "";
                            int max_length = 49;
                            if ((PaymentModel.FirstName + " " + PaymentModel.LastName).Length > max_length)
                            {
                                roster.CardName = (PaymentModel.FirstName + " " + PaymentModel.LastName).Substring(0, max_length);
                            }
                            else
                            {
                                roster.CardName = (PaymentModel.FirstName + " " + PaymentModel.LastName);
                            }
                            if (PaymentModel.Address.Length > max_length)
                            {
                                roster.CardAddress = PaymentModel.Address.Substring(0, max_length);
                            }
                            else
                            {
                                roster.CardAddress = PaymentModel.Address;
                            }
                        }
                        if (AuthorizationHelper.CurrentStudentUser != null)
                        {
                            if (rosterList.Where(roster_value => roster_value.STUDENTID != AuthorizationHelper.CurrentStudentUser.STUDENTID).Count() <= 0)
                            {
                                if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                                {
                                    roster.OrderNumber = roster.MasterOrderNumber;
                                    roster.MasterOrderNumber = "";
                                }
                            }
                        }
                        if (WebConfiguration.LogAuthorizeNetTransaction)
                        {
                            EnrollmentAuditingandTracking(roster, "[Step 06a]-processpaymentnext:", " ");
                        }
                        if (((roster.PaidInFull != 1) && (roster.PaidInFull != -1)) || (process_all_course_payment == true) || (chkout.PaymentCaller == "paynowuserdash"))
                        {

                            ProcessPayment(roster, PaymentModel);
                            process_all_course_payment = true; // set true, to process all courses in the order.
                        }
                        else
                        {
                            if ((Settings.Instance.GetMasterInfo2().CertificationsOnOff == 1) || (Settings.Instance.GetMasterInfo2().CertificationsOnOff == 2))
                            {
                                ApplyCertificationProgram(roster);
                            }
                        }

                        if (CreditCardPaymentHelper.UseAuthorizeNet && WebConfiguration.LogAuthorizeNetTransaction)
                        {
                            audit = Context.AuditTrails.FirstOrDefault(ad => ad.RoutineName == "CreditCardPayment.cs-" + rosters.OrderNumber);
                            if (audit != null)
                            {
                                audit.DetailDescription = "Approved Payment -PaidinFull?" + roster.PaidInFull;
                            }
                        }
                        if (PaymentModel.PaymentType == "PayPal" || (PaymentModel.PaymentType == "Credit Card" && CreditCardPaymentHelper.UsePayPalAdvance))
                        {
                            audit = Context.AuditTrails.FirstOrDefault(ad => ad.RoutineName == "CreditCardPayment.cs-" + rosters.OrderNumber);
                            if (audit != null)
                            {
                                audit.DetailDescription = "Paypal Approved Payment -PaidinFull?" + roster.PaidInFull;
                                audit.StudentID = string.IsNullOrEmpty(roster.STUDENTID.ToString()) ? 0 : (int)roster.STUDENTID;
                                audit.CourseID = string.IsNullOrEmpty(roster.COURSEID.ToString()) ? 0 : (int)roster.COURSEID;
                            }
                        }

                        Context.SaveChanges();

                        if (WebConfiguration.EnrollToWaitList)
                        {
                            //only consider the roster if it's not cancelled
                            if (roster.Cancel == 0)
                            {
                                audit.DetailDescription = audit.DetailDescription + ". Roster is on Waitlist and waiting for approval from Supervisor";
                                dynamic enrollWaitlistConfig = new ExpandoObject();
                                enrollWaitlistConfig.Enabled = 1;
                                string jsonConfig = Newtonsoft.Json.JsonConvert.SerializeObject(enrollWaitlistConfig);
                                string query = @"UPDATE [Course Roster] SET WAITING = -1, EnrollToWaitListConfig = '" + jsonConfig + "' WHERE rosterid = " + roster.RosterID;
                                db.Database.ExecuteSqlCommand(query);
                            }
                        }

                        try
                        {
                            EnrollUserToBlackBoardApi(roster.COURSEID.Value, roster.STUDENTID.Value,0,"student", roster.RosterID);
                        }
                        catch { }
                    }
                    catch (DbEntityValidationException ex)
                    {
                        var errorMessages = ex.EntityValidationErrors
                       .SelectMany(x => x.ValidationErrors)
                       .Select(x => x.ErrorMessage);

                        // Join the list to a single string.
                        var fullErrorMessage = string.Join("; ", errorMessages);

                        // Combine the original exception message with the new one.
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                        // Throw a new DbEntityValidationException with the improved exception message.
                        throw new DbEntityValidationException(exceptionMessage, ex.EntityValidationErrors);
                    }
                    catch (Exception ex)
                    {
                        AuditTrail trail = new AuditTrail()
                        {
                            RoutineName = "EnrollmentFunction.cs-" + rosters.OrderNumber,
                            ShortDescription = "Error Occured",
                            DetailDescription = "Error Stack Trace : " + (string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message) + " - Message : " + ex.InnerException.Message ,
                            AuditDate = System.DateTime.Now,
                            CourseID = 0,
                            StudentID = 0
                        };
                        db.AuditTrails.Add(trail);
                        db.SaveChanges();
                    }
                    
                }
            }

        }
        public void EnrollmentAuditingandTracking(Course_Roster roster, string Description, string MoreInfo = "")
        {

            //Logging Process for Payment Processing.
            try
            {
                string LoggingDetails = "";
                if (AuthorizationHelper.CurrentUser != null)
                {
                    LoggingDetails = LoggingDetails + "User: " + AuthorizationHelper.CurrentUser.LoggedInUsername + "|" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                }
                else
                {
                    LoggingDetails = LoggingDetails + "User: NOT LOGIN";
                }
                var dnsData = System.Net.Dns.GetHostName();
                LoggingDetails = LoggingDetails + "@Host Name: " + dnsData + "@IPS: ";
                System.Net.IPHostEntry ipEntry = System.Net.Dns.GetHostEntry(dnsData);
                System.Net.IPAddress[] addr = ipEntry.AddressList;
                for (int i = 0; i < addr.Length; i++)
                {
                    LoggingDetails = LoggingDetails+" " + addr[i].ToString();
                }
                var Context = new SchoolEntities();
                AuditTrail trail = new AuditTrail()
                {

                    RoutineName = "EnrollmentFunction.cs-" + roster.OrderNumber,
                    ShortDescription = Description,
                    DetailDescription = LoggingDetails,
                    AuditDate = System.DateTime.Now,
                    CourseID = 0,
                    StudentID = 0,
                    ATErrorMsg = "MoreInfo:"+ MoreInfo
                };
                Context.AuditTrails.Add(trail);
                Context.SaveChanges();
            }

            catch { }
            //End of Process Payment Logging//
        }
        public void ProcessPayment(Course_Roster roster, CreditCardPaymentModel pay)
        {

            EnrollmentAuditingandTracking(roster, "Process Payment- Before executing");

            string DebugStr = "";
            var chkout = CheckoutInfo.Instance;
            DebugStr += ",ORGrsTotalPaid " + roster.TotalPaid + ",ORGckTotalPaid " + chkout.TotalPaid;

            chkout.PrvTotalPaid = decimal.Parse(roster.TotalPaid.ToString().Trim() == "" ? "0" : roster.TotalPaid.ToString());
            chkout.TotalPaid = decimal.Add(chkout.PaymentTotal, chkout.PrvTotalPaid);
            chkout.RemainderAmount = decimal.Subtract(chkout.OrderTotal, chkout.TotalPaid);

            string PPDelmtr = string.IsNullOrEmpty(roster.CRPartialPaymentList) ? "" : "|";
            string PartialPaymentInfo = PPDelmtr + "1@@@" + DateTime.Now.ToShortDateString()
                + "@@@1@@@" + pay.CardType + pay.PaymentType
                + "@@@" + chkout.PaymentTotal
                + "@@@Ref #" + roster.payNumber + " " + chkout.PaymentResult + " " + roster.AuthNum;


            // CHECKOUT - FULL PAYMENT
            if (chkout.PaymentCaller == "checkout" && chkout.PaymentMode == "fullpayment")
            {
                if (String.IsNullOrEmpty(roster.MasterOrderNumber))
                {
                    roster.TotalPaid = chkout.TotalPaid;
                    if (chkout.TotalPaid == 0)
                    {
                        roster.TotalPaid =decimal.Parse( pay.TotalPaid.ToString());
                    }
                }
                else
                {

                    
                    roster.TotalPaid = GetOrderTotalForSingleRoster(roster.OrderNumber);

                }
                roster.PaidInFull = -1;
                roster.Cancel = 0;
                roster.ChargeDate = DateTime.Now;
                roster.paidremainderamount = 0;
            }
            // CHECKOUT - PARTIAL PAYMENT
            if (chkout.PaymentCaller == "checkout" && chkout.PaymentMode == "partialpayment")
            {
                if (String.IsNullOrEmpty(roster.MasterOrderNumber))
                {

                    roster.TotalPaid = chkout.TotalPaid;
                }
                else
                {
                    roster.TotalPaid = chkout.TotalPaid;

                }
                roster.PaidInFull = 0;
                roster.Cancel = 0;
                roster.ChargeDate = null;
                roster.paidremainderamount = float.Parse(chkout.RemainderAmount.ToString());
                roster.CRPartialPaymentList =roster.CRPartialPaymentList+ PartialPaymentInfo;
            }

            // PAYNOW - STUDENT USERDASHBOARD
            if (chkout.PaymentCaller == "paynowuserdash")
            {
                roster.CRPartialPaymentList = roster.CRPartialPaymentList + PartialPaymentInfo;
                // IF NO BALANCE / PAYABLE
                if (chkout.RemainderAmount <= 0)
                {
                    roster.PaidInFull = -1;
                    roster.ChargeDate = DateTime.Now;
                    roster.paidremainderamount = 0;
                    
                }
                else
                {
                    roster.PaidInFull = 0;
                    //roster.CRPartialPaymentList = roster.CRPartialPaymentList + PartialPaymentInfo;
                    roster.ChargeDate = null;
                    roster.paidremainderamount = float.Parse(chkout.RemainderAmount.ToString());
                }
              //  roster.Cancel = 0;
                roster.TotalPaid = chkout.TotalPaid;
            }
            CreditCardPayments payment = new CreditCardPayments();
            Payment_Option payoption = payment.GetPaymentTypeInfo(pay.PaymentType);
            if (payoption != null)
            {
                if (payoption.autoApprovedPayment == 1)
                {
                    if (chkout.PaymentMode == "partialpayment" && chkout.PaymentCaller != "paynowuserdash")
                    {
                        roster.PaidInFull = 0;
                    }
                    else
                    {
                        roster.PaidInFull = -1;
                    }
                }

                if (payoption.allowPublicPayPending == 1 || payoption.allowPublicPayPending == -1)
                {
                    
                    if (chkout.PaymentCaller != "paynowuserdash")
                    {
                        if (chkout.PaymentMode == "partialpayment")
                        {
                            roster.PaidInFull = 0;
                            roster.TotalPaid = chkout.TotalPaid;
                        }
                        else
                        {
                            roster.PaidInFull = 0;
                            roster.TotalPaid = 0;
                        }
                    }
                    
                }
                else
                {
                    if (payoption.PaymentClass != 0 &&
                        (payoption.PAYMENTTYPE != "PayPal" && !CreditCardPaymentHelper.UsePayPalAdvance) ||
                        (payoption.PAYMENTTYPE != "AuthorizeNetRedirect" && !CreditCardPaymentHelper.UseAuthorizeNetRedirect))
                    {
                        if (payoption.autoApprovedPayment != 1)
                        {
                            roster.TotalPaid = 0;
                        }
                    }
                    if (chkout.PaymentCaller != "paynowuserdash")
                    {
                        if (payoption.autoApprovedPayment != 1)
                        {
                            roster.PaidInFull = 0;
                        }
                    }
                }
            }
            else
            {
                if (pay.PaymentType != "PayPal" && !CreditCardPaymentHelper.UsePayPalAdvance && pay.PaymentType != "Credit Card" && !CreditCardPaymentHelper.UseAuthorizeNetRedirect) 
                {
                    roster.PaidInFull = -1;
                }
                else if (chkout.PaymentCaller == "paynowuserdash" && (pay.PaymentType == "PayPal" || CreditCardPaymentHelper.UsePayPalAdvance) || (pay.PaymentType == "Credit Card" || CreditCardPaymentHelper.UseAuthorizeNetRedirect))
                {
                    roster.PaidInFull = -1;
                }
                else if ((chkout.PaymentMode == "fullpayment" && pay.PaymentType == "PayPal") || (chkout.PaymentMode == "fullpayment") && (pay.ActiveCCPayMethod == "AuthorizeNetRedirect"))
                {
                    roster.PaidInFull = -1;
                }
                else if (Settings.Instance.GetMasterInfo3().AutoApproveZeroOrder == 1 && (roster.PaidInFull == -1)) 
                {
                    roster.PaidInFull = -1;
                }
                else
                {
                    roster.PaidInFull = 0;
                }
                
            }

            //add this condition to avoid paid in full if order is not fully paid or no payment. applicable only on checkout.
            if (chkout.PaymentCaller == "checkout")
            {
                if (roster.CouponDiscount != null)
                {

                    chkout.RemainderAmount = chkout.RemainderAmount - decimal.Parse(roster.CouponDiscount.ToString());
                    roster.paidremainderamount =float.Parse( chkout.RemainderAmount.ToString());
                    if (roster.CouponDiscount > 0)
                    {
                        if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 0)
                        {
                            if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                            {
                                if (AuthorizationHelper.CurrentAdminUser != null)
                                {
                                    //this will prevent coupon discount to reset its value especially the edited discount by admin
                                }
                                else
                                {
                                    roster.CouponDiscount = float.Parse(GetMasterOrderIndividualDiscount(roster.OrderNumber).ToString());
                                }
                                
                            }
                            
                        }
                    }
                }
                if (chkout.RemainderAmount > 0)
                {
                   

                    roster.PaidInFull = 0;
                    if (pay.PaymentType == "" || pay.PaymentType == null)
                    {
                        roster.Cancel = 1;
                    }


                }
                else
                {
                    roster.paidremainderamount = 0;
                    if ((roster.CouponCode != "") && roster.CouponDiscount!=null && roster.MasterOrderNumber!="" && roster.MasterOrderNumber!=null)
                    {
                        if (roster.CouponDiscount > 0)
                        {
                            if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                            {
                                roster.CouponDiscount = float.Parse(GetMasterOrderIndividualDiscount(roster.OrderNumber).ToString());
                                roster.ChargeDate = null;
                            }
                        }
                    }
                }
            }
            if (pay.ActiveCCPayMethod == "AuthorizeNetRedirect")
            {
               if(( roster.TotalPaid ==0) ||(roster.TotalPaid==null)){
                   roster.TotalPaid =decimal.Parse( pay.TotalPaid.ToString());
               }
            }
            if (roster.TotalPaid > 0 &&  chkout.PaymentCaller != "paynowuserdash")
            {
                roster.Cancel = 0;
                roster.ChargeDate = DateTime.Now;

            }
            if ((roster.TotalPaid < 0) && (roster.CouponCode != "") && (roster.CouponCode != null))
            {
                roster.TotalPaid = 0;
            }
            //finalizes if not paynowuserdash
            if (chkout.PaymentMode == "partialpayment" && chkout.PaymentCaller != "paynowuserdash")
            {
                roster.PaidInFull = 0;
            }
            DebugStr += "OrderTotal " + chkout.OrderTotal + ",PaymentTotal " + chkout.PaymentTotal;
            DebugStr += ",rsTotalPaid " + roster.TotalPaid + ",ckTotalPaid " + chkout.TotalPaid;
            DebugStr += ",RemainderAmount " + chkout.RemainderAmount + ",PaymentMode " + chkout.PaymentMode;
            DebugStr += ",PaymentCaller " + chkout.PaymentCaller;
            //throw new Exception(DebugStr);

            if ((Settings.Instance.GetMasterInfo2().CertificationsOnOff == 1) || (Settings.Instance.GetMasterInfo2().CertificationsOnOff == 2))
            {
                ApplyCertificationProgram(roster);
            }
            if (pay.PaymentType == "Credit Card" || pay.PaymentType == "CC")
               {
                   if (CreditCardPaymentHelper.UseAuthorizeNetRedirect ||  CreditCardPaymentHelper.UseTouchnetRedirect)
                   {
                       if (roster.TotalPaid != 0)
                       {
                           if (Authorization.AuthorizationHelper.CurrentStudentUser != null)
                           {
                               if (chkout.OrderTotal == roster.TotalPaid)
                               {
                                   roster.creditcardfee = ComputeConvenienceFee((roster.TotalPaid.Value));
                                   roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                               }
                               else
                               {
                                   roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                               }
                           }
                           else if (AuthorizationHelper.CurrentSupervisorUser != null)
                           {
                               if (chkout.OrderTotal == roster.TotalPaid)
                               {
                                   roster.creditcardfee = ComputeConvenienceFee(roster.TotalPaid.Value);
                                   roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                               }
                               else
                               {
                                   roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                               }
                           }
                           else if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                           {
                               roster.creditcardfee = ComputeConvenienceFee(roster.TotalPaid.Value);
                               roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                           }
                           else
                           {
                               if (chkout.OrderTotal == roster.TotalPaid)
                               {
                                   roster.creditcardfee = ComputeConvenienceFee((roster.TotalPaid.Value));
                                   roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                               }
                               else
                               {
                                   roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                               }
                           }
                           
                       }
                       else
                       {
                           if (pay.TotalPaid > 0)
                           {
                               if (Authorization.AuthorizationHelper.CurrentStudentUser != null)
                               {
                                   roster.TotalPaid = decimal.Parse(pay.TotalPaid.ToString());
                                   if (chkout.OrderTotal == roster.TotalPaid)
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee((roster.TotalPaid.Value));
                                       roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                                   }
                                   else
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                                   }
                               }
                               else if (AuthorizationHelper.CurrentSupervisorUser != null)
                               {
                                   if (chkout.OrderTotal == roster.TotalPaid)
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee(roster.TotalPaid.Value);
                                       roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                                   }
                                   else
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                                   }
                               }
                               else if (roster.MasterOrderNumber != null && roster.MasterOrderNumber != "")
                               {
                                   roster.creditcardfee = ComputeConvenienceFee(roster.TotalPaid.Value);
                                   roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                               }
                               else
                               {
                                   roster.TotalPaid = decimal.Parse(pay.TotalPaid.ToString());
                                   if (chkout.OrderTotal == roster.TotalPaid)
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee((roster.TotalPaid.Value));
                                       roster.TotalPaid = roster.TotalPaid.Value + roster.creditcardfee;
                                   }
                                   else
                                   {
                                       roster.creditcardfee = ComputeConvenienceFee(GetOriginalPriceLessConvenienceFee(roster.TotalPaid.Value));
                                   }
                               }
                              
                           }
                       }

                        roster.PaidInFull = -1;
                       
                   }
               }
            if (chkout.PaymentMode == "partialpayment" && chkout.PaymentCaller != "paynowuserdash")
            {
                roster.PaidInFull = 0;
            }
            // CHECKOUT - FULL PAYMENT
            if ((chkout.PaymentCaller == "checkout" || chkout.PaymentCaller == "paynowuserdash") && chkout.PaymentMode == "fullpayment")
            {
                if (Settings.Instance.GetMasterInfo3().AllowPartialPayment == 0 && payoption != null)
                {
                    if (payoption.allowPublicPayPending == 1 || payoption.allowPublicPayPending == -1)
                    {
                        roster.TotalPaid = 0;
                        roster.PaidInFull = -1;
                        roster.Cancel = 0;
                        //roster.ChargeDate = DateTime.Now;
                        roster.paidremainderamount = (float)chkout.TotalPaid;
                    }
                    else
                    {
                        roster.TotalPaid = chkout.TotalPaid;
                        roster.PaidInFull = -1;
                        roster.Cancel = 0;
                        roster.ChargeDate = DateTime.Now;
                        roster.paidremainderamount = 0;
                        if (chkout.PaymentMode == "partialpayment" && chkout.PaymentCaller != "paynowuserdash")
                        {
                            roster.PaidInFull = 0;
                        }
                        else
                        {
                            roster.PaidInFull = -1;
                        }
                    }
                }
                else
                {
                    roster.TotalPaid = chkout.TotalPaid;
                    roster.Cancel = 0;
                    roster.ChargeDate = DateTime.Now;
                    roster.paidremainderamount = 0;
                    if (chkout.PaymentMode == "partialpayment" && chkout.PaymentCaller != "paynowuserdash")
                    {
                        roster.PaidInFull = 0;
                    }
                    else
                    {
                        roster.PaidInFull = -1;
                    }
                }

            }
            if (payoption != null) //Other Payment
            {
                if (payoption.autoApprovedPayment == 0)
                {
                    chkout.TotalPaid = 0;
                    roster.TotalPaid = 0;
                    roster.PaidInFull = 0;
                }
            }


            DebugStr += " >>>FINAL VALUES: ";
            DebugStr += "OrderTotal " + chkout.OrderTotal + ",PaymentTotal " + chkout.PaymentTotal;
            DebugStr += ",rsTotalPaid " + roster.TotalPaid + ",ckTotalPaid " + chkout.TotalPaid;
            DebugStr += ",RemainderAmount " + chkout.RemainderAmount + ",PaymentMode " + chkout.PaymentMode;
            DebugStr += ",PaymentCaller " + chkout.PaymentCaller + ",pay.PaymentType " + pay.PaymentType;
            if (payoption != null)
            {
                DebugStr += ",autoApprovedPayment " + payoption.autoApprovedPayment + ",PaidInFull " + roster.PaidInFull + ",TotalPaid " + roster.TotalPaid + "payoption" + payoption.PAYMENTTYPE;
            }
            else
            {
                DebugStr += ",autoApprovedPayment N/A" + ",PaidInFull " + roster.PaidInFull + ",TotalPaid " + roster.TotalPaid;
            }

            if (CourseShoppingCart.Instance.CurrentOrderPaymntMode == "true")
            {
                roster.PaidInFull = 0;
                roster.CRPartialPaymentList = roster.CRPartialPaymentList + PartialPaymentInfo;
            }
            try
            {
                if (CourseShoppingCart.Instance.PaidInfull)
                {
                    roster.PaidInFull = -1;
                    roster.TotalPaid = chkout.OrderTotal;
                }
            }
            catch { }

            if(chkout.PaymentMode== "partialpayment")
            {
                if(roster.TotalPaid == 0 && chkout.PaymentTotal>0)
                {
                    roster.TotalPaid = chkout.PaymentTotal;
                }
            }
            EnrollmentAuditingandTracking(roster, "Process Payment- After executing", DebugStr);
        }

        public void ApplyCertificationProgram(Course_Roster roster)
        {
            try
            {
                using (var db = new SchoolEntities())
                {
                    db.Configuration.LazyLoadingEnabled = false;
                    db.Configuration.ProxyCreationEnabled = false;
                    db.Configuration.AutoDetectChangesEnabled = false;

                    var course = (from c in db.Courses.AsNoTracking() where c.COURSEID == roster.COURSEID select c).FirstOrDefault();
                    if (course != null)
                    {
                        // var certid = (from certificationList in db.CertificationsCourses where certificationList.CourseNum == coursenumber select certificationList.CertificationsId).FirstOrDefault();
                        var coursenumber = "";
                        var coursedates = (from coursedate in db.Course_Times.AsNoTracking() where coursedate.COURSEID == course.COURSEID orderby coursedate.COURSEDATE ascending select coursedate).FirstOrDefault();
                        if (Settings.Instance.GetMasterInfo2().CertificationsOnOff == 1)
                        {
                            coursenumber = course.COURSENUM;
                        }
                        else
                        {
                            coursenumber = course.CustomCourseField5;
                        }
                        var certificationfromCertificateTable = (from certificationList in db.CertificationsCourses where certificationList.CourseNum == coursenumber select certificationList.CertificationsId).FirstOrDefault();
                        var certificationID = 0;
                        if ((course.CourseCertificationsId != null) && (course.CourseCertificationsId != 0))
                        {
                            certificationID = course.CourseCertificationsId.Value;
                        }
                        if ((certificationID == 0) && (certificationfromCertificateTable != null))
                        {
                            certificationID = certificationfromCertificateTable.Value;
                        }

                        if ((certificationID != null) && (certificationID != 0))
                        {
                            var YearsToExpire = (from certp in db.Certifications where certp.CertificationsId == certificationID select certp).FirstOrDefault();
                            int CurrentYearsToExpire = (int)YearsToExpire.CertificationsYearsToExpire;
                            var certstudentinList = (from certst in db.CertificationsStudents where certst.StudentId == roster.STUDENTID && certst.CertificationsId == certificationID select certst.StudentId).FirstOrDefault();
                            int expiresDays = 0;
                            if (certstudentinList == null)
                            {
                                CertificationsStudent cStudent = new CertificationsStudent();
                                cStudent.CertificationsId = certificationID;
                                cStudent.StudentId = roster.STUDENTID;
                                if (String.IsNullOrEmpty(CurrentYearsToExpire.ToString()))
                                {
                                    //individual certification set to 10000, meaning to use the global value
                                    CurrentYearsToExpire = 10000;
                                }
                                if (CurrentYearsToExpire == 10000)
                                {
                                    expiresDays = int.Parse(Settings.Instance.GetMasterInfo2().CertificationsYearsToExpire.ToString());
                                }
                                else
                                {
                                    expiresDays = CurrentYearsToExpire;
                                }

                                if (expiresDays < 0)
                                {
                                    if (coursedates != null)
                                    {
                                        cStudent.ExpireDate = coursedates.COURSEDATE.Value.AddDays(expiresDays * -1);
                                    }
                                    else
                                    {
                                        cStudent.ExpireDate = DateTime.Now.AddDays(expiresDays * -1);
                                    }
                                }
                                else if (expiresDays > 0)
                                {
                                    if (coursedates != null)
                                    {
                                        cStudent.ExpireDate = coursedates.COURSEDATE.Value.AddDays(expiresDays * 365);
                                    }
                                    else
                                    {
                                        cStudent.ExpireDate = DateTime.Now.AddDays(expiresDays * 365);
                                    }

                                }
                                else if (expiresDays == 0)
                                {
                                    DateTime lastDayofYear = new DateTime(DateTime.Now.Year, 12, 31);
                                    cStudent.ExpireDate = lastDayofYear;
                                }
                                if (coursedates != null)
                                {
                                    cStudent.QualificationStartDate = coursedates.COURSEDATE.Value;
                                }
                                else
                                {
                                    cStudent.QualificationStartDate = DateTime.Today;
                                }

                                cStudent.AutoHistoryOff = 0;
                                cStudent.LastReminderSent = DateTime.Parse("1990-01-01 00:00:00.000");
                                db.CertificationsStudents.Add(cStudent);
                            }
                            db.SaveChanges();
                        }

                    }
                }
            }
             catch(Exception ex){
                 using (var db = new SchoolEntities())
                 {
                     AuditTrail trail = new AuditTrail()
                     {
                         RoutineName = "EnrollmentFunction.cs-" + "Certification",
                         ShortDescription = "Error Occured",
                         DetailDescription = "Error Stack Trace : " + (string.IsNullOrEmpty(ex.InnerException.Message) ? ex.InnerException.Message : ex.Message) + " - Message : " + ex.InnerException.Message,
                         AuditDate = System.DateTime.Now,
                         CourseID = 0,
                         StudentID = 0
                     };
                     db.AuditTrails.Add(trail);
                     db.SaveChanges();
                 }
             }
                    

        }
        public decimal GetOrderTotalForSingleRoster(string OrderNumber)
        {
            List<Course_Roster> CourseRosters = new List<Course_Roster>();
            if (!string.IsNullOrWhiteSpace(OrderNumber))
            {
                var listcount = 0;
                float discount = 0;
                using (var db = new SchoolEntities())
                {
                    db.Configuration.LazyLoadingEnabled = false;
                    db.Configuration.ProxyCreationEnabled = false;
                    CourseRosters = (from cr in db.Course_Rosters where cr.OrderNumber == OrderNumber select cr).ToList();
                    //Check for Master Order.
                    if (CourseRosters == null)
                    {
                        CourseRosters = (from cr in db.Course_Rosters where cr.MasterOrderNumber == OrderNumber select cr).ToList();
                    }
                    var list = new List<Gsmu.Api.Data.School.Entities.Course_Roster>();
                    foreach (var roster in CourseRosters)
                    {
                        roster.Cours = (from c in db.Courses where c.COURSEID == roster.COURSEID select c).FirstOrDefault();
                        roster.Student = (from s in db.Students where s.STUDENTID == roster.STUDENTID select s).FirstOrDefault();

                        var materials = (from m in db.rostermaterials where m.RosterID == roster.RosterID select m).ToList();
                        roster.rostermaterials = new System.Collections.ObjectModel.Collection<rostermaterial>(materials);
                        roster.CourseExtraParticipants = (from m in db.CourseExtraParticipants where m.RosterId == roster.RosterID select m).ToList();
                        list.Add(roster);

                        if (Settings.Instance.GetMasterInfo4().AllowCouponPerOrder == 0)
                        {
                            discount = roster.CouponDiscount.Value;
                        }
                        else
                        {
                            if (roster.SingleRosterDiscountAmount != null)
                            {
                                discount =discount+ float.Parse( roster.SingleRosterDiscountAmount.Value.ToString());
                            }
                        }

                    }

                    CourseRosters = list;

                }

                return (CourseRosters.Sum(cr => cr.MaterialTotalCost + cr.CourseCostDecimal) - decimal.Parse(discount.ToString()));
            }
            else
            {
                return 0;
            }
        }
        public decimal GetMasterOrderIndividualDiscount(string ordernumber)
        {
            decimal discounttotal = 0;
            try
            {

                using (var db = new SchoolEntities())
                {
                    db.Configuration.LazyLoadingEnabled = false;
                    db.Configuration.ProxyCreationEnabled = false;
                    var CourseRosters = (from cr in db.Course_Rosters where cr.OrderNumber == ordernumber select cr).ToList();
                    var totalamount = CourseRosters.Sum(cr => cr.MaterialTotalCost + cr.CourseCostDecimal);
                    Coupon couponDetails = GetCouponDiscountDetails(CourseRosters.First().CouponCode);

                    var coupoun_dollar = couponDetails.CouponDollarAmount;
                    var coupon_percent = couponDetails.CouponPercentAmount;
                    if (coupoun_dollar > 0)
                    {
                        discounttotal = decimal.Parse(coupoun_dollar.ToString());
                    }
                    if (coupon_percent > 0)
                    {
                        discounttotal = totalamount * (decimal.Parse(coupon_percent.ToString()) / 100);
                    }
                }
            }
            catch
            {
            }
            return discounttotal;
        }
        public int ApproveTranscriptPayment(CreditCardPaymentModel PaymentModel, string TranscriptID)
        {
            int courseid = 0;
            int intTranscriptID = int.Parse(TranscriptID);
            string PaidInfo = "{\"Paydate\": \"" + DateTime.Now + " \",  \"AuthNum\": \"" + PaymentModel.AuthNum + "\",  \"TransactionID\": \"" + TranscriptID + "\",  \"Amount\": \"" + PaymentModel.TotalPaid + "\" }";

            var Context = new SchoolEntities();
            Transcript transcirpt = Context.Transcripts.First(tr => tr.TranscriptID == intTranscriptID);
            transcirpt.IsHoursPaid = 1;
            transcirpt.IsHoursPaidInfo = PaidInfo;
            courseid = transcirpt.CourseId.Value;
            Course_Roster roster = Context.Course_Rosters.First(cr => cr.STUDENTID == transcirpt.STUDENTID && cr.COURSEID == transcirpt.CourseId);
            roster.IsHoursPaid = 1;

            var orderinpProgress = (from orderinProgress in Context.OrderInProgresses where orderinProgress.OrderNumber == TranscriptID select orderinProgress).FirstOrDefault();
            if (orderinpProgress != null)
            {
                orderinpProgress.OrderCurStatus = "Successfully Paid";
            }
            Context.SaveChanges();

            Entities.Student student = Context.Students.First(s => s.STUDENTID == transcirpt.STUDENTID);

            string defaultInfo = student.EMAIL + " - " + student.FIRST + " - " + student.LAST + " - " + PaymentModel.TotalPaid + " - Course Name: " + transcirpt.CourseId;

            AuditTrail audit = new AuditTrail();
            audit.TableName = "course roster";
            audit.UserName = TranscriptID;
            audit.StudentID = int.Parse(transcirpt.STUDENTID.ToString());
            audit.CourseID = int.Parse(transcirpt.CourseId.ToString());
            audit.AuditDate = DateTime.Now;
            audit.AuditAction = defaultInfo;
            audit.RoutineName = PaymentModel.ActiveCCPayMethod;
            audit.ShortDescription = "Response Code: " + PaymentModel.Result;
            using (var db = new SchoolEntities())
            {
                db.AuditTrails.Add(audit);
                db.SaveChanges();
            }

            return courseid;
        }

        public void UpdateStudentInfo(int? studentid, string cmd)
        {

            var Context = new SchoolEntities();
            Entities.Student student = Context.Students.First(s => s.STUDENTID == studentid);
            if (cmd == "approvemembership")
            {
                student.DISTEMPLOYEE = -1;
            }
            Context.SaveChanges();
        }

        public void LogAuthorizeNetTransaction(CreditCardPaymentModel paymentmodel, string Amount, string request_string, string response_string, string routine_name)
        {
            AuditTrail audit = new AuditTrail();
            audit.TableName = "Not Applicable";
            if (AuthorizationHelper.CurrentStudentUser != null)
            {
                audit.StudentID = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                audit.UserName = AuthorizationHelper.CurrentStudentUser.USERNAME;
            }

            else
            {
                audit.UserName = paymentmodel.OrderNumber;
            }
            audit.AuditDate = DateTime.Now;
            if (CreditCardPaymentHelper.UseAuthorizeNetRedirect)
            {
                audit.AuditAction = "Card Payment";
            }
            else
            {
                audit.AuditAction = request_string;
            }
            audit.RoutineName = routine_name + paymentmodel.OrderNumber;
            if (CreditCardPaymentHelper.UseAuthorizeNetRedirect)
            {
                audit.ShortDescription = "response code: 1";
                audit.DetailDescription = "approved";
            }
            else
            {
                if (response_string.Count() < 254)
                {
                    audit.ShortDescription = response_string;
                }
                else
                {
                    audit.ShortDescription = response_string.Substring(0, 250);
                }
                audit.DetailDescription = "not yet available";
            }
            audit.DetailDescription = audit.DetailDescription + "@";
            using (var db = new SchoolEntities())
            {
                db.AuditTrails.Add(audit);
                db.SaveChanges();

            }
        }
        public void CancelEnrollment(string OrderNumber)
        {
            using (var db = new SchoolEntities())
            {
                db.Course_Rosters.Where(x => x.OrderNumber == OrderNumber).ToList().ForEach(f => CancelEnrollment(f.RosterID));
                var Context = new SchoolEntities();
                AuditTrail audit = new AuditTrail();
                if (CreditCardPaymentHelper.UseAuthorizeNet && WebConfiguration.LogAuthorizeNetTransaction)
                {
                    audit = Context.AuditTrails.FirstOrDefault(ad => ad.RoutineName == "CreditCardPayment.cs-" + OrderNumber);
                    audit.DetailDescription = "Cancelled Payment";

                }

                db.SaveChanges();
            }
        }
        public void CancelEnrollment(int RosterId)
        {
            using (var db = new SchoolEntities())
            {
                var roster = (from r in db.Course_Rosters where r.RosterID == RosterId select r).FirstOrDefault();

                if (roster != null)
                {

                    var waitlistRoster = (from wr in db.Course_Rosters where wr.IsWaiting == true && wr.COURSEID == roster.COURSEID select wr).FirstOrDefault();
                    if (waitlistRoster != null)
                    {
                        waitlistRoster.WAITING = 0;
                    }
                    var student = roster.StudentOld;
                    var course = roster.Course;

					if(Settings.Instance.GetMasterInfo3().bb_allow_normalize == 1){
						RemoveStudentToBlackBoardApi(course.COURSEID, student.STUDENTID);
					}

                    if (haiku.Configuration.Instance.EnableRosterCancellationSynchronization &&
                        student != null &&
                        course != null &&
                        student.haiku_user_id > 0 &&
                        course.haiku_course_id.HasValue &&
                        course.haiku_course_id > 0
                        )
                    {

                        haiku.HaikuExport.DeleteRoster(course.haiku_course_id.Value, student.haiku_user_id);
                        roster.haiku_roster_id = null;
                    }

                    if (canvas.Configuration.Instance.ExportEnrollmentCancellation && student != null && course != null && roster.canvas_roster_id.HasValue && roster.canvas_roster_id > 0 && student.canvas_user_id > 0 && course.canvas_course_id.HasValue && course.canvas_course_id.Value > 0)
                    {
                        
                        canvas.CanvasExport.DeleteEnrollment(course.canvas_course_id.Value, int.Parse(roster.canvas_roster_id.Value.ToString()));
                        roster.canvas_roster_id = null;
                        //cancel section if here is any
                        //need to cancel enrollment first before removing the Section
                        //exportEnrollmentCancellation
                        if (canvas.Configuration.Instance.allowCourseSectionPerRegistration && !string.IsNullOrEmpty(roster.OptionalCollectedInfo1))
                        {
                            string[] tempCCInfo = roster.OptionalCollectedInfo1.Split('|'); ;
                            //canvas.CanvasExport.DeleteEnrollment(course.canvas_course_id.Value, roster.canvas_roster_id.Value);
                            //need to delete special course section here
                            if (tempCCInfo[0] != "")
                            {
                                canvas.Clients.CourseClient.DeleteCourseSection(tempCCInfo[0]);
                            }
                            roster.OptionalCollectedInfo1 = null;
                        }
                //if (!string.IsNullOrEmpty(gsmuCourse.CourseConfiguration) || currentRoster.OptionalCollectedInfo1 == "")
                //{
                    }

                    roster.CancelStatus = RosterCancelStatus.InvalidOrCancelled;
                    roster.CancelDate = DateTime.Now;
                    if (AuthorizationHelper.CurrentUser != null)
                    {
                        roster.internalnote = roster.internalnote + " Cancel: from Rbuy User: " + AuthorizationHelper.CurrentUser.LoggedInUsername;
                    }
                    Random randNumber = new Random();
                    roster.CancelNumber = "EFW" + DateTime.Today.Month.ToString() + DateTime.Today.Date.ToString() + DateTime.Today.Second.ToString() + randNumber.Next(100, 1000).ToString();

                    try
                    {
                        AuditTrail trail = new AuditTrail()
                        {
                            RoutineName = "EnrollmentFunction.cs-" + roster.OrderNumber,
                            ShortDescription = "Cancelled from Ruby User: " + AuthorizationHelper.CurrentUser.LoggedInUsername,
                            AuditAction = "Individual Cancellation",
                            DetailDescription = "",
                            AuditDate = System.DateTime.Now,
                            CourseID = roster.COURSEID.Value,
                            StudentID = roster.STUDENTID.Value
                        };
                        db.AuditTrails.Add(trail);
                    }
                    catch { }
                    db.SaveChanges();

                }
            }

        }


		public void RemoveStudentToBlackBoardApi(int courseid, int studentid)
		{
			using (var db = new SchoolEntities())
			{
				var studentuuid = (from s in db.Students where s.STUDENTID == studentid select s).FirstOrDefault();
				var courseuuid = (from s in db.Courses where s.COURSEID == courseid select s).FirstOrDefault();

				if (studentuuid != null && courseuuid != null)
				{
					if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI && Settings.Instance.GetMasterInfo3().bb_allow_normalize == 1)
					{
						BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                        var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                        var _skey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
						var _akey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
						var _curl = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
						handelr.DeleteUserBBEnrollment(_skey, _akey, "", _curl, studentuuid.Blackboard_user_UUID, courseuuid.blackboard_api_uuid, "uuid", "uuid", "", jsonToken);
					}
				}
			}
		}

		public void STMoveToEnroll(int RosterId, bool sendEmail = true)
        {
            using (var db = new SchoolEntities())
            {
                var roster = (from r in db.Course_Rosters where r.RosterID == RosterId select r).FirstOrDefault();
                if (roster != null)
                {
                    var student = roster.StudentOld;

                    if (WebConfiguration.EnrollToWaitList)
                    {
                        dynamic enrollWaitlistConfig = new ExpandoObject();
                        enrollWaitlistConfig.Enabled = 1;
                        enrollWaitlistConfig.Enrolled = 1;
                        enrollWaitlistConfig.EnrolledDate = DateTime.Now.ToShortDateString();
                        string jsonConfig = Newtonsoft.Json.JsonConvert.SerializeObject(enrollWaitlistConfig);
                        string query = @"UPDATE [Course Roster] SET EnrollToWaitListConfig = '" + jsonConfig + "' WHERE rosterid = " + roster.RosterID;
                        db.Database.ExecuteSqlCommand(query);
                    }

                    roster.CRInitialAuditInfo = roster.CRInitialAuditInfo + " WaitListEnrolledDate:" + System.DateTime.Now.ToString("MM/dd/yyyy");

                    if (roster.WAITING != 0)
                    {
                        roster.WAITING = 0;

                        db.SaveChanges();

                        //send email part
                        if(sendEmail)
                        { 
                            EmailFunction email = new EmailFunction();
                            email.SendWaitListToEnrollEMail(roster);
                        }

                        var roster_count = (from r in db.Course_Rosters where r.COURSEID == roster.COURSEID && r.WAITING == 0 && r.Cancel == 0 select r).Count();
                        var course_selected = (from c in db.Courses where c.COURSEID == roster.COURSEID select c).FirstOrDefault();
                        if (roster_count > course_selected.MAXENROLL)
                        {
                            course_selected.MAXENROLL = roster_count;
                            db.SaveChanges();
                        }
                        
                    }
                    else //
                    {
                        roster.WAITING = 1;
                        roster.WaitOrder = 1000;
                        db.SaveChanges();
                    }
                }
            }

        }

        public string CheckGenerateOrderID(int inStudentID)
        {
            string strDatetoday = DateTime.Now.ToShortDateString();
            DateTime Datetoday = DateTime.Parse(strDatetoday);
            string strTimetoday = DateTime.Now.ToShortTimeString();


            string StrCourseIds = "";
            int xinc = 0;
            foreach (var item in CourseShoppingCart.Instance.Items)
            {
                if (++xinc == 1)
                {
                    StrCourseIds = item.Course.COURSEID.ToString();
                }
                else
                {
                    StrCourseIds = StrCourseIds + "," + item.Course.COURSEID.ToString();
                }
            }

            string orderid = "C" + CreateRandomKey();
            int loopSearch = 0;
            int maxloopSearch = 100;
            if (IsOrderIdExist(orderid))
            {
                do
                {
                    orderid = "C" + CreateRandomKey();
                    loopSearch++;
                } while (IsOrderIdExist(orderid) && loopSearch < maxloopSearch);

            }
            return orderid;
        }


        public static string CreateRandomKey()
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

            ////suffle again the Combined CreateRandomKey
            //length = m_Rand.Next(14, 14 + 1);
            //StringBuilder RandomKeyshuffle = new StringBuilder(length);
            //for (int i = 0; i < length; i++)
            //{
            //    RandomKeyshuffle.Append(RandomKeyStr[m_Rand.Next(0, RandomKeyStr.Length)]);
            //}

            //RandomKeyStr = RandomKeyshuffle.ToString();

            return RandomKeyStr;
        }


        public bool IsOrderIdExist(string OrderNumber)
        {
            using (var db = new SchoolEntities())
            {
                var exits = (from op in db.OrderInProgresses where op.OrderNumber == OrderNumber select op).Count() > 0;
                return exits;
            }
        }

        public int CheckRequiredPaynumberData(string strPaytype)
        {
            int result = 0;
            try
            {
                using (var db = new SchoolEntities())
                {
                    var query = (from m in db.Payment_Options where m.PAYMENTTYPE == strPaytype select m.paymentNumberNotRequired).First();
                    result = int.Parse(query.ToString());
                }
            }
            catch
            {
            }

            return result;
        }

        public string GetMaterialListFromCourse(int intCourseId)
        {

            using (var db = new SchoolEntities())
            {
                try
                {
                    string result = (from c in db.Courses where c.COURSEID == intCourseId select c.MATERIALS).First();
                    return result;
                }
                catch
                {
                    return "";
                }
            }
        }



        public IEnumerable<Material> GetAllMaterialsList()
        {
            using (var db = new SchoolEntities())
            {
                IEnumerable<Material> item = (from m in db.Materials orderby m.product_num select m).ToList();
                return item;
            }
        }

        public IEnumerable<Material> GetAllMaterialsById(string strMaterialId)
        {
            strMaterialId = strMaterialId.Replace("~", "");
            var listOfMaterials = strMaterialId.Split(',');
            int intArraySize = listOfMaterials.Length;
            var intArrProductId = new int[intArraySize];
            int index = 0;
            foreach (string singleMaterial in listOfMaterials)
            {
                try
                {
                    intArrProductId.SetValue(int.Parse(singleMaterial), index);
                    index = index + 1;
                }
                catch
                {
                }
            }
            using (var db = new SchoolEntities())
            {
                CourseModel courseModel = new CourseModel();
                var raw_mat_data = db.Materials.Where(x => intArrProductId.Contains(x.productID)).ToList();

                for (int x = 0; x < raw_mat_data.Count(); x++)
                {
                    if (raw_mat_data[x].quantity > 0)
                    {
                        raw_mat_data[x].quantity = (raw_mat_data[x].quantity - courseModel.getMaterialQtyPurchased(raw_mat_data[x].productID));
                    }
                }
                IEnumerable<Material> item = raw_mat_data;
                return item;
            }
        }
        public void SaveRoomMateRequest(RoommateRequest RMRequest)
        {
            try
            {
                using (var db = new SchoolEntities())
                {
                    var request = (from rmrequest in db.RoommateRequests where rmrequest.courseid == RMRequest.courseid && rmrequest.rosterid == RMRequest.rosterid select rmrequest).FirstOrDefault();
                    if (request == null)
                    {
                        db.RoommateRequests.Add(RMRequest);
                        db.SaveChanges();
                    }
                }
            }
            catch { }
        }
        public Material GetMaterialDetails(int intMaterialId)
        {
            Material Material = new Material();
            using (var db = new SchoolEntities())
            {
                Material = db.Materials.Where(x => x.productID == intMaterialId).First();
            }

            return Material;
        }

        public void SaveRosterMaterial(int intRosterId, Material material, int qty)
        {
            rostermaterial rostermaterial = new rostermaterial();
            rostermaterial.productID = material.productID;
            rostermaterial.price = material.price * qty;
            rostermaterial.RosterID = intRosterId;
            rostermaterial.product_name = material.product_name;
            rostermaterial.priceincluded = material.priceincluded;
            rostermaterial.shipping_cost = material.shipping_cost;
            rostermaterial.taxable = material.taxable;
            rostermaterial.qty_purchased = qty;
            // rostermaterial.salestax = material.taxable
            try
            {
                using (var db = new SchoolEntities())
                {
                    // SAVE MATERIALS QTY TO ROSTER MATERIALS
                    var materialduplicatecheck = (from _rostermaterial in db.rostermaterials where _rostermaterial.productID == rostermaterial.productID && _rostermaterial.RosterID == rostermaterial.RosterID select _rostermaterial).FirstOrDefault();
                    if (materialduplicatecheck == null)
                    {
                        db.rostermaterials.Add(rostermaterial);
                        db.SaveChanges();
                    }
                    try
                    {
                        int id = rostermaterial.RosterMaterialsID;
                        if (id != 0)
                        {
                            string query = "UPDATE rostermaterials SET qty_purchased =" + qty + " WHERE RosterMaterialsID = " + id + ";";
                            using (var connection = Connections.GetSchoolConnection())
                            {
                                connection.Open();
                                var cmd = connection.CreateCommand();
                                cmd.Connection = connection;
                                cmd.CommandText = query;
                                cmd.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public string GetCheckOutQuestions()
        {
            if (String.IsNullOrEmpty(Settings.Instance.GetMasterInfo2().CheckoutQuestionAnswer))
            {
                return "";
            }
            else
            {
                return Settings.Instance.GetMasterInfo2().CheckoutQuestionAnswer;
            }

        }

        public Dictionary<string, string> GetCheckoutCommentandQuestionSetUp()
        {
            Dictionary<string, string> setUp = new Dictionary<string, string>();
            setUp.Add("ischeckoutcommentenabled", Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo2().ShowCheckoutComments).ToString());
            setUp.Add("checkoutcommentlabel", Settings.Instance.GetMasterInfo2().CheckoutCommentsLabel);
            setUp.Add("emailtoadmin", Settings.Instance.GetMasterInfo2().EmailCOComment.ToString());
            setUp.Add("checkoutquestionenabled", Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo2().CheckoutQuestion).ToString());

            return setUp;
        }

        public CourseModel GetPrerequisitesSetup(int intCourseId)
        {
            CourseModel CourseModel = new CourseModel(intCourseId);
            return CourseModel;
        }

        public Dictionary<string, string> GetCheckOutMessageSetUp()
        {
            Dictionary<string, string> setUp = new Dictionary<string, string>();
            try
            {
                int HideCheckoutApproval = 0;
                if (Settings.Instance.GetMasterInfo2().HideCheckoutApproval == -1)
                {
                    HideCheckoutApproval = 0;
                }
                else
                {
                    HideCheckoutApproval = 1;
                }
                setUp.Add("hidecheckoutapproval", Settings.GetVbScriptBoolValue(HideCheckoutApproval).ToString());
                if (Settings.Instance.GetMasterInfo3().ShowAlternateCheckoutMessage != null)
                {
                    setUp.Add("showslternatecheckoutmessage", Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo3().ShowAlternateCheckoutMessage).ToString());
                }
                else
                {
                    setUp.Add("showslternatecheckoutmessage", Settings.GetVbScriptBoolValue(0).ToString());

                }
                if (Settings.Instance.GetMasterInfo3().AlternateCheckoutMessage != null)
                {
                    setUp.Add("alternatecheckoutmessage", Settings.Instance.GetMasterInfo3().AlternateCheckoutMessage.ToString());
                }
                else
                {
                    setUp.Add("alternatecheckoutmessage", "");
                }
                setUp.Add("defaulcheckoutmessage", "");
            }
            catch
            {
                setUp.Add("hidecheckoutapproval", Settings.GetVbScriptBoolValue(-1).ToString());
                setUp.Add("showalternatecheckoutmessage", Settings.GetVbScriptBoolValue(0).ToString());
                setUp.Add("alternatecheckoutmessage", "");
                setUp.Add("defaulcheckoutmessage", "");
            }
            return setUp;
        }
        public IEnumerable<Course_Time> GetCourse_TimeList(int intCourseId)
        {
            using (var db = new SchoolEntities())
            {
                IEnumerable<Course_Time> item = db.Course_Times.Where(x => x.COURSEID == intCourseId).OrderBy(x => x.COURSEDATE).ToList();
                return item;
            }
        }

        public String getCartCIDList()
        {
            String lstCartCIDList = "";
            int cnti = 0;
            if (CourseShoppingCart.Instance.MultipleStudentCourses.Count == 0)
            {
                foreach (var item in CourseShoppingCart.Instance.Items)
                {
                    if (cnti == 0)
                    {
                        lstCartCIDList = item.Course.COURSEID.ToString();
                    }
                    else
                    {
                        lstCartCIDList = lstCartCIDList + "," + item.Course.COURSEID.ToString();
                    }
                    cnti += 1;
                }
            }
            else
            {
                foreach (var item in CourseShoppingCart.Instance.MultipleStudentCourses)
                {
                    if (cnti == 0)
                    {
                        lstCartCIDList = item.CourseId.ToString();
                    }
                    else
                    {
                        lstCartCIDList = lstCartCIDList + "," + item.CourseId.ToString();
                    }
                    cnti += 1;
                }
            }
            return lstCartCIDList;
        }


        public Dictionary<string, string> GetCCVandRequiredFieldSetUp()
        {
            Dictionary<string, string> CCVsetup = new Dictionary<string, string>();
            CCVsetup.Add("ccvon", Settings.Instance.GetMasterInfo2().CCVOn.ToString());
            CCVsetup.Add("requiredsettings", Settings.Instance.GetMasterInfo().RequestAdditionalCCChargingInfo.ToString());
            return CCVsetup;
        }

        public Dictionary<string, string> GetConfirmationDetail(string OrderNumber)
        {
            Dictionary<string, string> confirmationdetails = new Dictionary<string, string>();
            OrderModel CourseRosters = new OrderModel(OrderNumber);
            int intstudentid = 0;
            int waitlist = 0;
            string paymenttype = "";
            string paynumber = "";
            string paynotes = "";
            foreach (Course_Roster roster in CourseRosters.CourseRosters)
            {
                intstudentid = int.Parse(roster.STUDENTID.ToString());
                paymenttype = roster.PAYMETHOD;
                paynumber = roster.payNumber;
                paynotes = roster.PaymentNotes;
                waitlist = roster.WAITING;

            }

            //general information
            if (waitlist == 0)
            {
                confirmationdetails.Add("confirmationtexttop", Settings.Instance.GetMasterInfo().EnrollConfirm);
            }
            else
            {
                confirmationdetails.Add("confirmationtexttop", Settings.Instance.GetMasterInfo2().AltCourseConfirmation);
            }
            confirmationdetails.Add("confirmationtextbottom", Settings.Instance.GetMasterInfo().CancelInfo);
            confirmationdetails.Add("registrationnumber", OrderNumber);


            //Student Info
            var studentinfo = Gsmu.Api.Data.School.Entities.Student.GetStudent(intstudentid);
            confirmationdetails.Add("studentname", studentinfo.FIRST + " " + studentinfo.LAST);
            confirmationdetails.Add("homephone", studentinfo.HOMEPHONE);
            confirmationdetails.Add("workphone", studentinfo.WORKPHONE);
            confirmationdetails.Add("studentemail", studentinfo.EMAIL);
            confirmationdetails.Add("studentusername", studentinfo.USERNAME);

            //Payment Information
            confirmationdetails.Add("paymethod", paymenttype);
            confirmationdetails.Add("paynumber", paynumber);
            confirmationdetails.Add("paynotes", paynotes);
            return confirmationdetails;
        }

        public int GetRosterIdFromCourseIdandOrderNumber(int intCourseId, string OrderNumber)
        {
            OrderModel CourseRosters = new OrderModel(OrderNumber);
            int intRosterId = 0;
            foreach (Course_Roster roster in CourseRosters.CourseRosters)
            {
                if (intCourseId == roster.COURSEID)
                {
                    intRosterId = roster.RosterID;
                }
            }

            return intRosterId;
        }

        public IEnumerable<Material> GetConfirmedMaterials(int intRosterId)
        {
            string materialids = "";

            using (var db = new SchoolEntities())
            {

                var materialroster = (from c in db.rostermaterials where c.RosterID == intRosterId select c.productID).ToList();

                foreach (int a in materialroster)
                {
                    materialids = materialids + "," + a.ToString();
                }
            }

            return GetAllMaterialsById(materialids);
        }

        public string GetOtherPaymentLabel()
        {
            return Settings.Instance.GetMasterInfo2().paytypelabel;
        }
        public bool ShowOtherPayments()
        {
            return Settings.GetVbScriptBoolValue(Settings.Instance.GetMasterInfo().ShowOtherPayment);
        }

        public Coupon GetCouponDiscountDetails(string strCouponCode)
        {

            Coupon coupon = new Coupon();
            try
            {
                using (var db = new SchoolEntities())
                {
                    coupon = (from c in db.Coupons where c.CouponCode == strCouponCode select c).First();
                }
            }
            catch (Exception)
            {
                coupon.CouponCode = "notexist";
            }
            return coupon;
        }

        public Coupon GetCouponDiscountDetailsByCourse(int courseid)
        {
            string strcourseid = courseid.ToString();

            Coupon coupon = new Coupon();
            try
            {
                using (var db = new SchoolEntities())
                {
                    coupon = (from c in db.Coupons where (c.CouponCourseId == courseid || c.additionalcourseid.Contains(strcourseid)) && c.CouponExpireDate >= DateTime.Now && c.CouponStartDate <= DateTime.Now select c).First();
                }
            }
            catch (Exception)
            {
                coupon.CouponCode = "";
            }
            return coupon;
        }

        public List<Coupon> GetAllAutomaticCouponDiscounts()
        {
            List<Coupon> coupons = new List<Coupon>();
            using (var db = new SchoolEntities())
            {
                coupons = (from c in db.Coupons where c.CouponExpireDate >= DateTime.Now && c.CouponStartDate <= DateTime.Now && c.CouponAutomatic != 0 select c).ToList();
            }

            return coupons;
        }
        public Coupon PrimaryAutomaticCoupon
        {
            get
            {
                // right now it only handles 1 automatic coupon
                List<Coupon> coupons = new List<Coupon>();
                Coupon resultCoupon = null;
                using (var db = new SchoolEntities())
                {
                    // get non expired coupons, that are automatic and are for order within the number of courses in the order
                    coupons = (
                        from c
                        in db.Coupons
                        where
                            c.CouponExpireDate >= DateTime.Now &&
                            c.CouponStartDate <= DateTime.Now &&
                            c.CouponAutomatic.HasValue &&
                            c.CouponAutomatic > 0 &&
                            c.CouponOrderCourses <= CourseShoppingCart.Instance.CountNonBundleCourses
                        orderby c.CouponOrderCourses ascending
                        select c)
                    .ToList();

                    resultCoupon = (from c in coupons where c.IsValidForShoppingCart orderby c.CouponOrderCourses descending select c).FirstOrDefault();
                }
                return resultCoupon;
            }
        }



        public static string GetCourseInstructorName(int courseId)
        {
            string result = "";
            CourseModel CourseModel = new CourseModel(courseId);
            if (CourseModel.Course.INSTRUCTORID != null && CourseModel.Course.INSTRUCTORID != 0)
            {
                InstructorModel InstructorModel = new InstructorModel(int.Parse(CourseModel.Course.INSTRUCTORID.ToString()));
                result = InstructorModel.Instructor.FIRST + " " + InstructorModel.Instructor.LAST;
            }
            return result;
        }

        public static string GetCourseInstructorBio(int courseId)
        {
            string result = "";
            CourseModel CourseModel = new CourseModel(courseId);
            if (CourseModel.Course.INSTRUCTORID != null && CourseModel.Course.INSTRUCTORID != 0)
            {
                InstructorModel InstructorModel = new InstructorModel(int.Parse(CourseModel.Course.INSTRUCTORID.ToString()));
                result = InstructorModel.Instructor.Bio;
            }
            return result;
        }

         public int PopulateWaitingPeople(int courseid, bool isMultiple = false)
        {
            EmailFunction emailFunction = new EmailFunction();
            using (SchoolEntities db = new SchoolEntities())
            {
                //UPDATE COURSE ROSTER
                int nextRosterId = 0;
                int ? maxEnroll = db.Courses.Where(c => c.COURSEID == courseid).SingleOrDefault().MAXENROLL;
                var course_roster_totalenroll = (from cr in db.Course_Rosters where cr.Cancel == 0 && cr.COURSEID == courseid && cr.WAITING == 0 select cr).Count();
                var rosterData = (from cr in db.Course_Rosters
                                  join c in db.Courses on cr.COURSEID equals c.COURSEID
                                  where cr.COURSEID == courseid && cr.Cancel == 0 && cr.WAITING != 0
                                  orderby cr.WaitOrder, cr.DATEADDED, cr.TIMEADDED select new { cr, c });

                maxEnroll = maxEnroll.HasValue ? maxEnroll.Value : 0;

                if (rosterData.Count() > 0)
                {
                    //multiple mode is when a maxenroll of course is updated
                    if (isMultiple) {

                        if (WebConfiguration.EnrollToWaitList)
                        {
                            string query = "SELECT TOP " + maxEnroll.Value + " rosterid FROM [Course Roster] WHERE COURSEID = " + courseid +
                                " AND (EnrollToWaitListConfig LIKE '%Waiting%') ORDER BY WAITORDER,DATEADDED, TIMEADDED DESC";

                            var data = db.Database.SqlQuery<int>(query).ToList();
                            foreach (int rosterId in data)
                            {
                                nextRosterId = rosterId;
                                this.STMoveToEnroll(nextRosterId, true);
                            }
                        }
                        else
                        {
                            //get the available wait list people but limit to the current maxenroll
                            var courseRosterReulsts = rosterData.Select(r => new PoplateWaitingPeopleModel()
                            {
                                CourseId = r.cr.COURSEID.Value,
                                RosterId = r.cr.RosterID
                            })
                            .Take(maxEnroll.Value)
                            .ToList();

                            foreach (PoplateWaitingPeopleModel people in courseRosterReulsts)
                            {
                                nextRosterId = people.RosterId;
                                this.STMoveToEnroll(nextRosterId, true);
                            }
                        }
                    }
                    //normal process
                    else {
                        //get the first in the list of waiting
                        var course_roster_results = rosterData.
                        Select(r => new PoplateWaitingPeopleModel()
                        {
                            CourseId = r.cr.COURSEID.Value,
                            RosterId = r.cr.RosterID
                        }).FirstOrDefault();

                        if (WebConfiguration.EnrollToWaitList) {
                            string query = "SELECT rosterid FROM [Course Roster] WHERE COURSEID = " + courseid +
                                " AND (EnrollToWaitListConfig LIKE '%Waiting%') ORDER BY WAITORDER,DATEADDED, TIMEADDED DESC";

                            var data = db.Database.SqlQuery<int>(query).ToList();
                            if (data.Count() > 0)
                            {
                                int waitToApproveListRosterId = data[0];
                                nextRosterId = waitToApproveListRosterId;
                            }
                        }
                        else
                        {
                            nextRosterId = course_roster_results.RosterId;
                        }

                        if (course_roster_totalenroll < maxEnroll.Value)
                        {
                            this.STMoveToEnroll(nextRosterId, true);
                        }

                    }
                }
                return nextRosterId;
            }
        }
        public string AddNewStudentForEnrollment(string FirstName, string LastName, string Email, Entities.Student NewStudent, int? cid=0)
        {
            var emailvalidator = new EmailAddressAttribute();
            if (String.IsNullOrEmpty(FirstName) || String.IsNullOrEmpty(LastName) || String.IsNullOrEmpty(Email))
            {
                return "All Fields are required.";
            }
            if (!emailvalidator.IsValid(Email))
            {
                return "Invalid Email Address.";
            }
            try
            {
                using (var db = new SchoolEntities())
                {
                    int studentCheckifexist = (from student in db.Students where student.USERNAME == Email select student).Count();
                    if (studentCheckifexist > 0)
                    {
                        return "Student already exist.";
                    }
                    else
                    {
                        if (AuthorizationHelper.CurrentStudentUser != null)
                        {
                            Entities.Student studentdetails = NewStudent;
                            studentdetails.USERNAME = Email;
                            studentdetails.FIRST = FirstName;
                            studentdetails.LAST = LastName;
                            studentdetails.EMAIL = Email;
                            studentdetails.CreatedBy = AuthorizationHelper.CurrentStudentUser.STUDENTID;
                            db.Students.Add(studentdetails);
                            db.SaveChanges();
                            UserDashQueries.SendCheckRecover(studentdetails.USERNAME, studentdetails.FIRST, studentdetails.USERNAME, "pass", "S");

                            CourseShoppingCart.Instance.AddCourse(cid.Value, null, 0, null, 0, null, null, null, false, false, 0, null, studentdetails.STUDENTID, null, false);
                        }

                        else if (AuthorizationHelper.CurrentSupervisorUser != null)
                        {
                            Entities.Student studentdetails = NewStudent;
                            studentdetails.USERNAME = Email;
                            studentdetails.FIRST = FirstName;
                            studentdetails.LAST = LastName;
                            studentdetails.EMAIL = Email;
                            studentdetails.CreatedBy = AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID;
                            db.Students.Add(studentdetails);
                            db.SaveChanges();

                            Entities.SupervisorStudent supstud = new SupervisorStudent();
                            supstud.studentid = studentdetails.STUDENTID;
                            supstud.SupervisorID = AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID;
                          
                            db.SupervisorStudents.Add(supstud);
                            db.SaveChanges();

                            CourseShoppingCart.Instance.AddCourse(cid.Value, null, 0, null, 0, null, null, null, false, false, 0, null, studentdetails.STUDENTID, null, false);
                            UserDashQueries.SendCheckRecover(studentdetails.USERNAME, studentdetails.FIRST, studentdetails.USERNAME, "pass", "S");
                        }
                        else
                        {
                            Entities.Student studentdetails = NewStudent;
                            studentdetails.USERNAME = Email;
                            studentdetails.FIRST = FirstName;
                            studentdetails.LAST = LastName;
                            studentdetails.EMAIL = Email;
                            studentdetails.CreatedBy = AuthorizationHelper.CurrentAdminUser.AdminID;
                            db.Students.Add(studentdetails);
                            db.SaveChanges();

                           CourseShoppingCart.Instance.AddCourse(cid.Value, null, 0, null, 0, null, null, null, false, false, 0, null, studentdetails.STUDENTID, null, false);
                            UserDashQueries.SendCheckRecover(studentdetails.USERNAME, studentdetails.FIRST, studentdetails.USERNAME, "pass", "S");
                        }
                        return "Student has been added successfully.";
                    }
                }
            }
            catch
            {
                return "Failed to add new Student.";
            }
        }

        public Guid? GetSessionFromOrder(string ordernumber)
        {
            using (var db = new SchoolEntities())
            {
                try
                {

                    var orderinprogress = (from s in db.OrderInProgresses where s.OrderNumber == ordernumber || s.MasterOrderNumber == ordernumber select s).FirstOrDefault();
                    return orderinprogress.UserSessionid;
                }
                catch
                {
                    return Guid.NewGuid();
                }
            }
        }

        public void UpdateSessionOfPayNowOrder(string ordernumber)
        {
            using (var db = new SchoolEntities())
            {
               
                var orderinprogress = (from s in db.OrderInProgresses where s.OrderNumber == ordernumber || s.MasterOrderNumber == ordernumber select s).ToList();

                if (AuthorizationHelper.CurrentStudentUser != null)
                {
                    var studentsession  = (from s in db.Students where s.USERNAME == AuthorizationHelper.CurrentStudentUser.USERNAME select s.UserSessionId).FirstOrDefault();

                    foreach (var orderinprog in orderinprogress)
                    {
                        orderinprog.UserSessionid = studentsession;
                    }
                }
                else if (AuthorizationHelper.CurrentSupervisorUser != null)
                {
                    foreach (var orderinprog in orderinprogress)
                    {
                        orderinprog.UserSessionid = AuthorizationHelper.CurrentSupervisorUser.UserSessionId;
                    }
                }

                db.SaveChanges();
            }
        }

        public void EnrollUserToBlackBoardApi(int courseid, int studentid, int instructorid, string userType, int gsmurosterid)
        {
            string BB_sec_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;
            string BB_app_key = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;
            string bb_connection_url = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl;
            using (var db = new SchoolEntities())
            {
                var studentuuid = (from s in db.Students where s.STUDENTID == studentid select s).FirstOrDefault();
                var courseuuid = (from s in db.Courses where s.COURSEID == courseid select s).FirstOrDefault();
                var instructoruuid = (from inst in db.Instructors where inst.INSTRUCTORID == instructorid select inst).FirstOrDefault();
                string studBB_identifier = "";
                string courseBB_identifier = "";
                string instructorBB_identifier = "";
                string studfieldsearch = "uuid";
                string coursefieldsearch = "uuid";
                string instructorfieldsearch = "uuid";

                if (studentuuid != null && courseuuid != null && userType == "student")
                {
                    if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI && Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardMembershipIntegrationEnabled)
                    {
                        BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                        var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();

                        //look up to see if student user exists.
                        var user = handelr.GetUserDetails(BB_sec_key, BB_app_key, "", bb_connection_url, studentuuid.USERNAME, "", "", jsonToken);
                        if (user.userName == null)
                        {
                            //student doesnt exist in BB.
                            BBUser user_update = new BBUser();
                            //check DSK
                            if (Settings.Instance.GetMasterInfo4().blackboard_students_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_students_dsk != null)
                            {
                                string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_students_dsk;
                                if (!string.IsNullOrEmpty(tempDSK))
                                {
                                    if (tempDSK.IndexOf("_") == -1 || tempDSK.IndexOf("_") > 0)
                                    {
                                        var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, Gsmu.Api.Integration.Blackboard.Configuration.Instance.StudentDsk, "dsk", "", jsonToken);
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
                            user_update.userName = studentuuid.USERNAME;
                            user_update.password = studentuuid.STUDNUM;

                            user_update.contact = new ProfileContactObj();
                            user_update.contact.email = studentuuid.EMAIL;
                            user_update.name = new ProfileNameObj();
                            user_update.name.given = studentuuid.FIRST;
                            user_update.name.family = studentuuid.LAST;

                            //  string[] bbSystemRole = new string[1];
                            string[] bbInstitutionRole = new string[1];
                            //bbSystemRole[0] = Configuration.Instance.BlackboardSystemRole;
                            bbInstitutionRole[0] = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardInstitutionalRole;
                            //user_update.systemRoleIds = bbSystemRole;
                            user_update.institutionRoleIds = bbInstitutionRole;

                            //create user in BB
                            BBRespUserProfile updateduser = handelr.CreateNewUser(BB_sec_key, BB_app_key, "", bb_connection_url, user_update, "", jsonToken, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.StudentInstitutionalHierarchyNodeId);
                            //update GSMU with uuid
                            var Context = new SchoolEntities();
                            var st = Context.Students.Where(s => s.STUDENTID == studentuuid.STUDENTID).SingleOrDefault();
                            if (st != null && updateduser.uuid != null)
                            {
                                st.Blackboard_user_UUID = updateduser.uuid;
                                Context.SaveChanges();
                            }
                        }

                        BBEnrollment myEnrollmentData = new BBEnrollment();
                        //if (string.IsNullOrEmpty(Gsmu.Api.Integration.Blackboard.Configuration.Instance.CourseRosterDsk))
                        //{
                        myEnrollmentData.courseRoleId = "Student";
                        //} else
                        //{
                        //myEnrollmentData.courseRoleId = Gsmu.Api.Integration.Blackboard.Configuration.Instance.CourseRosterDsk;
                        //}
                        
                        if (string.IsNullOrEmpty(studentuuid.Blackboard_user_UUID))
                        {
                            studBB_identifier = studentuuid.USERNAME;
                            studfieldsearch = "username";
                        } else
                        {
                            studBB_identifier = studentuuid.Blackboard_user_UUID;
                            studfieldsearch = "uuid";
                        }
                        if (string.IsNullOrEmpty(courseuuid.blackboard_api_uuid))
                        {
                            courseBB_identifier = courseuuid.CustomCourseField1;
                            coursefieldsearch = "courseId";
                        } else
                        {
                            courseBB_identifier = courseuuid.blackboard_api_uuid;
                            coursefieldsearch = "uuid";
                        }
                        BlackBoardAPI.BlackboardAPIRequestHandler handelr2 = new BlackboardAPIRequestHandler();
                        //BBToken BBToken2 = new BBToken();
                        //BBToken2 = handelr2.GenerateAccessToken(BB_sec_key, BB_app_key, "", bb_connection_url);
                        //var jsonToken2 = new JavaScriptSerializer().Serialize(BBToken2);
                        var jsonToken2 = AuthorizationHelper.getCurrentBBAccessToken();

                        var result= handelr2.CreateNewEnrollment(BB_sec_key, BB_app_key, "", bb_connection_url, myEnrollmentData, studBB_identifier, courseBB_identifier, studfieldsearch, coursefieldsearch, "", jsonToken2);
                        //update GSMU roster with id
                        var Context2 = new SchoolEntities();
                        var GSMUcourseRoster = Context2.Course_Rosters.Where(cr => cr.RosterID == gsmurosterid).SingleOrDefault();
                        if (GSMUcourseRoster != null && result.id != null && Settings.Instance.GetMasterInfo3().BBStudentRegEmailEnable == 1)
                        {
                            GSMUcourseRoster.blackboard_roster_id = result.id;
                            if (string.IsNullOrEmpty(GSMUcourseRoster.bb_sent_confirm))
                            {
                                GSMUcourseRoster.bb_sent_confirm = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");
                                Context2.SaveChanges();
                                EmailFunction emailFunc = new EmailFunction();
                                emailFunc.SendBlackboardEmailConfirmation(courseuuid, studentuuid);
                            } else
                            {
                                Context2.SaveChanges();
                            }
                        }
                        Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                        Audittrail.TableName = "Course Roster";
                        Audittrail.DetailDescription = "Blakcboard Enrollment-Public";
                        Audittrail.AuditDate = DateTime.Now;
                        Audittrail.CourseID = courseid;
                        Audittrail.RoutineName = "Enroll-" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                        Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                        //Audittrail.AuditAction = "info" + result.responseMessage;
                        Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                        LogManager.LogSiteActivity(Audittrail);
                    }
                }
                if (instructoruuid != null && courseuuid != null && userType == "instructor" && Settings.Instance.GetMasterInfo3().blackboard_sync_instructor != 0)
                {
                    if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
                    {
                        BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                        var jsonToken = AuthorizationHelper.getCurrentBBAccessToken();
                        BBEnrollment myEnrollmentData = new BBEnrollment();
                        myEnrollmentData.courseRoleId = Settings.Instance.GetMasterInfo3().BlackboardCourseRole;

                        //look up to see if instructor exists.
                        var user = handelr.GetUserDetails(BB_sec_key, BB_app_key, "", bb_connection_url, instructoruuid.USERNAME, "", "", jsonToken);
                        if (user.userName == null)
                        {
                            //student doesnt exist in BB.
                            BBUser user_update = new BBUser();
                            //check DSK
                            if (Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk != "" && Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk != null)
                            {
                                string tempDSK = Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk;
                                if (!string.IsNullOrEmpty(tempDSK))
                                {
                                    if (tempDSK.IndexOf("_") < 0)
                                    {
                                        var globaldatasourceKeyDetails = handelr.GetDatasourceKeyDetails(BB_sec_key, BB_app_key, "", bb_connection_url, Gsmu.Api.Integration.Blackboard.Configuration.Instance.InstructorsDsk, "dsk", "", jsonToken);
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
                            user_update.userName = instructoruuid.USERNAME;
                            user_update.password = instructoruuid.PASSWORD;

                            user_update.contact = new ProfileContactObj();
                            user_update.contact.email = instructoruuid.EMAIL;
                            user_update.name = new ProfileNameObj();
                            user_update.name.given = instructoruuid.FIRST;
                            user_update.name.family = instructoruuid.LAST;

                            //  string[] bbSystemRole = new string[1];
                            string[] bbInstitutionRole = new string[1];
                            //bbSystemRole[0] = Configuration.Instance.BlackboardSystemRole;
                            bbInstitutionRole[0] = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardInstitutionalRole;
                            //user_update.systemRoleIds = bbSystemRole;
                            user_update.institutionRoleIds = bbInstitutionRole;

                            //create user in BB
                            BBRespUserProfile updateduser = handelr.CreateNewUser(BB_sec_key, BB_app_key, "", bb_connection_url, user_update, "", jsonToken, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.InstructorInstitutionalHierarchyNodeId);
                            //update GSMU with uuid
                            var Context = new SchoolEntities();
                            var inst = Context.Instructors.Where(s => s.INSTRUCTORID == courseuuid.INSTRUCTORID).SingleOrDefault();
                            if (inst != null)
                            {
                                inst.Blackboard_user_UUID = updateduser.uuid;
                                Context.SaveChanges();
                            }
                        }

                        if (string.IsNullOrEmpty(instructoruuid.Blackboard_user_UUID))
                        {
                            instructorBB_identifier = instructoruuid.USERNAME;
                            instructorfieldsearch = "username";
                        } else
                        {
                            instructorBB_identifier = instructoruuid.Blackboard_user_UUID;
                            instructorfieldsearch = "uuid";
                        }
                        if (string.IsNullOrEmpty(courseuuid.blackboard_api_uuid))
                        {
                            courseBB_identifier = courseuuid.CustomCourseField1;
                            coursefieldsearch = "courseId";
                        }
                        else
                        {
                            courseBB_identifier = courseuuid.blackboard_api_uuid;
                            coursefieldsearch = "uuid";
                        }
                        var result = handelr.CreateNewEnrollment(BB_sec_key, BB_app_key, "", bb_connection_url, myEnrollmentData, instructorBB_identifier, courseBB_identifier, instructorfieldsearch, coursefieldsearch, "", jsonToken);
                        Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                        Audittrail.TableName = "Course Roster";
                        Audittrail.DetailDescription = "Blakcboard Enrollment-Instructor";
                        Audittrail.AuditDate = DateTime.Now;
                        Audittrail.RoutineName = "Enroll-" + AuthorizationHelper.CurrentUser.LoggedInUserType + " -Admin";
                        Audittrail.UserName = AuthorizationHelper.CurrentUser.LoggedInUsername;
                        Audittrail.AuditAction = "info" + result.responseMessage;
                        Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                        LogManager.LogSiteActivity(Audittrail);
                    }
                }
            }
        }

       
    }
    public class PoplateWaitingPeopleModel {
        public int CourseId { get; set; }
        public int RosterId { get; set; }
        public int MaxEnroll { get; set; }
    }
}

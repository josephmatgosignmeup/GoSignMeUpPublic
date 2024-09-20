using Gsmu.Service.Models.Admin.CourseDashboard;
using Gsmu.Service.Interface.Admin.Dashboard;
using Gsmu.Api.Data.School.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsmu.Api.Data.Survey.Entities;
using Gsmu.Service.BusinessLogic.GlobalTools;
using System.Web;
using System.IO;
using Gsmu.Service.Models.Constants;
using Gsmu.Service.Models.School;
using Gsmu.Service.Models.Courses;
using System.Reflection;
using Gsmu.Service.Models.Generic;

namespace Gsmu.Service.BusinessLogic.Admin.Dashboard
{
    public class CourseDashBoardManager : ICourseDashboardManager
    {
        private ISchoolEntities _db; // no need to use using(db) - the ISchoolEntities already inherited IDisposable
        private string connString = ConfigParserManager.ConnectionStringReader(ConfigParserManager.ConfigSourcePath(), ConfigSettingConstant.schoolEntitiesKey);
        private string surveyConnString = ConfigParserManager.ConnectionStringReader(ConfigParserManager.ConfigSourcePath(), ConfigSettingConstant.surveyEntitiesKey);
        IEnumerable<Course_Time> courseTimes = null;
        public CourseDashBoardManager()
        {
            _db = new SchoolEntities(connString);
        }
        //@TODO : This function should be broken to pieces and be separated for optimization
        public CourseConfigurationModel GetCourseConfigurationById(int courseId)
        {
            var masterInfo = _db.MasterInfoes.FirstOrDefault();
            var masterInfo2 = _db.MasterInfo2.FirstOrDefault();
            var masterInfo3 = _db.MasterInfo3.FirstOrDefault();
            var masterInfo4 = _db.masterinfo4.FirstOrDefault();

            var onlineclasslabel = string.Empty;
            var categories = GetCourseCategoriesById(courseId);
            var departments = new List<DeparmentModel>();

            if (masterInfo.DepartmentRequired == 1)
            {
                departments = _db.Departments.Where(d => d.DepartmentName != string.Empty)
                    .Select(d => new DeparmentModel()
                    {
                        DepartmentId = d.DeptID,
                        DepartmentName = d.DepartmentName
                    }).ToList();
            }

            if (masterInfo3.OnlineClassLabel.Trim() == string.Empty)
            {
                onlineclasslabel = "Online course";
            }

            var config = new CourseConfigurationModel
            {

                Showcredithours = masterInfo.DontDisplayCreditHours == 0,
                Showinservice = masterInfo.ShowInservice != 0,
                Showcustomcredit = masterInfo2.ShowCustomCreditType == 1,
                Showceuandgraduatecredit = masterInfo2.ShowCEUandGraduateCreditCourses == 1,
                Showoptionalcredit = masterInfo3.OptionalCredithoursvisible1 == 1,
                Creditname = masterInfo2.CreditHoursName == string.Empty ? "Credit" : masterInfo2.CreditHoursName,
                Creditinservicename = masterInfo2.InserviceHoursName == string.Empty ? "Inservice" : masterInfo2.InserviceHoursName,
                Creditcustomname = masterInfo2.CustomCreditTypeName == string.Empty ? "Credit Type" : masterInfo2.CustomCreditTypeName,
                Creditoptionalname = masterInfo3.OptionalcredithoursLabel1,
                Creditoptionalname2 = masterInfo3.OptionalcredithoursLabel2,
                Creditoptionalname3 = masterInfo3.OptionalcredithoursLabel3,
                Creditoptionalname4 = masterInfo3.OptionalcredithoursLabel4,
                Creditoptionalname5 = masterInfo3.OptionalcredithoursLabel5,
                Creditoptionalname6 = masterInfo3.OptionalcredithoursLabel6,
                Creditoptionalname7 = masterInfo3.OptionalcredithoursLabel7,
                Creditoptionalname8 = masterInfo3.OptionalcredithoursLabel8,

                Pricemember = masterInfo2.membertypememberlabel,
                Pricenonmember = masterInfo2.membertypenonmemberlabel,
                Pricespecial = masterInfo2.MemberTypeSpecialMemberLabel1,
                //Blackboard = ,
                //Helius = ,
                //Survey = ,
                Coursecategories = masterInfo.NoOfCategories,
                Gradefield = masterInfo.Field1Name,
                Schoolfield = masterInfo.Field2Name,
                Districtfield = masterInfo.Field3Name,
                Membershipfield = masterInfo.Field4Name,
                Maincatcontainer = masterInfo3.maincat_container,
                Onlineclasslabel = onlineclasslabel,
                Membership_status = masterInfo.Membership_Status,
                NoOfInstructors = masterInfo.NoOfInstructors,
                CourseCloseDays = masterInfo.CourseCloseDays,
                Pricingoptionslabel = masterInfo2.PricingOptionsLabel,
                Pricingvisible = masterInfo2.PricingVisible,
                Pricingoptionoverride = masterInfo2.PricingOptionOverride,
                Pricingoptrange = masterInfo2.PricingOptRange,
                Alloweditingofcourseprice = masterInfo2.AllowEditingOfCoursePrice,
                Systemaccessliteon = masterInfo3.SystemAccessLiteOn == 1,

                //Courseconfirmation_email_max_upload = ,

                Google_sso_client_id = masterInfo3.google_sso_client_id,
                Google_sso_client_secret = masterInfo3.google_sso_client_secret,
                Google_sso_enabled = masterInfo3.google_sso_enabled,
                Google_sso_api_key = masterInfo4.google_sso_api_key,
                Google_drive_enabled = masterInfo4.google_drive_enabled == 1,
                Roomnumberoption = masterInfo.RoomNumberOption,
                Roomdirectionsoption = masterInfo.RoomDirectionsOption,
                Hidelocationname = masterInfo2.HideLocationName == -1,
                Publicterminology = masterInfo2.PublicTerminology == 1,
                Publiclayoutconfiguration = masterInfo4.PublicLayoutConfiguration,
                Dotnetsiterooturl = masterInfo4.DotNetSiteRootUrl,
                Collectionstylelabel = masterInfo2.CollectionStyleLabel,
                Splitordersfeature = masterInfo3.SplitOrdersFeature,
                Allowpartialpayment = masterInfo3.AllowPartialPayment,
                Blackboard_course_roster_dsk = masterInfo4.blackboard_course_roster_dsk,
                Blackboard_courses_dsk = masterInfo4.blackboard_courses_dsk,
                Blackboard_instructors_dsk = masterInfo4.blackboard_instructors_dsk,
                Blackboard_students_dsk = masterInfo4.blackboard_students_dsk,
                Defaultpublicpricingtype = masterInfo2.DefaultPublicPricingType,
                Contactinfo = masterInfo2.ContactInfo,
                Customcoursefieldlabel5 = masterInfo2.CustomCourseFieldLabel5,
                Customcoursefieldshow5 = masterInfo2.CustomCourseFieldShow5,

                //Categories = categories,

                //Pricingoptions = ,

                //Bbstart = ,
                //Bbend = ,

                //Surveys = ,

                //Audiences =,
                //Icons = ,

                //Coursecolorgrouping = ,

                Depratmentrequired = masterInfo.DepartmentRequired,
                Departments = departments

            };
            return config;
        }
        /// <summary>
        /// Returning only the necessary objects, should not load everything
        /// </summary>
        /// <returns></returns>
        public CourseConfigurationModel GetCourseConfiguration()
        {
            CourseConfigurationModel configModel = new CourseConfigurationModel();
            configModel.Audiences = _db.Audiences.Select(a => new AudienceModel()
            {
                AudienceId = a.AudienceID,
                AudienceName = a.Audience1
            }).ToList();

            configModel.Icons = _db.Icons.Select(i => new IconsModel()
            {
                IconId = i.IconsID,
                IconTitle = i.IconTitle,
                IconImageUrl = i.IconImage
            }).ToList();

            configModel.Departments = _db.Departments.Select(d => new DeparmentModel()
            {
                DepartmentId = d.DeptID,
                DepartmentName = d.DepartmentName
            }).ToList();

            configModel.CourseColorGrouping = _db.CourseCategories.Select(c => new CourseGroupingModel()
            {
                CourseCategoryID = c.CourseCategoryID,
                CourseCategoryColor = c.CourseCategoryColor.ToLower(),
                CourseCategoryName = c.CourseCategoryName,
                UseColorBar = c.UseColorBar
            }).ToList();

            configModel.CustomCertificates = _db.customcetificates.Select(cc => new Models.School.CustomCertificateModel()
            {
                CustomCertificateId = cc.customcertid,
                CertificateTitle = cc.certtitle
            }).ToList();

            return configModel;
        }

        public List<CourseBasicDetails> GetCourseByFilter() {
            var data = (from c in _db.Courses
                        where
                        c.COURSENAME != null && c.COURSENUM != null
                        select c)
                        .Take(100)
                        .ToList().AsEnumerable();
            return data.Select(c => new CourseBasicDetails()
            {
                CourseId = c.COURSEID,
                CourseName = c.COURSENAME,
                CourseNumber = c.COURSENUM,
                Description = c.DESCRIPTION,
                TileImage = c.TileImageUrl,
                CourseStartDate = this.CourseStartAsDate(c.COURSEID).Value,
                CourseEndDate = this.CourseEndAsDate(c.COURSEID).Value,
            })
            .OrderBy(c => c.CourseName)
            .ToList();
        }

        public List<CourseBasicDetails> GetCourseByFilter(string query)
        {
            var data = (from c in _db.Courses
                        where
                        c.COURSENAME != null && c.COURSENUM != null &&
                        c.COURSEID.ToString().StartsWith(query) ||
                        c.COURSENAME.StartsWith(query) ||
                        c.COURSENUM.StartsWith(query) ||
                        c.DESCRIPTION.StartsWith(query)
                        select c).ToList().AsEnumerable();
            return data.Select(c => new CourseBasicDetails()
            {
                CourseId = c.COURSEID,
                CourseName = c.COURSENAME,
                CourseNumber = c.COURSENUM,
                Description = c.DESCRIPTION,
                TileImage = c.TileImageUrl,
                CourseStartDate =  this.CourseStartAsDate(c.COURSEID).Value,
                CourseEndDate = this.CourseEndAsDate(c.COURSEID).Value,
            })
            .OrderBy(c => c.CourseName)
            .ToList();
        }

        public CourseDescriptionModel GetCourseDescriptionById(int courseId)
        {
            return _db.Courses.Where(c => c.COURSEID == courseId)
                .Select(c => new CourseDescriptionModel()
                {
                    CourseId = c.COURSEID,
                    CourseNumber = c.COURSENUM,
                    CourseName = c.COURSENAME,
                    CancelCourse = c.CANCELCOURSE,
                    MasterCourseId = c.CustomCourseField5,
                    InternalClass = c.InternalClass,
                    DistPrice = c.DISTPRICE,
                    NoDistPrice = c.NODISTPRICE,
                    Location = c.LOCATION,
                    Locationurl = c.LOCATIONURL,
                    Street = c.STREET,
                    City = c.CITY,
                    State = c.STATE,
                    Zip = c.ZIP,
                    Room = c.ROOM,
                    Times = c.TIMES,
                    MaxEnroll = c.MAXENROLL,
                    MaxWait = c.MAXWAIT,
                    InstructorId = c.INSTRUCTORID,
                    InstructorId2 = c.INSTRUCTORID2,
                    InstructorId3 = c.INSTRUCTORID3,
                    Description = c.DESCRIPTION,
                    ShortDescription = c.ShortDescription,
                    MainCategory = c.MAINCATEGORY,
                    MainCategory2 = c.MAINCATEGORY2,
                    Maincategory3 = c.MAINCATEGORY3,
                    SubCategory1 = c.SUBCATEGORY1,
                    Subcategory2 = c.SUBCATEGORY2,
                    SubCategory1b = c.SUBCATEGORY1b,
                    SubCategory2b = c.SUBCATEGORY2b,
                    SubCategory1c = c.SUBCATEGORY1c,
                    SubCategory2c = c.SUBCATEGORY2c,
                    Materials = c.MATERIALS,
                    Days = c.DAYS,
                    CreditHours = c.CREDITHOURS,
                    EmailReminderType = c.EmailReminderType,
                    EmailReminderSubject = c.EmailReminderSubject,
                    EmailReminderBody = c.EmailReminderBody,
                    CourseConfirmationEmailExtraText = c.CourseConfirmationEmailExtraText,
                    Notes = c.Notes,
                    ContactName = c.ContactName,
                    ContactPhone = c.ContactPhone,
                    AllowCreditRollOver = c.AllowCreditRollover,
                    GradingSystem = c.GradingSystem,
                    ShowCreditInPublic = c.showcreditinpublic,
                    CustomCreditHours = c.CustomCreditHours.HasValue ? (float)c.CustomCreditHours.Value : 0,
                    InServiceHours = (float)c.InserviceHours,
                    CEUCredit = (float)c.CEUCredit,
                    GraduateCredit = (float)c.GraduateCredit,
                    OnlineCourse = c.OnlineCourse.HasValue ? c.OnlineCourse.Value : 0,
                    CourseCloseDays = c.CourseCloseDays.HasValue ? c.CourseCloseDays.Value : 0,
                    ShowPrerequisiteInfo = c.ShowPrerequisite,
                    PrerequisiteInfo = c.PrerequisiteInfo,
                    AccessCode = c.courseinternalaccesscode,
                    SpecialPrice = c.SpecialDistPrice1,
                    AudienceId = c.AudienceID,
                    DepartmentId = c.DepartmentNameID,
                    CourseColorGrouping = c.CourseColorGrouping.HasValue ? c.CourseColorGrouping.Value : 0,
                    CourseCertificationsId = c.CourseCertificationsId.HasValue ? c.CourseCertificationsId.Value : 0,
                    ViewPastCoursesDays = c.viewpastcoursesdays.HasValue ? c.viewpastcoursesdays.Value : 0,
                    StartEndTimeDisplay = c.StartEndTimeDisplay,
                    AllowSendSurvey = c.AllowSendSurvey.HasValue ? c.AllowSendSurvey.Value : 0,
                    TileImageUrl = c.TileImageUrl,
                    HaikuLastResult = c.haiku_last_result == null ? "{}" : c.haiku_last_result,
                    DisableHaikuIntegration = c.disablehaikuintegration.HasValue ? c.disablehaikuintegration.Value : 0,
                    HaikuCourseId = c.haiku_course_id.HasValue ? c.haiku_course_id.Value : 0,
                    HaikuImportId = c.haiku_import_id == null ? "" : c.haiku_import_id,
                    HaikuIntegrationDate = c.haiku_integration_date.HasValue ? c.haiku_integration_date.Value : new DateTime().Date,
                    HaikuLastIntegrationDate = c.haiku_last_integration_date.HasValue ? c.haiku_last_integration_date.Value : new DateTime().Date,
                    CanvasCourseId = c.canvas_course_id.HasValue ? c.canvas_course_id.Value : 0,
                    DisableCanvasIntegration = c.disable_canvas_integration.HasValue ? c.disable_canvas_integration.Value : 0,
                    BBServer = c.BBServer.HasValue ? c.BBServer.Value : 0,
                    BBAutoEnroll = c.bbautoenroll.HasValue ? c.bbautoenroll.Value : 0,
                    BBCourseCloned = c.BBCourseCloned.HasValue ? c.BBCourseCloned.Value : 0,
                    BBDescription = c.blackboard_dsk == null ? "" : c.blackboard_dsk,
                    BBLastIntegrationDate = c.bb_last_integration_date.HasValue ? c.bb_last_integration_date.Value : new DateTime().Date,
                    BBLastUpdateGrade = c.bb_last_update_grade.HasValue ? c.bb_last_update_grade.Value : new DateTime().Date,
                    BBLastIntegrationState = c.bb_last_integration_state,
                    MaterialsRequired = c.MaterialsRequired.HasValue ? c.MaterialsRequired.Value : 0,
                    CourseCertificate = c.coursecertificate.HasValue ? c.coursecertificate.Value : 0,
                    Icons = c.Icons,
                    PartialPaymentAmount = c.PartialPaymentAmount,
                    PartialPaymentNon = c.PartialPaymentNon,
                    PartialPaymentSP = c.PartialPaymentSP,
                    DisplayPrice = c.DisplayPrice,
                    NoRegEmail = c.NoRegEmail.HasValue ? c.NoRegEmail.Value : -1,
                    GoogleCalendarSyncEnabled = c.google_calendar_import_enable.HasValue ? c.google_calendar_import_enable.Value : 0
                }).FirstOrDefault();
        }

        public List<CourseExpensesModel> GetCourseExpensesById(int courseId)
        {
            return _db.CourseExpenses.Where(c => c.CourseId == courseId)
                .Select(ce => new CourseExpensesModel()
                {
                    CourseExpenseId = ce.CourseExpenseId,
                    CourseExpenseAmount = ce.CourseExpenseAmount,
                    CourseId = ce.CourseId,
                    CourseDisplayPosition = ce.CourseDisplayPosition,
                    CourseExpenseTitle = ce.CourseExpenseTitle,
                    InstructorId = ce.InstructorId,
                    InstructorDisplayPosition = ce.InstructorDisplayPosition,
                    InstructorFixedFee = ce.InstructorFixedFee,
                    InstructorPerStudent = ce.InstructorPerStudent,
                    InstructorRevenuePercentage = ce.InstructorRevenuePercentage
                }).ToList();
        }

        public CoursePricingMainModel GetCoursePricingById(int courseId)
        {
            //public = 1, member = 0, special = 3
            //SELECT cpo.*, po.PriceTypedesc FROM CoursePricingOptions 
            //cpo LEFT JOIN PricingOptions po ON po.pricingoptionid = cpo.PricingOptionId 
            //WHERE cpo.CourseId = "& courseid &" ORDER BY po.PriceTypedesc asc
            var pricing = (from cpo in _db.CoursePricingOptions
                           join po in _db.PricingOptions on cpo.PricingOptionId equals po.PricingOptionID
                           where cpo.CourseId == courseId
                           orderby cpo.PricingOptionId
                           select new CoursePricingModel()
                           {
                               Id = po.PricingOptionID,
                               Price = cpo.Price,
                               Title = po.PriceTypedesc,
                               RangeStart = cpo.rangestart,
                               RangeEnd = cpo.rangeend,
                               Type = cpo.Type
                           }).ToList();
            var publicPricing = pricing.Where(p => p.Type == 1).OrderBy(o => o.Id).ToList();
            var memberPricing = pricing.Where(p => p.Type == 0).OrderBy(o => o.Id).ToList();
            var specialPricing = pricing.Where(p => p.Type == 3).OrderBy(o => o.Id).ToList();

            return new CoursePricingMainModel()
            {
                PublicCoursePricing = publicPricing,
                MemberCoursePricing = memberPricing,
                SpecialCoursePricing = specialPricing,
            };
        }

        public CourseRostersMainModel GetCourseRostersById(int courseId)
        {
            var rosters = (from cr in _db.Course_Rosters
                           join s in _db.Students on cr.STUDENTID equals s.STUDENTID
                           join sc in _db.Schools on s.SCHOOL equals sc.locationid into ssc
                           from scs in ssc.DefaultIfEmpty()
                           where cr.COURSEID == courseId
                           select new CourseRostersModel()
                           {
                               Rosterid = cr.RosterID,
                               FirstName = s.FIRST,
                               LastName = s.LAST,
                               RegisteredDate = cr.DATEADDED.Value,
                               Cancel = cr.Cancel,
                               Waiting = cr.WAITING,
                               School = scs.LOCATION
                           }).ToList();

            var activeRosters = rosters.Where(cr => cr.Waiting == 0 && cr.Cancel == 0)
                .Select(cr => new CourseRostersModel()
                {
                    Status = "Active",
                    Rosterid = cr.Rosterid,
                    RegisteredDate = cr.RegisteredDate,
                    FirstName = cr.FirstName,
                    LastName = cr.LastName,
                    School = cr.School ?? "",
                    RegisteredDateString = cr.RegisteredDate.Date.ToString("MM-dd-yyyy"),
                }).ToList();

            var waitingRosters = rosters.Where(cr => cr.Waiting != 0)
                .Select(cr => new CourseRostersModel()
                {
                    Status = "Waiting",
                    Rosterid = cr.Rosterid,
                    RegisteredDate = cr.RegisteredDate,
                    FirstName = cr.FirstName,
                    LastName = cr.LastName,
                    School = cr.School ?? "",
                    RegisteredDateString = cr.RegisteredDate.Date.ToString("MM-dd-yyyy"),
                }).ToList();

            var cancelledRosters = rosters.Where(cr => cr.Cancel != 0)
                .Select(cr => new CourseRostersModel()
                {
                    Status = "Cancelled",
                    Rosterid = cr.Rosterid,
                    RegisteredDate = cr.RegisteredDate,
                    FirstName = cr.FirstName,
                    LastName = cr.LastName,
                    School = cr.School ?? "",
                    RegisteredDateString = cr.RegisteredDate.Date.ToString("MM-dd-yyyy")
                }).ToList();

            var all = activeRosters.Concat(waitingRosters).Concat(cancelledRosters).ToList();

            return new CourseRostersMainModel()
            {
                ActiveRosters = activeRosters,
                WaitingRosters = waitingRosters,
                CancelledRosters = cancelledRosters,
                AllRosters = all
            };
        }

        public List<CourseSurveyModel> GetCourseSurveyById(int courseId)
        {
            using (var db = new SurveyEntities(surveyConnString))
            {
                var courseSurvey = (from cs in db.CourseSurveys
                                    from s in db.Surveys.Where(s => s.SurveyID == cs.SurveyID).DefaultIfEmpty()
                                    from bs in db.Surveys.Where(bs => bs.SurveyID == cs.BeforeCourseSurveyId).DefaultIfEmpty()
                                    where cs.CourseID == courseId
                                    select new CourseSurveyModel
                                    {
                                        SurveyID = cs.SurveyID,
                                        BeforeCourseSurveyId = cs.BeforeCourseSurveyId,
                                        Name = s.Name,
                                        BeforeName = bs.Name
                                    }).ToList();

                return courseSurvey;
            }
        }

        public List<CourseSurveyResultModel> GetCourseSurveyResultById(int courseId)
        {
            using (var db = new SurveyEntities(surveyConnString))
            {
                var surveyResult = (from u in db.Users
                                    from s in db.Surveys.Where(s => s.SurveyID == u.SurveyID).DefaultIfEmpty()
                                    where u.CourseID == courseId
                                    select new CourseSurveyResultModel()
                                    {
                                        SurveyID = u.SurveyID,
                                        Name = s.Name
                                    }).Distinct().ToList();

                return surveyResult;
            }
        }

        public List<Models.Courses.CourseCategoriesModel> GetCourseCategoriesById(int courseId)
        {
            return (from mc in _db.MainCategories
                    join sc in _db.SubCategories on mc.MainCategoryID equals sc.MainCategoryID into mcsc //_db.MainCategories.Where(c => c.CourseID == courseId).ToList();
                    from mcscAlias in mcsc.DefaultIfEmpty()
                    join ssc in _db.SubSubCategories on mcscAlias.SubCategoryID equals ssc.SubSubCategoryID into scssc
                    from scsscAlias in scssc.DefaultIfEmpty()
                    where mc.CourseID == courseId
                    select new Models.Courses.CourseCategoriesModel()
                    {
                        MainCategoryName = mc.MainCategory1,
                        SubCategoryName = mcscAlias.SubCategory1,
                        SubSubCategoryName = scsscAlias.SubSubCategory1,
                        MainOrder = mc.MainOrder
                    }).ToList();
        }

        public List<CourseInstructorsModel> GetCourseInstructorsById(int courseId)
        {
            List<int> instructorIds = new List<int>();
            var courseInstructors = (from c in _db.Courses
                                     where c.COURSEID == courseId
                                     select new { c.INSTRUCTORID, c.INSTRUCTORID2, c.INSTRUCTORID3 }).SingleOrDefault();
            if (courseInstructors != null)
            {
                if (courseInstructors.INSTRUCTORID.HasValue)
                    instructorIds.Add(courseInstructors.INSTRUCTORID.Value);
                if (courseInstructors.INSTRUCTORID2.HasValue)
                    instructorIds.Add(courseInstructors.INSTRUCTORID2.Value);
                if (courseInstructors.INSTRUCTORID3.HasValue)
                    instructorIds.Add(courseInstructors.INSTRUCTORID3.Value);
            }

            if (!string.IsNullOrEmpty(instructorIds.ToString()))
            {
                var instructors = (from i in _db.Instructors
                                   where instructorIds.Contains(i.INSTRUCTORID)
                                   select new CourseInstructorsModel()
                                   {
                                       //Address = i.ADDRESS,
                                       //AdminNotes = i.AdminNotes,
                                       Bio = i.Bio,
                                       //City = i.CITY,
                                       //Country = i.COUNTRY,
                                       //Disabled = i.DISABLED,
                                       //DistEmployee = i.DISTEMPLOYEE,
                                       //DistrictId = i.DISTRICT,
                                       Email = i.EMAIL,
                                       Fax = i.FAX,
                                       FirstName = i.FIRST,
                                       GradeLevel = i.GRADELEVEL,
                                       HomePhone = i.HOMEPHONE,
                                       InstructorId = i.INSTRUCTORID,
                                       InstructorNumber = i.INSTRUCTORNUM,
                                       LastName = i.LAST,
                                       //Password = i.PASSWORD,
                                       PhotoImage = i.PhotoImage,
                                       //SchoolId = i.SCHOOL,
                                       //State = i.STATE,
                                       UserName = i.USERNAME,
                                       WorkPhone = i.WORKPHONE,
                                       //Zip = i.ZIP
                                   }).ToList();
                return instructors;
            }
            return new List<CourseInstructorsModel>();
        }

        public InstructorModel GetInstructorById(int instructorId)
        {
            return _db.Instructors.Where(i => i.INSTRUCTORID == instructorId)
                .Select(i => new InstructorModel()
                {
                    Bio = i.Bio,
                    LastName = i.LAST,
                    FirstName = i.FIRST,
                    GradeLevel = i.GRADELEVEL,
                    InstructorId = i.INSTRUCTORID,
                    InstructorNumber = i.INSTRUCTORNUM,
                    PhotoImage = i.PhotoImage,
                    UserName = i.USERNAME,
                    Email = i.EMAIL,
                    Fax = i.FAX,
                    HomePhone = i.HOMEPHONE,
                    WorkPhone = i.WORKPHONE,
                }).SingleOrDefault();
        }

        public List<CourseMaterialsModel> GetCourseMaterialsById(int courseId)
        {
            var course = GetCourseDescriptionById(courseId);
            var materials = course.Materials.Replace("~", string.Empty).Replace(" ", string.Empty).TrimEnd(',').Split(',').ToList();
            if (materials.Count() > 0)
            {
                var courseMaterials = (from m in _db.Materials
                                       where materials.Contains(m.productID.ToString())
                                       select new CourseMaterialsModel()
                                       {
                                           ProductId = m.productID,
                                           ProductNumber = m.product_num,
                                           ProductName = m.product_name,
                                           Price = m.price,
                                           ShippingCost = m.shipping_cost,
                                           PriceIncluded = m.priceincluded,
                                           Taxable = m.taxable,
                                           ShippingWeight = m.shipping_weight,
                                           Quantity = m.quantity,
                                           UseQuantityFromMaterialId = m.use_qty_from_materialid,
                                           NonRefundable = m.non_refundable,
                                           HideMaterialPrice = m.hidematerialprice
                                       }).ToList();

                return courseMaterials;
            }
            return new List<CourseMaterialsModel>();
        }

        public CourseRosterExtraModel GetCourseRosterExtraById(int courseId)
        {
            var rostersExtra = (from cep in _db.CourseExtraParticipants
                                from cr in _db.Course_Rosters.Where(cr => cr.RosterID == cep.RosterId).DefaultIfEmpty()
                                from s in _db.Students.Where(s => s.STUDENTID == cr.STUDENTID).DefaultIfEmpty()
                                where s.STUDENTID > 0 && cr.COURSEID == courseId
                                select new { cep, cr }).ToList();

            var activeRostersExtra = rostersExtra.Where(re => re.cr.WAITING == 0 && re.cr.Cancel == 0)
                .Select(re => new Gsmu.Service.Models.Admin.CourseDashboard.CourseExtraParticipant()
                {
                    CourseExtraParticipantId = re.cep.CourseExtraParticipantId,
                    RosterId = re.cep.RosterId,
                    StudentFirst = re.cep.StudentFirst,
                    StudentLast = re.cep.StudentLast,
                    StudentEmail = re.cep.StudentEmail,
                    CustomField2 = re.cep.CustomField2
                }).ToList();

            var waitingRostersExtra = rostersExtra.Where(re => re.cr.WAITING != 0)
                    .Select(re => new Gsmu.Service.Models.Admin.CourseDashboard.CourseExtraParticipant()
                    {
                        CourseExtraParticipantId = re.cep.CourseExtraParticipantId,
                        RosterId = re.cep.RosterId,
                        StudentFirst = re.cep.StudentFirst,
                        StudentLast = re.cep.StudentLast,
                        StudentEmail = re.cep.StudentEmail,
                        CustomField2 = re.cep.CustomField2
                    }).ToList();

            var cancelledRostersExtra = rostersExtra.Where(re => re.cr.Cancel != 0)
                    .Select(re => new Gsmu.Service.Models.Admin.CourseDashboard.CourseExtraParticipant()
                    {
                        CourseExtraParticipantId = re.cep.CourseExtraParticipantId,
                        RosterId = re.cep.RosterId,
                        StudentFirst = re.cep.StudentFirst,
                        StudentLast = re.cep.StudentLast,
                        StudentEmail = re.cep.StudentEmail,
                        CustomField2 = re.cep.CustomField2
                    }).ToList();

            return new CourseRosterExtraModel()
            {
                ActiveRostersExtra = activeRostersExtra,
                WaitingRostersExtra = waitingRostersExtra,
                CancelledRostersExtra = cancelledRostersExtra
            };
        }

        public List<Models.Courses.CourseDateTimeModel> GetCourseDateAndTimesById(int courseId)
        {
            return _db.Course_Times.Where(c => c.COURSEID == courseId)
                .Select(ct => new Models.Courses.CourseDateTimeModel()
                {
                    Id = ct.ID,
                    CourseId = ct.COURSEID,
                    CourseDate = ct.COURSEDATE,
                    StartTime = ct.STARTTIME,
                    EndTime = ct.FINISHTIME
                })
                .OrderBy(c => c.CourseDate)
                .ToList();
        }

        public void DeleteCourseDateWhenOnlineById(int courseId) {
            using (var db = new SchoolEntities(connString))
            {
                var data = db.Course_Times.Where(c => c.COURSEID == courseId)
                .OrderBy(c => c.COURSEDATE)
                .ToList();

                if (data.Count() > 2)
                {
                    var forDeletion = data.Skip(2).ToList();
                    db.Course_Times.RemoveRange(forDeletion);
                    db.SaveChanges();
                }
            }
        }

        public List<CourseTransciptsModel> GetCourseTransciptsById(int courseId)
        {
            return _db.Transcripts.Where(t => t.CourseId == courseId)
                .Select(t => new CourseTransciptsModel()
                {
                    TranscriptID = t.TranscriptID,
                    studrosterid = _db.Course_Rosters.Where(cr => cr.STUDENTID == t.STUDENTID && cr.COURSEID == t.CourseId).FirstOrDefault() != null ?
                                _db.Course_Rosters.Where(cr => cr.STUDENTID == t.STUDENTID && cr.COURSEID == t.CourseId).FirstOrDefault().RosterID : 0,
                    CourseId = t.CourseId,
                    CourseName = t.CourseName,
                    CourseNum = t.CourseNum,
                    CourseDate = t.CourseDate,
                    CourseLocation = t.CourseLocation,
                    DistPrice = t.DistPrice,
                    NoDistPrice = t.NoDistPrice,
                    StudentId = t.STUDENTID,
                    StudentsSchool = t.StudentsSchool,
                    District = t.District,
                    InstructorName = t.InstructorName,
                    InstructorName2 = t.InstructorName2,
                    InstructorName3 = t.InstructorName3,
                    Room = t.Room,
                    Days = t.Days,
                    CreditHours = t.CreditHours,
                    DateAdded = t.DATEADDED,
                    TimeAdded = t.TIMEADDED,
                    Hours = t.HOURS,
                    CourseCost = t.CourseCost,
                    TotalPaid = t.TotalPaid,
                    PaidInFull = t.PaidInFull,
                    AttendanceDetail = t.AttendanceDetail,
                    CustomCreditHours = t.CustomCreditHours,
                    graduatecredit = t.graduatecredit,
                    CEUCredit = t.ceucredit,
                    StudentFirstName = t.Student.FIRST,
                    StudentLastName = t.Student.LAST
                }).ToList();
        }

        public List<CourseChoices> GetCourseChoicesById(int courseId)
        {
            string query = "SELECT * FROM [CoursesCourseChoices] WHERE courseId = " + courseId;
            using (var db = new SchoolEntities(connString))
            {
                List<CourseChoices> courseChoices = db.Database.SqlQuery<CourseChoices>(query).ToList();
                return courseChoices;
            }
        }

        public List<FileModel> GetDocumentFilesById(int courseId)
        {
            //string documentsPath = "/admin/Documents/"; // live environment
            string documentsPath = ConfigSettingConstant.webRootAdminUrlDev + "/Documents/";
            var rootDirectory = new DirectoryInfo(HttpContext.Current.Server.MapPath("~/")).Parent.FullName;
            DirectoryInfo directoryInfo = new DirectoryInfo(rootDirectory + documentsPath);
            if (!directoryInfo.Exists) return new List<FileModel>();

            var files = directoryInfo.GetFiles("*" + courseId.ToString() + "*.*").ToList();
            return files.Select(f => new FileModel()
            {
                Name = f.Name,
                Size = f.Length,
                Extension = f.Extension,
                LastDateModified = f.LastWriteTime
                ///Directory = f.FullName dont expose
            }).ToList();
        }

        //EDITING
        public Models.Courses.CourseModel SaveCourse(CourseDescriptionModel model)
        {
            using (var db = new SchoolEntities(connString))
            {
                var course = db.Courses.Where(c => c.COURSEID == model.CourseId).SingleOrDefault();
                if (course != null)
                {
                    course.COURSENAME = model.CourseName;
                    course.COURSENUM = model.CourseNumber;
                    course.CustomCourseField5 = model.MasterCourseId;
                    course.CANCELCOURSE = model.CancelCourse;
                    course.InternalClass = model.InternalClass;
                    course.OnlineCourse = model.OnlineCourse;
                    //description
                    course.DESCRIPTION = model.Description;
                    course.ShortDescription = model.ShortDescription;
                    //pricing
                    course.DISTPRICE = model.DistPrice;
                    course.NODISTPRICE = model.NoDistPrice;
                    course.SpecialDistPrice1 = model.SpecialPrice;
                    //location
                    course.LOCATION = model.Location;
                    course.LOCATIONURL = model.Locationurl;
                    course.STREET = model.Street;
                    course.CITY = model.City;
                    course.STATE = model.State;
                    course.ZIP = model.Zip;
                    course.ROOM = model.Room;
                    //enrollment
                    course.MAXENROLL = model.MaxEnroll == null ? 0 : model.MaxEnroll.Value;
                    course.MAXWAIT = model.MaxWait == null ? 0 : model.MaxWait.Value;
                    //instructors
                    course.INSTRUCTORID = model.InstructorId;
                    course.INSTRUCTORID2 = model.InstructorId2;
                    course.INSTRUCTORID3 = model.InstructorId3;


                    course.CourseConfirmationEmailExtraText = model.CourseConfirmationEmailExtraText;
                    //contact
                    course.ContactName = model.ContactName;
                    course.ContactPhone = model.ContactPhone;

                    course.AllowCreditRollover = (short)model.AllowCreditRollOver;
                    course.courseinternalaccesscode = model.AccessCode;

                    //course options
                    course.GradingSystem = (short)model.GradingSystem;
                    course.showcreditinpublic = model.ShowCreditInPublic;
                    course.CustomCreditHours = model.CustomCreditHours;
                    course.InserviceHours = (float)model.InServiceHours;
                    course.CEUCredit = (float)model.CEUCredit;
                    course.GraduateCredit = (float)model.GraduateCredit;

                    course.coursecertificate = (int)model.CourseCertificate;

                    course.MaterialsRequired = (short)model.MaterialsRequired;
                    course.PartialPaymentAmount = model.PartialPaymentAmount;
                    course.PartialPaymentNon = model.PartialPaymentNon;
                    course.PartialPaymentSP = model.PartialPaymentSP;
                    course.DisplayPrice = (short)model.DisplayPrice;
                    course.SendConfirmationEmailtoInstructor = model.SendConfirmationEmailtoInstructor;
                    course.NoRegEmail = model.NoRegEmail;
                    course.google_calendar_import_enable = model.GoogleCalendarSyncEnabled;
                }
                else
                {
                    //create
                }
                db.SaveChanges();
                if (model.uploadPhoto == true) SaveCourseImage();
                if (model.deletePhoto == true) DeleteCourseImage();
            }

            return new Models.Courses.CourseModel();
        }

        public void SaveCourseImage()
        {
            var httpPostedFile = HttpContext.Current.Request.Files["UploadedImage"];
            var courseId = HttpContext.Current.Request.Form["courseId"];
            if (httpPostedFile != null)
            {
                var fileSavePath = Path.Combine(new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath("~")).Parent.FullName
                    + ConfigSettingConstant.webRootUrlDev
                    + ConfigSettingConstant.imageDestinationDirectory, courseId + ".jpg");
                httpPostedFile.SaveAs(fileSavePath);
            }
        }

        public void SaveFile()
        {
            var httpPostedFile = HttpContext.Current.Request.Files["UploadedFile"];
            var courseId = HttpContext.Current.Request.Form["courseId"];
            if (httpPostedFile != null)
            {
                var ext = Path.GetExtension(httpPostedFile.FileName);
                var fileSavePath = Path.Combine(new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath("~")).Parent.FullName
                    + ConfigSettingConstant.webRootAdminUrlDev
                    + @"Documents\", courseId + "_" + httpPostedFile.FileName);
                httpPostedFile.SaveAs(fileSavePath);
            }
        }

        public void DeleteCourseImage()
        {
            var courseId = HttpContext.Current.Request.Form["courseId"];
            var filePath = Path.Combine(new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath("~")).Parent.FullName
                + ConfigSettingConstant.webRootUrlDev
                + ConfigSettingConstant.imageDestinationDirectory, courseId + ".jpg");
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        public Models.Courses.CourseDateTimeModel SaveDateTime(Models.Courses.CourseDateTimeModel model)
        {
            try
            {
                using (var db = new SchoolEntities(connString))
                {
                    var courseDateTime = db.Course_Times.Where(c => c.ID == model.Id).SingleOrDefault();
                    if (courseDateTime != null)
                    {
                        courseDateTime.COURSEDATE = Convert.ToDateTime(model.CourseDateString);
                        courseDateTime.STARTTIME = Convert.ToDateTime(model.StartTimeString);
                        courseDateTime.FINISHTIME = Convert.ToDateTime(model.EndTimeString);
                    }
                    else
                    {
                        //create
                        Course_Time courseTime = new Course_Time()
                        {
                            COURSEDATE = Convert.ToDateTime(model.CourseDateString),
                            STARTTIME = Convert.ToDateTime(model.StartTimeString),
                            FINISHTIME = Convert.ToDateTime(model.EndTimeString),
                            COURSEID = model.CourseId
                        };
                        db.Course_Times.Add(courseTime);
                    }
                    db.SaveChanges();
                    return model;
                }
            }
            catch (Exception ex)
            {
                throw ex; // handled by the API (perhaps)
            }
        }

        public CourseMaterialsModel SaveCourseMaterial(int courseId, int materialId)
        {
            using (var db = new SchoolEntities(connString))
            {
                var material = db.Materials.Where(m => m.productID == materialId).FirstOrDefault();
                if (material != null)
                {
                    var course = db.Courses.Where(c => c.COURSEID == courseId).SingleOrDefault();
                    if (course != null)
                    {
                        course.MATERIALS = course.MATERIALS == string.Empty ? "~" + material.productID + "~" : course.MATERIALS + ",~" + material.productID + "~";
                        db.SaveChanges();
                    }
                    return new CourseMaterialsModel()
                    {
                        ProductId = material.productID,
                        ProductNumber = material.product_num,
                        ProductName = material.product_name,
                        Price = material.price,
                        ShippingCost = material.shipping_cost,
                        PriceIncluded = material.priceincluded,
                        Taxable = material.taxable,
                        ShippingWeight = material.shipping_weight,
                        Quantity = material.quantity
                    };
                }
                return new CourseMaterialsModel();
            }
        }

        public void SaveCourseChoice(int courseId, int choiceId)
        {
            using (var db = new SchoolEntities(connString))
            {
                var course = db.Courses.Where(c => c.COURSEID == courseId).SingleOrDefault();
                if (course != null)
                {
                    string query = "INSERT INTO [CoursesCourseChoices] (CourseId,CourseChoiceId) VALUES (" + courseId + ", " + choiceId + ")";
                    db.Database.ExecuteSqlCommand(query);
                }
            }
        }

        public void DeleteCourseChoice(int courseId, int choiceId)
        {
            using (var db = new SchoolEntities(connString))
            {
                var course = db.Courses.Where(c => c.COURSEID == courseId).SingleOrDefault();
                if (course != null)
                {
                    string query = "DELETE FROM [CoursesCourseChoices] WHERE CourseId = " + courseId + " AND CourseChoiceId = " + choiceId;
                    db.Database.ExecuteSqlCommand(query);
                }
            }
        }

        public void SaveCategories(Models.Courses.CourseCategoriesModel model)
        {
            using (var db = new SchoolEntities(connString))
            {
                if (!string.IsNullOrEmpty(model.MainCategoryName))
                {
                    int mainCategoryId = 0;
                    //delete
                    var mainCategory = db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName).FirstOrDefault();
                    db.SubCategories.RemoveRange(db.SubCategories.Where(sc => sc.MainCategoryID == mainCategory.MainCategoryID).ToList());
                    db.SubSubCategories.RemoveRange(db.SubSubCategories.Where(ssc => ssc.MainCategoryID == mainCategory.MainCategoryID).ToList());
                    db.MainCategories.RemoveRange(db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName).ToList());
                    db.SaveChanges();
                    MainCategory mainCategoryDB = new MainCategory()
                    {
                        MainCategory1 = model.MainCategoryName,
                        CourseID = model.CourseId,
                        MainOrder = 1,
                        ShowTopCatalog = 1,
                        SpecialCategoryType = 0,
                        mcatorder = 1
                    };
                    db.MainCategories.Add(mainCategoryDB);
                    db.SaveChanges();
                    mainCategoryId = mainCategoryDB.MainCategoryID;

                    if (!string.IsNullOrEmpty(model.SubCategoryName))
                    {
                        SubCategory subCategoryDB = new SubCategory()
                        {
                            MainCategoryID = mainCategoryId,
                            SubCategory1 = model.SubCategoryName
                        };
                        db.SubCategories.Add(subCategoryDB);
                        db.SaveChanges();
                    }
                    if (!string.IsNullOrEmpty(model.SubSubCategoryName))
                    {
                        SubSubCategory subSubCategoryDB = new SubSubCategory()
                        {
                            SubSubCategory1 = model.SubSubCategoryName,
                            MainCategoryID = mainCategoryId
                        };
                        db.SubSubCategories.Add(subSubCategoryDB);
                        db.SaveChanges();
                    }
                }
                if (!string.IsNullOrEmpty(model.MainCategoryName2))
                {
                    int mainCategoryId2 = 0;
                    //delete
                    var mainCategory2 = db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName2).FirstOrDefault();
                    db.SubCategories.RemoveRange(db.SubCategories.Where(sc => sc.MainCategoryID == mainCategory2.MainCategoryID).ToList());
                    db.SubSubCategories.RemoveRange(db.SubSubCategories.Where(ssc => ssc.MainCategoryID == mainCategory2.MainCategoryID).ToList());
                    db.MainCategories.RemoveRange(db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName2).ToList());
                    db.SaveChanges();
                    MainCategory mainCategoryDB2 = new MainCategory()
                    {
                        MainCategory1 = model.MainCategoryName2,
                        CourseID = model.CourseId,
                        MainOrder = 1,
                        ShowTopCatalog = 1,
                        SpecialCategoryType = 0,
                        mcatorder = 1
                    };
                    db.MainCategories.Add(mainCategoryDB2);
                    db.SaveChanges();
                    mainCategoryId2 = mainCategoryDB2.MainCategoryID;

                    if (!string.IsNullOrEmpty(model.SubCategoryName2))
                    {
                        SubCategory subCategoryDB2 = new SubCategory()
                        {
                            MainCategoryID = mainCategoryId2,
                            SubCategory1 = model.SubCategoryName2
                        };
                        db.SubCategories.Add(subCategoryDB2);
                        db.SaveChanges();
                    }
                    if (!string.IsNullOrEmpty(model.SubSubCategoryName2))
                    {
                        SubSubCategory subSubCategoryDB2 = new SubSubCategory()
                        {
                            MainCategoryID = mainCategoryId2,
                            SubSubCategory1 = model.SubSubCategoryName2
                        };
                        db.SubSubCategories.Add(subSubCategoryDB2);
                        db.SaveChanges();
                    }
                }

                if (!string.IsNullOrEmpty(model.MainCategoryName3))
                {
                    int mainCategoryId3 = 0;
                    //delete
                    var mainCategory3 = db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName3).FirstOrDefault();
                    db.SubCategories.RemoveRange(db.SubCategories.Where(sc => sc.MainCategoryID == mainCategory3.MainCategoryID).ToList());
                    db.SubSubCategories.RemoveRange(db.SubSubCategories.Where(ssc => ssc.MainCategoryID == mainCategory3.MainCategoryID).ToList());
                    db.MainCategories.RemoveRange(db.MainCategories.Where(mc => mc.CourseID == model.CourseId && mc.MainCategory1 == model.MainCategoryName3).ToList());
                    db.SaveChanges();
                    MainCategory mainCategoryDB3 = new MainCategory()
                    {
                        MainCategory1 = model.MainCategoryName3,
                        CourseID = model.CourseId,
                        MainOrder = 1,
                        ShowTopCatalog = 1,
                        SpecialCategoryType = 0,
                        mcatorder = 1
                    };
                    db.MainCategories.Add(mainCategoryDB3);
                    db.SaveChanges();
                    mainCategoryId3 = mainCategoryDB3.MainCategoryID;

                    if (!string.IsNullOrEmpty(model.SubCategoryName3))
                    {
                        SubCategory subCategoryDB3 = new SubCategory()
                        {
                            MainCategoryID = mainCategoryId3,
                            SubCategory1 = model.SubCategoryName3
                        };
                        db.SubCategories.Add(subCategoryDB3);
                        db.SaveChanges();
                    }
                    if (!string.IsNullOrEmpty(model.SubSubCategoryName2))
                    {
                        SubSubCategory subSubCategoryDB3 = new SubSubCategory()
                        {
                            MainCategoryID = mainCategoryId3,
                            SubSubCategory1 = model.SubSubCategoryName3
                        };
                        db.SubSubCategories.Add(subSubCategoryDB3);
                        db.SaveChanges();
                    }
                }
            }
        }

        public void SaveCoursePrice(CoursePricingMainModel priceModel)
        {
            using (var db = new SchoolEntities(connString))
            {
                //remove everything first
                db.CoursePricingOptions.RemoveRange(db.CoursePricingOptions.Where(cpo => cpo.CourseId == priceModel.CourseId).ToList());
                db.SaveChanges();

                if (priceModel.PublicCoursePricing.Count() > 0)
                {
                    foreach (var nonMemberPrice in priceModel.PublicCoursePricing)
                    {
                        CoursePricingOption coursePricingOption = new CoursePricingOption()
                        {
                            Price = nonMemberPrice.Price,
                            Type = nonMemberPrice.Type,
                            PricingOptionId = nonMemberPrice.Id,
                            rangestart = nonMemberPrice.RangeStart,
                            rangeend = nonMemberPrice.RangeEnd,
                            CourseId = priceModel.CourseId
                        };
                        db.CoursePricingOptions.Add(coursePricingOption);
                    }
                }

                if (priceModel.MemberCoursePricing != null && priceModel.MemberCoursePricing.Count() > 0)
                {
                    foreach (var memberPrice in priceModel.MemberCoursePricing)
                    {
                        CoursePricingOption coursePricingOption = new CoursePricingOption()
                        {
                            Price = memberPrice.Price,
                            Type = memberPrice.Type,
                            PricingOptionId = memberPrice.Id,
                            rangestart = memberPrice.RangeStart,
                            rangeend = memberPrice.RangeEnd,
                            CourseId = priceModel.CourseId
                        };
                        db.CoursePricingOptions.Add(coursePricingOption);
                    }
                }

                if (priceModel.MemberCoursePricing != null && priceModel.SpecialCoursePricing.Count() > 0)
                {
                    foreach (var specialPrice in priceModel.SpecialCoursePricing)
                    {
                        CoursePricingOption coursePricingOption = new CoursePricingOption()
                        {
                            Price = specialPrice.Price,
                            Type = specialPrice.Type,
                            PricingOptionId = specialPrice.Id,
                            rangestart = specialPrice.RangeStart,
                            rangeend = specialPrice.RangeEnd,
                            CourseId = priceModel.CourseId
                        };
                        db.CoursePricingOptions.Add(coursePricingOption);
                    }
                }
                db.SaveChanges();
            }
        }

        public Course Course(int courseId)
        {
            return _db.Courses.Where(c => c.COURSEID == courseId).SingleOrDefault();
        }

        public IEnumerable<Course_Time> CourseTime(int courseId)
        {
            return _db.Course_Times.Where(c => c.COURSEID == courseId).ToList();
        }

        public IEnumerable<Course_Time> CourseTimes(int courseId)
        {
            return courseTimes = Gsmu.Api.Data.School.Entities.Course.FixCourseTimesForOnlineCourse(this.Course(courseId), this.CourseTime(courseId));
        }

        public DateTime? CourseStartAsDate(int courseId)
        {
            if (this.CourseTimes(courseId).Count() == 0) {
                return new DateTime().Date;
            }
            var start = this.CourseTimes(courseId).First();
            DateTime date = new DateTime(start.COURSEDATE.Value.Year, start.COURSEDATE.Value.Month, start.COURSEDATE.Value.Day, start.STARTTIME.Value.Hour, start.STARTTIME.Value.Minute, start.STARTTIME.Value.Second);
            return date;

        }

        public DateTime? CourseEndAsDate(int courseId)
        {
            if (this.CourseTimes(courseId).Count() == 0)
            {
                return new DateTime().Date;
            }

            Course_Time end;
            if (this.CourseTimes(courseId).Count() == 1)
            {
                end = this.CourseTimes(courseId).First();
            }
            else
            {
                end = this.CourseTimes(courseId).Last();
            }
            DateTime date = new DateTime(end.COURSEDATE.Value.Year, end.COURSEDATE.Value.Month, end.COURSEDATE.Value.Day, end.FINISHTIME.Value.Hour, end.FINISHTIME.Value.Minute, end.FINISHTIME.Value.Second);
            return date;
        }

        public void SaveInstructorBio(int instructorId, string bio)
        {
            using (var db = new SchoolEntities(connString))
            {
                var instructor = db.Instructors.Where(i => i.INSTRUCTORID == instructorId).SingleOrDefault();
                if (instructor != null)
                {
                    instructor.Bio = bio;
                    db.SaveChanges();
                }
            }
        }
    }
}

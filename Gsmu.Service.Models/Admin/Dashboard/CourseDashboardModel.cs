using Gsmu.Service.Models.School;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gsmu.Service.Models.Admin.CourseDashboard
{
    public class CourseDashboardModel
    {

    }

    public class CourseConfigurationModel
    {
        public bool Showcredithours { get; set; }
        public bool Showinservice { get; set; }
        public bool Showcustomcredit { get; set; }
        public bool Showceuandgraduatecredit { get; set; }
        public bool Showoptionalcredit { get; set; }
        public string Creditname { get; set; }
        public string Creditinservicename { get; set; }
        public string Creditcustomname { get; set; }
        public string Creditoptionalname { get; set; }
        public string Creditoptionalname2 { get; set; }
        public string Creditoptionalname3 { get; set; }
        public string Creditoptionalname4 { get; set; }
        public string Creditoptionalname5 { get; set; }
        public string Creditoptionalname6 { get; set; }
        public string Creditoptionalname7 { get; set; }
        public string Creditoptionalname8 { get; set; }

        public string Pricemember { get; set; }
        public string Pricenonmember { get; set; }
        public string Pricespecial { get; set; }
        public string Blackboard { get; set; }
        public string Helius { get; set; }
        public string Survey { get; set; }
        public byte? Coursecategories { get; set; }
        public string Gradefield { get; set; }
        public string Schoolfield { get; set; }
        public string Districtfield { get; set; }
        public string Membershipfield { get; set; }
        public int? Maincatcontainer { get; set; }
        public string Onlineclasslabel { get; set; }
        public short Membership_status { get; set; }
        public byte? NoOfInstructors { get; set; }
        public short? CourseCloseDays { get; set; }
        public string Pricingoptionslabel { get; set; }
        public short Pricingvisible { get; set; }
        public short Pricingoptionoverride { get; set; }
        public int? Pricingoptrange { get; set; }
        public int? Alloweditingofcourseprice { get; set; }
        public bool Systemaccessliteon { get; set; }

        public string Courseconfirmation_email_max_upload { get; set; }

        public string Google_sso_client_id { get; set; }
        public string Google_sso_client_secret { get; set; }
        public int? Google_sso_enabled { get; set; }
        public string Google_sso_api_key { get; set; }
        public bool Google_drive_enabled { get; set; }
        public int? Roomnumberoption { get; set; }
        public int? Roomdirectionsoption { get; set; }
        public bool Hidelocationname { get; set; }
        public bool Publicterminology { get; set; }
        public string Publiclayoutconfiguration { get; set; }
        public string Dotnetsiterooturl { get; set; }
        public string Collectionstylelabel { get; set; }
        public int? Splitordersfeature { get; set; }
        public int? Allowpartialpayment { get; set; }
        public string Blackboard_course_roster_dsk { get; set; }
        public string Blackboard_courses_dsk { get; set; }
        public string Blackboard_instructors_dsk { get; set; }
        public string Blackboard_students_dsk { get; set; }
        public int Defaultpublicpricingtype { get; set; }
        public short Contactinfo { get; set; }
        public string Customcoursefieldlabel5 { get; set; }
        public int? Customcoursefieldshow5 { get; set; }

        public List<CourseCategoriesModel> Categories { get; set; }

        public string Pricingoptions { get; set; }

        public string Bbstart { get; set; }
        public string Bbend { get; set; }

        public string Surveys { get; set; }

        public List<AudienceModel> Audiences { get; set; }
        public List<IconsModel> Icons { get; set; }

        public short Depratmentrequired { get; set; }
        public List<DeparmentModel> Departments { get; set; }

        public List<CourseGroupingModel> CourseColorGrouping { get; set; }
        public List<CustomCertificateModel> CustomCertificates { get; set; }
    }

    public class CourseDescriptionModel //@TODO : Transfer to Courses Model on Courses Directory
    {
        public int CourseId { get; set; }
        public string CourseNumber { get; set; }
        public string CourseName { get; set; }

        public short CancelCourse { get; set; }
        public short InternalClass { get; set; }

        public decimal? DistPrice { get; set; }
        public decimal? NoDistPrice { get; set; }
        public string Location { get; set; }
        public string Locationurl { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        public string Room { get; set; }
        public int RoomDirectionsId { get; set; }
        public string LocationAdditionInfo { get; set; }

        public DateTime? Times { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxEnroll { get; set; }
        public int? MaxWait { get; set; }
        public int? InstructorId { get; set; }
        public int? InstructorId2 { get; set; }
        public int? InstructorId3 { get; set; }

        public string Description { get; set; }
        public string MainCategory { get; set; }
        public string MainCategory2 { get; set; }
        public string Maincategory3 { get; set; }
        public string SubCategory1 { get; set; }
        public string Subcategory2 { get; set; }
        public string SubCategory1b { get; set; }
        public string SubCategory2b { get; set; }
        public string SubCategory1c { get; set; }
        public string SubCategory2c { get; set; }

        public string Materials { get; set; }
        public int? Days { get; set; }
        public double? CreditHours { get; set; }

        public int? EmailReminderType { get; set; }
        public string EmailReminderSubject { get; set; }
        public string EmailReminderBody { get; set; }
        public string Notes { get; set; }
        public string CourseConfirmationEmailExtraText { get; set; }

        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ShortDescription { get; set; }
        public string MasterCourseId { get; set; }

        public int AllowCreditRollOver { get; set; }
        public int GradingSystem { get; set; }
        public int ? ShowCreditInPublic { get; set; }
        public float CustomCreditHours { get; set; } //clockhours
        public float InServiceHours { get; set; }
        public float CEUCredit { get; set; }
        public float GraduateCredit { get; set; }

        public int OnlineCourse { get; set; }
        public int CourseCloseDays { get; set; }
        public int ViewPastCoursesDays { get; set; }

        public int ? ShowPrerequisiteInfo { get; set; }
        public string PrerequisiteInfo { get; set; }
        public string AccessCode { get; set; }
        public decimal? SpecialPrice { get; set; }

        public int AudienceId { get; set; }
        public int DepartmentId { get; set; }
        public string Icons { get; set; }
        public int CourseColorGrouping { get; set; }
        public int CourseCertificationsId { get; set; }
        
        public string StartEndTimeDisplay { get; set; }
        public int AllowSendSurvey { get; set; }

        public string TileImageUrl { get; set; }
        //integration settings
        public int HaikuCourseId { get; set; }
        public string HaikuImportId { get; set; }
        public DateTime HaikuIntegrationDate { get; set; }
        public DateTime HaikuLastIntegrationDate { get; set; }
        public string HaikuLastResult { get; set; }
        public int DisableHaikuIntegration { get; set; }
        
        public int CanvasCourseId { get; set; }
        public int DisableCanvasIntegration { get; set; }

        public int BBServer { get; set; }
        public int BBAutoEnroll { get; set; }
        public int BBCourseCloned { get; set; }
        
        public string BBDescription { get; set; }
        public string BBLastIntegrationState { get; set; }
        public DateTime BBDateSyncInstructor { get; set; }
        public DateTime BBLastIntegrationDate { get; set; }
        public DateTime BBLastUpdateGrade { get; set; }

        public int MaterialsRequired { get; set; }
        public int CourseCertificate { get; set; }
        public int DisplayPrice { get; set; }
        public string PartialPaymentAmount { get; set; }
        public string PartialPaymentNon { get; set; }
        public string PartialPaymentSP { get; set; }

        public int SendConfirmationEmailtoInstructor { get; set; }
        public int NoRegEmail { get; set; }
        public int GoogleCalendarSyncEnabled { get; set; }
        //PHOTO realted
        public bool uploadPhoto { get; set; }
        public bool deletePhoto { get; set; }
    }

    public class AudienceModel {
        public int AudienceId { get; set; }
        public string AudienceName { get; set; }
    }

    public class IconsModel {
        public int IconId { get; set; }
        public string IconTitle { get; set; }
        public string IconImageUrl { get; set; }
    }

    public class DeparmentModel
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
    /// <summary>
    /// CourseCategory table - used on the Course Options > Identifiers > Course Grouping
    /// </summary>
    public class CourseGroupingModel
    {
        public int CourseCategoryID { get; set; }
        public string CourseCategoryColor { get; set; }
        public string CourseCategoryName { get; set; }

        public int UseColorBar { get; set; }
    }

    public class CourseExpensesModel
    {
        public int? CourseDisplayPosition { get; set; }
        public decimal? CourseExpenseAmount { get; set; }
        public int CourseExpenseId { get; set; }
        public string CourseExpenseTitle { get; set; }
        public int? CourseId { get; set; }
        public int InstructorDisplayPosition { get; set; }
        public decimal? InstructorFixedFee { get; set; }
        public int? InstructorId { get; set; }
        public decimal? InstructorPerStudent { get; set; }
        public float? InstructorRevenuePercentage { get; set; }
    }

    public class CoursePricingModel
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public int? RangeStart { get; set; }
        public int? RangeEnd { get; set; }
        public string Title { get; set; }
        public int Type { get; set; }
    }

    public class CoursePricingMainModel
    {
        public List<CoursePricingModel> PublicCoursePricing { get; set; }
        public List<CoursePricingModel> MemberCoursePricing { get; set; }
        public List<CoursePricingModel> SpecialCoursePricing { get; set; }
        public int CourseId { get; set; }
    }

    public class CourseRostersModel
    {
        public int Rosterid { get; set; }
        public DateTime RegisteredDate { get; set; }
        public string RegisteredDateString { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string School { get; set; }
        public string Status { get; set; }
        public int Cancel { get; set; }
        public int Waiting { get; set; }
    }

    public class CourseRostersMainModel
    {
        public List<CourseRostersModel> ActiveRosters { get; set; }
        public List<CourseRostersModel> WaitingRosters { get; set; }
        public List<CourseRostersModel> CancelledRosters { get; set; }
        public List<CourseRostersModel> AllRosters { get; set; }
    }

    public class CourseExtraParticipant
    {
        public int CourseExtraParticipantId { get; set; }
        public int? RosterId { get; set; }
        public string StudentFirst { get; set; }
        public string StudentLast { get; set; }
        public string StudentEmail { get; set; }
        public string CustomField2 { get; set; }
    }

    public class CourseRosterExtraModel
    {
        public List<CourseExtraParticipant> ActiveRostersExtra { get; set; }
        public List<CourseExtraParticipant> WaitingRostersExtra { get; set; }
        public List<CourseExtraParticipant> CancelledRostersExtra { get; set; }
    }

    public class CourseSurveyModel
    {
        public int SurveyID { get; set; }
        public int? BeforeCourseSurveyId { get; set; }
        public string Name { get; set; }
        public string BeforeName { get; set; }
    }

    public class CourseSurveyResultModel
    {
        public int SurveyID { get; set; }
        public string Name { get; set; }
    }

    public class CourseCategoriesModel : CategoriesModel { }
    public class CourseInstructorsModel : InstructorModel { }
    public class CourseMaterialsModel : MaterialModel { }
    public class CourseDateAndTimesModel : CourseDateTimeModel { }
    public class CourseTransciptsModel : TranscriptsModel { }
}

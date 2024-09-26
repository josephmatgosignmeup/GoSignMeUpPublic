using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Web.Configuration;
using System.Web;
using Gsmu.Api.Data.ViewModels;
using System.Web.Script.Serialization;

namespace Gsmu.Api.Data
{
    public static class WebConfiguration
    {
        public static string AdminUser
        {
            get
            {
                return ConfigurationManager.AppSettings["AdminUser"];
            }
        }

        public static string AdminPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["AdminPassword"];
            }
        }

        public static string V3AdminVirtualDirectory
        {
            get
            {
                return ConfigurationManager.AppSettings["V3AdminVirtualDirectory"];
            }
        }

        public static string NoProfileImageVirtualPath
        {
            get
            {
                return ConfigurationManager.AppSettings["NoProfileImageVirtualPath"];
            }
        }

        public static int ProfileImageMaxWidth
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["ProfileImageMaxWidth"]);
            }
        }

        public static int ProfileImageMaxHeight
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["ProfileImageMaxHeight"]);
            }
        }

        public static int ProfileImageWidgetWidth
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["ProfileImageWidgetWidth"]);
            }
        }

        public static int ProfileImageWidgetHeight
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["ProfileImageWidgetHeight"]);
            }
        }

        public static string SupervisorProfileImageVirtualDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings["SupervisorProfileImageVirtualDirectory"];
                if (string.IsNullOrEmpty(value))
                {
                    return "~/Images/SupervisorProfileImages/";
                }
                return value;
            }
        }

        public static string SupervisorProfileImageDirectoryAbsolutePath
        {
            get {
                return HttpContext.Current.Server.MapPath(SupervisorProfileImageVirtualDirectory);
            }
        }
        public static string InstructorProfileImageVirtualDirectory
        {
            get
            {
                return ConfigurationManager.AppSettings["InstructorfileImageVirtualDirectory"];
            }
        }


        public static string  InstructorImageDirectoryAbsolutePath
        {
            get
            {
                return HttpContext.Current.Server.MapPath(InstructorProfileImageVirtualDirectory);
            }
        }

        public static string CourseIconsVirtualDirectoryVirtual
        {
            get
            {
                return ConfigurationManager.AppSettings["CourseIconsDirectory"];
            }
        }

        public static string CourseIconsDirectoryAbsolutePath
        {
            get
            {
                return HttpContext.Current.Server.MapPath(CourseIconsVirtualDirectoryVirtual);
            }
        }

        public static string DocumentsFolder
        {
            get
            {
                return ConfigurationManager.AppSettings["DocumentsFolder"];
            }
        }

        public static string PublicCourseBrowseAscendingImage
        {
            get
            {
                return ConfigurationManager.AppSettings["PublicCourseBrowseAscendingImage"];
            }
        }

        public static string PublicCourseBrowseDescendingImage
        {
            get
            {
                return ConfigurationManager.AppSettings["PublicCourseBrowseDescendingImage"];
            }
        }

        public static string CartImageFull
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CartImageFull"];
                if(string.IsNullOrEmpty(value)){
                    return "Areas/Public/Images/Interface-Icons/trolley-full.png";
                }
                return value;
            }
        }

        public static string CartImageEmpty
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CartImageEmpty"];
                if(string.IsNullOrEmpty(value)){
                    return "Images/Icons/glyph2/Icons24x24/trolley.png";
                }
                return value;
            }
        }

        public static string CartButtonListIcon
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CartButtonListIcon"];
                if(string.IsNullOrEmpty(value)){
                    return "images/icons/famfamfam/cart.png";
                }
                return value;
            }
        }

        public static string CartButtonExpandedIcon
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CartButtonExpandedIcon"];
                if(string.IsNullOrEmpty(value)){
                    return "images/share/shopcartwhite.png";
                }
                return value;
            }
        }

        public static string GoogleMapsApiKey
        {
            get
            {
                string value = ConfigurationManager.AppSettings["GoogleMapsApiKey"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }

        public static string GoogleMapsUrl
        {
            get
            {
                string gsmuGoogleAPIKey = "AIzaSyC0xAyystvOBYRuOrX8N_lRCOEkMqLEJkw";
                if (!string.IsNullOrEmpty(GoogleMapsApiKey))
                {
                    return "https://maps.googleapis.com/maps/api/js?key=" + GoogleMapsApiKey + "&v=3&sensor=false";
                }
                return "https://maps.googleapis.com/maps/api/js?key=" + gsmuGoogleAPIKey + "&v=3&sensor=false";
            }
        }

        public static string PublicCourseAccessCodeImage
        {
            get
            {
                return ConfigurationManager.AppSettings["PublicCourseAccessCodeImage"];
            }
        }

        public static bool CourseSearchSingleView
        {
            get
            {
                return bool.Parse(ConfigurationManager.AppSettings["CourseSearchSingleView"]);
            }
        }

        public static string UserDashCourseGridDefaultView
        {
            get
            {
                string value = ConfigurationManager.AppSettings["UserDashCourseGridDefaultView"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }
        public static string UserDashCourseShowCourseNum
        {
            get
            {
                string value = ConfigurationManager.AppSettings["UserDashCourseShowCourseNum"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }
        
        public static bool ForceCssJssCompression
        {
            get
            {
                return bool.Parse(ConfigurationManager.AppSettings["ForceCssJssCompression"]);
            }
        }

        public static string LoadWidgetDashboard
        {
            get
            {
                try
                {
                    string value = ConfigurationManager.AppSettings["LoadWidgetDashboard"];
                    return value.ToLower();
                }
                catch
                {
                    return "all";
                }
            }
        }

        public static string ToolTipSearchCourseText
        {
            get
            {
                try
                {
                    string value = ConfigurationManager.AppSettings["ToolTipSearchCourseText"];
                    return value.ToLower();
                    //put value "enable" to run
                }
                catch
                {
                    return "disable";
                }
            }
        }

        public static ViewTemplateType DefaultCourseSearchView
        {
            get
            {
                ViewTemplateType value = (ViewTemplateType)Enum.Parse(typeof(ViewTemplateType), ConfigurationManager.AppSettings["DefaultCourseSearchView"]);
                return value;
            }
        }


        public static bool DevelopmentMode
        {
            get
            {
                var value = bool.Parse(ConfigurationManager.AppSettings["DevelopmentMode"]);
                return value || IsGsmuDevelopmentMachine;
            }
            set
            {
                SaveValue("DevelopmentMode", value.ToString());
            }
        }

        public static bool LogAuthorizeNetTransaction
        {
            get
            {
                var value = bool.Parse(ConfigurationManager.AppSettings["LogAuthorizeNetTransaction"]);
                return value;
            }
            set
            {
                SaveValue("LogAuthorizeNetTransaction", value.ToString());
            }
        }

        public static bool HideAdditionalCourseOffering
        {
            get
            {
                try
                {
                    var value = bool.Parse(ConfigurationManager.AppSettings["HideAdditionalCourseOffering"]);
                    return value;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool SiteIsEmbedded
        {
            get
            {
                try
                {
                    var value = bool.Parse(ConfigurationManager.AppSettings["SiteIsEmbedded"]);
                    return value;
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                SaveValue("SiteIsEmbedded", value.ToString());
            }

        }

        public static int LoggedInSessionTimeout
        {
            get
            {
                var value = int.Parse(ConfigurationManager.AppSettings["LoggedInSessionTimeout"]);
                return value;
            }
        }

        public static int LoggedOutSessionTimeout
        {
            get
            {
                var value = int.Parse(ConfigurationManager.AppSettings["LoggedOutSessionTimeout"]);
                return value;
            }
        }

        public static bool IsGsmuDevelopmentMachine
        {
            get
            {
                if (System.Web.HttpContext.Current != null)
                {
                    var context = System.Web.HttpContext.Current;
                    if (DevelopmentMachineList.Contains(context.Request.Url.DnsSafeHost))
                    {
                        return true;
                    }
                    return false;
                }
                return true;
            }
        }

        private static void SaveValue(string key, string value)
        {
            var config = WebConfigurationManager.OpenWebConfiguration("~");
            var appSettings = (AppSettingsSection)config.GetSection("appSettings");
            appSettings.Settings.Remove(key);
            appSettings.Settings.Add(key, value);
            config.Save();
        }

        public static string SessionCookieName
        {
            get
            {
                SessionStateSection sessionStateSection =
                  (System.Web.Configuration.SessionStateSection)
                  ConfigurationManager.GetSection("system.web/sessionState");

                string cookieName = sessionStateSection.CookieName;
                return cookieName;
            }
        }

        public static bool UseCyberSourceItemized
        {
            get
            {
                //try
                //{
                //    var value = bool.Parse(ConfigurationManager.AppSettings["UseCyberSourceItemized"]);
                //    return value;
                //}
                //catch
                //{
                //    return true;
                //}
                // we will not use itemized item because of computation issues. Discrepancy will show if list will have discount and multiple entries.

                return false;
            }
        }

        public static string[] DevelopmentMachineList
        {
            get
            {
                var value = ConfigurationManager.AppSettings["DevelopmentMachineList"];
                return value.Split(',');
            }
        }
        public static string AuditSiteAcitivityLevel
        {
            get
            {
                string AuditSiteAcitivityLevel =  ConfigurationManager.AppSettings["AuditSiteAcitivityLevel"];
                if (string.IsNullOrEmpty(AuditSiteAcitivityLevel))
                {
                    return "off";
                }
                else
                {
                    return AuditSiteAcitivityLevel;
                }
            }
        }

        public static string TouchNetIncludeUserEmailToAncillaryField
        {
            get
            {
                string TouchNetIncludeUserEmailToAncillaryField = ConfigurationManager.AppSettings["TouchNetIncludeUserEmailToAncillaryField"];
                if (string.IsNullOrEmpty(TouchNetIncludeUserEmailToAncillaryField))
                {
                    return "false";
                }
                else
                {
                    return TouchNetIncludeUserEmailToAncillaryField;
                }
            }
        }

        public static bool EnrollToWaitList {
            get {
                var json = new JavaScriptSerializer();
                string supervisorConfigurationValue = Settings.Instance.GetMasterInfo4().SupervisorConfiguration;
                if (supervisorConfigurationValue == "" || supervisorConfigurationValue == null)
                {
                    supervisorConfigurationValue = "{}";
                }
                dynamic supervisorConfigurationObject = json.Deserialize(supervisorConfigurationValue, typeof(object));
                int enrollToWaitList = 0;
                if (supervisorConfigurationObject.ContainsKey("enrollToWaitList"))
                {
                    enrollToWaitList = int.Parse(supervisorConfigurationObject["enrollToWaitList"]);
                }
                var value = enrollToWaitList > 0;  //Settings.Instance.GetMasterInfo4().EnrollToWaitList; //bool.Parse(ConfigurationManager.AppSettings["EnrollToWaitList"]);
                return value;
            }
        }
        public static string WaitListVerbiage
        {
            get
            {
                string WaitListVerbiage = ConfigurationManager.AppSettings["WaitListVerbiage"];
                if (string.IsNullOrEmpty(WaitListVerbiage))
                {
                    return "At Least One Class Is Wait Listed";
                }
                else
                {
                    return WaitListVerbiage;
                }
            }
        }

        public static bool AllowSupervisorToAddStudentOnCheckout
        {
            get
            {
                var json = new JavaScriptSerializer();
                string supervisorConfigurationValue = Settings.Instance.GetMasterInfo4().SupervisorConfiguration;
                if (supervisorConfigurationValue == "" || supervisorConfigurationValue == null)
                {
                    supervisorConfigurationValue = "{}";
                }
                dynamic supervisorConfigurationObject = json.Deserialize(supervisorConfigurationValue, typeof(object));
                int AllowSupervisorToAddStudentOnCheckout = 0;
                if (supervisorConfigurationObject.ContainsKey("AllowSupervisorToAddStudentOnCheckout"))
                {
                    AllowSupervisorToAddStudentOnCheckout = int.Parse(supervisorConfigurationObject["AllowSupervisorToAddStudentOnCheckout"]);
                }
                var value = AllowSupervisorToAddStudentOnCheckout > 0;  //Settings.Instance.GetMasterInfo4().EnrollToWaitList; //bool.Parse(ConfigurationManager.AppSettings["EnrollToWaitList"]);
                return value;
            }
        }
        public static string SystemPrefix
        {
            get
            {
                try
                {
                    string value = ConfigurationManager.AppSettings["SystemPrefix"];
                    return value;
                }
                catch
                {
                    return "";
                }
            }
        }
        public static int AnetReconcileLevel
        {
            get
            {
                try
                {
                    var value = int.Parse(ConfigurationManager.AppSettings["AnetReconcileLevel"]);
                    return value;
                }
                catch
                {
                    return 1;
                }
            }
        }
        public static string sendRequestUsernamePass2AdditionalEmail
        {
            get
            {
                try
                {
                    string value = ConfigurationManager.AppSettings["sendRequestUsernamePass2AdditionalEmail"];
                    return value;
                }
                catch
                {
                    return "";
                }
            }
        }
        
        public static string CanvasforceSyncInstructor
        {
            get
            {
                string value = ConfigurationManager.AppSettings["forceSyncInstructor"];
                if (string.IsNullOrEmpty(value) || value != "false")
                {
                    return "true";
                }
                return value;
            }
        }
        public static string AllowCreateNewAccountOverInactive
        {
            get
            {
                string value = ConfigurationManager.AppSettings["AllowCreateNewAccountOverInactive"];
                if (string.IsNullOrEmpty(value) || value != "false")
                {
                    return "true";
                }
                return value;
            }
        }
        
        public static string CanvasSetShortDescToCourseName
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasSetShortDescToCourseName"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }
        public static string CanvasManuallyPublishCourse
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasManuallyPublishCourse"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }
        public static string CanvasSyncOnPartialPayment
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasSyncOnPartialPayment"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }
        public static string CanvasSyncOnPendingPayment
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasSyncOnPendingPayment"];
                if (string.IsNullOrEmpty(value) || value != "true")
                {
                    return "false";
                }
                return value;
            }
        }

        public static string CanvasSyncUserAsStudentInGSMUdefault
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasSyncUserAsStudentInGSMUdefault"];
                if (string.IsNullOrEmpty(value) || value != "true")
                {
                    return "false";
                }
                return value;
            }
        }

        public static string CanvasSkipSyncUserAccount
        {
            get
            {
                string value = ConfigurationManager.AppSettings["CanvasSkipSyncUserAccount"];
                if (string.IsNullOrEmpty(value) || value != "false")
                {
                    return "true";
                }
                return value;
            }
        }

        public static string PreReqsPubView
        {
            get
            {
                string value = ConfigurationManager.AppSettings["PreReqsPubView"];
                if (string.IsNullOrEmpty(value))
                {
                    return "";
                }
                return value;
            }
        }

        public static string PreReqNotMetMsg
        {
            get
            {
                //string preReqNotMetMsg = ConfigurationManager.AppSettings["PreReqNotMetMsg"];
                //return preReqNotMetMsg;

                var json = new JavaScriptSerializer();
                string supervisorConfigurationValue = Settings.Instance.GetMasterInfo4().GlobalConfiguration;
                if (supervisorConfigurationValue == "" || supervisorConfigurationValue == null)
                {
                    supervisorConfigurationValue = "{}";
                }
                dynamic supervisorConfigurationObject = json.Deserialize(supervisorConfigurationValue, typeof(object));
                var preReqNotMetMsg = "";
                if (supervisorConfigurationObject.ContainsKey("prerequisitenotmet"))
                {
                    preReqNotMetMsg = supervisorConfigurationObject["prerequisitenotmet"];
                }
                return preReqNotMetMsg;
            }
        }

        public static bool GenerateUniqueICS
        {
            get
            {
                string value = ConfigurationManager.AppSettings["GenerateUniqueICS"];
                if (string.IsNullOrEmpty(value) || value.ToLower() == "false")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        public static bool RequiredReferrerCheck
        {
            get
            {
                string value = ConfigurationManager.AppSettings["RequiredReferrerCheck"];
                if (string.IsNullOrEmpty(value) || value.ToLower() == "false")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static int IsAdvance
        {
            get
            {
                return Settings.Instance.GetMasterInfo3().CoursePrerequisite.Value;
            }
        }

        public static bool IsDebugRelease
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
    }
}

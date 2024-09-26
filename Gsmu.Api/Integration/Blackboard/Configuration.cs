using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gsmu.Api.Data;
using System.Web;
using System.Collections.Specialized;
using Gsmu.Api.Data.School.Entities;
using Newtonsoft.Json;
using Gsmu.Api.Web;

namespace Gsmu.Api.Integration.Blackboard
{
    public class Configuration
    {
        [JsonIgnore]
        public static Configuration Instance
        {
            get
            {
                var instance = ObjectHelper.GetRequestObject<Configuration>(WebContextObject.BlackboardConfiguration);
                if (instance == null)
                {
                    instance = new Configuration();
                    ObjectHelper.SetRequestObject<Configuration>(WebContextObject.BlackboardConfiguration, instance);
                    return instance;
                }
                return instance;
            }
        }


        public bool BlackboardRealtimeStudentSyncEnabled
        {
            get
            {
                var value = Settings.Instance.GetMasterInfo3().bb_real_time_student_sync;
                return value == 1;
            }
        }

        public bool BlacboardSsoUserIntegrationEnabled
        {
            get
            {
                var value = Settings.Instance.GetMasterInfo3().BBSSOUserIntegrationEnabled;
                return value.HasValue && value.Value == 1;
            }
        }

        public bool BlackboardSsoEnabled
        {
            get
            {
                var value = Settings.Instance.GetMasterInfo3().BBSSOEnabled;
                return value.HasValue && value.Value == 1;
            }
        }

        public string BlackboardConnectionUrl
        {
            get
            {
                if(Settings.Instance.GetMasterInfo4().use_blackboard_api==1)
                        return Settings.Instance.GetMasterInfo4().blackboard_api_url;
                else
                        return Settings.Instance.GetMasterInfo2().BlackboardConnectionUrl;
            }
        }

        public string BlackboardInstructorRole
        {
            get
            {
                return Settings.Instance.GetMasterInfo3().BlackboardInstructorRole;
            }
        }

        public string BlackboardPortalSecRole
        {
            get
            {
                return Settings.Instance.GetMasterInfo3().BlackboardPortalSecRole;
            }
        }


        public NameValueCollection BlackboardStudentIntegrationFields
        {
            get
            {
                return HttpUtility.ParseQueryString(Settings.Instance.GetMasterInfo3().BBStudentIntegrationFields);
            }
        }

        public string StudentDsk
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_students_dsk;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_students_dsk = value;
            }
        }

        public string InstructorsDsk
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_instructors_dsk = value;
            }
        }

        public string CoursesDsk
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_courses_dsk;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_courses_dsk = value;
            }
        }

        public string CourseRosterDsk
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_course_roster_dsk;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_course_roster_dsk = value;
            }
        }

        public string CourseInstitutionalHierarchyNodeId
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_courses_node_id;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_courses_node_id = value;
            }
        }

        public string InstructorInstitutionalHierarchyNodeId
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_instructors_node_id;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_instructors_node_id = value;
            }
        }

        public string StudentInstitutionalHierarchyNodeId
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_students_node_id;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_students_node_id = value;
            }
        }

        public string BlackboardAccessToken
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_access_token;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_access_token = value;
            }
        }

        public DateTime BlackboardAccessTokenExpiry
        {
           get
           {
                var value = Settings.Instance.GetMasterInfo4().blackboard_token_expiry ?? DateTime.MinValue;
                    return value;
           }

        }

        public string BlackBoardSecurityKey
        {
            get
            {
                return Settings.Instance.GetMasterInfo4().blackboard_security_key;
            }
            set
            {
                Settings.Instance.GetMasterInfo4().blackboard_security_key = value;
            }
        }

        public string BlackBoardSecretKey
        {
            get
            {
                return Settings.Instance.GetMasterInfo3().BlackBoardSecretKey;
            }
            set
            {
                Settings.Instance.GetMasterInfo3().BlackBoardSecretKey = value;
            }
        }

        public bool BlackboardUseAPI
        {
            get
            {
                var BlackboardUseAPI =Settings.Instance.GetMasterInfo4().use_blackboard_api;
                if (BlackboardUseAPI != null)
                    return BlackboardUseAPI == 1;
                else
                    return false;

            }
        }

        public bool BlackBoardMembershipIntegrationEnabled
        {
            get
            {
                var BlackBoardMembershipIntegrationEnabled = Settings.Instance.GetMasterInfo3().BlackBoardMembershipIntegrationEnabled;
                if (BlackBoardMembershipIntegrationEnabled != null)
                {
                    return BlackBoardMembershipIntegrationEnabled == 1;
                }

                return false;
            }
        }

        public string BlackboardInstitutionalRole
        {
            get
            {
                var BlackboardInstitutionalRole = Settings.Instance.GetMasterInfo3().BlackboardPortalRole;
                if (BlackboardInstitutionalRole != null)
                    return BlackboardInstitutionalRole;
                else
                    return "";

            }
        }


        public string BlackboardSystemRole
        {
            get
            {
                var BlackboardSystemRole = System.Configuration.ConfigurationManager.AppSettings["BlackboardSystemRole"];
                if (BlackboardSystemRole != null)
                    return BlackboardSystemRole;
                else
                    return "";

            }
        }

        

        public void Save()
        {
            Settings.Instance.SaveChanges();
        }
    }
}

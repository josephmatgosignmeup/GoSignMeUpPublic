using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;
using System.Web;
using System.Security.Cryptography;
using Chilkat;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Web;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Commerce.ShoppingCart;
using Gsmu.Api.Data;
using Gsmu.Api.Integration.Blackboard;
using blackboard = Gsmu.Api.Integration.Blackboard;
using haiku = Gsmu.Api.Integration.Haiku;
using BlackBoardAPI;
using Gsmu.Api.Integration.Blackboard.API;
using static BlackBoardAPI.BlackBoardAPIModel;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Gsmu.Api.Authorization
{
    public static class AuthorizationHelper
    {
        public static AbstractSiteUser CurrentUser
        {
            get
            {
                var user = ObjectHelper.GetSessionObject<AbstractSiteUser>(WebContextObject.CurrentUser);
                if (user == null)
                {
                    return new GuestUser();
                }
                return user;
            }
            set
            {
                ObjectHelper.SetSessionObject<AbstractSiteUser>(WebContextObject.CurrentUser, value);
            }
        }



        public static Student CurrentStudentUser
        {
            get
            {
                var student = CurrentUser as Student;
                return student;
            }
        }

   
        public static adminpass CurrentAdminUser
        {
        get{
            var admin = CurrentUser as adminpass;
            return admin;
            }
        }
        public static Manager CurrentSubAdminUser
        {
            get
            {
                var subadmin = CurrentUser as Manager;
                return subadmin;
            }
        }
        public static Instructor CurrentInstructorUser
        {
            get
            {
                var instructor = CurrentUser as Instructor;
                return instructor;
            }
        }

        public static Supervisor CurrentSupervisorUser
        {
            get
            {
                return CurrentUser as Supervisor;
            }
        }

        public static List<string> LoginStudent(string username)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.USERNAME == username && s.InActive == 0 select s).FirstOrDefault();
                return CompleteStudentLogin(student);
            }
        }

        public static List<string> LoginStudentByBlackboardUUID(BBToken token)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.Blackboard_user_UUID == token.user_id && s.InActive == 0 select s).FirstOrDefault();

                if (student == null)
                {
                    Student stud = new Student();

                    BBUser bbuser = BlackboardAPIRequest.GetUserDetails(token);
                    stud.USERNAME = bbuser.userName;
                    stud.FIRST = bbuser.name.given;
                    stud.LAST = bbuser.name.family;
                    stud.Blackboard_user_UUID = token.user_id;
                    db.Students.Add(stud);
                    db.SaveChanges();

                    student = stud;

                }

                return CompleteStudentLogin(student);
            }
        }
        public static List<string> LoginStudentByEmail(string curEmailAddress)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.EMAIL == curEmailAddress && s.InActive == 0 select s).FirstOrDefault();
                return CompleteStudentLogin(student);
            }
        }

        public static List<string> LoginUserBySessionID(Guid? SessionID)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.UserSessionId == SessionID && s.InActive == 0 select s).FirstOrDefault();
                if (student != null)
                {
                    return CompleteStudentLogin(student);
                }
                else
                {
                    var supervisor = (from s in db.Supervisors where s.UserSessionId == SessionID select s).FirstOrDefault();
                    if (supervisor != null)
                    {
                        CurrentUser = supervisor;
                        return CompleteGeneralLogin();
                    }
                    else
                    {
                        var admin = (from i in db.adminpasses where i.UserSessionId == SessionID select i).FirstOrDefault();
                        return CompleteAdministratorLogin(admin);
                    }
                }
            }
        }
        public static List<string> LoginStudent(string username, string password)
        {
            string loginInfo = null;
            var LoginAuthOption = Settings.Instance.GetMasterInfo4().LoginAuthOption;
            Student student = null;
            using (var db = new SchoolEntities())
            {
                if (student == null && LoginAuthOption == 1)
                {
                    student = (from s in db.Students where s.EMAIL == username && s.STUDNUM == password && s.InActive == 0 select s).FirstOrDefault();
                }
                
                if (student == null && LoginAuthOption == 2)
                {
                    student = (from s in db.Students where (s.EMAIL == username || s.USERNAME == username) && s.STUDNUM == password && s.InActive == 0 select s).FirstOrDefault();
                }
                if (student == null && (Settings.Instance.GetMasterInfo2().LDAPOn == 1 || Settings.Instance.GetMasterInfo2().LDAPOn == 2))
                {
                    student = new Student();
                    student.USERNAME = username;
                    student.PASSWORD = password;
                    student = LDAPAuthentication.LdapConnProcess(student);
                    if (student != null)
                    {
                        student = (from s in db.Students where s.USERNAME == username && s.InActive == 0 select s).FirstOrDefault();
                    }
                }

                try
                {
                    if (student == null && haiku.Configuration.Instance.HaikuAuthenticationEnabled)
                    {
                        var config = haiku.Configuration.Instance;
                        var response = haiku.HaikuClient.Authenticate(username, password);
                        if (response.Authentication != null && response.Authentication.Success)
                        {
                            var haikuUserResponse = haiku.HaikuImport.GetUser(response.Authentication.UserId.Value);
                            var haikuUser = haikuUserResponse.Users.FirstUser;

                            if (config.HaikuUserSynchronizationEnabled || config.HaikuUserImportEnabled)
                            {
                                if (haikuUser.UserTypeAsEnum != haiku.Responses.Entities.UserType.Student)
                                {
                                    throw new Exception("Only Haiku students can login right now!");
                                }
                                student = haiku.HaikuImport.SynchronizeStudent(haikuUser);
                            }
                        }
                        if (student == null)
                        {
                            if (Settings.Instance.GetMasterInfo3().google_sso_enabled == 1)
                            {
                                student = (from s in db.Students where s.canvas_user_id == 0 && s.USERNAME == username && s.google_user == 1 && s.InActive == 0 select s).FirstOrDefault();
                            }
                        }
                    }

                    var bbConfig = Configuration.Instance;
                    if (student == null && bbConfig.BlackboardSsoEnabled)
                    {
                        var result = BlackboardSso.AuthenticateUser(username, password);
                        var authenticated = result.Item1;
                        var blackboardStudentDataCollection = result.Item2;
                        if (authenticated)
                        {
                            student = (from s in db.Students where s.USERNAME == username select s).FirstOrDefault();
                            if (student == null && bbConfig.BlacboardSsoUserIntegrationEnabled && bbConfig.BlackboardRealtimeStudentSyncEnabled)
                            {
                                student = BlackboardSso.InsertBlackboardStudent(username, blackboardStudentDataCollection);
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                  //  loginInfo = e.Message;
                }

                if (student == null)
                {
                    //only authenticate non Canvas user.
                    if (Gsmu.Api.Integration.Canvas.Configuration.Instance.DisableGSMUAuthIfUserInCanvas)
                    {
                        student = (from s in db.Students where s.canvas_user_id == 0 && s.USERNAME == username && s.STUDNUM == password && s.InActive == 0 select s).FirstOrDefault();
                    }
                    else
                    {
                        student = (from s in db.Students where s.USERNAME == username && s.STUDNUM == password && s.InActive == 0 select s).FirstOrDefault();
                    }
                }
                if (student != null)
                {
                    db.Entry(student).State = System.Data.Entity.EntityState.Detached;

                }
                db.SaveChanges();
                return CompleteStudentLogin(student, loginInfo);
            }
        }

        public static List<string> CompleteStudentLogin(Student student, string loginInfo = null)
        {
            if (student == null)
            {
                throw new Exception("Invalid username or password! Code: CST800");

            }

            //Comment out to prevent from logging in Inactive /Disable student
            /*else
            {
                if (student.InActive != 0)
                {
                    using (var db = new SchoolEntities())
                    {
                        var student_foractivation = (from s in db.Students where s.STUDENTID == student.STUDENTID select s).FirstOrDefault();
                        student_foractivation.InActive = 0;
                        db.SaveChanges();
                    }
                }
            }
             */
            if (student != null)
            {
                MembershipHelper.CheckMembershipExpiry(student);
            }
            CurrentUser = student;
            var messages = CourseShoppingCart.Instance.Validate();
            if (!string.IsNullOrWhiteSpace(loginInfo))
            {
                messages.Insert(0, loginInfo);
            }
            return CompleteGeneralLogin(messages);
        }

        public static void UpdateCurrentLoginStudent(int studentId)
        {
            using (var db = new SchoolEntities())
            {
                var student = (from s in db.Students where s.STUDENTID == studentId select s).FirstOrDefault();
                CurrentUser = student;
            }
        }

        public static void UpdateCurrentLoginSupervisor(int supervisorId)
        {
            using (var db = new SchoolEntities())
            {
                var supervisor = (from s in db.Supervisors where s.SUPERVISORID == supervisorId select s).FirstOrDefault();
                CurrentUser = supervisor;
            }
        }


        public static List<string> LoginInstructor(string username)
        {
            using (var db = new SchoolEntities())
            {
                var instructor = (from i in db.Instructors where i.USERNAME == username && i.DISABLED == 0 select i).FirstOrDefault();
                if (instructor != null)
                {
                    instructor.UserSessionId = Guid.NewGuid();
                    db.SaveChanges();
                }
                return CompleteInstructorLogin(instructor);
            }
        }

        public static List<string> LoginSupervisor(string username)
        {
            Supervisor supervisor = null;
            using (var db = new SchoolEntities())
            {
                supervisor = (from su in db.Supervisors where su.UserName == username && su.ACTIVE == 1 select su).FirstOrDefault();

                if (supervisor != null)
                {
                        CourseShoppingCart.Instance.Empty();
                        db.Entry(supervisor).State = System.Data.Entity.EntityState.Detached;
                        supervisor.UserSessionId = Guid.NewGuid();
                        db.SaveChanges();
                        CurrentUser = supervisor;
                        var messages = CourseShoppingCart.Instance.Validate();
                        return CompleteGeneralLogin(messages);

                }
                else
                {
                    throw new Exception("Invalid username or password! Code: LS800");
                }

            }
        }

        public static List<string> LoginInstructor(string username, string password)
        {
            var LoginAuthOption = Settings.Instance.GetMasterInfo4().LoginAuthOption;
            using (var db = new SchoolEntities())
            {
                if (LoginAuthOption == 1)
                {
                    var instructor = (from i in db.Instructors where i.EMAIL == username && i.PASSWORD == password && i.DISABLED == 0  select i).FirstOrDefault();
                    if (instructor != null)
                    {
                        instructor.UserSessionId = Guid.NewGuid();
                        db.SaveChanges();
                    }
                    return CompleteInstructorLogin(instructor);
                }
                else if (LoginAuthOption == 2)
                {
                    var instructor = (from i in db.Instructors where (i.EMAIL == username || i.USERNAME == username) && i.PASSWORD == password && i.DISABLED == 0 select i).FirstOrDefault();
                    if (instructor != null)
                    {
                        instructor.UserSessionId = Guid.NewGuid();
                        db.SaveChanges();
                    }
                    return CompleteInstructorLogin(instructor);
                }
                else
                {
                    var instructor = new Instructor();
                    //if (Settings.Instance.GetMasterInfo2().LDAPOn == 1 || Settings.Instance.GetMasterInfo2().LDAPOn == 2)
                    //{
                    //    instructor.USERNAME = username;
                    //    instructor.PASSWORD = password;
                    //    instructor = LDAPAuthentication.LdapConnProcess(instructor);
                    //    instructor = (from s in db.Students where s.USERNAME == username && s.STUDNUM == password select s).FirstOrDefault();
                    //}
                    //else
                    //{
                    instructor = (from i in db.Instructors where i.USERNAME == username && i.PASSWORD == password && i.DISABLED == 0 select i).FirstOrDefault();
                    //}
                    if (instructor != null)
                    {
                        instructor.UserSessionId = Guid.NewGuid();
                        db.SaveChanges();
                    }
                    return CompleteInstructorLogin(instructor);
                }
            }
        }

        public static List<string> CompleteInstructorLogin(Instructor instructor)
        {
            if (instructor == null)
            {
                throw new Exception("Invalid username or password! Code: CIL800");
            }
            CourseShoppingCart.Instance.Empty();
            CurrentUser = instructor;
            return CompleteGeneralLogin(null);
        }
        public static List<string> LoginAdministrator(string username, string password)
        {
            using (var db = new SchoolEntities())
            {

                   var admin = (from i in db.adminpasses where i.username == username && i.userpass == password select i).FirstOrDefault();

                   return CompleteAdministratorLogin(admin);
            }
        }
        public static List<string> CompleteAdministratorLogin(adminpass admin)
        {
            if (admin == null)
            {
                throw new Exception("Invalid username or password! Code: CAL800");
            }
            CourseShoppingCart.Instance.Empty();
            CurrentUser = admin;
            CompleteGeneralLogin(null);
            List<string> adminResponse = new List<string>();
            adminResponse.Add("UserType:"+AuthorizationHelper.CurrentUser.LoggedInUserType.ToString());
            return adminResponse;

        }
        public static List<string> CompleteSubAdministratorLogin(Manager subadmin)
        {
            if (subadmin == null)
            {
                throw new Exception("Invalid username or password! Code: CSAL800");
            }
            CourseShoppingCart.Instance.Empty();
            CurrentUser = subadmin;
            CompleteGeneralLogin(null);
            List<string> adminResponse = new List<string>();
            adminResponse.Add("UserType:" + AuthorizationHelper.CurrentUser.LoggedInUserType.ToString());
            return adminResponse;

        }
        public static List<string> ConfigurePortalAdministratorLogin(string username,string usersessionid)
        {
            using (var db = new SchoolEntities())
            {
                usersessionid = usersessionid.Replace("{", "").Replace("}", ""); //from admin usersessionid sometimes has bracket
                Guid sessionid = Guid.Parse(usersessionid);
                string admsessionid = sessionid.ToString();
                var admin = (from i in db.adminpasses where i.username == username && (i.UserSessionId == sessionid || i.UserSessionId.ToString() == admsessionid) select i).FirstOrDefault();
                if(admin == null && username == "admin")
                {
                    admin = (from i in db.adminpasses where (i.UserSessionId == sessionid || i.UserSessionId.ToString() == admsessionid) select i).FirstOrDefault();
                }
                if (admin != null)
                {
                    return CompleteAdministratorLogin(admin);
                }
                else
                {
                    var subadmin = (from i in db.Managers where i.MANAGERID == username & i.UserSessionId == sessionid select i).FirstOrDefault();
                    if (subadmin == null)
                    {
                        var instructor = (from instructor_ in db.Instructors where instructor_.USERNAME == username select instructor_).FirstOrDefault();
                        if (instructor != null)
                        {
                            subadmin = (from i in db.Managers where i.InstructorId == instructor.INSTRUCTORID & i.UserSessionId == sessionid select i).FirstOrDefault();
                        }
						else if (instructor == null && username == "admin")
						{
							subadmin = (from i in db.Managers where i.UserSessionId == sessionid select i).FirstOrDefault();
						}
                    }
                    return CompleteSubAdministratorLogin(subadmin);
                }
            }
        }
        private static List<string> CompleteGeneralLogin(List<string> messages = null)
        {
            HttpContext.Current.Session.Timeout = WebConfiguration.LoggedInSessionTimeout;

            if (CurrentUser != null)
            {
                switch (CurrentUser.LoggedInUserType)
                {
                    case LoggedInUserType.Student:
                        var student = AuthorizationHelper.CurrentStudentUser;
                        string studentSessionId = student.UpdateLastLogin();
                        //messages.Add("sessionId : " + studentSessionId); Remove the session id on warning message. Another variable must hold the sessionid.
                        if (Settings.Instance.GetMasterInfo2().AllowStudentMultiEnroll == 1)
                        {
                            CourseShoppingCart.Instance.MultipleOrder_PrincipalStudent = student.STUDENTID;
                            CourseShoppingCart.Instance.MultipleOrder_SelectedStudent = student.STUDENTID;
                            CourseShoppingCart.Instance.MultipleOrder_PrincipalStudentName = student.USERNAME + " " + student.FIRST + " " + student.LAST;
                        }
                        break;
                    case LoggedInUserType.Supervisor:
                        var curSupervisor = AuthorizationHelper.CurrentSupervisorUser;
                        curSupervisor.UpdateSupervisorLastLogin();
                        break;
                }
            }

            return messages;
        }

        public static List<string> Logout()
        {
            if (CurrentUser != null)
            {
                using (var db = new SchoolEntities())
                {
                    switch (CurrentUser.LoggedInUserType)
                    {
                        case LoggedInUserType.Supervisor:
                            var supervisor = (from sup in db.Supervisors where sup.SUPERVISORID == AuthorizationHelper.CurrentSupervisorUser.SUPERVISORID select sup).SingleOrDefault();
                            if (supervisor != null)
                            {
                                supervisor.UserSessionId = null;
                                db.SaveChanges();
                            }
                            break;
                        case LoggedInUserType.Instructor:
                            var instructor = (from ins in db.Instructors where ins.INSTRUCTORID == AuthorizationHelper.CurrentInstructorUser.INSTRUCTORID select ins).SingleOrDefault();
                            if (instructor != null)
                            {
                                instructor.UserSessionId = null;
                                db.SaveChanges();
                            }
                            break;
                        case LoggedInUserType.Student:
                            var student = (from ins in db.Students where ins.STUDENTID == AuthorizationHelper.CurrentStudentUser.STUDENTID select ins).SingleOrDefault();
                            if (student != null)
                            {
                                student.UserSessionId = null;
                                db.SaveChanges();
                            }
                            break;
                    }
                    
                }
            }
            var messages = new List<string>();
            LoggedInUserType loggedinType = CurrentUser.LoggedInUserType;
            CurrentUser = null;
            messages = CourseShoppingCart.Instance.Validate();
            Gsmu.Api.Authorization.AuthorizationHelper.VisibleInternalCourses = 0;
            return messages;
        }

      public static int VisibleInternalCourses
        {
            get;
            set;

        }

      internal static void UpdateCurrentUser(AbstractSiteUser user)
      {
          var currentUser = AuthorizationHelper.CurrentUser;
          if (currentUser != null && currentUser.LoggedInUserType == user.LoggedInUserType && currentUser.SiteUserId == user.SiteUserId)
          {
              CurrentUser = user;
          }
      }

      public static List<string> LoginSupervisor(string username, string password)
      {
          Supervisor supervisor = null;
          using (var db = new SchoolEntities())
          {
              supervisor = (from su in db.Supervisors where su.UserName == username && su.PASSWORD == password && su.ACTIVE==1 select su).FirstOrDefault();

              if (supervisor != null)
              {
                  if (supervisor.ACTIVE == 1)
                  {
                        try
                        {
                            CourseShoppingCart.Instance.Empty();
                            db.Entry(supervisor).State = System.Data.Entity.EntityState.Detached;
                            supervisor.UserSessionId =Guid.NewGuid();
                            db.SaveChanges();
                        }
                        catch
                        {
                            throw new Exception("Your account has invalid value");
                        }
                        CurrentUser = supervisor;
                            var messages = CourseShoppingCart.Instance.Validate();
                            return CompleteGeneralLogin(messages);
                        


                  }
                  else
                  {
                      throw new Exception("Your account is disabled!");
                  }
              }
              else
              {
                  throw new Exception("Invalid username or password! Code: LS800");
              }
          }
      }

      public static List<string> SetSupervisor(int userid)
      {
          Supervisor supervisor = null;
          using (var db = new SchoolEntities())
          {
              supervisor = (from su in db.Supervisors where su.SUPERVISORID == userid select su).FirstOrDefault();

              if (supervisor != null && AuthorizationHelper.CurrentAdminUser != null)
              {
                  if (supervisor.ACTIVE == 1)
                  {
                      CourseShoppingCart.Instance.Empty();
                      db.Entry(supervisor).State = System.Data.Entity.EntityState.Detached;
                      supervisor.UserSessionId = Guid.NewGuid();
                      db.SaveChanges();
                      CurrentUser = supervisor;
                      var messages = CourseShoppingCart.Instance.Validate();
                      return CompleteGeneralLogin(messages);

                  }
                  else
                  {
                      throw new Exception("Your account is disabled!");
                  }
              }
              else
              {
                  throw new Exception("Invalid ID!");
              }
          }
      }
      public static bool CheckValidUserSessionId()
      {
          using (var db = new SchoolEntities())
          {
              if (AuthorizationHelper.CurrentSupervisorUser != null)
              {
                  var supervisor = (from su in db.Supervisors where su.UserName==AuthorizationHelper.CurrentSupervisorUser.UserName select su).FirstOrDefault();
                  if (supervisor.UserSessionId == null)
                  {
                      return false;
                  }
              }
              else if (AuthorizationHelper.CurrentInstructorUser != null)
              {
                  var instructor = (from inst in db.Instructors where inst.USERNAME == AuthorizationHelper.CurrentInstructorUser.USERNAME select inst).FirstOrDefault();
                  if (instructor.UserSessionId == null)
                  {
                      return false;
                  }
              }
          }
          return true;
      }

      public static string Encryptsha256(string password)
      {

          System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();

          System.Text.StringBuilder hash = new System.Text.StringBuilder();
          
          byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
          
          foreach (byte theByte in crypto)
          {

              hash.Append(theByte.ToString("x2"));

          }

          return Convert.ToBase64String(crypto);

      }
      public static string RC2Encrytion(string s)
      {

            Chilkat.Crypt2 crypt = new Chilkat.Crypt2();
            bool success = crypt.UnlockComponent("GOSIGN.CB10517_PAm63yUk84pc");
            if (success != true)
            {
                return crypt.LastErrorText;
            }

            crypt.CryptAlgorithm = "rc2";
            crypt.CipherMode = "cbc";
            crypt.KeyLength = 128;
            crypt.Rc2EffectiveKeyLength = 128;
            crypt.PaddingScheme = 0;
            crypt.EncodingMode = "hex";
            string ivHex = "0001020304050607";
            crypt.SetEncodedIV(ivHex, "hex");
            string keyHex = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";
            crypt.SetEncodedKey(keyHex, "hex");
            string encStr = crypt.EncryptStringENC(s);

            return encStr;
      }

      public static string RC2Decryption(string s)
      {

            Chilkat.Crypt2 crypt = new Chilkat.Crypt2();
            bool success = crypt.UnlockComponent("GOSIGN.CB10517_PAm63yUk84pc");
            if (success != true)
            {
                return crypt.LastErrorText;
            }

            crypt.CryptAlgorithm = "rc2";
            crypt.CipherMode = "cbc";
            crypt.KeyLength = 128;
            crypt.Rc2EffectiveKeyLength = 128;
            crypt.PaddingScheme = 0;
            crypt.EncodingMode = "hex";
            string ivHex = "0001020304050607";
            crypt.SetEncodedIV(ivHex, "hex");
            string keyHex = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F";
            crypt.SetEncodedKey(keyHex, "hex");
            string decStr = crypt.DecryptStringENC(s);

            return decStr;
      }

        public static string getCurrentBBAccessToken()
        {
            DateTime TempExpiraDate = new DateTime(1900, 1, 1);
            string BlackBoardSecurityKey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey; // applicationid
            string BlackBoardSecretKey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey;  //secret key
            string BlackboardAccessToken = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardAccessToken;  //current access token
            DateTime BlackboardAccessTokenExpiry = Settings.Instance.GetMasterInfo4().blackboard_token_expiry ?? TempExpiraDate;  //secret key
            DateTime DatetimeNow = DateTime.Now;
            DateTime CurrTokenExp = BlackboardAccessTokenExpiry.AddHours(1);

            if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
            {
                //if (DateTime.Now >= BlackboardAccessTokenExpiry.AddHours(1))
                if (DateTime.Now >= BlackboardAccessTokenExpiry)
                {
                    BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                    BBToken BBToken = new BBToken();
                    BBToken = handelr.GenerateAccessToken(BlackBoardSecretKey, BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl);
                    var jsonToken = new JavaScriptSerializer().Serialize(BBToken);
                    BBToken CurBBtoken = new BBToken();
                    CurBBtoken = JsonConvert.DeserializeObject<BBToken>(jsonToken);
                    CurrTokenExp = DateTime.Now;
                    CurrTokenExp = CurrTokenExp.AddSeconds(Int32.Parse(CurBBtoken.expires_in) - 1);
                    Gsmu.Api.Data.Settings.Instance.SetMasterinfoValue(4, "blackboard_access_token", jsonToken);
                    Gsmu.Api.Data.Settings.Instance.SetMasterinfoValue(4, "blackboard_token_expiry", CurrTokenExp.ToString());

                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = "Authorization";
                    Audittrail.DetailDescription = "Blakcboard Token Gen";
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.CourseID = 0;
                    Audittrail.RoutineName = "AuthorizationHelper" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = "";
                    Audittrail.AuditAction = "Old Time: " + BlackboardAccessTokenExpiry + " - New Token:" + jsonToken;
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                    return jsonToken;
                }
                else {
                    Gsmu.Api.Data.School.Entities.AuditTrail Audittrail = new Gsmu.Api.Data.School.Entities.AuditTrail();
                    Audittrail.TableName = "Authorization";
                    Audittrail.DetailDescription = "Blakcboard Token reuse";
                    Audittrail.AuditDate = DateTime.Now;
                    Audittrail.CourseID = 0;
                    Audittrail.RoutineName = "AuthorizationHelper" + AuthorizationHelper.CurrentUser.LoggedInUserType;
                    Audittrail.UserName = "";
                    Audittrail.AuditAction = "Current Time: " + BlackboardAccessTokenExpiry + " - Current Token:" + BlackboardAccessToken;
                    Gsmu.Api.Logging.LogManagerDispossable LogManager = new Api.Logging.LogManagerDispossable();
                    LogManager.LogSiteActivity(Audittrail);
                    return BlackboardAccessToken;
                }
            }
            else
            {
                return "";
            }
        }
    }
}

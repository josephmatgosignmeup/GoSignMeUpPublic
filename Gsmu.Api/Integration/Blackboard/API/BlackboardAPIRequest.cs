using BlackBoardAPI;
using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static BlackBoardAPI.BlackBoardAPIModel;


namespace Gsmu.Api.Integration.Blackboard.API
{
    public static class BlackboardAPIRequest
    {
        public static  void GetBlackBoardCourses()
        {

           // Task.Run(async () => await BlackBoardAPIConnector.BlckBoardAPICall(""));

        }

        public static BBToken GetAuthenticatedUser(string code)
        {
            string application_key = Settings.Instance.GetMasterInfo4().blackboard_security_key;
            string secret_key = Settings.Instance.GetMasterInfo3().BlackBoardSecretKey;
            string return_url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl;
            string connection_url = Settings.Instance.GetMasterInfo4().blackboard_api_url;
            BBToken token =   Task.Run(async () => await BlackBoardAPIConnector.BlckBoardAPICall(code,secret_key,application_key,return_url,connection_url)).Result;
            return token;
        }


        public static BBUser GetUserDetails(BBToken token)
        {
            string application_key = Settings.Instance.GetMasterInfo4().blackboard_security_key;
            string secret_key = Settings.Instance.GetMasterInfo3().BlackBoardSecretKey;
            string return_url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl;
            string connection_url = Settings.Instance.GetMasterInfo4().blackboard_api_url;
            BBUser user = BlackBoardAPIConnector.BlckBoardAPICallGetUserDetails(connection_url,token.user_id,token);
            return user;
        }

        public static Student InsertStudentUserFromBlackboard(BBToken token)
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

                    return student;
               
            }
        }

        public static Instructor InsertInstructorUserFromBlackboard(BBToken token)
        {
            using (var db = new SchoolEntities())
            {
                   var faculty = (from s in db.Instructors where s.Blackboard_user_UUID == token.user_id select s).FirstOrDefault();

                    if (faculty == null)
                    {
                        Instructor stud = new Instructor();

                        BBUser bbuser = BlackboardAPIRequest.GetUserDetails(token);
                        stud.USERNAME = bbuser.userName;
                        stud.FIRST = bbuser.name.given;
                        stud.LAST = bbuser.name.family;
                        stud.Blackboard_user_UUID = token.user_id;
                        db.Instructors.Add(stud);
                        db.SaveChanges();

                    faculty = stud;

                    }

                    return faculty;
            }
        }
    }
}

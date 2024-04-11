using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using json = Newtonsoft.Json;
using http = System.Net.Http;
using school = Gsmu.Api.Data.School.Entities;
using entities = Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.Terminology;
using Gsmu.Api.Data.School.Entities;

namespace Gsmu.Api.Integration.Canvas
{
    public class CanvasImport
    {
        public static UserSynchronizationResponse SynchronizeUser(Int64 canvasUserId)
        {
            using (var db = new school.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;
                var CanvasConfig = Configuration.Instance;

                var userResponse = Clients.UserClient.GetUser(canvasUserId);
                var user = userResponse.User;
                var enrollmentResponse = Clients.EnrollmentClient.ListUserEnrollments(canvasUserId);
                var enrollments = enrollmentResponse.Enrollments;
                bool isTeacher = (from e in enrollments where e.Type == Entities.EnrollmenType.TeacherEnrollment select e).Count() > 0;
                bool isStudent = (from e in enrollments where e.Type == Entities.EnrollmenType.StudentEnrollment select e).Count() > 0;
                using (var dbx = new SchoolEntities())
                {
                    AuditTrail trail = new AuditTrail()
                    {
                        RoutineName = "CanvasCheck",
                        ShortDescription = "Update existing Account Mapped to Canvas",
                        UserName = user.ShortName,
                        DetailDescription = "Stack Trace: "+ isTeacher+"----->"+ isStudent,
                        AuditDate = System.DateTime.Now,
                        CourseID = 0,
                        StudentID = 0
                    };
                    dbx.AuditTrails.Add(trail);
                    dbx.SaveChanges();
                }
                entities.Instructor instructor = SynchronizeInstructor(user);
                entities.Student student = SynchronizeStudent(user);
                entities.Supervisor supervisor= SynchronizeSupervisor(user);

                //if (isTeacher)
                //{
                //    instructor = SynchronizeInstructor(user);
                //    if (CanvasConfig.enableTeacherLoginAsStudent == true)
                //    {
                //        student = SynchronizeStudent(user);
                //        isStudent = true;
                //    }
                //}
                //if (isStudent || !isTeacher)
                //{
                //    isStudent = true;
                //    student = SynchronizeStudent(user);
                //}

                UserSynchronizationResult result = UserSynchronizationResult.Error;
                
                if (!isTeacher && !isStudent)
                {
                    if (WebConfiguration.CanvasSyncUserAsStudentInGSMUdefault == "true")
                    {
                        result = UserSynchronizationResult.SynchronizedStudent;
                    }
                    else
                    { 
                        result = UserSynchronizationResult.DoesntHaveRole;
                    }
                    

                }
                else if (isTeacher && isStudent)  
                {
                    result = UserSynchronizationResult.SynchronizedAsStudentAndInstructor;
                }
                else if (isTeacher)
                {
                    result = UserSynchronizationResult.SynchronizedInstructor;
                    if (CanvasConfig.enableTeacherLoginAsStudent == true)
                    {
                        result = UserSynchronizationResult.SynchronizedStudent;
                        isStudent = true;
                        isTeacher = false;
                    }
                }
                else if (isStudent)
                {
                    result = UserSynchronizationResult.SynchronizedStudent;
                }

                var response = new UserSynchronizationResponse()
                {
                    SynchRonizationResult = result,
                    Student = student,
                    Instructor = instructor,
                    Supervisor = supervisor
                };
                return response;
            }
        }

        public static Tuple<string, string> ExtractCanvasUsersNameInFirstNameLastNameOrder(Entities.User user)
        {
            var name = user.SortableName ?? string.Empty;
            name = name.Replace(", ", ",");
            var names = name.Split(',');
            if (names.Length == 1)
            {
                return new Tuple<string, string>(name, null);
            }
            return new Tuple<string, string>(names.Last(), names.First());
        }

        public static string GetCanvasUsername(Entities.User user)
        {
            //return string.Format("canvas-{0}", user.Id);
            return string.Format("{0}", user.LoginId);
        }

        public static string GetCanvasPassword(Entities.User user)
        {
            return string.Format("canvas-{0}-@random238473", user.Id);
        }

        public static entities.Student SynchronizeStudent(Entities.User user)
        {
            Action<entities.Student> sync = delegate(entities.Student student)
            {
                var name = ExtractCanvasUsersNameInFirstNameLastNameOrder(user);
                student.FIRST = name.Item1;
                student.LAST = name.Item2;
                if (string.IsNullOrEmpty(student.EMAIL))
                {
                    // only update this field if account exist
                    student.DateAdded = DateTime.Now;
                }
                student.EMAIL = user.PrimaryEmail;
                if (string.IsNullOrEmpty(student.ADDRESS)) { student.ADDRESS = ""; }
                if (string.IsNullOrEmpty(student.CITY)) { student.CITY = ""; }
                if (string.IsNullOrEmpty(student.STATE)) { student.STATE = ""; }
                if (string.IsNullOrEmpty(student.ZIP)) { student.ZIP = ""; }
                if (string.IsNullOrEmpty(student.COUNTRY)) { student.COUNTRY = ""; }
                if (string.IsNullOrEmpty(student.HOMEPHONE)) { student.HOMEPHONE = ""; }
                if (string.IsNullOrEmpty(student.WORKPHONE)) { student.WORKPHONE = ""; }
                if (string.IsNullOrEmpty(student.StudRegField1)) { student.StudRegField1 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField2)) { student.StudRegField2 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField3)) { student.StudRegField3 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField4)) { student.StudRegField4 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField5)) { student.StudRegField5 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField6)) { student.StudRegField6 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField7)) { student.StudRegField7 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField8)) { student.StudRegField8 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField9)) { student.StudRegField9 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField10)) { student.StudRegField10 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField11)) { student.StudRegField11 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField12)) { student.StudRegField12 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField13)) { student.StudRegField13 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField14)) { student.StudRegField14 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField15)) { student.StudRegField15 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField16)) { student.StudRegField16 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField17)) { student.StudRegField17 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField18)) { student.StudRegField18 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField19)) { student.StudRegField19 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField20)) { student.StudRegField20 = ""; }
                if (string.IsNullOrEmpty(student.StudRegField20)) { student.StudRegField20 = ""; }
                student.LastUpdateTime = DateTime.Now;
                student.SAPLastPendingReason = "Passing from Canvas";
                student.AuthFromLDAP = 0;
                student.loginTally = 0;
                student.google_user = 0;


                if (string.IsNullOrWhiteSpace(student.USERNAME))
                {
                    student.USERNAME = GetCanvasUsername(user);
                }
                if (string.IsNullOrWhiteSpace(student.STUDNUM))
                {
                    student.STUDNUM = GetCanvasPassword(user);
                }
                student.canvas_user_id = Convert.ToInt64(user.Id.ToString());
            };

            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;

                var student = (from s in db.Students where s.canvas_user_id == user.Id select s).FirstOrDefault();
                if (student == null)
                {

                    student = new entities.Student();
                    sync(student);
                    student.SCHOOL = 0;
                    student.DISTRICT = 0;
                    student.GRADE = 0;
                    StudentHelper.RegisterStudent(student, db);
                }
                else
                {
                    sync(student);
                }
                db.SaveChanges();
                if (student.InActive == 1)
                {
                    student = null;
                }
                return student;
            }
        }

        public static entities.Instructor SynchronizeInstructor(Entities.User user)
        {
            Action<entities.Instructor> sync = delegate(entities.Instructor instructor)
            {
                var name = ExtractCanvasUsersNameInFirstNameLastNameOrder(user);
                instructor.FIRST = name.Item1;
                instructor.LAST = name.Item2;
                instructor.EMAIL = user.PrimaryEmail;
                if (string.IsNullOrWhiteSpace(instructor.USERNAME))
                {
                    instructor.USERNAME = GetCanvasUsername(user);
                }
                if (string.IsNullOrWhiteSpace(instructor.PASSWORD))
                {
                    instructor.PASSWORD = GetCanvasPassword(user);
                }

                instructor.canvas_user_id = Convert.ToInt64(user.Id.ToString());
            };

            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;
                
                var instructor = (from i in db.Instructors where i.canvas_user_id == user.Id && i.DISABLED==0 select i).FirstOrDefault();
                if (instructor == null)
                {
                    return instructor;
                    // db.Instructors.Add(instructor);
                }
                sync(instructor);
                db.SaveChanges();
                return instructor;
            }
            
        }



        public static entities.Supervisor SynchronizeSupervisor(Entities.User user)
        {
            Action<entities.Supervisor> sync = delegate (entities.Supervisor supervisor)
            {
                var name = ExtractCanvasUsersNameInFirstNameLastNameOrder(user);
                supervisor.FIRST = name.Item1;
                supervisor.LAST = name.Item2;
                supervisor.EMAIL = user.PrimaryEmail;
                if (string.IsNullOrWhiteSpace(supervisor.UserName))
                {
                    supervisor.UserName = GetCanvasUsername(user);
                }
                if (string.IsNullOrWhiteSpace(supervisor.PASSWORD))
                {
                    supervisor.PASSWORD = GetCanvasPassword(user);
                }

                supervisor.canvas_user_id = Convert.ToInt64(user.Id.ToString());
            };

            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;

                var supervisor = (from i in db.Supervisors where i.canvas_user_id == user.Id && i.ACTIVE==1 select i).FirstOrDefault();
                if (supervisor == null)
                {
                    return supervisor;
                    // db.Instructors.Add(instructor);
                }
                sync(supervisor);
                db.SaveChanges();
                return supervisor;
            }

        }

        public static CourseSynchronizationResponse SynchronizeCourse(int canvasCourseId, int isNewSectionCourse = 0)
        {
            var courseResponse = Clients.CourseClient.GetCourse(canvasCourseId);
            var course = courseResponse.Course;
            return SynchronizeCourse(course, isNewSectionCourse);
        }

        public static CourseSynchronizationResponse SynchronizeCourse(Entities.Course canvasCourse, int isNewSectionCourse = 0)
        {
            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;

                var gsmuCourse = (from c in db.Courses where c.canvas_course_id == canvasCourse.Id select c).FirstOrDefault();
                if (gsmuCourse == null || (isNewSectionCourse == 1 && Gsmu.Api.Integration.Canvas.Configuration.Instance.allowCanvasCourseSectionIntegration == true))
                {
                    gsmuCourse = new entities.Course(true);
                    if (!string.IsNullOrEmpty(canvasCourse.CourseCode))
                    {
                        if (canvasCourse.CourseCode.Length < 50)
                        {
                            gsmuCourse.COURSENUM = canvasCourse.CourseCode;
                        }
                        else
                        {
                            gsmuCourse.COURSENUM = canvasCourse.CourseCode.Substring(0, 49);
                        }
                    }
                    else
                    {
                        gsmuCourse.COURSENUM = "";
                    }

                    
                    gsmuCourse.OnlineCourse = 1;
                    //Added to add initial value for non-null fields.
                    gsmuCourse.CANCELCOURSE = 0;
                    gsmuCourse.InternalClass = 0;
                    gsmuCourse.DisplayPrice = 0;
                    gsmuCourse.AudienceID = 0;
                    gsmuCourse.GradingSystem = 0;
                    gsmuCourse.BuybackCourse = 0;
                    gsmuCourse.DepartmentNameID = 0;
                    gsmuCourse.InserviceHours = 0;
                    gsmuCourse.courseoutlineid = 0;
                    gsmuCourse.ShowTopCatalog = 0;
                    gsmuCourse.ShowBoldCatalog = 0;
                    gsmuCourse.AllowCreditRollover = 0;
                    gsmuCourse.Requirements = 0;
                    gsmuCourse.Duplicated = 0;
                    gsmuCourse.OfflineStudentCount = 0;
                    gsmuCourse.viewpastcoursesdays = 0;

                    gsmuCourse.DISTPRICE = 0;
                    gsmuCourse.NODISTPRICE = 0;
                    gsmuCourse.LOCATION = "";
                    gsmuCourse.MATERIALS = "";
                    gsmuCourse.Icons = "~|~~|~~|~";
                    gsmuCourse.ContactName = "";
                    gsmuCourse.ContactPhone = "";
                    gsmuCourse.coursecertificate = 0;
                    gsmuCourse.SingleCreditCost = 0;
                    gsmuCourse.disable_canvas_integration = 0;
                    gsmuCourse.PartialPaymentAmount = "0";
                    gsmuCourse.PartialPaymentNon = "0";
                    gsmuCourse.PartialPaymentSP = "0";
                    gsmuCourse.CustomCreditHours = 0;
                    gsmuCourse.LOCATIONURL = "";
                    gsmuCourse.LocationAdditionalInfo = "";
                    gsmuCourse.STREET = "";
                    gsmuCourse.CITY = "";
                    gsmuCourse.STATE = "";
                    gsmuCourse.ZIP = "";
                    gsmuCourse.ROOM = "";
                    gsmuCourse.CourseConfirmationEmailExtraText = "";
                    string AssignedAccountID = "0";
                    if (!string.IsNullOrEmpty(canvasCourse.AccountId.ToString())) {
                        AssignedAccountID = canvasCourse.AccountId.ToString();
                    }
                    gsmuCourse.CourseConfiguration = "{  \"purchasecredit\": \"0\",  \"creditoption\": \"0\",  \"canvasaccountid\": \"" + AssignedAccountID + "\",\"canvassectionID\":\"\", \"canvassectionName\": \"\" }";

                    //end
                    db.Courses.Add(gsmuCourse);
                    canvasCourse.GsmuSynchronizationStatus = SynchronizationStatus.Inserted;
                }
                else
                {
                    canvasCourse.GsmuSynchronizationStatus = SynchronizationStatus.Updated;
                }
                gsmuCourse.DESCRIPTION = canvasCourse.PublicDescription ?? string.Empty;
                gsmuCourse.COURSENAME = canvasCourse.Name;
                if (WebConfiguration.CanvasSetShortDescToCourseName == "yes")
                {
                    gsmuCourse.ShortDescription = canvasCourse.Name;
                }
                gsmuCourse.canvas_course_id = canvasCourse.Id;

                db.SaveChanges();

                if (canvasCourse.GsmuSynchronizationStatus == SynchronizationStatus.Inserted)
                {
                    var canvasmaincategory = new entities.MainCategory();
                    canvasmaincategory.CourseID = (short)gsmuCourse.COURSEID;
                    canvasmaincategory.MainCategory1 = Configuration.CanvasCourseCategory;
                    canvasmaincategory.mcatorder = 0;
                    db.MainCategories.Add(canvasmaincategory);
                    db.SaveChanges();

                    var canvassubcategory = new entities.SubCategory();
                    canvassubcategory.MainCategoryID = canvasmaincategory.MainCategoryID;
                    canvassubcategory.SubCategory1 = Configuration.CanvasCourseSubCategory;
                    db.SubCategories.Add(canvassubcategory);
                    db.SaveChanges();

                    if (canvasCourse.StartAt != null || canvasCourse.EndAt != null)
                    {
                        string timezone_string = string.Empty;
                        string icsvalidTZstring = string.Empty;
                        string timezonesAbv = string.Empty;
                        string timezonesT = string.Empty;
                        string TZDaylight = string.Empty;

                        int time_zone = Settings.Instance.GetMasterInfo3().system_timezone_hour.Value;
                        switch (time_zone)
                        {
                            case 1:
                                icsvalidTZstring = "Mountain Standard Time";
                                timezonesAbv = "MST";
                                timezonesT = "-0700";
                                TZDaylight = "-0600";
                                break;
                            case 2:
                                icsvalidTZstring = "Central Standard Time";
                                timezonesAbv = "CT";
                                timezonesT = "-0600";
                                TZDaylight = "-0500";
                                break;
                            case 3:
                                icsvalidTZstring = "Eastern Standard Time";
                                timezonesAbv = "ET";
                                timezonesT = "-0500";
                                TZDaylight = "-0400";
                                break;
                            //for approval not yet implemented 
                            case 4:
                                icsvalidTZstring = "US Mountain Standard Time";
                                timezonesAbv = "MT";
                                timezonesT = "-0700";
                                TZDaylight = "-0600";
                                break;
                            case 5:
                                icsvalidTZstring = "US Mountain Standard Time";
                                timezonesAbv = "MT";
                                timezonesT = "-0700";
                                TZDaylight = "-0700";
                                break;
                            case 6:
                                icsvalidTZstring = "Gulf Standard Time";
                                timezonesAbv = "UAE";
                                timezonesT = "+0400";
                                TZDaylight = "+0400";
                                break;
                            default:
                                icsvalidTZstring = "Pacific Standard Time";
                                timezonesAbv = "PT";
                                timezonesT = "-0800";
                                TZDaylight = "-0700";
                                break;
                        }
                        if (canvasCourse.StartAt != null)
                        {
                            TimeZoneInfo TimeZoneDiff = TimeZoneInfo.FindSystemTimeZoneById(icsvalidTZstring);
                            DateTime STARTTIME = new DateTime(canvasCourse.StartAt.Value.Year, canvasCourse.StartAt.Value.Month, canvasCourse.StartAt.Value.Day, canvasCourse.StartAt.Value.Hour, canvasCourse.StartAt.Value.Minute, canvasCourse.StartAt.Value.Second);
                            DateTime ConvertedStartTime = TimeZoneInfo.ConvertTimeFromUtc(STARTTIME, TimeZoneDiff);
                            var start = new entities.Course_Time();
                            start.COURSEDATE = canvasCourse.StartAt.Value;
                            start.STARTTIME = ConvertedStartTime;
                            start.FINISHTIME = start.STARTTIME;
                            gsmuCourse.Course_Times.Add(start);
                            db.SaveChanges();
                        }
                        if (canvasCourse.EndAt != null)
                        {
                            TimeZoneInfo TimeZoneDiff = TimeZoneInfo.FindSystemTimeZoneById(icsvalidTZstring);
                            DateTime ENDTIME = new DateTime(canvasCourse.EndAt.Value.Year, canvasCourse.EndAt.Value.Month, canvasCourse.EndAt.Value.Day, canvasCourse.EndAt.Value.Hour, canvasCourse.EndAt.Value.Minute, canvasCourse.EndAt.Value.Second);
                            DateTime ConvertedEndTime = TimeZoneInfo.ConvertTimeFromUtc(ENDTIME, TimeZoneDiff);
                            var end = new entities.Course_Time();
                            end.COURSEDATE = canvasCourse.EndAt.Value;
                            end.STARTTIME = ConvertedEndTime;
                            end.FINISHTIME = end.STARTTIME;
                            gsmuCourse.Course_Times.Add(end);
                            db.SaveChanges();
                        }
                        
                    }
                
                }

                return new CourseSynchronizationResponse()
                {
                     CanvasCourse = canvasCourse,
                     GsmuCourse = gsmuCourse
                };
            }

        }

        public static EnrollmentSynchronizationResponse SyncronizeCourseAndEnrollment(int canvasCourseId, int isNewCanvasSection =0 )
        {
            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;

                var config = Configuration.Instance;

                var courseSynchResponse = SynchronizeCourse(canvasCourseId, isNewCanvasSection);

                var enrollmentSynchResponse = new EnrollmentSynchronizationResponse(courseSynchResponse)
                {
                };

                var gsmuCourse = enrollmentSynchResponse.GsmuCourse;
                db.Courses.Attach(gsmuCourse);

                var enrollmentResponse = Clients.EnrollmentClient.ListCourseEnrollments(canvasCourseId,null,false,true);

                var enrollments = enrollmentResponse.Enrollments;

                enrollmentSynchResponse.Enrollments = enrollments;

                var instructorCount = 0;
                
                gsmuCourse.INSTRUCTORID = null;
                gsmuCourse.INSTRUCTORID2 = null;
                gsmuCourse.INSTRUCTORID3 = null;

                foreach (var enrollment in enrollments)
                {
                    var userResponse = Clients.UserClient.GetUser(enrollment.UserId);
                    var canvasUser = userResponse.User;
                    var syncType = config.CanvasEnrollmentToGsmuEntityMapping[enrollment.Type];
                    switch (syncType)
                    {
                        case GsmuEntityType.Student:
                            var student = SynchronizeStudent(canvasUser);

                            if(student!=null)
                                SynchronizeCourseRoster(enrollment, gsmuCourse, student);

                            break;

                        case GsmuEntityType.Instructor:
                            var instructor = SynchronizeInstructor(canvasUser);

                            instructorCount++;
                            switch (instructorCount)
                            {
                                case 1:
                                    gsmuCourse.INSTRUCTORID = instructor.INSTRUCTORID;
                                    break;

                                case 2:
                                    gsmuCourse.INSTRUCTORID2 = instructor.INSTRUCTORID;
                                    break;

                                case 3:
                                    gsmuCourse.INSTRUCTORID3 = instructor.INSTRUCTORID;
                                    break;
                            }
                            break;

                        default:
                            enrollmentSynchResponse.Errors.Add(
                                new Exception(
                                    string.Format("Unmapped Canvas Enrollment to Gsmu Entity Mapping: {0}", enrollment.Type.ToString())
                                )
                            );
                            break;
                    }
                }

                SynchronizeOrphanedRosters(enrollments, gsmuCourse, db);

                db.SaveChanges();

                return enrollmentSynchResponse;
            }
        }

        private static void SynchronizeOrphanedRosters(Entities.Enrollment[] canvasRosters, entities.Course gsmuCourse, entities.SchoolEntities db)
        {
            var gsmuRosters = (from cr in db.Course_Rosters where cr.COURSEID == gsmuCourse.COURSEID select cr);
            foreach (var roster in gsmuRosters)
            {
                if (roster.canvas_roster_id.HasValue && roster.canvas_roster_id > 0)
                {
                    var canvasId = roster.canvas_roster_id.Value;
                    var canvasRoster = (from cr in canvasRosters where cr.Id == canvasId select cr).FirstOrDefault();
                    if (canvasRoster == null)
                    {
                        roster.CancelRoster();
                    }
                }
            }
        }

        private static void SynchronizeCourseRoster(Entities.Enrollment enrollment, school.Course gsmuCourse, entities.Student gsmuStudent)
        {
            using (var db = new entities.SchoolEntities())
            {
                db.Configuration.LazyLoadingEnabled = false;
                db.Configuration.ProxyCreationEnabled = false;

                var gsmuRoster = (from cr in db.Course_Rosters where cr.canvas_roster_id == enrollment.Id select cr).FirstOrDefault();

                if (gsmuRoster == null)
                {
                    gsmuRoster = new entities.Course_Roster(true);
                    db.Course_Rosters.Add(gsmuRoster);
                }
                gsmuRoster.COURSEID = gsmuCourse.COURSEID;
                gsmuRoster.STUDENTID = gsmuStudent.STUDENTID;
                gsmuRoster.canvas_roster_id = enrollment.Id;
                gsmuRoster.OrderNumber = "I" + Gsmu.Api.Data.School.Student.EnrollmentFunction.CreateRandomKey();
                gsmuRoster.PaidInFull = -1;
                gsmuRoster.WaitOrder = 10000;
                gsmuRoster.CourseCost = "$0.00";
                gsmuRoster.HOURS = 0;
                gsmuRoster.TotalPaid = 0;
                gsmuRoster.InserviceHours = 0;
                gsmuRoster.CustomCreditHours = 0;
                gsmuRoster.graduatecredit = 0;
                gsmuRoster.ceucredit = 0;
                gsmuRoster.Optionalcredithours1 = 0;
                gsmuRoster.PricingOption = "n/a";
                gsmuRoster.PricingMember = 0;
                gsmuRoster.CourseHoursType = "NONE";
                gsmuRoster.CouponCode = "";
                gsmuRoster.CouponDiscount = 0;
                gsmuRoster.EnrollMaster = gsmuStudent.STUDENTID;
                gsmuRoster.SubSiteId = 0;
                gsmuRoster.RosterFrom = 2;
                gsmuRoster.CourseSurveySent = 0;
                gsmuRoster.sendSurvey = 0;
                gsmuRoster.CRPrimaryTotalPaid = 0;
                gsmuRoster.BBCancelled = 0;
                gsmuRoster.creditcardfee = 0;
                gsmuRoster.paidremainderamount = 0;
                gsmuRoster.eventid = 0;
                gsmuRoster.HasPartialPayment = 0;
                gsmuRoster.FTCourseRosterId = 0;
                gsmuRoster.StudentChoiceCourse = 0;
                gsmuRoster.IsHoursPaid = 0;
                gsmuRoster.SingleRosterDiscountAmount = 0;
                gsmuRoster.AttendanceStatusId = 0;
                gsmuRoster.Parking = 0;
                gsmuRoster.CheckoutComments = "";
                gsmuRoster.CheckoutComments2 = "";
                gsmuRoster.ActivateRoster();

                db.SaveChanges();
            }
        }
        public static Entities.Enrollment[] getCourseGrade(Int32 canvascourseid, entities.SchoolEntities db = null)
        {
            var canvasEnrollmentResponse = Canvas.Clients.EnrollmentClient.ListCourseEnrollments(canvascourseid);
            Entities.Enrollment[] enrollments = canvasEnrollmentResponse.Enrollments;
            return null;
        }
        public static System.Web.Mvc.ActionResult SynchronizeLtiUser(System.Web.Mvc.ControllerBase controller)
        {
            var request = controller.ControllerContext.HttpContext.Request;
            var canvasUserId = Convert.ToInt64(request.Form["custom_canvas_user_id"]);
            var response = SynchronizeUser(canvasUserId);

            return UserSynchornizationResultHandling(controller, response);
        }

        public static System.Web.Mvc.ActionResult UserSynchornizationResultHandling(System.Web.Mvc.ControllerBase controller, UserSynchronizationResponse synchronizationResult, string reason = null)
        {
            if (synchronizationResult.SynchRonizationResult == UserSynchronizationResult.Error)
            {
                Gsmu.Api.Web.ObjectHelper.AddRequestMessage(controller, "There was an error synchronizing you student, please contact you educational institution or GSMU support.");
            }

            if ((synchronizationResult.Instructor != null && synchronizationResult.Student != null && synchronizationResult.Supervisor != null))
            {
                var url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl + "SSO/SetCanvasUser?instructor=" + synchronizationResult.Instructor.USERNAME + "&student=" + synchronizationResult.Student.USERNAME + "&supervisor=" + synchronizationResult.Supervisor.UserName;
                return new System.Web.Mvc.RedirectResult(url);
            }
            else if (synchronizationResult.Instructor != null && synchronizationResult.Supervisor != null)
            {
                var url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl + "SSO/SetCanvasUser?instructor=" + synchronizationResult.Instructor.USERNAME + "&student= &supervisor=" + synchronizationResult.Supervisor.UserName;
                return new System.Web.Mvc.RedirectResult(url);
            }
            else if (synchronizationResult.Instructor != null && synchronizationResult.Student != null)
            {
                var url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl + "SSO/SetCanvasUser?instructor=" + synchronizationResult.Instructor.USERNAME + "&student=" + synchronizationResult.Student.USERNAME + "&supervisor=" ;
                return new System.Web.Mvc.RedirectResult(url);
            }
            else if (synchronizationResult.Supervisor != null && synchronizationResult.Student != null)
            {
                var url = Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl + "SSO/SetCanvasUser?instructor=&student=" + synchronizationResult.Student.USERNAME + "&supervisor=" + synchronizationResult.Supervisor.UserName;
                return new System.Web.Mvc.RedirectResult(url);
            }
            else if (synchronizationResult.Instructor != null)
            {
                Authorization.AuthorizationHelper.LoginInstructor(synchronizationResult.Instructor.USERNAME);
                var url = "/public/instructor?action=login&canvas-id=" + controller.ControllerContext.HttpContext.Session.SessionID;
                return new System.Web.Mvc.RedirectResult(url);
            }
            else if (synchronizationResult.Supervisor != null)
            {
                Authorization.AuthorizationHelper.LoginInstructor(synchronizationResult.Supervisor.UserName);
                var url = "/public/supervisor?action=login&canvas-id=" + controller.ControllerContext.HttpContext.Session.SessionID;
                return new System.Web.Mvc.RedirectResult(url);
            }
            if (reason == null || reason == "instructor")
            {
                if (synchronizationResult.SynchRonizationResult == UserSynchronizationResult.SynchronizedInstructor || synchronizationResult.SynchRonizationResult == UserSynchronizationResult.SynchronizedAsStudentAndInstructor)
                {
                    Authorization.AuthorizationHelper.LoginInstructor(synchronizationResult.Instructor.USERNAME);
                    //document.location = Settings self.AspSiteRootUrl + '';
                    var url = "/public/instructor?action=login&canvas-id=" + controller.ControllerContext.HttpContext.Session.SessionID;
                    return new System.Web.Mvc.RedirectResult(url);
                }

            }
            
            if (reason == null || reason == "student")
            {
                if (synchronizationResult.SynchRonizationResult == UserSynchronizationResult.SynchronizedStudent || synchronizationResult.SynchRonizationResult == UserSynchronizationResult.SynchronizedAsStudentAndInstructor)
                {
                    var messages = Authorization.AuthorizationHelper.LoginStudent(synchronizationResult.Student.USERNAME);
                    Gsmu.Api.Web.ObjectHelper.AddRequestMessages(controller, messages);
                    Gsmu.Api.Web.ObjectHelper.AddRequestMessage(controller, "Welcome Canvas user! You have been logged into GSMU.");
                }
                else
                {
                    Gsmu.Api.Web.ObjectHelper.AddRequestMessage(controller, "Sorry, but your user does not have the Student role in Canvas, so you cannot login as a student into GSMU.");

                }
            }

            return null;
        }
    }
}

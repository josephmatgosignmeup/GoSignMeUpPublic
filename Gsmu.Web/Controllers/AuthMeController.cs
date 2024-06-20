using Gsmu.Api.Authorization;
using Gsmu.Api.Data;
using Gsmu.Api.Data.School.Entities;
using Gsmu.Api.Data.School.Student;
using Gsmu.Api.Data.School.User;
using Gsmu.Api.Integration.Shibboleth;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Gsmu.Api.Data.School;
using Gsmu.Api.Data.School.Supervisor;
using System.IO;
using Gsmu.Api.Data.Survey;
using System.Web.Script.Serialization;
using Gsmu.Api.Data.School.InstructorHelper;
using canvas = Gsmu.Api.Integration.Canvas;
using Gsmu.Web.Areas.Public;
using Gsmu.Api.Commerce.ShoppingCart;
using Gsmu.Api.Integration.Blackboard.API;

using System.Text;
using static BlackBoardAPI.BlackBoardAPIModel;

namespace Gsmu.Web.Controllers
{
    public class AuthMeController : Controller
    {
        //
        // GET: /AuthMe/
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult SelectShibUser()
        {
            ViewBag.ShowInstructorIcon = "none";
            ViewBag.ShowStudentIcon = "none";
            ViewBag.ShowSupervisorIcon = "none";
            if (Session["ShibbolethStudent"] != null)
            {
                ViewBag.ShowStudentIcon = "inline";
            }
            if (Session["ShibbolethSupervisor"] != null)
            {
                ViewBag.ShowSupervisorIcon = "inline";
            }
            if (Session["ShibbolethInstructor"] != null)
            {
                ViewBag.ShowInstructorIcon = "inline";
            }
            return View();
        }

        public ActionResult SelectBBUser()
        {
            ViewBag.ShowInstructorIcon = "none";
            ViewBag.ShowStudentIcon = "none";
            ViewBag.ShowSupervisorIcon = "none";
            if (Session["BBStudent"] != null)
            {
                ViewBag.ShowStudentIcon = "inline";
            }
            if (Session["BBSupervisor"] != null)
            {
                ViewBag.ShowSupervisorIcon = "inline";
            }
            if (Session["BBInstructor"] != null)
            {
                ViewBag.ShowInstructorIcon = "inline";
            }
            return View();
        }


        public string LoginBBUser(string type)
        {
            string result = "";
              if (type == "ST")
            {
                Student ShibbolethStudent = (Student)Session["BBStudent"];
                AuthorizationHelper.LoginStudent(ShibbolethStudent.USERNAME);
                if (CourseShoppingCart.Instance.SelectedCourseID != null && CourseShoppingCart.Instance.SelectedCourseID != 0)
                {
                    result = "Public/Course/Browse?courseid=" + CourseShoppingCart.Instance.SelectedCourseID;
                }
                else
                {
                    if (Settings.Instance.GetMasterInfo3().StrtupPage == 0)
                    {
                        result = "Public/User/Dashboard";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 1)
                    {
                        result = "Public/Course/Browse";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 2)
                    {
                    }
                    else
                    {
                        result = "Public/Course/Browse";
                    }
                }
            }
            else if (type == "IN")
            {
                Instructor ShibbolethInstructor = (Instructor)Session["BBInstructor"];
                AuthorizationHelper.LoginInstructor(ShibbolethInstructor.USERNAME);
                if (Settings.Instance.GetMasterInfo3().StrtupPage == 0)
                {
                    result = "Public/Instructor";
                }
                else if (Settings.Instance.GetMasterInfo3().StrtupPage == 1)
                {
                    result = "Public/Course/Browse";
                }
                else if (Settings.Instance.GetMasterInfo3().StrtupPage == 2)
                {
                }
                else
                {
                    result = "Public/Course/Browse";
                }
            }
            else
            {
                result = "Public/Course/Browse";
            }
            Session["BBStudent"] = null;
            Session["BBSupervisor"] = null;
            Session["BBInstructor"] = null;
            try
            {
                if (SurveyInfo.Instance.studentid != 0)
                {
                    result = "/public/survey/showsurvey?studid=" + SurveyInfo.Instance.studentid + "&sid=" + SurveyInfo.Instance.surveyid + "&cid=" + SurveyInfo.Instance.courseid + "&misc=11:34:05%20AM";
                }
            }
            catch { }
            return result;
        }
        public string LoginShibbolethUser(string type)
        {
            string result = "";
            if (type == "SP")
            {
                Supervisor ShibbolethSupervisor = (Supervisor)Session["ShibbolethSupervisor"];
                AuthorizationHelper.LoginSupervisor(ShibbolethSupervisor.UserName, ShibbolethSupervisor.PASSWORD);
                if (CourseShoppingCart.Instance.SelectedCourseID != null && CourseShoppingCart.Instance.SelectedCourseID != 0)
                {
                    result = "Public/Course/Browse?courseid=" + CourseShoppingCart.Instance.SelectedCourseID;
                }
                else
                {
                    if (Settings.Instance.GetMasterInfo3().StrtupPage == 0)
                    {
                        result = "Public/Supervisor";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 1)
                    {
                        result = "Public/Course/Browse";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 2)
                    {
                    }
                    else
                    {
                        result = "Public/Course/Browse";
                    }
                }
            }
            else if (type == "ST")
            {
                Student ShibbolethStudent = (Student)Session["ShibbolethStudent"];
                AuthorizationHelper.LoginStudent(ShibbolethStudent.USERNAME);
                if (CourseShoppingCart.Instance.SelectedCourseID != null && CourseShoppingCart.Instance.SelectedCourseID != 0)
                {
                    result = "Public/Course/Browse?courseid=" + CourseShoppingCart.Instance.SelectedCourseID;
                }
                else
                {
                    if (Settings.Instance.GetMasterInfo3().StrtupPage == 0)
                    {
                        result = "Public/User/Dashboard";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 1)
                    {
                        result = "Public/Course/Browse";
                    }
                    else if (Settings.Instance.GetMasterInfo3().StrtupPage == 2)
                    {
                    }
                    else
                    {
                        result = "Public/Course/Browse";
                    }
                }
            }
            else if (type == "IN")
            {
                Instructor ShibbolethInstructor = (Instructor)Session["ShibbolethInstructor"];
                AuthorizationHelper.LoginInstructor(ShibbolethInstructor.USERNAME);
                if (Settings.Instance.GetMasterInfo3().StrtupPage == 0)
                {
                    result = "Public/Instructor";
                }
                else if (Settings.Instance.GetMasterInfo3().StrtupPage == 1)
                {
                    result = "Public/Course/Browse";
                }
                else if (Settings.Instance.GetMasterInfo3().StrtupPage == 2)
                {
                }
                else
                {
                    result = "Public/Course/Browse";
                }
            }
            else
            {
                result = "Public/Course/Browse";
            }
            Session["ShibbolethStudent"] = null;
            Session["ShibbolethSupervisor"] = null;
            Session["ShibbolethInstructor"] = null;
            try
            {
                if (SurveyInfo.Instance.studentid != 0)
                {
                    result = "/public/survey/showsurvey?studid=" + SurveyInfo.Instance.studentid + "&sid=" + SurveyInfo.Instance.surveyid + "&cid=" + SurveyInfo.Instance.courseid + "&misc=11:34:05%20AM";
                }
            }
            catch { }
            return result;
        }
        public ActionResult ShibbolethAuthentication()
        {
            var TurnOnDebugTracingMode = System.Configuration.ConfigurationManager.AppSettings["TurnOnDebugTracingMode"];
            if (string.IsNullOrEmpty(TurnOnDebugTracingMode))
            {
                TurnOnDebugTracingMode = "Off";
            }
            bool login_shib_sso_gsmuactive = false;
            bool login_shib_sso_gsmuonly = false;
            bool allow_user_tologin_base_on_department = true; // initially set it to true. for this not to interupt old implementation.
            string shib_allowed_departments = "";
            string loginErrorMessage = "";
            int GSMUUniqueIdentifierIndex = 0;
            if (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField != "username" && canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField != "")
            {
                switch (canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField)
                {
                    case "studregfield1":
                        GSMUUniqueIdentifierIndex = 1;
                        break;
                    case "studregfield2":
                        GSMUUniqueIdentifierIndex = 2;
                        break;
                    case "studregfield3":
                        GSMUUniqueIdentifierIndex = 3;
                        break;
                    case "studregfield4":
                        GSMUUniqueIdentifierIndex = 4;
                        break;
                    case "studregfield5":
                        GSMUUniqueIdentifierIndex = 5;
                        break;
                }

            }
            ShibbolethConfiguration shibbolethConfiguration = GetShibbolethConfiguration(Settings.Instance.GetMasterInfo4().ShibbolethConfiguration);
            if (shibbolethConfiguration != null)
            {
                if (shibbolethConfiguration.login_shib_sso_gsmuactive == 1)
                {
                    login_shib_sso_gsmuactive = true;
                }
                if (shibbolethConfiguration.login_shib_sso_gsmuonly == 1)
                {
                    login_shib_sso_gsmuonly = true;
                }

                if (shibbolethConfiguration.allowed_departments != "")
                {
                    shib_allowed_departments = shibbolethConfiguration.allowed_departments;
                }
            }
            var shibmap_sessionid = ShibbolethHelper.GetShibbolethUserNameField("SessionId");
            int? shibboleth_sso_enabled = Settings.Instance.GetMasterInfo4().shibboleth_sso_enabled;
            if ((shibmap_sessionid == "") || (shibmap_sessionid == null))
            {
                shibmap_sessionid = "HTTP_SHIBSESSIONID";
            }
            if (TurnOnDebugTracingMode.ToLower() == "on")
            {
                try
                {
                    using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                    {
                        _testData.WriteLine("Session Validation values : " + Request[shibmap_sessionid]); // Write the file.
                        _testData.WriteLine("Start log");
                        foreach (string key in Request.Form.Keys)
                        {
                            _testData.WriteLine(key + "=" + Request.Form[key] + "Form<br/>");
                        }
						foreach (string key in Request.QueryString.Keys)
                        {
                            _testData.WriteLine(key + "=" + Request.QueryString[key] + "Keys<br/>");
                        }
                        foreach (string key in Request.Params.Keys)
                        {
                            _testData.WriteLine(key + "=" + Request.Params[key] + "Param<br/>");
                        }
                        foreach (var key in Request.Headers.AllKeys)
                        {
                            _testData.WriteLine(key + "=" + Request.Headers[key] + "Header<br/>");
                        }

                        foreach (var key in Request.ServerVariables.AllKeys)
                        {
                            _testData.WriteLine(key + "=" + Request.ServerVariables[key] + "Server<br/>");
                        }
                        foreach (var key in Request.Cookies.AllKeys)
                        {
                            _testData.WriteLine(key + "=" + Request.Cookies[key] + "Cookie<br/>");
                        }
                        _testData.WriteLine("end log");
                    }
                }
                catch
                {

                }
            }
            //Make sure Shibboleth has session and only when enabled
            if (((Request[shibmap_sessionid] != null || Request.QueryString[shibmap_sessionid] != null || Request.Params[shibmap_sessionid] != null)) && ((Request[shibmap_sessionid] != null || Request.QueryString[shibmap_sessionid] != null || Request.Params[shibmap_sessionid] != null) && (shibboleth_sso_enabled == 1 || shibboleth_sso_enabled == 2)))
            //if (1 == 1)
            {
                ShibbolethHelper shibHelper = new ShibbolethHelper();
                var shibmap_username = ShibbolethHelper.GetShibbolethUserNameField("UserName");
                //Check if The Usernmae field has shibboleth Mapping attribute
                if ((shibmap_username != null) && (shibmap_username != ""))
                //if (1 == 1)
                {
                    if (shib_allowed_departments != "")
                    {
                        allow_user_tologin_base_on_department = false; //set it to false if the department restriction is configured and set.
                        if (shib_allowed_departments.Contains(','))
                        {

                            foreach (var dept in shib_allowed_departments.Split(','))
                            {
                                if (dept.ToLower() == Request[ShibbolethHelper.GetShibbolethUserNameField("Department").ToLower()])
                                {
                                    allow_user_tologin_base_on_department = true;// set it to true if equal.
                                }
                            }
                        }
                        else
                        {
                            if (shib_allowed_departments == Request[ShibbolethHelper.GetShibbolethUserNameField("Department").ToLower()])
                            {
                                allow_user_tologin_base_on_department = true;// set it to true if equal.
                            }
                        }
                    }
                    if (!allow_user_tologin_base_on_department)
                    {
                        loginErrorMessage = "* You do not have correct department to access. Please contact administrator.";
                    }
                    if (TurnOnDebugTracingMode.ToLower() == "on")
                    {
                        try
                        {
                            using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                            {
                                _testData.WriteLine("Login Values Validation : " + Request[shibmap_username] + "   " + allow_user_tologin_base_on_department); // Write the file.
                            }
                        }
                        catch
                        {
                        }
                    }
                    ////If Username has corresponding attribute, check the value on Header.
                    if ((Request["HTTP_" + shibmap_username] != null || Request[shibmap_username] != null || Request.Params[shibmap_username] != null) && (Request["HTTP_" + shibmap_username] != "" || Request[shibmap_username] != "" || Request.Params[shibmap_username] != "") && allow_user_tologin_base_on_department)
                    //if (1 == 1)
                    {

                        string username = "";
                        try
                        {
                            username = Request["HTTP_" + shibmap_username];
                            if (username == null || username == "")
                            {
                                username = Request[shibmap_username];
                                if (username == null || username == "")
                                {
                                    username = Request.Params[shibmap_username];
                                }
                            }
                        }
                        catch {
                            username = Request[shibmap_username];
                        }

                        //username = "testAuto";
                        // string username = "omgme8";
                        var student = StudentHelper.GetStudent(username);
                        var supervisor = SupervisorHelper.GetSupervisor(username);
                        var instructor = InstructorHelper.GetInstructor(username);
                        try
                        {
                            if (Settings.Instance.GetMasterInfo3().hideInstructorLogin == 1)
                            {
                                {
                                    instructor = null;
                                }
                            }
                            if (System.Configuration.ConfigurationManager.AppSettings["hide_InstructorLogin_onCartCheckOut"] == "1")
                            {
                                if (CourseShoppingCart.Instance.Count > 0)
                                {
                                    instructor = null;
                                }
                            }
                            if (Settings.Instance.GetMasterInfo3().HideSupervisorLogin == 1)
                            {
                                supervisor = null;
                            }
                        }
                        catch { }
                        //If Username from the Header has value, check it to student table if already exist.
                        //check if student exist
                        if (supervisor != null)
                        {
                            if (supervisor.ACTIVE != 1)
                            {
                                supervisor.ACTIVE = 1;
                            }
                        }
                        if (student != null)
                        {
                            if (student.InActive != 0)
                            {
                                student.InActive=0;
                            }
                        }
                        if (student == null && supervisor != null && instructor ==  null)
                        {
                            AuthorizationHelper.LoginSupervisor(supervisor.UserName, supervisor.PASSWORD);
                        }
                        else if (student == null && supervisor == null && instructor != null)
                        {
                            AuthorizationHelper.LoginInstructor(instructor.USERNAME, instructor.PASSWORD);
                        }
                        if ((student == null) || (shibboleth_sso_enabled == 2 && student != null))
                            {
                                var firstname = "";
                                var email = "";
                                var lastname = "";
                                var city = "";
                                var state = "";
                                var zip = "";
                                var country = "";
                                var homephone = "";
                                var workphone = "";
                                var fax = "";
                                var sss = "";
                                int schoolid = 0;
                                int districtid = 0;
                                int gradeid = 0;
                                var school_name = "";
                                var district_name = "";
                                var grade_name = "";
                                var shibmap_fname = ShibbolethHelper.GetShibbolethUserNameField("First");
                                var shibmap_lname = ShibbolethHelper.GetShibbolethUserNameField("Last");
                                var shibmap_email = ShibbolethHelper.GetShibbolethUserNameField("Email");
                                var shibmap_city = ShibbolethHelper.GetShibbolethUserNameField("City");
                                var shibmap_state = ShibbolethHelper.GetShibbolethUserNameField("State");
                                var shibmap_zip = ShibbolethHelper.GetShibbolethUserNameField("Zip");
                                var shibmap_country = ShibbolethHelper.GetShibbolethUserNameField("Country");
                                var shibmap_hphone = ShibbolethHelper.GetShibbolethUserNameField("HomePhone");
                                var shibmap_wphone = ShibbolethHelper.GetShibbolethUserNameField("WorkPhone");
                                var shibmap_fax = ShibbolethHelper.GetShibbolethUserNameField("Fax");
                                var shibmap_sss = ShibbolethHelper.GetShibbolethUserNameField("SS");
                                var shibmap_school = ShibbolethHelper.GetShibbolethUserNameField("School");
                                var shibmap_district = ShibbolethHelper.GetShibbolethUserNameField("District");
                                var shibmap_grade = ShibbolethHelper.GetShibbolethUserNameField("Grade");

                                string supervisorUniqueID = "";
                                int currentSupervisorUniqueIDfield = 0;
                                int foundSupervisorID = 0;
                                Supervisor LookUpSupervisor;
                                string[] shibmap_field = new string[21];
                                string[] studregfield = new string[21];
                                for (int x = 1; x <= 20; x++)
                                {
                                    shibmap_field[x] = ShibbolethHelper.GetShibbolethUserNameField("studregfield" + x.ToString());
                                    if ((shibmap_field[x] != null) && (shibmap_field[x] != ""))
                                    {
                                        studregfield[x] = Request["HTTP_" + shibmap_field[x]];
                                        if (studregfield[x] == null || studregfield[x] == "")
                                        {
                                            studregfield[x] = Request[shibmap_field[x]];
                                            if (studregfield[x] == null || studregfield[x] == "")
                                            {
                                                studregfield[x] = Request.Params[shibmap_field[x]];
                                            }
                                        }


                                    }
                                    //check for supervisor assigned fields #1-20
                                    if (Settings.Instance.GetMasterInfo3().AssignSup2Stud == 1 && shibbolethConfiguration.shibboleth_supervisor_attribute.ToString() != "")
                                    {
                                        currentSupervisorUniqueIDfield = int.Parse(shibbolethConfiguration.shibboleth_supervisor_attribute.ToString().Replace("studregfield", ""));
                                        if (x == currentSupervisorUniqueIDfield)
                                        {
                                            supervisorUniqueID = Request["HTTP_" + shibmap_field[x]];
                                            if (!string.IsNullOrEmpty(supervisorUniqueID))
                                            {
                                                try
                                                {
                                                    LookUpSupervisor = SupervisorHelper.GetSupervisor(supervisorUniqueID);
                                                    foundSupervisorID = LookUpSupervisor.SUPERVISORID;
                                                }
                                                catch
                                                {
                                                    foundSupervisorID = 0;
                                                }
                                            }
                                        }
                                    }
                                }

                                if ((shibmap_fname != null) && (shibmap_fname != ""))
                                {
                                    firstname = Request["HTTP_" + shibmap_fname];
                                    if (firstname == null || firstname == "")
                                    {
                                        firstname = Request[shibmap_fname];
                                        if (firstname == null || firstname == "")
                                        {
                                            firstname = Request.Params[shibmap_fname];
                                        }
                                    }
                                }
                                if ((shibmap_lname != null) && (shibmap_lname != ""))
                                {
                                    lastname = Request["HTTP_" + shibmap_lname];
                                    if (lastname == null || lastname == "")
                                    {
                                        lastname = Request[shibmap_lname];
                                        if (lastname == null || lastname == "")
                                        {
                                            lastname = Request.Params[shibmap_lname];
                                        }
                                    }
                                }
                                if ((shibmap_email != null) && (shibmap_email != ""))
                                {
                                    email = Request["HTTP_" + shibmap_email];
                                    if (email == null || email == "")
                                    {
                                        email = Request[shibmap_email];
                                        if (email == null || email == "")
                                        {
                                            email = Request.Params[shibmap_email];
                                        }
                                    }
                                }

                                if ((shibmap_city != null) && (shibmap_city != ""))
                                {
                                    city = Request["HTTP_" + shibmap_city];
                                }

                                if ((shibmap_state != null) && (shibmap_state != ""))
                                {
                                    state = Request["HTTP_" + shibmap_state];
                                    if (state == null || state == "")
                                    {
                                        state = Request[shibmap_state];
                                        if (state == null || state == "")
                                        {
                                            state = Request.Params[shibmap_state];
                                        }
                                    }
                                }

                                if ((shibmap_zip != null) && (shibmap_zip != ""))
                                {
                                    zip = Request["HTTP_" + shibmap_zip];
                                    if (zip == null || zip == "")
                                    {
                                        zip = Request[shibmap_zip];
                                        if (zip == null || zip == "")
                                        {
                                            zip = Request.Params[shibmap_zip];
                                        }
                                    }
                                }

                                if ((shibmap_country != null) && (shibmap_country != ""))
                                {
                                    city = Request["HTTP_" + shibmap_city];
                                    if (city == null || city == "")
                                    {
                                        city = Request[shibmap_city];
                                        if (city == null || city == "")
                                        {
                                            city = Request.Params[shibmap_city];
                                        }
                                    }
                                }

                                if ((shibmap_hphone != null) && (shibmap_hphone != ""))
                                {
                                    homephone = Request["HTTP_" + shibmap_hphone];
                                    if (homephone == null || homephone == "")
                                    {
                                        homephone = Request[shibmap_hphone];
                                        if (homephone == null || homephone == "")
                                        {
                                            homephone = Request.Params[shibmap_hphone];
                                        }
                                    }
                                }

                                if ((shibmap_wphone != null) && (shibmap_wphone != ""))
                                {
                                    workphone = Request["HTTP_" + shibmap_wphone];
                                    if (workphone == null || workphone == "")
                                    {
                                        workphone = Request[shibmap_wphone];
                                        if (workphone == null || workphone == "")
                                        {
                                            workphone = Request.Params[shibmap_wphone];
                                        }
                                    }
                                }

                                if ((shibmap_fax != null) && (shibmap_fax != ""))
                                {
                                    fax = Request["HTTP_" + shibmap_fax];
                                    if (fax == null || fax == "")
                                    {
                                        fax = Request[shibmap_fax];
                                        if (fax == null || fax == "")
                                        {
                                            fax = Request.Params[shibmap_fax];
                                        }
                                    }
                                }

                                if ((shibmap_sss != null) && (shibmap_sss != ""))
                                {
                                    sss = Request["HTTP_" + shibmap_sss];
                                    if (sss == null || sss == "")
                                    {
                                        sss = Request[shibmap_sss];
                                        if (sss == null || sss == "")
                                        {
                                            sss = Request.Params[shibmap_sss];
                                        }
                                    }
                                }
                           

                                
                                try
                                {
                                    if ((shibmap_school != null) && (shibmap_school != ""))
                                    {
                                        school_name = Request["HTTP_" + shibmap_school];
                                        if (school_name == null || school_name == "")
                                        {
                                            school_name = Request[shibmap_school];
                                            if (school_name == null || school_name == "")
                                            {
                                                school_name = Request.Params[shibmap_school];
                                            }
                                        }
                                        // school_name = "No";
                                        var schools = Gsmu.Api.Data.School.School.Queries.GetSchoolByName(school_name);
                                        if (schools != null)
                                        {
                                            int? countLocID = 0;
                                            if (schools.Count < 1)
                                            {

                                                if ((school_name != null) && (school_name != ""))
                                                {
                                                    using (var dbx = new SchoolEntities())
                                                    {
                                                        countLocID = (from locid in dbx.Schools select locid.locationid).Max();
                                                    }

                                                    var school = new Gsmu.Api.Data.School.Entities.School
                                                    {
                                                        locationid = countLocID + 1,
                                                        LOCATION = school_name,
                                                    };
                                                    Gsmu.Api.Data.School.School.Queries.AddSchool(school);
                                                }
                                            }
                                            bool schoolexist = false;

                                            foreach (var sc in schools)
                                            {
                                                if (sc.LOCATION.ToLower() == school_name.ToLower())
                                                {
                                                    //schoolid = sc.ID; //it is using locationid from the old classic. will 
                                                    //need to change to ID field at some point
                                                    schoolid = Convert.ToInt32(sc.locationid);
                                                    schoolexist = true;
                                                }
                                            }

                                            if (!schoolexist)
                                            {
                                                if ((school_name != null) && (school_name != ""))
                                                {
                                                    using (var dbx = new SchoolEntities())
                                                    {
                                                        countLocID = (from locid in dbx.Schools select locid.locationid).Max();
                                                    }
                                                    var school = new Gsmu.Api.Data.School.Entities.School
                                                    {
                                                        locationid = countLocID + 1,
                                                        LOCATION = school_name,
                                                    };
                                                    schoolid = Gsmu.Api.Data.School.School.Queries.AddSchool(school);
                                                }
                                            }
                                        }
                                    }
                                    if ((shibmap_district != null) && (shibmap_district != ""))
                                    {
                                        district_name = Request["HTTP_" + shibmap_district];
                                        if (district_name == null || district_name == "")
                                        {
                                            district_name = Request[shibmap_district];
                                            if (district_name == null || district_name == "")
                                            {
                                                district_name = Request.Params[shibmap_district];
                                            }
                                        }
                                        //district_name = "District";

                                        var districts = Gsmu.Api.Data.School.District.Queries.GetDistrictByName(district_name);
                                        Queries q = new Queries();
                                        if (districts != null)
                                        {
                                            if (districts.Count < 1)
                                            {
                                                if ((district_name != null) && (district_name != ""))
                                                {
                                                    var district = new Gsmu.Api.Data.School.Entities.District
                                                    {
                                                        SortOrder = 0,
                                                        DISTRICT1 = district_name,
                                                    };
                                                    Gsmu.Api.Data.School.District.Queries.AddDistrict(district);
                                                }
                                            }
                                            bool districtexist = false;

                                            foreach (var sc in districts)
                                            {
                                                if (sc.DISTRICT1.ToLower() == district_name.ToLower())
                                                {
                                                    districtid = sc.DISTID;
                                                    districtexist = true;
                                                }
                                            }

                                            if (!districtexist)
                                            {
                                                if ((district_name != null) && (district_name != ""))
                                                {
                                                    var district = new Gsmu.Api.Data.School.Entities.District
                                                    {
                                                        SortOrder = 0,
                                                        DISTRICT1 = district_name,
                                                    };
                                                    districtid = Gsmu.Api.Data.School.District.Queries.AddDistrict(district);
                                                }
                                            }
                                        }
                                    }
                                    if ((shibmap_grade != null) && (shibmap_grade != ""))
                                    {
                                        grade_name = Request["HTTP_" + shibmap_grade];
                                        if (grade_name == null || grade_name == "")
                                        {
                                            grade_name = Request[shibmap_grade];
                                            if (grade_name == null || grade_name == "")
                                            {
                                                grade_name = Request.Params[shibmap_grade];
                                            }
                                        }
                                        // grade_name = "grade t";
                                        var grades = Gsmu.Api.Data.School.Grade.Queries.GetGradeByName(grade_name);
                                        if (grades != null)
                                        {
                                            var grade = new Gsmu.Api.Data.School.Entities.Grade_Level
                                            {
                                                SortOrder = 0,
                                                SchoolId = 0,
                                                GRADE = grade_name,
                                            };
                                            Queries q = new Queries();
                                            if (grades.Count < 1)
                                            {
                                                if ((grade_name != null) && (grade_name != ""))
                                                {
                                                    Gsmu.Api.Data.School.Grade.Queries.AddGrade(grade);
                                                }
                                            }
                                            bool gradeexist = false;
                                            foreach (var sc in grades)
                                            {
                                                if (sc.GRADE.ToLower() == grade_name.ToLower())
                                                {
                                                    gradeid = sc.GRADEID;
                                                    gradeexist = true;
                                                }
                                            }

                                            if (!gradeexist)
                                            {
                                                if ((grade_name != null) && (grade_name != ""))
                                                {
                                                    gradeid = Gsmu.Api.Data.School.Grade.Queries.AddGrade(grade);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }

                                //new account
                                if (student == null && supervisor == null && instructor == null)
                                {
                                    if (!login_shib_sso_gsmuonly)
                                    {
                                        int existingCanvasUserID = 0;
                                        string existingCanvasUserSISid = "";
                                        var studnum = "Shibboleth Assigned/Maintains" + username;
                                        if (Gsmu.Api.Integration.Canvas.Configuration.Instance.EnableOAuth2Authentication && Gsmu.Api.Integration.Canvas.Configuration.Instance.autoMapCanvasAccount)
                                        {
                                            var lookUpUserInCanvas = Gsmu.Api.Integration.Canvas.Clients.UserClient.SearchCanvasUserByUserName(username);
                                            if (lookUpUserInCanvas == null && Gsmu.Api.Integration.Canvas.Configuration.Instance.autoMapCanvasAccount && canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField != "username" && !string.IsNullOrEmpty(studregfield[GSMUUniqueIdentifierIndex]))
                                            {
                                                lookUpUserInCanvas = Gsmu.Api.Integration.Canvas.Clients.UserClient.SearchCanvasUserBySisUserID(studregfield[GSMUUniqueIdentifierIndex]);
                                            }

                                            //null mean no matching (per function catch return
                                            if (lookUpUserInCanvas != null && !string.IsNullOrEmpty(studregfield[GSMUUniqueIdentifierIndex]))
                                            {
                                                existingCanvasUserSISid = lookUpUserInCanvas.User.SisUserId.ToString();
                                                //compare with config file
                                                if (studregfield[GSMUUniqueIdentifierIndex] == existingCanvasUserSISid)
                                                {
                                                    existingCanvasUserID = int.Parse(lookUpUserInCanvas.User.Id.ToString());
                                                }
                                            }
                                            if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                                            {
                                                using (var dbx = new SchoolEntities())
                                                {

                                                    string lookupUseridinCanvas = "";
                                                    try
                                                    {
                                                        lookupUseridinCanvas = lookUpUserInCanvas.User.Id.ToString();
                                                    }
                                                    catch { }
                                                    AuditTrail trail = new AuditTrail()
                                                    {
                                                        RoutineName = "AuthMeController.cs",
                                                        ShortDescription = "New Account Mapped to Canvas",
                                                        UserName = username,
                                                        DetailDescription = "Stack Trace: - Shibboleth unnique ID: " + studregfield[GSMUUniqueIdentifierIndex] + " -CanvasInternalID: " + lookupUseridinCanvas + " -Canvas SISuserID: " + existingCanvasUserSISid,
                                                        AuditDate = System.DateTime.Now,
                                                        CourseID = 0,
                                                        StudentID = 0
                                                    };
                                                    dbx.AuditTrails.Add(trail);
                                                    dbx.SaveChanges();
                                                }
                                            }
                                        }
                                   
                                        student = new Student
                                        {
                                            USERNAME = username,
                                            FIRST = firstname ?? "",
                                            LAST = lastname ?? "",
                                            EMAIL = email ?? "",
                                            STUDNUM = studnum ?? "",
                                            CITY = city ?? "",
                                            STATE = state ?? "",
                                            ZIP = zip ?? "",
                                            COUNTRY = country ?? "",
                                            HOMEPHONE = homephone ?? "",
                                            WORKPHONE = workphone ?? "",
                                            FAX = fax ?? "",
                                            SS = sss ?? "",
                                            StudRegField1 = studregfield[1] ?? "",
                                            StudRegField2 = studregfield[2] ?? "",
                                            StudRegField3 = studregfield[3] ?? "",
                                            StudRegField4 = studregfield[4] ?? "",
                                            StudRegField5 = studregfield[5] ?? "",
                                            StudRegField6 = studregfield[6] ?? "",
                                            StudRegField7 = studregfield[7] ?? "",
                                            StudRegField8 = studregfield[8] ?? "",
                                            StudRegField9 = studregfield[9] ?? "",
                                            StudRegField10 = studregfield[10] ?? "",

                                            StudRegField11 = studregfield[11] ?? "",
                                            StudRegField12 = studregfield[12] ?? "",
                                            StudRegField13 = studregfield[13] ?? "",
                                            StudRegField14 = studregfield[14] ?? "",
                                            StudRegField15 = studregfield[15] ?? "",
                                            StudRegField16 = studregfield[16] ?? "",
                                            StudRegField17 = studregfield[17] ?? "",
                                            StudRegField18 = studregfield[18] ?? "",
                                            StudRegField19 = studregfield[19] ?? "",
                                            StudRegField20 = studregfield[20] ?? "",
                                            SCHOOL = schoolid,
                                            DISTRICT = districtid,
                                            GRADE = gradeid,
                                            DateAdded = DateTime.Now,
                                            SAPLastPendingReason = "Passing from Shibboleth",
                                            AuthFromLDAP = 0,
                                            loginTally = 0,
                                            google_user = 0,
                                            canvas_user_id = existingCanvasUserID
                                        };
                                        if (TurnOnDebugTracingMode.ToLower() == "on")
                                        {
                                            try
                                            {
                                                using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                                                {
                                                    _testData.WriteLine("Found Existing Canvas Account ID : " + existingCanvasUserID + "  - Stud Object Stat: " ); // Write the file.
                                                    
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                        StudentHelper.RegisterStudent(student);
                                        if (existingCanvasUserID != 0 && canvas.Configuration.Instance.ExportUserAfterRegistration)
                                        {
                                            var response = canvas.CanvasExport.SynchronizeStudent(student, null);
                                        }
                                        /*
                                        //incompleted
                                        if (foundSupervisorID != 0 && Settings.Instance.GetMasterInfo3().AssignSup2Stud == 1 && shibbolethConfiguration.shibboleth_supervisor_attribute.ToString() != "")
                                        {
                                            using (var dbz = new SchoolEntities())
                                            {
                                                var CheckSupStud = (from a in dbz.SupervisorStudents where a.studentid == student.STUDENTID && a.SupervisorID == foundSupervisorID select a.SupervisorID).Count();
                                                SupervisorStudent SupStudentity = new SupervisorStudent();
                                                SupStudentity = new SupervisorStudent();
                                                SupStudentity.studentid = student.STUDENTID;
                                                SupStudentity.SupervisorID = foundSupervisorID;
                                                dbz.SupervisorStudents.Add(SupStudentity);
                                                dbz.SaveChanges();
                                            }
                                        }*/
                                    }
                                    else
                                    {
                                        loginErrorMessage = "* You do not have an active account. Please contact administrator.";
                                    }
                                } //new account
                                else if (shibboleth_sso_enabled == 2 && student != null)
                                {
                                    //update existing account
                                    SchoolEntities db = new SchoolEntities();
                                    var stud = (from s in db.Students where s.USERNAME == username select s).FirstOrDefault();
                                    var existingCanvasUserID = 0;
                                    string existingCanvasUserSISid = "";
                                    if (stud.InActive == 0)
                                    {
                                        stud.FIRST = firstname ?? stud.FIRST;
                                        stud.LAST = lastname ?? stud.LAST;
                                        stud.EMAIL = email ?? stud.EMAIL;
                                        if (city != "")
                                        {
                                            stud.CITY = city ?? stud.CITY;
                                        }
                                        if (state != "")
                                        {
                                            stud.STATE = state ?? stud.STATE;
                                        }
                                        if (zip != "")
                                        {
                                            stud.ZIP = zip ?? stud.ZIP;
                                        }
                                        stud.COUNTRY = country ?? stud.COUNTRY;
                                        if (homephone != "")
                                        {
                                            stud.HOMEPHONE = homephone ?? stud.HOMEPHONE;
                                        }
                                        if (homephone != "")
                                        {
                                            stud.WORKPHONE = workphone ?? stud.WORKPHONE;
                                        }
                                        stud.FAX = fax ?? stud.FAX;
                                        stud.SS = sss ?? stud.SS;
                                        stud.StudRegField1 = studregfield[1] ?? stud.StudRegField1;
                                        stud.StudRegField2 = studregfield[2] ?? stud.StudRegField2;
                                        stud.StudRegField3 = studregfield[3] ?? stud.StudRegField3;
                                        stud.StudRegField4 = studregfield[4] ?? stud.StudRegField4;
                                        stud.StudRegField5 = studregfield[5] ?? stud.StudRegField5;
                                        stud.StudRegField6 = studregfield[6] ?? stud.StudRegField6;
                                        stud.StudRegField7 = studregfield[7] ?? stud.StudRegField7;
                                        stud.StudRegField8 = studregfield[8] ?? stud.StudRegField8;
                                        stud.StudRegField9 = studregfield[9] ?? stud.StudRegField9;
                                        stud.StudRegField10 = studregfield[10] ?? stud.StudRegField10;

                                        stud.StudRegField11 = studregfield[11] ?? stud.StudRegField11;
                                        stud.StudRegField12 = studregfield[12] ?? stud.StudRegField12;
                                        stud.StudRegField13 = studregfield[13] ?? stud.StudRegField13;
                                        stud.StudRegField14 = studregfield[14] ?? stud.StudRegField14;
                                        stud.StudRegField15 = studregfield[15] ?? stud.StudRegField15;
                                        stud.StudRegField16 = studregfield[16] ?? stud.StudRegField16;
                                        stud.StudRegField17 = studregfield[17] ?? stud.StudRegField17;
                                        stud.StudRegField18 = studregfield[18] ?? stud.StudRegField18;
                                        stud.StudRegField19 = studregfield[19] ?? stud.StudRegField19;
                                        stud.StudRegField20 = studregfield[20] ?? stud.StudRegField20;
                                        if (schoolid != 0) { stud.SCHOOL = schoolid; }
                                        if (districtid != 0) { stud.DISTRICT = districtid; }
                                        if (gradeid != 0) { stud.GRADE = gradeid; }

                                        if ((string.IsNullOrEmpty(stud.canvas_user_id.ToString()) || stud.canvas_user_id ==0) && Gsmu.Api.Integration.Canvas.Configuration.Instance.EnableOAuth2Authentication && Gsmu.Api.Integration.Canvas.Configuration.Instance.autoMapCanvasAccount)
                                        {
                                            var lookUpUserInCanvas = Gsmu.Api.Integration.Canvas.Clients.UserClient.SearchCanvasUserByUserName(username);
                                            if (lookUpUserInCanvas == null && Gsmu.Api.Integration.Canvas.Configuration.Instance.autoMapCanvasAccount && canvas.Configuration.Instance.UserGSMUCanvasUniqueIdentifierField != "username"  && !string.IsNullOrEmpty(studregfield[GSMUUniqueIdentifierIndex]))
                                            {
                                                lookUpUserInCanvas = Gsmu.Api.Integration.Canvas.Clients.UserClient.SearchCanvasUserBySisUserID(studregfield[GSMUUniqueIdentifierIndex]);
                                            }
                                            //null mean no matching (per function catch return

                                            if (lookUpUserInCanvas != null && !string.IsNullOrEmpty(studregfield[GSMUUniqueIdentifierIndex]))
                                            {
                                                existingCanvasUserSISid = lookUpUserInCanvas.User.SisUserId.ToString();
                                                //compare with config file
                                                if (studregfield[GSMUUniqueIdentifierIndex] == existingCanvasUserSISid)
                                                {
                                                    existingCanvasUserID = int.Parse(lookUpUserInCanvas.User.Id.ToString());
                                                    stud.canvas_user_id = existingCanvasUserID;
                                                }
                                            }
                                            if ((WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "off") && (WebConfiguration.AuditSiteAcitivityLevel.ToLower() != "initial"))
                                            {
                                                using (var dbx = new SchoolEntities())
                                                {
                                                    AuditTrail trail = new AuditTrail()
                                                    {
                                                        RoutineName = "AuthMeController.cs",
                                                        ShortDescription = "Update existing Account Mapped to Canvas",
                                                        UserName = username,
                                                        DetailDescription = "Stack Trace: - canvas_user_id: " + existingCanvasUserID + " -username:" + username,
                                                        AuditDate = System.DateTime.Now,
                                                        CourseID = 0,
                                                        StudentID = 0
                                                    };
                                                    dbx.AuditTrails.Add(trail);
                                                    dbx.SaveChanges();
                                                }
                                            }
                                        }
                                        db.SaveChanges();
                                        //deal with Supervisor/Students relation
                                        if (foundSupervisorID != 0 && Settings.Instance.GetMasterInfo3().AssignSup2Stud == 1 && shibbolethConfiguration.shibboleth_supervisor_attribute.ToString() != "")
                                        {
                                            int CheckSupStud = (from a in db.SupervisorStudents where a.studentid == stud.STUDENTID && a.SupervisorID == foundSupervisorID select a.SupervisorID).Count();
                                            if (CheckSupStud == 0)
                                            {
                                                using (var ssshib = new SchoolEntities())
                                                {
                                                    SupervisorStudent SupStudentity = new SupervisorStudent();
                                                    SupStudentity = new SupervisorStudent();
                                                    SupStudentity.studentid = stud.STUDENTID;
                                                    SupStudentity.SupervisorID = foundSupervisorID;
                                                    ssshib.SupervisorStudents.Add(SupStudentity);
                                                    ssshib.SaveChanges();
                                                }
                                            }
                                        }
                                    }

                                } // update existing account
                            } // end check if student exist condition
                        if ((supervisor == null) && instructor == null)
                        {
                            if (login_shib_sso_gsmuactive && student != null)
                            {
                                if (student.InActive == 0)
                                {
                                    AuthorizationHelper.LoginStudent(student.USERNAME, student.STUDNUM);
                                }
                                else
                                {
                                    loginErrorMessage = "* Your account is currently inactive. Please contact administrator.";
                                }
                            }
                            else if (student != null)
                            {
                                AuthorizationHelper.LoginStudent(student.USERNAME, student.STUDNUM);
                            }
                        }
                        if ((student != null && supervisor != null && instructor != null) || (student != null && supervisor != null) || (student != null && instructor != null) || (supervisor != null && instructor != null))
                        {
                            Session["ShibbolethStudent"] = student;
                            Session["ShibbolethSupervisor"] = supervisor;
                            Session["ShibbolethInstructor"] = instructor;
                            return RedirectToAction("SelectShibUser", "AuthMe");
                        }
                    }
                }
                if (TurnOnDebugTracingMode != null)
                {
                    if (TurnOnDebugTracingMode.ToLower() == "on")
                    {
                        var headers = String.Empty;
                        foreach (var key in Request.Headers.AllKeys)
                            headers += key + "=" + Request.Headers[key] + Environment.NewLine;
                        foreach (var key in Request.Form.AllKeys)
                            headers += key + "=" + Request.Form[key] + Environment.NewLine;
                        foreach (var key in Request.ServerVariables.AllKeys)
                            headers += key + "=" + Request.ServerVariables[key] + Environment.NewLine;
                        try
                        {
                            using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                            {
                                _testData.WriteLine(DateTime.Today + " **** " + headers); // Write the file.
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                if (AuthorizationHelper.CurrentUser.IsLoggedIn)
                {
                    var headers = String.Empty;
                    foreach (var key in Request.Headers.AllKeys)
                        headers += key + "=" + Request.Headers[key] + Environment.NewLine;
                    foreach (var key in Request.Form.AllKeys)
                        headers += key + "=" + Request.Form[key] + Environment.NewLine;
                    foreach (var key in Request.ServerVariables.AllKeys)
                        headers += key + "=" + Request.ServerVariables[key] + Environment.NewLine;
                    if (TurnOnDebugTracingMode.ToLower() == "on")
                    {
                        try
                        {
                            using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                            {
                                _testData.WriteLine(DateTime.Today + " **** " + headers); // Write the file.
                            }
                        }
                        catch
                        {
                        }
                    }
                    try
                    {
                        if (SurveyInfo.Instance.studentid != 0)
                        {
                            return RedirectToAction("showsurvey", "Survey", new
                            {
                                area = "Public",
                                studid = SurveyInfo.Instance.studentid,
                                sid = SurveyInfo.Instance.surveyid,
                                cid = SurveyInfo.Instance.courseid
                            });
                        }
                    }
                    catch { }

                    if (CourseShoppingCart.Instance.SelectedCourseID != null)
                    {
                        int? SelectedCourseID = CourseShoppingCart.Instance.SelectedCourseID;
                        return RedirectToAction("Browse", "Course", new
                        {

                            area = "Public",
                            message = Request["HTTP_" + ShibbolethHelper.GetShibbolethUserNameField("First")],
                            courseid = SelectedCourseID
                        });

                    }
                    else
                    {
                        return RedirectToAction("Browse", "Course", new
                         {

                             area = "Public",
                             message = Request["HTTP_" + ShibbolethHelper.GetShibbolethUserNameField("First")]
                         });
                    }
                }
                else
                {
                    if (Settings.Instance.GetMasterInfo4().shibboleth_required_login == 1)
                    {
                        return RedirectToAction("Login", "Shibboleth.sso");
                    }
                    else
                    {
                        return RedirectToAction("Browse", "Course", new
                        {
                            area = "Public",
                            message = loginErrorMessage
                        });
                    }
                }

            }

            else
            {
                try
                {
                    using (StreamWriter _testData = new StreamWriter(Server.MapPath("~/shibheader.txt"), true))
                    {
                        _testData.WriteLine(DateTime.Today + "Not Valid Session ID"); // Write the file.
                    }
                }
                catch
                {
                }

                if (Settings.Instance.GetMasterInfo4().shibboleth_required_login == 1 && Gsmu.Api.Authorization.AuthorizationHelper.CurrentUser.IsLoggedIn == false)
                {
                        return RedirectToAction("Login", "Shibboleth.sso");
                }
                else
                {
                    return RedirectToAction("Browse", "Course", new
                    {
                        area = "Public"
                    });
                }


            }
        }

        public ActionResult CASServiceValidation()
        {
            string string_CAS_toValidate = "<?xml version='1.0'?>" + System.Environment.NewLine +
    @"<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">" +
    "<SOAP-ENV:Header/>" +
    "<SOAP-ENV:Body>" +
    "<samlp:Request " +
    @"xmlns:samlp=""urn:oasis:names:tc:SAML:1.0:protocol"" " +
    @"MajorVersion=""1"" " +
    @"MinorVersion=""1"" " +
    @"RequestID=""" + Request.QueryString["requestID"] + @""" " +
    @"IssueInstant=""" + Request.QueryString["issueInstant"] + @""">" +
    "<samlp:AssertionArtifact>" + Request.QueryString["ticket"] + "</samlp:AssertionArtifact>" +
    "</samlp:Request>" + "</SOAP-ENV:Body>" + "</SOAP-ENV:Envelope>";
            if (Request.QueryString["ticket"] != "")
            {
                try
                {
                    ViewBag.Ticket = CASAuthentication.ValidateCASAUthentication(string_CAS_toValidate);
                }
                catch
                {
                    ViewBag.Ticket = "";
                }
            }
            if (SurveyInfo.Instance.studentid != 0)
            {
                return RedirectToAction("showsurvey", "Survey", new
                {
                    area = "Public",
                    studid = SurveyInfo.Instance.studentid,
                    sid = SurveyInfo.Instance.surveyid,
                    cid = SurveyInfo.Instance.courseid
                });
            }
            return RedirectToAction("Browse", "Course", new
            {
                area = "Public",
                message = StripHtmlTags.Strip(Request.QueryString["message"])
            });
        }
        public ShibbolethConfiguration GetShibbolethConfiguration(string jsonfields)
        {
            ShibbolethConfiguration fields = new ShibbolethConfiguration();
            List<ShibbolethConfiguration> ListSelectedFields = new List<ShibbolethConfiguration>();
            JavaScriptSerializer j = new JavaScriptSerializer();
            dynamic settingsconfig = j.Deserialize(jsonfields, typeof(object));
            fields = new ShibbolethConfiguration();
            fields.login_shib_sso_gsmuonly = int.Parse(settingsconfig["login_shib_sso_gsmuonly"]);
            fields.login_shib_sso_gsmuactive = int.Parse(settingsconfig["login_shib_sso_gsmuactive"]);
            
            try
            {
                fields.shibboleth_supervisor_attribute = settingsconfig["shibboleth_supervisor_attribute"];
                fields.allowed_departments = settingsconfig["current_shibboleth_department_attribute"];
            }
            catch
            {
                fields.shibboleth_supervisor_attribute = "";
                fields.allowed_departments = "";
            }

            return fields;
        }

        public ActionResult BlackBoardAuthentication()
        {
            // GET /learn/api/public/v1/oauth2/authorizationcode

            Guid stateId = Guid.NewGuid();

            string applicationKey = Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey;

            string redirectUrl = string.Format(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl +"/learn/api/public/v1/oauth2/authorizationcode" +
                "?redirect_uri="+ Settings.Instance.GetMasterInfo4().DotNetSiteRootUrl+"/authme/BlackBoardAuthenticationResponseHanlder&response_type=code&client_id={0}&scope=read&state={1}",
                applicationKey, stateId);
            Response.Redirect(redirectUrl, true);

            return View();
        }

        public ActionResult BlackBoardAuthenticationResponseHanlder()
        {

            BBToken token=  BlackboardAPIRequest.GetAuthenticatedUser(Request.QueryString["code"]);


            if(token!=null)
            {
                Student student = null;
                Instructor instructor = null;
                if (token.user_id != null)
                {
                    string main_role = "";
                    BBUser user = BlackboardAPIRequest.GetUserDetails(token);
                    foreach(var role in user.institutionRoleIds)
                    {
                            main_role = role;
                        if(role=="STUDENT")
                            student= BlackboardAPIRequest.InsertStudentUserFromBlackboard(token);
                        else if(role=="FACULTY")
                            instructor= BlackboardAPIRequest.InsertInstructorUserFromBlackboard(token);

                    }
                    if (user.institutionRoleIds.Count() == 1 && main_role=="STUDENT")
                    {
                        AuthorizationHelper.LoginStudentByBlackboardUUID(token);
                    }
                    else if (user.institutionRoleIds.Count() == 1 && main_role == "FACULTY")
                    {
                    }
                    else if (user.institutionRoleIds.Count() > 1)
                    {
                        Session["BBStudent"] = student;
                        Session["ShibbolethSupervisor"] = null;
                        Session["BBInstructor"] = instructor;
                        return RedirectToAction("SelectBBUser", "AuthMe");
                    }
                }
            }

            return RedirectToAction("Dashboard", "User", new
            {
                area = "Public",
                message = StripHtmlTags.Strip(Request.QueryString["message"])
            });
        }
    }
    public class ShibbolethConfiguration
    {
        public int login_shib_sso_gsmuonly
        {
            get;
            set;
        }
        public int login_shib_sso_gsmuactive
        {
            get;
            set;
        }
        public string allowed_departments
        {
            get;
            set;
        }
        public string shibboleth_supervisor_attribute
        {
            get;
            set;
        }
    }
}

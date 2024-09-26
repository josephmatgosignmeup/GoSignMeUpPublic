using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using BlackBoardAPI;
using Gsmu.Api.Integration.Blackboard;
using Gsmu.Api.Integration.Blackboard.Connector;
using static BlackBoardAPI.BlackBoardAPIModel;

namespace Gsmu.Web.Areas.Adm.Controllers
{
    public class BlackboardController : Controller
    {
        // GET: Adm/Blackboard
        public ActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SaveSettings(string blackboard_students_dsk = null, string blackboard_instructors_dsk = null, string blackboard_courses_dsk = null, string blackboard_course_roster_dsk = null, string blackboard_students_node_id = null, string blackboard_courses_node_id = null, string blackboard_instructors_node_id = null)
        {
            bool success = true;
            string message = string.Empty;

            var config = Configuration.Instance;
            config.StudentDsk = blackboard_students_dsk;
            config.InstructorsDsk = blackboard_instructors_dsk;
            config.CoursesDsk = blackboard_courses_dsk;
            config.CourseRosterDsk = blackboard_course_roster_dsk;

            config.CourseInstitutionalHierarchyNodeId = blackboard_courses_node_id;
            config.StudentInstitutionalHierarchyNodeId = blackboard_students_node_id;
            config.InstructorInstitutionalHierarchyNodeId = blackboard_instructors_node_id;
            
            config.Save();

            message = "Settings saved successfully.";
            var result = new
            {
                config = config,
                success = success,
                message = message
            };
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        public ActionResult InstitutionalHierarchy()
        {


            if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
            {

                string JsonNodeList = "";
                string node = Request.QueryString["node"];
                if (node!="root")
                {
                    BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                    var jsonToken = Gsmu.Api.Authorization.AuthorizationHelper.getCurrentBBAccessToken();
                    var hierarchies = handelr.GetChildHierarchy(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", node, jsonToken);
                    foreach (var item in hierarchies.results)
                    {
                        JsonNodeList = JsonNodeList + "{\"nodeName\":\"" + item.externalId + "\",\"nodeId\":\"" + item.id + "\"},";
                    }
                }
                else
                {
                    BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                    var jsonToken = Gsmu.Api.Authorization.AuthorizationHelper.getCurrentBBAccessToken();
                    var hierarchies = handelr.GetBBAPIHierarchiess(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", "", jsonToken);
                    foreach(var item in hierarchies.results)
                    {
                        JsonNodeList = JsonNodeList + "{\"nodeName\":\""+item.externalId+"\",\"nodeId\":\""+item.id+"\"},";
                    }

                }
                var nodes = "{ \"expanded\": true, \"children\": [" + JsonNodeList + "]}";
                return Content(nodes, "application/json");
            }

            else
            {

                var nodes = "{ \"expanded\": true, \"children\": [" + NodeConnector.NodeListJson + "]}";
                return Content(nodes, "application/json");
            }
        }

        public ActionResult DataSourceStore()
        {

            if (Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardUseAPI)
            {
                BlackBoardAPI.BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
                //BBToken BBToken = new BBToken();
                //BBToken = handelr.GenerateAccessToken(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl);
                //var jsonToken = new JavaScriptSerializer().Serialize(BBToken);
                var jsonToken = Gsmu.Api.Authorization.AuthorizationHelper.getCurrentBBAccessToken();
                var datasources = handelr.GetBBAPIDataSources(Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecretKey, Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackBoardSecurityKey, "", Gsmu.Api.Integration.Blackboard.Configuration.Instance.BlackboardConnectionUrl, "", "", "", jsonToken);

                object[] json = new object[datasources.results.Count()];
                int index = 0;
                foreach (var item in datasources.results)
                {
                    json[index] = new
                    {
                        display = item.externalId,
                        value = item.id
                    };

                    index = index + 1;
                }
                return Json(json, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var dsks = Gsmu.Api.Integration.Blackboard.Connector.DataSourceKeyConnector.DataSourceKeys;

                object[] json = new object[dsks.Length];
                for (var index = 0; index < dsks.Length; index++)
                {
                    json[index] = new
                    {
                        display = dsks[index],
                        value = dsks[index]
                    };
                }

                return Json(json, JsonRequestBehavior.AllowGet);
            }

        }

    }
}
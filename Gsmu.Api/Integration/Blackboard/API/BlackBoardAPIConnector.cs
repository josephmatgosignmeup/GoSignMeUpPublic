using BlackBoardAPI;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using static BlackBoardAPI.BlackBoardAPIModel;

namespace Gsmu.Api.Integration.Blackboard.API
{
    static class BlackBoardAPIConnector
    {
        private static BBToken token = new BBToken();
        public static async Task<BBToken> BlckBoardAPICall(string code, string secretkey, string applicationkey,string return_url, string connectionurl)
        {
            Authorizer authorizer = new Authorizer();
            token  = await authorizer.Authorize(  secretkey,  applicationkey,  return_url, connectionurl, code);

            return token;
        }

        public static BBUser BlckBoardAPICallGetUserDetails(string connectionurl,string uuid,BBToken tokeninstance)
        {
            UserService UserService = new UserService(tokeninstance);
            BBUser user =  UserService.ReadBlackboardUser(connectionurl,uuid);

            return user;
        }



        public static void UpdateExistingBBUser(BBUser bbUser)
        {
            BlackboardAPIRequestHandler handelr = new BlackboardAPIRequestHandler();
            var jsonToken = Gsmu.Api.Authorization.AuthorizationHelper.getCurrentBBAccessToken();
            BBUser user = new BBUser();
            BBRespUserProfile testuser = handelr.UpdateExisitingUser(Configuration.Instance.BlackBoardSecretKey, Configuration.Instance.BlackBoardSecurityKey, "", Configuration.Instance.BlackboardConnectionUrl, user, "", jsonToken, "");

        }

    }

}
public class BlackBoardCourse{

    public string id { get; set; }
        }
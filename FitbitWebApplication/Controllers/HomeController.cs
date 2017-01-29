using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;
using Fitbit.Models;
using FitbitWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FitbitWebApplication.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Physician()
        {
            ViewBag.Message = "Your internal Physician Portal.";

            return View();
        }

        public ActionResult User()
        {
            ViewBag.Message = "Your internal User Portal.";

            return View();
        }

        //
        // GET: /FitbitAuth/
        // Setup - prepare the user redirect to Fitbit.com to prompt them to authorize this app.
        public ActionResult Authorize()
        {
            var appCredentials = new FitbitAppCredentials()
            {
                ClientId = ConfigurationManager.AppSettings["FitbitClientId"],
                ClientSecret = ConfigurationManager.AppSettings["FitbitClientSecret"]
            };
            //make sure you've set these up in Web.Config under <appSettings>:

            Session["AppCredentials"] = appCredentials;

            //Provide the App Credentials. You get those by registering your app at dev.fitbit.com
            //Configure Fitbit authenticaiton request to perform a callback to this constructor's Callback method
            //var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/Home/CallBack/");
            var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/FitbitWebApplicationDev/Home/CallBack/");
            string[] scopes = new string[] { "activity", "nutrition", "heartrate", "location", "nutrition", "profile", "settings", "sleep", "social", "weight" };

            string authUrl = authenticator.GenerateAuthUrl(scopes, null);

            ConfigurationManager.AppSettings.Set("AuthKey", authUrl);            

            return Redirect(authUrl);
        }

        //Final step. Take this authorization information and use it in the app
        public async Task<ActionResult> CallBack()
        {
            FitbitAppCredentials appCredentials = (FitbitAppCredentials)Session["AppCredentials"];

            //var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/Home/CallBack/");
            var authenticator = new OAuth2Helper(appCredentials, Request.Url.GetLeftPart(UriPartial.Authority) + "/FitbitWebApplicationDev/Home/CallBack/");

            string code = Request.Params["code"];

            OAuth2AccessToken accessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            //Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
            var fitbitClient = GetFitbitClient(accessToken);

            //ViewBag.AccessToken = accessToken;

            UserProfile userProfile = await fitbitClient.GetUserProfileAsync();

            using (var db = new CloudFitbitDbEntities())
            {
                try
                {
                    db.Database.Connection.Open();
                    MembershipUser membershipUser = db.MembershipUsers.Where(m => m.Email == HttpContext.User.Identity.Name).FirstOrDefault();

                    membershipUser.UserName = userProfile.EncodedId;

                    //    = new MembershipUser()
                    //{
                    //    UserType = model.UserType,
                    //    Email = model.Email
                    //};
                    //db.MembershipUsers.Add(membershipUser);
                    db.SaveChanges();
                    db.Database.Connection.Close();
                }
                catch (Exception e)
                {

                }
            }



            //TimeSeriesDataList results = await fitbitClient.GetHeartRateTimeSeriesAsync(TimeSeriesResourceType.Steps, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);            

            //using (var context = new CloudFitbitDbEntities1())
            //{
            //    try
            //    {
            //        context.Database.Connection.Open();
            //        var userId = context.MembershipUsers.Where(m => m.UserName == userProfile.EncodedId).Select(m => m.id).FirstOrDefault();

            //        foreach (var item in results.DataList)
            //        {
            //            context.TempTables.Add(new TempTable()
            //            {
            //                DateTime = item.DateTime,
            //                Name = "HR",
            //                Value = item.Value,
            //                UserID = userId
            //            });
            //            context.SaveChanges();
            //        }
            //        context.Database.Connection.Close();
            //    }
            //    catch (Exception e)
            //    {
            //        throw e;
            //    }
            //}

            //    string sOutput = "";
            //foreach (var result in results.DataList)
            //{
            //    sOutput += result.DateTime.ToString() + " - " + result.Value.ToString();
            //}

            //return sOutput;

            return RedirectToAction("User", "Home");
        }

        //Final step. Take this authorization information and use it in the app
        public async Task<ActionResult> SyncFitbitData()
        {
            //Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
            var fitbitClient = GetFitbitClient();

            //ViewBag.AccessToken = accessToken;

            UserProfile userProfile = await fitbitClient.GetUserProfileAsync();

            TimeSeriesDataList results = await fitbitClient.GetHeartRateTimeSeriesAsync(TimeSeriesResourceType.Steps, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            using (var context = new CloudFitbitDbEntities1())
            {
                try
                {
                    context.Database.Connection.Open();
                    var userId = context.MembershipUsers.Where(m => m.UserName == userProfile.EncodedId).Select(m => m.id).FirstOrDefault();

                    foreach (var item in results.DataList)
                    {
                        context.TempTables.Add(new TempTable()
                        {
                            DateTime = item.DateTime,
                            Name = "HR",
                            Value = item.Value,
                            UserID = userId
                        });
                        context.SaveChanges();
                    }
                    context.Database.Connection.Close();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            //string sOutput = "";
            //foreach (var result in results.DataList)
            //{
            //    sOutput += result.DateTime.ToString() + " - " + result.Value.ToString();
            //}

            //return sOutput;

            return RedirectToAction("User", "Home");
        }

        /// <summary>
        /// HttpClient and hence FitbitClient are designed to be long-lived for the duration of the session. This method ensures only one client is created for the duration of the session.
        /// More info at: http://stackoverflow.com/questions/22560971/what-is-the-overhead-of-creating-a-new-httpclient-per-call-in-a-webapi-client
        /// </summary>
        /// <returns></returns>
        private FitbitClient GetFitbitClient(OAuth2AccessToken accessToken = null)
        {
            if (Session["FitbitClient"] == null)
            {
                if (accessToken != null)
                {
                    var appCredentials = (FitbitAppCredentials)Session["AppCredentials"];
                    FitbitClient client = new FitbitClient(appCredentials, accessToken);
                    Session["FitbitClient"] = client;
                    return client;
                }
                else
                {
                    throw new Exception("First time requesting a FitbitClient from the session you must pass the AccessToken.");
                }

            }
            else
            {
                return (FitbitClient)Session["FitbitClient"];
            }
        }

        //////[HttpPost]
        //public void CallBack()
        //{
        //    var url = Request.Url;


        //    //var responseString = Encoding.ASCII.GetString(response);
        //}
    }
}
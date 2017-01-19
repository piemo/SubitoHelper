using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SubitoNotifier.Helper
{
    public static class MailGunHelper
    {
        public static string SendEmail(string to, string subject, string body)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://api.mailgun.net/v3");
            client.Authenticator =
                new HttpBasicAuthenticator("api",
                                            "key-72562e56c4aea578316ac3dc41ed30e7");
            RestRequest request = new RestRequest();
            request.AddParameter("domain", "app21b0de520a384e0c8c1c51603d7f728e.mailgun.org", ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", "Subito Notifier <e69beb86-2aab-42fd-b546-eed41a65f37b@apphb.com>");
            request.AddParameter("to", to);
            request.AddParameter("subject", subject);
            request.AddParameter("text", body);
            request.Method = Method.POST;
            var response = client.Execute(request);
            return response.Content;
        }
    }
}
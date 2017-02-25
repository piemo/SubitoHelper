using System;
using System.Net;
using System.Text;

namespace SubitoNotifier.Helper
{
    public class SubitoWebClient : WebClient
    {
        private string BASEURL ="https://ade.subito.it/v1/users/login";
        public WebResponse getLoginResponse(string loginData)
        {
            CookieContainer container;

            var request = (HttpWebRequest)WebRequest.Create("https://ade.subito.it/v1/users/login");

            request.Method = "POST";
            request.ContentType = "application/json";
            var buffer = Encoding.ASCII.GetBytes(loginData);
            request.ContentLength = buffer.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(buffer, 0, buffer.Length);
            requestStream.Close();

            container = request.CookieContainer = new CookieContainer();

            var response = request.GetResponse();
            CookieContainer = container;
            return response;
        }

        public SubitoWebClient(CookieContainer container)
        {
            CookieContainer = container;
        }

        public SubitoWebClient()
          : this(new CookieContainer())
        { }

        public CookieContainer CookieContainer { get; private set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }
    }
}
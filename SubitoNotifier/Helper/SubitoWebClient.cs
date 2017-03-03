using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SubitoNotifier.Helper
{
    public class SubitoWebClient : WebClient
    {
        public SubitoWebClient(CookieContainer container)
        {
            CookieContainer = container;
        }

        public SubitoWebClient()
          : this(new CookieContainer())
        { }

        public CookieContainer CookieContainer { get; private set; }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var request = (HttpWebRequest)base.GetWebRequest(uri);
            //this.Headers.Add("Accept", "*/*");
            //this.Headers.Add("host", "hades.subito.it");
            //this.Headers.Add("X-Subito-Channel", "50");
            //this.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36");
            //this.Headers.Add("Accept-Language", "it-IT;q=1, en-US;q=0.9");
            //this.Headers.Add("Accept-Encoding", "gzip, deflate");
            //this.Headers.Add("Connection", "keep-alive");
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.CookieContainer = CookieContainer;
            return request;
        }

        public async Task<string> getLoginResponse(string loginData, Uri uri)
        {
            CookieContainer container;
            var request = (HttpWebRequest)WebRequest.Create(uri);

            //setting the headers
            request.Method = "POST";
            request.ContentType = "application/json";

            //this writes the body of the request
            WriteRequestBody(request, loginData);

            //setting the cookies for the class. all next requests will have these cookies
            container = request.CookieContainer = new CookieContainer();

            //return the response string
            return readResponse(await request.GetResponseAsync());

        }

        public async Task<bool> DeleteRequest(Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            
            //setting the headers
            request.Method = "DELETE";
            request.CookieContainer = this.CookieContainer;
            
            //after sending the request, we get the response and check the code for result.
            HttpWebResponse response =  (HttpWebResponse) await request.GetResponseAsync();
            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                return true;
            else
                return false;
        }

        public async Task<string> GetRequest(Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);

            //setting the headers
            request.Method = "GET";
            request.CookieContainer = this.CookieContainer;
            request.AutomaticDecompression = DecompressionMethods.GZip;

            //return the response string
            return readResponse(await request.GetResponseAsync());
        }

        public async Task<string> PostRequest (string message ,Uri uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            //setting the headers. these must not be changed. The api requires these to be exactly like this
            request.Method = "POST";
            request.CookieContainer = this.CookieContainer;
            request.ContentType = "application/x-www-form-urlencoded";

            //this writes the body of the request
            WriteRequestBody(request, message);

            //return the response string
            return readResponse(await request.GetResponseAsync());
        }

        public async Task<string> PostImageRequest(string imageString, int category, Uri uri)
        {
            //setting the new uri with the added category. the api requires that
            Uri newUri = new Uri(uri + "?category=" + category);
            var request = (HttpWebRequest)WebRequest.Create(newUri);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            //setting the headers. these must not be changed. The api requires these to be exactly like this
            request.Method = "POST";
            request.CookieContainer = this.CookieContainer;
            request.ContentType = "text/plain;charset=UTF-8";

            //manipulating the body of the request we want to send. the left string must be added before appending the jpg converted in base64
            string package = "data:image/jpeg;base64,"+ imageString;

            //this writes the body of the request
            WriteRequestBody(request, package);

            //return the response string
            return readResponse(await request.GetResponseAsync());
        }

        public string readResponse(WebResponse response)
        {
            //read the body of the response it and return it
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }

        public void WriteRequestBody(HttpWebRequest request, string package)
        {
            //this writes the body of the request
            var buffer = Encoding.ASCII.GetBytes(package);
            request.ContentLength = buffer.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(buffer, 0, buffer.Length);
            requestStream.Close();
        }
    }
}
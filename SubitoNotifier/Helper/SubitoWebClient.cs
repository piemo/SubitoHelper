using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SubitoNotifier.Helper
{
    public class SubitoWebClient : WebClient
    {
        public async Task<WebResponse> getLoginResponse(string loginData, Uri uri)
        {
            CookieContainer container;

            var request = (HttpWebRequest)WebRequest.Create(uri);

            request.Method = "POST";
            request.ContentType = "application/json";
            var buffer = Encoding.ASCII.GetBytes(loginData);
            request.ContentLength = buffer.Length;
            var requestStream = request.GetRequestStream();
            requestStream.Write(buffer, 0, buffer.Length);
            requestStream.Close();

            container = request.CookieContainer = new CookieContainer();

            var response = await request.GetResponseAsync();
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

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var request = (HttpWebRequest)base.GetWebRequest(uri);
            this.Headers.Add("Accept", "*/*");
            this.Headers.Add("host", "hades.subito.it");
            this.Headers.Add("X-Subito-Channel", "50");
            this.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36");
            this.Headers.Add("Accept-Language", "it-IT;q=1, en-US;q=0.9");
            this.Headers.Add("Accept-Encoding", "gzip, deflate");
            this.Headers.Add("Connection", "keep-alive");
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.CookieContainer = CookieContainer;
            return request;
        }

        public async Task<bool> DeleteRequest(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)GetWebRequest(uri);
            request.Method = "DELETE";
            HttpWebResponse response =  (HttpWebResponse) await request.GetResponseAsync();
            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
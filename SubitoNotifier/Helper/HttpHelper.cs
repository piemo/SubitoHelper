using Newtonsoft.Json;
using SubitoNotifier.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SubitoNotifier.Helper
{
    public static class HttpHelper
    {
        public static async Task<T> GetDeserializedObjectAsync<T>(this HttpClient http, Uri uri)
        {
            var result = await http.GetAsync(uri);
            result.EnsureSuccessStatusCode();
            string content = await result.Content.ReadAsStringAsync();
            var objs = JsonConvert.DeserializeObject<T>(content);
            return objs;
        }

        public static async Task<string> GetStringWithGzipAsync(this HttpClient client, Uri uri)
        {
            HttpClientHandler hc = new HttpClientHandler();
            hc.AutomaticDecompression = DecompressionMethods.GZip;
            var response = await client.GetAsync(uri);
            response.EnsureSuccessStatusCode(); //will throw an exception if not successful
            string content = await response.Content.ReadAsStringAsync();
            return content;
        }

        public static async Task<SubitoLoginDetail> LoginSubito(string username, string password, SubitoWebClient webClient)
        {
            string loginString = "{ \"password\":\"" + password + "\",\"remember_me\":true,\"username\":\"" + username + "\"}";
            WebResponse response = webClient.getLoginResponse(loginString);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseString = reader.ReadToEnd(); // do something fun...
                return JsonConvert.DeserializeObject<SubitoLoginDetail>(responseString);
            }
        }
    }
}
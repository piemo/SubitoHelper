using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using SubitoNotifier.Models;
using SubitoNotifier.Providers;
using SubitoNotifier.Results;
using Newtonsoft.Json;
using SubitoNotifier.Helper;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Drawing;

namespace SubitoNotifier.Controllers
{
    [RoutePrefix("api/Subito")]
    public class SubitoController : ApiController
    {
        string URL = "https://hades.subito.it/v1";  //url base subito per richieste senza cookies
        string COOKIESURL = "https://ade.subito.it/v1"; // url base subito per richieste con cookies

        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
        string maxNum;          //quantità massima di inserzioni restituite
        string pin;             //da capire
        string searchText;      //stringa ricercata
        string sort;            //ordinamento risultati. Impostato su data decrescente
        string typeIns;         //da utilizzare per gli immobili. s= in vendita, u= in affitto, h= in affitto per vacanze, oppure "s,u,h" per tutte le inserzioni
        string category;        //2 auto,3 moto e scooter,4 veicoli commerciali,5 accessori auto,7 appartamenti,8 Uffici e Locali commerciali,44 Console e Videogiochi,10 Informatica,11 Audio/Video,12 telefonia
                                //14 Arredamento e Casalinghi,15 Giardino e Fai da te,16 Abbigliamento e Accessori,17 Tutto per i bambini,23 Animali,24 Candidati in cerca di lavoro,25 Attrezzature di lavoro
                                //26 Offerte di lavoro,28 Altri,29 Ville singole e a schiera,30 Terreni e rustici,31 Garage e box,32 Loft mansarde e altro,33 Case vacanza,34 Caravan e Camper,36 Accessori Moto,
                                //37 Elettrodomestici,38 Libri e Riviste,39 Strumenti Musicali,40 fotografia,41 biciclette, 
        string city;            //città. codici da estrapolare 
        string region;          //regione. codice da estrapolare al momento


        public SubitoController()
        {
            this.maxNum = Uri.EscapeDataString("20");
            this.pin = "0,0,0";
            this.sort = "datedesc";
            this.typeIns = "s,u,h";
        }

        [Route("GetLatestNewInsertion")]
        public async Task<string> GetInsertion(string botToken, string chatToken, string category="", string city ="", string region ="", string searchText="")
        {
            try
            {
                this.searchText = Uri.EscapeDataString(searchText);
                this.category = Uri.EscapeDataString(category.ToString());
                this.city = Uri.EscapeDataString(city.ToString());
                this.region = Uri.EscapeDataString(region.ToString());
                string parameter = $"/search/ads?lim={this.maxNum}&pin={this.pin}&sort={this.sort}&t={this.typeIns}";

                if (this.category != "")
                    parameter += $"&c={this.category}";

                if (this.city != "")
                    parameter += $"&ci={this.city}";

                if (this.region != "")
                    parameter += $"&r={this.region}";

                if (this.searchText != "")
                    parameter += $"&q={this.searchText}";

                SubitoWebClient webClient = new SubitoWebClient();
                string subitoResponse = await webClient.DownloadStringTaskAsync(new Uri(URL + parameter, UriKind.Absolute));
                var insertions = JsonConvert.DeserializeObject<Insertions>(subitoResponse);
                if(insertions.ads.Count>0)
                {
                    List<Ad> newAds = new List<Ad>();
                    var firstId = insertions.GetFirstAdId();
                    var latestInsertion = SQLHelper.GetLatestInsertionID(this.searchText);
                    if (latestInsertion == null)
                    {
                        newAds.Add(insertions.ads.FirstOrDefault());
                        SQLHelper.InsertLatestInsertion(firstId, this.searchText);
                    }
                    else if (firstId > latestInsertion.SubitoId)
                    {
                        for (int i = 0; i < insertions.ads.Count() && SubitoHelper.GetAdId(insertions.ads[i]) > latestInsertion.SubitoId; i++)
                        {
                            newAds.Add(insertions.ads.ElementAt(i));
                        }
                        latestInsertion.SubitoId = firstId;
                        SQLHelper.UpdateLatestInsertion(latestInsertion);
                    }

                    foreach(Ad ad in newAds)
                    {
                        await SubitoHelper.sendTelegramInsertion(botToken, $"-{chatToken}", this.searchText, ad);
                    }
                }
                return $"Controllato {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [Route("GetDeleteAll")]
        public async Task<string> GetDeleteAll(string username = "", string password = "")
        {
            try
            {
                SubitoWebClient subitoWebClient = new SubitoWebClient();
                //login to get cookies
                SubitoLoginDetail loginData = await LoginSubito(username, password, subitoWebClient, new Uri(COOKIESURL + "/users/login"));

                //getting the list of own insertions
                Uri uri = new Uri(COOKIESURL + "/users/" + loginData.user_id + "/ads?start=0");
                string responseString = await subitoWebClient.DownloadStringTaskAsync(uri);
                Insertions insertions = JsonConvert.DeserializeObject<Insertions>(responseString);

                //deleting insertions
                foreach (Ad ad in insertions.ads)
                {
                    uri = new Uri(COOKIESURL + "/users/" + loginData.user_id + "/ads/" + ad.urn + "?delete_reason=sold_on_subito");
                    bool result = await subitoWebClient.DeleteRequest(uri);
                    await Task.Delay(1000);
                }

                return $"inserzioni rimosse {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [Route("GetReinsertAll")]
        public async Task<string> GetReinsertAll(string username = "", string password = "", string addressNewInserions = "")
        {
            try
            {
                SubitoWebClient subitoWebClient = new SubitoWebClient();
                Bitmap bitmap = new Bitmap(@"C:\Users\piemo\Desktop\OI0000211.jpg");
                //login to get cookies
                SubitoLoginDetail loginData = await LoginSubito(username,password,subitoWebClient, new Uri(COOKIESURL + "/users/login"));

                //getting the list of own insertions
                Uri uri = new Uri(COOKIESURL + "/users/" +  loginData.user_id + "/ads?start=0");
                string responseString = await subitoWebClient.DownloadStringTaskAsync(uri);
                Insertions insertions = JsonConvert.DeserializeObject<Insertions>(responseString);

                //inserting the new insertions.
                await subitoWebClient.GetRequest(new Uri("https://api2.subito.it:8443/api/v5/aij/form/0?v=5", UriKind.Absolute));
                await subitoWebClient.GetRequest(new Uri("https://api2.subito.it:8443/aij/init/0?v=5&v=5", UriKind.Absolute));
                await subitoWebClient.GetRequest(new Uri("https://api2.subito.it:8443/aij/load/0?v=5&v=5", UriKind.Absolute));
                await subitoWebClient.GetRequest(new Uri("https://api2.subito.it:8443/aij/form/0?v=5&v=5", UriKind.Absolute));
                
                //check
                await subitoWebClient.PostRequest("tos=1&ch=4&region=4&city=1&phone=3386231529&email=djpiemo%40gmail.com&body=Vendo+come+nuovo&phone_hidden=1&price=50&town=016008&category=44&company_ad=0&name=Lorenzo&subject=Gamecube&type=s", new Uri("https://api2.subito.it:8443/api/v5/aij/verify/0", UriKind.Absolute));

                //inserimento foto
                string imageToString = Convert.ToBase64String(File.ReadAllBytes(@"C:\Users\piemo\Desktop\OI0000211.png"));
                await subitoWebClient.PostImageRequest(imageToString, 44, new Uri("https://www2.subito.it/api/v5/aij/addimage/0", UriKind.Absolute));
                string temp = await subitoWebClient.PostImageRequest(imageToString, 44, new Uri("https://www2.subito.it/aij/addimage_form/0?v=5", UriKind.Absolute));
                

                //inserito
                string result = await subitoWebClient.PostRequest("tos=1&ch=4&region=4&city=1&phone=3386231529&email=djpiemo%40gmail.com&body=Vendo+come+nuovo&phone_hidden=1&price=50&town=016008&category=44&company_ad=0&name=Lorenzo&subject=Gamecube&type=s",new Uri("https://api2.subito.it:8443/api/v5/aij/create/0", UriKind.Absolute));

                return $"inserzioni aggiunte {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        public static async Task<SubitoLoginDetail> LoginSubito(string username, string password, SubitoWebClient webClient, Uri uri)
        {
            string loginString = "{ \"password\":\"" + password + "\",\"remember_me\":true,\"username\":\"" + username + "\"}";
            string responseString = await webClient.getLoginResponse(loginString, uri);
            return JsonConvert.DeserializeObject<SubitoLoginDetail>(responseString);
        }

    }

}


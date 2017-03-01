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
                string subitoResponse = await webClient.DownloadStringTaskAsync(new Uri(URL + parameter, UriKind.Relative));
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
                        latestInsertion.SubitoId = firstId;
                        SQLHelper.UpdateLatestInsertion(latestInsertion);
                        int currentId = firstId;
                        for (int i = 0; currentId > latestInsertion.SubitoId && i < insertions.ads.Count(); i++)
                        {
                            currentId = SubitoHelper.GetAdId(insertions.ads.ElementAt(i));
                            newAds.Add(insertions.ads.ElementAt(i));
                        }
                        latestInsertion.SubitoId = firstId;
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

        [Route("GetReinsertAll")]
        public async Task<string> GetReinsertAll(string username = "", string password = "", string addressNewInserions = "")
        {
            try
            {
                SubitoWebClient subitoWebClient = new SubitoWebClient();
                SubitoLoginDetail loginData = await LoginSubito(username,password,subitoWebClient, new Uri(COOKIESURL + "/users/login"));

                Uri uri = new Uri(COOKIESURL + "/users/" +  loginData.user_id + "/ads?start=0");
                string responseString = await subitoWebClient.DownloadStringTaskAsync(uri);
                Insertions insertions = JsonConvert.DeserializeObject<Insertions>(responseString);

                foreach (Ad ad in insertions.ads){
                    uri = new Uri(COOKIESURL + "/users/" + loginData.user_id + "/ads/" + ad.urn + "?delete_reason=sold_on_subito");
                    bool result = await subitoWebClient.DeleteRequest(uri);
                    await Task.Delay(5000);
                }

                return $"inserzioni rimosse e riaggiunte {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        public static async Task<SubitoLoginDetail> LoginSubito(string username, string password, SubitoWebClient webClient, Uri uri)
        {
            string loginString = "{ \"password\":\"" + password + "\",\"remember_me\":true,\"username\":\"" + username + "\"}";
            WebResponse response = await webClient.getLoginResponse(loginString, uri);
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseString = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<SubitoLoginDetail>(responseString);
            }
        }

    }

}


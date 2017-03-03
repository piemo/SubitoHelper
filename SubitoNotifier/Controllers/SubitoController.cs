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
        string INSERTIONURL = "https://api2.subito.it:8443"; //url per le api di inserimento nuove inserzioni

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
                //composing the string to be put as a parameter in the GetRequest to subito
                this.searchText = Uri.EscapeDataString(searchText);
                this.category = Uri.EscapeDataString(category.ToString());
                this.city = Uri.EscapeDataString(city.ToString());
                this.region = Uri.EscapeDataString(region.ToString());
                string parameters = $"/search/ads?lim={this.maxNum}&pin={this.pin}&sort={this.sort}&t={this.typeIns}";

                if (this.category != "")
                    parameters += $"&c={this.category}";

                if (this.city != "")
                    parameters += $"&ci={this.city}";

                if (this.region != "")
                    parameters += $"&r={this.region}";

                if (this.searchText != "")
                    parameters += $"&q={this.searchText}";

                //gets the list of insertion by calling the async method
                Insertions insertions = await getListInsertions(parameters);

                //begind working on the list received
                if(insertions.ads.Count>0)
                {
                    List<Ad> newAds = new List<Ad>(); //these are the new insertions to be sent by the telegram bot
                    var firstId = insertions.GetFirstAdId();
                    var latestInsertion = SQLHelper.GetLatestInsertionID(parameters); //grab the id of the last checked insertion with the same parameters
                    if (latestInsertion == null) // if there is no id, it means this is a new search
                    {
                        newAds.Add(insertions.ads.FirstOrDefault());
                        SQLHelper.InsertLatestInsertion(firstId, parameters); // insert a new line with these parameters
                    }
                    else if (firstId > latestInsertion.SubitoId) //if there is an id, we just update the line with the new top id and send all the new insertions to the telegram bot
                    {
                        for (int i = 0; i < insertions.ads.Count() && SubitoHelper.GetAdId(insertions.ads[i]) > latestInsertion.SubitoId; i++)
                        {
                            newAds.Add(insertions.ads.ElementAt(i));
                        }
                        latestInsertion.SubitoId = firstId;
                        SQLHelper.UpdateLatestInsertion(latestInsertion);
                    }

                    //this sends the messages to telegram
                    foreach (Ad ad in newAds) 
                    {
                        await SubitoHelper.sendTelegramInsertion(botToken, $"-{chatToken}", searchText, ad);
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
        public async Task<string> GetDeleteAll(string username , string password)
        {
            try
            {
                SubitoWebClient subitoWebClient = new SubitoWebClient();

                //login to get cookies
                SubitoLoginDetail loginData = await LoginSubito(username, password, subitoWebClient);

                //getting the list of own insertions
                Insertions insertions = await GetUserInsertionsByID(loginData.user_id, subitoWebClient);

                //deleting insertions
                foreach (Ad ad in insertions.ads)
                {
                    bool result = await DeleteInsertion(loginData.user_id, ad, subitoWebClient);
                    //if it couldn't delete the ad, throw an exception
                    if (result == false)
                        throw(new Exception());

                    //wait 1 sec
                    await Task.Delay(1000);
                }

                return $"{insertions.ads.Count} inserzioni rimosse {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        [Route("GetReinsertAll")]
        public async Task<string> GetReinsertAll(string username, string password , string addressNewInsertions )
        {
            try
            {
                SubitoWebClient subitoWebClient = new SubitoWebClient();
                
                //login to get cookies
                SubitoLoginDetail loginData = await LoginSubito(username,password,subitoWebClient);

                // Getting the list of insertions to post from a json on pastebin.com
                List<NewInsertion> newInsertions = new List<NewInsertion>();
                string responseString = await subitoWebClient.DownloadStringTaskAsync(new Uri("http://pastebin.com/raw/"+ addressNewInsertions));
                newInsertions = JsonConvert.DeserializeObject<List<NewInsertion>>(responseString);

                //inserting each new insertion
                foreach(NewInsertion ins in newInsertions)
                {
                    string result = await PostNewInsertion(ins, subitoWebClient);

                    //wait 1 sec
                    await Task.Delay(1000);
                }

                return $"{newInsertions.Count} inserzioni aggiunte {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public async Task<string> PostNewInsertion(NewInsertion newInsertion, SubitoWebClient subitoWebClient)
        {
            //calling these services to initiate the insertion procedure. can't skip these
            string response = await subitoWebClient.GetRequest(new Uri(INSERTIONURL + "/api/v5/aij/form/0?v=5", UriKind.Absolute));
            response = await subitoWebClient.GetRequest(new Uri(INSERTIONURL + "/aij/init/0?v=5&v=5", UriKind.Absolute));
            response = await subitoWebClient.GetRequest(new Uri(INSERTIONURL + "/aij/load/0?v=5&v=5", UriKind.Absolute));
            response = await subitoWebClient.GetRequest(new Uri(INSERTIONURL + "/aij/form/0?v=5&v=5", UriKind.Absolute));

            //check if the insertion with the datas (body, title etc) cab be posted with those datas
            response = await subitoWebClient.PostRequest(newInsertion.ToString(), new Uri(INSERTIONURL + "/api/v5/aij/verify/0", UriKind.Absolute));

            //inserting images
            foreach (string imageAddress in newInsertion.images)
            {
                //downloading the image from imgur first. The link is in the pastebin json file
                string imageToString = Convert.ToBase64String(subitoWebClient.DownloadData(new Uri(imageAddress)));
                //sending the image
                response = await subitoWebClient.PostImageRequest(imageToString, newInsertion.Category, new Uri(INSERTIONURL + "/api/v5/aij/addimage/0", UriKind.Absolute));
                //checking if the upload is ok. can't skip this
                response = await subitoWebClient.GetRequest(new Uri(INSERTIONURL + "/aij/addimage_form/0?v=5&category=" + newInsertion.Category, UriKind.Absolute));
            }

            //confirm the insertion
            return await subitoWebClient.PostRequest(newInsertion.ToString(), new Uri(INSERTIONURL + "/api/v5/aij/create/0", UriKind.Absolute));
        }

        public async Task<Insertions> GetUserInsertionsByID(int id, SubitoWebClient subitoWebClient)
        {
            //downloading the string of ads
            Uri uri = new Uri(COOKIESURL + "/users/" + id + "/ads?start=0");
            string responseString = await subitoWebClient.DownloadStringTaskAsync(uri);
            return JsonConvert.DeserializeObject<Insertions>(responseString);
        }

        public async Task<SubitoLoginDetail> LoginSubito(string username, string password, SubitoWebClient webClient)
        {
            //login. the uri and the body of the request must be formatted like this
            Uri uri =  new Uri(COOKIESURL + "/users/login");
            string loginString = "{ \"password\":\"" + password + "\",\"remember_me\":true,\"username\":\"" + username + "\"}";
            string responseString = await webClient.getLoginResponse(loginString, uri);
            return JsonConvert.DeserializeObject<SubitoLoginDetail>(responseString);
        }

        public async Task<bool> DeleteInsertion(int userID, Ad ad,SubitoWebClient subitoWebClient)
        {
            //delete request. the uri must be formatted like this
            Uri uri = new Uri(COOKIESURL + "/users/" + userID + "/ads/" + ad.urn + "?delete_reason=sold_on_subito");
            bool result = await subitoWebClient.DeleteRequest(uri);
            return result;
        }

        public async Task<Insertions> getListInsertions(string parameters) {
            //this gets the list of insertions for some specified parameters. The parameters must be correctly formatted beforehand and are not checked for their syntax.
            SubitoWebClient webClient = new SubitoWebClient();
            string subitoResponse = await webClient.DownloadStringTaskAsync(new Uri(URL + parameters, UriKind.Absolute));
            return JsonConvert.DeserializeObject<Insertions>(subitoResponse);
        }
    }

}


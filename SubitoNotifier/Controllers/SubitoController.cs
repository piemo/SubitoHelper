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
using Telegram.Bot;

namespace SubitoNotifier.Controllers
{
    [RoutePrefix("api/Subito")]
    public class SubitoController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
        string URL = "https://hades.subito.it/v1/search/ads?";  //url base subito
        string maxNum;          //quantità massima di inserzioni restituite
        string pin;             //da capire
        string searchText;      //stringa ricercata
        string sort;            //ordinamento risultati. Impostato su data decrescente
        string typeIns;         //da utilizzare per gli immobili. s= in vendita, u= in affitto, h= in affitto per vacanze, oppure "s,u,h" per tutte le inserzioni
        string category;        //2 auto,3 moto e scooter,4 veicoli commerciali,5 accessori auto,7 appartamenti,8 Uffici e Locali commerciali,9 Console e Videogiochi,10 Informatica,11 Audio/Video,12 telefonia
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
                string parameter = $"lim={this.maxNum}&pin={this.pin}&sort={this.sort}&t={this.typeIns}";

                if (this.category != "")
                    parameter += $"&c={this.category}";

                if (this.city != "")
                    parameter += $"&ci={this.city}";

                if (this.region != "")
                    parameter += $"&r={this.region}";

                if (this.searchText != "")
                    parameter += $"&q={this.searchText}";

                string subitoResponse = await GetSubitoResponse(parameter);
                var insertions = JsonConvert.DeserializeObject<Insertions>(subitoResponse);
                if(insertions.ads.Count>0)
                {
                    var firstId = insertions.GetFirstId();
                    var latestInsertion = SQLHelper.GetLatestInsertionID(this.searchText);
                    if (latestInsertion == null)
                    {
                        SQLHelper.InsertLatestInsertion(firstId, this.searchText);
                    }
                    else if (firstId > latestInsertion.SubitoId)
                    {
                        latestInsertion.SubitoId = firstId;
                        SQLHelper.UpdateLatestInsertion(latestInsertion);
                    }

                    //await sendTelegramInsertion(botToken, $"-{chatToken}", this.searchText, insertions.ads.FirstOrDefault());
                }
                return $"Controllato {DateTime.Now}";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }


        //[Route("InsertionBySellerName")]
        //public async Task<string> GetInsertionBySellerName(string token, string sellerName)
        //{
        //    try
        //    {
        //        var product = "SLLR:" + sellerName;
        //        var t = Uri.EscapeDataString("s,u,h");
        //        string parameter = $"lim={lim}&pin={pin}&sort=datedesc&t={t}";
        //        string url = $"https://hades.subito.it/v1/search/ads?{parameter}";
        //        Uri uri = new Uri(url, UriKind.Absolute);
        //        HttpClient client = new HttpClient();
        //        #region Headers
        //        client.DefaultRequestHeaders.Add("Accept", "*/*");
        //        client.DefaultRequestHeaders.Add("host", "hades.subito.it");
        //        client.DefaultRequestHeaders.Add("X-Subito-Channel", "50");
        //        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36");
        //        client.DefaultRequestHeaders.Add("Accept-Language", "it-IT;q=1, en-US;q=0.9");
        //        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        //        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        //        #endregion
        //        var latestInsertion = SQLHelper.GetLatestInsertionID(product);
        //        var stringResult = await client.GetStringWithGzipAsync(uri);
        //        var insertions = JsonConvert.DeserializeObject<Insertions>(stringResult);
        //        var idsToCheck = insertions.GetNewIds(latestInsertion);

        //        foreach (var id in idsToCheck)
        //        {

        //        }


        //        if (latestInsertion == null)
        //        {
        //            SQLHelper.InsertLatestInsertion(idsToCheck.FirstOrDefault(), product);
        //        }
        //        else if (idsToCheck.FirstOrDefault() > latestInsertion.SubitoId)
        //        {
        //            latestInsertion.SubitoId = idsToCheck.FirstOrDefault();
        //            SQLHelper.UpdateLatestInsertion(latestInsertion);
        //        }

        //        return $"Controllato {DateTime.Now}";
        //    }
        //    catch (Exception ex)
        //    {
        //        return ex.ToString();
        //    }
        //}

        private async Task<Telegram.Bot.Types.Message> sendTelegramInsertion(string botToken, string chatToken, string searchtext, Ad insertion)
        {
            var message = $"{searchText}: {insertion.features.FirstOrDefault(x => x.label == "Prezzo")?.values?.FirstOrDefault()?.value}\n{insertion.subject}\n\n{insertion.body}\n\n{insertion.urls.@default}";
            return await sendTelegramMessage(botToken, chatToken, searchText, message);
        }

        private async Task<Telegram.Bot.Types.Message> sendTelegramMessage(string botToken, string chatToken, string searchtext, string message)
        {
            var bot = new TelegramBotClient(botToken);
            return await bot.SendTextMessageAsync(chatToken, message);
        }

        private async Task<string> GetSubitoResponse(string parameter)
        {
            Uri uri = new Uri(URL + parameter, UriKind.Absolute);
            HttpClient client = new HttpClient();
            #region Headers
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("host", "hades.subito.it");
            client.DefaultRequestHeaders.Add("X-Subito-Channel", "50");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.99 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept-Language", "it-IT;q=1, en-US;q=0.9");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            #endregion
            return await client.GetStringWithGzipAsync(uri);
        }

    }
}

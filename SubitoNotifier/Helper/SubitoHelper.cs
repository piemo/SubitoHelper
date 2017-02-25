using SubitoNotifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot;

namespace SubitoNotifier.Helper
{
    public static class SubitoHelper
    {
        public static int GetFirstAdId(this Insertions insertions)
        {
            var firstId = GetAdId(insertions.ads.FirstOrDefault());
            return firstId;
        }

        public static int GetAdId(this Ad ad)
        {

            var firstInsertionUrl = ad.urls?.@default;
            if (firstInsertionUrl == null)
                throw new ArgumentNullException("firstInsertionUrl");
            var groups = Regex.Match(firstInsertionUrl, @"https?:\/\/www.*\/(?<id>\d+).*").Groups;
            var firstId = Convert.ToInt32(groups["id"].Value);
            return firstId;
        }

        public static int GetIdFromUrl(this string url)
        {
            if (url == null)
                throw new ArgumentNullException("firstInsertionUrl");
            var groups = Regex.Match(url, @"https?:\/\/www.*\/(?<id>\d+).*").Groups;
            var id = Convert.ToInt32(groups["id"].Value);
            return id;
        }

        public static IList<int> GetIds(this Insertions insertions)
        {
            var urls = insertions.ads.Select(x => x?.urls.@default);
            var ids = urls.Select(x => GetIdFromUrl(x));
            return ids.ToList();
        }

        public static IList<int> GetIds(this List<Ad> ads)
        {
            var urls = ads.Select(x => x?.urls.@default);
            var ids = urls.Select(x => GetIdFromUrl(x));
            return ids.ToList();
        }

        public static async Task<Telegram.Bot.Types.Message> sendTelegramInsertion(string botToken, string chatToken, string searchText, Ad insertion)
        {
            var message = $"{searchText}: {insertion.features.FirstOrDefault(x => x.label == "Prezzo")?.values?.FirstOrDefault()?.value}\n{insertion.subject}\n\n{insertion.body}\n\n{insertion.urls.@default}";
            return await sendTelegramMessage(botToken, chatToken, searchText, message);
        }

        private static async Task<Telegram.Bot.Types.Message> sendTelegramMessage(string botToken, string chatToken, string searchtext, string message)
        {
            var bot = new TelegramBotClient(botToken);
            return await bot.SendTextMessageAsync(chatToken, message);
        }


    }
}
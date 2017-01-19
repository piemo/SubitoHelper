using SubitoNotifier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SubitoNotifier.Helper
{
    public static class SubitoHelper
    {
        public static int GetFirstId(this Insertions insertions)
        {
            var firstInsertionUrl = insertions.ads.FirstOrDefault()?.urls?.@default;
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

        //public static IList<int> GetNew(this Insertions insertions, LatestInsertion latestInsertion)
        //{
        //    var ids = insertions.GetIds().Where(x => x > latestInsertion.SubitoId).OrderBy(x=>x).ToList();
            
        //}
    }
}
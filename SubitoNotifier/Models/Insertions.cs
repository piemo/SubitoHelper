using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SubitoNotifier.Models
{
    public class Insertions
    {
        public int count_all { get; set; }
        public int lines { get; set; }
        public int start { get; set; }
        public string pin { get; set; }
        public string checknew { get; set; }
        public List<Ad> ads { get; set; }
    }

    public class Type
    {
        public string key { get; set; }
        public string value { get; set; }
        public int weight { get; set; }
    }

    public class Category
    {
        public string key { get; set; }
        public string value { get; set; }
        public string friendly_name { get; set; }
        public string macrocategory_id { get; set; }
        public int weight { get; set; }
    }

    public class Dates
    {
        public string display { get; set; }
    }

    public class Scale
    {
        public string uri { get; set; }
        public string secureuri { get; set; }
        public string size { get; set; }
    }

    public class Image
    {
        public string uri { get; set; }
        public List<Scale> scale { get; set; }
    }

    public class Value
    {
        public string key { get; set; }
        public string value { get; set; }
    }

    public class Feature
    {
        public string type { get; set; }
        public string uri { get; set; }
        public string label { get; set; }
        public List<Value> values { get; set; }
    }

    public class Advertiser
    {
        public string user_id { get; set; }
        public string name { get; set; }
        public string phone { get; set; }
        public bool company { get; set; }
    }

    public class Region
    {
        public string key { get; set; }
        public string value { get; set; }
        public string friendly_name { get; set; }
        public string label { get; set; }
        public int level { get; set; }
        public string neighbors { get; set; }
    }

    public class City
    {
        public string key { get; set; }
        public string value { get; set; }
        public string label { get; set; }
        public string friendly_name { get; set; }
        public string short_name { get; set; }
        public int level { get; set; }
        public string istat { get; set; }
        public string region_id { get; set; }
    }

    public class Town
    {
        public string key { get; set; }
        public string value { get; set; }
        public string label { get; set; }
        public int level { get; set; }
        public string istat { get; set; }
        public string region_id { get; set; }
        public string city_id { get; set; }
        public bool has_zone { get; set; }
        public string friendly_name { get; set; }
    }

    public class Map
    {
        public string address { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string zoom { get; set; }
        public string show_pin { get; set; }
    }

    public class Geo
    {
        public Region region { get; set; }
        public City city { get; set; }
        public Town town { get; set; }
        public string uri { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public Map map { get; set; }
    }

    public class Urls
    {
        public string @default { get; set; }
        public string mobile { get; set; }
    }

    public class Ad
    {
        public string urn { get; set; }
        public Type type { get; set; }
        public Category category { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public Dates dates { get; set; }
        public List<Image> images { get; set; }
        public List<Feature> features { get; set; }
        public Advertiser advertiser { get; set; }
        public Geo geo { get; set; }
        public Urls urls { get; set; }
    }
}
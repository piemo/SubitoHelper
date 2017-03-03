using System;
using System.Collections.Generic;
using System.Web;

namespace SubitoNotifier.Models
{
    public class NewInsertion
    {
        private int tos = 1;
        private int ch = 4;
        private int company_ad = 0;
        private string type = "s";
        public int Region { get; set; }
        public int City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Body { get; set; }
        public int Phone_hidden { get; set; }
        public int Price { get; set; }
        public string Town { get; set; }
        public int Category { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public List<string> images { get; set; }

        public override string ToString() //method to be used when inserting a new add. return an encoded string which must be used
        {

            return $"tos={tos}&ch={ch}&region={Region}&city={City}&phone={Phone}&email={HttpUtility.UrlEncode(Email)}&body={HttpUtility.UrlEncode(Body)}&phone_hidden={Phone_hidden}&price={Price}&town={Town}&category={Category}&company_ad={company_ad}&name={Name}&subject={Subject}&type={type}";
        }

    }
}
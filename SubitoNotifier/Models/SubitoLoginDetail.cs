using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SubitoNotifier.Models
{
    public class SubitoLoginDetail
    {
        public string secret { get; set; }
        public int user_id { get; set; }
        public int tracking_id { get; set; }
    }
}
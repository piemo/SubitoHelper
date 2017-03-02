using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SubitoNotifier.Models
{
    public class SubitoImageUploadResponse
    {
        public string status { get; set; }
        public int max_images { get; set; }
        public int images_left { get; set; }
        public string action { get; set; }
        public string image_url { get; set; }
        public string image_name { get; set; }
        public Ad ad { get; set; }
        public string s { get; set; }
    }
}
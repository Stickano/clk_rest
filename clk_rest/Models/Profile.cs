using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class Profile
    {
        public string email { get; set; }
        public string password { get; set; }
        public string created { get; set; }
        public string id { get; set; }
        public string username { get; set; }
    }
}
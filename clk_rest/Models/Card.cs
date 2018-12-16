using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class Card
    {
        public string id { get; set; }
        public string name { get; set; }
        public string created { get; set; }
        public string listId { get; set; }
        public string description { get; set; }
    }
}
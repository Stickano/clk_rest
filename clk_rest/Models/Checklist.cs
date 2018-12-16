using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class Checklist
    {
        public string id { get; set; }
        public string name { get; set; }
        public string cardId { get; set; }
        public string created { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class List
    {
        public string id { get; set; }
        public string name { get; set; }
        public string boardId { get; set; }
        public string created { get; set; }
        public bool active { get; set; } = true;
    }
}
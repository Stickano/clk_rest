using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class Comment
    {
        public string comment { get; set; }
        public string created { get; set; }
        public string cardId { get; set; }
        public string userId { get; set; }
        public string id { get; set; }
    }
}
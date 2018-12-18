using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class BoardMember
    {
        public string boardId { get; set; }
        public string userId { get; set; }
        public string email { get; set; }
        public string username { get; set; }
    }
}
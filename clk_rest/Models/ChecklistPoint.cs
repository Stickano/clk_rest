using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class ChecklistPoint
    {
        public string name { get; set; }
        public string id { get; set; }
        public string checklistId { get; set; }
        public string created { get; set; }
        public bool isCheck { get; set; }
        public bool active { get; set; } = true;
    }
}
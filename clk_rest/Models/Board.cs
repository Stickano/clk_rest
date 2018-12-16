using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Models
{
    public class Board
    {
        public string id { get; set; } 
        public string name { get; set; }
        public string created { get; set; }

        public List<List> lists { get; set; }
        public List<Card> cards { get; set; }
        public List<Checklist> checklists { get; set; }
        public List<ChecklistPoint> points { get; set; }
        public List<Comment> comments { get; set; }

        public string userId { get; set; }
        public string password { get; set; }

        public Board()
        {
            id = "";
            name = "";
            created = "";

            lists = new List<List>();
            cards = new List<Card>();
            checklists = new List<Checklist>();
            points = new List<ChecklistPoint>();
            comments = new List<Comment>();

            userId = "";
            password = "";
        }
    }
}
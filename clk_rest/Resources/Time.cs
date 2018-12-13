using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace clk_rest.Resources
{
    public class Time
    {
        public static string timestamp(string format = "dd.MM.yyyy HH:mm")
        {
            return DateTime.Now.ToString(format);
        }
    }
}
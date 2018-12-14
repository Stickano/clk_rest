using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace clk_rest.Resources
{
    public static class Validators
    {

        /// <summary>
        /// This will validate, that it is a valid email when creating new profiles i.e.
        /// and not just some bogus value.
        /// </summary>
        /// <param name="address">The email address to validate</param>
        /// <returns>True/False if correct email address format</returns>
        public static bool isMail(string address)
        {
            try
            {
                new MailAddress(address);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
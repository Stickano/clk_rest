using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace clk_rest
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService1
    {
        [WebInvoke(UriTemplate = "profile/create/{token}",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        string CreateProfile(string token, Profile profile);

        [WebInvoke(UriTemplate = "profile/read/{token}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        [OperationContract]
        IList<Profile> GetAllProfiles(string token);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using clk_rest.Models;

namespace clk_rest
{
    [ServiceContract]
    public interface IService1
    {
        [WebInvoke(UriTemplate = "profile/create",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        int createProfile(Profile profile);

        [WebInvoke(UriTemplate = "profile/login",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        [OperationContract]
        Profile login(Profile profile);

        //[WebInvoke(UriTemplate = "peek",
        //    Method = "GET",
        //    ResponseFormat = WebMessageFormat.Json)]
        //[OperationContract]
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Proxies
{
    public class ProxyInfo
    {
        #region ** Proxies Attributes **

        public string ip   { get; set; }
        public string port { get; set; }
        public string psw  { get; set; }
        public string user { get; set; }

        #endregion

        #region ** Helper Methods **

        public WebProxy AsWebProxy()
        {
            // Creating URI from proxy info
            string URI = String.Format ("{0}:{1}", ip, port);

            //Set credentials
            ICredentials credentials = new NetworkCredential (user, psw);

            // New Instance of Web Proxy
            return new WebProxy (URI, true, null, credentials);
        }

        #endregion

    }
}

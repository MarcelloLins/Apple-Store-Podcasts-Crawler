using SharedLibrary.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebUtilsLib;

namespace SharedLibrary
{
    public class RequestsHandler
    {
        public string GetRootPage (bool useProxies)
        {
            string htmlResponse = String.Empty;
            int currentRetry = 0, maxRetries = 100;

            using (WebRequests httpClient = new WebRequests ())
            {
                // (Re) Trying to reach Root Page
                do
                {
                    // Should this request use HTTP Proxies ?
                    if (useProxies)
                    {
                        httpClient.Proxy = ProxiesLoader.GetWebProxy ();
                    }

                    htmlResponse = httpClient.Get (Consts.ROOT_STORE_URL);

                    currentRetry++;

                } while (String.IsNullOrEmpty (htmlResponse) && currentRetry <= maxRetries);
            }

            return htmlResponse;
        }

        public string Get (string url, bool useProxies)
        {
            using (WebRequests httpClient = new WebRequests ())
            {
                // Should this request use HTTP Proxies ?
                if (useProxies)
                {
                    httpClient.Proxy = ProxiesLoader.GetWebProxy ();
                }

                httpClient.UserAgent = Consts.USER_AGENT;
                string htmlResponse  = httpClient.Get (url);

                return htmlResponse;
            }
        }
    }
}

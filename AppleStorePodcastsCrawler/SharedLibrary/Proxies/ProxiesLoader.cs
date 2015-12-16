using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Proxies
{
    public class ProxiesLoader
    {
        private static int _currentProxy = 0;

        private static List<ProxyInfo> _loadedProxies { get; set; }

        public static void Load (List<String> proxies)
        {
            // Housekeeping
            Clear (proxies.Count);

            // Creating Randomizer for adding proxies to the list
            Random randomizer = new Random ();

            // Adding proxies from the file
            foreach (string proxyData in proxies)
            {
                // Splitting String for it's data
                string[] proxyInfo = proxyData.Split (':');

                ProxyInfo pInfo;

                // Checking for "No Login and Password Proxy"
                if (proxyInfo.Length == 2)
                {
                    // Building Proxy Info Instance - With no Credentials
                    pInfo = new ProxyInfo ()
                    {
                        ip = proxyInfo[0],
                        port = proxyInfo[1]
                    };
                }
                else // Proxy Length expected is 4 (Address,Port,Username,Password)
                {
                    // Building Proxy Info Instance - With Credentials
                    pInfo = new ProxyInfo ()
                    {
                        ip = proxyInfo[0],
                        port = proxyInfo[1],
                        user = proxyInfo[2],
                        psw = proxyInfo[3]
                    };
                }

                // Sanity Check
                if (_loadedProxies.Count > 0)
                {

                    // Adding Proxy Info to the List
                    _loadedProxies.Insert (randomizer.Next (_loadedProxies.Count), pInfo);
                }
                else
                {
                    _loadedProxies.Add (pInfo);
                }
            }
        }

        /// <summary>
        /// Clears the current list of proxies
        /// </summary>
        private static void Clear (int size)
        {
            _loadedProxies = new List<ProxyInfo> (size);
        }

        /// <summary>
        /// Casts the ProxyInfo Object
        /// to an instance of WebProxy
        /// </summary>
        /// <returns></returns>
        public static WebProxy GetWebProxy ()
        {
            // Getting "Next" Proxy
            int proxyIndex = (_currentProxy++ % _loadedProxies.Count);

            // Returning new instance of Web Proxy to be used
            return _loadedProxies[proxyIndex].AsWebProxy ();
        }
    }
}

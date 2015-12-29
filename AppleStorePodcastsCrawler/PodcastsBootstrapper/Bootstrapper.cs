using NLog;
using SharedLibrary;
using SharedLibrary.AWS;
using SharedLibrary.ConfigurationReader;
using SharedLibrary.Log;
using SharedLibrary.Parsing;
using SharedLibrary.Proxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PodcastsBootstrapper
{
    class Bootstrapper
    {
        // Logging Tool
        private static Logger _logger;

        // Configuration Values
        private static string _categoriesQueueName;
        private static string _awsKey;
        private static string _awsKeySecret;

        static void Main (string[] args)
        {
            // Creating Needed Instances
            RequestsHandler httpClient = new RequestsHandler ();
            PodcastsParser  parser     = new PodcastsParser ();

            // Setting Up Log
            LogSetup.InitializeLog ("Apple_Podcasts_Bootstrapper.log", "info");
            _logger = LogManager.GetCurrentClassLogger ();

            // Starting Flow
            _logger.Info ("Boostrapper Started");

            // Loading Configuration
            _logger.Info ("Reading Configuration");
            LoadConfiguration ();

            // Control Variable (Bool - Should the process use proxies? )
            bool shouldUseProxies = false;

            // Checking for the need to use proxies
            if (args != null && args.Length == 1)
            {
                // Setting flag to true
                shouldUseProxies = true;

                // Loading proxies from .txt received as argument
                String fPath = args[0];

                // Sanity Check
                if (!File.Exists (fPath))
                {
                    _logger.Fatal ("Couldnt find proxies on path : " + fPath);
                    System.Environment.Exit (-100);
                }

                // Reading Proxies from File
                string[] fLines = File.ReadAllLines (fPath, Encoding.GetEncoding ("UTF-8"));

                try
                {
                    // Actual Load of Proxies
                    ProxiesLoader.Load (fLines.ToList ());
                }
                catch (Exception ex)
                {
                    _logger.Fatal (ex);
                    System.Environment.Exit (-101);
                }
            }

            // AWS Queue Handler
            _logger.Info ("Initializing Queues");
            AWSSQSHelper sqsWrapper = new AWSSQSHelper (_categoriesQueueName, 10, Amazon.RegionEndpoint.USEast1, _awsKey, _awsKeySecret);

            // Step 1 - Trying to obtain the root page html (source of all the apps)
            var rootPageResponse = httpClient.GetRootPage (shouldUseProxies);

            // Sanity Check
            if (String.IsNullOrWhiteSpace (rootPageResponse))
            {
                _logger.Info ("Error obtaining Root Page HTMl - Aborting", "Timeout Error");
                return;
            }

            // Step 2 - Extracting Category Urls from the Root Page and queueing their Urls
            // Logging Feedback
            _logger.Info ("Queueing Categories");

            // Queueing Category Urls
            var categoryUrls = parser.ParseCategoryUrls (rootPageResponse).ToList ();

            sqsWrapper.EnqueueMessages (categoryUrls);

            _logger.Info ("End of Bootstrapping phase");
        }

        private static void LoadConfiguration ()
        {
            _categoriesQueueName = ConfigurationReader.LoadConfigurationSetting<String> ("AWSCategoriesQueue", String.Empty);
            _awsKey              = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKey"            , String.Empty);
            _awsKeySecret        = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKeySecret"      , String.Empty);
        }
    }
}

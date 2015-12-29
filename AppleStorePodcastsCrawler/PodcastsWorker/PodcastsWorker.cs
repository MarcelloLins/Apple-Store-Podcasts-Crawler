using NLog;
using SharedLibrary;
using SharedLibrary.AWS;
using SharedLibrary.ConfigurationReader;
using SharedLibrary.Log;
using SharedLibrary.Models;
using SharedLibrary.MongoDB;
using SharedLibrary.Parsing;
using SharedLibrary.Proxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PodcastsWorker
{
    class PodcastsWorker
    {
        // Logging Tool
        private static Logger _logger;

        // Configuration Values
        private static string _podcastUrlsQueueName;
        private static string _awsKey;
        private static string _awsKeySecret;
        private static int    _maxRetries;
        private static int    _maxMessagesPerDequeue;

        // Configuration - Database
        private static string _mongoServer;
        private static int    _mongoPort;     
        private static string _mongoUser;     
        private static string _mongoPass;
        private static string _mongoDatabase;
        private static string _mongoCollection;
        private static string _mongoAuthDatabase;
        private static int    _mongoTimeout;

        // Control Variables
        private static int    _hiccupTime = 1000;

        static void Main (string[] args)
        {
             // Creating Needed Instances
            RequestsHandler httpClient = new RequestsHandler ();
            PodcastsParser  parser     = new PodcastsParser ();

            // Loading Configuration
            LogSetup.InitializeLog ("Apple_Store_Urls_Worker.log", "info");
            _logger = LogManager.GetCurrentClassLogger ();

            // Loading Config
            _logger.Info ("Loading Configurations from App.config");
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

            // MongoDB
            _logger.Info ("Initializing Database");
            MongoDBWrapper mongoDB = new MongoDBWrapper ();
            string serverAddr = String.Join (":", _mongoServer, _mongoPort);
            mongoDB.ConfigureDatabase (_mongoUser, _mongoPass, _mongoAuthDatabase, serverAddr, _mongoTimeout, _mongoDatabase, _mongoCollection);

            // AWS Queue Handler
            _logger.Info ("Initializing Queues");
            AWSSQSHelper podcastsUrlsQueue = new AWSSQSHelper (_podcastUrlsQueueName , _maxMessagesPerDequeue, Amazon.RegionEndpoint.USEast1, _awsKey, _awsKeySecret);
                        
            // Setting Error Flag to No Error ( 0 )
            System.Environment.ExitCode = 0;

            // Initialiazing Control Variables
            int fallbackWaitTime = 1;

            _logger.Info ("Started Processing Individual Podcast Urls");

            do
            {
                try
                {
                    // Dequeueing messages from the Queue
                    if (!podcastsUrlsQueue.DeQueueMessages ())
                    {
                        Thread.Sleep (_hiccupTime); // Hiccup                   
                        continue;
                    }

                    // Checking for no message received, and false positives situations
                    if (!podcastsUrlsQueue.AnyMessageReceived ())
                    {
                        // If no message was found, increases the wait time
                        int waitTime;
                        if (fallbackWaitTime <= 12)
                        {
                            // Exponential increase on the wait time, truncated after 12 retries
                            waitTime = Convert.ToInt32 (Math.Pow (2, fallbackWaitTime) * 1000);
                        }
                        else // Reseting Wait after 12 fallbacks
                        {
                            waitTime = 2000;
                            fallbackWaitTime = 0;
                        }

                        fallbackWaitTime++;

                        // Sleeping before next try
                        Console.WriteLine ("Fallback (seconds) => " + waitTime);
                        Thread.Sleep (waitTime);
                        continue;
                    }

                    // Reseting fallback time
                    fallbackWaitTime = 1;

                    // Iterating over dequeued Messages
                    foreach (var appUrl in podcastsUrlsQueue.GetDequeuedMessages ())
                    {
                        if (appUrl.Body.IndexOf ("https://itunes.apple.com/us/podcast") < 0 && appUrl.Body.IndexOf ("https://itunes.apple.com/podcast/") < 0)
                        {
                            _logger.Info ("Invalid Message. Deleting [{0}]", appUrl.Body);
                            podcastsUrlsQueue.DeleteMessage (appUrl);
                            continue;
                        }

                        bool processingWorked = true;

                        try
                        {
                            // Retries Counter
                            int retries = 0;
                            string htmlResponse;

                            // Retrying if necessary
                            do
                            {
                                // Executing Http Request for the Category Url
                                htmlResponse = httpClient.Get (appUrl.Body, shouldUseProxies);

                                if (String.IsNullOrEmpty (htmlResponse))
                                {
                                    // Extending Fallback time
                                    retries++;
                                    int sleepTime = retries * _hiccupTime <= 30000 ? retries * _hiccupTime : 30000;

                                    _logger.Info ("Retrying Request for Podcast Page [ " + sleepTime / 1000 + " ]");

                                    Thread.Sleep (sleepTime);
                                }

                            } while (String.IsNullOrWhiteSpace (htmlResponse) && retries <= _maxRetries);

                            // Checking if retries failed
                            if (String.IsNullOrWhiteSpace (htmlResponse))
                            {
                                continue;
                            }

                            // Feedback
                            _logger.Info ("Current page " + appUrl.Body, "Parsing Podcast Data");

                            // Parsing Data out of the Html Page
                            AppleStorePodcastModel parsedPodcast = parser.ParsePodcastPage (htmlResponse);
                            parsedPodcast.url                    = appUrl.Body;
                            parsedPodcast._id                    = appUrl.Body;

                            // Storing Podcast Data on MongoDB
                            mongoDB.Insert (parsedPodcast);

                            // Little Hiccup
                            Thread.Sleep (_hiccupTime);

                        }
                        catch (Exception ex)
                        {
                            _logger.Error (ex);

                            // Setting Flag to "False"
                            processingWorked = false;
                        }
                        finally
                        {
                            //Deleting the message - Only if the processing worked
                            if (processingWorked)
                            {
                                podcastsUrlsQueue.DeleteMessage (appUrl);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error (ex);
                }

            } while (true);
        }

        private static void LoadConfiguration ()
        {
            _maxRetries             = ConfigurationReader.LoadConfigurationSetting<int>    ("MaxRetries"           , 0);
            _maxMessagesPerDequeue  = ConfigurationReader.LoadConfigurationSetting<int>    ("MaxMessagesPerDequeue", 10);
            _podcastUrlsQueueName   = ConfigurationReader.LoadConfigurationSetting<String> ("AWSPodcastUrlsQueue"  , String.Empty);
            _awsKey                 = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKey"               , String.Empty);
            _awsKeySecret           = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKeySecret"         , String.Empty);

            _mongoServer            = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_SERVER"         , String.Empty);
            _mongoPort              = ConfigurationReader.LoadConfigurationSetting<int> ("MONGO_PORT"              , 21766);
            _mongoUser              = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_USER"           , String.Empty);
            _mongoPass              = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_PASS"           , String.Empty);
            _mongoDatabase          = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_DATABASE"       , String.Empty);
            _mongoCollection        = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_COLLECTION"     , String.Empty);
            _mongoAuthDatabase      = ConfigurationReader.LoadConfigurationSetting<String> ("MONGO_AUTH_DB"        , String.Empty);
            _mongoTimeout           = ConfigurationReader.LoadConfigurationSetting<int> ("MONGO_TIMEOUT"           , 16000);
        }
    }
}

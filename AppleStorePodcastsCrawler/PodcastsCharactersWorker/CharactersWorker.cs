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
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PodcastsCharactersWorker
{
    class Program
    {
        // Logging Tool
        private static Logger _logger;

        // Configuration Values
        private static string _characterUrlsQueueName;
        private static string _podcastUrlsQueueName;
        private static string _awsKey;
        private static string _awsKeySecret;
        private static int    _maxRetries;
        private static int    _maxMessagesPerDequeue;

        // Control Variables
        private static int    _hiccupTime = 1000;

        static void Main (string[] args)
        {
            // Creating Needed Instances
            RequestsHandler httpClient = new RequestsHandler ();
            PodcastsParser  parser     = new PodcastsParser ();

            // Loading Configuration
            LogSetup.InitializeLog ("Apple_Podcasts_CharacterUrls_Worker.log", "info");
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

            // AWS Queue Handler
            _logger.Info ("Initializing Queues");
            AWSSQSHelper charactersUrlQueue = new AWSSQSHelper (_characterUrlsQueueName, _maxMessagesPerDequeue, Amazon.RegionEndpoint.USEast1, _awsKey, _awsKeySecret);
            AWSSQSHelper podcastUrlsQueue   = new AWSSQSHelper (_podcastUrlsQueueName  , _maxMessagesPerDequeue, Amazon.RegionEndpoint.USEast1, _awsKey, _awsKeySecret);

            // Setting Error Flag to No Error ( 0 )
            System.Environment.ExitCode = 0;

            // Initialiazing Control Variables
            int fallbackWaitTime = 1;

            _logger.Info ("Started Processing Character Urls");

            do
            {
                try
                {
                    // Dequeueing messages from the Queue
                    if (!charactersUrlQueue.DeQueueMessages ())
                    {
                        Thread.Sleep (_hiccupTime); // Hiccup                   
                        continue;
                    }

                    // Checking for no message received, and false positives situations
                    if (!charactersUrlQueue.AnyMessageReceived ())
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
                    foreach (var characterUrl in charactersUrlQueue.GetDequeuedMessages ())
                    {
                        // Console Feedback
                        _logger.Info ("Started Parsing Url : " + characterUrl.Body);

                        try
                        {
                            // Retries Counter
                            int retries = 0;
                            string htmlResponse;

                            // Retrying if necessary
                            do
                            {
                                // Executing Http Request for the Category Url
                                htmlResponse = httpClient.Get (characterUrl.Body, shouldUseProxies);

                                if (String.IsNullOrEmpty (htmlResponse))
                                {
                                    _logger.Info ("Retrying Request for Character Page");
                                    retries++;

                                    // Small Hiccup
                                    Thread.Sleep (_hiccupTime);
                                }

                            } while (String.IsNullOrWhiteSpace (htmlResponse) && retries <= _maxRetries);

                            // Checking if retries failed
                            if (String.IsNullOrWhiteSpace (htmlResponse))
                            {
                                // Deletes Message and moves on
                                charactersUrlQueue.DeleteMessage (characterUrl);
                                continue;
                            }

                            // Hashset of urls processed (to avoid duplicates)
                            HashSet<String> urlsQueued = new HashSet<String> ();

                            // If the URL is not a "Page" Url and for the presence of "Page" itens within the HTML
                            if (characterUrl.Body.IndexOf ("&page=", StringComparison.InvariantCultureIgnoreCase) < 0 && parser.HasPageIndexes(htmlResponse))
                            {
                                // Feedback
                                _logger.Info ("Found 'ROOT' URL For this Category : {0}", characterUrl.Body);

                                // Executing Request and Queueing Urls until there's no other Url to be queued
                                // If the request worked, parses the Urls out of the page and queue them into the same queue
                                var numericUrls = parser.ParseNumericUrls (htmlResponse).Select (t => HttpUtility.HtmlDecode (t)).Distinct().ToList();
                                
                                // Enqueueing Urls
                                charactersUrlQueue.EnqueueMessages (numericUrls);                   
                            }
                            else // The Url is a "Page" one
                            {
                                // Parsing Podcast urls
                                // Feedback
                                _logger.Info ("Found 'PAGE NUMBER' URL For this Category : {0}", characterUrl.Body);

                                var podcastUrls = parser.ParsePodcastUrls (htmlResponse).Select (t => HttpUtility.HtmlDecode (t)).ToList ();
                                
                                // Decoding the urls
                                podcastUrls.ForEach (t => HttpUtility.HtmlDecode (t));

                                // Enqueueing Urls
                                podcastUrlsQueue.EnqueueMessages (podcastUrls);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error (ex);
                        }
                        finally
                        {
                            charactersUrlQueue.DeleteMessage (characterUrl);
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
            _characterUrlsQueueName = ConfigurationReader.LoadConfigurationSetting<String> ("AWSCharacterUrlsQueue", String.Empty);
            _podcastUrlsQueueName   = ConfigurationReader.LoadConfigurationSetting<String> ("AWSPodcastUrlsQueue"  , String.Empty);
            _awsKey                 = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKey"               , String.Empty);
            _awsKeySecret           = ConfigurationReader.LoadConfigurationSetting<String> ("AWSKeySecret"         , String.Empty);
        }
    }
}

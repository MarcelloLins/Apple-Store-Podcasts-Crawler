using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace SharedLibrary.AWS
{
    /// <summary>
    /// Refer to http://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/Welcome.html for the online API reference
    /// </summary>
    public class AWSSQSHelper
    {
        ///////////////////////////////////////////////////////////////////////
        //                           Fields                                  //
        ///////////////////////////////////////////////////////////////////////

        public IAmazonSQS queue { get; set; }   // AMAZON simple queue service reference
        public GetQueueUrlResponse queueurl { get; set; }   // AMAZON queue url
        public ReceiveMessageRequest rcvMessageRequest { get; set; }   // AMAZON receive message request
        public ReceiveMessageResponse rcvMessageResponse { get; set; }   // AMAZON receive message response
        public DeleteMessageRequest delMessageRequest { get; set; }   // AMAZON delete message request

        public bool IsValid { get; set; }   // True when the queue is OK

        public int ErrorCode { get; set; }   // Last error code
        public string ErrorMessage { get; set; }   // Last error message

        public const int e_Exception = -1;

        public const int AmazonSQSMaxMessageSize = 256 * 1024;                  // AMAZON queue max message size

        public string AWSAccessKey;
        public string AWSSecretKey;


        ///////////////////////////////////////////////////////////////////////
        //                    Methods & Functions                            //
        ///////////////////////////////////////////////////////////////////////



        /// <summary>
        /// This method initializes the client in order to avoid writing AWS AccessKey and SecretKey for testing zith XUnit
        /// </summary>
        /// <param name="regionEndpoint"></param>
        /// <param name="AWSAcessKey"></param>
        /// <param name="AWSSecretKey"></param>
        //private void Initialize (RegionEndpoint regionEndpoint, string AWSAcessKey, string AWSSecretKey)
        //{
        //    // Create SQS client
        //    IAmazonSQS queueClient = AWSClientFactory.CreateAmazonSQSClient (
        //                    AWSAcessKey,
        //                    AWSSecretKey,
        //                    regionEndpoint);
        //}

        /// <summary>
        /// This static method creates an SQS queue to be used later. For parameter definitions beyond error message, 
        /// please check the online documentation (http://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_CreateQueue.html)
        /// </summary>
        /// <param name="QueueName">Name of the queue to be created</param>
        /// <param name="RegionEndpoint">Endpoint corresponding to the AWS region where the queue should be created</param>
        /// <param name="ErrorMessage">String that will receive the error message, if an error occurs</param>
        /// <returns>Boolean indicating if the queue was created</returns>        
        public static bool CreateSQSQueue (string QueueName, RegionEndpoint RegionEndpoint, out string ErrorMessage, int DelaySeconds = 0, int MaximumMessageSize = AmazonSQSMaxMessageSize,
                                           int MessageRetentionPeriod = 345600, int ReceiveMessageWaitTimeSeconds = 0, int VisibilityTimeout = 30, string Policy = "",
                                           string AWSAccessKey = "", string AWSSecretKey = "")
        {
            bool result = false;
            ErrorMessage = "";

            // Validate and adjust input parameters
            DelaySeconds = Math.Min (Math.Max (DelaySeconds, 0), 900);
            MaximumMessageSize = Math.Min (Math.Max (MaximumMessageSize, 1024), AmazonSQSMaxMessageSize);
            MessageRetentionPeriod = Math.Min (Math.Max (MessageRetentionPeriod, 60), 1209600);
            ReceiveMessageWaitTimeSeconds = Math.Min (Math.Max (ReceiveMessageWaitTimeSeconds, 0), 20);
            VisibilityTimeout = Math.Min (Math.Max (VisibilityTimeout, 0), 43200);

            if (!String.IsNullOrWhiteSpace (QueueName))
            {
                IAmazonSQS queueClient;

                if (!String.IsNullOrEmpty (AWSAccessKey))
                {
                    queueClient = AWSClientFactory.CreateAmazonSQSClient (AWSAccessKey, AWSSecretKey, RegionEndpoint);
                }
                else
                {
                    queueClient = AWSClientFactory.CreateAmazonSQSClient (RegionEndpoint);
                }
                try
                {
                    // Generate the queue creation request
                    CreateQueueRequest createRequest = new CreateQueueRequest ();
                    createRequest.QueueName = QueueName;

                    // Add other creation parameters
                    createRequest.Attributes.Add ("DelaySeconds", DelaySeconds.ToString ());
                    createRequest.Attributes.Add ("MaximumMessageSize", MaximumMessageSize.ToString ());
                    createRequest.Attributes.Add ("MessageRetentionPeriod", MessageRetentionPeriod.ToString ());
                    createRequest.Attributes.Add ("ReceiveMessageWaitTimeSeconds", ReceiveMessageWaitTimeSeconds.ToString ());
                    createRequest.Attributes.Add ("VisibilityTimeout", VisibilityTimeout.ToString ());

                    // Run the request
                    CreateQueueResponse createResponse = queueClient.CreateQueue (createRequest);

                    // Check for errros
                    if (createResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        ErrorMessage = "An error occurred while creating the queue. Please try again.";
                    }

                    result = true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }
            else
            {
                ErrorMessage = "Invalid Queue Name";
            }

            return result;
        }

        /// <summary>
        /// This static method deletes a SQS queue. Once deleted, the queue and any messages on it will no longer be available.
        /// </summary>
        /// <param name="QueueName">The name of the queue to be deleted</param>
        /// <param name="RegionEndpoint">Endpoint corresponding to the AWS region where the queue is located</param>
        /// <param name="ErrorMessage">String that will receive the error message, if an error occurs</param>
        /// <returns></returns>
        public static bool DestroySQSQueue (string QueueName, RegionEndpoint RegionEndpoint, out string ErrorMessage, string AWSAccessKey = "", string AWSSecretKey = "")
        {
            bool result = false;
            ErrorMessage = "";
            IAmazonSQS queueClient;


            if (!String.IsNullOrWhiteSpace (QueueName))
            {
                if (!String.IsNullOrEmpty (AWSAccessKey))
                {
                    queueClient = AWSClientFactory.CreateAmazonSQSClient (AWSAccessKey, AWSSecretKey, RegionEndpoint);
                }
                else
                {
                    queueClient = AWSClientFactory.CreateAmazonSQSClient (RegionEndpoint);
                }
                try
                {
                    // Load the queue URL
                    string url = queueClient.GetQueueUrl (QueueName).QueueUrl;

                    // Destroy the queue
                    queueClient.DeleteQueue (url);
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }

            return result;
        }

        /// <summary>
        /// Base class constructor
        /// </summary>
        public AWSSQSHelper ()
        {
        }

        /// <summary>
        /// Class constructor that initializes and opens the queue based on input parameters
        /// </summary>
        /// <param name="queueName">The name of the queue to be opened when we create the class</param>
        /// <param name="maxNumberOfMessages">The maximum number of messages that will be received upon a GET request</param>
        /// <param name="regionEndpoint">Endpoint corresponding to the AWS region where the queue we want to open resides</param>
        public AWSSQSHelper (string queueName, int maxNumberOfMessages, RegionEndpoint regionEndpoint, String AWSAccessKey = "", String AWSSecretKey = "")
        {
            OpenQueue (queueName, maxNumberOfMessages, regionEndpoint, AWSAccessKey, AWSSecretKey);
        }

        /// <summary>
        /// The method clears the error information associated with the queue
        /// </summary>
        private void ClearErrorInfo ()
        {
            ErrorCode = 0;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// The method opens the queue
        /// </summary>
        public bool OpenQueue (string queuename, int maxnumberofmessages, RegionEndpoint regionendpoint, String AWSAccessKey = "", String AWSSecretKey = "")
        {
            ClearErrorInfo ();

            IsValid = false;

            if (!string.IsNullOrWhiteSpace (queuename))
            {
                if (!String.IsNullOrEmpty (AWSAccessKey))
                {
                    queue = AWSClientFactory.CreateAmazonSQSClient (AWSAccessKey, AWSSecretKey, regionendpoint);
                }
                else
                {
                    queue = AWSClientFactory.CreateAmazonSQSClient (regionendpoint);
                }
                try
                {
                    // Get queue url
                    GetQueueUrlRequest sqsRequest = new GetQueueUrlRequest ();
                    sqsRequest.QueueName = queuename;
                    queueurl = queue.GetQueueUrl (sqsRequest);

                    // Format receive messages request
                    rcvMessageRequest = new ReceiveMessageRequest ();
                    rcvMessageRequest.QueueUrl = queueurl.QueueUrl;
                    rcvMessageRequest.MaxNumberOfMessages = maxnumberofmessages;

                    // Format the delete messages request
                    delMessageRequest = new DeleteMessageRequest ();
                    delMessageRequest.QueueUrl = queueurl.QueueUrl;

                    IsValid = true;
                }
                catch (Exception ex)
                {
                    ErrorCode = e_Exception;
                    ErrorMessage = ex.Message;
                }
            }

            return IsValid;
        }

        /// <summary>
        /// Returns the approximate number of queued messages
        /// </summary>
        public int ApproximateNumberOfMessages ()
        {
            ClearErrorInfo ();

            int result = 0;
            try
            {
                GetQueueAttributesRequest attrreq = new GetQueueAttributesRequest ();
                attrreq.QueueUrl = queueurl.QueueUrl;
                attrreq.AttributeNames.Add ("ApproximateNumberOfMessages");
                GetQueueAttributesResponse attrresp = queue.GetQueueAttributes (attrreq);
                if (attrresp != null)
                    result = attrresp.ApproximateNumberOfMessages;
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// The method loads a one or more messages from the queue
        /// </summary>
        public bool DeQueueMessages ()
        {
            ClearErrorInfo ();

            bool result = false;
            try
            {
                rcvMessageResponse = queue.ReceiveMessage (rcvMessageRequest);
                result = true;
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Deletes a single message from the queue
        /// </summary>
        public bool DeleteMessage (Message message)
        {
            ClearErrorInfo ();

            bool result = false;
            try
            {
                delMessageRequest.ReceiptHandle = message.ReceiptHandle;
                queue.DeleteMessage (delMessageRequest);
                result = true;
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Delete multiple messages from the queue at once 
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public bool DeleteMessages (IList<Message> messages)
        {
            ClearErrorInfo ();

            try
            {
                var request = new DeleteMessageBatchRequest
                {
                    QueueUrl = queueurl.QueueUrl,
                    Entries = messages.Select (i => new DeleteMessageBatchRequestEntry (i.MessageId, i.ReceiptHandle)).ToList ()
                };
                var response = queue.DeleteMessageBatch (request);

                if (response.Failed != null && response.Failed.Count > 0)
                {
                    ErrorMessage = String.Format ("ErrorCount: {0}, Messages: [{1}]", response.Failed.Count,
                        String.Join (",", response.Failed.Select (i => i.Message).Distinct ()));

                    //var retryList = messages.Where (i => response.Failed.Any (j => j.Id == i.MessageId));
                    //foreach (var e in retryList)
                    //    DeleteMessage (e);
                }

                return String.IsNullOrEmpty (ErrorMessage);
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }

            return false;
        }

        /// <summary>
        /// Insert a message in the queue
        /// </summary>
        public bool EnqueueMessage (string msgbody)
        {
            ClearErrorInfo ();

            bool result = false;
            try
            {
                SendMessageRequest sendMessageRequest = new SendMessageRequest ();
                sendMessageRequest.QueueUrl = queueurl.QueueUrl;
                sendMessageRequest.MessageBody = msgbody;
                queue.SendMessage (sendMessageRequest);
                result = true;
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Insert a message in the queue and retry if an error is detected
        /// </summary>
        public bool EnqueueMessage (string msgbody, int maxretries)
        {
            // Insert domain info into queue
            bool result = false;
            int retrycount = maxretries;
            while (true)
            {
                // Try the insertion
                if (EnqueueMessage (msgbody))
                {
                    result = true;
                    break;
                }

                // Retry
                retrycount--;
                if (retrycount <= 0)
                    break;
                Thread.Sleep (new Random ().Next (500, 2000));
            }

            // Return
            return result;
        }

        /// <summary>
        /// Enqueues multiple messages into the opened queue at the same time
        /// </summary>
        public bool EnqueueMessages (IList<string> messages)
        {
            ClearErrorInfo ();

            bool result = false;
            try
            {
                var request = new SendMessageBatchRequest
                {
                    QueueUrl = queueurl.QueueUrl
                };
                List<SendMessageBatchRequestEntry> entries = new List<SendMessageBatchRequestEntry> ();

                // Messages counter
                int ix = 0;

                // Iterating until theres no message left
                while (ix < messages.Count)
                {
                    entries.Clear ();

                    // Storing upper limit of iteration
                    var len = Math.Min (ix + 10, messages.Count);

                    // Iterating over 10
                    for (var i = ix; i < len; i++)
                    {
                        entries.Add (new SendMessageBatchRequestEntry (i.ToString (), messages[i]));
                        ix++;
                    }

                    // Renewing entries from the object
                    request.Entries = entries;

                    // Batch Sending
                    var response = queue.SendMessageBatch (request);

                    // If any message failed to enqueue, use individual enqueue method
                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        // Hiccup
                        Thread.Sleep (100);

                        foreach (var failedMessage in response.Failed)
                        {
                            // Individual Enqueues
                            EnqueueMessage (failedMessage.Message);
                        }
                    }

                }

                result = true;
            }
            catch (Exception ex)
            {
                ErrorCode = e_Exception;
                ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Check if any messages were received by the last call of the DeQueueMessages method
        /// </summary>
        public bool AnyMessageReceived ()
        {
            try
            {
                if (rcvMessageResponse == null)
                    return false;

                var messageResults = rcvMessageResponse.Messages;

                if (messageResults != null && messageResults.FirstOrDefault () != null)
                {
                    return true;
                }
            }
            catch
            {
                // Nothing to do here                
            }

            return false;
        }

        /// <summary>
        /// Get an IEnumerable (that can be iterated over) collection of messages after a call to DeQueueMessages
        /// </summary>
        public IEnumerable<Message> GetDequeuedMessages ()
        {
            return rcvMessageResponse.Messages;
        }

        /// <summary>
        /// Initiate a "message receive" loop to fetch messages from the queue, returning messages as they are fetched in IEnumerable (yeld return) format
        /// </summary>
        public IEnumerable<Message> GetMessages (bool throwOnError = false)
        {
            do
            {
                // Dequeueing messages from the Queue
                if (!DeQueueMessages ())
                {
                    Thread.Sleep (250); // Hiccup                   
                    continue;
                }

                // Checking for no message received, and false positives situations
                if (!AnyMessageReceived ())
                {
                    break;
                }

                // Iterating over dequeued messages
                IEnumerable<Message> messages = null;
                try
                {
                    messages = GetDequeuedMessages ();
                }
                catch (Exception ex)
                {
                    ErrorCode = e_Exception;
                    ErrorMessage = ex.Message;
                    if (throwOnError)
                        throw ex;
                }

                if (messages == null) continue;

                foreach (Message awsMessage in messages)
                {
                    yield return awsMessage;
                }

            } while (true); // Loops Forever
        }

        /// <summary>
        /// Initiate a "message receive" loop to fetch messages from the queue, returning messages as they are fetched in IEnumerable (yeld return) format, 
        /// with an exponentially growing wait time whenever no messages are left on the queue 
        /// </summary>
        /// <param name="maxWaitTimeInMilliseconds">The maximum wait time for the exponentially increasing wait periods</param>
        /// <param name="waitCallback">A callback function to be called whenever there are no messages left in the queue and a wait period is about to be initiated</param>
        public IEnumerable<Message> GetMessagesWithWait (int maxWaitTimeInMilliseconds = 1800000, Func<int, int, bool> waitCallback = null, bool throwOnError = false)
        {
            int fallbackWaitTime = 1;

            // start dequeue loop
            do
            {
                // dequeue messages
                foreach (var message in GetMessages (throwOnError))
                {
                    // Reseting fallback time
                    fallbackWaitTime = 1;

                    // process message
                    yield return message;
                }

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

                if (waitTime > maxWaitTimeInMilliseconds)
                    waitTime = maxWaitTimeInMilliseconds;

                fallbackWaitTime++;

                // Sleeping before next try
                //Console.WriteLine ("Fallback (seconds) => " + waitTime);
                if (waitCallback != null)
                {
                    if (!waitCallback (fallbackWaitTime, waitTime))
                        break;
                }
                Thread.Sleep (waitTime);

            } while (true); // Loops Forever
        }

        /// <summary>
        /// This method repeatedly dequeues messages until there are no messages left
        /// </summary>
        public void ClearQueue ()
        {
            // TODO: We must alter the code to check how many messages are left in the queue. If there are too many messages, we should destroy the queue, wait one minute, and create it again.
            do
            {
                // Dequeueing Messages
                if (!DeQueueMessages ())
                {
                    // Checking for the need to abort (queue error)
                    if (!String.IsNullOrWhiteSpace (ErrorMessage))
                    {
                        return; // Abort
                    }

                    continue; // Continue in case de dequeue fails, to make sure no message will be kept in the queue
                }

                // Retrieving Message Results
                var resultMessages = rcvMessageResponse.Messages;

                // Checking for no message dequeued
                if (resultMessages.Count == 0)
                {
                    break; // Breaks loop
                }

                // Iterating over messages of the result to remove it
                foreach (Message message in resultMessages)
                {
                    // Deleting Message from Queue
                    DeleteMessage (message);
                }

            } while (true);
        }

        /// <summary>
        /// This method repeatedly dequeues messages from several queues until there are no messages left
        /// </summary>
        /// <param name="queueNames">The names of the queues we want to clear.</param>
        /// <param name="regionendpoint">The region endpoint for the AWS region we're using</param>
        public void ClearQueues (List<String> queueNames, RegionEndpoint regionendpoint)
        {
            // TODO: We must alter the code to check how many messages are left in the queue. If there are too many messages, we should destroy the queue, wait one minute, and create it again.

            // Iterating over queues
            foreach (string queueName in queueNames)
            {
                OpenQueue (queueName, 10, regionendpoint);

                do
                {
                    // Dequeueing Messages
                    if (!DeQueueMessages ())
                    {
                        continue; // Continue in case de dequeue fails, to make sure no message will be kept in the queue
                    }

                    // Retrieving Message Results
                    var resultMessages = rcvMessageResponse.Messages;

                    // Checking for no message dequeued
                    if (resultMessages.Count == 0)
                    {
                        break;
                    }

                    // Iterating over messages of the result to remove it
                    foreach (Message message in resultMessages)
                    {
                        // Deleting Message from Queue
                        DeleteMessage (message);
                    }

                } while (true);
            }
        }

        /// <summary>
        /// This method calls the new "Purge" function in the API to clear a queue
        /// </summary>
        public void PurgeQueue ()
        {
            queue.PurgeQueue (new PurgeQueueRequest
            {
                QueueUrl = queueurl.QueueUrl
            });
        }
    }
}

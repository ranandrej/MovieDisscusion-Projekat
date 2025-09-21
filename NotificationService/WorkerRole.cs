using Common;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NotificationService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private CloudQueue _queue;
        private CloudTable _subsTable;
        private CloudTable _logTable;
        private IEmailSender _email;
        private HttpListener _listener;
        public override void Run()
        {
            Trace.TraceInformation("NotificationService is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Use TLS 1.2 for Service Bus connections
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at https://go.microsoft.com/fwlink/?LinkId=166357.

            var storageAccount = CloudStorageAccount.Parse(
                RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"));

            // Queue

            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference("notifications");
            _queue.CreateIfNotExists();

            // Tables
            var tableClient = storageAccount.CreateCloudTableClient();
            _subsTable = tableClient.GetTableReference("FollowTable");
            _subsTable.CreateIfNotExists();
            _logTable = tableClient.GetTableReference("NotificationLog");
            _logTable.CreateIfNotExists();

            // Email sender (SMTP verzija)
            _email = new SMTPEmailSender(
                RoleEnvironment.GetConfigurationSettingValue("SmtpHost"),
                int.Parse(RoleEnvironment.GetConfigurationSettingValue("SmtpPort")),
                RoleEnvironment.GetConfigurationSettingValue("SmtpUser"),
                RoleEnvironment.GetConfigurationSettingValue("SmtpPass"),
                RoleEnvironment.GetConfigurationSettingValue("FromEmail"));

            var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["HealthEndpoint"].IPEndpoint;
            string url = $"http://{endpoint}/health-monitoring/";

            _listener = new HttpListener();
            _listener.Prefixes.Add(url); // endpoint mora da se poklapa sa csdef
            _listener.Start();

            Task.Run(() => ListenForHealthCheck());


            bool result = base.OnStart();

            Trace.TraceInformation("NotificationService has been started");

            return result;
        }

        private void ListenForHealthCheck()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext(); // blokira dok ne dodje zahtev
                    var responseMessage = "OK";

                    var buffer = Encoding.UTF8.GetBytes(responseMessage);
                    ctx.Response.ContentLength64 = buffer.Length;
                    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch (HttpListenerException) // thrown kad se listener zaustavi
                {
                    break;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Health endpoint error: " + ex.Message);
                }
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("NotificationService is stopping");

            _listener?.Stop();
            _listener?.Close();

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("NotificationService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await _queue.GetMessageAsync();
                if(msg != null)
                {
                    try
                    {
                        // 1. Deserialize message

                        var payload = JsonConvert.DeserializeObject<NotifyMessage>(msg.AsString);

                        // 2. Fetch all subscribers

                        var query = new TableQuery<FollowEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, payload.DiscussionId));

                        var followers = await _subsTable.ExecuteQuerySegmentedAsync(query, null);

                        // 2b. Izvuci sve email adrese
                        var recipientList = followers.Results
                            .Select(f => f.UserEmail)
                            .Where(e => !string.IsNullOrWhiteSpace(e))
                            .Distinct()
                            .ToList();


                        var commentsTable = _subsTable.ServiceClient.GetTableReference("Comments"); // koristi isti tableClient
                        var retrieve = TableOperation.Retrieve<CommentEntity>(payload.DiscussionId, payload.CommentId);
                        var resultComment = await commentsTable.ExecuteAsync(retrieve);
                        var commentEntity = resultComment.Result as CommentEntity;

                        string userNameOrEmail = commentEntity?.CreatorEmail ?? commentEntity?.AuthorEmail ?? "Nepoznat";
                        string commentText = commentEntity?.Text ?? $"Novi komentar (ID: {payload.CommentId})";
                        string body = $"{userNameOrEmail}: {commentText}";

                        // int sentCount = 0;

                        // 3. Send emails

                        await _email.SendAsync(recipientList, "Novi komentar na diskusiji", body);

                        // 4. Log notification
                        var sentCount = recipientList.Count;
                        var log = new NotificationLogEntity(payload.CommentId, DateTime.UtcNow, sentCount);
                        var insert = TableOperation.InsertOrReplace(log);
                        await _logTable.ExecuteAsync(insert);

                        // 5. Remove message from queue
                        await _queue.DeleteMessageAsync(msg);

                        Trace.TraceInformation($"Processed comment {payload.CommentId}, sent {sentCount} emails");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Greška prilikom obrade: " + ex.Message);
                    }
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        // DTO za poruku u redu
        public class NotifyMessage
        {
            public string CommentId { get; set; }
            public string DiscussionId { get; set; }
        }
    }
}

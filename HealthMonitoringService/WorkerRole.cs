using Common;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NotificationService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using static Common.AlterEmailEntity;

namespace HealthMonitoringService
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudTable healthCheckTable;
        private CloudTable alertEmailsTable;
        private IEmailSender _email;
        // Update URLs to actual deployed endpoints or local if testing
        private readonly string movieDiscussionUrl = "http://localhost:8081/health-monitoring";
        private readonly string notificationUrl = "http://localhost:8082/health-monitoring";


        public override bool OnStart()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 12;
            InitializeAzureTables();

            _email = new SMTPEmailSender(
                RoleEnvironment.GetConfigurationSettingValue("SmtpHost"),
                int.Parse(RoleEnvironment.GetConfigurationSettingValue("SmtpPort")),
                RoleEnvironment.GetConfigurationSettingValue("SmtpUser"),
                RoleEnvironment.GetConfigurationSettingValue("SmtpPass"),
                RoleEnvironment.GetConfigurationSettingValue("FromEmail"));

            bool result = base.OnStart();
            Trace.TraceInformation("HealthMonitoringService has been started");
            return result;
        }

        private void InitializeAzureTables()
        {
            string connectionString = RoleEnvironment.GetConfigurationSettingValue("DataConnectionString");
            //string connectionString = "DataConnectionString"; // Adjust for real storage
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            healthCheckTable = tableClient.GetTableReference("HealthCheck");
            alertEmailsTable = tableClient.GetTableReference("AlertEmails");
            // healthCheckTable.CreateIfNotExistsAsync().Wait();
            // alertEmailsTable.CreateIfNotExistsAsync().Wait();
        }

        public override void Run()
        {
            Trace.TraceInformation("HealthMonitoringService is running");
            try
            {
                RunAsync(cancellationTokenSource.Token).Wait();
            }
            finally
            {
                runCompleteEvent.Set();
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("HealthMonitoringService is stopping");
            cancellationTokenSource.Cancel();
            runCompleteEvent.WaitOne();
            base.OnStop();
            Trace.TraceInformation("HealthMonitoringService has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var services = new Dictionary<string, string>
            {
                { "MovieDiscussionService", movieDiscussionUrl },
                { "NotificationService", notificationUrl }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var service in services)
                {
                    bool isOk = await CheckService(service.Value);
                    await LogHealthStatus(service.Key, isOk);

                    if (!isOk)
                        await SendAlertEmails(service.Key);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }

        private async Task<bool> CheckService(string url)
        {
            try
            {
                using (var client = new WebClient())
                {
                    await client.DownloadStringTaskAsync(new Uri(url));
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task LogHealthStatus(string serviceName, bool isOk)
        {
            // Kreiramo entitet koristeći HealthCheckEntity klasu
            var entity = new HealthCheckEntity(serviceName, DateTime.UtcNow)
            {
                Status = isOk ? "OK" : "NOT_OK"
            };

            var insertOperation = TableOperation.Insert(entity);
            await healthCheckTable.ExecuteAsync(insertOperation);

            Trace.TraceInformation($"{serviceName}: {(isOk ? "OK" : "NOT_OK")}");
        }


        private async Task SendAlertEmails(string serviceName)
        {
            var query = new TableQuery<AlertEmailEntity>()
       .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Alert"));

            var results = await alertEmailsTable.ExecuteQuerySegmentedAsync(query, null);

            // Može više emailova – recimo da je RowKey = email adresa
            var recipients = results.Results
                .Select(e => e.RowKey)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            if (recipients.Any())
            {
                try
                {
                    await _email.SendAsync(
                        recipients, // lista primaoca
                        $"[ALERT] Service {serviceName} is down",
                        $"Service {serviceName} failed health-check at {DateTime.UtcNow}."
                    );

                    Trace.TraceInformation($"Alert sent to {recipients.Count} recipients");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Greška prilikom slanja alert email-a: " + ex.Message);
                }
            }
        }
    }
}

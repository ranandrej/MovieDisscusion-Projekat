using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Common;
using HealthStatusService.Models;

namespace HealthStatusService.Controllers
{
    public class HomeController : Controller
    {
        private readonly string connectionString = "UseDevelopmentStorage=true";

        public async Task<ActionResult> Index()
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var healthCheckTable = tableClient.GetTableReference("HealthCheck");

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);

            // Query za sve entitete iz poslednja 2 sata
            var query = new TableQuery<HealthCheckEntity>();
            TableContinuationToken token = null;
            var allRecords = new List<HealthCheckEntity>();

            do
            {
                var segment = await healthCheckTable.ExecuteQuerySegmentedAsync(query, token);
                allRecords.AddRange(segment.Results);
                token = segment.ContinuationToken;
            } while (token != null);

            var recentRecords = allRecords
                .Where(r => r.TimestampUtc >= twoHoursAgo)
                .ToList();

            // Grupisanje po servisu i računanje uptime
            var grouped = recentRecords
                .GroupBy(r => r.ServiceName)
                .Select(g => new HealthStatusViewModel
                {
                    ServiceName = g.Key,
                    Records = g.OrderBy(r => r.TimestampUtc).ToList(),
                    UptimePercentage = g.Count(r => r.Status == "OK") * 100.0 / g.Count()
                })
                .ToList();

            return View(grouped);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
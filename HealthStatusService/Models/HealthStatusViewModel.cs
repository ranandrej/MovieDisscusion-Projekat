using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;

namespace HealthStatusService.Models
{
    public class HealthStatusViewModel
    {
        // Ime servisa, npr. MovieDiscussionService
        public string ServiceName { get; set; }

        // Lista HealthCheckEntity za taj servis
        public List<HealthCheckEntity> Records { get; set; }

        // Procentualna dostupnost (0-100)
        public double UptimePercentage { get; set; }
    }
}
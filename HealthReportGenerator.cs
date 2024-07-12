using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Testing.HealthReport
{
    internal class HealthReportGenerator
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly List<HealthDataItem> _healthData;

        public HealthReportGenerator(IDateTimeProvider dateTimeProvider, List<HealthDataItem> healthData)
        {
            _dateTimeProvider = dateTimeProvider;
            _healthData = healthData;
        }

        public void GenerateReport(int days)
        {
            var now = _dateTimeProvider.OffsetNow;
            var startDate = now.AddDays(-days).Date;

            for (var date = startDate; date < now.Date; date = date.AddDays(1))
            {
                var dailyData = _healthData.Where(h => h.Date.Date == date).ToList();

                if (!dailyData.Any())
                {
                    Console.WriteLine($"Unavailable {date:yyyy-MM-dd}");
                    continue;
                }

                var groupedData = dailyData.GroupBy(x => x.Service);
                foreach (var serviceData in groupedData)
                {
                    var totalCount = serviceData.Count();
                    var healthyCount = serviceData.Count(h => h.Status == HealthStatus.Healthy);
                    var unhealthyCount = serviceData.Count(h => h.Status == HealthStatus.Unhealthy);
                    var degradedCount = serviceData.Count(h => h.Status == HealthStatus.Degraded);

                    var uptimePercent = (double)healthyCount / totalCount * 100;
                    var unhealthyPercent = (double)unhealthyCount / totalCount * 100;
                    var degradedPercent = (double)degradedCount / totalCount * 100;

                    Console.WriteLine($"{serviceData.Key}, {date:yyyy-MM-dd}, Uptime: {healthyCount}, UptimePercent: {uptimePercent:f2}%, UnhealthyPercent: {unhealthyPercent:f2}%, DegradedPercent: {degradedPercent:f2}%");
                }
            }
        }
    }
}

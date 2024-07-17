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

            var groupedData = _healthData.GroupBy(x => x.Service);
            foreach (var serviceData in groupedData)
            {
                Console.WriteLine($"Report for past {days} days for {serviceData.Key}");
                Console.WriteLine("Format: {ServiceName} {Date} {Uptime} {UptimePercent} {UnhealthyPercent} {DegradedPercent}");

                bool startFromFirstLog = startDate < serviceData.First().Date;
                HealthStatus lastStatus = startFromFirstLog ? serviceData.First().Status : serviceData.Last(x => x.Date < startDate).Status;
                DateTimeOffset lastStatusDate = startFromFirstLog ? serviceData.First().Date : serviceData.Last(x => x.Date < startDate).Date;

                for (var date = startDate; date <= now.Date; date = date.AddDays(1))
                {
                    var dailyData = serviceData.Where(d => d.Date.Date == date.Date).ToList();

                    var statusTimeSpans = new Dictionary<HealthStatus, TimeSpan>
                    {
                        { HealthStatus.Healthy, TimeSpan.Zero },
                        { HealthStatus.Unhealthy, TimeSpan.Zero },
                        { HealthStatus.Degraded, TimeSpan.Zero }
                    };

                    if (!dailyData.Any())
                    {
                        if (lastStatusDate.Date <= date.Date)
                        {
                            statusTimeSpans[lastStatus] += TimeSpan.FromDays(1);
                            lastStatusDate = date.AddDays(1);
                        }
                        else
                        {
                            Console.WriteLine($"{serviceData.Key} {date:M/d/yyyy} Unavailable");
                            continue;
                        }
                    }
                    else
                    {
                        if (lastStatusDate != dailyData.First().Date)
                        {
                            dailyData.Insert(0, new HealthDataItem(serviceData.Key, date.Date, lastStatus));
                        }
                        dailyData.Add(new HealthDataItem(serviceData.Key, date.Date.AddDays(1), dailyData.Last().Status));

                        for (int i = 0; i < dailyData.Count - 1; i++)
                        {
                            var current = dailyData[i];
                            var next = dailyData[i + 1];
                            var duration = next.Date - current.Date;
                            statusTimeSpans[current.Status] += duration;
                        }

                        lastStatus = dailyData.Last().Status;
                        lastStatusDate = dailyData.Last().Date;
                    }

                    var dayTimeSpan = TimeSpan.FromDays(1);
                    var totalUptime = statusTimeSpans[HealthStatus.Healthy];
                    var uptimePercent = (statusTimeSpans[HealthStatus.Healthy].TotalMinutes / dayTimeSpan.TotalMinutes) * 100;
                    var unhealthyPercent = (statusTimeSpans[HealthStatus.Unhealthy].TotalMinutes / dayTimeSpan.TotalMinutes) * 100;
                    var degradedPercent = (statusTimeSpans[HealthStatus.Degraded].TotalMinutes / dayTimeSpan.TotalMinutes) * 100;

                    Console.WriteLine($"{serviceData.Key} {date:M/d/yyyy} {(int)totalUptime.TotalHours:D2}:{totalUptime.Minutes:D2}:{totalUptime.Seconds:D2} {uptimePercent:f2}% {unhealthyPercent:f2}% {degradedPercent:f2}%");
                }
            }
        }
    }
}

using Quartz;
using Sitecore.Buckets.Managers;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Sitecron.SitecronSettings;
using System;
using System.Linq;

namespace Sitecron.Jobs
{
    public class CleanUpReports : IJob
    {
        private const int defaultMaxDaysOld = 200;
        private const string defaultDb = "master";
        private const string defaultIndex = "sitecore_master_index";

        public void Execute(IJobExecutionContext context)
        {
            var reportRootIds = context.JobDetail.JobDataMap["Items"].ToString().Split('|');
            var jobParameters = WebUtil.ParseQueryString(context.JobDetail.JobDataMap["Parameters"].ToString());
            var maxReportDays = jobParameters.TryGetValue("maxDaysOld", out string maxDaysOld)
                ? Math.Abs(int.Parse(maxDaysOld))
                : defaultMaxDaysOld;
            var dbName = jobParameters.TryGetValue("db", out string db) ? db : defaultDb;
            var indexName = jobParameters.TryGetValue("index", out string index) ? index : defaultIndex;

            int count = 0;
            foreach (var reportRootId in reportRootIds)
            {
                count += RemoveOldSiteCronReports(reportRootId, maxReportDays, dbName, indexName);
            }

            LogInfo(context, $"{nameof(CleanUpReports)} finished, deleted {count} report{(count != 1 ? "s" : "")}");
        }

        private int RemoveOldSiteCronReports(string reportRootId, int maxReportDays, string dbName, string indexName)
        {
            var maxAge = DateTime.Today.AddDays(-maxReportDays);
            var reportRootItem = Database.GetDatabase(dbName).GetItem(new ID(reportRootId));
            var searchResultItemsToDelete = GetAllItemsFromBucket(reportRootItem.Paths.ContentPath, maxAge, indexName);
            var itemCount = searchResultItemsToDelete.Count();

            if (itemCount == 0)
            {
                return 0;
            }

            using (new SecurityDisabler())
            using (new DatabaseCacheDisabler())
            using (new EventDisabler())
            using (new BulkUpdateContext())
            {
                try
                {
                    IndexCustodian.PauseIndexing();

                    foreach (var searchResultsItem in searchResultItemsToDelete)
                    {
                        searchResultsItem?.GetItem()?.Delete();
                    }
                }
                finally
                {
                    IndexCustodian.ResumeIndexing();
                }
            }

            BucketManager.Sync(reportRootItem);

            return itemCount;
        }

        private IQueryable<SearchResultItem> GetAllItemsFromBucket(string bucketPath, DateTime maxAge, string indexName)
        {
            var index = ContentSearchManager.GetIndex(indexName);

            using (var context = index.CreateSearchContext())
            {
                return context.GetQueryable<SearchResultItem>()
                 .Where(x => x.Path.StartsWith(bucketPath)
                     && x.TemplateId == SitecronConstants.Templates.SiteCronExecutionReportTemplateID
                     && x.CreatedDate < maxAge);
            }
        }

        private void LogInfo(IJobExecutionContext context, string message)
        {
            Log.Info(message, this);
            context.JobDetail.JobDataMap.Put(SitecronConstants.ParamNames.SitecronJobLogData, message);
        }
    }
}
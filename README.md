# SiteCron

SiteCron module provides an advanced way to run Cron based scheduling jobs using Sitecore and Quartz Scheduler. It uses the CronTrigger functionality to help you schedule simple to complex jobs.

Add your scheduled jobs at /sitecore/system/Modules/Sitecron

You can get more information about the Cron triggers and examples from Quartz Scheduler website at: 
http://www.quartz-scheduler.net/documentation/quartz-2.x/tutorial/crontriggers.html

Sitecron comes with a sample script called SampleLogJob which logs an info log entry based on the schedule you set. Make sure any implementations of schedule jobs inherit Quartz.IJob.

Thank you for using Sitecron.

Instructions are also available on the blog along with the video: http://www.akshaysura.com

## This fork

This fork tackles a few issues:

1. For more than a few people, the upgrade to Quartz.NET 3.x meant that SiteCron jobs simply wouldn't fire.  
   In this fork Quartz.NET is downgraded back to 2.x
2. Template fields for Sitecron Job items are marked _Shared_ so multilingual sites don't run into issues.
3. The Cleanup job is implemented in the DLL to not be dependent on Sitecore PowerShell Extensions.
4. `EditContext()` is removed in favor of `BeginEdit()`/`EndEdit()` as it's [considered harmful](https://kamsar.net/index.php/2017/01/EditContext-Considered-Harmful/).
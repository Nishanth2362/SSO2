using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests;
using System;
using System.Threading.Tasks;

namespace SSO.WebApplication.Filters
{
    public class JobFailureFilter : JobFilterAttribute, IElectStateFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public JobFailureFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState failedState)
            {
                var mailService = _serviceProvider.GetService<IMailService>();
                var config = _serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (mailService != null)
                {
                    var jobName = context.BackgroundJob.Job.Method.Name;
                    var exception = failedState.Exception;
                    var appName = config?["DefaultSetting:AppName"] ?? config?["DefaultSetting:TenantName"] ?? "SSO Server";
                    var recipientEmail = config?["DefaultSetting:Email"] ?? "support@auxinz.io";

                    var subjectTemplate = config?["DefaultSetting:JobFailureSubject"] ?? $"[{appName} Alert] Background Operation Failure: {jobName}";
                    var bodyTemplate = config?["DefaultSetting:JobFailureBody"] ?? 
                                       $"<h3>Hangfire Job Execution Failed</h3>" +
                                       $"<p><b>Job Identifier:</b> {context.BackgroundJob.Id}</p>" +
                                       $"<p><b>Job Method:</b> {jobName}</p>" +
                                       $"<p><b>Error Message:</b> {exception?.Message}</p>" +
                                       $"<pre>{exception?.StackTrace}</pre>";

                    if (config?["DefaultSetting:JobFailureSubject"] != null)
                    {
                        subjectTemplate = subjectTemplate
                            .Replace("{appName}", appName)
                            .Replace("{jobName}", jobName);
                    }
                    if (config?["DefaultSetting:JobFailureBody"] != null)
                    {
                        bodyTemplate = bodyTemplate
                            .Replace("{jobId}", context.BackgroundJob.Id)
                            .Replace("{jobName}", jobName)
                            .Replace("{exceptionMessage}", exception?.Message ?? "")
                            .Replace("{stackTrace}", exception?.StackTrace ?? "");
                    }

                    // Async dispatch to avoid blocking Hangfire pipeline
                    Task.Run(() => mailService.SendAsync(new MailRequest
                    {
                        To = new() { recipientEmail },
                        Subject = subjectTemplate,
                        Body = bodyTemplate
                    }));
                }
            }
        }
    }
}

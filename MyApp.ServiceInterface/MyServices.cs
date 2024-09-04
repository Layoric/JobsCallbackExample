using MyApp.ServiceInterface.Commands;
using ServiceStack;
using MyApp.ServiceModel;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface;

public class MyServices(IBackgroundJobs jobs) : Service
{
    public object Any(Hello request)
    {
        return new HelloResponse { Result = $"Hello, {request.Name}!" };
    }
    
    public object Any(QueueCheckUrls request)
    {
        var jobRef = jobs.EnqueueCommand<CheckUrlsCommand>(new CheckUrls
            {
                Urls = request.Urls.Split("\n").ToList()
            },new()
        {
            Worker = nameof(CheckUrlsCommand),
            Callback = nameof(CheckUrlsReportCommand)
        });

        return new QueueCheckUrlsResponse
        {
            JobRef = jobRef
        };
    }
}

public class QueueCheckUrlsResponse
{
    public BackgroundJobRef JobRef { get; set; }
}

public class QueueCheckUrls : IReturn<QueueCheckUrlsResponse>
{
    [Input(Type = "textarea")] public string Urls { get; set; }
}
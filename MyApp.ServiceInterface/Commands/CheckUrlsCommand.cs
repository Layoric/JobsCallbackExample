using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Jobs;

namespace MyApp.ServiceInterface.Commands;

// Request DTO
public class CheckUrls
{
    public List<string> Urls { get; set; }
}

// Result DTO
public class CheckUrlsResult
{
    public Dictionary<string, bool> UrlStatuses { get; set; }
}

// Command implementation
public class CheckUrlsCommand(
    IHttpClientFactory httpClientFactory,
    IBackgroundJobs jobs,
    ILogger<CheckUrlsCommand> logger) : AsyncCommandWithResult<CheckUrls, CheckUrlsResult>
{
    protected override async Task<CheckUrlsResult> RunAsync(CheckUrls request, CancellationToken token)
    {
        var result = new CheckUrlsResult
        {
            UrlStatuses = new Dictionary<string, bool>()
        };
        
        var job = Request.GetBackgroundJob();
        
        // Create a JobLogger to log messages to the background job
        var log = Request.CreateJobLogger(jobs, logger);

        log.LogInformation("Checking {Count} URLs", request.Urls.Count);
        using var client = httpClientFactory.CreateClient();
        // Set a timeout of 3 seconds for each request
        client.Timeout = TimeSpan.FromSeconds(3);

        foreach (var url in request.Urls.Where(x => !string.IsNullOrEmpty(x.Trim())))
        {
            try
            {
                var testUrl = url.Trim();
                log.LogInformation("Checking {Url}", testUrl);
                // Send a HEAD request to check if the URL is up
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url), token);
                result.UrlStatuses[url] = response.IsSuccessStatusCode;
                log.LogInformation("{Url} is {Status}", testUrl, response.IsSuccessStatusCode ? "up" : "down");
                await Task.Delay(1000, token);
            }
            catch (Exception e)
            {
                // If an exception occurs (e.g., network error), consider the URL as down
                result.UrlStatuses[url] = false;
            }
        }
        
        log.LogInformation("Finished checking URLs, {Up} up, {Down} down", 
            result.UrlStatuses.Values.Count(x => x), result.UrlStatuses.Values.Count(x => !x));

        return result;
    }
}

public class CheckUrlsReportCommand(
    ILogger<CheckUrlsReportCommand> logger,
    IBackgroundJobs jobs) : AsyncCommand<CheckUrlsResult>
{

    protected override Task RunAsync(CheckUrlsResult request, CancellationToken token)
    {
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Reporting on {Count} URLs", request.UrlStatuses.Count);
        foreach (var (url, status) in request.UrlStatuses)
        {
            log.LogInformation("{Url} is {Status}", url, status ? "up" : "down");
        }
        return Task.CompletedTask;
    }
}
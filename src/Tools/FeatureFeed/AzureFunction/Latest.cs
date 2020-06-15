using System;
using System.IO;
using System.Linq;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace AzureFunction
{
    public static class Latest
    {
        static readonly Uri BaseUri = new BlobServiceClient(Environment.GetEnvironmentVariable("FeedStorageConnectionString")).Uri;

        [FunctionName("latest")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "latest")] HttpRequest req)
        {
            if (req.Query.ContainsKey("feature"))
                return new RedirectResult(new Uri(BaseUri, $"latest/{req.Query["feature"]}/RoslynDeployment.vsix").ToString(), false);

            // Shorthand notation: /latest?FEATURE
            if (req.Query.Keys.Count == 1)
                return new RedirectResult(new Uri(BaseUri, $"latest/{req.Query.Keys.First()}/RoslynDeployment.vsix").ToString(), false);

            if (Uri.TryCreate(req.Headers["Referer"], UriKind.Absolute, out var referer))
            {
                // Incoming URL will be like https://github.com/kzu/roslyn/tree/features/records/src/Tools/FeatureFeed
                var paths = referer.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/').Skip(2).ToArray();
                // No paths means this is a link from the README.md in the default branch
                if (paths.Length == 0)
                    return new RedirectResult(new Uri(BaseUri, "latest/master/RoslynDeployment.vsix").ToString(), false);

                if (paths[0] == "tree" && paths.Length >= 3 && paths[1] == "features")
                    return new RedirectResult(new Uri(BaseUri, $"latest/{paths[2]}/RoslynDeployment.vsix").ToString(), false);
            }

            return new NotFoundResult();
        }
    }
}

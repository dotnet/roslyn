// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GithubMergeTool
{
    public class GithubMergeTool
    {
        private static readonly Uri GithubBaseUri = new Uri("https://api.github.com/");

        private readonly HttpClient _client;

        public GithubMergeTool(
            string username,
            string password)
        {
            var client = new HttpClient();
            client.BaseAddress = GithubBaseUri;

            var authArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(authArray));
            client.DefaultRequestHeaders.Add(
                "user-agent",
                "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2;)");

            _client = client;
        }

        /// <summary>
        /// Create a merge PR.
        /// </summary>
        /// <returns>
        /// null if the merge PR was completed without error. Otherwise,
        /// the response which had an error is returned. Note that a response
        /// with an <see cref="HttpStatusCode" /> value 422 is returned if a PR
        /// cannot be created because the <paramref name="srcBranch"/> creates
        /// all the commits in the <paramref name="destBranch"/>.
        /// </returns>
        public async Task<HttpResponseMessage> CreateMergePr(
            string repoOwner,
            string repoName,
            string srcBranch,
            string destBranch)
        {
            // Get the SHA for the source branch
            var response = await _client.GetAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{srcBranch}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return response;
            }

            var jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (jsonBody.Type == JTokenType.Array)
            {
                // Branch doesn't exist
                return response;
            }

            var srcSha = ((JValue)jsonBody["object"]["sha"]).ToObject<string>();

            // Generate a new branch name for the merge branch
            var prBranchName = $"merges/{srcBranch}-to-{destBranch}-{DateTime.UtcNow.ToString("yyyMMdd-HHmmss")}";

            // Create a branch on the repo
            var body = $@"
{{
    ""ref"": ""refs/heads/{prBranchName}"",
    ""sha"": ""{srcSha}""
}}";

            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/git/refs", body);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                return response;
            }

            const string newLine = @"
";

            var prMessage = $@"
This is an automatically generated pull request from {srcBranch} into {destBranch}.

``` bash
git fetch --all
git checkout {prBranchName}
git reset --hard upstream/{destBranch}
git merge upstream/{srcBranch}
# Fix merge conflicts
git commit
git push {prBranchName} --force
```

Once all conflicts are resolved and all the tests pass, you are free to merge the pull request.";

            prMessage = prMessage.Replace(newLine, "\\n");

            // Create a PR from the new branch to the dest
            body = $@"
{{
    ""title"": ""Merge {srcBranch} to {destBranch}"",
    ""body"": ""{prMessage}"",
    ""head"": ""{prBranchName}"",
    ""base"": ""{destBranch}""
}}";

            response = await _client.PostAsyncAsJson($"repos/{repoOwner}/{repoName}/pulls", body);

            jsonBody = JObject.Parse(await response.Content.ReadAsStringAsync());

            // 422 (Unprocessable Entity) indicates there were no commits to merge
            if (response.StatusCode == (HttpStatusCode)422)
            {
                // Delete the pr branch if the PR was not created.
                await _client.DeleteAsync($"repos/{repoOwner}/{repoName}/git/refs/heads/{prBranchName}");
                return response;
            }

            return null;
        }
    }
}

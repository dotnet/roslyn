// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GitMergeBot
{
    internal sealed class VisualStudioOnlineRepository : RepositoryBase
    {
        private const string ApiVersion = "3.0";

        private HttpClient _client;
        private string _project;
        private string _remoteName;
        private string _repositoryId;
        private string _userId;

        public VisualStudioOnlineRepository(string path, string repoName, string project, string userId, string userName, string password, string remoteName)
            : base(path, repoName, userName, password)
        {
            _project = project;
            _remoteName = remoteName;
            _userId = userId;

            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{userName}:{password}"));
            var remote = Repository.Network.Remotes[remoteName];
            var remoteUri = new Uri(remote.Url);
            _client = new HttpClient();
            _client.BaseAddress = new Uri($"{remoteUri.Scheme}://{remoteUri.Host}");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        public override async Task Initialize()
        {
            // find the repository ID
            // https://www.visualstudio.com/en-us/docs/integrate/api/git/repositories#get-a-list-of-repositories
            var repositories = await GetJsonAsync($"DefaultCollection/{_project}/_apis/git/repositories?api-version={ApiVersion}");
            _repositoryId = (string)repositories["value"].Single(r => r?["name"].Type == JTokenType.String && (string)r["name"] == RepositoryName)["id"];
        }

        public override async Task<bool> ShouldMakePullRequestAsync(string title)
        {
            // https://www.visualstudio.com/en-us/docs/integrate/api/git/pull-requests/pull-requests#get-a-list-of-pull-requests-in-the-repository
            var foundMatch = false;
            var result = await GetJsonAsync($"DefaultCollection/_apis/git/repositories/{_repositoryId}/pullRequests?api-version={ApiVersion}&creatorId={_userId}");
            var pullRequests = (JArray)result["value"];
            foreach (JObject pr in pullRequests)
            {
                if (pr?["repository"]?["name"].Type == JTokenType.String && (string)pr["repository"]["name"] == RepositoryName)
                {
                    var prTitle = (string)pr["title"];
                    Console.WriteLine($"  Open PR: {prTitle}");
                    foundMatch |= prTitle == title;
                }
            }

            return !foundMatch;
        }

        public override async Task CreatePullRequestAsync(string title, string destinationOwner, string pullRequestBranch, string prBranchSourceRemote, string sourceBranch, string destinationBranch)
        {
            // https://www.visualstudio.com/en-us/docs/integrate/api/git/pull-requests/pull-requests#create-a-pull-request
            var prMessage = $@"
This is an automatically generated pull request from {sourceBranch} into {destinationBranch}.

``` bash
git remote add {_remoteName} {Repository.Network.Remotes[_remoteName].Url}
git fetch --all
git checkout {pullRequestBranch}
git reset --hard {_remoteName}/{destinationBranch}
git merge {prBranchSourceRemote}/{sourceBranch}
# Fix merge conflicts
git commit
git push {pullRequestBranch} --force
```

Once all conflicts are resolved and all the tests pass, you are free to merge the pull request.
".Trim();
            var request = new JObject()
            {
                ["sourceRefName"] = $"refs/heads/{pullRequestBranch}",
                ["targetRefName"] = $"refs/heads/{destinationBranch}",
                ["title"] = title,
                ["description"] = prMessage,
                ["reviewers"] = new JArray() // no required reviewers, but necessary for the request
            };
            var result = await GetJsonAsync(
                $"DefaultCollection/_apis/git/repositories/{_repositoryId}/pullRequests?api-version={ApiVersion}",
                body: request,
                method: "POST");

            var pullRequestId = (string)result["pullRequestId"];

            // close the PR if there are no commits
            // https://www.visualstudio.com/en-us/docs/integrate/api/git/pull-requests/pull-requests#get-commits-for-the-pull-request
            result = await GetJsonAsync($"DefaultCollection/_apis/git/repositories/{_repositoryId}/pullRequests/{pullRequestId}/commits?api-version={ApiVersion}");
            var commits = result["value"] as JArray;
            if (commits?.Count == 0)
            {
                // https://www.visualstudio.com/en-us/docs/integrate/api/git/pull-requests/pull-requests#status
                var completeRequest = new JObject()
                {
                    ["status"] = "abandoned"
                };

                result = await GetJsonAsync(
                    $"DefaultCollection/_apis/git/repositories/{_repositoryId}/pullRequests/{pullRequestId}?api-version={ApiVersion}",
                    body: completeRequest,
                    method: "PATCH");

                return;
            }

            // mark the PR to auto complete
            // https://www.visualstudio.com/en-us/docs/integrate/api/git/pull-requests/pull-requests#auto-complete
            var autoCompleteRequest = new JObject()
            {
                ["autoCompleteSetBy"] = new JObject()
                {
                    ["id"] = _userId
                },
                ["completionOptions"] = new JObject()
                {
                    ["deleteSourceBranch"] = true,
                    ["mergeCommitMessage"] = $"Pull request #{pullRequestId} auto-completed after passing checks.",
                    ["squashMerge"] = false
                }
            };

            result = await GetJsonAsync(
                $"DefaultCollection/_apis/git/repositories/{_repositoryId}/pullRequests/{pullRequestId}?api-version={ApiVersion}",
                body: autoCompleteRequest,
                method: "PATCH");
        }

        private async Task<JObject> GetJsonAsync(string requestUri, JObject body = null, string method = "GET")
        {
            HttpResponseMessage response;
            if (body == null)
            {
                response = await _client.GetAsync(requestUri);
            }
            else
            {
                var requestMessage = new HttpRequestMessage(new HttpMethod(method), requestUri);
                requestMessage.Content = new ByteArrayContent(Encoding.ASCII.GetBytes(body.ToString()));
                requestMessage.Content.Headers.Add("Content-Type", "application/json");
                response = await _client.SendAsync(requestMessage);
            }

            var result = await response.Content.ReadAsStringAsync();
            return JObject.Parse(result);
        }
    }
}

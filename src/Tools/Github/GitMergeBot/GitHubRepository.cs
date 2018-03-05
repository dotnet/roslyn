// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitMergeBot
{
    internal sealed class GitHubRepository : RepositoryBase
    {
        private GitHubClient _client;

        public GitHubRepository(string path, string repoName, string userName, string authToken)
            : base(path, repoName, userName, authToken)
        {
            _client = new GitHubClient(new ProductHeaderValue(userName))
            {
                Credentials = new Credentials(authToken)
            };
        }

        public override async Task<bool> ShouldMakePullRequestAsync(string title)
        {
            return (await GetExistingMergePrsAsync(title)).Count == 0;
        }

        public override async Task CreatePullRequestAsync(string title, string destinationOwner, string pullRequestBranch, string prBranchSourceRemote, string sourceBranch, string destinationBranch)
        {
            var remoteName = $"{UserName}-{RepositoryName}";
            var prMessage = $@"
This is an automatically generated pull request from {sourceBranch} into {destinationBranch}.

@dotnet/roslyn-infrastructure:

``` bash
git remote add {remoteName} ""https://github.com/{UserName}/{RepositoryName}.git""
git fetch {remoteName}
git fetch {prBranchSourceRemote}
git checkout {pullRequestBranch}
git reset --hard {prBranchSourceRemote}/{destinationBranch}
git merge {prBranchSourceRemote}/{sourceBranch}
# Fix merge conflicts
git commit
git push {remoteName} {pullRequestBranch} --force
```

Once all conflicts are resolved and all the tests pass, you are free to merge the pull request.
".Trim();

            try
            {
                var pullRequest = await _client.PullRequest.Create(
                    owner: destinationOwner,
                    name: RepositoryName,
                    newPullRequest: new NewPullRequest(
                        title: title,
                        head: $"{UserName}:{pullRequestBranch}",
                        baseRef: destinationBranch)
                    {
                        Body = prMessage
                    }
                    );

                // The reason for this delay is twofold:
                //
                // * Github has a bug in which it can "create" a pull request without that pull request
                //   being able to be commented on for a short period of time.
                // * The Jenkins "comment watcher" has a bug whereby any comment posted shortly after
                //   pull-request creation is ignored.
                //
                // Thus, this delay sidesteps both of those bugs by asking for a VSI test 30 seconds after 
                // the creation of the PR.  Ugly, yes; but the only *real* way to sidestep this would be to 
                // 1) Fix github, 2) Fix jenkins, and while those might be lofty goals, they are not in the 
                // scope of this PR.
                await Task.Delay(TimeSpan.FromSeconds(30.0));

                await _client.Issue.Comment.Create(destinationOwner, RepositoryName, pullRequest.Number, "@dotnet-bot test vsi please");
            }
            catch (Exception ex) when (DidPullRequestFailDueToNoChanges(ex))
            {
                Console.WriteLine("There were no commits between the specified branchs.  Pull request not created.");
            }
        }

        /// <returns>The existing open merge PRs.</returns>
        private async Task<IList<PullRequest>> GetExistingMergePrsAsync(string newBranchPrefix)
        {
            var allPullRequests = await _client.PullRequest.GetAllForRepository(UserName, RepositoryName);
            var openPrs = allPullRequests.Where(pr => pr.Head.Ref.StartsWith(newBranchPrefix) && pr.User.Login == UserName).ToList();

            Console.WriteLine($"Found {openPrs.Count} existing open merge pull requests.");
            foreach (var pr in openPrs)
            {
                Console.WriteLine($"  Open PR: {pr.HtmlUrl}");
            }

            return openPrs;
        }

        /// <summary>
        /// The Octokit API fails on pull request creation if the PR would have been empty, but there is no way
        /// to know that ahead of time.
        ///
        /// The fall-back is to check for a very specific "failure".
        /// </summary>
        private static bool DidPullRequestFailDueToNoChanges(Exception ex)
        {
            if (!(ex is ApiValidationException apiException))
            {
                return false;
            }

            if (apiException.ApiError.Errors.Count != 1)
            {
                return false;
            }

            var error = apiException.ApiError.Errors.Single();
            return error.Message.StartsWith("No commits between");
        }
    }
}

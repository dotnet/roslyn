// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Octokit;
using System;
using System.Threading.Tasks;

namespace ConsoleApplication2
{

    class Program
    {
        /// <returns> The SHA at the tip of `branchName` in the repository `user/repo` </returns>
        static async Task<string> GetShaFromBranch(GitHubClient github, string user, string repo, string branchName) 
        {
            var refs = await github.GitDatabase.Reference.Get(user, repo, $"heads/{branchName}");
            return refs.Object.Sha;
        }

        /// <summary>
        /// Creates a PR branch on the bot account with the branch head at `sha`.
        /// </summary>
        /// <returns> The name of the branch that was created </returns>
        static async Task<string> MakePrBranch(GitHubClient github, string user, string repo, string sha, string branchNamePrefix)
        {
            var branchName = branchNamePrefix + DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");

            var resp = await github.Connection.Post<string>(
                uri: new Uri($"https://api.github.com/repos/{user}/{repo}/git/refs"),
                body: $"{{\"ref\": \"refs/heads/{branchName}\", \"sha\": \"{sha}\"", 
                accepts: "*/*", 
                contentType: "application/json");
            var statusCode = resp.HttpResponse.StatusCode;
            if (statusCode != System.Net.HttpStatusCode.Created)
            {
                throw new Exception($"Failed creating a new branch {branchName} on {user}/{repo} with code {statusCode}");
            }

            return branchName;
        }

        /// <summary>
        /// Creates a pull request 
        /// </summary>
        static async Task SubmitPullRequest(GitHubClient github, string remoteUser, string myUser, string repoName, 
                                            string newBranchName, string fromBranch, string intoBranch)
        {
            await github.PullRequest.Create(remoteUser, repoName,
                new NewPullRequest($"Merge {fromBranch} into {intoBranch}", head: $"{myUser}:{newBranchName}", baseRef: intoBranch) {
                    Body = $@"
This is an automatically generated pull request from {fromBranch} into {intoBranch}.

@dotnet/roslyn-infrastructure:

```bash
git remote add roslyn-bot ""https://github.com/roslyn-bot/roslyn.git""
git fetch roslyn-bot
git checkout {newBranchName}
git pull upstream {intoBranch}
# Fix merge conflicts
git commit
git push roslyn-bot {newBranchName} -f 
```

Once the merge can be made and all the tests pass, you are free to merge the pull request.

".Trim()
                });
        }

        static async Task MakePullRequest(GitHubClient github, string remoteUser, string myUser, string repoName, string fromBranch, string intoBranch)
        {

            var remoteIntoBranch = await GetShaFromBranch(github, remoteUser, repoName, fromBranch);
            var newBranchName = await MakePrBranch(github, myUser, repoName, remoteIntoBranch, $"{fromBranch}-{intoBranch}");
            await SubmitPullRequest(github, remoteUser, myUser, repoName, newBranchName, fromBranch, intoBranch);
            return;
        }

        static async Task _Main(GitHubClient github)
        {
            await MakePullRequest(github, remoteUser: "dotnet", myUser: "roslyn-bot", repoName: "roslyn", fromBranch: "master", intoBranch: "future");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("    roslyn-merge-bot [auth-code]");
            Console.WriteLine();
            Console.WriteLine("If the auth-code parameter is not set, an environment variable named AUTH_CODE must be set.");
        }

        static int Main(string[] args)
        {
            string auth = null;
            if (args.Length == 1)
            {
                if (args[0] == "--help" || args[0] == "/help")
                {
                    PrintUsage();
                    return 0;
                }
                auth = args[0];
            }
            else
            {
                auth = (string) Environment.GetEnvironmentVariables()["AUTH_CODE"];
            }

            if (auth != null) {
                var github = new GitHubClient(new ProductHeaderValue("roslyn-bot"));
                github.Credentials = new Credentials(auth);
                _Main(github).GetAwaiter().GetResult();
                return 0;
            }
            else
            {
                PrintUsage();
                return 1;
            }
        }
    }
}

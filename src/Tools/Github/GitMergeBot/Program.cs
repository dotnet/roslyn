// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Options;
using Octokit;

namespace GitMergeBot
{
    class Program
    {
        static int Main(string[] args)
        {
            var exeName = Assembly.GetExecutingAssembly().GetName().Name;
            var options = new Options();

            // default to using an environment variable, but allow an explicitly provided value to override
            options.AuthToken = Environment.GetEnvironmentVariable("AUTH_CODE");
            var parameters = new OptionSet()
            {
                $"Usage: {exeName} [options]",
                "Create a pull request from the specified user and branch to another specified user and branch.",
                "",
                "Options:",
                { "a|auth=", "The GitHub authentication token.", value => options.AuthToken = value },
                { "r|repo=", "The name of the remote repository.", value => options.RepoName = value },
                { "s|source=", "The source branch of the merge operation.", value => options.SourceBranch = value },
                { "d|dest=", "The destination branch of the merge operation.", value => options.DestinationBranch = value },
                { "su|sourceuser=", "The user hosting the source branch of the merge operation.", value => options.SourceUser = value },
                { "du|destuser=", "The user hosting the destination branch of the merge operation.", value => options.DestinationUser = value },
                { "debug", "Print debugging information about the merge but don't actually create the pull request.", value => options.Debug = value != null },
                { "h|help", "Show this message and exit.", value => options.ShowHelp = value != null }
            };

            try
            {
                parameters.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"{exeName}: {e.Message}");
                Console.WriteLine($"Try `{exeName} --help` for more information.");
                return 1;
            }

            if (options.ShowHelp || !options.AreValid)
            {
                parameters.WriteOptionDescriptions(Console.Out);
                return options.AreValid ? 0 : 1;
            }
            else
            {
                var github = new GitHubClient(new ProductHeaderValue(options.SourceUser));
                github.Credentials = new Credentials(options.AuthToken);
                new Program().MakePullRequest(github, options).GetAwaiter().GetResult();
                return 0;
            }
        }

        public async Task MakePullRequest(GitHubClient github, Options options)
        {

            var remoteIntoBranch = await GetShaFromBranch(github, options.DestinationUser, options.RepoName, options.SourceBranch);
            var newBranchName = await MakePrBranch(github, options, options.SourceUser, options.RepoName, remoteIntoBranch, $"merge-{options.SourceBranch}-into-{options.DestinationBranch}");
            await SubmitPullRequest(github, options, newBranchName);
            return;
        }

        /// <returns> The SHA at the tip of `branchName` in the repository `user/repo` </returns>
        private async Task<string> GetShaFromBranch(GitHubClient github, string user, string repo, string branchName) 
        {
            var refs = await github.GitDatabase.Reference.Get(user, repo, $"heads/{branchName}");
            return refs.Object.Sha;
        }

        /// <summary>
        /// Creates a PR branch on the bot account with the branch head at `sha`.
        /// </summary>
        /// <returns> The name of the branch that was created </returns>
        private async Task<string> MakePrBranch(GitHubClient github, Options options, string user, string repo, string sha, string branchNamePrefix)
        {
            var branchName = branchNamePrefix + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

            if (options.Debug)
            {
                WriteDebugLine($"Create remote branch '{user}/{repo}/{branchName}' at {sha}");
            }
            else
            {
                var resp = await github.Connection.Post<string>(
                    uri: new Uri($"https://api.github.com/repos/{user}/{repo}/git/refs"),
                    body: $"{{\"ref\": \"refs/heads/{branchName}\", \"sha\": \"{sha}\"",
                    accepts: "*/*",
                    contentType: "application/json");
                var statusCode = resp.HttpResponse.StatusCode;
                if (statusCode != HttpStatusCode.Created)
                {
                    throw new Exception($"Failed creating a new branch {branchName} on {user}/{repo} with code {statusCode}");
                }
            }

            return branchName;
        }

        /// <summary>
        /// Creates a pull request 
        /// </summary>
        private async Task SubmitPullRequest(GitHubClient github, Options options, string newBranchName)
        {
            var remoteName = $"{options.SourceUser}-{options.RepoName}";
            var prTitle = $"Merge {options.SourceBranch} into {options.DestinationBranch}";
            var prMessage = $@"
This is an automatically generated pull request from {options.SourceBranch} into {options.DestinationBranch}.

@dotnet/roslyn-infrastructure:

``` bash
git remote add {remoteName} ""https://github.com/{options.SourceUser}/{options.RepoName}.git""
git fetch {remoteName}
git checkout {newBranchName}
git reset --hard upstream/{options.DestinationBranch}
git merge upstream/{options.SourceBranch}
# Fix merge conflicts
git commit
git push {remoteName} {newBranchName} --force
```

Once the merge can be made and all the tests pass, you are free to merge the pull request.
".Trim();

            if (options.Debug)
            {
                WriteDebugLine($"Create PR with title: {prTitle}.");
                WriteDebugLine($"Create PR with body:\r\n{prMessage}");
            }
            else
            {
                await github.PullRequest.Create(
                    owner: options.DestinationUser,
                    name: options.RepoName,
                    newPullRequest: new NewPullRequest(
                        title: prTitle,
                        head: $"{options.SourceUser}:{newBranchName}",
                        baseRef: options.DestinationBranch)
                        {
                            Body = prMessage
                        }
                    );
            }
        }

        private void WriteDebugLine(string line)
        {
            Console.WriteLine("Debug: " + line);
        }
    }
}

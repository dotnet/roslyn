// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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
                { "f|force", "Force the creation of the PR even if an open PR already exists.", value => options.Force = value != null },
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
                new Program(options).MakePullRequest().GetAwaiter().GetResult();
                return 0;
            }
        }

        private Options _options;
        private GitHubClient _client;

        private Program(Options options)
        {
            _options = options;
        }

        public async Task MakePullRequest()
        {
            _client = new GitHubClient(new ProductHeaderValue(_options.SourceUser));
            _client.Credentials = new Credentials(_options.AuthToken);
            var remoteIntoBranch = await GetShaFromBranch(_options.DestinationUser, _options.RepoName, _options.SourceBranch);
            var newBranchPrefix = $"merge-{_options.SourceBranch}-into-{_options.DestinationBranch}";
            if (!_options.Force && await DoesOpenPrAlreadyExist(newBranchPrefix))
            {
                Console.WriteLine("Existing merge PRs exist; aboring creation.  Use `--force` option to override.");
                return;
            }

            var newBranchName = await MakePrBranch(_options.SourceUser, _options.RepoName, remoteIntoBranch, newBranchPrefix);
            await SubmitPullRequest(newBranchName);
            return;
        }

        /// <returns> The SHA at the tip of `branchName` in the repository `user/repo` </returns>
        private async Task<string> GetShaFromBranch(string user, string repo, string branchName) 
        {
            var refs = await _client.GitDatabase.Reference.Get(user, repo, $"heads/{branchName}");
            return refs.Object.Sha;
        }

        /// <returns>True if an existing auto merge PR is still open.</returns>
        private async Task<bool> DoesOpenPrAlreadyExist(string newBranchPrefix)
        {
            return (await GetExistingMergePrs(newBranchPrefix)).Count > 0;
        }

        /// <returns>The existing open merge PRs.</returns>
        private async Task<IList<PullRequest>> GetExistingMergePrs(string newBranchPrefix)
        {
            var allPullRequests = await _client.PullRequest.GetAllForRepository(_options.DestinationUser, _options.RepoName);
            var openPrs = allPullRequests.Where(pr => pr.Head.Ref.StartsWith(newBranchPrefix) && pr.User.Login == _options.SourceUser).ToList();

            Console.WriteLine($"Found {openPrs.Count} existing open merge pull requests.");
            foreach (var pr in openPrs)
            {
                Console.WriteLine($"  Open PR: {pr.HtmlUrl}");
            }

            return openPrs;
        }

        /// <summary>
        /// Creates a PR branch on the bot account with the branch head at `sha`.
        /// </summary>
        /// <returns> The name of the branch that was created </returns>
        private async Task<string> MakePrBranch(string user, string repo, string sha, string branchNamePrefix)
        {
            var branchName = branchNamePrefix + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");

            if (_options.Debug)
            {
                WriteDebugLine($"Create remote branch '{user}/{repo}/{branchName}' at {sha}");
            }
            else
            {
                var resp = await _client.Connection.Post<string>(
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
        private async Task SubmitPullRequest(string newBranchName)
        {
            var remoteName = $"{_options.SourceUser}-{_options.RepoName}";
            var prTitle = $"Merge {_options.SourceBranch} into {_options.DestinationBranch}";
            var prMessage = $@"
This is an automatically generated pull request from {_options.SourceBranch} into {_options.DestinationBranch}.

@dotnet/roslyn-infrastructure:

``` bash
git remote add {remoteName} ""https://github.com/{_options.SourceUser}/{_options.RepoName}.git""
git fetch {remoteName}
git fetch upstream
git checkout {newBranchName}
git reset --hard upstream/{_options.DestinationBranch}
git merge upstream/{_options.SourceBranch}
# Fix merge conflicts
git commit
git push {remoteName} {newBranchName} --force
```

Once the merge can be made and all the tests pass, you are free to merge the pull request.

@dotnet-bot test vsi please
".Trim();

            if (_options.Debug)
            {
                WriteDebugLine($"Create PR with title: {prTitle}.");
                WriteDebugLine($"Create PR with body:\r\n{prMessage}");
            }
            else
            {
                await _client.PullRequest.Create(
                    owner: _options.DestinationUser,
                    name: _options.RepoName,
                    newPullRequest: new NewPullRequest(
                        title: prTitle,
                        head: $"{_options.SourceUser}:{newBranchName}",
                        baseRef: _options.DestinationBranch)
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

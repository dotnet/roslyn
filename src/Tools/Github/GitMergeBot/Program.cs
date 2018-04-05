// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Configuration;
using LibGit2Sharp;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Mono.Options;

using static System.Console;

namespace GitMergeBot
{
    internal sealed class Program
    {
        static int Main(string[] args)
        {
            var exeName = Assembly.GetExecutingAssembly().GetName().Name;
            var showHelp = false;
            var options = new Options();
            var parameters = new OptionSet()
            {
                $"Usage: {exeName} [options]",
                "Create a pull request from the specified user and branch to another specified user and branch.",
                "",
                "Options:",
                { "repopath=", "The local path to the repository.", value => options.RepositoryPath = value },
                { "sourcetype=", "The source repository type.  Valid values are 'GitHub' and 'VisualStudioOnline'.", value => options.SourceRepoType = (RepositoryType)Enum.Parse(typeof(RepositoryType), value) },
                { "sourcereponame=", "The name of the source repository.", value => options.SourceRepoName = value },
                { "sourceproject=", "The name of the source project.  Only needed for VisualStudioOnline repos.", value => options.SourceProject = value },
                { "sourceuserid=", "The source user ID.  Only needed for VisualStudioOnline repos.", value => options.SourceUserId = value },
                { "sourceuser=", "The source user name.", value => options.SourceUserName = value },
                { "sourceauthsecret=", "The secret name for the source auth token.", value => options.SourceAuthTokenSecretName = value },
                { "sourceremote=", "The source remote name.", value => options.SourceRemoteName = value },
                { "sourcebranch=", "The source branch name.", value => options.SourceBranchName = value },
                { "pushtodestination=", "If true the PR branch will be pushed to the destination repository; if false the PR branch will be pushed to the source.", value => options.PushBranchToDestination = value != null },
                { "prbranchsourceremote=", "The name of the remote the PR should initiate from.  Defaults to `sourceremote` parameter.", value => options.PullRequestBranchSourceRemote = value },
                { "destinationtype=", "The destination repository type.  Valid values are 'GitHub' and 'VisualStudioOnline'.  Defaults to `sourcetype` parameter.", value => options.DestinationRepoType = (RepositoryType)Enum.Parse(typeof(RepositoryType), value) },
                { "destinationrepoowner=", "", value => options.DestinationRepoOwner = value },
                { "destinationreponame=", "The name of the destination repository.  Defaults to `sourcereponame` parameter.", value => options.DestinationRepoName = value },
                { "destinationproject=", "The name of the destination project.  Only needed for VisualStudioOnline repos.", value => options.DestinationProject = value },
                { "destinationuserid=", "The destination user ID.  Only needed for VisualStudioOnline repos.", value => options.DestinationUserId = value },
                { "destinationuser=", "The destination user name.  Defaults to `sourceuser` parameter.", value => options.DestinationUserName = value },
                { "destinationauthsecret=", "The secret name for the destination auth token.  Defaults to `sourceauthsecret` parameter.", value => options.DestinationAuthTokenSecretName = value },
                { "destinationremote=", "The destination remote name.  Defaults to `sourceremote` parameter.", value => options.DestinationRemoteName = value },
                { "destinationbranch=", "The destination branch name.  Defaults to `sourcebranch` parameter.", value => options.DestinationBranchName = value },
                { "f|force", "Force the creation of the PR even if an open PR already exists.", value => options.Force = value != null },
                { "debug", "Print debugging information about the merge but don't actually create the pull request.", value => options.Debug = value != null },
                { "h|?|help", "Show this message and exit.", value => showHelp = value != null }
            };

            try
            {
                parameters.Parse(args);
                if (showHelp || !options.IsValid)
                {
                    parameters.WriteOptionDescriptions(Out);
                    return options.IsValid ? 0 : 1;
                }

                var sourceRepository = RepositoryBase.Create(options.SourceRepoType, options.RepositoryPath, options.SourceRepoName, options.SourceProject, options.SourceUserId, options.SourceUserName, options.SourceAuthTokenSecretName, options.SourceRemoteName);
                var destRepository = RepositoryBase.Create(options.DestinationRepoType, options.RepositoryPath, options.DestinationRepoName, options.DestinationProject, options.DestinationUserId, options.DestinationUserName, options.DestinationAuthTokenSecretName, options.DestinationRemoteName);
                new Program(sourceRepository, destRepository, options).RunAsync().GetAwaiter().GetResult();
                return 0;
            }
            catch (OptionException e)
            {
                WriteLine($"{exeName}: {e.Message}");
                WriteLine($"Try `{exeName} --help` for more information.");
                return 1;
            }
        }

        private RepositoryBase _sourceRepo;
        private RepositoryBase _destRepo;
        private Options _options;

        private Program(RepositoryBase sourceRepository, RepositoryBase destinationRepository, Options options)
        {
            _sourceRepo = sourceRepository;
            _destRepo = destinationRepository;
            _options = options;
        }

        public async Task RunAsync()
        {
            await _sourceRepo.Initialize();
            await _destRepo.Initialize();

            // fetch latest sources
            WriteLine("Fetching.");
            _sourceRepo.Fetch(_options.PullRequestBranchSourceRemote);

            var (prRepo, prRemoteName, prUserName, prAuthTokenSecretName) = _options.PushBranchToDestination
                ? (_destRepo, _options.DestinationRemoteName, _options.DestinationUserName, _options.DestinationAuthTokenSecretName)
                : (_sourceRepo, _options.SourceRemoteName, _options.SourceUserName, _options.SourceAuthTokenSecretName);

            var prPassword = GetSecret(prAuthTokenSecretName).Result;

            // branch from the source
            var title = $"Merge {_options.SourceBranchName} into {_options.DestinationBranchName}";
            if (_options.Force || await prRepo.ShouldMakePullRequestAsync(title))
            {
            }
            else
            {
                WriteLine("Existing merge PRs exist; aborting creation.");
                return;
            }

            var prBranchName = $"merge-{_options.SourceBranchName}-into-{_options.DestinationBranchName}-{DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")}";
            var prSourceBranch = $"{_options.PullRequestBranchSourceRemote}/{_options.SourceBranchName}";
            WriteLine($"Creating branch '{prBranchName}' from '{prSourceBranch}'.");
            if (_options.Debug)
            {
                WriteLine("Debug: Skiping branch creation.");
            }
            else
            {
                var prBranch = prRepo.Repository.CreateBranch(prBranchName, prSourceBranch);
            }

            // push the branch
            var remote = prRepo.Repository.Network.Remotes[prRemoteName];
            WriteLine($"Pushing branch '{prBranchName}'.");
            if (_options.Debug)
            {
                WriteLine("Debug: Skipping branch push.");
            }
            else
            {
                var pushOptions = new PushOptions()
                {
                    CredentialsProvider = (url, usernameFromUrl, types) => new UsernamePasswordCredentials()
                    {
                        Username = prUserName,
                        Password = prPassword
                    }
                };
                prRepo.Repository.Network.Push(remote, $"+refs/heads/{prBranchName}:refs/heads/{prBranchName}", pushOptions);
            }

            // create PR
            WriteLine("Creating PR.");
            if (_options.Debug)
            {
                WriteLine("Debug: Skipping PR creation.");
            }
            else
            {
                await _destRepo.CreatePullRequestAsync(title, _options.DestinationRepoOwner, prBranchName, _options.PullRequestBranchSourceRemote, _options.SourceBranchName, _options.DestinationBranchName);
            }
        }

        private const string KeyVaultUrl = "https://roslyninfra.vault.azure.net:443";

        //  Use application ID of powershell cmdlets, see http://stackoverflow.com/questions/30096576/using-adal-for-accessing-the-azure-keyvault-on-behalf-of-a-user
        private const string ApplicationId = "1950a258-227b-4e31-a9cf-717495945fc2";

        /// <summary>
        /// Gets the specified secret from the key vault;
        /// </summary>
        public static async Task<string> GetSecret(string secretName)
        {
            var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessToken));
            var secret = await kv.GetSecretAsync(KeyVaultUrl, secretName);
            return secret.Value;
        }

        private static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            var context = new AuthenticationContext(authority);
            AuthenticationResult authResult;
            if (string.IsNullOrEmpty(WebConfigurationManager.AppSettings["ClientId"]))
            {
                // use default domain authentication
                authResult = await context.AcquireTokenAsync(resource, ApplicationId, new UserCredential());
            }
            else
            {
                // use client authentication; "ClientId" and "ClientSecret" are only available when run as a web job
                var credentials = new ClientCredential(WebConfigurationManager.AppSettings["ClientId"], WebConfigurationManager.AppSettings["ClientSecret"]);
                authResult = await context.AcquireTokenAsync(resource, credentials);
            }

            return authResult.AccessToken;
        }
    }
}

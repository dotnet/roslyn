// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Web.Configuration;
using LibGit2Sharp;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace GitMergeBot
{
    internal abstract class RepositoryBase
    {
        public Repository Repository { get; }
        public string RepositoryName { get; }
        public string UserName { get; }
        public string Password { get; }

        protected RepositoryBase(string path, string repoName, string userName, string password)
        {
            Repository = new Repository(path);
            RepositoryName = repoName;
            UserName = userName;
            Password = password;
        }

        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        public abstract Task<bool> ShouldMakePullRequestAsync(string title);
        public abstract Task CreatePullRequestAsync(string title, string destinationOwner, string pullRequestBranch, string prBranchSourceRemote, string sourceBranch, string destinationBranch);

        protected void WriteDebugLine(string line)
        {
            Console.WriteLine("Debug: " + line);
        }

        public void Fetch(string remoteName)
        {
            var fetchOptions = new FetchOptions()
            {
                CredentialsProvider = (url, usernameFromUrl, types) => new UsernamePasswordCredentials()
                {
                    Username = UserName,
                    Password = Password
                }
            };
            Repository.Fetch(remoteName, fetchOptions);
        }

        public static RepositoryBase Create(RepositoryType type, string path, string repoName, string project, string userId, string userName, string authTokenSecretName, string remoteName)
        {
            string password = Program.GetSecret(authTokenSecretName).Result;

            switch (type)
            {
                case RepositoryType.GitHub:
                    return new GitHubRepository(path, repoName, userName, password);
                case RepositoryType.VisualStudioOnline:
                    return new VisualStudioOnlineRepository(path, repoName, project, userId, userName, password, remoteName);
                default:
                    throw new InvalidOperationException("Unknown repository type.");
            }
        }
    }
}

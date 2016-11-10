// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using LibGit2Sharp;

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

        public abstract Task<bool> ShouldMakePullRequestAsync(string title);
        public abstract Task CreatePullRequestAsync(string title, string destinationOwner, string pullRequestBranch, string sourceBranch, string destinationBranch);

        protected void WriteDebugLine(string line)
        {
            Console.WriteLine("Debug: " + line);
        }

        public void FetchAll()
        {
            foreach (var remote in Repository.Network.Remotes)
            {
                Repository.Fetch(remote.Name);
            }
        }

        public static RepositoryBase Create(RepositoryType type, string path, string repoName, string userName, string password)
        {
            switch (type)
            {
                case RepositoryType.GitHub:
                    return new GitHubRepository(path, repoName, userName, password);
                case RepositoryType.VisualStudioOnline:
                    return new VisualStudioOnlineRepository(path, repoName, userName, password);
                default:
                    throw new InvalidOperationException("Unknown repository type.");
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace GitMergeBot
{
    internal sealed class VisualStudioOnlineRepository : RepositoryBase
    {
        public VisualStudioOnlineRepository(string path, string repoName, string userName, string password)
            : base(path, repoName, userName, password)
        {
        }

        public override Task<bool> ShouldMakePullRequestAsync(string title)
        {
            throw new NotImplementedException();
        }

        public override Task CreatePullRequestAsync(string title, string destinationOwner, string pullRequestBranch, string sourceBranch, string destinationBranch)
        {
            throw new NotImplementedException();
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace GitMergeBot
{
    internal sealed class Options
    {
        public string AuthToken { get; set; }
        public string RepoName { get; set; }
        public string SourceBranch { get; set; }
        public string DestinationBranch { get; set; }
        public string SourceUser { get; set; }
        public string DestinationUser { get; set; }
        public bool Debug { get; set; }
        public bool ShowHelp { get; set; }

        public bool AreValid => new[] { AuthToken, RepoName, SourceBranch, DestinationBranch, SourceUser, DestinationUser }.All(s => s != null);
    }
}

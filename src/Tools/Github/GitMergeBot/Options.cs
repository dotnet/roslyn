// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace GitMergeBot
{
    internal sealed class Options
    {
        public bool Force { get; set; }
        public bool Debug { get; set; }
        public string RepositoryPath { get; set; }
        public RepositoryType SourceRepoType { get; set; }
        public string SourceRepoName { get; set; }
        public string SourceProject { get; set; }
        public string SourceUserId { get; set; }
        public string SourceUserName { get; set; }
        public string SourceAuthTokenSecretName { get; set; }
        public string SourceRemoteName { get; set; }
        public string SourceBranchName { get; set; }

        public bool PushBranchToDestination { get; set; }

        private string _prBranchSourceRemote;
        private RepositoryType? _destinationRepoType;
        private string _destinationRepoOwner;
        private string _destinationRepoName;
        private string _destinationUserName;
        private string _destinationAuthTokenSecretName;
        private string _destinationRemoteName;
        private string _destinationBranchName;

        public string DestinationProject { get; set; }
        public string DestinationUserId { get; set; }

        public string PullRequestBranchSourceRemote
        {
            get { return _prBranchSourceRemote ?? SourceRemoteName; }
            set { _prBranchSourceRemote = value; }
        }

        public RepositoryType DestinationRepoType
        {
            get { return _destinationRepoType ?? SourceRepoType; }
            set { _destinationRepoType = value; }
        }

        public string DestinationRepoOwner
        {
            get { return _destinationRepoOwner ?? DestinationUserName; }
            set { _destinationRepoOwner = value; }
        }

        public string DestinationRepoName
        {
            get { return _destinationRepoName ?? SourceRepoName; }
            set { _destinationRepoName = value; }
        }

        public string DestinationUserName
        {
            get { return _destinationUserName ?? SourceUserName; }
            set { _destinationUserName = value; }
        }

        public string DestinationAuthTokenSecretName
        {
            get { return _destinationAuthTokenSecretName ?? SourceAuthTokenSecretName; }
            set { _destinationAuthTokenSecretName = value; }
        }

        public string DestinationRemoteName
        {
            get { return _destinationRemoteName ?? SourceRemoteName; }
            set { _destinationRemoteName = value; }
        }

        public string DestinationBranchName
        {
            get { return _destinationBranchName ?? SourceBranchName; }
            set { _destinationBranchName = value; }
        }

        public bool IsValid => new[]
        {
            RepositoryPath,
            SourceRepoName,
            SourceUserName,
            SourceAuthTokenSecretName,
            SourceRemoteName,
            SourceBranchName
        }.All(s => s != null);
    }
}

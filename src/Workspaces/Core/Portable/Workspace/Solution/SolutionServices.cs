// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This class basically holds onto a set of services and gets reused across solution instances.
    /// </summary>
    internal partial class SolutionServices
    {
        internal readonly Workspace Workspace;
        internal readonly ITemporaryStorageService TemporaryStorage;
        internal readonly IMetadataService MetadataService;
        internal readonly IProjectCacheHostService CacheService;

        internal bool SupportsCachingRecoverableObjects { get { return this.CacheService != null; } }

        public SolutionServices(Workspace workspace)
        {
            this.Workspace = workspace;
            this.TemporaryStorage = workspace.Services.GetService<ITemporaryStorageService>();
            this.MetadataService = workspace.Services.GetService<IMetadataService>();
            this.CacheService = workspace.Services.GetService<IProjectCacheHostService>();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

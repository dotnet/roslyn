// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This class basically holds onto a set of services and gets reused across solution instances.
    /// </summary>
    internal partial class SolutionServices
    {
        internal readonly Workspace Workspace;
        internal readonly ITemporaryStorageService TemporaryStorage;
        internal readonly ITextFactoryService TextFactory;
        internal readonly ITextCacheService TextCache;
        internal readonly ICompilationCacheService CompilationCacheService;
        internal readonly IMetadataService MetadataService;

        public SolutionServices(Workspace workspace)
        {
            this.Workspace = workspace;
            this.TemporaryStorage = workspace.Services.GetService<ITemporaryStorageService>();
            this.TextFactory = workspace.Services.GetService<ITextFactoryService>();
            this.TextCache = workspace.Services.GetService<ITextCacheService>();
            this.CompilationCacheService = workspace.Services.GetService<ICompilationCacheService>();
            this.MetadataService = workspace.Services.GetService<IMetadataService>();
        }
    }
}

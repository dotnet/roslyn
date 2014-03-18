// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This class basically holds onto a set of services and gets reused across solution instances.
    /// </summary>
    internal partial class SolutionServices
    {
        internal readonly Workspace Workspace;
        internal readonly IWorkspaceServiceProvider WorkspaceServices;
        internal readonly ILanguageServiceProviderFactory LanguageServicesFactory;
        internal readonly ITemporaryStorageService TemporaryStorage;
        internal readonly ITextFactoryService TextFactory;
        internal readonly ITextCacheService TextCache;
        internal readonly ICompilationCacheService CompilationCacheService;
        internal readonly MetadataReferenceProvider MetadataReferenceProvider;

        public SolutionServices(Workspace workspace, IWorkspaceServiceProvider workspaceServices)
        {
            this.Workspace = workspace;
            this.WorkspaceServices = workspaceServices;
            this.LanguageServicesFactory = WorkspaceServices.GetService<ILanguageServiceProviderFactory>();
            this.TemporaryStorage = WorkspaceServices.GetService<ITemporaryStorageService>();
            this.TextFactory = WorkspaceServices.GetService<ITextFactoryService>();
            this.TextCache = WorkspaceServices.GetService<ITextCacheService>();
            this.CompilationCacheService = WorkspaceServices.GetService<ICompilationCacheService>();
            this.MetadataReferenceProvider = WorkspaceServices.GetService<IMetadataReferenceProviderService>().GetProvider();
        }
    }
}

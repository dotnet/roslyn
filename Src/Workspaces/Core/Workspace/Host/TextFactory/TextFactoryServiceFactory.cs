// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
#if MEF
    [ExportWorkspaceServiceFactory(typeof(ITextFactoryService), WorkspaceKind.Any)]
#endif
    internal partial class TextFactoryServiceFactory : IWorkspaceServiceFactory
    {
        private readonly TextFactoryService singleton = new TextFactoryService();

        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return singleton;
        }
    }
}
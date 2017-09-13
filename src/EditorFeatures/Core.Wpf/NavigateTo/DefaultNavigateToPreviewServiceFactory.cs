// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    [ExportWorkspaceServiceFactory(typeof(INavigateToPreviewService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultNavigateToPreviewServiceFactory : IWorkspaceServiceFactory
    {
        private Lazy<INavigateToPreviewService> _singleton =
            new Lazy<INavigateToPreviewService>(() => new DefaultNavigateToPreviewService());

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton.Value;
        }
    }
}

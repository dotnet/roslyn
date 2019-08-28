// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo
{
    [ExportWorkspaceServiceFactory(typeof(INavigateToPreviewService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioNavigateToPreviewServiceFactory : IWorkspaceServiceFactory
    {
        private readonly Lazy<INavigateToPreviewService> _singleton =
            new Lazy<INavigateToPreviewService>(() => new VisualStudioNavigateToPreviewService());

        [ImportingConstructor]
        public VisualStudioNavigateToPreviewServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton.Value;
        }
    }
}

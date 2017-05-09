// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IErrorReportingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioErrorReportingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IErrorReportingService _singleton;

        [ImportingConstructor]
        public VisualStudioErrorReportingServiceFactory(IInfoBarService infoBarService)
        {
            _singleton = new VisualStudioErrorReportingService(infoBarService);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }
    }
}

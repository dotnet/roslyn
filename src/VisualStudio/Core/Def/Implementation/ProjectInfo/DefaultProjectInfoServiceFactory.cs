// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices.ProjectInfoService;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectInfoService
{
    [ExportWorkspaceServiceFactory(typeof(IProjectInfoService), ServiceLayer.Editor), Shared]
    internal sealed class DefaultProjectInfoServiceFactory : IWorkspaceServiceFactory
    {
        private Lazy<IProjectInfoService> _singleton =
            new Lazy<IProjectInfoService>(() => new DefaultProjectInfoService());

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton.Value;
        }
    }
}

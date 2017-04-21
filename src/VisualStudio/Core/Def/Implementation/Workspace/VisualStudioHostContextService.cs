// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IHostContextService), ServiceLayer.Host), Shared]
    internal class VisualStudioHostContextService : IHostContextService
    {
        public bool SolutionExistsAndFullyLoaded
            => KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive;
    }
}
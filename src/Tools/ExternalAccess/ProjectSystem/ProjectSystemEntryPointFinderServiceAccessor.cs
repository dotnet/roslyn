// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    internal sealed class ProjectSystemEntryPointFinderServiceAccessor : IProjectSystemEntryPointFinderServiceAccessor
    {
        private readonly HostWorkspaceServices _workspaceServices;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public ProjectSystemEntryPointFinderServiceAccessor(HostWorkspaceServices workspaceServices)
        {
            _workspaceServices = workspaceServices;
        }

        public IEnumerable<INamedTypeSymbol> FindEntryPoints(string languageName, INamespaceSymbol symbol, bool findFormsOnly)
        {
            var service = _workspaceServices.GetLanguageServices(languageName).GetRequiredService<IEntryPointFinderService>();
            return service.FindEntryPoints(symbol, findFormsOnly);
        }
    }
}

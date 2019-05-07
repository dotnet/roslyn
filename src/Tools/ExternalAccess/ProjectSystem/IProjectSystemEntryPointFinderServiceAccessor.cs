// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    public interface IProjectSystemEntryPointFinderServiceAccessor : IWorkspaceService
    {
        IEnumerable<INamedTypeSymbol> FindEntryPoints(string languageName, INamespaceSymbol symbol, bool findFormsOnly);
    }
}

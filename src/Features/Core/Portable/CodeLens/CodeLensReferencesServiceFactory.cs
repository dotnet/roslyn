// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeLens
{
    [ExportWorkspaceServiceFactory(typeof(ICodeLensReferencesService)), Shared]
    internal sealed class CodeLensReferencesServiceFactory : IWorkspaceServiceFactory
    {
        public static readonly ICodeLensReferencesService Instance = new CodeLensReferencesService();

        [ImportingConstructor]
        public CodeLensReferencesServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return Instance;
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.CodeLens
{
    [ExportWorkspaceServiceFactory(typeof(ICodeLensReferencesServiceAccessor))]
    [Shared]
    internal sealed class CodeLensReferencesServiceAccessorFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeLensReferencesServiceAccessorFactory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var implementation = workspaceServices.GetRequiredService<ICodeLensReferencesService>();
            return new CodeLensReferencesServiceAccessor(implementation);
        }
    }
}

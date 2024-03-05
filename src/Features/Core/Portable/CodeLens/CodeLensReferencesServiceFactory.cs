// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CodeLens;

[ExportWorkspaceServiceFactory(typeof(ICodeLensReferencesService)), Shared]
internal sealed class CodeLensReferencesServiceFactory : IWorkspaceServiceFactory
{
    public static readonly ICodeLensReferencesService Instance = new CodeLensReferencesService();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensReferencesServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => Instance;
}

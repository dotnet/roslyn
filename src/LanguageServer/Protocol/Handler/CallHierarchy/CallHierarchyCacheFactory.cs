// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

[ExportCSharpVisualBasicLspServiceFactory(typeof(CallHierarchyCache)), Shared]
internal sealed class CallHierarchyCacheFactory : ILspServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CallHierarchyCacheFactory()
    {
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new CallHierarchyCache();
}

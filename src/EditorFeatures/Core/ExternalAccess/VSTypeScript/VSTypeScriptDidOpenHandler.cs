// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[Shared]
[ExportLspRequestHandlerProvider(ProtocolConstants.TypeScriptLanguageContract, typeof(DidOpenHandler))]
internal class VSTypeScriptDidOpenHandler : DidOpenHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VSTypeScriptDidOpenHandler()
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

namespace Microsoft.CodeAnalysis.LanguageServer.EditorConfig;

[ExportStatelessLspService(typeof(DidCloseHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
internal class EditorConfigDidCloseHandler : DidCloseHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorConfigDidCloseHandler()
    {
    }
}

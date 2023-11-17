// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle the command that adds an event handler method in code
/// </summary>
[ExportXamlStatelessLspService(typeof(CreateEventCommandHandler)), Shared]
[Command(StringConstants.CreateEventHandlerCommand)]
internal class CreateEventCommandHandler : XamlRequestHandlerBase<LSP.ExecuteCommandParams, object>
{
    [ImportingConstructor]
    [Obsolete(StringConstants.ImportingConstructorMessage, error: true)]
    public CreateEventCommandHandler([Import(AllowDefault = true)] IXamlRequestHandler<LSP.ExecuteCommandParams, object> xamlHandler)
        : base(xamlHandler)
    {
    }

    public override bool RequiresLSPSolution => true;

    public override TextDocumentIdentifier GetTextDocumentIdentifier(ExecuteCommandParams request)
        => ((JToken)request.Arguments!.Last()).ToObject<TextDocumentIdentifier>()!;
}

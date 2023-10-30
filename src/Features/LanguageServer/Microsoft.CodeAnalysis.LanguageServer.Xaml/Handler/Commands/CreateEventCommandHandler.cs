// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Commands;
using Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.LanguageServer.Xaml.Handler;

/// <summary>
/// Handle the command that adds an event handler method in code
/// </summary>
[ExportXamlStatelessLspService(typeof(CreateEventCommandHandler)), Shared]
[Command(StringConstants.CreateEventHandlerCommand)]
internal class CreateEventCommandHandler : AbstractExecuteWorkspaceCommandHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CreateEventCommandHandler()
    {
    }

    public override string Command => StringConstants.CreateEventHandlerCommand;

    public override bool MutatesSolutionState => false;

    public override bool RequiresLSPSolution => true;

    public override TextDocumentIdentifier GetTextDocumentIdentifier(ExecuteCommandParams request)
        => ((JToken)request.Arguments!.Last()).ToObject<TextDocumentIdentifier>()!;

    public override async Task<object> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.Arguments);

        var document = context.TextDocument;
        if (document is null)
        {
            return false;
        }

        var commandService = document.Project.Services.GetService<IXamlCommandService>();
        if (commandService is null)
        {
            return false;
        }

        // request.Arguments has two arguments for CreateEventHandlerCommand
        // Arguments[0]: XamlEventDescription
        // Arguments[1]: TextDocumentIdentifier
        var eventDescription = ((JToken)request.Arguments.First()).ToObject<XamlEventDescription>();
        var arguments = eventDescription is not null ? new object[] { eventDescription } : null;
        return await commandService.ExecuteCommandAsync(document, request.Command, arguments, cancellationToken).ConfigureAwait(false);
    }
}

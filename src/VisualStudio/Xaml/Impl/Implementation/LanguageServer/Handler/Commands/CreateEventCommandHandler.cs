// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Commands;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;
using Newtonsoft.Json.Linq;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

/// <summary>
/// Handle the command that adds an event handler method in code
/// </summary>
[ExportStatelessXamlLspService(typeof(CreateEventCommandHandler)), Shared]
[Command(StringConstants.CreateEventHandlerCommand)]
internal sealed class CreateEventCommandHandler : AbstractExecuteWorkspaceCommandHandler
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
        => ((JToken)request.Arguments.First()).ToObject<TextDocumentIdentifier>()!;

    public override async Task<object> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.Arguments);

        var document = context.Document;
        if (document == null)
        {
            return false;
        }

        var commandService = document.Project.Services.GetService<IXamlCommandService>();
        if (commandService == null)
        {
            return false;
        }

        // request.Arguments has two argument for CreateEventHandlerCommand
        // Arguments[0]: TextDocumentIdentifier
        // Arguments[1]: XamlEventDescription
        var arguments = new object[] { ((JToken)request.Arguments[1]).ToObject<XamlEventDescription>() };
        return await commandService.ExecuteCommandAsync(document, request.Command, arguments, cancellationToken).ConfigureAwait(false);
    }
}

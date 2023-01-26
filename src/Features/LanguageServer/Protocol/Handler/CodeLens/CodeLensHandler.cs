// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;

[ExportCSharpVisualBasicStatelessLspService(typeof(CodeLensHandler)), Shared]
[Method(LSP.Methods.TextDocumentCodeLensName)]
internal sealed class CodeLensHandler : ILspServiceDocumentRequestHandler<LSP.CodeLensParams, LSP.CodeLens[]?>
{
    /// <summary>
    /// Command name implemented by the client and invoked when the references code lens is selected.
    /// </summary>
    private const string ClientReferencesCommand = "roslyn.client.peekReferences";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeLensHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeLensParams request)
        => request.TextDocument;

    public async Task<LSP.CodeLens[]?> HandleRequestAsync(LSP.CodeLensParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var codeLensMemberFinder = document.GetRequiredLanguageService<ICodeLensMemberFinder>();
        var members = await codeLensMemberFinder.GetCodeLensMembersAsync(document, cancellationToken).ConfigureAwait(false);

        if (members.IsEmpty)
        {
            return Array.Empty<LSP.CodeLens>();
        }

        var codeLensReferencesService = document.Project.Solution.Services.GetRequiredService<ICodeLensReferencesService>();
        using var _ = ArrayBuilder<LSP.CodeLens>.GetInstance(out var codeLenses);

        // TODO - Code lenses need to be refreshed by the server when we detect solution/project wide changes.
        // See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1730462

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        foreach (var member in members)
        {
            var referenceCount = await codeLensReferencesService.GetReferenceCountAsync(document.Project.Solution, document.Id, member.Node, maxSearchResults: 99, cancellationToken).ConfigureAwait(false);
            if (referenceCount != null)
            {
                var range = ProtocolConversions.TextSpanToRange(member.Span, text);
                var codeLens = new LSP.CodeLens
                {
                    Range = range,
                    Command = new LSP.Command
                    {
                        Title = referenceCount.Value.GetDescription(),
                        CommandIdentifier = ClientReferencesCommand,
                        Arguments = new object[]
                        {
                            request.TextDocument.Uri,
                            range.Start
                        }
                    }
                };

                codeLenses.Add(codeLens);
            }
        }

        return codeLenses.ToArray();
    }
}


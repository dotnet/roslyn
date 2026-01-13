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
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;
using Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// </summary>
    [ExportLspRequestHandlerProvider(StringConstants.XamlLanguageName), Shared]
    [ProvidesMethod(LSP.Methods.TextDocumentCompletionResolveName)]
    internal class CompletionResolveHandler : AbstractStatelessRequestHandler<LSP.CompletionItem, LSP.CompletionItem>
    {
        public override string Method => LSP.Methods.TextDocumentCompletionResolveName;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionResolveHandler()
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(CompletionItem request) => null;

        public override async Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            if (completionItem is not VSCompletionItem vsCompletionItem)
            {
                return completionItem;
            }

            CompletionResolveData? data;
            if (completionItem.Data is JToken token)
            {
                data = token.ToObject<CompletionResolveData>();
                Assumes.Present(data);
            }
            else
            {
                return completionItem;
            }

            var documentId = DocumentId.CreateFromSerialized(ProjectId.CreateFromSerialized(data.ProjectGuid), data.DocumentGuid);
            var document = context.Solution.GetDocument(documentId) ?? context.Solution.GetAdditionalDocument(documentId);
            if (document == null)
            {
                return completionItem;
            }

            var offset = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(data.Position), cancellationToken).ConfigureAwait(false);
            var completionService = document.Project.LanguageServices.GetRequiredService<IXamlCompletionService>();
            var symbol = await completionService.GetSymbolAsync(new XamlCompletionContext(document, offset), completionItem.Label, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return completionItem;
            }

            var description = await symbol.GetDescriptionAsync(document, cancellationToken).ConfigureAwait(false);

            vsCompletionItem.Description = new ClassifiedTextElement(description.Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
            return vsCompletionItem;
        }
    }
}

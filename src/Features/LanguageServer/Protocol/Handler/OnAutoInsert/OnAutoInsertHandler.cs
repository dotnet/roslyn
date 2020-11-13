// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.Completion.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.MSLSPMethods.OnAutoInsertName, mutatesSolutionState: false)]
    internal class OnAutoInsertHandler : IRequestHandler<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem?>
    {
        private readonly ImmutableArray<IBraceCompletionService> _csharpBraceCompletionServices;
        private readonly ImmutableArray<IBraceCompletionService> _visualBasicBraceCompletionServices;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandler(
            [ImportMany(LanguageNames.CSharp)] IEnumerable<IBraceCompletionService> csharpBraceCompletionServices,
            [ImportMany(LanguageNames.VisualBasic)] IEnumerable<IBraceCompletionService> visualBasicBraceCompletionServices)
        {
            _csharpBraceCompletionServices = csharpBraceCompletionServices.ToImmutableArray();
            _visualBasicBraceCompletionServices = _visualBasicBraceCompletionServices.ToImmutableArray();
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.DocumentOnAutoInsertParams request) => request.TextDocument;

        public async Task<LSP.DocumentOnAutoInsertResponseItem?> HandleRequestAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;

            if (document == null)
            {
                return null;
            }

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            // The editor calls this handler for C# and VB comment characters, but we only need to process the one for the language that matches the document
            if (autoInsertParams.Character == "\n" || autoInsertParams.Character == service.DocumentationCommentCharacter)
            {
                var documentationCommentResponse = await GetDocumentationCommentResponseAsync(autoInsertParams, document, service, cancellationToken).ConfigureAwait(false);
                if (documentationCommentResponse != null)
                {
                    return documentationCommentResponse;
                }
            }

            // Only support this for razor as LSP doesn't support overtype yet.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1165179/
            // Once LSP supports overtype we can move all of brace completion to LSP.
            if (autoInsertParams.Character == "\n" && context.ClientName == document.Services.GetService<DocumentPropertiesService>()?.DiagnosticsLspClientName)
            {
                var braceCompletionAfterReturnResponse = await GetBraceCompletionAfterReturnResponseAsync(autoInsertParams, document, cancellationToken).ConfigureAwait(false);
                if (braceCompletionAfterReturnResponse != null)
                {
                    return braceCompletionAfterReturnResponse;
                }
            }

            return null;
        }

        private static async Task<LSP.DocumentOnAutoInsertResponseItem?> GetDocumentationCommentResponseAsync(
            LSP.DocumentOnAutoInsertParams autoInsertParams,
            Document document,
            IDocumentationCommentSnippetService service,
            CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var linePosition = ProtocolConversions.PositionToLinePosition(autoInsertParams.Position);
            var position = sourceText.Lines.GetPosition(linePosition);

            var result = autoInsertParams.Character == "\n"
                ? service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, sourceText, position, options, cancellationToken)
                : service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, sourceText, position, options, cancellationToken);

            if (result == null)
            {
                return null;
            }

            return new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = LSP.InsertTextFormat.Snippet,
                TextEdit = new LSP.TextEdit
                {
                    NewText = result.SnippetText.Insert(result.CaretOffset, "$0"),
                    Range = ProtocolConversions.TextSpanToRange(result.SpanToReplace, sourceText)
                }
            };
        }

        private async Task<LSP.DocumentOnAutoInsertResponseItem?> GetBraceCompletionAfterReturnResponseAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, Document document, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var linePosition = ProtocolConversions.PositionToLinePosition(autoInsertParams.Position);
            var position = sourceText.Lines.GetPosition(linePosition);

            var serviceAndContext = await GetBraceCompletionContextAsync(position, document, cancellationToken).ConfigureAwait(false);
            if (serviceAndContext == null)
            {
                return null;
            }

            var (service, context) = serviceAndContext.Value;
            var postReturnEdit = await service.GetTextChangeAfterReturnAsync(context, cancellationToken, supportsVirtualSpace: false).ConfigureAwait(false);
            if (postReturnEdit == null)
            {
                return null;
            }

            var textChange = await GetCollapsedChangeAsync(postReturnEdit.Value, document, cancellationToken).ConfigureAwait(false);
            Debug.Assert(postReturnEdit.Value.CaretLocation >= textChange.Span.Start);
            var offsetInTextChange = postReturnEdit.Value.CaretLocation - textChange.Span.Start;

            var newText = textChange.NewText!.Insert(offsetInTextChange, "$0");
            var autoInsertChange = new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = LSP.InsertTextFormat.Snippet,
                TextEdit = new LSP.TextEdit
                {
                    NewText = newText,
                    Range = ProtocolConversions.TextSpanToRange(textChange.Span, sourceText)
                }
            };

            return autoInsertChange;

            static async Task<TextChange> GetCollapsedChangeAsync(BraceCompletionResult result, Document oldDocument, CancellationToken cancellationToken)
            {
                var documentText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                foreach (var changesForVersion in result.TextChangesPerVersion)
                {
                    documentText = documentText.WithChanges(changesForVersion);
                }

                var newDocument = oldDocument.WithText(documentText);
                var overallTextChanges = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
                return Collapse(documentText, overallTextChanges.ToImmutableArray());
            }
        }

        private async Task<(IBraceCompletionService Service, BraceCompletionContext Context)?> GetBraceCompletionContextAsync(int caretLocation, Document document, CancellationToken cancellationToken)
        {
            var servicesForDocument = document.Project.Language switch
            {
                LanguageNames.CSharp => _csharpBraceCompletionServices,
                LanguageNames.VisualBasic => _visualBasicBraceCompletionServices,
                _ => throw new ArgumentException($"Language {document.Project.Language} is not recognized for OnAutoInsert")
            };

            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return servicesForDocument
                .Select(service => GetContext(caretLocation, syntaxRoot, document, service))
                .SingleOrDefault(serviceAndContext => serviceAndContext != null);

            static (IBraceCompletionService Service, BraceCompletionContext Context)? GetContext(int caretLocation, SyntaxNode root, Document document, IBraceCompletionService service)
            {
                var context = service.IsInsideCompletedBraces(caretLocation, root, document);
                if (context == null)
                {
                    return null;
                }

                return (service, context);
            }
        }
    }
}

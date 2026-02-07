// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Debugging;

/// <summary>
/// Handles the roslyn/debuggerCompletion request to provide IntelliSense completion
/// for debugger expression windows (QuickWatch, Watch, Immediate) in VSCode.
/// The approach is similar to that used for Visual Studio debugger completion.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(DebuggerCompletionHandler)), Shared]
[Method(LSP.Methods.RoslynDebuggerCompletionName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DebuggerCompletionHandler(IGlobalOptionService globalOptions)
    : ILspServiceDocumentRequestHandler<DebuggerCompletionParams, VSInternalCompletionList?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DebuggerCompletionParams request)
        => request.TextDocument;

    public async Task<VSInternalCompletionList?> HandleRequestAsync(
        DebuggerCompletionParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document == null)
        {
            context.TraceInformation("Debugger completion failed: document not found in workspace");
            return null;
        }

        var solution = context.Solution;
        Contract.ThrowIfNull(solution);

        try
        {
            // 1. Convert statementRange.End to absolute position
            var contextPoint = await document
                .GetPositionFromLinePositionAsync(
                    ProtocolConversions.PositionToLinePosition(request.StatementRange.End),
                    cancellationToken)
                .ConfigureAwait(false);

            // 2. Get the language-specific splicer
            var splicer = document.Project.Services.GetService<IDebuggerSplicer>();
            if (splicer == null)
            {
                context.TraceInformation($"Debugger completion not supported for language: {document.Project.Language}");
                return null;
            }

            // 3. Validate cursorOffset
            if (request.CursorOffset < 0 || request.CursorOffset > request.Expression.Length)
            {
                context.TraceInformation($"Invalid cursorOffset: {request.CursorOffset} for expression length {request.Expression.Length}");
                return null;
            }

            // 4. Compute spliced text and completion position
            var spliceResult = await splicer
                .SpliceAsync(document, contextPoint, request.Expression, request.CursorOffset, cancellationToken)
                .ConfigureAwait(false);

            // 5. Fork solution with spliced text (same pattern as VS implementation)
            // Use PreservationMode.PreserveIdentity to maintain document identity for completion
            var forkedSolution = solution.WithDocumentText(
                document.Id,
                spliceResult.Text,
                PreservationMode.PreserveIdentity);

            // Also update linked documents (for multi-targeting projects)
            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(
                    linkedDocumentId,
                    spliceResult.Text,
                    PreservationMode.PreserveIdentity);
            }

            // 6. Get the document from the forked solution
            var splicedDocument = forkedSolution.GetRequiredDocument(document.Id);

            // 7. Call existing completion pipeline with splice adjustment info.
            // This ensures that when completionItem/resolve runs later, the cached items have
            // positions shifted back to reference the original document (not the spliced one),
            // so semantic lookups work correctly.
            var capabilityHelper = new CompletionCapabilityHelper(context.GetRequiredClientCapabilities());
            var completionListCache = context.GetRequiredLspService<CompletionListCache>();
            var spliceAdjustment = new SpliceAdjustment(
                spliceResult.SpliceStart,
                spliceResult.InsertedLength);

            // Use debugger-specific completion options, matching the overrides applied by CompletionSource.
            var completionOptions = globalOptions.GetCompletionOptionsForLsp(document.Project.Language, capabilityHelper) with
            {
                FilterOutOfScopeLocals = false,
                ShowXmlDocCommentCompletion = false,
                CanAddImportStatement = false,
            };

            return await CompletionHandler
                .GetCompletionListAsync(
                    splicedDocument,
                    spliceResult.CompletionPosition,
                    request.Context,
                    globalOptions,
                    capabilityHelper,
                    completionListCache,
                    spliceAdjustment,
                    completionOptions,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log error and return null for graceful degradation
            context.TraceException(ex);
            return null;
        }
    }
}


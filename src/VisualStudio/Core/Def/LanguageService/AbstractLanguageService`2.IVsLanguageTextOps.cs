// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using RoslynTextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageTextOps
    where TPackage : AbstractPackage<TPackage, TLanguageService>
    where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
{
    public int Format(IVsTextLayer textLayer, TextSpan[] selections)
    {
        var result = VSConstants.S_OK;
        var uiThreadOperationExecutor = this.Package.ComponentModel.GetService<IUIThreadOperationExecutor>();
        uiThreadOperationExecutor.Execute(
            "Intellisense",
            defaultDescription: "",
            allowCancellation: true,
            showProgress: false,
            action: c => result = FormatWorker(textLayer, selections, c.UserCancellationToken));

        return result;
    }

    private int FormatWorker(IVsTextLayer textLayer, TextSpan[] selections, CancellationToken cancellationToken)
    {
        var editorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
        var textBuffer = editorAdaptersFactoryService.GetDataBuffer((IVsTextBuffer)textLayer);
        if (textBuffer == null)
        {
            return VSConstants.E_UNEXPECTED;
        }

        var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return VSConstants.E_FAIL;
        }

        var documentSyntax = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var text = documentSyntax.Text;
        var root = documentSyntax.Root;
        var editorOptionsService = this.Package.ComponentModel.GetService<EditorOptionsService>();
        var formattingOptions = textBuffer.GetSyntaxFormattingOptions(editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), document.Project.Services, explicitFormat: true);

        var ts = selections.Single();
        var start = text.Lines[ts.iStartLine].Start + ts.iStartIndex;
        var end = text.Lines[ts.iEndLine].Start + ts.iEndIndex;
        var adjustedSpan = GetFormattingSpan(root, start, end);

        // Since we know we are on the UI thread, lets get the base indentation now, so that there is less
        // cleanup work to do later in Venus.
        var ruleFactory = Workspace.Value.Services.GetService<IHostDependentFormattingRuleFactoryService>();

        // use formatting that return text changes rather than tree rewrite which is more expensive
        var formatter = document.GetRequiredLanguageService<ISyntaxFormattingService>();
        var originalChanges = formatter.GetFormattingResult(
            root, [adjustedSpan], formattingOptions,
            [ruleFactory.CreateRule(documentSyntax, start), .. Formatter.GetDefaultFormattingRules(document.Project.Services)],
            cancellationToken).GetTextChanges(cancellationToken);

        var originalSpan = RoslynTextSpan.FromBounds(start, end);
        var formattedChanges = ruleFactory.FilterFormattedChanges(document.Id, originalSpan, originalChanges);
        if (formattedChanges.IsEmpty())
        {
            return VSConstants.S_OK;
        }

        textBuffer.ApplyChanges(formattedChanges);

        return VSConstants.S_OK;
    }

    private static RoslynTextSpan GetFormattingSpan(SyntaxNode root, int start, int end)
    {
        // HACK: The formatting engine is inclusive in it's spans, so it won't insert
        // adjust the indentation if there is a token right at the spans we start at.
        // Instead, we make sure we include preceding indentation.
        var prevToken = root.FindToken(start).GetPreviousToken();
        if (prevToken != default)
        {
            start = prevToken.Span.Start;
        }

        var nextToken = root.FindTokenFromEnd(end).GetNextToken();
        if (nextToken != default)
        {
            end = nextToken.Span.End;
        }

        return RoslynTextSpan.FromBounds(start, end);
    }

    public int GetDataTip(IVsTextLayer textLayer, TextSpan[] selection, TextSpan[] tipSpan, out string text)
    {
        text = null;
        return VSConstants.E_NOTIMPL;
    }

    public int GetPairExtent(IVsTextLayer textLayer, TextAddress ta, TextSpan[] textSpan)
        => VSConstants.E_NOTIMPL;

    public int GetWordExtent(IVsTextLayer textLayer, TextAddress ta, WORDEXTFLAGS flags, TextSpan[] textSpan)
        => VSConstants.E_NOTIMPL;
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using RoslynTextSpan = Microsoft.CodeAnalysis.Text.TextSpan;
using TextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageTextOps
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        public int Format(IVsTextLayer textLayer, TextSpan[] selections)
        {
            var waitIndicator = this.Package.ComponentModel.GetService<IWaitIndicator>();
            var result = VSConstants.S_OK;
            waitIndicator.Wait(
                "Intellisense",
                allowCancel: true,
                action: c => result = FormatWorker(textLayer, selections, c.CancellationToken));

            return result;
        }

        private int FormatWorker(IVsTextLayer textLayer, TextSpan[] selections, CancellationToken cancellationToken)
        {
            var textBuffer = this.EditorAdaptersFactoryService.GetDataBuffer((IVsTextBuffer)textLayer);
            if (textBuffer == null)
            {
                return VSConstants.E_UNEXPECTED;
            }

            var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return VSConstants.E_FAIL;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var text = root.SyntaxTree.GetText(cancellationToken);
            var options = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var ts = selections.Single();
            var start = text.Lines[ts.iStartLine].Start + ts.iStartIndex;
            var end = text.Lines[ts.iEndLine].Start + ts.iEndIndex;
            var adjustedSpan = GetFormattingSpan(root, start, end);

            // Since we know we are on the UI thread, lets get the base indentation now, so that there is less
            // cleanup work to do later in Venus.
            var ruleFactory = this.Workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var rules = ruleFactory.CreateRule(document, start).Concat(Formatter.GetDefaultFormattingRules(document));

            // use formatting that return text changes rather than tree rewrite which is more expensive
            var originalChanges = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(adjustedSpan), document.Project.Solution.Workspace, options, rules, cancellationToken);

            var originalSpan = RoslynTextSpan.FromBounds(start, end);
            var formattedChanges = ruleFactory.FilterFormattedChanges(document, originalSpan, originalChanges);
            if (formattedChanges.IsEmpty())
            {
                return VSConstants.S_OK;
            }

            // create new formatted document
            var formattedDocument = document.WithText(text.WithChanges(formattedChanges));
            formattedDocument.Project.Solution.Workspace.ApplyDocumentChanges(formattedDocument, cancellationToken);

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
        {
            return VSConstants.E_NOTIMPL;
        }

        public int GetWordExtent(IVsTextLayer textLayer, TextAddress ta, WORDEXTFLAGS flags, TextSpan[] textSpan)
        {
            return VSConstants.E_NOTIMPL;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    internal class StackFrameViewModel : FrameViewModel
    {
        private readonly ParsedStackFrame _frame;
        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;

        private ISymbol? _cachedSymbol;
        private Document? _cachedDocument;
        private int _cachedLineNumber;

        public StackFrameViewModel(
            ParsedStackFrame frame,
            IThreadingContext threadingContext,
            Workspace workspace,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap,
            IStreamingFindUsagesPresenter streamingPresenter)
            : base(formatMap, typeMap)
        {
            _frame = frame;
            _threadingContext = threadingContext;
            _workspace = workspace;
            _streamingPresenter = streamingPresenter;
        }

        public override bool ShowMouseOver => true;

        public void NavigateToClass()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToClassAsync(cancellationToken), cancellationToken).ReportNonFatalErrorAsync();
        }

        public async Task NavigateToClassAsync(CancellationToken cancellationToken)
        {
            try
            {
                var symbol = await GetSymbolAsync(cancellationToken).ConfigureAwait(false);

                if (symbol is not { ContainingSymbol: not null })
                {
                    // Show some dialog?
                    return;
                }

                // Use the parent class instead of the method to navigate to
                symbol = symbol.ContainingSymbol;

                var success = NavigateToSymbol(symbol, cancellationToken);
                if (!success)
                {
                    // show some dialog?
                    return;
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        public void NavigateToSymbol()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToMethodAsync(cancellationToken), cancellationToken).ReportNonFatalErrorAsync();
        }

        public async Task NavigateToMethodAsync(CancellationToken cancellationToken)
        {
            try
            {
                var symbol = await _frame.ResolveSymbolAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

                if (symbol is null)
                {
                    // Show some dialog?
                    return;
                }

                var success = NavigateToSymbol(symbol, cancellationToken);

                if (!success)
                {
                    // Show some dialog?
                    return;
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        private bool NavigateToSymbol(ISymbol symbol, CancellationToken cancellationToken)
            => GoToDefinitionHelpers.TryGoToDefinition(
                    symbol,
                    _workspace.CurrentSolution,
                    _threadingContext,
                    _streamingPresenter,
                    thirdPartyNavigationAllowed: true,
                    cancellationToken: cancellationToken);

        public void NavigateToFile()
        {
            var cancellationToken = _threadingContext.DisposalToken;
            Task.Run(() => NavigateToFileAsync(cancellationToken), cancellationToken).ReportNonFatalErrorAsync();
        }

        public async Task NavigateToFileAsync(CancellationToken cancellationToken)
        {
            try
            {
                var (document, lineNumber) = GetDocumentAndLine();

                if (document is not null)
                {
                    // While navigating do not activate the tab, which will change focus from the tool window
                    var options = _workspace.Options
                            .WithChangedOption(new OptionKey(NavigationOptions.PreferProvisionalTab), true)
                            .WithChangedOption(new OptionKey(NavigationOptions.ActivateTab), false);

                    var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    // If the line number is larger than the total lines in the file
                    // then just go to the end of the file (lines count). This can happen
                    // if the file changed between the stack trace being looked at and the current
                    // version of the file.
                    lineNumber = Math.Min(sourceText.Lines.Count, lineNumber);

                    var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
                    if (navigationService is null)
                    {
                        return;
                    }

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    navigationService.TryNavigateToLineAndOffset(_workspace, document.Id, lineNumber - 1, 0, cancellationToken);
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
            {
            }
        }

        protected override IEnumerable<Inline> CreateInlines()
        {
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, _frame.GetTextBeforeType());

            var classLink = new Hyperlink();
            classLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.ClassName, _frame.GetQualifiedTypeText()));
            classLink.Click += (s, a) => NavigateToClass();
            classLink.RequestNavigate += (s, a) => NavigateToClass();
            yield return classLink;

            var methodLink = new Hyperlink();
            methodLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.MethodName, _frame.GetMethodText()));
            methodLink.Click += (s, a) => NavigateToSymbol();
            methodLink.RequestNavigate += (s, a) => NavigateToSymbol();
            yield return methodLink;

            if (_frame.FileSpan != default)
            {
                var textBetween = _frame.GetTextBetweenTypeAndFile();
                if (textBetween is not null)
                {
                    yield return MakeClassifiedRun(ClassificationTypeNames.Text, textBetween);
                }

                var fileText = _frame.GetFileText();
                RoslynDebug.AssertNotNull(fileText);

                var fileHyperlink = new Hyperlink();
                fileHyperlink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.Text, fileText));
                fileHyperlink.RequestNavigate += (s, e) => NavigateToFile();
                fileHyperlink.Click += (s, e) => NavigateToFile();
                yield return fileHyperlink;
            }

            yield return MakeClassifiedRun(ClassificationTypeNames.Text, _frame.GetTrailingText());
        }

        private (Document? document, int lineNumber) GetDocumentAndLine()
        {
            if (_cachedDocument is not null)
            {
                return (_cachedDocument, _cachedLineNumber);
            }

            (_cachedDocument, _cachedLineNumber) = _frame.GetDocumentAndLine(_workspace.CurrentSolution);
            return (_cachedDocument, _cachedLineNumber);
        }

        private async Task<ISymbol?> GetSymbolAsync(CancellationToken cancellationToken)
        {
            if (_cachedSymbol is not null)
            {
                return _cachedSymbol;
            }

            _cachedSymbol = await _frame.ResolveSymbolAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
            return _cachedSymbol;
        }
    }
}

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
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using System.Text;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{
    using StackFrameNodeOrToken = EmbeddedSyntaxNodeOrToken<StackFrameKind, StackFrameNode>;
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

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
            var methodDeclaration = _frame.Root.MethodDeclaration;
            var tree = _frame.Tree;
            var className = methodDeclaration.MemberAccessExpression.Left;
            var leadingTrivia = className.GetLeadingTrivia();
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, leadingTrivia.CreateString());

            //
            // Build the link to the class
            //

            var classLink = new Hyperlink();
            classLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.ClassName, className.CreateString(tree, skipTrivia: true)));
            classLink.Click += (s, a) => NavigateToClass();
            classLink.RequestNavigate += (s, a) => NavigateToClass();
            yield return classLink;

            // Since we're only using the left side of a qualified name, we expect 
            // there to be no trivia on the right (trailing).
            Debug.Assert(className.GetTrailingTrivia().IsDefaultOrEmpty);

            //
            // Build the link to the method
            //
            var methodLink = new Hyperlink();
            var methodTextBuilder = new StringBuilder();
            methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.DotToken.CreateString());
            methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.Right.CreateString(tree));

            if (methodDeclaration.TypeArguments is not null)
            {
                methodTextBuilder.Append(methodDeclaration.TypeArguments.CreateString(tree));
            }

            methodTextBuilder.Append(methodDeclaration.ArgumentList.CreateString(tree));
            methodLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.MethodName, methodTextBuilder.ToString()));
            methodLink.Click += (s, a) => NavigateToSymbol();
            methodLink.RequestNavigate += (s, a) => NavigateToSymbol();
            yield return methodLink;

            //
            // If there is file information build a link to that
            //
            if (_frame.Root.FileInformationExpression is not null)
            {
                var fileInformation = _frame.Root.FileInformationExpression;
                yield return MakeClassifiedRun(ClassificationTypeNames.Text, fileInformation.GetLeadingTrivia().CreateString());

                var fileLink = new Hyperlink();
                fileLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.Text, fileInformation.CreateString(tree, skipTrivia: true)));
                fileLink.Click += (s, a) => NavigateToFile();
                fileLink.RequestNavigate += (s, a) => NavigateToFile();
                yield return fileLink;

                yield return MakeClassifiedRun(ClassificationTypeNames.Text, fileInformation.GetTrailingTrivia().CreateString());
            }

            //
            // Don't lose the trailing trivia text
            //
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, _frame.Root.EndOfLineToken.CreateString());
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

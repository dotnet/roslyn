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
using Microsoft.CodeAnalysis.PooledObjects;

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
                var symbol = await GetSymbolAsync(cancellationToken).ConfigureAwait(false);

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
            var classNameLeafTokens = GetLeafTokens(className);
            Debug.Assert(classNameLeafTokens.Length > 1);
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(classNameLeafTokens[0].LeadingTrivia));

            //
            // Build the link to the class
            //

            var classLink = new Hyperlink();
            var classLinkText = GetStringWithoutFirstLeadingTriviaAndLastTrailingTrivia(classNameLeafTokens);
            classLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.ClassName, classLinkText));
            classLink.Click += (s, a) => NavigateToClass();
            classLink.RequestNavigate += (s, a) => NavigateToClass();
            yield return classLink;

            // Since we're only using the left side of a qualified name, we expect 
            // there to be no trivia on the right (trailing).
            Debug.Assert(classNameLeafTokens[^1].TrailingTrivia.IsEmpty);

            //
            // Build the link to the method
            //
            var methodLink = new Hyperlink();
            var methodTextBuilder = new StringBuilder();
            methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.DotToken.ToString());
            methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.Right.ToString());

            if (methodDeclaration.TypeArguments is not null)
            {
                methodTextBuilder.Append(methodDeclaration.TypeArguments.ToString());
            }

            methodTextBuilder.Append(methodDeclaration.ArgumentList.ToString());
            methodLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.MethodName, methodTextBuilder.ToString()));
            methodLink.Click += (s, a) => NavigateToSymbol();
            methodLink.RequestNavigate += (s, a) => NavigateToSymbol();
            yield return methodLink;

            //
            // If there is file information build a link to that
            //
            if (_frame.Root.FileInformationExpression is not null)
            {
                var leafTokens = GetLeafTokens(_frame.Root.FileInformationExpression);
                yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(leafTokens[0].LeadingTrivia));

                var fileLink = new Hyperlink();
                var fileLinkText = GetStringWithoutFirstLeadingTriviaAndLastTrailingTrivia(leafTokens);
                fileLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.Text, fileLinkText));
                fileLink.Click += (s, a) => NavigateToFile();
                fileLink.RequestNavigate += (s, a) => NavigateToFile();
                yield return fileLink;

                if (leafTokens.Length > 1)
                {
                    yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(leafTokens[^1].TrailingTrivia));
                }
            }

            //
            // Don't lose the trailing trivia text
            //
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, _frame.Root.EndOfLineToken.ToString());
        }

        private string GetStringWithoutFirstLeadingTriviaAndLastTrailingTrivia(ImmutableArray<StackFrameToken> leafTokens)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);

            sb.Append(leafTokens[0].VirtualChars.CreateString());
            sb.Append(CreateString(leafTokens[0].TrailingTrivia));

            if (leafTokens.Length == 1)
            {
                return sb.ToString();
            }

            for (var i = 1; i < leafTokens.Length - 1; i++)
            {
                var token = leafTokens[i];
                sb.Append(token.ToString());
            }

            sb.Append(CreateString(leafTokens[^1].LeadingTrivia));
            sb.Append(leafTokens[^1].VirtualChars.CreateString());

            return sb.ToString();
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

        private static ImmutableArray<StackFrameToken> GetLeafTokens(StackFrameNode node)
        {
            using var _ = ArrayBuilder<StackFrameToken>.GetInstance(out var builder);
            GetLeafTokens(node, builder);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Depth first traversal of the descendents of a node to the tokens
        /// </summary>
        private static void GetLeafTokens(StackFrameNode node, ArrayBuilder<StackFrameToken> builder)
        {
            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    GetLeafTokens(child.Node, builder);
                }
                else
                {
                    builder.Add(child.Token);
                }
            }
        }

        private static string CreateString(ImmutableArray<StackFrameTrivia> triviaList)
        {
            using var _ = PooledStringBuilder.GetInstance(out var sb);
            foreach (var trivia in triviaList)
            {
                sb.Append(trivia.ToString());
            }

            return sb.ToString();
        }
    }
}

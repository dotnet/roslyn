// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

internal sealed class StackFrameViewModel(
    ParsedStackFrame frame,
    IThreadingContext threadingContext,
    Workspace workspace,
    IClassificationFormatMap formatMap,
    ClassificationTypeMap typeMap) : FrameViewModel(formatMap, typeMap)
{
    private readonly ParsedStackFrame _frame = frame;
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly Workspace _workspace = workspace;
    private readonly IStackTraceExplorerService _stackExplorerService = workspace.Services.GetRequiredService<IStackTraceExplorerService>();
    private readonly Dictionary<StackFrameSymbolPart, DefinitionItem?> _definitionCache = [];

    private TextDocument? _cachedDocument;
    private int _cachedLineNumber;

    private readonly CancellationSeries _navigationCancellation = new(threadingContext.DisposalToken);

    public override bool ShowMouseOver => true;

    public void NavigateToClass()
    {
        var cancellationToken = _navigationCancellation.CreateNext();
        _ = NavigateToClassAsync(cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);
    }

    public async Task NavigateToClassAsync(CancellationToken cancellationToken)
    {
        try
        {
            var definition = await GetDefinitionAsync(StackFrameSymbolPart.ContainingType, cancellationToken).ConfigureAwait(false);
            await NavigateToDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
        {
        }
    }

    private async Task NavigateToDefinitionAsync(DefinitionItem? definition, CancellationToken cancellationToken)
    {
        if (definition is null)
            return;

        var location = await definition.GetNavigableLocationAsync(
            _workspace, cancellationToken).ConfigureAwait(false);
        await location.TryNavigateToAsync(
            _threadingContext, new NavigationOptions(PreferProvisionalTab: true, ActivateTab: false), cancellationToken).ConfigureAwait(false);
    }

    public void NavigateToSymbol()
    {
        var cancellationToken = _navigationCancellation.CreateNext();
        _ = NavigateToMethodAsync(cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);
    }

    public async Task NavigateToMethodAsync(CancellationToken cancellationToken)
    {
        try
        {
            var definition = await GetDefinitionAsync(StackFrameSymbolPart.Method, cancellationToken).ConfigureAwait(false);
            await NavigateToDefinitionAsync(definition, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, cancellationToken))
        {
        }
    }

    public void NavigateToFile()
    {
        var cancellationToken = _navigationCancellation.CreateNext();
        _ = NavigateToFileAsync(cancellationToken).ReportNonFatalErrorUnlessCancelledAsync(cancellationToken);
    }

    public async Task NavigateToFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (textDocument, lineNumber) = GetDocumentAndLine();

            if (textDocument is not null)
            {
                var sourceText = await textDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // If the line number is larger than the total lines in the file
                // then just go to the end of the file (lines count). This can happen
                // if the file changed between the stack trace being looked at and the current
                // version of the file.
                lineNumber = Math.Min(sourceText.Lines.Count, lineNumber);

                var navigationService = _workspace.Services.GetService<IDocumentNavigationService>();
                if (navigationService is null)
                    return;

                // While navigating do not activate the tab, which will change focus from the tool window
                var options = new NavigationOptions(PreferProvisionalTab: true, ActivateTab: false);

                await navigationService.TryNavigateToLineAndOffsetAsync(
                    _threadingContext, _workspace, textDocument.Id, lineNumber - 1, offset: 0, options, cancellationToken)
                    .ConfigureAwait(false);
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
        var classLeadingTrivia = GetLeadingTrivia(className);
        yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(classLeadingTrivia));

        //
        // Build the link to the class
        //

        var classLink = new Hyperlink();
        var classLinkText = className.ToString();
        classLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.ClassName, classLinkText));
        classLink.Click += (s, a) => NavigateToClass();
        classLink.RequestNavigate += (s, a) => NavigateToClass();
        yield return classLink;

        // Since we're only using the left side of a qualified name, we expect 
        // there to be no trivia on the right (trailing).
        Debug.Assert(GetTrailingTrivia(className).IsEmpty);

        //
        // Build the link to the method
        //
        var methodLink = new Hyperlink();
        var methodTextBuilder = new StringBuilder();
        methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.DotToken.ToFullString());
        methodTextBuilder.Append(methodDeclaration.MemberAccessExpression.Right.ToFullString());

        if (methodDeclaration.TypeArguments is not null)
        {
            methodTextBuilder.Append(methodDeclaration.TypeArguments.ToFullString());
        }

        methodTextBuilder.Append(methodDeclaration.ArgumentList.ToFullString());
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
            var leadingTrivia = GetLeadingTrivia(fileInformation);
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(leadingTrivia));

            var fileLink = new Hyperlink();
            var fileLinkText = _frame.Root.FileInformationExpression.ToString();
            fileLink.Inlines.Add(MakeClassifiedRun(ClassificationTypeNames.Text, fileInformation.ToString()));
            fileLink.Click += (s, a) => NavigateToFile();
            fileLink.RequestNavigate += (s, a) => NavigateToFile();
            yield return fileLink;

            var trailingTrivia = GetTrailingTrivia(fileInformation);
            yield return MakeClassifiedRun(ClassificationTypeNames.Text, CreateString(trailingTrivia));
        }

        //
        // Don't lose the trailing trivia text
        //
        yield return MakeClassifiedRun(ClassificationTypeNames.Text, _frame.Root.EndOfLineToken.ToFullString());
    }

    private (TextDocument? document, int lineNumber) GetDocumentAndLine()
    {
        if (_cachedDocument is not null)
        {
            return (_cachedDocument, _cachedLineNumber);
        }

        (_cachedDocument, _cachedLineNumber) = _stackExplorerService.GetDocumentAndLine(_workspace.CurrentSolution, _frame);
        return (_cachedDocument, _cachedLineNumber);
    }

    private async Task<DefinitionItem?> GetDefinitionAsync(StackFrameSymbolPart symbolPart, CancellationToken cancellationToken)
    {
        if (_definitionCache.TryGetValue(symbolPart, out var definition) && definition is not null)
        {
            return definition;
        }

        _definitionCache[symbolPart] = await _stackExplorerService.TryFindDefinitionAsync(_workspace.CurrentSolution, _frame, symbolPart, cancellationToken).ConfigureAwait(false);
        return _definitionCache[symbolPart];
    }

    private static ImmutableArray<StackFrameTrivia> GetLeadingTrivia(StackFrameNode node)
    {
        if (node.ChildCount == 0)
        {
            return [];
        }

        var child = node[0];
        if (child.IsNode)
        {
            return GetLeadingTrivia(child.Node);
        }

        return child.Token.LeadingTrivia;
    }

    private static ImmutableArray<StackFrameTrivia> GetTrailingTrivia(StackFrameNode node)
    {
        if (node.ChildCount == 0)
        {
            return [];
        }

        var child = node[^1];
        if (child.IsNode)
        {
            return GetTrailingTrivia(child.Node);
        }

        return child.Token.TrailingTrivia;
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

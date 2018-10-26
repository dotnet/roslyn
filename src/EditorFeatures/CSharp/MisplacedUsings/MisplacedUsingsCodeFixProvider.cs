// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MoveMisplacedUsings)]
    [Shared]
    internal sealed partial class MisplacedUsingsCodeFixProvider : CodeFixProvider
    {
        private const string SystemUsingDirectiveIdentifier = nameof(System);

        private static readonly List<UsingDirectiveSyntax> s_emptyUsingsList = new List<UsingDirectiveSyntax>();
        private static readonly SyntaxAnnotation s_usingPlacementCodeFixAnnotation = new SyntaxAnnotation(nameof(s_usingPlacementCodeFixAnnotation));
        private static readonly SymbolDisplayFormat s_fullNamespaceDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId);

        public override FixAllProvider GetFixAllProvider()
        {
            return FixAll.Instance;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var compilationUnit = (CompilationUnitSyntax)syntaxRoot;
            var options =  await context.Document.GetOptionsAsync(context.CancellationToken).ConfigureAwait(false);

            // do not offer a code fix for IDE0056 when there are multiple namespaces in the source file
            if (CountNamespaces(compilationUnit.Members) > 1)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new MoveMisplacedUsingsCodeAction(cancellationToken => 
                        GetTransformedDocumentAsync(context.Document, syntaxRoot, options, cancellationToken)),
                    diagnostic);
            }
        }

        private static async Task<Document> GetTransformedDocumentAsync(Document document, SyntaxNode syntaxRoot, OptionSet options, CancellationToken cancellationToken)
        {
            var fileHeader = GetFileHeader(syntaxRoot);
            var compilationUnit = (CompilationUnitSyntax)syntaxRoot;
            var usingPlacementPreference = DeterminePlacement(compilationUnit, options);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var usingsHelper = new UsingsSorter(options, semanticModel, compilationUnit, fileHeader);

            var documentOptions = await document.GetOptionsAsync(cancellationToken);
            var usingsIndentation = DetermineIndentation(compilationUnit, options, usingPlacementPreference);

            // - The strategy is to strip all using directive that are not inside a conditional directive and replace them later with a sorted list at the correct spot
            // - The using directives that are inside a conditional directive are replaced (in sorted order) on the spot.
            // - Conditional directives are not moved, as correctly parsing them is too tricky
            // - No using directives will be stripped when there are multiple namespaces. In that case everything is replaced on the spot.
            List<UsingDirectiveSyntax> stripList;
            var replaceMap = new Dictionary<UsingDirectiveSyntax, UsingDirectiveSyntax>();

            // When there are multiple namespaces, do not move using statements outside of them, only sort.
            if (usingPlacementPreference == UsingPlacementPreference.NoPreference)
            {
                BuildReplaceMapForNamespaces(usingsHelper, replaceMap, options, false);
                stripList = new List<UsingDirectiveSyntax>();
            }
            else
            {
                stripList = BuildStripList(usingsHelper);
            }

            BuildReplaceMapForConditionalDirectives(usingsHelper, replaceMap, options, usingsHelper.ConditionalRoot);

            var usingSyntaxRewriter = new UsingSyntaxRewriter(stripList, replaceMap, fileHeader);
            var newSyntaxRoot = usingSyntaxRewriter.Visit(syntaxRoot);

            if (usingPlacementPreference == UsingPlacementPreference.InsideNamespace)
            {
                newSyntaxRoot = AddUsingsToNamespace(newSyntaxRoot, usingsHelper, usingsIndentation, replaceMap.Any());
            }
            else if (usingPlacementPreference == UsingPlacementPreference.OutsideNamespace)
            {
                newSyntaxRoot = AddUsingsToCompilationRoot(newSyntaxRoot, usingsHelper, usingsIndentation, replaceMap.Any());
            }

            // Final cleanup
            newSyntaxRoot = StripMultipleBlankLines(newSyntaxRoot);
            newSyntaxRoot = ReAddFileHeader(newSyntaxRoot, fileHeader);

            var newDocument = document.WithSyntaxRoot(FormattingHelper.WithoutFormatting(newSyntaxRoot));

            return newDocument;
        }

        private static string DetermineIndentation(CompilationUnitSyntax compilationUnit, OptionSet options, UsingPlacementPreference usingPlacementPreference)
        {
            string usingsIndentation;

            if (usingPlacementPreference == UsingPlacementPreference.InsideNamespace)
            {
                var rootNamespace = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().First();
                var indentationLevel = IndentationHelper.GetIndentationSteps(options, rootNamespace);
                usingsIndentation = IndentationHelper.GenerateIndentationString(options, indentationLevel + 1);
            }
            else
            {
                usingsIndentation = string.Empty;
            }

            return usingsIndentation;
        }

        private static UsingPlacementPreference DeterminePlacement(CompilationUnitSyntax compilationUnit, OptionSet options)
        {
            switch (options.GetOption(CSharpCodeStyleOptions.PreferredUsingPlacement).Value)
            {
                case UsingPlacementPreference.InsideNamespace:
                    var namespaceCount = CountNamespaces(compilationUnit.Members);

                    // Only move using declarations inside the namespace when
                    // - There are no global attributes
                    // - There is only a single namespace declared at the top level
                    // - OrderingSettings.UsingPlacementPreference is set to InsideNamespace
                    if (compilationUnit.AttributeLists.Any()
                        || compilationUnit.Members.Count > 1
                        || namespaceCount > 1)
                    {
                        // Override the user's setting with a more conservative one
                        return UsingPlacementPreference.NoPreference;
                    }

                    if (namespaceCount == 0)
                    {
                        return UsingPlacementPreference.OutsideNamespace;
                    }

                    return UsingPlacementPreference.InsideNamespace;

                case UsingPlacementPreference.OutsideNamespace:
                    return UsingPlacementPreference.OutsideNamespace;

                case UsingPlacementPreference.NoPreference:
                default:
                    return UsingPlacementPreference.NoPreference;
            }
        }

        private static int CountNamespaces(SyntaxList<MemberDeclarationSyntax> members)
        {
            var result = 0;

            foreach (var namespaceDeclaration in members.OfType<NamespaceDeclarationSyntax>())
            {
                result += 1 + CountNamespaces(namespaceDeclaration.Members);
            }

            return result;
        }

        private static List<UsingDirectiveSyntax> BuildStripList(UsingsSorter usingsHelper)
        {
            return usingsHelper.GetContainedUsings(TreeTextSpan.Empty).ToList();
        }

        private static void BuildReplaceMapForNamespaces(UsingsSorter usingsHelper, Dictionary<UsingDirectiveSyntax, UsingDirectiveSyntax> replaceMap, OptionSet options, bool qualifyNames)
        {
            var usingsPerNamespace = usingsHelper
                .GetContainedUsings(TreeTextSpan.Empty)
                .GroupBy(ud => ud.Parent)
                .Select(gr => gr.ToList());

            foreach (var usingList in usingsPerNamespace)
            {
                if (usingList.Count > 0)
                {
                    // sort the original using declarations on Span.Start, in order to have the correct replace mapping.
                    usingList.Sort(CompareSpanStart);

                    var indentationSteps = IndentationHelper.GetIndentationSteps(options, usingList[0].Parent);
                    if (usingList[0].Parent is NamespaceDeclarationSyntax)
                    {
                        indentationSteps++;
                    }

                    var indentation = IndentationHelper.GenerateIndentationString(options, indentationSteps);

                    var modifiedUsings = usingsHelper.GenerateGroupedUsings(usingList, indentation, false, qualifyNames);

                    for (var i = 0; i < usingList.Count; i++)
                    {
                        replaceMap.Add(usingList[i], modifiedUsings[i]);
                    }
                }
            }
        }

        private static void BuildReplaceMapForConditionalDirectives(UsingsSorter usingsHelper, Dictionary<UsingDirectiveSyntax, UsingDirectiveSyntax> replaceMap, OptionSet options, TreeTextSpan rootSpan)
        {
            foreach (var childSpan in rootSpan.Children)
            {
                var originalUsings = usingsHelper.GetContainedUsings(childSpan);
                if (originalUsings.Count > 0)
                {
                    // sort the original using declarations on Span.Start, in order to have the correct replace mapping.
                    originalUsings.Sort(CompareSpanStart);

                    var indentationSteps = IndentationHelper.GetIndentationSteps(options, originalUsings[0].Parent);
                    if (originalUsings[0].Parent is NamespaceDeclarationSyntax)
                    {
                        indentationSteps++;
                    }

                    var indentation = IndentationHelper.GenerateIndentationString(options, indentationSteps);

                    var modifiedUsings = usingsHelper.GenerateGroupedUsings(childSpan, indentation, false, qualifyNames: false);

                    for (var i = 0; i < originalUsings.Count; i++)
                    {
                        replaceMap.Add(originalUsings[i], modifiedUsings[i]);
                    }
                }

                BuildReplaceMapForConditionalDirectives(usingsHelper, replaceMap, options, childSpan);
            }
        }

        private static int CompareSpanStart(UsingDirectiveSyntax left, UsingDirectiveSyntax right)
        {
            return left.SpanStart - right.SpanStart;
        }

        private static SyntaxNode AddUsingsToNamespace(SyntaxNode newSyntaxRoot, UsingsSorter usingsHelper, string usingsIndentation, bool hasConditionalDirectives)
        {
            var rootNamespace = ((CompilationUnitSyntax)newSyntaxRoot).Members.OfType<NamespaceDeclarationSyntax>().First();
            var withTrailingBlankLine = hasConditionalDirectives || rootNamespace.Members.Any() || rootNamespace.Externs.Any();

            var groupedUsings = usingsHelper.GenerateGroupedUsings(TreeTextSpan.Empty, usingsIndentation, withTrailingBlankLine, qualifyNames: false);
            groupedUsings = groupedUsings.AddRange(rootNamespace.Usings);

            var newRootNamespace = rootNamespace.WithUsings(groupedUsings);
            newSyntaxRoot = newSyntaxRoot.ReplaceNode(rootNamespace, newRootNamespace);

            return newSyntaxRoot;
        }

        private static SyntaxNode AddUsingsToCompilationRoot(SyntaxNode newSyntaxRoot, UsingsSorter usingsHelper, string usingsIndentation, bool hasConditionalDirectives)
        {
            var newCompilationUnit = (CompilationUnitSyntax)newSyntaxRoot;
            var withTrailingBlankLine = hasConditionalDirectives || newCompilationUnit.AttributeLists.Any() || newCompilationUnit.Members.Any() || newCompilationUnit.Externs.Any();

            var groupedUsings = usingsHelper.GenerateGroupedUsings(TreeTextSpan.Empty, usingsIndentation, withTrailingBlankLine, qualifyNames: true);
            groupedUsings = groupedUsings.AddRange(newCompilationUnit.Usings);
            newSyntaxRoot = newCompilationUnit.WithUsings(groupedUsings);

            return newSyntaxRoot;
        }

        private static SyntaxNode StripMultipleBlankLines(SyntaxNode syntaxRoot)
        {
            var replaceMap = new Dictionary<SyntaxToken, SyntaxToken>();

            var usingDirectives = syntaxRoot.GetAnnotatedNodes(s_usingPlacementCodeFixAnnotation).Cast<UsingDirectiveSyntax>();

            foreach (var usingDirective in usingDirectives)
            {
                var nextToken = usingDirective.SemicolonToken.GetNextToken(true);

                // start at -1 to compensate for the always present end-of-line.
                var trailingCount = -1;

                // count the blanks lines at the end of the using statement.
                foreach (var trivia in usingDirective.SemicolonToken.TrailingTrivia.Reverse())
                {
                    if (!trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        break;
                    }

                    trailingCount++;
                }

                // count the blank lines at the start of the next token
                var leadingCount = 0;

                foreach (var trivia in nextToken.LeadingTrivia)
                {
                    if (!trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        break;
                    }

                    leadingCount++;
                }

                if ((trailingCount + leadingCount) > 1)
                {
                    var totalStripCount = trailingCount + leadingCount - 1;

                    if (trailingCount > 0)
                    {
                        var trailingStripCount = Math.Min(totalStripCount, trailingCount);

                        var trailingTrivia = usingDirective.SemicolonToken.TrailingTrivia;
                        replaceMap[usingDirective.SemicolonToken] = usingDirective.SemicolonToken.WithTrailingTrivia(trailingTrivia.Take(trailingTrivia.Count - trailingStripCount));
                        totalStripCount -= trailingStripCount;
                    }

                    if (totalStripCount > 0)
                    {
                        replaceMap[nextToken] = nextToken.WithLeadingTrivia(nextToken.LeadingTrivia.Skip(totalStripCount));
                    }
                }
            }

            var newSyntaxRoot = syntaxRoot.ReplaceTokens(replaceMap.Keys, (original, rewritten) => replaceMap[original]);
            return newSyntaxRoot;
        }

        private static ImmutableArray<SyntaxTrivia> GetFileHeader(SyntaxNode syntaxRoot)
        {
            var onBlankLine = true;
            var hasHeader = false;
            var fileHeaderBuilder = ImmutableArray.CreateBuilder<SyntaxTrivia>();

            var firstToken = syntaxRoot.GetFirstToken(includeZeroWidth: true);
            var firstTokenLeadingTrivia = firstToken.LeadingTrivia;

            int i;
            for (i = 0; i < firstTokenLeadingTrivia.Count; i++)
            {
                var done = false;
                switch (firstTokenLeadingTrivia[i].Kind())
                {
                    case SyntaxKind.SingleLineCommentTrivia:
                    case SyntaxKind.MultiLineCommentTrivia:
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);
                        onBlankLine = false;
                        break;

                    case SyntaxKind.WhitespaceTrivia:
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);
                        break;

                    case SyntaxKind.EndOfLineTrivia:
                        hasHeader = true;
                        fileHeaderBuilder.Add(firstTokenLeadingTrivia[i]);

                        if (onBlankLine)
                        {
                            done = true;
                        }
                        else
                        {
                            onBlankLine = true;
                        }

                        break;

                    default:
                        done = true;
                        break;
                }

                if (done)
                {
                    break;
                }
            }

            return hasHeader ? fileHeaderBuilder.ToImmutableArray() : ImmutableArray.Create<SyntaxTrivia>();
        }

        private static SyntaxNode ReAddFileHeader(SyntaxNode syntaxRoot, ImmutableArray<SyntaxTrivia> fileHeader)
        {
            if (fileHeader.IsEmpty)
            {
                // Only re-add the file header if it was stripped.
                return syntaxRoot;
            }

            var firstToken = syntaxRoot.GetFirstToken(includeZeroWidth: true);
            var newLeadingTrivia = firstToken.LeadingTrivia.InsertRange(0, fileHeader);
            return syntaxRoot.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(newLeadingTrivia));
        }

        private class UsingSyntaxRewriter : CSharpSyntaxRewriter
        {
            private readonly List<UsingDirectiveSyntax> _stripList;
            private readonly Dictionary<UsingDirectiveSyntax, UsingDirectiveSyntax> _replaceMap;
            private readonly ImmutableArray<SyntaxTrivia> _fileHeader;
            private readonly LinkedList<SyntaxToken> _tokensToStrip = new LinkedList<SyntaxToken>();

            public UsingSyntaxRewriter(List<UsingDirectiveSyntax> stripList, Dictionary<UsingDirectiveSyntax, UsingDirectiveSyntax> replaceMap, ImmutableArray<SyntaxTrivia> fileHeader)
            {
                _stripList = stripList;
                _replaceMap = replaceMap;
                _fileHeader = fileHeader;
            }

            public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
            {
                // The strip list is used to remove using directives that will be moved.
                if (_stripList.Contains(node))
                {
                    var nextToken = node.SemicolonToken.GetNextToken();

                    if (!nextToken.IsKind(SyntaxKind.None))
                    {
                        var index = IndexOfFirstNonBlankLineTrivia(nextToken.LeadingTrivia);
                        if (index != 0)
                        {
                            _tokensToStrip.AddLast(nextToken);
                        }
                    }

                    return null;
                }

                // The replacement map is used to replace using declarations in place in sorted order (inside directive trivia)
                UsingDirectiveSyntax replacementNode;
                if (_replaceMap.TryGetValue(node, out replacementNode))
                {
                    return replacementNode;
                }

                return base.VisitUsingDirective(node);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_tokensToStrip.Contains(token))
                {
                    _tokensToStrip.Remove(token);

                    var index = IndexOfFirstNonBlankLineTrivia(token.LeadingTrivia);
                    var newLeadingTrivia = (index == -1) ? SyntaxFactory.TriviaList() : SyntaxFactory.TriviaList(token.LeadingTrivia.Skip(index));
                    return token.WithLeadingTrivia(newLeadingTrivia);
                }

                return base.VisitToken(token);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (_fileHeader.Contains(trivia))
                {
                    return default;
                }

                return base.VisitTrivia(trivia);
            }

            /// <summary>
            /// Returns the index of the first trivia that is not part of a blank line.
            /// </summary>
            /// <param name="triviaList">The trivia list to process.</param>
            /// <typeparam name="T">The type of the trivia list.</typeparam>
            /// <returns>The index of the first trivia that is not part of a blank line, or -1 if there is no such trivia.</returns>
            internal static int IndexOfFirstNonBlankLineTrivia<T>(T triviaList)
                where T : IReadOnlyList<SyntaxTrivia>
            {
                var firstNonWhitespaceTriviaIndex = IndexOfFirstNonWhitespaceTrivia(triviaList);
                var startIndex = (firstNonWhitespaceTriviaIndex == -1) ? triviaList.Count : firstNonWhitespaceTriviaIndex;

                for (var index = startIndex - 1; index >= 0; index--)
                {
                    // Find an end-of-line trivia, to indicate that there actually are blank lines and not just excess whitespace.
                    if (triviaList[index].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        return index == (triviaList.Count - 1) ? -1 : index + 1;
                    }
                }

                return 0;
            }

            /// <summary>
            /// Returns the index of the first non-whitespace trivia in the given trivia list.
            /// </summary>
            /// <param name="triviaList">The trivia list to process.</param>
            /// <param name="endOfLineIsWhitespace"><see langword="true"/> to treat <see cref="SyntaxKind.EndOfLineTrivia"/>
            /// as whitespace; otherwise, <see langword="false"/>.</param>
            /// <typeparam name="T">The type of the trivia list.</typeparam>
            /// <returns>The index where the non-whitespace starts, or -1 if there is no non-whitespace trivia.</returns>
            internal static int IndexOfFirstNonWhitespaceTrivia<T>(T triviaList, bool endOfLineIsWhitespace = true)
                where T : IReadOnlyList<SyntaxTrivia>
            {
                for (var index = 0; index < triviaList.Count; index++)
                {
                    var currentTrivia = triviaList[index];
                    switch (currentTrivia.Kind())
                    {
                        case SyntaxKind.EndOfLineTrivia:
                            if (!endOfLineIsWhitespace)
                            {
                                return index;
                            }

                            break;

                        case SyntaxKind.WhitespaceTrivia:
                            break;

                        default:
                            // encountered non-whitespace trivia -> the search is done.
                            return index;
                    }
                }

                return -1;
            }
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            public static FixAllProvider Instance { get; } = new FixAll();

            protected override string CodeActionTitle => CSharpEditorResources.Move_misplaced_using_statements;

            /// <inheritdoc/>
            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return null;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var options = await document.GetOptionsAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var newDocument = await GetTransformedDocumentAsync(document, syntaxRoot, options, fixAllContext.CancellationToken).ConfigureAwait(false);
                return await newDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }

        private class MoveMisplacedUsingsCodeAction : CodeAction 
        {
            private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;

            public override string Title => CSharpEditorResources.Move_misplaced_using_statements;

            public MoveMisplacedUsingsCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
            {
                _createChangedDocument = createChangedDocument;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return _createChangedDocument(cancellationToken);
            }
        }
    }
}

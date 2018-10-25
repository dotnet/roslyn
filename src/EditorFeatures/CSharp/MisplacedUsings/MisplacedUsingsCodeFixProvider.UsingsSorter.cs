// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        /// <summary>
        /// Helper class that will sort the using statements and generate new using groups based on the given settings.
        /// </summary>
        private class UsingsSorter
        {
            private readonly SemanticModel _semanticModel;
            private readonly ImmutableArray<SyntaxTrivia> _fileHeader;
            private readonly bool _separateSystemDirectives;
            private readonly bool _insertBlankLinesBetweenGroups;

            private readonly SourceMap sourceMap;

            private readonly Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> _systemUsings = new Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>>();
            private readonly Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> _namespaceUsings = new Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>>();
            private readonly Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> _aliases = new Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>>();
            private readonly Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> _systemStaticImports = new Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>>();
            private readonly Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> _staticImports = new Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>>();

            private readonly IComparer<NameSyntax> _nameSyntaxComparer = NameSyntaxComparer.Create();

            public UsingsSorter(OptionSet options, SemanticModel semanticModel, CompilationUnitSyntax compilationUnit, ImmutableArray<SyntaxTrivia> fileHeader)
            {
                this._separateSystemDirectives = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, semanticModel.Language);
                this._insertBlankLinesBetweenGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups, semanticModel.Language);

                this._semanticModel = semanticModel;
                this._fileHeader = fileHeader;

                this.sourceMap = SourceMap.FromCompilationUnit(compilationUnit);

                this.ProcessUsingDirectives(compilationUnit.Usings);
                this.ProcessMembers(compilationUnit.Members);
            }

            public TreeTextSpan ConditionalRoot
            {
                get { return this.sourceMap.ConditionalRoot; }
            }

            public List<UsingDirectiveSyntax> GetContainedUsings(TreeTextSpan directiveSpan)
            {
                List<UsingDirectiveSyntax> result = new List<UsingDirectiveSyntax>();
                List<UsingDirectiveSyntax> usingsList;

                if (this._systemUsings.TryGetValue(directiveSpan, out usingsList))
                {
                    result.AddRange(usingsList);
                }

                if (this._namespaceUsings.TryGetValue(directiveSpan, out usingsList))
                {
                    result.AddRange(usingsList);
                }

                if (this._aliases.TryGetValue(directiveSpan, out usingsList))
                {
                    result.AddRange(usingsList);
                }

                if (this._systemStaticImports.TryGetValue(directiveSpan, out usingsList))
                {
                    result.AddRange(usingsList);
                }

                if (this._staticImports.TryGetValue(directiveSpan, out usingsList))
                {
                    result.AddRange(usingsList);
                }

                return result;
            }

            public SyntaxList<UsingDirectiveSyntax> GenerateGroupedUsings(TreeTextSpan directiveSpan, string indentation, bool withTrailingBlankLine, bool qualifyNames)
            {
                var usingList = new List<UsingDirectiveSyntax>();
                List<SyntaxTrivia> triviaToMove = new List<SyntaxTrivia>();

                usingList.AddRange(this.GenerateUsings(this._systemUsings, directiveSpan, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._namespaceUsings, directiveSpan, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._systemStaticImports, directiveSpan, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._staticImports, directiveSpan, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._aliases, directiveSpan, indentation, triviaToMove, qualifyNames));

                if (triviaToMove.Count > 0)
                {
                    var newLeadingTrivia = SyntaxFactory.TriviaList(triviaToMove).AddRange(usingList[0].GetLeadingTrivia());
                    usingList[0] = usingList[0].WithLeadingTrivia(newLeadingTrivia);
                }

                if (withTrailingBlankLine && (usingList.Count > 0))
                {
                    var lastUsing = usingList[usingList.Count - 1];
                    usingList[usingList.Count - 1] = lastUsing.WithTrailingTrivia(lastUsing.GetTrailingTrivia().Add(SyntaxFactory.CarriageReturnLineFeed));
                }

                return SyntaxFactory.List(usingList);
            }

            public SyntaxList<UsingDirectiveSyntax> GenerateGroupedUsings(List<UsingDirectiveSyntax> usingsList, string indentation, bool withTrailingBlankLine, bool qualifyNames)
            {
                var usingList = new List<UsingDirectiveSyntax>();
                List<SyntaxTrivia> triviaToMove = new List<SyntaxTrivia>();

                usingList.AddRange(this.GenerateUsings(this._systemUsings, usingsList, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._namespaceUsings, usingsList, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._systemStaticImports, usingsList, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._staticImports, usingsList, indentation, triviaToMove, qualifyNames));
                usingList.AddRange(this.GenerateUsings(this._aliases, usingsList, indentation, triviaToMove, qualifyNames));

                if (triviaToMove.Count > 0)
                {
                    var newLeadingTrivia = SyntaxFactory.TriviaList(triviaToMove).AddRange(usingList[0].GetLeadingTrivia());
                    usingList[0] = usingList[0].WithLeadingTrivia(newLeadingTrivia);
                }

                if (withTrailingBlankLine && (usingList.Count > 0))
                {
                    var lastUsing = usingList[usingList.Count - 1];
                    usingList[usingList.Count - 1] = lastUsing.WithTrailingTrivia(lastUsing.GetTrailingTrivia().Add(SyntaxFactory.CarriageReturnLineFeed));
                }

                return SyntaxFactory.List(usingList);
            }

            private List<UsingDirectiveSyntax> GenerateUsings(Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> usingsGroup, TreeTextSpan directiveSpan, string indentation, List<SyntaxTrivia> triviaToMove, bool qualifyNames)
            {
                List<UsingDirectiveSyntax> result = new List<UsingDirectiveSyntax>();
                List<UsingDirectiveSyntax> usingsList;

                if (!usingsGroup.TryGetValue(directiveSpan, out usingsList))
                {
                    return result;
                }

                return this.GenerateUsings(usingsList, indentation, triviaToMove, qualifyNames);
            }

            private List<UsingDirectiveSyntax> GenerateUsings(List<UsingDirectiveSyntax> usingsList, string indentation, List<SyntaxTrivia> triviaToMove, bool qualifyNames)
            {
                List<UsingDirectiveSyntax> result = new List<UsingDirectiveSyntax>();

                if (!usingsList.Any())
                {
                    return result;
                }

                for (var i = 0; i < usingsList.Count; i++)
                {
                    var currentUsing = usingsList[i];

                    // strip the file header, if the using is the first node in the source file.
                    List<SyntaxTrivia> leadingTrivia;
                    if ((i == 0) && IsMissingOrDefault(currentUsing.GetFirstToken().GetPreviousToken()))
                    {
                        leadingTrivia = currentUsing.GetLeadingTrivia().Except(this._fileHeader).ToList();
                    }
                    else
                    {
                        leadingTrivia = currentUsing.GetLeadingTrivia().ToList();
                    }

                    // when there is a directive trivia, add it (and any trivia before it) to the triviaToMove collection.
                    // when there are leading blank lines for the first entry, add them to the triviaToMove collection.
                    int triviaToMoveCount = triviaToMove.Count;
                    var previousIsEndOfLine = false;
                    for (var m = leadingTrivia.Count - 1; m >= 0; m--)
                    {
                        if (leadingTrivia[m].IsDirective)
                        {
                            // When a directive is followed by a blank line, keep the blank line with the directive.
                            int takeCount = previousIsEndOfLine ? m + 2 : m + 1;
                            triviaToMove.InsertRange(0, leadingTrivia.Take(takeCount));
                            break;
                        }

                        if ((i == 0) && leadingTrivia[m].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            if (previousIsEndOfLine)
                            {
                                triviaToMove.InsertRange(0, leadingTrivia.Take(m + 2));
                                break;
                            }

                            previousIsEndOfLine = true;
                        }
                        else
                        {
                            previousIsEndOfLine = false;
                        }
                    }

                    // preserve leading trivia (excluding directive trivia), indenting each line as appropriate
                    var newLeadingTrivia = leadingTrivia.Except(triviaToMove).ToList();

                    // indent the triviaToMove if necessary so it behaves correctly later
                    bool atStartOfLine = triviaToMoveCount == 0 || HasBuiltinEndLine(triviaToMove.Last());
                    for (int m = triviaToMoveCount; m < triviaToMove.Count; m++)
                    {
                        bool currentAtStartOfLine = atStartOfLine;
                        atStartOfLine = HasBuiltinEndLine(triviaToMove[m]);
                        if (!currentAtStartOfLine)
                        {
                            continue;
                        }

                        if (triviaToMove[m].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            // This is a blank line; indenting it would only add trailing whitespace
                            continue;
                        }

                        if (triviaToMove[m].IsDirective)
                        {
                            // Only #region and #endregion directives get indented
                            if (!triviaToMove[m].IsKind(SyntaxKind.RegionDirectiveTrivia) && !triviaToMove[m].IsKind(SyntaxKind.EndRegionDirectiveTrivia))
                            {
                                // This is a preprocessor line that doesn't need to be indented
                                continue;
                            }
                        }

                        if (triviaToMove[m].IsKind(SyntaxKind.DisabledTextTrivia))
                        {
                            // This is text in a '#if false' block; just ignore it
                            continue;
                        }

                        if (string.IsNullOrEmpty(indentation))
                        {
                            if (triviaToMove[m].IsKind(SyntaxKind.WhitespaceTrivia))
                            {
                                // Remove the trivia and analyze the current position again
                                triviaToMove.RemoveAt(m);
                                m--;
                                atStartOfLine = true;
                            }
                        }
                        else
                        {
                            triviaToMove.Insert(m, SyntaxFactory.Whitespace(indentation));
                            m++;
                        }
                    }

                    // strip any leading whitespace on each line (and also blank lines)
                    var k = 0;
                    var startOfLine = true;
                    while (k < newLeadingTrivia.Count)
                    {
                        switch (newLeadingTrivia[k].Kind())
                        {
                            case SyntaxKind.WhitespaceTrivia:
                                newLeadingTrivia.RemoveAt(k);
                                break;

                            case SyntaxKind.EndOfLineTrivia:
                                if (startOfLine)
                                {
                                    newLeadingTrivia.RemoveAt(k);
                                }
                                else
                                {
                                    startOfLine = true;
                                    k++;
                                }

                                break;

                            default:
                                startOfLine = newLeadingTrivia[k].IsDirective;
                                k++;
                                break;
                        }
                    }

                    for (var j = newLeadingTrivia.Count - 1; j >= 0; j--)
                    {
                        if (newLeadingTrivia[j].IsKind(SyntaxKind.EndOfLineTrivia))
                        {
                            newLeadingTrivia.Insert(j + 1, SyntaxFactory.Whitespace(indentation));
                        }
                    }

                    newLeadingTrivia.Insert(0, SyntaxFactory.Whitespace(indentation));

                    // preserve trailing trivia, adding an end of line if necessary.
                    var currentTrailingTrivia = currentUsing.GetTrailingTrivia();
                    var newTrailingTrivia = currentTrailingTrivia;
                    if (!currentTrailingTrivia.Any() || !currentTrailingTrivia.Last().IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        newTrailingTrivia = newTrailingTrivia.Add(SyntaxFactory.CarriageReturnLineFeed);
                    }

                    var processedUsing = (qualifyNames ? this.QualifyUsingDirective(currentUsing) : currentUsing)
                        .WithLeadingTrivia(newLeadingTrivia)
                        .WithTrailingTrivia(newTrailingTrivia)
                        .WithAdditionalAnnotations(s_usingPlacementCodeFixAnnotation);

                    result.Add(processedUsing);
                }

                result.Sort(this.CompareUsings);

                if (this._insertBlankLinesBetweenGroups)
                {
                    var last = result[result.Count - 1];

                    if (last.Alias == null &&
                        !last.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                    {
                        InsertBlankLinesBetweenSubGroups(result);
                    }

                    result[result.Count - 1] = last.WithTrailingTrivia(last.GetTrailingTrivia().Add(SyntaxFactory.CarriageReturnLineFeed));
                }

                return result;
            }

            private void InsertBlankLinesBetweenSubGroups(List<UsingDirectiveSyntax> usingsList)
            {
                var previousUsing = usingsList[0];
                var root = GetNamespaceRoot(previousUsing.Name);

                // We prime with the first using's Namespace root and the last using will always get a blank line.
                for (var i = 1; i < usingsList.Count - 1; i++)
                {
                    var currentUsing = usingsList[i];
                    var currentRoot = GetNamespaceRoot(currentUsing.Name);

                    if (root != currentRoot)
                    {
                        usingsList[i - 1] = previousUsing.WithTrailingTrivia(previousUsing.GetTrailingTrivia().Add(SyntaxFactory.CarriageReturnLineFeed));
                        root = currentRoot;
                    }

                    previousUsing = currentUsing;
                }

                return;

                string GetNamespaceRoot(NameSyntax name)
                {
                    // Since the Using statement has already been qualified we can get the
                    // root without getting the Symbol, which is good because this Node
                    // isn't part of the SyntaxTree yet.
                    return name.ToString().Split('.')[0];
                }
            }

            private UsingDirectiveSyntax QualifyUsingDirective(UsingDirectiveSyntax usingDirective)
            {
                NameSyntax originalName = usingDirective.Name;
                NameSyntax rewrittenName;
                switch (originalName.Kind())
                {
                    case SyntaxKind.QualifiedName:
                    case SyntaxKind.IdentifierName:
                    case SyntaxKind.GenericName:
                        if (originalName.Parent.IsKind(SyntaxKind.UsingDirective)
                            || originalName.Parent.IsKind(SyntaxKind.TypeArgumentList))
                        {
                            var symbol = this._semanticModel.GetSymbolInfo(originalName, cancellationToken: CancellationToken.None).Symbol;
                            if (symbol == null)
                            {
                                rewrittenName = originalName;
                                break;
                            }

                            if (symbol is INamespaceSymbol)
                            {
                                // TODO: Preserve inner trivia
                                string fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                NameSyntax replacement = SyntaxFactory.ParseName(fullName);
                                if (!originalName.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>().Any())
                                {
                                    replacement = replacement.ReplaceNodes(
                                        replacement.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>(),
                                        (originalNode2, rewrittenNode2) => rewrittenNode2.Name);
                                }

                                rewrittenName = replacement.WithTriviaFrom(originalName);
                                break;
                            }
                            else if (symbol is INamedTypeSymbol)
                            {
                                // TODO: Preserve inner trivia
                                // TODO: simplify after qualification
                                string fullName;
                                if (IsPredefinedType(((INamedTypeSymbol)symbol).OriginalDefinition.SpecialType))
                                {
                                    fullName = "global::System." + symbol.Name;
                                }
                                else
                                {
                                    fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                }

                                NameSyntax replacement = SyntaxFactory.ParseName(fullName);
                                if (!originalName.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>().Any())
                                {
                                    replacement = replacement.ReplaceNodes(
                                        replacement.DescendantNodesAndSelf().OfType<AliasQualifiedNameSyntax>(),
                                        (originalNode2, rewrittenNode2) => rewrittenNode2.Name);
                                }

                                rewrittenName = replacement.WithTriviaFrom(originalName);
                                break;
                            }
                            else
                            {
                                rewrittenName = originalName;
                                break;
                            }
                        }
                        else
                        {
                            rewrittenName = originalName;
                            break;
                        }

                    case SyntaxKind.AliasQualifiedName:
                    case SyntaxKind.PredefinedType:
                    default:
                        rewrittenName = originalName;
                        break;
                }

                if (rewrittenName == originalName)
                {
                    return usingDirective;
                }

                return usingDirective.ReplaceNode(originalName, rewrittenName);
            }

            private int CompareUsings(UsingDirectiveSyntax left, UsingDirectiveSyntax right)
            {
                if ((left.Alias != null) && (right.Alias != null))
                {
                    return _nameSyntaxComparer.Compare(left.Alias.Name, right.Alias.Name);
                }

                return _nameSyntaxComparer.Compare(left.Name, right.Name);
            }

            private bool IsSeparatedStaticSystemUsing(UsingDirectiveSyntax syntax)
            {
                if (!this._separateSystemDirectives)
                {
                    return false;
                }

                return this.StartsWithSystemUsingDirectiveIdentifier(syntax.Name);
            }

            private bool IsSeparatedSystemUsing(UsingDirectiveSyntax syntax)
            {
                if (!this._separateSystemDirectives
                    || HasNamespaceAliasQualifier(syntax))
                {
                    return false;
                }

                return this.StartsWithSystemUsingDirectiveIdentifier(syntax.Name);
            }

            private bool StartsWithSystemUsingDirectiveIdentifier(NameSyntax name)
            {
                if (!(this._semanticModel.GetSymbolInfo(name).Symbol is INamespaceOrTypeSymbol namespaceOrTypeSymbol))
                {
                    return false;
                }

                var namespaceTypeName = namespaceOrTypeSymbol.ToDisplayString(s_fullNamespaceDisplayFormat);
                var firstPart = namespaceTypeName.ToString().Split('.')[0];

                return string.Equals(SystemUsingDirectiveIdentifier, firstPart, StringComparison.Ordinal);
            }

            private void ProcessMembers(SyntaxList<MemberDeclarationSyntax> members)
            {
                foreach (var namespaceDeclaration in members.OfType<NamespaceDeclarationSyntax>())
                {
                    this.ProcessUsingDirectives(namespaceDeclaration.Usings);
                    this.ProcessMembers(namespaceDeclaration.Members);
                }
            }

            private void ProcessUsingDirectives(SyntaxList<UsingDirectiveSyntax> usingDirectives)
            {
                foreach (var usingDirective in usingDirectives)
                {
                    TreeTextSpan containingSpan = this.sourceMap.GetContainingSpan(usingDirective);

                    if (usingDirective.Alias != null)
                    {
                        this.AddUsingDirective(this._aliases, usingDirective, containingSpan);
                    }
                    else if (usingDirective.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                    {
                        if (this.IsSeparatedStaticSystemUsing(usingDirective))
                        {
                            this.AddUsingDirective(this._systemStaticImports, usingDirective, containingSpan);
                        }
                        else
                        {
                            this.AddUsingDirective(this._staticImports, usingDirective, containingSpan);
                        }
                    }
                    else if (this.IsSeparatedSystemUsing(usingDirective))
                    {
                        this.AddUsingDirective(this._systemUsings, usingDirective, containingSpan);
                    }
                    else
                    {
                        this.AddUsingDirective(this._namespaceUsings, usingDirective, containingSpan);
                    }
                }
            }

            private void AddUsingDirective(Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> container, UsingDirectiveSyntax usingDirective, TreeTextSpan containingSpan)
            {
                List<UsingDirectiveSyntax> usingList;

                if (!container.TryGetValue(containingSpan, out usingList))
                {
                    usingList = new List<UsingDirectiveSyntax>();
                    container.Add(containingSpan, usingList);
                }

                usingList.Add(usingDirective);
            }

            private List<UsingDirectiveSyntax> GenerateUsings(Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> usingsGroup, List<UsingDirectiveSyntax> usingsList, string indentation, List<SyntaxTrivia> triviaToMove, bool qualifyNames)
            {
                var filteredUsingsList = this.FilterRelevantUsings(usingsGroup, usingsList);

                return this.GenerateUsings(filteredUsingsList, indentation, triviaToMove, qualifyNames);
            }

            private List<UsingDirectiveSyntax> FilterRelevantUsings(Dictionary<TreeTextSpan, List<UsingDirectiveSyntax>> usingsGroup, List<UsingDirectiveSyntax> usingsList)
            {
                List<UsingDirectiveSyntax> groupList;

                if (!usingsGroup.TryGetValue(TreeTextSpan.Empty, out groupList))
                {
                    return s_emptyUsingsList;
                }

                return groupList.Where(u => usingsList.Contains(u)).ToList();
            }

            private static bool HasBuiltinEndLine(SyntaxTrivia trivia)
            {
                return trivia.IsDirective
                    || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.EndOfLineTrivia);
            }

            private static bool HasNamespaceAliasQualifier(UsingDirectiveSyntax usingDirective) 
                => usingDirective.DescendantNodes().Any(node => node.IsKind(SyntaxKind.AliasQualifiedName));

            private static bool IsMissingOrDefault(SyntaxToken token)
            {
                return token.IsKind(SyntaxKind.None)
                    || token.IsMissing;
            }

            private bool IsPredefinedType(SpecialType specialType)
            {
                return specialType == SpecialType.System_Boolean
                    || specialType == SpecialType.System_Byte
                    || specialType == SpecialType.System_Char
                    || specialType == SpecialType.System_Decimal
                    || specialType == SpecialType.System_Double
                    || specialType == SpecialType.System_Int16
                    || specialType == SpecialType.System_Int32
                    || specialType == SpecialType.System_Int64
                    || specialType == SpecialType.System_Object
                    || specialType == SpecialType.System_SByte
                    || specialType == SpecialType.System_Single
                    || specialType == SpecialType.System_String
                    || specialType == SpecialType.System_UInt16
                    || specialType == SpecialType.System_UInt32
                    || specialType == SpecialType.System_UInt64;
            }
        }
    }
}

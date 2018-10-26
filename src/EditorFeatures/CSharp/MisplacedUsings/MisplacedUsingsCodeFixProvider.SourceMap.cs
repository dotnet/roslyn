// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    /// <summary>
    /// Implements a code fix for all misplaced using statements.
    /// </summary>
    internal partial class MisplacedUsingsCodeFixProvider
    {
        /// <summary>
        /// Contains a map of the different regions of a source file.
        /// </summary>
        /// <remarks>
        /// <para>Used source file regions are:</para>
        ///
        /// <list type="bullet">
        /// <item><description>conditional directives (#if, #else, #elif, #endif)</description></item>
        /// <item><description>pragma warning directives</description></item>
        /// <item><description>region directives</description></item>
        /// </list>
        /// </remarks>
        private class SourceMap
        {
            private readonly TreeTextSpan _regionRoot;
            private readonly TreeTextSpan _pragmaWarningRoot;

            private SourceMap(TreeTextSpan conditionalRoot, TreeTextSpan regionRoot, TreeTextSpan pragmaWarningRoot)
            {
                ConditionalRoot = conditionalRoot;
                _regionRoot = regionRoot;
                _pragmaWarningRoot = pragmaWarningRoot;
            }

            /// <summary>
            /// Gets the root entry for all conditional directive spans.
            /// </summary>
            /// <value>A <see cref="TreeTextSpan"/> object representing the root conditional directive span.</value>
            internal TreeTextSpan ConditionalRoot { get; }

            /// <summary>
            /// Constructs the directive map for the given <paramref name="compilationUnit"/>.
            /// </summary>
            /// <param name="compilationUnit">The compilation unit to scan for directive trivia.</param>
            /// <returns>A new <see cref="SourceMap"/> object containing the directive trivia information from the passed <paramref name="compilationUnit"/>.</returns>
            internal static SourceMap FromCompilationUnit(CompilationUnitSyntax compilationUnit)
            {
                TreeTextSpan conditionalRoot;
                TreeTextSpan regionRoot;
                TreeTextSpan pragmaWarningRoot;

                BuildDirectiveTriviaMaps(compilationUnit, out conditionalRoot, out regionRoot, out pragmaWarningRoot);

                return new SourceMap(conditionalRoot, regionRoot, pragmaWarningRoot);
            }

            /// <summary>
            /// Gets the containing span for the given <paramref name="node"/>.
            /// </summary>
            /// <param name="node">The node for which the containing span will be determined.</param>
            /// <returns>The span that contains the node.</returns>
            internal TreeTextSpan GetContainingSpan(SyntaxNode node)
            {
                var textSpan = node.GetLocation().SourceSpan;

                var containingSpans = _pragmaWarningRoot.Children
                    .Where(child => (textSpan.Start >= child.Start) && (textSpan.End <= child.End))
                    .ToList();

                var containingConditionalSpan = ConditionalRoot.GetContainingSpan(textSpan);
                if (containingConditionalSpan != ConditionalRoot)
                {
                    containingSpans.Add(containingConditionalSpan);
                }

                var containingRegionSpan = _regionRoot.GetContainingSpan(textSpan);
                if (containingRegionSpan != _regionRoot)
                {
                    containingSpans.Add(containingRegionSpan);
                }

                if (containingSpans.Count == 0)
                {
                    return TreeTextSpan.Empty;
                }

                for (var i = containingSpans.Count - 1; i > 0; i--)
                {
                    if (containingSpans[i].Contains(containingSpans[i - 1]))
                    {
                        containingSpans.RemoveAt(i);
                    }
                    else if (containingSpans[i - 1].Contains(containingSpans[i]))
                    {
                        containingSpans.RemoveAt(i - 1);
                    }
                }

                if (containingSpans.Count == 1)
                {
                    return containingSpans[0];
                }

                var newStart = int.MinValue;
                var newEnd = int.MaxValue;

                foreach (var span in containingSpans)
                {
                    newStart = Math.Max(newStart, span.Start);
                    newEnd = Math.Min(newEnd, span.End);
                }

                return new TreeTextSpan(newStart, newEnd, ImmutableArray<TreeTextSpan>.Empty);
            }

            private static void ProcessNodeMembers(TreeTextSpan.Builder builder, SyntaxList<MemberDeclarationSyntax> members)
            {
                foreach (var namespaceDeclaration in members.OfType<NamespaceDeclarationSyntax>())
                {
                    var childBuilder = builder.AddChild(namespaceDeclaration.FullSpan.Start);
                    childBuilder.SetEnd(namespaceDeclaration.FullSpan.End);

                    ProcessNodeMembers(childBuilder, namespaceDeclaration.Members);
                }
            }

            private static void BuildDirectiveTriviaMaps(CompilationUnitSyntax compilationUnit, out TreeTextSpan conditionalRoot, out TreeTextSpan regionRoot, out TreeTextSpan pragmaWarningRoot)
            {
                var conditionalStack = new Stack<TreeTextSpan.Builder>();
                var regionStack = new Stack<TreeTextSpan.Builder>();
                var pragmaWarningList = new List<DirectiveTriviaSyntax>();

                var conditionalBuilder = SetupBuilder(compilationUnit, conditionalStack);
                var regionBuilder = SetupBuilder(compilationUnit, regionStack);

                for (var directiveTrivia = compilationUnit.GetFirstDirective(); directiveTrivia != null; directiveTrivia = directiveTrivia.GetNextDirective())
                {
                    switch (directiveTrivia.Kind())
                    {
                        case SyntaxKind.IfDirectiveTrivia:
                            AddNewDirectiveTriviaSpan(conditionalBuilder, conditionalStack, directiveTrivia);
                            break;

                        case SyntaxKind.ElifDirectiveTrivia:
                        case SyntaxKind.ElseDirectiveTrivia:
                            var previousSpan = conditionalStack.Pop();
                            previousSpan.SetEnd(directiveTrivia.FullSpan.Start);

                            AddNewDirectiveTriviaSpan(conditionalBuilder, conditionalStack, directiveTrivia);
                            break;

                        case SyntaxKind.EndIfDirectiveTrivia:
                            CloseDirectiveTriviaSpan(conditionalBuilder, conditionalStack, directiveTrivia);
                            break;

                        case SyntaxKind.RegionDirectiveTrivia:
                            AddNewDirectiveTriviaSpan(regionBuilder, regionStack, directiveTrivia);
                            break;

                        case SyntaxKind.EndRegionDirectiveTrivia:
                            CloseDirectiveTriviaSpan(regionBuilder, regionStack, directiveTrivia);
                            break;

                        case SyntaxKind.PragmaWarningDirectiveTrivia:
                            pragmaWarningList.Add(directiveTrivia);
                            break;

                        default:
                            // ignore all other directive trivia
                            break;
                    }
                }

                conditionalRoot = FinalizeBuilder(conditionalBuilder, conditionalStack, compilationUnit.Span.End);
                regionRoot = FinalizeBuilder(regionBuilder, regionStack, compilationUnit.Span.End);
                pragmaWarningRoot = BuildPragmaWarningSpans(pragmaWarningList, compilationUnit);
            }

            private static TreeTextSpan.Builder SetupBuilder(CompilationUnitSyntax compilationUnit, Stack<TreeTextSpan.Builder> stack)
            {
                var rootBuilder = TreeTextSpan.CreateBuilder(compilationUnit.SpanStart);
                stack.Push(rootBuilder);

                return rootBuilder;
            }

            private static void AddNewDirectiveTriviaSpan(TreeTextSpan.Builder spanBuilder, Stack<TreeTextSpan.Builder> spanStack, DirectiveTriviaSyntax directiveTrivia)
            {
                var parent = spanStack.Peek();
                var newDirectiveSpan = parent.AddChild(directiveTrivia.FullSpan.Start);
                spanStack.Push(newDirectiveSpan);
            }

            private static void CloseDirectiveTriviaSpan(TreeTextSpan.Builder spanBuilder, Stack<TreeTextSpan.Builder> spanStack, DirectiveTriviaSyntax directiveTrivia)
            {
                var previousSpan = spanStack.Pop();
                previousSpan.SetEnd(directiveTrivia.FullSpan.End);
            }

            private static TreeTextSpan FinalizeBuilder(TreeTextSpan.Builder builder, Stack<TreeTextSpan.Builder> stack, int end)
            {
                // close all spans (including the root) that have not been closed yet
                while (stack.Count > 0)
                {
                    var span = stack.Pop();
                    span.SetEnd(end);
                }

                // Fill the gaps to make sure that directives on either side of an conditional directive group are not combined
                builder.FillGaps();

                return builder.ToSpan();
            }

            private static TreeTextSpan BuildPragmaWarningSpans(List<DirectiveTriviaSyntax> pragmaWarningList, CompilationUnitSyntax compilationUnit)
            {
                var map = new Dictionary<string, PragmaWarningDirectiveTriviaSyntax>();
                var builder = TreeTextSpan.CreateBuilder(compilationUnit.SpanStart);

                foreach (var pragmaWarning in pragmaWarningList.Cast<PragmaWarningDirectiveTriviaSyntax>())
                {
                    var errorCodes = GetErrorCodes(pragmaWarning);

                    switch (pragmaWarning.DisableOrRestoreKeyword.Kind())
                    {
                        case SyntaxKind.DisableKeyword:
                            foreach (var errorCode in errorCodes)
                            {
                                if (!map.ContainsKey(errorCode))
                                {
                                    // only add it if the warning isn't disabled already
                                    map[errorCode] = pragmaWarning;
                                }
                            }

                            break;

                        case SyntaxKind.RestoreKeyword:
                            foreach (var errorCode in errorCodes)
                            {
                                PragmaWarningDirectiveTriviaSyntax startOfSpan;

                                if (map.TryGetValue(errorCode, out startOfSpan))
                                {
                                    map.Remove(errorCode);

                                    var childSpan = builder.AddChild(startOfSpan.FullSpan.Start);
                                    childSpan.SetEnd(pragmaWarning.FullSpan.End);
                                }
                            }

                            break;
                    }
                }

                // create spans for all pragma warning disable statements that have not been closed.
                foreach (var pragmaWarning in map.Values)
                {
                    var childSpan = builder.AddChild(pragmaWarning.FullSpan.Start);
                    childSpan.SetEnd(compilationUnit.FullSpan.End);
                }

                builder.SetEnd(compilationUnit.FullSpan.End);
                return builder.ToSpan();
            }

            private static List<string> GetErrorCodes(PragmaWarningDirectiveTriviaSyntax pragmaWarningDirectiveTrivia)
            {
                return pragmaWarningDirectiveTrivia.ErrorCodes
                    .OfType<IdentifierNameSyntax>()
                    .Select(x => x.Identifier.ValueText)
                    .ToList();
            }
        }
    }
}

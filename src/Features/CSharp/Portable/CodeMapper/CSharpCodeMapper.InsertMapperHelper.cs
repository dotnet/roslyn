// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal sealed partial class CSharpCodeMapper
{
    /// <summary>
    /// This C# mapper helper focuses on Code Insertions. Specifically inserting code that
    /// doesn't currently exists in the target document.
    /// </summary>
    private class InsertionHelper : IMappingHelper
    {
        public bool TryGetValidInsertions(SyntaxNode target, ImmutableArray<CSharpSourceNode> sourceNodes, out CSharpSourceNode[] validInsertions, out InvalidInsertion[] invalidInsertions)
        {
            var validNodes = new List<CSharpSourceNode>();
            var invalidNodes = new List<InvalidInsertion>();
            validInsertions = Array.Empty<CSharpSourceNode>();
            invalidInsertions = Array.Empty<InvalidInsertion>();
            foreach (var sn in sourceNodes)
            {
                // For insertions we want the nodes that don't already exist on the target.
                if (!sn.ExistsOnTarget(target, out _))
                {
                    validNodes.Add(sn);
                }
                else
                {
                    invalidNodes.Add(new InvalidInsertion(sn, InvalidInsertionReason.InsertIdentifierAlreadyExistsOnTarget));
                }
            }

            // As long as we can find a Valid node to insert, we will return true.
            if (validNodes.Any())
            {
                validInsertions = validNodes.ToArray();
                return true;
            }

            invalidInsertions = invalidNodes.ToArray();
            return false;
        }

        /// <summary>
        /// We want to get the Insertion Position.
        /// When dealing with a Method or LocalFunction (usually a method without a class)
        /// We want to insert those at the bottom of the class for now.
        /// </summary>
        /// <param name="documentSyntax">The target document syntax where the snippet will be inserted.</param>
        /// <param name="insertion">The snippet to insert.</param>
        /// <param name="target"></param>
        /// <param name="adjustedInsertion"></param>
        /// <returns></returns>
        /// 

        public TextSpan? GetInsertSpan(SyntaxNode documentSyntax, CSharpSourceNode insertion, MappingTarget target, out SyntaxNode? adjustedNodeToMap)
        {
            adjustedNodeToMap = null;
            int insertionPoint;
            if (insertion.Scope is not Scope.None)
            {
                if (TryGetScopedInsertionPoint(documentSyntax, insertion.Scope, out insertionPoint))
                {
                    return new TextSpan(insertionPoint, 0);
                }
            }

            // If there's an specific focus area, or caret provided, we should try to insert as close as possible.
            // As long as the focused area is not empty.
            if (TryGetFocusedInsertionPoint(target.FocusArea.SourceSpan, documentSyntax, insertion, out insertionPoint))
            {
                return new TextSpan(insertionPoint, 0);
            }

            // Fallback: Attempt to infer the insertion point without a caret or line.
            // This will attempt to get a default insertion point for the insert node within the
            // current document.
            if (TryGetDefaultInsertionPoint(documentSyntax, out insertionPoint))
            {
                return new TextSpan(insertionPoint, 0);
            }

            return null;
        }

        /// <summary>
        /// Tries to get the insertion point for the default case.
        /// There's many heuristics here that are applied like:
        /// If what is intended to be inserted is a method, or a class, then we find the right
        /// place in the document for it. If currently the document is classless, we also take that
        /// into consideration.
        /// </summary>
        /// <param name="node">The node to get insertion point for.</param>
        /// <param name="insertionPoint">The computed insertion point.</param>
        /// <returns><c>true</c> if an insertion point was successfully computed,
        /// <c>false</c> otherwise.</returns>
        private static bool TryGetDefaultInsertionPoint(SyntaxNode node, out int insertionPoint)
        {
            var firstClass = node.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();
            if (firstClass is null)
            {
                // Get classless insertion.
                return TryGetClasslessInsertionPoint(node, out insertionPoint);
            }

            // If last property is not null, get the span end as insertion point.
            var lastProperty = firstClass.DescendantNodes()
                .OfType<MemberDeclarationSyntax>()
                .Where(CSharpSourceNode.IsSimpleNode)
                .LastOrDefault();
            if (lastProperty is not null)
            {
                insertionPoint = lastProperty.FullSpan.End;
                return true;
            }

            // If there's no last property, try and find the first constructor and place it above.
            var firstConstructor = firstClass.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            if (firstConstructor is not null)
            {
                insertionPoint = firstConstructor.FullSpan.Start;
                return true;
            }

            // Otherwise
            // Just insert after the open bracket.
            insertionPoint = firstClass.OpenBraceToken.FullSpan.End;
            return true;
        }

        private static bool TryGetScopedInsertionPoint(SyntaxNode node, Scope scope, out int insertionPoint)
        {
            insertionPoint = 0;
            return scope switch
            {
                Scope.Method => TryGetMethodInsertionPoint(node, out insertionPoint),
                Scope.Class => TryGetClassInsertionPoint(node, out insertionPoint),
                _ => false,
            };
        }

        private static bool TryGetMethodInsertionPoint(SyntaxNode node, out int insertionPoint)
        {
            // In essence, we want to be able to Map to the class where this method belongs to.
            // However there can be more than one class, and if that happens it's going to
            // be hard for us to determine which class to insert this into.
            // So for now we will take the first class.
            var insertionClass = node.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();
            if (insertionClass is not null)
            {
                var lastMember = insertionClass.Members.OfType<MethodDeclarationSyntax>().LastOrDefault();

                if (lastMember is null)
                {
                    insertionPoint = insertionClass.CloseBraceToken.FullSpan.Start;
                    return true;
                }
                else
                {
                    insertionPoint = lastMember.FullSpan.End;
                    return true;
                }
            }
            else
            {
                return TryGetClasslessInsertionPoint(node, out insertionPoint);
            }
        }

        private static bool TryGetClassInsertionPoint(SyntaxNode node, out int insertionPoint)
        {
            insertionPoint = 0;
            var lastClass = node.DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .LastOrDefault();
            if (lastClass is not null)
            {
                insertionPoint = lastClass.CloseBraceToken.FullSpan.End;
                return true;
            }
            else
            {
                // If there's no class, look for namespace.
                var namespaceDec = node.DescendantNodesAndSelf()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();
                if (namespaceDec is not null)
                {
                    insertionPoint = namespaceDec.OpenBraceToken.FullSpan.End;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetClasslessInsertionPoint(SyntaxNode node, out int insertionPoint)
        {
            // If it's a C# classless file.
            insertionPoint = 0;
            if (node.IsKind(SyntaxKind.CompilationUnit))
            {
                var lastStatement = node
                    .DescendantNodes()
                    .OfType<StatementSyntax>()
                    .LastOrDefault();
                if (lastStatement is not null)
                {
                    insertionPoint = lastStatement.FullSpan.End;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetFocusedInsertionPoint(TextSpan? target, SyntaxNode documentSyntax, CSharpSourceNode insertion, out int insertionPoint)
        {
            // If there's an specific focus area, or caret provided, we should try to insert as close as possible.
            // As long as the focused area is not empty.
            insertionPoint = 0;
            if (target is null || target.Equals(documentSyntax.FullSpan))
            {
                return false;
            }

            var text = documentSyntax.SyntaxTree.GetText();
                var temptativeLine = text.Lines.IndexOf(target.Value.Start);
                if (temptativeLine >= 0)
                {
                    var adjustedLine = AdjustInsertionLineNumber(insertion, temptativeLine, documentSyntax);
                    insertionPoint = documentSyntax.SyntaxTree.GetText().Lines[adjustedLine].Span.Start;
                    return true;
                }

            return false;
        }

        /// <summary>
        /// Adjusts the insertion line number based on the target and source node.
        /// This will make sure that an insertion point is not disrupting the current flow of the code.
        /// Example: If original insertion point is within the position of a methods signature, this will adjust it
        /// so that the new insertion points inside the method's scope.
        /// </summary>
        /// <param name="sourceNode">The source code node.</param>
        /// <param name="lineNumber">The line number to adjust.</param>
        /// <param name="target">The target syntax node.</param>
        /// <returns>The adjusted line number.</returns>
        private static int AdjustInsertionLineNumber(CSharpSourceNode sourceNode, int lineNumber, SyntaxNode target)
        {
            var adjustedLineNumber = lineNumber;
            if (sourceNode.Scope == Scope.Method || sourceNode.Scope == Scope.Class)
            {
                // override line with new closest valid position.
                adjustedLineNumber = FindClosestScopedInsertionLine(target, sourceNode.Scope, lineNumber);
            }
            else
            {
                if (target.GetText().Lines.Count > lineNumber)
                {
                    // Get the current body from target, where the line number is located
                    // we do this by using roslyn, and getting the current node that is located within the given line
                    // example: If we have a method body that goes from line 1 to 10, and we want to insert at line 5
                    // this will return the method body.
                    var containingScopes = target.DescendantNodesAndSelf()
                        .Where(node =>
                        {
                            var lineSpan = node.GetLocation().GetLineSpan();
                            return lineSpan.StartLinePosition.Line <= lineNumber && lineSpan.EndLinePosition.Line >= lineNumber;
                        })
                        .Select<SyntaxNode, (SyntaxNode node, Scope scope)?>(node => CSharpSourceNode.IsScopedNode(node, out var scopeType) ? (node, scopeType) : null)
                        .Where(pair => pair is not null)
                        .OfType<(SyntaxNode node, Scope scope)>()
                        .OrderBy(pair => pair.scope);

                    if (containingScopes.Any())
                    {
                        // Get the first containing scope
                        // since these are ordered by importance (Class goes higher than Method, for example)
                        // getting the first containing scope should be enough to have an insight on where are we currently located.
                        var containingScope = containingScopes.First().node;
                        var validLineSpan = target.SyntaxTree.GetLineSpan(containingScope.FullSpan);
                        var validLineStart = validLineSpan.StartLinePosition.Line;

                        // Get the brace tokens from the current scope.
                        var braceTokens = CSharpSourceNode.GetOpenCloseBraceTokens(containingScope);

                        // If brace tokens are found, use then to get the minimum value you need to be able to
                        // insert something inside the brace tokens.
                        if (braceTokens is not null)
                        {
                            var openBraceLine = braceTokens.Open.GetLocation().GetLineSpan().StartLinePosition.Line;
                            // readjust the valid start line.
                            validLineStart = openBraceLine + 1;
                        }

                        // make sure we don't exceed the valid start and end positions.
                        if (lineNumber >= validLineSpan.EndLinePosition.Line)
                        {
                            adjustedLineNumber = validLineSpan.EndLinePosition.Line;
                        }

                        if (lineNumber <= validLineStart)
                        {
                            adjustedLineNumber = validLineStart;
                        }
                    }
                    else
                    {
                        // It's safe to insert in the next line.
                        adjustedLineNumber++;
                    }
                }
            }

            return adjustedLineNumber;
        }

        /// <summary>
        /// Finds the closest line in within a scope of a specified type to the given line number.
        /// This will bind the insertion point to happen inside the currently selected scoped member.
        /// If closest member is a class, this will readjust the line to be inside the class.
        /// If closest member is a method, this will readjust the line to be inside the method.
        /// </summary>
        /// <param name="target">The target node to search for scoped nodes within.</param>
        /// <param name="scopeType">The type of scope to search for.</param>
        /// <param name="line">The line number to search for the closest scoped line.</param>
        /// <returns>The line number of the closest scoped node to the given line number.</returns>
        private static int FindClosestScopedInsertionLine(SyntaxNode target, Scope scopeType, int line)
        {
            var closestLine = int.MaxValue;
            var temptativeLines = target.DescendantNodesAndSelf()
                .OfType<MemberDeclarationSyntax>()
                .Where(member => CSharpSourceNode.IsScopedNode(member, out var scope) && scope == scopeType)
                .Select(member => member.GetLocation().GetLineSpan())
                .SelectMany(span => new[] { span.StartLinePosition.Line, span.EndLinePosition.Line + 1 });

            foreach (var memberLine in temptativeLines)
            {
                var distance = Math.Abs(line - memberLine);
                if (distance < Math.Abs(line - closestLine))
                {
                    closestLine = memberLine;
                }
            }

            return closestLine == int.MaxValue ? line : closestLine;
        }
    }
}

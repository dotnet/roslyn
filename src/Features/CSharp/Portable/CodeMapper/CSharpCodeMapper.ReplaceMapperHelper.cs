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
    /// This C# mapper helper focuses on Code Replacements. Specifically replacing code that
    /// currently exists in the target document.
    /// </summary>
    private class ReplaceHelper : IMappingHelper
    {
        public TextSpan? GetInsertSpan(SyntaxNode documentSyntax, CSharpSourceNode insertion, MappingTarget? target, out string? adjustedInsertion)
        {
            adjustedInsertion = null;
            if (!insertion.ExistsOnTarget(documentSyntax, out var matchingNode))
            {
                return null;
            }

            if (matchingNode is null)
            {
                throw new Exception($"Could not find a match for `{insertion}` in the current document.");
            }

            // Get rid of all leading whitespace and line breaks that only add noise.
            var (adjustedMatchingNode, textSpan) = RemoveLeadingWhitespaceTrivia(matchingNode);

            var insertionAttributes = TryGetNodeAttributes(insertion.Node);
            var nodeToReplaceAttributes = TryGetNodeAttributes(matchingNode);
            var adjustedNode = insertion.Node;
            if (HasAnyCommentTrivia(adjustedNode))
            {
                // If the insertion has docs, we should insert the docs as well.
                // In the situation where the suggested code doesn't include the attributes
                // we should include them if the node to replace does have atributes.
                if (insertionAttributes.Length == 0 && nodeToReplaceAttributes.Length > 0)
                {
                    adjustedNode = TryAddAttributesAfterLeadingTrivia(adjustedNode, nodeToReplaceAttributes);
                }
            }
            else
            {
                // When the node to replace has leading trivia, but the replace node doesn't,
                // we must exclude this leading trivia from the replace span.
                // This scenario should only cover situations where there are no attributes present.
                if (HasAnyCommentTrivia(adjustedMatchingNode))
                {
                    (adjustedMatchingNode, textSpan) = RemoveLeadingCommentsTrivia(adjustedMatchingNode, textSpan);
                    // After removing the docs, comments or XML, there still might be some indentation and whitespace left floating
                    // for the spaces where these docs or comments where located. We have to trim those too.
                    (adjustedMatchingNode, textSpan) = RemoveLeadingWhitespaceTrivia(adjustedMatchingNode, textSpan);
                    nodeToReplaceAttributes = TryGetNodeAttributes(adjustedMatchingNode);
                }

                if (ShouldSkipAttributes(insertionAttributes, nodeToReplaceAttributes))
                {
                    // In the scenarios where:
                    // 1) insertion doesn't have any attributes but the node to replace does have attributes.
                    // 2) both insertion and replace have the same attributes
                    // we don't want to pay attention to the attributes.
                    // since that's just going to make things more complex when there's no need to.
                    // for these situations we will remove the attributes from both.
                    if (nodeToReplaceAttributes.Any() && TryRemoveAttributes(adjustedMatchingNode) is SyntaxNode nodeWithoutAttributes)
                    {
                        var spanWithoutAttributes = nodeWithoutAttributes.FullSpan;
                        var skipDiff = textSpan.Length - spanWithoutAttributes.Length;
                        textSpan = new TextSpan(textSpan.Start + skipDiff, spanWithoutAttributes.Length);
                    }

                    if (insertionAttributes.Any())
                    {
                        adjustedNode = TryRemoveAttributes(adjustedNode);
                        if (insertion == null)
                        {
                            throw new Exception("Could not remove attributes from insertion code.");
                        }
                    }
                }
            }

            // If node was adjusted, set the adjustedInsertion to the new adjusted text.
            if (adjustedNode is not null && adjustedNode != insertion.Node)
            {
                adjustedInsertion = adjustedNode.ToFullString();
            }

            return textSpan;
        }

        private static bool HasAnyCommentTrivia(SyntaxNode node)
        {
            if (!node.HasLeadingTrivia)
            {
                return false;
            }

            return node.GetLeadingTrivia().Any(IsCommentOrXmlDocTrivia);
        }

        private static (SyntaxNode, TextSpan) RemoveLeadingWhitespaceTrivia(SyntaxNode matchingNode, TextSpan? textSpan = null)
        {
            // When a textSpan is not provided, we assume that the node is the original node.
            if (textSpan is null)
            {
                textSpan = matchingNode.FullSpan;
            }

            var originalLength = textSpan.Value.Length;
            var leadingTrivia = matchingNode.GetLeadingTrivia();
            var removeLeadingTrivia = new List<SyntaxTrivia>();
            foreach (var trivia in leadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    removeLeadingTrivia.Add(trivia);
                }
                else
                {
                    break;
                }
            }

            // remove removeLeadingTrivia from matching node
            var adjustedMatchingNode = matchingNode
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(leadingTrivia.Except(removeLeadingTrivia));
            var newLength = adjustedMatchingNode.FullSpan.Length;
            var diff = originalLength - newLength;
            textSpan = new TextSpan(textSpan.Value.Start + diff, newLength);
            return (adjustedMatchingNode, textSpan.Value);
        }

        private static (SyntaxNode, TextSpan) RemoveLeadingCommentsTrivia(SyntaxNode matchingNode, TextSpan? textSpan = null)
        {
            // When a textSpan is not provided, we assume that the node is the original node.
            if (textSpan is null)
            {
                textSpan = matchingNode.FullSpan;
            }

            var originalLength = textSpan.Value.Length;
            var leadingTrivia = matchingNode.GetLeadingTrivia();
            var removeLeadingTrivia = new List<SyntaxTrivia>();
            foreach (var trivia in leadingTrivia)
            {
                if (IsCommentOrXmlDocTrivia(trivia))
                {
                    removeLeadingTrivia.Add(trivia);
                }
            }

            // remove removeLeadingTrivia from matching node
            matchingNode = matchingNode
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(leadingTrivia.Except(removeLeadingTrivia));
            var newLength = matchingNode.FullSpan.Length;
            var diff = originalLength - newLength;
            textSpan = new TextSpan(textSpan.Value.Start + diff, newLength);
            return (matchingNode, textSpan.Value);
        }

        private static bool IsCommentOrXmlDocTrivia(SyntaxTrivia t)
        {
            return t.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
                    || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                    || t.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia)
                    || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                    || t.IsKind(SyntaxKind.SingleLineCommentTrivia);
        }
        

        public bool TryGetValidInsertions(SyntaxNode target, ImmutableArray<CSharpSourceNode> sourceNodes, out CSharpSourceNode[] validInsertions, out InvalidInsertion[] invalidInsertions)
        {
            var validNodes = new List<CSharpSourceNode>();
            var invalidNodes = new List<InvalidInsertion>();
            foreach (var sn in sourceNodes)
            {
                // For Replace we will validate those nodes that already exists
                // in the given target.
                if (sn.ExistsOnTarget(target, out var matchingNode))
                {
                    validNodes.Add(sn);
                }
                else
                {
                    invalidNodes.Add(new InvalidInsertion(sn, InvalidInsertionReason.ReplaceIdentifierMissingOnTarget));
                }
            }

            validInsertions = validNodes.ToArray();
            invalidInsertions = invalidNodes.ToArray();
            return validNodes.Any();
        }

        private static bool ShouldSkipAttributes(AttributeSyntax[] insertionAttributes, AttributeSyntax[] targetAttributes)
        {
            if (insertionAttributes.Length == 0)
            {
                return true;
            }

            if (insertionAttributes.Length == targetAttributes.Length)
            {
                foreach (var insertionAttribute in insertionAttributes)
                {
                    // If we find at least one attribute that doesn't exists on target, we'll take it as we need to
                    // replace those attributes.
                    if (!targetAttributes.Any(a => a.ToString() == insertionAttribute.ToString()))
                    {
                        return false;
                    }
                }

                // If all insertion attributes are also located in target attributes, we can skip the attributes.
                return true;
            }

            return false;
        }

        private static AttributeSyntax[] TryGetNodeAttributes(SyntaxNode node)
        {
            var insertionAttributes = Array.Empty<AttributeSyntax>();
            if (node is LocalFunctionStatementSyntax lfs)
            {
                insertionAttributes = lfs.AttributeLists.SelectMany(a => a.Attributes).ToArray();
            }
            else if (node is MethodDeclarationSyntax mds)
            {
                insertionAttributes = mds.AttributeLists.SelectMany(a => a.Attributes).ToArray();
            }
            else if (node is ClassDeclarationSyntax cds)
            {
                insertionAttributes = cds.AttributeLists.SelectMany(a => a.Attributes).ToArray();
            }

            return insertionAttributes;
        }

        private static SyntaxNode? TryRemoveAttributes(SyntaxNode node)
        {
            if (node is LocalFunctionStatementSyntax lfs)
            {
                return lfs.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            }
            else if (node is MethodDeclarationSyntax mds)
            {
                return mds.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            }
            else if (node is ClassDeclarationSyntax cds)
            {
                return cds.WithAttributeLists(SyntaxFactory.List<AttributeListSyntax>());
            }

            return null;
        }

        private static SyntaxNode? TryAddAttributesAfterLeadingTrivia(SyntaxNode node, IEnumerable<AttributeSyntax> attributes)
        {
            var attributeList = new List<AttributeListSyntax>();
            foreach (var attribute in attributes)
            {
                attributeList.Add(SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(
                    new[] { attribute })));
            }

            var listSyntax = SyntaxFactory.List(
                attributeList.Select(attr => attr.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)).ToArray());
            var trivia = node.GetLeadingTrivia();
            var nodeWithoutTrivia = node.WithoutLeadingTrivia();
            if (nodeWithoutTrivia is LocalFunctionStatementSyntax lfs)
            {
                return lfs.WithAttributeLists(listSyntax).WithLeadingTrivia(trivia);
            }
            else if (nodeWithoutTrivia is MethodDeclarationSyntax mds)
            {
                return mds.WithAttributeLists(listSyntax).WithLeadingTrivia(trivia);
            }
            else if (nodeWithoutTrivia is ClassDeclarationSyntax cds)
            {
                return cds.WithAttributeLists(listSyntax).WithLeadingTrivia(trivia);
            }

            return null;
        }
    }
}

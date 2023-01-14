// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class CSharpSyntaxFactsServiceFactory
    {
        private sealed class CSharpSyntaxFactsService : CSharpSyntaxFacts, ISyntaxFactsService
        {
            internal static new readonly CSharpSyntaxFactsService Instance = new();

            public bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
            {
                if (syntaxTree == null)
                {
                    return false;
                }

                return syntaxTree.IsInNonUserCode(position, cancellationToken);
            }

            private static readonly SyntaxAnnotation s_annotation = new();

            public void AddFirstMissingCloseBrace<TContextNode>(
                SyntaxNode root, TContextNode contextNode,
                out SyntaxNode newRoot, out TContextNode newContextNode) where TContextNode : SyntaxNode
            {
                newRoot = new AddFirstMissingCloseBraceRewriter(contextNode).Visit(root);
                newContextNode = (TContextNode)newRoot.GetAnnotatedNodes(s_annotation).Single();
            }

            private class AddFirstMissingCloseBraceRewriter : CSharpSyntaxRewriter
            {
                private readonly SyntaxNode _contextNode;
                private bool _seenContextNode = false;
                private bool _addedFirstCloseCurly = false;

                public AddFirstMissingCloseBraceRewriter(SyntaxNode contextNode)
                    => _contextNode = contextNode;

                public override SyntaxNode Visit(SyntaxNode node)
                {
                    if (node == _contextNode)
                    {
                        _seenContextNode = true;

                        // Annotate the context node so we can find it again in the new tree
                        // after we've added the close curly.
                        return node.WithAdditionalAnnotations(s_annotation);
                    }

                    // rewrite this node normally.
                    var rewritten = base.Visit(node);
                    if (rewritten == node)
                    {
                        return rewritten;
                    }

                    // This node changed.  That means that something underneath us got
                    // rewritten.  (i.e. we added the annotation to the context node).
                    Debug.Assert(_seenContextNode);

                    // Ok, we're past the context node now.  See if this is a node with 
                    // curlies.  If so, if it has a missing close curly then add in the 
                    // missing curly.  Also, even if it doesn't have missing curlies, 
                    // then still ask to format its close curly to make sure all the 
                    // curlies up the stack are properly formatted.
                    var braces = rewritten.GetBraces();
                    if (braces.openBrace.Kind() == SyntaxKind.None &&
                        braces.closeBrace.Kind() == SyntaxKind.None)
                    {
                        // Not an item with braces.  Just pass it up.
                        return rewritten;
                    }

                    // See if the close brace is missing.  If it's the first missing one 
                    // we're seeing then definitely add it.
                    if (braces.closeBrace.IsMissing)
                    {
                        if (!_addedFirstCloseCurly)
                        {
                            var closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                                .WithAdditionalAnnotations(Formatter.Annotation);
                            rewritten = rewritten.ReplaceToken(braces.closeBrace, closeBrace);
                            _addedFirstCloseCurly = true;
                        }
                    }
                    else
                    {
                        // Ask for the close brace to be formatted so that all the braces
                        // up the spine are in the right location.
                        rewritten = rewritten.ReplaceToken(braces.closeBrace,
                            braces.closeBrace.WithAdditionalAnnotations(Formatter.Annotation));
                    }

                    return rewritten;
                }
            }

            public Task<ImmutableArray<SyntaxNode>> GetSelectedFieldsAndPropertiesAsync(SyntaxTree tree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken)
                => CSharpSelectedMembers.Instance.GetSelectedFieldsAndPropertiesAsync(tree, textSpan, allowPartialSelection, cancellationToken);
        }
    }
}

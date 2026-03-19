// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SourceDocumentationCommentUtils
    {
        internal static string GetAndCacheDocumentationComment(Symbol symbol, bool expandIncludes, ref string lazyXmlText)
        {
            // NOTE: For xml doc comments from source, the culture is ignored (we just return the
            // doc comment as it appears in source), so it won't affect what we cache.
            if (lazyXmlText == null)
            {
                string xmlText = DocumentationCommentCompiler.GetDocumentationCommentXml(symbol, expandIncludes, default(CancellationToken));
                Interlocked.CompareExchange(ref lazyXmlText, xmlText, null);
            }

            return lazyXmlText;
        }

        internal static ImmutableArray<DocumentationCommentTriviaSyntax> GetDocumentationCommentTriviaFromSyntaxNode(CSharpSyntaxNode syntaxNode, DiagnosticBag diagnostics)
        {
            if (syntaxNode.SyntaxTree.Options.DocumentationMode < DocumentationMode.Parse)
            {
                return ImmutableArray<DocumentationCommentTriviaSyntax>.Empty;
            }

            // All declarators in a declaration get the same doc comment.
            // (As a consequence, the same duplicate diagnostics are produced for each declarator.)
            if (syntaxNode.Kind() == SyntaxKind.VariableDeclarator)
            {
                CSharpSyntaxNode curr = syntaxNode;
                while ((object)curr != null)
                {
                    SyntaxKind kind = curr.Kind();
                    if (kind == SyntaxKind.FieldDeclaration || kind == SyntaxKind.EventFieldDeclaration)
                    {
                        break;
                    }

                    curr = curr.Parent;
                }

                if ((object)curr != null)
                {
                    syntaxNode = curr;
                }
            }

            ArrayBuilder<DocumentationCommentTriviaSyntax> builder = null;
            bool seenOtherTrivia = false;
            foreach (var trivia in syntaxNode.GetLeadingTrivia().Reverse())
            {
                switch (trivia.Kind())
                {
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                        {
                            if (seenOtherTrivia)
                            {
                                // In most cases, unprocessed doc comments are reported by UnprocessedDocumentationCommentFinder.
                                // However, in places where doc comments *are* allowed, it's easier to determine which will
                                // be unprocessed here.
                                var tree = trivia.SyntaxTree;
                                if (tree.ReportDocumentationCommentDiagnostics())
                                {
                                    int start = trivia.Position; // FullSpan start to include /** or ///
                                    const int length = 1; //Match dev11: span is just one character
                                    diagnostics.Add(ErrorCode.WRN_UnprocessedXMLComment, new SourceLocation(tree, new TextSpan(start, length)));
                                }
                            }
                            else
                            {
                                if (builder == null)
                                {
                                    builder = ArrayBuilder<DocumentationCommentTriviaSyntax>.GetInstance();
                                }

                                builder.Add((DocumentationCommentTriviaSyntax)trivia.GetStructure());
                            }
                            break;
                        }
                    case SyntaxKind.WhitespaceTrivia:
                    case SyntaxKind.EndOfLineTrivia:
                        // These can legally appear between doc comments.
                        break;
                    default:
                        // For some reason, dev11 ignores trivia between the last doc comment and the
                        // symbol declaration.  (e.g. can have regular comment between doc comment and decl).
                        if (builder != null)
                        {
                            seenOtherTrivia = true;
                        }
                        break;
                }
            }

            if (builder == null)
            {
                return ImmutableArray<DocumentationCommentTriviaSyntax>.Empty;
            }

            builder.ReverseContents();
            return builder.ToImmutableAndFree();
        }
    }
}

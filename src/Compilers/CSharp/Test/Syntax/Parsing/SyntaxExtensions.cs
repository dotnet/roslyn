// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public static class SyntaxExtensions
    {
        # region SyntaxNodeExtensions
        public static SyntaxTriviaList GetLeadingTrivia(this SyntaxNode node)
        {
            return node.GetFirstToken(includeSkipped: true).LeadingTrivia;
        }

        public static SyntaxTriviaList GetTrailingTrivia(this SyntaxNode node)
        {
            return node.GetLastToken(includeSkipped: true).TrailingTrivia;
        }

        internal static ImmutableArray<DiagnosticInfo> Errors(this SyntaxNode node)
        {
            return node.Green.ErrorsOrWarnings(errorsOnly: true);
        }

        internal static ImmutableArray<DiagnosticInfo> Warnings(this SyntaxNode node)
        {
            return node.Green.ErrorsOrWarnings(errorsOnly: false);
        }

        internal static ImmutableArray<DiagnosticInfo> ErrorsAndWarnings(this SyntaxNode node)
        {
            return node.Green.ErrorsAndWarnings();
        }
        #endregion

        # region SyntaxTokenExtensions
        public static SyntaxTriviaList GetLeadingTrivia(this SyntaxToken token)
        {
            return token.LeadingTrivia;
        }

        public static SyntaxTriviaList GetTrailingTrivia(this SyntaxToken token)
        {
            return token.TrailingTrivia;
        }

        internal static ImmutableArray<DiagnosticInfo> Errors(this SyntaxToken token)
        {
            return ((Syntax.InternalSyntax.CSharpSyntaxNode)token.Node).ErrorsOrWarnings(errorsOnly: true);
        }

        internal static ImmutableArray<DiagnosticInfo> Warnings(this SyntaxToken token)
        {
            return ((Syntax.InternalSyntax.CSharpSyntaxNode)token.Node).ErrorsOrWarnings(errorsOnly: false);
        }

        internal static ImmutableArray<DiagnosticInfo> ErrorsAndWarnings(this SyntaxToken token)
        {
            return ((Syntax.InternalSyntax.CSharpSyntaxNode)token.Node).ErrorsAndWarnings();
        }
        #endregion

        # region SyntaxNodeOrTokenExtensions
        internal static ImmutableArray<DiagnosticInfo> Errors(this SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.UnderlyingNode.ErrorsOrWarnings(errorsOnly: true);
        }

        internal static ImmutableArray<DiagnosticInfo> Warnings(this SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.UnderlyingNode.ErrorsOrWarnings(errorsOnly: false);
        }

        internal static ImmutableArray<DiagnosticInfo> ErrorsAndWarnings(this SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.UnderlyingNode.ErrorsAndWarnings();
        }
        #endregion

        # region SyntaxTriviaExtensions
        internal static ImmutableArray<DiagnosticInfo> Errors(this SyntaxTrivia trivia)
        {
            return ((InternalSyntax.CSharpSyntaxNode)trivia.UnderlyingNode).ErrorsOrWarnings(errorsOnly: true);
        }

        internal static ImmutableArray<DiagnosticInfo> Warnings(this SyntaxTrivia trivia)
        {
            return ((InternalSyntax.CSharpSyntaxNode)trivia.UnderlyingNode).ErrorsOrWarnings(errorsOnly: false);
        }

        internal static ImmutableArray<DiagnosticInfo> ErrorsAndWarnings(this SyntaxTrivia trivia)
        {
            return ((InternalSyntax.CSharpSyntaxNode)trivia.UnderlyingNode).ErrorsAndWarnings();
        }
        #endregion

        private static ImmutableArray<DiagnosticInfo> ErrorsOrWarnings(this GreenNode node, bool errorsOnly)
        {
            ArrayBuilder<DiagnosticInfo> b = ArrayBuilder<DiagnosticInfo>.GetInstance();

            var l = new SyntaxDiagnosticInfoList(node);

            foreach (var item in l)
            {
                if (item.Severity == (errorsOnly ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning))
                    b.Add(item);
            }

            return b.ToImmutableAndFree();
        }

        private static ImmutableArray<DiagnosticInfo> ErrorsAndWarnings(this GreenNode node)
        {
            ArrayBuilder<DiagnosticInfo> b = ArrayBuilder<DiagnosticInfo>.GetInstance();

            var l = new SyntaxDiagnosticInfoList(node);

            foreach (var item in l)
            {
                b.Add(item);
            }

            return b.ToImmutableAndFree();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class CSharpSimplifyLinqExpressionCodeFixProvider : AbstractSimplifyLinqExpressionCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpSimplifyLinqExpressionCodeFixProvider()
        {
        }

        protected override SyntaxNode? GetIdentifierName(SyntaxNode node)
            => (node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First().Expression as MemberAccessExpressionSyntax)?.Name;

        protected override SyntaxNode[] GetLambdaExpression(SyntaxNode node)
            => node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Skip(1).First().ArgumentList.Arguments.ToArray();

        protected override SyntaxNode GetMemberAccessExpression(SyntaxNode node)
            => node.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().Skip(1).First().Expression;
    }
}

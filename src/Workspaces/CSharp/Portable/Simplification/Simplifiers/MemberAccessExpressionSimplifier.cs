// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal class MemberAccessExpressionSimplifier : AbstractMemberAccessExpressionSimplifier<
        ExpressionSyntax,
        MemberAccessExpressionSyntax,
        ThisExpressionSyntax>
    {
        public static readonly MemberAccessExpressionSimplifier Instance = new();

        private MemberAccessExpressionSimplifier()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override ISpeculationAnalyzer GetSpeculationAnalyzer(
            SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken)
        {
            return new SpeculationAnalyzer(memberAccessExpression, memberAccessExpression.Name, semanticModel, cancellationToken);
        }
    }
}

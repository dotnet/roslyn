// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Linq;
using System;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqMethodToLinqQueryProvider : AbstractConvertLinqMethodToLinqQueryProvider
    {
        protected override IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken) => new CSharpAnalyzer(semanticModel, cancellationToken);

        private sealed class CSharpAnalyzer : Analyzer<ExpressionSyntax, QueryExpressionSyntax>
        {
            private static readonly ImmutableHashSet<string> methodNames = ImmutableHashSet.Create(
                nameof(Enumerable.Where),
                nameof(Enumerable.Select),
                nameof(Enumerable.SelectMany),
                nameof(Enumerable.GroupBy),
                nameof(Enumerable.OrderBy),
                nameof(Enumerable.OrderByDescending),
                nameof(Enumerable.ThenBy),
                nameof(Enumerable.ThenByDescending),
                nameof(Enumerable.GroupJoin),
                nameof(Enumerable.Join));

            public CSharpAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken)
                : base(semanticModel, cancellationToken)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_to_query;

            protected override ExpressionSyntax FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context)
            {
                return (ExpressionSyntax)root.FindNode(context.Span).FirstAncestorOrSelf<MemberAccessExpressionSyntax>(m => methodNames.Contains(((IdentifierNameSyntax)m.Name).Identifier.ValueText)).Parent;
            }

            protected override bool TryConvert(ExpressionSyntax source, out QueryExpressionSyntax destination)
            {
                throw new NotImplementedException();
            }
        }
    }
}

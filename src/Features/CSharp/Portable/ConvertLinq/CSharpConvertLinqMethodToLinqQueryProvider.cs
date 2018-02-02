// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertLinqMethodToLinqQueryProvider)), Shared]
    internal sealed class CSharpConvertLinqMethodToLinqQueryProvider : AbstractConvertLinqProvider
    {
        protected override IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            => new CSharpAnalyzer(syntaxFacts, semanticModel);

        private sealed class CSharpAnalyzer : Analyzer<InvocationExpressionSyntax, QueryExpressionSyntax>
        {
            public CSharpAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
                : base(syntaxFacts, semanticModel)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_linq_method_to_linq_query;

            protected override QueryExpressionSyntax Convert(InvocationExpressionSyntax source)
            {
                return SyntaxFactory.QueryExpression(default, default);
            }

            protected override InvocationExpressionSyntax FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context)
            {
                throw new System.NotImplementedException();
            }

            // TODO refactor
            protected override bool Validate(InvocationExpressionSyntax source, QueryExpressionSyntax destination, CancellationToken cancellationToken)
            {
                var speculationAnalyzer = new SpeculationAnalyzer(source, destination, _semanticModel, cancellationToken);
                if (speculationAnalyzer.ReplacementChangesSemantics())
                {
                    return false;
                }

                // TODO add more checks
                return true;
            }
        }
    }
}

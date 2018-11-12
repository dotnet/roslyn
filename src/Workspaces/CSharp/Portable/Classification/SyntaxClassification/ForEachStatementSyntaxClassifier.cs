using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class ForEachStatementSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override void AddClassifications(
            Workspace workspace,
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (syntax is ForEachStatementSyntax forEachStatement)
            {
                result.Add(new ClassifiedSpan(forEachStatement.InKeyword.Span, ClassificationTypeNames.ControlKeyword));
            }
        }

        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(typeof(ForEachStatementSyntax));
    }
}

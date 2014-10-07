using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Shared.Extensions;
using Roslyn.Utilities;

namespace Roslyn.Services.CSharp.Classification.Classifiers
{
    internal class TypeParameterConstraintClauseSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override IEnumerable<ClassifiedSpan> ClassifyNode(
            IDocument document,
            CommonSyntaxNode syntax,
            CancellationToken cancellationToken)
        {
            if (syntax is TypeParameterConstraintClauseSyntax)
            {
                return ClassifyTypeParameterConstraintClause(document, (TypeParameterConstraintClauseSyntax)syntax, cancellationToken);
            }

            return null;
        }

        public override IEnumerable<System.Type> SyntaxNodeTypes
        {
            get
            {
                yield return typeof(TypeParameterConstraintClauseSyntax);
            }
        }

        private IEnumerable<ClassifiedSpan> ClassifyTypeParameterConstraintClause(
            IDocument document,
            TypeParameterConstraintClauseSyntax constraintClause,
            CancellationToken cancellationToken)
        {
            var semanticModel = document.GetSemanticModel(cancellationToken);
            var identifier = constraintClause.Identifier;
            var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).GetAllSymbols().OfType<ITypeSymbol>().FirstOrDefault();

            if (symbol != null)
            {
                var classification = GetClassificationForType(symbol);
                if (classification != null)
                {
                    return SpecializedCollections.SingletonEnumerable(new ClassifiedSpan(identifier.Span, classification));
                }
            }

            return null;
        }
    }
}
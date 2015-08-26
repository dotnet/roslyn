using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices.TypeInferenceService
{
    internal abstract class AbstractTypeInferenceService<TExpressionSyntax> : ITypeInferenceService
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract class AbstractTypeInferrer
        {
            protected readonly CancellationToken CancellationToken;
            protected readonly SemanticModel SemanticModel;

            private readonly HashSet<TExpressionSyntax> _seenExpressionInferType = new HashSet<TExpressionSyntax>();
            private readonly HashSet<TExpressionSyntax> _seenExpressionGetType = new HashSet<TExpressionSyntax>();
            private readonly Func<ITypeSymbol, bool> isUsableTypeFunc;

            protected AbstractTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                this.SemanticModel = semanticModel;
                this.CancellationToken = cancellationToken;
                this.isUsableTypeFunc =  t => t != null && !IsUnusableType(t);
            }

            protected abstract IEnumerable<ITypeSymbol> InferTypesWorker_DoNotCallDirectly(int position);
            protected abstract IEnumerable<ITypeSymbol> InferTypesWorker_DoNotCallDirectly(TExpressionSyntax expression);
            protected abstract IEnumerable<ITypeSymbol> GetTypes_DoNotCallDirectly(TExpressionSyntax expression, bool objectAsDefault);
            protected abstract bool IsUnusableType(ITypeSymbol arg);

            protected Compilation Compilation => SemanticModel.Compilation;

            public IEnumerable<ITypeSymbol> InferTypes(int position)
            {
                var types = InferTypesWorker_DoNotCallDirectly(position);
                return Filter(types);
            }

            public IEnumerable<ITypeSymbol> InferTypes(TExpressionSyntax expression)
            {
                if (expression != null)
                {
                    if (_seenExpressionInferType.Add(expression))
                    {
                        var types = InferTypesWorker_DoNotCallDirectly(expression);
                        return Filter(types);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            protected IEnumerable<ITypeSymbol> GetTypes(TExpressionSyntax expression, bool objectAsDefault = false )
            {
                if (_seenExpressionGetType.Add(expression))
                {
                    return GetTypes_DoNotCallDirectly(expression, objectAsDefault);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> Filter(IEnumerable<ITypeSymbol> types)
            {
                return types.Where(isUsableTypeFunc)
                            .Distinct()
                            .ToImmutableReadOnlyListOrEmpty();
            }

        }

        protected abstract AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken);

        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(position);
        }

        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        {
            return CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(expression as TExpressionSyntax);
        }
    }
}

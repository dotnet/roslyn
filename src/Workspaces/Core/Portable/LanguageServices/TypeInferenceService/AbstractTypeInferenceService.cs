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
            protected readonly Func<ITypeSymbol, bool> IsUsableTypeFunc;

            private readonly HashSet<TExpressionSyntax> _seenExpressionInferType = new HashSet<TExpressionSyntax>();
            private readonly HashSet<TExpressionSyntax> _seenExpressionGetType = new HashSet<TExpressionSyntax>();

            private static readonly Func<ITypeSymbol, bool> isNotNull = t => t != null;

            protected AbstractTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                this.SemanticModel = semanticModel;
                this.CancellationToken = cancellationToken;
                this.IsUsableTypeFunc =  t => t != null && !IsUnusableType(t);
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

            public IEnumerable<ITypeSymbol> InferTypes(TExpressionSyntax expression, bool filterUnusable = true)
            {
                if (expression != null)
                {
                    if (_seenExpressionInferType.Add(expression))
                    {
                        var types = InferTypesWorker_DoNotCallDirectly(expression);
                        return Filter(types, filterUnusable);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            protected IEnumerable<ITypeSymbol> GetTypes(TExpressionSyntax expression, bool objectAsDefault = false)
            {
                if (_seenExpressionGetType.Add(expression))
                {
                    return GetTypes_DoNotCallDirectly(expression, objectAsDefault);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> Filter(IEnumerable<ITypeSymbol> types, bool filterUnusable = true)
            {
                return types.Where(filterUnusable ? IsUsableTypeFunc : isNotNull)
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            protected readonly Func<TypeInferenceInfo, bool> IsUsableTypeFunc;

            private readonly HashSet<TExpressionSyntax> _seenExpressionInferType = new HashSet<TExpressionSyntax>();
            private readonly HashSet<TExpressionSyntax> _seenExpressionGetType = new HashSet<TExpressionSyntax>();

            private static readonly Func<TypeInferenceInfo, bool> s_isNotNull = t => t.InferredType != null;

            protected AbstractTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                this.SemanticModel = semanticModel;
                this.CancellationToken = cancellationToken;
                this.IsUsableTypeFunc = t => t.InferredType != null && !IsUnusableType(t.InferredType);
            }

            protected abstract IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(int position);
            protected abstract IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(TExpressionSyntax expression);
            protected abstract IEnumerable<TypeInferenceInfo> GetTypes_DoNotCallDirectly(TExpressionSyntax expression, bool objectAsDefault);
            protected abstract bool IsUnusableType(ITypeSymbol arg);

            protected Compilation Compilation => SemanticModel.Compilation;

            public IEnumerable<TypeInferenceInfo> InferTypes(int position)
            {
                var types = InferTypesWorker_DoNotCallDirectly(position);
                return Filter(types);
            }

            public IEnumerable<TypeInferenceInfo> InferTypes(TExpressionSyntax expression, bool filterUnusable = true)
            {
                if (expression != null)
                {
                    if (_seenExpressionInferType.Add(expression))
                    {
                        var types = InferTypesWorker_DoNotCallDirectly(expression);
                        return Filter(types, filterUnusable);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            protected IEnumerable<TypeInferenceInfo> GetTypes(TExpressionSyntax expression, bool objectAsDefault = false)
            {
                if (_seenExpressionGetType.Add(expression))
                {
                    return GetTypes_DoNotCallDirectly(expression, objectAsDefault);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> Filter(IEnumerable<TypeInferenceInfo> types, bool filterUnusable = true)
            {
                return types.Where(filterUnusable ? IsUsableTypeFunc : s_isNotNull)
                            .Distinct()
                            .ToImmutableReadOnlyListOrEmpty();
            }

            protected IEnumerable<ITypeSymbol> ExpandParamsParameter(IParameterSymbol parameterSymbol)
            {
                var result = new List<ITypeSymbol>();
                result.Add(parameterSymbol.Type);

                if (parameterSymbol.IsParams)
                {
                    var arrayTypeSymbol = parameterSymbol.Type as IArrayTypeSymbol;
                    if (arrayTypeSymbol != null)
                    {
                        result.Add(arrayTypeSymbol.ElementType);
                    }
                }

                return result;
            }
        }

        protected abstract AbstractTypeInferrer CreateTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken);

        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(position).Select(t => t.InferredType);
        }

        public IEnumerable<ITypeSymbol> InferTypes(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        {
            var result = new List<ITypeSymbol>();
            var typeInferenceInfo = CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(expression as TExpressionSyntax);
            foreach (var info in typeInferenceInfo)
            {
                result.Add(info.InferredType);
            }

            return result;
        }

        public IEnumerable<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(position);
        }

        public IEnumerable<TypeInferenceInfo> GetTypeInferenceInfo(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        {
            return CreateTypeInferrer(semanticModel, cancellationToken).InferTypes(expression as TExpressionSyntax);
        }
    }
}

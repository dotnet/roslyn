// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices.TypeInferenceService
{
    internal partial class AbstractTypeInferenceService : ITypeInferenceService
    {
        protected abstract class AbstractTypeInferrer
        {
            protected readonly CancellationToken CancellationToken;
            protected readonly SemanticModel SemanticModel;
            protected readonly Func<TypeInferenceInfo, bool> IsUsableTypeFunc;

            private readonly HashSet<SyntaxNode> _seenExpressionInferType = new HashSet<SyntaxNode>();
            private readonly HashSet<SyntaxNode> _seenExpressionGetType = new HashSet<SyntaxNode>();

            private static readonly Func<TypeInferenceInfo, bool> s_isNotNull = t => t.InferredType != null;

            protected AbstractTypeInferrer(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                this.SemanticModel = semanticModel;
                this.CancellationToken = cancellationToken;
                this.IsUsableTypeFunc = t => t.InferredType != null && !IsUnusableType(t.InferredType);
            }

            protected abstract IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(int position);
            protected abstract IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(SyntaxNode expression);
            protected abstract IEnumerable<TypeInferenceInfo> GetTypes_DoNotCallDirectly(SyntaxNode expression, bool objectAsDefault);
            protected abstract bool IsUnusableType(ITypeSymbol arg);

            protected Compilation Compilation => SemanticModel.Compilation;

            public ImmutableArray<TypeInferenceInfo> InferTypes(int position)
            {
                var types = InferTypesWorker_DoNotCallDirectly(position);
                return Filter(types);
            }

            public ImmutableArray<TypeInferenceInfo> InferTypes(SyntaxNode expression, bool filterUnusable = true)
            {
                if (expression != null)
                {
                    if (_seenExpressionInferType.Add(expression))
                    {
                        var types = InferTypesWorker_DoNotCallDirectly(expression);
                        return Filter(types, filterUnusable);
                    }
                }

                return ImmutableArray<TypeInferenceInfo>.Empty;
            }

            protected IEnumerable<TypeInferenceInfo> GetTypes(SyntaxNode expression, bool objectAsDefault = false)
            {
                if (expression != null)
                {
                    if (_seenExpressionGetType.Add(expression))
                    {
                        return GetTypes_DoNotCallDirectly(expression, objectAsDefault);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ImmutableArray<TypeInferenceInfo> Filter(IEnumerable<TypeInferenceInfo> types, bool filterUnusable = true)
            {
                return types.Where(filterUnusable ? IsUsableTypeFunc : s_isNotNull)
                            .Distinct()
                            .ToImmutableArray();
            }

            protected IEnumerable<TypeInferenceInfo> CreateResult(SpecialType type, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
                => CreateResult(Compilation.GetSpecialType(type).WithNullability(nullableAnnotation));

            protected IEnumerable<TypeInferenceInfo> CreateResult(ITypeSymbol type)
                => type == null
                    ? SpecializedCollections.EmptyCollection<TypeInferenceInfo>()
                    : SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(type));

            protected IEnumerable<ITypeSymbol> ExpandParamsParameter(IParameterSymbol parameterSymbol)
            {
                var result = new List<ITypeSymbol>();
                result.Add(parameterSymbol.Type);

                if (parameterSymbol.IsParams)
                {
                    if (parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
                    {
                        result.Add(arrayTypeSymbol.ElementType);
                    }
                }

                return result;
            }
        }
    }
}

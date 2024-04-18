// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService.TypeInferenceService;

internal partial class AbstractTypeInferenceService : ITypeInferenceService
{
    protected abstract class AbstractTypeInferrer
    {
        protected readonly CancellationToken CancellationToken;
        protected readonly SemanticModel SemanticModel;
        protected readonly Func<TypeInferenceInfo, bool> IsUsableTypeFunc;

        private readonly HashSet<SyntaxNode> _seenExpressionInferType = [];
        private readonly HashSet<SyntaxNode> _seenExpressionGetType = [];

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

            return [];
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

            return [];
        }

        private ImmutableArray<TypeInferenceInfo> Filter(IEnumerable<TypeInferenceInfo> types, bool filterUnusable = true)
        {
            return types.Where(filterUnusable ? IsUsableTypeFunc : s_isNotNull)
                        .Distinct()
                        .ToImmutableArray();
        }

        protected IEnumerable<TypeInferenceInfo> CreateResult(SpecialType type, NullableAnnotation nullableAnnotation = NullableAnnotation.None)
            => CreateResult(Compilation.GetSpecialType(type).WithNullableAnnotation(nullableAnnotation));

        protected static IEnumerable<TypeInferenceInfo> CreateResult(ITypeSymbol type)
            => type == null ? [] : [new TypeInferenceInfo(type)];

        protected static IEnumerable<ITypeSymbol> ExpandParamsParameter(IParameterSymbol parameterSymbol)
        {
            var result = new List<ITypeSymbol>
            {
                parameterSymbol.Type
            };

            if (parameterSymbol.IsParams)
            {
                if (parameterSymbol.Type is IArrayTypeSymbol arrayTypeSymbol)
                {
                    result.Add(arrayTypeSymbol.ElementType);
                }
            }

            return result;
        }

        protected static IEnumerable<TypeInferenceInfo> GetCollectionElementType(INamedTypeSymbol type)
        {
            if (type != null)
            {
                var parameters = type.TypeArguments;

                var elementType = parameters.ElementAtOrDefault(0);
                if (elementType != null)
                    return [new TypeInferenceInfo(elementType)];
            }

            return [];
        }

        protected static bool IsEnumHasFlag(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method &&
                   symbol.Name == nameof(Enum.HasFlag) &&
                   symbol.ContainingType?.SpecialType == SpecialType.System_Enum;
        }
    }
}

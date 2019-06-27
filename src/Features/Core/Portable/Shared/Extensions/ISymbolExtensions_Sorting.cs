// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ISymbolExtensions2
    {
        public static ImmutableArray<TSymbol> Sort<TSymbol>(
            this ImmutableArray<TSymbol> symbols,
            ISymbolDisplayService symbolDisplayService,
            SemanticModel semanticModel,
            int position)
            where TSymbol : ISymbol
        {
            var symbolToParameterTypeNames = new ConcurrentDictionary<TSymbol, string[]>();
            string[] getParameterTypeNames(TSymbol s) => GetParameterTypeNames(s, symbolDisplayService, semanticModel, position);

            return symbols.OrderBy((s1, s2) => Compare(s1, s2, symbolToParameterTypeNames, getParameterTypeNames))
                          .ToImmutableArray();
        }

        private static INamedTypeSymbol GetNamedType(ITypeSymbol type)
        {
            switch (type)
            {
                case INamedTypeSymbol namedType: return namedType;
                case IArrayTypeSymbol arrayType: return GetNamedType(arrayType.ElementType);
                case IPointerTypeSymbol pointerType: return GetNamedType(pointerType.PointedAtType);
                default: return null;
            }
        }

        private static int CompareParameters(
            ImmutableArray<IParameterSymbol> xParameters, string[] xTypeNames,
            ImmutableArray<IParameterSymbol> yParameters, string[] yTypeNames)
        {
            // * Order by the number of parameters
            // * If the same number of parameters...
            //   * Sort alphabetically by parameter type name
            //   * Params parameters are sorted at the end
            //   * Parameter types with type parameters are after those
            if (xParameters.IsDefault || yParameters.IsDefault)
            {
                return xParameters.IsDefault && yParameters.IsDefault ? 0 :
                       xParameters.IsDefault ? -1 : 1;
            }

            var diff = xParameters.Length - yParameters.Length;
            if (diff != 0)
            {
                return diff;
            }

            var paramCount = xParameters.Length;
            for (var i = 0; i < paramCount; i++)
            {
                var xParam = xParameters[i];
                var yParam = yParameters[i];

                var xParamType = GetNamedType(xParam.Type);
                var yParamType = GetNamedType(yParam.Type);
                if (xParamType != null && yParamType != null)
                {
                    diff = CompareNamedTypes(xParamType, yParamType);
                    if (diff != 0)
                    {
                        return diff;
                    }
                }

                if (xParam.IsParams != yParam.IsParams)
                {
                    return xParam.IsParams ? 1 : -1;
                }

                diff = CultureInfo.CurrentUICulture.CompareInfo.Compare(xTypeNames[i], yTypeNames[i], CompareOptions.StringSort);
                if (diff != 0)
                {
                    return diff;
                }
            }

            return 0;
        }

        private static int CompareProperties(IPropertySymbol xProperty, string[] xTypeNames, IPropertySymbol yProperty, string[] yTypeNames)
        {
            return CompareParameters(xProperty.Parameters, xTypeNames, yProperty.Parameters, yTypeNames);
        }

        private static int CompareMethods(IMethodSymbol xMethod, string[] xTypeNames, IMethodSymbol yMethod, string[] yTypeNames)
        {
            // * Order by arity
            // * Order by parameters

            var diff = xMethod.Arity - yMethod.Arity;
            if (diff != 0)
            {
                return diff;
            }

            return CompareParameters(xMethod.Parameters, xTypeNames, yMethod.Parameters, yTypeNames);
        }

        private static int CompareEvents(IEventSymbol xEvent, string[] xTypeNames, IEventSymbol yEvent, string[] yTypeNames)
        {
            return CompareParameters(GetMethodOrIndexerOrEventParameters(xEvent), xTypeNames, GetMethodOrIndexerOrEventParameters(yEvent), yTypeNames);
        }

        private static int CompareNamedTypes(INamedTypeSymbol xNamedType, INamedTypeSymbol yNamedType)
        {
            // For named types, we sort on arity.
            return xNamedType.Arity - yNamedType.Arity;
        }

        private static string[] GetParameterTypeNames(
            ISymbol symbol,
            ISymbolDisplayService symbolDisplayService,
            SemanticModel semanticModel,
            int position)
        {
            return GetMethodOrIndexerOrEventParameters(symbol)
                         .Select(p => symbolDisplayService.ToMinimalDisplayString(semanticModel, position, p.Type))
                         .ToArray();
        }

        private static ImmutableArray<IParameterSymbol> GetMethodOrIndexerOrEventParameters(ISymbol symbol)
        {
            if (symbol is IEventSymbol ev)
            {
                var type = ev.Type as INamedTypeSymbol;
                if (type.IsDelegateType())
                {
                    return type.DelegateInvokeMethod.Parameters;
                }
            }

            return symbol.GetParameters();
        }

        private static int Compare<TSymbol>(
            TSymbol s1, TSymbol s2,
            ConcurrentDictionary<TSymbol, string[]> symbolToParameterTypeNames,
            Func<TSymbol, string[]> getParameterTypeNames)
            where TSymbol : ISymbol
        {
            var symbol1ParameterTypeNames = symbolToParameterTypeNames.GetOrAdd(s1, getParameterTypeNames);
            var symbol2ParameterTypeNames = symbolToParameterTypeNames.GetOrAdd(s2, getParameterTypeNames);

            // Order named types before methods and properties, and methods before properties.

            if (s1.Kind == SymbolKind.NamedType || s2.Kind == SymbolKind.NamedType)
            {
                return s1.Kind == s2.Kind
                    ? CompareNamedTypes((INamedTypeSymbol)s1, (INamedTypeSymbol)s2)
                    : s1.Kind == SymbolKind.NamedType ? -1 : 1;
            }

            if (s1.Kind == SymbolKind.Method || s2.Kind == SymbolKind.Method)
            {
                return s1.Kind == s2.Kind
                    ? CompareMethods((IMethodSymbol)s1, symbol1ParameterTypeNames, (IMethodSymbol)s2, symbol2ParameterTypeNames)
                    : s1.Kind == SymbolKind.Method ? -1 : 1;
            }

            if (s1.Kind == SymbolKind.Property || s2.Kind == SymbolKind.Property)
            {
                return s1.Kind == s2.Kind
                    ? CompareProperties((IPropertySymbol)s1, symbol1ParameterTypeNames, (IPropertySymbol)s2, symbol2ParameterTypeNames)
                    : s1.Kind == SymbolKind.Property ? -1 : 1;
            }

            if (s1.Kind == SymbolKind.Event || s2.Kind == SymbolKind.Event)
            {
                return s1.Kind == s2.Kind
                    ? CompareEvents((IEventSymbol)s1, symbol1ParameterTypeNames, (IEventSymbol)s2, symbol2ParameterTypeNames)
                    : s1.Kind == SymbolKind.Event ? -1 : 1;
            }

            return Contract.FailWithReturn<int>(
                string.Format("Comparing unexpected symbol kinds: {0} and {1}.", s1.Kind, s2.Kind));
        }
    }
}

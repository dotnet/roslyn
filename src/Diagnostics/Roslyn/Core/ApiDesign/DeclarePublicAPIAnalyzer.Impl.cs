// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers.ApiDesign
{
    public partial class DeclarePublicAPIAnalyzer : DiagnosticAnalyzer
    {
        private sealed class ApiLine
        {
            public readonly string Text;
            public readonly TextSpan Span;
            public readonly SourceText SourceText;
            public readonly string Path;

            internal ApiLine(string text, TextSpan span, SourceText sourceText, string path)
            {
                Text = text;
                Span = span;
                SourceText = sourceText;
                Path = path;
            }
        }

        private struct ApiData
        {
            public readonly ImmutableArray<ApiLine> ApiList;
            public readonly ImmutableArray<ApiLine> RemovedApiList;

            internal ApiData(ImmutableArray<ApiLine> apiList, ImmutableArray<ApiLine> removedApiList)
            {
                ApiList = apiList;
                RemovedApiList = removedApiList;
            }
        }

        private sealed class Impl
        {
            private static readonly HashSet<MethodKind> s_ignorableMethodKinds = new HashSet<MethodKind>
            {
                MethodKind.EventAdd,
                MethodKind.EventRemove
            };

            private readonly ApiData _unshippedData;
            private readonly Dictionary<ITypeSymbol, bool> _typeCanBeExtendedCache = new Dictionary<ITypeSymbol, bool>();
            private readonly HashSet<string> _visitedApiList = new HashSet<string>(StringComparer.Ordinal);
            private readonly Dictionary<string, ApiLine> _publicApiMap = new Dictionary<string, ApiLine>(StringComparer.Ordinal);

            internal Impl(ApiData shippedData, ApiData unshippedData)
            {
                _unshippedData = unshippedData;

                foreach (var cur in shippedData.ApiList)
                {
                    _publicApiMap.Add(cur.Text, cur);
                }

                foreach (var cur in unshippedData.ApiList)
                {
                    _publicApiMap.Add(cur.Text, cur);
                }
            }

            internal void OnSymbolAction(SymbolAnalysisContext symbolContext)
            {
                var symbol = symbolContext.Symbol;

                var methodSymbol = symbol as IMethodSymbol;
                if (methodSymbol != null &&
                    s_ignorableMethodKinds.Contains(methodSymbol.MethodKind))
                {
                    return;
                }

                if (!IsPublicApi(symbol))
                {
                    return;
                }

                string publicApiName = GetPublicApiName(symbol);
                _visitedApiList.Add(publicApiName);

                if (!_publicApiMap.ContainsKey(publicApiName))
                {
                    var errorMessageName = symbol.ToDisplayString(ShortSymbolNameFormat);
                    var propertyBag = ImmutableDictionary<string, string>.Empty
                        .Add(PublicApiNamePropertyBagKey, publicApiName)
                        .Add(MinimalNamePropertyBagKey, errorMessageName);

                    foreach (var sourceLocation in symbol.Locations.Where(loc => loc.IsInSource))
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(DeclareNewApiRule, sourceLocation, propertyBag, errorMessageName));
                    }
                }

                // Check if a public API is a constructor that makes this class instantiable, even though the base class
                // is not instantiable. That API pattern is not allowed, because it causes protected members of
                // the base class, which are not considered public APIs, to be exposed to subclasses of this class.
                if ((symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor &&
                    symbol.ContainingType.TypeKind == TypeKind.Class &&
                    !symbol.ContainingType.IsSealed &&
                    symbol.ContainingType.BaseType != null &&
                    IsPublicApi(symbol.ContainingType.BaseType) &&
                    !CanTypeBeExtendedPublicly(symbol.ContainingType.BaseType))
                {
                    var errorMessageName = symbol.ToDisplayString(ShortSymbolNameFormat);
                    var propertyBag = ImmutableDictionary<string, string>.Empty;
                    symbolContext.ReportDiagnostic(Diagnostic.Create(ExposedNoninstantiableType, symbol.Locations[0], propertyBag, errorMessageName));
                }
            }

            internal void OnCompilationEnd(CompilationAnalysisContext context)
            {
                List<ApiLine> deletedApiList = GetDeletedApiList();
                foreach (var cur in deletedApiList)
                {
                    var linePositionSpan = cur.SourceText.Lines.GetLinePositionSpan(cur.Span);
                    var location = Location.Create(cur.Path, cur.Span, linePositionSpan);
                    var propertyBag = ImmutableDictionary<string, string>.Empty.Add(PublicApiNamePropertyBagKey, cur.Text);
                    context.ReportDiagnostic(Diagnostic.Create(RemoveDeletedApiRule, location, propertyBag, cur.Text));
                }
            }

            /// <summary>
            /// Calculated the set of APIs which have been deleted but not yet documented.
            /// </summary>
            /// <returns></returns>
            internal List<ApiLine> GetDeletedApiList()
            {
                var list = new List<ApiLine>();
                foreach (var pair in _publicApiMap)
                {
                    if (_visitedApiList.Contains(pair.Key))
                    {
                        continue;
                    }

                    if (_unshippedData.RemovedApiList.Any(x => x.Text == pair.Key))
                    {
                        continue;
                    }

                    list.Add(pair.Value);
                }

                return list;
            }

            private bool IsPublicApi(ISymbol symbol)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        return symbol.ContainingType == null || IsPublicApi(symbol.ContainingType);
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        // Protected symbols must have parent types (that is, top-level protected
                        // symbols are not allowed.
                        return
                            symbol.ContainingType != null &&
                            IsPublicApi(symbol.ContainingType) &&
                            CanTypeBeExtendedPublicly(symbol.ContainingType);
                    default:
                        return false;
                }
            }

            private bool CanTypeBeExtendedPublicly(ITypeSymbol type)
            {
                bool result;
                if (_typeCanBeExtendedCache.TryGetValue(type, out result))
                {
                    return result;
                }

                // a type can be extended publicly if (1) it isn't sealed, and (2) it has some constructor that is
                // not internal, private or protected&internal
                result = !type.IsSealed &&
                    type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Any(
                        m => m.DeclaredAccessibility != Accessibility.Internal && m.DeclaredAccessibility != Accessibility.Private && m.DeclaredAccessibility != Accessibility.ProtectedAndInternal
                    );

                _typeCanBeExtendedCache.Add(type, result);
                return result;
            }
        }
    }
}

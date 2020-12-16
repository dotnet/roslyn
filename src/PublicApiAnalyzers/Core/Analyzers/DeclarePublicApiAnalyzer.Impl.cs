// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    public partial class DeclarePublicApiAnalyzer : DiagnosticAnalyzer
    {
        private sealed class ApiLine
        {
            public string Text { get; }
            public TextSpan Span { get; }
            public SourceText SourceText { get; }
            public string Path { get; }
            public bool IsShippedApi { get; }

            internal ApiLine(string text, TextSpan span, SourceText sourceText, string path, bool isShippedApi)
            {
                Text = text;
                Span = span;
                SourceText = sourceText;
                Path = path;
                IsShippedApi = isShippedApi;
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private readonly struct RemovedApiLine
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public string Text { get; }
            public ApiLine ApiLine { get; }

            internal RemovedApiLine(string text, ApiLine apiLine)
            {
                Text = text;
                ApiLine = apiLine;
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private struct ApiName
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public string Name { get; }
            public string NameWithNullability { get; }

            public ApiName(string name, string nameWithNullability)
            {
                Name = name;
                NameWithNullability = nameWithNullability;
            }
        }

#pragma warning disable CA1815 // Override equals and operator equals on value types
        private readonly struct ApiData
#pragma warning restore CA1815 // Override equals and operator equals on value types
        {
            public static readonly ApiData Empty = new ApiData(ImmutableArray<ApiLine>.Empty, ImmutableArray<RemovedApiLine>.Empty, nullableRank: -1);

            public ImmutableArray<ApiLine> ApiList { get; }
            public ImmutableArray<RemovedApiLine> RemovedApiList { get; }
            // Number for the max line where #nullable enable was found (-1 otherwise)
            public int NullableRank { get; }

            internal ApiData(ImmutableArray<ApiLine> apiList, ImmutableArray<RemovedApiLine> removedApiList, int nullableRank)
            {
                ApiList = apiList;
                RemovedApiList = removedApiList;
                NullableRank = nullableRank;
            }
        }

        private sealed class Impl
        {
            private const char ObliviousMarker = '~';

            private static readonly ImmutableArray<MethodKind> s_ignorableMethodKinds
                = ImmutableArray.Create(MethodKind.EventAdd, MethodKind.EventRemove);

            private readonly Compilation _compilation;
            private readonly ApiData _unshippedData;
            private readonly bool _useNullability;
            private readonly ConcurrentDictionary<ITypeSymbol, bool> _typeCanBeExtendedCache = new();
            private readonly ConcurrentDictionary<string, UnusedValue> _visitedApiList = new(StringComparer.Ordinal);
            private readonly IReadOnlyDictionary<string, ApiLine> _publicApiMap;

            internal Impl(Compilation compilation, ApiData shippedData, ApiData unshippedData)
            {
                _compilation = compilation;
                _useNullability = shippedData.NullableRank >= 0 || unshippedData.NullableRank >= 0;
                _unshippedData = unshippedData;

                var publicApiMap = new Dictionary<string, ApiLine>(StringComparer.Ordinal);
                foreach (ApiLine cur in shippedData.ApiList)
                {
                    publicApiMap.Add(cur.Text, cur);
                }

                foreach (ApiLine cur in unshippedData.ApiList)
                {
                    publicApiMap.Add(cur.Text, cur);
                }

                _publicApiMap = publicApiMap;
            }

            internal void OnSymbolAction(SymbolAnalysisContext symbolContext)
            {
                OnSymbolActionCore(symbolContext.Symbol, symbolContext.ReportDiagnostic);
            }

            internal void OnPropertyAction(SymbolAnalysisContext symbolContext)
            {
                // If a property is non-implicit, but it's accessors *are* implicit,
                // then we will not get called back for the accessor methods.  Add
                // those methods explicitly in this case.  This happens, for example,
                // in VB with properties like:
                //
                //      public readonly property A as Integer
                //
                // In this case, the getter/setters are both implicit, and will not
                // trigger the callback to analyze them.  So we manually do it ourselves.
                var property = (IPropertySymbol)symbolContext.Symbol;
                if (!property.IsImplicitlyDeclared)
                {
                    this.CheckPropertyAccessor(symbolContext, property.GetMethod);
                    this.CheckPropertyAccessor(symbolContext, property.SetMethod);
                }
            }

            private void CheckPropertyAccessor(SymbolAnalysisContext symbolContext, IMethodSymbol accessor)
            {
                if (accessor == null)
                {
                    return;
                }

                // Only process implicit accessors.  We won't get callbacks for them
                // normally with RegisterSymbolAction.
                if (!accessor.IsImplicitlyDeclared)
                {
                    return;
                }

                if (!this.IsPublicAPI(accessor))
                {
                    return;
                }

                this.OnSymbolActionCore(accessor, symbolContext.ReportDiagnostic, isImplicitlyDeclaredConstructor: false);
            }

            /// <param name="symbol">The symbol to analyze. Will also analyze implicit constructors too.</param>
            /// <param name="reportDiagnostic">Action called to actually report a diagnostic.</param>
            /// <param name="explicitLocation">A location to report the diagnostics for a symbol at. If null, then
            /// the location of the symbol will be used.</param>
            private void OnSymbolActionCore(ISymbol symbol, Action<Diagnostic> reportDiagnostic, Location? explicitLocation = null)
            {
                if (!IsPublicAPI(symbol))
                {
                    return;
                }

                Debug.Assert(!symbol.IsImplicitlyDeclared);
                OnSymbolActionCore(symbol, reportDiagnostic, isImplicitlyDeclaredConstructor: false, explicitLocation: explicitLocation);

                // Handle implicitly declared public constructors.
                if (symbol.Kind == SymbolKind.NamedType)
                {
                    var namedType = (INamedTypeSymbol)symbol;
                    if ((namedType.TypeKind == TypeKind.Class && namedType.InstanceConstructors.Length == 1)
                        || namedType.TypeKind == TypeKind.Struct)
                    {
                        var implicitConstructor = namedType.InstanceConstructors.FirstOrDefault(x => x.IsImplicitlyDeclared);
                        if (implicitConstructor != null)
                        {
                            OnSymbolActionCore(implicitConstructor, reportDiagnostic, isImplicitlyDeclaredConstructor: true, explicitLocation: explicitLocation);
                        }
                    }
                }
            }

            private static string WithObliviousMarker(string name)
            {
                return ObliviousMarker + name;
            }

            /// <param name="symbol">The symbol to analyze.</param>
            /// <param name="reportDiagnostic">Action called to actually report a diagnostic.</param>
            /// <param name="isImplicitlyDeclaredConstructor">If the symbol is an implicitly declared constructor.</param>
            /// <param name="explicitLocation">A location to report the diagnostics for a symbol at. If null, then
            /// the location of the symbol will be used.</param>
            private void OnSymbolActionCore(ISymbol symbol, Action<Diagnostic> reportDiagnostic, bool isImplicitlyDeclaredConstructor, Location? explicitLocation = null)
            {
                Debug.Assert(IsPublicAPI(symbol));

                ApiName publicApiName = GetPublicApiName(symbol);
                _visitedApiList.TryAdd(publicApiName.Name, default);
                _visitedApiList.TryAdd(WithObliviousMarker(publicApiName.Name), default);
                _visitedApiList.TryAdd(publicApiName.NameWithNullability, default);
                _visitedApiList.TryAdd(WithObliviousMarker(publicApiName.NameWithNullability), default);

                List<Location> locationsToReport = new List<Location>();

                if (explicitLocation != null)
                {
                    locationsToReport.Add(explicitLocation);
                }
                else
                {
                    var locations = isImplicitlyDeclaredConstructor ? symbol.ContainingType.Locations : symbol.Locations;
                    locationsToReport.AddRange(locations.Where(l => l.IsInSource));
                }

                ApiLine foundApiLine;
                bool symbolUsesOblivious = false;
                if (_useNullability)
                {
                    symbolUsesOblivious = UsesOblivious(symbol);
                    if (symbolUsesOblivious)
                    {
                        reportObliviousApi(symbol);
                    }

                    var hasPublicApiEntryWithNullability = _publicApiMap.TryGetValue(publicApiName.NameWithNullability, out foundApiLine);

                    var hasPublicApiEntryWithNullabilityAndOblivious =
                        !hasPublicApiEntryWithNullability &&
                        symbolUsesOblivious &&
                        _publicApiMap.TryGetValue(WithObliviousMarker(publicApiName.NameWithNullability), out foundApiLine);

                    if (!hasPublicApiEntryWithNullability && !hasPublicApiEntryWithNullabilityAndOblivious)
                    {
                        var hasPublicApiEntryWithoutNullability = _publicApiMap.TryGetValue(publicApiName.Name, out foundApiLine);

                        var hasPublicApiEntryWithoutNullabilityButOblivious =
                            !hasPublicApiEntryWithoutNullability &&
                            _publicApiMap.TryGetValue(WithObliviousMarker(publicApiName.Name), out foundApiLine);

                        if (!hasPublicApiEntryWithoutNullability && !hasPublicApiEntryWithoutNullabilityButOblivious)
                        {
                            reportDeclareNewApi(symbol, isImplicitlyDeclaredConstructor, withObliviousIfNeeded(publicApiName.NameWithNullability));
                        }
                        else
                        {
                            reportAnnotateApi(symbol, isImplicitlyDeclaredConstructor, publicApiName, foundApiLine.IsShippedApi);
                        }
                    }
                    else if (hasPublicApiEntryWithNullability && symbolUsesOblivious)
                    {
                        reportAnnotateApi(symbol, isImplicitlyDeclaredConstructor, publicApiName, foundApiLine.IsShippedApi);
                    }
                }
                else
                {
                    var hasPublicApiEntryWithoutNullability = _publicApiMap.TryGetValue(publicApiName.Name, out foundApiLine);
                    if (!hasPublicApiEntryWithoutNullability)
                    {
                        reportDeclareNewApi(symbol, isImplicitlyDeclaredConstructor, publicApiName.Name);
                    }

                    if (publicApiName.Name != publicApiName.NameWithNullability)
                    {
                        // '#nullable enable' would be useful and should be set
                        reportDiagnosticAtLocations(ShouldAnnotateApiFilesRule, ImmutableDictionary<string, string>.Empty);
                    }
                }

                if (symbol.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)symbol;
                    var isMethodShippedApi = foundApiLine?.IsShippedApi == true;

                    // Check if a public API is a constructor that makes this class instantiable, even though the base class
                    // is not instantiable. That API pattern is not allowed, because it causes protected members of
                    // the base class, which are not considered public APIs, to be exposed to subclasses of this class.
                    if (!isMethodShippedApi &&
                        method.MethodKind == MethodKind.Constructor &&
                        method.ContainingType.TypeKind == TypeKind.Class &&
                        !method.ContainingType.IsSealed &&
                        method.ContainingType.BaseType != null &&
                        IsPublicApiCore(method.ContainingType.BaseType) &&
                        !CanTypeBeExtendedPublicly(method.ContainingType.BaseType))
                    {
                        string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                        ImmutableDictionary<string, string> propertyBag = ImmutableDictionary<string, string>.Empty;
                        var locations = isImplicitlyDeclaredConstructor ? method.ContainingType.Locations : method.Locations;
                        reportDiagnostic(Diagnostic.Create(ExposedNoninstantiableType, locations[0], propertyBag, errorMessageName));
                    }

                    // Flag public API with optional parameters that violate backcompat requirements: https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md.
                    if (method.HasOptionalParameters())
                    {
                        foreach (var overload in method.GetOverloads())
                        {
                            if (!IsPublicAPI(overload))
                            {
                                continue;
                            }

                            // Don't flag overloads which have identical params (e.g. overloading a generic and non-generic method with same parameter types).
                            if (overload.Parameters.Length == method.Parameters.Length &&
                                overload.Parameters.Select(p => p.Type).SequenceEqual(method.Parameters.Select(p => p.Type)))
                            {
                                continue;
                            }

                            // RS0026: Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.
                            var overloadHasOptionalParams = overload.HasOptionalParameters();
                            if (overloadHasOptionalParams)
                            {
                                // Flag only if 'method' is a new unshipped API with optional parameters.
                                if (!isMethodShippedApi)
                                {
                                    string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                    reportDiagnosticAtLocations(AvoidMultipleOverloadsWithOptionalParameters, ImmutableDictionary<string, string>.Empty, errorMessageName, AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri);
                                    break;
                                }
                            }

                            // RS0027: Symbol '{0}' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.
                            if (method.Parameters.Length <= overload.Parameters.Length)
                            {
                                // 'method' is unshipped: Flag regardless of whether the overload is shipped/unshipped.
                                // 'method' is shipped:   Flag only if overload is unshipped and has no optional parameters (overload will already be flagged with RS0026)
                                if (!isMethodShippedApi)
                                {
                                    string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                    reportDiagnosticAtLocations(OverloadWithOptionalParametersShouldHaveMostParameters, ImmutableDictionary<string, string>.Empty, errorMessageName, OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri);
                                    break;
                                }
                                else if (!overloadHasOptionalParams)
                                {
                                    var overloadPublicApiName = GetPublicApiName(overload);
                                    var isOverloadUnshipped = !lookupPublicApi(overloadPublicApiName, out ApiLine overloadPublicApiLine) ||
                                        !overloadPublicApiLine.IsShippedApi;
                                    if (isOverloadUnshipped)
                                    {
                                        string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                        reportDiagnosticAtLocations(OverloadWithOptionalParametersShouldHaveMostParameters, ImmutableDictionary<string, string>.Empty, errorMessageName, OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return;

                // local functions
                void reportDiagnosticAtLocations(DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> propertyBag, params object[] args)
                {
                    foreach (var location in locationsToReport)
                    {
                        reportDiagnostic(Diagnostic.Create(descriptor, location, propertyBag, args));
                    }
                }

                void reportDeclareNewApi(ISymbol symbol, bool isImplicitlyDeclaredConstructor, string publicApiName)
                {
                    // TODO: workaround for https://github.com/dotnet/wpf/issues/2690
                    if (publicApiName is "XamlGeneratedNamespace.GeneratedInternalTypeHelper" or
                        "XamlGeneratedNamespace.GeneratedInternalTypeHelper.GeneratedInternalTypeHelper() -> void")
                    {
                        return;
                    }

                    // Unshipped public API with no entry in public API file - report diagnostic.
                    string errorMessageName = GetErrorMessageName(symbol, isImplicitlyDeclaredConstructor);
                    // Compute public API names for any stale siblings to remove from unshipped text (e.g. during signature change of unshipped public API).
                    var siblingPublicApiNamesToRemove = GetSiblingNamesToRemoveFromUnshippedText(symbol);
                    ImmutableDictionary<string, string> propertyBag = ImmutableDictionary<string, string>.Empty
                        .Add(PublicApiNamePropertyBagKey, publicApiName)
                        .Add(MinimalNamePropertyBagKey, errorMessageName)
                        .Add(PublicApiNamesOfSiblingsToRemovePropertyBagKey, siblingPublicApiNamesToRemove);

                    reportDiagnosticAtLocations(DeclareNewApiRule, propertyBag, errorMessageName);
                }

                void reportAnnotateApi(ISymbol symbol, bool isImplicitlyDeclaredConstructor, ApiName publicApiName, bool isShipped)
                {
                    // Public API missing annotations in public API file - report diagnostic.
                    string errorMessageName = GetErrorMessageName(symbol, isImplicitlyDeclaredConstructor);
                    ImmutableDictionary<string, string> propertyBag = ImmutableDictionary<string, string>.Empty
                        .Add(PublicApiNamePropertyBagKey, publicApiName.Name)
                        .Add(PublicApiNameWithNullabilityPropertyBagKey, withObliviousIfNeeded(publicApiName.NameWithNullability))
                        .Add(MinimalNamePropertyBagKey, errorMessageName)
                        .Add(PublicApiIsShippedPropertyBagKey, isShipped ? "true" : "false");

                    reportDiagnosticAtLocations(AnnotateApiRule, propertyBag, errorMessageName);
                }

                string withObliviousIfNeeded(string name)
                {
                    return symbolUsesOblivious ? WithObliviousMarker(name) : name;
                }

                void reportObliviousApi(ISymbol symbol)
                {
                    // Public API using oblivious types in public API file - report diagnostic.
                    string errorMessageName = GetErrorMessageName(symbol, isImplicitlyDeclaredConstructor);

                    reportDiagnosticAtLocations(ObliviousApiRule, ImmutableDictionary<string, string>.Empty, errorMessageName);
                }

                bool lookupPublicApi(ApiName overloadPublicApiName, out ApiLine overloadPublicApiLine)
                {
                    if (_useNullability)
                    {
                        return _publicApiMap.TryGetValue(overloadPublicApiName.NameWithNullability, out overloadPublicApiLine) ||
                            _publicApiMap.TryGetValue(WithObliviousMarker(overloadPublicApiName.NameWithNullability), out overloadPublicApiLine) ||
                            _publicApiMap.TryGetValue(overloadPublicApiName.Name, out overloadPublicApiLine);
                    }
                    else
                    {
                        return _publicApiMap.TryGetValue(overloadPublicApiName.Name, out overloadPublicApiLine);
                    }
                }
            }

            private static string GetErrorMessageName(ISymbol symbol, bool isImplicitlyDeclaredConstructor)
            {
                if (symbol.IsImplicitlyDeclared &&
                    symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.AssociatedSymbol is IPropertySymbol property)
                {
                    var formatString = symbol.Equals(property.GetMethod)
                        ? PublicApiAnalyzerResources.PublicImplicitGetAccessor
                        : PublicApiAnalyzerResources.PublicImplicitSetAccessor;

                    return string.Format(CultureInfo.CurrentCulture, formatString, property.Name);
                }

                return isImplicitlyDeclaredConstructor ?
                    string.Format(CultureInfo.CurrentCulture, PublicApiAnalyzerResources.PublicImplicitConstructorErrorMessageName, symbol.ContainingSymbol.ToDisplayString(ShortSymbolNameFormat)) :
                    symbol.ToDisplayString(ShortSymbolNameFormat);
            }

            private string GetSiblingNamesToRemoveFromUnshippedText(ISymbol symbol)
            {
                // Don't crash the analyzer if we are unable to determine stale entries to remove in public API text.
                try
                {
                    return GetSiblingNamesToRemoveFromUnshippedTextCore(symbol);
                }
#pragma warning disable CA1031 // Do not catch general exception types - https://github.com/dotnet/roslyn-analyzers/issues/2181
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    return string.Empty;
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            private string GetSiblingNamesToRemoveFromUnshippedTextCore(ISymbol symbol)
            {
                // Compute all sibling names that must be removed from unshipped text, as they are no longer public or have been changed.
                if (symbol.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol)
                {
                    // First get the lines in the unshipped text for siblings of the symbol:
                    //  (a) Contains Public API name of containing symbol.
                    //  (b) Doesn't contain Public API name of nested types/namespaces of containing symbol.
                    var containingSymbolPublicApiName = GetPublicApiName(containingSymbol);

                    var nestedNamespaceOrTypeMembers = containingSymbol.GetMembers().OfType<INamespaceOrTypeSymbol>().ToImmutableArray();
                    var nestedNamespaceOrTypesPublicApiNames = new List<string>(nestedNamespaceOrTypeMembers.Length);
                    foreach (var nestedNamespaceOrType in nestedNamespaceOrTypeMembers)
                    {
                        var nestedNamespaceOrTypePublicApiName = GetPublicApiName(nestedNamespaceOrType).Name;
                        nestedNamespaceOrTypesPublicApiNames.Add(nestedNamespaceOrTypePublicApiName);
                    }

                    var publicApiLinesForSiblingsOfSymbol = new HashSet<string>();
                    foreach (var apiLine in _unshippedData.ApiList)
                    {
                        var apiLineText = apiLine.Text;
                        if (apiLineText == containingSymbolPublicApiName.Name)
                        {
                            // Not a sibling of symbol.
                            continue;
                        }

                        if (!ContainsPublicApiName(apiLineText, containingSymbolPublicApiName.Name + "."))
                        {
                            // Doesn't contain containingSymbol public API name - not a sibling of symbol.
                            continue;
                        }

                        var containedInNestedMember = false;
                        foreach (var nestedNamespaceOrTypePublicApiName in nestedNamespaceOrTypesPublicApiNames)
                        {
                            if (ContainsPublicApiName(apiLineText, nestedNamespaceOrTypePublicApiName + "."))
                            {
                                // Belongs to a nested type/namespace in containingSymbol - not a sibling of symbol.
                                containedInNestedMember = true;
                                break;
                            }
                        }

                        if (containedInNestedMember)
                        {
                            continue;
                        }

                        publicApiLinesForSiblingsOfSymbol.Add(apiLineText);
                    }

                    // Now remove the lines for siblings which are still public APIs - we don't want to remove those.
                    if (publicApiLinesForSiblingsOfSymbol.Count > 0)
                    {
                        var siblings = containingSymbol.GetMembers();
                        foreach (var sibling in siblings)
                        {
                            if (sibling.IsImplicitlyDeclared)
                            {
                                if (!sibling.IsConstructor())
                                {
                                    continue;
                                }
                            }
                            else if (!IsPublicAPI(sibling))
                            {
                                continue;
                            }

                            var siblingPublicApiName = GetPublicApiName(sibling);
                            publicApiLinesForSiblingsOfSymbol.Remove(siblingPublicApiName.Name);
                            publicApiLinesForSiblingsOfSymbol.Remove(siblingPublicApiName.NameWithNullability);
                            publicApiLinesForSiblingsOfSymbol.Remove(WithObliviousMarker(siblingPublicApiName.NameWithNullability));
                        }

                        // Join all the symbols names with a special separator.
                        return string.Join(PublicApiNamesOfSiblingsToRemovePropertyBagValueSeparator, publicApiLinesForSiblingsOfSymbol);
                    }
                }

                return string.Empty;
            }

            private static bool UsesOblivious(ISymbol symbol)
            {
                if (symbol.Kind == SymbolKind.NamedType)
                {
                    return ObliviousDetector.VisitNamedTypeDeclaration((INamedTypeSymbol)symbol);
                }

                return ObliviousDetector.Instance.Visit(symbol);
            }

            private ApiName GetPublicApiName(ISymbol symbol)
            {
                return new ApiName(
                    getPublicApiString(symbol, s_publicApiFormat),
                    getPublicApiString(symbol, s_publicApiFormatWithNullability));

                string getPublicApiString(ISymbol symbol, SymbolDisplayFormat format)
                {
                    string publicApiName = symbol.ToDisplayString(format);

                    ITypeSymbol? memberType = null;
                    if (symbol is IMethodSymbol method)
                    {
                        memberType = method.ReturnType;
                    }
                    else if (symbol is IPropertySymbol property)
                    {
                        memberType = property.Type;
                    }
                    else if (symbol is IEventSymbol @event)
                    {
                        memberType = @event.Type;
                    }
                    else if (symbol is IFieldSymbol field)
                    {
                        memberType = field.Type;
                    }

                    if (memberType != null)
                    {
                        publicApiName = publicApiName + " -> " + memberType.ToDisplayString(format);
                    }

                    if (((symbol as INamespaceSymbol)?.IsGlobalNamespace).GetValueOrDefault())
                    {
                        return string.Empty;
                    }

                    if (symbol.ContainingAssembly != null && !symbol.ContainingAssembly.Equals(_compilation.Assembly))
                    {
                        publicApiName += $" (forwarded, contained in {symbol.ContainingAssembly.Name})";
                    }

                    return publicApiName;
                }
            }

            private static bool ContainsPublicApiName(string apiLineText, string publicApiNameToSearch)
            {
                apiLineText = apiLineText.Trim(ObliviousMarker);

                // Ensure we don't search in parameter list/return type.
                var indexOfParamsList = apiLineText.IndexOf('(');
                if (indexOfParamsList > 0)
                {
                    apiLineText = apiLineText.Substring(0, indexOfParamsList);
                }
                else
                {
                    var indexOfReturnType = apiLineText.IndexOf("->", StringComparison.Ordinal);
                    if (indexOfReturnType > 0)
                    {
                        apiLineText = apiLineText.Substring(0, indexOfReturnType);
                    }
                }

                // Ensure that we don't have any leading characters in matched substring, apart from whitespace.
                var index = apiLineText.IndexOf(publicApiNameToSearch, StringComparison.Ordinal);
                return index == 0 || (index > 0 && apiLineText[index - 1] == ' ');
            }

            internal void OnCompilationEnd(CompilationAnalysisContext context)
            {
                ProcessTypeForwardedAttributes(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
                List<ApiLine> deletedApiList = GetDeletedApiList();
                foreach (ApiLine cur in deletedApiList)
                {
                    LinePositionSpan linePositionSpan = cur.SourceText.Lines.GetLinePositionSpan(cur.Span);
                    Location location = Location.Create(cur.Path, cur.Span, linePositionSpan);
                    ImmutableDictionary<string, string> propertyBag = ImmutableDictionary<string, string>.Empty.Add(PublicApiNamePropertyBagKey, cur.Text);
                    context.ReportDiagnostic(Diagnostic.Create(RemoveDeletedApiRule, location, propertyBag, cur.Text));
                }
            }

            private void ProcessTypeForwardedAttributes(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                var typeForwardedToAttribute = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesTypeForwardedToAttribute);

                if (typeForwardedToAttribute != null)
                {
                    foreach (var attribute in compilation.Assembly.GetAttributes())
                    {
                        if (attribute.AttributeClass.Equals(typeForwardedToAttribute))
                        {
                            if (attribute.AttributeConstructor.Parameters.Length == 1 &&
                                attribute.ConstructorArguments.Length == 1)
                            {
                                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol forwardedType)
                                {
                                    VisitForwardedTypeRecursively(forwardedType, reportDiagnostic, attribute.ApplicationSyntaxReference.GetSyntax(cancellationToken).GetLocation(), cancellationToken);
                                }
                            }
                        }
                    }
                }
            }

            private void VisitForwardedTypeRecursively(ISymbol symbol, Action<Diagnostic> reportDiagnostic, Location typeForwardedAttributeLocation, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnSymbolActionCore(symbol, reportDiagnostic, typeForwardedAttributeLocation);

                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var nestedType in namedTypeSymbol.GetTypeMembers())
                    {
                        VisitForwardedTypeRecursively(nestedType, reportDiagnostic, typeForwardedAttributeLocation, cancellationToken);
                    }

                    foreach (var member in namedTypeSymbol.GetMembers())
                    {
                        if (!(member.IsImplicitlyDeclared && member.IsDefaultConstructor()))
                        {
                            VisitForwardedTypeRecursively(member, reportDiagnostic, typeForwardedAttributeLocation, cancellationToken);
                        }
                    }
                }
            }

            /// <summary>
            /// Calculated the set of APIs which have been deleted but not yet documented.
            /// </summary>
            /// <returns></returns>
            internal List<ApiLine> GetDeletedApiList()
            {
                var list = new List<ApiLine>();
                foreach (KeyValuePair<string, ApiLine> pair in _publicApiMap)
                {
                    if (_visitedApiList.ContainsKey(pair.Key))
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

            private bool IsPublicAPI(ISymbol symbol)
            {
                if (symbol is IMethodSymbol methodSymbol && s_ignorableMethodKinds.Contains(methodSymbol.MethodKind))
                {
                    return false;
                }

                // We don't consider properties to be public APIs. Instead, property getters and setters
                // (which are IMethodSymbols) are considered as public APIs.
                if (symbol is IPropertySymbol)
                {
                    return false;
                }

                return IsPublicApiCore(symbol);
            }

            private bool IsPublicApiCore(ISymbol symbol)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                        return symbol.ContainingType == null || IsPublicApiCore(symbol.ContainingType);
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        // Protected symbols must have parent types (that is, top-level protected
                        // symbols are not allowed.
                        return
                            symbol.ContainingType != null &&
                            IsPublicApiCore(symbol.ContainingType) &&
                            CanTypeBeExtendedPublicly(symbol.ContainingType);
                    default:
                        return false;
                }
            }

            private bool CanTypeBeExtendedPublicly(ITypeSymbol type)
            {
                return _typeCanBeExtendedCache.GetOrAdd(type, t => CanTypeBeExtendedPubliclyImpl(t));
            }

            private static bool CanTypeBeExtendedPubliclyImpl(ITypeSymbol type)
            {
                // a type can be extended publicly if (1) it isn't sealed, and (2) it has some constructor that is
                // not internal, private or protected&internal
                return !type.IsSealed &&
                    type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Any(
                        m => m.DeclaredAccessibility is not Accessibility.Internal and not Accessibility.Private and not Accessibility.ProtectedAndInternal
                    );
            }

            /// <summary>
            /// Various Visit* methods return true if an oblivious reference type is detected.
            /// </summary>
            private sealed class ObliviousDetector : SymbolVisitor<bool>
            {
                // We need to ignore top-level nullability for outer types: `Outer<...>.Inner`
                private static readonly ObliviousDetector IgnoreTopLevelNullabilityInstance = new ObliviousDetector(ignoreTopLevelNullability: true);

                public static readonly ObliviousDetector Instance = new ObliviousDetector(ignoreTopLevelNullability: false);

                private readonly bool _ignoreTopLevelNullability;

                private ObliviousDetector(bool ignoreTopLevelNullability)
                {
                    _ignoreTopLevelNullability = ignoreTopLevelNullability;
                }

                public override bool VisitField(IFieldSymbol symbol)
                {
                    return Visit(symbol.Type);
                }

                public override bool VisitMethod(IMethodSymbol symbol)
                {
                    if (Visit(symbol.ReturnType))
                    {
                        return true;
                    }

                    foreach (var parameter in symbol.Parameters)
                    {
                        if (Visit(parameter.Type))
                        {
                            return true;
                        }
                    }

                    foreach (var typeParameter in symbol.TypeParameters)
                    {
                        if (CheckTypeParameterConstraints(typeParameter))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                /// <summary>This is visiting type references, not type definitions (that's done elsewhere).</summary>
                public override bool VisitNamedType(INamedTypeSymbol symbol)
                {
                    if (!_ignoreTopLevelNullability)
                    {
                        if (symbol.IsReferenceType &&
                            symbol.NullableAnnotation() == NullableAnnotation.None)
                        {
                            return true;
                        }
                    }

                    if (symbol.ContainingType is INamedTypeSymbol containing)
                    {
                        if (IgnoreTopLevelNullabilityInstance.Visit(containing))
                        {
                            return true;
                        }
                    }

                    foreach (var typeArgument in symbol.TypeArguments)
                    {
                        if (Instance.Visit(typeArgument))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                public override bool VisitArrayType(IArrayTypeSymbol symbol)
                {
                    if (symbol.NullableAnnotation() == NullableAnnotation.None)
                    {
                        return true;
                    }

                    return Visit(symbol.ElementType);
                }

                public override bool VisitPointerType(IPointerTypeSymbol symbol)
                {
                    return Visit(symbol.PointedAtType);
                }

                /// <summary>This only checks the use of a type parameter. We're checking their definition (looking at type constraints) elsewhere.</summary>
                public override bool VisitTypeParameter(ITypeParameterSymbol symbol)
                {
                    if (symbol.IsReferenceType &&
                        symbol.NullableAnnotation() == NullableAnnotation.None)
                    {
                        // Example:
                        // I<TReferenceType~>
                        return true;
                    }

                    return false;
                }

                /// <summary>This is checking the definition of a type (as opposed to its usage).</summary>
                public static bool VisitNamedTypeDeclaration(INamedTypeSymbol symbol)
                {
                    foreach (var typeParameter in symbol.TypeParameters)
                    {
                        if (CheckTypeParameterConstraints(typeParameter))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                private static bool CheckTypeParameterConstraints(ITypeParameterSymbol symbol)
                {
                    if (symbol.HasReferenceTypeConstraint() &&
                        symbol.ReferenceTypeConstraintNullableAnnotation() == NullableAnnotation.None)
                    {
                        // where T : class~
                        return true;
                    }

                    foreach (var constraintType in symbol.ConstraintTypes)
                    {
                        if (Instance.Visit(constraintType))
                        {
                            // Examples:
                            // where T : SomeReferenceType~
                            // where T : I<SomeReferenceType~>
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
    }
}

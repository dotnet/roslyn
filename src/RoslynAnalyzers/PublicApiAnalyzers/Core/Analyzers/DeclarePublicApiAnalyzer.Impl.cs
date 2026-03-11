// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
    public partial class DeclarePublicApiAnalyzer : DiagnosticAnalyzer
    {
        private sealed record AdditionalFileInfo(SourceText SourceText, bool IsShippedApi)
        {
            public string GetPath(ImmutableDictionary<AdditionalText, SourceText> additionalFiles)
            {
                foreach (var (additionalText, sourceText) in additionalFiles)
                {
                    if (SourceText == sourceText)
                        return additionalText.Path;
                }

                throw new InvalidOperationException();
            }
        }

        private readonly record struct ApiLine(string Text, TextSpan Span, AdditionalFileInfo FileInfo)
        {
            public bool IsDefault => FileInfo == null;

            public SourceText SourceText => FileInfo.SourceText;
            public bool IsShippedApi => FileInfo.IsShippedApi;

            public string GetPath(ImmutableDictionary<AdditionalText, SourceText> additionalFiles)
                => FileInfo.GetPath(additionalFiles);

            public Location GetLocation(ImmutableDictionary<AdditionalText, SourceText> additionalFiles)
            {
                LinePositionSpan linePositionSpan = SourceText.Lines.GetLinePositionSpan(Span);
                return Location.Create(GetPath(additionalFiles), Span, linePositionSpan);
            }
        }

        private readonly record struct RemovedApiLine(string Text, ApiLine ApiLine);

        private readonly record struct ApiName(string Name, string NameWithNullability);

        /// <param name="NullableLineNumber">Number for the max line where #nullable enable was found (-1 otherwise)</param>
        private sealed record ApiData(ImmutableArray<ApiLine> ApiList, ImmutableArray<RemovedApiLine> RemovedApiList, int NullableLineNumber)
        {
            public static readonly ApiData Empty = new(ImmutableArray<ApiLine>.Empty, ImmutableArray<RemovedApiLine>.Empty, NullableLineNumber: -1);
        }

        private sealed class Impl
        {
            private static readonly ImmutableArray<MethodKind> s_ignorableMethodKinds
                = ImmutableArray.Create(MethodKind.EventAdd, MethodKind.EventRemove);

            private static readonly SymbolDisplayFormat s_namespaceFormat = new(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            private readonly Compilation _compilation;
            private readonly ImmutableDictionary<AdditionalText, SourceText> _additionalFiles;
            private readonly ApiData _unshippedData;
            private readonly bool _useNullability;
            private readonly bool _isPublic;
            private readonly ConcurrentDictionary<(ITypeSymbol Type, bool IsPublic), bool> _typeCanBeExtendedCache = new();
            private readonly ConcurrentDictionary<string, UnusedValue> _visitedApiList = new(StringComparer.Ordinal);
            private readonly ConcurrentDictionary<SyntaxTree, ImmutableArray<string>> _skippedNamespacesCache = new();
            private readonly Lazy<IReadOnlyDictionary<string, ApiLine>> _apiMap;
            private readonly AnalyzerOptions _analyzerOptions;

            internal Impl(Compilation compilation, ImmutableDictionary<AdditionalText, SourceText> additionalFiles, ApiData shippedData, ApiData unshippedData, bool isPublic, AnalyzerOptions analyzerOptions)
            {
                _compilation = compilation;
                _additionalFiles = additionalFiles;
                _useNullability = shippedData.NullableLineNumber >= 0 || unshippedData.NullableLineNumber >= 0;
                _unshippedData = unshippedData;

                _apiMap = new Lazy<IReadOnlyDictionary<string, ApiLine>>(() => CreateApiMap(shippedData, unshippedData));
                _isPublic = isPublic;
                _analyzerOptions = analyzerOptions;

                static IReadOnlyDictionary<string, ApiLine> CreateApiMap(ApiData shippedData, ApiData unshippedData)
                {
                    // Defer allocating/creating the apiMap until it's needed as there are many cases where it's never used
                    //   and can be fairly large.
                    var publicApiMap = new Dictionary<string, ApiLine>(shippedData.ApiList.Length + unshippedData.ApiList.Length, StringComparer.Ordinal);
                    foreach (ApiLine cur in shippedData.ApiList)
                    {
                        publicApiMap.Add(cur.Text, cur);
                    }

                    foreach (ApiLine cur in unshippedData.ApiList)
                    {
                        publicApiMap.Add(cur.Text, cur);
                    }

                    return publicApiMap;
                }
            }

            internal void OnSymbolAction(SymbolAnalysisContext symbolContext)
            {
                var obsoleteAttribute = symbolContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
                OnSymbolActionCore(symbolContext.Symbol, symbolContext.ReportDiagnostic, obsoleteAttribute, symbolContext.CancellationToken);
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

            private void CheckPropertyAccessor(SymbolAnalysisContext symbolContext, IMethodSymbol? accessor)
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

                if (!this.IsTrackedAPI(accessor, symbolContext.CancellationToken))
                {
                    return;
                }

                var obsoleteAttribute = symbolContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
                this.OnSymbolActionCore(accessor, symbolContext.ReportDiagnostic, isImplicitlyDeclaredConstructor: false, obsoleteAttribute, symbolContext.CancellationToken);
            }

            /// <param name="symbol">The symbol to analyze. Will also analyze implicit constructors too.</param>
            /// <param name="reportDiagnostic">Action called to actually report a diagnostic.</param>
            /// <param name="explicitLocation">A location to report the diagnostics for a symbol at. If null, then
            /// the location of the symbol will be used.</param>
            private void OnSymbolActionCore(ISymbol symbol, Action<Diagnostic> reportDiagnostic, INamedTypeSymbol? obsoleteAttribute, CancellationToken cancellationToken, Location? explicitLocation = null)
            {
                if (!IsTrackedAPI(symbol, cancellationToken))
                {
                    return;
                }

                Debug.Assert(!symbol.IsImplicitlyDeclared);
                OnSymbolActionCore(symbol, reportDiagnostic, isImplicitlyDeclaredConstructor: false, obsoleteAttribute, cancellationToken, explicitLocation: explicitLocation);

                // Handle implicitly declared public constructors.
                if (symbol is INamedTypeSymbol namedType)
                {
                    IMethodSymbol? implicitConstructor = null;
                    if (namedType is { TypeKind: TypeKind.Class, InstanceConstructors.Length: 1 } or { TypeKind: TypeKind.Struct })
                    {
                        implicitConstructor = namedType.InstanceConstructors.FirstOrDefault(x => x.IsImplicitlyDeclared);
                        if (implicitConstructor != null)
                            OnSymbolActionCore(implicitConstructor, reportDiagnostic, isImplicitlyDeclaredConstructor: true, obsoleteAttribute, cancellationToken, explicitLocation: explicitLocation);
                    }

                    // Ensure that any implicitly declared members of a record are emitted as well.
                    foreach (var member in namedType.GetMembers())
                    {
                        // Handled above.
                        if (member.Equals(implicitConstructor))
                            continue;

                        if (IsTrackedAPI(member, cancellationToken) && member is IMethodSymbol { IsImplicitlyDeclared: true } method)
                        {
                            // Record property accessors (for `record X(int P)`) are considered implicitly declared.
                            // However, we still handle the normal property symbol for that through our standard symbol
                            // callbacks.  So we don't need to process those here.
                            //
                            // We do, however, need to process any implicit accessors for *implicit* properties. For
                            // example, for the implicit `virtual Type EqualityContract { get; }` member
                            if (method.MethodKind is not (MethodKind.PropertyGet or MethodKind.PropertySet) ||
                                method is { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet, AssociatedSymbol.IsImplicitlyDeclared: true })
                            {
                                OnSymbolActionCore(member, reportDiagnostic, isImplicitlyDeclaredConstructor: false, obsoleteAttribute, cancellationToken, explicitLocation: explicitLocation);
                            }
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
            private void OnSymbolActionCore(ISymbol symbol, Action<Diagnostic> reportDiagnostic, bool isImplicitlyDeclaredConstructor, INamedTypeSymbol? obsoleteAttribute, CancellationToken cancellationToken, Location? explicitLocation = null)
            {
                Debug.Assert(IsTrackedAPI(symbol, cancellationToken));

                ApiName publicApiName = GetApiName(symbol);
                _visitedApiList.TryAdd(publicApiName.Name, default);
                _visitedApiList.TryAdd(WithObliviousMarker(publicApiName.Name), default);
                _visitedApiList.TryAdd(publicApiName.NameWithNullability, default);
                _visitedApiList.TryAdd(WithObliviousMarker(publicApiName.NameWithNullability), default);

                List<Location> locationsToReport = [];
                IReadOnlyDictionary<string, ApiLine> apiMap = _apiMap.Value;

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
                    if (symbolUsesOblivious && !symbol.IsImplicitlyDeclared)
                    {
                        reportObliviousApi(symbol);
                    }

                    var hasApiEntryWithNullability = apiMap.TryGetValue(publicApiName.NameWithNullability, out foundApiLine);

                    var hasApiEntryWithNullabilityAndOblivious =
                        !hasApiEntryWithNullability &&
                        symbolUsesOblivious &&
                        apiMap.TryGetValue(WithObliviousMarker(publicApiName.NameWithNullability), out foundApiLine);

                    if (!hasApiEntryWithNullability && !hasApiEntryWithNullabilityAndOblivious)
                    {
                        var hasApiEntryWithoutNullability = apiMap.TryGetValue(publicApiName.Name, out foundApiLine);

                        var hasApiEntryWithoutNullabilityButOblivious =
                            !hasApiEntryWithoutNullability &&
                            apiMap.TryGetValue(WithObliviousMarker(publicApiName.Name), out foundApiLine);

                        if (!hasApiEntryWithoutNullability && !hasApiEntryWithoutNullabilityButOblivious)
                        {
                            reportDeclareNewApi(symbol, isImplicitlyDeclaredConstructor, withObliviousIfNeeded(publicApiName.NameWithNullability));
                        }
                        else
                        {
                            reportAnnotateApi(symbol, isImplicitlyDeclaredConstructor, publicApiName, foundApiLine.IsShippedApi, foundApiLine.GetPath(_additionalFiles));
                        }
                    }
                    else if (hasApiEntryWithNullability && symbolUsesOblivious)
                    {
                        reportAnnotateApi(symbol, isImplicitlyDeclaredConstructor, publicApiName, foundApiLine.IsShippedApi, foundApiLine.GetPath(_additionalFiles));
                    }
                }
                else
                {
                    var hasApiEntryWithoutNullability = apiMap.TryGetValue(publicApiName.Name, out foundApiLine);
                    if (!hasApiEntryWithoutNullability)
                    {
                        reportDeclareNewApi(symbol, isImplicitlyDeclaredConstructor, publicApiName.Name);
                    }

                    if (publicApiName.Name != publicApiName.NameWithNullability)
                    {
                        // '#nullable enable' would be useful and should be set
                        reportDiagnosticAtLocations(GetDiagnostic(ShouldAnnotatePublicApiFilesRule, ShouldAnnotateInternalApiFilesRule), ImmutableDictionary<string, string?>.Empty);
                    }
                }

                if (symbol.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)symbol;
                    var isMethodShippedApi = !foundApiLine.IsDefault && foundApiLine.IsShippedApi;

                    // Check if a public API is a constructor that makes this class instantiable, even though the base class
                    // is not instantiable. That API pattern is not allowed, because it causes protected members of
                    // the base class, which are not considered public APIs, to be exposed to subclasses of this class.
                    if (!isMethodShippedApi &&
                        method.MethodKind == MethodKind.Constructor &&
                        method.ContainingType.TypeKind == TypeKind.Class &&
                        !method.ContainingType.IsSealed &&
                        method.ContainingType.BaseType != null &&
                        IsTrackedApiCore(method.ContainingType.BaseType, cancellationToken) &&
                        !CanTypeBeExtended(method.ContainingType.BaseType))
                    {
                        string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                        ImmutableDictionary<string, string?> propertyBag = ImmutableDictionary<string, string?>.Empty;
                        var locations = isImplicitlyDeclaredConstructor ? method.ContainingType.Locations : method.Locations;
                        reportDiagnostic(Diagnostic.Create(GetDiagnostic(ExposedNoninstantiableTypePublic, ExposedNoninstantiableTypeInternal), locations[0], propertyBag, errorMessageName));
                    }

                    // Flag public API with optional parameters that violate backcompat requirements: https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md.
                    if (method.HasOptionalParameters())
                    {
                        foreach (var overload in method.GetOverloads())
                        {
                            var symbolAccessibility = overload.GetResultantVisibility();
                            var minAccessibility = _isPublic ? SymbolVisibility.Public : SymbolVisibility.Internal;
                            if (symbolAccessibility > minAccessibility)
                            {
                                continue;
                            }

                            // Don't flag overloads which have identical params (e.g. overloading a generic and non-generic method with same parameter types).
                            if (overload.Parameters.Length == method.Parameters.Length &&
                                overload.Parameters.Select(p => p.Type).SequenceEqual(method.Parameters.Select(p => p.Type)))
                            {
                                continue;
                            }

                            // Don't flag obsolete overloads
                            if (overload.HasAnyAttribute(obsoleteAttribute))
                            {
                                continue;
                            }

                            // RS0026: Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.
                            var overloadHasOptionalParams = overload.HasOptionalParameters();
                            // Flag only if 'method' is a new unshipped API with optional parameters.
                            if (overloadHasOptionalParams && !isMethodShippedApi)
                            {
                                string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                var diagnostic = GetDiagnostic(AvoidMultipleOverloadsWithOptionalParametersPublic, AvoidMultipleOverloadsWithOptionalParametersInternal);
                                reportDiagnosticAtLocations(diagnostic, ImmutableDictionary<string, string?>.Empty, errorMessageName, diagnostic.HelpLinkUri);
                                break;
                            }

                            // RS0027: Symbol '{0}' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.
                            if (method.Parameters.Length <= overload.Parameters.Length)
                            {
                                // 'method' is unshipped: Flag regardless of whether the overload is shipped/unshipped.
                                // 'method' is shipped:   Flag only if overload is unshipped and has no optional parameters (overload will already be flagged with RS0026)
                                if (!isMethodShippedApi)
                                {
                                    string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                    var diagnostic = GetDiagnostic(OverloadWithOptionalParametersShouldHaveMostParametersPublic, OverloadWithOptionalParametersShouldHaveMostParametersInternal);
                                    reportDiagnosticAtLocations(diagnostic, ImmutableDictionary<string, string?>.Empty, errorMessageName, diagnostic.HelpLinkUri);
                                    break;
                                }
                                else if (!overloadHasOptionalParams)
                                {
                                    var overloadPublicApiName = GetApiName(overload);
                                    var isOverloadUnshipped = !lookupPublicApi(overloadPublicApiName, out ApiLine overloadPublicApiLine) ||
                                        !overloadPublicApiLine.IsShippedApi;
                                    if (isOverloadUnshipped)
                                    {
                                        string errorMessageName = GetErrorMessageName(method, isImplicitlyDeclaredConstructor);
                                        var diagnostic = GetDiagnostic(OverloadWithOptionalParametersShouldHaveMostParametersPublic, OverloadWithOptionalParametersShouldHaveMostParametersInternal);
                                        reportDiagnosticAtLocations(diagnostic, ImmutableDictionary<string, string?>.Empty, errorMessageName, diagnostic.HelpLinkUri);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return;

                // local functions
                void reportDiagnosticAtLocations(DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?> propertyBag, params object[] args)
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
                    var siblingPublicApiNamesToRemove = GetSiblingNamesToRemoveFromUnshippedText(symbol, cancellationToken);
                    ImmutableDictionary<string, string?> propertyBag = ImmutableDictionary<string, string?>.Empty
                        .Add(ApiNamePropertyBagKey, publicApiName)
                        .Add(MinimalNamePropertyBagKey, errorMessageName)
                        .Add(ApiNamesOfSiblingsToRemovePropertyBagKey, siblingPublicApiNamesToRemove);

                    reportDiagnosticAtLocations(GetDiagnostic(DeclareNewPublicApiRule, DeclareNewInternalApiRule), propertyBag, publicApiName);
                }

                void reportAnnotateApi(ISymbol symbol, bool isImplicitlyDeclaredConstructor, ApiName publicApiName, bool isShipped, string filename)
                {
                    // Public API missing annotations in public API file - report diagnostic.
                    string errorMessageName = GetErrorMessageName(symbol, isImplicitlyDeclaredConstructor);
                    ImmutableDictionary<string, string?> propertyBag = ImmutableDictionary<string, string?>.Empty
                        .Add(ApiNamePropertyBagKey, publicApiName.Name)
                        .Add(ApiNameWithNullabilityPropertyBagKey, withObliviousIfNeeded(publicApiName.NameWithNullability))
                        .Add(MinimalNamePropertyBagKey, errorMessageName)
                        .Add(ApiIsShippedPropertyBagKey, isShipped ? "true" : "false")
                        .Add(FileName, filename);

                    reportDiagnosticAtLocations(GetDiagnostic(AnnotatePublicApiRule, AnnotateInternalApiRule), propertyBag, publicApiName.NameWithNullability);
                }

                string withObliviousIfNeeded(string name)
                {
                    return symbolUsesOblivious ? WithObliviousMarker(name) : name;
                }

                void reportObliviousApi(ISymbol symbol)
                {
                    // Public API using oblivious types in public API file - report diagnostic.
                    string errorMessageName = GetErrorMessageName(symbol, isImplicitlyDeclaredConstructor);

                    reportDiagnosticAtLocations(GetDiagnostic(ObliviousPublicApiRule, ObliviousInternalApiRule), ImmutableDictionary<string, string?>.Empty, errorMessageName);
                }

                bool lookupPublicApi(ApiName overloadPublicApiName, out ApiLine overloadPublicApiLine)
                {
                    if (_useNullability)
                    {
                        return apiMap.TryGetValue(overloadPublicApiName.NameWithNullability, out overloadPublicApiLine) ||
                            apiMap.TryGetValue(WithObliviousMarker(overloadPublicApiName.NameWithNullability), out overloadPublicApiLine) ||
                            apiMap.TryGetValue(overloadPublicApiName.Name, out overloadPublicApiLine);
                    }
                    else
                    {
                        return apiMap.TryGetValue(overloadPublicApiName.Name, out overloadPublicApiLine);
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
                        ? PublicApiAnalyzerResources.ImplicitGetAccessor
                        : PublicApiAnalyzerResources.ImplicitSetAccessor;

                    return string.Format(CultureInfo.CurrentCulture, formatString, property.Name);
                }

                return isImplicitlyDeclaredConstructor ?
                    string.Format(CultureInfo.CurrentCulture, PublicApiAnalyzerResources.ImplicitConstructorErrorMessageName, symbol.ContainingSymbol.ToDisplayString(ShortSymbolNameFormat)) :
                    symbol.ToDisplayString(ShortSymbolNameFormat);
            }

            private string GetSiblingNamesToRemoveFromUnshippedText(ISymbol symbol, CancellationToken cancellationToken)
            {
                // Don't crash the analyzer if we are unable to determine stale entries to remove in public API text.
                try
                {
                    return GetSiblingNamesToRemoveFromUnshippedTextCore(symbol, cancellationToken);
                }
#pragma warning disable CA1031 // Do not catch general exception types - https://github.com/dotnet/roslyn-analyzers/issues/2181
                catch (Exception ex)
                {
                    Debug.Assert(false, ex.Message);
                    return string.Empty;
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            private string GetSiblingNamesToRemoveFromUnshippedTextCore(ISymbol symbol, CancellationToken cancellationToken)
            {
                // Compute all sibling names that must be removed from unshipped text, as they are no longer public or have been changed.
                if (symbol.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol)
                {
                    // First get the lines in the unshipped text for siblings of the symbol:
                    //  (a) Contains API name of containing symbol.
                    //  (b) Doesn't contain API name of nested types/namespaces of containing symbol.
                    var containingSymbolApiName = GetApiName(containingSymbol);

                    var nestedNamespaceOrTypeMembers = containingSymbol.GetMembers().OfType<INamespaceOrTypeSymbol>().ToImmutableArray();
                    var nestedNamespaceOrTypesApiNames = new List<string>(nestedNamespaceOrTypeMembers.Length);
                    foreach (var nestedNamespaceOrType in nestedNamespaceOrTypeMembers)
                    {
                        var nestedNamespaceOrTypeApiName = GetApiName(nestedNamespaceOrType).Name;
                        nestedNamespaceOrTypesApiNames.Add(nestedNamespaceOrTypeApiName);
                    }

                    var publicApiLinesForSiblingsOfSymbol = new HashSet<string>();
                    foreach (var apiLine in _unshippedData.ApiList)
                    {
                        var apiLineText = apiLine.Text;
                        if (apiLineText == containingSymbolApiName.Name)
                        {
                            // Not a sibling of symbol.
                            continue;
                        }

                        if (!ContainsPublicApiName(apiLineText, containingSymbolApiName.Name + "."))
                        {
                            // Doesn't contain containingSymbol public API name - not a sibling of symbol.
                            continue;
                        }

                        var containedInNestedMember = false;
                        foreach (var nestedNamespaceOrTypePublicApiName in nestedNamespaceOrTypesApiNames)
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
                                if (sibling is not IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.PropertyGet or MethodKind.PropertySet })
                                {
                                    continue;
                                }
                            }
                            else if (!IsTrackedAPI(sibling, cancellationToken))
                            {
                                continue;
                            }

                            var siblingPublicApiName = GetApiName(sibling);
                            publicApiLinesForSiblingsOfSymbol.Remove(siblingPublicApiName.Name);
                            publicApiLinesForSiblingsOfSymbol.Remove(siblingPublicApiName.NameWithNullability);
                            publicApiLinesForSiblingsOfSymbol.Remove(WithObliviousMarker(siblingPublicApiName.NameWithNullability));
                        }

                        // Join all the symbols names with a special separator.
                        return string.Join(ApiNamesOfSiblingsToRemovePropertyBagValueSeparator, publicApiLinesForSiblingsOfSymbol);
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

            private ApiName GetApiName(ISymbol symbol)
            {
                var experimentName = getExperimentName(symbol);

                return new ApiName(
                    getApiString(_compilation, symbol, experimentName, s_publicApiFormat),
                    getApiString(_compilation, symbol, experimentName, s_publicApiFormatWithNullability));

                static string? getExperimentName(ISymbol symbol)
                {
                    for (var current = symbol; current is not null; current = current.ContainingSymbol)
                    {
start:
                        foreach (var attribute in current.GetAttributes())
                        {
                            if (attribute.AttributeClass is { Name: "ExperimentalAttribute", ContainingSymbol: INamespaceSymbol { Name: nameof(System.Diagnostics.CodeAnalysis), ContainingNamespace: { Name: nameof(System.Diagnostics), ContainingNamespace: { Name: nameof(System), ContainingNamespace.IsGlobalNamespace: true } } } })
                            {
                                if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Primitive, Type.SpecialType: SpecialType.System_String, Value: string diagnosticId }])
                                    return "???";

                                return diagnosticId;
                            }
                        }

                        if (current is IMethodSymbol { AssociatedSymbol: { } associatedSymbol })
                        {
                            current = associatedSymbol;
                            goto start;
                        }
                    }

                    return null;
                }

                static string getApiString(Compilation compilation, ISymbol symbol, string? experimentName, SymbolDisplayFormat format)
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

                    if (symbol.ContainingAssembly != null && !symbol.ContainingAssembly.Equals(compilation.Assembly))
                    {
                        publicApiName += $" (forwarded, contained in {symbol.ContainingAssembly.Name})";
                    }

                    if (experimentName != null)
                    {
                        publicApiName = "[" + experimentName + "]" + publicApiName;
                    }

                    return publicApiName;
                }
            }

            private static bool ContainsPublicApiName(string apiLineText, string publicApiNameToSearch)
            {
                apiLineText = apiLineText.TrimStart(ObliviousMarkerArray);

                // Ensure we don't search in parameter list/return type.
                var indexOfParamsList = apiLineText.IndexOf('(');
                if (indexOfParamsList > 0)
                {
                    apiLineText = apiLineText[..indexOfParamsList];
                }
                else
                {
                    var indexOfReturnType = apiLineText.IndexOf("->", StringComparison.Ordinal);
                    if (indexOfReturnType > 0)
                    {
                        apiLineText = apiLineText[..indexOfReturnType];
                    }
                }

                // Ensure that we don't have any leading characters in matched substring, apart from whitespace.
                var index = apiLineText.IndexOf(publicApiNameToSearch, StringComparison.Ordinal);
                return index == 0 || (index > 0 && apiLineText[index - 1] == ' ');
            }

            internal void OnCompilationEnd(CompilationAnalysisContext context)
            {
                ProcessTypeForwardedAttributes(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
                ReportDeletedApiList(context.ReportDiagnostic);
                ReportMarkedAsRemovedButNotActuallyRemovedApiList(context.ReportDiagnostic);
            }

            private void ProcessTypeForwardedAttributes(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                var typeForwardedToAttribute = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemRuntimeCompilerServicesTypeForwardedToAttribute);

                if (typeForwardedToAttribute != null)
                {
                    foreach (var attribute in compilation.Assembly.GetAttributes(typeForwardedToAttribute))
                    {
                        if (attribute.AttributeConstructor?.Parameters.Length == 1 &&
                            attribute.ApplicationSyntaxReference != null &&
                            attribute.ConstructorArguments.Length == 1 &&
                            attribute.ConstructorArguments[0].Value is INamedTypeSymbol forwardedType)
                        {
                            var obsoleteAttribute = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
                            if (forwardedType.IsUnboundGenericType)
                            {
                                forwardedType = forwardedType.ConstructedFrom;
                            }

                            VisitForwardedTypeRecursively(forwardedType, reportDiagnostic, obsoleteAttribute, attribute.ApplicationSyntaxReference.GetSyntax(cancellationToken).GetLocation(), cancellationToken);
                        }
                    }
                }
            }

            private void VisitForwardedTypeRecursively(ISymbol symbol, Action<Diagnostic> reportDiagnostic, INamedTypeSymbol? obsoleteAttribute, Location typeForwardedAttributeLocation, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                OnSymbolActionCore(symbol, reportDiagnostic, obsoleteAttribute, cancellationToken, typeForwardedAttributeLocation);

                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    foreach (var nestedType in namedTypeSymbol.GetTypeMembers())
                    {
                        VisitForwardedTypeRecursively(nestedType, reportDiagnostic, obsoleteAttribute, typeForwardedAttributeLocation, cancellationToken);
                    }

                    foreach (var member in namedTypeSymbol.GetMembers())
                    {
                        if (!(member.IsImplicitlyDeclared && member.IsDefaultConstructor()))
                        {
                            VisitForwardedTypeRecursively(member, reportDiagnostic, obsoleteAttribute, typeForwardedAttributeLocation, cancellationToken);
                        }
                    }
                }
            }

            /// <summary>
            /// Report diagnostics to the set of APIs which have been deleted but not yet documented.
            /// </summary>
            internal void ReportDeletedApiList(Action<Diagnostic> reportDiagnostic)
            {
                IReadOnlyDictionary<string, ApiLine> apiMap = _apiMap.Value;
                foreach (KeyValuePair<string, ApiLine> pair in apiMap)
                {
                    if (_visitedApiList.ContainsKey(pair.Key))
                    {
                        continue;
                    }

                    if (_unshippedData.RemovedApiList.Any(x => x.Text == pair.Key))
                    {
                        continue;
                    }

                    Location location = pair.Value.GetLocation(_additionalFiles);
                    ImmutableDictionary<string, string?> propertyBag = ImmutableDictionary<string, string?>.Empty.Add(ApiNamePropertyBagKey, pair.Value.Text);
                    reportDiagnostic(Diagnostic.Create(GetDiagnostic(RemoveDeletedPublicApiRule, RemoveDeletedInternalApiRule), location, propertyBag, pair.Value.Text));
                }
            }

            /// <summary>
            /// Report diagnostics to the set of APIs which have been marked with *REMOVED* but still exists in source code.
            /// </summary>
            internal void ReportMarkedAsRemovedButNotActuallyRemovedApiList(Action<Diagnostic> reportDiagnostic)
            {
                foreach (var markedAsRemoved in _unshippedData.RemovedApiList)
                {
                    if (_visitedApiList.ContainsKey(markedAsRemoved.Text))
                    {
                        Location location = markedAsRemoved.ApiLine.GetLocation(_additionalFiles);
                        reportDiagnostic(Diagnostic.Create(RemovedApiIsNotActuallyRemovedRule, location, messageArgs: markedAsRemoved.Text));
                    }
                }
            }

            private bool IsTrackedAPI(ISymbol symbol, CancellationToken cancellationToken)
            {
                if (symbol is IMethodSymbol methodSymbol)
                {
                    if (s_ignorableMethodKinds.Contains(methodSymbol.MethodKind))
                        return false;

                    if (methodSymbol is { MethodKind: MethodKind.Constructor, ContainingType.TypeKind: TypeKind.Enum })
                        return false;

                    // include a delegate's 'Invoke' method so we encode its signature (it would be a breaking change to
                    // change that). All other delegate methods can be ignored though.
                    if (methodSymbol is { ContainingType.TypeKind: TypeKind.Delegate, MethodKind: not MethodKind.DelegateInvoke })
                        return false;
                }

                // We don't consider properties to be public APIs. Instead, property getters and setters
                // (which are IMethodSymbols) are considered as public APIs.
                if (symbol is IPropertySymbol)
                {
                    return false;
                }

                if (IsNamespaceSkipped(symbol))
                {
                    return false;
                }

                return IsTrackedApiCore(symbol, cancellationToken);
            }

            private bool IsNamespaceSkipped(ISymbol symbol)
            {
                var @namespace = symbol as INamespaceSymbol ?? symbol.ContainingNamespace;

                PooledHashSet<string>? skippedNamespaces = null;

                try
                {
                    foreach (var location in symbol.Locations)
                    {
                        if (!location.IsInSource)
                        {
                            continue;
                        }

                        var syntaxTree = location.SourceTree;
                        var currentSkippedNamespaces = _skippedNamespacesCache.GetOrAdd(syntaxTree, GetSkippedNamespacesForTree);
                        if (currentSkippedNamespaces.Length == 0)
                        {
                            continue;
                        }

                        (skippedNamespaces ??= PooledHashSet<string>.GetInstance()).AddRange(currentSkippedNamespaces);
                    }

                    if (skippedNamespaces == null)
                    {
                        return false;
                    }

                    var namespaceString = @namespace.ToDisplayString(s_namespaceFormat);
                    return skippedNamespaces.Any(n => namespaceString.StartsWith(n, StringComparison.Ordinal));
                }
                finally
                {
                    skippedNamespaces?.Free();
                }
            }

            private ImmutableArray<string> GetSkippedNamespacesForTree(SyntaxTree tree)
            {
                if (TryGetEditorConfigOptionForSkippedNamespaces(_analyzerOptions, tree, out var skippedNamespaces))
                {
                    return skippedNamespaces;
                }

                return ImmutableArray<string>.Empty;
            }

            private bool IsTrackedApiCore(ISymbol symbol, CancellationToken cancellationToken)
            {
                var resultantVisibility = symbol.GetResultantVisibility();

#pragma warning disable IDE0047 // Remove unnecessary parentheses
                if (resultantVisibility == SymbolVisibility.Private
                    || ((resultantVisibility == SymbolVisibility.Public) != _isPublic))
                {
                    return false;
                }
#pragma warning restore IDE0047 // Remove unnecessary parentheses

                cancellationToken.ThrowIfCancellationRequested();

                for (var current = symbol; current != null; current = current.ContainingType)
                {
                    switch (current.DeclaredAccessibility)
                    {
                        case Accessibility.Protected:
                        case Accessibility.ProtectedOrInternal when _isPublic:
                            // Can't have top-level protected or protected internal members
                            if (!CanTypeBeExtended(current.ContainingType))
                            {
                                return false;
                            }

                            break;
                    }
                }

                return true;
            }

            private bool CanTypeBeExtended(ITypeSymbol type)
            {
                return _typeCanBeExtendedCache.GetOrAdd((type, _isPublic), CanTypeBeExtendedImpl);
            }

            private static bool CanTypeBeExtendedImpl((ITypeSymbol Type, bool IsPublic) key)
            {
                // a type can be extended publicly if (1) it isn't sealed, and (2) it has some constructor that is
                // not internal, private or protected&internal
                return !key.Type.IsSealed &&
                    key.Type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Any(
                        m => m.DeclaredAccessibility switch
                        {
                            Accessibility.Internal or Accessibility.ProtectedAndInternal => !key.IsPublic,
                            Accessibility.Private => false,
                            _ => true,
                        }
                    );
            }

            private DiagnosticDescriptor GetDiagnostic(DiagnosticDescriptor publicDiagnostic, DiagnosticDescriptor privateDiagnostic)
                => _isPublic ? publicDiagnostic : privateDiagnostic;

            /// <summary>
            /// Various Visit* methods return true if an oblivious reference type is detected.
            /// </summary>
            private sealed class ObliviousDetector : SymbolVisitor<bool>
            {
                // We need to ignore top-level nullability for outer types: `Outer<...>.Inner`
                private static readonly ObliviousDetector IgnoreTopLevelNullabilityInstance = new(ignoreTopLevelNullability: true);

                public static readonly ObliviousDetector Instance = new(ignoreTopLevelNullability: false);

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
                    if (!_ignoreTopLevelNullability &&
                        symbol.IsReferenceType &&
                        symbol.NullableAnnotation == NullableAnnotation.None)
                    {
                        return true;
                    }

                    if (symbol.ContainingType is INamedTypeSymbol containing &&
                        IgnoreTopLevelNullabilityInstance.Visit(containing))
                    {
                        return true;
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
                    if (symbol.NullableAnnotation == NullableAnnotation.None)
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
                        symbol.NullableAnnotation == NullableAnnotation.None)
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
                    if (symbol.HasReferenceTypeConstraint &&
                        symbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.None)
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

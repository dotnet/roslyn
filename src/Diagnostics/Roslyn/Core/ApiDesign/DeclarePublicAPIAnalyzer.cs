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
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class DeclarePublicAPIAnalyzer : DiagnosticAnalyzer
    {
        internal const string PublicApiFileName = "PublicAPI.txt";
        internal const string PublicApiNamePropertyBagKey = "PublicAPIName";
        internal const string MinimalNamePropertyBagKey = "MinimalName";

        internal static readonly DiagnosticDescriptor DeclareNewApiRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.DeclarePublicApiRuleId,
            title: RoslynDiagnosticsResources.DeclarePublicApiTitle,
            messageFormat: RoslynDiagnosticsResources.DeclarePublicApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.DeclarePublicApiDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RemoveDeletedApiRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.RemoveDeletedApiRuleId,
            title: RoslynDiagnosticsResources.RemoveDeletedApiTitle,
            messageFormat: RoslynDiagnosticsResources.RemoveDeletedApiMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.RemoveDeletedApiDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor ExposedNoninstantiableType = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.ExposedNoninstantiableTypeRuleId,
            title: RoslynDiagnosticsResources.ExposedNoninstantiableTypeTitle,
            messageFormat: RoslynDiagnosticsResources.ExposedNoninstantiableTypeMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: RoslynDiagnosticsResources.ExposedNoninstantiableTypeDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly SymbolDisplayFormat ShortSymbolNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.None);

        private static readonly SymbolDisplayFormat s_publicApiFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeConstantValue,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly HashSet<MethodKind> s_ignorableMethodKinds = new HashSet<MethodKind>
        {
            MethodKind.EventAdd,
            MethodKind.EventRemove
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DeclareNewApiRule, RemoveDeletedApiRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                AdditionalText publicApiAdditionalText = TryGetPublicApiSpec(compilationContext.Options.AdditionalFiles, compilationContext.CancellationToken);

                if (publicApiAdditionalText == null)
                {
                    return;
                }

                SourceText publicApiSourceText = publicApiAdditionalText.GetText(compilationContext.CancellationToken);
                HashSet<string> declaredPublicSymbols = ReadPublicSymbols(publicApiSourceText, compilationContext.CancellationToken);
                HashSet<string> examinedPublicTypes = new HashSet<string>();
                object lockObj = new object();

                Dictionary<ITypeSymbol, bool> typeCanBeExtendedPubliclyMap = new Dictionary<ITypeSymbol, bool>();
                Func<ITypeSymbol, bool> typeCanBeExtendedPublicly = type =>
                {
                    bool result;
                    if (typeCanBeExtendedPubliclyMap.TryGetValue(type, out result)) return result;

                    // a type can be extended publicly if (1) it isn't sealed, and (2) it has some constructor that is
                    // not internal, private or protected&internal
                    result = !type.IsSealed &&
                        type.GetMembers(WellKnownMemberNames.InstanceConstructorName).Any(
                            m => m.DeclaredAccessibility != Accessibility.Internal && m.DeclaredAccessibility != Accessibility.Private && m.DeclaredAccessibility != Accessibility.ProtectedAndInternal
                        );

                    typeCanBeExtendedPubliclyMap.Add(type, result);
                    return result;
                };

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var symbol = symbolContext.Symbol;

                    var methodSymbol = symbol as IMethodSymbol;
                    if (methodSymbol != null &&
                        s_ignorableMethodKinds.Contains(methodSymbol.MethodKind))
                    {
                        return;
                    }

                    lock (lockObj)
                    {
                        if (!IsPublicApi(symbol, typeCanBeExtendedPublicly))
                        {
                            return;
                        }

                        string publicApiName = GetPublicApiName(symbol);

                        examinedPublicTypes.Add(publicApiName);

                        if (!declaredPublicSymbols.Contains(publicApiName))
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
                            IsPublicApi(symbol.ContainingType.BaseType, typeCanBeExtendedPublicly) &&
                            !typeCanBeExtendedPublicly(symbol.ContainingType.BaseType))
                        {
                            var errorMessageName = symbol.ToDisplayString(ShortSymbolNameFormat);
                            var propertyBag = ImmutableDictionary<string, string>.Empty;
                            symbolContext.ReportDiagnostic(Diagnostic.Create(ExposedNoninstantiableType, symbol.Locations[0], propertyBag, errorMessageName));
                        }
                    }
                },
                SymbolKind.NamedType,
                SymbolKind.Event,
                SymbolKind.Field,
                SymbolKind.Method);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    ImmutableArray<string> deletedSymbols;
                    lock (lockObj)
                    {
                        deletedSymbols = declaredPublicSymbols.Where(symbol => !examinedPublicTypes.Contains(symbol)).ToImmutableArray();
                    }

                    foreach (var symbol in deletedSymbols)
                    {
                        var span = FindString(publicApiSourceText, symbol);
                        Location location;
                        if (span.HasValue)
                        {
                            var linePositionSpan = publicApiSourceText.Lines.GetLinePositionSpan(span.Value);
                            location = Location.Create(publicApiAdditionalText.Path, span.Value, linePositionSpan);
                        }
                        else
                        {
                            location = Location.Create(publicApiAdditionalText.Path, default(TextSpan), default(LinePositionSpan));
                        }

                        var propertyBag = ImmutableDictionary<string, string>.Empty.Add(PublicApiNamePropertyBagKey, symbol);

                        compilationEndContext.ReportDiagnostic(Diagnostic.Create(RemoveDeletedApiRule, location, propertyBag, symbol));
                    }
                });
            });
        }

        internal static string GetPublicApiName(ISymbol symbol)
        {
            var publicApiName = symbol.ToDisplayString(s_publicApiFormat);

            ITypeSymbol memberType = null;
            if (symbol is IMethodSymbol)
            {
                memberType = ((IMethodSymbol)symbol).ReturnType;
            }
            else if (symbol is IPropertySymbol)
            {
                memberType = ((IPropertySymbol)symbol).Type;
            }
            else if (symbol is IEventSymbol)
            {
                memberType = ((IEventSymbol)symbol).Type;
            }
            else if (symbol is IFieldSymbol)
            {
                memberType = ((IFieldSymbol)symbol).Type;
            }

            if (memberType != null)
            {
                publicApiName = publicApiName + " -> " + memberType.ToDisplayString(s_publicApiFormat);
            }

            return publicApiName;
        }

        private TextSpan? FindString(SourceText sourceText, string symbol)
        {
            foreach (var line in sourceText.Lines)
            {
                if (line.ToString() == symbol)
                {
                    return line.Span;
                }
            }

            return null;
        }

        private static HashSet<string> ReadPublicSymbols(SourceText file, CancellationToken cancellationToken)
        {
            HashSet<string> publicSymbols = new HashSet<string>();

            foreach (var line in file.Lines)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = line.ToString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    publicSymbols.Add(text);
                }
            }

            return publicSymbols;
        }

        private static bool IsPublicApi(ISymbol symbol, Func<ITypeSymbol, bool> typeCanBeExtendedPublicly)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return symbol.ContainingType == null || IsPublicApi(symbol.ContainingType, typeCanBeExtendedPublicly);
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    // Protected symbols must have parent types (that is, top-level protected
                    // symbols are not allowed.
                    return
                        symbol.ContainingType != null &&
                        IsPublicApi(symbol.ContainingType, typeCanBeExtendedPublicly) &&
                        typeCanBeExtendedPublicly(symbol.ContainingType);
                default:
                    return false;
            }
        }

        private static AdditionalText TryGetPublicApiSpec(ImmutableArray<AdditionalText> additionalTexts, CancellationToken cancellationToken)
        {
            foreach (var text in additionalTexts)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (Path.GetFileName(text.Path).Equals(PublicApiFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return text;
                }
            }

            return null;
        }
    }
}

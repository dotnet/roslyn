// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers.ApiDesign
{
    [DiagnosticAnalyzer]
    public class DeclarePublicAPIAnalyzer : DiagnosticAnalyzer
    {
        internal const string PublicApiFileName = "PublicAPI.txt";

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

        internal static readonly SymbolDisplayFormat PublicApiFormat =
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

        private static HashSet<MethodKind> ignorableMethodKinds = new HashSet<MethodKind>
        {
            MethodKind.EventAdd,
            MethodKind.EventRemove
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DeclareNewApiRule, RemoveDeletedApiRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                AdditionalText publicApiAdditionalText = TryGetPublicApiSpec(compilationContext.Options.AdditionalFiles);

                if (publicApiAdditionalText == null)
                {
                    return;
                }

                HashSet<string> declaredPublicSymbols = ReadPublicSymbols(publicApiAdditionalText);
                HashSet<string> examinedPublicTypes = new HashSet<string>();
                object lockObj = new object();

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var symbol = symbolContext.Symbol;

                    var methodSymbol = symbol as IMethodSymbol;
                    if (methodSymbol != null &&
                        ignorableMethodKinds.Contains(methodSymbol.MethodKind))
                    {
                        return;
                    }

                    if (!IsPublicOrPublicProtected(symbol))
                    {
                        return;
                    }

                    var fullyQualifiedName = symbol.ToDisplayString(PublicApiFormat);

                    lock (lockObj)
                    {
                        examinedPublicTypes.Add(fullyQualifiedName);

                        if (!declaredPublicSymbols.Contains(fullyQualifiedName))
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(DeclareNewApiRule, symbol.Locations.First(loc => loc.IsInSource), fullyQualifiedName));
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
                        var location = Location.Create(publicApiAdditionalText.Path, default(TextSpan), default(LinePositionSpan));
                        compilationEndContext.ReportDiagnostic(Diagnostic.Create(RemoveDeletedApiRule, location, symbol));
                    }
                });
            });
        }

        private static HashSet<string> ReadPublicSymbols(AdditionalText additionalFile)
        {
            HashSet<string> publicSymbols = new HashSet<string>();

            foreach (var line in additionalFile.GetText().Lines)
            {
                publicSymbols.Add(line.ToString());
            }

            return publicSymbols;
        }

        private static bool IsPublic(ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                return symbol.ContainingType == null || IsPublic(symbol.ContainingType);
            }

            return false;
        }

        private static bool IsPublicOrPublicProtected(ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility == Accessibility.Public)
            {
                return symbol.ContainingType == null || IsPublic(symbol.ContainingType);
            }

            if (symbol.DeclaredAccessibility == Accessibility.Protected ||
                symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
            {
                // Protected symbols must have parent types (that is, top-level protected
                // symbols are not allowed.
                return symbol.ContainingType != null && IsPublicOrPublicProtected(symbol.ContainingType);
            }

            return false;
        }

        private static AdditionalText TryGetPublicApiSpec(ImmutableArray<AdditionalText> additionalTexts)
        {
            foreach (var text in additionalTexts)
            {
                if (Path.GetFileName(text.Path).Equals(PublicApiFileName, StringComparison.OrdinalIgnoreCase))
                {
                    return text;
                }
            }

            return null;
        }
    }
}
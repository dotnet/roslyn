// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.InteropServices.Analyzers
{
    /// <summary>
    /// CA1060 - Move P/Invokes to native methods class
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class MovePInvokesToNativeMethodsClassAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1060";
        private static LocalizableString s_localizableTitleAndMessage = new LocalizableResourceString(nameof(SystemRuntimeInteropServicesAnalyzersResources.MovePInvokesToNativeMethodsClass), SystemRuntimeInteropServicesAnalyzersResources.ResourceManager, typeof(SystemRuntimeInteropServicesAnalyzersResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitleAndMessage,
                                                                         s_localizableTitleAndMessage,
                                                                         DiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: false,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182161.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        private const string NativeMethodsText = "NativeMethods";
        private const string SafeNativeMethodsText = "SafeNativeMethods";
        private const string UnsafeNativeMethodsText = "UnsafeNativeMethods";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(symbolContext =>
            {
                AnalyzeSymbol((INamedTypeSymbol)symbolContext.Symbol, symbolContext.Compilation, symbolContext.ReportDiagnostic);
            }, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic)
        {
            if (symbol.GetMembers().Any(member => IsDllImport(member)) && !IsTypeNamedCorrectly(symbol.Name))
            {
                addDiagnostic(Diagnostic.Create(Rule, symbol.Locations.First(l => l.IsInSource)));
            }
        }

        private static bool IsDllImport(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).GetDllImportData() != null;
        }

        private static bool IsTypeNamedCorrectly(string name)
        {
            return string.Compare(name, NativeMethodsText, StringComparison.Ordinal) == 0 ||
                string.Compare(name, SafeNativeMethodsText, StringComparison.Ordinal) == 0 ||
                string.Compare(name, UnsafeNativeMethodsText, StringComparison.Ordinal) == 0;
        }
    }
}

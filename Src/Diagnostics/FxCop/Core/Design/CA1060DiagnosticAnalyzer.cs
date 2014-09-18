// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1060 - Move P/Invokes to native methods class
    /// </summary>
    [DiagnosticAnalyzer]
    public sealed class CA1060DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1060";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.MovePInvokesToNativeMethodsClass,
                                                                         FxCopRulesResources.MovePInvokesToNativeMethodsClass,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182161.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private const string NativeMethodsText = "NativeMethods";
        private const string SafeNativeMethodsText = "SafeNativeMethods";
        private const string UnsafeNativeMethodsText = "UnsafeNativeMethods";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (symbol.GetMembers().Any(member => IsDllImport(member)) && !IsTypeNamedCorrectly(symbol.Name))
            {
                addDiagnostic(symbol.CreateDiagnostic(Rule));
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

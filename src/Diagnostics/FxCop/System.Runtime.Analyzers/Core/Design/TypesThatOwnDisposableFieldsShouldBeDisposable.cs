﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class TypesThatOwnDisposableFieldsShouldBeDisposableAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1001";
        internal const string Dispose = "Dispose";
        internal const string IDisposable = "System.IDisposable";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.TypesThatOwnDisposableFieldsShouldBeDisposable), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources)),
                                                                         new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.TypeOwnsDisposableFieldButIsNotDisposable), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources)),
                                                                         DiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182172.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSymbolAction(context =>
            {
                AnalyzeSymbol((INamedTypeSymbol)context.Symbol, context.Compilation, context.ReportDiagnostic, context.Options, context.CancellationToken);
            },
            SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var disposableType = WellKnownTypes.IDisposable(compilation);
            if (disposableType != null && !symbol.AllInterfaces.Contains(disposableType))
            {
                var disposableFields = from member in symbol.GetMembers()
                                       where member.Kind == SymbolKind.Field && !member.IsStatic
                                       let field = member as IFieldSymbol
                                       where field.Type != null && field.Type.AllInterfaces.Contains(disposableType)
                                       select field;

                if (disposableFields.Any())
                {
                    addDiagnostic(symbol.CreateDiagnostic(Rule, symbol.Name));
                }
            }
        }
    }
}

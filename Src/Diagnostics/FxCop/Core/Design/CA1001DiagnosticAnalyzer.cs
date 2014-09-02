// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    [DiagnosticAnalyzer]
    public sealed class CA1001DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1001";
        internal const string Dispose = "Dispose";
        internal const string IDisposable = "System.IDisposable";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.TypesThatOwnDisposableFieldsShouldBeDisposable,
                                                                         FxCopRulesResources.TypeOwnsDisposableFieldButIsNotDisposable,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182172.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
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
﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.AnalyzerPowerPack.Utilities;
using Microsoft.CodeAnalysis;

namespace Microsoft.AnalyzerPowerPack.Design
{
    /// <summary>
    /// CA1012: Abstract classes should not have public constructors
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CA1012DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA1012";
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzerPowerPackRulesResources.AbstractTypesShouldNotHavePublicConstructors), AnalyzerPowerPackRulesResources.ResourceManager, typeof(AnalyzerPowerPackRulesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(AnalyzerPowerPackRulesResources.TypeIsAbstractButHasPublicConstructors), AnalyzerPowerPackRulesResources.ResourceManager, typeof(AnalyzerPowerPackRulesResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitle,
                                                                         s_localizableMessage,
                                                                         AnalyzerPowerPackDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: false,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182126.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (symbol.IsAbstract)
            {
                // TODO: Should we also check symbol.GetResultantVisibility() == SymbolVisibility.Public?

                var hasAnyPublicConstructors =
                    symbol.InstanceConstructors.Any(
                        (constructor) => constructor.DeclaredAccessibility == Accessibility.Public);

                if (hasAnyPublicConstructors)
                {
                    addDiagnostic(symbol.CreateDiagnostic(Rule, symbol.Name));
                }
            }
        }
    }
}

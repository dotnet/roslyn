// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2231: Complain if the type implements Equals without overloading the equality operator.
    /// </summary>
    [DiagnosticAnalyzer]
    public sealed class CA2231DiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleId = "CA2231";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.OverloadOperatorEqualsOnOverridingValueTypeEquals,
                                                                         FxCopRulesResources.OverloadOperatorEqualsOnOverridingValueTypeEquals,
                                                                         FxCopDiagnosticCategory.Usage,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         description: FxCopRulesResources.OverloadOperatorEqualsOnOverridingValueTypeEqualsDescription,
                                                                         helpLink: "http://msdn.microsoft.com/library/ms182359.aspx",
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (namedTypeSymbol.IsValueType && IsOverridesEquals(namedTypeSymbol) && !IsEqualityOperatorImplemented(namedTypeSymbol))
            {
                addDiagnostic(namedTypeSymbol.CreateDiagnostic(Rule));
            }
        }

        private static bool IsOverridesEquals(INamedTypeSymbol symbol)
        {
            // do override Object.Equals?
            return symbol.GetMembers(WellKnownMemberNames.ObjectEquals).OfType<IMethodSymbol>().Where(m => IsEqualsOverride(m)).Any();
        }

        private static bool IsEqualsOverride(IMethodSymbol method)
        {
            return method != null &&
                   method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                   method.Parameters.Length == 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        private static bool IsEqualityOperatorImplemented(INamedTypeSymbol symbol)
        {
            // do implement the equality operator?
            return symbol.GetMembers(WellKnownMemberNames.EqualityOperatorName).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator).Any() ||
                    symbol.GetMembers(WellKnownMemberNames.InequalityOperatorName).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator).Any();
        }
    }
}

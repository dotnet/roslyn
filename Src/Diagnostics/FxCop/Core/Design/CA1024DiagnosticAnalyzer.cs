// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Shared.Extensions;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1024: Use properties where appropriate
    /// 
    /// Cause:
    /// A public or protected method has a name that starts with Get, takes no parameters, and returns a value that is not an array.
    /// </summary>
    public abstract class CA1024DiagnosticAnalyzer : ICodeBlockNestedAnalyzerFactory
    {
        internal const string RuleId = "CA1024";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         FxCopRulesResources.UsePropertiesWhereAppropriate,
                                                                         FxCopRulesResources.ChangeToAPropertyIfAppropriate,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);
        private const string GetHashCodeName = "GetHashCode";
        private const string GetEnumeratorName = "GetEnumerator";

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var methodSymbol = ownerSymbol as IMethodSymbol;

            if (methodSymbol == null ||
                methodSymbol.ReturnsVoid ||
                methodSymbol.ReturnType.Kind == SymbolKind.ArrayType ||
                methodSymbol.Parameters.Length > 0 ||
                !(methodSymbol.DeclaredAccessibility == Accessibility.Public || methodSymbol.DeclaredAccessibility == Accessibility.Protected) ||
                methodSymbol.IsAccessorMethod() ||
                !IsPropertyLikeName(methodSymbol.Name))
            {
                return null;
            }

            // Fxcop has a few additional checks to reduce the noise for this diagnostic:
            // Ensure that the method is non-generic, non-virtual/override, has no overloads and doesn't have special names: 'GetHashCode' or 'GetEnumerator'.
            // Also avoid generating this diagnostic if the method body has any invocation expressions.
            if (methodSymbol.IsGenericMethod ||
                methodSymbol.IsVirtual ||
                methodSymbol.IsOverride ||
                methodSymbol.ContainingType.GetMembers(methodSymbol.Name).Length > 1 ||
                methodSymbol.Name == GetHashCodeName ||
                methodSymbol.Name == GetEnumeratorName)
            {
                return null;
            }

            return GetCodeBlockEndedAnalyzer();
        }

        protected abstract CA1024CodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer();

        protected abstract class CA1024CodeBlockEndedAnalyzer : ICodeBlockAnalyzer
        {
            protected bool suppress = false;

            protected abstract Location GetDiagnosticLocation(SyntaxNode node);

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Rule);
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                // We are analyzing an invocation expression node. This method is suffiently complex to suppress the diagnostic.
                suppress = true;
            }

            public void AnalyzeCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                if (!suppress)
                {
                    addDiagnostic(GetDiagnosticLocation(codeBlock).CreateDiagnostic(Rule, ownerSymbol.Name));
                }
            }
        }

        private static bool IsPropertyLikeName(string methodName)
        {
            return methodName.Length > 3 &&
                methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) &&
                !char.IsLower(methodName[3]);
        }
    }
}

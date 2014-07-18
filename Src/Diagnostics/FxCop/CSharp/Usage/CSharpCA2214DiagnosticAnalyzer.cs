// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Usage;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2214: Do not call overridable methods in constructors
    /// 
    /// Cause: The constructor of an unsealed type calls a virtual method defined in its class.
    /// 
    /// Description: When a virtual method is called, the actual type that executes the method is not selected 
    /// until run time. When a constructor calls a virtual method, it is possible that the constructor for the 
    /// instance that invokes the method has not executed. 
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(RuleId, LanguageNames.CSharp)]
    public class CSharpCA2214DiagnosticAnalyzer : CA2214DiagnosticAnalyzer
    {
        protected override IDiagnosticAnalyzer GetCodeBlockEndedAnalyzer(IMethodSymbol constructorSymbol)
        {
            return new SyntaxNodeAnalyzer(constructorSymbol);
        }

        private sealed class SyntaxNodeAnalyzer : AbstractSyntaxNodeAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.InvocationExpression);
            private INamedTypeSymbol containingType;

            public SyntaxNodeAnalyzer(IMethodSymbol constructorSymbol)
            {
                this.containingType = constructorSymbol.ContainingType;
            }

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                // TODO: For this to be correct, we need flow analysis to determine if a given method
                // is actually invoked inside the current constructor. A method may be assigned to a
                // delegate which can be called inside or outside the constructor. A method may also
                // be called from within a lambda which is called inside or outside the constructor.
                // Currently, FxCop does not produce a warning if a virtual method is called indirectly
                // through a delegate or through a lambda.

                var invocationExpression = (InvocationExpressionSyntax)node;
                var method = semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol as IMethodSymbol;
                if (method != null &&
                    (method.IsAbstract || method.IsVirtual) &&
                    method.ContainingType == this.containingType)
                {
                    addDiagnostic(invocationExpression.Expression.CreateDiagnostic(Rule));
                }
            }
        }
    }
}

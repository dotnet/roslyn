// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        protected override ICodeBlockEndedAnalyzer GetCodeBlockEndedAnalyzer()
        {
            return new SyntaxNodeAnalyzer();
        }

        private sealed class SyntaxNodeAnalyzer : AbstractSyntaxNodeAnalyzer, ISyntaxNodeAnalyzer<SyntaxKind>
        {
            private static readonly ImmutableArray<SyntaxKind> kindsOfInterest = ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.IdentifierName);

            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest
            {
                get
                {
                    return kindsOfInterest;
                }
            }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                // TODO: should we restrict this to invocation, delegate creation, etc?
                switch (node.CSharpKind())
                {
                    case SyntaxKind.IdentifierName:
                        {
                            var id = (IdentifierNameSyntax)node;
                            var method = semanticModel.GetSymbolInfo(id).Symbol as IMethodSymbol;
                            if (method == null || !(method.IsAbstract || method.IsVirtual))
                            {
                                return;
                            }

                            addDiagnostic(id.CreateDiagnostic(Rule));
                        }

                        return;

                    case SyntaxKind.SimpleMemberAccessExpression:
                        {
                            var qid = (MemberAccessExpressionSyntax)node;
                            var method = semanticModel.GetSymbolInfo(qid).Symbol as IMethodSymbol;
                            if (method == null || !(method.IsAbstract || method.IsVirtual))
                            {
                                return;
                            }

                            if (qid.Expression.CSharpKind() == SyntaxKind.BaseExpression)
                            {
                                return;
                            }

                            var receiver = semanticModel.GetSymbolInfo(qid.Expression).Symbol as IParameterSymbol;
                            if (receiver == null || !receiver.IsThis)
                            {
                                return;
                            }

                            addDiagnostic(qid.CreateDiagnostic(Rule));
                        }

                        return;
                }
            }
        }
    }
}

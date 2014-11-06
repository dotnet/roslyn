// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncPackage
{
    /// <summary>
    /// Analyzer that examines async lambdas and checks if they are being passed or stored as void-returning delegate types.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncLambdaAnalyzer : DiagnosticAnalyzer
    {
        internal const string AsyncLambdaId1 = "Async003";
        internal const string AsyncLambdaId2 = "Async004";

        internal static DiagnosticDescriptor Rule1 = new DiagnosticDescriptor(id: AsyncLambdaId1,
            title: "Don't Pass Async Lambdas as Void Returning Delegate Types",
            messageFormat: "This async lambda is passed as a void-returning delegate type",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor Rule2 = new DiagnosticDescriptor(id: AsyncLambdaId2,
            title: "Don't Store Async Lambdas as Void Returning Delegate Types",
            messageFormat: "This async lambda is stored as a void-returning delegate type",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule1, Rule2); } }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

            var methodLambda = symbol as IMethodSymbol;

            if (methodLambda != null && methodLambda.IsAsync)
            {
                var type = context.SemanticModel.GetTypeInfo(context.Node);
                if (this.CheckIfVoidReturningDelegateType(type.ConvertedType))
                {
                    // check if the lambda is being assigned to a variable.  This has a code fix.
                    var parent = context.Node.Parent;

                    while (parent != null && !(parent is InvocationExpressionSyntax))
                    {
                        if (parent is VariableDeclarationSyntax)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule2, parent.GetLocation()));
                            return;
                        }

                        parent = parent.Parent;
                    }

                    // if not, add the normal diagnostic
                    context.ReportDiagnostic(Diagnostic.Create(Rule1, context.Node.GetLocation()));
                    return;
                }
            }

            return;
        }

        /// <summary>
        /// Check if the method is a void returning delegate type
        /// </summary>
        /// <param name="convertedType"></param>
        /// <returns>
        /// Returns false if analysis failed or if not a void-returning delegate type
        /// Returns true if the inputted node has a converted type that is a void-returning delegate type
        /// </returns>
        private bool CheckIfVoidReturningDelegateType(ITypeSymbol convertedType)
        {
            if (convertedType != null && convertedType.TypeKind.Equals(TypeKind.Delegate))
            {
                var invoke = convertedType.GetMembers("Invoke").FirstOrDefault() as IMethodSymbol;

                if (invoke != null)
                {
                    return invoke.ReturnsVoid;
                }
            }

            return false;
        }
    }
}

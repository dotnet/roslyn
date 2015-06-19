// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// This analyzer check to see if there are Cancellation Tokens that can be propagated through async method calls
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CancellationAnalyzer : DiagnosticAnalyzer
    {
        internal const string CancellationId = "Async005";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(id: CancellationId,
            title: "Propagate CancellationTokens When Possible",
            messageFormat: "This method can take a CancellationToken",
            category: "Library",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCodeBlockStartAction<SyntaxKind>(CreateAnalyzerWithinCodeBlock);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private void CreateAnalyzerWithinCodeBlock(CodeBlockStartAnalysisContext<SyntaxKind> context)
        {
            var methodDeclaration = context.OwningSymbol as IMethodSymbol;

            if (methodDeclaration != null)
            {
                ITypeSymbol cancellationTokenType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                var paramTypes = methodDeclaration.Parameters.Select(x => x.Type);

                if (paramTypes.Contains(cancellationTokenType))
                {
                    // Analyze the inside of the code block for invocationexpressions
                    context.RegisterSyntaxNodeAction(new CancellationAnalyzer_Inner().AnalyzeNode, SyntaxKind.InvocationExpression);
                }
            }
        }

        internal class CancellationAnalyzer_Inner
        {
            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var invokeMethod = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IMethodSymbol;

                if (invokeMethod != null)
                {
                    ITypeSymbol cancellationTokenType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                    var invokeParams = invokeMethod.Parameters.Select(x => x.Type);

                    if (invokeParams.Contains(cancellationTokenType))
                    {
                        var passedToken = false;

                        foreach (var arg in ((InvocationExpressionSyntax)context.Node).ArgumentList.Arguments)
                        {
                            var thisArgType = context.SemanticModel.GetTypeInfo(arg.Expression).Type;

                            if (thisArgType != null && thisArgType.Equals(cancellationTokenType))
                            {
                                passedToken = true;
                            }
                        }

                        if (!passedToken)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(CancellationAnalyzer.Rule, context.Node.GetLocation()));
                        }
                    }
                }
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncPackage
{
    /// <summary>
    /// This analyzer checks to see if asynchronous and synchronous code is mixed. 
    /// This causes blocking and deadlocks. The analyzer will check when async 
    /// methods are used and then checks if synchronous code is used within the method.
    /// A codefix will then change that synchronous code to its asynchronous counterpart.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BlockingAsyncAnalyzer : DiagnosticAnalyzer
    {
        internal const string BlockingAsyncId = "Async006";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(id: BlockingAsyncId,
            title: "Don't Mix Blocking and Async",
            messageFormat: "This method is blocking on async code",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var method = context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart) as IMethodSymbol;

            if (method != null && method.IsAsync)
            {
                var invokeMethod = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IMethodSymbol;

                if (invokeMethod != null && !invokeMethod.IsExtensionMethod)
                {
                    // Checks if the Wait method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("Wait"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the WaitAny method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAny"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the WaitAll method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAll"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the Sleep method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("Sleep"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the GetResult method is called within an async method then creates the diagnostic.     
                    if (invokeMethod.OriginalDefinition.Name.Equals("GetResult"))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.Parent.GetLocation()));
                        return;
                    }
                }

                var property = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IPropertySymbol;

                // Checks if the Result property is called within an async method then creates the diagnostic.
                if (property != null && property.OriginalDefinition.Name.Equals("Result"))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
                    return;
                }
            }
        }
    }
}

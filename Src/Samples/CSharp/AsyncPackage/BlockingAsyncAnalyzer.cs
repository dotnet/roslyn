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
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BlockingAsyncAnalyzer : ISyntaxNodeAnalyzer<SyntaxKind>
    {
        internal const string BlockingAsyncId = "Async006";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(id: BlockingAsyncId,
            title: "Don't Mix Blocking and Async",
            messageFormat: "This method is blocking on async code",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get { return ImmutableArray.Create(SyntaxKind.SimpleMemberAccessExpression); } }

        public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var method = semanticModel.GetEnclosingSymbol(node.SpanStart) as IMethodSymbol;

            if (method != null && method.IsAsync)
            {
                var invokeMethod = semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;

                if (invokeMethod != null && !invokeMethod.IsExtensionMethod)
                {
                    // Checks if the Wait method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("Wait"))
                    {
                        addDiagnostic(Diagnostic.Create(Rule, node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the WaitAny method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAny"))
                    {
                        addDiagnostic(Diagnostic.Create(Rule, node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the WaitAll method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("WaitAll"))
                    {
                        addDiagnostic(Diagnostic.Create(Rule, node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the Sleep method is called within an async method then creates the diagnostic.
                    if (invokeMethod.OriginalDefinition.Name.Equals("Sleep"))
                    {
                        addDiagnostic(Diagnostic.Create(Rule, node.Parent.GetLocation()));
                        return;
                    }

                    // Checks if the GetResult method is called within an async method then creates the diagnostic.     
                    if (invokeMethod.OriginalDefinition.Name.Equals("GetResult"))
                    {
                        addDiagnostic(Diagnostic.Create(Rule, node.Parent.GetLocation()));
                        return;
                    }
                }

                var property = semanticModel.GetSymbolInfo(node).Symbol as IPropertySymbol;

                // Checks if the Result property is called within an async method then creates the diagnostic.
                if (property != null && property.OriginalDefinition.Name.Equals("Result"))
                {
                    addDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
                    return;
                }
            }
        }
    }
}

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncPackage
{
    /// <summary>
    /// This Analyzer determines if a method is Async and needs to be returning a Task instead of having a void return type.
    /// </summary>
    [DiagnosticAnalyzer]
    public class AsyncVoidAnalyzer : DiagnosticAnalyzer
    {
        internal const string AsyncVoidId = "Async001";

        internal static DiagnosticDescriptor VoidReturnType = new DiagnosticDescriptor(id: AsyncVoidId,
            title: "Avoid Async Void",
            messageFormat: "This method has the async keyword but it returns void",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(VoidReturnType); } }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // Filter out methods that do not use Async and that do not have exactly two parameters
            var methodSymbol = (IMethodSymbol)context.Symbol;

            var eventType = context.Compilation.GetTypeByMetadataName("System.EventArgs");

            if (methodSymbol.ReturnsVoid && methodSymbol.IsAsync)
            {
                if (methodSymbol.Parameters.Length == 2)
                {
                    var firstParam = methodSymbol.Parameters[0];
                    var secondParam = methodSymbol.Parameters[1];

                    if (firstParam is object)
                    {
                        // Check each parameter for EventHandler shape and return if it matches.
                        if (firstParam.Name.ToLower().Equals("sender") && secondParam.Type == eventType)
                        {
                            return;
                        }
                        else
                        {
                            // Check if the second parameter implements EventArgs. If it does; return.
                            var checkForEventType = secondParam.Type.BaseType;
                            while (checkForEventType.OriginalDefinition != context.Compilation.GetTypeByMetadataName("System.Object"))
                            {
                                if (checkForEventType == eventType)
                                {
                                    return;
                                }

                                checkForEventType = checkForEventType.BaseType;
                            }
                        }
                    }
                }

                context.ReportDiagnostic(Diagnostic.Create(VoidReturnType, methodSymbol.Locations[0], methodSymbol.Name));
                return;
            }

            return;
        }
    }
}

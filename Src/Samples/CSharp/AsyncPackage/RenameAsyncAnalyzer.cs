using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncPackage
{
    /// <summary>
    /// This analyzer will run a codefix on any method that qualifies as async that renames it to follow naming conventions
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class RenameAsyncAnalyzer : ISymbolAnalyzer
    {
        internal const string RenameAsyncId = "Async002";

        internal static DiagnosticDescriptor RenameAsyncMethod = new DiagnosticDescriptor(id: RenameAsyncId,
            title: "Async Method Names Should End in Async",
            messageFormat: "This method is async but the method name does not end in Async",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RenameAsyncMethod); } }

        public ImmutableArray<SymbolKind> SymbolKindsOfInterest { get { return ImmutableArray.Create(SymbolKind.Method); } }

        public void AnalyzeSymbol(ISymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // Filter out methods that do not use Async and make sure to include methods that return a Task
            var methodSymbol = (IMethodSymbol)symbol;

            // Check if method name is an override or virtual class. If it is disregard it.
            // (This assumes if a method is virtual the programmer will not want to change the name)
            // Check if the method returns a Task or Task<TResult>
            if ((methodSymbol.ReturnType == compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")
                || methodSymbol.ReturnType.OriginalDefinition == compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1").OriginalDefinition)
                && !methodSymbol.Name.EndsWith("Async") && !methodSymbol.IsOverride && !methodSymbol.IsVirtual)
            {
                addDiagnostic(Diagnostic.Create(RenameAsyncMethod, methodSymbol.Locations[0], methodSymbol.Name));
                return;
            }

            return;
        }
    }
}

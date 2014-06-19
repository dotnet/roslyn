using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal static class Analyzer
    {
        internal static BoundStatement AnalyzeMethodBody(MethodBodyCompiler methodCompiler, MethodSymbol method, BoundBlock body, DiagnosticBag diagnostics)
        {
            Debug.Assert(diagnostics != null);

            body = FlowAnalysisPass.Rewrite(method, body, diagnostics);
            var analyzedBody = (BoundBlock)RewritePass.Rewrite(methodCompiler, method.ContainingType, body);
            return RewritePass.InsertPrologueSequencePoint(analyzedBody, method);
        }

        internal static BoundStatementList AnalyzeFieldInitializers(MethodBodyCompiler methodCompiler, MethodSymbol constructor, ReadOnlyArray<BoundInitializer> boundInitializers)
        {
            if (boundInitializers.IsNull)
            {
                return null;
            }

            // The lowered tree might be reused in multiple constructors. Therefore, unless there is just a single constructor 
            // (e.g. in interactive submission), the lowered code should not refer to it.
            var initializerStatements = InitializerRewriter.Rewrite(boundInitializers, constructor.IsInteractiveSubmissionConstructor ? constructor : null);
            return (BoundStatementList)RewritePass.Rewrite(methodCompiler, constructor.ContainingType, initializerStatements);
        }
    }
}
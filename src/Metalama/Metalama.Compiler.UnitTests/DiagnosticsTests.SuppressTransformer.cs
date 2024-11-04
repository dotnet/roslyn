using Microsoft.CodeAnalysis;

namespace Metalama.Compiler.UnitTests;

partial class DiagnosticsTests
{
    private class SuppressTransformer : ISourceTransformer
    {
        private readonly string _diagnosticId;
        private readonly string _filePath;

        public SuppressTransformer(string diagnosticId, string filePath)
        {
            _diagnosticId = diagnosticId;
            _filePath = filePath;
        }

        public void Execute(TransformerContext context)
        {
            context.RegisterDiagnosticFilter(
                new DiagnosticFilter(
                    new SuppressionDescriptor("Suppress." + _diagnosticId, _diagnosticId, ""),
                    _filePath,
                    (in DiagnosticFilteringRequest _) => true));
        }
    }
}

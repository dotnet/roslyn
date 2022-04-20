using Microsoft.CodeAnalysis;

namespace Metalama.Compiler.UnitTests;

partial class DiagnosticsTests
{
    class SuppressTransformer : ISourceTransformer
    {
        private readonly string _diagnosticId;

        public SuppressTransformer(string diagnosticId)
        {
            _diagnosticId = diagnosticId;
        }

        public void Execute(TransformerContext context)
        {
            context.RegisterDiagnosticFilter(
                new SuppressionDescriptor("Suppress." + _diagnosticId, _diagnosticId, ""),
                request => request.Suppress());
        }
    }
}

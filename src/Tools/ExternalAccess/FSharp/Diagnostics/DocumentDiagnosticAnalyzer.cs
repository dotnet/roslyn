using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    /// <summary>
    /// IDE-only document based diagnostic analyzer.
    /// </summary>
    public abstract class DocumentDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public const int DefaultPriority = 50;

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken);

        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both DocumentDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
        public sealed override void Initialize(AnalysisContext context)
        {
        }

        /// <summary>
        /// This lets vsix installed <see cref="DocumentDiagnosticAnalyzer"/> or <see cref="ProjectDiagnosticAnalyzer"/> to
        /// specify priority of the analyzer. Regular <see cref="DiagnosticAnalyzer"/> always comes before those 2 different types.
        /// Priority is ascending order and this only works on HostDiagnosticAnalyzer meaning Vsix installed analyzers in VS.
        /// This is to support partner teams (such as typescript and F#) who want to order their analyzer's execution order.
        /// </summary>
        public virtual int Priority => DefaultPriority;
    }
}

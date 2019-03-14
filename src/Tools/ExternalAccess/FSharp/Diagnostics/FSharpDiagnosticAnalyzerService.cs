using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    [Export(typeof(IDiagnosticAnalyzerService))]
    [Shared]
    internal sealed class FSharpDiagnosticAnalyzerService : IDiagnosticAnalyzerService
    {
        private readonly Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService _delegatee;

        [ImportingConstructor]
        public FSharpDiagnosticAnalyzerService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService delegatee)
        {
            _delegatee = delegatee;
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
        {
            _delegatee.Reanalyze(workspace, projectIds, documentIds, highPriority);
        }
    }
}

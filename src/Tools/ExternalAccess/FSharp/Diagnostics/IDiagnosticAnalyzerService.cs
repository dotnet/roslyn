using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    public interface IDiagnosticAnalyzerService
    {
        /// <summary>
        /// re-analyze given projects and documents
        /// </summary>
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);
    }
}

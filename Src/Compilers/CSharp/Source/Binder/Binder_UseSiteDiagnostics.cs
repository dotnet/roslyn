using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class Binder
    {
        /// <summary>
        /// Appends diagnostics from useSiteDiagnostics into diagnostics and returns True if there were any errors.
        /// </summary>
        internal static bool AppendUseSiteDiagnostics(
            SyntaxNode node,
            HashSet<DiagnosticInfo> useSiteDiagnostics,
            DiagnosticBag diagnostics)
        {
            if (useSiteDiagnostics.IsNullOrEmpty())
            {
                return false;
            }

            bool haveErrors = false;

            foreach (var info in useSiteDiagnostics)
            {
                if (info.Severity == DiagnosticSeverity.Error)
                {
                    haveErrors = true;
                }

                Error(diagnostics, info, node);
            }

            return haveErrors;
        }

        internal static bool AppendUseSiteDiagnostics(
            Location location,
            HashSet<DiagnosticInfo> useSiteDiagnostics,
            DiagnosticBag diagnostics)
        {
            if (useSiteDiagnostics.IsNullOrEmpty())
            {
                return false;
            }

            bool haveErrors = false;

            foreach (var info in useSiteDiagnostics)
            {
                if (info.Severity == DiagnosticSeverity.Error)
                {
                    haveErrors = true;
                }

                Error(diagnostics, info, location);
            }

            return haveErrors;
        }
    }
}

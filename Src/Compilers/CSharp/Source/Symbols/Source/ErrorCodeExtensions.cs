
namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Building the symbol table lazily is done by pushing errors into local error bags by reference (they are null
    /// when there are no errors).  These methods provide a convenient way to push diagnostics into the bags.
    /// </summary>
    static class ErrorCodeExtensions
    {
        internal static CSDiagnosticInfo ReportDiagnostic(this ErrorCode code, ref DiagnosticBag diagnostics, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo(code, args);
            var diag = new Diagnostic(info, location);
            if (diagnostics == null)
                diagnostics = new DiagnosticBag();
            diagnostics.Add(diag);
            return info;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.UseNameOf
{
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.LanguageServices;
    using Microsoft.CodeAnalysis.UseNameof;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseNameOfDiagnosticAnalyzer : AbstractUseNameOfDiagnosticAnalyzer
    {
        protected override ISyntaxFactsService GetSyntaxFactsService() => CSharpSyntaxFactsService.Instance;
    }
}

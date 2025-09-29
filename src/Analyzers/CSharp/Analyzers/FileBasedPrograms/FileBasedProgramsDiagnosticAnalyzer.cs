using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FileBasedProgramsDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FBP0001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        title: "Stub file-based program diagnostic",
        messageFormat: "This is a stub diagnostic produced by the FileBasedPrograms analyzer",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        // TODO2: implement analyzer(s) for file-based program directives.
    }
}
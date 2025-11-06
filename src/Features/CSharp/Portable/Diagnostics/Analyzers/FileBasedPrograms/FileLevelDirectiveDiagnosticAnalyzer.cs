// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FileLevelDirectiveDiagnosticAnalyzer()
    : AbstractCodeQualityDiagnosticAnalyzer(
        descriptors: [Rule],
        generatedCodeAnalysisFlags: GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
{
    public const string DiagnosticId = "FileBasedPrograms";

    private static readonly DiagnosticDescriptor Rule = CreateDescriptor(
                id: DiagnosticId,
                enforceOnBuild: EnforceOnBuild.Never,
                title: DiagnosticId,
                messageFormat: "{0}",
                hasAnyCodeStyleOption: false,
                isUnnecessary: false,
                isEnabledByDefault: true,
                isConfigurable: false,
                defaultSeverity: DiagnosticSeverity.Error,
                helpLinkUri: "https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives#file-based-apps");

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            context.RegisterSyntaxTreeAction(context =>
            {
                var cancellationToken = context.CancellationToken;
                var tree = context.Tree;
                if (!tree.Options.Features.ContainsKey("FileBasedProgram"))
                    return;

                var root = tree.GetRoot(cancellationToken);
                if (!root.ContainsDirectives)
                    return;

                // The compiler already reports an error on all the directives past the first token in the file.
                // Therefore, the analyzer only deals with the directives on the first token.
                //     Console.WriteLine("Hello World!");
                //     #:property foo=bar // error CS9297: '#:' directives cannot be after first token in file
                var diagnosticBag = DiagnosticBag.Collect(out var diagnosticsBuilder);
                FileLevelDirectiveHelpers.FindLeadingDirectives(
                    new SourceFile(tree.FilePath, tree.GetText(cancellationToken)),
                    root.GetLeadingTrivia(),
                    diagnosticBag,
                    builder: null);

                foreach (var simpleDiagnostic in diagnosticsBuilder)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        location: Location.Create(tree, simpleDiagnostic.Location.TextSpan),
                        simpleDiagnostic.Message));
                }
            });
        });
    }
}

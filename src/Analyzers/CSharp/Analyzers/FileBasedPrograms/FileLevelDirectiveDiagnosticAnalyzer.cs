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

#pragma warning disable RS0030 // Do not use banned APIs
    // We would use 'AbstractCodeQualityDiagnosticAnalyzer.CreateDescriptor()', but, we want these diagnostics to have error severity.
    private static readonly DiagnosticDescriptor Rule = new(
                id: DiagnosticId,
                title: DiagnosticId,
                messageFormat: "{0}",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                helpLinkUri: "https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives#file-based-apps",
                // Note that this is an "editor-only" analyzer.
                // When building or running file-based apps, the dotnet cli uses its own process to report errors on file-level directives.
                customTags: DiagnosticCustomTags.Create(isUnnecessary: false, isConfigurable: false, isCustomConfigurable: false, enforceOnBuild: EnforceOnBuild.Never));
#pragma warning restore RS0030 // Do not use banned APIs

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            context.RegisterSyntaxTreeAction(visitSyntaxTree);
        });

        void visitSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            if (!tree.Options.Features.ContainsKey("FileBasedProgram"))
                return;

            var root = tree.GetRoot(context.CancellationToken);
            if (!root.ContainsDirectives)
                return;

            // App directives are only valid when they appear before the first C# token
            var rootLeadingTrivia = root.GetLeadingTrivia();
            var diagnosticBag = DiagnosticBag.Collect(out var diagnosticsBuilder);
            FileLevelDirectiveHelpers.FindLeadingDirectives(
                new SourceFile(tree.FilePath, tree.GetText(context.CancellationToken)),
                root.GetLeadingTrivia(),
                diagnosticBag,
                builder: null);

            foreach (var diag in diagnosticsBuilder)
            {
                context.ReportDiagnostic(createDiagnostic(tree, diag));
            }

            // The compiler already reports an error on all the directives past the first token in the file.
            // Therefore, the analyzer only deals with the directives on the first token.
            //     Console.WriteLine("Hello World!");
            //     #:property foo=bar // error CS9297: '#:' directives cannot be after first token in file
        }

        Diagnostic createDiagnostic(SyntaxTree syntaxTree, SimpleDiagnostic simpleDiagnostic)
        {
            return Diagnostic.Create(
                Rule,
                location: Location.Create(syntaxTree, simpleDiagnostic.Location.TextSpan),
                simpleDiagnostic.Message);
        }
    }
}

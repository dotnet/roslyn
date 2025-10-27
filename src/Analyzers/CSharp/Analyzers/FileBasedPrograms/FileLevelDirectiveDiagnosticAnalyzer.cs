// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class FileLevelDirectiveDiagnosticAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FileBasedPrograms";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        title: DiagnosticId,
        // TODO: we probably want to have a different diagnostic for each kind that the SDK package can produce
        messageFormat: "{0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

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
            //     Console.WriteLine("Hello World!");
            //     #:property foo=bar // error CS9297: '#:' directives cannot be after first token in file
        }

        // TODO: should SimpleDiagnostics have IDs? message args? TextSpan?
        // It feels unreasonable for users to suppress these.
        // When these are present, the user cannot build/run, period.
        Diagnostic createDiagnostic(SyntaxTree syntaxTree, SimpleDiagnostic simpleDiagnostic)
        {
            return Diagnostic.Create(
                Rule,
                location: Location.Create(syntaxTree, simpleDiagnostic.Location.TextSpan),
                simpleDiagnostic.Message);
        }
    }
}
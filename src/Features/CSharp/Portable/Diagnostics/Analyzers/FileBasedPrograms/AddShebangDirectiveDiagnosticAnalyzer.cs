// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

// TODO2: ship this in Features or in Analyzers? i.e. do we want this to always be editor-only?
// A file-based program, being built on command line, could theoretically be reporting code style diagnostics on build, because of a Directory.Build.props
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AddShebangDirectiveDiagnosticAnalyzer()
    : AbstractCodeQualityDiagnosticAnalyzer(
        descriptors: [s_descriptor],
        generatedCodeAnalysisFlags: GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics)
{
    private static readonly LocalizableResourceString s_localizableAddShebang = new(
       nameof(CSharpAnalyzersResources.Add_shebang), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptor(
                id: IDEDiagnosticIds.AddShebangDirective,
                enforceOnBuild: EnforceOnBuild.Never,
                title: s_localizableAddShebang,
                messageFormat: s_localizableAddShebang,
                hasAnyCodeStyleOption: false,
                isUnnecessary: false,
                isEnabledByDefault: true,
                isConfigurable: true);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterSyntaxTreeAction(context =>
        {
            var cancellationToken = context.CancellationToken;
            var tree = context.Tree;
            var features = tree.Options.Features;

            // Report only on "rich miscellaneous files" in VS Code.
            // TODO2: Perhaps this should also apply to file-based programs being built on command line.
            if (!features.ContainsKey("RichMiscellaneousFile"))
                return;

            var root = tree.GetRoot(cancellationToken);
            if (root.ContainsDirectives)
            {
                var leadingTriviaList = root.GetLeadingTrivia();
                foreach (var trivia in leadingTriviaList)
                {
                    // Has a '#!' or already. Don't report anything.
                    if (trivia.Kind() is SyntaxKind.ShebangDirectiveTrivia or SyntaxKind.IgnoredDirectiveTrivia)
                        return;
                }
            }

            // Didn't find any '#!' or '#:'. Look for top-level statements, which are a sign that file-based programs editor behavior may be desired.
            if (root is not CompilationUnitSyntax compilationUnit)
                return;

            foreach (var member in compilationUnit.Members)
            {
                if (member.IsKind(SyntaxKind.GlobalStatement))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            s_descriptor,
                            member.GetFirstToken().GetLocation()));
                    return;
                }
            }
        });
    }
}

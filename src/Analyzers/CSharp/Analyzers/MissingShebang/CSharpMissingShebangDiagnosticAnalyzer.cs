// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MissingShebang;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMissingShebangDiagnosticAnalyzer : AbstractCodeQualityDiagnosticAnalyzer
{
    private const string EntryPointFilePathOption = "build_property.EntryPointFilePath";

    private static readonly LocalizableResourceString s_localizableTitle = new(
        nameof(CSharpAnalyzersResources.File_based_program_entry_point_should_start_with_shebang), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

    private static readonly DiagnosticDescriptor s_diagnosticDescriptor = CreateDescriptor(
        IDEDiagnosticIds.MissingShebangInFileBasedProgramDiagnosticId,
        EnforceOnBuildValues.MissingShebangInFileBasedProgram,
        s_localizableTitle,
        s_localizableTitle,
        hasAnyCodeStyleOption: false, isUnnecessary: false,
        defaultSeverity: DiagnosticSeverity.Warning);

    public CSharpMissingShebangDiagnosticAnalyzer()
        : base([s_diagnosticDescriptor], GeneratedCodeAnalysisFlags.None)
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(EntryPointFilePathOption, out var entryPointFilePath)
                || string.IsNullOrEmpty(entryPointFilePath))
            {
                return;
            }

            context.RegisterSyntaxTreeAction(treeContext =>
            {
                if (!IsEntryPointFile(treeContext.Tree.FilePath, entryPointFilePath))
                    return;

                var root = treeContext.Tree.GetRoot(treeContext.CancellationToken);
                if (root.GetLeadingTrivia().Any(SyntaxKind.ShebangDirectiveTrivia))
                    return;

                var firstToken = root.GetFirstToken(includeZeroWidth: true);
                treeContext.ReportDiagnostic(Diagnostic.Create(s_diagnosticDescriptor, firstToken.GetLocation()));
            });
        });
    }

    private static bool IsEntryPointFile(string treeFilePath, string entryPointFilePath)
        => string.Equals(treeFilePath, entryPointFilePath, StringComparison.OrdinalIgnoreCase);
}

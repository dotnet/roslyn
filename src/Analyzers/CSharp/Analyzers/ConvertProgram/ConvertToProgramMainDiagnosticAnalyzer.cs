// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.TopLevelStatements
{
    using static ConvertProgramAnalysis;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ConvertToProgramMainDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConvertToProgramMainDiagnosticAnalyzer()
            : base(
                  IDEDiagnosticIds.UseProgramMainId,
                  EnforceOnBuildValues.UseProgramMain,
                  CSharpCodeStyleOptions.PreferTopLevelStatements,
                  LanguageNames.CSharp,
                  new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_Program_Main_style_program), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (!IsApplication(context.Compilation))
                    return;

                context.RegisterSyntaxNodeAction(ProcessCompilationUnit, SyntaxKind.CompilationUnit);
            });
        }

        private void ProcessCompilationUnit(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var root = (CompilationUnitSyntax)context.Node;

            var optionSet = options.GetAnalyzerOptionSet(root.SyntaxTree, context.CancellationToken);
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

            if (!CanOfferUseProgramMain(option, root, context.Compilation, forAnalyzer: true))
                return;

            var severity = option.Notification.Severity;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                GetUseProgramMainDiagnosticLocation(
                    root, isHidden: severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden),
                severity,
                ImmutableArray<Location>.Empty,
                ImmutableDictionary<string, string?>.Empty));
        }
    }
}

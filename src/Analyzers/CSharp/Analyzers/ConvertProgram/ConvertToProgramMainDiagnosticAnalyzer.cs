// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.TopLevelStatements
{
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
            => context.RegisterSyntaxNodeAction(ProcessCompilationUnit, SyntaxKind.CompilationUnit);

        private void ProcessCompilationUnit(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var root = (CompilationUnitSyntax)context.Node;

            var optionSet = options.GetAnalyzerOptionSet(root.SyntaxTree, context.CancellationToken);
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

            if (!ConvertProgramAnalysis.CanOfferUseProgramMain(option, root, context.Compilation, forAnalyzer: true))
                return;

            var severity = option.Notification.Severity;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                GetDiagnosticLocation(root, severity),
                severity,
                ImmutableArray<Location>.Empty,
                ImmutableDictionary<string, string?>.Empty));
        }

        private static Location GetDiagnosticLocation(CompilationUnitSyntax root, ReportDiagnostic severity)
        {
            // if the diagnostic is hidden, show it anywhere from the top of the file through the end of the last global
            // statement.  That way the user can make the change anywhere in teh top level code.  Otherwise, just put
            // the diagnostic on the start of the first global statement.
            if (severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) != ReportDiagnostic.Hidden)
                return root.Members.OfType<GlobalStatementSyntax>().First().GetFirstToken().GetLocation();

            return Location.Create(
                root.SyntaxTree,
                TextSpan.FromBounds(0, root.Members.OfType<GlobalStatementSyntax>().Last().FullSpan.End));
        }
    }
}

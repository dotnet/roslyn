// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.TopLevelStatements
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class ConvertToProgramMainDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
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
            var syntaxTree = root.SyntaxTree;

            var cancellationToken = context.CancellationToken;

            if (!HasGlobalStatement(root))
                return;

            var optionSet = options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);
            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

            // if they prefer top level statements, there's nothing for us to do as this code matches their preference
            if (option.Value)
                return;

            var compilation = context.Compilation;

            // resiliency check for later on.  This shouldn't happen but we don't want to crash if we are in a weird
            // state where we have top level statements but no 'Program' type.
            var programType = compilation.GetBestTypeByMetadataName(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName);
            if (programType == null)
                return;

            if (programType.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).FirstOrDefault() is not IMethodSymbol)
                return;

            var severity = option.Notification.Severity;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                GetDiagnosticLocation(root, severity),
                severity,
                ImmutableArray<Location>.Empty,
                ImmutableDictionary<string, string?>.Empty));
        }

        private static bool HasGlobalStatement(CompilationUnitSyntax root)
        {
            foreach (var member in root.Members)
            {
                if (member.Kind() is SyntaxKind.GlobalStatement)
                    return true;
            }

            return false;
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

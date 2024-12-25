// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.TopLevelStatements;

using static ConvertProgramAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ConvertToTopLevelStatementsDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public ConvertToTopLevelStatementsDiagnosticAnalyzer()
        : base(
              IDEDiagnosticIds.UseTopLevelStatementsId,
              EnforceOnBuildValues.UseTopLevelStatements,
              CSharpCodeStyleOptions.PreferTopLevelStatements,
              new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_top_level_statements), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // can only suggest moving to top level statement on c# 9 or above.
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9 ||
                !IsApplication(context.Compilation))
            {
                return;
            }

            context.RegisterSyntaxNodeAction(ProcessCompilationUnit, SyntaxKind.CompilationUnit);
        });
    }

    private void ProcessCompilationUnit(SyntaxNodeAnalysisContext context)
    {
        // Don't want to suggest moving if the user doesn't have a preference for top-level-statements.
        var option = context.GetCSharpAnalyzerOptions().PreferTopLevelStatements;
        if (ShouldSkipAnalysis(context, option.Notification)
            || !CanOfferUseTopLevelStatements(option, forAnalyzer: true))
        {
            return;
        }

        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;
        var mainTypeName = GetMainTypeName(compilation);

        // Ok, the user does like top level statements.  Check if we can find a suitable hit in this type that
        // indicates we're on the entrypoint of the program.
        var root = (CompilationUnitSyntax)context.Node;
        var methodDeclarations = root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or ClassDeclarationSyntax).OfType<MethodDeclarationSyntax>();
        foreach (var methodDeclaration in methodDeclarations)
        {
            if (IsProgramMainMethod(
                    semanticModel, methodDeclaration, mainTypeName, cancellationToken, out var canConvertToTopLevelStatement))
            {
                if (canConvertToTopLevelStatement)
                {
                    // Looks good.  Let the user know this type/method can be converted to a top level program.
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        this.Descriptor,
                        GetUseTopLevelStatementsDiagnosticLocation(
                            methodDeclaration, isHidden: option.Notification.Severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden),
                        option.Notification,
                        context.Options,
                        ImmutableArray.Create(methodDeclaration.GetLocation()),
                        ImmutableDictionary<string, string?>.Empty));
                }

                // We found the main method, but it's not convertible, bail out as we have nothing else to do.
                return;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class MisplacedUsingDirectivesDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableTitle = new(
           nameof(CSharpAnalyzersResources.Misplaced_using_directive), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableResourceString s_localizableOutsideMessage = new(
            nameof(CSharpAnalyzersResources.Using_directives_must_be_placed_outside_of_a_namespace_declaration), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly DiagnosticDescriptor s_outsideDiagnosticDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId,
            EnforceOnBuildValues.MoveMisplacedUsingDirectives,
            hasAnyCodeStyleOption: true,
            s_localizableTitle, s_localizableOutsideMessage);

        private static readonly LocalizableResourceString s_localizableInsideMessage = new(
            nameof(CSharpAnalyzersResources.Using_directives_must_be_placed_inside_of_a_namespace_declaration), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly DiagnosticDescriptor s_insideDiagnosticDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId,
            EnforceOnBuildValues.MoveMisplacedUsingDirectives,
            hasAnyCodeStyleOption: true,
            s_localizableTitle, s_localizableInsideMessage);

        public MisplacedUsingDirectivesDiagnosticAnalyzer()
           : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                    .Add(s_outsideDiagnosticDescriptor, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement)
                    .Add(s_insideDiagnosticDescriptor, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeCompilationUnitNode, SyntaxKind.CompilationUnit);
        }

        private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().UsingDirectivePlacement;
            if (option.Value != AddImportPlacement.OutsideNamespace
                || ShouldSkipAnalysis(context, option.Notification))
            {
                return;
            }

            var namespaceDeclaration = (BaseNamespaceDeclarationSyntax)context.Node;
            ReportDiagnostics(context, s_outsideDiagnosticDescriptor, namespaceDeclaration.Usings, option);
        }

        private void AnalyzeCompilationUnitNode(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().UsingDirectivePlacement;
            var compilationUnit = (CompilationUnitSyntax)context.Node;

            if (option.Value != AddImportPlacement.InsideNamespace
               || ShouldSkipAnalysis(context, option.Notification)
               || ShouldSuppressDiagnostic(compilationUnit))
            {
                return;
            }

            // Only report for non-global usings.  Global usings must stay at the compilation unit level.
            var nonGlobalUsings = compilationUnit.Usings.Where(u => u.GlobalKeyword == default);

            // Note: We will report diagnostics when a code file contains multiple namespaces even though we will not
            // offer a code fix in these cases.
            ReportDiagnostics(context, s_insideDiagnosticDescriptor, nonGlobalUsings, option);
        }

        private static bool ShouldSuppressDiagnostic(CompilationUnitSyntax compilationUnit)
        {
            // Suppress if there are nodes other than usings and namespaces in the 
            // compilation unit (including ExternAlias).
            return compilationUnit.ChildNodes().Any(
                t => t.Kind() is not (SyntaxKind.UsingDirective or SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration));
        }

        private static void ReportDiagnostics(
           SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
           IEnumerable<UsingDirectiveSyntax> usingDirectives, CodeStyleOption2<AddImportPlacement> option)
        {
            foreach (var usingDirective in usingDirectives)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    descriptor,
                    usingDirective.GetLocation(),
                    option.Notification,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}

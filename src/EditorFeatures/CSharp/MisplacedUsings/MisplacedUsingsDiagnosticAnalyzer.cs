// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class MisplacedUsingsDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly CodeStyleOption<UsingDirectivesPlacement> s_noPreferenceOption =
            new CodeStyleOption<UsingDirectivesPlacement>(UsingDirectivesPlacement.Preserve, NotificationOption.None);

        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
            nameof(CSharpEditorResources.Misplaced_using), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_inside_of_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        private static readonly LocalizableResourceString s_localizableOutsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_outside_of_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        internal static readonly DiagnosticDescriptor _insideDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId,
            s_localizableTitle,
            s_localizableInsideMessage,
            configurable: true);

        internal static readonly DiagnosticDescriptor _outsideDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId,
            s_localizableTitle,
            s_localizableOutsideMessage,
            configurable: true);

        public MisplacedUsingsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId, s_localizableTitle)
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCompilationUnitNode, SyntaxKind.CompilationUnit);
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzeCompilationUnitNode(SyntaxNodeAnalysisContext context)
        {
            var option = GetPreferredPlacementOption(context);
            var compilationUnit = (CompilationUnitSyntax)context.Node;

            if (option.Value != UsingDirectivesPlacement.InsideNamespace
                || ShouldSuppressDiagnostic(compilationUnit))
            {
                return;
            }

            ReportDiagnostics(context, _insideDescriptor, compilationUnit.Usings, option);
        }

        private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            var option = GetPreferredPlacementOption(context);
            if (option.Value != UsingDirectivesPlacement.OutsideNamespace)
            {
                return;
            }

            var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
            ReportDiagnostics(context, _outsideDescriptor, namespaceDeclaration.Usings, option);
        }

        private CodeStyleOption<UsingDirectivesPlacement> GetPreferredPlacementOption(SyntaxNodeAnalysisContext context)
        {
            return context.GetOptionOrDefaultAsync(
                CSharpCodeStyleOptions.PreferredUsingDirectivesPlacement, CSharpCodeStyleOptions.ParseUsingDirectivesPlacement,
                s_noPreferenceOption).GetAwaiter().GetResult();
        }

        private bool ShouldSuppressDiagnostic(CompilationUnitSyntax compilationUnit)
        {
            // Suppress if there are nodes other than usings and namespaces in the compilation unit.
            return !compilationUnit.ChildNodes().All(node =>
                node.IsKind(SyntaxKind.UsingDirective)
                || node.IsKind(SyntaxKind.NamespaceDeclaration));
        }

        private void ReportDiagnostics(
            SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
            IEnumerable<UsingDirectiveSyntax> usingDirectives, CodeStyleOption<UsingDirectivesPlacement> option)
        {
            foreach (var usingDirective in usingDirectives)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    descriptor,
                    usingDirective.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}

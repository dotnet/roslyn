// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsings
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class MisplacedUsingsInCompilationUnitDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_inside_of_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        // Created for unit tests to validate against 
        internal static readonly DiagnosticDescriptor InsideDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId,
            MisplacedUsingsUtilities.LocalizableTitle,
            s_localizableInsideMessage,
            isConfigurable: true);

        public MisplacedUsingsInCompilationUnitDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MoveMisplacedUsingsDiagnosticId, MisplacedUsingsUtilities.LocalizableTitle, s_localizableInsideMessage)
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCompilationUnitNode, SyntaxKind.CompilationUnit);
        }

        private void AnalyzeCompilationUnitNode(SyntaxNodeAnalysisContext context)
        {
            var option = MisplacedUsingsUtilities.GetPreferredPlacementOptionAsync(context).GetAwaiter().GetResult();
            var compilationUnit = (CompilationUnitSyntax)context.Node;

            if (option.Value != AddImportPlacement.InsideNamespace
                || ShouldSuppressDiagnostic(compilationUnit))
            {
                return;
            }

            MisplacedUsingsUtilities.ReportDiagnostics(context, Descriptor, compilationUnit.Usings, option);
        }

        private bool ShouldSuppressDiagnostic(CompilationUnitSyntax compilationUnit)
        {
            // Suppress if there are nodes other than usings and namespaces in the compilation unit.
            return !compilationUnit.ChildNodes().All(node =>
                node.IsKind(SyntaxKind.UsingDirective)
                || node.IsKind(SyntaxKind.NamespaceDeclaration));
        }
    }
}

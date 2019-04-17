// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp;

namespace Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class MisplacedUsingDirectivesInNamespaceDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableOutsideMessage = new LocalizableResourceString(
            nameof(CSharpEditorResources.Using_directives_must_be_placed_outside_of_a_namespace_declaration), CSharpEditorResources.ResourceManager, typeof(CSharpEditorResources));

        public MisplacedUsingDirectivesInNamespaceDiagnosticAnalyzer()
           : base(IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId, MisplacedUsingsUtilities.LocalizableTitle, s_localizableOutsideMessage)
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            var option = MisplacedUsingsUtilities.GetPreferredPlacementOptionAsync(context).GetAwaiter().GetResult();
            if (option.Value != AddImportPlacement.OutsideNamespace)
            {
                return;
            }

            var namespaceDeclaration = (NamespaceDeclarationSyntax)context.Node;
            MisplacedUsingsUtilities.ReportDiagnostics(context, Descriptor, namespaceDeclaration.Usings, option);
        }
    }
}

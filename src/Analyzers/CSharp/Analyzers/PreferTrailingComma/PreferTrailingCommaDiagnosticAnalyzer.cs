// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.PreferTrailingComma
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class PreferTrailingCommaDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public PreferTrailingCommaDiagnosticAnalyzer() : base(
            diagnosticId: IDEDiagnosticIds.PreferTrailingCommaDiagnosticId,
            enforceOnBuild: EnforceOnBuildValues.PreferTrailingComma,
            option: CSharpCodeStyleOptions.PreferTrailingComma,
            language: LanguageNames.CSharp,
            title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_to_Program_Main_style_program), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            // enum members
            // list patterns
            // property pattern
            // anonymous object creation
            // initializer expression
            // switch expression
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.EnumDeclaration);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().PreferTrailingComma;
            if (!option.Value)
                return;

            var node = context.Node;
            switch (node)
            {
                case EnumDeclarationSyntax enumDeclaration:
                    AnalyzeEnumDeclaration(enumDeclaration);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static void AnalyzeEnumDeclaration(EnumDeclarationSyntax enumDeclaration)
        {
            var members = enumDeclaration.Members;
        }
    }
}

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
            title: new LocalizableResourceString(nameof(CSharpAnalyzersResources.Missing_trailing_comma), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            // TODO:
            // list patterns
            // property pattern
            // anonymous object creation
            // initializer expression
            // switch expression
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.EnumDeclaration);
        }

        private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            var option = context.GetCSharpAnalyzerOptions().PreferTrailingComma;
            if (!option.Value)
                return;

            var node = context.Node;
            switch (node)
            {
                case EnumDeclarationSyntax enumDeclaration:
                    AnalyzeEnumDeclaration(context, enumDeclaration, option.Notification.Severity);
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context, EnumDeclarationSyntax enumDeclaration, ReportDiagnostic severity)
        {
            var nodesWithSeparators = enumDeclaration.Members.GetWithSeparators();
            if (nodesWithSeparators.Count < 1)
            {
                return;
            }

            var lastNodeOrSeparator = nodesWithSeparators[^1];
            if (lastNodeOrSeparator.IsToken)
            {
                return;
            }

            if (lastNodeOrSeparator.IsNode)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor, lastNodeOrSeparator.AsNode()!.GetLocation(), severity, additionalLocations: null, properties: null));
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.NewFolder
{
    internal class CSharpUseDeconstructionDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        protected CSharpUseDeconstructionDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseDeconstructionDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Inline_variable_declaration), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Variable_declaration_can_be_inlined), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            switch (context.Node.Kind())
            {
                case SyntaxKind.VariableDeclaration:
                    AnalyzeVariableDeclaration(context);
                    return;
                case SyntaxKind.ForEachStatement:
                    AnalyzeForEachStatement(context);
                    return;
            }
        }

        private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            if (!node.IsParentKind(SyntaxKind.LocalDeclarationStatement))
            {
                return;
            }

            var variableDeclaration = (VariableDeclarationSyntax)node;
            if (!variableDeclaration.Type.IsVar)
            {
                return;
            }

            if (variableDeclaration.Variables.Count != 1)
            {
                return;
            }

            var declarator = variableDeclaration.Variables[0];
            if (declarator.Initializer == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var type = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type;
            if (type == null || !type.IsTupleType)
            {
                return;
            }

            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            var searchScope = node.Parent.Parent;
            var variableName = declarator.Identifier.ValueText;

            if (!OnlyUsedToAccessTupleFields(searchScope, local))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                this.GetDescriptorWithSeverity(DiagnosticSeverity.Info)))
        }
    }
}

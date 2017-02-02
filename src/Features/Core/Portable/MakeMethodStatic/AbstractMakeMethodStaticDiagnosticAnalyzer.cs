// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.MakeMethodStatic
{
    internal abstract class AbstractMakeMethodStaticDiagnosticAnalyzer<TSyntaxKind, TMethodDeclarationSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TMethodDeclarationSyntax : SyntaxNode
    {
        protected AbstractMakeMethodStaticDiagnosticAnalyzer(LocalizableString title, LocalizableString message)
            : base(IDEDiagnosticIds.MakeMethodStaticDiagnosticId, title, message)
        {
        }

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, GetMethodDeclarationSyntaxKind());

        public sealed override bool OpenFileOnly(Workspace workspace) => true;
        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var declaration = (TMethodDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var methodSymbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            if (methodSymbol == null)
            {
                return;
            }

            if (methodSymbol.IsStatic ||
                methodSymbol.DeclaredAccessibility != Accessibility.Private)
            {
                return;
            }

            var body = GetBody(declaration);
            if (body == null)
            {
                return;
            }

            var dataFlowAnalysisData = semanticModel.AnalyzeDataFlow(body);
            var isInstanceMemberUsed =
                dataFlowAnalysisData.ReadInside.Any(IsThis)
                || methodSymbol.ContainingType.IsValueType
                && dataFlowAnalysisData.WrittenInside.Any(IsThis);

            if (isInstanceMemberUsed)
            {
                return;
            }

            // TODO: Get the severity from user preference
            context.ReportDiagnostic(Diagnostic.Create(
                CreateDescriptorWithSeverity(DiagnosticSeverity.Info),
                declaration.GetLocation()));
        }

        protected abstract SyntaxNode GetBody(TMethodDeclarationSyntax declaration);
        protected abstract TSyntaxKind GetMethodDeclarationSyntaxKind();

        private static bool IsThis(ISymbol s) => (s as IParameterSymbol)?.IsThis == true;
    }
}

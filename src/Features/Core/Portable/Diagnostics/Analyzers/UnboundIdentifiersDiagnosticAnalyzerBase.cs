// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.AddImport
{
    internal abstract class UnboundIdentifiersDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIncompleteMemberSyntax, TLambdaExpressionSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer
        where TLanguageKindEnum : struct
        where TSimpleNameSyntax : SyntaxNode
        where TQualifiedNameSyntax : SyntaxNode
        where TIncompleteMemberSyntax : SyntaxNode
        where TLambdaExpressionSyntax : SyntaxNode
    {
        protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
        protected abstract DiagnosticDescriptor DiagnosticDescriptor2 { get; }
        protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
        protected abstract bool ConstructorDoesNotExist(SyntaxNode node, SymbolInfo info, SemanticModel semanticModel);
        protected abstract bool IsNameOf(SyntaxNode node);
        protected abstract bool IsVarParenthesisDeclaration(SyntaxNode node);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptor, DiagnosticDescriptor2);
        public bool OpenFileOnly(Workspace workspace) => false;

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest.ToArray());
        }

        protected DiagnosticDescriptor GetDiagnosticDescriptor(string id, LocalizableString messageFormat)
        {
            // it is not configurable diagnostic, title doesn't matter
            return new DiagnosticDescriptor(
                id, string.Empty, messageFormat,
                DiagnosticCategory.Compiler,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (IsBrokenLambda(context) || context.Node is TIncompleteMemberSyntax)
            {
                ReportUnboundIdentifierNames(context, context.Node);
            }
        }

        private static bool IsBrokenLambda(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is TLambdaExpressionSyntax)
            {
                if (context.Node.ContainsDiagnostics)
                {
                    return true;
                }

                var lastToken = context.Node.GetLastToken();
                return lastToken.GetNextToken(includeZeroWidth: true).IsMissing;
            }

            return false;
        }

        private static bool isQualifiedOrSimpleName(SyntaxNode n)
        {
            return n is TQualifiedNameSyntax || n is TSimpleNameSyntax;
        }

        private void ReportUnboundIdentifierNames(SyntaxNodeAnalysisContext context, SyntaxNode member)
        {
            //static bool isQualifiedOrSimpleName(SyntaxNode n) => n is TQualifiedNameSyntax || n is TSimpleNameSyntax;
            var typeNames = member.DescendantNodes().Where(n => isQualifiedOrSimpleName(n) && !n.Span.IsEmpty);
            foreach (var typeName in typeNames)
            {
                var info = context.SemanticModel.GetSymbolInfo(typeName);
                if (info.Symbol == null && info.CandidateSymbols.Length == 0)
                {
                    // GetSymbolInfo returns no symbols for "nameof" expression, so handle it specially.
                    if (IsNameOf(typeName) || IsVarParenthesisDeclaration(typeName))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, typeName.GetLocation(), typeName.ToString()));
                }
                else if (ConstructorDoesNotExist(typeName, info, context.SemanticModel))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor2, typeName.GetLocation(), typeName.ToString()));
                }
            }
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}

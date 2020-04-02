// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
{
    internal abstract class AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer<
        TSyntaxKind,
        TAnonymousObjectCreationExpressionSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TAnonymousObjectCreationExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxKinds _syntaxKinds;

        protected AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer(ISyntaxKinds syntaxKinds)
            : base(IDEDiagnosticIds.ConvertAnonymousTypeToTupleDiagnosticId,
                   option: null,
                   new LocalizableResourceString(nameof(AnalyzersResources.Convert_to_tuple), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Convert_to_tuple), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            _syntaxKinds = syntaxKinds;
        }

        protected abstract int GetInitializerCount(TAnonymousObjectCreationExpressionSyntax anonymousType);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                AnalyzeSyntax,
                _syntaxKinds.Convert<TSyntaxKind>(_syntaxKinds.AnonymousObjectCreationExpression));

        // Analysis is trivial.  All anonymous types with more than two fields are marked as being
        // convertible to a tuple.
        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var anonymousType = (TAnonymousObjectCreationExpressionSyntax)context.Node;
            if (GetInitializerCount(anonymousType) < 2)
            {
                return;
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, context.Node.GetFirstToken().GetLocation(), ReportDiagnostic.Hidden,
                    additionalLocations: null, properties: null));
        }
    }
}

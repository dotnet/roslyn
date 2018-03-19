// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses;

namespace Microsoft.CodeAnalysis.AddRequiredParentheses
{
    internal abstract class AbstractAddRequiredParenthesesDiagnosticAnalyzer<TLanguageKindEnum>
        : AbstractParenthesesDiagnosticAnalyzer
        where TLanguageKindEnum : struct
    {
        protected AbstractAddRequiredParenthesesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddRequiredParenthesesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_parentheses_for_clarity), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Parentheses_should_be_added_for_clarity), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKinds());

        protected abstract ImmutableArray<TLanguageKindEnum> GetSyntaxNodeKinds();

        protected abstract void AnalyzeSyntax(SyntaxNodeAnalysisContext context);
    }
}

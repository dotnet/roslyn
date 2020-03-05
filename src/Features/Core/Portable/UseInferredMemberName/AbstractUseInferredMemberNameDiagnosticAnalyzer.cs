// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseInferredMemberName
{
    internal abstract class AbstractUseInferredMemberNameDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        abstract protected void LanguageSpecificAnalyzeSyntax(SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, AnalyzerOptions options, CancellationToken cancellationToken);

        public AbstractUseInferredMemberNameDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId,
                   options: ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, CodeStyleOptions.PreferInferredTupleNames),
                   new LocalizableResourceString(nameof(FeaturesResources.Use_inferred_member_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Member_name_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var options = context.Options;

            LanguageSpecificAnalyzeSyntax(context, syntaxTree, options, cancellationToken);
        }
    }
}

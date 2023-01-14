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
    internal abstract class AbstractUseInferredMemberNameDiagnosticAnalyzer : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    {
        protected abstract void AnalyzeSyntax(SyntaxNodeAnalysisContext context);

        public AbstractUseInferredMemberNameDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId,
                   EnforceOnBuildValues.UseInferredMemberName,
                   options: ImmutableHashSet.Create<IOption2>(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, CodeStyleOptions2.PreferInferredTupleNames),
                   fadingOption: null,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_inferred_member_name), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Member_name_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}

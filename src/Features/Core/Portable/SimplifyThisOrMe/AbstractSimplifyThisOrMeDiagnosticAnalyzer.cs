// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SimplifyThisOrMe
{
    internal abstract class AbstractSimplifyThisOrMeDiagnosticAnalyzer<
        TLanguageKindEnum,
        TExpressionSyntax,
        TThisExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TSimplifierOptions> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TExpressionSyntax : SyntaxNode
        where TThisExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TSimplifierOptions : SimplifierOptions
    {
        protected AbstractSimplifyThisOrMeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveThisOrMeQualificationDiagnosticId,
                   EnforceOnBuildValues.RemoveQualification,
                   ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOptions2.QualifyPropertyAccess, CodeStyleOptions2.QualifyMethodAccess, CodeStyleOptions2.QualifyEventAccess),
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Name_can_be_simplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                   isUnnecessary: true)
        {
        }

        protected abstract ISyntaxKinds SyntaxKinds { get; }
        protected abstract TSimplifierOptions GetSimplifierOptions(AnalyzerOptions options, SyntaxTree syntaxTree);

        protected abstract AbstractMemberAccessExpressionSimplifier<TExpressionSyntax, TMemberAccessExpressionSyntax, TThisExpressionSyntax> Simplifier { get; }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, this.SyntaxKinds.Convert<TLanguageKindEnum>(this.SyntaxKinds.ThisExpression));

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var node = context.Node;
            var semanticModel = context.SemanticModel;

            if (node.Parent is not TMemberAccessExpressionSyntax memberAccessExpression)
                return;

            var syntaxTree = node.SyntaxTree;
            var simplifierOptions = GetSimplifierOptions(context.Options, syntaxTree);

            if (!this.Simplifier.ShouldSimplifyThisMemberAccessExpression(
                    memberAccessExpression, semanticModel, simplifierOptions, out var thisExpression, out var severity, cancellationToken))
            {
                return;
            }

            var builder = ImmutableDictionary.CreateBuilder<string, string?>();

            // used so we can provide a link in the preview to the options page. This value is
            // hard-coded there to be the one that will go to the code-style page.
            builder["OptionName"] = nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration);
            builder["OptionLanguage"] = semanticModel.Language;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor, thisExpression.GetLocation(), severity,
                ImmutableArray.Create(memberAccessExpression.GetLocation()),
                builder.ToImmutable()));
        }
    }
}

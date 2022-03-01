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
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SimplifyThisOrMe
{
    internal abstract class AbstractSimplifyThisOrMeDiagnosticAnalyzer<
        TLanguageKindEnum,
        TExpressionSyntax,
        TThisExpressionSyntax,
        TMemberAccessExpressionSyntax> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TExpressionSyntax : SyntaxNode
        where TThisExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
    {
        private readonly ImmutableArray<TLanguageKindEnum> _kindsOfInterest;

        protected AbstractSimplifyThisOrMeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                   EnforceOnBuildValues.RemoveQualification,
                   ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions2.QualifyFieldAccess, CodeStyleOptions2.QualifyPropertyAccess, CodeStyleOptions2.QualifyMethodAccess, CodeStyleOptions2.QualifyEventAccess),
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Name_can_be_simplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                   isUnnecessary: true)
        {
            var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
            _kindsOfInterest = ImmutableArray.Create(
                syntaxKinds.Convert<TLanguageKindEnum>(syntaxKinds.ThisExpression));
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();

        protected abstract bool CanSimplifyTypeNameExpression(
            SemanticModel model, TMemberAccessExpressionSyntax memberAccess, OptionSet optionSet, out TextSpan issueSpan, CancellationToken cancellationToken);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, _kindsOfInterest);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var node = (TThisExpressionSyntax)context.Node;

            if (node.Parent is not TMemberAccessExpressionSyntax expr)
            {
                return;
            }

            var analyzerOptions = context.Options;
            var syntaxTree = node.SyntaxTree;
            var optionSet = analyzerOptions.GetAnalyzerOptionSet(syntaxTree, cancellationToken);

            var model = context.SemanticModel;
            if (!CanSimplifyTypeNameExpression(
                    model, expr, optionSet, out var issueSpan, cancellationToken))
            {
                return;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                return;
            }

            var symbolInfo = model.GetSymbolInfo(expr, cancellationToken);
            if (symbolInfo.Symbol == null)
            {
                return;
            }

            var applicableOption = QualifyMembersHelpers.GetApplicableOptionFromSymbolKind(symbolInfo.Symbol.Kind);
            var optionValue = optionSet.GetOption(applicableOption, model.Language);
            if (optionValue == null)
            {
                return;
            }

            var severity = optionValue.Notification.Severity;
            var builder = ImmutableDictionary.CreateBuilder<string, string?>();

            // used so we can provide a link in the preview to the options page. This value is
            // hard-coded there to be the one that will go to the code-style page.
            builder["OptionName"] = nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration);
            builder["OptionLanguage"] = model.Language;

            var diagnostic = DiagnosticHelper.Create(
                Descriptor, syntaxTree.GetLocation(issueSpan), severity,
                ImmutableArray.Create(expr.GetLocation()), builder.ToImmutable());

            context.ReportDiagnostic(diagnostic);
        }
    }
}

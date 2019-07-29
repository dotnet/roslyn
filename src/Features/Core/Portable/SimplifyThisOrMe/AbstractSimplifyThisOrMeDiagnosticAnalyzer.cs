// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        protected AbstractSimplifyThisOrMeDiagnosticAnalyzer(
            ImmutableArray<TLanguageKindEnum> kindsOfInterest)
            : base(IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                   ImmutableHashSet.Create<IPerLanguageOption>(CodeStyleOptions.QualifyFieldAccess, CodeStyleOptions.QualifyPropertyAccess, CodeStyleOptions.QualifyMethodAccess, CodeStyleOptions.QualifyEventAccess),
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_qualification), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Name_can_be_simplified), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)))
        {
            _kindsOfInterest = kindsOfInterest;
        }

        protected abstract string GetLanguageName();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected abstract bool CanSimplifyTypeNameExpression(
            SemanticModel model, TMemberAccessExpressionSyntax memberAccess, OptionSet optionSet, out TextSpan issueSpan, CancellationToken cancellationToken);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, _kindsOfInterest);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var node = (TMemberAccessExpressionSyntax)context.Node;

            var syntaxFacts = GetSyntaxFactsService();
            var expr = syntaxFacts.GetExpressionOfMemberAccessExpression(node);
            if (!(expr is TThisExpressionSyntax))
            {
                return;
            }

            var analyzerOptions = context.Options;

            var syntaxTree = node.SyntaxTree;
            var optionSet = analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var model = context.SemanticModel;
            if (!CanSimplifyTypeNameExpression(
                    model, node, optionSet, out var issueSpan, cancellationToken))
            {
                return;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                return;
            }

            var symbolInfo = model.GetSymbolInfo(node, cancellationToken);
            if (symbolInfo.Symbol == null)
            {
                return;
            }

            var applicableOption = QualifyMembersHelpers.GetApplicableOptionFromSymbolKind(symbolInfo.Symbol.Kind);
            var optionValue = optionSet.GetOption(applicableOption, GetLanguageName());
            var severity = optionValue.Notification.Severity;

            var descriptor = CreateUnnecessaryDescriptor(DescriptorId);
            if (descriptor == null)
            {
                return;
            }

            var tree = model.SyntaxTree;
            var builder = ImmutableDictionary.CreateBuilder<string, string>();

            // used so we can provide a link in the preview to the options page. This value is
            // hard-coded there to be the one that will go to the code-style page.
            builder["OptionName"] = nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration);
            builder["OptionLanguage"] = model.Language;

            var diagnostic = DiagnosticHelper.Create(
                descriptor, tree.GetLocation(issueSpan), severity,
                ImmutableArray.Create(node.GetLocation()), builder.ToImmutable());

            context.ReportDiagnostic(diagnostic);
        }
    }
}

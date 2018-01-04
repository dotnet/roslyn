// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RegularExpressions;

namespace Microsoft.CodeAnalysis.ValidateRegexString
{
    internal abstract class AbstractValidateRegexStringDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private readonly int _stringLiteralKind;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly IVirtualCharService _virtualCharService;

        protected AbstractValidateRegexStringDiagnosticAnalyzer(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(IDEDiagnosticIds.RegexPatternDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Regex_issue_0), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _stringLiteralKind = stringLiteralKind;
            _syntaxFacts = syntaxFacts;
            _semanticFacts = semanticFacts;
            _virtualCharService = virtualCharService;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(RegularExpressionsOptions.ReportInvalidRegexPatterns, syntaxTree.Options.Language);
            if (!option)
            {
                return;
            }

            var detector = RegexPatternDetector.TryCreate(semanticModel, _syntaxFacts, _semanticFacts);
            if (detector == null)
            {
                return;
            }

            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, _virtualCharService, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context, RegexPatternDetector detector,
            IVirtualCharService virtualCharService, SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Analyze(context, detector, virtualCharService, child.AsNode(), cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
                    if (token.RawKind == _stringLiteralKind)
                    {
                        var tree = detector.TryParseRegexPattern(token, virtualCharService, cancellationToken);
                        if (tree != null)
                        {
                            foreach (var diag in tree.Diagnostics)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    this.GetDescriptorWithSeverity(DiagnosticSeverity.Warning),
                                    Location.Create(context.SemanticModel.SyntaxTree, diag.Span),
                                    diag.Message));
                            }
                        }
                    }
                }
            }
        }
    }
}

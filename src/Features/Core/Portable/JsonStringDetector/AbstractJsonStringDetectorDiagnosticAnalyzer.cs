// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Json;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.JsonStringDetector
{
    internal abstract class AbstractJsonStringDetectorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string StrictKey = nameof(StrictKey);

        private readonly int _stringLiteralKind;
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly ISemanticFactsService _semanticFacts;
        private readonly IVirtualCharService _virtualCharService;

        protected AbstractJsonStringDetectorDiagnosticAnalyzer(
            int stringLiteralKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(IDEDiagnosticIds.JsonDetectionDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Probable_JSON_string_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
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

            var option = optionSet.GetOption(JsonOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings, syntaxTree.Options.Language);
            if (!option)
            {
                return;
            }

            var detector = JsonPatternDetector.GetOrCreate(
                semanticModel, _syntaxFacts, _semanticFacts, _virtualCharService);

            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context, JsonPatternDetector detector, 
            SyntaxNode node, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Analyze(context, detector, child.AsNode(), cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
                    if (token.RawKind == _stringLiteralKind &&
                        !JsonPatternDetector.IsDefinitelyNotJson(token, _syntaxFacts) &&
                        !detector.IsDefinitelyJson(token, cancellationToken) &&
                        detector.IsProbablyJson(token, cancellationToken))
                    {
                        var chars = _virtualCharService.TryConvertToVirtualChars(token);
                        var strictTree = JsonParser.TryParse(chars, strict: true);
                        var properties = strictTree != null && strictTree.Diagnostics.Length == 0
                            ? ImmutableDictionary<string, string>.Empty.Add(StrictKey, "")
                            : ImmutableDictionary<string, string>.Empty;

                        context.ReportDiagnostic(Diagnostic.Create(
                            this.GetDescriptorWithSeverity(DiagnosticSeverity.Info),
                            token.GetLocation(),
                            properties));
                    }
                }
            }
        }
    }
}

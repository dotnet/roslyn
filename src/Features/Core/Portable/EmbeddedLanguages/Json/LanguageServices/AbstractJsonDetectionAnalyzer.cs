// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Analyzer that helps find strings that are likely to be JSON and which we should offer the
    /// enable language service features for.
    /// </summary>
    internal abstract class AbstractJsonDetectionAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string DiagnosticId = "JSON002";
        public const string StrictKey = nameof(StrictKey);

        private static readonly ImmutableDictionary<string, string?> s_strictProperties =
            ImmutableDictionary<string, string?>.Empty.Add(StrictKey, "");

        private readonly EmbeddedLanguageInfo _info;

        protected AbstractJsonDetectionAnalyzer(EmbeddedLanguageInfo info)
            : base(DiagnosticId,
                   EnforceOnBuildValues.DetectProbableJsonStrings,
                   JsonFeatureOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings,
                   new LocalizableResourceString(nameof(FeaturesResources.Probable_JSON_string_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Probable_JSON_string_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _info = info;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Options.OptionSet options)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(Analyze);

        public void Analyze(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var option = context.GetOption(JsonFeatureOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings, syntaxTree.Options.Language);
            if (!option)
                return;

            var detector = JsonLanguageDetector.GetOrCreate(semanticModel.Compilation, _info);
            var root = syntaxTree.GetRoot(cancellationToken);
            Analyze(context, detector, root, cancellationToken);
        }

        private void Analyze(
            SemanticModelAnalysisContext context,
            JsonLanguageDetector detector,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Analyze(context, detector, child.AsNode()!, cancellationToken);
                }
                else
                {
                    var token = child.AsToken();
                    if (token.RawKind == _info.StringLiteralTokenKind &&
                        detector.TryParseString(token, context.SemanticModel, cancellationToken) == null &&
                        IsProbablyJson(token))
                    {
                        var chars = _info.VirtualCharService.TryConvertToVirtualChars(token);
                        var strictTree = JsonParser.TryParse(chars, JsonOptions.Strict);
                        var properties = strictTree != null && strictTree.Diagnostics.Length == 0
                            ? s_strictProperties
                            : ImmutableDictionary<string, string?>.Empty;

                        context.ReportDiagnostic(DiagnosticHelper.Create(
                            this.Descriptor,
                            token.GetLocation(),
                            ReportDiagnostic.Info,
                            additionalLocations: null,
                            properties));
                    }
                }
            }
        }

        public bool IsProbablyJson(SyntaxToken token)
        {
            var chars = _info.VirtualCharService.TryConvertToVirtualChars(token);
            var tree = JsonParser.TryParse(chars, JsonOptions.Newtonsoft);
            if (tree == null || !tree.Diagnostics.IsEmpty)
                return false;

            return ContainsProbableJsonObject(tree.Root);
        }

        private static bool ContainsProbableJsonObject(JsonNode node)
        {
            if (node.Kind == JsonKind.Object)
            {
                var objNode = (JsonObjectNode)node;
                if (objNode.Sequence.Length >= 1)
                    return true;
            }

            foreach (var child in node)
            {
                if (child.IsNode)
                {
                    if (ContainsProbableJsonObject(child.Node))
                        return true;
                }
            }

            return false;
        }
    }
}

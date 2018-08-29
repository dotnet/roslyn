// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices
{
    /// <summary>
    /// Analyzer that helps find strings that are likely to be JSON and which we should offer the
    /// enable language service features for.
    /// </summary>
    internal class JsonDetectionAnalyzer : IEmbeddedDiagnosticAnalyzer
    {
        public const string DiagnosticId = "JSON002";
        public const string StrictKey = nameof(StrictKey);

        private static readonly ImmutableDictionary<string, string> s_strictProperties =
            ImmutableDictionary<string, string>.Empty.Add(StrictKey, "");

        private readonly JsonEmbeddedLanguage _language;
        private readonly DiagnosticDescriptor _descriptor;

        public JsonDetectionAnalyzer(JsonEmbeddedLanguage language)
        {
            _language = language;

            _descriptor = new DiagnosticDescriptor(DiagnosticId,
                new LocalizableResourceString(nameof(WorkspacesResources.Probable_JSON_string_detected), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                new LocalizableResourceString(nameof(WorkspacesResources.Probable_JSON_string_detected), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                "JSON",
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

            SupportedDiagnostics = ImmutableArray.Create(_descriptor);
        }

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public void Analyze(SemanticModelAnalysisContext context, OptionSet optionSet)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var options = context.Options;
            var option = optionSet.GetOption(JsonFeatureOptions.DetectAndOfferEditorFeaturesForProbableJsonStrings, syntaxTree.Options.Language);
            if (!option)
            {
                return;
            }

            var detector = JsonPatternDetector.GetOrCreate(semanticModel, _language);

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
                    if (token.RawKind == _language.StringLiteralKind &&
                        !JsonPatternDetector.IsDefinitelyNotJson(token, _language.SyntaxFacts) &&
                        !detector.IsDefinitelyJson(token, cancellationToken) &&
                        detector.IsProbablyJson(token))
                    {
                        var chars = _language.VirtualCharService.TryConvertToVirtualChars(token);
                        var strictTree = JsonParser.TryParse(chars, JsonOptions.Strict);
                        var properties = strictTree != null && strictTree.Diagnostics.Length == 0
                            ? s_strictProperties
                            : ImmutableDictionary<string, string>.Empty;

                        context.ReportDiagnostic(Diagnostic.Create(
                            _descriptor, token.GetLocation(), properties));
                    }
                }
            }
        }
    }
}

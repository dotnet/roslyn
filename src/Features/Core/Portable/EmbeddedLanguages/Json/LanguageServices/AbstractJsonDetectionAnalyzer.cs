// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;

/// <summary>
/// Analyzer that helps find strings that are likely to be JSON and which we should offer the
/// enable language service features for.
/// </summary>
internal abstract class AbstractJsonDetectionAnalyzer(EmbeddedLanguageInfo info)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(DiagnosticId,
        EnforceOnBuildValues.DetectProbableJsonStrings,
        option: null,
        new LocalizableResourceString(nameof(FeaturesResources.Probable_JSON_string_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
        new LocalizableResourceString(nameof(FeaturesResources.Probable_JSON_string_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
{
    public const string DiagnosticId = "JSON002";
    public const string StrictKey = nameof(StrictKey);

    private static readonly ImmutableDictionary<string, string?> s_strictProperties =
        ImmutableDictionary<string, string?>.Empty.Add(StrictKey, "");

    private readonly EmbeddedLanguageInfo _info = info;

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(Analyze);

    public void Analyze(SemanticModelAnalysisContext context)
    {
        if (!context.GetAnalyzerOptions().GetOption(JsonDetectionOptionsStorage.DetectAndOfferEditorFeaturesForProbableJsonStrings) ||
            ShouldSkipAnalysis(context, notification: null))
        {
            return;
        }

        var detector = JsonLanguageDetector.GetOrCreate(context.SemanticModel.Compilation, _info);
        Analyze(context, detector, context.GetAnalysisRoot(findInTrivia: true), context.CancellationToken);
    }

    private void Analyze(
        SemanticModelAnalysisContext context,
        JsonLanguageDetector detector,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var _1 = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var stack);
        stack.Push(node);

        while (stack.TryPop(out var child))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (child.AsNode(out var childNode))
            {
                foreach (var nodeOrToken in childNode.ChildNodesAndTokens())
                {
                    if (context.ShouldAnalyzeSpan(nodeOrToken.FullSpan))
                        stack.Push(nodeOrToken);
                }
            }
            else
            {
                var token = child.AsToken();

                // If we have a string, and it's not being passed to a known JSON api (and it doesn't have a
                // lang=json comment), but it is parseable as JSON with enough structure that we are confident it is
                // json, then report that json features could light up here.
                if (_info.IsAnyStringLiteral(token.RawKind) &&
                    detector.TryParseString(token, context.SemanticModel, includeProbableStrings: false, cancellationToken) == null &&
                    detector.IsProbablyJson(token, out _))
                {
                    var chars = _info.VirtualCharService.TryConvertToVirtualChars(token);
                    var strictTree = JsonParser.TryParse(chars, JsonOptions.Strict);
                    var properties = strictTree != null && strictTree.Diagnostics.Length == 0
                        ? s_strictProperties
                        : ImmutableDictionary<string, string?>.Empty;

                    // Show this as a hidden diagnostic so the user can enable json features explicitly if they
                    // want, but do not spam them with a ... notification if they don't want it.
                    context.ReportDiagnostic(DiagnosticHelper.Create(
                        this.Descriptor,
                        token.GetLocation(),
                        NotificationOption2.Silent,
                        context.Options,
                        additionalLocations: null,
                        properties));
                }
            }
        }
    }
}

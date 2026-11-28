// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.InlineHints;

/// <summary>
/// The service to locate all positions where inline hints should be placed.
/// </summary>
[ExportLanguageService(typeof(IInlineHintsService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpInlineHintsService() : AbstractInlineHintsService, IInlineHintsService
{
    async Task<ImmutableArray<InlineHint>> IInlineHintsService.GetInlineHintsAsync(
        Document document, TextSpan textSpan, InlineHintsOptions options, bool displayAllOverride, CancellationToken cancellationToken)
    {
        var hints = await GetInlineHintsAsync(document, textSpan, options, displayAllOverride, cancellationToken).ConfigureAwait(false);
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        // Preview hints are opt-in for each file.
        if (!document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree)
                .TryGetValue("dotnet_format_string_preview_hints", out var enabled) || enabled != "true")
        {
            return hints;
        }

        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<InlineHint>.GetInstance(out var result);
        result.AddRange(hints);
        CSharpFormatStringValueEvaluator? evaluator = null;
        var remainingCandidates = CSharpFormatStringQuickInfoProvider.CandidateLimit;
        var remainingLiteralCharacters = CSharpFormatStringQuickInfoProvider.RequestLiteralLimit;
        try
        {
            foreach (var token in root.DescendantTokens(textSpan))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (remainingCandidates == 0 || evaluator?.IsRequestExhausted == true)
                    break;
                // Interpolation clauses have both a colon and a format token. Emit one hint.
                if (token.Parent is InterpolationFormatClauseSyntax clause && token != clause.ColonToken ||
                    !CSharpFormatStringQuickInfoProvider.IsFormatCandidate(token))
                {
                    continue;
                }

                var length = token.Parent is InterpolationFormatClauseSyntax formatClause ? formatClause.FormatStringToken.Span.Length : token.Span.Length;
                if (length > remainingLiteralCharacters)
                    break;
                remainingLiteralCharacters -= length;

                if (token.Parent is LiteralExpressionSyntax { Parent: ArgumentSyntax argument } &&
                    semanticModel.GetOperation(argument, cancellationToken) is IArgumentOperation
                    { Parameter.Name: "format", Parent: IInvocationOperation invocation } &&
                    CSharpFormatStringQuickInfoProvider.IsCompositeFormatMethod(invocation.TargetMethod, semanticModel.Compilation))
                {
                    var chars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
                    if (chars.IsDefault)
                        continue;
                    var cursor = 0;
                    while (remainingCandidates > 0 && evaluator?.IsRequestExhausted != true && CSharpFormatStringQuickInfoProvider.TryGetNextCompositeFormat(chars, ref cursor, out var part))
                        AddHint(token, part);
                }
                else
                {
                    AddHint(token);
                }
            }
            return result.ToImmutableAndClear();
        }
        finally
        {
            evaluator?.Dispose();
        }

        void AddHint(SyntaxToken token, CSharpFormatStringQuickInfoProvider.CompositeFormatPart? part = null)
        {
            var position = part?.ClosingBraceSpan.End ?? (token.Parent is InterpolationFormatClauseSyntax { Parent: InterpolationSyntax interpolation }
                ? interpolation.CloseBraceToken.Span.End
                : token.Parent!.FirstAncestorOrSelf<InvocationExpressionSyntax>()!.Span.End);
            if (!textSpan.Contains(position))
                return;
            remainingCandidates--;
            if (!CSharpFormatStringQuickInfoProvider.TryGetFormatInfo(semanticModel, token, token.SpanStart,
                    ref evaluator, cancellationToken, out var span, out var format, out var info, out var isNullable, part) ||
                !info.IsPreview || info.Example.Length > 80 || info.Example.IndexOfAny(['\r', '\n']) >= 0)
            {
                return;
            }

            result.Add(new InlineHint(new TextSpan(position, 0), [new TaggedText(TextTags.Text, " → " + (info.Example.Length == 0 ? "\"\"" : info.Example))],
                getDescriptionAsync: (_, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    var description = CSharpFormatStringQuickInfoProvider.CreateQuickInfo(span, format, info, isNullable);
                    return Task.FromResult(description.Sections.SelectMany(section =>
                        section.TaggedParts.Concat([new TaggedText(TextTags.LineBreak, Environment.NewLine)])).ToImmutableArray());
                }));
        }
    }
}

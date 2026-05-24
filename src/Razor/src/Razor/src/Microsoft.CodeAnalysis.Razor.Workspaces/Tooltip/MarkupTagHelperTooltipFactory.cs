// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Tooltip;

internal static class MarkupTagHelperTooltipFactory
{
    public static async Task<MarkupContent?> TryCreateTooltipAsync(
        string? documentFilePath,
        AggregateBoundElementDescription elementDescriptionInfo,
        IComponentAvailabilityService componentAvailabilityService,
        MarkupKind markupKind,
        CancellationToken cancellationToken)
    {
        if (elementDescriptionInfo is null)
        {
            throw new ArgumentNullException(nameof(elementDescriptionInfo));
        }

        var associatedTagHelperInfos = elementDescriptionInfo.DescriptionInfos;
        if (associatedTagHelperInfos.Length == 0)
        {
            return null;
        }

        // This generates a markdown description that looks like the following:
        // **SomeTagHelper**
        //
        // The Summary documentation text with `CrefTypeValues` in code.
        //
        // Additional description infos result in a triple `---` to separate the markdown entries.

        using var _ = StringBuilderPool.GetPooledObject(out var descriptionBuilder);

        foreach (var descriptionInfo in associatedTagHelperInfos)
        {
            if (descriptionBuilder.Length > 0)
            {
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine("---");
            }

            var tagHelperType = descriptionInfo.TagHelperTypeName;
            var reducedTypeName = DocCommentHelpers.ReduceTypeName(tagHelperType);

            // If the reducedTypeName != tagHelperType, then the type is prefixed by a namespace
            if (reducedTypeName != tagHelperType)
            {
                descriptionBuilder.Append(tagHelperType[..^reducedTypeName.Length]);
            }

            // We make the reducedTypeName bold while leaving the namespace intact
            StartOrEndBold(descriptionBuilder, markupKind);
            descriptionBuilder.Append(reducedTypeName);
            StartOrEndBold(descriptionBuilder, markupKind);

            var documentation = descriptionInfo.Documentation;
            if (DocCommentHelpers.TryExtractSummary(documentation, out var summaryContent))
            {
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine();
                var finalSummaryContent = CleanSummaryContent(summaryContent);
                descriptionBuilder.Append(finalSummaryContent);
            }

            if (documentFilePath is not null)
            {
                var availability = await componentAvailabilityService
                    .GetProjectAvailabilityTextAsync(documentFilePath, tagHelperType, cancellationToken)
                    .ConfigureAwait(false);

                if (availability is not null)
                {
                    descriptionBuilder.AppendLine();
                    descriptionBuilder.Append(availability);
                }
            }
        }

        return new MarkupContent
        {
            Kind = markupKind,
            Value = descriptionBuilder.ToString(),
        };
    }

    public static bool TryCreateTooltip(
        AggregateBoundAttributeDescription attributeDescriptionInfo,
        MarkupKind markupKind,
        [NotNullWhen(true)] out MarkupContent? tooltipContent)
    {
        if (attributeDescriptionInfo is null)
        {
            throw new ArgumentNullException(nameof(attributeDescriptionInfo));
        }

        var associatedAttributeInfos = attributeDescriptionInfo.DescriptionInfos;
        if (associatedAttributeInfos.Length == 0)
        {
            tooltipContent = null;
            return false;
        }

        // This generates a markdown description that looks like the following:
        // **ReturnTypeName** SomeTypeName.**SomeProperty**
        //
        // The Summary documentation text with `CrefTypeValues` in code.
        //
        // Additional description infos result in a triple `---` to separate the markdown entries.

        using var _ = StringBuilderPool.GetPooledObject(out var descriptionBuilder);

        foreach (var descriptionInfo in associatedAttributeInfos)
        {
            if (descriptionBuilder.Length > 0)
            {
                descriptionBuilder.AppendLine();
                descriptionBuilder.AppendLine("---");
            }

            StartOrEndBold(descriptionBuilder, markupKind);
            if (!TypeNameStringResolver.TryGetSimpleName(descriptionInfo.ReturnTypeName, out var returnTypeName))
            {
                returnTypeName = descriptionInfo.ReturnTypeName;
            }

            var reducedReturnTypeName = DocCommentHelpers.ReduceTypeName(returnTypeName);
            descriptionBuilder.Append(reducedReturnTypeName);
            StartOrEndBold(descriptionBuilder, markupKind);
            descriptionBuilder.Append(' ');
            var tagHelperTypeName = descriptionInfo.TypeName;
            var reducedTagHelperTypeName = DocCommentHelpers.ReduceTypeName(tagHelperTypeName);
            descriptionBuilder.Append(reducedTagHelperTypeName);
            descriptionBuilder.Append('.');
            StartOrEndBold(descriptionBuilder, markupKind);
            descriptionBuilder.Append(descriptionInfo.PropertyName);
            StartOrEndBold(descriptionBuilder, markupKind);

            var documentation = descriptionInfo.Documentation;
            if (!DocCommentHelpers.TryExtractSummary(documentation, out var summaryContent))
            {
                continue;
            }

            descriptionBuilder.AppendLine();
            descriptionBuilder.AppendLine();
            var finalSummaryContent = CleanSummaryContent(summaryContent);
            descriptionBuilder.Append(finalSummaryContent);
        }

        tooltipContent = new MarkupContent
        {
            Kind = markupKind,
            Value = descriptionBuilder.ToString(),
        };

        return true;
    }

    // Internal for testing
    internal static string CleanSummaryContent(string summaryContent)
    {
        // Cleans out all <see cref="..." /> and <seealso cref="..." /> elements. It's possible to
        // have additional doc comment types in the summary but none that require cleaning. For instance
        // if there's a <para> in the summary element when it's shown in the completion description window
        // it'll be serialized as html (wont show).
        summaryContent = summaryContent.Trim();
        var crefMatches = DocCommentHelpers.ExtractCrefMatches(summaryContent);

        using var _ = StringBuilderPool.GetPooledObject(out var summaryBuilder);

        summaryBuilder.Append(summaryContent);

        for (var i = crefMatches.Count - 1; i >= 0; i--)
        {
            var cref = crefMatches[i];
            if (cref.Success)
            {
                var value = cref.Groups[DocCommentHelpers.TagContentGroupName].Value;
                var reducedValue = DocCommentHelpers.ReduceCrefValue(value);
                reducedValue = reducedValue.Replace("{", "<").Replace("}", ">");
                summaryBuilder.Remove(cref.Index, cref.Length);
                summaryBuilder.Insert(cref.Index, $"`{reducedValue}`");
            }
        }

        var lines = summaryBuilder.ToString().Split(new[] { '\n' }, StringSplitOptions.None).Select(line => line.Trim());
        var finalSummaryContent = string.Join(Environment.NewLine, lines);
        return finalSummaryContent;
    }

    private static void StartOrEndBold(StringBuilder builder, MarkupKind markupKind)
    {
        if (markupKind == MarkupKind.Markdown)
        {
            builder.Append("**");
        }
    }
}

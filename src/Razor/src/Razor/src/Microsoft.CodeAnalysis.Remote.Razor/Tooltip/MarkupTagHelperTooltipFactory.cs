// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Remote.Razor.Tooltip;

internal static class MarkupTagHelperTooltipFactory
{
    public static async Task<MarkupContent?> TryCreateTooltipAsync(
        string? documentFilePath,
        AggregateBoundElementDescription elementDescriptionInfo,
        IComponentAvailabilityService componentAvailabilityService,
        MarkupKind markupKind,
        DocCommentFormattingContext? formattingContext,
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
                descriptionBuilder.Append(FormatSummary(summaryContent, tagHelperType, propertyName: null, markupKind, formattingContext, cancellationToken));
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
        DocCommentFormattingContext? formattingContext,
        CancellationToken cancellationToken,
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
            descriptionBuilder.Append(FormatSummary(summaryContent, tagHelperTypeName, descriptionInfo.PropertyName, markupKind, formattingContext, cancellationToken));
        }

        tooltipContent = new MarkupContent
        {
            Kind = markupKind,
            Value = descriptionBuilder.ToString(),
        };

        return true;
    }

    private static string FormatSummary(
        string summaryContent,
        string typeName,
        string? propertyName,
        MarkupKind markupKind,
        DocCommentFormattingContext? formattingContext,
        CancellationToken cancellationToken)
    {
        // Let Roslyn format the doc comment, and then turn it into markdown or plain text, the same way C# tooltips do
        var parts = DocCommentHelpers.FormatSummary(summaryContent, typeName, propertyName, formattingContext!, cancellationToken);
        return ProtocolConversions.GetDocumentationMarkupContent(
            parts,
            LanguageNames.CSharp,
            featureSupportsMarkdown: markupKind == MarkupKind.Markdown).Value;
    }

    private static void StartOrEndBold(StringBuilder builder, MarkupKind markupKind)
    {
        if (markupKind == MarkupKind.Markdown)
        {
            builder.Append("**");
        }
    }
}

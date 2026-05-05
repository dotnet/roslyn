// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Roslyn.Text.Adornments;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class RazorCompletionItemResolver : CompletionItemResolver
{
    public override Task<VSInternalCompletionItem?> ResolveAsync(
        VSInternalCompletionItem completionItem,
        VSInternalCompletionList containingCompletionList,
        ICompletionResolveContext originalRequestContext,
        VSInternalClientCapabilities? clientCapabilities,
        IComponentAvailabilityService componentAvailabilityService,
        CancellationToken cancellationToken)
    {
        if (originalRequestContext is not RazorCompletionResolveContext razorCompletionResolveContext)
        {
            // Can't recognize the original request context, bail.
            return SpecializedTasks.Null<VSInternalCompletionItem>();
        }

        return ResolveAsync(completionItem, clientCapabilities, componentAvailabilityService, razorCompletionResolveContext, cancellationToken);
    }

    public static async Task<VSInternalCompletionItem?> ResolveAsync(VSInternalCompletionItem completionItem, VSInternalClientCapabilities? clientCapabilities, IComponentAvailabilityService componentAvailabilityService, RazorCompletionResolveContext razorCompletionResolveContext, CancellationToken cancellationToken)
    {
        var associatedRazorCompletion = razorCompletionResolveContext.CompletionItems.FirstOrDefault(completion =>
        {
            if (completion.DisplayText != completionItem.Label)
            {
                return false;
            }

            // We may have items of different types with the same label (e.g. snippet and keyword)
            if (clientCapabilities is not null)
            {
                // CompletionItem.Kind and RazorCompletionItem.Kind are not compatible/comparable, so we need to convert
                // Razor completion item to VS completion item (as logic to convert just the kind is not easy to separate from
                // the rest of the conversion logic) prior to comparing them
                if (RazorCompletionListProvider.TryConvert(completion, clientCapabilities, out var convertedRazorCompletionItem))
                {
                    return completionItem.Kind == convertedRazorCompletionItem.Kind;
                }
            }

            // If display text matches but we couldn't convert razor completion item to VS completion item for some reason,
            // do what previous version of the code did and return true.
            return true;
        });

        if (associatedRazorCompletion is null)
        {
            return null;
        }

        // If the client is VS, also fill in the Description property.
        var useDescriptionProperty = clientCapabilities?.SupportsVisualStudioExtensions ?? false;
        var completionSupportedKinds = clientCapabilities?.TextDocument?.Completion?.CompletionItem?.DocumentationFormat;
        var documentationKind = completionSupportedKinds?.Contains(MarkupKind.Markdown) == true ? MarkupKind.Markdown : MarkupKind.PlainText;

        MarkupContent? tagHelperMarkupTooltip = null;
        ClassifiedTextElement? tagHelperClassifiedTextTooltip = null;

        switch (associatedRazorCompletion.Kind)
        {
            case RazorCompletionItemKind.Directive:
                {
                    if (associatedRazorCompletion.DescriptionInfo is DirectiveCompletionDescription descriptionInfo)
                    {
                        completionItem.Documentation = descriptionInfo.Description;
                    }

                    break;
                }
            case RazorCompletionItemKind.MarkupTransition:
                {
                    if (associatedRazorCompletion.DescriptionInfo is MarkupTransitionCompletionDescription descriptionInfo)
                    {
                        completionItem.Documentation = descriptionInfo.Description;
                    }

                    break;
                }
            case RazorCompletionItemKind.Attribute:
                {
                    if (associatedRazorCompletion.DescriptionInfo is AttributeDescriptionInfo descriptionInfo)
                    {
                        completionItem.Documentation = new MarkupContent
                        {
                            Kind = documentationKind,
                            Value = descriptionInfo.Documentation
                        };
                    }

                    break;
                }
            case RazorCompletionItemKind.DirectiveAttribute:
            case RazorCompletionItemKind.DirectiveAttributeParameter:
            case RazorCompletionItemKind.TagHelperAttribute:
                {
                    if (associatedRazorCompletion.DescriptionInfo is not AggregateBoundAttributeDescription descriptionInfo)
                    {
                        break;
                    }

                    if (useDescriptionProperty)
                    {
                        ClassifiedTagHelperTooltipFactory.TryCreateTooltip(descriptionInfo, out tagHelperClassifiedTextTooltip);
                    }
                    else
                    {
                        MarkupTagHelperTooltipFactory.TryCreateTooltip(descriptionInfo, documentationKind, out tagHelperMarkupTooltip);
                    }

                    break;
                }
            case RazorCompletionItemKind.TagHelperElement:
                {
                    if (associatedRazorCompletion.DescriptionInfo is not AggregateBoundElementDescription descriptionInfo)
                    {
                        break;
                    }

                    if (useDescriptionProperty)
                    {
                        tagHelperClassifiedTextTooltip = await ClassifiedTagHelperTooltipFactory
                            .TryCreateTooltipAsync(razorCompletionResolveContext.FilePath, descriptionInfo, componentAvailabilityService, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        tagHelperMarkupTooltip = await MarkupTagHelperTooltipFactory
                            .TryCreateTooltipAsync(razorCompletionResolveContext.FilePath, descriptionInfo, componentAvailabilityService, documentationKind, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    break;
                }
            case RazorCompletionItemKind.CSharpRazorKeyword:
                {
                    if (associatedRazorCompletion.DescriptionInfo is CSharpRazorKeywordCompletionDescription descriptionInfo)
                    {
                        completionItem.Documentation = descriptionInfo.Description;
                    }

                    break;
                }
        }

        if (tagHelperMarkupTooltip != null)
        {
            completionItem.Documentation = tagHelperMarkupTooltip;
        }

        if (tagHelperClassifiedTextTooltip != null)
        {
            completionItem.Description = tagHelperClassifiedTextTooltip;
        }

        return completionItem;
    }
}

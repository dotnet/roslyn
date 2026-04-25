// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class EventHandlerTagHelperProducer : TagHelperProducer
{
    private readonly INamedTypeSymbol _eventHandlerAttributeType;

    private EventHandlerTagHelperProducer(INamedTypeSymbol eventHandlerAttributeType)
    {
        _eventHandlerAttributeType = eventHandlerAttributeType;
    }

    public override TagHelperProducerKind Kind => TagHelperProducerKind.EventHandler;

    public override bool SupportsTypes => true;

    public override bool IsCandidateType(INamedTypeSymbol type)
        => type.DeclaredAccessibility == Accessibility.Public &&
           type.Name == "EventHandlers";

    public override void AddTagHelpersForType(
        INamedTypeSymbol type,
        ref TagHelperCollection.RefBuilder results,
        CancellationToken cancellationToken)
    {
        // Not handling duplicates here for now since we're the primary ones extending this.
        // If we see users adding to the set of event handler constructs we will want to add deduplication
        // and potentially diagnostics.
        foreach (var attribute in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _eventHandlerAttributeType))
            {
                if (!AttributeArgs.TryGet(attribute, out var args))
                {
                    // If this occurs, the [EventHandler] was defined incorrectly, so we can't create a tag helper.
                    continue;
                }

                var typeName = type.GetDefaultDisplayString();
                var namespaceName = type.ContainingNamespace.GetFullName();
                results.Add(CreateTagHelper(typeName, namespaceName, type.Name, args));
            }
        }
    }


    private readonly record struct AttributeArgs(
        string Attribute,
        INamedTypeSymbol EventArgsType,
        bool EnableStopPropagation = false,
        bool EnablePreventDefault = false)
    {
        public static bool TryGet(AttributeData attribute, out AttributeArgs args)
        {
            // EventHandlerAttribute has two constructors:
            //
            // - EventHandlerAttribute(string attributeName, Type eventArgsType);
            // - EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault);

            var arguments = attribute.ConstructorArguments;

            return TryGetFromTwoArguments(arguments, out args) ||
                   TryGetFromFourArguments(arguments, out args);

            static bool TryGetFromTwoArguments(ImmutableArray<TypedConstant> arguments, out AttributeArgs args)
            {
                // Ctor 1: EventHandlerAttribute(string attributeName, Type eventArgsType);

                if (arguments is [
                    { Value: string attributeName },
                    { Value: INamedTypeSymbol eventArgsType }])
                {
                    args = new(attributeName, eventArgsType);
                    return true;
                }

                args = default;
                return false;
            }

            static bool TryGetFromFourArguments(ImmutableArray<TypedConstant> arguments, out AttributeArgs args)
            {
                // Ctor 2: EventHandlerAttribute(string attributeName, Type eventArgsType, bool enableStopPropagation, bool enablePreventDefault);

                // TODO: The enablePreventDefault and enableStopPropagation arguments are incorrectly swapped!
                // However, they have been that way since the 4-argument constructor variant was introduced
                // in https://github.com/dotnet/razor/commit/7635bba6ef2d3e6798d0846ceb96da6d5908e1b0.
                // Fixing this is tracked be https://github.com/dotnet/razor/issues/10497

                if (arguments is [
                    { Value: string attributeName },
                    { Value: INamedTypeSymbol eventArgsType },
                    { Value: bool enablePreventDefault },
                    { Value: bool enableStopPropagation }])
                {
                    args = new(attributeName, eventArgsType, enableStopPropagation, enablePreventDefault);
                    return true;
                }

                args = default;
                return false;
            }
        }
    }

    private static TagHelperDescriptor CreateTagHelper(
        string typeName,
        string typeNamespace,
        string typeNameIdentifier,
        AttributeArgs args)
    {
        var (attribute, eventArgsType, enableStopPropagation, enablePreventDefault) = args;

        var attributeName = "@" + attribute;
        var eventArgType = eventArgsType.GetDefaultDisplayString();
        using var _ = TagHelperDescriptorBuilder.GetPooledInstance(
            TagHelperKind.EventHandler, attribute, ComponentsApi.AssemblyName,
            out var builder);

        builder.SetTypeName(typeName, typeNamespace, typeNameIdentifier);

        builder.CaseSensitive = true;
        builder.ClassifyAttributesOnly = true;
        builder.SetDocumentation(
            DocumentationDescriptor.From(
                DocumentationId.EventHandlerTagHelper,
                attributeName,
                eventArgType));

        builder.SetMetadata(new EventHandlerMetadata()
        {
            EventArgsType = eventArgType
        });

        builder.TagMatchingRule(rule =>
        {
            rule.TagName = "*";

            rule.Attribute(a =>
            {
                a.Name = attributeName;
                a.NameComparison = RequiredAttributeNameComparison.FullMatch;
                a.IsDirectiveAttribute = true;
            });
        });

        if (enablePreventDefault)
        {
            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";

                rule.Attribute(a =>
                {
                    a.Name = attributeName + ":preventDefault";
                    a.NameComparison = RequiredAttributeNameComparison.FullMatch;
                    a.IsDirectiveAttribute = true;
                });
            });
        }

        if (enableStopPropagation)
        {
            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";

                rule.Attribute(a =>
                {
                    a.Name = attributeName + ":stopPropagation";
                    a.NameComparison = RequiredAttributeNameComparison.FullMatch;
                    a.IsDirectiveAttribute = true;
                });
            });
        }

        builder.BindAttribute(a =>
        {
            a.SetDocumentation(
                DocumentationDescriptor.From(
                    DocumentationId.EventHandlerTagHelper,
                    attributeName,
                    eventArgType));

            a.Name = attributeName;

            // We want event handler directive attributes to default to C# context.
            a.TypeName = $"Microsoft.AspNetCore.Components.EventCallback<{eventArgType}>";

            a.PropertyName = attribute;

            a.IsDirectiveAttribute = true;

            // Make this weakly typed (don't type check) - delegates have their own type-checking
            // logic that we don't want to interfere with.
            a.IsWeaklyTyped = true;

            if (enablePreventDefault)
            {
                a.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "preventDefault";
                    parameter.PropertyName = "PreventDefault";
                    parameter.TypeName = typeof(bool).FullName;
                    parameter.SetDocumentation(
                        DocumentationDescriptor.From(
                            DocumentationId.EventHandlerTagHelper_PreventDefault,
                            attributeName));
                });
            }

            if (enableStopPropagation)
            {
                a.BindAttributeParameter(parameter =>
                {
                    parameter.Name = "stopPropagation";
                    parameter.PropertyName = "StopPropagation";
                    parameter.TypeName = typeof(bool).FullName;
                    parameter.SetDocumentation(
                        DocumentationDescriptor.From(
                            DocumentationId.EventHandlerTagHelper_StopPropagation,
                            attributeName));
                });
            }
        });

        return builder.Build();
    }
}

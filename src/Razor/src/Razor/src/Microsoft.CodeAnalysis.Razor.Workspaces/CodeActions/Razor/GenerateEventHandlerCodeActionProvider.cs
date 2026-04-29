// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using SyntaxFacts = Microsoft.CodeAnalysis.CSharp.SyntaxFacts;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

using SyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

internal class GenerateEventHandlerCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (!context.ContainsDiagnostic("CS0103"))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var syntaxRoot = context.CodeDocument.GetRequiredSyntaxRoot();
        var owner = syntaxRoot.FindToken(context.StartAbsoluteIndex).Parent.AssumeNotNull();

        if (IsGenerateEventHandlerValid(owner, out var methodName, out var eventParameterType, out var allowsAsync))
        {
            var textDocument = context.Request.TextDocument;

            if (allowsAsync)
            {
                return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>(
                    [
                        RazorCodeActionFactory.CreateGenerateEventHandler(textDocument, context.DelegatedDocumentUri, methodName, eventParameterType),
                        RazorCodeActionFactory.CreateAsyncGenerateEventHandler(textDocument, context.DelegatedDocumentUri, methodName, eventParameterType)
                    ]);
            }
            else
            {
                return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>(
                    [
                        RazorCodeActionFactory.CreateGenerateEventHandler(textDocument, context.DelegatedDocumentUri, methodName, eventParameterType)
                    ]);
            }
        }

        return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
    }

    private static bool IsGenerateEventHandlerValid(
        SyntaxNode owner,
        [NotNullWhen(true)] out string? methodName,
        out string? eventParameterType,
        out bool allowAsync)
    {
        methodName = null;
        eventParameterType = null;
        allowAsync = false;

        // The owner should have a SyntaxKind of CSharpExpressionLiteral or MarkupTextLiteral.
        // MarkupTextLiteral if the cursor is directly before the first letter of the method name.
        // CSharpExpressionalLiteral if cursor is anywhere else in the method name.
        if (owner.Kind != SyntaxKind.CSharpExpressionLiteral && owner.Kind != SyntaxKind.MarkupTextLiteral)
        {
            return false;
        }

        // We want to get MarkupTagHelperDirectiveAttribute since this has information about the event name.
        // Hierarchy:
        // MarkupTagHelper[Directive]Attribute > MarkupTextLiteral
        // or
        // MarkupTagHelper[Directive]Attribute > MarkupTagHelperAttributeValue > CSharpExpressionLiteral
        var commonParent = owner.Kind == SyntaxKind.CSharpExpressionLiteral ? owner.Parent.Parent : owner.Parent;

        // MarkupTagHelperElement > MarkupTagHelperStartTag > MarkupTagHelperDirectiveAttribute
        if (commonParent.Parent.Parent is not MarkupTagHelperElementSyntax { TagHelperInfo.BindingResult: var binding })
        {
            return false;
        }

        return commonParent switch
        {
            MarkupTagHelperDirectiveAttributeSyntax markupTagHelperDirectiveAttribute => TryGetEventNameAndMethodName(markupTagHelperDirectiveAttribute, binding, out methodName, out eventParameterType, out allowAsync),
            MarkupTagHelperAttributeSyntax markupTagHelperAttribute => TryGetEventNameAndMethodName(markupTagHelperAttribute, binding, out methodName, out eventParameterType, out allowAsync),
            _ => false
        };
    }

    private static bool TryGetEventNameAndMethodName(
        MarkupTagHelperDirectiveAttributeSyntax markupTagHelperDirectiveAttribute,
        TagHelperBinding binding,
        [NotNullWhen(true)] out string? methodName,
        out string? eventParameterType,
        out bool allowAsync)
    {
        methodName = null;
        eventParameterType = null;
        allowAsync = true;

        var attributeName = markupTagHelperDirectiveAttribute.TagHelperAttributeInfo.Name;

        // For attributes with a parameter, the attribute name actually includes the parameter, so we have to parse it
        // out ourself in order to find the attribute tag helper properly. We only do this for parameters that are valid
        // places to put C# method names.
        if (markupTagHelperDirectiveAttribute.TagHelperAttributeInfo.ParameterName is "after" or "set")
        {
            attributeName = attributeName[..attributeName.IndexOf(':')];
        }

        var found = false;
        foreach (var tagHelper in binding.TagHelpers)
        {
            foreach (var attribute in tagHelper.BoundAttributes)
            {
                if (attribute.Name == attributeName)
                {
                    // We found the attribute that matches the directive attribute, now we need to check if the
                    // tag helper it's bound to is an event handler. This filters out things like @ref and @rendermode
                    if (tagHelper.Kind == TagHelperKind.EventHandler)
                    {
                        // An event handler like "@onclick"
                        eventParameterType = tagHelper.GetEventArgsType() ?? "";
                    }
                    else if (tagHelper.Kind == TagHelperKind.Bind)
                    {
                        // A bind tag helper, so either @bind-XX:after or @bind-XX:set, the latter of which has a parameter
                        if (markupTagHelperDirectiveAttribute.TagHelperAttributeInfo.ParameterName == "set" &&
                            ComponentAttributeIntermediateNode.TryGetEventCallbackArgument(attribute.TypeName.AsMemory(), out var argument))
                        {
                            // Set has a parameter
                            eventParameterType = argument.ToString();
                        }
                    }
                    else
                    {
                        return false;
                    }

                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        if (!found)
        {
            return false;
        }

        var content = markupTagHelperDirectiveAttribute.Value.GetContent();
        if (!SyntaxFacts.IsValidIdentifier(content))
        {
            return false;
        }

        methodName = content;
        return true;
    }

    private static bool TryGetEventNameAndMethodName(
        MarkupTagHelperAttributeSyntax markupTagHelperDirectiveAttribute,
        TagHelperBinding binding,
        [NotNullWhen(true)] out string? methodName,
        out string? eventParameterType,
        out bool allowAsync)
    {
        methodName = null;
        eventParameterType = null;
        allowAsync = true;

        foreach (var tagHelper in binding.TagHelpers)
        {
            foreach (var attribute in tagHelper.BoundAttributes)
            {
                if (attribute.Name == markupTagHelperDirectiveAttribute.TagHelperAttributeInfo.Name)
                {
                    if (attribute.IsEventCallbackProperty())
                    {
                        // TypeName is something like "EventCallback<System.String>", so we need to parse out the parameter type.
                        if (ComponentAttributeIntermediateNode.TryGetEventCallbackArgument(attribute.TypeName.AsMemory(), out var argument))
                        {
                            eventParameterType = argument.ToString();
                        }
                    }
                    else if (attribute.IsDelegateProperty())
                    {
                        // Systm.Action<Type<TItem>> doesn't allow for variations in sync/async like EventCallback does 
                        allowAsync = false;

                        if (attribute.IsGenericTypedProperty())
                        {
                            if (tagHelper.TryGetGenericTypeNameFromComponent(binding, out var genericType) &&
                                ComponentAttributeIntermediateNode.TryGetGenericActionArgument(attribute.TypeName.AsMemory(), genericType, out var argument))
                            {
                                eventParameterType = argument.ToString();
                            }
                        }
                        else
                        {
                            if (ComponentAttributeIntermediateNode.TryGetActionArgument(attribute.TypeName.AsMemory(), out var argument))
                            {
                                eventParameterType = argument.ToString();
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }
            }
        }

        var content = markupTagHelperDirectiveAttribute.Value.GetContent();
        if (!SyntaxFacts.IsValidIdentifier(content))
        {
            return false;
        }

        methodName = content;
        return true;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal static class CodeActionExtensions
{
    private const string NestedCodeActionCommand = Constants.RunNestedCodeActionCommandName;
    private const string NestedCodeActionsProperty = Constants.NestedCodeActionsPropertyName;
    private const string CodeActionPathProperty = Constants.CodeActionPathPropertyName;
    private const string FixAllFlavorsProperty = Constants.FixAllFlavorsPropertyName;

    public static SumType<Command, CodeAction> AsVSCodeCommandOrCodeAction(this VSInternalCodeAction razorCodeAction, VSTextDocumentIdentifier textDocument, Uri? delegatedDocumentUri)
    {
        if (razorCodeAction.Data is null)
        {
            // Only code action edit, we must convert this to a resolvable command

            var resolutionParams = new RazorCodeActionResolutionParams
            {
                TextDocument = textDocument,
                Action = LanguageServerConstants.CodeActions.EditBasedCodeActionCommand,
                Language = RazorLanguageKind.Razor,
                DelegatedDocumentUri = delegatedDocumentUri,
                Data = razorCodeAction.Edit ?? new WorkspaceEdit(),
            };

            razorCodeAction = new VSInternalCodeAction()
            {
                Title = razorCodeAction.Title,
                Data = JsonSerializer.SerializeToElement(resolutionParams),
                TelemetryId = razorCodeAction.TelemetryId,
            };
        }

        var serializedParams = JsonSerializer.SerializeToNode(razorCodeAction.Data).AssumeNotNull();
        var arguments = new JsonArray(serializedParams);

        return new Command
        {
            Title = razorCodeAction.Title ?? string.Empty,
            CommandIdentifier = LanguageServerConstants.RazorCodeActionRunnerCommand,
            Arguments = arguments.ToArray()!
        };
    }

    public static RazorVSInternalCodeAction WrapResolvableCodeAction(
        this RazorVSInternalCodeAction razorCodeAction,
        RazorCodeActionContext context,
        string action = LanguageServerConstants.CodeActions.Default,
        RazorLanguageKind language = RazorLanguageKind.CSharp,
        bool isOnAllowList = true)
    {
        if (!TryHandleNestedCodeAction(razorCodeAction, context, action, language))
        {
            var resolutionParams = new RazorCodeActionResolutionParams()
            {
                TextDocument = context.Request.TextDocument,
                Action = action,
                Language = language,
                DelegatedDocumentUri = context.DelegatedDocumentUri,
                Data = razorCodeAction.Data
            };
            razorCodeAction.Data = JsonSerializer.SerializeToElement(resolutionParams);
        }

        if (!isOnAllowList)
        {
            razorCodeAction.Title = $"(Exp) {razorCodeAction.Title} ({razorCodeAction.Name})";
        }

        if (razorCodeAction.Children != null)
        {
            for (var i = 0; i < razorCodeAction.Children.Length; i++)
            {
                razorCodeAction.Children[i] = razorCodeAction.Children[i].WrapResolvableCodeAction(context, action, language, isOnAllowList);
            }
        }

        return razorCodeAction;
    }

    private static bool TryHandleNestedCodeAction(RazorVSInternalCodeAction razorCodeAction, RazorCodeActionContext context, string action, RazorLanguageKind language)
    {
        if (language != RazorLanguageKind.CSharp ||
            razorCodeAction.Command is not { CommandIdentifier: NestedCodeActionCommand, Arguments: [JsonElement arg] })
        {
            return false;
        }

        // For nested code actions in VS Code, we want to not wrap the data from this code action with our context,
        // but wrap all of the nested code actions in the first argument. That way, the custom command in the C#
        // Extension will work (it expects Data to be unwrapped), and when it tries to resolve the children, they
        // will come to us because they're wrapped, and we'll send them on to Roslyn.
        //
        // We extract each nested code action, wrap its data with our context, then copy across a couple of things
        // from its data to our new wrapped data, and we're done. We end up with data that is an odd hybrid of Razor
        // and Roslyn expectations, but thanks to the dynamic nature of JSON, it works out.
        using var mappedNestedActions = new PooledArrayBuilder<RazorVSInternalCodeAction>();
        var nestedCodeActions = arg.GetProperty(NestedCodeActionsProperty);
        foreach (var nestedAction in nestedCodeActions.EnumerateArray())
        {
            var nestedCodeAction = nestedAction.Deserialize<RazorVSInternalCodeAction>(JsonHelpers.JsonSerializerOptions).AssumeNotNull();
            var resolutionParams = new RazorCodeActionResolutionParams()
            {
                TextDocument = context.Request.TextDocument,
                Action = action,
                Language = language,
                DelegatedDocumentUri = context.DelegatedDocumentUri,
                Data = nestedCodeAction.Data
            };

            // We have to set two extra properties that Roslyn requires for nested code actions, copied from it's data object
            var newActionData = JsonSerializer.SerializeToNode(resolutionParams).AssumeNotNull();
            var nestedData = nestedAction.GetProperty("data");
            if (nestedData.TryGetProperty(CodeActionPathProperty, out var codeActionPath))
            {
                newActionData[CodeActionPathProperty] = JsonSerializer.SerializeToNode(codeActionPath, JsonHelpers.JsonSerializerOptions);
            }

            if (nestedData.TryGetProperty(FixAllFlavorsProperty, out var fixAllFlavors))
            {
                newActionData[FixAllFlavorsProperty] = JsonSerializer.SerializeToNode(fixAllFlavors, JsonHelpers.JsonSerializerOptions);
            }

            nestedCodeAction.Data = newActionData;
            mappedNestedActions.Add(nestedCodeAction);
        }

        // We can't update NestedCodeActions directly, because JsonElement is immutable, so we have to convert to a node
        var newArg = JsonSerializer.SerializeToNode(arg, JsonHelpers.JsonSerializerOptions).AssumeNotNull();
        newArg.AsObject()[NestedCodeActionsProperty] = JsonSerializer.SerializeToNode(mappedNestedActions.ToArray(), JsonHelpers.JsonSerializerOptions);
        razorCodeAction.Command.Arguments[0] = newArg;
        return true;
    }

    private static VSInternalCodeAction WrapResolvableCodeAction(
        this VSInternalCodeAction razorCodeAction,
        RazorCodeActionContext context,
        string action,
        RazorLanguageKind language,
        bool isOnAllowList)
    {
        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = action,
            Language = language,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = razorCodeAction.Data
        };
        razorCodeAction.Data = JsonSerializer.SerializeToElement(resolutionParams);

        if (!isOnAllowList)
        {
            razorCodeAction.Title = "(Exp) " + razorCodeAction.Title;
        }

        if (razorCodeAction.Children != null)
        {
            for (var i = 0; i < razorCodeAction.Children.Length; i++)
            {
                razorCodeAction.Children[i] = razorCodeAction.Children[i].WrapResolvableCodeAction(context, action, language, isOnAllowList);
            }
        }

        return razorCodeAction;
    }
}

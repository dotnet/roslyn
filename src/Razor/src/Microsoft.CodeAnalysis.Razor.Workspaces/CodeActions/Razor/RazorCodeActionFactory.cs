// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Razor;

internal static class RazorCodeActionFactory
{
    private static readonly Guid s_addComponentUsingTelemetryId = new("6c5416b7-7be7-49ee-aa60-904385be676f");
    private static readonly Guid s_fullyQualifyComponentTelemetryId = new("3d9abe36-7d10-4e08-8c18-ad88baa9a923");
    private static readonly Guid s_createComponentFromTagTelemetryId = new("a28e0baa-a4d5-4953-a817-1db586035841");
    private static readonly Guid s_createExtractToCssTelemetryId = new("a3773518-35ff-455c-a8c2-d6adaf1d2c48");
    private static readonly Guid s_createExtractToCodeBehindTelemetryId = new("f63167f7-fdc6-450f-8b7b-b240892f4a27");
    private static readonly Guid s_createExtractToComponentTelemetryId = new("af67b0a3-f84b-4808-97a7-b53e85b22c64");
    private static readonly Guid s_simplifyComponentTelemetryId = new("2207f68c-419e-4baa-8493-2e7769e5c91d");
    private static readonly Guid s_generateEventHandlerTelemetryId = new("c14fa003-c752-45fc-bb29-3a123ae5ecef");
    private static readonly Guid s_generateAsyncEventHandlerTelemetryId = new("9058ca47-98e2-4f11-bf7c-a16a444dd939");
    private static readonly Guid s_promoteUsingDirectiveTelemetryId = new("751f9012-e37b-444a-9211-b4ebce91d96e");
    private static readonly Guid s_removeUnnecessaryDirectivesTelemetryId = new("92bbd4a4-6076-4742-b90b-9b480e6a23ec");
    private static readonly Guid s_wrapAttributesTelemetryId = new("1df50ba6-4ed1-40d8-8fe2-1c4c1b08e8b5");
    private static readonly Guid s_simplifyFullyQualifiedComponentTelemetryId = new("f8640324-2037-49fd-9697-2227690c33c3");
    private static readonly Guid s_sortAndConsolidateUsingsTelemetryId = new("a3dc8f52-7e1b-4a09-9c6d-2f5e7a8b3c01");

    public static RazorVSInternalCodeAction CreateWrapAttributes(RazorCodeActionResolutionParams resolutionParams)
        => new RazorVSInternalCodeAction
        {
            Title = SR.Wrap_attributes,
            Data = JsonSerializer.SerializeToElement(resolutionParams),
            TelemetryId = s_wrapAttributesTelemetryId,
            Name = LanguageServerConstants.CodeActions.WrapAttributes,
        };

    public static RazorVSInternalCodeAction CreatePromoteUsingDirective(string importsFileName, RazorCodeActionResolutionParams resolutionParams)
        => new RazorVSInternalCodeAction
        {
            Title = SR.FormatPromote_using_directive_to(importsFileName),
            Data = JsonSerializer.SerializeToElement(resolutionParams),
            TelemetryId = s_promoteUsingDirectiveTelemetryId,
            Name = LanguageServerConstants.CodeActions.PromoteUsingDirective,
        };

    public static RazorVSInternalCodeAction CreateRemoveUnnecessaryDirectives(RazorCodeActionResolutionParams resolutionParams)
        => new RazorVSInternalCodeAction
        {
            Title = SR.Remove_unnecessary_directives,
            Data = JsonSerializer.SerializeToElement(resolutionParams),
            TelemetryId = s_removeUnnecessaryDirectivesTelemetryId,
            Name = LanguageServerConstants.CodeActions.RemoveUnnecessaryDirectives,
            // Removing an unused using should be higher than promoting it
            Order = -100,
        };

    public static RazorVSInternalCodeAction CreateAddComponentUsing(string @namespace, string? newTagName, RazorCodeActionResolutionParams resolutionParams)
    {
        var title = $"@using {@namespace}";
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction
        {
            Title = newTagName is null ? title : $"{newTagName} - {title}",
            Data = data,
            TelemetryId = s_addComponentUsingTelemetryId,
            Priority = VSInternalPriorityLevel.High,
            Name = LanguageServerConstants.CodeActions.AddUsing,
            // Adding a using for an existing component should be first
            Order = -1000,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateFullyQualifyComponent(string fullyQualifiedName, WorkspaceEdit workspaceEdit)
    {
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = fullyQualifiedName,
            Edit = workspaceEdit,
            TelemetryId = s_fullyQualifyComponentTelemetryId,
            Priority = VSInternalPriorityLevel.High,
            Name = LanguageServerConstants.CodeActions.FullyQualify,
            // Fully qualifying an existing component should be very high, but not quite as high as Add Using
            Order = -900,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateComponentFromTag(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.Create_Component_FromTag_Title,
            Data = data,
            TelemetryId = s_createComponentFromTagTelemetryId,
            Name = LanguageServerConstants.CodeActions.CreateComponentFromTag,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateExtractToCss(string razorFileName, RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.FormatExtractTo_Css_Title(razorFileName),
            Data = data,
            TelemetryId = s_createExtractToCssTelemetryId,
            Name = LanguageServerConstants.CodeActions.ExtractToCss,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateExtractToCodeBehind(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.ExtractTo_CodeBehind_Title,
            Data = data,
            TelemetryId = s_createExtractToCodeBehindTelemetryId,
            Name = LanguageServerConstants.CodeActions.ExtractToCodeBehind,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateExtractToComponent(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.ExtractTo_Component_Title,
            Data = data,
            TelemetryId = s_createExtractToComponentTelemetryId,
            Name = LanguageServerConstants.CodeActions.ExtractToNewComponent,
            // Since Extract to Component is offered basically everywhere, always offer it last
            Order = 9999
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateSimplifyTagToSelfClosing(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.Simplify_Tag_To_SelfClosing_Title,
            Data = data,
            TelemetryId = s_simplifyComponentTelemetryId,
            Name = LanguageServerConstants.CodeActions.SimplifyTagToSelfClosing,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateGenerateEventHandler(VSTextDocumentIdentifier textDocument, Uri? delegatedDocumentUri, string methodName, string? eventParameterType)
    {
        var @params = new GenerateEventHandlerCodeActionParams
        {
            MethodName = methodName,
            EventParameterType = eventParameterType,
            IsAsync = false
        };
        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = textDocument,
            Action = LanguageServerConstants.CodeActions.GenerateEventHandler,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = delegatedDocumentUri,
            Data = @params,
        };

        var title = SR.FormatGenerate_Event_Handler_Title(methodName);
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = title,
            Data = data,
            TelemetryId = s_generateEventHandlerTelemetryId,
            Name = LanguageServerConstants.CodeActions.GenerateEventHandler,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateAsyncGenerateEventHandler(VSTextDocumentIdentifier textDocument, Uri? delegatedDocumentUri, string methodName, string? eventParameterType)
    {
        var @params = new GenerateEventHandlerCodeActionParams
        {
            MethodName = methodName,
            EventParameterType = eventParameterType,
            IsAsync = true
        };
        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = textDocument,
            Action = LanguageServerConstants.CodeActions.GenerateEventHandler,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = delegatedDocumentUri,
            Data = @params,
        };

        var title = SR.FormatGenerate_Async_Event_Handler_Title(methodName);
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = title,
            Data = data,
            TelemetryId = s_generateAsyncEventHandlerTelemetryId,
            Name = LanguageServerConstants.CodeActions.GenerateAsyncEventHandler,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateSimplifyFullyQualifiedComponent(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.Simplify_Fully_Qualified_Component_Title,
            Data = data,
            TelemetryId = s_simplifyFullyQualifiedComponentTelemetryId,
            Name = LanguageServerConstants.CodeActions.SimplifyFullyQualifiedComponent,
        };
        return codeAction;
    }

    public static RazorVSInternalCodeAction CreateSortAndConsolidateUsings(RazorCodeActionResolutionParams resolutionParams)
    {
        var data = JsonSerializer.SerializeToElement(resolutionParams);
        var codeAction = new RazorVSInternalCodeAction()
        {
            Title = SR.Sort_And_Consolidate_Usings_Title,
            Data = data,
            TelemetryId = s_sortAndConsolidateUsingsTelemetryId,
            Name = LanguageServerConstants.CodeActions.SortAndConsolidateUsings,
            // Since this code action only appears when necessary, and promote appears all the time, it makes sense
            // to order this before Promote Using Directive
            Order = -10
        };
        return codeAction;
    }
}

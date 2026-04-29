// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal static class RazorServices
{
    // Generally speaking we prefer MessagePack services because the serialization is lower overhead, however
    // given that we work with LSP types, which as Json serializable, sometimes converting is an unnecssary
    // extra step. Judgement is needed to decide which way is better, paying particular extra attention to
    // complex data types in LSP that are painful to represent in MessagePack.

    private static readonly IEnumerable<(Type, Type?)> MessagePackServices =
        [
            (typeof(IRemoteLinkedEditingRangeService), null),
            (typeof(IRemoteSemanticTokensService), null),
            (typeof(IRemoteHtmlDocumentService), null),
            (typeof(IRemoteUriPresentationService), null),
            (typeof(IRemoteFoldingRangeService), null),
            (typeof(IRemoteDocumentHighlightService), null),
            (typeof(IRemoteAutoInsertService), null),
            (typeof(IRemoteFormattingService), null),
            (typeof(IRemoteSpellCheckService), null),
            (typeof(IRemoteInlineCompletionService), null),
            (typeof(IRemoteDebugInfoService), null),
            (typeof(IRemoteWrapWithTagService), null),
            (typeof(IRemoteSpanMappingService), null),
            (typeof(IRemoteDevToolsService), null),
            (typeof(IRemoteRemoveAndSortUsingsService), null),
        ];

    private static readonly IEnumerable<(Type, Type?)> JsonServices =
        [
            (typeof(IRemoteClientInitializationService), null),
            (typeof(IRemoteClientSettingsService), null),
            (typeof(IRemoteGoToDefinitionService), null),
            (typeof(IRemoteHoverService), null),
            (typeof(IRemoteSignatureHelpService), null),
            (typeof(IRemoteInlayHintService), null),
            (typeof(IRemoteDocumentSymbolService), null),
            (typeof(IRemoteRenameService), null),
            (typeof(IRemoteGoToImplementationService), null),
            (typeof(IRemoteDiagnosticsService), null),
            (typeof(IRemoteCompletionService), null),
            (typeof(IRemoteCodeActionsService), null),
            (typeof(IRemoteAddNestedFileService), null),
            (typeof(IRemoteFindAllReferencesService), null),
            (typeof(IRemoteMEFInitializationService), null),
            (typeof(IRemoteCodeLensService), null),
            (typeof(IRemoteDataTipRangeService), null),
        ];

    private const string ComponentName = "Razor";

    public static readonly RazorServiceDescriptorsWrapper Descriptors = new(
        ComponentName,
        featureDisplayNameProvider: GetFeatureDisplayName,
        additionalFormatters: [],
        additionalResolvers: [],
        interfaces: MessagePackServices);

    public static readonly RazorServiceDescriptorsWrapper JsonDescriptors = new(
        ComponentName, // Needs to match the above because so much of our ServiceHub infrastructure is convention based
        featureDisplayNameProvider: GetFeatureDisplayName,
        jsonConverters: RazorServiceDescriptorsWrapper.GetLspConverters(),
        interfaces: JsonServices);

    private static string GetFeatureDisplayName(string feature)
    {
        return $"Razor {feature} Feature";
    }

    internal static class TestAccessor
    {
        internal static IEnumerable<(Type, Type?)> MessagePackServices => RazorServices.MessagePackServices;

        internal static IEnumerable<(Type, Type?)> JsonServices => RazorServices.JsonServices;
    }
}

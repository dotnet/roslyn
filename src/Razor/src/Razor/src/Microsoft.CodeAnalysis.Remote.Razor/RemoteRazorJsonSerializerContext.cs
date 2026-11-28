// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

/// <summary>
/// Metadata for payloads owned by the remote layer, which Razor Workspaces cannot reference.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(AddUsingsCodeActionParams))]
[JsonSerializable(typeof(CreateComponentCodeActionParams))]
[JsonSerializable(typeof(ExtractToCodeBehindCodeActionParams))]
[JsonSerializable(typeof(ExtractToComponentCodeActionParams))]
[JsonSerializable(typeof(ExtractToCssCodeActionParams))]
[JsonSerializable(typeof(GenerateEventHandlerCodeActionParams))]
[JsonSerializable(typeof(PromoteToUsingCodeActionParams))]
[JsonSerializable(typeof(RemoveUnnecessaryDirectivesCodeActionParams))]
[JsonSerializable(typeof(SimplifyFullyQualifiedComponentCodeActionParams))]
[JsonSerializable(typeof(SimplifyTagToSelfClosingCodeActionParams))]
[JsonSerializable(typeof(WrapAttributesCodeActionParams))]
[JsonSerializable(typeof(ImmutableArray<LineInfo>))]
[JsonSerializable(typeof(ImmutableArray<SourceMapping>))]
[JsonSerializable(typeof(ImmutableArray<TextChange>))]
[JsonSerializable(typeof(TextChange[]))]
[JsonSerializable(typeof(FormattingEngineOptions))]
[JsonSerializable(typeof(RazorFormattingService.FormattingLogParameters))]
internal sealed partial class RemoteRazorJsonSerializerContext : JsonSerializerContext
{
    private static readonly Lazy<JsonSerializerOptions> s_jsonSerializerOptions = new(CreateJsonSerializerOptions);
    private static readonly Lazy<JsonSerializerOptions> s_formattingLogJsonSerializerOptions = new(CreateFormattingLogJsonSerializerOptions);
    private static readonly Lazy<RazorServiceDescriptors> s_jsonServiceDescriptors = new(
        static () => RazorServices.CreateJsonDescriptors(JsonSerializerOptions.TypeInfoResolver!));

    internal static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions.Value;
    internal static JsonSerializerOptions FormattingLogJsonSerializerOptions => s_formattingLogJsonSerializerOptions.Value;
    internal static RazorServiceDescriptors JsonServiceDescriptors => s_jsonServiceDescriptors.Value;

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonHelpers.JsonSerializerOptions);
        options.TypeInfoResolverChain.Insert(0, Default);
        return options;
    }

    private static JsonSerializerOptions CreateFormattingLogJsonSerializerOptions()
    {
        // Keep the existing log format rather than applying LSP converter and null-handling rules.
        var options = new JsonSerializerOptions { TypeInfoResolver = Default };
        options.TypeInfoResolverChain.Add(RazorJsonSerializerContext.Default);
        options.TypeInfoResolverChain.Add(ReflectionFallbackJsonTypeInfoResolver.Instance);
        return options;
    }
}

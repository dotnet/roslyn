// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

/// <summary>
/// Generated formatting-log contracts without a dependency on Razor Workspaces or the LSP protocol assembly.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(RazorFileKind))]
[JsonSerializable(typeof(FormattingEngineOptions))]
[JsonSerializable(typeof(LinePositionSpan?))]
[JsonSerializable(typeof(ImmutableArray<RazorTextChange>))]
[JsonSerializable(typeof(ImmutableArray<SourceMapping>))]
[JsonSerializable(typeof(ImmutableArray<LineInfo>))]
[JsonSerializable(typeof(TextChange[]))]
internal sealed partial class FormattingJsonSerializerContext : JsonSerializerContext
{
    private static readonly Lazy<JsonSerializerOptions> s_jsonSerializerOptions = new(CreateJsonSerializerOptions);

    internal static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions.Value;

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions { TypeInfoResolver = Default };
        options.TypeInfoResolverChain.Add(ReflectionFallbackJsonTypeInfoResolver.Instance);
        return options;
    }
}

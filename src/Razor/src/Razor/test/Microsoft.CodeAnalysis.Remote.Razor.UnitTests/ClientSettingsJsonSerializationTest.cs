// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Remote.Razor.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class ClientSettingsJsonSerializationTest
{
    [Fact]
    public void FormattingLoggerRejectsMissingMetadataInStrictValidation()
    {
        using var tempRoot = new TempRoot();
        var factory = new FormattingLoggerFactory();
        factory.SetLogDirectory(tempRoot.CreateDirectory().Path);
        var logger = Assert.IsType<FormattingLogger>(factory.CreateLogger("test.razor", "Full", RemoteRazorJsonSerializerContext.FormattingLogJsonSerializerOptions));

        Assert.Throws<NotSupportedException>(() => logger.LogObject("Unknown", new UnregisteredPayload()));
    }

    [Fact]
    public void FormattingLoggerPreservesLogShape()
    {
        using var tempRoot = new TempRoot();
        var logDirectory = tempRoot.CreateDirectory().Path;
        var factory = new FormattingLoggerFactory();
        factory.SetLogDirectory(logDirectory);
        var logger = Assert.IsType<FormattingLogger>(factory.CreateLogger("test.razor", "Full", RemoteRazorJsonSerializerContext.FormattingLogJsonSerializerOptions));
        var value = new FormattingEngineOptions();

        logger.LogObject("Options", value);
        logger.LogObject<LinePositionSpan?>("Range", null);

        var folder = Assert.Single(Directory.GetDirectories(logDirectory));
        Assert.Equal(JsonSerializer.Serialize(value, JsonSerializerOptions.Default), File.ReadAllText(Path.Combine(folder, "Options.json")));
        Assert.Equal("null", File.ReadAllText(Path.Combine(folder, "Range.json")));
    }

    [Fact]
    public void AllRazorOptionsKeepTheAuditedFallbackLast()
    {
        foreach (var options in new[] { JsonHelpers.JsonSerializerOptions, RemoteRazorJsonSerializerContext.JsonSerializerOptions, RemoteRazorJsonSerializerContext.FormattingLogJsonSerializerOptions })
        {
            Assert.Same(ReflectionFallbackJsonTypeInfoResolver.Instance, options.TypeInfoResolverChain[options.TypeInfoResolverChain.Count - 1]);
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new UnregisteredPayload(), options));
        }
    }

    private sealed class UnregisteredPayload
    {
        public int Value { get; set; }
    }

    [Fact]
    public void GeneratedMetadataCoversRazorContracts()
    {
        var options = new JsonSerializerOptions(RemoteRazorJsonSerializerContext.JsonSerializerOptions);
        options.MakeReadOnly();
        Assert.DoesNotContain(options.TypeInfoResolverChain, resolver => resolver is DefaultJsonTypeInfoResolver);
        Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(ClientSettingsJsonSerializationTest)));

        var assemblies = new[] { typeof(JsonHelpers).Assembly, typeof(RemoteRazorJsonSerializerContext).Assembly };
        var pending = new Queue<Type>(assemblies.SelectMany(assembly => assembly.GetTypes()).Where(type =>
            !type.ContainsGenericParameters && !type.IsInterface &&
            (typeof(DocumentResolveData).IsAssignableFrom(type) ||
             ((type.Namespace?.StartsWith("Microsoft.CodeAnalysis.Razor.Protocol", StringComparison.Ordinal) == true ||
               type.Namespace == "Roslyn.LanguageServer.Protocol" ||
               type.Namespace == "Microsoft.CodeAnalysis.Razor.CodeActions.Models") &&
              (type.GetCustomAttribute<DataContractAttribute>() is not null ||
               type.GetProperties().Any(property => property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null))))));
        Type[] objectPayloads =
        [
            typeof(CompletionListMerger.MergedCompletionListData),
            typeof(RazorFileKind),
            typeof(RazorFormattingOptions),
            typeof(FormattingEngineOptions),
            typeof(LinePositionSpan?),
            typeof(RazorTextChange[]),
            typeof(ImmutableArray<RazorTextChange>),
            typeof(ImmutableArray<LineInfo>),
            typeof(ImmutableArray<SourceMapping>),
            typeof(ImmutableArray<TextChange>),
            typeof(TextChange[]),
            typeof(RazorFormattingService.FormattingLogParameters),
            typeof(ClientSettings),
        ];
        foreach (var type in objectPayloads)
        {
            pending.Enqueue(type);
        }

        var visited = new HashSet<Type>();
        var missing = new SortedSet<string>();
        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            if (!visited.Add(type))
            {
                continue;
            }

            var info = options.TypeInfoResolver!.GetTypeInfo(type, options);
            if (info is null)
            {
                missing.Add(type.ToString());
                continue;
            }

            foreach (var property in info.Properties)
            {
                pending.Enqueue(property.PropertyType);
            }

            if (type.HasElementType)
            {
                pending.Enqueue(type.GetElementType()!);
            }

            foreach (var argument in type.GenericTypeArguments)
            {
                pending.Enqueue(argument);
            }
        }

        Assert.True(missing.Count == 0, "Missing generated Razor metadata:\n" + string.Join("\n", missing));

        // Formatting logs serialize the declared generic type, even when a nullable value is null.
        Assert.Equal("null", JsonSerializer.Serialize<LinePositionSpan?>(null, options));
        LinePositionSpan? range = new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4));
        using var rangeLog = JsonDocument.Parse(JsonSerializer.Serialize(range, options));
        Assert.Equal(1, rangeLog.RootElement.GetProperty("Start").GetProperty("Line").GetInt32());
        Assert.Equal(2, rangeLog.RootElement.GetProperty("Start").GetProperty("Character").GetInt32());
        Assert.Equal(3, rangeLog.RootElement.GetProperty("End").GetProperty("Line").GetInt32());
        Assert.Equal(4, rangeLog.RootElement.GetProperty("End").GetProperty("Character").GetInt32());
        Assert.Equal("[]", JsonSerializer.Serialize(ImmutableArray<RazorTextChange>.Empty, options));
        Assert.Equal("[]", JsonSerializer.Serialize(ImmutableArray<SourceMapping>.Empty, options));
        Assert.Equal("[]", JsonSerializer.Serialize(ImmutableArray<LineInfo>.Empty, options));
        Assert.Equal("[]", JsonSerializer.Serialize(ImmutableArray<TextChange>.Empty, options));
        Assert.Equal("[]", JsonSerializer.Serialize(Array.Empty<TextChange>(), options));

        var request = JsonSerializer.Deserialize<RazorVSInternalCompletionParams>(
            """{"textDocument":{"uri":"file:///test.razor"},"position":{"line":1,"character":2},"context":{"triggerKind":1,"_vs_invokeKind":2}}""",
            options)!;
        Assert.Equal(1, request.Position.Line);
        Assert.Equal(2, request.Position.Character);
        Assert.Equal((VSInternalCompletionInvokeKind)2, request.Context!.InvokeKind);
    }

    [Fact]
    public void ClientSettings_RoundTripsWithExpectedPropertyNames()
    {
        var settings = new ClientSettings(
            new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 2),
            new ClientCompletionSettings(AutoShowCompletion: false, AutoListParams: false),
            new ClientAdvancedSettings(
                FormatOnType: false,
                AutoClosingTags: false,
                AutoInsertAttributeQuotes: false,
                ColorBackground: true,
                CodeBlockBraceOnNextLine: true,
                AttributeIndentStyle: AttributeIndentStyle.IndentByOne,
                IgnoreIndentationInScriptOrStyleBlocksWithComplexRazor: false,
                CommitElementsWithSpace: false,
                SnippetSetting: SnippetSetting.None,
                LogLevel: LogLevel.Trace,
                FormatOnPaste: false,
                ShowAllCSharpCodeActions: true,
                TaskListDescriptors: ["TODO", "HACK"]));

        var json = JsonSerializer.Serialize(settings, JsonHelpers.JsonSerializerOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("clientSpaceSettings", out var clientSpaceSettings));
        Assert.True(root.TryGetProperty("clientCompletionSettings", out var clientCompletionSettings));
        Assert.True(root.TryGetProperty("advancedSettings", out var advancedSettings));

        Assert.True(clientSpaceSettings.TryGetProperty("indentWithTabs", out var indentWithTabs));
        Assert.True(indentWithTabs.GetBoolean());
        Assert.True(clientSpaceSettings.TryGetProperty("indentSize", out var indentSize));
        Assert.Equal(2, indentSize.GetInt32());

        Assert.True(clientCompletionSettings.TryGetProperty("autoShowCompletion", out var autoShowCompletion));
        Assert.False(autoShowCompletion.GetBoolean());
        Assert.True(clientCompletionSettings.TryGetProperty("autoListParams", out var autoListParams));
        Assert.False(autoListParams.GetBoolean());

        Assert.True(advancedSettings.TryGetProperty("formatOnType", out var formatOnType));
        Assert.False(formatOnType.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("autoClosingTags", out var autoClosingTags));
        Assert.False(autoClosingTags.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("autoInsertAttributeQuotes", out var autoInsertAttributeQuotes));
        Assert.False(autoInsertAttributeQuotes.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("colorBackground", out var colorBackground));
        Assert.True(colorBackground.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("codeBlockBraceOnNextLine", out var codeBlockBraceOnNextLine));
        Assert.True(codeBlockBraceOnNextLine.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("attributeIndentStyle", out var attributeIndentStyle));
        Assert.Equal((int)AttributeIndentStyle.IndentByOne, attributeIndentStyle.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("ignoreIndentationInScriptOrStyleBlocksWithComplexRazor", out var ignoreIndentationInScriptOrStyleBlocksWithComplexRazor));
        Assert.False(ignoreIndentationInScriptOrStyleBlocksWithComplexRazor.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("commitElementsWithSpace", out var commitElementsWithSpace));
        Assert.False(commitElementsWithSpace.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("snippetSetting", out var snippetSetting));
        Assert.Equal((int)SnippetSetting.None, snippetSetting.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("logLevel", out var logLevel));
        Assert.Equal((int)LogLevel.Trace, logLevel.GetInt32());
        Assert.True(advancedSettings.TryGetProperty("formatOnPaste", out var formatOnPaste));
        Assert.False(formatOnPaste.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("showAllCSharpCodeActions", out var showAllCSharpCodeActions));
        Assert.True(showAllCSharpCodeActions.GetBoolean());
        Assert.True(advancedSettings.TryGetProperty("taskListDescriptors", out var taskListDescriptors));
        Assert.Collection<string>(taskListDescriptors.EnumerateArray().Select(e => e.GetString())!,
            descriptor => Assert.Equal("TODO", descriptor),
            descriptor => Assert.Equal("HACK", descriptor));

        var roundTripped = JsonSerializer.Deserialize<ClientSettings>(json, JsonHelpers.JsonSerializerOptions);
        Assert.Equal(settings, roundTripped);
    }
}

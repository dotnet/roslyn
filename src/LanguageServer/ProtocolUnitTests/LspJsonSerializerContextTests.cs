// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class LspJsonSerializerContextTests
{
    [Fact]
    public void SharedExtensionConvertersPreserveEachOptionsResolver()
    {
        var firstOptions = CreateOptions("first");
        var secondOptions = CreateOptions("second");
        var value = new CodeAction { Title = "test" };

        Assert.Contains("\"first\":\"test\"", JsonSerializer.Serialize(value, firstOptions));
        Assert.Contains("\"second\":\"test\"", JsonSerializer.Serialize(value, secondOptions));
        Assert.Contains("\"first\":\"test\"", JsonSerializer.Serialize(value, firstOptions));

        static JsonSerializerOptions CreateOptions(string titlePropertyName)
        {
            var options = new JsonSerializerOptions(ProtocolConversions.LspJsonSerializerOptions);
            options.TypeInfoResolver = options.TypeInfoResolver!.WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(CodeAction) && typeInfo.Kind == JsonTypeInfoKind.Object)
                    typeInfo.Properties.Single(property => property.Name == "title").Name = titlePropertyName;
            });
            return options;
        }
    }

    [Fact]
    public void ProtocolTypesAreCoveredByGeneratedMetadata()
    {
        var assembly = typeof(InitializeParams).Assembly;
        var pending = new Queue<Type>(assembly.GetTypes().Where(type =>
            !type.ContainsGenericParameters && !type.IsInterface &&
            (type.GetCustomAttribute<JsonConverterAttribute>() is not null ||
             typeof(DocumentResolveData).IsAssignableFrom(type) ||
             type.GetProperties().Any(property =>
                 property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null ||
                 property.GetCustomAttribute<JsonIgnoreAttribute>() is not null))));
        var visited = new HashSet<Type>();
        var missing = new SortedSet<string>();
        var formatter = RoslynLanguageServer.CreateJsonMessageFormatter();
        var options = formatter.JsonSerializerOptions;
        options.MakeReadOnly();
        using var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(Stream.Null, Stream.Null, formatter));
        var requestOptions = new JsonSerializerOptions(options);
        LspJsonSerializerContext.AddProgressConverters(requestOptions, rpc);
        requestOptions.MakeReadOnly();

        Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(LspJsonSerializerContextTests)));
        var documentUri = new DocumentUri("file:///C:/test.cs");
        var documentUriJson = JsonSerializer.Serialize(documentUri, LspJsonSerializerContext.Default.DocumentUri);
        Assert.Equal("\"file:///C:/test.cs\"", documentUriJson);
        Assert.Equal(documentUri, JsonSerializer.Deserialize(documentUriJson, LspJsonSerializerContext.Default.DocumentUri));
        Assert.NotNull(options.GetTypeInfo(typeof(CommonErrorData)));
        Assert.Equal("test value", JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(
            """["test value"]""", options)![0]!.GetValue<string>());
        var jsonObject = JsonNode.Parse("""{"value":1}""")!;
        Assert.Equal("""{"value":1}""", JsonSerializer.Serialize<object>(jsonObject, options));
        Assert.Equal("1", JsonSerializer.Serialize<object>(JsonValue.Create(1)!, options));
        Assert.Equal(1, JsonSerializer.Deserialize<JsonObject>("""{"value":1}""", options)!["value"]!.GetValue<int>());
        Assert.IsAssignableFrom<JsonRpcRequest>(formatter.Deserialize(new ReadOnlySequence<byte>(
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}"""u8.ToArray())));
        Assert.IsAssignableFrom<JsonRpcResult>(formatter.Deserialize(new ReadOnlySequence<byte>(
            """{"jsonrpc":"2.0","id":1,"result":null}"""u8.ToArray())));
        Assert.Equal(new RequestId(1), JsonSerializer.Deserialize<RequestId>("1", options));
        Assert.Equal(new RequestId("request"), JsonSerializer.Deserialize<RequestId>("\"request\"", options));
        Assert.Equal(RequestId.Null, JsonSerializer.Deserialize<RequestId>("null", options));
        var cancellation = Assert.IsAssignableFrom<JsonRpcRequest>(formatter.Deserialize(new ReadOnlySequence<byte>(
            """{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":1}}"""u8.ToArray())));
        Assert.True(cancellation.TryGetArgumentByNameOrIndex("id", 0, typeof(RequestId), out var cancelledRequestId));
        Assert.Equal(new RequestId(1), Assert.IsType<RequestId>(cancelledRequestId));
        Assert.NotNull(options.GetTypeInfo(typeof(SumType<DocumentSymbol[], SymbolInformation[]>?)));
        try
        {
            throw new InvalidOperationException("test failure");
        }
        catch (InvalidOperationException exception)
        {
            var guidKey = Guid.NewGuid();
            exception.Data["test key"] = "test value";
            exception.Data[guidKey] = guidKey;
            var json = JsonSerializer.Serialize<object>(exception, options);
            Assert.Contains("test failure", json);
            var deserializedException = JsonSerializer.Deserialize<Exception>(json, options)!;
            Assert.Equal(exception.Message, deserializedException.Message);
            Assert.Equal("test value", deserializedException.Data["test key"]);
            Assert.Equal(guidKey.ToString(), deserializedException.Data[guidKey.ToString()]);
        }
        var invalidRequest = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<InitializeParams>("{", requestOptions));
        var invalidRequestJson = JsonSerializer.Serialize<object>(invalidRequest, options);
        Assert.Equal(invalidRequest.Message, JsonSerializer.Deserialize<Exception>(invalidRequestJson, options)!.Message);

        foreach (var type in assembly.GetTypes())
        {
            if (type.GetCustomAttribute<KindAttribute>() is { } kind)
            {
                var registeredKind = SumConverter.SumTypeInfoCache.UnionTypeInfo.GetKind(type);
                Assert.NotNull(registeredKind);
                Assert.Equal(kind.Kind, registeredKind.Kind);
                Assert.Equal(kind.KindPropertyName, registeredKind.KindPropertyName);
            }

            foreach (var contract in type.GetInterfaces().Where(contract => contract.IsGenericType))
            {
                var definition = contract.GetGenericTypeDefinition();
                if (definition == typeof(IRequestHandler<,,>) || definition == typeof(IRequestHandler<,>) ||
                    definition == typeof(INotificationHandler<,>))
                {
                    foreach (var argument in contract.GenericTypeArguments.Take(contract.GenericTypeArguments.Length - 1))
                        pending.Enqueue(argument);
                }
            }
        }

        while (pending.Count > 0)
        {
            var type = pending.Dequeue();
            if (type.ContainsGenericParameters || !visited.Add(type))
                continue;

            if (LspJsonSerializerContext.Default.GetTypeInfo(type) is null)
                missing.Add(FormatType(type));

            if (type.HasElementType)
                pending.Enqueue(type.GetElementType()!);

            foreach (var argument in type.GenericTypeArguments)
                pending.Enqueue(argument);

            if (type.Assembly != assembly)
                continue;

            foreach (var property in type.GetProperties())
            {
                if (property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null ||
                    property.GetCustomAttribute<JsonIncludeAttribute>() is not null)
                {
                    pending.Enqueue(property.PropertyType);
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<JsonIncludeAttribute>() is not null)
                    pending.Enqueue(field.FieldType);
            }
        }

        Assert.True(missing.Count == 0, "Missing generated metadata:\n" + string.Join("\n", missing));

        foreach (var type in visited)
        {
            Assert.NotNull(options.GetTypeInfo(type));
            Assert.NotNull(requestOptions.GetTypeInfo(type));
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IProgress<>))
            {
                Assert.Equal(typeof(LspProgressConverter<>).MakeGenericType(type.GenericTypeArguments),
                    requestOptions.GetConverter(type).GetType());
            }
        }

        var testOptions = new JsonSerializerOptions(options);
        testOptions.MakeReadOnly();

        using var progressDictionary = JsonDocument.Parse(JsonSerializer.Serialize<object>(
            new Dictionary<string, WorkDoneProgress> { ["test"] = new WorkDoneProgressEnd() }, testOptions));
        Assert.Equal("end", progressDictionary.RootElement.GetProperty("test").GetProperty("kind").GetString());

        static string FormatType(Type type)
        {
            if (type.IsArray)
                return FormatType(type.GetElementType()!) + "[]";
            if (type.IsGenericType)
                return type.GetGenericTypeDefinition().FullName!.Split('`')[0].Replace('+', '.') +
                    "<" + string.Join(", ", type.GenericTypeArguments.Select(FormatType)) + ">";
            return type.FullName!.Replace('+', '.');
        }
    }
}

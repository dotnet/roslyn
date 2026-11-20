// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ReflectionFallbackJsonTypeInfoResolverTests
{
    [Fact]
    public void ProductionOptionsCanSerializeUnknownPayloads()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);
        var payload = new UnknownPayload { Value = 42 };

        var json = JsonSerializer.Serialize(payload, options);

        Assert.Equal("""{"value":42}""", json);
        Assert.Equal(payload.Value, JsonSerializer.Deserialize<UnknownPayload>(json, options)!.Value);
        Assert.Equal([typeof(UnknownPayload)], reportedTypes);
    }

    [Fact]
    public void GeneratedContractsDoNotInitializeReflection()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out var fallback);

        Assert.Equal("""{"line":1,"character":2}""", JsonSerializer.Serialize(new Position(1, 2), options));
        Assert.Empty(reportedTypes);
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void RuntimeObjectPayloadUsesAuditedFallback()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);

        Assert.Equal("""{"value":42}""", JsonSerializer.Serialize<object>(new UnknownPayload { Value = 42 }, options));
        Assert.Equal([typeof(UnknownPayload)], reportedTypes);
    }

    [Fact]
    public void FrameworkCollectionDoesNotHideUnknownPayload()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);

        Assert.Equal("""[{"value":42}]""", JsonSerializer.Serialize(new List<UnknownPayload> { new() { Value = 42 } }, options));
        Assert.Contains(typeof(List<UnknownPayload>), reportedTypes);
        Assert.Contains(typeof(UnknownPayload), reportedTypes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeDictionaryPreservesDeclaredValuePolymorphism(bool useDerivedDictionary)
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);
        Dictionary<string, UnknownPolymorphicPayload> payload = useDerivedDictionary ? new UnknownPayloadDictionary() : new();
        payload["test"] = new UnknownDerivedPayload();

        var json = JsonSerializer.Serialize<object>(payload, options);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("derived", document.RootElement.GetProperty("test").GetProperty("kind").GetString());
        Assert.Contains(payload.GetType(), reportedTypes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StrictValidationAuditsRuntimeDictionaryContract(bool useDerivedDictionary)
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: true, reportedTypes.Add, out var fallback);
        Dictionary<string, Position> payload = useDerivedDictionary ? new UnknownPositionDictionary() : new();
        payload["test"] = new Position(1, 2);

        var exception = Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize<object>(payload, options));

        Assert.Contains(payload.GetType().ToString(), exception.Message);
        Assert.Empty(reportedTypes);
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void FrameworkContractsAreAuditedRegardlessOfAssembly()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);

        Assert.Equal("\"1.2\"", JsonSerializer.Serialize(new Version(1, 2), options));
        Assert.Equal([typeof(Version)], reportedTypes);
    }

    [Fact]
    public void PrivateExceptionDictionaryDoesNotInvokeReflectionFallback()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out var fallback);
        var exception = new InvalidOperationException("test failure");
        exception.Data["value"] = 42;

        var json = JsonSerializer.Serialize<object>(exception, options);

        Assert.Contains("\"value\":42", json);
        Assert.Empty(reportedTypes);
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void SyntheticTestResolverDoesNotHideNonTestContracts()
    {
        var resolver = new TestJsonTypeInfoResolver(typeof(ReflectionFallbackJsonTypeInfoResolverTests).Assembly);
        var options = RoslynLanguageServer.CreateJsonMessageFormatter().JsonSerializerOptions;
        options.AddLspSerializerContext(resolver);

        Assert.Equal("""{"value":42}""", JsonSerializer.Serialize(new UnknownPayload { Value = 42 }, options));
        Assert.Null(resolver.GetTypeInfo(typeof(Position), options));
        Assert.Null(resolver.GetTypeInfo(typeof(List<Position>), options));
        Assert.Null(resolver.GetTypeInfo(typeof(Version), options));
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new Version(1, 2), options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void StrictValidationRejectsMissingMetadataBeforeReflection(int shape)
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: true, reportedTypes.Add, out var fallback);
        options.MakeReadOnly();
        var type = shape switch
        {
            0 => typeof(UnknownPayload),
            1 => typeof(UnknownPayload[]),
            2 => typeof(List<UnknownPayload>),
            _ => typeof(Dictionary<string, UnknownPayload>),
        };

        var exception = Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(type));

        Assert.Contains(type.ToString(), exception.Message);
        Assert.Contains("owning JsonSerializerContext", exception.Message);
        Assert.Empty(reportedTypes);
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void TestProcessesRequireGeneratedMetadata()
    {
        Assert.Equal("1", Environment.GetEnvironmentVariable(
            ReflectionFallbackJsonTypeInfoResolver.RequireGeneratedMetadataEnvironmentVariable));
        var options = RoslynLanguageServer.CreateJsonMessageFormatter().JsonSerializerOptions;

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(new UnknownPayload(), options));
    }

    [Fact]
    public void FallbackIsReportedOnceAcrossOptionsCopies()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);
        JsonSerializer.Serialize(new UnknownPayload(), options);
        var copy = new JsonSerializerOptions(options);

        JsonSerializer.Serialize(new UnknownPayload(), copy);

        Assert.Equal([typeof(UnknownPayload)], reportedTypes);
    }

    [Fact]
    public void ConcurrentFallbackIsReportedOnce()
    {
        var reportCount = 0;
        var options = CreateOptions(requireGeneratedMetadata: false, _ => Interlocked.Increment(ref reportCount), out var fallback);
        options.MakeReadOnly();

        Parallel.For(0, 20, _ => fallback.GetTypeInfo(typeof(UnknownPayload), options));

        Assert.Equal(1, reportCount);
        Assert.True(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void AdditionalContractResolversPrecedeFallback()
    {
        var options = RoslynLanguageServer.CreateJsonMessageFormatter().JsonSerializerOptions;
        options.AddLspSerializerContext(new ProvidedPayloadResolver());
        var fallback = new ReflectionFallbackJsonTypeInfoResolver(requireGeneratedMetadata: true);
        options.TypeInfoResolverChain[options.TypeInfoResolverChain.Count - 1] = fallback;

        Assert.Equal("""{"value":42}""", JsonSerializer.Serialize(new UnknownPayload { Value = 42 }, options));
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void ConverterFailuresAreNotRetriedWithReflection()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out var fallback);
        options.Converters.Insert(0, new FailingIntConverter());

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(42, options));

        Assert.Contains("Expected converter failure", exception.Message);
        Assert.Empty(reportedTypes);
        Assert.False(fallback.IsReflectionResolverCreated);
    }

    [Fact]
    public void UnsupportedReflectionContractsStillFail()
    {
        var reportedTypes = new List<Type>();
        var options = CreateOptions(requireGeneratedMetadata: false, reportedTypes.Add, out _);
        Action payload = static () => { };

        Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(payload, options));
        Assert.Contains(typeof(Action), reportedTypes);
    }

    private static JsonSerializerOptions CreateOptions(
        bool requireGeneratedMetadata, Action<Type> reportFallback, out ReflectionFallbackJsonTypeInfoResolver fallback)
    {
        var options = RoslynLanguageServer.CreateJsonMessageFormatter().JsonSerializerOptions;
        fallback = new ReflectionFallbackJsonTypeInfoResolver(requireGeneratedMetadata, reportFallback);
        Assert.Same(ReflectionFallbackJsonTypeInfoResolver.Instance, options.TypeInfoResolverChain[options.TypeInfoResolverChain.Count - 1]);
        options.TypeInfoResolverChain[options.TypeInfoResolverChain.Count - 1] = fallback;
        return options;
    }

    private sealed class UnknownPayload
    {
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
    [JsonDerivedType(typeof(UnknownDerivedPayload), "derived")]
    private abstract class UnknownPolymorphicPayload;

    private sealed class UnknownDerivedPayload : UnknownPolymorphicPayload;

    private sealed class UnknownPayloadDictionary : Dictionary<string, UnknownPolymorphicPayload>;

    private sealed class UnknownPositionDictionary : Dictionary<string, Position>;

    private sealed class ProvidedPayloadResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            => type == typeof(UnknownPayload)
                ? JsonMetadataServices.CreateValueInfo<UnknownPayload>(options, new PayloadConverter())
                : null;
    }

    private sealed class PayloadConverter : JsonConverter<UnknownPayload>
    {
        public override UnknownPayload Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return new UnknownPayload { Value = document.RootElement.GetProperty("value").GetInt32() };
        }

        public override void Write(Utf8JsonWriter writer, UnknownPayload value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("value", value.Value);
            writer.WriteEndObject();
        }
    }

    private sealed class FailingIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new JsonException("Expected converter failure");

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            => throw new JsonException("Expected converter failure");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Converters;

/// <summary>
/// Tests for SumConverter's type filtering logic, which avoids costly exception-based
/// type probing by rejecting incompatible SumType arms based on the JSON token type.
/// This includes StartObject rejection for primitive types and array element peeking.
/// </summary>
public sealed class SumConverterTypeFilteringTests
{
    [Fact]
    public void Deserialize_StringArray_MatchesStringArrayArm()
    {
        var json = """{"value": ["a", "b", "c"]}""";
        var result = DeserializeWithNoExceptions<StringOrVSInternalCommitCharacterArrayHolder>(json);

        Assert.True(result.Value.TryGetFirst(out var strings));
        Assert.Equal(["a", "b", "c"], strings);
    }

    [Fact]
    public void Deserialize_ObjectArray_MatchesObjectArrayArm()
    {
        var json = """{"value": [{"_vs_character": "x", "_vs_insert": false}]}""";
        var result = DeserializeWithNoExceptions<StringOrVSInternalCommitCharacterArrayHolder>(json);

        Assert.True(result.Value.TryGetSecond(out var objects));
        Assert.Single(objects);
        Assert.Equal("x", objects[0].Character);
        Assert.False(objects[0].Insert);
    }

    [Fact]
    public void Deserialize_EmptyArray_MatchesFirstArrayArm()
    {
        var json = """{"value": []}""";
        var result = DeserializeWithNoExceptions<StringOrVSInternalCommitCharacterArrayHolder>(json);

        // Empty array is compatible with any array type; first arm wins.
        Assert.True(result.Value.TryGetFirst(out _));
    }

    [Fact]
    public void Deserialize_NumberArray_MatchesIntArrayArm()
    {
        var json = """{"value": [1, 2, 3]}""";
        var result = DeserializeWithNoExceptions<IntOrStringArrayHolder>(json);

        Assert.True(result.Value.TryGetFirst(out var ints));
        Assert.Equal([1, 2, 3], ints);
    }

    [Fact]
    public void Deserialize_NumberArray_DoesNotMatchStringArrayArm()
    {
        // When the SumType has string[] first and int[] second,
        // a number array should skip string[] and match int[].
        var json = """{"value": [42]}""";
        var result = DeserializeWithNoExceptions<StringOrIntArrayHolder>(json);

        Assert.True(result.Value.TryGetSecond(out var ints));
        Assert.Equal([42], ints);
    }

    [Fact]
    public void Deserialize_NestedArrays_MatchesCorrectArm()
    {
        // SumType<string[][], int[]>: nested array should match string[][] not int[]
        var json = """{"value": [["a", "b"], ["c"]]}""";
        var result = DeserializeWithNoExceptions<NestedStringOrIntArrayHolder>(json);

        Assert.True(result.Value.TryGetFirst(out var nested));
        Assert.Equal(2, nested.Length);
        Assert.Equal(["a", "b"], nested[0]);
        Assert.Equal(["c"], nested[1]);
    }

    [Fact]
    public void Deserialize_SumTypeElementArray_MatchesCorrectArm()
    {
        // When the array element type is itself a SumType (has a custom JsonConverter),
        // peeking cannot determine compatibility, so the array arm must not be rejected.
        var json = """{"value": ["hello"]}""";
        var result = JsonSerializer.Deserialize<SumTypeOrBoolArrayHolder>(json);

        Assert.NotNull(result);
        Assert.True(result.Value.TryGetFirst(out var array));
        Assert.Single(array);
        Assert.True(array[0].TryGetFirst(out var str));
        Assert.Equal("hello", str);
    }

    [Fact]
    public void Deserialize_ObjectArray_SkipsNestedArrayTypeArm()
    {
        // Array element peeking sees a StartObject token in the first element.
        // int[][] should be rejected because its element type (int[]) is an array
        // and a JSON object can never deserialize into an array type. Without the
        // !type.IsArray check on the StartObject case, int[] would pass the filter
        // (it's not a primitive type) causing an unnecessary deserialization attempt.
        var json = """{"value": [{"name": "a"}, {"name": "b"}]}""";
        var result = DeserializeWithNoExceptions<NestedIntArrayOrObjectArrayHolder>(json);

        Assert.True(result.Value.TryGetSecond(out var objects));
        Assert.Equal(2, objects.Length);
        Assert.Equal("a", objects[0].Name);
        Assert.Equal("b", objects[1].Name);
    }

    [Fact]
    public void Deserialize_ArrayWithOptionsRegisteredElementConverter_MatchesViaFallback()
    {
        // StringWrapper has no type-level [JsonConverter], so IsTokenCompatibleWithType
        // sees a String element token and rejects StringWrapper[] (StringWrapper isn't
        // string/Uri/etc.). int[] is also rejected (Number != String token).
        // Pass 1 skips both; pass 2 retries and StringWrapper[] succeeds because the
        // converter registered via options handles String tokens for StringWrapper.
        // Without the two-pass approach, this would throw "No sum type match".
        var json = """{"value": ["hello", "world"]}""";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StringWrapperConverter());

        var result = JsonSerializer.Deserialize<IntOrStringWrapperArrayHolder>(json, options);

        Assert.NotNull(result);
        Assert.True(result.Value.TryGetSecond(out var wrappers));
        Assert.Equal(2, wrappers.Length);
        Assert.Equal("hello", wrappers[0].Text);
        Assert.Equal("world", wrappers[1].Text);
    }

    /// <summary>
    /// Deserializes JSON while asserting that no exceptions are thrown internally.
    /// This verifies that the type filtering logic is actually skipping incompatible
    /// arms rather than relying on the old try/catch fallback behavior.
    /// </summary>
    private static T DeserializeWithNoExceptions<T>(string json)
    {
        var exceptions = new List<Exception>();
        var threadId = Environment.CurrentManagedThreadId;
        void handler(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (Environment.CurrentManagedThreadId == threadId)
                exceptions.Add(e.Exception);
        }

        AppDomain.CurrentDomain.FirstChanceException += handler;
        try
        {
            var result = JsonSerializer.Deserialize<T>(json);
            Assert.NotNull(result);
            Assert.Empty(exceptions);
            return result;
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= handler;
        }
    }

    // Test holder types: SumType is deserialized by SumConverter via [JsonConverter] attribute

    private sealed class StringOrVSInternalCommitCharacterArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<string[], VSInternalCommitCharacter[]> Value { get; set; }
    }

    private sealed class IntOrStringArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<int[], string[]> Value { get; set; }
    }

    private sealed class StringOrIntArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<string[], int[]> Value { get; set; }
    }

    private sealed class NestedStringOrIntArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<string[][], int[]> Value { get; set; }
    }

    private sealed class SumTypeOrBoolArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<SumType<string, int>[], bool> Value { get; set; }
    }

    private sealed class IntOrStringWrapperArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<int[], StringWrapper[]> Value { get; set; }
    }

    private sealed class NestedIntArrayOrObjectArrayHolder
    {
        [JsonPropertyName("value")]
        public SumType<int[][], SimpleObject[]> Value { get; set; }
    }

    internal sealed class SimpleObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A type with no [JsonConverter] attribute. Its converter is registered via
    /// JsonSerializerOptions to test the two-pass fallback for options-level converters.
    /// </summary>
    internal sealed class StringWrapper
    {
        public string Text { get; set; } = string.Empty;
    }

    private sealed class StringWrapperConverter : JsonConverter<StringWrapper>
    {
        public override StringWrapper Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new StringWrapper { Text = reader.GetString()! };
        }

        public override void Write(Utf8JsonWriter writer, StringWrapper value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Text);
        }
    }
}

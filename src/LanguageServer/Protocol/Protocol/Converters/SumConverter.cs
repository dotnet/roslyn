// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class SumConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(ISumType).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(SumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    internal sealed class SumTypeInfoCache
    {
        // netstandard1.0 doesn't support Array.Empty
#pragma warning disable CA1825 // Avoid zero-length array allocations.
        private static readonly IReadOnlyList<UnionTypeInfo> EmptyUnionInfos = new UnionTypeInfo[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations.

        private readonly IReadOnlyList<UnionTypeInfo> allUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> primitiveUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> arrayUnionTypeInfos;

        private readonly IReadOnlyList<UnionTypeInfo> objectUnionTypeInfos;

        public SumTypeInfoCache(Type sumTypeType)
        {
            var allUnionTypeInfosSet = new List<UnionTypeInfo>();
            List<UnionTypeInfo>? primitiveUnionTypeInfosSet = null;
            List<UnionTypeInfo>? arrayUnionTypeInfosSet = null;
            List<UnionTypeInfo>? objectUnionTypeInfosSet = null;

            // If the SumType is a nullable extract the underlying type and re-assign
            sumTypeType = NormalizeToNonNullable(sumTypeType);

            var typeInfo = sumTypeType.GetTypeInfo();
            var parameterTypes = typeInfo.GenericTypeArguments;
            foreach (var parameterType in parameterTypes)
            {
                var parameterTypeInfo = NormalizeToNonNullable(parameterType).GetTypeInfo();
                var declaredConstructor = typeInfo.GetConstructor([parameterType]) ??
                    throw new ArgumentException(nameof(sumTypeType), "All constructor parameter types must be represented in the generic type arguments of the SumType");

                var unionTypeInfo = new UnionTypeInfo(parameterType, declaredConstructor);
                allUnionTypeInfosSet.Add(unionTypeInfo);

                if (parameterTypeInfo.IsPrimitive ||
                    parameterTypeInfo == typeof(string) ||
                    parameterTypeInfo == typeof(DocumentUri) ||
                    parameterTypeInfo == typeof(Uri) ||
                    typeof(IStringEnum).IsAssignableFrom(parameterTypeInfo))
                {
                    primitiveUnionTypeInfosSet ??= [];
                    primitiveUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else if (parameterTypeInfo.IsArray)
                {
                    arrayUnionTypeInfosSet ??= [];
                    arrayUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else
                {
                    objectUnionTypeInfosSet ??= [];
                    objectUnionTypeInfosSet.Add(unionTypeInfo);
                }
            }

            this.allUnionTypeInfos = allUnionTypeInfosSet;
            this.primitiveUnionTypeInfos = primitiveUnionTypeInfosSet ?? EmptyUnionInfos;
            this.arrayUnionTypeInfos = arrayUnionTypeInfosSet ?? EmptyUnionInfos;
            this.objectUnionTypeInfos = objectUnionTypeInfosSet ?? EmptyUnionInfos;
        }

        public IReadOnlyList<UnionTypeInfo> GetApplicableInfos(JsonTokenType startingTokenType)
        {
            return startingTokenType switch
            {
                JsonTokenType.StartArray
                    => this.arrayUnionTypeInfos,
                JsonTokenType.Number or
                JsonTokenType.String or
                JsonTokenType.True or
                JsonTokenType.False
                    => this.primitiveUnionTypeInfos,
                JsonTokenType.StartObject
                    => this.objectUnionTypeInfos,
                _ => this.allUnionTypeInfos,
            };
        }

        public int GetApplicableInfoIndex(Type t)
        {
            for (var i = 0; i < this.allUnionTypeInfos.Count; i++)
            {
                var unionTypeInfo = this.allUnionTypeInfos[i];
                if (unionTypeInfo.Type.IsAssignableFrom(t))
                {
                    return i;
                }
            }

            throw new System.Text.Json.JsonException($"No sum type match for {t.FullName}");
        }

        public UnionTypeInfo? TryGetTypeInfoFromIndex(int index)
            => index >= 0 && index < this.allUnionTypeInfos.Count ? this.allUnionTypeInfos[index] : null;

        private static Type NormalizeToNonNullable(Type sumTypeType)
        {
            return Nullable.GetUnderlyingType(sumTypeType) ?? sumTypeType;
        }

        public sealed class UnionTypeInfo
        {
            // System.Text.Json can pre-compile the generic SumType<> constructor call so we don't need to do it through reflection every time.
            internal delegate T StjReader<T>(ref Utf8JsonReader reader, JsonSerializerOptions options);

            private static readonly Type[] expressionLambdaMethodTypes = [typeof(Type), typeof(Expression), typeof(ParameterExpression[])];
            private static readonly MethodInfo expressionLambdaMethod = typeof(Expression)
                .GetMethods()
                .Where(mi =>
                                !mi.IsGenericMethod &&
                                mi.Name == "Lambda" &&
                                mi.GetParameters()
                                .Select(p => p.ParameterType)
                                .SequenceEqual(expressionLambdaMethodTypes))
                .Single();

            private static readonly Type[] jsonSerializerDeserializeMethodTypes = [typeof(Utf8JsonReader).MakeByRefType(), typeof(JsonSerializerOptions)];
            private static readonly MethodInfo jsonSerializerDeserializeMethod = typeof(JsonSerializer)
                .GetMethods()
                .Where(mi =>
                                mi.IsGenericMethod &&
                                mi.Name == "Deserialize" &&
                                mi.GetParameters()
                                .Select(p => p.ParameterType)
                                .SequenceEqual(jsonSerializerDeserializeMethodTypes))
                .Single();

            public UnionTypeInfo(Type type, ConstructorInfo constructor)
            {
                this.Type = type ?? throw new ArgumentNullException(nameof(type));
                this.Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));

                var param1 = Expression.Parameter(typeof(Utf8JsonReader).MakeByRefType(), "reader");
                var param2 = Expression.Parameter(typeof(JsonSerializerOptions), "options");
                var body = Expression.New(
                    constructor,
                    Expression.Call(
                        jsonSerializerDeserializeMethod.MakeGenericMethod(type),
                        param1,
                        param2));
                var expression = (LambdaExpression)expressionLambdaMethod.Invoke(null, [typeof(StjReader<>).MakeGenericType(constructor.DeclaringType), body, new[] { param1, param2 }])!;

                StjReaderFunction = expression.Compile();
            }

            public Type Type { get; }

            public ConstructorInfo Constructor { get; }

            public object StjReaderFunction { get; }
        }
    }
}

internal sealed class SumConverter<T> : JsonConverter<T>
    where T : struct, ISumType
{
    private static readonly ConcurrentDictionary<Type, SumConverter.SumTypeInfoCache> SumTypeCache = new ConcurrentDictionary<Type, SumConverter.SumTypeInfoCache>();

    /// <inheritdoc/>
    public override T Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            // We shouldn't really ever have a non-Nullable SumType being received as null but, if we do, return an "empty" SumType
            return (T)Activator.CreateInstance(objectType)!;
        }

        // objectType will be one of the various SumType variants. In order for this converter to work with all SumTypes it has to use reflection.
        // This method works by attempting to deserialize the json into each of the type parameters to a SumType and stops at the first success.
        var sumTypeInfoCache = SumTypeCache.GetOrAdd(objectType, (t) => new SumConverter.SumTypeInfoCache(t));

        var backupReader = reader;
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Peek ahead to see if we have there is a discriminator hint to aid in deserialization
            if (reader.Read()
                && reader.TokenType == JsonTokenType.PropertyName
                && GetStringAndCompare(ref reader, "_vs_discriminatorIndex")
                && reader.Read()
                && reader.TokenType == JsonTokenType.Number
                && reader.GetInt16() is short index
                && reader.Read()
                && reader.TokenType == JsonTokenType.PropertyName
                && GetStringAndCompare(ref reader, "_vs_value")
                && sumTypeInfoCache.TryGetTypeInfoFromIndex(index) is SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo
                && reader.Read())
            {
                if (TryInvokeCtor(ref reader, unionTypeInfo, options) is T result)
                {
                    reader.Read();
                    return result;
                }
            }

            reader = backupReader;
        }

        var applicableUnionTypeInfos = sumTypeInfoCache.GetApplicableInfos(reader.TokenType);
        for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
        {
            var unionTypeInfo = applicableUnionTypeInfos[i];

            if (TryInvokeCtor(ref reader, unionTypeInfo, options) is T result)
            {
                return result;
            }

            reader = backupReader;
        }

        throw new JsonException($"No sum type match for {objectType}");

        static T? TryInvokeCtor(ref Utf8JsonReader reader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo, JsonSerializerOptions options)
        {
            if (!IsTokenCompatibleWithType(reader.TokenType, unionTypeInfo.Type))
            {
                return null;
            }

            try
            {
                var stjReader = (SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction;
                return stjReader.Invoke(ref reader, options);
            }
            catch
            {
                return null;
            }
        }
    }

    private static bool GetStringAndCompare(ref readonly Utf8JsonReader reader, string toCompare)
    {
        var valueLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
        const int MaxStackAllocLen = 64;

        if (valueLength <= MaxStackAllocLen)
        {
            Span<char> scratchChars = stackalloc char[(int)valueLength];

            // If the value fits into the scratch buffer, copy it there.
            var actualLength = reader.CopyString(scratchChars);

            return scratchChars.Slice(0, actualLength).SequenceEqual(toCompare.AsSpan());
        }
        else
        {
            // Otherwise, ask the reader to allocate a string.
            return reader.GetString() == toCompare;
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer = writer ?? throw new ArgumentNullException(nameof(writer));

        var sumValue = value.Value;

        if (sumValue is null)
        {
            return;
        }

        var sumTypeInfoCache = SumTypeCache.GetOrAdd(typeof(T), (t) => new SumConverter.SumTypeInfoCache(t));
        var discriminatorIndex = sumTypeInfoCache.GetApplicableInfoIndex(sumValue.GetType());

        // Don't bother with the discriminator persistence if it's the first type
        if (discriminatorIndex == 0)
        {
            SerializeValue(writer, options, sumValue);
        }
        else
        {
            writer.WriteStartObject();
            writer.WritePropertyName("_vs_discriminatorIndex");
            writer.WriteNumberValue(discriminatorIndex);
            writer.WritePropertyName("_vs_value");

            SerializeValue(writer, options, sumValue);

            writer.WriteEndObject();
        }

        static void SerializeValue(Utf8JsonWriter writer, JsonSerializerOptions options, object sumValue)
        {
            // behavior from DocumentUriConverter
            if (sumValue is DocumentUri documentUri)
            {
                writer.WriteStringValue(documentUri.UriString);
            }
            else if (sumValue is Uri)
            {
                writer.WriteStringValue(sumValue.ToString());
            }
            else if (sumValue != null)
            {
                JsonSerializer.Serialize(writer, sumValue, options);
            }
        }
    }

    private static bool IsTokenCompatibleWithType(JsonTokenType tokenType, Type type)
    {
        if (tokenType == JsonTokenType.Number && type.IsEnum)
        {
            System.Diagnostics.Debug.Assert(false);
        }

        return tokenType switch
        {
            JsonTokenType.True or JsonTokenType.False
                => type == typeof(bool),
            JsonTokenType.Number
                => type == typeof(int) ||
                   type == typeof(uint) ||
                   type == typeof(long) ||
                   type == typeof(ulong) ||
                   type == typeof(short) ||
                   type == typeof(ushort) ||
                   type == typeof(byte) ||
                   type == typeof(sbyte) ||
                   type == typeof(double) ||
                   type == typeof(float),
            JsonTokenType.String
                => type == typeof(string) ||
                   typeof(IStringEnum).IsAssignableFrom(type),
            _ => true
        };
    }
}

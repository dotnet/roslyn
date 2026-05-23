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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class SumConverter : JsonConverterFactory
{
    // This cache is defined on the non-generic class so it is shared across all
    // SumConverter<T> instantiations rather than duplicated per generic type argument.
    internal static readonly ConcurrentDictionary<Type, SumTypeInfoCache> SumTypeCache = new();

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

                var kindAttribute = parameterType.GetCustomAttribute<KindAttribute>();
                var unionTypeInfo = new UnionTypeInfo(parameterType, declaredConstructor, kindAttribute);
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
            if ((objectUnionTypeInfosSet?.Count ?? 0) > 1)
            {
                // If some types are tagged with a KindAttribute, make sure they are first in the list in order to avoid the wrong type being deserialized
                this.objectUnionTypeInfos = objectUnionTypeInfosSet.Where(t => t.KindAttribute is not null).Concat(
                                            objectUnionTypeInfosSet.Where(t => t.KindAttribute is null)).ToList();
            }
            else
            {
                this.objectUnionTypeInfos = objectUnionTypeInfosSet ?? EmptyUnionInfos;
            }
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

            public UnionTypeInfo(Type type, ConstructorInfo constructor, KindAttribute? kindAttribute)
            {
                this.Type = type ?? throw new ArgumentNullException(nameof(type));
                this.Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
                this.KindAttribute = kindAttribute;

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

            public KindAttribute? KindAttribute { get; }

            public object StjReaderFunction { get; }
        }
    }
}

internal sealed class SumConverter<T> : JsonConverter<T>
    where T : struct, ISumType
{

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
        var sumTypeInfoCache = SumConverter.SumTypeCache.GetOrAdd(objectType, (t) => new SumConverter.SumTypeInfoCache(t));

        var applicableUnionTypeInfos = sumTypeInfoCache.GetApplicableInfos(reader.TokenType);
        if (applicableUnionTypeInfos.Count > 0)
        {
            var backupReader = reader;
            if (applicableUnionTypeInfos[0].KindAttribute is { } kindAttribute)
            {
                using var document = JsonDocument.ParseValue(ref reader);
                reader = backupReader;
                if (document.RootElement.TryGetProperty(kindAttribute.KindPropertyName, out var value))
                {
                    var kind = value.GetString();
                    for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
                    {
                        var unionTypeInfo = applicableUnionTypeInfos[i];
                        if (unionTypeInfo.KindAttribute == null)
                        {
                            throw new JsonException(LanguageServerProtocolResources.NoSumTypeMatch);
                        }

                        if (unionTypeInfo.KindAttribute.Kind == kind)
                        {
                            var result = ((SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction).Invoke(ref reader, options);
                            return result;
                        }
                    }
                }
            }

            // Track which arms were skipped by token filtering so pass 2 only retries those.
            using var _ = ArrayBuilder<SumConverter.SumTypeInfoCache.UnionTypeInfo>.GetInstance(out var untestedArms);

            // Pass 1: Try only token-compatible arms (avoids costly exception-based probing)
            for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
            {
                var unionTypeInfo = applicableUnionTypeInfos[i];

                if (unionTypeInfo.KindAttribute != null)
                {
                    continue;
                }

                if (!IsTokenCompatibleWithType(ref reader, unionTypeInfo))
                {
                    untestedArms.Add(unionTypeInfo);
                    continue;
                }

                if (TryDeserializeArm(ref reader, backupReader, unionTypeInfo, options, out var result))
                {
                    return result;
                }
            }

            // Pass 2: If no token-compatible arm succeeded, fall back to trying untested arms.
            // This handles types whose converters accept unexpected token types (e.g.,
            // converters registered at the property level or via JsonSerializerOptions).
            for (var i = 0; i < untestedArms.Count; i++)
            {
                if (TryDeserializeArm(ref reader, backupReader, untestedArms[i], options, out var result))
                {
                    return result;
                }
            }
        }

        throw new JsonException($"No sum type match for {objectType}");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer = writer ?? throw new ArgumentNullException(nameof(writer));

        var sumValue = value.Value;

        // behavior from DocumentUriConverter
        if (sumValue is DocumentUri documentUri)
        {
            writer.WriteStringValue(documentUri.UriString);
            return;
        }
        else if (sumValue is Uri)
        {
            writer.WriteStringValue(sumValue.ToString());
            return;
        }

        if (sumValue != null)
        {
            JsonSerializer.Serialize(writer, sumValue, options);
        }
    }

    private static bool TryDeserializeArm(ref Utf8JsonReader reader, Utf8JsonReader backupReader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo, JsonSerializerOptions options, out T result)
    {
        try
        {
            result = ((SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction).Invoke(ref reader, options);
            return true;
        }
        catch
        {
            reader = backupReader;
            result = default!;
            return false;
        }
    }

    private static bool IsTokenCompatibleWithType(ref Utf8JsonReader reader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo)
    {
        return IsTokenCompatibleWithType(ref reader, unionTypeInfo.Type);
    }

    private static bool IsTokenCompatibleWithType(ref Utf8JsonReader reader, Type type)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => IsBooleanType(type),
            JsonTokenType.Number => IsNumericType(type),
            JsonTokenType.String => IsStringLikeType(type),
            JsonTokenType.StartObject => !IsSerializedAsJsonPrimitiveType(type) && !type.IsArray,
            JsonTokenType.StartArray => IsArrayElementCompatible(ref reader, type),
            _ => true,
        };
    }

    /// <summary>
    /// Returns true for types that are serialized as JSON primitives (strings, numbers, booleans)
    /// rather than JSON objects. Used to reject incompatible types for StartObject tokens.
    /// </summary>
    private static bool IsSerializedAsJsonPrimitiveType(Type type)
    {
        return IsBooleanType(type) || IsStringLikeType(type) || IsNumericType(type);
    }

    private static bool IsBooleanType(Type type)
    {
        return type == typeof(bool);
    }

    private static bool IsStringLikeType(Type type)
    {
        return type == typeof(string) ||
               type == typeof(Uri) ||
               type == typeof(DocumentUri) ||
               typeof(IStringEnum).IsAssignableFrom(type);
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(double) || type == typeof(float) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// For array SumType variants, peeks at the first array element to determine compatibility.
    /// This avoids costly exception-based type probing (e.g., trying to deserialize
    /// VSInternalCommitCharacter[] as string[] which throws InvalidOperationException per attempt).
    /// </summary>
    private static bool IsArrayElementCompatible(ref Utf8JsonReader reader, Type arrayType)
    {
        if (!arrayType.IsArray)
        {
            return false;
        }

        var elementType = arrayType.GetElementType()!;

        // Peek at first array element to disambiguate array types.
        var peekReader = reader;
        if (!peekReader.Read())
        {
            // Can't read into the array, let the normal try/catch handle it.
            return true;
        }

        // Empty array is compatible with any array type.
        if (peekReader.TokenType == JsonTokenType.EndArray)
        {
            return true;
        }

        return IsTokenCompatibleWithType(ref peekReader, elementType);
    }
}

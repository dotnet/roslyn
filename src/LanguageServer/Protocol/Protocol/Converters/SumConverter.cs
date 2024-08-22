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
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;
internal class SumConverter : JsonConverterFactory
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

    internal class SumTypeInfoCache
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
                var declaredConstructor = typeInfo.GetConstructor(new Type[] { parameterType }) ??
                    throw new ArgumentException(nameof(sumTypeType), "All constructor parameter types must be represented in the generic type arguments of the SumType");

                var kindAttribute = parameterType.GetCustomAttribute<KindAttribute>();
                var unionTypeInfo = new UnionTypeInfo(parameterType, declaredConstructor, kindAttribute);
                allUnionTypeInfosSet.Add(unionTypeInfo);

                if (parameterTypeInfo.IsPrimitive ||
                    parameterTypeInfo == typeof(string) ||
                    parameterTypeInfo == typeof(Uri) ||
                    typeof(IStringEnum).IsAssignableFrom(parameterTypeInfo))
                {
                    primitiveUnionTypeInfosSet ??= new List<UnionTypeInfo>();
                    primitiveUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else if (parameterTypeInfo.IsArray)
                {
                    arrayUnionTypeInfosSet ??= new List<UnionTypeInfo>();
                    arrayUnionTypeInfosSet.Add(unionTypeInfo);
                }
                else
                {
                    objectUnionTypeInfosSet ??= new List<UnionTypeInfo>();
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

        public class UnionTypeInfo
        {
            // System.Text.Json can pre-compile the generic SumType<> constructor call so we don't need to do it through reflection every time.
            internal delegate T StjReader<T>(ref Utf8JsonReader reader, JsonSerializerOptions options);

            private static readonly Type[] expressionLambdaMethodTypes = new[] { typeof(Type), typeof(Expression), typeof(ParameterExpression[]) };
            private static readonly MethodInfo expressionLambdaMethod = typeof(Expression)
                .GetMethods()
                .Where(mi =>
                                !mi.IsGenericMethod &&
                                mi.Name == "Lambda" &&
                                mi.GetParameters()
                                .Select(p => p.ParameterType)
                                .SequenceEqual(expressionLambdaMethodTypes))
                .Single();

            private static readonly Type[] jsonSerializerDeserializeMethodTypes = new[] { typeof(Utf8JsonReader).MakeByRefType(), typeof(JsonSerializerOptions) };
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
                var expression = (LambdaExpression)expressionLambdaMethod.Invoke(null, new object[] { typeof(StjReader<>).MakeGenericType(constructor.DeclaringType), body, new[] { param1, param2 } })!;

                StjReaderFunction = expression.Compile();
            }

            public Type Type { get; }

            public ConstructorInfo Constructor { get; }

            public KindAttribute? KindAttribute { get; }

            public object StjReaderFunction { get; }
        }
    }
}

internal class SumConverter<T> : JsonConverter<T>
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

            for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
            {
                var unionTypeInfo = applicableUnionTypeInfos[i];

                if (!IsTokenCompatibleWithType(ref reader, unionTypeInfo))
                {
                    continue;
                }

                if (unionTypeInfo.KindAttribute != null)
                {
                    continue;
                }

                try
                {
                    var result = ((SumConverter.SumTypeInfoCache.UnionTypeInfo.StjReader<T>)unionTypeInfo.StjReaderFunction).Invoke(ref reader, options);
                    return result;
                }
                catch
                {
                    reader = backupReader;
                    continue;
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
        if (sumValue is Uri)
        {
            writer.WriteStringValue(sumValue.ToString());
            return;
        }

        if (sumValue != null)
        {
            JsonSerializer.Serialize(writer, sumValue, options);
        }
    }

    private static bool IsTokenCompatibleWithType(ref Utf8JsonReader reader, SumConverter.SumTypeInfoCache.UnionTypeInfo unionTypeInfo)
    {
        var isCompatible = true;
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
            case JsonTokenType.False:
                isCompatible = unionTypeInfo.Type == typeof(bool);
                break;
            case JsonTokenType.Number:
                isCompatible = unionTypeInfo.Type == typeof(int) ||
                               unionTypeInfo.Type == typeof(uint) ||
                               unionTypeInfo.Type == typeof(long) ||
                               unionTypeInfo.Type == typeof(ulong) ||
                               unionTypeInfo.Type == typeof(short) ||
                               unionTypeInfo.Type == typeof(ushort) ||
                               unionTypeInfo.Type == typeof(byte) ||
                               unionTypeInfo.Type == typeof(sbyte) ||
                               unionTypeInfo.Type == typeof(double) ||
                               unionTypeInfo.Type == typeof(float);
                break;
            case JsonTokenType.String:
                isCompatible = unionTypeInfo.Type == typeof(string) ||
                               unionTypeInfo.Type == typeof(Uri) ||
                               typeof(IStringEnum).IsAssignableFrom(unionTypeInfo.Type);
                break;
        }

        return isCompatible;
    }
}

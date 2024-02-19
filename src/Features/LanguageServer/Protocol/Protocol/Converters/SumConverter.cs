// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis.LanguageServer;
    using Microsoft.CommonLanguageServerProtocol.Framework;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Converter to translate to and from SumTypes.
    /// </summary>
    internal class SumConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, SumTypeInfoCache> SumTypeCache = new ConcurrentDictionary<Type, SumTypeInfoCache>();

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(ISumType).IsAssignableFrom(objectType);
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            reader = reader ?? throw new ArgumentNullException(nameof(reader));
            serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            // Even if CanConvert only returns true for ISumType, ReadJson is invoked for Nullable<SumType<...>> as well
            if (reader.TokenType == JsonToken.Null)
            {
                if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return null;
                }
                else
                {
                    // We shouldn't really ever have a non-Nullable SumType being received as null but, if we do, return an "empty" SumType
                    return Activator.CreateInstance(objectType);
                }
            }

            // objectType will be one of the various SumType variants. In order for this converter to work with all SumTypes it has to use reflection.
            // This method works by attempting to deserialize the json into each of the type parameters to a SumType and stops at the first success.
            var sumTypeInfoCache = SumTypeCache.GetOrAdd(objectType, (t) => new SumTypeInfoCache(t));

            JToken? token = null;
            var applicableUnionTypeInfos = sumTypeInfoCache.GetApplicableInfos(reader.TokenType);

            for (var i = 0; i < applicableUnionTypeInfos.Count; i++)
            {
                var unionTypeInfo = applicableUnionTypeInfos[i];

                if (!IsTokenCompatibleWithType(reader, unionTypeInfo))
                {
                    continue;
                }

                try
                {
                    object? sumValue;
                    if (token == null && i + 1 == applicableUnionTypeInfos.Count)
                    {
                        // We're at the very last entry, we don't need to maintain the JsonReader, can read directly from the JsonReader to avoid the inbetween JObject type.
                        sumValue = serializer.Deserialize(reader, unionTypeInfo.Type);
                    }
                    else
                    {
                        if (token == null)
                        {
                            token = JToken.ReadFrom(reader);
                        }

                        if (unionTypeInfo.KindAttribute is not null &&
                            (token is not JObject jObject || jObject[unionTypeInfo.KindAttribute.KindPropertyName]?.ToString() != unionTypeInfo.KindAttribute.Kind))
                        {
                            continue;
                        }

                        sumValue = token.ToObject(unionTypeInfo.Type, serializer);
                    }

                    object?[] args = { sumValue };
                    var sum = unionTypeInfo.Constructor.Invoke(args);
                    return sum;
                }
                catch
                {
                    continue;
                }
            }

            throw new JsonSerializationException(LanguageServerProtocolResources.NoSumTypeMatch);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer = writer ?? throw new ArgumentNullException(nameof(writer));

            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer = writer ?? throw new ArgumentNullException(nameof(writer));

                var sumValue = ((ISumType)value!).Value;

                if (sumValue != null)
                {
                    var token = JToken.FromObject(sumValue);
                    token.WriteTo(writer);
                }
            }
        }

        private static bool IsTokenCompatibleWithType(JsonReader reader, SumTypeInfoCache.UnionTypeInfo unionTypeInfo)
        {
            var isCompatible = true;
            switch (reader.TokenType)
            {
                case JsonToken.Float:
                    isCompatible = unionTypeInfo.Type == typeof(double) ||
                                   unionTypeInfo.Type == typeof(float);
                    break;
                case JsonToken.Boolean:
                    isCompatible = unionTypeInfo.Type == typeof(bool);
                    break;
                case JsonToken.Integer:
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
                case JsonToken.String:
                    isCompatible = unionTypeInfo.Type == typeof(string) ||
                                   typeof(IStringEnum).IsAssignableFrom(unionTypeInfo.Type);
                    break;
            }

            return isCompatible;
        }

        private class SumTypeInfoCache
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

            public IReadOnlyList<UnionTypeInfo> GetApplicableInfos(JsonToken startingTokenType)
            {
                return startingTokenType switch
                {
                    JsonToken.StartArray
                        => this.arrayUnionTypeInfos,
                    JsonToken.Integer or
                    JsonToken.Float or
                    JsonToken.Bytes or
                    JsonToken.String or
                    JsonToken.Boolean
                        => this.primitiveUnionTypeInfos,
                    JsonToken.StartObject
                        => this.objectUnionTypeInfos,
                    _ => this.allUnionTypeInfos,
                };
            }

            private static Type NormalizeToNonNullable(Type sumTypeType)
            {
                return Nullable.GetUnderlyingType(sumTypeType) ?? sumTypeType;
            }

            internal class UnionTypeInfo
            {
                public UnionTypeInfo(Type type, ConstructorInfo constructor, KindAttribute? kindAttribute)
                {
                    this.Type = type ?? throw new ArgumentNullException(nameof(type));
                    this.Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
                    this.KindAttribute = kindAttribute;
                }

                public Type Type { get; }

                public ConstructorInfo Constructor { get; }

                public KindAttribute? KindAttribute { get; }
            }
        }
    }
}

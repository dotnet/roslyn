// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class UnionConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeof(IUnion).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(UnionConverter<>).MakeGenericType(typeToConvert))!;
}

internal sealed class UnionConverter<T> : JsonConverter<T> where T : class, IUnion
{
    private delegate T ReadArm(ref Utf8JsonReader reader, JsonSerializerOptions options);

    private sealed record Arm(Type Type, ReadArm Read, string? DiscriminatorName, string? DiscriminatorValue);

    private static readonly Arm[] s_arms = CreateArms();

    public override bool HandleNull => typeof(T) == typeof(ProgressToken);

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var savedReader = reader;
        if (reader.TokenType == JsonTokenType.StartObject &&
            s_arms.FirstOrDefault(arm => arm.DiscriminatorName is not null) is { } taggedArm)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            reader = savedReader;
            if (document.RootElement.TryGetProperty(taggedArm.DiscriminatorName!, out var kind))
            {
                var discriminator = kind.GetString();
                foreach (var arm in s_arms)
                {
                    if (arm.DiscriminatorName is not null && arm.DiscriminatorValue == discriminator)
                        return arm.Read(ref reader, options);
                }

                throw new JsonException($"Unknown discriminator for {typeof(T)}.");
            }
        }

        // Defer token-incompatible arms so options-level converters can still accept them.
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var arm in s_arms)
            {
                if (arm.DiscriminatorName is not null || !IsInTokenBucket(reader.TokenType, arm.Type) ||
                    IsCompatible(ref reader, arm.Type) != (pass == 0))
                {
                    continue;
                }

                try
                {
                    return arm.Read(ref reader, options);
                }
                catch (JsonException)
                {
                    reader = savedReader;
                }
            }
        }

        throw new JsonException($"No union arm matches {typeof(T)}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value?.Value is not { } payload)
            throw new JsonException($"A {typeof(T).Name} must contain a value.");

        if (payload is DocumentUri uri)
            writer.WriteStringValue(uri.UriString);
        else if (payload is Uri systemUri)
            writer.WriteStringValue(systemUri.ToString());
        else
            JsonSerializer.Serialize(writer, payload, payload.GetType(), options);
    }

    private static Arm[] CreateArms()
    {
        var arms = new List<Arm>();
        var readMethod = typeof(UnionConverter<T>).GetMethod(nameof(CreateReader), BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var constructor in typeof(T).GetConstructors())
        {
            if (constructor.GetParameters() is not [{ ParameterType: var armType }])
                continue;

            var read = (ReadArm)readMethod.MakeGenericMethod(armType).Invoke(null, [constructor])!;
            string? discriminatorName = null;
            string? discriminatorValue = null;
            if (armType.IsClass && !armType.IsAbstract && armType.GetConstructor(Type.EmptyTypes) is not null)
            {
                var discriminator = armType.GetProperties().FirstOrDefault(property =>
                    property.PropertyType == typeof(string) && property.CanRead && !property.CanWrite &&
                    property.GetIndexParameters().Length == 0 &&
                    property.GetCustomAttribute<JsonPropertyNameAttribute>() is not null);
                if (discriminator?.GetValue(Activator.CreateInstance(armType)) is string kind)
                {
                    discriminatorName = discriminator.GetCustomAttribute<JsonPropertyNameAttribute>()!.Name;
                    discriminatorValue = kind;
                }
            }

            arms.Add(new(armType, read, discriminatorName, discriminatorValue));
        }

        return [.. arms];
    }

    private static ReadArm CreateReader<TArm>(ConstructorInfo constructor)
    {
        var parameter = Expression.Parameter(typeof(TArm));
        var create = Expression.Lambda<Func<TArm, T>>(Expression.New(constructor, parameter), parameter).Compile();
        return (ref Utf8JsonReader reader, JsonSerializerOptions options) =>
            create(JsonSerializer.Deserialize<TArm>(ref reader, options)!);
    }

    private static bool IsCompatible(ref Utf8JsonReader reader, Type type)
    {
        if (typeof(IUnion).IsAssignableFrom(type))
        {
            foreach (var constructor in type.GetConstructors())
            {
                if (constructor.GetParameters() is [{ ParameterType: var armType }] &&
                    IsCompatible(ref reader, armType))
                {
                    return true;
                }
            }

            return false;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        var stringLike = type == typeof(string) || type == typeof(DocumentUri) ||
            type == typeof(Uri) || typeof(IStringEnum).IsAssignableFrom(type);
        var numeric = type.IsEnum || type == typeof(byte) || type == typeof(sbyte) ||
            type == typeof(short) || type == typeof(ushort) || type == typeof(int) ||
            type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal);

        return reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => type == typeof(bool),
            JsonTokenType.Number => numeric,
            JsonTokenType.String => stringLike,
            JsonTokenType.StartObject => !stringLike && !numeric && type != typeof(bool) && !type.IsArray,
            JsonTokenType.StartArray => IsArrayCompatible(ref reader, type),
            _ => true,
        };
    }

    private static bool IsArrayCompatible(ref Utf8JsonReader reader, Type type)
    {
        if (!type.IsArray)
            return false;

        var elementReader = reader;
        return !elementReader.Read() || elementReader.TokenType == JsonTokenType.EndArray ||
            IsCompatible(ref elementReader, type.GetElementType()!);
    }

    private static bool IsInTokenBucket(JsonTokenType token, Type type)
    {
        if (typeof(IUnion).IsAssignableFrom(type))
        {
            return type.GetConstructors().Any(constructor =>
                constructor.GetParameters() is [{ ParameterType: var armType }] &&
                IsInTokenBucket(token, armType));
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        var scalar = type.IsPrimitive || type.IsEnum || type == typeof(decimal) ||
            type == typeof(string) || type == typeof(DocumentUri) || type == typeof(Uri) ||
            typeof(IStringEnum).IsAssignableFrom(type);
        return token switch
        {
            JsonTokenType.StartArray => type.IsArray,
            JsonTokenType.StartObject => !type.IsArray && !scalar,
            _ => scalar,
        };
    }
}

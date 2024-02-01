// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Newtonsoft.Json;

/// <summary>
/// JsonConverter for serializing and deserializing string-based enums.
/// </summary>
/// <typeparam name="TStringEnumType">The actual type implementing <see cref="IStringEnum"/>.</typeparam>
internal class StringEnumConverter<TStringEnumType>
    : JsonConverter
    where TStringEnumType : IStringEnum
{
    private static readonly Func<string, TStringEnumType> CreateEnum;

    static StringEnumConverter()
    {
        // TODO. When C# starts supporting static methods in interfaces, add a static Create method to IStringEnum and remove CreateEnum.
        var constructor = typeof(TStringEnumType).GetConstructor([typeof(string)]);
        if (constructor is null)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.StringEnumMissingConstructor, typeof(TStringEnumType).FullName));
        }

        var param = Expression.Parameter(typeof(string), "value");
        var body = Expression.New(constructor, param);
        CreateEnum = Expression.Lambda<Func<string, TStringEnumType>>(body, param).Compile();
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(TStringEnumType);

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        reader = reader ?? throw new ArgumentNullException(nameof(reader));

        if (reader.TokenType == JsonToken.String)
        {
            return CreateEnum((string)reader.Value!);
        }
        else if (reader.TokenType == JsonToken.Null)
        {
            return default(TStringEnumType);
        }

        throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.StringEnumSerializationError, reader.Value));
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        writer = writer ?? throw new ArgumentNullException(nameof(writer));

        if (value is TStringEnumType kind)
        {
            writer.WriteValue(kind.Value);
        }
        else
        {
            throw new ArgumentException($"{nameof(value)} must be of type {typeof(TStringEnumType).FullName}");
        }
    }

    /// <summary>
    /// Type converter from <see langword="string"/> to <typeparamref name="TStringEnumType"/>.
    /// This is required to support <see cref="DefaultValueAttribute(Type, string)"/>.
    /// </summary>
    internal class TypeConverter
        : System.ComponentModel.TypeConverter
    {
        /// <inheritdoc/>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string stringValue)
            {
                return CreateEnum(stringValue);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}

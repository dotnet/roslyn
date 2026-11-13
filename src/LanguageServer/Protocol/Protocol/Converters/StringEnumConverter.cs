// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;
/// <summary>
/// JsonConverter for serializing and deserializing string-based enums.
/// </summary>
/// <typeparam name="TStringEnumType">The actual type implementing <see cref="IStringEnum"/>.</typeparam>
internal sealed class StringEnumConverter<TStringEnumType>
    : JsonConverter<TStringEnumType>
    where TStringEnumType : struct, IStringEnum<TStringEnumType>
{
    public override TStringEnumType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return default(TStringEnumType).Create(reader.GetString()!);
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return default;
        }

        throw new JsonException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.StringEnumSerializationError, reader.GetString()));
    }

    public override void Write(Utf8JsonWriter writer, TStringEnumType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }

    /// <summary>
    /// Type converter from <see langword="string"/> to <typeparamref name="TStringEnumType"/>.
    /// This is required to support <see cref="DefaultValueAttribute(Type, string)"/>.
    /// </summary>
    public sealed class TypeConverter
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
                return default(TStringEnumType).Create(stringValue);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}

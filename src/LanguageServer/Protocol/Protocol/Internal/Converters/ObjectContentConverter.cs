// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Roslyn.Core.Imaging;
using Roslyn.Text.Adornments;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Object Content converter used to serialize and deserialize Text and Adornements from VS.
///
/// This converts the following types:
/// <list type="bullet">
/// <item><description><see cref="ImageId"/></description></item>,
/// <item><description><see cref="ImageElement"/></description></item>,
/// <item><description><see cref="ContainerElement"/></description></item>,
/// <item><description><see cref="ClassifiedTextElement"/></description></item>,
/// <item><description><see cref="ClassifiedTextRun"/></description></item>.
/// </list>
/// Every other type is serialized as a string using the <see cref="object.ToString()"/> method.
/// </summary>
internal sealed class ObjectContentConverter : JsonConverter<object>
{
    /// <summary>
    /// The property name used to save the .NET Type name of the serialized object.
    /// </summary>
    public const string TypeProperty = "_vs_type";

    /// <summary>
    /// A reusable instance of the <see cref="ObjectContentConverter"/>.
    /// </summary>
    public static readonly ObjectContentConverter Instance = new();

    public override object? Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            reader.Read();
            return null;
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            var clonedReader = reader;
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            var data = jsonDocument.RootElement;
            var type = data.GetProperty(TypeProperty).GetString() ?? throw new JsonException();

            switch (type)
            {
                case nameof(ImageId):
                    return ImageIdConverter.Instance.Read(ref clonedReader, typeof(ImageId), options);
                case nameof(ImageElement):
                    return ImageElementConverter.Instance.Read(ref clonedReader, typeof(ImageElement), options);
                case nameof(ContainerElement):
                    return ContainerElementConverter.Instance.Read(ref clonedReader, typeof(ContainerElementConverter), options);
                case nameof(ClassifiedTextElement):
                    return ClassifiedTextElementConverter.Instance.Read(ref clonedReader, typeof(ClassifiedTextElementConverter), options);
                case nameof(ClassifiedTextRun):
                    return ClassifiedTextRunConverter.Instance.Read(ref clonedReader, typeof(ClassifiedTextRunConverter), options);
                default:
                    return data;
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        else
        {
            return JsonSerializer.Deserialize(ref reader, objectType, options);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        switch (value)
        {
            case ImageId:
                ImageIdConverter.Instance.Write(writer, (ImageId)value, options);
                break;
            case ImageElement:
                ImageElementConverter.Instance.Write(writer, (ImageElement)value, options);
                break;
            case ContainerElement:
                ContainerElementConverter.Instance.Write(writer, (ContainerElement)value, options);
                break;
            case ClassifiedTextElement:
                ClassifiedTextElementConverter.Instance.Write(writer, (ClassifiedTextElement)value, options);
                break;
            case ClassifiedTextRun:
                ClassifiedTextRunConverter.Instance.Write(writer, (ClassifiedTextRun)value, options);
                break;
            default:
                // According to the docs of ContainerElement point to https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.text.adornments.iviewelementfactoryservice
                // which states that Editor supports ClassifiedTextElement, ContainerElement, ImageElement and that other objects would be presented using ToString unless an extender
                // exports a IViewElementFactory for that type. So I will simply serialize unknown objects as strings.
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}

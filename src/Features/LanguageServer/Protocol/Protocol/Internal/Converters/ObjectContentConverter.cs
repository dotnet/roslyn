// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using Roslyn.Core.Imaging;
    using Roslyn.Text.Adornments;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

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
    internal class ObjectContentConverter : JsonConverter
    {
        /// <summary>
        /// The property name used to save the .NET Type name of the serialized object.
        /// </summary>
        public const string TypeProperty = "_vs_type";

        /// <summary>
        /// A reusable instance of the <see cref="ObjectContentConverter"/>.
        /// </summary>
        public static readonly ObjectContentConverter Instance = new ObjectContentConverter();

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(object);
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                reader.Read();
                return null;
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var data = JObject.Load(reader);
                var type = data[TypeProperty]?.ToString() ?? throw new JsonSerializationException();

                var tokenReader = data.CreateReader();
                tokenReader.Read();
                switch (type)
                {
                    case nameof(ImageId):
                        return ImageIdConverter.Instance.ReadJson(tokenReader, typeof(ImageId), null, serializer);
                    case nameof(ImageElement):
                        return ImageElementConverter.Instance.ReadJson(tokenReader, typeof(ImageElement), null, serializer);
                    case nameof(ContainerElement):
                        return ContainerElementConverter.Instance.ReadJson(tokenReader, typeof(ContainerElementConverter), null, serializer);
                    case nameof(ClassifiedTextElement):
                        return ClassifiedTextElementConverter.Instance.ReadJson(tokenReader, typeof(ClassifiedTextElementConverter), null, serializer);
                    case nameof(ClassifiedTextRun):
                        return ClassifiedTextRunConverter.Instance.ReadJson(tokenReader, typeof(ClassifiedTextRunConverter), null, serializer);
                    default:
                        return data;
                }
            }
            else
            {
                return serializer.Deserialize(reader);
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            switch (value)
            {
                case ImageId:
                    ImageIdConverter.Instance.WriteJson(writer, value, serializer);
                    break;
                case ImageElement:
                    ImageElementConverter.Instance.WriteJson(writer, value, serializer);
                    break;
                case ContainerElement:
                    ContainerElementConverter.Instance.WriteJson(writer, value, serializer);
                    break;
                case ClassifiedTextElement:
                    ClassifiedTextElementConverter.Instance.WriteJson(writer, value, serializer);
                    break;
                case ClassifiedTextRun:
                    ClassifiedTextRunConverter.Instance.WriteJson(writer, value, serializer);
                    break;
                default:
                    // According to the docs of ContainerElement point to https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.text.adornments.iviewelementfactoryservice
                    // which states that Editor supports ClassifiedTextElement, ContainerElement, ImageElement and that other objects would be presented using ToString unless an extender
                    // exports a IViewElementFactory for that type. So I will simply serialize unknown objects as strings.
                    writer.WriteValue(value.ToString());
                    break;
            }
        }
    }
}
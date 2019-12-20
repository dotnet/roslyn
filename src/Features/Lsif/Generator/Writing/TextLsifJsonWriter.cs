// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.CodeAnalysis.Lsif.Generator.Writing
{
    internal sealed class TextLsifJsonWriter : ILsifJsonWriter, IDisposable
    {
        private readonly JsonTextWriter _jsonTextWriter;
        private readonly JsonSerializer _jsonSerializer;
        private readonly LsifFormat _format;

        public TextLsifJsonWriter(TextWriter outputWriter, LsifFormat format)
        {
            _format = format;
            _jsonTextWriter = new JsonTextWriter(outputWriter);

            var settings = new JsonSerializerSettings
            {
                Formatting = _format == LsifFormat.Json ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                Converters = new[] { new LsifConverter() }
            };

            _jsonSerializer = JsonSerializer.Create(settings);

            if (_format == LsifFormat.Json)
            {
                _jsonTextWriter.WriteStartArray();
            }
        }

        public void Write(Element element)
        {
            _jsonSerializer.Serialize(_jsonTextWriter, element);

            if (_format == LsifFormat.Line)
            {
                _jsonTextWriter.WriteWhitespace("\r\n");
            }
        }

        public void Dispose()
        {
            if (_format == LsifFormat.Json)
            {
                _jsonTextWriter.WriteEndArray();
            }

            _jsonTextWriter.Close();
        }

        internal class LsifConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(ISerializableId).IsAssignableFrom(objectType) ||
                       objectType == typeof(Uri);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                switch (value)
                {
                    case ISerializableId id:

                        writer.WriteValue(id.NumericId);
                        break;

                    case Uri uri:

                        writer.WriteValue(uri.AbsoluteUri);
                        break;

                    default:

                        throw new NotSupportedException();
                }
            }
        }
    }
}

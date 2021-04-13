// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    /// <summary>
    /// An <see cref="ILsifJsonWriter"/> that writes in <see cref="LsifFormat.Json"/>.
    /// </summary>
    internal sealed class JsonModeLsifJsonWriter : ILsifJsonWriter, IDisposable
    {
        private readonly JsonTextWriter _jsonTextWriter;
        private readonly JsonSerializer _jsonSerializer;
        private readonly object _writeGate = new object();

        public JsonModeLsifJsonWriter(TextWriter outputWriter)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                Converters = new[] { new LsifConverter() }
            };

            _jsonSerializer = JsonSerializer.Create(settings);

            _jsonTextWriter = new JsonTextWriter(outputWriter);
            _jsonTextWriter.WriteStartArray();
        }

        public void Write(Element element)
        {
            lock (_writeGate)
            {
                _jsonSerializer.Serialize(_jsonTextWriter, element);
            }
        }

        public void WriteAll(List<Element> elements)
        {
            lock (_writeGate)
            {
                foreach (var element in elements)
                    _jsonSerializer.Serialize(_jsonTextWriter, element);
            }
        }

        public void Dispose()
        {
            _jsonTextWriter.WriteWhitespace(Environment.NewLine);
            _jsonTextWriter.WriteEndArray();
            _jsonTextWriter.Close();
        }
    }
}

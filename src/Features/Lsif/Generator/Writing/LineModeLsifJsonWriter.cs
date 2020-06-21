// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    /// <summary>
    /// An <see cref="ILsifJsonWriter"/> that writes in <see cref="LsifFormat.Line"/>.
    /// </summary>
    internal sealed partial class LineModeLsifJsonWriter : ILsifJsonWriter
    {
        private readonly object _writeGate = new object();
        private readonly TextWriter _outputWriter;
        private readonly JsonSerializerSettings _settings;

        public LineModeLsifJsonWriter(TextWriter outputWriter)
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = Newtonsoft.Json.Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.None,
                Converters = new[] { new LsifConverter() }
            };
            _outputWriter = outputWriter;
        }

        public void Write(Element element)
        {
            var line = JsonConvert.SerializeObject(element, _settings);

            lock (_writeGate)
            {
                _outputWriter.WriteLine(line);
            }
        }
    }
}

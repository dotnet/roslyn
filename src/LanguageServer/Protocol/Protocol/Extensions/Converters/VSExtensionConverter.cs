// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Converter used to serialize and deserialize classes extending types defined in the
    /// Microsoft.VisualStudio.LanguageServer.Protocol package.
    /// </summary>
    /// <typeparam name="TBase">Base class that is specified in the
    /// Microsoft.VisualStudio.LanguageServer.Protocol package.</typeparam>
    /// <typeparam name="TExtension">Extension class that extends TBase.</typeparam>
    internal class VSExtensionConverter<TBase, TExtension> : JsonConverter<TBase>
    where TExtension : TBase
    {
        private JsonSerializerOptions? _trimmedOptions;

        public override TBase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<TExtension>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
        {
            // System.Text.Json doesn't serialize properties from derived classes by default, and there's no 'readonly' converters
            // like Newtonsoft has.
            if (value is TExtension extension)
            {
                JsonSerializer.Serialize(writer, extension, options);
            }
            else
            {
                // There's no ability to fallback to a 'default' serialization, so we clone our options
                // and exclude this converter from it to prevent a stack overflow.
                JsonSerializer.Serialize(writer, (object)value!, DropConverter(options));
            }
        }

        private JsonSerializerOptions DropConverter(JsonSerializerOptions options)
        {
            if (_trimmedOptions != null)
            {
                return _trimmedOptions;
            }

            lock (this)
            {
                options = new System.Text.Json.JsonSerializerOptions(options);
                options.Converters.Remove(this);
                _trimmedOptions = options;
                return options;
            }
        }
    }
}

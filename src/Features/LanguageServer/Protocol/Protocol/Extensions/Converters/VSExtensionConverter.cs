// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Converter used to serialize and deserialize classes extending types defined in the
    /// Microsoft.VisualStudio.LanguageServer.Protocol package.
    /// </summary>
    /// <typeparam name="TBase">Base class that is specified in the
    /// Microsoft.VisualStudio.LanguageServer.Protocol package.</typeparam>
    /// <typeparam name="TExtension">Extension class that extends TBase.</typeparam>
    internal class VSExtensionConverter<TBase, TExtension> : JsonConverter
        where TExtension : TBase
    {
        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TBase);
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<TExtension>(reader);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}

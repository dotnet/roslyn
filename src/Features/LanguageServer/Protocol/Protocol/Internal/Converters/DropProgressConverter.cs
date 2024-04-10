// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Converter used to deserialize objects dropping any <see cref="IProgress{T}"/> property.
    /// </summary>
    internal class DropProgressConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <summary>
        /// Static method to get a <see cref="JsonSerializer"/> containing a <see cref="DropProgressConverter"/>.
        /// </summary>
        /// <returns><see cref="JsonSerializer"/> object containing a <see cref="DropProgressConverter"/>.</returns>
        public static JsonSerializer CreateSerializer()
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DropProgressConverter());
            return serializer;
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            var isIProgressOfT = objectType.IsConstructedGenericType && objectType.GetGenericTypeDefinition().Equals(typeof(IProgress<>));
            var implementsIProgressOfT = objectType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IProgress<>)));

            return isIProgressOfT || implementsIProgressOfT;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // We deserialize all IProgress<T> objects as null.
            return null;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteNull();
        }
    }
}

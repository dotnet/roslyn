// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    internal sealed class LsifConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ISerializableId).IsAssignableFrom(objectType) ||
                   objectType == typeof(Uri);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            switch (value)
            {
                case ISerializableId id:
                    writer.WriteValue(id.NumericId);
                    break;

                case Uri uri:
                    writer.WriteValue(uri.ToString());
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing
{
    internal sealed class LsifConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (typeof(ISerializableId).IsAssignableFrom(objectType) ||
                objectType == typeof(Uri))
            {
                return true;
            }

            if (objectType.IsConstructedGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return CanConvert(objectType.GenericTypeArguments.Single());
            }

            return false;
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

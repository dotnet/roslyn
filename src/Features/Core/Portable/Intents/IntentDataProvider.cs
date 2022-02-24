// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Microsoft.CodeAnalysis.Features.Intents
{
    internal class IntentDataProvider
    {
        private readonly JsonSerializerOptions _serializerOptions;

        private readonly string? _serializedIntentData;

        public IntentDataProvider(string? serializedIntentData, JsonSerializerOptions serializerOptions)
        {
            _serializedIntentData = serializedIntentData;
            _serializerOptions = serializerOptions;
        }

        public T? GetIntentData<T>() where T : class
        {
            if (_serializedIntentData != null)
            {
                return JsonSerializer.Deserialize<T>(_serializedIntentData, _serializerOptions);
            }

            return null;
        }
    }
}

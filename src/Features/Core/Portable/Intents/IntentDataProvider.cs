// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Features.Intents
{
    internal sealed class IntentDataProvider
    {
        private static readonly Lazy<JsonSerializerOptions> s_serializerOptions = new Lazy<JsonSerializerOptions>(() =>
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter());
            return serializerOptions;
        });

        public readonly CodeAndImportGenerationOptionsProvider FallbackOptions;

        private readonly string? _serializedIntentData;

        public IntentDataProvider(
            string? serializedIntentData,
            CodeAndImportGenerationOptionsProvider fallbackOptions)
        {
            _serializedIntentData = serializedIntentData;
            FallbackOptions = fallbackOptions;
        }

        public T? GetIntentData<T>() where T : class
        {
            if (_serializedIntentData != null)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(_serializedIntentData, s_serializerOptions.Value);
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
                {
                }
            }

            return null;
        }
    }
}

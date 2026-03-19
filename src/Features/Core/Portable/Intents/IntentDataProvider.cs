// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Features.Intents;

internal sealed class IntentDataProvider(
    string? serializedIntentData)
{
    private static readonly Lazy<JsonSerializerOptions> s_serializerOptions = new(() =>
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        return serializerOptions;
    });

    private readonly string? _serializedIntentData = serializedIntentData;

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

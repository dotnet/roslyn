// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Specifies that an option should be read from an .editorconfig file.
/// </summary>
internal sealed class EditorConfigValueSerializer<T>(
    Func<string, Optional<T>> parseValue,
    Func<T, string> serializeValue) : IEditorConfigValueSerializer
{
    public static readonly EditorConfigValueSerializer<T> Unsupported = new(
        parseValue: _ => throw new NotSupportedException("Option does not support serialization to editorconfig format"),
        serializeValue: _ => throw new NotSupportedException("Option does not support serialization to editorconfig format"));

    private readonly ConcurrentDictionary<string, Optional<T>> _cachedValues = [];

    bool IEditorConfigValueSerializer.TryParse(string value, out object? result)
    {
        if (TryParseValue(value, out var typedResult))
        {
            result = typedResult;
            return true;
        }

        result = null;
        return false;
    }

    internal bool TryParseValue(string value, [MaybeNullWhen(false)] out T result)
    {
        var optionalValue = _cachedValues.GetOrAdd(value, parseValue);
        if (optionalValue.HasValue)
        {
            result = optionalValue.Value;
            return true;
        }
        else
        {
            result = default!;
            return false;
        }
    }

    public string GetEditorConfigStringValue(T value)
    {
        var editorConfigStringForValue = serializeValue(value);
        Contract.ThrowIfTrue(RoslynString.IsNullOrEmpty(editorConfigStringForValue));
        return editorConfigStringForValue;
    }

    public string Serialize(T value)
        => serializeValue(value);

    string IEditorConfigValueSerializer.Serialize(object? value)
        => GetEditorConfigStringValue((T)value!);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor;

public readonly struct Optional<T>(T value)
{
    public bool HasValue { get; } = true;

    public T Value { get; } = value;

    public T GetValueOrDefault(T defaultValue)
        => HasValue ? Value : defaultValue;

    public static implicit operator Optional<T>(T value)
        => new(value);

    public override string ToString()
        => HasValue ? Value?.ToString() ?? "null" : "unspecified";
}

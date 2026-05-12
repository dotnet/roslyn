// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodParameter(string name, string type, ImmutableArray<string> modifiers = default)
{
    public string Name { get; } = name;
    public string Type { get; } = type;

    public ImmutableArray<string> Modifiers { get; } = modifiers.NullToEmpty();
}

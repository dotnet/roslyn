// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MethodImplementation;

internal sealed record MethodImplementationParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public ImmutableArray<string> Modifiers { get; }

    public MethodImplementationParameterInfo(string name, string type, ImmutableArray<string> modifiers)
    {
        Name = name;
        Type = type;
        Modifiers = modifiers;
    }
}

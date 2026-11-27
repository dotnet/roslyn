// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class RestrictedInternalsVisibleToAttribute(string assemblyName, params string[] allowedNamespaces) : Attribute
{
    public string AssemblyName { get; } = assemblyName;
    public ImmutableArray<string> AllowedNamespaces { get; } = allowedNamespaces.ToImmutableArray();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;

namespace RunTests;

public readonly record struct AssemblyInfo(string AssemblyPath) : IComparable<AssemblyInfo>
{
    public string AssemblyName => Path.GetFileName(AssemblyPath);

    public int CompareTo(AssemblyInfo other)
    {
        // Ensure we have a consistent ordering by ordering by assembly path.
        return string.Compare(this.AssemblyPath, other.AssemblyPath, StringComparison.Ordinal);
    }
}

public readonly record struct TypeInfo(string Name, string FullyQualifiedName, ImmutableArray<TestMethodInfo> Tests)
{
    public override string ToString() => $"[Type]{FullyQualifiedName}";
}

public readonly record struct TestMethodInfo(string Name, string FullyQualifiedName, TimeSpan ExecutionTime)
{
    public override string ToString() => $"[Method]{FullyQualifiedName}";
}

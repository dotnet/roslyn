// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace PrepareTests;

public readonly record struct AssemblyInfo(string AssemblyPath) : IComparable
{
    public string AssemblyName => Path.GetFileName(AssemblyPath);

    public int CompareTo(object? obj)
    {
        if (obj == null)
        {
            return 1;
        }

        var otherAssembly = (AssemblyInfo)obj;

        // Ensure we have a consistent ordering by ordering by assembly path.
        return this.AssemblyPath.CompareTo(otherAssembly.AssemblyPath);
    }
}

public readonly record struct TypeInfo(string Name, string FullyQualifiedName, int TestCount);

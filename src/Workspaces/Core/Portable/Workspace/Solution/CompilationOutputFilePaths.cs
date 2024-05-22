// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Paths of files produced by the compilation.
/// </summary>
public readonly struct CompilationOutputInfo : IEquatable<CompilationOutputInfo>
{
    /// <summary>
    /// Full path to the assembly or module produced by the compilation, or <see langword="null"/> if unknown.
    /// </summary>
    public readonly string? AssemblyPath { get; }

    // TODO: https://github.com/dotnet/roslyn/issues/35065
    // The project system doesn't currently provide paths to the PDB or XML files that the compiler produces.
    // public readonly string? PdbPath { get; }
    // public readonly string? DocumentationCommentsPath { get; }

    internal CompilationOutputInfo(string? assemblyPath)
    {
        AssemblyPath = assemblyPath;
    }

#pragma warning disable CA1822 // Mark members as static - unshipped public API which will use instance members in future https://github.com/dotnet/roslyn/issues/35065
    public CompilationOutputInfo WithAssemblyPath(string? path)
#pragma warning restore CA1822 // Mark members as static
        => new(assemblyPath: path);

    public override bool Equals(object? obj)
        => obj is CompilationOutputInfo info && Equals(info);

    public bool Equals(CompilationOutputInfo other)
        => AssemblyPath == other.AssemblyPath;

    public override int GetHashCode()
        => AssemblyPath?.GetHashCode() ?? 0;

    public static bool operator ==(in CompilationOutputInfo left, in CompilationOutputInfo right)
        => left.Equals(right);

    public static bool operator !=(in CompilationOutputInfo left, in CompilationOutputInfo right)
        => !left.Equals(right);

    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(AssemblyPath);
    }

    internal static CompilationOutputInfo ReadFrom(ObjectReader reader)
    {
        var assemblyPath = reader.ReadString();
        return new CompilationOutputInfo(assemblyPath);
    }
}

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

    /// <summary>
    /// Absolute path to the root directory of source generated files, or null if it is not known.
    /// </summary>
    public readonly string? GeneratedFilesOutputDirectory { get; }

    // TODO: https://github.com/dotnet/roslyn/issues/35065
    // The project system doesn't currently provide paths to the PDB or XML files that the compiler produces.
    // public readonly string? PdbPath { get; }
    // public readonly string? DocumentationCommentsPath { get; }

    internal CompilationOutputInfo(string? assemblyPath, string? generatedFilesOutputDirectory)
    {
        AssemblyPath = assemblyPath;
        GeneratedFilesOutputDirectory = generatedFilesOutputDirectory;
    }

    public CompilationOutputInfo WithAssemblyPath(string? path)
        => new(assemblyPath: path, GeneratedFilesOutputDirectory);

    public CompilationOutputInfo WithGeneratedFilesOutputDirectory(string? path)
    {
        if (path != null && !PathUtilities.IsAbsolute(path))
        {
            throw new ArgumentException(WorkspacesResources.AbsolutePathExpected, nameof(path));
        }

        return new(AssemblyPath, path);
    }

    /// <summary>
    /// True if the project has an absolute generated source file output path.
    /// 
    /// Must be true for any workspace that supports EnC. If false, the compiler and IDE wouldn't agree on the file paths of source-generated files,
    /// which might cause different metadata to be emitted for file-scoped classes between compilation and EnC.
    /// </summary>
    internal bool HasEffectiveGeneratedFilesOutputDirectory
        => PathUtilities.IsAbsolute(GeneratedFilesOutputDirectory ?? AssemblyPath);

    /// <summary>
    /// Absolute path of a directory used to produce absolute file paths of source generated files.
    /// </summary>
    internal string? GetEffectiveGeneratedFilesOutputDirectory()
        => HasEffectiveGeneratedFilesOutputDirectory ? GeneratedFilesOutputDirectory ?? PathUtilities.GetDirectoryName(AssemblyPath) : null;

    public override bool Equals(object? obj)
        => obj is CompilationOutputInfo info && Equals(info);

    public bool Equals(CompilationOutputInfo other)
        => AssemblyPath == other.AssemblyPath &&
           GeneratedFilesOutputDirectory == other.GeneratedFilesOutputDirectory;

    public override int GetHashCode()
        => Hash.Combine(GeneratedFilesOutputDirectory, Hash.Combine(AssemblyPath, 0));

    public static bool operator ==(in CompilationOutputInfo left, in CompilationOutputInfo right)
        => left.Equals(right);

    public static bool operator !=(in CompilationOutputInfo left, in CompilationOutputInfo right)
        => !left.Equals(right);

    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(AssemblyPath);
        writer.WriteString(GeneratedFilesOutputDirectory);
    }

    internal static CompilationOutputInfo ReadFrom(ObjectReader reader)
    {
        var assemblyPath = reader.ReadString();
        var generatedFilesOutputDirectory = reader.ReadString();
        return new CompilationOutputInfo(assemblyPath, generatedFilesOutputDirectory);
    }
}

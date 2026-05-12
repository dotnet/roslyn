// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Use to configure optional properties for creating a <see cref="RazorSourceDocument"/>.
/// </summary>
public abstract class RazorSourceDocumentProperties
{
    /// <summary>
    /// A <see cref="RazorSourceDocumentProperties"/> with default values.
    /// </summary>
    internal static readonly RazorSourceDocumentProperties Default = new DefaultRazorSourceDocumentProperties();

    internal static RazorSourceDocumentProperties Create(string? filePath, string? relativePath)
    {
        if (relativePath == null)
        {
            return filePath == null
                ? Default
                : new NoRelativePathRazorSourceDocumentProperties(filePath);
        }

        return new RelativePathRazorSourceDocumentProperties(filePath, relativePath);
    }

    /// <summary>
    /// Creates a new <see cref="RazorSourceDocumentProperties"/>.
    /// </summary>
    private RazorSourceDocumentProperties()
    {
    }

    /// <summary>
    /// Gets the path to the source file. May be an absolute or project-relative path. May be <c>null</c>.
    /// </summary>
    /// <remarks>
    /// An absolute path must be provided to generate debuggable assemblies.
    /// </remarks>
    public abstract string? FilePath { get; }

    /// <summary>
    /// Gets the project-relative path to the source file. May be <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The relative path (if provided) is used for display (error messages). The project-relative path may also
    /// be used to embed checksums of the original source documents to support runtime recompilation of Razor code.
    /// </remarks>
    public abstract string? RelativePath { get; }

    private sealed class DefaultRazorSourceDocumentProperties : RazorSourceDocumentProperties
    {
        public override string? FilePath => null;
        public override string? RelativePath => null;
    }

    private sealed class NoRelativePathRazorSourceDocumentProperties(string filePath) : RazorSourceDocumentProperties
    {
        public override string FilePath { get; } = filePath;
        public override string? RelativePath => null;
    }

    private sealed class RelativePathRazorSourceDocumentProperties(string? filePath, string relativePath) : RazorSourceDocumentProperties
    {
        public override string? FilePath { get; } = filePath;
        public override string RelativePath { get; } = relativePath;
    }
}

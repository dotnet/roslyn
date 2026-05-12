// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The Razor template source.
/// </summary>
public sealed class RazorSourceDocument
{
    internal const int LargeObjectHeapLimitInChars = 40 * 1024; // 40K Unicode chars is 80KB which is less than the large object heap limit.

    private readonly RazorSourceDocumentProperties _properties;

    /// <summary>
    /// Gets the source text of the document.
    /// </summary>
    public SourceText Text { get; }

    /// <summary>
    /// Gets the file path of the original source document.
    /// </summary>
    /// <remarks>
    /// The file path may be either an absolute path or project-relative path. An absolute path is required
    /// to generate debuggable assemblies.
    /// </remarks>
    public string? FilePath => _properties.FilePath;

    /// <summary>
    /// Gets the project-relative path to the source file. May be <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The relative path (if provided) is used for display (error messages). The project-relative path may also
    /// be used to embed checksums of the original source documents to support runtime recompilation of Razor code.
    /// </remarks>
    public string? RelativePath => _properties.RelativePath;

    /// <summary>
    /// Gets the file path in a format that should be used for display.
    /// </summary>
    /// <returns>The <see cref="RelativePath"/> if set, or the <see cref="FilePath"/>.</returns>
    public string? GetFilePathForDisplay()
    {
        return RelativePath ?? FilePath;
    }

    /// <summary>
    /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <param name="fileName">The file name of the template.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument ReadFrom(Stream stream, string fileName)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var properties = RazorSourceDocumentProperties.Create(fileName, relativePath: null);
        var sourceText = SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, properties);
    }

    /// <summary>
    /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <param name="fileName">The file name of the template.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding"/> to use to read the <paramref name="stream"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument ReadFrom(Stream stream, string fileName, Encoding encoding)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        var properties = RazorSourceDocumentProperties.Create(fileName, relativePath: null);
        var sourceText = SourceText.From(stream, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, properties);
    }

    /// <summary>
    /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding"/> to use to read the <paramref name="stream"/>.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument ReadFrom(Stream stream, Encoding encoding, RazorSourceDocumentProperties properties)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        var sourceText = SourceText.From(stream, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, properties);
    }

    /// <summary>
    /// Reads the <see cref="RazorSourceDocument"/> from the specified <paramref name="projectItem"/>.
    /// </summary>
    /// <param name="projectItem">The <see cref="RazorProjectItem"/> to read from.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument ReadFrom(RazorProjectItem projectItem)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        // ProjectItem.PhysicalPath is usually an absolute (rooted) path.
        var filePath = projectItem.PhysicalPath;
        if (string.IsNullOrEmpty(filePath))
        {
            // Fall back to the relative path only if necessary.
            filePath = projectItem.RelativePhysicalPath;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            // Then fall back to the FilePath (yeah it's a bad name) which is like an MVC view engine path
            // It's much better to have something than nothing.
            filePath = projectItem.FilePath;
        }

        using (var stream = projectItem.Read())
        {
            // Autodetect the encoding.
            var relativePath = projectItem.RelativePhysicalPath ?? projectItem.FilePath;
            var sourceText = SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256);
            return new RazorSourceDocument(sourceText, RazorSourceDocumentProperties.Create(filePath, relativePath));
        }
    }

    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    /// <remarks>Uses <see cref="System.Text.Encoding.UTF8" /></remarks>
    public static RazorSourceDocument Create(string content, string fileName)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        return Create(content, fileName, Encoding.UTF8);
    }


    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    /// <remarks>Uses <see cref="System.Text.Encoding.UTF8" /></remarks>
    public static RazorSourceDocument Create(string content, RazorSourceDocumentProperties properties)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        return Create(content, Encoding.UTF8, properties);
    }

    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="fileName">The file name of the <see cref="RazorSourceDocument"/>.</param>
    /// <param name="encoding">The <see cref="System.Text.Encoding"/> of the file <paramref name="content"/> was read from.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument Create(string content, string fileName, Encoding encoding)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        var properties = RazorSourceDocumentProperties.Create(fileName, relativePath: null);
        var sourceText = SourceText.From(content, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, properties);
    }

    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="content"/>.
    /// </summary>
    /// <param name="content">The source document content.</param>
    /// <param name="encoding">The encoding of the source document.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument Create(string content, Encoding encoding, RazorSourceDocumentProperties properties)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        var sourceText = SourceText.From(content, encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
        return new RazorSourceDocument(sourceText, properties);
    }

    /// <summary>
    /// Creates a <see cref="RazorSourceDocument"/> from the specified <paramref name="text"/>.
    /// </summary>
    /// <param name="content">The source text.</param>
    /// <param name="properties">Properties to configure the <see cref="RazorSourceDocument"/>.</param>
    /// <returns>The <see cref="RazorSourceDocument"/>.</returns>
    public static RazorSourceDocument Create(SourceText text, RazorSourceDocumentProperties properties)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        return new RazorSourceDocument(text, properties);
    }

    private RazorSourceDocument(SourceText sourceText, RazorSourceDocumentProperties properties)
    {
        Text = sourceText;
        _properties = properties;
    }
}

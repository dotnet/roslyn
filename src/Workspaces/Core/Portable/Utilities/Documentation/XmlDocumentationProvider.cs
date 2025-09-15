// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A class used to provide XML documentation to the compiler for members from metadata from an XML document source.
/// </summary>
public abstract class XmlDocumentationProvider : DocumentationProvider
{
    private readonly SemaphoreSlim _gate = new(initialCount: 1);
    private Dictionary<string, string> _docComments;

    /// <summary>
    /// Gets the source stream for the XML document.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    protected abstract Stream GetSourceStream(CancellationToken cancellationToken);

    /// <summary>
    /// Creates an <see cref="XmlDocumentationProvider"/> from bytes representing XML documentation data.
    /// </summary>
    /// <param name="xmlDocCommentBytes">The XML document bytes.</param>
    /// <returns>An <see cref="XmlDocumentationProvider"/>.</returns>
    public static XmlDocumentationProvider CreateFromBytes(byte[] xmlDocCommentBytes)
        => new ContentBasedXmlDocumentationProvider(xmlDocCommentBytes);

    private static XmlDocumentationProvider DefaultXmlDocumentationProvider { get; } = new NullXmlDocumentationProvider();

    /// <summary>
    /// Creates an <see cref="XmlDocumentationProvider"/> from an XML documentation file.
    /// </summary>
    /// <param name="xmlDocCommentFilePath">The path to the XML file.</param>
    /// <returns>An <see cref="XmlDocumentationProvider"/>.</returns>
    public static XmlDocumentationProvider CreateFromFile(string xmlDocCommentFilePath)
    {
        if (!File.Exists(xmlDocCommentFilePath))
        {
            return DefaultXmlDocumentationProvider;
        }

        return new FileBasedXmlDocumentationProvider(xmlDocCommentFilePath);
    }

    protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
    {
        if (_docComments == null)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                try
                {
                    using (var stream = GetSourceStream(cancellationToken))
                    using (var xmlReader = XmlReader.Create(stream, s_xmlSettings))
                    {
                        var comments = new Dictionary<string, string>();
                        while (xmlReader.Read())
                        {
                            if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "member")
                            {
                                var name = xmlReader.GetAttribute("name");
                                if (name != null)
                                {
                                    // Read the entire <member> element as a string
                                    var memberXml = xmlReader.ReadOuterXml();
                                    comments[name] = memberXml;
                                }
                            }
                        }

                        _docComments = comments;
                    }
                }
                catch (Exception)
                {
                    _docComments = [];
                }
            }
        }

        return _docComments.TryGetValue(documentationMemberID, out var docComment) ? docComment : "";
    }

    private static readonly XmlReaderSettings s_xmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
    };

    private sealed class ContentBasedXmlDocumentationProvider : XmlDocumentationProvider
    {
        private readonly byte[] _xmlDocCommentBytes;

        public ContentBasedXmlDocumentationProvider(byte[] xmlDocCommentBytes)
        {
            Contract.ThrowIfNull(xmlDocCommentBytes);

            _xmlDocCommentBytes = xmlDocCommentBytes;
        }

        protected override Stream GetSourceStream(CancellationToken cancellationToken)
            => SerializableBytes.CreateReadableStream(_xmlDocCommentBytes);

        public override bool Equals(object obj)
        {
            return obj is ContentBasedXmlDocumentationProvider other && EqualsHelper(other);
        }

        private bool EqualsHelper(ContentBasedXmlDocumentationProvider other)
        {
            // Check for reference equality first
            if (this == other || _xmlDocCommentBytes == other._xmlDocCommentBytes)
            {
                return true;
            }

            // Compare byte sequences
            if (_xmlDocCommentBytes.Length != other._xmlDocCommentBytes.Length)
            {
                return false;
            }

            for (var i = 0; i < _xmlDocCommentBytes.Length; i++)
            {
                if (_xmlDocCommentBytes[i] != other._xmlDocCommentBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
            => Hash.CombineValues(_xmlDocCommentBytes);
    }

    private sealed class FileBasedXmlDocumentationProvider : XmlDocumentationProvider
    {
        private readonly string _filePath;

        public FileBasedXmlDocumentationProvider(string filePath)
        {
            Contract.ThrowIfNull(filePath);
            Debug.Assert(PathUtilities.IsAbsolute(filePath));

            _filePath = filePath;
        }

        protected override Stream GetSourceStream(CancellationToken cancellationToken)
            => new FileStream(_filePath, FileMode.Open, FileAccess.Read);

        public override bool Equals(object obj)
        {
            return obj is FileBasedXmlDocumentationProvider other && _filePath == other._filePath;
        }

        public override int GetHashCode()
            => _filePath.GetHashCode();
    }

    /// <summary>
    /// A trivial XmlDocumentationProvider which never returns documentation.
    /// </summary>
    private sealed class NullXmlDocumentationProvider : XmlDocumentationProvider
    {
        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default)
            => "";

        protected override Stream GetSourceStream(CancellationToken cancellationToken)
            => new MemoryStream();

        public override bool Equals(object obj)
        {
            // Only one instance is expected to exist, so reference equality is fine.
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
            => 0;
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents all the arguments necessary to create a new document instance.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay() , nq}")]
    public sealed class DocumentInfo
    {
        internal DocumentAttributes Attributes { get; }

        /// <summary>
        /// The Id of the document.
        /// </summary>
        public DocumentId Id => Attributes.Id;

        /// <summary>
        /// The name of the document.
        /// </summary>
        public string Name => Attributes.Name;

        /// <summary>
        /// The names of the logical nested folders the document is contained in.
        /// </summary>
        public IReadOnlyList<string> Folders => Attributes.Folders;

        /// <summary>
        /// The kind of the source code.
        /// </summary>
        public SourceCodeKind SourceCodeKind => Attributes.SourceCodeKind;

        /// <summary>
        /// The file path of the document.
        /// </summary>
        public string? FilePath => Attributes.FilePath;

        /// <summary>
        /// True if the document is a side effect of the build.
        /// </summary>
        public bool IsGenerated => Attributes.IsGenerated;

        /// <summary>
        /// A loader that can retrieve the document text.
        /// </summary>
        public TextLoader? TextLoader { get; }

        /// <summary>
        /// A <see cref="IDocumentServiceProvider"/> associated with this document
        /// </summary>
        internal IDocumentServiceProvider? DocumentServiceProvider { get; }

        /// <summary>
        /// Create a new instance of a <see cref="DocumentInfo"/>.
        /// </summary>
        internal DocumentInfo(DocumentAttributes attributes, TextLoader? loader, IDocumentServiceProvider? documentServiceProvider)
        {
            Attributes = attributes;
            TextLoader = loader;
            DocumentServiceProvider = documentServiceProvider;
        }

        public static DocumentInfo Create(
            DocumentId id,
            string name,
            IEnumerable<string>? folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader? loader = null,
            string? filePath = null,
            bool isGenerated = false)
        {
            return Create(id, name, folders, sourceCodeKind, loader, filePath, isGenerated, documentServiceProvider: null);
        }

        internal static DocumentInfo Create(
            DocumentId id,
            string name,
            IEnumerable<string>? folders,
            SourceCodeKind sourceCodeKind,
            TextLoader? loader,
            string? filePath,
            bool isGenerated,
            IDocumentServiceProvider? documentServiceProvider)
        {
            return new DocumentInfo(new DocumentAttributes(id, name, folders, sourceCodeKind, filePath, isGenerated), loader, documentServiceProvider);
        }

        private DocumentInfo With(
            DocumentAttributes? attributes = null,
            Optional<TextLoader?> loader = default,
            Optional<IDocumentServiceProvider?> documentServiceProvider = default)
        {
            var newAttributes = attributes ?? Attributes;
            var newLoader = loader.HasValue ? loader.Value : TextLoader;
            var newDocumentServiceProvider = documentServiceProvider.HasValue ? documentServiceProvider.Value : DocumentServiceProvider;

            if (newAttributes == Attributes &&
                newLoader == TextLoader &&
                newDocumentServiceProvider == DocumentServiceProvider)
            {
                return this;
            }

            return new DocumentInfo(newAttributes, newLoader, newDocumentServiceProvider);
        }

        public DocumentInfo WithId(DocumentId id)
        {
            return With(attributes: Attributes.With(id: id));
        }

        public DocumentInfo WithName(string name)
        {
            return this.With(attributes: Attributes.With(name: name));
        }

        public DocumentInfo WithFolders(IEnumerable<string>? folders)
        {
            return this.With(attributes: Attributes.With(folders: folders.ToImmutableReadOnlyListOrEmpty()));
        }

        public DocumentInfo WithSourceCodeKind(SourceCodeKind kind)
        {
            return this.With(attributes: Attributes.With(sourceCodeKind: kind));
        }

        public DocumentInfo WithTextLoader(TextLoader? loader)
        {
            return With(loader: loader);
        }

        public DocumentInfo WithFilePath(string? filePath)
        {
            return this.With(attributes: Attributes.With(filePath: filePath));
        }

        private string GetDebuggerDisplay()
        {
            return (FilePath == null) ? (nameof(Name) + " = " + Name) : (nameof(FilePath) + " = " + FilePath);
        }

        /// <summary>
        /// type that contains information regarding this document itself but
        /// no tree information such as document info
        /// </summary>
        internal class DocumentAttributes : IChecksummedObject, IObjectWritable
        {
            /// <summary>
            /// The Id of the document.
            /// </summary>
            public DocumentId Id { get; }

            /// <summary>
            /// The name of the document.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// The names of the logical nested folders the document is contained in.
            /// </summary>
            public IReadOnlyList<string> Folders { get; }

            /// <summary>
            /// The kind of the source code.
            /// </summary>
            public SourceCodeKind SourceCodeKind { get; }

            /// <summary>
            /// The file path of the document.
            /// </summary>
            public string? FilePath { get; }

            /// <summary>
            /// True if the document is a side effect of the build.
            /// </summary>
            public bool IsGenerated { get; }

            public DocumentAttributes(
                DocumentId id,
                string name,
                IEnumerable<string>? folders,
                SourceCodeKind sourceCodeKind,
                string? filePath,
                bool isGenerated)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Folders = folders.ToImmutableReadOnlyListOrEmpty();
                SourceCodeKind = sourceCodeKind;
                FilePath = filePath;
                IsGenerated = isGenerated;
            }

            public DocumentAttributes With(
                DocumentId? id = null,
                string? name = null,
                IEnumerable<string>? folders = null,
                Optional<SourceCodeKind> sourceCodeKind = default,
                Optional<string?> filePath = default,
                Optional<bool> isGenerated = default)
            {
                var newId = id ?? Id;
                var newName = name ?? Name;
                var newFolders = folders?.ToImmutableReadOnlyListOrEmpty() ?? Folders;
                var newSourceCodeKind = sourceCodeKind.HasValue ? sourceCodeKind.Value : SourceCodeKind;
                var newFilePath = filePath.HasValue ? filePath.Value : FilePath;
                var newIsGenerated = isGenerated.HasValue ? isGenerated.Value : IsGenerated;

                if (newId == Id &&
                    newName == Name &&
                    newFolders == Folders &&
                    newSourceCodeKind == SourceCodeKind &&
                    newFilePath == FilePath &&
                    newIsGenerated == IsGenerated)
                {
                    return this;
                }

                return new DocumentAttributes(newId, newName, newFolders, newSourceCodeKind, newFilePath, newIsGenerated);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            public void WriteTo(ObjectWriter writer)
            {
                Id.WriteTo(writer);

                writer.WriteString(Name);
                writer.WriteValue(Folders.ToArray());
                writer.WriteInt32((int)SourceCodeKind);
                writer.WriteString(FilePath);
                writer.WriteBoolean(IsGenerated);
            }

            public static DocumentAttributes ReadFrom(ObjectReader reader)
            {
                var documentId = DocumentId.ReadFrom(reader);

                var name = reader.ReadString();
                var folders = (string[])reader.ReadValue();
                var sourceCodeKind = reader.ReadInt32();
                var filePath = reader.ReadString();
                var isGenerated = reader.ReadBoolean();

                return new DocumentAttributes(documentId, name, folders, (SourceCodeKind)sourceCodeKind, filePath, isGenerated);
            }

            private Checksum? _lazyChecksum;
            Checksum IChecksummedObject.Checksum
            {
                get
                {
                    if (_lazyChecksum == null)
                    {
                        _lazyChecksum = Checksum.Create(WellKnownSynchronizationKind.DocumentAttributes, this);
                    }

                    return _lazyChecksum;
                }
            }
        }
    }
}

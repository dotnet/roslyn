﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            return Create(
                id ?? throw new ArgumentNullException(nameof(id)),
                name ?? throw new ArgumentNullException(nameof(name)),
                folders.AsBoxedImmutableArrayWithNonNullItems() ?? throw new ArgumentNullException(nameof(folders)),
                sourceCodeKind,
                loader,
                filePath,
                isGenerated,
                documentServiceProvider: null);
        }

        // TODO: https://github.com/dotnet/roslyn/issues/35079
        // Used by Razor: https://github.com/dotnet/aspnetcore-tooling/blob/master/src/Razor/src/Microsoft.VisualStudio.Editor.Razor/DefaultVisualStudioMacDocumentInfoFactory.cs#L38
        [Obsolete("This is a compatibility shim for Razor; please do not use it.")]
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
            return new DocumentInfo(new DocumentAttributes(id, name, folders.ToBoxedImmutableArray(), sourceCodeKind, filePath, isGenerated), loader, documentServiceProvider);
        }

        internal static DocumentInfo Create(
            DocumentId id,
            string name,
            IReadOnlyList<string> folders,
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
            => With(attributes: Attributes.With(id: id ?? throw new ArgumentNullException(nameof(id))));

        public DocumentInfo WithName(string name)
            => With(attributes: Attributes.With(name: name ?? throw new ArgumentNullException(nameof(name))));

        public DocumentInfo WithFolders(IEnumerable<string>? folders)
            => With(attributes: Attributes.With(folders: folders.AsBoxedImmutableArrayWithNonNullItems() ?? throw new ArgumentNullException(nameof(folders))));

        public DocumentInfo WithSourceCodeKind(SourceCodeKind kind)
            => With(attributes: Attributes.With(sourceCodeKind: kind));

        public DocumentInfo WithFilePath(string? filePath)
            => With(attributes: Attributes.With(filePath: filePath));

        public DocumentInfo WithTextLoader(TextLoader? loader)
            => With(loader: loader);

        private string GetDebuggerDisplay()
            => (FilePath == null) ? (nameof(Name) + " = " + Name) : (nameof(FilePath) + " = " + FilePath);

        /// <summary>
        /// type that contains information regarding this document itself but
        /// no tree information such as document info
        /// </summary>
        internal sealed class DocumentAttributes : IChecksummedObject, IObjectWritable
        {
            private Checksum? _lazyChecksum;

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
                IReadOnlyList<string> folders,
                SourceCodeKind sourceCodeKind,
                string? filePath,
                bool isGenerated)
            {
                Id = id;
                Name = name;
                Folders = folders;
                SourceCodeKind = sourceCodeKind;
                FilePath = filePath;
                IsGenerated = isGenerated;
            }

            public DocumentAttributes With(
                DocumentId? id = null,
                string? name = null,
                IReadOnlyList<string>? folders = null,
                Optional<SourceCodeKind> sourceCodeKind = default,
                Optional<string?> filePath = default,
                Optional<bool> isGenerated = default)
            {
                var newId = id ?? Id;
                var newName = name ?? Name;
                var newFolders = folders ?? Folders;
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

            Checksum IChecksummedObject.Checksum
                => _lazyChecksum ??= Checksum.Create(WellKnownSynchronizationKind.DocumentAttributes, this);
        }
    }
}

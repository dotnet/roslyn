// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public string FilePath => Attributes.FilePath;

        /// <summary>
        /// True if the document is a side effect of the build.
        /// </summary>
        public bool IsGenerated => Attributes.IsGenerated;

        /// <summary>
        /// A loader that can retrieve the document text.
        /// </summary>
        public TextLoader TextLoader { get; }

        /// <summary>
        /// Create a new instance of a <see cref="DocumentInfo"/>.
        /// </summary>
        private DocumentInfo(DocumentAttributes attributes, TextLoader loader)
        {
            Attributes = attributes;
            TextLoader = loader;
        }

        public static DocumentInfo Create(
            DocumentId id,
            string name,
            IEnumerable<string> folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader loader = null,
            string filePath = null,
            bool isGenerated = false)
        {
            return new DocumentInfo(new DocumentAttributes(id, name, folders, sourceCodeKind, filePath, projectFilePath: null, isGenerated: isGenerated), loader);
        }

        private DocumentInfo With(
            DocumentAttributes attributes = null,
            Optional<TextLoader> loader = default(Optional<TextLoader>))
        {
            var newAttributes = attributes ?? Attributes;
            var newLoader = loader.HasValue ? loader.Value : TextLoader;

            if (newAttributes == Attributes && newLoader == TextLoader)
            {
                return this;
            }

            return new DocumentInfo(newAttributes, newLoader);
        }

        public DocumentInfo WithId(DocumentId id)
        {
            return With(attributes: Attributes.With(id: id));
        }

        public DocumentInfo WithName(string name)
        {
            return this.With(attributes: Attributes.With(name: name));
        }

        public DocumentInfo WithFolders(IEnumerable<string> folders)
        {
            return this.With(attributes: Attributes.With(folders: folders.ToImmutableReadOnlyListOrEmpty()));
        }

        public DocumentInfo WithSourceCodeKind(SourceCodeKind kind)
        {
            return this.With(attributes: Attributes.With(sourceCodeKind: kind));
        }

        public DocumentInfo WithTextLoader(TextLoader loader)
        {
            return With(loader: loader);
        }

        public DocumentInfo WithFilePath(string filePath)
        {
            return this.With(attributes: Attributes.With(filePath: filePath));
        }

        internal DocumentInfo WithProjectFilePath(string projectFilePath)
        {
            return this.With(attributes: Attributes.With(projectFilePath: projectFilePath));
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
            public string FilePath { get; }

            /// <summary>
            /// The file path of the project
            /// </summary>
            public string ProjectFilePath { get; }

            /// <summary>
            /// True if the document is a side effect of the build.
            /// </summary>
            public bool IsGenerated { get; }

            public DocumentAttributes(
                DocumentId id,
                string name,
                IEnumerable<string> folders,
                SourceCodeKind sourceCodeKind,
                string filePath,
                string projectFilePath,
                bool isGenerated)
            {
                Id = id ?? throw new ArgumentNullException(nameof(id));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Folders = folders.ToImmutableReadOnlyListOrEmpty();
                SourceCodeKind = sourceCodeKind;
                FilePath = filePath;
                ProjectFilePath = projectFilePath;
                IsGenerated = isGenerated;
            }

            public DocumentAttributes With(
                DocumentId id = null,
                string name = null,
                IEnumerable<string> folders = null,
                Optional<SourceCodeKind> sourceCodeKind = default(Optional<SourceCodeKind>),
                Optional<string> filePath = default(Optional<string>),
                Optional<string> projectFilePath = default(Optional<string>),
                Optional<bool> isGenerated = default(Optional<bool>))
            {
                var newId = id ?? Id;
                var newName = name ?? Name;
                var newFolders = folders ?? Folders;
                var newSourceCodeKind = sourceCodeKind.HasValue ? sourceCodeKind.Value : SourceCodeKind;
                var newFilePath = filePath.HasValue ? filePath.Value : FilePath;
                var newProjectFilePath = projectFilePath.HasValue ? projectFilePath.Value : ProjectFilePath;
                var newIsGenerated = isGenerated.HasValue ? isGenerated.Value : IsGenerated;

                if (newId == Id &&
                    newName == Name &&
                    newFolders == Folders &&
                    newSourceCodeKind == SourceCodeKind &&
                    newFilePath == FilePath &&
                    newProjectFilePath == ProjectFilePath &&
                    newIsGenerated == IsGenerated)
                {
                    return this;
                }

                return new DocumentAttributes(newId, newName, newFolders, newSourceCodeKind, newFilePath, newProjectFilePath, newIsGenerated);
            }

            public void WriteTo(ObjectWriter writer)
            {
                // these information is volatile. it can be different
                // per session or not content based value. basically not
                // persistable. these information will not be included in checksum
                Id.WriteTo(writer);

                writer.WriteString(FilePath);
                writer.WriteString(ProjectFilePath);

                writer.WriteString(Name);
                writer.WriteValue(Folders.ToArray());
                writer.WriteInt32((int)SourceCodeKind);
                writer.WriteBoolean(IsGenerated);
            }

            public static DocumentAttributes ReadFrom(ObjectReader reader)
            {
                var documentId = DocumentId.ReadFrom(reader);

                var filePath = reader.ReadString();
                var projectFilePath = reader.ReadString();

                var name = reader.ReadString();
                var folders = (string[])reader.ReadValue();
                var sourceCodeKind = reader.ReadInt32();
                var isGenerated = reader.ReadBoolean();

                return new DocumentAttributes(documentId, name, folders, (SourceCodeKind)sourceCodeKind, filePath, projectFilePath, isGenerated);
            }

            private Checksum _lazyChecksum;
            Checksum IChecksummedObject.Checksum
            {
                get
                {
                    if (_lazyChecksum == null)
                    {
                        using (var stream = SerializableBytes.CreateWritableStream())
                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteString(nameof(DocumentAttributes));

                            if (FilePath == null || ProjectFilePath == null)
                            {
                                // this checksum is not persistable because
                                // this info doesn't have non volatile info
                                Id.WriteTo(writer);
                            }

                            // these information is not volatile. it won't be different
                            // per session, basically persistable content based values. 
                            // only these information will be included in checksum
                            writer.WriteString(FilePath);

                            // we need project file path due to linked file
                            writer.WriteString(ProjectFilePath);

                            writer.WriteString(Name);
                            writer.WriteValue(Folders.ToArray());
                            writer.WriteInt32((int)SourceCodeKind);
                            writer.WriteBoolean(IsGenerated);

                            _lazyChecksum = Checksum.Create(stream);
                        }
                    }

                    return _lazyChecksum;
                }
            }
        }
    }
}

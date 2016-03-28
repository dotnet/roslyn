// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents all the arguments necessary to create a new document instance.
    /// </summary>
    public sealed class DocumentInfo
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
        /// A loader that can retrieve the document text.
        /// </summary>
        public TextLoader TextLoader { get; }

        /// <summary>
        /// True if the document is a side effect of the build.
        /// </summary>
        public bool IsGenerated { get; }

        /// <summary>
        /// Create a new instance of a <see cref="DocumentInfo"/>.
        /// </summary>
        private DocumentInfo(
            DocumentId id,
            string name,
            IEnumerable<string> folders,
            SourceCodeKind sourceCodeKind,
            TextLoader loader,
            string filePath,
            bool isGenerated)
        {
            Debug.Assert(isGenerated == ((filePath != null) && IsGeneratedFile(filePath)));

            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.Id = id;
            this.Name = name;
            this.Folders = folders.ToImmutableReadOnlyListOrEmpty();
            this.SourceCodeKind = sourceCodeKind;
            this.TextLoader = loader;
            this.FilePath = filePath;
            this.IsGenerated = isGenerated;
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
            return new DocumentInfo(id, name, folders, sourceCodeKind, loader, filePath, isGenerated);
        }

        internal static bool IsGeneratedFile(string path)
        {
            path = path.ToLower();
            return path.Contains(@"obj\debug\") && !path.Contains(@"obj\debug\temporarygeneratedfile_");
        }

        private DocumentInfo With(
            DocumentId id = null,
            string name = null,
            IEnumerable<string> folders = null,
            Optional<SourceCodeKind> sourceCodeKind = default(Optional<SourceCodeKind>),
            Optional<TextLoader> loader = default(Optional<TextLoader>),
            Optional<string> filePath = default(Optional<string>))
        {
            var newId = id ?? this.Id;
            var newName = name ?? this.Name;
            var newFolders = folders ?? this.Folders;
            var newSourceCodeKind = sourceCodeKind.HasValue ? sourceCodeKind.Value : this.SourceCodeKind;
            var newLoader = loader.HasValue ? loader.Value : this.TextLoader;
            var newFilePath = filePath.HasValue ? filePath.Value : this.FilePath;

            if (newId == this.Id &&
                newName == this.Name &&
                newFolders == this.Folders &&
                newSourceCodeKind == this.SourceCodeKind &&
                newLoader == this.TextLoader &&
                newFilePath == this.FilePath)
            {
                return this;
            }

            return new DocumentInfo(newId, newName, newFolders, newSourceCodeKind, newLoader, newFilePath, this.IsGenerated);
        }

        public DocumentInfo WithId(DocumentId id)
        {
            return this.With(id: id);
        }

        public DocumentInfo WithName(string name)
        {
            return this.With(name: name);
        }

        public DocumentInfo WithFolders(IEnumerable<string> folders)
        {
            return this.With(folders: folders.ToImmutableReadOnlyListOrEmpty());
        }

        public DocumentInfo WithSourceCodeKind(SourceCodeKind kind)
        {
            return this.With(sourceCodeKind: kind);
        }

        public DocumentInfo WithTextLoader(TextLoader loader)
        {
            return this.With(loader: loader);
        }

        public DocumentInfo WithFilePath(string filePath)
        {
            return this.With(filePath: filePath);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class FrozenDocumentInfo : DocumentInfo
    {
        private readonly DocumentId id;
        private readonly VersionStamp version;
        private readonly string name;
        private readonly ImmutableList<string> folders;
        private readonly SourceCodeKind sourceCodeKind;
        private readonly string filePath;
        private readonly bool isGenerated;
        private readonly TextLoader loader;

        public FrozenDocumentInfo(
            DocumentId id,
            string name,
            ImmutableList<string> folders,
            SourceCodeKind sourceCodeKind,
            TextLoader loader,
            VersionStamp version,
            string filePath,
            bool isGenerated)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            this.id = id;
            this.folders = folders;
            this.name = name;
            this.sourceCodeKind = sourceCodeKind;
            this.loader = loader;
            this.version = version;
            this.filePath = filePath;
            this.isGenerated = isGenerated;
        }

        public override DocumentId Id
        {
            get
            {
                return id;
            }
        }

        public override VersionStamp Version
        {
            get
            {
                return version;
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override IList<string> Folders
        {
            get
            {
                return folders;
            }
        }

        public override SourceCodeKind SourceCodeKind
        {
            get
            {
                return sourceCodeKind;
            }
        }

        public override string FilePath
        {
            get
            {
                return filePath;
            }
        }

        public override bool IsGenerated
        {
            get 
            {
                return this.isGenerated;
            }
        }

        public override TextLoader Loader
        {
            get
            {
                return loader;
            }
        }

        private FrozenDocumentInfo With(
            DocumentId id = null,
            Optional<ImmutableList<string>> folders = default(Optional<ImmutableList<string>>),
            string name = null,
            Optional<SourceCodeKind> sourceCodeKind = default(Optional<SourceCodeKind>),
            TextLoader loader = null,
            Optional<VersionStamp> version = default(Optional<VersionStamp>),
            Optional<string> filePath = default(Optional<string>))
        {
            var newId = id ?? this.Id;
            var newName = name ?? this.Name;
            var newFolders = folders.HasValue ? folders.Value : this.folders;
            var newSourceCodeKind = sourceCodeKind.HasValue ? sourceCodeKind.Value : this.SourceCodeKind;
            var newLoader = loader ?? this.Loader;
            var newVersion = version.HasValue ? version.Value : this.Version;
            var newFilePath = filePath.HasValue ? filePath.Value : this.FilePath;

            if (newId == this.Id &&
                newName == this.Name &&
                newFolders == this.Folders &&
                newSourceCodeKind == this.SourceCodeKind &&
                newLoader == this.Loader &&
                newVersion == this.Version &&
                newFilePath == this.FilePath)
            {
                return this;
            }

            return new FrozenDocumentInfo(newId, newName, newFolders, newSourceCodeKind, newLoader, newVersion, newFilePath, this.isGenerated);
        }

        internal FrozenDocumentInfo WithFolders(ImmutableList<string> folders)
        {
            return this.With(folders: new Optional<ImmutableList<string>>(folders));
        }

        internal DocumentInfo WithVersion(VersionStamp version)
        {
            return this.With(version: version);
        }
    }
}
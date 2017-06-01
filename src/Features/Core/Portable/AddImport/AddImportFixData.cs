// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal class AddImportFixData
    {
        /// <summary>
        /// The document where we started the Add-Import operation from.  (Also the document that
        /// will have the import added to it).  
        /// </summary>
        public readonly Document OriginalDocument;

        /// <summary>
        /// Text changes to make to the document.  Usually just the import to add.  May also
        /// include a change to the name node the feature was invoked on to fix the casing of it.
        /// May be empty for fixes that don't need to add an import and only do something like
        /// add a project/metadata reference.
        /// </summary>
        public readonly ImmutableArray<TextChange> TextChanges;

        /// <summary>
        /// String to display in the lightbulb menu.
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// Tags that control what glyph is displayed in the lightbulb menu.
        /// </summary>
        public readonly ImmutableArray<string> Tags;

        /// <summary>
        /// The priority this item should have in the lightbulb list.
        /// </summary>
        public readonly CodeActionPriority Priority;

        #region When adding P2P refrences.

        /// <summary>
        /// The optional id for a <see cref="Project"/> we'd like to add a reference to.
        /// </summary>
        public readonly ProjectId ProjectReferenceToAdd;

        #endregion

        #region When adding a metadata reference

        /// <summary>
        /// If we're adding <see cref="PortableExecutableReferenceFilePathToAdd"/> then this
        /// is the id for the <see cref="Project"/> we can find that <see cref="PortableExecutableReference"/>
        /// referenced from.
        /// </summary>
        public readonly ProjectId PortableExecutableReferenceProjectId;

        /// <summary>
        /// If we want to add a <see cref="PortableExecutableReference"/> metadata reference, this 
        /// is the <see cref="PortableExecutableReference.FilePath"/> for it.
        /// </summary>
        public readonly string PortableExecutableReferenceFilePathToAdd;

        #endregion

        #region When adding an assembly reference

        public readonly string AssemblyReferenceAssemblyName;
        public readonly string AssemblyReferenceFullyQualifiedTypeName;

        #endregion

        #region When adding a package reference

        public readonly string PackageSource;
        public readonly string PackageName;
        public readonly string PackageVersionOpt;

        #endregion

        private AddImportFixData(
            Document contextDocument,
            ImmutableArray<TextChange> textChanges,
            string title,
            ImmutableArray<string> tags,
            CodeActionPriority priority)
        {
            OriginalDocument = contextDocument;
            TextChanges = textChanges;
            Title = title;
            Tags = tags;
            Priority = priority;
        }

        public AddImportFixData(Document contextDocument, ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId projectReferenceToAdd)
            : this(contextDocument, textChanges, title, tags, priority)
        {
            ProjectReferenceToAdd = projectReferenceToAdd;
        }

        public AddImportFixData(Document contextDocument, ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId portableExecutableReferenceProjectId, string portableExecutableReferenceFilePathToAdd)
            : this(contextDocument, textChanges, title, tags, priority)
        {
            PortableExecutableReferenceProjectId = portableExecutableReferenceProjectId;
            PortableExecutableReferenceFilePathToAdd = portableExecutableReferenceFilePathToAdd;
        }

        public AddImportFixData(Document contextDocument, ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, string assemblyReferenceAssemblyName, string assemblyReferenceFullyQualifiedTypeName)
            : this(contextDocument, textChanges, title, tags, priority)
        {
            AssemblyReferenceAssemblyName = assemblyReferenceAssemblyName;
            AssemblyReferenceFullyQualifiedTypeName = assemblyReferenceFullyQualifiedTypeName;
        }

        public AddImportFixData(Document contextDocument, ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, string packageSource, string packageName, string packageVersionOpt)
            : this(contextDocument, textChanges, title, tags, priority)
        {
            PackageSource = packageSource;
            PackageName = packageName;
            PackageVersionOpt = packageVersionOpt;
        }
    }
}
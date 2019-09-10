// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal class AddImportFixData
    {
        public AddImportFixKind Kind { get; }

        /// <summary>
        /// Text changes to make to the document.  Usually just the import to add.  May also
        /// include a change to the name node the feature was invoked on to fix the casing of it.
        /// May be empty for fixes that don't need to add an import and only do something like
        /// add a project/metadata reference.
        /// </summary>
        public IList<TextChange> TextChanges { get; }

        /// <summary>
        /// String to display in the lightbulb menu.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Tags that control what glyph is displayed in the lightbulb menu.
        /// </summary>
        public IList<string> Tags { get; private set; }

        /// <summary>
        /// The priority this item should have in the lightbulb list.
        /// </summary>
        public CodeActionPriority Priority { get; private set; }

        #region When adding P2P refrences.

        /// <summary>
        /// The optional id for a <see cref="Project"/> we'd like to add a reference to.
        /// </summary>
        public ProjectId ProjectReferenceToAdd { get; private set; }

        #endregion

        #region When adding a metadata reference

        /// <summary>
        /// If we're adding <see cref="PortableExecutableReferenceFilePathToAdd"/> then this
        /// is the id for the <see cref="Project"/> we can find that <see cref="PortableExecutableReference"/>
        /// referenced from.
        /// </summary>
        public ProjectId PortableExecutableReferenceProjectId { get; private set; }

        /// <summary>
        /// If we want to add a <see cref="PortableExecutableReference"/> metadata reference, this 
        /// is the <see cref="PortableExecutableReference.FilePath"/> for it.
        /// </summary>
        public string PortableExecutableReferenceFilePathToAdd { get; private set; }

        #endregion

        #region When adding an assembly reference

        public string AssemblyReferenceAssemblyName { get; private set; }
        public string AssemblyReferenceFullyQualifiedTypeName { get; private set; }

        #endregion

        #region When adding a package reference

        public string PackageSource { get; private set; }
        public string PackageName { get; private set; }
        public string PackageVersionOpt { get; private set; }

        #endregion

        private AddImportFixData(
            AddImportFixKind kind,
            ImmutableArray<TextChange> textChanges)
        {
            Kind = kind;
            TextChanges = textChanges;
            Tags = ImmutableArray<string>.Empty;
        }

        public static AddImportFixData CreateForProjectSymbol(ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId projectReferenceToAdd)
        {
            return new AddImportFixData(AddImportFixKind.ProjectSymbol, textChanges)
            {
                Title = title,
                Tags = tags,
                Priority = priority,
                ProjectReferenceToAdd = projectReferenceToAdd
            };
        }

        public static AddImportFixData CreateForMetadataSymbol(ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId portableExecutableReferenceProjectId, string portableExecutableReferenceFilePathToAdd)
        {
            return new AddImportFixData(AddImportFixKind.MetadataSymbol, textChanges)
            {
                Title = title,
                Tags = tags,
                Priority = priority,
                PortableExecutableReferenceProjectId = portableExecutableReferenceProjectId,
                PortableExecutableReferenceFilePathToAdd = portableExecutableReferenceFilePathToAdd
            };
        }

        public static AddImportFixData CreateForReferenceAssemblySymbol(ImmutableArray<TextChange> textChanges, string title, string assemblyReferenceAssemblyName, string assemblyReferenceFullyQualifiedTypeName)
        {
            return new AddImportFixData(AddImportFixKind.ReferenceAssemblySymbol, textChanges)
            {
                Title = title,
                Tags = WellKnownTagArrays.AddReference,
                Priority = CodeActionPriority.Low,
                AssemblyReferenceAssemblyName = assemblyReferenceAssemblyName,
                AssemblyReferenceFullyQualifiedTypeName = assemblyReferenceFullyQualifiedTypeName
            };
        }

        public static AddImportFixData CreateForPackageSymbol(ImmutableArray<TextChange> textChanges, string packageSource, string packageName, string packageVersionOpt)
        {
            return new AddImportFixData(AddImportFixKind.PackageSymbol, textChanges)
            {
                PackageSource = packageSource,
                Priority = CodeActionPriority.Low,
                PackageName = packageName,
                PackageVersionOpt = packageVersionOpt,
            };
        }
    }
}

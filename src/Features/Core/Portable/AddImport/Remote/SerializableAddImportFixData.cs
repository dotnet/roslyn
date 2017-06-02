// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal class SerializableAddImportFixData
    {
        public AddImportFixKind Kind;

        /// <summary>
        /// Text changes to make to the document.  Usually just the import to add.  May also
        /// include a change to the name node the feature was invoked on to fix the casing of it.
        /// May be empty for fixes that don't need to add an import and only do something like
        /// add a project/metadata reference.
        /// </summary>
        public TextChange[] TextChanges;

        /// <summary>
        /// String to display in the lightbulb menu.
        /// </summary>
        public string Title;

        /// <summary>
        /// Tags that control what glyph is displayed in the lightbulb menu.
        /// </summary>
        public string[] Tags;

        /// <summary>
        /// The priority this item should have in the lightbulb list.
        /// </summary>
        public CodeActionPriority Priority;

        #region When adding P2P refrences.

        /// <summary>
        /// The optional id for a <see cref="Project"/> we'd like to add a reference to.
        /// </summary>
        public ProjectId ProjectReferenceToAdd;

        #endregion

        #region When adding a metadata reference

        /// <summary>
        /// If we're adding <see cref="PortableExecutableReferenceFilePathToAdd"/> then this
        /// is the id for the <see cref="Project"/> we can find that <see cref="PortableExecutableReference"/>
        /// referenced from.
        /// </summary>
        public ProjectId PortableExecutableReferenceProjectId;

        /// <summary>
        /// If we want to add a <see cref="PortableExecutableReference"/> metadata reference, this 
        /// is the <see cref="PortableExecutableReference.FilePath"/> for it.
        /// </summary>
        public string PortableExecutableReferenceFilePathToAdd;

        #endregion

        #region When adding an assembly reference

        public string AssemblyReferenceAssemblyName;
        public string AssemblyReferenceFullyQualifiedTypeName;

        #endregion

        #region When adding a package reference

        public string PackageSource;
        public string PackageName;
        public string PackageVersionOpt;

        #endregion

        public AddImportFixData Rehydrate()
        {
            switch (Kind)
            {
                case AddImportFixKind.ProjectSymbol:
                    return AddImportFixData.CreateForProjectSymbol(
                        TextChanges.ToImmutableArray(), Title, Tags.ToImmutableArray(),
                        Priority, ProjectReferenceToAdd);

                case AddImportFixKind.MetadataSymbol:
                    return AddImportFixData.CreateForMetadataSymbol(
                        TextChanges.ToImmutableArray(), Title, Tags.ToImmutableArray(),
                        Priority, PortableExecutableReferenceProjectId, PortableExecutableReferenceFilePathToAdd);

                case AddImportFixKind.PackageSymbol:
                    return AddImportFixData.CreateForPackageSymbol(
                        TextChanges.ToImmutableArray(), PackageSource, PackageName, PackageVersionOpt);

                case AddImportFixKind.ReferenceAssemblySymbol:
                    return AddImportFixData.CreateForReferenceAssemblySymbol(
                        TextChanges.ToImmutableArray(), Title, 
                        AssemblyReferenceAssemblyName, AssemblyReferenceFullyQualifiedTypeName);
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
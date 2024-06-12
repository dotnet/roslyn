// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    [DataContract]
    internal sealed class AddImportFixData(
        AddImportFixKind kind,
        ImmutableArray<TextChange> textChanges,
        string title = null,
        ImmutableArray<string> tags = default,
        CodeActionPriority priority = default,
        ProjectId projectReferenceToAdd = null,
        ProjectId portableExecutableReferenceProjectId = null,
        string portableExecutableReferenceFilePathToAdd = null,
        string assemblyReferenceAssemblyName = null,
        string assemblyReferenceFullyQualifiedTypeName = null,
        string packageSource = null,
        string packageName = null,
        string packageVersionOpt = null)
    {
        [DataMember(Order = 0)]
        public AddImportFixKind Kind { get; } = kind;

        /// <summary>
        /// Text changes to make to the document.  Usually just the import to add.  May also
        /// include a change to the name node the feature was invoked on to fix the casing of it.
        /// May be empty for fixes that don't need to add an import and only do something like
        /// add a project/metadata reference.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly ImmutableArray<TextChange> TextChanges = textChanges;

        /// <summary>
        /// String to display in the lightbulb menu.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly string Title = title;

        /// <summary>
        /// Tags that control what glyph is displayed in the lightbulb menu.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly ImmutableArray<string> Tags = tags;

        /// <summary>
        /// The priority this item should have in the lightbulb list.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly CodeActionPriority Priority = priority;

        #region When adding P2P references.

        /// <summary>
        /// The optional id for a <see cref="Project"/> we'd like to add a reference to.
        /// </summary>
        [DataMember(Order = 5)]
        public readonly ProjectId ProjectReferenceToAdd = projectReferenceToAdd;

        #endregion

        #region When adding a metadata reference

        /// <summary>
        /// If we're adding <see cref="PortableExecutableReferenceFilePathToAdd"/> then this
        /// is the id for the <see cref="Project"/> we can find that <see cref="PortableExecutableReference"/>
        /// referenced from.
        /// </summary>
        [DataMember(Order = 6)]
        public readonly ProjectId PortableExecutableReferenceProjectId = portableExecutableReferenceProjectId;

        /// <summary>
        /// If we want to add a <see cref="PortableExecutableReference"/> metadata reference, this 
        /// is the <see cref="PortableExecutableReference.FilePath"/> for it.
        /// </summary>
        [DataMember(Order = 7)]
        public readonly string PortableExecutableReferenceFilePathToAdd = portableExecutableReferenceFilePathToAdd;

        #endregion

        #region When adding an assembly reference

        [DataMember(Order = 8)]
        public readonly string AssemblyReferenceAssemblyName = assemblyReferenceAssemblyName;

        [DataMember(Order = 9)]
        public readonly string AssemblyReferenceFullyQualifiedTypeName = assemblyReferenceFullyQualifiedTypeName;

        #endregion

        #region When adding a package reference

        [DataMember(Order = 10)]
        public readonly string PackageSource = packageSource;

        [DataMember(Order = 11)]
        public readonly string PackageName = packageName;

        [DataMember(Order = 12)]
        public readonly string PackageVersionOpt = packageVersionOpt;

        #endregion

        public static AddImportFixData CreateForProjectSymbol(ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId projectReferenceToAdd)
            => new(AddImportFixKind.ProjectSymbol,
                   textChanges,
                   title: title,
                   tags: tags,
                   priority: priority,
                   projectReferenceToAdd: projectReferenceToAdd);

        public static AddImportFixData CreateForMetadataSymbol(ImmutableArray<TextChange> textChanges, string title, ImmutableArray<string> tags, CodeActionPriority priority, ProjectId portableExecutableReferenceProjectId, string portableExecutableReferenceFilePathToAdd)
            => new(AddImportFixKind.MetadataSymbol,
                   textChanges,
                   title: title,
                   tags: tags,
                   priority: priority,
                   portableExecutableReferenceProjectId: portableExecutableReferenceProjectId,
                   portableExecutableReferenceFilePathToAdd: portableExecutableReferenceFilePathToAdd);

        public static AddImportFixData CreateForReferenceAssemblySymbol(ImmutableArray<TextChange> textChanges, string title, string assemblyReferenceAssemblyName, string assemblyReferenceFullyQualifiedTypeName)
            => new(AddImportFixKind.ReferenceAssemblySymbol,
                   textChanges,
                   title: title,
                   tags: WellKnownTagArrays.AddReference,
                   priority: CodeActionPriority.Low,
                   assemblyReferenceAssemblyName: assemblyReferenceAssemblyName,
                   assemblyReferenceFullyQualifiedTypeName: assemblyReferenceFullyQualifiedTypeName);

        public static AddImportFixData CreateForPackageSymbol(ImmutableArray<TextChange> textChanges, string packageSource, string packageName, string packageVersionOpt)
            => new(AddImportFixKind.PackageSymbol,
                   textChanges,
                   packageSource: packageSource,
                   priority: CodeActionPriority.Low,
                   packageName: packageName,
                   packageVersionOpt: packageVersionOpt);
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class MiscellaneousFilesWorkspace
    {
        private class HostProject : IVisualStudioHostProject
        {
            public ProjectId Id { get; }
            public string Language { get; }

            internal IVisualStudioHostDocument Document { get; set; }

            private readonly string _assemblyName;
            private readonly ParseOptions _parseOptions;
            private readonly IEnumerable<MetadataReference> _metadataReferences;
            private readonly VersionStamp _version;
            private readonly Workspace _workspace;

            public HostProject(Workspace workspace, SolutionId solutionId, string languageName, ParseOptions parseOptions, IEnumerable<MetadataReference> metadataReferences)
            {
                Debug.Assert(workspace != null);
                Debug.Assert(languageName != null);
                Debug.Assert(parseOptions != null);
                Debug.Assert(metadataReferences != null);

                _workspace = workspace;
                this.Id = ProjectId.CreateNewId(debugName: "Miscellaneous Files");
                this.Language = languageName;
                _parseOptions = parseOptions;

                // the assembly name must be unique for each collection of loose files.  since the name doesn't matter
                // a random GUID can be used.
                _assemblyName = Guid.NewGuid().ToString("N");

                _version = VersionStamp.Create();
                _metadataReferences = metadataReferences;
            }

            public ProjectInfo CreateProjectInfoForCurrentState()
            {
                return ProjectInfo.Create(
                    this.Id,
                    _version,
                    name: ServicesVSResources.MiscellaneousFiles,
                    assemblyName: _assemblyName,
                    language: this.Language,
                    filePath: null,
                    compilationOptions: null, // we don't have compilation options?
                    parseOptions: _parseOptions,
                    documents: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectReference>(),
                    metadataReferences: _metadataReferences,
                    isSubmission: false,
                    hostObjectType: null);
            }

            public Microsoft.VisualStudio.Shell.Interop.IVsHierarchy Hierarchy => null;

            public Guid Guid => Guid.Empty;

            public string ProjectType => Constants.vsProjectKindMisc;

            public Workspace Workspace => _workspace;

            public string ProjectSystemName => "MiscellaneousFiles";

            public IVisualStudioHostDocument GetDocumentOrAdditionalDocument(DocumentId id)
            {
                if (id == this.Document.Id)
                {
                    return this.Document;
                }
                else
                {
                    return null;
                }
            }

            public IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath)
            {
                if (Document == null || !filePath.Equals(Document.Key.Moniker, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return Document;
            }

            public bool ContainsFile(string moniker)
            {
                // We may have created our project but haven't created the document yet for it
                if (Document == null)
                {
                    return false;
                }

                return moniker.Equals(Document.Key.Moniker, StringComparison.OrdinalIgnoreCase);
            }

            public IReadOnlyList<string> GetFolderNames(uint documentItemID)
            {
                return SpecializedCollections.EmptyReadOnlyList<string>();
            }
        }
    }
}

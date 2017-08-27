// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class MiscellaneousFilesWorkspace
    {
        private sealed class HostProject : IVisualStudioHostProject
        {
            public ProjectId Id { get; }
            public string Language { get; }

            internal IVisualStudioHostDocument Document { get; set; }

            private readonly string _assemblyName;
            private readonly ParseOptions _parseOptionsOpt;
            private readonly CompilationOptions _compilationOptionsOpt;
            private readonly IEnumerable<MetadataReference> _metadataReferences;
            private readonly VersionStamp _version;
            private readonly Workspace _workspace;

            public HostProject(Workspace workspace, SolutionId solutionId, string languageName, ParseOptions parseOptionsOpt, CompilationOptions compilationOptionsOpt, IEnumerable<MetadataReference> metadataReferences)
            {
                Debug.Assert(workspace != null);
                Debug.Assert(languageName != null);
                Debug.Assert(metadataReferences != null);

                _workspace = workspace;
                _parseOptionsOpt = parseOptionsOpt;
                _compilationOptionsOpt = compilationOptionsOpt;

                Id = ProjectId.CreateNewId(debugName: "Miscellaneous Files");
                Language = languageName;

                // the assembly name must be unique for each collection of loose files.  since the name doesn't matter
                // a random GUID can be used.
                _assemblyName = Guid.NewGuid().ToString("N");

                _version = VersionStamp.Create();
                _metadataReferences = metadataReferences;
            }

            public ProjectInfo CreateProjectInfoForCurrentState()
            {
                var info = ProjectInfo.Create(
                    Id,
                    _version,
                    name: ServicesVSResources.Miscellaneous_Files,
                    assemblyName: _assemblyName,
                    language: Language,
                    filePath: null,
                    compilationOptions: _compilationOptionsOpt,
                    parseOptions: _parseOptionsOpt,
                    documents: SpecializedCollections.EmptyEnumerable<DocumentInfo>(),
                    projectReferences: SpecializedCollections.EmptyEnumerable<ProjectReference>(),
                    metadataReferences: _metadataReferences,
                    isSubmission: false,
                    hostObjectType: null);

                // misc project will never be fully loaded since, by defintion, it won't know
                // what the full set of information is.
                return info.WithHasAllInformation(hasAllInformation: false);
            }

            public Microsoft.VisualStudio.Shell.Interop.IVsHierarchy Hierarchy => null;

            public Guid Guid => Guid.Empty;

            public string ProjectType => Constants.vsProjectKindMisc;

            public Workspace Workspace => _workspace;

            public string DisplayName => "MiscellaneousFiles";
            public string ProjectSystemName => DisplayName;

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
        }
    }
}

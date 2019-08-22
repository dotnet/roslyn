// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class GeneralWorkspaceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDocumentContent_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));

            var changedDoc = originalDoc.WithText(SourceText.From("new"));

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution));
            Assert.Equal(WorkspacesResources.Changing_documents_is_not_supported, exception.Message);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDocumentName_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal(originalDoc.Name, "TestDocument");

            var newName = "ChangedName";
            var changedDoc = originalDoc.WithName(newName);
            Assert.Equal(newName, changedDoc.Name);

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution));
            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported, exception.Message);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDocumentFolders_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));

            Assert.Equal(0, originalDoc.Folders.Count);

            var changedDoc = originalDoc.WithFolders(new[] { "A", "B" });
            Assert.Equal(2, changedDoc.Folders.Count);
            Assert.Equal("A", changedDoc.Folders[0]);
            Assert.Equal("B", changedDoc.Folders[1]);

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution));
            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported, exception.Message);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDocumentFilePath_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Null(originalDoc.FilePath);

            var newPath = @"\goo\TestDocument.cs";
            var changedDoc = originalDoc.WithFilePath(newPath);
            Assert.Equal(newPath, changedDoc.FilePath);

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution));
            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported, exception.Message);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDocumentSourceCodeKind_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal(SourceCodeKind.Regular, originalDoc.SourceCodeKind);

            var changedDoc = originalDoc.WithSourceCodeKind(SourceCodeKind.Script);
            Assert.Equal(SourceCodeKind.Script, changedDoc.SourceCodeKind);

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution));
            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported, exception.Message);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestChangeDefaultNamespace_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).WithDefaultNamespace("OriginalName").Id;

            var newName = "ChangedName";
            var newSolution = ws.CurrentSolution.WithProjectDefaultNamespace(projectId, newName);

            var exception = Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(newSolution));
            Assert.Equal(WorkspacesResources.Changing_project_properties_is_not_supported, exception.Message);
        }

        private class NoChangesAllowedWorkspace : Workspace
        {
            public NoChangesAllowedWorkspace(HostServices services, string workspaceKind = "Custom")
                : base(services, workspaceKind)
            {
            }

            public NoChangesAllowedWorkspace()
                : this(Host.Mef.MefHostServices.DefaultHost)
            {
            }

            public override bool CanApplyChange(ApplyChangesKind feature)
            {
                return false;
            }

            public Project AddProject(string name, string language)
            {
                var info = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), name, name, language);
                return this.AddProject(info);
            }

            public Project AddProject(ProjectInfo projectInfo)
            {
                if (projectInfo == null)
                {
                    throw new ArgumentNullException(nameof(projectInfo));
                }

                this.OnProjectAdded(projectInfo);

                this.UpdateReferencesAfterAdd();

                return this.CurrentSolution.GetProject(projectInfo.Id);
            }

            public Document AddDocument(ProjectId projectId, string name, SourceText text)
            {
                if (projectId == null)
                {
                    throw new ArgumentNullException(nameof(projectId));
                }

                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                if (text == null)
                {
                    throw new ArgumentNullException(nameof(text));
                }

                var id = DocumentId.CreateNewId(projectId);
                var loader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));

                return this.AddDocument(DocumentInfo.Create(id, name, loader: loader));
            }

            /// <summary>
            /// Adds a document to the workspace.
            /// </summary>
            public Document AddDocument(DocumentInfo documentInfo)
            {
                if (documentInfo == null)
                {
                    throw new ArgumentNullException(nameof(documentInfo));
                }

                this.OnDocumentAdded(documentInfo);

                return this.CurrentSolution.GetDocument(documentInfo.Id);
            }
        }
    }
}

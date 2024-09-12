// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Editor.UnitTests.Formating;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider, Trait(Traits.Feature, Traits.Features.Workspace)]
    public class WorkspaceTests
    {
        [Fact]
        public void TestChangeDocumentContent_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));

            var changedDoc = originalDoc.WithText(SourceText.From("new"));

            Assert.Equal(WorkspacesResources.Changing_documents_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution)).Message);
        }

        [Fact]
        public void TestChangeDocumentName_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal("TestDocument", originalDoc.Name);

            var newName = "ChangedName";
            var changedDoc = originalDoc.WithName(newName);
            Assert.Equal(newName, changedDoc.Name);

            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution)).Message);
        }

        [Fact]
        public void TestChangeDocumentFolders_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));

            Assert.Equal(0, originalDoc.Folders.Count);

            var changedDoc = originalDoc.WithFolders(["A", "B"]);
            Assert.Equal(2, changedDoc.Folders.Count);
            Assert.Equal("A", changedDoc.Folders[0]);
            Assert.Equal("B", changedDoc.Folders[1]);

            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution)).Message);
        }

        [Fact]
        public void TestChangeDocumentFilePath_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Null(originalDoc.FilePath);

            var newPath = @"\goo\TestDocument.cs";
            var changedDoc = originalDoc.WithFilePath(newPath);
            Assert.Equal(newPath, changedDoc.FilePath);

            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution)).Message);
        }

        [Fact]
        public void TestChangeDocumentSourceCodeKind_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal(SourceCodeKind.Regular, originalDoc.SourceCodeKind);

            var changedDoc = originalDoc.WithSourceCodeKind(SourceCodeKind.Script);
            Assert.Equal(SourceCodeKind.Script, changedDoc.SourceCodeKind);

            Assert.Equal(WorkspacesResources.Changing_document_property_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(changedDoc.Project.Solution)).Message);
        }

        [Fact]
        public void WithAnalyzerReferences_TryApplyChanges_Throws()
        {
            using var ws = new NoChangesAllowedWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var newSolution = ws.CurrentSolution.WithAnalyzerReferences(new[] { new TestAnalyzerReference() });

            Assert.Equal(WorkspacesResources.Adding_analyzer_references_is_not_supported,
                Assert.Throws<NotSupportedException>(() => ws.TryApplyChanges(newSolution)).Message);
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
                => false;

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

        [Fact, Obsolete("Testing obsolete API")]
        public void SetOptions_PublicGlobalOptions()
        {
            using var workspace1 = new AdhocWorkspace();
            var solution = workspace1.CurrentSolution;

            var newOptions = OptionsTestHelpers.GetOptionSetWithChangedOptions(solution.Options, OptionsTestHelpers.AllPublicOptionsWithNonDefaultValues);

            // Sets options to global options that are shared among all workspaces:
            workspace1.Options = newOptions;

            using var workspace2 = new AdhocWorkspace();
            foreach (var (option, value) in OptionsTestHelpers.AllPublicOptionsWithNonDefaultValues)
            {
                foreach (var language in OptionsTestHelpers.GetApplicableLanguages(option))
                {
                    Assert.Equal(value, workspace2.Options.GetOption(new OptionKey(option, language)));
                }
            }
        }
    }
}

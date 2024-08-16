// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Workspace)]
    public partial class AdhocWorkspaceTests : TestBase
    {
        [Fact]
        public void TestAddProject_ProjectInfo()
        {
            var info = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp);

            using var ws = new AdhocWorkspace();
            var project = ws.AddProject(info);
            Assert.Equal(project, ws.CurrentSolution.Projects.FirstOrDefault());
            Assert.Equal(info.Name, project.Name);
            Assert.Equal(info.Id, project.Id);
            Assert.Equal(info.AssemblyName, project.AssemblyName);
            Assert.Equal(info.Language, project.Language);
        }

        [Fact]
        public void TestAddProject_NameAndLanguage()
        {
            using var ws = new AdhocWorkspace();
            var project = ws.AddProject("TestProject", LanguageNames.CSharp);
            Assert.Same(project, ws.CurrentSolution.Projects.FirstOrDefault());
            Assert.Equal("TestProject", project.Name);
            Assert.Equal(LanguageNames.CSharp, project.Language);
        }

        [Fact]
        public void TestAddDocument_DocumentInfo()
        {
            using var ws = new AdhocWorkspace();
            var project = ws.AddProject("TestProject", LanguageNames.CSharp);
            var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
            var doc = ws.AddDocument(info);

            Assert.Equal(ws.CurrentSolution.GetDocument(info.Id), doc);
            Assert.Equal(info.Name, doc.Name);
        }

        [Fact]
        public async Task TestAddDocument_NameAndTextAsync()
        {
            using var ws = new AdhocWorkspace();
            var project = ws.AddProject("TestProject", LanguageNames.CSharp);
            var name = "code.cs";
            var source = "class C {}";
            var doc = ws.AddDocument(project.Id, name, SourceText.From(source));

            Assert.Equal(name, doc.Name);
            Assert.Equal(source, (await doc.GetTextAsync()).ToString());
        }

        [Fact]
        public void TestAddSolution_SolutionInfo()
        {
            using var ws = new AdhocWorkspace();
            var pinfo = ProjectInfo.Create(
ProjectId.CreateNewId(),
version: VersionStamp.Default,
name: "TestProject",
assemblyName: "TestProject.dll",
language: LanguageNames.CSharp);

            var sinfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, projects: new ProjectInfo[] { pinfo });

            var solution = ws.AddSolution(sinfo);

            Assert.Same(ws.CurrentSolution, solution);
            Assert.Equal(solution.Id, sinfo.Id);

            Assert.Equal(sinfo.Projects.Count, solution.ProjectIds.Count);
            var project = solution.Projects.FirstOrDefault();
            Assert.NotNull(project);
            Assert.Equal(pinfo.Name, project.Name);
            Assert.Equal(pinfo.Id, project.Id);
            Assert.Equal(pinfo.AssemblyName, project.AssemblyName);
            Assert.Equal(pinfo.Language, project.Language);
        }

        [Fact]
        public void TestAddProjects()
        {
            var id1 = ProjectId.CreateNewId();
            var info1 = ProjectInfo.Create(
                id1,
                version: VersionStamp.Default,
                name: "TestProject1",
                assemblyName: "TestProject1.dll",
                language: LanguageNames.CSharp);

            var id2 = ProjectId.CreateNewId();
            var info2 = ProjectInfo.Create(
                id2,
                version: VersionStamp.Default,
                name: "TestProject2",
                assemblyName: "TestProject2.dll",
                language: LanguageNames.VisualBasic,
                projectReferences: [new ProjectReference(id1)]);

            using var ws = new AdhocWorkspace();
            ws.AddProjects([info1, info2]);
            var solution = ws.CurrentSolution;
            Assert.Equal(2, solution.ProjectIds.Count);

            var project1 = solution.GetProject(id1);
            Assert.Equal(info1.Name, project1.Name);
            Assert.Equal(info1.Id, project1.Id);
            Assert.Equal(info1.AssemblyName, project1.AssemblyName);
            Assert.Equal(info1.Language, project1.Language);

            var project2 = solution.GetProject(id2);
            Assert.Equal(info2.Name, project2.Name);
            Assert.Equal(info2.Id, project2.Id);
            Assert.Equal(info2.AssemblyName, project2.AssemblyName);
            Assert.Equal(info2.Language, project2.Language);
            Assert.Equal(1, project2.ProjectReferences.Count());
            Assert.Equal(id1, project2.ProjectReferences.First().ProjectId);
        }

        [Fact]
        public void TestAddProject_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var pid = ProjectId.CreateNewId();

            var docInfo = DocumentInfo.Create(
                            DocumentId.CreateNewId(pid),
                            "MyDoc.cs",
                            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create())));

            var projInfo = ProjectInfo.Create(
                    pid,
                    VersionStamp.Create(),
                    "NewProject",
                    "NewProject.dll",
                    LanguageNames.CSharp,
                    documents: [docInfo]);

            var newSolution = ws.CurrentSolution.AddProject(projInfo);

            Assert.Equal(0, ws.CurrentSolution.Projects.Count());

            var result = ws.TryApplyChanges(newSolution);
            Assert.True(result);

            Assert.Equal(1, ws.CurrentSolution.Projects.Count());
            var proj = ws.CurrentSolution.Projects.First();

            Assert.Equal("NewProject", proj.Name);
            Assert.Equal("NewProject.dll", proj.AssemblyName);
            Assert.Equal(LanguageNames.CSharp, proj.Language);
            Assert.Equal(1, proj.Documents.Count());

            var doc = proj.Documents.First();
            Assert.Equal("MyDoc.cs", doc.Name);
        }

        [Fact]
        public void TestRemoveProject_TryApplyChanges()
        {
            var pid = ProjectId.CreateNewId();
            var info = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp);

            using var ws = new AdhocWorkspace();
            ws.AddProject(info);

            Assert.Equal(1, ws.CurrentSolution.Projects.Count());

            var newSolution = ws.CurrentSolution.RemoveProject(pid);
            Assert.Equal(0, newSolution.Projects.Count());

            var result = ws.TryApplyChanges(newSolution);
            Assert.True(result);

            Assert.Equal(0, ws.CurrentSolution.Projects.Count());
        }

        [Fact]
        public void TestOpenCloseDocument()
        {
            var pid = ProjectId.CreateNewId();
            var text = SourceText.From("public class C { }");
            var version = VersionStamp.Create();
            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(pid), "c.cs", loader: TextLoader.From(TextAndVersion.Create(text, version)));
            var projInfo = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp,
                documents: [docInfo]);

            using var ws = new AdhocWorkspace();
            ws.AddProject(projInfo);
            var doc = ws.CurrentSolution.GetDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out var currentText));

            ws.OpenDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetDocument(docInfo.Id);
            Assert.True(doc.TryGetText(out currentText));
            Assert.True(doc.TryGetTextVersion(out var currentVersion));
            Assert.Same(text, currentText);
            Assert.Equal(version, currentVersion);

            ws.CloseDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out currentText));
        }

        [Fact]
        public void TestOpenCloseAdditionalDocument()
        {
            var pid = ProjectId.CreateNewId();
            var text = SourceText.From("public class C { }");
            var version = VersionStamp.Create();
            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(pid), "c.cs", loader: TextLoader.From(TextAndVersion.Create(text, version)));
            var projInfo = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp,
                additionalDocuments: [docInfo]);

            using var ws = new AdhocWorkspace();
            ws.AddProject(projInfo);
            var doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out var currentText));

            ws.OpenAdditionalDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
            Assert.True(doc.TryGetText(out currentText));
            Assert.True(doc.TryGetTextVersion(out var currentVersion));
            Assert.Same(text, currentText);
            Assert.Equal(version, currentVersion);

            ws.CloseAdditionalDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out currentText));
        }

        [Fact]
        public void TestOpenCloseAnnalyzerConfigDocument()
        {
            var pid = ProjectId.CreateNewId();
            var text = SourceText.From("public class C { }");
            var version = VersionStamp.Create();
            var analyzerConfigDocFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(Temp.CreateDirectory().Path, ".editorconfig");
            var docInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(pid),
                    name: ".editorconfig",
                    loader: TextLoader.From(TextAndVersion.Create(text, version, analyzerConfigDocFilePath)),
                    filePath: analyzerConfigDocFilePath);
            var projInfo = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp)
                .WithAnalyzerConfigDocuments([docInfo]);

            using var ws = new AdhocWorkspace();
            ws.AddProject(projInfo);
            var doc = ws.CurrentSolution.GetAnalyzerConfigDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out var currentText));

            ws.OpenAnalyzerConfigDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetAnalyzerConfigDocument(docInfo.Id);
            Assert.True(doc.TryGetText(out currentText));
            Assert.True(doc.TryGetTextVersion(out var currentVersion));
            Assert.Same(text, currentText);
            Assert.Equal(version, currentVersion);

            ws.CloseAnalyzerConfigDocument(docInfo.Id);

            doc = ws.CurrentSolution.GetAnalyzerConfigDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out currentText));
        }

        [Fact]
        public async Task TestUpdatedDocumentHasTextVersionAsync()
        {
            var pid = ProjectId.CreateNewId();
            var text = SourceText.From("public class C { }");
            var version = VersionStamp.Create();
            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(pid), "c.cs", loader: TextLoader.From(TextAndVersion.Create(text, version)));
            var projInfo = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp,
                documents: [docInfo]);

            using var ws = new AdhocWorkspace();
            ws.AddProject(projInfo);
            var doc = ws.CurrentSolution.GetDocument(docInfo.Id);
            Assert.False(doc.TryGetText(out var currentText));
            Assert.False(doc.TryGetTextVersion(out var currentVersion));

            // cause text to load and show that TryGet now works for text and version
            currentText = await doc.GetTextAsync();
            Assert.True(doc.TryGetText(out currentText));
            Assert.True(doc.TryGetTextVersion(out currentVersion));
            Assert.Equal(version, currentVersion);

            // change document
            var root = await doc.GetSyntaxRootAsync();
            var newRoot = root.WithAdditionalAnnotations(new SyntaxAnnotation());
            Assert.NotSame(root, newRoot);
            var newDoc = doc.WithSyntaxRoot(newRoot);
            Assert.NotSame(doc, newDoc);

            // text is now unavailable since it must be constructed from tree
            Assert.False(newDoc.TryGetText(out currentText));

            // version is available because it is cached
            Assert.True(newDoc.TryGetTextVersion(out currentVersion));

            // access it the hard way
            var actualVersion = await newDoc.GetTextVersionAsync();

            // version is the same 
            Assert.Equal(currentVersion, actualVersion);

            // accessing text version did not cause text to be constructed.
            Assert.False(newDoc.TryGetText(out currentText));

            // now access text directly (force it to be constructed)
            var actualText = await newDoc.GetTextAsync();
            actualVersion = await newDoc.GetTextVersionAsync();

            // prove constructing text did not introduce a new version
            Assert.Equal(currentVersion, actualVersion);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1174396")]
        public async Task TestUpdateCSharpLanguageVersionAsync()
        {
            using var ws = new AdhocWorkspace();
            var projid = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var docid1 = ws.AddDocument(projid, "A.cs", SourceText.From("public class A { }")).Id;
            var docid2 = ws.AddDocument(projid, "B.cs", SourceText.From("public class B { }")).Id;

            var pws = new WorkspaceWithPartialSemantics(ws.CurrentSolution);
            var proj = pws.CurrentSolution.GetProject(projid);
            var comp = await proj.GetCompilationAsync();

            // change language version
            var parseOptions = proj.ParseOptions as CS.CSharpParseOptions;
            pws.SetParseOptions(projid, parseOptions.WithLanguageVersion(CS.LanguageVersion.CSharp3));

            // get partial semantics doc
            var frozen = pws.CurrentSolution.GetDocument(docid1).WithFrozenPartialSemantics(CancellationToken.None);
        }

        public class WorkspaceWithPartialSemantics : Workspace
        {
            public WorkspaceWithPartialSemantics(Solution solution)
                : base(solution.Workspace.Services.HostServices, solution.Workspace.Kind)
            {
                this.SetCurrentSolutionEx(solution);
            }

            protected internal override bool PartialSemanticsEnabled
            {
                get { return true; }
            }

            public void SetParseOptions(ProjectId id, ParseOptions options)
                => base.OnParseOptionsChanged(id, options);
        }

        [Fact]
        public async Task TestChangeDocumentName_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal("TestDocument", originalDoc.Name);

            var newName = "ChangedName";
            var changedDoc = originalDoc.WithName(newName);
            Assert.Equal(newName, changedDoc.Name);

            var tcs = new TaskCompletionSource<bool>();
            ws.WorkspaceChanged += (s, args) =>
            {
                if (args.Kind == WorkspaceChangeKind.DocumentInfoChanged
                    && args.DocumentId == originalDoc.Id)
                {
                    tcs.SetResult(true);
                }
            };

            Assert.True(ws.TryApplyChanges(changedDoc.Project.Solution));

            var appliedDoc = ws.CurrentSolution.GetDocument(originalDoc.Id);
            Assert.Equal(newName, appliedDoc.Name);

            await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.True(tcs.Task.IsCompleted && tcs.Task.Result);
        }

        [Fact]
        public async Task TestChangeDocumentFolders_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));

            Assert.Equal(0, originalDoc.Folders.Count);

            var changedDoc = originalDoc.WithFolders(["A", "B"]);
            Assert.Equal(2, changedDoc.Folders.Count);
            Assert.Equal("A", changedDoc.Folders[0]);
            Assert.Equal("B", changedDoc.Folders[1]);

            var tcs = new TaskCompletionSource<bool>();
            ws.WorkspaceChanged += (s, args) =>
            {
                if (args.Kind == WorkspaceChangeKind.DocumentInfoChanged
                    && args.DocumentId == originalDoc.Id)
                {
                    tcs.SetResult(true);
                }
            };

            Assert.True(ws.TryApplyChanges(changedDoc.Project.Solution));

            var appliedDoc = ws.CurrentSolution.GetDocument(originalDoc.Id);
            Assert.Equal(2, appliedDoc.Folders.Count);
            Assert.Equal("A", appliedDoc.Folders[0]);
            Assert.Equal("B", appliedDoc.Folders[1]);

            await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.True(tcs.Task.IsCompleted && tcs.Task.Result);
        }

        [Fact]
        public async Task TestChangeDocumentFilePath_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Null(originalDoc.FilePath);

            var newPath = @"\goo\TestDocument.cs";
            var changedDoc = originalDoc.WithFilePath(newPath);
            Assert.Equal(newPath, changedDoc.FilePath);

            var tcs = new TaskCompletionSource<bool>();
            ws.WorkspaceChanged += (s, args) =>
            {
                if (args.Kind == WorkspaceChangeKind.DocumentInfoChanged
                    && args.DocumentId == originalDoc.Id)
                {
                    tcs.SetResult(true);
                }
            };

            Assert.True(ws.TryApplyChanges(changedDoc.Project.Solution));

            var appliedDoc = ws.CurrentSolution.GetDocument(originalDoc.Id);
            Assert.Equal(newPath, appliedDoc.FilePath);

            await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.True(tcs.Task.IsCompleted && tcs.Task.Result);
        }

        [Fact]
        public async Task TestChangeDocumentSourceCodeKind_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal(SourceCodeKind.Regular, originalDoc.SourceCodeKind);

            var changedDoc = originalDoc.WithSourceCodeKind(SourceCodeKind.Script);
            Assert.Equal(SourceCodeKind.Script, changedDoc.SourceCodeKind);

            var tcs = new TaskCompletionSource<bool>();
            ws.WorkspaceChanged += (s, args) =>
            {
                if (args.Kind == WorkspaceChangeKind.DocumentInfoChanged
                    && args.DocumentId == originalDoc.Id)
                {
                    tcs.SetResult(true);
                }
            };

            Assert.True(ws.TryApplyChanges(changedDoc.Project.Solution));

            var appliedDoc = ws.CurrentSolution.GetDocument(originalDoc.Id);
            Assert.Equal(SourceCodeKind.Script, appliedDoc.SourceCodeKind);

            await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Assert.True(tcs.Task.IsCompleted && tcs.Task.Result);
        }

        [Fact]
        public void TestChangeDocumentInfo_TryApplyChanges()
        {
            using var ws = new AdhocWorkspace();
            var projectId = ws.AddProject("TestProject", LanguageNames.CSharp).Id;

            var originalDoc = ws.AddDocument(projectId, "TestDocument", SourceText.From(""));
            Assert.Equal("TestDocument", originalDoc.Name);
            Assert.Equal(0, originalDoc.Folders.Count);
            Assert.Null(originalDoc.FilePath);

            var newName = "ChangedName";
            var newPath = @"\A\B\ChangedName.cs";
            var changedDoc = originalDoc.WithName(newName).WithFolders(["A", "B"]).WithFilePath(newPath);

            Assert.Equal(newName, changedDoc.Name);
            Assert.Equal(2, changedDoc.Folders.Count);
            Assert.Equal("A", changedDoc.Folders[0]);
            Assert.Equal("B", changedDoc.Folders[1]);
            Assert.Equal(newPath, changedDoc.FilePath);

            Assert.True(ws.TryApplyChanges(changedDoc.Project.Solution));

            var appliedDoc = ws.CurrentSolution.GetDocument(originalDoc.Id);
            Assert.Equal(newName, appliedDoc.Name);
            Assert.Equal(2, appliedDoc.Folders.Count);
            Assert.Equal("A", appliedDoc.Folders[0]);
            Assert.Equal("B", appliedDoc.Folders[1]);
            Assert.Equal(newPath, appliedDoc.FilePath);
        }

        [Fact]
        public void TestDefaultDocumentTextDifferencingService()
        {
            using var ws = new AdhocWorkspace();
            var service = ws.Services.GetService<IDocumentTextDifferencingService>();
            Assert.NotNull(service);
            Assert.Equal(typeof(DefaultDocumentTextDifferencingService), service.GetType());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/67142")]
        public void TestNotGCRootedOnConstruction()
        {
            var composition = FeaturesTestCompositions.Features;
            var exportProvider = composition.ExportProviderFactory.CreateExportProvider();
            var adhocWorkspaceReference = ObjectReference.CreateFromFactory(
                static composition => new AdhocWorkspace(composition.GetHostServices()),
                composition);

            // Verify the GC can reclaim member for a workspace which has not been disposed.
            adhocWorkspaceReference.AssertReleased();

            // Keep the export provider alive longer than the workspace to further ensure that the workspace is not GC
            // rooted within the export provider instance.
            GC.KeepAlive(exportProvider);
        }
    }
}

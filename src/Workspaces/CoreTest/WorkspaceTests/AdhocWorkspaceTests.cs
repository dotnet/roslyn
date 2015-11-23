// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Shared.Utilities;
using System;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class AdhocWorkspaceTests : WorkspaceTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_ProjectInfo()
        {
            var info = ProjectInfo.Create(
                ProjectId.CreateNewId(),
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp);

            using (var ws = new AdhocWorkspace())
            {
                var project = ws.AddProject(info);
                Assert.Equal(project, ws.CurrentSolution.Projects.FirstOrDefault());
                Assert.Equal(info.Name, project.Name);
                Assert.Equal(info.Id, project.Id);
                Assert.Equal(info.AssemblyName, project.AssemblyName);
                Assert.Equal(info.Language, project.Language);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_NameAndLanguage()
        {
            using (var ws = new AdhocWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                Assert.Same(project, ws.CurrentSolution.Projects.FirstOrDefault());
                Assert.Equal("TestProject", project.Name);
                Assert.Equal(LanguageNames.CSharp, project.Language);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddDocument_DocumentInfo()
        {
            using (var ws = new AdhocWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var doc = ws.AddDocument(info);

                Assert.Equal(ws.CurrentSolution.GetDocument(info.Id), doc);
                Assert.Equal(info.Name, doc.Name);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddDocument_NameAndText()
        {
            using (var ws = new AdhocWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                var name = "code.cs";
                var source = "class C {}";
                var doc = ws.AddDocument(project.Id, name, SourceText.From(source));

                Assert.Equal(name, doc.Name);
                Assert.Equal(source, doc.GetTextAsync().Result.ToString());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddSolution_SolutionInfo()
        {
            using (var ws = new AdhocWorkspace())
            {
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
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
                projectReferences: new[] { new ProjectReference(id1) });

            using (var ws = new AdhocWorkspace())
            {
                ws.AddProjects(new[] { info1, info2 });
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_CommandLineProject()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());

            string commandLine = @"CSharpClass.cs /out:foo.dll /target:library";
            var baseDirectory = Path.Combine(this.SolutionDirectory.Path, "CSharpProject");

            using (var ws = new AdhocWorkspace())
            {
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, baseDirectory);
                ws.AddProject(info);
                var project = ws.CurrentSolution.GetProject(info.Id);

                Assert.Equal("TestProject", project.Name);
                Assert.Equal("foo", project.AssemblyName);
                Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

                Assert.Equal(1, project.Documents.Count());

                var fooDoc = project.Documents.First(d => d.Name == "CSharpClass.cs");
                Assert.Equal(0, fooDoc.Folders.Count);
                var expectedPath = Path.Combine(baseDirectory, "CSharpClass.cs");
                Assert.Equal(expectedPath, fooDoc.FilePath);

                var text = fooDoc.GetTextAsync().Result.ToString();
                Assert.NotEqual("", text);

                var tree = fooDoc.GetSyntaxRootAsync().Result;
                Assert.Equal(false, tree.ContainsDiagnostics);

                var compilation = project.GetCompilationAsync().Result;
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_TryApplyChanges()
        {
            using (var ws = new AdhocWorkspace())
            {
                ProjectId pid = ProjectId.CreateNewId();

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
                        documents: new[] { docInfo });

                var newSolution = ws.CurrentSolution.AddProject(projInfo);

                Assert.Equal(0, ws.CurrentSolution.Projects.Count());

                var result = ws.TryApplyChanges(newSolution);
                Assert.Equal(result, true);

                Assert.Equal(1, ws.CurrentSolution.Projects.Count());
                var proj = ws.CurrentSolution.Projects.First();

                Assert.Equal("NewProject", proj.Name);
                Assert.Equal("NewProject.dll", proj.AssemblyName);
                Assert.Equal(LanguageNames.CSharp, proj.Language);
                Assert.Equal(1, proj.Documents.Count());

                var doc = proj.Documents.First();
                Assert.Equal("MyDoc.cs", doc.Name);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRemoveProject_TryApplyChanges()
        {
            var pid = ProjectId.CreateNewId();
            var info = ProjectInfo.Create(
                pid,
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp);

            using (var ws = new AdhocWorkspace())
            {
                ws.AddProject(info);

                Assert.Equal(1, ws.CurrentSolution.Projects.Count());

                var newSolution = ws.CurrentSolution.RemoveProject(pid);
                Assert.Equal(0, newSolution.Projects.Count());

                var result = ws.TryApplyChanges(newSolution);
                Assert.Equal(true, result);

                Assert.Equal(0, ws.CurrentSolution.Projects.Count());
            }
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
                documents: new[] { docInfo });

            using (var ws = new AdhocWorkspace())
            {
                ws.AddProject(projInfo);

                SourceText currentText;
                VersionStamp currentVersion;

                var doc = ws.CurrentSolution.GetDocument(docInfo.Id);
                Assert.Equal(false, doc.TryGetText(out currentText));

                ws.OpenDocument(docInfo.Id);

                doc = ws.CurrentSolution.GetDocument(docInfo.Id);
                Assert.Equal(true, doc.TryGetText(out currentText));
                Assert.Equal(true, doc.TryGetTextVersion(out currentVersion));
                Assert.Same(text, currentText);
                Assert.Equal(version, currentVersion);

                ws.CloseDocument(docInfo.Id);

                doc = ws.CurrentSolution.GetDocument(docInfo.Id);
                Assert.Equal(false, doc.TryGetText(out currentText));
            }
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
                additionalDocuments: new[] { docInfo });

            using (var ws = new AdhocWorkspace())
            {
                ws.AddProject(projInfo);

                SourceText currentText;
                VersionStamp currentVersion;

                var doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
                Assert.Equal(false, doc.TryGetText(out currentText));

                ws.OpenAdditionalDocument(docInfo.Id);

                doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
                Assert.Equal(true, doc.TryGetText(out currentText));
                Assert.Equal(true, doc.TryGetTextVersion(out currentVersion));
                Assert.Same(text, currentText);
                Assert.Equal(version, currentVersion);

                ws.CloseAdditionalDocument(docInfo.Id);

                doc = ws.CurrentSolution.GetAdditionalDocument(docInfo.Id);
                Assert.Equal(false, doc.TryGetText(out currentText));
            }
        }

        [Fact]
        public void TestGenerateUniqueName()
        {
            var a = NameGenerator.GenerateUniqueName("ABC", "txt", _ => true);
            Assert.True(a.StartsWith("ABC", StringComparison.Ordinal));
            Assert.True(a.EndsWith(".txt", StringComparison.Ordinal));
            Assert.False(a.EndsWith("..txt", StringComparison.Ordinal));

            var b = NameGenerator.GenerateUniqueName("ABC", ".txt", _ => true);
            Assert.True(b.StartsWith("ABC", StringComparison.Ordinal));
            Assert.True(b.EndsWith(".txt", StringComparison.Ordinal));
            Assert.False(b.EndsWith("..txt", StringComparison.Ordinal));

            var c = NameGenerator.GenerateUniqueName("ABC", "\u0640.txt", _ => true);
            Assert.True(c.StartsWith("ABC", StringComparison.Ordinal));
            Assert.True(c.EndsWith(".\u0640.txt", StringComparison.Ordinal));
            Assert.False(c.EndsWith("..txt", StringComparison.Ordinal));
        }

        [Fact]
        public void TestUpdatedDocumentHasTextVersion()
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
                documents: new[] { docInfo });

            using (var ws = new AdhocWorkspace())
            {
                ws.AddProject(projInfo);

                SourceText currentText;
                VersionStamp currentVersion;

                var doc = ws.CurrentSolution.GetDocument(docInfo.Id);
                Assert.Equal(false, doc.TryGetText(out currentText));
                Assert.Equal(false, doc.TryGetTextVersion(out currentVersion));

                // cause text to load and show that TryGet now works for text and version
                currentText = doc.GetTextAsync().Result;
                Assert.Equal(true, doc.TryGetText(out currentText));
                Assert.Equal(true, doc.TryGetTextVersion(out currentVersion));
                Assert.Equal(version, currentVersion);

                // change document
                var root = doc.GetSyntaxRootAsync().Result;
                var newRoot = root.WithAdditionalAnnotations(new SyntaxAnnotation());
                Assert.NotSame(root, newRoot);
                var newDoc = doc.WithSyntaxRoot(newRoot);
                Assert.NotSame(doc, newDoc);

                // text is now unavailable since it must be constructed from tree
                Assert.Equal(false, newDoc.TryGetText(out currentText));

                // version is available because it is cached
                Assert.Equal(true, newDoc.TryGetTextVersion(out currentVersion));

                // access it the hard way
                var actualVersion = newDoc.GetTextVersionAsync().Result;

                // version is the same 
                Assert.Equal(currentVersion, actualVersion);

                // accessing text version did not cause text to be constructed.
                Assert.Equal(false, newDoc.TryGetText(out currentText));

                // now access text directly (force it to be constructed)
                var actualText = newDoc.GetTextAsync().Result;
                actualVersion = newDoc.GetTextVersionAsync().Result;

                // prove constructing text did not introduce a new version
                Assert.Equal(currentVersion, actualVersion);
            }
        }

        private AdhocWorkspace CreateWorkspaceWithRecoverableTrees()
        {
            var ws = new AdhocWorkspace(TestHost.Services, workspaceKind: "NotKeptAlive");
            ws.Options = ws.Options.WithChangedOption(Host.CacheOptions.RecoverableTreeLengthThreshold, 0);
            return ws;
        }

        [Fact]
        public void TestUpdatedDocumentTextIsObservablyConstant()
        {
            CheckUpdatedDocumentTextIsObservablyConstant(new AdhocWorkspace());
            CheckUpdatedDocumentTextIsObservablyConstant(CreateWorkspaceWithRecoverableTrees());
        }

        public void CheckUpdatedDocumentTextIsObservablyConstant(AdhocWorkspace ws)
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
                documents: new[] { docInfo });

            ws.AddProject(projInfo);
            var doc = ws.CurrentSolution.GetDocument(docInfo.Id);

            // change document
            var root = doc.GetSyntaxRootAsync().Result;
            var newRoot = root.WithAdditionalAnnotations(new SyntaxAnnotation());
            Assert.NotSame(root, newRoot);
            var newDoc = doc.Project.Solution.WithDocumentSyntaxRoot(doc.Id, newRoot).GetDocument(doc.Id);
            Assert.NotSame(doc, newDoc);

            var newDocText = newDoc.GetTextAsync().Result;
            var sameText = newDoc.GetTextAsync().Result;
            Assert.Same(newDocText, sameText);

            var newDocTree = newDoc.GetSyntaxTreeAsync().Result;
            var treeText = newDocTree.GetText();
            Assert.Same(newDocText, treeText);
        }

        [WorkItem(1174396, "DevDiv")]
        [Fact]
        public void TestUpdateCSharpLanguageVersion()
        {
            using (var ws = new AdhocWorkspace())
            {
                var projid = ws.AddProject("TestProject", LanguageNames.CSharp).Id;
                var docid1 = ws.AddDocument(projid, "A.cs", SourceText.From("public class A { }")).Id;
                var docid2 = ws.AddDocument(projid, "B.cs", SourceText.From("public class B { }")).Id;

                var pws = new WorkspaceWithPartialSemantics(ws.CurrentSolution);
                var proj = pws.CurrentSolution.GetProject(projid);
                var comp = proj.GetCompilationAsync().Result;

                // change language version
                var parseOptions = proj.ParseOptions as CS.CSharpParseOptions;
                pws.SetParseOptions(projid, parseOptions.WithLanguageVersion(CS.LanguageVersion.CSharp3));

                // get partial semantics doc
                var frozen = pws.CurrentSolution.GetDocument(docid1).WithFrozenPartialSemanticsAsync(CancellationToken.None).Result;
            }
        }

        public class WorkspaceWithPartialSemantics : Workspace
        {
            public WorkspaceWithPartialSemantics(Solution solution)
                : base(solution.Workspace.Services.HostServices, solution.Workspace.Kind)
            {
                this.SetCurrentSolution(solution);
            }

            protected internal override bool PartialSemanticsEnabled
            {
                get { return true; }
            }

            public void SetParseOptions(ProjectId id, ParseOptions options)
            {
                base.OnParseOptionsChanged(id, options);
            }
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Workspaces
{
    public partial class WorkspaceTests
    {
        private TestWorkspace CreateWorkspace(bool disablePartialSolutions = true)
        {
            return new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, disablePartialSolutions: disablePartialSolutions);
        }

        private static async Task WaitForWorkspaceOperationsToComplete(TestWorkspace workspace)
        {
            var workspaceWaiter = workspace.ExportProvider
                .GetExports<IAsynchronousOperationListener, FeatureMetadata>()
                .First(l => l.Metadata.FeatureName == FeatureAttribute.Workspace).Value as IAsynchronousOperationWaiter;
            await workspaceWaiter.CreateWaitTask();
        }

        [Fact]
        public async Task TestEmptySolutionUpdateDoesNotFireEvents()
        {
            using (var workspace = CreateWorkspace())
            {
                var project = new TestHostProject(workspace);
                workspace.AddTestProject(project);

                // wait for all previous operations to complete
                await WaitForWorkspaceOperationsToComplete(workspace);

                var solution = workspace.CurrentSolution;
                bool workspaceChanged = false;

                workspace.WorkspaceChanged += (s, e) => workspaceChanged = true;

                // make an 'empty' update by claiming something changed, but its the same as before
                workspace.OnParseOptionsChanged(project.Id, project.ParseOptions);

                // wait for any new outstanding operations to complete (there shouldn't be any)
                await WaitForWorkspaceOperationsToComplete(workspace);

                // same solution instance == nothing changed
                Assert.Equal(solution, workspace.CurrentSolution);

                // no event was fired because nothing was changed
                Assert.False(workspaceChanged);
            }
        }

        [Fact]
        public void TestAddProject()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                Assert.Equal(0, solution.Projects.Count());

                var project = new TestHostProject(workspace);

                workspace.AddTestProject(project);
                solution = workspace.CurrentSolution;

                Assert.Equal(1, solution.Projects.Count());
            }
        }

        [Fact]
        public void TestRemoveExistingProject1()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project = new TestHostProject(workspace);

                workspace.AddTestProject(project);
                workspace.OnProjectRemoved(project.Id);
                solution = workspace.CurrentSolution;

                Assert.Equal(0, solution.Projects.Count());
            }
        }

        [Fact]
        public void TestRemoveExistingProject2()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project = new TestHostProject(workspace);

                workspace.AddTestProject(project);
                solution = workspace.CurrentSolution;
                workspace.OnProjectRemoved(project.Id);
                solution = workspace.CurrentSolution;

                Assert.Equal(0, solution.Projects.Count());
            }
        }

        [Fact]
        public void TestRemoveNonAddedProject1()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project = new TestHostProject(workspace);

                Assert.Throws<ArgumentException>(() => workspace.OnProjectRemoved(project.Id));
            }
        }

        [Fact]
        public void TestRemoveNonAddedProject2()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);

                Assert.Throws<ArgumentException>(() => workspace.OnProjectRemoved(project2.Id));
            }
        }

        [Fact]
        public async Task TestChangeOptions1()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(
@"#if FOO
class C { }
#else
class D { }
#endif");

                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);

                await VerifyRootTypeNameAsync(workspace, "D");

                workspace.OnParseOptionsChanged(document.Id.ProjectId,
                    new CSharpParseOptions(preprocessorSymbols: new[] { "FOO" }));

                await VerifyRootTypeNameAsync(workspace, "C");
            }
        }

        [Fact]
        public async Task TestChangeOptions2()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(
@"#if FOO
class C { }
#else
class D { }
#endif");

                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());

                await VerifyRootTypeNameAsync(workspace, "D");

                workspace.OnParseOptionsChanged(document.Id.ProjectId,
                    new CSharpParseOptions(preprocessorSymbols: new[] { "FOO" }));

                await VerifyRootTypeNameAsync(workspace, "C");

                workspace.OnDocumentClosed(document.Id);
            }
        }

        private static async Task VerifyRootTypeNameAsync(TestWorkspace workspaceSnapshotBuilder, string typeName)
        {
            var currentSnapshot = workspaceSnapshotBuilder.CurrentSolution;
            var type = await GetRootTypeDeclarationAsync(currentSnapshot);

            Assert.Equal(type.Identifier.ValueText, typeName);
        }

        private static async Task<TypeDeclarationSyntax> GetRootTypeDeclarationAsync(Solution currentSnapshot)
        {
            var tree = await currentSnapshot.Projects.First().Documents.First().GetSyntaxTreeAsync();
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var type = (TypeDeclarationSyntax)root.Members[0];
            return type;
        }

        [Fact]
        public void TestAddP2PReferenceFails()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);

                Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id)));
            }
        }

        [Fact]
        public void TestAddP2PReference1()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var reference = new ProjectReference(project2.Id);
                workspace.OnProjectReferenceAdded(project1.Id, reference);

                var snapshot = workspace.CurrentSolution;
                var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

                Assert.True(snapshot.GetProject(id1).ProjectReferences.Contains(reference), "ProjectReferences did not contain project2");
            }
        }

        [Fact]
        public void TestAddP2PReferenceTwice()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));

                Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id)));
            }
        }

        [Fact]
        public void TestRemoveP2PReference1()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));
                workspace.OnProjectReferenceRemoved(project1.Id, new ProjectReference(project2.Id));

                var snapshot = workspace.CurrentSolution;
                var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

                Assert.Equal(0, snapshot.GetProject(id1).ProjectReferences.Count());
            }
        }

        [Fact]
        public void TestAddP2PReferenceCircularity()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var project1 = new TestHostProject(workspace, name: "project1");
                var project2 = new TestHostProject(workspace, name: "project2");

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));

                Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project2.Id, new ProjectReference(project1.Id)));
            }
        }

        [Fact]
        public void TestRemoveProjectWithOpenedDocuments()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(string.Empty);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());

                Assert.Throws<ArgumentException>(() => workspace.OnProjectRemoved(project1.Id));

                workspace.OnDocumentClosed(document.Id);
                workspace.OnProjectRemoved(project1.Id);
            }
        }

        [Fact]
        public void TestRemoveProjectWithClosedDocuments()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(string.Empty);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());
                workspace.OnDocumentClosed(document.Id);
                workspace.OnProjectRemoved(project1.Id);
            }
        }

        [Fact]
        public void TestRemoveOpenedDocument()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(string.Empty);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());

                Assert.Throws<ArgumentException>(() => workspace.OnDocumentRemoved(document.Id));

                workspace.OnDocumentClosed(document.Id);
                workspace.OnProjectRemoved(project1.Id);
            }
        }

        [Fact]
        public async Task TestGetCompilation()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(@"class C { }");
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                await VerifyRootTypeNameAsync(workspace, "C");

                var snapshot = workspace.CurrentSolution;
                var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;

                var compilation = await snapshot.GetProject(id1).GetCompilationAsync();
                var classC = compilation.SourceModule.GlobalNamespace.GetMembers("C").Single();
            }
        }

        [Fact]
        public async Task TestGetCompilationOnDependentProject()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document1 = new TestHostDocument(@"public class C { }");
                var project1 = new TestHostProject(workspace, document1, name: "project1");

                var document2 = new TestHostDocument(@"class D : C { }");
                var project2 = new TestHostProject(workspace, document2, name: "project2", projectReferences: new[] { project1 });

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var snapshot = workspace.CurrentSolution;
                var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

                var compilation2 = await snapshot.GetProject(id2).GetCompilationAsync();
                var classD = compilation2.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classC = classD.BaseType;
            }
        }

        [Fact]
        public async Task TestGetCompilationOnCrossLanguageDependentProject()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document1 = new TestHostDocument(@"public class C { }");
                var project1 = new TestHostProject(workspace, document1, name: "project1");

                var document2 = new TestHostDocument("Public Class D \r\n  Inherits C\r\nEnd Class");
                var project2 = new TestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var snapshot = workspace.CurrentSolution;
                var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

                var compilation2 = await snapshot.GetProject(id2).GetCompilationAsync();
                var classD = compilation2.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classC = classD.BaseType;
            }
        }

        [Fact]
        public async Task TestGetCompilationOnCrossLanguageDependentProjectChanged()
        {
            using (var workspace = CreateWorkspace())
            {
                var solutionX = workspace.CurrentSolution;

                var document1 = new TestHostDocument(@"public class C { }");
                var project1 = new TestHostProject(workspace, document1, name: "project1");

                var document2 = new TestHostDocument("Public Class D \r\n  Inherits C\r\nEnd Class");
                var project2 = new TestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var solutionY = workspace.CurrentSolution;
                var id1 = solutionY.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = solutionY.Projects.First(p => p.Name == project2.Name).Id;

                var compilation2 = await solutionY.GetProject(id2).GetCompilationAsync();
                var errors = compilation2.GetDiagnostics();
                var classD = compilation2.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classC = classD.BaseType;
                Assert.NotEqual(TypeKind.Error, classC.TypeKind);

                // change the class name in document1
                workspace.OnDocumentOpened(document1.Id, document1.GetOpenTextContainer());
                var buffer1 = document1.GetTextBuffer();

                // change C to X
                buffer1.Replace(new Span(13, 1), "X");

                // this solution should have the change
                var solutionZ = workspace.CurrentSolution;
                var docZ = solutionZ.GetDocument(document1.Id);
                var docZText = await docZ.GetTextAsync();

                var compilation2Z = await solutionZ.GetProject(id2).GetCompilationAsync();
                var classDz = compilation2Z.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classCz = classDz.BaseType;

                Assert.Equal(TypeKind.Error, classCz.TypeKind);
            }
        }

        [WpfFact]
        public async Task TestDependentSemanticVersionChangesWhenNotOriginallyAccessed()
        {
            using (var workspace = CreateWorkspace(disablePartialSolutions: false))
            {
                var solutionX = workspace.CurrentSolution;

                var document1 = new TestHostDocument(@"public class C { }");
                var project1 = new TestHostProject(workspace, document1, name: "project1");

                var document2 = new TestHostDocument("Public Class D \r\n  Inherits C\r\nEnd Class");
                var project2 = new TestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var solutionY = workspace.CurrentSolution;
                var id1 = solutionY.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = solutionY.Projects.First(p => p.Name == project2.Name).Id;

                var compilation2y = await solutionY.GetProject(id2).GetCompilationAsync();
                var errors = compilation2y.GetDiagnostics();
                var classDy = compilation2y.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classCy = classDy.BaseType;
                Assert.NotEqual(TypeKind.Error, classCy.TypeKind);

                // open both documents so background compiler works on their compilations
                workspace.OnDocumentOpened(document1.Id, document1.GetOpenTextContainer());
                workspace.OnDocumentOpened(document2.Id, document2.GetOpenTextContainer());

                // change C to X
                var buffer1 = document1.GetTextBuffer();
                buffer1.Replace(new Span(13, 1), "X");

                for (int iter = 0; iter < 10; iter++)
                {
                    WaitHelper.WaitForDispatchedOperationsToComplete(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    Thread.Sleep(1000);

                    // the current solution should eventually have the change
                    var cs = workspace.CurrentSolution;
                    var doc1Z = cs.GetDocument(document1.Id);
                    var hasX = (await doc1Z.GetTextAsync()).ToString().Contains("X");

                    if (hasX)
                    {
                        var newVersion = await cs.GetProject(project1.Id).GetDependentSemanticVersionAsync();
                        var newVersionX = await doc1Z.Project.GetDependentSemanticVersionAsync();
                        Assert.NotEqual(VersionStamp.Default, newVersion);
                        Assert.Equal(newVersion, newVersionX);
                        break;
                    }
                }
            }
        }

        [WpfFact]
        public async Task TestGetCompilationOnCrossLanguageDependentProjectChangedInProgress()
        {
            using (var workspace = CreateWorkspace(disablePartialSolutions: false))
            {
                var solutionX = workspace.CurrentSolution;

                var document1 = new TestHostDocument(@"public class C { }");
                var project1 = new TestHostProject(workspace, document1, name: "project1");

                var document2 = new TestHostDocument("Public Class D \r\n  Inherits C\r\nEnd Class");
                var project2 = new TestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

                workspace.AddTestProject(project1);
                workspace.AddTestProject(project2);

                var solutionY = workspace.CurrentSolution;
                var id1 = solutionY.Projects.First(p => p.Name == project1.Name).Id;
                var id2 = solutionY.Projects.First(p => p.Name == project2.Name).Id;

                var compilation2y = await solutionY.GetProject(id2).GetCompilationAsync();
                var errors = compilation2y.GetDiagnostics();
                var classDy = compilation2y.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                var classCy = classDy.BaseType;
                Assert.NotEqual(TypeKind.Error, classCy.TypeKind);

                // open both documents so background compiler works on their compilations
                workspace.OnDocumentOpened(document1.Id, document1.GetOpenTextContainer());
                workspace.OnDocumentOpened(document2.Id, document2.GetOpenTextContainer());

                // change C to X
                var buffer1 = document1.GetTextBuffer();
                buffer1.Replace(new Span(13, 1), "X");

                var foundTheError = false;
                for (int iter = 0; iter < 10; iter++)
                {
                    WaitHelper.WaitForDispatchedOperationsToComplete(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    Thread.Sleep(1000);

                    // the current solution should eventually have the change
                    var cs = workspace.CurrentSolution;
                    var doc1Z = cs.GetDocument(document1.Id);
                    var hasX = (await doc1Z.GetTextAsync()).ToString().Contains("X");

                    if (hasX)
                    {
                        var doc2Z = cs.GetDocument(document2.Id);
                        var partialDoc2Z = await doc2Z.WithFrozenPartialSemanticsAsync(CancellationToken.None);
                        var compilation2Z = await partialDoc2Z.Project.GetCompilationAsync();
                        var classDz = compilation2Z.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                        var classCz = classDz.BaseType;

                        if (classCz.TypeKind == TypeKind.Error)
                        {
                            foundTheError = true;
                            break;
                        }
                    }
                }

                Assert.True(foundTheError, "Did not find error");
            }
        }

        [Fact]
        public async Task TestOpenAndChangeDocument()
        {
            using (var workspace = CreateWorkspace())
            {
                var solution = workspace.CurrentSolution;

                var document = new TestHostDocument(string.Empty);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                var buffer = document.GetTextBuffer();
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());

                buffer.Insert(0, "class C {}");

                solution = workspace.CurrentSolution;
                var doc = solution.Projects.Single().Documents.First();

                var syntaxTree = await doc.GetSyntaxTreeAsync(CancellationToken.None);
                Assert.True(syntaxTree.GetRoot().Width() > 0, "syntaxTree.GetRoot().Width should be > 0");

                workspace.OnDocumentClosed(document.Id);
                workspace.OnProjectRemoved(project1.Id);
            }
        }

        [Fact]
        public async Task TestApplyChangesWithDocumentTextUpdated()
        {
            using (var workspace = CreateWorkspace())
            {
                var startText = "public class C { }";
                var newText = "public class D { }";

                var document = new TestHostDocument(startText);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);
                var buffer = document.GetTextBuffer();
                workspace.OnDocumentOpened(document.Id, document.GetOpenTextContainer());

                // prove the document has the correct text
                Assert.Equal(startText, (await workspace.CurrentSolution.GetDocument(document.Id).GetTextAsync()).ToString());

                // fork the solution to introduce a change.
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.WithDocumentText(document.Id, SourceText.From(newText));

                // prove that current document text is unchanged
                Assert.Equal(startText, (await workspace.CurrentSolution.GetDocument(document.Id).GetTextAsync()).ToString());

                // prove buffer is unchanged too
                Assert.Equal(startText, buffer.CurrentSnapshot.GetText());

                workspace.TryApplyChanges(newSolution);

                // new text should have been pushed into buffer
                Assert.Equal(newText, buffer.CurrentSnapshot.GetText());
            }
        }

        [Fact]
        public void TestApplyChangesWithDocumentAdded()
        {
            using (var workspace = CreateWorkspace())
            {
                var doc1Text = "public class C { }";
                var doc2Text = "public class D { }";

                var document = new TestHostDocument(doc1Text);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);

                // fork the solution to introduce a change.
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.AddDocument(DocumentId.CreateNewId(project1.Id), "Doc2", SourceText.From(doc2Text));

                workspace.TryApplyChanges(newSolution);

                // new document should have been added.
                Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            }
        }

        [Fact]
        public void TestApplyChangesWithDocumentRemoved()
        {
            using (var workspace = CreateWorkspace())
            {
                var doc1Text = "public class C { }";

                var document = new TestHostDocument(doc1Text);
                var project1 = new TestHostProject(workspace, document, name: "project1");

                workspace.AddTestProject(project1);

                // fork the solution to introduce a change.
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.RemoveDocument(document.Id);

                workspace.TryApplyChanges(newSolution);

                // document should have been removed
                Assert.Equal(0, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            }
        }

        [Fact]
        public async Task TestDocumentEvents()
        {
            using (var workspace = CreateWorkspace())
            {
                var doc1Text = "public class C { }";
                var document = new TestHostDocument(doc1Text);
                var project1 = new TestHostProject(workspace, document, name: "project1");
                var longEventTimeout = TimeSpan.FromMinutes(5);
                var shortEventTimeout = TimeSpan.FromSeconds(5);

                workspace.AddTestProject(project1);

                // Creating two waiters that will allow us to know for certain if the events have fired.
                using (var closeWaiter = new EventWaiter())
                using (var openWaiter = new EventWaiter())
                {
                    // Wrapping event handlers so they can notify us on being called.
                    var documentOpenedEventHandler = openWaiter.Wrap<DocumentEventArgs>(
                        (sender, args) => Assert.True(args.Document.Id == document.Id,
                        "The document given to the 'DocumentOpened' event handler did not have the same id as the one created for the test."));

                    var documentClosedEventHandler = closeWaiter.Wrap<DocumentEventArgs>(
                        (sender, args) => Assert.True(args.Document.Id == document.Id,
                        "The document given to the 'DocumentClosed' event handler did not have the same id as the one created for the test."));

                    workspace.DocumentOpened += documentOpenedEventHandler;
                    workspace.DocumentClosed += documentClosedEventHandler;

                    workspace.OpenDocument(document.Id);
                    workspace.CloseDocument(document.Id);

                    // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
                    await WaitForWorkspaceOperationsToComplete(workspace);

                    // Wait to receive signal that events have fired.
                    Assert.True(openWaiter.WaitForEventToFire(longEventTimeout),
                                            string.Format("event 'DocumentOpened' was not fired within {0} minutes.",
                                            longEventTimeout.Minutes));

                    Assert.True(closeWaiter.WaitForEventToFire(longEventTimeout),
                                            string.Format("event 'DocumentClosed' was not fired within {0} minutes.",
                                            longEventTimeout.Minutes));

                    workspace.DocumentOpened -= documentOpenedEventHandler;
                    workspace.DocumentClosed -= documentClosedEventHandler;

                    workspace.OpenDocument(document.Id);
                    workspace.CloseDocument(document.Id);

                    // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
                    await WaitForWorkspaceOperationsToComplete(workspace);

                    // Verifying that an event has not been called is difficult to prove.  
                    // All events should have already been called so we wait 5 seconds and then assume the event handler was removed correctly. 
                    Assert.False(openWaiter.WaitForEventToFire(shortEventTimeout),
                                            string.Format("event handler 'DocumentOpened' was called within {0} seconds though it was removed from the list.",
                                            shortEventTimeout.Seconds));

                    Assert.False(closeWaiter.WaitForEventToFire(shortEventTimeout),
                                            string.Format("event handler 'DocumentClosed' was called within {0} seconds though it was removed from the list.",
                                            shortEventTimeout.Seconds));
                }
            }
        }

        [Fact]
        public async Task TestAdditionalFile_Properties()
        {
            using (var workspace = CreateWorkspace())
            {
                var document = new TestHostDocument("public class C { }");
                var additionalDoc = new TestHostDocument("some text");
                var project1 = new TestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

                workspace.AddTestProject(project1);

                var project = workspace.CurrentSolution.Projects.Single();

                Assert.Equal(1, project.Documents.Count());
                Assert.Equal(1, project.AdditionalDocuments.Count());
                Assert.Equal(1, project.AdditionalDocumentIds.Count);

                var doc = project.GetDocument(additionalDoc.Id);
                Assert.Null(doc);

                var additionalDocument = project.GetAdditionalDocument(additionalDoc.Id);

                Assert.Equal("some text", (await additionalDocument.GetTextAsync()).ToString());
            }
        }

        [Fact]
        public async Task TestAdditionalFile_DocumentChanged()
        {
            using (var workspace = CreateWorkspace())
            {
                var startText = @"<setting value = ""foo""";
                var newText = @"<setting value = ""foo1""";
                var document = new TestHostDocument("public class C { }");
                var additionalDoc = new TestHostDocument(startText);
                var project1 = new TestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

                workspace.AddTestProject(project1);
                var buffer = additionalDoc.GetTextBuffer();
                workspace.OnAdditionalDocumentOpened(additionalDoc.Id, additionalDoc.GetOpenTextContainer());

                var project = workspace.CurrentSolution.Projects.Single();
                var oldVersion = await project.GetSemanticVersionAsync();

                // fork the solution to introduce a change.
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.WithAdditionalDocumentText(additionalDoc.Id, SourceText.From(newText));
                workspace.TryApplyChanges(newSolution);

                var doc = workspace.CurrentSolution.GetAdditionalDocument(additionalDoc.Id);

                // new text should have been pushed into buffer
                Assert.Equal(newText, buffer.CurrentSnapshot.GetText());

                // Text changes are considered top level changes and they change the project's semantic version.
                Assert.Equal(await doc.GetTextVersionAsync(), await doc.GetTopLevelChangeTextVersionAsync());
                Assert.NotEqual(oldVersion, await doc.Project.GetSemanticVersionAsync());
            }
        }

        [Fact]
        public async Task TestAdditionalFile_OpenClose()
        {
            using (var workspace = CreateWorkspace())
            {
                var startText = @"<setting value = ""foo""";
                var document = new TestHostDocument("public class C { }");
                var additionalDoc = new TestHostDocument(startText);
                var project1 = new TestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

                workspace.AddTestProject(project1);
                var buffer = additionalDoc.GetTextBuffer();
                var doc = workspace.CurrentSolution.GetAdditionalDocument(additionalDoc.Id);
                var text = await doc.GetTextAsync(CancellationToken.None);
                var version = await doc.GetTextVersionAsync(CancellationToken.None);

                workspace.OnAdditionalDocumentOpened(additionalDoc.Id, additionalDoc.GetOpenTextContainer());

                // We don't have a GetOpenAdditionalDocumentIds since we don't need it. But make sure additional documents
                // don't creep into OpenDocumentIds (Bug: 1087470)
                Assert.Empty(workspace.GetOpenDocumentIds());

                workspace.OnAdditionalDocumentClosed(additionalDoc.Id, TextLoader.From(TextAndVersion.Create(text, version)));

                // Reopen and close to make sure we are not leaking anything.
                workspace.OnAdditionalDocumentOpened(additionalDoc.Id, additionalDoc.GetOpenTextContainer());
                workspace.OnAdditionalDocumentClosed(additionalDoc.Id, TextLoader.From(TextAndVersion.Create(text, version)));
                Assert.Empty(workspace.GetOpenDocumentIds());
            }
        }

        [Fact]
        public void TestAdditionalFile_AddRemove()
        {
            using (var workspace = CreateWorkspace())
            {
                var startText = @"<setting value = ""foo""";
                var document = new TestHostDocument("public class C { }");
                var additionalDoc = new TestHostDocument(startText, "original.config");
                var project1 = new TestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });
                workspace.AddTestProject(project1);

                var project = workspace.CurrentSolution.Projects.Single();

                // fork the solution to introduce a change.
                var newDocId = DocumentId.CreateNewId(project.Id);
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.AddAdditionalDocument(newDocId, "app.config", "text");

                var doc = workspace.CurrentSolution.GetAdditionalDocument(additionalDoc.Id);

                workspace.TryApplyChanges(newSolution);

                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
                Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Count());

                // Now remove the newly added document

                oldSolution = workspace.CurrentSolution;
                newSolution = oldSolution.RemoveAdditionalDocument(newDocId);

                workspace.TryApplyChanges(newSolution);

                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Count());
                Assert.Equal("original.config", workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Single().Name);
            }
        }

        [Fact]
        public void TestAdditionalFile_AddRemove_FromProject()
        {
            using (var workspace = CreateWorkspace())
            {
                var startText = @"<setting value = ""foo""";
                var document = new TestHostDocument("public class C { }");
                var additionalDoc = new TestHostDocument(startText, "original.config");
                var project1 = new TestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });
                workspace.AddTestProject(project1);

                var project = workspace.CurrentSolution.Projects.Single();

                // fork the solution to introduce a change.
                var doc = project.AddAdditionalDocument("app.config", "text");
                workspace.TryApplyChanges(doc.Project.Solution);

                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
                Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Count());

                // Now remove the newly added document
                project = workspace.CurrentSolution.Projects.Single();
                workspace.TryApplyChanges(project.RemoveAdditionalDocument(doc.Id).Solution);

                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
                Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Count());
                Assert.Equal("original.config", workspace.CurrentSolution.GetProject(project1.Id).AdditionalDocuments.Single().Name);
            }
        }
    }
}

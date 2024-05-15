// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Workspaces
{
    [UseExportProvider]
    public class WorkspaceTests_EditorFeatures : TestBase
    {
        private static EditorTestWorkspace CreateWorkspace(
            string workspaceKind = null,
            bool disablePartialSolutions = true,
            TestComposition composition = null)
        {
            composition ??= EditorTestCompositions.EditorFeatures;
            return new EditorTestWorkspace(composition, workspaceKind, disablePartialSolutions: disablePartialSolutions);
        }

        private static async Task WaitForWorkspaceOperationsToComplete(EditorTestWorkspace workspace)
        {
            var workspaceWaiter = workspace.ExportProvider
                                    .GetExportedValue<AsynchronousOperationListenerProvider>()
                                    .GetWaiter(FeatureAttribute.Workspace);

            await workspaceWaiter.ExpeditedWaitAsync();
        }

        [Fact]
        public async Task TestEmptySolutionUpdateDoesNotFireEvents()
        {
            using var workspace = CreateWorkspace();
            var project = new EditorTestHostProject(workspace);
            workspace.AddTestProject(project);

            // wait for all previous operations to complete
            await WaitForWorkspaceOperationsToComplete(workspace);

            var solution = workspace.CurrentSolution;
            var workspaceChanged = false;

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

        [Fact]
        public void TestAddProject()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            Assert.Equal(0, solution.Projects.Count());

            var project = new EditorTestHostProject(workspace);

            workspace.AddTestProject(project);
            solution = workspace.CurrentSolution;

            Assert.Equal(1, solution.Projects.Count());
        }

        [Fact]
        public void TestRemoveExistingProject1()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project = new EditorTestHostProject(workspace);

            workspace.AddTestProject(project);
            workspace.OnProjectRemoved(project.Id);
            solution = workspace.CurrentSolution;

            Assert.Equal(0, solution.Projects.Count());
        }

        [Fact]
        public void TestRemoveExistingProject2()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project = new EditorTestHostProject(workspace);

            workspace.AddTestProject(project);
            solution = workspace.CurrentSolution;
            workspace.OnProjectRemoved(project.Id);
            solution = workspace.CurrentSolution;

            Assert.Equal(0, solution.Projects.Count());
        }

        [Fact]
        public void TestRemoveNonAddedProject1()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project = new EditorTestHostProject(workspace);

            Assert.Throws<ArgumentException>(() => workspace.OnProjectRemoved(project.Id));
        }

        [Fact]
        public void TestRemoveNonAddedProject2()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);

            Assert.Throws<ArgumentException>(() => workspace.OnProjectRemoved(project2.Id));
        }

        [Fact]
        public async Task TestChangeOptions1()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(
                """
                #if GOO
                class C { }
                #else
                class D { }
                #endif
                """);

            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);

            await VerifyRootTypeNameAsync(workspace, "D");

            workspace.OnParseOptionsChanged(document.Id.ProjectId,
                new CSharpParseOptions(preprocessorSymbols: new[] { "GOO" }));

            await VerifyRootTypeNameAsync(workspace, "C");
        }

        [Fact]
        public async Task TestChangeOptions2()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(
                """
                #if GOO
                class C { }
                #else
                class D { }
                #endif
                """);

            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            workspace.OpenDocument(document.Id);

            await VerifyRootTypeNameAsync(workspace, "D");

            workspace.OnParseOptionsChanged(document.Id.ProjectId,
                new CSharpParseOptions(preprocessorSymbols: new[] { "GOO" }));

            await VerifyRootTypeNameAsync(workspace, "C");

            workspace.CloseDocument(document.Id);
        }

        [Fact]
        public async Task TestAddedSubmissionParseTreeHasEmptyFilePath()
        {
            using var workspace = CreateWorkspace();
            var document1 = new EditorTestHostDocument("var x = 1;", displayName: "Sub1", sourceCodeKind: SourceCodeKind.Script);
            var project1 = new EditorTestHostProject(workspace, document1, name: "Submission");

            var document2 = new EditorTestHostDocument("var x = 2;", displayName: "Sub2", sourceCodeKind: SourceCodeKind.Script, filePath: "a.csx");
            var project2 = new EditorTestHostProject(workspace, document2, name: "Script");

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            workspace.TryApplyChanges(workspace.CurrentSolution);

            // Check that a parse tree for a submission has an empty file path.
            var tree1 = await workspace.CurrentSolution
                .GetProjectState(project1.Id)
                .DocumentStates.GetState(document1.Id)
                .GetSyntaxTreeAsync(CancellationToken.None);
            Assert.Equal("", tree1.FilePath);

            // Check that a parse tree for a script does not have an empty file path.
            var tree2 = await workspace.CurrentSolution
                .GetProjectState(project2.Id)
                .DocumentStates.GetState(document2.Id)
                .GetSyntaxTreeAsync(CancellationToken.None);
            Assert.Equal("a.csx", tree2.FilePath);
        }

        private static async Task VerifyRootTypeNameAsync(EditorTestWorkspace workspaceSnapshotBuilder, string typeName)
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
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);

            Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id)));
        }

        [Fact]
        public void TestAddP2PReference1()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            var reference = new ProjectReference(project2.Id);
            workspace.OnProjectReferenceAdded(project1.Id, reference);

            var snapshot = workspace.CurrentSolution;
            var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
            var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

            Assert.True(snapshot.GetProject(id1).ProjectReferences.Contains(reference), "ProjectReferences did not contain project2");
        }

        [Fact]
        public void TestAddP2PReferenceTwice()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));

            Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id)));
        }

        [Fact]
        public void TestRemoveP2PReference1()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));
            workspace.OnProjectReferenceRemoved(project1.Id, new ProjectReference(project2.Id));

            var snapshot = workspace.CurrentSolution;
            var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
            var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

            Assert.Equal(0, snapshot.GetProject(id1).ProjectReferences.Count());
        }

        [Fact]
        public void TestAddP2PReferenceCircularity()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var project1 = new EditorTestHostProject(workspace, name: "project1");
            var project2 = new EditorTestHostProject(workspace, name: "project2");

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            workspace.OnProjectReferenceAdded(project1.Id, new ProjectReference(project2.Id));

            Assert.Throws<ArgumentException>(() => workspace.OnProjectReferenceAdded(project2.Id, new ProjectReference(project1.Id)));
        }

        [Fact]
        public void TestRemoveProjectWithOpenedDocuments()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(string.Empty);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            workspace.OpenDocument(document.Id);

            workspace.OnProjectRemoved(project1.Id);
            Assert.False(workspace.IsDocumentOpen(document.Id));
            Assert.Empty(workspace.CurrentSolution.Projects);
        }

        [Fact]
        public void TestRemoveProjectWithClosedDocuments()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(string.Empty);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            workspace.OpenDocument(document.Id);
            workspace.CloseDocument(document.Id);
            workspace.OnProjectRemoved(project1.Id);
        }

        [Fact]
        public void TestRemoveOpenedDocument()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(string.Empty);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            workspace.OpenDocument(document.Id);

            workspace.OnDocumentRemoved(document.Id);

            Assert.Empty(workspace.CurrentSolution.Projects.Single().Documents);

            workspace.OnProjectRemoved(project1.Id);
        }

        [Fact]
        public async Task TestGetCompilation()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(@"class C { }");
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            await VerifyRootTypeNameAsync(workspace, "C");

            var snapshot = workspace.CurrentSolution;
            var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;

            var compilation = await snapshot.GetProject(id1).GetCompilationAsync();
            var classC = compilation.SourceModule.GlobalNamespace.GetMembers("C").Single();
        }

        [Fact]
        public async Task TestGetCompilationOnDependentProject()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document1 = new EditorTestHostDocument(@"public class C { }");
            var project1 = new EditorTestHostProject(workspace, document1, name: "project1");

            var document2 = new EditorTestHostDocument(@"class D : C { }");
            var project2 = new EditorTestHostProject(workspace, document2, name: "project2", projectReferences: new[] { project1 });

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            var snapshot = workspace.CurrentSolution;
            var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
            var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

            var compilation2 = await snapshot.GetProject(id2).GetCompilationAsync();
            var classD = compilation2.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
            var classC = classD.BaseType;
        }

        [Fact]
        public async Task TestGetCompilationOnCrossLanguageDependentProject()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document1 = new EditorTestHostDocument(@"public class C { }");
            var project1 = new EditorTestHostProject(workspace, document1, name: "project1");

            var document2 = new EditorTestHostDocument("""
                Public Class D 
                  Inherits C
                End Class
                """);
            var project2 = new EditorTestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

            workspace.AddTestProject(project1);
            workspace.AddTestProject(project2);

            var snapshot = workspace.CurrentSolution;
            var id1 = snapshot.Projects.First(p => p.Name == project1.Name).Id;
            var id2 = snapshot.Projects.First(p => p.Name == project2.Name).Id;

            var compilation2 = await snapshot.GetProject(id2).GetCompilationAsync();
            var classD = compilation2.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
            var classC = classD.BaseType;
        }

        [Fact]
        public async Task TestGetCompilationOnCrossLanguageDependentProjectChanged()
        {
            using var workspace = CreateWorkspace();
            var solutionX = workspace.CurrentSolution;

            var document1 = new EditorTestHostDocument(@"public class C { }");
            var project1 = new EditorTestHostProject(workspace, document1, name: "project1");

            var document2 = new EditorTestHostDocument("""
                Public Class D 
                  Inherits C
                End Class
                """);
            var project2 = new EditorTestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

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
            workspace.OpenDocument(document1.Id);
            var buffer1 = document1.GetTextBuffer();

            // change C to X
            buffer1.Replace(new Span(13, 1), "X");

            // this solution should have the change
            var solutionZ = workspace.CurrentSolution;
            var docZ = solutionZ.GetDocument(document1.Id);
            var docZText = await docZ.GetTextAsync();
            Assert.Equal("public class X { }", docZText.ToString());

            var compilation2Z = await solutionZ.GetProject(id2).GetCompilationAsync();
            var classDz = compilation2Z.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
            var classCz = classDz.BaseType;

            Assert.Equal(TypeKind.Error, classCz.TypeKind);
        }

        [WpfFact]
        public async Task TestDependentSemanticVersionChangesWhenNotOriginallyAccessed()
        {
            using var workspace = CreateWorkspace(disablePartialSolutions: false);
            var solutionX = workspace.CurrentSolution;

            var document1 = new EditorTestHostDocument(@"public class C { }");
            var project1 = new EditorTestHostProject(workspace, document1, name: "project1");

            var document2 = new EditorTestHostDocument("""
                Public Class D 
                  Inherits C
                End Class
                """);
            var project2 = new EditorTestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

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
            workspace.OpenDocument(document1.Id);
            workspace.OpenDocument(document2.Id);

            // change C to X
            var buffer1 = document1.GetTextBuffer();
            buffer1.Replace(new Span(13, 1), "X");

            for (var iter = 0; iter < 10; iter++)
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

        [WpfFact]
        public async Task TestGetCompilationOnCrossLanguageDependentProjectChangedInProgress()
        {
            var composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestDocumentTrackingService));

            using var workspace = CreateWorkspace(disablePartialSolutions: false, composition: composition);
            var trackingService = (TestDocumentTrackingService)workspace.Services.GetRequiredService<IDocumentTrackingService>();
            var solutionX = workspace.CurrentSolution;

            var document1 = new EditorTestHostDocument(@"public class C { }");
            var project1 = new EditorTestHostProject(workspace, document1, name: "project1");

            var document2 = new EditorTestHostDocument("""
                Public Class D 
                  Inherits C
                End Class
                """);
            var project2 = new EditorTestHostProject(workspace, document2, language: LanguageNames.VisualBasic, name: "project2", projectReferences: new[] { project1 });

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

            // Make the second document active.  As there is no automatic background compiler, no changes will be seen as long as we keep asking for frozen-partial semantics.
            trackingService.SetActiveDocument(document2.Id);

            workspace.OpenDocument(document1.Id);
            workspace.OpenDocument(document2.Id);

            // change C to X
            var buffer1 = document1.GetTextBuffer();
            buffer1.Replace(new Span(13, 1), "X");

            var foundTheError = false;
            for (var iter = 0; iter < 5 && !foundTheError; iter++)
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
                    var partialDoc2Z = doc2Z.WithFrozenPartialSemantics(CancellationToken.None);
                    var compilation2Z = await partialDoc2Z.Project.GetCompilationAsync();
                    var classDz = compilation2Z.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                    var classCz = classDz.BaseType;

                    if (classCz.TypeKind == TypeKind.Error)
                        foundTheError = true;
                }
            }

            // Should never find this since we're using partial semantics.
            Assert.False(foundTheError, "Did find error");

            {
                // the current solution should eventually have the change
                var cs = workspace.CurrentSolution;
                var doc1Z = cs.GetDocument(document1.Id);
                var hasX = (await doc1Z.GetTextAsync()).ToString().Contains("X");

                if (hasX)
                {
                    var doc2Z = cs.GetDocument(document2.Id);
                    var compilation2Z = await doc2Z.Project.GetCompilationAsync();
                    var classDz = compilation2Z.SourceModule.GlobalNamespace.GetTypeMembers("D").Single();
                    var classCz = classDz.BaseType;

                    if (classCz.TypeKind == TypeKind.Error)
                        foundTheError = true;
                }
            }

            // Should find now that we're going a normal compilation.
            Assert.True(foundTheError, "Did not find error");
        }

        [Fact]
        public async Task TestOpenAndChangeDocument()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var document = new EditorTestHostDocument(string.Empty);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            var buffer = document.GetTextBuffer();
            workspace.OpenDocument(document.Id);

            buffer.Insert(0, "class C {}");

            solution = workspace.CurrentSolution;
            var doc = solution.Projects.Single().Documents.First();

            var syntaxTree = await doc.GetSyntaxTreeAsync(CancellationToken.None);
            Assert.True(syntaxTree.GetRoot().Width() > 0, "syntaxTree.GetRoot().Width should be > 0");

            workspace.CloseDocument(document.Id);
            workspace.OnProjectRemoved(project1.Id);
        }

        [Fact]
        public async Task TestApplyChangesWithDocumentTextUpdated()
        {
            using var workspace = CreateWorkspace();
            var startText = "public class C { }";
            var newText = "public class D { }";

            var document = new EditorTestHostDocument(startText);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);
            var buffer = document.GetTextBuffer();
            workspace.OpenDocument(document.Id);

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

        [Fact]
        public void TestApplyChangesWithDocumentAdded()
        {
            using var workspace = CreateWorkspace();
            var doc1Text = "public class C { }";
            var doc2Text = "public class D { }";

            var document = new EditorTestHostDocument(doc1Text);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);

            // fork the solution to introduce a change.
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.AddDocument(DocumentId.CreateNewId(project1.Id), "Doc2", SourceText.From(doc2Text));

            workspace.TryApplyChanges(newSolution);

            // new document should have been added.
            Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
        }

        [Fact]
        public void TestApplyChangesWithDocumentRemoved()
        {
            using var workspace = CreateWorkspace();
            var doc1Text = "public class C { }";

            var document = new EditorTestHostDocument(doc1Text);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");

            workspace.AddTestProject(project1);

            // fork the solution to introduce a change.
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.RemoveDocument(document.Id);

            workspace.TryApplyChanges(newSolution);

            // document should have been removed
            Assert.Equal(0, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
        }

        [Fact]
        public async Task TestDocumentEvents()
        {
            using var workspace = CreateWorkspace();
            var doc1Text = "public class C { }";
            var document = new EditorTestHostDocument(doc1Text);
            var project1 = new EditorTestHostProject(workspace, document, name: "project1");
            var longEventTimeout = TimeSpan.FromMinutes(5);
            var shortEventTimeout = TimeSpan.FromSeconds(5);

            workspace.AddTestProject(project1);

            // Creating two waiters that will allow us to know for certain if the events have fired.
            using var closeWaiter = new EventWaiter();
            using var openWaiter = new EventWaiter();
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

        [Fact]
        public async Task TestSourceGeneratedDocumentEvents()
        {
            var doc1Text = "public class C { }";
            var workspaceElement = $@"<Workspace>
  <Project AssemblyName=""Test"" Language=""C#"" CommonReferences=""true"">
    <DocumentFromSourceGenerator FilePath=""test1.cs"">{new XText(doc1Text)}</DocumentFromSourceGenerator>
  </Project>
</Workspace>";
            using var workspace = EditorTestWorkspace.Create(workspaceElement, composition: EditorTestCompositions.EditorFeatures);
            var document = workspace.Documents.Single();

            var longEventTimeout = TimeSpan.FromMinutes(5);
            var shortEventTimeout = TimeSpan.FromSeconds(5);

            // Creating two waiters that will allow us to know for certain if the events have fired.
            using var closeWaiter = new EventWaiter();
            using var openWaiter = new EventWaiter();

            // Wrapping event handlers so they can notify us on being called.
            var documentOpenedEventHandler = openWaiter.Wrap<DocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                $"The source generated document given to the '{nameof(Workspace.DocumentOpened)}' event handler did not have the same id as the one created for the test."));

            var documentClosedEventHandler = closeWaiter.Wrap<DocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                $"The source generated document given to the '{nameof(Workspace.DocumentClosed)}' event handler did not have the same id as the one created for the test."));

            workspace.DocumentOpened += documentOpenedEventHandler;
            workspace.DocumentClosed += documentClosedEventHandler;

            workspace.OpenSourceGeneratedDocument(document.Id);
            var sourceGeneratedDocumentId = workspace.GetDocumentIdInCurrentContext(document.GetOpenTextContainer());
            Assert.Equal(document.Id, sourceGeneratedDocumentId);

            await workspace.CloseSourceGeneratedDocumentAsync(sourceGeneratedDocumentId);

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

            workspace.OpenSourceGeneratedDocument(document.Id);
            await workspace.CloseSourceGeneratedDocumentAsync(document.Id);

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

        [Fact]
        public async Task TestAdditionalDocumentEvents()
        {
            using var workspace = CreateWorkspace();
            var document = new EditorTestHostDocument();
            var project1 = new EditorTestHostProject(workspace, additionalDocuments: new[] { document }, name: "project1");
            var longEventTimeout = TimeSpan.FromMinutes(5);
            var shortEventTimeout = TimeSpan.FromSeconds(5);

            workspace.AddTestProject(project1);

            // Creating two waiters that will allow us to know for certain if the events have fired.
            using var closeWaiter = new EventWaiter();
            using var openWaiter = new EventWaiter();
            // Wrapping event handlers so they can notify us on being called.
            var documentOpenedEventHandler = openWaiter.Wrap<TextDocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                "The document given to the 'AdditionalDocumentOpened' event handler did not have the same id as the one created for the test."));

            var documentClosedEventHandler = closeWaiter.Wrap<TextDocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                "The document given to the 'AdditionalDocumentClosed' event handler did not have the same id as the one created for the test."));

            workspace.TextDocumentOpened += documentOpenedEventHandler;
            workspace.TextDocumentClosed += documentClosedEventHandler;

            workspace.OpenAdditionalDocument(document.Id);
            workspace.CloseAdditionalDocument(document.Id);

            // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
            await WaitForWorkspaceOperationsToComplete(workspace);

            // Wait to receive signal that events have fired.
            Assert.True(openWaiter.WaitForEventToFire(longEventTimeout),
                                    string.Format("event 'AdditionalDocumentOpened' was not fired within {0} minutes.",
                                    longEventTimeout.Minutes));

            Assert.True(closeWaiter.WaitForEventToFire(longEventTimeout),
                                    string.Format("event 'AdditionalDocumentClosed' was not fired within {0} minutes.",
                                    longEventTimeout.Minutes));

            workspace.TextDocumentOpened -= documentOpenedEventHandler;
            workspace.TextDocumentClosed -= documentClosedEventHandler;

            workspace.OpenAdditionalDocument(document.Id);
            workspace.CloseAdditionalDocument(document.Id);

            // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
            await WaitForWorkspaceOperationsToComplete(workspace);

            // Verifying that an event has not been called is difficult to prove.  
            // All events should have already been called so we wait 5 seconds and then assume the event handler was removed correctly. 
            Assert.False(openWaiter.WaitForEventToFire(shortEventTimeout),
                                    string.Format("event handler 'AdditionalDocumentOpened' was called within {0} seconds though it was removed from the list.",
                                    shortEventTimeout.Seconds));

            Assert.False(closeWaiter.WaitForEventToFire(shortEventTimeout),
                                    string.Format("event handler 'AdditionalDocumentClosed' was called within {0} seconds though it was removed from the list.",
                                    shortEventTimeout.Seconds));
        }

        [Fact]
        public async Task TestAnalyzerConfigDocumentEvents()
        {
            using var workspace = CreateWorkspace();
            var document = new EditorTestHostDocument();
            var project1 = new EditorTestHostProject(workspace, analyzerConfigDocuments: new[] { document }, name: "project1");
            var longEventTimeout = TimeSpan.FromMinutes(5);
            var shortEventTimeout = TimeSpan.FromSeconds(5);

            workspace.AddTestProject(project1);

            // Creating two waiters that will allow us to know for certain if the events have fired.
            using var closeWaiter = new EventWaiter();
            using var openWaiter = new EventWaiter();
            // Wrapping event handlers so they can notify us on being called.
            var documentOpenedEventHandler = openWaiter.Wrap<TextDocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                "The document given to the 'AnalyzerConfigDocumentOpened' event handler did not have the same id as the one created for the test."));

            var documentClosedEventHandler = closeWaiter.Wrap<TextDocumentEventArgs>(
                (sender, args) => Assert.True(args.Document.Id == document.Id,
                "The document given to the 'AnalyzerConfigDocumentClosed' event handler did not have the same id as the one created for the test."));

            workspace.TextDocumentOpened += documentOpenedEventHandler;
            workspace.TextDocumentClosed += documentClosedEventHandler;

            workspace.OpenAnalyzerConfigDocument(document.Id);
            workspace.CloseAnalyzerConfigDocument(document.Id);

            // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
            await WaitForWorkspaceOperationsToComplete(workspace);

            // Wait to receive signal that events have fired.
            Assert.True(openWaiter.WaitForEventToFire(longEventTimeout),
                                    string.Format("event 'AnalyzerConfigDocumentOpened' was not fired within {0} minutes.",
                                    longEventTimeout.Minutes));

            Assert.True(closeWaiter.WaitForEventToFire(longEventTimeout),
                                    string.Format("event 'AnalyzerConfigDocumentClosed' was not fired within {0} minutes.",
                                    longEventTimeout.Minutes));

            workspace.TextDocumentOpened -= documentOpenedEventHandler;
            workspace.TextDocumentClosed -= documentClosedEventHandler;

            workspace.OpenAnalyzerConfigDocument(document.Id);
            workspace.CloseAnalyzerConfigDocument(document.Id);

            // Wait for all workspace tasks to finish.  After this is finished executing, all handlers should have been notified.
            await WaitForWorkspaceOperationsToComplete(workspace);

            // Verifying that an event has not been called is difficult to prove.  
            // All events should have already been called so we wait 5 seconds and then assume the event handler was removed correctly. 
            Assert.False(openWaiter.WaitForEventToFire(shortEventTimeout),
                                    string.Format("event handler 'AnalyzerConfigDocumentOpened' was called within {0} seconds though it was removed from the list.",
                                    shortEventTimeout.Seconds));

            Assert.False(closeWaiter.WaitForEventToFire(shortEventTimeout),
                                    string.Format("event handler 'AnalyzerConfigDocumentClosed' was called within {0} seconds though it was removed from the list.",
                                    shortEventTimeout.Seconds));
        }

        [Fact]
        public async Task TestAdditionalFile_Properties()
        {
            using var workspace = CreateWorkspace();
            var document = new EditorTestHostDocument("public class C { }");
            var additionalDoc = new EditorTestHostDocument("some text");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

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

        [Fact]
        public async Task TestAnalyzerConfigFile_Properties()
        {
            using var workspace = CreateWorkspace();
            var document = new EditorTestHostDocument("public class C { }");
            var analyzerConfigDoc = new EditorTestHostDocument("root = true");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });

            workspace.AddTestProject(project1);

            var project = workspace.CurrentSolution.Projects.Single();

            Assert.Equal(1, project.Documents.Count());
            Assert.Equal(1, project.AnalyzerConfigDocuments.Count());
            Assert.Equal(1, project.State.AnalyzerConfigDocumentStates.Count);

            var doc = project.GetDocument(analyzerConfigDoc.Id);
            Assert.Null(doc);

            var analyzerConfigDocument = project.GetAnalyzerConfigDocument(analyzerConfigDoc.Id);

            Assert.Equal("root = true", (await analyzerConfigDocument.GetTextAsync()).ToString());
        }

        [Fact]
        public async Task TestAdditionalFile_DocumentChanged()
        {
            using var workspace = CreateWorkspace();
            var startText = """
                <setting value = "goo"
                """;
            var newText = """
                <setting value = "goo1"
                """;
            var document = new EditorTestHostDocument("public class C { }");
            var additionalDoc = new EditorTestHostDocument(startText);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

            workspace.AddTestProject(project1);

            workspace.OpenAdditionalDocument(additionalDoc.Id);

            var project = workspace.CurrentSolution.Projects.Single();
            var oldVersion = await project.GetSemanticVersionAsync();

            // fork the solution to introduce a change.
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.WithAdditionalDocumentText(additionalDoc.Id, SourceText.From(newText));
            workspace.TryApplyChanges(newSolution);

            // new text should have been pushed into buffer
            var buffer = additionalDoc.GetTextBuffer();
            Assert.Equal(newText, buffer.CurrentSnapshot.GetText());

            // Text changes are considered top level changes and they change the project's semantic version.
            var doc = workspace.CurrentSolution.GetAdditionalDocument(additionalDoc.Id);
            Assert.Equal(await doc.GetTextVersionAsync(), await doc.GetTopLevelChangeTextVersionAsync());
            Assert.NotEqual(oldVersion, await doc.Project.GetSemanticVersionAsync());
        }

        [Fact]
        public async Task TestAnalyzerConfigFile_DocumentChanged()
        {
            using var workspace = CreateWorkspace();
            var startText = @"root = true";
            var newText = @"root = false";
            var document = new EditorTestHostDocument("public class C { }");
            var analyzerConfigPath = PathUtilities.CombineAbsoluteAndRelativePaths(Temp.CreateDirectory().Path, ".editorconfig");
            var analyzerConfigDoc = new EditorTestHostDocument(startText, filePath: analyzerConfigPath);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });

            workspace.AddTestProject(project1);

            workspace.OpenAnalyzerConfigDocument(analyzerConfigDoc.Id);

            var project = workspace.CurrentSolution.Projects.Single();
            var oldVersion = await project.GetSemanticVersionAsync();

            // fork the solution to introduce a change.
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.WithAnalyzerConfigDocumentText(analyzerConfigDoc.Id, SourceText.From(newText));
            workspace.TryApplyChanges(newSolution);

            // new text should have been pushed into buffer
            var buffer = analyzerConfigDoc.GetTextBuffer();
            Assert.Equal(newText, buffer.CurrentSnapshot.GetText());

            // Text changes are considered top level changes and they change the project's semantic version.
            var doc = workspace.CurrentSolution.GetAnalyzerConfigDocument(analyzerConfigDoc.Id);
            Assert.Equal(await doc.GetTextVersionAsync(), await doc.GetTopLevelChangeTextVersionAsync());
            Assert.NotEqual(oldVersion, await doc.Project.GetSemanticVersionAsync());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31540")]
        public void TestAdditionalFile_OpenClose()
        {
            using var workspace = CreateWorkspace();
            var startText = """
                <setting value = "goo"
                """;
            var document = new EditorTestHostDocument("public class C { }");
            var additionalDoc = new EditorTestHostDocument(startText);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });

            workspace.AddTestProject(project1);

            workspace.OpenAdditionalDocument(additionalDoc.Id);

            // Make sure that additional documents are included in GetOpenDocumentIds.
            var openDocumentIds = workspace.GetOpenDocumentIds();
            Assert.Single(openDocumentIds);
            Assert.Equal(additionalDoc.Id, openDocumentIds.Single());

            workspace.CloseAdditionalDocument(additionalDoc.Id);

            // Make sure that closed additional documents are not include in GetOpenDocumentIds.
            Assert.Empty(workspace.GetOpenDocumentIds());

            // Reopen and close to make sure we are not leaking anything.
            workspace.OpenAdditionalDocument(additionalDoc.Id);
            workspace.CloseAdditionalDocument(additionalDoc.Id);
            Assert.Empty(workspace.GetOpenDocumentIds());
        }

        [Fact]
        public void TestAnalyzerConfigFile_OpenClose()
        {
            using var workspace = CreateWorkspace();
            var startText = @"root = true";
            var document = new EditorTestHostDocument("public class C { }");
            var analyzerConfigDoc = new EditorTestHostDocument(startText);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });

            workspace.AddTestProject(project1);

            workspace.OpenAnalyzerConfigDocument(analyzerConfigDoc.Id);

            // Make sure that analyzer config documents are included in GetOpenDocumentIds.
            var openDocumentIds = workspace.GetOpenDocumentIds();
            Assert.Single(openDocumentIds);
            Assert.Equal(analyzerConfigDoc.Id, openDocumentIds.Single());

            workspace.CloseAnalyzerConfigDocument(analyzerConfigDoc.Id);

            // Make sure that closed analyzer config documents are not include in GetOpenDocumentIds.
            Assert.Empty(workspace.GetOpenDocumentIds());

            // Reopen and close to make sure we are not leaking anything.
            workspace.OpenAnalyzerConfigDocument(analyzerConfigDoc.Id);
            workspace.CloseAnalyzerConfigDocument(analyzerConfigDoc.Id);
            Assert.Empty(workspace.GetOpenDocumentIds());
        }

        [Fact]
        public void TestAdditionalFile_AddRemove()
        {
            using var workspace = CreateWorkspace();
            var startText = """
                <setting value = "goo"
                """;
            var document = new EditorTestHostDocument("public class C { }");
            var additionalDoc = new EditorTestHostDocument(startText, "original.config");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });
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

        [Fact]
        public void TestAnalyzerConfigFile_AddRemove()
        {
            using var workspace = CreateWorkspace();
            var startText = @"root = true";
            var document = new EditorTestHostDocument("public class C { }");
            var analyzerConfigDoc = new EditorTestHostDocument(startText, "original.config");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });
            workspace.AddTestProject(project1);

            var project = workspace.CurrentSolution.Projects.Single();

            // fork the solution to introduce a change.
            var newDocId = DocumentId.CreateNewId(project.Id);
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.AddAnalyzerConfigDocument(newDocId, "app.config", SourceText.From("text"));

            var doc = workspace.CurrentSolution.GetAnalyzerConfigDocument(analyzerConfigDoc.Id);

            workspace.TryApplyChanges(newSolution);

            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Count());

            // Now remove the newly added document

            oldSolution = workspace.CurrentSolution;
            newSolution = oldSolution.RemoveAnalyzerConfigDocument(newDocId);

            workspace.TryApplyChanges(newSolution);

            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Count());
            Assert.Equal("original.config", workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Single().Name);
        }

        [Fact]
        public void TestAdditionalFile_AddRemove_FromProject()
        {
            using var workspace = CreateWorkspace();
            var startText = """
                <setting value = "goo"
                """;
            var document = new EditorTestHostDocument("public class C { }");
            var additionalDoc = new EditorTestHostDocument(startText, "original.config");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });
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

        [Fact]
        public void TestAnalyzerConfigFile_AddRemove_FromProject()
        {
            using var workspace = CreateWorkspace();
            var startText = @"root = true";
            var document = new EditorTestHostDocument("public class C { }");
            var analyzerConfigDoc = new EditorTestHostDocument(startText, "original.config");
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });
            workspace.AddTestProject(project1);

            var project = workspace.CurrentSolution.Projects.Single();

            // fork the solution to introduce a change.
            var doc = project.AddAnalyzerConfigDocument("app.config", SourceText.From("text"));
            workspace.TryApplyChanges(doc.Project.Solution);

            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            Assert.Equal(2, workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Count());

            // Now remove the newly added document
            project = workspace.CurrentSolution.Projects.Single();
            workspace.TryApplyChanges(project.RemoveAnalyzerConfigDocument(doc.Id).Solution);

            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).Documents.Count());
            Assert.Equal(1, workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Count());
            Assert.Equal("original.config", workspace.CurrentSolution.GetProject(project1.Id).AnalyzerConfigDocuments.Single().Name);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31540")]
        public void TestAdditionalFile_GetDocumentIdsWithFilePath()
        {
            using var workspace = CreateWorkspace();
            const string docFilePath = "filePath1", additionalDocFilePath = "filePath2";
            var document = new EditorTestHostDocument("public class C { }", filePath: docFilePath);
            var additionalDoc = new EditorTestHostDocument("""
                <setting value = "goo"
                """, filePath: additionalDocFilePath);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, additionalDocuments: new[] { additionalDoc });
            workspace.AddTestProject(project1);

            var documentIdsWithFilePath = workspace.CurrentSolution.GetDocumentIdsWithFilePath(docFilePath);
            Assert.Single(documentIdsWithFilePath);
            Assert.Equal(document.Id, documentIdsWithFilePath.Single());

            documentIdsWithFilePath = workspace.CurrentSolution.GetDocumentIdsWithFilePath(additionalDocFilePath);
            Assert.Single(documentIdsWithFilePath);
            Assert.Equal(additionalDoc.Id, documentIdsWithFilePath.Single());
        }

        [Fact]
        public void TestAnalyzerConfigFile_GetDocumentIdsWithFilePath()
        {
            using var workspace = CreateWorkspace();
            const string docFilePath = "filePath1";
            var document = new EditorTestHostDocument("public class C { }", filePath: docFilePath);
            var analyzerConfigDocFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(Temp.CreateDirectory().Path, ".editorconfig");
            var analyzerConfigDoc = new EditorTestHostDocument(@"root = true", filePath: analyzerConfigDocFilePath);
            var project1 = new EditorTestHostProject(workspace, name: "project1", documents: new[] { document }, analyzerConfigDocuments: new[] { analyzerConfigDoc });
            workspace.AddTestProject(project1);

            var documentIdsWithFilePath = workspace.CurrentSolution.GetDocumentIdsWithFilePath(docFilePath);
            Assert.Single(documentIdsWithFilePath);
            Assert.Equal(document.Id, documentIdsWithFilePath.Single());

            documentIdsWithFilePath = workspace.CurrentSolution.GetDocumentIdsWithFilePath(analyzerConfigDocFilePath);
            Assert.Single(documentIdsWithFilePath);
            Assert.Equal(analyzerConfigDoc.Id, documentIdsWithFilePath.Single());
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=209299")]
        public async Task TestLinkedFilesStayInSync()
        {
            var originalText = "class Program1 { }";
            var updatedText = "class Program2 { }";

            var input = $@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""Test.cs"">{originalText}</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Assembly1"" LinkFilePath=""Test.cs"" />
    </Project>
</Workspace>";

            using var workspace = EditorTestWorkspace.Create(input, composition: EditorTestCompositions.EditorFeatures, openDocuments: true);
            var eventArgs = new List<WorkspaceChangeEventArgs>();

            workspace.WorkspaceChanged += (s, e) =>
            {
                Assert.Equal(WorkspaceChangeKind.DocumentChanged, e.Kind);
                eventArgs.Add(e);
            };

            var originalDocumentId = workspace.GetOpenDocumentIds().Single(id => !workspace.GetTestDocument(id).IsLinkFile);
            var linkedDocumentId = workspace.GetOpenDocumentIds().Single(id => workspace.GetTestDocument(id).IsLinkFile);

            workspace.GetTestDocument(originalDocumentId).Update(SourceText.From("class Program2 { }"));
            await WaitForWorkspaceOperationsToComplete(workspace);

            Assert.Equal(2, eventArgs.Count);
            AssertEx.SetEqual(workspace.Projects.SelectMany(p => p.Documents).Select(d => d.Id), eventArgs.Select(e => e.DocumentId));

            Assert.Equal(eventArgs[0].OldSolution, eventArgs[1].OldSolution);
            Assert.Equal(eventArgs[0].NewSolution, eventArgs[1].NewSolution);

            Assert.Equal(originalText, (await eventArgs[0].OldSolution.GetDocument(originalDocumentId).GetTextAsync().ConfigureAwait(false)).ToString());
            Assert.Equal(originalText, (await eventArgs[1].OldSolution.GetDocument(originalDocumentId).GetTextAsync().ConfigureAwait(false)).ToString());

            Assert.Equal(updatedText, (await eventArgs[0].NewSolution.GetDocument(originalDocumentId).GetTextAsync().ConfigureAwait(false)).ToString());
            Assert.Equal(updatedText, (await eventArgs[1].NewSolution.GetDocument(originalDocumentId).GetTextAsync().ConfigureAwait(false)).ToString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31928")]
        public void TestVersionStamp_Local()
        {
            // only Utc is allowed
            Assert.Throws<ArgumentException>(() => VersionStamp.Create(DateTime.Now));
        }

        [Fact]
        public void TestVersionStamp_Default()
        {
            var version1 = VersionStamp.Create(default);
            var version2 = VersionStamp.Create(default);

            var version3 = version1.GetNewerVersion(version2);
            Assert.Equal(version3, version2);

            var version4 = version1.GetNewerVersion();
            var version5 = version4.GetNewerVersion(version3);

            Assert.Equal(version5, version4);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19284")]
        public void TestSolutionWithOptions()
        {
            using var workspace1 = CreateWorkspace();
            var solution = workspace1.CurrentSolution;

            var optionKey = new OptionKey(FormattingOptions2.SmartIndent, LanguageNames.CSharp);
            var defaultValue = solution.Options.GetOption(optionKey);
            var changedValue = FormattingOptions.IndentStyle.Block;
            Assert.NotEqual(defaultValue, changedValue);

            var newOptions = solution.Options.WithChangedOption(optionKey, changedValue);
            var newSolution = solution.WithOptions(newOptions);
            var newOptionValue = newSolution.Options.GetOption(optionKey);
            Assert.Equal(changedValue, newOptionValue);

            Assert.True(workspace1.TryApplyChanges(newSolution));

            var currentOptionValue = workspace1.CurrentSolution.Options.GetOption(optionKey);
            Assert.Equal(changedValue, currentOptionValue);

            // option is set to global options that are shared among all workspaces
            using var workspace2 = CreateWorkspace();
            var value2 = workspace2.Options.GetOption(optionKey);
            Assert.Equal(changedValue, value2);
        }

        [Obsolete]
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19284")]
        public void TestOptionChangedHandlerInvokedAfterCurrentSolutionChanged()
        {
            using var primaryWorkspace = CreateWorkspace();
            using var secondaryWorkspace = CreateWorkspace();

            var document = new EditorTestHostDocument("class C { }");

            var project1 = new EditorTestHostProject(primaryWorkspace, document, name: "project1");

            primaryWorkspace.AddTestProject(project1);
            secondaryWorkspace.AddTestProject(project1);

            var beforeSolutionForPrimaryWorkspace = primaryWorkspace.CurrentSolution;
            var beforeSolutionForSecondaryWorkspace = secondaryWorkspace.CurrentSolution;

            var optionKey = new OptionKey(FormattingOptions2.SmartIndent, LanguageNames.CSharp);
            Assert.Equal(FormattingOptions2.IndentStyle.Smart, primaryWorkspace.Options.GetOption(optionKey));
            Assert.Equal(FormattingOptions2.IndentStyle.Smart, secondaryWorkspace.Options.GetOption(optionKey));

            // Hook up the option changed event handler.
            primaryWorkspace.GlobalOptions.AddOptionChangedHandler(this, OptionService_OptionChanged);

            // Change workspace options through primary workspace
            primaryWorkspace.Options = primaryWorkspace.Options.WithChangedOption(optionKey, FormattingOptions2.IndentStyle.Block);

            // Verify current solution and option change for both workspaces.
            Assert.NotEqual(beforeSolutionForPrimaryWorkspace, primaryWorkspace.CurrentSolution);
            Assert.NotEqual(beforeSolutionForSecondaryWorkspace, secondaryWorkspace.CurrentSolution);

            Assert.Equal(FormattingOptions2.IndentStyle.Block, primaryWorkspace.Options.GetOption(optionKey));
            Assert.Equal(FormattingOptions2.IndentStyle.Block, secondaryWorkspace.Options.GetOption(optionKey));

            primaryWorkspace.GlobalOptions.RemoveOptionChangedHandler(this, OptionService_OptionChanged);
            return;

            void OptionService_OptionChanged(object sender, OptionChangedEventArgs e)
            {
                // CurrentSolution has been updated when the event fires.

                Assert.NotSame(beforeSolutionForPrimaryWorkspace, primaryWorkspace.CurrentSolution);
                Assert.NotSame(beforeSolutionForSecondaryWorkspace, secondaryWorkspace.CurrentSolution);

                Assert.Equal(FormattingOptions2.IndentStyle.Block, primaryWorkspace.Options.GetOption(optionKey));
                Assert.Equal(FormattingOptions2.IndentStyle.Block, secondaryWorkspace.Options.GetOption(optionKey));
            }
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host.UnitTests;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class SolutionTests : TestBase
    {
        private static readonly MetadataReference s_mscorlib = TestReferences.NetFx.v4_0_30319.mscorlib;

        public static byte[] GetResourceBytes(string fileName)
        {
            var fullName = @"Microsoft.CodeAnalysis.UnitTests.TestFiles." + fileName;
            var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullName);
            if (resourceStream != null)
            {
                using (resourceStream)
                {
                    var bytes = new byte[resourceStream.Length];
                    resourceStream.Read(bytes, 0, (int)resourceStream.Length);
                    return bytes;
                }
            }

            return null;
        }

        private Solution CreateSolution()
        {
            return new AdhocWorkspace().CurrentSolution;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCreateSolution()
        {
            var sol = CreateSolution();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject()
        {
            var sol = CreateSolution();
            var pid = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp);
            Assert.True(sol.ProjectIds.Any(), "Solution was expected to have projects");
            Assert.NotNull(pid);
            var project = sol.GetProject(pid);
            Assert.False(project.HasDocuments);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestUpdateAssemblyName()
        {
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "foo", "foo.dll", LanguageNames.CSharp);
            solution = solution.WithProjectAssemblyName(project1, "bar");
            var project = solution.GetProject(project1);
            Assert.Equal("bar", project.AssemblyName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [WorkItem(543964, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543964")]
        public void MultipleProjectsWithSameDisplayName()
        {
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            var project2 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "name", "assemblyName", LanguageNames.CSharp);
            solution = solution.AddProject(project2, "name", "assemblyName", LanguageNames.CSharp);
            Assert.Equal(2, solution.GetProjectsByName("name").Count());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public Solution TestAddFirstDocument()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);
            var sol = CreateSolution()
                .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                .AddDocument(did, "foo.cs", "public class Foo { }");

            // verify project & document
            Assert.NotNull(pid);
            var project = sol.GetProject(pid);
            Assert.NotNull(project);
            Assert.True(sol.ContainsProject(pid), "Solution was expected to have project " + pid);
            Assert.True(project.HasDocuments, "Project was expected to have documents");
            Assert.Equal(project, sol.GetProject(pid));
            Assert.NotNull(did);
            var document = sol.GetDocument(did);
            Assert.True(project.ContainsDocument(did), "Project was expected to have document " + did);
            Assert.Equal(document, project.GetDocument(did));
            Assert.Equal(document, sol.GetDocument(did));
            var semantics = document.GetSemanticModelAsync().Result;
            Assert.NotNull(semantics);

            ValidateSolutionAndCompilations(sol);

            return sol;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddSecondDocument()
        {
            var sol = TestAddFirstDocument();
            var pid = sol.Projects.Single().Id;
            var did = DocumentId.CreateNewId(pid);
            sol = sol.AddDocument(did, "bar.cs", "public class Bar { }");

            // verify project & document
            var project = sol.GetProject(pid);
            Assert.NotNull(project);
            Assert.NotNull(did);
            var document = sol.GetDocument(did);
            Assert.True(project.ContainsDocument(did), "Project was expected to have document " + did);
            Assert.Equal(document, project.GetDocument(did));
            Assert.Equal(document, sol.GetDocument(did));

            ValidateSolutionAndCompilations(sol);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestOneCSharpProject()
        {
            var sol = CreateSolutionWithOneCSharpProject();
            ValidateSolutionAndCompilations(sol);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestTwoCSharpProjects()
        {
            var sol = CreateSolutionWithTwoCSharpProjects();
            ValidateSolutionAndCompilations(sol);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCrossLanguageProjects()
        {
            var sol = CreateCrossLanguageSolution();
            ValidateSolutionAndCompilations(sol);
        }

        private Solution CreateSolutionWithOneCSharpProject()
        {
            return this.CreateSolution()
                       .AddProject("foo", "foo.dll", LanguageNames.CSharp)
                       .AddMetadataReference(s_mscorlib)
                       .AddDocument("foo.cs", "public class Foo { }")
                       .Project.Solution;
        }

        private Solution CreateSolutionWithTwoCSharpProjects()
        {
            var pm1 = ProjectId.CreateNewId();
            var pm2 = ProjectId.CreateNewId();
            var doc1 = DocumentId.CreateNewId(pm1);
            var doc2 = DocumentId.CreateNewId(pm2);
            return this.CreateSolution()
                       .AddProject(pm1, "foo", "foo.dll", LanguageNames.CSharp)
                       .AddProject(pm2, "bar", "bar.dll", LanguageNames.CSharp)
                       .AddProjectReference(pm2, new ProjectReference(pm1))
                       .AddDocument(doc1, "foo.cs", "public class Foo { }")
                       .AddDocument(doc2, "bar.cs", "public class Bar : Foo { }");
        }

        private Solution CreateCrossLanguageSolution()
        {
            var pm1 = ProjectId.CreateNewId();
            var pm2 = ProjectId.CreateNewId();
            return this.CreateSolution()
                       .AddProject(pm1, "foo", "foo.dll", LanguageNames.CSharp)
                       .AddMetadataReference(pm1, s_mscorlib)
                       .AddProject(pm2, "bar", "bar.dll", LanguageNames.VisualBasic)
                       .AddMetadataReference(pm2, s_mscorlib)
                       .AddProjectReference(pm2, new ProjectReference(pm1))
                       .AddDocument(DocumentId.CreateNewId(pm1), "foo.cs", "public class X { }")
                       .AddDocument(DocumentId.CreateNewId(pm2), "bar.vb", "Public Class Y\r\nInherits X\r\nEnd Class");
        }

        private void ValidateSolutionAndCompilations(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                Assert.True(solution.ContainsProject(project.Id), "Solution was expected to have project " + project.Id);
                Assert.Equal(project, solution.GetProject(project.Id));

                // these won't always be unique in real-world but should be for these tests
                Assert.Equal(project, solution.GetProjectsByName(project.Name).FirstOrDefault());

                var compilation = project.GetCompilationAsync().Result;
                Assert.NotNull(compilation);

                // check that the options are the same
                Assert.Equal(project.CompilationOptions, compilation.Options);

                // check that all known metadata references are present in the compilation
                foreach (var meta in project.MetadataReferences)
                {
                    Assert.True(compilation.References.Contains(meta), "Compilation references were expected to contain " + meta);
                }

                // check that all project-to-project reference metadata is present in the compilation
                foreach (var referenced in project.ProjectReferences)
                {
                    if (solution.ContainsProject(referenced.ProjectId))
                    {
                        var referencedMetadata = solution.GetMetadataReferenceAsync(referenced, solution.GetProjectState(project.Id), CancellationToken.None).Result;
                        Assert.NotNull(referencedMetadata);
                        var compilationReference = referencedMetadata as CompilationReference;
                        if (compilationReference != null)
                        {
                            compilation.References.Single(r =>
                            {
                                var cr = r as CompilationReference;
                                return cr != null && cr.Compilation == compilationReference.Compilation;
                            });
                        }
                    }
                }

                // check that the syntax trees are the same
                var docs = project.Documents.ToList();
                var trees = compilation.SyntaxTrees.ToList();
                Assert.Equal(docs.Count, trees.Count);

                foreach (var doc in docs)
                {
                    Assert.True(trees.Contains(doc.GetSyntaxTreeAsync().Result), "trees list was expected to contain the syntax tree of doc");
                }
            }
        }

#if false
        [Fact(Skip = "641963"), Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestDeepProjectReferenceTree()
        {
            int projectCount = 5;
            var solution = CreateSolutionWithProjectDependencyChain(projectCount);
            ProjectId[] projectIds = solution.ProjectIds.ToArray();

            Compilation compilation;
            for (int i = 0; i < projectCount; i++)
            {
                Assert.False(solution.GetProject(projectIds[i]).TryGetCompilation(out compilation));
            }

            var top = solution.GetCompilationAsync(projectIds.Last(), CancellationToken.None).Result;
            var partialSolution = solution.GetPartialSolution();
            for (int i = 0; i < projectCount; i++)
            {
                // While holding a compilation, we also hold its references, plus one further level
                // of references alive.  However, the references are only partial Declaration 
                // compilations
                var isPartialAvailable = i >= projectCount - 3;
                var isFinalAvailable = i == projectCount - 1;

                var projectId = projectIds[i];
                Assert.Equal(isFinalAvailable, solution.GetProject(projectId).TryGetCompilation(out compilation));
                Assert.Equal(isPartialAvailable, partialSolution.ProjectIds.Contains(projectId) && partialSolution.GetProject(projectId).TryGetCompilation(out compilation));
            }
        }
#endif

        private Solution CreateSolutionWithProjectDependencyChain(int projectCount)
        {
            Solution solution = this.CreateNotKeptAliveSolution();
            projectCount = 5;
            var projectIds = Enumerable.Range(0, projectCount).Select(i => ProjectId.CreateNewId()).ToArray();
            for (int i = 0; i < projectCount; i++)
            {
                solution = solution.AddProject(projectIds[i], i.ToString(), i.ToString(), LanguageNames.CSharp);
                if (i >= 1)
                {
                    solution = solution.AddProjectReference(projectIds[i], new ProjectReference(projectIds[i - 1]));
                }
            }

            return solution;
        }

        [WorkItem(636431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636431")]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestProjectDependencyLoading()
        {
            int projectCount = 3;
            var solution = CreateSolutionWithProjectDependencyChain(projectCount);
            ProjectId[] projectIds = solution.ProjectIds.ToArray();

            var compilation0 = solution.GetProject(projectIds[0]).GetCompilationAsync(CancellationToken.None).Result;
            var compilation2 = solution.GetProject(projectIds[2]).GetCompilationAsync(CancellationToken.None).Result;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddMetadataReferences()
        {
            var csharpReference = MetadataReference.CreateFromImage(GetResourceBytes(@"CSharpProject.dll"));
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "foo", "foo.dll", LanguageNames.CSharp);
            solution = solution.AddMetadataReference(project1, s_mscorlib);

            // For CSharp Reference
            solution = solution.AddMetadataReference(project1, csharpReference);
            var assemblyReference = (IAssemblySymbol)solution.GetProject(project1).GetCompilationAsync().Result.GetAssemblyOrModuleSymbol(csharpReference);
            var namespacesAndTypes = assemblyReference.GlobalNamespace.GetAllNamespacesAndTypes(CancellationToken.None);
            var foundSymbol = from symbol in namespacesAndTypes
                              where symbol.Name.Equals("CSharpClass")
                              select symbol;
            Assert.Equal(1, foundSymbol.Count());
            solution = solution.RemoveMetadataReference(project1, csharpReference);
            assemblyReference = (IAssemblySymbol)solution.GetProject(project1).GetCompilationAsync().Result.GetAssemblyOrModuleSymbol(csharpReference);
            Assert.Null(assemblyReference);

            ValidateSolutionAndCompilations(solution);
        }

        private class MockDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override void Initialize(AnalysisContext analysisContext)
            {
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestProjectDiagnosticAnalyzers()
        {
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "foo", "foo.dll", LanguageNames.CSharp);
            Assert.Empty(solution.Projects.Single().AnalyzerReferences);

            DiagnosticAnalyzer analyzer = new MockDiagnosticAnalyzer();
            var analyzerReference = new AnalyzerImageReference(ImmutableArray.Create(analyzer));

            // Test AddAnalyzer
            var newSolution = solution.AddAnalyzerReference(project1, analyzerReference);
            var actualAnalyzerReferences = newSolution.Projects.Single().AnalyzerReferences;
            Assert.Equal(1, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            var actualAnalyzers = actualAnalyzerReferences[0].GetAnalyzersForAllLanguages();
            Assert.Equal(1, actualAnalyzers.Length);
            Assert.Equal(analyzer, actualAnalyzers[0]);

            // Test ProjectChanges
            var changes = newSolution.GetChanges(solution).GetProjectChanges().Single();
            var addedAnalyzerReference = changes.GetAddedAnalyzerReferences().Single();
            Assert.Equal(analyzerReference, addedAnalyzerReference);
            var removedAnalyzerReferences = changes.GetRemovedAnalyzerReferences();
            Assert.Empty(removedAnalyzerReferences);
            solution = newSolution;

            // Test RemoveAnalyzer
            solution = solution.RemoveAnalyzerReference(project1, analyzerReference);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Empty(actualAnalyzerReferences);

            // Test AddAnalyzers
            analyzerReference = new AnalyzerImageReference(ImmutableArray.Create(analyzer));
            DiagnosticAnalyzer secondAnalyzer = new MockDiagnosticAnalyzer();
            var secondAnalyzerReference = new AnalyzerImageReference(ImmutableArray.Create(secondAnalyzer));
            var analyzerReferences = new[] { analyzerReference, secondAnalyzerReference };
            solution = solution.AddAnalyzerReferences(project1, analyzerReferences);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(2, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[1]);

            solution = solution.RemoveAnalyzerReference(project1, analyzerReference);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(1, actualAnalyzerReferences.Count);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[0]);

            // Test WithAnalyzers
            solution = solution.WithProjectAnalyzerReferences(project1, analyzerReferences);
            actualAnalyzerReferences = solution.Projects.Single().AnalyzerReferences;
            Assert.Equal(2, actualAnalyzerReferences.Count);
            Assert.Equal(analyzerReference, actualAnalyzerReferences[0]);
            Assert.Equal(secondAnalyzerReference, actualAnalyzerReferences[1]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestProjectCompilationOptions()
        {
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "foo", "foo.dll", LanguageNames.CSharp);
            solution = solution.AddMetadataReference(project1, s_mscorlib);

            // Compilation Options
            var oldCompOptions = solution.GetProject(project1).CompilationOptions;
            var newCompOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication, mainTypeName: "After");
            solution = solution.WithProjectCompilationOptions(project1, newCompOptions);
            var newUpdatedCompOptions = solution.GetProject(project1).CompilationOptions;
            Assert.NotEqual(oldCompOptions, newUpdatedCompOptions);
            Assert.Same(newCompOptions, newUpdatedCompOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestProjectParseOptions()
        {
            var solution = CreateSolution();
            var project1 = ProjectId.CreateNewId();
            solution = solution.AddProject(project1, "foo", "foo.dll", LanguageNames.CSharp);
            solution = solution.AddMetadataReference(project1, s_mscorlib);

            // Parse Options
            var oldParseOptions = solution.GetProject(project1).ParseOptions;
            var newParseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "AFTER" });
            solution = solution.WithProjectParseOptions(project1, newParseOptions);
            var newUpdatedParseOptions = solution.GetProject(project1).ParseOptions;
            Assert.NotEqual(oldParseOptions, newUpdatedParseOptions);
            Assert.Same(newParseOptions, newUpdatedParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRemoveProject()
        {
            var sol = CreateSolution();

            var pid = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp);
            Assert.True(sol.ProjectIds.Any(), "Solution was expected to have projects");
            Assert.NotNull(pid);
            var project = sol.GetProject(pid);
            Assert.False(project.HasDocuments);

            var sol2 = sol.RemoveProject(pid);
            Assert.False(sol2.ProjectIds.Any());

            ValidateSolutionAndCompilations(sol);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRemoveProjectWithReferences()
        {
            var sol = CreateSolution();

            var pid = ProjectId.CreateNewId();
            var pid2 = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                   .AddProject(pid2, "bar", "bar.dll", LanguageNames.CSharp)
                   .AddProjectReference(pid2, new ProjectReference(pid));

            Assert.Equal(2, sol.Projects.Count());

            // remove the project that is being referenced
            // this should leave a dangling reference
            var sol2 = sol.RemoveProject(pid);

            Assert.False(sol2.ContainsProject(pid));
            Assert.True(sol2.ContainsProject(pid2), "sol2 was expected to contain project " + pid2);
            Assert.Equal(1, sol2.Projects.Count());
            Assert.True(sol2.GetProject(pid2).AllProjectReferences.Any(r => r.ProjectId == pid), "sol2 project pid2 was expected to contain project reference " + pid);

            ValidateSolutionAndCompilations(sol2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRemoveProjectWithReferencesAndAddItBack()
        {
            var sol = CreateSolution();

            var pid = ProjectId.CreateNewId();
            var pid2 = ProjectId.CreateNewId();
            sol = sol.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                   .AddProject(pid2, "bar", "bar.dll", LanguageNames.CSharp)
                   .AddProjectReference(pid2, new ProjectReference(pid));

            Assert.Equal(2, sol.Projects.Count());

            // remove the project that is being referenced
            var sol2 = sol.RemoveProject(pid);

            Assert.False(sol2.ContainsProject(pid));
            Assert.True(sol2.ContainsProject(pid2), "sol2 was expected to contain project " + pid2);
            Assert.Equal(1, sol2.Projects.Count());
            Assert.True(sol2.GetProject(pid2).AllProjectReferences.Any(r => r.ProjectId == pid), "sol2 pid2 was expected to contain " + pid);

            var sol3 = sol2.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp);

            Assert.True(sol3.ContainsProject(pid), "sol3 was expected to contain " + pid);
            Assert.True(sol3.ContainsProject(pid2), "sol3 was expected to contain " + pid2);
            Assert.Equal(2, sol3.Projects.Count());

            ValidateSolutionAndCompilations(sol3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxRoot()
        {
            var text = "public class Foo { }";

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var sol = CreateSolution()
                .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                .AddDocument(did, "foo.cs", text);

            var document = sol.GetDocument(did);

            SyntaxNode root;
            Assert.Equal(false, document.TryGetSyntaxRoot(out root));

            root = document.GetSyntaxRootAsync().Result;
            Assert.NotNull(root);
            Assert.Equal(text, root.ToString());

            Assert.Equal(true, document.TryGetSyntaxRoot(out root));
            Assert.NotNull(root);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestUpdateDocument()
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution1 = CreateSolution()
                .AddProject(projectId, "ProjectName", "AssemblyName", LanguageNames.CSharp)
                .AddDocument(documentId, "DocumentName", SourceText.From("class Class{}"));

            var document = solution1.GetDocument(documentId);
            var newRoot = Formatter.FormatAsync(document).Result.GetSyntaxRootAsync().Result;
            var solution2 = solution1.WithDocumentSyntaxRoot(documentId, newRoot);

            Assert.NotEqual(solution1, solution2);

            var newText = solution2.GetDocument(documentId).GetTextAsync().Result.ToString();
            Assert.Equal("class Class { }", newText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestUpdateSyntaxTreeWithAnnotations()
        {
            var text = "public class Foo { }";

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var sol = CreateSolution()
                .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                .AddDocument(did, "foo.cs", text);

            var document = sol.GetDocument(did);
            var tree = document.GetSyntaxTreeAsync().Result;
            var root = tree.GetRoot();

            var annotation = new SyntaxAnnotation();
            var annotatedRoot = root.WithAdditionalAnnotations(annotation);

            var sol2 = sol.WithDocumentSyntaxRoot(did, annotatedRoot);
            var doc2 = sol2.GetDocument(did);
            var tree2 = doc2.GetSyntaxTreeAsync().Result;
            var root2 = tree2.GetRoot();

            // text should not be available yet (it should be defer created from the node)
            // and getting the document or root should not cause it to be created.
            SourceText text2;
            Assert.Equal(false, tree2.TryGetText(out text2));

            text2 = tree2.GetText();
            Assert.NotNull(text2);

            Assert.NotSame(tree, tree2);
            Assert.NotSame(annotatedRoot, root2);

            Assert.Equal(true, annotatedRoot.IsEquivalentTo(root2));
            Assert.Equal(true, root2.HasAnnotation(annotation));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestSyntaxRootNotKeptAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var sol = CreateNotKeptAliveSolution()
                .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                .AddDocument(did, "foo.cs", "public class Foo { }");

            var observedRoot = GetObservedSyntaxTreeRoot(sol, did);
            StopObservingAndWaitForReferenceToGo(observedRoot);

            // re-get the tree (should recover from storage, not reparse)
            var root = sol.GetDocument(did).GetSyntaxRootAsync().Result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        [WorkItem(542736, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542736")]
        public void TestDocumentChangedOnDiskIsNotObserved()
        {
            var text1 = "public class A {}";
            var text2 = "public class B {}";

            var file = Temp.CreateFile().WriteAllText(text1, Encoding.UTF8);

            // create a solution that evicts from the cache immediately.
            var sol = CreateNotKeptAliveSolution();

            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            sol = sol.AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                     .AddDocument(did, "x", new FileTextLoader(file.Path, Encoding.UTF8));

            var observedText = GetObservedText(sol, did, text1);

            // change text on disk & verify it is changed
            file.WriteAllText(text2);
            var textOnDisk = file.ReadAllText();
            Assert.Equal(text2, textOnDisk);

            // stop observing it and let GC reclaim it
            StopObservingAndWaitForReferenceToGo(observedText);

            // if we ask for the same text again we should get the original content
            var observedText2 = sol.GetDocument(did).GetTextAsync().Result;
            Assert.Equal(text1, observedText2.ToString());
        }

        private Solution CreateNotKeptAliveSolution()
        {
            var workspace = new AdhocWorkspace(TestHost.Services, "NotKeptAlive");
            workspace.Options = workspace.Options.WithChangedOption(CacheOptions.RecoverableTreeLengthThreshold, 0);
            return workspace.CurrentSolution;
        }

        [DllImport("dbghelp.dll")]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, int ProcessId, IntPtr hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

        private static void DumpProcess(string dumpFileName)
        {
            using (var fs = File.Create(dumpFileName))
            {
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                MiniDumpWriteDump(proc.Handle, proc.Id, fs.SafeFileHandle.DangerousGetHandle(), /*MiniDumpWithFullMemory*/2, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void StopObservingAndWaitForReferenceToGo(ObjectReference observed, int delay = 0, string dumpFileName = null)
        {
            // stop observing it and let GC reclaim it
            observed.Strong = null;

            DateTime start = DateTime.UtcNow;
            TimeSpan maximumTimeToWait = TimeSpan.FromSeconds(120);

            while (observed.Weak.IsAlive && (DateTime.UtcNow - start) < maximumTimeToWait)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            const int TimerPrecision = 30;
            var actualTimePassed = DateTime.UtcNow - start + TimeSpan.FromMilliseconds(TimerPrecision);
            var isTargetCollected = observed.Weak.Target == null;

            if (!isTargetCollected && !string.IsNullOrEmpty(dumpFileName))
            {
                if (string.Compare(Path.GetExtension(dumpFileName), ".dmp", StringComparison.OrdinalIgnoreCase) != 0)
                {
                    dumpFileName += ".dmp";
                }

                DumpProcess(dumpFileName);
                Assert.True(false, $"Target object not collected.  Process dump saved to '{dumpFileName}'");
            }

            Assert.True(isTargetCollected,
                string.Format("Target object ({0}) was not collected after {1} ms", observed.Weak.Target, actualTimePassed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            var doc = sol.GetDocument(did);

            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetLoadedTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var file = Temp.CreateFile().WriteAllText(text, Encoding.UTF8);

            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "x", new FileTextLoader(file.Path, Encoding.UTF8));

            var doc = sol.GetDocument(did);

            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetRecoveredTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedText(sol, did, text);
            StopObservingAndWaitForReferenceToGo(observed);

            // get it async and force it to recover from temporary storage
            var doc = sol.GetDocument(did);
            var docText = doc.GetTextAsync().Result;

            Assert.NotNull(docText);
            Assert.Equal(text, docText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxTreeAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            var doc = sol.GetDocument(did);

            var docTree = doc.GetSyntaxTreeAsync().Result;

            Assert.NotNull(docTree);
            Assert.Equal(text, docTree.GetRoot().ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxTreeFromLoadedTextAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var file = Temp.CreateFile().WriteAllText(text, Encoding.UTF8);

            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "x", new FileTextLoader(file.Path, Encoding.UTF8));

            var doc = sol.GetDocument(did);
            var docTree = doc.GetSyntaxTreeAsync().Result;

            Assert.NotNull(docTree);
            Assert.Equal(text, docTree.GetRoot().ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxTreeFromAddedTree()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var tree = CSharp.SyntaxFactory.ParseSyntaxTree("public class C {}").GetRoot(CancellationToken.None);
            tree = tree.WithAdditionalAnnotations(new SyntaxAnnotation("test"));

            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "x", tree);

            var doc = sol.GetDocument(did);
            var docTree = doc.GetSyntaxRootAsync().Result;

            Assert.NotNull(docTree);
            Assert.True(tree.IsEquivalentTo(docTree));
            Assert.NotNull(docTree.GetAnnotatedNodes("test").Single());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxRootAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            var doc = sol.GetDocument(did);

            var docRoot = doc.GetSyntaxRootAsync().Result;

            Assert.NotNull(docRoot);
            Assert.Equal(text, docRoot.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetRecoveredSyntaxRootAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";

            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // observe the syntax tree root and wait for the references to be GC'd
            var observed = GetObservedSyntaxTreeRoot(sol, did);
            StopObservingAndWaitForReferenceToGo(observed);

            // get it async and force it to be recovered from storage
            var doc = sol.GetDocument(did);
            var docRoot = doc.GetSyntaxRootAsync().Result;

            Assert.NotNull(docRoot);
            Assert.Equal(text, docRoot.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetCompilationAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            var proj = sol.GetProject(pid);

            var compilation = proj.GetCompilationAsync().Result;

            Assert.NotNull(compilation);
            Assert.Equal(1, compilation.SyntaxTrees.Count());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSemanticModelAsync()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            var doc = sol.GetDocument(did);

            var docModel = doc.GetSemanticModelAsync().Result;
            Assert.NotNull(docModel);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetTextDoesNotKeepTextAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedText(sol, did, text);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedText(Solution solution, DocumentId documentId, string expectedText = null)
        {
            var observedText = solution.GetDocument(documentId).GetTextAsync().Result;

            if (expectedText != null)
            {
                Assert.Equal(expectedText, observedText.ToString());
            }

            return new ObjectReference(observedText);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetTextAsyncDoesNotKeepTextAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // observe the text and then wait for the references to be GC'd
            var observed = GetObservedTextAsync(sol, did, text);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedTextAsync(Solution solution, DocumentId documentId, string expectedText = null)
        {
            var observedText = solution.GetDocument(documentId).GetTextAsync().Result;

            if (expectedText != null)
            {
                Assert.Equal(expectedText, observedText.ToString());
            }

            return new ObjectReference(observedText);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxRootDoesNotKeepRootAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";

            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRoot(sol, did);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedSyntaxTreeRoot(Solution solution, DocumentId documentId)
        {
            var observedTree = solution.GetDocument(documentId).GetSyntaxRootAsync().Result;
            return new ObjectReference(observedTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetSyntaxRootAsyncDoesNotKeepRootAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";

            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRootAsync(sol, did);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedSyntaxTreeRootAsync(Solution solution, DocumentId documentId)
        {
            var observedTree = solution.GetDocument(documentId).GetSyntaxRootAsync().Result;
            return new ObjectReference(observedTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRecoverableSyntaxTreeCSharp()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = @"public class C {
    public void Method1() {}
    public void Method2() {}
    public void Method3() {}
    public void Method4() {}
    public void Method5() {}
    public void Method6() {}
}";

            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            TestRecoverableSyntaxTree(sol, did);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRecoverableSyntaxTreeVisualBasic()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = @"Public Class C
    Sub Method1()
    End Sub
    Sub Method2()
    End Sub
    Sub Method3()
    End Sub
    Sub Method4()
    End Sub
    Sub Method5()
    End Sub
    Sub Method6()
    End Sub
End Class";

            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.VisualBasic)
                        .AddDocument(did, "foo.vb", text);

            TestRecoverableSyntaxTree(sol, did);
        }

        private void TestRecoverableSyntaxTree(Solution sol, DocumentId did, [CallerMemberName] string callingTest = null)
        {
            // get it async and wait for it to get GC'd
            var observed = GetObservedSyntaxTreeRootAsync(sol, did);
            StopObservingAndWaitForReferenceToGo(observed, dumpFileName: callingTest);

            var doc = sol.GetDocument(did);

            // access the tree & root again (recover it)
            var tree = doc.GetSyntaxTreeAsync().Result;

            // this should cause reparsing
            var root = tree.GetRoot();

            // prove that the new root is correctly associated with the tree
            Assert.Equal(tree, root.SyntaxTree);

            // reset the syntax root, to make it 'refactored' by adding an attribute
            var newRoot = doc.GetSyntaxRootAsync().Result.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
            var doc2 = doc.Project.Solution.WithDocumentSyntaxRoot(doc.Id, newRoot, PreservationMode.PreserveValue).GetDocument(doc.Id);

            // get it async and wait for it to get GC'd
            var observed2 = GetObservedSyntaxTreeRootAsync(doc2.Project.Solution, did);
            StopObservingAndWaitForReferenceToGo(observed2, dumpFileName: callingTest);

            // access the tree & root again (recover it)
            var tree2 = doc2.GetSyntaxTreeAsync().Result;

            // this should cause deserialization
            var root2 = tree2.GetRoot();

            // prove that the new root is correctly associated with the tree
            Assert.Equal(tree2, root2.SyntaxTree);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetCompilationAsyncDoesNotKeepCompilationAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedCompilationAsync(sol, pid);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedCompilationAsync(Solution solution, ProjectId projectId)
        {
            var observed = solution.GetProject(projectId).GetCompilationAsync().Result;
            return new ObjectReference(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestGetCompilationDoesNotKeepCompilationAlive()
        {
            var pid = ProjectId.CreateNewId();
            var did = DocumentId.CreateNewId(pid);

            var text = "public class C {}";
            var sol = CreateNotKeptAliveSolution()
                        .AddProject(pid, "foo", "foo.dll", LanguageNames.CSharp)
                        .AddDocument(did, "foo.cs", text);

            // get it async and wait for it to get GC'd
            var observed = GetObservedCompilation(sol, pid);
            StopObservingAndWaitForReferenceToGo(observed);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ObjectReference GetObservedCompilation(Solution solution, ProjectId projectId)
        {
            var observed = solution.GetProject(projectId).GetCompilationAsync().Result;
            return new ObjectReference(observed);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestLoadProjectFromCommandLine()
        {
            string commandLine = @"foo.cs subdir\bar.cs /out:foo.dll /target:library";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");
            var ws = new AdhocWorkspace();
            ws.AddProject(info);
            var project = ws.CurrentSolution.GetProject(info.Id);

            Assert.Equal("TestProject", project.Name);
            Assert.Equal("foo", project.AssemblyName);
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

            Assert.Equal(2, project.Documents.Count());

            var fooDoc = project.Documents.First(d => d.Name == "foo.cs");
            Assert.Equal(0, fooDoc.Folders.Count);
            Assert.Equal(@"C:\ProjectDirectory\foo.cs", fooDoc.FilePath);

            var barDoc = project.Documents.First(d => d.Name == "bar.cs");
            Assert.Equal(1, barDoc.Folders.Count);
            Assert.Equal("subdir", barDoc.Folders[0]);
            Assert.Equal(@"C:\ProjectDirectory\subdir\bar.cs", barDoc.FilePath);
        }

        public void TestCommandLineProjectWithRelativePathOutsideProjectCone()
        {
            string commandLine = @"..\foo.cs";
            var ws = new AdhocWorkspace();
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");

            var docInfo = info.Documents.First();
            Assert.Equal(0, docInfo.Folders.Count);
            Assert.Equal("foo.cs", docInfo.Name);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestWorkspaceLanguageServiceOverride()
        {
            var ws = new AdhocWorkspace(TestHost.Services, ServiceLayer.Host);
            var service = ws.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ITestLanguageService>();
            Assert.NotNull(service as TestLanguageServiceA);

            var ws2 = new AdhocWorkspace(TestHost.Services, "Quasimodo");
            var service2 = ws2.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ITestLanguageService>();
            Assert.NotNull(service2 as TestLanguageServiceB);
        }

#if false
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestSolutionInfo()
        {
            var oldSolutionId = SolutionId.CreateNewId("oldId");
            var oldVersion = VersionStamp.Create();
            var solutionInfo = SolutionInfo.Create(oldSolutionId, oldVersion, null, null);

            var newSolutionId = SolutionId.CreateNewId("newId");
            solutionInfo = solutionInfo.WithId(newSolutionId);
            Assert.NotEqual(oldSolutionId, solutionInfo.Id);
            Assert.Equal(newSolutionId, solutionInfo.Id);
            
            var newVersion = oldVersion.GetNewerVersion();
            solutionInfo = solutionInfo.WithVersion(newVersion);
            Assert.NotEqual(oldVersion, solutionInfo.Version);
            Assert.Equal(newVersion, solutionInfo.Version);

            Assert.Equal(null, solutionInfo.FilePath);
            var newFilePath = @"C:\test\fake.sln";
            solutionInfo = solutionInfo.WithFilePath(newFilePath);
            Assert.Equal(newFilePath, solutionInfo.FilePath);

            Assert.Equal(0, solutionInfo.Projects.Count());
        }
#endif

        private interface ITestLanguageService : ILanguageService
        {
        }

        [ExportLanguageService(typeof(ITestLanguageService), LanguageNames.CSharp, ServiceLayer.Default), Shared]
        private class TestLanguageServiceA : ITestLanguageService
        {
        }

        [ExportLanguageService(typeof(ITestLanguageService), LanguageNames.CSharp, "Quasimodo"), Shared]
        private class TestLanguageServiceB : ITestLanguageService
        {
        }

        [Fact]
        public void TestDocumentFileAccessFailureMissingFile()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            WorkspaceDiagnostic diagnostic = null;

            solution.Workspace.WorkspaceFailed += (sender, args) =>
            {
                diagnostic = args.Diagnostic;
            };

            ProjectId pid = ProjectId.CreateNewId();
            DocumentId did = DocumentId.CreateNewId(pid);

            solution = solution.AddProject(pid, "foo", "foo", LanguageNames.CSharp)
                               .AddDocument(did, "x", new FileTextLoader(@"C:\doesnotexist.cs", Encoding.UTF8));

            var doc = solution.GetDocument(did);
            var text = doc.GetTextAsync().Result;

            WaitFor(() => diagnostic != null, TimeSpan.FromSeconds(5));

            Assert.NotNull(diagnostic);
            var dd = diagnostic as DocumentDiagnostic;
            Assert.NotNull(dd);
            Assert.Equal(did, dd.DocumentId);
            Assert.Equal(WorkspaceDiagnosticKind.Failure, dd.Kind);
        }

        [Fact]
        [WorkItem(666263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666263")]
        public void TestWorkspaceDiagnosticHasDebuggerText()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            WorkspaceDiagnostic diagnostic = null;

            solution.Workspace.WorkspaceFailed += (sender, args) =>
            {
                diagnostic = args.Diagnostic;
            };

            ProjectId pid = ProjectId.CreateNewId();
            DocumentId did = DocumentId.CreateNewId(pid);

            solution = solution.AddProject(pid, "foo", "foo", LanguageNames.CSharp)
                               .AddDocument(did, "x", new FileTextLoader(@"C:\doesnotexist.cs", Encoding.UTF8));

            var doc = solution.GetDocument(did);
            var text = doc.GetTextAsync().Result;

            WaitFor(() => diagnostic != null, TimeSpan.FromSeconds(5));

            Assert.NotNull(diagnostic);
            var dd = diagnostic as DocumentDiagnostic;
            Assert.NotNull(dd);
            Assert.Equal(dd.ToString(), string.Format("[{0}] {1}", WorkspacesResources.Failure, dd.Message));
        }

        private bool WaitFor(Func<bool> condition, TimeSpan timeout)
        {
            DateTime start = DateTime.UtcNow;

            while ((DateTime.UtcNow - start) < timeout && !condition())
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(10));
            }

            return condition();
        }

        [Fact]
        public void TestGetProjectForAssemblySymbol()
        {
            var pid1 = ProjectId.CreateNewId("p1");
            var pid2 = ProjectId.CreateNewId("p2");
            var pid3 = ProjectId.CreateNewId("p3");
            var did1 = DocumentId.CreateNewId(pid1);
            var did2 = DocumentId.CreateNewId(pid2);
            var did3 = DocumentId.CreateNewId(pid3);

            var text1 = @"
Public Class A
End Class";

            var text2 = @"
Public Class B
End Class
";

            var text3 = @"
public class C : B {
}
";

            var text4 = @"
public class C : A {
}
";

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(pid1, "FooA", "Foo.dll", LanguageNames.VisualBasic)
                .AddDocument(did1, "A.vb", text1)
                .AddMetadataReference(pid1, s_mscorlib)
                .AddProject(pid2, "FooB", "Foo2.dll", LanguageNames.VisualBasic)
                .AddDocument(did2, "B.vb", text2)
                .AddMetadataReference(pid2, s_mscorlib)
                .AddProject(pid3, "Bar", "Bar.dll", LanguageNames.CSharp)
                .AddDocument(did3, "C.cs", text3)
                .AddMetadataReference(pid3, s_mscorlib)
                .AddProjectReference(pid3, new ProjectReference(pid1))
                .AddProjectReference(pid3, new ProjectReference(pid2));

            var project3 = solution.GetProject(pid3);
            var comp3 = project3.GetCompilationAsync().Result;
            var classC = comp3.GetTypeByMetadataName("C");
            var projectForBaseType = solution.GetProject(classC.BaseType.ContainingAssembly);
            Assert.Equal(pid2, projectForBaseType.Id);

            // switch base type to A then try again
            var solution2 = solution.WithDocumentText(did3, SourceText.From(text4));
            project3 = solution2.GetProject(pid3);
            comp3 = project3.GetCompilationAsync().Result;
            classC = comp3.GetTypeByMetadataName("C");
            projectForBaseType = solution2.GetProject(classC.BaseType.ContainingAssembly);
            Assert.Equal(pid1, projectForBaseType.Id);
        }

        [WorkItem(1088127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1088127")]
        [Fact]
        public void TestEncodingRetainedAfterTreeChanged()
        {
            var ws = new AdhocWorkspace();
            var proj = ws.AddProject("proj", LanguageNames.CSharp);
            var doc = ws.AddDocument(proj.Id, "a.cs", SourceText.From("public class c { }", Encoding.UTF32));

            Assert.Equal(Encoding.UTF32, doc.GetTextAsync().Result.Encoding);

            // updating root doesn't change original encoding
            var root = doc.GetSyntaxRootAsync().Result;
            var newRoot = root.WithLeadingTrivia(root.GetLeadingTrivia().Add(CS.SyntaxFactory.Whitespace("    ")));
            var newDoc = doc.WithSyntaxRoot(newRoot);

            Assert.Equal(Encoding.UTF32, newDoc.GetTextAsync().Result.Encoding);
        }

        [Fact]
        public void TestProjectWithNoBrokenReferencesHasNoIncompleteReferences()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject("CSharpProject", LanguageNames.CSharp);
            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            // Nothing should have incomplete references, and everything should build
            Assert.True(project1.HasCompleteReferencesAsync().Result);
            Assert.True(project2.HasCompleteReferencesAsync().Result);
            Assert.Single(project2.GetCompilationAsync().Result.ExternalReferences);
        }

        [Fact]
        public void TestProjectWithBrokenCrossLanguageReferenceHasIncompleteReferences()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject("CSharpProject", LanguageNames.CSharp);
            workspace.AddDocument(project1.Id, "Broken.cs", SourceText.From("class "));

            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            Assert.True(project1.HasCompleteReferencesAsync().Result);
            Assert.False(project2.HasCompleteReferencesAsync().Result);
            Assert.Empty(project2.GetCompilationAsync().Result.ExternalReferences);
        }

        [Fact]
        public void TestFrozenPartialProjectAlwaysIsIncomplete()
        {
            var workspace = new AdhocWorkspace();
            var project1 = workspace.AddProject("CSharpProject", LanguageNames.CSharp);

            var project2 = workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "VisualBasicProject",
                    "VisualBasicProject",
                    LanguageNames.VisualBasic,
                    projectReferences: new[] { new ProjectReference(project1.Id) }));

            var document = workspace.AddDocument(project2.Id, "Test.cs", SourceText.From(""));

            // Nothing should have incomplete references, and everything should build
            var frozenSolution = document.WithFrozenPartialSemanticsAsync(CancellationToken.None).Result.Project.Solution;

            Assert.True(frozenSolution.GetProject(project1.Id).HasCompleteReferencesAsync().Result);
            Assert.True(frozenSolution.GetProject(project2.Id).HasCompleteReferencesAsync().Result);
        }
    }
}

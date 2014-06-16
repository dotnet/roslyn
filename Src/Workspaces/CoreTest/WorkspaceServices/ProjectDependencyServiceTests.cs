// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Host.UnitTests
{
    public class ProjectDependencyServiceTests : TestBase
    {
        [WorkItem(8683, "DevDiv_Projects/Roslyn"), WorkItem(542393)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestMoveToLatestSolution()
        {
            var workspace = new TestWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = workspace.AddProject("P1");
            var graph = ProjectDependencyService.GetDependencyGraphAsync(workspace.CurrentSolution, CancellationToken.None).Result;
            var project2 = workspace.AddProject("P2");
            graph = ProjectDependencyService.GetDependencyGraphAsync(workspace.CurrentSolution, CancellationToken.None).Result;
            var sortedProjects = graph.GetTopologicallySortedProjects();
            AssertEx.SetEqual(sortedProjects, project1, project2);
            workspace.OnAssemblyNameChanged(project1, "ChangedP1");                     
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void Test_WorkspaceChanges()
        {
            var workspace = new TestWorkspace();
            var solution = workspace.CurrentSolution;
            var project1 = workspace.AddProject("P1");
            var graph = ProjectDependencyService.GetDependencyGraphAsync(workspace.CurrentSolution, CancellationToken.None).Result;
            var project2 = workspace.AddProject("P2");
            graph = ProjectDependencyService.GetDependencyGraphAsync(workspace.CurrentSolution, CancellationToken.None).Result;
            var sortedProjects = graph.GetTopologicallySortedProjects();
            AssertEx.SetEqual(sortedProjects, project1, project2);
            
            Project ps = workspace.CurrentSolution.GetProject(project1);
            int startCount = ps.MetadataReferences.Count;

            var source2 = @"
using System;
public class X
{
}
";
            MetadataReference comp1 = CreateCSharpCompilation(source2).ToMetadataReference();            
            workspace.OnMetadataReferenceAdded(project1, comp1);
            workspace.OnAssemblyNameChanged(project1, "ChangedP1");
            
            Assert.False(ps.CompilationOptions.CheckOverflow);
            CompilationOptions co = new CSharp.CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, checkOverflow: true);            
            workspace.OnCompilationOptionsChanged(project1, co);
            
            ps = workspace.CurrentSolution.GetProject(project1);
            Assert.Equal(startCount + 1, ps.MetadataReferences.Count);
            Assert.Equal(ps.AssemblyName, "ChangedP1");
            Assert.True(ps.CompilationOptions.CheckOverflow);            
        }

        [WorkItem(705220)]
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void Test_WorkspaceOutputFilePathChanges()
        {
            var workspace = new TestWorkspace();
            var solution = workspace.CurrentSolution;
            var project = workspace.AddProject("P1");
            Project ps = workspace.CurrentSolution.GetProject(project);
            Assert.Equal(null, ps.OutputFilePath);
            workspace.OnOutputFilePathChanged(project, "NewPath");            
            ps = workspace.CurrentSolution.GetProject(project);
            Assert.Equal("NewPath", ps.OutputFilePath);
        }

        private CS.CSharpCompilation CreateCSharpCompilation(string sourceText)
        {
            MetadataReference mscorlib = new MetadataFileReference(typeof(int).Assembly.Location);
            var syntaxTree = CS.SyntaxFactory.ParseSyntaxTree(sourceText);
            return (CS.CSharpCompilation)CS.CSharpCompilation.Create("foo.exe").AddReferences(mscorlib).AddSyntaxTrees(syntaxTree);
        }
    }
}

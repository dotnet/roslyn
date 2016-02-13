// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceTests
{
    public class WorkspaceReferenceTests
    {
        [Fact]
        public async Task CheckPEReferencesSameAfterSolutionChangedTest()
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var project = ws.AddProject(projectInfo);

                // get original references
                var compilation1 = await project.GetCompilationAsync();
                var references1 = compilation1.ExternalReferences;

                // just some arbitary action to create new snpahost that doesnt affect references
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var document = ws.AddDocument(info);

                // get new compilation
                var compilation2 = await document.Project.GetCompilationAsync();
                var references2 = compilation2.ExternalReferences;

                Assert.Equal(references1, references2);
            }
        }

        [Fact]
        public async Task CheckP2PReferencesSameAfterSolutionChangedTest()
        {
            using (var ws = new AdhocWorkspace())
            {
                var referenceInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "ReferenceProject",
                    "ReferenceProject",
                    LanguageNames.CSharp,
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var referenceProject = ws.AddProject(referenceInfo);

                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    projectReferences: ImmutableArray.Create<ProjectReference>(new ProjectReference(referenceInfo.Id)),
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var project = ws.AddProject(projectInfo);

                // get original references
                var compilation1 = await project.GetCompilationAsync();
                var references1 = compilation1.ExternalReferences;

                // just some arbitary action to create new snpahost that doesnt affect references
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var document = ws.AddDocument(info);

                // get new compilation
                var compilation2 = await document.Project.GetCompilationAsync();
                var references2 = compilation2.ExternalReferences;

                Assert.Equal(references1, references2);
            }
        }

        [Fact]
        public async Task CheckCrossLanguageReferencesSameAfterSolutionChangedTest()
        {
            using (var ws = new AdhocWorkspace())
            {
                var referenceInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "ReferenceProject",
                    "ReferenceProject",
                    LanguageNames.VisualBasic,
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var referenceProject = ws.AddProject(referenceInfo);

                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    projectReferences: ImmutableArray.Create<ProjectReference>(new ProjectReference(referenceInfo.Id)),
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var project = ws.AddProject(projectInfo);

                // get original references
                var compilation1 = await project.GetCompilationAsync();
                var references1 = compilation1.ExternalReferences;

                // just some arbitary action to create new snpahost that doesnt affect references
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var document = ws.AddDocument(info);

                // get new compilation
                var compilation2 = await document.Project.GetCompilationAsync();
                var references2 = compilation2.ExternalReferences;

                Assert.Equal(references1, references2);
            }
        }

        [Fact]
        public async Task CheckP2PReferencesNotSameAfterReferenceChangedTest()
        {
            using (var ws = new AdhocWorkspace())
            {
                var referenceInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "ReferenceProject",
                    "ReferenceProject",
                    LanguageNames.CSharp,
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var referenceProject = ws.AddProject(referenceInfo);

                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    projectReferences: ImmutableArray.Create<ProjectReference>(new ProjectReference(referenceInfo.Id)),
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var project = ws.AddProject(projectInfo);

                // get original references
                var compilation1 = await project.GetCompilationAsync();
                var references1 = compilation1.ExternalReferences;

                // some random action that causes reference project to be changed.
                var referenceDocumentInfo = DocumentInfo.Create(DocumentId.CreateNewId(referenceProject.Id), "code.cs");
                var referenceDocument = ws.AddDocument(referenceDocumentInfo);

                // just some arbitary action to create new snpahost that doesnt affect references
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var document = ws.AddDocument(info);

                // get new compilation
                var compilation2 = await document.Project.GetCompilationAsync();
                var references2 = compilation2.ExternalReferences;

                Assert.NotEqual(references1, references2);
            }
        }

        [Fact]
        public async Task CheckPEReferencesNotSameAfterReferenceChangedTest()
        {
            using (var ws = new AdhocWorkspace())
            {
                var projectInfo = ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Create(),
                    "TestProject",
                    "TestProject",
                    LanguageNames.CSharp,
                    metadataReferences: ImmutableArray.Create<MetadataReference>(PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location)));

                var project = ws.AddProject(projectInfo);

                // get original references
                var compilation1 = await project.GetCompilationAsync();
                var references1 = compilation1.ExternalReferences;

                // explicitly change references
                var forkedProject = project.WithMetadataReferences(ImmutableArray.Create<MetadataReference>(
                    PortableExecutableReference.CreateFromFile(typeof(object).Assembly.Location),
                    PortableExecutableReference.CreateFromFile(typeof(Workspace).Assembly.Location)));

                // get new compilation
                var compilation2 = await forkedProject.GetCompilationAsync();
                var references2 = compilation2.ExternalReferences;

                Assert.NotEqual(references1, references2);
            }
        }
    }
}

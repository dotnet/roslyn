// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ProjectInfoTests
    {
        [Fact]
        public void Create_Errors_NullReferences()
        {
            var pid = ProjectId.CreateNewId();
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(id: null, version: VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: null, assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: null, language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: null));

            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#",
                documents: [null]));
        }

        [Fact]
        public void Create_Errors_DuplicateItems()
        {
            var pid = ProjectId.CreateNewId();

            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(pid), "doc");
            Assert.Throws<ArgumentException>("documents[1]",
                () => ProjectInfo.Create(pid, VersionStamp.Default, "proj", "assembly", "C#", documents: [documentInfo, documentInfo]));

            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#",
                additionalDocuments: [null]));

            Assert.Throws<ArgumentException>("additionalDocuments[1]",
                () => ProjectInfo.Create(pid, VersionStamp.Default, "proj", "assembly", "C#", additionalDocuments: [documentInfo, documentInfo]));

            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#",
                projectReferences: [null]));

            var projectReference = new ProjectReference(ProjectId.CreateNewId());
            Assert.Throws<ArgumentException>("projectReferences[1]",
                () => ProjectInfo.Create(pid, VersionStamp.Default, "proj", "assembly", "C#", projectReferences: [projectReference, projectReference]));

            Assert.Throws<ArgumentNullException>("analyzerReferences[0]", () => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#",
                analyzerReferences: [null]));

            var analyzerReference = new TestAnalyzerReference();
            Assert.Throws<ArgumentException>("analyzerReferences[1]",
                () => ProjectInfo.Create(pid, VersionStamp.Default, "proj", "assembly", "C#", analyzerReferences: [analyzerReference, analyzerReference]));

            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#",
                metadataReferences: [null]));

            var metadataReference = new TestMetadataReference();
            Assert.Throws<ArgumentException>("metadataReferences[1]",
                () => ProjectInfo.Create(pid, VersionStamp.Default, "proj", "assembly", "C#", metadataReferences: [metadataReference, metadataReference]));
        }

        [Fact]
        public void Create_Documents()
        {
            var version = VersionStamp.Default;
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc");

            var info1 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", documents: [documentInfo]);
            Assert.Same(documentInfo, ((ImmutableArray<DocumentInfo>)info1.Documents).Single());

            var info2 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            Assert.True(((ImmutableArray<DocumentInfo>)info2.Documents).IsEmpty);

            var info3 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", documents: []);
            Assert.True(((ImmutableArray<DocumentInfo>)info3.Documents).IsEmpty);

            var info4 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", documents: []);
            Assert.True(((ImmutableArray<DocumentInfo>)info4.Documents).IsEmpty);
        }

        [Fact]
        public void Create_AdditionalDocuments()
        {
            var version = VersionStamp.Default;
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc");

            var info1 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", additionalDocuments: [documentInfo]);
            Assert.Same(documentInfo, ((ImmutableArray<DocumentInfo>)info1.AdditionalDocuments).Single());

            var info2 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            Assert.True(((ImmutableArray<DocumentInfo>)info2.AdditionalDocuments).IsEmpty);

            var info3 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", additionalDocuments: []);
            Assert.True(((ImmutableArray<DocumentInfo>)info3.AdditionalDocuments).IsEmpty);

            var info4 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", additionalDocuments: []);
            Assert.True(((ImmutableArray<DocumentInfo>)info4.AdditionalDocuments).IsEmpty);
        }

        [Fact]
        public void Create_ProjectReferences()
        {
            var version = VersionStamp.Default;
            var projectReference = new ProjectReference(ProjectId.CreateNewId());

            var info1 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", projectReferences: [projectReference]);
            Assert.Same(projectReference, ((ImmutableArray<ProjectReference>)info1.ProjectReferences).Single());

            var info2 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            Assert.True(((ImmutableArray<ProjectReference>)info2.ProjectReferences).IsEmpty);

            var info3 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", projectReferences: []);
            Assert.True(((ImmutableArray<ProjectReference>)info3.ProjectReferences).IsEmpty);

            var info4 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", projectReferences: []);
            Assert.True(((ImmutableArray<ProjectReference>)info4.ProjectReferences).IsEmpty);
        }

        [Fact]
        public void Create_MetadataReferences()
        {
            var version = VersionStamp.Default;
            var metadataReference = new TestMetadataReference();

            var info1 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", metadataReferences: [metadataReference]);
            Assert.Same(metadataReference, ((ImmutableArray<MetadataReference>)info1.MetadataReferences).Single());

            var info2 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            Assert.True(((ImmutableArray<MetadataReference>)info2.MetadataReferences).IsEmpty);

            var info3 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", metadataReferences: []);
            Assert.True(((ImmutableArray<MetadataReference>)info3.MetadataReferences).IsEmpty);

            var info4 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", metadataReferences: []);
            Assert.True(((ImmutableArray<MetadataReference>)info4.MetadataReferences).IsEmpty);
        }

        [Fact]
        public void Create_AnalyzerReferences()
        {
            var version = VersionStamp.Default;
            var analyzerReference = new TestAnalyzerReference();

            var info1 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", analyzerReferences: [analyzerReference]);
            Assert.Same(analyzerReference, ((ImmutableArray<AnalyzerReference>)info1.AnalyzerReferences).Single());

            var info2 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#");
            Assert.True(((ImmutableArray<AnalyzerReference>)info2.AnalyzerReferences).IsEmpty);

            var info3 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", analyzerReferences: []);
            Assert.True(((ImmutableArray<AnalyzerReference>)info3.AnalyzerReferences).IsEmpty);

            var info4 = ProjectInfo.Create(ProjectId.CreateNewId(), version, "proj", "assembly", "C#", analyzerReferences: []);
            Assert.True(((ImmutableArray<AnalyzerReference>)info4.AnalyzerReferences).IsEmpty);
        }

        [Fact]
        public void DebuggerDisplayHasProjectNameAndFilePath()
        {
            var projectInfo = ProjectInfo.Create(name: "Goo", filePath: @"C:\", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "Bar", language: "C#");
            Assert.Equal(@"ProjectInfo Goo C:\", projectInfo.GetDebuggerDisplay());
        }

        [Fact]
        public void DebuggerDisplayHasOnlyProjectNameWhenFilePathNotSpecified()
        {
            var projectInfo = ProjectInfo.Create(name: "Goo", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "Bar", language: "C#");
            Assert.Equal(@"ProjectInfo Goo", projectInfo.GetDebuggerDisplay());
        }

        [Fact]
        public void TestProperties()
        {
            var projectId = ProjectId.CreateNewId();
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "doc");
            var instance = ProjectInfo.Create(name: "Name", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "AssemblyName", language: "C#");

            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithId(value), opt => opt.Id, ProjectId.CreateNewId(), defaultThrows: true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithVersion(value), opt => opt.Version, VersionStamp.Create());
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithName(value), opt => opt.Name, "New", defaultThrows: true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithAssemblyName(value), opt => opt.AssemblyName, "New", defaultThrows: true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithFilePath(value), opt => opt.FilePath, "New");
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithOutputFilePath(value), opt => opt.OutputFilePath, "New");
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithOutputRefFilePath(value), opt => opt.OutputRefFilePath, "New");
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithCompilationOutputInfo(value), opt => opt.CompilationOutputInfo, new CompilationOutputInfo("NewPath"));
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithDefaultNamespace(value), opt => opt.DefaultNamespace, "New");
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithChecksumAlgorithm(value), opt => opt.ChecksumAlgorithm, SourceHashAlgorithm.None);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithHasAllInformation(value), opt => opt.HasAllInformation, false);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithRunAnalyzers(value), opt => opt.RunAnalyzers, false);

            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithDocuments(value), opt => opt.Documents, documentInfo, allowDuplicates: false);
            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithAdditionalDocuments(value), opt => opt.AdditionalDocuments, documentInfo, allowDuplicates: false);
            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithAnalyzerConfigDocuments(value), opt => opt.AnalyzerConfigDocuments, documentInfo, allowDuplicates: false);
            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithAnalyzerReferences(value), opt => opt.AnalyzerReferences, (AnalyzerReference)new TestAnalyzerReference(), allowDuplicates: false);
            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithMetadataReferences(value), opt => opt.MetadataReferences, (MetadataReference)new TestMetadataReference(), allowDuplicates: false);
            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithProjectReferences(value), opt => opt.ProjectReferences, new ProjectReference(projectId), allowDuplicates: false);
        }
    }
}

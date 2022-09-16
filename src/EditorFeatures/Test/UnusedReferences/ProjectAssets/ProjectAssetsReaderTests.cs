// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.UnusedReferences.ProjectAssets
{
    [Trait(Traits.Feature, Traits.Features.Packaging)]
    public partial class ProjectAssetsReaderTests
    {
        private const string TargetFramework = ".NETCoreApp,Version=v3.1";
        private const int Version3 = 3;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(5)]
        public void NoReferencesReadWhenProjectAssetsVersionNot3(int version)
        {
            var myPackage = PackageReference("MyPackage.dll");
            var references = ImmutableArray.Create(myPackage);
            var projectAssets = TestProjectAssetsFile.Create(version, TargetFramework, references);
            var realizedReferences = ProjectAssetsReader.AddDependencyHierarchies(references, projectAssets);
            Assert.Empty(realizedReferences);
        }

        [Fact]
        public void ReferencesReadWhenProjectAssetsVersionIs3()
        {
            var myPackage = PackageReference("MyPackage.dll");
            var references = ImmutableArray.Create(myPackage);
            var projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references);
            var realizedReferences = ProjectAssetsReader.AddDependencyHierarchies(references, projectAssets);
            var realizedReference = Assert.Single(realizedReferences);
            Assert.Equal(myPackage.ItemSpecification, realizedReference.ItemSpecification);
        }

        [Fact]
        public void ReferenceNotReadWhenReferenceNotPresent()
        {
            var references = ImmutableArray.Create(PackageReference("MyPackage.dll"));
            var projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references);
            var differentReference = ImmutableArray.Create(ProjectReference("MyProject.csproj"));
            var realizedReferences = ProjectAssetsReader.AddDependencyHierarchies(differentReference, projectAssets);
            Assert.Empty(realizedReferences);
        }

        [Fact]
        public void ProjectReferencesReadHaveTheirPathAsTheItemSpecification()
        {
            const string mylibraryPath = @".\Library\MyLibrary.csproj";
            var references = ImmutableArray.Create(ProjectReference(mylibraryPath));
            var projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references);
            var realizedReferences = ProjectAssetsReader.AddDependencyHierarchies(references, projectAssets);
            var realizedReference = Assert.Single(realizedReferences);
            Assert.Equal(mylibraryPath, realizedReference.ItemSpecification);
        }

        private static ReferenceInfo ProjectReference(string projectPath, params ReferenceInfo[] dependencies) => ProjectReference(projectPath, false, dependencies);
        private static ReferenceInfo ProjectReference(string projectPath, bool treatAsUsed, params ReferenceInfo[] dependencies)
            => new(ReferenceType.Project, projectPath, treatAsUsed, ImmutableArray.Create(Path.ChangeExtension(projectPath, "dll")), dependencies.ToImmutableArray());

        private static ReferenceInfo PackageReference(string assemblyPath, params ReferenceInfo[] dependencies) => PackageReference(assemblyPath, false, dependencies);
        private static ReferenceInfo PackageReference(string assemblyPath, bool treatAsUsed, params ReferenceInfo[] dependencies)
            => new(ReferenceType.Package, Path.GetFileNameWithoutExtension(assemblyPath), treatAsUsed, ImmutableArray.Create(assemblyPath), dependencies.ToImmutableArray());
    }
}

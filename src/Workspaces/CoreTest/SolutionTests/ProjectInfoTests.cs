// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ProjectInfoTests
    {
        [Fact]
        public void Create()
        {
            var pid = ProjectId.CreateNewId();
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(id: null, version: VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: null, assemblyName: "Bar", language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: null, language: "C#"));
            Assert.Throws<ArgumentNullException>(() => ProjectInfo.Create(pid, VersionStamp.Default, name: "Goo", assemblyName: "Bar", language: null));
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

        private static void TestProperty<T>(Func<ProjectInfo, T, ProjectInfo> factory, Func<ProjectInfo, T> getter, T validNonDefaultValue, bool defaultThrows = false)
        {
            Assert.NotEqual<T>(default, validNonDefaultValue);

            var instance = ProjectInfo.Create(name: "Name", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "AssemblyName", language: "C#");

            var instanceWithValue = factory(instance, validNonDefaultValue);
            Assert.Equal(validNonDefaultValue, getter(instanceWithValue));

            var instanceWithValue2 = factory(instanceWithValue, validNonDefaultValue);
            Assert.Same(instanceWithValue2, instanceWithValue);

            if (defaultThrows)
            {
                Assert.Throws<ArgumentNullException>(() => factory(instance, default));
            }
            else
            {
                Assert.NotNull(factory(instance, default));
            }
        }

        private static void TestListProperty<T>(Func<ProjectInfo, IEnumerable<T>, ProjectInfo> factory, Func<ProjectInfo, IEnumerable<T>> getter, T item)
        {
            TestProperty(factory, getter, ImmutableArray.Create(item), defaultThrows: false);

            var instanceWithNoItem = ProjectInfo.Create(name: "Name", id: ProjectId.CreateNewId(), version: VersionStamp.Default, assemblyName: "AssemblyName", language: "C#");
            Assert.Empty(getter(instanceWithNoItem));

            var instanceWithItem = factory(instanceWithNoItem, ImmutableArray.Create(item));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, default));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, Array.Empty<T>()));
            Assert.Same(instanceWithNoItem, factory(instanceWithNoItem, ImmutableArray<T>.Empty));

            Assert.Throws<ArgumentNullException>(() => factory(instanceWithNoItem, new T[] { item, default }));
        }

        [Fact]
        public void TestProperties()
        {
            var projectId = ProjectId.CreateNewId();
            var documentInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId), "doc");

            TestProperty((old, value) => old.WithVersion(value), opt => opt.Version, VersionStamp.Create());
            TestProperty((old, value) => old.WithName(value), opt => opt.Name, "New", defaultThrows: true);
            TestProperty((old, value) => old.WithAssemblyName(value), opt => opt.AssemblyName, "New", defaultThrows: true);
            TestProperty((old, value) => old.WithFilePath(value), opt => opt.FilePath, "New");
            TestProperty((old, value) => old.WithOutputFilePath(value), opt => opt.OutputFilePath, "New");
            TestProperty((old, value) => old.WithOutputRefFilePath(value), opt => opt.OutputRefFilePath, "New");
            TestProperty((old, value) => old.WithDefaultNamespace(value), opt => opt.DefaultNamespace, "New");
            TestProperty((old, value) => old.WithHasAllInformation(value), opt => opt.HasAllInformation, true);
            TestProperty((old, value) => old.WithRunAnalyzers(value), opt => opt.RunAnalyzers, true);

            TestListProperty((old, value) => old.WithDocuments(value), opt => opt.Documents, documentInfo);
            TestListProperty((old, value) => old.WithAdditionalDocuments(value), opt => opt.AdditionalDocuments, documentInfo);
            TestListProperty((old, value) => old.WithAnalyzerConfigDocuments(value), opt => opt.AnalyzerConfigDocuments, documentInfo);
            TestListProperty((old, value) => old.WithAnalyzerReferences(value), opt => opt.AnalyzerReferences, new TestAnalyzerReference());
            TestListProperty((old, value) => old.WithMetadataReferences(value), opt => opt.MetadataReferences, new TestMetadataReference());
            TestListProperty((old, value) => old.WithProjectReferences(value), opt => opt.ProjectReferences, new ProjectReference(projectId));
        }
    }
}

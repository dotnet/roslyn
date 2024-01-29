// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DocumentInfoTests
    {
        [Fact]
        public void Create_Errors()
        {
            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId());

            Assert.Throws<ArgumentNullException>(() => DocumentInfo.Create(id: null, "doc"));
            Assert.Throws<ArgumentNullException>(() => DocumentInfo.Create(documentId, name: null));

            Assert.Throws<ArgumentNullException>(() => DocumentInfo.Create(documentId, "doc", folders: ["folder", null]));
        }

        [Fact]
        public void Create()
        {
            var loader = new TestTextLoader("text");
            var id = DocumentId.CreateNewId(ProjectId.CreateNewId());

            var info = DocumentInfo.Create(
                id,
                name: "doc",
                sourceCodeKind: SourceCodeKind.Script,
                loader: loader,
                isGenerated: true);

            Assert.Equal(id, info.Id);
            Assert.Equal("doc", info.Name);
            Assert.Equal(SourceCodeKind.Script, info.SourceCodeKind);
            Assert.Same(loader, info.TextLoader);
            Assert.True(info.IsGenerated);
        }

        [Fact]
        public void Create_NullLoader()
        {
            var id = DocumentId.CreateNewId(ProjectId.CreateNewId());

            var info = DocumentInfo.Create(
                id,
                name: "doc",
                sourceCodeKind: SourceCodeKind.Script,
                loader: null,
                isGenerated: true);

            Assert.Equal(id, info.Id);
            Assert.Equal("doc", info.Name);
            Assert.Equal(SourceCodeKind.Script, info.SourceCodeKind);
            Assert.Null(info.TextLoader);
            Assert.True(info.IsGenerated);
        }

        [Fact]
        public void Create_Folders()
        {
            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId());

            var info1 = DocumentInfo.Create(documentId, "doc", folders: ["folder"]);
            Assert.Equal("folder", ((ImmutableArray<string>)info1.Folders).Single());

            var info2 = DocumentInfo.Create(documentId, "doc");
            Assert.True(((ImmutableArray<string>)info2.Folders).IsEmpty);

            var info3 = DocumentInfo.Create(documentId, "doc", folders: new string[0]);
            Assert.True(((ImmutableArray<string>)info3.Folders).IsEmpty);

            var info4 = DocumentInfo.Create(documentId, "doc", folders: []);
            Assert.True(((ImmutableArray<string>)info4.Folders).IsEmpty);
        }

        [Theory]
        [InlineData(SourceCodeKind.Script, null, "")]
        [InlineData(SourceCodeKind.Script, "", "")]
        [InlineData(SourceCodeKind.Script, "path", "path")]
        [InlineData(SourceCodeKind.Regular, null, "doc_name")]
        [InlineData(SourceCodeKind.Regular, "", "")]
        [InlineData(SourceCodeKind.Regular, "path", "path")]
        public void Create_FilePath(SourceCodeKind kind, string path, string expectedSyntaxTreeFilePath)
        {
            var info = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc_name", filePath: path, sourceCodeKind: kind);
            Assert.Equal(path, info.FilePath);
            Assert.Equal(expectedSyntaxTreeFilePath, info.Attributes.SyntaxTreeFilePath);
        }

        [Fact]
        public void TestProperties()
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var instance = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc");
            var serviceProvider = (IDocumentServiceProvider)new TestDocumentServiceProvider();

            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithId(value), opt => opt.Id, documentId, defaultThrows: true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithName(value), opt => opt.Name, "New", defaultThrows: true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithTextLoader(value), opt => opt.TextLoader, (TextLoader)new TestTextLoader("text"));
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithDesignTimeOnly(value), opt => opt.Attributes.DesignTimeOnly, true);
            SolutionTestHelpers.TestProperty(instance, (old, value) => old.WithDocumentServiceProvider(value), opt => opt.DocumentServiceProvider, serviceProvider);

            SolutionTestHelpers.TestListProperty(instance, (old, value) => old.WithFolders(value), opt => opt.Folders, "folder", allowDuplicates: true);
        }
    }
}

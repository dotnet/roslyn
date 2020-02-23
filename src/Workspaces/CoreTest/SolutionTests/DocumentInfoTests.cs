// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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

            Assert.Throws<ArgumentNullException>(() => DocumentInfo.Create(documentId, "doc", folders: new[] { "folder", null }));
        }

        [Fact]
        public void Create()
        {
            var loader = new FileTextLoader(Path.GetTempPath(), defaultEncoding: null);
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
        public void Create_Folders()
        {
            var documentId = DocumentId.CreateNewId(ProjectId.CreateNewId());

            var info1 = DocumentInfo.Create(documentId, "doc", folders: new[] { "folder" });
            Assert.Equal("folder", ((ImmutableArray<string>)info1.Folders).Single());

            var info2 = DocumentInfo.Create(documentId, "doc");
            Assert.True(((ImmutableArray<string>)info2.Folders).IsEmpty);

            var info3 = DocumentInfo.Create(documentId, "doc", folders: new string[0]);
            Assert.True(((ImmutableArray<string>)info3.Folders).IsEmpty);

            var info4 = DocumentInfo.Create(documentId, "doc", folders: ImmutableArray<string>.Empty);
            Assert.True(((ImmutableArray<string>)info4.Folders).IsEmpty);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("path")]
        public void Create_FilePath(string path)
        {
            var info = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc", filePath: path);
            Assert.Equal(path, info.FilePath);
        }

        private static void TestProperty<T>(Func<DocumentInfo, T, DocumentInfo> factory, Func<DocumentInfo, T> getter, T validNonDefaultValue, bool defaultThrows = false)
        {
            Assert.NotEqual<T>(default, validNonDefaultValue);

            var instance = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc");

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

        private static void TestListProperty<T>(Func<DocumentInfo, IEnumerable<T>, DocumentInfo> factory, Func<DocumentInfo, IEnumerable<T>> getter, T item)
        {
            TestProperty(factory, getter, ImmutableArray.Create(item), defaultThrows: false);

            var instanceWithNoItem = DocumentInfo.Create(DocumentId.CreateNewId(ProjectId.CreateNewId()), "doc");
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
            var documentId = DocumentId.CreateNewId(projectId);

            TestProperty((old, value) => old.WithId(value), opt => opt.Id, documentId, defaultThrows: true);
            TestProperty((old, value) => old.WithName(value), opt => opt.Name, "New", defaultThrows: true);
            TestProperty((old, value) => old.WithSourceCodeKind(value), opt => opt.SourceCodeKind, SourceCodeKind.Script);
            TestProperty((old, value) => old.WithTextLoader(value), opt => opt.TextLoader, new FileTextLoader(Path.GetTempPath(), defaultEncoding: null));

            TestListProperty((old, value) => old.WithFolders(value), opt => opt.Folders, "folder");
        }
    }
}

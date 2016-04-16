// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    using static SymTestHelpers;

    public class SymBinderTests
    {
        private static ISymUnmanagedBinder4 SymBinder => new SymBinder();

        private sealed class NotImplementedMetadataProvider : IMetadataImportProvider
        {
            public static readonly IMetadataImportProvider Instance = new NotImplementedMetadataProvider();

            public object GetMetadataImport()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class TestMetadataProvider : IMetadataImportProvider
        {
            private readonly Func<IMetadataImport> _importProvider;

            public TestMetadataProvider(Func<IMetadataImport> importProvider)
            {
                _importProvider = importProvider;
            }

            public object GetMetadataImport() => _importProvider();
        }

        [Fact]
        public void GetReaderForFile_NextToPE()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(tempDir, "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);

            string searchPath = null;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_SearchPaths()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(searchDir, "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(searchDir);
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_SearchPaths_SubDir1()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(searchDir, "dll", "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(pdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_SearchPaths_SubDir2()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(searchDir, "symbols", "dll", "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(pdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        // TODO: test Environment, Registry (need test hooks)

        [Fact]
        public void GetReaderForFile_SkipNative1()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(searchDir, "Documents.pdb");
            string nativePdbFilePath = Path.Combine(searchDir, "symbols", "dll", "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(pdbFilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(nativePdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);
            PortableShim.File.WriteAllBytes(nativePdbFilePath, TestResources.Documents.Pdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_SkipNative2()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir1 = Path.Combine(tempDir, "Dir1");
            string searchDir2 = Path.Combine(tempDir, "Dir2");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string nativePdbFilePath = Path.Combine(searchDir1, "Documents.pdb");
            string pdbFilePath = Path.Combine(searchDir2, "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(pdbFilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(nativePdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);
            PortableShim.File.WriteAllBytes(nativePdbFilePath, TestResources.Documents.Pdb);

            string searchPath = searchDir1 + ";" + searchDir2;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_SkipNonMatching()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string pdbFilePath = Path.Combine(searchDir, "Documents.pdb");
            string anotherPdbFilePath = Path.Combine(searchDir, "symbols", "dll", "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(pdbFilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(anotherPdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(pdbFilePath, TestResources.Documents.PortablePdb);
            PortableShim.File.WriteAllBytes(anotherPdbFilePath, TestResources.Async.PortablePdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            ((ISymUnmanagedDispose)symReader).Destroy();

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderForFile_MatchingNotFound()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));

            string tempDir = Path.Combine(Path.GetDirectoryName(PortableShim.Path.GetTempFileName()), Guid.NewGuid().ToString());
            string searchDir = Path.Combine(tempDir, "Dir");
            string peFilePath = Path.Combine(tempDir, "Documents.dll");
            string anotherPdbFilePath = Path.Combine(searchDir, "symbols", "dll", "Documents.pdb");

            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.GetDirectoryName(anotherPdbFilePath));
            PortableShim.File.WriteAllBytes(peFilePath, TestResources.Documents.PortableDll);
            PortableShim.File.WriteAllBytes(anotherPdbFilePath, TestResources.Async.PortablePdb);

            string searchPath = searchDir;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.E_PDB_NOT_FOUND, SymBinder.GetReaderForFile(importer, peFilePath, searchPath, out symReader));
            Assert.Null(symReader);

            Directory.Delete(tempDir, recursive: true);
        }

        [Fact]
        public void GetReaderFromPdbFile()
        {
            string filePath = PortableShim.Path.GetTempFileName();
            PortableShim.File.WriteAllBytes(filePath, TestResources.Documents.PortablePdb);

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderFromPdbFile(NotImplementedMetadataProvider.Instance, filePath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            Assert.Equal(HResult.S_FALSE, ((ISymUnmanagedDispose)symReader).Destroy());
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedDispose)symReader).Destroy());

            Assert.Throws<ObjectDisposedException>(() => symReader.GetDocuments(0, out actualCount, null));

            PortableShim.File.Delete(filePath);
        }

        [Fact]
        public void GetReaderFromStream()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.PortableDll));
            var stream = new MemoryStream(TestResources.Documents.PortablePdb);
            var wrapper = new ComStreamWrapper(stream);

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderFromStream(importer, wrapper, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            Assert.Equal(HResult.S_FALSE, ((ISymUnmanagedDispose)symReader).Destroy());
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedDispose)symReader).Destroy());

            Assert.Throws<ObjectDisposedException>(() => symReader.GetDocuments(0, out actualCount, null));
        }

        [Fact]
        public void GetReaderFromPdbStream()
        {
            var stream = new MemoryStream(TestResources.Documents.PortablePdb);
            var wrapper = new ComStreamWrapper(stream);

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderFromPdbStream(NotImplementedMetadataProvider.Instance, wrapper, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(13, actualCount);

            Assert.Equal(HResult.S_FALSE, ((ISymUnmanagedDispose)symReader).Destroy());
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedDispose)symReader).Destroy());

            Assert.Throws<ObjectDisposedException>(() => symReader.GetDocuments(0, out actualCount, null));
        }

        [Fact]
        public void LazyMetadataImport()
        {
            bool importCreated = false;
            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderFromPdbStream(
                new TestMetadataProvider(() =>
                {
                    importCreated = true;
                    return new SymMetadataImport(new MemoryStream(TestResources.Scopes.Dll));
                }),
                new ComStreamWrapper(new MemoryStream(TestResources.Scopes.Pdb)), out symReader));

            int count;

            //
            //  C<S>.F<T>
            //

            ISymUnmanagedMethod mF;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(0x06000002, out mF));

            // root scope:
            ISymUnmanagedScope rootScope;
            Assert.Equal(HResult.S_OK, mF.GetRootScope(out rootScope));

            // child scope:
            var children = GetAndValidateChildScopes(rootScope, expectedCount: 1);

            var child = children[0];
            Assert.Equal(HResult.S_OK, child.GetLocals(0, out count, null));
            Assert.Equal(0, count);

            ISymUnmanagedScope parent;
            Assert.Equal(HResult.S_OK, child.GetParent(out parent));
            Assert.NotSame(rootScope, parent); // a new instance should be created
            ValidateRootScope(parent);
            ValidateRange(parent, 0, 2);

            var constants = GetAndValidateConstants(child, expectedCount: 29);

            Assert.False(importCreated);
            ValidateConstant(constants[28], "D", 123456.78M, new byte[] { 0x11, 0x2D });
            Assert.True(importCreated);
        }
    }
}

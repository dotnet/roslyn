// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    public class SymBinderTests
    {
        private static ISymUnmanagedBinder SymBinder => new SymBinder();

        [Fact]
        public void GetReaderForFile()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.Dll));

            string filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            File.WriteAllBytes(filePath, TestResources.Documents.Pdb);

            string searchPath = null;

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderForFile(importer, filePath, searchPath, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(11, actualCount);

            Assert.Equal(HResult.S_FALSE, ((ISymUnmanagedDispose)symReader).Destroy());
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedDispose)symReader).Destroy());

            Assert.Throws<ObjectDisposedException>(() => symReader.GetDocuments(0, out actualCount, null));

            File.Delete(filePath);
        }

        [Fact]
        public void GetReaderFromStream()
        {
            var importer = new SymMetadataImport(new MemoryStream(TestResources.Documents.Dll));
            var stream = new MemoryStream(TestResources.Documents.Pdb);
            var wrapper = new ComStreamWrapper(stream);

            ISymUnmanagedReader symReader;
            Assert.Equal(HResult.S_OK, SymBinder.GetReaderFromStream(importer, wrapper, out symReader));

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(11, actualCount);

            Assert.Equal(HResult.S_FALSE, ((ISymUnmanagedDispose)symReader).Destroy());
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedDispose)symReader).Destroy());

            Assert.Throws<ObjectDisposedException>(() => symReader.GetDocuments(0, out actualCount, null));
        }
    }
}

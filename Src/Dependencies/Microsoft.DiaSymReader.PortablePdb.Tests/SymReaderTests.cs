// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System;
using System.IO;
using System.Reflection.Metadata;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    public class SymReaderTests
    {
        [Fact]
        public unsafe void TestMetadataHeaders1()
        {
            fixed (byte* pdbPtr = TestResources.Z.pdb)
            {
                var pdbReader = new MetadataReader(pdbPtr, TestResources.Z.pdb.Length);
                Assert.Equal("PDB v0.1", pdbReader.MetadataVersion);
                Assert.Equal(MetadataKind.Ecma335, pdbReader.MetadataKind);
                Assert.False(pdbReader.IsAssembly);

                var moduleDef = pdbReader.GetModuleDefinition();
                Assert.Equal("z.dll", pdbReader.GetString(moduleDef.Name));
                Assert.Equal(new Guid("50ae2a61-01ed-4daf-bb7b-afdee51f52e2"), pdbReader.GetGuid(moduleDef.Mvid));
                Assert.Equal(0, moduleDef.Generation);
                Assert.True(moduleDef.GenerationId.IsNil);
                Assert.True(moduleDef.BaseGenerationId.IsNil);
            }
        }

        [Fact]
        public void TestDocument1()
        {
            var pdbReader = new PortablePdbReader(TestResources.ResourceHelper.GetResourceStream("z.pdb"));
            var symReader = new SymReader(pdbReader);

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(1, actualCount);

            var actualDocuments = new ISymUnmanagedDocument[actualCount];
            int actualCount2;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(actualCount, out actualCount2, actualDocuments));
            Assert.Equal(1, actualCount2);

            var document = actualDocuments[0];
            Assert.Equal(HResult.S_OK, document.GetUrl(0, out actualCount, null));

            char[] actualUrl = new char[actualCount];
            Assert.Equal(HResult.S_OK, document.GetUrl(actualCount, out actualCount2, actualUrl));
            Assert.Equal(@"C:\Temp\z.cs", new string(actualUrl, 0, actualUrl.Length - 1));

            Assert.Equal(HResult.S_OK, document.GetChecksum(0, out actualCount, null));

            byte[] checksum = new byte[actualCount];
            Assert.Equal(HResult.S_OK, document.GetChecksum(actualCount, out actualCount2, checksum));
            Assert.Equal(actualCount, actualCount2);
            AssertEx.Equal(new byte[] { 0xF2, 0x5B, 0x40, 0xAC, 0xE3, 0x6E, 0x4E, 0xCA, 0x00, 0xC7, 0xEE, 0x46, 0x9C, 0x33, 0x17, 0x16, 0xC0, 0xB0, 0x6A, 0x53 }, checksum);

            Guid guid = default(Guid);
            Assert.Equal(HResult.S_OK, document.GetChecksumAlgorithmId(ref guid));
            Assert.Equal(new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460"), guid);

            Assert.Equal(HResult.S_OK, document.GetLanguageVendor(ref guid));
            Assert.Equal(new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1"), guid);

            Assert.Equal(HResult.S_OK, document.GetDocumentType(ref guid));
            Assert.Equal(new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd"), guid);
        }

        [Fact]
        public void TestSymGetAttribute()
        {
            var pdbReader = new PortablePdbReader(TestResources.ResourceHelper.GetResourceStream("z.pdb"));
            var symReader = new SymReader(pdbReader);

            int actualCount;
            int actualCount2;
            Assert.Equal(HResult.S_FALSE, symReader.GetSymAttribute(0, "<PortablePdbImage>", 0, out actualCount, null));

            byte[] image = new byte[actualCount];
            Assert.Equal(HResult.S_OK, symReader.GetSymAttribute(0, "<PortablePdbImage>", 0, out actualCount2, image));
            Assert.Equal(actualCount, actualCount2);

            AssertEx.Equal(TestResources.Z.pdb, image);
        }
    }
}

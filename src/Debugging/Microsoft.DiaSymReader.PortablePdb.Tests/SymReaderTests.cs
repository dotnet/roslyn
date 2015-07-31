// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Roslyn.Test.Utilities;
using System;
using System.Reflection.Metadata;
using Xunit;
using System.IO;
using System.Collections.Generic;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    public class SymReaderTests
    {
        #region Helpers

        private static SymReader CreateSymReaderFromResource(KeyValuePair<byte[], byte[]> peAndPdb)
        {
            MetadataReader mdReader;
            return CreateSymReaderFromResource(peAndPdb, out mdReader);
        }

        private static SymReader CreateSymReaderFromResource(KeyValuePair<byte[], byte[]> peAndPdb, out MetadataReader mdReader)
        {
            var importer = new SymMetadataImport(new MemoryStream(peAndPdb.Key));
            mdReader = importer.MetadataReader;
            return new SymReader(new PortablePdbReader(peAndPdb.Value, peAndPdb.Value.Length, importer));
        }

        private void ValidateDocumentUrl(ISymUnmanagedDocument document, string url)
        {
            int actualCount, actualCount2;
            Assert.Equal(HResult.S_OK, document.GetUrl(0, out actualCount, null));

            char[] actualUrl = new char[actualCount];
            Assert.Equal(HResult.S_OK, document.GetUrl(actualCount, out actualCount2, actualUrl));
            Assert.Equal(url, new string(actualUrl, 0, actualUrl.Length - 1));
        }

        private void ValidateDocument(ISymUnmanagedDocument document, string url, string algorithmId, byte[] checksum)
        {
            ValidateDocumentUrl(document, url);

            int actualCount, actualCount2;

            if (checksum != null)
            {
                Assert.Equal(HResult.S_OK, document.GetChecksum(0, out actualCount, null));
                byte[] actualChecksum = new byte[actualCount];
                Assert.Equal(HResult.S_OK, document.GetChecksum(actualCount, out actualCount2, actualChecksum));
                Assert.Equal(actualCount, actualCount2);
                AssertEx.Equal(checksum, actualChecksum);
            }
            else
            {
                Assert.Equal(HResult.S_FALSE, document.GetChecksum(0, out actualCount, null));
                Assert.Equal(0, actualCount);
            }

            var guid = Guid.NewGuid();

            Assert.Equal(HResult.S_OK, document.GetChecksumAlgorithmId(ref guid));
            Assert.Equal(algorithmId != null ? new Guid(algorithmId) : default(Guid), guid);

            guid = Guid.NewGuid();
            Assert.Equal(HResult.S_OK, document.GetLanguageVendor(ref guid));
            Assert.Equal(new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1"), guid);

            guid = Guid.NewGuid();
            Assert.Equal(HResult.S_OK, document.GetDocumentType(ref guid));
            Assert.Equal(new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd"), guid);
        }

        private void ValidateRange(ISymUnmanagedScope scope, int expectedStartOffset, int expectedLength)
        {
            int actualOffset;
            Assert.Equal(HResult.S_OK, scope.GetStartOffset(out actualOffset));
            Assert.Equal(expectedStartOffset, actualOffset);

            Assert.Equal(HResult.S_OK, scope.GetEndOffset(out actualOffset));
            Assert.Equal(expectedStartOffset + expectedLength, 2);
        }

        private void ValidateConstant(ISymUnmanagedConstant constant, string name, object value, byte[] signature)
        {
            int length, length2;
          
            // name:
            Assert.Equal(HResult.S_OK, constant.GetName(0, out length, null));
            Assert.Equal(name.Length + 1, length);
            var actualName = new char[length];
            Assert.Equal(HResult.S_OK, constant.GetName(length, out length2, actualName));
            Assert.Equal(length, length2);
            Assert.Equal(name + "\0", new string(actualName));

            // value:
            object actualValue;
            Assert.Equal(HResult.S_OK, constant.GetValue(out actualValue));
            Assert.Equal(value, actualValue);

            // signature:
            Assert.Equal(HResult.S_OK, constant.GetSignature(0, out length, null));
            var actualSignature = new byte[length];
            Assert.Equal(HResult.S_OK, constant.GetSignature(length, out length2, actualSignature));
            Assert.Equal(length, length2);
            AssertEx.Equal(signature, actualSignature);
        }

        private void ValidateVariable(ISymUnmanagedVariable variable, string name, int slot, LocalVariableAttributes attributes, byte[] signature)
        {
            int length, length2;

            // name:
            Assert.Equal(HResult.S_OK, variable.GetName(0, out length, null));
            Assert.Equal(name.Length + 1, length);
            var actualName = new char[length];
            Assert.Equal(HResult.S_OK, variable.GetName(length, out length2, actualName));
            Assert.Equal(length, length2);
            Assert.Equal(name + "\0", new string(actualName));

            int value;
            Assert.Equal(HResult.S_OK, variable.GetAddressField1(out value));
            Assert.Equal(slot, value);

            Assert.Equal(HResult.E_NOTIMPL, variable.GetAddressField2(out value));
            Assert.Equal(HResult.E_NOTIMPL, variable.GetAddressField3(out value));
            Assert.Equal(HResult.E_NOTIMPL, variable.GetStartOffset(out value));
            Assert.Equal(HResult.E_NOTIMPL, variable.GetEndOffset(out value));

            Assert.Equal(HResult.S_OK, variable.GetAttributes(out value));
            Assert.Equal(attributes, (LocalVariableAttributes)value);

            Assert.Equal(HResult.S_OK, variable.GetAddressKind(out value));
            Assert.Equal(1, value);
            
            Assert.Equal(HResult.S_OK, variable.GetSignature(0, out length, null));
            var actualSignature = new byte[length];
            Assert.Equal(HResult.S_OK, variable.GetSignature(length, out length2, actualSignature));
            Assert.Equal(length, length2);
            AssertEx.Equal(signature, actualSignature);
        }

        private void ValidateRootScope(ISymUnmanagedScope scope)
        {
            int count;
            Assert.Equal(HResult.S_OK, scope.GetLocalCount(out count));
            Assert.Equal(0, count);

            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetConstantCount(out count));
            Assert.Equal(0, count);

            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetNamespaces(0, out count, null));
            Assert.Equal(0, count);

            ISymUnmanagedScope parent;
            Assert.Equal(HResult.S_OK, scope.GetParent(out parent));
            Assert.Null(parent);
        }

        private ISymUnmanagedScope[] GetAndValidateChildScopes(ISymUnmanagedScope scope, int expectedCount)
        {
            int count, count2;
            Assert.Equal(HResult.S_OK, scope.GetChildren(0, out count, null));
            Assert.Equal(expectedCount, count);
            var children = new ISymUnmanagedScope[count];
            Assert.Equal(HResult.S_OK, scope.GetChildren(count, out count2, children));
            Assert.Equal(count, count2);
            return children;
        }

        private ISymUnmanagedConstant[] GetAndValidateConstants(ISymUnmanagedScope scope, int expectedCount)
        {
            int count, count2;
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetConstants(0, out count, null));
            Assert.Equal(expectedCount, count);
            var constants = new ISymUnmanagedConstant[count];
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetConstants(count, out count2, constants));
            Assert.Equal(count, count2);
            return constants;
        }

        private ISymUnmanagedVariable[] GetAndValidateVariables(ISymUnmanagedScope scope, int expectedCount)
        {
            int count, count2, count3;
            Assert.Equal(HResult.S_OK, scope.GetLocalCount(out count));
            Assert.Equal(expectedCount, count);
            Assert.Equal(HResult.S_OK, scope.GetLocals(0, out count2, null));
            Assert.Equal(expectedCount, count2);
            var variables = new ISymUnmanagedVariable[count];
            Assert.Equal(HResult.S_OK, scope.GetLocals(count, out count3, variables));
            Assert.Equal(count, count3);
            return variables;
        }

        private void ValidateAsyncMethod(ISymUnmanagedReader symReader, int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, int[] yieldOffsets, int[] resumeOffsets)
        {
            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(moveNextMethodToken, out method));

            var asyncMethod = (ISymUnmanagedAsyncMethod)method;

            bool isAsync;
            Assert.Equal(HResult.S_OK, asyncMethod.IsAsyncMethod(out isAsync));
            Assert.True(isAsync);

            int actualKickoffMethodToken;
            Assert.Equal(HResult.S_OK, asyncMethod.GetKickoffMethod(out actualKickoffMethodToken));
            Assert.Equal(kickoffMethodToken, actualKickoffMethodToken);

            bool hasCatchHandlerILOffset;
            Assert.Equal(HResult.S_OK, asyncMethod.HasCatchHandlerILOffset(out hasCatchHandlerILOffset));
            Assert.Equal(catchHandlerOffset >= 0, hasCatchHandlerILOffset);

            int actualCatchHandlerOffset;
            if (hasCatchHandlerILOffset)
            {
                Assert.Equal(HResult.S_OK, asyncMethod.GetCatchHandlerILOffset(out actualCatchHandlerOffset));
                Assert.Equal(catchHandlerOffset, actualCatchHandlerOffset);
            }
            else
            {
                Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.GetCatchHandlerILOffset(out actualCatchHandlerOffset));
            }

            int count, count2;
            Assert.Equal(HResult.S_OK, asyncMethod.GetAsyncStepInfoCount(out count));
            Assert.Equal(yieldOffsets.Length, count);
            Assert.Equal(resumeOffsets.Length, count);

            var actualYieldOffsets = new int[count];
            var actualResumeOffsets = new int[count];
            var actualResumeMethods = new int[count];
            
            Assert.Equal(HResult.S_OK, asyncMethod.GetAsyncStepInfo(count, out count2, actualYieldOffsets, actualResumeOffsets, actualResumeMethods));

            AssertEx.Equal(yieldOffsets, actualYieldOffsets);
            AssertEx.Equal(resumeOffsets, actualResumeOffsets);

            foreach (int actualResumeMethod in actualResumeMethods)
            {
                Assert.Equal(moveNextMethodToken, actualResumeMethod);
            }
        }

        #endregion

        [Fact]
        public unsafe void TestMetadataHeaders1()
        {
            fixed (byte* pdbPtr = TestResources.Documents.Pdb)
            {
                var pdbReader = new MetadataReader(pdbPtr, TestResources.Documents.Pdb.Length);
                Assert.Equal("PDB v0.1", pdbReader.MetadataVersion);
                Assert.Equal(MetadataKind.Ecma335, pdbReader.MetadataKind);
                Assert.False(pdbReader.IsAssembly);
                Assert.True(pdbReader.DebugMetadataHeader.EntryPoint.IsNil);
            }
        }

        [Fact]
        public void TestGetDocuments1()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Documents.DllAndPdb);

            int actualCount;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(0, out actualCount, null));
            Assert.Equal(11, actualCount);

            var actualDocuments = new ISymUnmanagedDocument[actualCount];
            int actualCount2;
            Assert.Equal(HResult.S_OK, symReader.GetDocuments(actualCount, out actualCount2, actualDocuments));
            Assert.Equal(11, actualCount2);

            ValidateDocument(actualDocuments[0],
                url: @"C:\Documents.cs", 
                algorithmId: "ff1816ec-aa5e-4d10-87f7-6f4963833460", 
                checksum: new byte[] { 0xDB, 0xEB, 0x2A, 0x06, 0x7B, 0x2F, 0x0E, 0x0D, 0x67, 0x8A, 0x00, 0x2C, 0x58, 0x7A, 0x28, 0x06, 0x05, 0x6C, 0x3D, 0xCE });

            ValidateDocument(actualDocuments[1], url: @"C:\a\b\c\d\1.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[2], url: @"C:\a\b\c\D\2.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[3], url: @"C:\a\b\C\d\3.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[4], url: @"C:\a\b\c\d\x.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[5], url: @"C:\A\b\c\x.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[6], url: @"C:\a\b\x.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[7], url: @"C:\a\B\3.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[8], url: @"C:\a\B\c\4.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[9], url: @"C:\*\5.cs", algorithmId: null, checksum: null);
            ValidateDocument(actualDocuments[10], url: @":6.cs", algorithmId: null, checksum: null);
        }

        [Fact]
        public void TestGetDocument1()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Documents.DllAndPdb);
            TestGetDocument(symReader, @"x.cs", expectedUrl: @"C:\a\b\c\d\x.cs");
            TestGetDocument(symReader, @"X.CS", expectedUrl: @"C:\a\b\c\d\x.cs");
            TestGetDocument(symReader, @"1.cs", expectedUrl: @"C:\a\b\c\d\1.cs");
            TestGetDocument(symReader, @"2.cs", expectedUrl: @"C:\a\b\c\D\2.cs");
            TestGetDocument(symReader, @"3.cs", expectedUrl: @"C:\a\b\C\d\3.cs");
            TestGetDocument(symReader, @"C:\A\b\c\x.cs", expectedUrl: @"C:\A\b\c\x.cs");
            TestGetDocument(symReader, @"C:\a\b\x.cs", expectedUrl: @"C:\a\b\x.cs");
            TestGetDocument(symReader, @"C:\*\5.cs", expectedUrl: @"C:\*\5.cs");
            TestGetDocument(symReader, @"5.cs", expectedUrl: @"C:\*\5.cs");
            TestGetDocument(symReader, @":6.cs", expectedUrl: @":6.cs");
        }

        private void TestGetDocument(SymReader symReader, string name, string expectedUrl)
        {
            ISymUnmanagedDocument document;
            if (expectedUrl != null)
            {
                // guids are ignored
                Assert.Equal(HResult.S_OK, symReader.GetDocument(name, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), out document));
                ValidateDocumentUrl(document, expectedUrl);
            }
            else
            {
                // guids are ignored
                Assert.Equal(HResult.S_FALSE, symReader.GetDocument(name, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), out document));
                Assert.Null(document);
            }
        }

        [Fact]
        public void TestSymGetAttribute()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Documents.DllAndPdb);

            int actualCount;
            int actualCount2;
            Assert.Equal(HResult.S_FALSE, symReader.GetSymAttribute(0, "<PortablePdbImage>", 0, out actualCount, null));

            byte[] image = new byte[actualCount];
            Assert.Equal(HResult.S_OK, symReader.GetSymAttribute(0, "<PortablePdbImage>", actualCount, out actualCount2, image));
            Assert.Equal(actualCount, actualCount2);

            AssertEx.Equal(TestResources.Documents.Pdb, image);
        }

        [Fact]
        public void TestMethods1()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Scopes.DllAndPdb);
            int count;

            //
            //  C<S>.F<T>
            //

            ISymUnmanagedMethod mF;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(0x06000002, out mF));

            // root scope:
            ISymUnmanagedScope rootScope, rootScopeCopy;
            Assert.Equal(HResult.S_OK, mF.GetRootScope(out rootScope));
            Assert.Equal(HResult.S_OK, mF.GetRootScope(out rootScopeCopy));
            Assert.NotSame(rootScope, rootScopeCopy);

            ValidateRange(rootScope, 0, 2);
            ValidateRootScope(rootScope);

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

            ValidateConstant(constants[0], "B", (short)0, new byte[] { 0x02 });
            ValidateConstant(constants[1], "C", (ushort)'\0', new byte[] { 0x03 });
            ValidateConstant(constants[2], "I1", (short)1, new byte[] { 0x04 });
            ValidateConstant(constants[3], "U1", (short)2, new byte[] { 0x05 });
            ValidateConstant(constants[4], "I2", (short)3, new byte[] { 0x06 });
            ValidateConstant(constants[5], "U2", (ushort)4, new byte[] { 0x07 });
            ValidateConstant(constants[6], "I4", 5, new byte[] { 0x08 });
            ValidateConstant(constants[7], "U4", (uint)6, new byte[] { 0x09 });
            ValidateConstant(constants[8], "I8", (long)7, new byte[] { 0x0A });
            ValidateConstant(constants[9], "U8", (ulong)8, new byte[] { 0x0B });
            ValidateConstant(constants[10], "R4", (float)9.1, new byte[] { 0x0C });
            ValidateConstant(constants[11], "R8", 10.2, new byte[] { 0x0D });

            ValidateConstant(constants[12], "EI1", (short)1, new byte[] { 0x11, 0x06 });
            ValidateConstant(constants[13], "EU1", (short)2, new byte[] { 0x11, 0x0A });
            ValidateConstant(constants[14], "EI2", (short)3, new byte[] { 0x11, 0x0E });
            ValidateConstant(constants[15], "EU2", (ushort)4, new byte[] { 0x11, 0x12 });
            ValidateConstant(constants[16], "EI4", 5, new byte[] { 0x11, 0x16 });
            ValidateConstant(constants[17], "EU4", (uint)6, new byte[] { 0x11, 0x1A });
            ValidateConstant(constants[18], "EI8", (long)7, new byte[] { 0x11, 0x1E });
            ValidateConstant(constants[19], "EU8", (ulong)8, new byte[] { 0x11, 0x22 });

            ValidateConstant(constants[20], "StrWithNul", "\0", new byte[] { 0x0e });
            ValidateConstant(constants[21], "EmptyStr", "", new byte[] { 0x0e });
            ValidateConstant(constants[22], "NullStr", 0, new byte[] { 0x0e });
            ValidateConstant(constants[23], "NullObject", 0, new byte[] { 0x1c });
            ValidateConstant(constants[24], "NullDynamic", 0, new byte[] { 0x1c });

            // Note: Natvie PDBs produce expanded form of the signature stored as StandAloneSig.
            // In Portable PDBs we produce a TypeSpec. Since a StandAlongSig can also contain a TypeSpec 
            // the consumers should be able to resolve it. If we find a case where that's not true we can
            // potentially expand the TypeSpec signature in ISymUnmanagedConstant.GetValue.
            ValidateConstant(constants[25], "NullTypeDef", 0, new byte[] { 0x12, 0x08 });
            ValidateConstant(constants[26], "NullTypeRef", 0, new byte[] { 0x12, 0x1D });
            ValidateConstant(constants[27], "NullTypeSpec", 0, new byte[] { 0x12, 0x26 });

            ValidateConstant(constants[28], "D", 123456.78M, new byte[] { 0x11, 0x2D });

            //
            //  C<S>.NestedScopes
            //

            ISymUnmanagedMethod mNestedScopes;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(0x06000003, out mNestedScopes));

            // root scope:
            Assert.Equal(HResult.S_OK, mNestedScopes.GetRootScope(out rootScope));
            ValidateRootScope(rootScope);

            var main = GetAndValidateChildScopes(rootScope, expectedCount: 1)[0];
            constants = GetAndValidateConstants(main, expectedCount: 0);
            var variables = GetAndValidateVariables(main, expectedCount: 2);

            ValidateVariable(variables[0], "x0", 0, LocalVariableAttributes.None, new byte[] { 0x08 });
            ValidateVariable(variables[1], "y0", 1, LocalVariableAttributes.None, new byte[] { 0x08 });

            children = GetAndValidateChildScopes(main, expectedCount: 2);
            var first = children[0];
            GetAndValidateChildScopes(first, expectedCount: 0);
            var second = children[1];
            var third = GetAndValidateChildScopes(second, expectedCount: 1)[0];
            GetAndValidateChildScopes(third, expectedCount: 0);

            constants = GetAndValidateConstants(first, expectedCount: 1);
            variables = GetAndValidateVariables(first, expectedCount: 1);
            ValidateConstant(constants[0], "c1", 11, new byte[] { 0x08 });
            ValidateVariable(variables[0], "x1", 2, LocalVariableAttributes.None, new byte[] { 0x08 });

            constants = GetAndValidateConstants(second, expectedCount: 0);
            variables = GetAndValidateVariables(second, expectedCount: 1);
            ValidateVariable(variables[0], "y1", 3, LocalVariableAttributes.None, new byte[] { 0x08 });

            constants = GetAndValidateConstants(third, expectedCount: 2);
            variables = GetAndValidateVariables(third, expectedCount: 1);
            ValidateConstant(constants[0], "c2", "c2", new byte[] { 0x0e });
            ValidateConstant(constants[1], "d2", "d2", new byte[] { 0x0e });
            ValidateVariable(variables[0], "y2", 4, LocalVariableAttributes.None, new byte[] { 0x08 });

            // TODO:
            // f.GetOffset();
            // f.GetRanges();

            ISymUnmanagedNamespace ns;
            ISymUnmanagedVariable[] ps = null;
            Assert.Equal(HResult.E_NOTIMPL, mF.GetNamespace(out ns));
            Assert.Equal(HResult.E_NOTIMPL, mF.GetParameters(0, out count, ps));
            // TODO:
            // f.GetScopeFromOffset()
        }

        [Fact]
        public void TestAsyncMethods()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Async.DllAndPdb);

            ValidateAsyncMethod(
                symReader, 
                moveNextMethodToken: 0x06000005,
                kickoffMethodToken: 0x06000001,
                catchHandlerOffset: -1,
                yieldOffsets: new[] { 0x46, 0xAF, 0x11A }, 
                resumeOffsets: new[] { 0x64, 0xCE, 0x136 });

            ValidateAsyncMethod(
                symReader,
                moveNextMethodToken: 0x06000008,
                kickoffMethodToken: 0x06000002,
                catchHandlerOffset: 0x76,
                yieldOffsets: new[] { 0x2D },
                resumeOffsets: new[] { 0x48 });
        }

        [Fact]
        public void TestAsyncMethods_GetAsyncStepInfo()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Async.DllAndPdb);

            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(0x06000005, out method));

            var asyncMethod = (ISymUnmanagedAsyncMethod)method;

            var actualYieldOffsets = new int[1];
            var actualResumeOffsets = new int[1];
            var actualResumeMethods = new int[1];

            int count2;
            Assert.Equal(HResult.S_OK, asyncMethod.GetAsyncStepInfo(1, out count2, actualYieldOffsets, actualResumeOffsets, actualResumeMethods));

            Assert.Equal(1, count2);
            Assert.NotEqual(0, actualYieldOffsets[0]);
            Assert.NotEqual(0, actualResumeOffsets[0]);
            Assert.NotEqual(0, actualResumeMethods[0]);

            actualYieldOffsets = new int[5];
            actualResumeOffsets = new int[5];
            actualResumeMethods = new int[5];

            Assert.Equal(HResult.S_OK, asyncMethod.GetAsyncStepInfo(4, out count2, actualYieldOffsets, actualResumeOffsets, actualResumeMethods));

            Assert.Equal(3, count2);

            for (int i = 0; i < 3; i++)
            {
                Assert.NotEqual(0, actualYieldOffsets[i]);
                Assert.NotEqual(0, actualResumeOffsets[i]);
                Assert.NotEqual(0, actualResumeMethods[i]);
            }

            for (int i = 3; i < 5; i++)
            {
                Assert.Equal(0, actualYieldOffsets[i]);
                Assert.Equal(0, actualResumeOffsets[i]);
                Assert.Equal(0, actualResumeMethods[i]);
            }
        }

        [Fact]
        public void TestAsyncMethods_Errors()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Scopes.DllAndPdb);

            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(0x06000002, out method));

            var asyncMethod = (ISymUnmanagedAsyncMethod)method;

            bool isAsync;
            Assert.Equal(HResult.S_OK, asyncMethod.IsAsyncMethod(out isAsync));
            Assert.False(isAsync);

            int actualKickoffMethodToken;
            Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.GetKickoffMethod(out actualKickoffMethodToken));

            bool hasCatchHandlerILOffset;
            Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.HasCatchHandlerILOffset(out hasCatchHandlerILOffset));

            int actualCatchHandlerOffset;
            Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.GetCatchHandlerILOffset(out actualCatchHandlerOffset));

            int count, count2;
            Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.GetAsyncStepInfoCount(out count));
            Assert.Equal(HResult.E_UNEXPECTED, asyncMethod.GetAsyncStepInfo(count, out count2, null, null, null));
        }
    }
}
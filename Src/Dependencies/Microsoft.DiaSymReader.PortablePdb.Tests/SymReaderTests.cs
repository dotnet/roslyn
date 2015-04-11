// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System;
using System.IO;
using System.Reflection.Metadata;
using Xunit;
using System.Reflection.Metadata.Ecma335;
using System.Linq;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    public class SymReaderTests
    {
        private static SymReader CreateSymReaderFromResource(string name)
        {
            MetadataReader mdReader;
            return CreateSymReaderFromResource(name, out mdReader);
        }

        private static SymReader CreateSymReaderFromResource(string name, out MetadataReader mdReader)
        {
            var importer = new SymMetadataImport(TestResources.ResourceHelper.GetResourceStream(name + ".dll"));
            mdReader = importer.MetadataReader;
            return new SymReader(new PortablePdbReader(TestResources.ResourceHelper.GetResourceStream(name + ".pdb"), importer));
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
            
            // TODO: signature:
            //Assert.Equal(HResult.S_OK, variable.GetSignature(0, out length, null));
            //var actualSignature = new byte[length];
            //Assert.Equal(HResult.S_OK, variable.GetSignature(length, out length2, actualSignature));
            //Assert.Equal(length, length2);
            //AssertEx.Equal(signature, actualSignature);
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
            var symReader = CreateSymReaderFromResource("z");

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
            var symReader = CreateSymReaderFromResource("z");

            int actualCount;
            int actualCount2;
            Assert.Equal(HResult.S_FALSE, symReader.GetSymAttribute(0, "<PortablePdbImage>", 0, out actualCount, null));

            byte[] image = new byte[actualCount];
            Assert.Equal(HResult.S_OK, symReader.GetSymAttribute(0, "<PortablePdbImage>", actualCount, out actualCount2, image));
            Assert.Equal(actualCount, actualCount2);

            AssertEx.Equal(TestResources.Z.pdb, image);
        }

        [Fact]
        public void TestMethods1()
        {
            var symReader = CreateSymReaderFromResource("Scopes");
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
            ValidateConstant(constants[22], "NullStr", null, new byte[] { 0x0e });
            ValidateConstant(constants[23], "NullObject", null, new byte[] { 0x1c });
            ValidateConstant(constants[24], "NullDynamic", null, new byte[] { 0x1c });

            // Note: Natvie PDBs produce expanded form of the signature stored as StandAloneSig.
            // In Portable PDBs we produce a TypeSpec. Since a StandAlongSig can also contain a TypeSpec 
            // the consumers should be able to resolve it. If we find a case where that's not true we can
            // potentially expand the TypeSpec signature in ISymUnmanagedConstant.GetValue.
            ValidateConstant(constants[25], "NullTypeDef", null, new byte[] { 0x12, 0x08 });
            ValidateConstant(constants[26], "NullTypeRef", null, new byte[] { 0x12, 0x1D });
            ValidateConstant(constants[27], "NullTypeSpec", null, new byte[] { 0x12, 0x26 });

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
    }
}

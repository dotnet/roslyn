// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    internal static class SymTestHelpers
    {
        public static ISymUnmanagedReader CreateSymReaderFromResource(KeyValuePair<byte[], byte[]> peAndPdb)
        {
            return SymReaderFactory.CreateReader(new MemoryStream(peAndPdb.Value), new SymMetadataImport(new MemoryStream(peAndPdb.Key)));
        }

        public static void ValidateDocumentUrl(ISymUnmanagedDocument document, string url)
        {
            int actualCount, actualCount2;
            Assert.Equal(HResult.S_OK, document.GetUrl(0, out actualCount, null));

            char[] actualUrl = new char[actualCount];
            Assert.Equal(HResult.S_OK, document.GetUrl(actualCount, out actualCount2, actualUrl));
            Assert.Equal(url, new string(actualUrl, 0, actualUrl.Length - 1));
        }

        public static void ValidateDocument(ISymUnmanagedDocument document, string url, string algorithmId, byte[] checksum)
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

        public static void ValidateRange(ISymUnmanagedScope scope, int expectedStartOffset, int expectedLength)
        {
            int actualOffset;
            Assert.Equal(HResult.S_OK, scope.GetStartOffset(out actualOffset));
            Assert.Equal(expectedStartOffset, actualOffset);

            Assert.Equal(HResult.S_OK, scope.GetEndOffset(out actualOffset));
            Assert.Equal(expectedStartOffset + expectedLength, 2);
        }

        public static void ValidateConstant(ISymUnmanagedConstant constant, string name, object value, byte[] signature)
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

        public static void ValidateVariable(ISymUnmanagedVariable variable, string name, int slot, LocalVariableAttributes attributes, byte[] signature)
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

        public static void ValidateRootScope(ISymUnmanagedScope scope)
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

        public static ISymUnmanagedScope[] GetAndValidateChildScopes(ISymUnmanagedScope scope, int expectedCount)
        {
            int count, count2;
            Assert.Equal(HResult.S_OK, scope.GetChildren(0, out count, null));
            Assert.Equal(expectedCount, count);
            var children = new ISymUnmanagedScope[count];
            Assert.Equal(HResult.S_OK, scope.GetChildren(count, out count2, children));
            Assert.Equal(count, count2);
            return children;
        }

        public static ISymUnmanagedConstant[] GetAndValidateConstants(ISymUnmanagedScope scope, int expectedCount)
        {
            int count, count2;
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetConstants(0, out count, null));
            Assert.Equal(expectedCount, count);
            var constants = new ISymUnmanagedConstant[count];
            Assert.Equal(HResult.S_OK, ((ISymUnmanagedScope2)scope).GetConstants(count, out count2, constants));
            Assert.Equal(count, count2);
            return constants;
        }

        public static ISymUnmanagedVariable[] GetAndValidateVariables(ISymUnmanagedScope scope, int expectedCount)
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

        public static void ValidateAsyncMethod(ISymUnmanagedReader symReader, int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, int[] yieldOffsets, int[] resumeOffsets)
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

        internal static int[] GetMethodTokensFromDocumentPosition(
            ISymUnmanagedReader symReader,
            ISymUnmanagedDocument symDocument,
            int line,
            int column)
        {
            Assert.True(line >= 1);
            Assert.True(column >= 0);

            int count;
            Assert.Equal(HResult.S_OK, symReader.GetMethodsFromDocumentPosition(symDocument, line, column, 0, out count, null));

            var methods = new ISymUnmanagedMethod[count];
            int count2;
            Assert.Equal(HResult.S_OK, symReader.GetMethodsFromDocumentPosition(symDocument, line, column, count, out count2, methods));
            Assert.Equal(count, count2);

            return methods.Select(m =>
            {
                int token;
                Assert.Equal(HResult.S_OK, m.GetToken(out token));
                return token;
            }).ToArray();
        }

        internal static int[][] GetMethodTokensForEachLine(ISymUnmanagedReader symReader, ISymUnmanagedDocument symDocument, int minLine, int maxLine)
        {
            Assert.True(minLine >= 1);
            Assert.True(maxLine >= minLine);

            var result = new List<int[]>();

            for (int line = minLine; line <= maxLine; line++)
            {
                int[] allMethodTokens = GetMethodTokensFromDocumentPosition(symReader, symDocument, line, 0);

                ISymUnmanagedMethod method;
                int hr = symReader.GetMethodFromDocumentPosition(symDocument, line, 1, out method);

                if (hr != HResult.S_OK)
                {
                    Assert.Equal(HResult.E_FAIL, hr);
                    Assert.Equal(0, allMethodTokens.Length);
                }
                else
                {
                    int primaryToken;
                    Assert.Equal(HResult.S_OK, method.GetToken(out primaryToken));
                    Assert.Equal(primaryToken, allMethodTokens.First());
                }

                result.Add(allMethodTokens);
            }

            return result.ToArray();
        }

        public static int[] GetILOffsetForEachLine(
            ISymUnmanagedReader symReader,
            int methodToken, 
            ISymUnmanagedDocument document, 
            int minLine, int maxLine)
        {
            Assert.True(minLine >= 1);
            Assert.True(maxLine >= minLine);

            var result = new List<int>();

            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(methodToken, out method));

            for (int line = minLine; line <= maxLine; line++)
            {
                int offset;
                int hr = method.GetOffset(document, line, 0, out offset);

                if (hr != HResult.S_OK)
                {
                    Assert.Equal(HResult.E_FAIL, hr);
                    offset = int.MaxValue;
                }

                result.Add(offset);
            }

            return result.ToArray();
        }

        public static int[][] GetILOffsetRangesForEachLine(
            ISymUnmanagedReader symReader,
            int methodToken,
            ISymUnmanagedDocument document,
            int minLine, int maxLine)
        {
            Assert.True(minLine >= 1);
            Assert.True(maxLine >= minLine);

            var result = new List<int[]>();

            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(methodToken, out method));

            for (int line = minLine; line <= maxLine; line++)
            {
                int count;
                Assert.Equal(HResult.S_OK, method.GetRanges(document, line, 0, 0, out count, null));

                int count2;
                int[] ranges = new int[count];
                Assert.Equal(HResult.S_OK, method.GetRanges(document, line, 0, count, out count2, ranges));
                Assert.Equal(count, count2);

                result.Add(ranges);
            }

            return result.ToArray();
        }

        public static void ValidateMethodExtent(ISymUnmanagedReader symReader, int methodDef, string documentName, int minLine, int maxLine)
        {
            Assert.True(minLine >= 1);
            Assert.True(maxLine >= minLine);

            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(methodDef, out method));

            ISymUnmanagedDocument document;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(documentName, default(Guid), default(Guid), default(Guid), out document));

            int actualMinLine, actualMaxLine;
            Assert.Equal(HResult.S_OK, ((ISymEncUnmanagedMethod)method).GetSourceExtentInDocument(document, out actualMinLine, out actualMaxLine));

            Assert.Equal(minLine, actualMinLine);
            Assert.Equal(maxLine, actualMaxLine);
        }

        public static void ValidateNoMethodExtent(ISymUnmanagedReader symReader, int methodDef, string documentName)
        {
            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(methodDef, out method));

            ISymUnmanagedDocument document;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(documentName, default(Guid), default(Guid), default(Guid), out document));

            int actualMinLine, actualMaxLine;
            Assert.Equal(HResult.E_FAIL, ((ISymEncUnmanagedMethod)method).GetSourceExtentInDocument(document, out actualMinLine, out actualMaxLine));
        }

        public static int[] FindClosestLineForEachLine(ISymUnmanagedDocument document, int minLine, int maxLine)
        {
            Assert.True(minLine >= 1);
            Assert.True(maxLine >= minLine);

            var result = new List<int>();
                     
            for (int line = minLine; line <= maxLine; line++)
            {
                int closestLine;
                int hr = document.FindClosestLine(line, out closestLine);

                if (hr != HResult.S_OK)
                {
                    Assert.Equal(HResult.E_FAIL, hr);
                    closestLine = 0;
                }

                result.Add(closestLine);
            }

            return result.ToArray();
        }
    }
}

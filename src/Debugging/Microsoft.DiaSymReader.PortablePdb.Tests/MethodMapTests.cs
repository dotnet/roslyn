// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.PortablePdb.UnitTests
{
    using static SymTestHelpers;

    public class MethodMapTests
    {
        private void TestGetMethodFromDocumentPosition(
            ISymUnmanagedReader symReader,
            ISymUnmanagedDocument symDocument,
            int zeroBasedLine,
            int zeroBasedColumn,
            int expectedToken)
        {
            ISymUnmanagedMethod method;
            Assert.Equal(HResult.S_OK, symReader.GetMethodFromDocumentPosition(symDocument, zeroBasedLine, zeroBasedColumn, out method));

            int token;
            Assert.Equal(HResult.S_OK, method.GetToken(out token));
            Assert.Equal(expectedToken, token);
        }

        private int[] GetMethodTokensFromDocumentPosition(
            ISymUnmanagedReader symReader,
            ISymUnmanagedDocument symDocument,
            int zeroBasedLine,
            int zeroBasedColumn)
        {
            int count;
            Assert.Equal(HResult.S_OK, symReader.GetMethodsFromDocumentPosition(symDocument, zeroBasedLine, zeroBasedColumn, 0, out count, null));

            var methods = new ISymUnmanagedMethod[count];
            int count2;
            Assert.Equal(HResult.S_OK, symReader.GetMethodsFromDocumentPosition(symDocument, zeroBasedLine, zeroBasedColumn, count, out count2, methods));
            Assert.Equal(count, count2);

            return methods.Select(m =>
            {
                int token;
                Assert.Equal(HResult.S_OK, m.GetToken(out token));
                return token;
            }).ToArray();
        }

        private int[][] GetMethodTokensForEachLine(ISymUnmanagedReader symReader, ISymUnmanagedDocument symDocument, int minZeroBasedLine, int maxZeroBasedLine)
        {
            var result = new List<int[]>();

            for (int line = minZeroBasedLine; line <= maxZeroBasedLine; line++)
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

        private const int tokenCtor = 0x06000001;
        private const int tokenF = 0x06000002;
        private const int tokenG = 0x06000003;
        private const int tokenE0 = 0x06000004;
        private const int tokenE1 = 0x06000005;
        private const int tokenH = 0x06000006;
        private const int tokenE2 = 0x06000007;
        private const int tokenE3 = 0x06000008;
        private const int tokenE4 = 0x06000009;
        private const int tokenJ1 = 0x0600000A;
        private const int tokenI = 0x0600000B;
        private const int tokenJ2 = 0x0600000C;
        private const int tokenK1 = 0x0600000D;
        private const int tokenK2 = 0x0600000E;
        private const int tokenK3 = 0x0600000F;
        private const int tokenK4 = 0x06000010;

        private static string TokenToConstantName(int token)
        {
            switch (token)
            {
                case 0: return "0";
                case tokenCtor: return nameof(tokenCtor);
                case tokenF: return nameof(tokenF);
                case tokenG: return nameof(tokenG);
                case tokenH: return nameof(tokenH);
                case tokenE0: return nameof(tokenE0);
                case tokenE1: return nameof(tokenE1);
                case tokenE2: return nameof(tokenE2);
                case tokenE3: return nameof(tokenE3);
                case tokenE4: return nameof(tokenE4);
                case tokenI: return nameof(tokenI);
                case tokenJ1: return nameof(tokenJ1);
                case tokenJ2: return nameof(tokenJ2);
                case tokenK1: return nameof(tokenK1);
                case tokenK2: return nameof(tokenK2);
                case tokenK3: return nameof(tokenK3);
                case tokenK4: return nameof(tokenK4);
                default: return token.ToString("X");
            }
        }

        private static readonly Func<int[], string> _tokenInspector = t =>
            t.Length == 0 ? "NoTokens" : "new[] { " + string.Join(", ", t.Select(TokenToConstantName)) + " }";

        private static readonly int[] NoTokens = Array.Empty<int>();

        [Fact]
        public void GetMethodFromDocumentPosition_UsingDIA_Native()
        {
            var symReader = CreateSymReaderFromResource(TestResources.MethodBoundaries.DllAndPdb);

            ISymUnmanagedDocument document1, document2, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries2.cs", default(Guid), default(Guid), default(Guid), out document2));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            var tokens = GetMethodTokensForEachLine(symReader, document1, 0, 29);

            AssertEx.Equal(new int[][]
            {
                NoTokens,                              // 1
                NoTokens,                              // 2
                NoTokens,                              // 3
                NoTokens,                              // 4
                new[] { tokenG },                      // 5
                new[] { tokenCtor, tokenF, tokenG  },  // 6
                new[] { tokenCtor, tokenG },           // 7
                new[] { tokenF },                      // 8
                new[] { tokenF, tokenG },              // 9
                new[] { tokenCtor, tokenG },           // 10
                new[] { tokenCtor, tokenF },           // 11
                new[] { tokenCtor },                   // 12
                new[] { tokenCtor },                   // 13
                NoTokens,                              // 14
                new[] { tokenCtor },                   // 15
                NoTokens,                              // 16
                NoTokens,                              // 17
                new[] { tokenF },                      // 18
                NoTokens,                              // 19
                NoTokens,                              // 20
                new[] { tokenF },                      // 21
                NoTokens,                              // 22
                new[] { tokenF },                      // 23
                new[] { tokenF },                      // 24
                NoTokens,                              // 25
                NoTokens,                              // 26
                NoTokens,                              // 27
                NoTokens,                              // 28
                NoTokens,                              // 29
                NoTokens,                              // 30
            }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document2, 0, 29);

            AssertEx.Equal(new int[][]
            {
                NoTokens,                    // 1
                new[] { tokenF },            // 2
                NoTokens,                    // 3
                NoTokens,                    // 4
                new[] { tokenH },            // 5
                new[] { tokenE0, tokenH },   // 6
                new[] { tokenE2 },           // 7
                new[] { tokenE1 },           // 8
                new[] { tokenE3 },           // 9
                new[] { tokenH, tokenE4 },   // 10
                new[] { tokenH },            // 11
                new[] { tokenI },            // 12
                new[] { tokenI },            // 13
                new[] { tokenJ1 },           // 14
                new[] { tokenJ1 },           // 15
                new[] { tokenJ1 },           // 16
                new[] { tokenJ2 },           // 17
                new[] { tokenJ2 },           // 18
                NoTokens,                    // 19
                NoTokens,                    // 20
                NoTokens,                    // 21
                NoTokens,                    // 22
                new[] { tokenI },            // 23
                new[] { tokenI },            // 24
                NoTokens,                    // 25
                NoTokens,                    // 26
                NoTokens,                    // 27
                NoTokens,                    // 28
                new[] { tokenJ2 },           // 29
                NoTokens,                    // 30
            }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document3, 0, 29);

            AssertEx.Equal(new int[][]
            {
                NoTokens,                     // 1
                new[] { tokenK1 },            // 2
                new[] { tokenK1 },            // 3
                new[] { tokenK2 },            // 4
                new[] { tokenK2 },            // 5
                new[] { tokenK3 },            // 6
                new[] { tokenK3 },            // 7
                new[] { tokenK4 },            // 8
                new[] { tokenK4 },            // 9
                NoTokens,                     // 10
                new[] { tokenK3 },            // 11
                new[] { tokenK2 },            // 12
                new[] { tokenK1 },            // 13
                NoTokens,                     // 14
                NoTokens,                     // 15
                NoTokens,                     // 16
                NoTokens,                     // 17
                NoTokens,                     // 18
                NoTokens,                     // 19
                NoTokens,                     // 20
                NoTokens,                     // 21
                NoTokens,                     // 22
                NoTokens,                     // 23
                NoTokens,                     // 24
                NoTokens,                     // 25
                NoTokens,                     // 26
                NoTokens,                     // 27
                NoTokens,                     // 28
                NoTokens,                     // 29
                NoTokens,                     // 30
            }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));
        }

        [Fact]
        public void GetMethodFromDocumentPosition_UsingBoundaries_Native()
        {
            GetMethodFromDocumentPosition_UsingBoundaries(TestResources.MethodBoundaries.DllAndPdb);
        }

        [Fact]
        public void GetMethodFromDocumentPosition_UsingBoundaries_Portable()
        {
            GetMethodFromDocumentPosition_UsingBoundaries(TestResources.MethodBoundaries.PortableDllAndPdb);
        }

        private void GetMethodFromDocumentPosition_UsingBoundaries(KeyValuePair<byte[], byte[]> dllAndPdb)
        {
            var symReader = CreateSymReaderFromResource(dllAndPdb);

            ISymUnmanagedDocument document1, document2, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries2.cs", default(Guid), default(Guid), default(Guid), out document2));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            // calling GetSourceExtentInDocument will flip DiaSymReader to MethodBoundaries:
            ISymUnmanagedMethod methodCtor;
            Assert.Equal(HResult.S_OK, symReader.GetMethod(tokenCtor, out methodCtor));
            int sl, el;
            Assert.Equal(HResult.S_OK, ((ISymEncUnmanagedMethod)methodCtor).GetSourceExtentInDocument(document1, out sl, out el));

            int[][] tokens = GetMethodTokensForEachLine(symReader, document1, 0, 29);

            AssertEx.Equal(new int[][]
            {
                NoTokens,                                 // 1
                NoTokens,                                 // 2
                NoTokens,                                 // 3
                NoTokens,                                 // 4
                new int[] { tokenG },                     // 5
                new int[] { tokenCtor, tokenF, tokenG },  // 6
                new int[] { tokenCtor, tokenF, tokenG },  // 7
                new int[] { tokenCtor, tokenF, tokenG },  // 8 (DIA: tokenF)
                new int[] { tokenCtor, tokenF, tokenG },  // 9 (DIA: tokenF, tokenG)
                new int[] { tokenCtor, tokenF, tokenG },  // 10
                new int[] { tokenCtor, tokenF },          // 11
                new int[] { tokenCtor, tokenF },          // 12
                new int[] { tokenCtor, tokenF },          // 13
                new int[] { tokenCtor, tokenF },          // 14 (DIA: 0)
                new int[] { tokenCtor, tokenF },          // 15
                new int[] { tokenF },                     // 16 (DIA: 0)
                new int[] { tokenF },                     // 17 (DIA: 0)
                new int[] { tokenF },                     // 18
                new int[] { tokenF },                     // 19 (DIA: 0)
                new int[] { tokenF },                     // 20 (DIA: 0)
                new int[] { tokenF },                     // 21
                new int[] { tokenF },                     // 22 (DIA: 0)
                new int[] { tokenF },                     // 23
                new int[] { tokenF },                     // 24
                NoTokens,                                 // 25
                NoTokens,                                 // 26
                NoTokens,                                 // 27
                NoTokens,                                 // 28
                NoTokens,                                 // 29
                NoTokens,                                 // 30
             }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document2, 0, 29);

            AssertEx.Equal(new int[][]
            {
                 NoTokens,                               // 1      
                 new int[] { tokenF },                   // 2  
                 NoTokens,                               // 3    
                 NoTokens,                               // 4    
                 new int[] { tokenH },                   // 5   
                 new int[] { tokenE0, tokenH },          // 6  
                 new int[] { tokenH, tokenE2 },          // 7   (DIA: tokenE2)    
                 new int[] { tokenE1, tokenH },          // 8   
                 new int[] { tokenH, tokenE3 },          // 9   (DIA: tokenE3)    
                 new int[] { tokenH, tokenE3, tokenE4 }, // 10  
                 new int[] { tokenH },                   // 11  
                 new int[] { tokenI },                   // 12     
                 new int[] { tokenI },                   // 13     
                 new int[] { tokenJ1, tokenI },          // 14     
                 new int[] { tokenJ1, tokenI },          // 15     
                 new int[] { tokenJ1, tokenI },          // 16     
                 new int[] { tokenI, tokenJ2 },          // 17  (DIA: tokenJ2) (!)    
                 new int[] { tokenI, tokenJ2 },          // 18  (DIA: tokenJ2) (!)   
                 new int[] { tokenI, tokenJ2 },          // 19  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 20  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 21  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 22  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 23  
                 new int[] { tokenI, tokenJ2 },          // 24  
                 new int[] { tokenJ2 },                  // 25  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 26  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 27  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 28  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 29  (DIA: tokenJ2) (!)      
                 NoTokens,                               // 30        
             }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document3, 0, 29);

            AssertEx.Equal(new int[][]
            {
                NoTokens,                                            // 1
                new int[] { tokenK1 },                               // 2   
                new int[] { tokenK1 },                               // 3   
                new int[] { tokenK1, tokenK2 },                      // 4   (DIA: tokenK2)    
                new int[] { tokenK1, tokenK2 },                      // 5   (DIA: tokenK2)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 6   (DIA: tokenK3)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 7   (DIA: tokenK3)    
                new int[] { tokenK1, tokenK2, tokenK3, tokenK4 },    // 8   (DIA: tokenK4)    
                new int[] { tokenK1, tokenK2, tokenK3, tokenK4 },    // 9   (DIA: tokenK4)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 10  (DIA: 0)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 11  (DIA: tokenK3)    
                new int[] { tokenK1, tokenK2 },                      // 12  (DIA: tokenK2)    
                new int[] { tokenK1 },                               // 13  
                NoTokens,                                            // 14
                NoTokens,                                            // 15
                NoTokens,                                            // 16
                NoTokens,                                            // 17
                NoTokens,                                            // 18
                NoTokens,                                            // 19
                NoTokens,                                            // 20
                NoTokens,                                            // 21
                NoTokens,                                            // 22
                NoTokens,                                            // 23
                NoTokens,                                            // 24
                NoTokens,                                            // 25
                NoTokens,                                            // 26
                NoTokens,                                            // 27
                NoTokens,                                            // 28
                NoTokens,                                            // 29
                NoTokens,                                            // 30
             }, tokens, itemInspector: _tokenInspector, comparer: (x, y) => x.SequenceEqual(y));
        }
    }
}

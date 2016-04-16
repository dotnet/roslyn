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

        private static readonly Func<int[], string> s_tokenInspector = tokens =>
            tokens.Length == 0 ? "NoTokens" : "new[] { " + string.Join(", ", tokens.Select(TokenToConstantName)) + " }";

        private static readonly Func<int, string> s_ilOffsetInspector = offset =>
            (offset == int.MaxValue) ? "NoOffset" : (offset >= 0) ? "0x" + offset.ToString("X2") : "-0x" + (-offset).ToString("X2");

        private static readonly Func<int[], string> s_rangeInspector = ranges =>
            ranges.Length == 0 ? "NoRange" : "new[] { " + string.Join(", ", ranges.Select(i => "0x" + i.ToString("X2"))) + " }";

        private static readonly int[] s_noTokens = new int[0];
        private static readonly int[] s_noRange = new int[0];
        private static readonly int s_noOffset = int.MaxValue;

        [Fact]
        public void GetMethodFromDocumentPosition_UsingDIA_Native()
        {
            var symReader = CreateSymReaderFromResource(TestResources.MethodBoundaries.DllAndPdb);

            ISymUnmanagedDocument document1, document2, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries2.cs", default(Guid), default(Guid), default(Guid), out document2));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            var tokens = GetMethodTokensForEachLine(symReader, document1, 1, 29);

            AssertEx.Equal(new int[][]
            {
                s_noTokens,                              // 1 
                s_noTokens,                              // 2
                s_noTokens,                              // 3
                new[] { tokenG },                      // 4
                new[] { tokenCtor, tokenF, tokenG  },  // 5
                new[] { tokenCtor, tokenG },           // 6
                new[] { tokenF },                      // 7
                new[] { tokenF, tokenG },              // 8
                new[] { tokenCtor, tokenG },           // 9
                new[] { tokenCtor, tokenF },           // 10
                new[] { tokenCtor },                   // 11
                new[] { tokenCtor },                   // 12
                s_noTokens,                              // 13
                new[] { tokenCtor },                   // 14
                s_noTokens,                              // 15
                s_noTokens,                              // 16
                new[] { tokenF },                      // 17
                s_noTokens,                              // 18
                s_noTokens,                              // 19
                new[] { tokenF },                      // 20
                s_noTokens,                              // 21
                new[] { tokenF },                      // 22
                new[] { tokenF },                      // 23
                s_noTokens,                              // 24
                s_noTokens,                              // 25
                s_noTokens,                              // 26
                s_noTokens,                              // 27
                s_noTokens,                              // 28
                s_noTokens,                              // 29
            }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document2, 1, 29);

            AssertEx.Equal(new int[][]
            {
                new[] { tokenF },            // 1
                s_noTokens,                    // 2
                s_noTokens,                    // 3
                new[] { tokenH },            // 4
                new[] { tokenE0, tokenH },   // 5
                new[] { tokenE2 },           // 6
                new[] { tokenE1 },           // 7
                new[] { tokenE3 },           // 8
                new[] { tokenH, tokenE4 },   // 9
                new[] { tokenH },            // 10
                new[] { tokenI },            // 11
                new[] { tokenI },            // 12
                new[] { tokenJ1 },           // 13
                new[] { tokenJ1 },           // 14
                new[] { tokenJ1 },           // 15
                new[] { tokenJ2 },           // 16
                new[] { tokenJ2 },           // 17
                s_noTokens,                    // 18
                s_noTokens,                    // 19
                s_noTokens,                    // 20
                s_noTokens,                    // 21
                new[] { tokenI },            // 22
                new[] { tokenI },            // 23
                s_noTokens,                    // 24
                s_noTokens,                    // 25
                s_noTokens,                    // 26
                s_noTokens,                    // 27
                new[] { tokenJ2 },           // 28
                s_noTokens,                    // 29
            }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document3, 1, 29);

            AssertEx.Equal(new int[][]
            {
                new[] { tokenK1 },            // 1
                new[] { tokenK1 },            // 2
                new[] { tokenK2 },            // 3
                new[] { tokenK2 },            // 4
                new[] { tokenK3 },            // 5
                new[] { tokenK3 },            // 6
                new[] { tokenK4 },            // 7
                new[] { tokenK4 },            // 8
                s_noTokens,                     // 9
                new[] { tokenK3 },            // 10
                new[] { tokenK2 },            // 11
                new[] { tokenK1 },            // 12
                s_noTokens,                     // 13
                s_noTokens,                     // 14
                s_noTokens,                     // 15
                s_noTokens,                     // 16
                s_noTokens,                     // 17
                s_noTokens,                     // 18
                s_noTokens,                     // 19
                s_noTokens,                     // 20
                s_noTokens,                     // 21
                s_noTokens,                     // 22
                s_noTokens,                     // 23
                s_noTokens,                     // 24
                s_noTokens,                     // 25
                s_noTokens,                     // 26
                s_noTokens,                     // 27
                s_noTokens,                     // 28
                s_noTokens,                     // 29
            }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));
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

            int[][] tokens = GetMethodTokensForEachLine(symReader, document1, 1, 29);

            AssertEx.Equal(new int[][]
            {
                s_noTokens,                                 // 1
                s_noTokens,                                 // 2
                s_noTokens,                                 // 3
                new int[] { tokenG },                     // 4
                new int[] { tokenCtor, tokenF, tokenG },  // 5
                new int[] { tokenCtor, tokenF, tokenG },  // 6
                new int[] { tokenCtor, tokenF, tokenG },  // 7 (DIA: tokenF)
                new int[] { tokenCtor, tokenF, tokenG },  // 8 (DIA: tokenF, tokenG)
                new int[] { tokenCtor, tokenF, tokenG },  // 9 
                new int[] { tokenCtor, tokenF },          // 10
                new int[] { tokenCtor, tokenF },          // 11
                new int[] { tokenCtor, tokenF },          // 12
                new int[] { tokenCtor, tokenF },          // 13 (DIA: 0)
                new int[] { tokenCtor, tokenF },          // 14 
                new int[] { tokenF },                     // 15 (DIA: 0)
                new int[] { tokenF },                     // 16 (DIA: 0)
                new int[] { tokenF },                     // 17 
                new int[] { tokenF },                     // 18 (DIA: 0)
                new int[] { tokenF },                     // 19 (DIA: 0)
                new int[] { tokenF },                     // 20 
                new int[] { tokenF },                     // 21 (DIA: 0)
                new int[] { tokenF },                     // 22 
                new int[] { tokenF },                     // 23
                s_noTokens,                                 // 24
                s_noTokens,                                 // 25
                s_noTokens,                                 // 26
                s_noTokens,                                 // 27
                s_noTokens,                                 // 28
                s_noTokens,                                 // 29
             }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document2, 1, 29);

            AssertEx.Equal(new int[][]
            {
                 new int[] { tokenF },                   // 1      
                 s_noTokens,                               // 2  
                 s_noTokens,                               // 3    
                 new int[] { tokenH },                   // 4    
                 new int[] { tokenE0, tokenH },          // 5   
                 new int[] { tokenH, tokenE2 },          // 6   (DIA: tokenE2)   
                 new int[] { tokenE1, tokenH },          // 7    
                 new int[] { tokenH, tokenE3 },          // 8   (DIA: tokenE3)    
                 new int[] { tokenH, tokenE3, tokenE4 }, // 9   
                 new int[] { tokenH },                   // 10  
                 new int[] { tokenI },                   // 11     
                 new int[] { tokenI },                   // 12     
                 new int[] { tokenJ1, tokenI },          // 13     
                 new int[] { tokenJ1, tokenI },          // 14     
                 new int[] { tokenJ1, tokenI },          // 15     
                 new int[] { tokenI, tokenJ2 },          // 16  (DIA: tokenJ2) (!)
                 new int[] { tokenI, tokenJ2 },          // 17  (DIA: tokenJ2) (!)    
                 new int[] { tokenI, tokenJ2 },          // 18  (DIA: 0)             
                 new int[] { tokenI, tokenJ2 },          // 19  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 20  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 21  (DIA: 0)          
                 new int[] { tokenI, tokenJ2 },          // 22  
                 new int[] { tokenI, tokenJ2 },          // 23  
                 new int[] { tokenJ2 },                  // 24  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 25  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 26  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 27  (DIA: 0)          
                 new int[] { tokenJ2 },                  // 28  (DIA: tokenJ2) (!)  
                 s_noTokens,                               // 29        
             }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));

            tokens = GetMethodTokensForEachLine(symReader, document3, 1, 29);

            AssertEx.Equal(new int[][]
            {
                new int[] { tokenK1 },                               // 1
                new int[] { tokenK1 },                               // 2   
                new int[] { tokenK1, tokenK2 },                      // 3   (DIA: tokenK2)   
                new int[] { tokenK1, tokenK2 },                      // 4   (DIA: tokenK2)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 5   (DIA: tokenK3)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 6   (DIA: tokenK3)    
                new int[] { tokenK1, tokenK2, tokenK3, tokenK4 },    // 7   (DIA: tokenK4)    
                new int[] { tokenK1, tokenK2, tokenK3, tokenK4 },    // 8   (DIA: tokenK4)    
                new int[] { tokenK1, tokenK2, tokenK3 },             // 9   (DIA: 0)       
                new int[] { tokenK1, tokenK2, tokenK3 },             // 10  (DIA: tokenK3) 
                new int[] { tokenK1, tokenK2 },                      // 11  (DIA: tokenK2)    
                new int[] { tokenK1 },                               // 12   
                s_noTokens,                                            // 13  
                s_noTokens,                                            // 14
                s_noTokens,                                            // 15
                s_noTokens,                                            // 16
                s_noTokens,                                            // 17
                s_noTokens,                                            // 18
                s_noTokens,                                            // 19
                s_noTokens,                                            // 20
                s_noTokens,                                            // 21
                s_noTokens,                                            // 22
                s_noTokens,                                            // 23
                s_noTokens,                                            // 24
                s_noTokens,                                            // 25
                s_noTokens,                                            // 26
                s_noTokens,                                            // 27
                s_noTokens,                                            // 28
                s_noTokens,                                            // 29
             }, tokens, itemInspector: s_tokenInspector, comparer: (x, y) => x.SequenceEqual(y));
        }

        [Fact]
        public void GetSourceExtentInDocument_Native()
        {
            GetSourceExtentInDocument(TestResources.MethodBoundaries.DllAndPdb);
        }

        [Fact]
        public void GetSourceExtentInDocument_Portable()
        {
            GetSourceExtentInDocument(TestResources.MethodBoundaries.PortableDllAndPdb);
        }

        private void GetSourceExtentInDocument(KeyValuePair<byte[], byte[]> dllAndPdb)
        {
            var symReader = CreateSymReaderFromResource(dllAndPdb);

            ValidateMethodExtent(symReader, tokenCtor, "MethodBoundaries1.cs", 5, 14);
            ValidateNoMethodExtent(symReader, tokenCtor, "MethodBoundaries2.cs");
            ValidateNoMethodExtent(symReader, tokenCtor, "MethodBoundaries3.cs");

            ValidateMethodExtent(symReader, tokenF, "MethodBoundaries1.cs", 5, 23);
            ValidateMethodExtent(symReader, tokenF, "MethodBoundaries2.cs", 1, 1);
            ValidateNoMethodExtent(symReader, tokenF, "MethodBoundaries3.cs");

            ValidateMethodExtent(symReader, tokenG, "MethodBoundaries1.cs", 4, 9);
            ValidateNoMethodExtent(symReader, tokenG, "MethodBoundaries2.cs");
            ValidateNoMethodExtent(symReader, tokenG, "MethodBoundaries3.cs");

            ValidateMethodExtent(symReader, tokenE0, "MethodBoundaries2.cs", 5, 5);
            ValidateMethodExtent(symReader, tokenE1, "MethodBoundaries2.cs", 7, 7);
            ValidateMethodExtent(symReader, tokenH, "MethodBoundaries2.cs", 4, 10);
            ValidateMethodExtent(symReader, tokenE2, "MethodBoundaries2.cs", 6, 6);
            ValidateMethodExtent(symReader, tokenE3, "MethodBoundaries2.cs", 8, 9);
            ValidateMethodExtent(symReader, tokenE4, "MethodBoundaries2.cs", 9, 9);
            ValidateMethodExtent(symReader, tokenJ1, "MethodBoundaries2.cs", 13, 15);
            ValidateMethodExtent(symReader, tokenI, "MethodBoundaries2.cs", 11, 23);
            ValidateMethodExtent(symReader, tokenJ2, "MethodBoundaries2.cs", 16, 28);
            ValidateMethodExtent(symReader, tokenK1, "MethodBoundaries3.cs", 1, 12);
            ValidateMethodExtent(symReader, tokenK2, "MethodBoundaries3.cs", 3, 11);
            ValidateMethodExtent(symReader, tokenK3, "MethodBoundaries3.cs", 5, 10);
            ValidateMethodExtent(symReader, tokenK4, "MethodBoundaries3.cs", 7, 8);
        }

        [Fact]
        public void GetOffset_Native()
        {
            var symReader = CreateSymReaderFromResource(TestResources.MethodBoundaries.DllAndPdb);

            ISymUnmanagedDocument document1;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));

            // GetOffsets seems to be very much broken. Not returning correct values for lines that are shared among multiple methods and 
            // for lines that are outside of the specified method.

            int[] offsets = GetILOffsetForEachLine(symReader, tokenCtor, document1, 1, 29);
            AssertEx.Equal(new int[]
            {
                s_noOffset,           // 1
                s_noOffset,           // 2
                s_noOffset,           // 3
                0x5C,               // 4  (G)
                0x00,               // 5  (OK)
                0x00,               // 6  (OK)
                0x38,               // 7  (F)
                0x3E,               // 8  (F)
                0x1C,               // 9  (OK)
                0x23,               // 10 (OK)
                0x24,               // 11 (OK)
                0x2A,               // 12 (OK)
                s_noOffset,           // 13
                0x11,               // 14 (OK)
                s_noOffset,           // 15
                s_noOffset,           // 16
                0x2B,               // 17 (F)
                s_noOffset,           // 18
                s_noOffset,           // 19
                0x50,               // 20 (F)
                s_noOffset,           // 21
                0x56,               // 22 (F)
                0x5A,               // 23 (F)
                s_noOffset,           // 24
                s_noOffset,           // 25
                s_noOffset,           // 26
                s_noOffset,           // 27
                s_noOffset,           // 28
                s_noOffset            // 29
            }, offsets, itemInspector: s_ilOffsetInspector);

            offsets = GetILOffsetForEachLine(symReader, tokenF, document1, 1, 29);
            AssertEx.Equal(new int[]
            {
                s_noOffset,           // 1
                s_noOffset,           // 2
                s_noOffset,           // 3
                0x31,               // 4
                -0x2B,              // 5 
                -0x2B,              // 6 
                0x0D,               // 7
                0x13,               // 8 
                -0x0F,              // 9 
                -0x08,              // 10
                -0x07,              // 11
                -0x01,              // 12
                s_noOffset,           // 13
                -0x1A,              // 14
                s_noOffset,           // 15
                s_noOffset,           // 16
                0x00,               // 17
                s_noOffset,           // 18
                s_noOffset,           // 19
                0x25,               // 20
                s_noOffset,           // 21
                0x2B,               // 22
                0x2F,               // 23
                s_noOffset,           // 24
                s_noOffset,           // 25
                s_noOffset,           // 26
                s_noOffset,           // 27
                s_noOffset,           // 28
                s_noOffset            // 29
            }, offsets, itemInspector: s_ilOffsetInspector);
        }

        [Fact]
        public void GetOffset_Portable()
        {
            var symReader = CreateSymReaderFromResource(TestResources.MethodBoundaries.PortableDllAndPdb);

            ISymUnmanagedDocument document1, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            int[] offsets = GetILOffsetForEachLine(symReader, tokenCtor, document1, 1, 29);
            AssertEx.Equal(new int[]
            {
                s_noOffset,       // 1
                s_noOffset,       // 2
                s_noOffset,       // 3
                s_noOffset,       // 4 
                0x00,           // 5 
                0x00,           // 6 
                0x00,           // 7 
                s_noOffset,       // 8 
                0x1C,           // 9 
                0x23,           // 10
                0x24,           // 11
                0x2A,           // 12
                s_noOffset,       // 13
                0x11,           // 14
                s_noOffset,       // 15
                s_noOffset,       // 16
                s_noOffset,       // 17
                s_noOffset,       // 18
                s_noOffset,       // 19
                s_noOffset,       // 20
                s_noOffset,       // 21
                s_noOffset,       // 22
                s_noOffset,       // 23
                s_noOffset,       // 24
                s_noOffset,       // 25
                s_noOffset,       // 26
                s_noOffset,       // 27
                s_noOffset,       // 28
                s_noOffset        // 29
            }, offsets, itemInspector: s_ilOffsetInspector);

            offsets = GetILOffsetForEachLine(symReader, tokenF, document1, 1, 29);
            AssertEx.Equal(new int[]
            {
                s_noOffset,       // 1
                s_noOffset,       // 2
                s_noOffset,       // 3
                s_noOffset,       // 4
                0x07,           // 5 (the first IL offset)
                s_noOffset,       // 6 
                0x0D,           // 7
                0x13,           // 8 
                s_noOffset,       // 9 
                0x01,           // 10
                s_noOffset,       // 11
                s_noOffset,       // 12
                s_noOffset,       // 13
                s_noOffset,       // 14
                s_noOffset,       // 15
                s_noOffset,       // 16
                0x00,           // 17
                s_noOffset,       // 18
                s_noOffset,       // 19
                0x25,           // 20
                s_noOffset,       // 21
                0x2B,           // 22
                0x2F,           // 23
                s_noOffset,       // 24
                s_noOffset,       // 25
                s_noOffset,       // 26
                s_noOffset,       // 27
                s_noOffset,       // 28
                s_noOffset        // 29
            }, offsets, itemInspector: s_ilOffsetInspector);

            offsets = GetILOffsetForEachLine(symReader, tokenK1, document3, 1, 14);
            AssertEx.Equal(new int[]
            {
                0x00,       // 1
                0x01,       // 2
                0x01,       // 3
                0x01,       // 4
                0x01,       // 5
                0x01,       // 6 
                0x01,       // 7
                0x01,       // 8 
                0x01,       // 9 
                0x01,       // 10
                0x01,       // 11
                0x07,       // 12
                s_noOffset,   // 13
                s_noOffset,   // 14
            }, offsets, itemInspector: s_ilOffsetInspector);
        }

        [Fact]
        public void GetRanges_Portable()
        {
            GetRanges(TestResources.MethodBoundaries.PortableDllAndPdb);
        }

        [Fact]
        public void GetRanges_Native()
        {
            GetRanges(TestResources.MethodBoundaries.DllAndPdb);
        }

        public void GetRanges(KeyValuePair<byte[], byte[]> dllAndPdb)
        {
            var symReader = CreateSymReaderFromResource(dllAndPdb);
            bool isPortable = (symReader as SymReader) != null;

            ISymUnmanagedDocument document1, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument("MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            var ranges = GetILOffsetRangesForEachLine(symReader, tokenCtor, document1, 1, 29);

            AssertEx.Equal(new int[][]
            {
                s_noRange,                    // 1
                s_noRange,                    // 2
                s_noRange,                    // 3
                s_noRange,                    // 4 
                new[] { 0x00, 0x11 },       // 5 
                new[] { 0x00, 0x11 },       // 6 
                isPortable ? new[] { 0x00, 0x11 } : s_noRange, // 7 (bug in DSR)
                s_noRange,                    // 8 
                new[] { 0x1C, 0x23 },       // 9 
                new[] { 0x23, 0x24 },       // 10
                new[] { 0x24, 0x2A },       // 11
                new[] { 0x2A, 0x2B },       // 12
                s_noRange,                    // 13
                new[] { 0x11, 0x1C },       // 14
                s_noRange,                    // 15
                s_noRange,                    // 16
                s_noRange,                    // 17
                s_noRange,                    // 18
                s_noRange,                    // 19
                s_noRange,                    // 20
                s_noRange,                    // 21
                s_noRange,                    // 22
                s_noRange,                    // 23
                s_noRange,                    // 24
                s_noRange,                    // 25
                s_noRange,                    // 26
                s_noRange,                    // 27
                s_noRange,                    // 28
                s_noRange                     // 29
            }, ranges, itemInspector: s_rangeInspector, comparer: (x, y) => x.SequenceEqual(y));

            ranges = GetILOffsetRangesForEachLine(symReader, tokenF, document1, 1, 29);

            AssertEx.Equal(new int[][]
            {
                s_noRange,                          // 1
                s_noRange,                          // 2
                s_noRange,                          // 3
                s_noRange,                          // 4 
                new[] { 0x07, 0x0D, 0x19, 0x1F }, // 5 
                s_noRange,                          // 6 
                new[] { 0x0D, 0x13 },             // 7 
                new[] { 0x13, 0x19 },             // 8 
                s_noRange,                          // 9 
                new[] { 0x01, 0x07 },             // 10
                s_noRange,                          // 11
                s_noRange,                          // 12
                s_noRange,                          // 13
                s_noRange,                          // 14
                s_noRange,                          // 15
                s_noRange,                          // 16
                new[] { 0x00, 0x01 },             // 17
                s_noRange,                          // 18
                s_noRange,                          // 19
                new[] { 0x25, 0x2B },             // 20
                s_noRange,                          // 21
                new[] { 0x2B, 0x2F },             // 22
                new[] { 0x2F, 0x31 },             // 23
                s_noRange,                          // 24
                s_noRange,                          // 25
                s_noRange,                          // 26
                s_noRange,                          // 27
                s_noRange,                          // 28
                s_noRange                           // 29
            }, ranges, itemInspector: s_rangeInspector, comparer: (x, y) => x.SequenceEqual(y));
        }

        [Fact]
        public void GetRangesHiddenSP_Portable()
        {
            GetRangesHiddenSP(TestResources.Async.PortableDllAndPdb);
        }

        [Fact]
        public void GetRangesHiddenSP_Native()
        {
            GetRangesHiddenSP(TestResources.Async.DllAndPdb);
        }

        public void GetRangesHiddenSP(KeyValuePair<byte[], byte[]> dllAndPdb)
        {
            var symReader = CreateSymReaderFromResource(dllAndPdb);

            const int M1_MoveNext = 0x06000005;

            ISymUnmanagedDocument document;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"C:\Async.cs", default(Guid), default(Guid), default(Guid), out document));

            var ranges = GetILOffsetRangesForEachLine(symReader, M1_MoveNext, document, 6, 16);

            AssertEx.Equal(new int[][]
            {
                s_noRange,                // 6 
                s_noRange,                // 7 
                new[] { 0x27, 0x28 },   // 8 
                new[] { 0x28, 0x34 },   // 9 
                new[] { 0x90, 0x9D },   // 10
                new[] { 0xFB, 0x108 },  // 11
                s_noRange,                // 12
                new[] { 0x163, 0x167 }, // 13
                new[] { 0x181, 0x189 }, // 14
                s_noRange,                // 15
                s_noRange                 // 16
            }, ranges, itemInspector: s_rangeInspector, comparer: (x, y) => x.SequenceEqual(y));
        }

        [Fact]
        public void FindClosestLine1_Portable()
        {
            FindClosestLine1(TestResources.MethodBoundaries.PortableDllAndPdb);
        }

        [Fact]
        public void FindClosestLine1_Native()
        {
            FindClosestLine1(TestResources.MethodBoundaries.DllAndPdb);
        }

        private void FindClosestLine1(KeyValuePair<byte[], byte[]> dllAndPdb)
        {
            var symReader = CreateSymReaderFromResource(dllAndPdb);

            ISymUnmanagedDocument document1, document2, document3;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"MethodBoundaries1.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"MethodBoundaries2.cs", default(Guid), default(Guid), default(Guid), out document2));
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"MethodBoundaries3.cs", default(Guid), default(Guid), default(Guid), out document3));

            var closestLines = FindClosestLineForEachLine(document1, 1, 29);
            AssertEx.Equal(new int[]
            {
                4,       // 1
                4,       // 2
                4,       // 3
                4,       // 4 
                5,       // 5 
                7,       // 6 
                7,       // 7 
                8,       // 8 
                9,       // 9 
                10,      // 10
                11,      // 11
                12,      // 12
                14,      // 13
                14,      // 14
                17,      // 15
                17,      // 16
                17,      // 17
                20,      // 18
                20,      // 19
                20,      // 20
                22,      // 21
                22,      // 22
                23,      // 23
                0,       // 24
                0,       // 25
                0,       // 26
                0,       // 27
                0,       // 28
                0        // 29
            }, closestLines);

            closestLines = FindClosestLineForEachLine(document2, 1, 29);
            AssertEx.Equal(new int[]
            {
                1,      // 1
                4,      // 2
                4,      // 3
                4,      // 4 
                5,      // 5 
                6,      // 6 
                7,      // 7 
                8,      // 8 
                9,      // 9 
                10,     // 10
                11,     // 11
                12,     // 12
                13,     // 13
                14,     // 14
                15,     // 15
                16,     // 16
                17,     // 17
                22,     // 18
                22,     // 19
                22,     // 20
                22,     // 21
                22,     // 22
                23,     // 23
                28,     // 24
                28,     // 25
                28,     // 26
                28,     // 27
                28,     // 28
                0       // 29
            }, closestLines);

            closestLines = FindClosestLineForEachLine(document3, 1, 29);
            AssertEx.Equal(new int[]
            {
                1,      // 1
                2,      // 2
                3,      // 3
                4,      // 4 
                5,      // 5 
                6,      // 6 
                7,      // 7 
                10,     // 8 
                10,     // 9 
                10,     // 10
                11,     // 11
                12,     // 12
                0,      // 13
                0,      // 14
                0,      // 15
                0,      // 16
                0,      // 17
                0,      // 18
                0,      // 19
                0,      // 20
                0,      // 21
                0,      // 22
                0,      // 23
                0,      // 24
                0,      // 25
                0,      // 26
                0,      // 27
                0,      // 28
                0       // 29
            }, closestLines);
        }

        [Fact]
        public void FindClosestLine2_Portable()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Documents.PortableDllAndPdb);

            ISymUnmanagedDocument document1, document2;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"C:\a\b\X.cs", default(Guid), default(Guid), default(Guid), out document1));
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"C:\a\B\x.cs", default(Guid), default(Guid), default(Guid), out document2));

            var closestLines = FindClosestLineForEachLine(document1, 120, 135);
            AssertEx.Equal(new int[]
            {
                120,     // 120
                0,       // 121
                0,       // 122
                0,       // 123 
                0,       // 124 
                0,       // 125 
                0,       // 126 
                0,       // 127 
                0,       // 128 
                0,       // 129
                0,       // 130
                0,       // 131
                0,       // 132
                0,       // 133
                0,       // 134
                0,       // 135
            }, closestLines);

            closestLines = FindClosestLineForEachLine(document2, 120, 135);
            AssertEx.Equal(new int[]
            {
                130,     // 120
                130,     // 121
                130,     // 122
                130,     // 123 
                130,     // 124 
                130,     // 125 
                130,     // 126 
                130,     // 127 
                130,     // 128 
                130,     // 129
                130,     // 130
                131,     // 131
                0,       // 132
                0,       // 133
                0,       // 134
                0        // 135
            }, closestLines);
        }

        [Fact]
        public void FindClosestLine2_Native()
        {
            var symReader = CreateSymReaderFromResource(TestResources.Documents.DllAndPdb);

            ISymUnmanagedDocument document1;
            Assert.Equal(HResult.S_OK, symReader.GetDocument(@"C:\a\b\X.cs", default(Guid), default(Guid), default(Guid), out document1));

            var closestLines = FindClosestLineForEachLine(document1, 120, 135);
            AssertEx.Equal(new int[]
            {
                120,     // 120
                130,     // 121
                130,     // 122
                130,     // 123 
                130,     // 124 
                130,     // 125 
                130,     // 126 
                130,     // 127 
                130,     // 128 
                130,     // 129
                130,     // 130
                131,     // 131
                0,       // 132
                0,       // 133
                0,       // 134
                0,       // 135
            }, closestLines);
        }
    }
}

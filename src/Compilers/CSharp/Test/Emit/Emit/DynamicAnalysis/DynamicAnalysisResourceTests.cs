// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.DynamicAnalysis.UnitTests
{
    public class DynamicAnalysisResourceTests : CSharpTestBase
    {
        private static string ExampleSource = @"
using System;

public class C
{
    public static void Main()
    {
        Console.WriteLine(123);
    }
}

namespace Microsoft.CodeAnalysis.Runtime
{
    public static class Instrumentation
    {
        public static void CreatePayload(System.Guid mvid, int methodToken, ref bool[] payload, int payloadLength)
        {
        }

        public static void FlushPayload()
        {
        }
    }
}
";

        [Fact]
        public void TestSpansPresentInResource()
        {
            var c = CreateCompilationWithMscorlib(Parse(ExampleSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader);

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' 21-AE-49-AE-F1-D9-19-19-73-C7-4C-0F-07-71-91-A7-1F-00-5F-31 (SHA1)");

            Assert.Equal(4, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],
                "(7,8)-(7,31)",
                "(6,4)-(8,5)");

            VerifySpans(reader, reader.Methods[1]);
        }

        [Fact]
        public void TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
        {
            var c = CreateCompilationWithMscorlib(Parse(ExampleSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default);

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader);

            Assert.Null(reader);
        }

        private static void VerifySpans(DynamicAnalysisDataReader reader, DynamicAnalysisMethod methodData, params string[] expected)
        {
            AssertEx.Equal(expected, reader.GetSpans(methodData.Blob).Select(s => s.ToString()));
        }

        private void VerifyDocuments(DynamicAnalysisDataReader reader, ImmutableArray<DynamicAnalysisDocument> documents, params string[] expected)
        {
            var sha1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");

            var actual = from d in documents
                         let name = reader.GetDocumentName(d.Name)
                         let hash = d.Hash.IsNil ? "" : " " + BitConverter.ToString(reader.GetBytes(d.Hash))
                         let hashAlgGuid = reader.GetGuid(d.HashAlgorithm)
                         let hashAlg = (hashAlgGuid == sha1) ? " (SHA1)" : (hashAlgGuid == default(Guid)) ? "" : " " + hashAlgGuid.ToString()
                         select $"'{name}'{hash}{hashAlg}";

            AssertEx.Equal(expected, actual);
        }
    }
}

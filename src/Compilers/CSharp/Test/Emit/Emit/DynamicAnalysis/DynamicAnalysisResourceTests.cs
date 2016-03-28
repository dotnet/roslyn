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
        const string InstrumentationHelperSource = @"
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
        public void Resource1()
        {
            string source = @"
using System;

public class C
{
    public static void Main()
    {
        Console.WriteLine(123);
        Console.WriteLine(123);
    }
}
";

            var c = CreateCompilationWithMscorlib(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithEmitDynamicAnalysisData(true));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader);

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' AE-D9-10-9C-B0-76-3E-8D-4B-C8-EC-29-65-B5-CE-AD-D3-04-5C-6B (SHA1)");

            Assert.Equal(5, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],
                "(7,8)-(7,31)",
                "(8,8)-(8,31)");

            VerifySpans(reader, reader.Methods[1]);
        }

        [Fact]
        public void ResourceStatementKinds()
        {
            string source = @"
using System;

public class C
{
    public static void Main()
    {
        int z = 11;
        int x = z + 10;
        switch (z)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
            default:
                break;
        }

        if (x > 10)
        {
            x++;
        }
        else
        {
            x--;
        }

        for (int y = 0; y < 50; y++)
        {
            if (y < 30)
            {
                x++;
                continue;
            }
            else
                break;
        }

        int[] a = new int[] { 1, 2, 3, 4 };
        foreach (int i in a)
        {
            x++;
        }

        while (x < 100)
        {
            x++;
        }

        try
        {
            x++;
            if (x > 10)
            {
                throw new System.Exception();
            }
            x++;
        }
        catch (System.Exception e)
        {
            x++;
        }
        finally
        {
            x++;
        }

        lock (new object())
        {
            ;
        }

        Console.WriteLine(x);

        try
        {
            using ((System.IDisposable)new object())
            {
                ;
            }
        }
        catch (System.Exception e)
        {
        }

        return;
    }
}
";

            var c = CreateCompilationWithMscorlib(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithEmitDynamicAnalysisData(true));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader);

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' 9D-F9-51-10-8D-40-2E-84-BA-96-61-EB-69-4D-34-9D-A5-12-31-2D (SHA1)");

            Assert.Equal(5, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],
                "(7,8)-(7,19)",
                "(8,8)-(8,23)",
                "(12,16)-(12,22)",
                "(14,16)-(14,22)",
                "(16,16)-(16,22)",
                "(18,16)-(18,22)",
                "(9,16)-(9,17)",
                "(23,12)-(23,16)",
                "(27,12)-(27,16)",
                "(21,12)-(21,18)",
                "(30,17)-(30,22)",
                "(30,32)-(30,35)",
                "(34,16)-(34,20)",
                "(35,16)-(35,25)",
                "(38,16)-(38,22)",
                "(32,16)-(32,22)",
                "(41,8)-(41,43)",
                "(44,12)-(44,16)",
                "(42,26)-(42,27)",
                "(49,12)-(49,16)",
                "(47,15)-(47,22)",
                "(54,12)-(54,16)",
                "(57,16)-(57,45)",
                "(55,16)-(55,22)",
                "(59,12)-(59,16)",
                "(63,12)-(63,16)",
                "(67,12)-(67,16)",
                "(72,12)-(72,13)",
                "(70,14)-(70,26)",
                "(75,8)-(75,29)",
                "(81,16)-(81,17)",
                "(79,19)-(79,51)",
                "(88,8)-(88,15)");

            VerifySpans(reader, reader.Methods[1]);
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

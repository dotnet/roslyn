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
        public static bool[] CreatePayload(System.Guid mvid, int methodToken, int fileIndex, ref bool[] payload, int payloadLength)
        {
            return payload;
        }

        public static void FlushPayload()
        {
        }
    }
}
";

        const string ExampleSource = @"
using System;

public class C
{
    public static void Main()
    {
        Console.WriteLine(123);
        Console.WriteLine(123);
    }

    public static int Fred => 3;

    public static int Barney(int x) => x;

    public static int Wilma
    {
        get { return 12; }
        set { }
    }

    public static int Betty { get; }
}
";

        [Fact]
        public void TestSpansPresentInResource()
        {
            var c = CreateCompilationWithMscorlib(Parse(ExampleSource + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"));
       
            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' B2-C1-91-21-17-72-39-D7-D8-C8-AC-3C-09-F6-3C-FF-B7-E5-97-8E (SHA1)");

            Assert.Equal(10, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],                                      // Main
                "(5,4)-(9,5)",
                "(7,8)-(7,31)",
                "(8,8)-(8,31)");

            VerifySpans(reader, reader.Methods[1],                                      // Fred get
                "(11,4)-(11,32)",
                "(11,30)-(11,31)");

            VerifySpans(reader, reader.Methods[2],                                      // Barney
                "(13,4)-(13,41)",
                "(13,39)-(13,40)");

            VerifySpans(reader, reader.Methods[3],                                      // Wilma get
                "(17,8)-(17,26)",
                "(17,14)-(17,24)");

            VerifySpans(reader, reader.Methods[4],
                "(18,8)-(18,15)");                                                      // Wilma set

            VerifySpans(reader, reader.Methods[5],                                      // Betty get
                "(21,4)-(21,36)",
                "(21,30)-(21,34)");

            VerifySpans(reader, reader.Methods[6]);
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
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' 89-73-A7-64-40-88-BA-0A-21-33-05-5D-E7-22-9B-74-1C-6A-2C-DC (SHA1)");

            Assert.Equal(5, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],
                "(5,4)-(89,5)",
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

        [Fact]
        public void TestMethodSpansWithAttributes()
        {
            string source = @"
using System;
using System.Security;

public class C
{
    static int x;

    public static void Main()                       // Method 0
    {
        Fred();
    }
            
    [Obsolete()]
    static void Fred()                              // Method 1
    {
    }

    static C()                                      // Method 2
    {
        x = 12;
    }

    [Obsolete()]
    public C()                                      // Method 3
    {
    }

    int Wilma
    {
        [SecurityCritical]
        get { return 12; }                          // Method 4 
    }

    [Obsolete()]
    int Betty => 13;                                // Method 5

    [SecurityCritical]
    int Pebbles()                                   // Method 6
    {
        return 3;
    }

    [SecurityCritical]
    ref int BamBam(ref int x)                       // Method 7
    {
        return ref x;
    }

    [SecurityCritical]                              // Method 8
    C(int x)
    {
    }

    [Obsolete()]
    public int Barney => 13;                        // Method 9

    [SecurityCritical]
    public static C operator +(C a, C b)            // Method 10
    {
        return a;
    }
}
";

            var c = CreateCompilationWithMscorlib(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' A2-A4-F7-A4-E2-11-AB-FF-E1-61-3E-4D-2B-9F-C1-75-B8-2A-40-A2 (SHA1)");

            Assert.Equal(14, reader.Methods.Length);

            VerifySpans(reader, reader.Methods[0],
                "(8,4)-(11,5)",
                "(10,8)-(10,15)");

            VerifySpans(reader, reader.Methods[1],
                "(14,4)-(16,5)");

            VerifySpans(reader, reader.Methods[2],
                "(18,4)-(21,5)",
                "(20,8)-(20,15)");

            VerifySpans(reader, reader.Methods[3],
                "(24,4)-(26,5)");

            VerifySpans(reader, reader.Methods[4],
                "(31,8)-(31,26)",
                "(31,14)-(31,24)");

            VerifySpans(reader, reader.Methods[5],
                "(35,4)-(35,20)",
                "(35,17)-(35,19)");

            VerifySpans(reader, reader.Methods[6],
                "(38,4)-(41,5)",
                "(40,8)-(40,17)");

            VerifySpans(reader, reader.Methods[7],
                "(44,4)-(47,5)",
                "(46,8)-(46,21)");

            VerifySpans(reader, reader.Methods[8],
                "(50,4)-(52,5)");

            VerifySpans(reader, reader.Methods[9],
                "(55,4)-(55,28)",
                "(55,25)-(55,27)");

            VerifySpans(reader, reader.Methods[10],
                "(58,4)-(61,5)",
                "(60,8)-(60,17)");
        }

        [Fact]
        public void TestPatternSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 0
    {
        Student s = new Student();
        s.Name = ""Bozo"";
        s.GPA = 2.3;
        Operate(s);
    }
     
    static string Operate(Person p)                             // Method 1
    {
        switch (p)
        {
            case Student s when s.GPA > 3.5:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Student s:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Teacher t:
                return $""Teacher {t.Name} of {t.Subject}"";
            default:
                return $""Person {p.Name}"";
        }
    }
}

class Person { public string Name; }
class Teacher : Person { public string Subject; }
class Student : Person { public double GPA; }
";

            var c = CreateCompilationWithMscorlib(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifySpans(reader, reader.Methods[0],
                "(5,4)-(11,5)",
                "(7,8)-(7,34)",
                "(8,8)-(8,24)",
                "(9,8)-(9,20)",
                "(10,8)-(10,19)");

            VerifySpans(reader, reader.Methods[1],
                "(13,4)-(26,5)",
                "(18,16)-(18,56)",
                "(20,16)-(20,56)",
                "(22,16)-(22,58)",
                "(24,16)-(24,42)",
                "(15,16)-(15,17)");
        }

        [Fact]
        public void TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
        {
            var c = CreateCompilationWithMscorlib(Parse(ExampleSource + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default);

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            Assert.Null(reader);
        }

        private static void VerifySpans(DynamicAnalysisDataReader reader, DynamicAnalysisMethod methodData, params string[] expected)
        {
            AssertEx.Equal(expected, reader.GetSpans(methodData.Blob).Select(s => $"({s.StartLine},{s.StartColumn})-({s.EndLine},{s.EndColumn})"));
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

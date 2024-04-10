// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public static bool[] CreatePayload(System.Guid mvid, int methodToken, int[] fileIndices, ref bool[] payload, int payloadLength)
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

    public static int Pebbles { get; set; }
}
";

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionHasNewLineDependency)]
        public void TestSpansPresentInResource()
        {
            var c = CreateCompilation(Parse(ExampleSource + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' 44-3F-7C-A1-EF-CA-A8-16-40-D2-09-4F-3E-52-7C-44-8D-22-C8-02 (SHA1)");

            Assert.Equal(13, reader.Methods.Length);

            string[] sourceLines = ExampleSource.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,                         // Main
                new SpanResult(5, 4, 9, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 31, "Console.WriteLine(123)"),
                new SpanResult(8, 8, 8, 31, "Console.WriteLine(123)"));

            VerifySpans(reader, reader.Methods[1], sourceLines,                         // Fred get
                new SpanResult(11, 4, 11, 32, "public static int Fred => 3"),
                new SpanResult(11, 30, 11, 31, "3"));

            VerifySpans(reader, reader.Methods[2], sourceLines,                         // Barney
                new SpanResult(13, 4, 13, 41, "public static int Barney(int x) => x"),
                new SpanResult(13, 39, 13, 40, "x"));

            VerifySpans(reader, reader.Methods[3], sourceLines,                         // Wilma get
                new SpanResult(17, 8, 17, 26, "get { return 12; }"),
                new SpanResult(17, 14, 17, 24, "return 12"));

            VerifySpans(reader, reader.Methods[4], sourceLines,                         // Wilma set
                new SpanResult(18, 8, 18, 15, "set { }"));

            VerifySpans(reader, reader.Methods[5], sourceLines,                         // Betty get
                new SpanResult(21, 4, 21, 36, "public static int Betty { get; }"),
                new SpanResult(21, 30, 21, 34, "get"));

            VerifySpans(reader, reader.Methods[6], sourceLines,                         // Pebbles get
                new SpanResult(23, 4, 23, 43, "public static int Pebbles { get; set; }"),
                new SpanResult(23, 32, 23, 36, "get"));

            VerifySpans(reader, reader.Methods[7], sourceLines,                         // Pebbles set
                new SpanResult(23, 4, 23, 43, "public static int Pebbles { get; set; }"),
                new SpanResult(23, 37, 23, 41, "set"));

            VerifySpans(reader, reader.Methods[8]);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionHasNewLineDependency)]
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

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' 6A-DC-C0-8A-16-CB-7C-A5-99-8B-2E-0C-3C-81-69-2C-B2-10-EE-F1 (SHA1)");

            Assert.Equal(6, reader.Methods.Length);

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 89, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 19, "int z = 11"),
                new SpanResult(8, 8, 8, 23, "int x = z + 10"),
                new SpanResult(12, 16, 12, 22, "break"),
                new SpanResult(14, 16, 14, 22, "break"),
                new SpanResult(16, 16, 16, 22, "break"),
                new SpanResult(18, 16, 18, 22, "break"),
                new SpanResult(9, 16, 9, 17, "z"),
                new SpanResult(23, 12, 23, 16, "x++"),
                new SpanResult(27, 12, 27, 16, "x--"),
                new SpanResult(21, 12, 21, 18, "x > 10"),
                new SpanResult(30, 17, 30, 22, "y = 0"),
                new SpanResult(30, 32, 30, 35, "y++"),
                new SpanResult(34, 16, 34, 20, "x++"),
                new SpanResult(35, 16, 35, 25, "continue"),
                new SpanResult(38, 16, 38, 22, "break"),
                new SpanResult(32, 16, 32, 22, "y < 30"),
                new SpanResult(41, 8, 41, 43, "int[] a = new int[] { 1, 2, 3, 4 }"),
                new SpanResult(44, 12, 44, 16, "x++"),
                new SpanResult(42, 26, 42, 27, "a"),
                new SpanResult(49, 12, 49, 16, "x++"),
                new SpanResult(47, 15, 47, 22, "x < 100"),
                new SpanResult(54, 12, 54, 16, "x++"),
                new SpanResult(57, 16, 57, 45, "throw new System.Exception()"),
                new SpanResult(55, 16, 55, 22, "x > 10"),
                new SpanResult(59, 12, 59, 16, "x++"),
                new SpanResult(63, 12, 63, 16, "x++"),
                new SpanResult(67, 12, 67, 16, "x++"),
                new SpanResult(72, 12, 72, 13, ";"),
                new SpanResult(70, 14, 70, 26, "new object()"),
                new SpanResult(75, 8, 75, 29, "Console.WriteLine(x)"),
                new SpanResult(81, 16, 81, 17, ";"),
                new SpanResult(79, 19, 79, 51, "(System.IDisposable)new object()"),
                new SpanResult(88, 8, 88, 15, "return"));

            VerifySpans(reader, reader.Methods[1]);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.TestExecutionHasNewLineDependency)]
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

    [SecurityCritical]
    C(int x)                                        // Method 8
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

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            VerifyDocuments(reader, reader.Documents,
                @"'C:\myproject\doc1.cs' A3-08-94-55-7C-64-8D-C7-61-7A-11-0B-4B-68-2C-3B-51-C3-C4-58 (SHA1)");

            Assert.Equal(15, reader.Methods.Length);

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(8, 4, 11, 5, "public static void Main()"),
                new SpanResult(10, 8, 10, 15, "Fred()"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(14, 4, 16, 5, "static void Fred()"));

            VerifySpans(reader, reader.Methods[2], sourceLines,
                new SpanResult(18, 4, 21, 5, "static C()"),
                new SpanResult(20, 8, 20, 15, "x = 12"));

            VerifySpans(reader, reader.Methods[3], sourceLines,
                new SpanResult(24, 4, 26, 5, "public C()"));

            VerifySpans(reader, reader.Methods[4], sourceLines,
                new SpanResult(31, 8, 31, 26, "get {"),
                new SpanResult(31, 14, 31, 24, "return 12"));

            VerifySpans(reader, reader.Methods[5], sourceLines,
                new SpanResult(35, 4, 35, 20, "int Betty"),
                new SpanResult(35, 17, 35, 19, "13"));

            VerifySpans(reader, reader.Methods[6], sourceLines,
                new SpanResult(38, 4, 41, 5, "int Pebbles()"),
                new SpanResult(40, 8, 40, 17, "return 3"));

            VerifySpans(reader, reader.Methods[7], sourceLines,
                new SpanResult(44, 4, 47, 5, "ref int BamBam"),
                new SpanResult(46, 8, 46, 21, "return ref x"));

            VerifySpans(reader, reader.Methods[8], sourceLines,
                new SpanResult(50, 4, 52, 5, "C(int x)"));

            VerifySpans(reader, reader.Methods[9], sourceLines,
                new SpanResult(55, 4, 55, 28, "public int Barney"),
                new SpanResult(55, 25, 55, 27, "13"));

            VerifySpans(reader, reader.Methods[10], sourceLines,
                new SpanResult(58, 4, 61, 5, "public static C operator +"),
                new SpanResult(60, 8, 60, 17, "return a"));
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
            case Student s when (s.GPA < 2.0):
                return $""Failing Student {s.Name} ({s.GPA:N1})"";
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

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 11, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 34, "Student s = new Student()"),
                new SpanResult(8, 8, 8, 24, "s.Name = \"Bozo\""),
                new SpanResult(9, 8, 9, 20, "s.GPA = 2.3"),
                new SpanResult(10, 8, 10, 19, "Operate(s)"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(13, 4, 28, 5, "static string Operate(Person p)"),
                new SpanResult(17, 27, 17, 43, "when s.GPA > 3.5"),
                new SpanResult(19, 27, 19, 45, "when (s.GPA < 2.0)"),
                new SpanResult(18, 16, 18, 56, "return $\"Student {s.Name} ({s.GPA:N1})\""),
                new SpanResult(20, 16, 20, 64, "return $\"Failing Student {s.Name} ({s.GPA:N1})\""),
                new SpanResult(22, 16, 22, 56, "return $\"Student {s.Name} ({s.GPA:N1})\""),
                new SpanResult(24, 16, 24, 58, "return $\"Teacher {t.Name} of {t.Subject}\""),
                new SpanResult(26, 16, 26, 42, "return $\"Person {p.Name}\""),
                new SpanResult(15, 16, 15, 17, "p"));
        }

        [Fact]
        public void TestPatternSpans_WithSharedWhenExpression()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                            // Method 0
    {
        Method1(1, b1: false, b2: false);
    }

    static string Method1(int i, bool b1, bool b2)       // Method 1
    {
        switch (i)
        {
            case not 1 when b1:
                return ""b1"";
            case var _ when b2:
                return ""b2"";
            case 1:
                return ""1"";
            default:
                return ""default"";
        }
    }
}
";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(10, 4, 23, 5, "static string Method1(int i, bool b1, bool b2)"),
                new SpanResult(14, 23, 14, 30, "when b1:"),
                new SpanResult(16, 23, 16, 30, "when b2:"),
                new SpanResult(15, 16, 15, 28, @"return ""b1"";"),
                new SpanResult(17, 16, 17, 28, @"return ""b2"";"),
                new SpanResult(19, 16, 19, 27, @"return ""1"";"),
                new SpanResult(21, 16, 21, 33, @"return ""default"";"),
                new SpanResult(12, 16, 12, 17, "i"));
        }

        [Fact]
        public void TestDeconstructionSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main() // Method 1
    {
        var (x, y) = new C();
    }

    public void Deconstruct(out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 8, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 29, "var (x, y) = new C()"));
        }

        [Fact]
        public void TestForeachSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main() // Method 1
    {
        C[] a = null;
        foreach
            (var x
            in a)
            ;
    }
}
";
            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 12, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 21, "C[] a = null"),
                new SpanResult(11, 12, 11, 13, ";"),
                new SpanResult(10, 15, 10, 16, "a")
                );
        }

        [Fact]
        public void TestForeachDeconstructionSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main() // Method 1
    {
        C[] a = null;
        foreach
            (var (x, y)
            in a)
            ;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = 1;
        y = 2;
    }
}
";
            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 12, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 21, "C[] a = null"),
                new SpanResult(11, 12, 11, 13, ";"),
                new SpanResult(10, 15, 10, 16, "a")
                );
        }

        [Fact]
        public void TestFieldInitializerSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 0
    {
        TestMain();
    }

    static void TestMain()                                      // Method 1
    {
        C local = new C(); local = new C(1, 2);
    }

    static int Init() => 33;                                    // Method 2

    C()                                                         // Method 3
    {
        _z = 12;
    }

    static C()                                                  // Method 4
    {
        s_z = 123;
    }

    int _x = Init();
    int _y = Init() + 12;
    int _z;
    static int s_x = Init();
    static int s_y = Init() + 153;
    static int s_z;

    C(int x)                                                    // Method 5
    {
        _z = x;
    }

    C(int a, int b)                                             // Method 6
    {
        _z = a + b;
    }

    int Prop1 { get; } = 15;
    static int Prop2 { get; } = 255;
}
";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 8, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 19, "TestMain()"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(10, 4, 13, 5, "static void TestMain()"),
                new SpanResult(12, 8, 12, 26, "C local = new C()"),
                new SpanResult(12, 27, 12, 47, "local = new C(1, 2)"));

            VerifySpans(reader, reader.Methods[2], sourceLines,
                new SpanResult(15, 4, 15, 28, "static int Init() => 33"),
                new SpanResult(15, 25, 15, 27, "33"));

            VerifySpans(reader, reader.Methods[3], sourceLines,
                new SpanResult(17, 4, 20, 5, "C()"),
                new SpanResult(27, 13, 27, 19, "Init()"),
                new SpanResult(28, 13, 28, 24, "Init() + 12"),
                new SpanResult(44, 25, 44, 27, "15"),
                new SpanResult(19, 8, 19, 16, "_z = 12"));

            VerifySpans(reader, reader.Methods[4], sourceLines,
                new SpanResult(22, 4, 25, 5, "static C()"),
                new SpanResult(30, 21, 30, 27, "Init()"),
                new SpanResult(31, 21, 31, 33, "Init() + 153"),
                new SpanResult(45, 32, 45, 35, "255"),
                new SpanResult(24, 8, 24, 18, "s_z = 123"));

            VerifySpans(reader, reader.Methods[5], sourceLines,
                new SpanResult(34, 4, 37, 5, "C(int x)"),
                new SpanResult(27, 13, 27, 19, "Init()"),
                new SpanResult(28, 13, 28, 24, "Init() + 12"),
                new SpanResult(44, 25, 44, 27, "15"),
                new SpanResult(36, 8, 36, 15, "_z = x"));

            VerifySpans(reader, reader.Methods[6], sourceLines,
                new SpanResult(39, 4, 42, 5, "C(int a, int b)"),
                new SpanResult(27, 13, 27, 19, "Init()"),
                new SpanResult(28, 13, 28, 24, "Init() + 12"),
                new SpanResult(44, 25, 44, 27, "15"),
                new SpanResult(41, 8, 41, 19, "_z = a + b"));
        }

        [Fact]
        public void TestImplicitConstructorSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 0
    {
        TestMain();
    }

    static void TestMain()                                      // Method 1
    {
        C local = new C();
    }

    static int Init() => 33;                                    // Method 2

    int _x = Init();
    int _y = Init() + 12;
    static int s_x = Init();
    static int s_y = Init() + 153;
    static int s_z = 144;

    int Prop1 { get; } = 15;
    static int Prop2 { get; } = 255;
}
";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 8, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 19, "TestMain()"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(10, 4, 13, 5, "static void TestMain()"),
                new SpanResult(12, 8, 12, 26, "C local = new C()"));

            VerifySpans(reader, reader.Methods[2], sourceLines,
               new SpanResult(15, 4, 15, 28, "static int Init() => 33"),
               new SpanResult(15, 25, 15, 27, "33"));

            VerifySpans(reader, reader.Methods[5], sourceLines,                     // Synthesized instance constructor
                new SpanResult(17, 13, 17, 19, "Init()"),
                new SpanResult(18, 13, 18, 24, "Init() + 12"),
                new SpanResult(23, 25, 23, 27, "15"));

            VerifySpans(reader, reader.Methods[6], sourceLines,                     // Synthesized static constructor
                new SpanResult(19, 21, 19, 27, "Init()"),
                new SpanResult(20, 21, 20, 33, "Init() + 153"),
                new SpanResult(21, 21, 21, 24, "144"),
                new SpanResult(24, 32, 24, 35, "255"));
        }

        [Fact]
        public void TestImplicitConstructorsWithLambdasSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 0
    {
        TestMain();
    }

    static void TestMain()                                      // Method 1
    {
        int y = s_c._function();
        D d = new D();
        int z = d._c._function();
        int zz = D.s_c._function();
    }

    public C(Func<int> f)                                       // Method 2
    {
        _function = f;
    }

    static C s_c = new C(() => 115);
    Func<int> _function;
}

class D
{
    public C _c = new C(() => 120);
    public static C s_c = new C(() => 144);
    public C _c1 = new C(() => 130);
    public static C s_c1 = new C(() => 156);
}

partial struct E
{
}

partial struct E
{
    public static C s_c = new C(() => 1444);
    public static C s_c1 = new C(() => { return 1567; });
}
";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 8, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 19, "TestMain()"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(10, 4, 16, 5, "static void TestMain()"),
                new SpanResult(12, 8, 12, 32, "int y = s_c._function()"),
                new SpanResult(13, 8, 13, 22, "D d = new D()"),
                new SpanResult(14, 8, 14, 33, "int z = d._c._function()"),
                new SpanResult(15, 8, 15, 35, "int zz = D.s_c._function()"));

            VerifySpans(reader, reader.Methods[2], sourceLines,
                new SpanResult(18, 4, 21, 5, "public C(Func<int> f)"),
                new SpanResult(20, 8, 20, 22, "_function = f"));

            VerifySpans(reader, reader.Methods[3], sourceLines,                     // Synthesized static constructor for C
                new SpanResult(23, 31, 23, 34, "115"),
                new SpanResult(23, 19, 23, 35, "new C(() => 115)"));

            VerifySpans(reader, reader.Methods[4], sourceLines,                     // Synthesized instance constructor for D
                new SpanResult(29, 30, 29, 33, "120"),
                new SpanResult(31, 31, 31, 34, "130"),
                new SpanResult(29, 18, 29, 34, "new C(() => 120)"),
                new SpanResult(31, 19, 31, 35, "new C(() => 130)"));

            VerifySpans(reader, reader.Methods[5], sourceLines,                     // Synthesized static constructor for D
                new SpanResult(30, 38, 30, 41, "144"),
                new SpanResult(32, 39, 32, 42, "156"),
                new SpanResult(30, 26, 30, 42, "new C(() => 144)"),
                new SpanResult(32, 27, 32, 43, "new C(() => 156"));

            VerifySpans(reader, reader.Methods[6], sourceLines,                     // Synthesized static constructor for E
                new SpanResult(41, 38, 41, 42, "1444"),
                new SpanResult(42, 41, 42, 53, "return 1567"),
                new SpanResult(41, 26, 41, 43, "new C(() => 1444)"),
                new SpanResult(42, 27, 42, 56, "new C(() => { return 1567; })"));
        }

        [Fact]
        public void TestLocalFunctionWithLambdaSpans()
        {
            string source = @"
using System;

public class C
{
    public static void Main()                                   // Method 0
    {
        TestMain();
    }

    static void TestMain()                                      // Method 1
    {
        new D().M1();
    }
}

public class D
{
    public void M1()                                            // Method 3
    {
        L1();
        void L1()
        {
            var f = new Func<int>(
                () => 1
            );

            f();

            var f1 = new Func<int>(
                () => { return 2; }
            );

            var f2 = new Func<int, int>(
                (x) => x + 3
            );

            var f3 = new Func<int, int>(
                x => x + 4
            );
        }
    }
}
";

            var c = CreateCompilation(Parse(source + InstrumentationHelperSource, @"C:\myproject\doc1.cs"), references: new[] { RefSafetyRulesAttributeLib });
            var peImage = c.EmitToArray(EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            string[] sourceLines = source.Split('\n');

            VerifySpans(reader, reader.Methods[0], sourceLines,
                new SpanResult(5, 4, 8, 5, "public static void Main()"),
                new SpanResult(7, 8, 7, 19, "TestMain()"));

            VerifySpans(reader, reader.Methods[1], sourceLines,
                new SpanResult(10, 4, 13, 5, "static void TestMain()"),
                new SpanResult(12, 8, 12, 21, "new D().M1()"));

            VerifySpans(reader, reader.Methods[3], sourceLines,
                new SpanResult(18, 4, 41, 5, "public void M1()"),
                new SpanResult(20, 8, 20, 13, "L1()"),
                new SpanResult(24, 22, 24, 23, "1"),
                new SpanResult(23, 12, 25, 14, "var f = new Func<int>"),
                new SpanResult(27, 12, 27, 16, "f()"),
                new SpanResult(30, 24, 30, 33, "return 2"),
                new SpanResult(29, 12, 31, 14, "var f1 = new Func<int>"),
                new SpanResult(34, 23, 34, 28, "x + 3"),
                new SpanResult(33, 12, 35, 14, "var f2 = new Func<int, int>"),
                new SpanResult(38, 21, 38, 26, "x + 4"),
                new SpanResult(37, 12, 39, 14, "var f3 = new Func<int, int>"));
        }

        [Fact]
        public void TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
        {
            var c = CreateCompilation(Parse(ExampleSource + InstrumentationHelperSource, @"C:\myproject\doc1.cs"));
            var peImage = c.EmitToArray(EmitOptions.Default);

            var peReader = new PEReader(peImage);
            var reader = DynamicAnalysisDataReader.TryCreateFromPE(peReader, "<DynamicAnalysisData>");

            Assert.Null(reader);
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void EmptyStaticConstructor_WithEnableTestCoverage()
        {
            string source = @"
#nullable enable
class C
{
    static C()
    {
    }

    static object obj = null!;
}" + InstrumentationHelperSource;
            var emitOptions = EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage));
            CompileAndVerify(source, emitOptions: emitOptions).VerifyIL("C..cctor()",
@"{
  // Code size       57 (0x39)
  .maxstack  5
  .locals init (bool[] V_0)
  IL_0000:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0005:  ldtoken    ""C..cctor()""
  IL_000a:  ldelem.ref
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  brtrue.s   IL_0034
  IL_000f:  ldsfld     ""System.Guid <PrivateImplementationDetails>.MVID""
  IL_0014:  ldtoken    ""C..cctor()""
  IL_0019:  ldtoken    Source Document 0
  IL_001e:  ldsfld     ""bool[][] <PrivateImplementationDetails>.PayloadRoot0""
  IL_0023:  ldtoken    ""C..cctor()""
  IL_0028:  ldelema    ""bool[]""
  IL_002d:  ldc.i4.1
  IL_002e:  call       ""bool[] Microsoft.CodeAnalysis.Runtime.Instrumentation.CreatePayload(System.Guid, int, int, ref bool[], int)""
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  ldc.i4.0
  IL_0036:  ldc.i4.1
  IL_0037:  stelem.i1
  IL_0038:  ret
}");
        }

        [WorkItem(42985, "https://github.com/dotnet/roslyn/issues/42985")]
        [Fact]
        public void SynthesizedStaticConstructor_WithEnableTestCoverage()
        {
            string source = @"
#nullable enable
class C
{
    static object obj = null!;
}" + InstrumentationHelperSource;
            var emitOptions = EmitOptions.Default.WithInstrumentationKinds(ImmutableArray.Create(InstrumentationKind.TestCoverage));
            CompileAndVerify(
                source,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All),
                emitOptions: emitOptions,
                symbolValidator: validator);

            void validator(ModuleSymbol module)
            {
                var type = module.ContainingAssembly.GetTypeByMetadataName("C");
                Assert.Empty(type.GetMembers(".cctor"));
            }
        }

        private class SpanResult
        {
            public int StartLine { get; }
            public int StartColumn { get; }
            public int EndLine { get; }
            public int EndColumn { get; }
            public string TextStart { get; }
            public SpanResult(int startLine, int startColumn, int endLine, int endColumn, string textStart)
            {
                StartLine = startLine;
                StartColumn = startColumn;
                EndLine = endLine;
                EndColumn = endColumn;
                TextStart = textStart;
            }
        }

        private static void VerifySpans(DynamicAnalysisDataReader reader, DynamicAnalysisMethod methodData, string[] sourceLines, params SpanResult[] expected)
        {
            ArrayBuilder<string> expectedSpanSpellings = ArrayBuilder<string>.GetInstance(expected.Length);
            foreach (SpanResult expectedSpanResult in expected)
            {
                var text = sourceLines[expectedSpanResult.StartLine].Substring(expectedSpanResult.StartColumn);
                Assert.True(text.StartsWith(expectedSpanResult.TextStart), $"Text doesn't start with {expectedSpanResult.TextStart}. Text is: {text}");

                expectedSpanSpellings.Add(string.Format("({0},{1})-({2},{3})", expectedSpanResult.StartLine, expectedSpanResult.StartColumn, expectedSpanResult.EndLine, expectedSpanResult.EndColumn));
            }

            VerifySpans(reader, methodData, expectedSpanSpellings.ToArrayAndFree());
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

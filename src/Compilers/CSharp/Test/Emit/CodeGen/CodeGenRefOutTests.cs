// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenRefOutTests : CSharpTestBase
    {
        [Fact]
        public void TestOutParamSignature()
        {
            var source = @"
class C
{
    void M(out int x)
    {
        x = 0;
    }
}";
            CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("C", "M", ".method private hidebysig instance System.Void M([out] System.Int32& x) cil managed")
            });
        }

        [Fact]
        public void TestRefParamSignature()
        {
            var source = @"
class C
{
    void M(ref int x)
    {
    }
}";
            CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("C", "M", ".method private hidebysig instance System.Void M(System.Int32& x) cil managed")
            });
        }

        [Fact]
        public void TestOneReferenceMultipleParameters()
        {
            var source = @"
class C
{
    static void Main()
    {
        int z = 0;
        Test(ref z, out z);
        System.Console.WriteLine(z);
    }

    static void Test(ref int x, out int y)
    {
        x = 1;
        y = 2;
    }
}";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void TestReferenceParameterOrder()
        {
            var source = @"
public class Test
{
    static int[] array = new int[1];

    public static void Main(string[] args)
    {
        // Named parameters are in reversed order
        // Arguments have side effects
        // Arguments refer to the same array element
        Foo(y: out GetArray(""A"")[GetIndex(""B"")], x: ref GetArray(""C"")[GetIndex(""D"")]);
        System.Console.WriteLine(array[0]);
    }

    static void Foo(ref int x, out int y)
    {
        x = 1;
        y = 2;
    }

    static int GetIndex(string msg)
    {
        System.Console.WriteLine(""Index {0}"", msg);
        return 0;
    }

    static int[] GetArray(string msg)
    {
        System.Console.WriteLine(""Array {0}"", msg);
        return array;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
Array A
Index B
Array C
Index D
2");
        }

        [Fact]
        public void TestPassMutableStructByReference()
        {
            var source = @"
class C
{
    static void Main()
    {
        MutableStruct s1 = new MutableStruct();
        s1.Dump();
        ByRef(ref s1, 2);
        s1.Dump();

        System.Console.WriteLine();

        MutableStruct s2 = new MutableStruct();
        s2.Dump();
        ByVal(s2, 2);
        s2.Dump();
    }

    static void ByRef(ref MutableStruct s, int depth)
    {
        if (depth <= 0)
        {
            s.Flag();
        }
        else
        {
            s.Dump();
            ByRef(ref s, depth - 1);
            s.Dump();
        }
    }

    static void ByVal(MutableStruct s, int depth)
    {
        if (depth <= 0)
        {
            s.Flag();
        }
        else
        {
            s.Dump();
            ByVal(s, depth - 1);
            s.Dump();
        }
    }
}

struct MutableStruct
{
    private bool flagged;

    public void Flag()
    {
        this.flagged = true;
    }

    public void Dump()
    {
        System.Console.WriteLine(flagged ? ""Flagged"" : ""Unflagged"");
    }
}";
            CompileAndVerify(source, expectedOutput: @"
Unflagged
Unflagged
Unflagged
Flagged
Flagged
Flagged

Unflagged
Unflagged
Unflagged
Unflagged
Unflagged
Unflagged");
        }

        [Fact]
        public void TestPassFieldByReference()
        {
            var source = @"
class C
{
    int field;
    int[] arrayField = new int[1];

    static int staticField;
    static int[] staticArrayField = new int[1];

    static void Main()
    {
        C c = new C();

        System.Console.WriteLine(c.field);
        TestRef(ref c.field);
        System.Console.WriteLine(c.field);

        System.Console.WriteLine(c.arrayField[0]);
        TestRef(ref c.arrayField[0]);
        System.Console.WriteLine(c.arrayField[0]);

        System.Console.WriteLine(C.staticField);
        TestRef(ref C.staticField);
        System.Console.WriteLine(C.staticField);

        System.Console.WriteLine(C.staticArrayField[0]);
        TestRef(ref C.staticArrayField[0]);
        System.Console.WriteLine(C.staticArrayField[0]);
    }

    static void TestRef(ref int x)
    {
        x++;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
0
1
0
1
0
1
0
1");
        }

        [Fact]
        public void TestSetFieldViaOutParameter()
        {
            var source = @"
class C
{
    int field;
    int[] arrayField = new int[1];

    static int staticField;
    static int[] staticArrayField = new int[1];

    static void Main()
    {
        C c = new C();

        System.Console.WriteLine(c.field);
        TestOut(out c.field);
        System.Console.WriteLine(c.field);

        System.Console.WriteLine(c.arrayField[0]);
        TestOut(out c.arrayField[0]);
        System.Console.WriteLine(c.arrayField[0]);

        System.Console.WriteLine(C.staticField);
        TestOut(out C.staticField);
        System.Console.WriteLine(C.staticField);

        System.Console.WriteLine(C.staticArrayField[0]);
        TestOut(out C.staticArrayField[0]);
        System.Console.WriteLine(C.staticArrayField[0]);
    }

    static void TestOut(out int x)
    {
        x = 1;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
0
1
0
1
0
1
0
1");
        }

        [WorkItem(543521, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543521")]
        [Fact()]
        public void TestConstructorWithOutParameter()
        {
            CompileAndVerify(@"
class Class1
{
	Class1(out bool outParam)
	{
		outParam = true;
	}
	static void Main()
	{
		var b = false;
		var c1 = new Class1(out b);
	}
}");
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenTypeOfTests : CSharpTestBase
    {
        [Fact]
        public void TestTypeofSimple()
        {
            var source = @"
class C
{
    static void Main()
    {
        System.Console.WriteLine(typeof(C));
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "C");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldtoken    ""C""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ret       
}");
        }

        [Fact]
        public void TestTypeofNonGeneric()
        {
            var source = @"
namespace Source
{
    class Class { }
    struct Struct { }
    enum Enum { e }
    interface Interface { }
    class StaticClass { }
}

class Program
{
    static void Main()
    {
        // From source
        System.Console.WriteLine(typeof(Source.Class));
        System.Console.WriteLine(typeof(Source.Struct));
        System.Console.WriteLine(typeof(Source.Enum));
        System.Console.WriteLine(typeof(Source.Interface));
        System.Console.WriteLine(typeof(Source.StaticClass));

        // From metadata
        System.Console.WriteLine(typeof(string));
        System.Console.WriteLine(typeof(int));
        System.Console.WriteLine(typeof(System.IO.FileMode));
        System.Console.WriteLine(typeof(System.IFormattable));
        System.Console.WriteLine(typeof(System.Math));

        // Special
        System.Console.WriteLine(typeof(void));
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Source.Class
Source.Struct
Source.Enum
Source.Interface
Source.StaticClass
System.String
System.Int32
System.IO.FileMode
System.IFormattable
System.Math
System.Void");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Main", @"{
  // Code size      166 (0xa6)
  .maxstack  1
  IL_0000:  ldtoken    ""Source.Class""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Source.Struct""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ldtoken    ""Source.Enum""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldtoken    ""Source.Interface""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  call       ""void System.Console.WriteLine(object)""
  IL_003c:  ldtoken    ""Source.StaticClass""
  IL_0041:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0046:  call       ""void System.Console.WriteLine(object)""
  IL_004b:  ldtoken    ""string""
  IL_0050:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0055:  call       ""void System.Console.WriteLine(object)""
  IL_005a:  ldtoken    ""int""
  IL_005f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0064:  call       ""void System.Console.WriteLine(object)""
  IL_0069:  ldtoken    ""System.IO.FileMode""
  IL_006e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0073:  call       ""void System.Console.WriteLine(object)""
  IL_0078:  ldtoken    ""System.IFormattable""
  IL_007d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0082:  call       ""void System.Console.WriteLine(object)""
  IL_0087:  ldtoken    ""System.Math""
  IL_008c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0091:  call       ""void System.Console.WriteLine(object)""
  IL_0096:  ldtoken    ""void""
  IL_009b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_00a0:  call       ""void System.Console.WriteLine(object)""
  IL_00a5:  ret       
}");
        }

        [Fact]
        public void TestTypeofGeneric()
        {
            var source = @"
class Class1<T> { }
class Class2<T, U> { }

class Program
{
    static void Main()
    {
        System.Console.WriteLine(typeof(Class1<int>));
        System.Console.WriteLine(typeof(Class1<Class1<int>>));

        System.Console.WriteLine(typeof(Class2<int, long>));
        System.Console.WriteLine(typeof(Class2<Class1<int>, Class1<long>>));
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Class1`1[System.Int32]
Class1`1[Class1`1[System.Int32]]
Class2`2[System.Int32,System.Int64]
Class2`2[Class1`1[System.Int32],Class1`1[System.Int64]]");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Main", @"{
  // Code size       61 (0x3d)
  .maxstack  1
  IL_0000:  ldtoken    ""Class1<int>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Class1<Class1<int>>""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ldtoken    ""Class2<int, long>""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldtoken    ""Class2<Class1<int>, Class1<long>>""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  call       ""void System.Console.WriteLine(object)""
  IL_003c:  ret       
}");
        }

        [Fact]
        public void TestTypeofTypeParameter()
        {
            var source = @"
class Class<T>
{
    public static void Print()
    {
        System.Console.WriteLine(typeof(T));
        System.Console.WriteLine(typeof(Class<T>));
    }

    public static void Print<U>()
    {
        System.Console.WriteLine(typeof(U));
        System.Console.WriteLine(typeof(Class<U>));
    }
}

class Program
{
    static void Main()
    {
        Class<int>.Print();
        Class<Class<int>>.Print();

        Class<int>.Print<long>();
        Class<Class<int>>.Print<Class<long>>();
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
System.Int32
Class`1[System.Int32]
Class`1[System.Int32]
Class`1[Class`1[System.Int32]]
System.Int64
Class`1[System.Int64]
Class`1[System.Int64]
Class`1[Class`1[System.Int64]]");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Class<T>.Print", @"{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    ""T""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Class<T>""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret       
}");

            comp.VerifyIL("Class<T>.Print<U>", @"{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    ""U""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Class<U>""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret       
}");
        }

        [Fact]
        public void TestTypeofUnboundGeneric()
        {
            var source = @"
class Class1<T> { }
class Class2<T, U> { }

class Program
{
    static void Main()
    {
        System.Console.WriteLine(typeof(Class1<>));
        System.Console.WriteLine(typeof(Class2<,>));
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Class1`1[T]
Class2`2[T,U]");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Main", @"{
  // Code size       31 (0x1f)
  .maxstack  1
  IL_0000:  ldtoken    ""Class1<T>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Class2<T, U>""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret       
}");
        }

        [WorkItem(542581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542581")]
        [Fact]
        public void TestTypeofInheritedNestedTypeThroughUnboundGeneric()
        {
            var source = @"
using System;

class D<T> : C, I1, I2<T, int> { }

interface I1 { }

interface I2<T, U> { }

class F<T> { public class E {} }

class G<T> : F<T>, I1, I2<T, int> { }

class H<T, U> { public class E {} }

class K<T> : H<T, int>{ }

class C
{
    public class E { }

    static void Main()
    {
        Console.WriteLine(typeof(D<>.E));
        Console.WriteLine(typeof(D<>).BaseType);
        var interfaces = typeof(D<>).GetInterfaces();
        Console.WriteLine(interfaces[0]);
        Console.WriteLine(interfaces[1]);

        Console.WriteLine(typeof(G<>.E));
        Console.WriteLine(typeof(G<>).BaseType);
        interfaces = typeof(G<>).GetInterfaces();
        Console.WriteLine(interfaces[0]);
        Console.WriteLine(interfaces[1]);

        Console.WriteLine(typeof(K<>.E));
        Console.WriteLine(typeof(K<>).BaseType);
    }
}";
            var expected = @"C+E
C
I1
I2`2[T,System.Int32]
F`1+E[T]
F`1[T]
I1
I2`2[T,System.Int32]
H`2+E[T,U]
H`2[T,System.Int32]";

            var comp = CompileAndVerify(source, expectedOutput: expected);

            comp.VerifyDiagnostics();
        }

        [WorkItem(542581, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542581")]
        [Fact]
        public void TestTypeofInheritedNestedTypeThroughUnboundGeneric_Attribute()
        {
            var source = @"
using System;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class TestAttribute : Attribute { public TestAttribute(System.Type type){} }

class D<T> : C, I1, I2<T, int> { }

interface I1 { }

interface I2<T, U> { }

class F<T> { public class E {} }

class G<T> : F<T>, I1, I2<T, int> { }

class H<T, U> { public class E {} }

class K<T> : H<T, int>{ }

[TestAttribute(typeof(D<>))]
[TestAttribute(typeof(D<>.E))]
[TestAttribute(typeof(G<>))]
[TestAttribute(typeof(G<>.E))]
[TestAttribute(typeof(K<>))]
[TestAttribute(typeof(K<>.E))]
class C
{
    public class E { }

    static void Main()
    {
        typeof(C).GetCustomAttributes(false);
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void TestTypeofArray()
        {
            var source = @"
class Class1<T> { }

class Program
{
    static void Print<U>()
    {
        System.Console.WriteLine(typeof(int[]));
        System.Console.WriteLine(typeof(int[,]));
        System.Console.WriteLine(typeof(int[][]));
        System.Console.WriteLine(typeof(U[]));
        System.Console.WriteLine(typeof(Class1<U>[]));
        System.Console.WriteLine(typeof(Class1<int>[]));
    }

    static void Main()
    {
        Print<long>();
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
System.Int32[]
System.Int32[,]
System.Int32[][]
System.Int64[]
Class1`1[System.Int64][]
Class1`1[System.Int32][]");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Program.Print<U>", @"{
  // Code size       91 (0x5b)
  .maxstack  1
  IL_0000:  ldtoken    ""int[]""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""int[,]""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ldtoken    ""int[][]""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldtoken    ""U[]""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  call       ""void System.Console.WriteLine(object)""
  IL_003c:  ldtoken    ""Class1<U>[]""
  IL_0041:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0046:  call       ""void System.Console.WriteLine(object)""
  IL_004b:  ldtoken    ""Class1<int>[]""
  IL_0050:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0055:  call       ""void System.Console.WriteLine(object)""
  IL_005a:  ret       
}");
        }

        [Fact]
        public void TestTypeofNested()
        {
            var source = @"
class Outer<T>
{
    public static void Print()
    {
        System.Console.WriteLine(typeof(Inner<>));
        System.Console.WriteLine(typeof(Inner<T>));
        System.Console.WriteLine(typeof(Inner<int>));

        System.Console.WriteLine(typeof(Outer<>.Inner<>));
//        System.Console.WriteLine(typeof(Outer<>.Inner<T>)); //CS7003
//        System.Console.WriteLine(typeof(Outer<>.Inner<int>)); //CS7003

//        System.Console.WriteLine(typeof(Outer<T>.Inner<>)); //CS7003
        System.Console.WriteLine(typeof(Outer<T>.Inner<T>));
        System.Console.WriteLine(typeof(Outer<T>.Inner<int>));

//        System.Console.WriteLine(typeof(Outer<int>.Inner<>)); //CS7003
        System.Console.WriteLine(typeof(Outer<int>.Inner<T>));
        System.Console.WriteLine(typeof(Outer<int>.Inner<int>));
    }

    class Inner<U> { }
}

class Program
{
    static void Main()
    {
        Outer<long>.Print();
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int64,System.Int64]
Outer`1+Inner`1[System.Int64,System.Int32]
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int64,System.Int64]
Outer`1+Inner`1[System.Int64,System.Int32]
Outer`1+Inner`1[System.Int32,System.Int64]
Outer`1+Inner`1[System.Int32,System.Int32]");

            comp.VerifyDiagnostics();

            comp.VerifyIL("Outer<T>.Print", @"{
  // Code size      121 (0x79)
  .maxstack  1
  IL_0000:  ldtoken    ""Outer<T>.Inner<U>""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ldtoken    ""Outer<T>.Inner<T>""
  IL_0014:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ldtoken    ""Outer<T>.Inner<int>""
  IL_0023:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0028:  call       ""void System.Console.WriteLine(object)""
  IL_002d:  ldtoken    ""Outer<T>.Inner<U>""
  IL_0032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0037:  call       ""void System.Console.WriteLine(object)""
  IL_003c:  ldtoken    ""Outer<T>.Inner<T>""
  IL_0041:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0046:  call       ""void System.Console.WriteLine(object)""
  IL_004b:  ldtoken    ""Outer<T>.Inner<int>""
  IL_0050:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0055:  call       ""void System.Console.WriteLine(object)""
  IL_005a:  ldtoken    ""Outer<int>.Inner<T>""
  IL_005f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0064:  call       ""void System.Console.WriteLine(object)""
  IL_0069:  ldtoken    ""Outer<int>.Inner<int>""
  IL_006e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0073:  call       ""void System.Console.WriteLine(object)""
  IL_0078:  ret       
}");
        }

        [Fact]
        public void TestTypeofInLambda()
        {
            var source = @"
using System;

public class Outer<T>
{
    public class Inner<U>
    {
        public Action Method<V>(V v)
        {
            return () =>
            {
                Console.WriteLine(v);

                Console.WriteLine(typeof(T));
                Console.WriteLine(typeof(U));
                Console.WriteLine(typeof(V));

                Console.WriteLine(typeof(Outer<>));
                Console.WriteLine(typeof(Outer<T>));
                Console.WriteLine(typeof(Outer<U>));
                Console.WriteLine(typeof(Outer<V>));

                Console.WriteLine(typeof(Inner<>));
                Console.WriteLine(typeof(Inner<T>));
                Console.WriteLine(typeof(Inner<U>));
                Console.WriteLine(typeof(Inner<V>));
            };
        }
    }
}

class Program
{
    static void Main()
    {
        Action a = new Outer<int>.Inner<char>().Method<byte>(1);
        a();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
1
System.Int32
System.Char
System.Byte
Outer`1[T]
Outer`1[System.Int32]
Outer`1[System.Char]
Outer`1[System.Byte]
Outer`1+Inner`1[T,U]
Outer`1+Inner`1[System.Int32,System.Int32]
Outer`1+Inner`1[System.Int32,System.Char]
Outer`1+Inner`1[System.Int32,System.Byte]");

            comp.VerifyDiagnostics();
        }

        [WorkItem(541600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541600")]
        [Fact]
        public void TestTypeOfAlias4TypeMemberOfGeneric()
        {
            var source = @"
using System;
using MyTestClass = TestClass<string>;

public class TestClass<T>
{
    public enum TestEnum
    {
        First = 0,
    }
}

public class mem178
{
    public static int Main()
    {
        Console.Write(typeof(MyTestClass.TestEnum));
        return 0;
    }
}
";
            CompileAndVerify(source, expectedOutput: @"TestClass`1+TestEnum[System.String]");
        }

        [WorkItem(541618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541618")]
        [Fact]
        public void TestTypeOfAlias5TypeMemberOfGeneric()
        {
            var source = @"
using OuterOfString = Outer<string>;
using OuterOfInt = Outer<int>;
public class Outer<T>
{
    public class Inner<U> { }
}
public class Program
{
    public static void Main()
    {
        System.Console.WriteLine(typeof(OuterOfString.Inner<>) == typeof(OuterOfInt.Inner<>));
    }
}
";
            // NOTE: this is the Dev10 output.  Change to false if we decide to take a breaking change.
            CompileAndVerify(source, expectedOutput: @"True");
        }
    }
}

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenConversionTests : CSharpTestBase
    {
        [Fact]
        public void ExplicitConversionRuntimeGrabbag()
        {
            var source = @"
using System;
enum E1 { e1, e2 }
enum E2 { e3, e4 }
interface I { }
interface J { }
class C : I, J { }

public class Program
{
    public static void Assert(bool condition)
    {
        if (!condition) throw new Exception();
    }

    public static void Main(string[] args)
    {
        // explicit numeric conversions 6.2.1
        int i = 12;
        byte b = (byte)i;
        Assert(b == 12);
        double d = 12.0;
        float f = (float)d;
        Assert(f == 12.0F);

        // explicit enumeration conversions 6.2.2
        b = 1;
        E1 e1 = (E1)b;
        Assert(e1 == E1.e2);
        E2 e2 = (E2)e1;
        Assert(e2 == E2.e4);

        // explicit nullable conversions 6.2.3
        int? ni = 0;
        byte? nb = (byte?)ni;
        Assert(nb == 0);
        ni = null;
        nb = (byte?)ni;
        Assert(!nb.HasValue);

        // explicit reference conversions 6.2.4
        I[] ia = new C[20];
        J[] ja = (J[])ia;

        // unboxing conversions 6.2.5
        i = 12;
        object o = i;
        i = (int)o;
        Assert(i == 12);
        e1 = E1.e1;
        Enum e = e1;
        e1 = (E1)e;
        Assert(e1 == E1.e1);

        // explicit dynamic conversions 6.2.6
        // explicit conversions involving type parameters 6.2.7
        // user defined explicit conversions 6.2.8
    }
}
";
            var compilationVerifier = CompileAndVerify(source, expectedOutput: @"");
        }

        [Fact]
        public void InaccessibleConversion()
        {
            var source = @"
using System;

interface J<T> { }
interface I<T> : J<object> { }
	
class A
{
    private class B { }
    public class C : I<B> { }
}

class Program
{
    static void Main()
    {
        Foo(new A.C());
    }

    static void Foo<T>(I<T> x)
    {
        Console.WriteLine(""Foo<T>(I<T> x)"");
    }
    static void Foo<T>(J<T> x)
    {
        Console.WriteLine(""Foo<T>(J<T> x)"");
    }
}
";
            var compilationVerifier = CompileAndVerify(source, expectedOutput: @"Foo<T>(J<T> x)
");
        }


        [Fact]
        public void BadCodeCast()
        {
            var csSource = @"using System;
 
class G<K>
{
    K k;
    public G(K k)
    {
        this.k = k;
    }
    public void M()
    {
        N((IComparable)k);
    }
    void N(IComparable ic)
    {
        Console.WriteLine(ic);
    }
}
 
class Program
{
    public static void Main(string[] args)
    {
        G<string> g1 = new G<string>(""hello"");
        g1.M();
    }
}";
            CompileAndVerify(csSource, expectedOutput: "hello");
        }

        [WorkItem(544427, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544427")]
        [Fact]
        public void WrongOrderConversion()
        {
            var text =
@"using System;

public class Test
{
    public static explicit operator int(Test x)
    {
        return 1;
    }
    static long? M(Test t)
    {
        return (long?) t;
    }
    static void Main()
    {
    }
}";
            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Test.M",
@"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int Test.op_Explicit(Test)""
  IL_0006:  conv.i8
  IL_0007:  newobj     ""long?..ctor(long)""
  IL_000c:  ret
}");
        }

        [WorkItem(602009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602009")]
        [Fact]
        public void DefaultParameterValue_DateTimeConstant()
        {
            var source1 = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Test
{
    public static void Generic<T>([Optional][DateTimeConstant(634953547672667479L)] T x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void DateTime([Optional][DateTimeConstant(634953547672667479L)] DateTime x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void NullableDateTime([Optional][DateTimeConstant(634953547672667479L)] DateTime? x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Object([Optional][DateTimeConstant(634953547672667479L)] object x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void String([Optional][DateTimeConstant(634953547672667479L)] string x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Int32([Optional][DateTimeConstant(634953547672667479L)] int x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void IComparable([Optional][DateTimeConstant(634953547672667479L)] IComparable x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void ValueType([Optional][DateTimeConstant(634953547672667479L)] ValueType x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }
}
";

            var source2 = @"
class Program
{
    public static void Main()
    {
        // To avoid failures if the test ran on different culture
        System.Threading.Thread.CurrentThread.CurrentCulture = 
                System.Globalization.CultureInfo.InvariantCulture;
        // Respects default value
        Test.Generic<System.DateTime>();    
        Test.Generic<System.DateTime?>();   
        Test.Generic<object>();             
        Test.DateTime();                    
        Test.NullableDateTime();            
        Test.Object();                      
        Test.IComparable();                 
        Test.ValueType();                   

        // Null, since not convertible
        Test.Generic<string>();             
        Test.String();                      
        Test.Int32();                       
    }
}
";

            var expectedOutput = @"
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
02/01/2013 22:32:47
null
null
0
";

            // When the method with the attribute is in source.
            var verifier1 = CompileAndVerify(source1 + source2, expectedOutput: expectedOutput);

            // When the method with the attribute is from metadata.
            var comp2 = CreateCompilationWithMscorlib(source2, new[] { MetadataReference.CreateFromImage(verifier1.EmittedAssemblyData) }, TestOptions.ReleaseExe);
            CompileAndVerify(comp2, expectedOutput: expectedOutput);
        }

        [WorkItem(602009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/602009")]
        [Fact]
        public void DefaultParameterValue_DecimalConstant()
        {
            var source1 = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Test
{
    public static void Generic<T>([Optional][DecimalConstant(0, 0, 0, 0, 50)] T x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Decimal([Optional][DecimalConstant(0, 0, 0, 0, 50)] Decimal x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void NullableDecimal([Optional][DecimalConstant(0, 0, 0, 0, 50)] Decimal? x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Object([Optional][DecimalConstant(0, 0, 0, 0, 50)] object x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void String([Optional][DecimalConstant(0, 0, 0, 0, 50)] string x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void Int32([Optional][DecimalConstant(0, 0, 0, 0, 50)] int x)
    {
        Console.WriteLine(x.ToString());
    }

    public static void IComparable([Optional][DecimalConstant(0, 0, 0, 0, 50)] IComparable x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }

    public static void ValueType([Optional][DecimalConstant(0, 0, 0, 0, 50)] ValueType x)
    {
        Console.WriteLine(x == null ? ""null"" : x.ToString());
    }
}
";

            var source2 = @"
class Program
{
    public static void Main()
    {
        // Respects default value
        Test.Generic<decimal>();    
        Test.Generic<decimal?>();   
        Test.Generic<object>();             
        Test.Decimal();                    
        Test.NullableDecimal();            
        Test.Object();                      
        Test.IComparable();                 
        Test.ValueType();                   
        Test.Int32();                       

        // Null, since not convertible
        Test.Generic<string>();             
        Test.String();                      
    }
}
";

            var expectedOutput = @"
50
50
50
50
50
50
50
50
50
null
null
";

            // When the method with the attribute is in source.
            var verifier1 = CompileAndVerify(source1 + source2, expectedOutput: expectedOutput);

            // When the method with the attribute is from metadata.
            var comp2 = CreateCompilationWithMscorlib(source2, new[] { MetadataReference.CreateFromImage(verifier1.EmittedAssemblyData) }, TestOptions.ReleaseExe);
            CompileAndVerify(comp2, expectedOutput: expectedOutput);
        }

        [WorkItem(659424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/659424")]
        [Fact]
        public void FloatConversion001()
        {
            var text =
@"using System;

public class Program
{
        static float Test(decimal d)
        {
            return (float)d;
        }

    static void Main()
    {
    }
}";
            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.Test(decimal)",
@"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""float decimal.op_Explicit(decimal)""
  IL_0006:  conv.r4
  IL_0007:  ret
}");
        }

        [WorkItem(659424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/659424")]
        [Fact]
        public void FloatConversion002()
        {
            var text =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(Test1(float.MaxValue));
        System.Console.WriteLine(Test2(float.MaxValue));
    }

    static float Test1(float arg)
    {
        var temp = Mul2(arg);
        return temp / 2;
    }

    static float Test2(float arg)
    {
        var temp = (float)Mul2(arg); // conv.r4 here. We want result of Mul2 to have float precision.
        return temp / 2;
    }

    private static float Mul2(float arg)
    {
        return arg * 2;
    }
}
";
            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.Test2(float)",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""float Program.Mul2(float)""
  IL_0006:  conv.r4
  IL_0007:  ldc.r4     2
  IL_000c:  div
  IL_000d:  ret
}");
        }

        [WorkItem(659424, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/659424")]
        [Fact]
        public void FloatConversion003()
        {
            var text =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(Test1(float.MaxValue));
        System.Console.WriteLine(Test2(float.MaxValue));
    }

    static float Test1(float arg)
    {
        var temp = Mul2(arg);
        return temp / 2;
    }

    static float Test2(float arg)
    {
        var temp = (float)(float?)Mul2(arg); // conv.r4 here. We want result of Mul2 to have float precision.
        return temp / 2;
    }

    private static float Mul2(float arg)
    {
        return arg * 2;
    }
}
";
            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.Test2(float)",
@"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""float Program.Mul2(float)""
  IL_0006:  conv.r4
  IL_0007:  ldc.r4     2
  IL_000c:  div
  IL_000d:  ret
}");
        }

        [WorkItem(448900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/448900")]
        [Fact]
        public void Regress448900()
        {
            var text =
@"

using System;

class Class1
{
    static void Main()
    {
        int? a = 1;
        a.ToString();
        MyClass b = a;
        b.ToString();
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        Console.WriteLine(""Value is: "" + Value);
        return new MyClass();
    }
}
";
            var compilation = CompileAndVerify(text, expectedOutput: "Value is: 1");
            compilation.VerifyIL("Class1.Main()",
@"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (int? V_0, //a
  int? V_1)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.1
  IL_0003:  call       ""int?..ctor(int)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  constrained. ""int?""
  IL_0010:  callvirt   ""string object.ToString()""
  IL_0015:  pop
  IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  ldloca.s   V_1
  IL_001a:  call       ""bool int?.HasValue.get""
  IL_001f:  brtrue.s   IL_0024
  IL_0021:  ldnull
  IL_0022:  br.s       IL_0035
  IL_0024:  ldloca.s   V_1
  IL_0026:  call       ""int int?.GetValueOrDefault()""
  IL_002b:  call       ""decimal decimal.op_Implicit(int)""
  IL_0030:  call       ""MyClass MyClass.op_Implicit(decimal)""
  IL_0035:  callvirt   ""string object.ToString()""
  IL_003a:  pop
  IL_003b:  ret
}");
        }

        [WorkItem(448900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/448900")]
        [Fact]
        public void Regress448900_Optimized()
        {
            var text =
@"

using System;

class Class1
{
    static void Main()
    {
        int? a = 1;
        MyClass b = a;
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        Console.WriteLine(""Value is: "" + Value);
        return new MyClass();
    }
}
";
            var compilation = CompileAndVerify(text, expectedOutput: "Value is: 1");
            compilation.VerifyIL("Class1.Main()",
@"
{
  // Code size       35 (0x23)
  .maxstack  1
  .locals init (int? V_0)
  IL_0000:  ldc.i4.1
  IL_0001:  newobj     ""int?..ctor(int)""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool int?.HasValue.get""
  IL_000e:  brfalse.s  IL_0022
  IL_0010:  ldloca.s   V_0
  IL_0012:  call       ""int int?.GetValueOrDefault()""
  IL_0017:  call       ""decimal decimal.op_Implicit(int)""
  IL_001c:  call       ""MyClass MyClass.op_Implicit(decimal)""
  IL_0021:  pop
  IL_0022:  ret
}");
        }

        [WorkItem(448900, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/448900")]
        [Fact]
        public void Regress448900_Folded()
        {
            var text =
@"

using System;

class Class1
{
    static void Main()
    {
        MyClass b = (int?)1;
    }
}

class MyClass
{
    public static implicit operator MyClass(decimal Value)
    {
        Console.WriteLine(""Value is: "" + Value);
        return new MyClass();
    }
}
";
            var compilation = CompileAndVerify(text, expectedOutput: "Value is: 1");
            compilation.VerifyIL("Class1.Main()",
@"
{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""decimal decimal.op_Implicit(int)""
  IL_0006:  call       ""MyClass MyClass.op_Implicit(decimal)""
  IL_000b:  pop
  IL_000c:  ret
}");
        }

        [WorkItem(674803, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674803")]
        [Fact]
        public void CastFrom0ToExplicitConversionViaEnum01()
        {
            var text =
@"enum E { a, b, c }
class C
{
    public static explicit operator C(E x) { return null; }
    public static explicit operator C(ulong x) { return null; }
    public static void Main()
    {
        C x = (C)0;
    }
}";
            // The native compiler does not consider an encompassing conversion from
            // a constant zero to an enum type to exist.  We reproduce that bug in
            // Roslyn for compatibility.
            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [WorkItem(844635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844635")]
        [Fact]
        public void RuntimeTypeCheckForGenericEnum()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        Foo(new G<int>.E(), new G<int>.E());
    }

    static void Foo<T>(G<T>.E x, G<int>.E y)
    {
        Console.Write(x is G<int>.E);
        Console.Write(y is G<T>.E);
    }
}

class G<T>
{
    public enum E { }
}
";

            var compilation = CompileAndVerify(source, expectedOutput: "TrueTrue");
            compilation.VerifyIL("Program.Foo<T>(G<T>.E, G<int>.E)",
@"
{
  // Code size       39 (0x27)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  box        ""G<T>.E""
  IL_0006:  isinst     ""G<int>.E""
  IL_000b:  ldnull
  IL_000c:  cgt.un
  IL_000e:  call       ""void System.Console.Write(bool)""
  IL_0013:  ldarg.1
  IL_0014:  box        ""G<int>.E""
  IL_0019:  isinst     ""G<T>.E""
  IL_001e:  ldnull
  IL_001f:  cgt.un
  IL_0021:  call       ""void System.Console.Write(bool)""
  IL_0026:  ret
}");
        }

        [WorkItem(864605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864605")]
        [WorkItem(864740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864740")]
        [Fact]
        public void MethodGroupIsExpression()
        {
            string source = @"
using System;
 
class Program
{
    static void Main()
    {
        var x = ICloneable.Clone is object;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyEmitDiagnostics(
                // (8,17): error CS0837: The first operand of an 'is' or 'as' operator may not be a lambda expression, anonymous method, or method group.
                //         var x = ICloneable.Clone is object;
                Diagnostic(ErrorCode.ERR_LambdaInIsAs, "ICloneable.Clone is object").WithLocation(8, 17));
        }

        [Fact]
        [WorkItem(1084278, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084278")]
        public void NullableConversionFromConst()
        {
            var source =
@"

using System;

class C
{
    static void Main()
    {
        Use((int?)3.5f);
        Use((int?)3.5d);
    }

    static void Use(int? p) { } 
}
";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Main()",
@"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldc.i4.3
  IL_0001:  newobj     ""int?..ctor(int)""
  IL_0006:  call       ""void C.Use(int?)""
  IL_000b:  ldc.i4.3
  IL_000c:  newobj     ""int?..ctor(int)""
  IL_0011:  call       ""void C.Use(int?)""
  IL_0016:  ret
}");
        }

        [Fact]
        public void LiftedFromIntPtrConversions()
        {
            var source =
@"
using System;

class C
{
    static void Main(string[] args)
    {
        Console.WriteLine((int?)M(null));
        Console.WriteLine((int?)M((IntPtr)42));
    }

    static IntPtr? M(IntPtr? p)
    {
        return p;
    }
}
";

            CompileAndVerify(source, expectedOutput:
@"
42");
        }

        [Fact]
        public void LiftedToIntPtrConversions()
        {
            var source =
@"
using System;

class C
{
    static void Main()
    {
        Console.WriteLine((IntPtr?)M_int());
        Console.WriteLine((IntPtr?)M_int(42));
        Console.WriteLine((IntPtr?)M_long());
        Console.WriteLine((IntPtr?)M_long(300));
    }

    static int? M_int(int? p = null) { return p; } 
    static long? M_long(long? p = null) { return p; } 
}
";

            CompileAndVerify(source, expectedOutput:
@"
42

300
");
        }

        [Fact]
        public void LiftedToIntPtrConversionsOptimized()
        {
            var source =
@"
using System;

class C
{
    static void Main()
    {
        Use((IntPtr?)(int?)(null));
        Use((IntPtr?)(int?)(42));
    }

    static void Use(IntPtr? p) { } 
}
";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Main()", @"
{
  // Code size       32 (0x20)
  .maxstack  1
  .locals init (System.IntPtr? V_0)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.IntPtr?""
  IL_0008:  ldloc.0
  IL_0009:  call       ""void C.Use(System.IntPtr?)""
  IL_000e:  ldc.i4.s   42
  IL_0010:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0015:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_001a:  call       ""void C.Use(System.IntPtr?)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void NullableNumericToIntPtr()
        {
            var source =
@"
using System;

class C
{
    static void Test()
    {
        byte? b = 0;
        IntPtr p = (IntPtr)b;
        Console.WriteLine(p);
    }
}";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Test()", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (byte? V_0) //b
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""byte?..ctor(byte)""
  IL_0008:  ldloca.s   V_0
  IL_000a:  call       ""byte byte?.Value.get""
  IL_000f:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0014:  box        ""System.IntPtr""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void NumericToNullableIntPtr()
        {
            var source =
@"
using System;

class C
{
    static void Test()
    {
        byte b = 0;
        IntPtr? p = (IntPtr?)b;
        Console.WriteLine(p);
    }
}";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Test()", @"
{
  // Code size       24 (0x18)
  .maxstack  1
  .locals init (byte V_0) //b
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0008:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_000d:  box        ""System.IntPtr?""
  IL_0012:  call       ""void System.Console.WriteLine(object)""
  IL_0017:  ret
}");
        }

        [Fact]
        [WorkItem(1210529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1210529")]
        public void LiftedIntToIntPtr()
        {
            var source =
@"
using System;

class C
{
    static void Main()
    {
        Use((IntPtr?)M());
    }

    static int? M() { return 0; } 
    static void Use(IntPtr? p) { } 
}
";

            var compilation = CompileAndVerify(source);
            compilation.VerifyIL("C.Main()",
@"
{
  // Code size       49 (0x31)
  .maxstack  1
  .locals init (int? V_0,
                System.IntPtr? V_1)
  IL_0000:  call       ""int? C.M()""
  IL_0005:  stloc.0
  IL_0006:  ldloca.s   V_0
  IL_0008:  call       ""bool int?.HasValue.get""
  IL_000d:  brtrue.s   IL_001a
  IL_000f:  ldloca.s   V_1
  IL_0011:  initobj    ""System.IntPtr?""
  IL_0017:  ldloc.1
  IL_0018:  br.s       IL_002b
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       ""int int?.GetValueOrDefault()""
  IL_0021:  call       ""System.IntPtr System.IntPtr.op_Explicit(int)""
  IL_0026:  newobj     ""System.IntPtr?..ctor(System.IntPtr)""
  IL_002b:  call       ""void C.Use(System.IntPtr?)""
  IL_0030:  ret
}");

        }
    }
}

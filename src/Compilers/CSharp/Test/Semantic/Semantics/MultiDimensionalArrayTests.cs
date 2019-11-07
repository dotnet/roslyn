// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class MultiDimensionalArrayTests : SemanticModelTestBase
    {
        [Fact]
        public void TestMultiDimensionalArray()
        {
            // Verify that operations on multidimensional arrays work and cause side effects only once.

            string source = @"
using System;
class P
{
  static int M(ref int x)
  {
    int y = x;
    x = 456;
    return y;
  }
  
  static int counter;
  static int[,] A(int[,] a)
  {
    Console.Write('A');
    Console.Write(counter);
    ++counter;
    return a;
  }
  static int B(int b)
  {
    Console.Write('B');
    Console.Write(counter);
    ++counter;
    return b;
  }
  static int C(int c)
  {
    Console.Write('C');
    Console.Write(counter);
    ++counter;
    return c;
  }


  static void Main()
  {
    int x = 4;
    int y = 5;
    int[,] a = new int[x,y];
    V((A(a)[B(3),C(4)] = 123) == 123);
    V(M(ref A(a)[B(1),C(2)]) == 0);
    V(A(a)[B(1),C(2)] == 456);
    V(A(a)[B(3),C(4)] == 123);
    V(A(a)[B(0),C(0)] == 0);
    V(A(a)[B(0),C(0)]++ == 0);
    V(A(a)[B(0),C(0)] == 1);
    V(++A(a)[B(0),C(0)] == 2);
    V(A(a)[B(0),C(0)] == 2);
    V((A(a)[B(1),C(1)] += 3) == 3);
    V((A(a)[B(1),C(1)] -= 2) == 1);
  }
  static void V(bool b)
  {
    Console.WriteLine(b ? 't' : 'f');
  }

}";
            string expected = @"A0B1C2t
A3B4C5t
A6B7C8t
A9B10C11t
A12B13C14t
A15B16C17t
A18B19C20t
A21B22C23t
A24B25C26t
A27B28C29t
A30B31C32t";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }

        [Fact]
        public void TestMultiDimensionalArrayInitializers()
        {
            // Verify that operations on multidimensional arrays work and cause side effects only once.

            string source = @"
using System;
class P
{
  static void Main()
  {
    byte[,,] b = new byte[,,]{ { { 1, 2, 3 }, { 4, 5, 6 } }, { { 7, 8, 9 }, { 10, 11, 12 } } };
    int x = 999;
    int y = 888;
    int[,,] i = new int[,,]{ { { 1, 2, 3 }, { 4, 5, y } }, { { x, 8, 9 }, { 10, 11, 12 } } };
    double[,,] d = new double[,,]{ { { y, y, y }, { x, x, x } }, { { x, y, x }, { 10, 11, 12 } } };

    for (int j = 0 ; j < 2; ++j)
        for (int k = 0; k < 2; ++k)
            for (int l = 0; l < 3; ++l)
                Console.Write(b[j,k,l]);

    Console.WriteLine();

    for (int j = 0 ; j < 2; ++j)
        for (int k = 0; k < 2; ++k)
            for (int l = 0; l < 3; ++l)
                Console.Write(i[j,k,l]);

    Console.WriteLine();

    for (int j = 0 ; j < 2; ++j)
        for (int k = 0; k < 2; ++k)
            for (int l = 0; l < 3; ++l)
                Console.Write(d[j,k,l]);
  }
}";
            string expected = @"123456789101112
1234588899989101112
888888888999999999999888999101112";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }


        [Fact]
        public void TestMultiDimensionalArrayForEach()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        int y = 50;
        foreach (var x in new int[2, 3] {{10, 20, 30}, {40, y, 60}})
        {
            System.Console.Write(x);
        }
    }
}";
            string expected = @"102030405060";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }

        [WorkItem(544081, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544081")]
        [Fact()]
        public void MultiDimArrayGenericTypeWiderThanArrayType()
        {
            string source = @"
class Program { 
    public static void Ref<T>(T[,] array) 
    {
        array[0, 0].GetType();
    } 

    static void Main(string[] args) 
    { 
        Ref<object>(new string[,] { { System.String.Empty } }); 
    }
}";
            string expected = @"";

            var verifier = CompileAndVerify(source: source, expectedOutput: expected);
        }

        [WorkItem(544364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544364")]
        [Fact]
        public void MissingNestedArrayInitializerWithNullConst()
        {
            var text =
@"class Program
{
    static void Main(string[] args)
    {
        int?[ , ] ar1 = { null  };
    }
}";
            CreateCompilation(text).VerifyDiagnostics(
// (5,27): error CS0846: A nested array initializer is expected
//         int?[ , ] ar1 = { null  };
Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "null")
                );
        }

        private static string s_arraysOfRank1IlSource = @"
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance float64[0...] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test1""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
                ldc.i4.0
                ldc.i4.1
                newobj instance void float64[...]::.ctor(int32, int32)
                dup
                ldc.i4.0
                ldc.r8 -100
                call instance void float64[...]::Set(int32, float64)
      IL_000a:  ret
    } // end of method Test::Test1

    .method public hidebysig newslot virtual 
            instance float64 Test2(float64[0...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  2
      IL_0000:  ldstr      ""Test2""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
                ldarg.1
                ldc.i4.0
                call instance float64 float64[...]::Get(int32)
      IL_000a:  ret
    } // end of method Test::Test2

    .method public hidebysig newslot virtual 
            instance void Test3(float64[0...] x) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  2
      IL_000a:  ret
    } // end of method Test::Test3

    .method public hidebysig static void  M1<T>(!!T[0...] a) cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M1

    .method public hidebysig static void  M2<T>(!!T[] a, !!T[0...] b) cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  nop
      IL_0001:  ldtoken    !!T
      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_000b:  call       void [mscorlib]System.Console::WriteLine(object)
      IL_0010:  nop
      IL_0011:  ret
    } // end of method M2

} // end of class Test
";

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_GetElement()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        System.Console.WriteLine(t.Test1()[0]);
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
-100");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       22 (0x16)
  .maxstack  2
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  callvirt   ""double[*] Test.Test1()""
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""double[*].Get""
  IL_0010:  call       ""void System.Console.WriteLine(double)""
  IL_0015:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_SetElement()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        var a = t.Test1();
        a[0] = 123;
        System.Console.WriteLine(t.Test2(a));
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
Test2
123");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       40 (0x28)
  .maxstack  4
  .locals init (double[*] V_0) //a
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""double[*] Test.Test1()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  ldc.r8     123
  IL_0017:  call       ""double[*].Set""
  IL_001c:  ldloc.0
  IL_001d:  callvirt   ""double Test.Test2(double[*])""
  IL_0022:  call       ""void System.Console.WriteLine(double)""
  IL_0027:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_ElementAddress()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        var a = t.Test1();
        TestRef(ref a[0]);
        System.Console.WriteLine(t.Test2(a));
    }

    static void TestRef(ref double val)
    {
        val = 123;
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
Test2
123");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (double[*] V_0) //a
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""double[*] Test.Test1()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  call       ""double[*].Address""
  IL_0013:  call       ""void C.TestRef(ref double)""
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""double Test.Test2(double[*])""
  IL_001e:  call       ""void System.Console.WriteLine(double)""
  IL_0023:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [Fact]
        public void ArraysOfRank1_Overriding01()
        {
            var source =
@"class C : Test
{
    public override double[] Test1()
    {
        return null;
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
    // (3,30): error CS0508: 'C.Test1()': return type must be 'double[*]' to match overridden member 'Test.Test1()'
    //     public override double[] Test1()
    Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "Test1").WithArguments("C.Test1()", "Test.Test1()", "double[*]").WithLocation(3, 30)
                );
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [Fact]
        public void ArraysOfRank1_Overriding02()
        {
            var source =
@"class C : Test
{
    public override double Test2(double[] x)
    {
        return x[0];
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
    // (3,28): error CS0115: 'C.Test2(double[])': no suitable method found to override
    //     public override double Test2(double[] x)
    Diagnostic(ErrorCode.ERR_OverrideNotExpected, "Test2").WithArguments("C.Test2(double[])").WithLocation(3, 28)
                );
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [Fact]
        public void ArraysOfRank1_Conversions()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        double[] a1 = t.Test1();
        double[] a2 = (double[])t.Test1();
        System.Collections.Generic.IList<double> a3 = t.Test1();
        double [] a4 = null;
        t.Test2(a4);
        var a5 = (System.Collections.Generic.IList<double>)t.Test1();
        System.Collections.Generic.IList<double> ilist = new double [] {};
        var mdarray = t.Test1();
        mdarray = ilist;
        mdarray = t.Test1();
        mdarray = new [] { 3.0d };
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
    // (6,23): error CS0029: Cannot implicitly convert type 'double[*]' to 'double[]'
    //         double[] a1 = t.Test1();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "t.Test1()").WithArguments("double[*]", "double[]").WithLocation(6, 23),
    // (7,23): error CS0030: Cannot convert type 'double[*]' to 'double[]'
    //         double[] a2 = (double[])t.Test1();
    Diagnostic(ErrorCode.ERR_NoExplicitConv, "(double[])t.Test1()").WithArguments("double[*]", "double[]").WithLocation(7, 23),
    // (8,55): error CS0029: Cannot implicitly convert type 'double[*]' to 'System.Collections.Generic.IList<double>'
    //         System.Collections.Generic.IList<double> a3 = t.Test1();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "t.Test1()").WithArguments("double[*]", "System.Collections.Generic.IList<double>").WithLocation(8, 55),
    // (10,17): error CS1503: Argument 1: cannot convert from 'double[]' to 'double[*]'
    //         t.Test2(a4);
    Diagnostic(ErrorCode.ERR_BadArgType, "a4").WithArguments("1", "double[]", "double[*]").WithLocation(10, 17),
    // (11,18): error CS0030: Cannot convert type 'double[*]' to 'System.Collections.Generic.IList<double>'
    //         var a5 = (System.Collections.Generic.IList<double>)t.Test1();
    Diagnostic(ErrorCode.ERR_NoExplicitConv, "(System.Collections.Generic.IList<double>)t.Test1()").WithArguments("double[*]", "System.Collections.Generic.IList<double>").WithLocation(11, 18),
    // (14,19): error CS0029: Cannot implicitly convert type 'System.Collections.Generic.IList<double>' to 'double[*]'
    //         mdarray = ilist;
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "ilist").WithArguments("System.Collections.Generic.IList<double>", "double[*]").WithLocation(14, 19),
    // (16,19): error CS0029: Cannot implicitly convert type 'double[]' to 'double[*]'
    //         mdarray = new [] { 3.0d };
    Diagnostic(ErrorCode.ERR_NoImplicitConv, "new [] { 3.0d }").WithArguments("double[]", "double[*]").WithLocation(16, 19)
                );
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [Fact]
        public void ArraysOfRank1_TypeArgumentInference01()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        var md = t.Test1();
        var sz = new double [] {};
        
        M1(sz);
        M1(md);
        M2(sz, sz);
        M2(md, md);
        M2(sz, md);
        M2(md, sz);
        M3(sz);
        M3(md);

        Test.M1(sz);
        Test.M1(md);
        Test.M2(sz, sz);
        Test.M2(md, md);
        Test.M2(sz, md);
        Test.M2(md, sz);
    }

    static void M1<T>(T [] a){}
    static void M2<T>(T a, T b){}
    static void M3<T>(System.Collections.Generic.IList<T> a){}
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);

            var m2 = compilation.GetTypeByMetadataName("Test").GetMember<MethodSymbol>("M2");
            var szArray = (ArrayTypeSymbol)m2.Parameters.First().Type;
            Assert.Equal("T[]", szArray.ToTestDisplayString());
            Assert.True(szArray.IsSZArray);
            Assert.Equal(1, szArray.Rank);
            Assert.True(szArray.Sizes.IsEmpty);
            Assert.True(szArray.LowerBounds.IsDefault);

            var mdArray = (ArrayTypeSymbol)m2.Parameters.Last().Type;
            Assert.Equal("T[*]", mdArray.ToTestDisplayString());
            Assert.False(mdArray.IsSZArray);
            Assert.Equal(1, mdArray.Rank);
            Assert.True(mdArray.Sizes.IsEmpty);
            Assert.True(mdArray.LowerBounds.IsDefault);

            compilation.VerifyDiagnostics(
    // (10,9): error CS0411: The type arguments for method 'C.M1<T>(T[])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         M1(md);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("C.M1<T>(T[])").WithLocation(10, 9),
    // (13,9): error CS0411: The type arguments for method 'C.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         M2(sz, md);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("C.M2<T>(T, T)").WithLocation(13, 9),
    // (14,9): error CS0411: The type arguments for method 'C.M2<T>(T, T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         M2(md, sz);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("C.M2<T>(T, T)").WithLocation(14, 9),
    // (16,9): error CS0411: The type arguments for method 'C.M3<T>(IList<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         M3(md);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M3").WithArguments("C.M3<T>(System.Collections.Generic.IList<T>)").WithLocation(16, 9),
    // (18,14): error CS0411: The type arguments for method 'Test.M1<T>(T[*])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         Test.M1(sz);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M1").WithArguments("Test.M1<T>(T[*])").WithLocation(18, 14),
    // (20,21): error CS1503: Argument 2: cannot convert from 'double[]' to 'double[*]'
    //         Test.M2(sz, sz);
    Diagnostic(ErrorCode.ERR_BadArgType, "sz").WithArguments("2", "double[]", "double[*]").WithLocation(20, 21),
    // (21,17): error CS1503: Argument 1: cannot convert from 'double[*]' to 'double[]'
    //         Test.M2(md, md);
    Diagnostic(ErrorCode.ERR_BadArgType, "md").WithArguments("1", "double[*]", "double[]").WithLocation(21, 17),
    // (23,14): error CS0411: The type arguments for method 'Test.M2<T>(T[], T[*])' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         Test.M2(md, sz);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "M2").WithArguments("Test.M2<T>(T[], T[*])").WithLocation(23, 14)
                );
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_TypeArgumentInference02()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        var md = t.Test1();
        var sz = new double [] {};
        
        M2(md, md);

        Test.M1(md);
        Test.M2(sz, md);
    }

    static void M2<T>(T a, T b)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput:
@"Test1
System.Double[*]
System.Double
System.Double
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_ForEach()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        foreach (var d in t.Test1())
        {
            System.Console.WriteLine(d);
        }
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
-100");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       50 (0x32)
  .maxstack  2
  .locals init (double[*] V_0,
                int V_1,
                int V_2)
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  callvirt   ""double[*] Test.Test1()""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0012:  stloc.1
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.0
  IL_0015:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_001a:  stloc.2
  IL_001b:  br.s       IL_002d
  IL_001d:  ldloc.0
  IL_001e:  ldloc.2
  IL_001f:  call       ""double[*].Get""
  IL_0024:  call       ""void System.Console.WriteLine(double)""
  IL_0029:  ldloc.2
  IL_002a:  ldc.i4.1
  IL_002b:  add
  IL_002c:  stloc.2
  IL_002d:  ldloc.2
  IL_002e:  ldloc.1
  IL_002f:  ble.s      IL_001d
  IL_0031:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_Length()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        System.Console.WriteLine(t.Test1().Length);
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
1");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  callvirt   ""double[*] Test.Test1()""
  IL_000a:  callvirt   ""int System.Array.Length.get""
  IL_000f:  call       ""void System.Console.WriteLine(int)""
  IL_0014:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ArraysOfRank1_LongLength()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        System.Console.WriteLine(t.Test1().LongLength);
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
1");

            verifier.VerifyIL("C.Main",
@"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  callvirt   ""double[*] Test.Test1()""
  IL_000a:  callvirt   ""long System.Array.LongLength.get""
  IL_000f:  call       ""void System.Console.WriteLine(long)""
  IL_0014:  ret
}
");
        }

        [WorkItem(126766, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/126766"), WorkItem(4924, "https://github.com/dotnet/roslyn/issues/4924")]
        [Fact]
        public void ArraysOfRank1_ParamArray()
        {
            var source =
@"class C
{
    static void Main()
    {
        var t = new Test();
        double d = 1.2;
        t.Test3(d);
        t.Test3(new double [] {d});
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, s_arraysOfRank1IlSource, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics(
    // (7,17): error CS1503: Argument 1: cannot convert from 'double' to 'params double[*]'
    //         t.Test3(d);
    Diagnostic(ErrorCode.ERR_BadArgType, "d").WithArguments("1", "double", "params double[*]").WithLocation(7, 17),
    // (8,17): error CS1503: Argument 1: cannot convert from 'double[]' to 'params double[*]'
    //         t.Test3(new double [] {d});
    Diagnostic(ErrorCode.ERR_BadArgType, "new double [] {d}").WithArguments("1", "double[]", "params double[*]").WithLocation(8, 17)
                );
        }

        [WorkItem(4954, "https://github.com/dotnet/roslyn/issues/4954")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void SizesAndLowerBounds_01()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance float64[,] Test1() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test1""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[...,] Test2() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test2""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[...,...] Test3() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test3""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,] Test4() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test4""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,...] Test5() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test5""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,5] Test6() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test6""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,2...] Test7() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test7""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[5,2...8] Test8() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test8""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,] Test9() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test9""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,...] Test10() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test10""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,5] Test11() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test11""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,2...] Test12() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test12""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5,2...8] Test13() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test13""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,] Test14() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test14""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,...] Test15() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test15""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...,2...] Test16() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test16""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance float64[1...5] Test17() cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test17""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_0007:  ldnull
      IL_000a:  ret
    } 
} // end of class Test
";

            var source =
@"class C : Test
{
    static void Main()
    {
        double[,] a;

        var t = new Test();
        a = t.Test1();
        a = t.Test2();
        a = t.Test3();
        a = t.Test4();
        a = t.Test5();
        a = t.Test6();
        a = t.Test7();
        a = t.Test8();
        a = t.Test9();
        a = t.Test10();
        a = t.Test11();
        a = t.Test12();
        a = t.Test13();
        a = t.Test14();
        a = t.Test15();
        a = t.Test16();

        t = new C();
        a = t.Test1();
        a = t.Test2();
        a = t.Test3();
        a = t.Test4();
        a = t.Test5();
        a = t.Test6();
        a = t.Test7();
        a = t.Test8();
        a = t.Test9();
        a = t.Test10();
        a = t.Test11();
        a = t.Test12();
        a = t.Test13();
        a = t.Test14();
        a = t.Test15();
        a = t.Test16();
    }

    public override double[,] Test1()
    {
        System.Console.WriteLine(""Overridden 1"");
        return null;
    }
    public override double[,] Test2()
    {
        System.Console.WriteLine(""Overridden 2"");
        return null;
    }
    public override double[,] Test3()
    {
        System.Console.WriteLine(""Overridden 3"");
        return null;
    }
    public override double[,] Test4()
    {
        System.Console.WriteLine(""Overridden 4"");
        return null;
    }
    public override double[,] Test5()
    {
        System.Console.WriteLine(""Overridden 5"");
        return null;
    }
    public override double[,] Test6()
    {
        System.Console.WriteLine(""Overridden 6"");
        return null;
    }
    public override double[,] Test7()
    {
        System.Console.WriteLine(""Overridden 7"");
        return null;
    }
    public override double[,] Test8()
    {
        System.Console.WriteLine(""Overridden 8"");
        return null;
    }
    public override double[,] Test9()
    {
        System.Console.WriteLine(""Overridden 9"");
        return null;
    }
    public override double[,] Test10()
    {
        System.Console.WriteLine(""Overridden 10"");
        return null;
    }
    public override double[,] Test11()
    {
        System.Console.WriteLine(""Overridden 11"");
        return null;
    }
    public override double[,] Test12()
    {
        System.Console.WriteLine(""Overridden 12"");
        return null;
    }
    public override double[,] Test13()
    {
        System.Console.WriteLine(""Overridden 13"");
        return null;
    }
    public override double[,] Test14()
    {
        System.Console.WriteLine(""Overridden 14"");
        return null;
    }
    public override double[,] Test15()
    {
        System.Console.WriteLine(""Overridden 15"");
        return null;
    }
    public override double[,] Test16()
    {
        System.Console.WriteLine(""Overridden 16"");
        return null;
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);

            var test = compilation.GetTypeByMetadataName("Test");
            var array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test1").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.True(array.LowerBounds.IsEmpty);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test2").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.True(array.LowerBounds.IsEmpty);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test3").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.True(array.LowerBounds.IsEmpty);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test4").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 0 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test5").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 0 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test6").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5, 5 }, array.Sizes);
            Assert.True(array.LowerBounds.IsDefault);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test7").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 0, 2 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test8").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5, 7 }, array.Sizes);
            Assert.Equal(new[] { 0, 2 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test9").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 1 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test10").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 1 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test11").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5, 5 }, array.Sizes);
            Assert.Equal(new[] { 1, 0 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test12").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 1, 2 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test13").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.Equal(new[] { 5, 7 }, array.Sizes);
            Assert.Equal(new[] { 1, 2 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test14").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.Equal(new[] { 1 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test15").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.Equal(new[] { 1 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test16").ReturnType;
            Assert.Equal("System.Double[,]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(2, array.Rank);
            Assert.True(array.Sizes.IsEmpty);
            Assert.Equal(new[] { 1, 2 }, array.LowerBounds);

            array = (ArrayTypeSymbol)test.GetMember<MethodSymbol>("Test17").ReturnType;
            Assert.Equal("System.Double[*]", array.ToTestDisplayString());
            Assert.False(array.IsSZArray);
            Assert.Equal(1, array.Rank);
            Assert.Equal(new[] { 5 }, array.Sizes);
            Assert.Equal(new[] { 1 }, array.LowerBounds);

            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
Test2
Test3
Test4
Test5
Test6
Test7
Test8
Test9
Test10
Test11
Test12
Test13
Test14
Test15
Test16
Overridden 1
Overridden 2
Overridden 3
Overridden 4
Overridden 5
Overridden 6
Overridden 7
Overridden 8
Overridden 9
Overridden 10
Overridden 11
Overridden 12
Overridden 13
Overridden 14
Overridden 15
Overridden 16
");
        }

        [WorkItem(4954, "https://github.com/dotnet/roslyn/issues/4954")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void SizesAndLowerBounds_02()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  1
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test1::.ctor

    .method public hidebysig newslot virtual 
            instance void Test1(float64[,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test1""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test2(float64[...,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test2""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test3(float64[...,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test3""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test4(float64[5,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test4""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test5(float64[5,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test5""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test6(float64[5,5] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test6""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test7(float64[5,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test7""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test8(float64[5,2...8] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test8""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test9(float64[1...5,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test9""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test10(float64[1...5,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test10""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test11(float64[1...5,5] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test11""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test12(float64[1...5,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test12""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test13(float64[1...5,2...8] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test13""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test14(float64[1...,] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test14""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test15(float64[1...,...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test15""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 

    .method public hidebysig newslot virtual 
            instance void Test16(float64[1...,2...] x) cil managed
    {
      // Code size       11 (0xb)
      .maxstack  4
      IL_0000:  ldstr      ""Test16""
      IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
      IL_000a:  ret
    } 
} // end of class Test
";

            var source =
@"class C : Test
{
    static void Main()
    {
        double[,] a = new double [,] {};

        var t = new Test();
        t.Test1(a);
        t.Test2(a);
        t.Test3(a);
        t.Test4(a);
        t.Test5(a);
        t.Test6(a);
        t.Test7(a);
        t.Test8(a);
        t.Test9(a);
        t.Test10(a);
        t.Test11(a);
        t.Test12(a);
        t.Test13(a);
        t.Test14(a);
        t.Test15(a);
        t.Test16(a);

        t = new C();
        t.Test1(a);
        t.Test2(a);
        t.Test3(a);
        t.Test4(a);
        t.Test5(a);
        t.Test6(a);
        t.Test7(a);
        t.Test8(a);
        t.Test9(a);
        t.Test10(a);
        t.Test11(a);
        t.Test12(a);
        t.Test13(a);
        t.Test14(a);
        t.Test15(a);
        t.Test16(a);
    }

    public override void Test1(double[,] x)
    {
        System.Console.WriteLine(""Overridden 1"");
    }
    public override void Test2(double[,] x)
    {
        System.Console.WriteLine(""Overridden 2"");
    }
    public override void Test3(double[,] x)
    {
        System.Console.WriteLine(""Overridden 3"");
    }
    public override void Test4(double[,] x)
    {
        System.Console.WriteLine(""Overridden 4"");
    }
    public override void Test5(double[,] x)
    {
        System.Console.WriteLine(""Overridden 5"");
    }
    public override void Test6(double[,] x)
    {
        System.Console.WriteLine(""Overridden 6"");
    }
    public override void Test7(double[,] x)
    {
        System.Console.WriteLine(""Overridden 7"");
    }
    public override void Test8(double[,] x)
    {
        System.Console.WriteLine(""Overridden 8"");
    }
    public override void Test9(double[,] x)
    {
        System.Console.WriteLine(""Overridden 9"");
    }
    public override void Test10(double[,] x)
    {
        System.Console.WriteLine(""Overridden 10"");
    }
    public override void Test11(double[,] x)
    {
        System.Console.WriteLine(""Overridden 11"");
    }
    public override void Test12(double[,] x)
    {
        System.Console.WriteLine(""Overridden 12"");
    }
    public override void Test13(double[,] x)
    {
        System.Console.WriteLine(""Overridden 13"");
    }
    public override void Test14(double[,] x)
    {
        System.Console.WriteLine(""Overridden 14"");
    }
    public override void Test15(double[,] x)
    {
        System.Console.WriteLine(""Overridden 15"");
    }
    public override void Test16(double[,] x)
    {
        System.Console.WriteLine(""Overridden 16"");
    }
}
";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(compilation, expectedOutput:
@"Test1
Test2
Test3
Test4
Test5
Test6
Test7
Test8
Test9
Test10
Test11
Test12
Test13
Test14
Test15
Test16
Overridden 1
Overridden 2
Overridden 3
Overridden 4
Overridden 5
Overridden 6
Overridden 7
Overridden 8
Overridden 9
Overridden 10
Overridden 11
Overridden 12
Overridden 13
Overridden 14
Overridden 15
Overridden 16
");
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        [WorkItem(4958, "https://github.com/dotnet/roslyn/issues/4958")]
        public void ArraysOfRank1_InAttributes()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void
          Test1() cil managed
  {
    .custom instance void TestAttribute::.ctor(class [mscorlib] System.Type) = {type(class 'System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')}
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Program::Test1

  .method public hidebysig instance void
          Test2() cil managed
  {
    .custom instance void TestAttribute::.ctor(class [mscorlib] System.Type) = {type(class 'System.Int32[*], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')}
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Program::Test2

  .method public hidebysig instance void
          Test3() cil managed
  {
    .custom instance void TestAttribute::.ctor(class [mscorlib] System.Type) = {type(class 'System.Int32[*,*], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')}
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Program::Test3

  .method public hidebysig instance void
          Test4() cil managed
  {
    .custom instance void TestAttribute::.ctor(class [mscorlib] System.Type) = {type(class 'System.Int32[,*], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089')}
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ret
  } // end of method Program::Test4
} // end of class Program

.class public auto ansi beforefieldinit TestAttribute
       extends [mscorlib]System.Attribute
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(class [mscorlib]System.Type val) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  } // end of method TestAttribute::.ctor

} // end of class TestAttribute
";

            var source =
@"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        System.Console.WriteLine(GetTypeFromAttribute(""Test1"")); 
        System.Console.WriteLine(GetTypeFromAttribute(""Test2"")); 

        try
        {
            GetTypeFromAttribute(""Test3"");
        }
        catch
        {
            System.Console.WriteLine(""Throws""); 
        }

        try
        {
            GetTypeFromAttribute(""Test4"");
        }
        catch
        {
            System.Console.WriteLine(""Throws""); 
        }
    }

    private static Type GetTypeFromAttribute(string target)
    {
        return (System.Type)typeof(Program).GetMember(target)[0].GetCustomAttributesData().ElementAt(0).ConstructorArguments[0].Value;
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource, references: new[] { SystemCoreRef }, options: TestOptions.ReleaseExe);

            var p = compilation.GetTypeByMetadataName("Program");
            var a1 = (IArrayTypeSymbol)p.GetMember<MethodSymbol>("Test1").GetAttributes().Single().ConstructorArguments.Single().Value;
            Assert.Equal("System.Int32[]", a1.ToTestDisplayString());
            Assert.Equal(1, a1.Rank);
            Assert.True(a1.IsSZArray);

            var a2 = (IArrayTypeSymbol)p.GetMember<MethodSymbol>("Test2").GetAttributes().Single().ConstructorArguments.Single().Value;
            Assert.Equal("System.Int32[*]", a2.ToTestDisplayString());
            Assert.Equal(1, a2.Rank);
            Assert.False(a2.IsSZArray);

            Assert.True(((ITypeSymbol)p.GetMember<MethodSymbol>("Test3").GetAttributes().Single().ConstructorArguments.Single().Value).IsErrorType());
            Assert.True(((ITypeSymbol)p.GetMember<MethodSymbol>("Test4").GetAttributes().Single().ConstructorArguments.Single().Value).IsErrorType());

            CompileAndVerify(compilation, expectedOutput:
@"System.Int32[]
System.Int32[*]
Throws
Throws");
        }
    }
}

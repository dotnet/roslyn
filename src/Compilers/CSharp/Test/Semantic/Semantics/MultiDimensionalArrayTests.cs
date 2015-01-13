// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WorkItem(544081, "DevDiv")]
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

        [WorkItem(544364, "DevDiv")]
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
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
// (5,27): error CS0846: A nested array initializer is expected
//         int?[ , ] ar1 = { null  };
Diagnostic(ErrorCode.ERR_ArrayInitializerExpected, "null")
                );
        }
    }
}

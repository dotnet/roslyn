// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class OperationAnalyzerTests : CompilingTestBase
    {
        [Fact]
        public void EmptyArrayCSharp()
        {
            const string source = @"
class C
{
    void M1()
    {
        int[] arr1 = new int[0];                       // yes
        byte[] arr2 = { };                             // yes
        C[] arr3 = new C[] { };                        // yes
        string[] arr4 = new string[] { null };         // no
        double[] arr5 = new double[1];                 // no
        int[] arr6 = new[] { 1 };                      // no
        int[][] arr7 = new int[0][];                   // yes
        int[][][][] arr8 = new int[0][][][];           // yes
        int[,] arr9 = new int[0,0];                    // no
        int[][,] arr10 = new int[0][,];                // yes
        int[][,] arr11 = new int[1][,];                // no
        int[,][] arr12 = new int[0,0][];               // no
    }
}";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new EmptyArrayOperationAnalyzer() }, null, null, false,
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0]").WithLocation(6, 22),
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "{ }").WithLocation(7, 23),
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "new C[] { }").WithLocation(8, 20),
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][]").WithLocation(12, 24),
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][][][]").WithLocation(13, 28),
                Diagnostic(EmptyArrayOperationAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][,]").WithLocation(15, 26)
                );
        }

        [Fact]
        public void BoxingCSharp()
        {
            const string source = @"
class C
{
    public object M1(object p1, object p2, object p3)
    {
         S v1 = new S();
         S v2 = v1;
         S v3 = v1.M1(v2);
         object v4 = M1(3, this, v1);
         object v5 = v3;
         if (p1 == null)
         {
             return 3;
         }
         if (p2 == null)
         {
             return v3;
         }
         if (p3 == null)
         {
             return v4;
         }
         return v5;
    }
}

struct S
{
    public int X;
    public int Y;
    public object Z;

    public S M1(S p1)
    {
        p1.GetType();
        Z = this;
        X = 1;
        Y = 2;
        return p1;
    }
}";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new BoxingOperationAnalyzer() }, null, null, false,
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "3").WithLocation(9, 25),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v1").WithLocation(9, 34),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v3").WithLocation(10, 22),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "3").WithLocation(13, 21),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "v3").WithLocation(17, 21),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "p1").WithLocation(35, 9),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "this").WithLocation(36, 13)
                );
        }

        [Fact]
        public void BigForCSharp()
        {
            const string source = @"
class C
{
    public void M1()
    {
        int x;
        for (x = 0; x < 200000; x++) {}
      
        for (x = 0; x < 2000000; x++) {}

        for (x = 1500000; x > 0; x -= 2) {}

        for (x = 3000000; x > 0; x -= 2) {}

        for (x = 0; x < 200000; x = x + 1) {}

        for (x = 0; x < 2000000; x = x + 1) {}
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new BigForTestAnalyzer() }, null, null, false,
                Diagnostic(BigForTestAnalyzer.BigForDescriptor.Id, "for (x = 0; x < 2000000; x++) {}").WithLocation(9, 9),
                Diagnostic(BigForTestAnalyzer.BigForDescriptor.Id, "for (x = 3000000; x > 0; x -= 2) {}").WithLocation(13, 9),
                Diagnostic(BigForTestAnalyzer.BigForDescriptor.Id, "for (x = 0; x < 2000000; x = x + 1) {}").WithLocation(17, 9)
                );
        }
   
        [Fact]
        public void SparseSwitchCSharp()
        {
            const string source = @"
class C
{
    public void M1(int x, int y)
    {
        switch (x)
        {
            case 1:
                break;
            case 10:
                break;
            default:
                break;
        }

        switch (y)
        {
            case 1:
                break;
            case 1000:
                break;
            default:
                break;
        }
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new SparseSwitchTestAnalyzer() }, null, null, false,
                Diagnostic(SparseSwitchTestAnalyzer.SparseSwitchDescriptor.Id, "y").WithLocation(16, 17)
                );
        }
   
        [Fact]
        public void InvocationCSharp()
        {
            const string source = @"
class C
{
    public void M0(int a, params int[] b)
    {
    }

    public void M1(int a, int b, int c, int x, int y, int z)
    {
    }

    public void M2()
    {
        M1(1, 2, 3, 4, 5, 6);
        M1(a: 1, b: 2, c: 3, x: 4, y:5, z:6);
        M1(a: 1, c: 2, b: 3, x: 4, y:5, z:6);
        M1(z: 1, x: 2, y: 3, c: 4, a:5, b:6);
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
        M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13);
        M0(1);
        M0(1, 2, 4, 3);
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new InvocationTestAnalyzer() }, null, null, false,
                Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "2").WithLocation(16, 21),
                Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "1").WithLocation(17, 15),
                Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "2").WithLocation(17, 21),
                Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "4").WithLocation(17, 33),
                Diagnostic(InvocationTestAnalyzer.BigParamarrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)").WithLocation(19, 9),
                Diagnostic(InvocationTestAnalyzer.BigParamarrayArgumentsDescriptor.Id, "M0(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13)").WithLocation(20, 9),
                Diagnostic(InvocationTestAnalyzer.OutOfNumericalOrderArgumentsDescriptor.Id, "3").WithLocation(22, 21)
                );
        }
    }
}

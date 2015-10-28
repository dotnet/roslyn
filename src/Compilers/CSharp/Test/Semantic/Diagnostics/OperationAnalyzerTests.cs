// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics.SystemLanguage;
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
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new EmptyArrayAnalyzer() }, null, null, false,
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0]").WithLocation(6, 22),
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "{ }").WithLocation(7, 23),
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "new C[] { }").WithLocation(8, 20),
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][]").WithLocation(12, 24),
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][][][]").WithLocation(13, 28),
                Diagnostic(EmptyArrayAnalyzer.UseArrayEmptyDescriptor.Id, "new int[0][,]").WithLocation(15, 26)
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

        [Fact]
        public void FieldCouldBeReadOnlyCSharp()
        {
            const string source = @"
class C
{
    int F1;
    const int F2 = 2;
    readonly int F3;
    int F4;
    int F5;
    int F6 = 6;
    int F7;
    int F8 = 8;
    S F9;
    C1 F10 = new C1();

    public C()
    {
        F1 = 1;
        F3 = 3;
        F4 = 4;
        F5 = 5;
    }

    public void M0()
    {
        int x = F1;
        x = F2;
        x = F3;
        x = F4;
        x = F5;
        x = F6;
        x = F7;

        F4 = 4;
        F7 = 7;
        M1(out F1, F5);
        F8++;
        F9.A = 10;
        F9.B = 20;
        F10.A = F9.A;
        F10.B = F9.B;
    }

    public void M1(out int x, int y)
    {
        x = 10;
    }

    struct S
    {
        public int A;
        public int B;
    }

    class C1
    {
        public int A;
        public int B;
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new FieldCouldBeReadOnlyAnalyzer() }, null, null, false,
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F5").WithLocation(8, 9),
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F6").WithLocation(9, 9),
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F10").WithLocation(13, 8)
                );
        }

        [Fact]
        public void StaticFieldCouldBeReadOnlyCSharp()
        {
            const string source = @"
class C
{
    static int F1;
    static readonly int F2 = 2;
    static readonly int F3;
    static int F4;
    static int F5;
    static int F6 = 6;
    static int F7;
    static int F8 = 8;
    static S F9;
    static C1 F10 = new C1();

    static C()
    {
        F1 = 1;
        F3 = 3;
        F4 = 4;
        F5 = 5;
    }

    public static void M0()
    {
        int x = F1;
        x = F2;
        x = F3;
        x = F4;
        x = F5;
        x = F6;
        x = F7;

        F4 = 4;
        F7 = 7;
        M1(out F1, F5);
        F7 = 7;
        F8--;
        F9.A = 10;
        F9.B = 20;
        F10.A = F9.A;
        F10.B = F9.B;
    }

    public static void M1(out int x, int y)
    {
        x = 10;
    }

    struct S
    {
        public int A;
        public int B;
    }

    class C1
    {
        public int A;
        public int B;
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new FieldCouldBeReadOnlyAnalyzer() }, null, null, false,
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F5").WithLocation(8, 16),
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F6").WithLocation(9, 16),
                Diagnostic(FieldCouldBeReadOnlyAnalyzer.FieldCouldBeReadOnlyDescriptor.Id, "F10").WithLocation(13, 15)
                );
        }

        [Fact]
        public void LocalCouldBeConstCSharp()
        {
            const string source = @"
class C
{
    public void M0(int p)
    {
        int x = p;
        int y = x;
        const int z = 1;
        int a = 2;
        int b = 3;
        int c = 4;
        int d = 5;
        int e = 6;
        string s = ""ZZZ"";
        b = 3;
        c++;
        d += e + b;
        M1(out y, z, ref a, s);
        S n;
        n.A = 10;
        n.B = 20;
        C1 o = new C1();
        o.A = 10;
        o.B = 20;
    }

    public void M1(out int x, int y, ref int z, string s)
    {
        x = 10;
    }

    struct S
    {
        public int A;
        public int B;
    }

    class C1
    {
        public int A;
        public int B;
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new LocalCouldBeConstAnalyzer() }, null, null, false,
                Diagnostic(LocalCouldBeConstAnalyzer.LocalCouldBeConstDescriptor.Id, "e").WithLocation(13, 13),
                Diagnostic(LocalCouldBeConstAnalyzer.LocalCouldBeConstDescriptor.Id, "s").WithLocation(14, 16)
                );
        }

        [Fact]
        public void LocalCouldHaveMoreSpecificTypeCSharp()
        {
            const string source = @"
class C
{
    public void M0(int p)
    {
        object a = new Middle();
        object b = new Value(10);
    }

    class Base
    {
    }

    class Middle
    {
    }

    class Derived
    {
    }

    struct Value
    {
        public Value(int a)
        {
            X = a;
        }

        public int X;
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new CouldHaveMoreSpecificTypeAnalyzer() }, null, null, false,
                Diagnostic(CouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "a").WithArguments("a", "C.Middle").WithLocation(6, 16),
                Diagnostic(CouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "b").WithArguments("b", "C.Value").WithLocation(7, 16)
                );
        }
    }
}

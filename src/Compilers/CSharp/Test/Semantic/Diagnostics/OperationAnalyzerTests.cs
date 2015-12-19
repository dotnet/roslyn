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
        public void ImplicitVsExplicitInstancesCSharp()
        {
            const string source = @"
class C
{
    public virtual void M1()
    {
        this.M1();
        M1();
    }
    public void M2()
    {
    }
}

class D : C
{
    public override void M1()
    {
        base.M1();
        M1();
        M2();
    }
}";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new ExplicitVsImplicitInstanceAnalyzer() }, null, null, false,
                Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ExplicitInstanceDescriptor.Id, "this").WithLocation(6, 9),
                Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M1").WithLocation(7, 9),
                Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ExplicitInstanceDescriptor.Id, "base").WithLocation(18, 9),
                Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M1").WithLocation(19, 9),
                Diagnostic(ExplicitVsImplicitInstanceAnalyzer.ImplicitInstanceDescriptor.Id, "M2").WithLocation(20, 9)
                );
        }

        [Fact]
        public void EventReferencesCSharp()
        {
            const string source = @"
public delegate void MumbleEventHandler(object sender, System.EventArgs args);

class C
{
    public event MumbleEventHandler Mumble;

    public void OnMumble(System.EventArgs args)
    {
        Mumble += new MumbleEventHandler(Mumbler);
        Mumble(this, args);
        object o = Mumble;
        MumbleEventHandler d = Mumbler;
    }

    private void Mumbler(object sender, System.EventArgs args) 
    {
    }
}";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new MemberReferenceAnalyzer() }, null, null, false,
                Diagnostic(MemberReferenceAnalyzer.EventReferenceDescriptor.Id, "Mumble").WithLocation(11, 9),
                Diagnostic(MemberReferenceAnalyzer.EventReferenceDescriptor.Id, "Mumble").WithLocation(12, 20)
                );
        }

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
}

class D
{
    object OField = 33;
    object SField = ""Zap"";
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
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "this").WithLocation(36, 13),
                Diagnostic(BoxingOperationAnalyzer.BoxingDescriptor.Id, "33").WithLocation(45, 21)
                );
        }

        [Fact]
        public void BadStuffCSharp()
        {
            const string source = @"
class C
{
    public void M1(int z)
    {
        Framitz();
        int x = Bexley();
        int y = 10;
        double d = 20;
        M1(y + d);
        goto;
    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new BadStuffTestAnalyzer() }, null, null, false,
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Framitz()").WithLocation(6, 9),
                Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "Framitz").WithLocation(6, 9),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Framitz").WithLocation(6, 9),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Bexley()").WithLocation(7, 17),
                Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "Bexley").WithLocation(7, 17),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "Bexley").WithLocation(7, 17),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "M1(y + d)").WithLocation(10, 9),
                Diagnostic(BadStuffTestAnalyzer.InvalidStatementDescriptor.Id, "goto;").WithLocation(11, 9),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "goto;").WithLocation(11, 9),
                Diagnostic(BadStuffTestAnalyzer.InvalidExpressionDescriptor.Id, "").WithLocation(11, 13),
                Diagnostic(BadStuffTestAnalyzer.IsInvalidDescriptor.Id, "").WithLocation(11, 13)
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
        public void SwitchCSharp()
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

        switch (x)
        {
            case 1:
                break;
            case 1000:
                break;
        }

        switch (y) 
        {
            default:
                break;
        }

        switch (y) {}

        switch (x)
        {
            case :
                break;
            case 1000:
                break;
        }

    }
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics(Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(40, 20),
                Diagnostic(ErrorCode.ERR_ConstantExpected, ":").WithLocation(44, 18))
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new SwitchTestAnalyzer() }, null, null, false,
                Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "y").WithLocation(16, 17),
                Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "x").WithLocation(26, 17),
                Diagnostic(SwitchTestAnalyzer.NoDefaultSwitchDescriptor.Id, "x").WithLocation(26, 17),
                Diagnostic(SwitchTestAnalyzer.OnlyDefaultSwitchDescriptor.Id, "y").WithLocation(34, 17),
                Diagnostic(SwitchTestAnalyzer.SparseSwitchDescriptor.Id, "y").WithLocation(40, 17),
                Diagnostic(SwitchTestAnalyzer.NoDefaultSwitchDescriptor.Id, "y").WithLocation(40, 17));
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
        public void SymbolCouldHaveMoreSpecificTypeCSharp()
        {
            const string source = @"
class C
{
    public void M0()
    {
        object a = new Middle();
        object b = new Value(10);
        object c = new Middle();
        c = new Base();
        Base d = new Derived();
        Base e = new Derived();
        e = new Middle();
        Base f = new Middle();
        f = new Base();
        object g = new Derived();
        g = new Base();
        g = new Middle();
        var h = new Middle();
        h = new Derived();
        object i = 3;
        object j;
        j = 10;
        j = 10.1;
        Middle k = new Derived();
        Middle l = new Derived();
        object o = new Middle();
        M(out l, ref o);

        IBase1 ibase1 = null;
        IBase2 ibase2 = null;
        IMiddle imiddle = null;
        IDerived iderived = null;

        object ia = imiddle;
        object ic = imiddle;
        ic = ibase1;
        IBase1 id = iderived;
        IBase1 ie = iderived;
        ie = imiddle;
        IBase1 iff = imiddle;
        iff = ibase1;
        object ig = iderived;
        ig = ibase1;
        ig = imiddle;
        var ih = imiddle;
        ih = iderived;
        IMiddle ik = iderived;
        IMiddle il = iderived;
        object io = imiddle;
        IM(out il, ref io);
        IBase2 im = iderived;
        object isink = ibase2;
        isink = 3;
    }

    object fa = new Middle();
    object fb = new Value(10);
    object fc = new Middle();
    Base fd = new Derived();
    Base fe = new Derived();
    Base ff = new Middle();
    object fg = new Derived();
    Middle fh = new Middle();
    object fi = 3;
    object fj;
    Middle fk = new Derived();
    Middle fl = new Derived();
    object fo = new Middle();

    static IBase1 fibase1 = null;
    static IBase2 fibase2 = null;
    static IMiddle fimiddle = null;
    static IDerived fiderived = null;

    object fia = fimiddle;
    object fic = fimiddle;
    IBase1 fid = fiderived;
    IBase1 fie = fiderived;
    IBase1 fiff = fimiddle;
    object fig = fiderived;
    IMiddle fih = fimiddle;
    IMiddle fik = fiderived;
    IMiddle fil = fiderived;
    object fio = fimiddle;
    object fisink = fibase2;
    IBase2 fim = fiderived;

    void M1()
    {
        fc = new Base();
        fe = new Middle();
        ff = new Base();
        fg = new Base();
        fg = new Middle();
        fh = new Derived();
        fj = 10;
        fj = 10.1;
        M(out fl, ref fo);

        fic = fibase1;
        fie = fimiddle;
        fiff = fibase1;
        fig = fibase1;
        fig = fimiddle;
        fih = fiderived;
        IM(out fil, ref fio);
        fisink = 3;
    }

    void M(out Middle p1, ref object p2)
    {
        p1 = new Middle();
        p2 = null;
    }

    void IM(out IMiddle p1, ref object p2)
    {
        p1 = null;
        p2 = null;
    }
}

class Base
{
}

class Middle : Base
{
}

class Derived : Middle
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

interface IBase1
{
}

interface IBase2
{
}

interface IMiddle : IBase1
{
}

interface IDerived : IMiddle, IBase2
{
}

";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new SymbolCouldHaveMoreSpecificTypeAnalyzer() }, null, null, false,
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "a").WithArguments("a", "Middle").WithLocation(6, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "b").WithArguments("b", "Value").WithLocation(7, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "c").WithArguments("c", "Base").WithLocation(8, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "d").WithArguments("d", "Derived").WithLocation(10, 14),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "e").WithArguments("e", "Middle").WithLocation(11, 14),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "g").WithArguments("g", "Base").WithLocation(15, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "i").WithArguments("i", "int").WithLocation(20, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "k").WithArguments("k", "Derived").WithLocation(24, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ia").WithArguments("ia", "IMiddle").WithLocation(34, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ic").WithArguments("ic", "IBase1").WithLocation(35, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "id").WithArguments("id", "IDerived").WithLocation(37, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ie").WithArguments("ie", "IMiddle").WithLocation(38, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ig").WithArguments("ig", "IBase1").WithLocation(42, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "ik").WithArguments("ik", "IDerived").WithLocation(47, 17),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.LocalCouldHaveMoreSpecificTypeDescriptor.Id, "im").WithArguments("im", "IDerived").WithLocation(51, 16),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fa").WithArguments("C.fa", "Middle").WithLocation(56, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fb").WithArguments("C.fb", "Value").WithLocation(57, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fc").WithArguments("C.fc", "Base").WithLocation(58, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fd").WithArguments("C.fd", "Derived").WithLocation(59, 10),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fe").WithArguments("C.fe", "Middle").WithLocation(60, 10),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fg").WithArguments("C.fg", "Base").WithLocation(62, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fi").WithArguments("C.fi", "int").WithLocation(64, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fk").WithArguments("C.fk", "Derived").WithLocation(66, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fia").WithArguments("C.fia", "IMiddle").WithLocation(75, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fic").WithArguments("C.fic", "IBase1").WithLocation(76, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fid").WithArguments("C.fid", "IDerived").WithLocation(77, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fie").WithArguments("C.fie", "IMiddle").WithLocation(78, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fig").WithArguments("C.fig", "IBase1").WithLocation(80, 12),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fik").WithArguments("C.fik", "IDerived").WithLocation(82, 13),
                Diagnostic(SymbolCouldHaveMoreSpecificTypeAnalyzer.FieldCouldHaveMoreSpecificTypeDescriptor.Id, "fim").WithArguments("C.fim", "IDerived").WithLocation(86, 12)
                );
        }

        [Fact]
        public void ValueContextsCSharp()
        {
            const string source = @"
class C
{
    public void M0(int a = 16, int b = 17, int c = 18)
    {
    }

    public int f1 = 16;
    public int f2 = 17;
    public int f3 = 18;

    public void M1()
    {
        M0(16, 17, 18);
        M0(f1, f2, f3);
        M0();
    }
}

enum E
{
    A = 16,
    B,
    C = 17,
    D = 18
}
";
            CreateCompilationWithMscorlib45(source)
            .VerifyDiagnostics()
            .VerifyAnalyzerDiagnostics(new DiagnosticAnalyzer[] { new SeventeenTestAnalyzer() }, null, null, false,
                Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(4, 40),
                Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(9, 21),
                Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(14, 16),
                Diagnostic(SeventeenTestAnalyzer.SeventeenDescriptor.Id, "17").WithLocation(24, 9)
                );
        }
    }
}

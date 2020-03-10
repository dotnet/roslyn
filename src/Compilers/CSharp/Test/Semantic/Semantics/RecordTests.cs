// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(source, parseOptions: TestOptions.RegularPreview);

        private CompilationVerifier CompileAndVerify(CSharpTestSource src, string? expectedOutput = null)
            => base.CompileAndVerify(
                src,
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.RegularPreview,
                options: TestOptions.ReleaseExe,
                // init-only is unverifiable
                verify: Verification.Skipped);

        [Fact]
        public void RecordLanguageVersion()
        {
            var src1 = @"
class Point(int x, int y);
";
            var src2 = @"
data class Point { }
";
            var src3 = @"
data class Point(int x, int y);
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (2,12): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("records").WithLocation(2, 12),
                // (2,12): error CS8761: Records must have both a 'data' modifier and parameter list
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "(int x, int y)").WithLocation(2, 12),
                // (2,26): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ";").WithArguments("records").WithLocation(2, 26)
            );
            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("records").WithLocation(2, 1),
                // (2,12): error CS8761: Records must have both a 'data' modifier and parameter list
                // data class Point { }
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "Point").WithLocation(2, 12)
            );
            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (2,1): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("records").WithLocation(2, 1),
                // (2,17): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("records").WithLocation(2, 17),
                // (2,31): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // data class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ";").WithArguments("records").WithLocation(2, 31)
            );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (2,12): error CS8761: Records must have both a 'data' modifier and parameter list
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "(int x, int y)").WithLocation(2, 12)
            );
            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics(
                // (2,12): error CS8761: Records must have both a 'data' modifier and parameter list
                // data class Point { }
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "Point").WithLocation(2, 12)
            );
            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_01()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
1
2");
        }

        [Fact]
        public void RecordProperties_02()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public C(int a, int b)
    {
    }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,13): error CS8762: There cannot be a primary constructor and a member constructor with the same parameter types.
                // data class C(int X, int Y)
                Diagnostic(ErrorCode.ERR_DuplicateRecordConstructor, "(int X, int Y)").WithLocation(3, 13)
            );
        }

        [Fact]
        public void RecordProperties_03()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public int X { get; }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
0
2");
        }

        [Fact]
        public void RecordProperties_04()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public int X { get; } = 3;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
3
2");
        }

        [Fact]
        public void RecordProperties_05()
        {
            var src = @"
data class C(int X, int X)
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,25): error CS0100: The parameter name 'X' is a duplicate
                // data class C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "X").WithArguments("X").WithLocation(2, 25),
                // (2,25): error CS0102: The type 'C' already contains a definition for 'X'
                // data class C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 25)
            );
        }

        [Fact]
        public void RecordProperties_06()
        {
            var src = @"
data class C(int X)
{
    public void get_X() {}
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,18): error CS0082: Type 'C' already reserves a member called 'get_X' with the same parameter types
                // data class C(int X)
                Diagnostic(ErrorCode.ERR_MemberReserved, "X").WithArguments("get_X", "C").WithLocation(2, 18)
            );
        }

        [Fact]
        public void RecordProperties_07()
        {
            var comp = CreateCompilation(@"
data class C1(object P, object get_P);
data class C2(object get_P, object P);");
            comp.VerifyDiagnostics(
                // (2,22): error CS0102: The type 'C1' already contains a definition for 'get_P'
                // data class C1(object P, object get_P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C1", "get_P").WithLocation(2, 22),
                // (3,36): error CS0102: The type 'C2' already contains a definition for 'get_P'
                // data class C2(object get_P, object P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C2", "get_P").WithLocation(3, 36)
            );
        }

        [Fact]
        public void RecordProperties_08()
        {
            var comp = CreateCompilation(@"
data class C1(object O1)
{
    public object O1 { get; } = O1;
    public object O2 { get; } = O1;
}");
            // PROTOTYPE: primary ctor parameters not currently in scope
            comp.VerifyDiagnostics(
                // (4,33): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C1.O1'
                //     public object O1 { get; } = O1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "O1").WithArguments("C1.O1").WithLocation(4, 33),
                // (5,33): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C1.O1'
                //     public object O2 { get; } = O1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "O1").WithArguments("C1.O1").WithLocation(5, 33)
            );
        }

        [Fact]
        public void RecordEquals_01()
        {
            CompileAndVerify(@"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        Console.WriteLine(c.Equals(c));
    }
    public bool Equals(C c) => throw null;
    public override bool Equals(object o) => false;
}", expectedOutput: "False");
        }

        [Fact]
        public void RecordEquals_02()
        {
            CompileAndVerify(@"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(1, 1);
        var c2 = new C(1, 1);
        Console.WriteLine(c.Equals(c));
        Console.WriteLine(c.Equals(c2));
    }
}", expectedOutput: @"True
True");
        }

        [Fact]
        public void RecordEquals_03()
        {
            var verifier = CompileAndVerify(@"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        var c2 = new C(0, 0);
        var c3 = new C(1, 1);
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
    public bool Equals(C c) => X == c.X && Y == c.Y;
}", expectedOutput: @"True
False");
            verifier.VerifyIL("C.Equals(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  call       ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""int C.X.get""
  IL_0006:  ldarg.1
  IL_0007:  callvirt   ""int C.X.get""
  IL_000c:  bne.un.s   IL_001d
  IL_000e:  ldarg.0
  IL_000f:  call       ""int C.Y.get""
  IL_0014:  ldarg.1
  IL_0015:  callvirt   ""int C.Y.get""
  IL_001a:  ceq
  IL_001c:  ret
  IL_001d:  ldc.i4.0
  IL_001e:  ret
}");
        }

        [Fact]
        public void RecordEquals_04()
        {
            var verifier = CompileAndVerify(@"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        object c = new C(0, 0);
        var c2 = new C(0, 0);
        var c3 = new C(1, 1);
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"True
False");
            verifier.VerifyIL("C.Equals(object)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  call       ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0034
  IL_0003:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""int C.<X>k__BackingField""
  IL_000e:  ldarg.1
  IL_000f:  ldfld      ""int C.<X>k__BackingField""
  IL_0014:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0019:  brfalse.s  IL_0032
  IL_001b:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0020:  ldarg.0
  IL_0021:  ldfld      ""int C.<Y>k__BackingField""
  IL_0026:  ldarg.1
  IL_0027:  ldfld      ""int C.<Y>k__BackingField""
  IL_002c:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0031:  ret
  IL_0032:  ldc.i4.0
  IL_0033:  ret
  IL_0034:  ldc.i4.0
  IL_0035:  ret
}");
        }

        [Fact]
        public void RecordEquals_06()
        {
            var verifier = CompileAndVerify(@"
using System;
data class C(int X, int Y)
{
    public static void Main()
    {
        var c = new C(0, 0);
        object c2 = null;
        C c3 = null;
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"False
False");
        }

        [Fact]
        public void RecordEquals_07()
        {
            var verifier = CompileAndVerify(@"
using System;
data class C(int[] X, string Y)
{
    public static void Main()
    {
        var arr = new[] {1, 2};
        var c = new C(arr, ""abc"");
        var c2 = new C(new[] {1, 2}, ""abc"");
        var c3 = new C(arr, ""abc"");
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c.Equals(c3));
    }
}", expectedOutput: @"False
True");
        }

        [Fact]
        public void EmptyRecord()
        {
            var src = @"
data class C(); ";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,13): error CS8770: Records must have both a 'data' modifier and non-empty parameter list
                // data class C(); 
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "()").WithLocation(2, 13)
            );
        }

        [Fact]
        public void WithExpr1()
        {
            var src = @"
data class C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        _ = Main() with { };
        _ = default with { };
        _ = null with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,13): error CS8802: The receiver of a `with` expression must have a valid non-void type.
                //         _ = Main() with { };
                Diagnostic(ErrorCode.ERR_InvalidWithReceiverType, "Main()").WithLocation(7, 13),
                // (8,13): error CS8716: There is no target type for the default literal.
                //         _ = default with { };
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(8, 13),
                // (9,13): error CS8802: The receiver of a `with` expression must have a valid non-void type.
                //         _ = null with { };
                Diagnostic(ErrorCode.ERR_InvalidWithReceiverType, "null").WithLocation(9, 13)
            );
        }

        [Fact]
        public void WithExpr2()
        {
            var src = @"
data class C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,13): error CS8803: The 'with' expression requires the receiver type 'C' to have a single accessible non-inherited instance method named "Clone".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(7, 13)
            );
        }

        [Fact]
        public void WithExpr3()
        {
            var src = @"
data class C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }

    public C Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr4()
        {
            var src = @"
class B
{
    public B Clone() => null;
}
data class C(int X) : B
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }

    public new C Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr6()
        {
            var src = @"
class B
{
    public int X { get; }
    public B Clone() => null;
}
data class C(int X) : B
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'C' to have a single accessible non-inherited instance method named "With".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(12, 13)
            );
        }

        [Fact]
        public void WithExpr7()
        {
            var src = @"
class B
{
    public int X { get; }
    public virtual B Clone() => null;
}
data class C(int X) : B
{
    public static void Main()
    {
        var c = new C(0);
        B b = c;
        b = b with { X = 0 };
        var b2 = c with { X = 0 };
    }
    public override B Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,22): error CS8808: All arguments to a `with` expression must be compiler-generated record properties.
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(13, 22)
            );
        }

        [Fact]
        public void WithExpr8()
        {
            var src = @"
class B
{
    public int X { get; }
    public virtual B Clone() => null;
}
data class C(int X) : B
{
    public string Y { get; }
    public static void Main()
    {
        var c = new C(0);
        B b = c;
        b = b with { };
        b = c with { };
    }
    public override B Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr9()
        {
            var src = @"
data class C(int X)
{
    public string Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS8804: The type of the 'with' expression receiver, 'C', does not derive from the return type of the 'Clone' method, 'string'.
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_ContainingTypeMustDeriveFromWithReturnType, "c").WithArguments("C", "string").WithLocation(8, 13)
            );
        }

        [Fact]
        public void WithExpr11()
        {
            var src = @"
data class C(int X)
{
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = """"};
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = ""};
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExpr12()
        {
            var src = @"
using System;
data class C(int X)
{
    public C Clone() => new C(this.X);
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine(c.X);
        c = c with { X = 5 };
        Console.WriteLine(c.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0
5");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""C..ctor(int)""
  IL_0006:  dup
  IL_0007:  callvirt   ""int C.X.get""
  IL_000c:  call       ""void System.Console.WriteLine(int)""
  IL_0011:  callvirt   ""C C.Clone()""
  IL_0016:  dup
  IL_0017:  ldc.i4.5
  IL_0018:  stfld      ""int C.<X>k__BackingField""
  IL_001d:  callvirt   ""int C.X.get""
  IL_0022:  call       ""void System.Console.WriteLine(int)""
  IL_0027:  ret
}");
        }

        [Fact]
        public void WithExpr13()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public C Clone() => new C(X, Y);
    public override string ToString() => X + "" "" + Y;
    public static void Main()
    {
        var c = new C(0, 1);
        Console.WriteLine(c);
        c = c with { X = 5 };
        Console.WriteLine(c);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0 1
5 1");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""C..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  call       ""void System.Console.WriteLine(object)""
  IL_000d:  callvirt   ""C C.Clone()""
  IL_0012:  dup
  IL_0013:  ldc.i4.5
  IL_0014:  stfld      ""int C.<X>k__BackingField""
  IL_0019:  call       ""void System.Console.WriteLine(object)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void WithExpr14()
        {
            var src = @"
using System;
data class C(int X, int Y)
{
    public C Clone() => new C(this.X, this.Y);
    public override string ToString() => X + "" "" + Y;
    public static void Main()
    {
        var c = new C(0, 1);
        Console.WriteLine(c);
        c = c with { X = 5 };
        Console.WriteLine(c);
        c = c with { Y = 2 };
        Console.WriteLine(c);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0 1
5 1
5 2");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""C..ctor(int, int)""
  IL_0007:  dup
  IL_0008:  call       ""void System.Console.WriteLine(object)""
  IL_000d:  callvirt   ""C C.Clone()""
  IL_0012:  dup
  IL_0013:  ldc.i4.5
  IL_0014:  stfld      ""int C.<X>k__BackingField""
  IL_0019:  dup
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  callvirt   ""C C.Clone()""
  IL_0024:  dup
  IL_0025:  ldc.i4.2
  IL_0026:  stfld      ""int C.<Y>k__BackingField""
  IL_002b:  call       ""void System.Console.WriteLine(object)""
  IL_0030:  ret
}");
        }

        [Fact]
        public void WithExpr15()
        {
            var src = @"
data class C(int X, int Y)
{
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { = 5 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,22): error CS1525: Invalid expression term '='
                //         c = c with { = 5 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(8, 22)
            );
        }

        [Fact]
        public void WithExpr16()
        {
            var src = @"
data class C(int X, int Y)
{
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { X = };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS1525: Invalid expression term '}'
                //         c = c with { X = };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExpr17()
        {
            var src = @"
class B
{
    public int X { get; }
    private B Clone() => null;
}
data class C(int X) : B
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "With".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
        }

        [Fact]
        public void WithExpr18()
        {
            var src = @"
class B
{
    public int X { get; }
    protected B Clone() => null;
}
data class C(int X) : B
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr19()
        {
            var src = @"
class B
{
    public int X { get; }
    protected B Clone() => null;
}
data class C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "With".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
        }

        [Fact]
        public void WithExpr20()
        {
            var src = @"
using System;
class C
{
    public event Action X;
    public C Clone() => null;
    public static void Main()
    {
        var c = new C();
        c = c with { X = null };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,25): warning CS0067: The event 'C.X' is never used
                //     public event Action X;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "X").WithArguments("C.X").WithLocation(5, 25),
                // (10,22): error CS8808: All arguments to a `with` expression must be compiler-generated record properties.
                //         c = c with { X = null };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(10, 22)
            );
        }

        [Fact]
        public void WithExpr21()
        {
            var src = @"
class B
{
    public class X { }
    public B Clone() => null;
}
data class C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,22): error CS8808: All arguments to a `with` expression must be compiler-generated record properties.
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(12, 22)
            );
        }

        [Fact]
        public void WithExpr22()
        {
            var src = @"
class B
{
    public int X = 0;
    public B Clone() => null;
}
data class C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr23()
        {
            var src = @"
class B
{
    public int X = 0;
    public B Clone() => null;
}
data class C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { Y = 2 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,22): error CS1061: 'B' does not contain a definition for 'Y' and no accessible extension method 'Y' accepting a first argument of type 'B' could be found (are you missing a using directive or an assembly reference?)
                //         b = b with { Y = 2 };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Y").WithArguments("B", "Y").WithLocation(12, 22)
            );
        }

        [Fact]
        public void WithExprNestedErrors()
        {
            var src = @"
data class C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = """"-3 };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,13): error CS8803: The 'with' expression requires the receiver type 'C' to have a single accessible non-inherited instance method named "With".
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(7, 13),
                // (7,26): error CS0019: Operator '-' cannot be applied to operands of type 'string' and 'int'
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"""""-3").WithArguments("-", "string", "int").WithLocation(7, 26)
            );
        }

        [Fact]
        public void WithExprNoExpressionToPropertyTypeConversion()
        {
            var src = @"
data class C(int X)
{
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = """" };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = "" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact]
        public void WithExprPropertyInaccessibleGet()
        {
            var src = @"
data class C(int X)
{
    public int X { private get; set; }
    public C Clone() => null;
}
class D
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,22): error CS8808: All arguments to a `with` expression must be compiler-generated record properties.
                //         c = c with { X = 0 };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(12, 22)
            );
        }

        [Fact]
        public void WithExprSideEffects1()
        {
            var src = @"
using System;
data class C(int X, int Y, int Z)
{
    public C Clone() => new C(X, Y, Z);
    public static void Main()
    {
        var c = new C(0, 1, 2);
        c = c with { Y = W(""Y""), X = W(""X"") };
    }

    public static int W(string s)
    {
        Console.WriteLine(s);
        return 0;
    }
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
Y
X");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  ldc.i4.2
  IL_0003:  newobj     ""C..ctor(int, int, int)""
  IL_0008:  callvirt   ""C C.Clone()""
  IL_000d:  dup
  IL_000e:  ldstr      ""Y""
  IL_0013:  call       ""int C.W(string)""
  IL_0018:  stfld      ""int C.<Y>k__BackingField""
  IL_001d:  dup
  IL_001e:  ldstr      ""X""
  IL_0023:  call       ""int C.W(string)""
  IL_0028:  stfld      ""int C.<X>k__BackingField""
  IL_002d:  pop
  IL_002e:  ret
}");
        }

        [Fact]
        public void WithExprConversions1()
        {
            var src = @"
using System;
data class C(long X)
{
    public C Clone() => new C(X);
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine((c with { X = 11 }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: "11");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       32 (0x20)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""C..ctor(long)""
  IL_0007:  callvirt   ""C C.Clone()""
  IL_000c:  dup
  IL_000d:  ldc.i4.s   11
  IL_000f:  conv.i8
  IL_0010:  stfld      ""long C.<X>k__BackingField""
  IL_0015:  callvirt   ""long C.X.get""
  IL_001a:  call       ""void System.Console.WriteLine(long)""
  IL_001f:  ret
}");
        }

        [Fact]
        public void WithExprConversions2()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static implicit operator long(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
data class C(long X)
{
    public C Clone() => new C(X);
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
11");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  newobj     ""C..ctor(long)""
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.s   11
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  callvirt   ""C C.Clone()""
  IL_0015:  dup
  IL_0016:  ldloc.0
  IL_0017:  call       ""long S.op_Implicit(S)""
  IL_001c:  stfld      ""long C.<X>k__BackingField""
  IL_0021:  callvirt   ""long C.X.get""
  IL_0026:  call       ""void System.Console.WriteLine(long)""
  IL_002b:  ret
}");
        }

        [Fact]
        public void WithExprConversions3()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static explicit operator int(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
data class C(long X)
{
    public C Clone() => new C(X);
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = (int)s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
11");
        }

        [Fact]
        public void WithExprConversions4()
        {
            var src = @"
using System;
struct S
{
    private int _i;
    public S(int i)
    {
        _i = i;
    }
    public static explicit operator long(S s) => s._i;
}
data class C(long X)
{
    public C Clone() => new C(X);
    public static void Main()
    {
        var c = new C(0);
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (19,41): error CS0266: Cannot implicitly convert type 'S' to 'long'. An explicit conversion exists (are you missing a cast?)
                //         Console.WriteLine((c with { X = s }).X);
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "s").WithArguments("S", "long").WithLocation(19, 41)
            );
        }

        [Fact]
        public void WithExprConversions5()
        {
            var src = @"
using System;
data class C(object X)
{
    public C Clone() => new C(X);
    public static void Main()
    {
        var c = new C(0);
        Console.WriteLine((c with { X = ""abc"" }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: "abc");
        }

        [Fact]
        public void WithExprStaticProperty()
        {
            var src = @"
data class C(int X)
{
    public static int X { get; }
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,22): error CS8808: All arguments to a `with` expression must be instance properties or fields.
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(10, 22)
            );
        }

        [Fact]
        public void WithExprMethodAsArgument()
        {
            var src = @"
class C
{
    public int X() => 0;
    public C Clone() => null;
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,22): error CS8808: All arguments to a `with` expression must be compiler-generated record properties.
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_WithMemberIsNotRecordProperty, "X").WithLocation(10, 22)
            );
        }

        [Fact]
        public void WithExprStaticWithMethod()
        {
            var src = @"
data class C(int X)
{
    public static C Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS8803: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(8, 13),
                // (9,13): error CS8803: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(9, 13)
            );
        }
 
        [Fact]
        public void WithExprStaticWithMethod2()
        {
            var src = @"
class B
{
    public B Clone() => null;
}
data class C(int X) : B
{
    public static new C Clone() => null;
    public static void Main()
    {
        var c = new C(0);
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(12, 13),
                // (13,13): error CS8803: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(13, 13)
            );
        }
    }
}
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(new[] { source, IsExternalInitTypeDefinition },
                parseOptions: TestOptions.RegularPreview);

        private CompilationVerifier CompileAndVerify(
            CSharpTestSource src,
            string? expectedOutput = null,
            IEnumerable<MetadataReference>? references = null)
            => base.CompileAndVerify(
                new[] { src, IsExternalInitTypeDefinition },
                expectedOutput: expectedOutput,
                parseOptions: TestOptions.RegularPreview,
                references: references,
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
                // (2,12): error CS8800: A positional record must have both a 'data' modifier and non-empty parameter list
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
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "data").WithArguments("records").WithLocation(2, 1)
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
                // (2,12): error CS8800: A positional record must have both a 'data' modifier and non-empty parameter list
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "(int x, int y)").WithLocation(2, 12)
            );
            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics(
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
        public void StructRecord1()
        {
            var src = @"
data struct Point(int X, int Y);";

            var verifier = CompileAndVerify(src);
            verifier.VerifyIL("Point.Equals(object)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Point V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Point""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""Point""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool Point.Equals(in Point)""
  IL_0019:  ret
}");
            verifier.VerifyIL("Point.Equals(in Point)", @"
{
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int Point.<X>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int Point.<X>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_002f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""int Point.<Y>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002e:  ret
  IL_002f:  ldc.i4.0
  IL_0030:  ret
}");
        }

        [Fact]
        public void StructRecord2()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public static void Main()
    {
        var s1 = new S(0, 1);
        var s2 = new S(0, 1);
        Console.WriteLine(s1.X);
        Console.WriteLine(s1.Y);
        Console.WriteLine(s1.Equals(s2));
        Console.WriteLine(s1.Equals(new S(1, 0)));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"0
1
True
False");
        }

        [Fact]
        public void StructRecord3()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(S s) => false;
    public static void Main()
    {
        var s1 = new S(0, 1);
        Console.WriteLine(s1.Equals(s1));
        Console.WriteLine(s1.Equals(in s1));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"False
True");
            verifier.VerifyIL("S.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  3
  .locals init (S V_0) //s1
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""S..ctor(int, int)""
  IL_0009:  ldloca.s   V_0
  IL_000b:  ldloc.0
  IL_000c:  call       ""bool S.Equals(S)""
  IL_0011:  call       ""void System.Console.WriteLine(bool)""
  IL_0016:  ldloca.s   V_0
  IL_0018:  ldloca.s   V_0
  IL_001a:  call       ""bool S.Equals(in S)""
  IL_001f:  call       ""void System.Console.WriteLine(bool)""
  IL_0024:  ret
}");
        }

        [Fact]
        public void StructRecord4()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public override bool Equals(object o)
    {
        Console.WriteLine(""obj"");
        return true;
    }
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"obj
s");
        }

        [Fact]
        public void StructRecord5()
        {
            var src = @"
using System;
data struct S(int X, int Y)
{
    public bool Equals(in S s)
    {
        Console.WriteLine(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"s
s");
        }

        [Fact]
        public void StructRecordDefaultCtor()
        {
            const string src = @"
public data struct S(int X);";
            const string src2 = @"
class C
{
    public S M() => new S();
}";
            var comp = CreateCompilation(src + src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src);
            var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();
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
using System;
data class C(int X)
{
    public static void Main()
    {
        var c1 = new C(1);
        c1 = c1 with { };
        var c2 = c1 with { X = 11 };
        Console.WriteLine(c1.X);
        Console.WriteLine(c2.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"1
11");
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
    public int X { get; init; }
    public B Clone() => null;
}
class C : B
{
    public static void Main()
    {
        var c = new C();
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS0266: Cannot implicitly convert type 'B' to 'C'. An explicit conversion exists (are you missing a cast?)
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c with { }").WithArguments("B", "C").WithLocation(12, 13)
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
class C : B
{
    public new int X { get; init; }
    public static void Main()
    {
        var c = new C();
        B b = c;
        b = b with { X = 0 };
        var b2 = c with { X = 0 };
    }
    public override B Clone() => null;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,22): error CS0200: Property or indexer 'B.X' cannot be assigned to -- it is read only
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("B.X").WithLocation(14, 22),
                // (15,27): error CS0200: Property or indexer 'B.X' cannot be assigned to -- it is read only
                //         var b2 = c with { X = 0 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("B.X").WithLocation(15, 27)
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
  IL_0018:  callvirt   ""void C.X.init""
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
  IL_0014:  callvirt   ""void C.X.init""
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
  IL_0014:  callvirt   ""void C.X.init""
  IL_0019:  dup
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  callvirt   ""C C.Clone()""
  IL_0024:  dup
  IL_0025:  ldc.i4.2
  IL_0026:  callvirt   ""void C.Y.init""
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
                // (8,22): error CS1001: Identifier expected
                //         c = c with { = 5 };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(8, 22)
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
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "Clone".
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
            comp.VerifyDiagnostics(
                // (12,13): error CS8803: The receiver type 'B' does not have an accessible parameterless instance method named "Clone".
                //         b = b with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13)
            );
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
                // (12,13): error CS8803: The 'with' expression requires the receiver type 'B' to have a single accessible non-inherited instance method named "Clone".
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
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "X").WithArguments("C.X").WithLocation(5, 25)
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
                // (12,22): error CS0572: 'X': cannot reference a type through an expression; try 'B.X' instead
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_BadTypeReference, "X").WithArguments("X", "B.X").WithLocation(12, 22),
                // (12,22): error CS0118: 'B.X' is a type but is used like a variable
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_BadSKknown, "X").WithArguments("B.X", "type", "variable").WithLocation(12, 22)
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
class C
{
    public int X { get; init; }
    public static void Main()
    {
        var c = new C();
        c = c with { X = """"-3 };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(8, 13),
                // (8,26): error CS0019: Operator '-' cannot be applied to operands of type 'string' and 'int'
                //         c = c with { X = ""-3 };
                Diagnostic(ErrorCode.ERR_BadBinaryOps, @"""""-3").WithArguments("-", "string", "int").WithLocation(8, 26)
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
        public void WithExprPropertyInaccessibleSet()
        {
            var src = @"
class C
{
    public int X { get; private set; }
    public C Clone() => null;
}
class D
{
    public static void Main()
    {
        var c = new C();
        c = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,22): error CS0272: The property or indexer 'C.X' cannot be used in this context because the set accessor is inaccessible
                //         c = c with { X = 0 };
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "X").WithArguments("C.X").WithLocation(12, 22)
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
  IL_0018:  callvirt   ""void C.Y.init""
  IL_001d:  dup
  IL_001e:  ldstr      ""X""
  IL_0023:  call       ""int C.W(string)""
  IL_0028:  callvirt   ""void C.X.init""
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
  IL_0010:  callvirt   ""void C.X.init""
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
  IL_001c:  callvirt   ""void C.X.init""
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
        public void WithExprConversions6()
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
    public static implicit operator int(S s)
    {
        Console.WriteLine(""conversion"");
        return s._i;
    }
}
class C
{
    private readonly long _x;
    public long X { get => _x; init { Console.WriteLine(""set""); _x = value; } }
    public C Clone() => new C();
    public static void Main()
    {
        var c = new C();
        var s = new S(11);
        Console.WriteLine((c with { X = s }).X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
conversion
set
11");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (S V_0) //s
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  ldloca.s   V_0
  IL_0007:  ldc.i4.s   11
  IL_0009:  call       ""S..ctor(int)""
  IL_000e:  callvirt   ""C C.Clone()""
  IL_0013:  dup
  IL_0014:  ldloc.0
  IL_0015:  call       ""int S.op_Implicit(S)""
  IL_001a:  conv.i8
  IL_001b:  callvirt   ""void C.X.init""
  IL_0020:  callvirt   ""long C.X.get""
  IL_0025:  call       ""void System.Console.WriteLine(long)""
  IL_002a:  ret
}");
        }

        [Fact]
        public void WithExprStaticProperty()
        {
            var src = @"
class C
{
    public static int X { get; }
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
                // (10,22): error CS0176: Member 'C.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("C.X").WithLocation(10, 22)
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
                // (10,22): error CS1656: Cannot assign to 'X' because it is a 'method group'
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "X = 11").WithArguments("X", "method group").WithLocation(10, 22)
            );
        }

        [Fact]
        public void WithExprStaticWithMethod()
        {
            var src = @"
class C
{
    public int X = 0;
    public static C Clone() => null;
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(9, 13),
                // (10,13): error CS8808: The receiver type 'C' does not have an accessible parameterless instance method named "Clone".
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "c").WithArguments("C").WithLocation(10, 13)
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
class C : B
{
    public int X = 0;
    public static new C Clone() => null; // static
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,13): error CS0266: Cannot implicitly convert type 'B' to 'C'. An explicit conversion exists (are you missing a cast?)
                //         c = c with { };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c with { }").WithArguments("B", "C").WithLocation(13, 13),
                // (14,13): error CS0266: Cannot implicitly convert type 'B' to 'C'. An explicit conversion exists (are you missing a cast?)
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "c with { X = 11 }").WithArguments("B", "C").WithLocation(14, 13),
                // (14,22): error CS1061: 'B' does not contain a definition for 'X' and no accessible extension method 'X' accepting a first argument of type 'B' could be found (are you missing a using directive or an assembly reference?)
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "X").WithArguments("B", "X").WithLocation(14, 22)
            );
        }

        [Fact]
        public void WithExprBadMemberBadType()
        {
            var src = @"
class C
{
    public C Clone() => null;
    public int X { get; init; }
    public static void Main()
    {
        var c = new C();
        c = c with { X = ""a"" };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = "a" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""a""").WithArguments("string", "int").WithLocation(9, 26)
            );
        }

        [Fact]
        public void WithExprCloneReturnDifferent()
        {
            var src = @"
class B
{ 
    public int X { get; init; }
}
class C : B
{
    public B Clone() => new B();
    public static void Main()
    {
        var c = new C();
        var b = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExprNotRecord()
        {
            var src = @"
using System;
class C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;
    public event Action E;
    
    public C Clone() => new C {
            X = this.X,
            Y = this.Y,
            Z = this.Z,
            E = this.E,
    };

    public static void Main()
    {
        var c = new C() { X = 1, Y = ""2"", Z = 3, E = () => { } };
        var c2 = c with {};
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c2.X);
        Console.WriteLine(c2.Y);
        Console.WriteLine(c2.Z);
        Console.WriteLine(ReferenceEquals(c.E, c2.E));
        var c3 = c with { Y = ""3"", X = 2 };
        Console.WriteLine(c.Y);
        Console.WriteLine(c3.Y);
        Console.WriteLine(c.X);
        Console.WriteLine(c3.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
False
1
2
3
True
2
3
1
2");
        }

        [Fact]
        public void WithExprNotRecord2()
        {
            var comp1 = CreateCompilation(@"
public class C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;
    
    public C Clone() => new C {
            X = this.X,
            Y = this.Y,
            Z = this.Z,
    };
}");
            comp1.VerifyDiagnostics();

            var verifier = CompileAndVerify(@"
class D
{
    public C M(C c) => c with
    {
        X = 5,
        Y = ""a"",
        Z = 2,
    };
}", references: new[] { comp1.EmitToImageReference() });

            verifier.VerifyIL("D.M", @"
{
  // Code size       33 (0x21)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""C C.Clone()""
  IL_0006:  dup
  IL_0007:  ldc.i4.5
  IL_0008:  callvirt   ""void C.X.set""
  IL_000d:  dup
  IL_000e:  ldstr      ""a""
  IL_0013:  callvirt   ""void C.Y.init""
  IL_0018:  dup
  IL_0019:  ldc.i4.2
  IL_001a:  conv.i8
  IL_001b:  stfld      ""long C.Z""
  IL_0020:  ret
}");
        }

        [Fact]
        public void NominalRecordWith()
        {
            var src = @"
using System;
data class C
{
    public int X { get; init; }
    public string Y;
    public int Z { get; set; }

    public static void Main()
    {
        var c = new C() { X = 1, Y = ""2"", Z = 3 };
        var c2 = new C() { X = 1, Y = ""2"", Z = 3 };
        Console.WriteLine(c.Equals(c2));
        var c3 = c2 with { X = 3, Y = ""2"", Z = 1 };
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(c3.Equals(c2));
        Console.WriteLine(c2.X + "" "" + c2.Y + "" "" + c2.Z);
    }
}";
            CompileAndVerify(src, expectedOutput: @"
True
True
False
1 2 3");
        }

        [Fact]
        public void WithSemanticModel1()
        {
            var src = @"
data class C(int X, string Y)
{
    public static void Main()
    {
        var c = new C(0, ""a"");
        c = c with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);
            var withExpr = root.DescendantNodes().OfType<WithExpressionSyntax>().Single();
            var typeInfo = model.GetTypeInfo(withExpr);
            var c = comp.GlobalNamespace.GetTypeMember("C");
            Assert.True(c.ISymbol.Equals(typeInfo.Type));

            var x = c.GetMembers("X").Single();
            var xId = withExpr.DescendantNodes().Single(id => id.ToString() == "X");
            var symbolInfo = model.GetSymbolInfo(xId);
            Assert.True(x.ISymbol.Equals(symbolInfo.Symbol));
        }
    }
}
﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
record Point { }
";
            var src3 = @"
record Point(int x, int y);
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,12): error CS1514: { expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS1513: } expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS8652: The feature 'top-level statements' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y);").WithArguments("top-level statements").WithLocation(2, 12),
                // (2,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 12),
                // (2,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 12),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 13),
                // (2,13): error CS0165: Use of unassigned local variable 'x'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 13),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 20),
                // (2,20): error CS0165: Use of unassigned local variable 'y'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 20)
            );
            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (2,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record Point { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 1),
                // (2,8): error CS0116: A namespace cannot directly contain members such as fields or methods
                // record Point { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Point").WithLocation(2, 8),
                // (2,8): error CS0548: '<invalid-global-code>.Point': property or indexer must have at least one accessor
                // record Point { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "Point").WithArguments("<invalid-global-code>.Point").WithLocation(2, 8)
            );
            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,1): error CS8652: The feature 'top-level statements' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "record Point(int x, int y);").WithArguments("top-level statements").WithLocation(2, 1),
                // (2,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 1),
                // (2,8): error CS8112: Local function 'Point(int, int)' must declare a body because it is not marked 'static extern'.
                // record Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "Point").WithArguments("Point(int, int)").WithLocation(2, 8),
                // (2,8): warning CS8321: The local function 'Point' is declared but never used
                // record Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Point").WithArguments("Point").WithLocation(2, 8)
            );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // error CS8805: Program using top-level statements must be an executable.
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable).WithLocation(1, 1),
                // (2,12): error CS1514: { expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS1513: } expected
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 12),
                // (2,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 12),
                // (2,12): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 12),
                // (2,13): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 13),
                // (2,13): error CS0165: Use of unassigned local variable 'x'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 13),
                // (2,20): error CS8185: A declaration is not allowed in this context.
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 20),
                // (2,20): error CS0165: Use of unassigned local variable 'y'
                // class Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 20)
            );
            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TestInExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;
public record C(int i)
{
    public static void M()
    {
        Expression<Func<C, C>> expr = c => c with { i = 5 };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,44): error CS8849: An expression tree may not contain a with-expression.
                //         Expression<Func<C, C>> expr = c => c with { i = 5 };
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsWithExpression, "c with { i = 5 }").WithLocation(8, 44)
                );
        }

        [Fact]
        public void PartialRecordMixedWithClass()
        {
            var src = @"
partial record C(int X, int Y)
{
}
partial class C
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,15): error CS0261: Partial declarations of 'C' must be all classes, all records, all structs, or all interfaces
                // partial class C
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C").WithArguments("C").WithLocation(5, 15)
                );
        }

        [Fact]
        public void RecordProperties_01()
        {
            var src = @"
using System;
record C(int X, int Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123");
            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  call       ""object..ctor()""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void RecordProperties_02()
        {
            var src = @"
using System;
record C(int X, int Y)
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
                // (3,9): error CS8851: There cannot be a primary constructor and a member constructor with the same parameter types.
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_DuplicateRecordConstructor, "(int X, int Y)").WithLocation(3, 9)
            );
        }

        [Fact]
        public void RecordProperties_03()
        {
            var src = @"
using System;
record C(int X, int Y)
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
record C(int X, int Y)
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
record C(int X, int X)
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,21): error CS0100: The parameter name 'X' is a duplicate
                // record C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "X").WithArguments("X").WithLocation(2, 21),
                // (2,21): error CS0102: The type 'C' already contains a definition for 'X'
                // record C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 21)
            );
        }

        [Fact]
        public void RecordProperties_06()
        {
            var src = @"
record C(int X, int Y)
{
    public void get_X() { }
    public void set_X() { }
    int get_Y(int value) => value;
    int set_Y(int value) => value;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,14): error CS0082: Type 'C' already reserves a member called 'get_X' with the same parameter types
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "X").WithArguments("get_X", "C").WithLocation(2, 14),
                // (2,21): error CS0082: Type 'C' already reserves a member called 'set_Y' with the same parameter types
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "Y").WithArguments("set_Y", "C").WithLocation(2, 21));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C C.<>Clone()",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 C.<X>k__BackingField",
                "System.Int32 C.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.X.init",
                "System.Int32 C.X { get; init; }",
                "System.Int32 C.<Y>k__BackingField",
                "System.Int32 C.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.Y.init",
                "System.Int32 C.Y { get; init; }",
                "void C.get_X()",
                "void C.set_X()",
                "System.Int32 C.get_Y(System.Int32 value)",
                "System.Int32 C.set_Y(System.Int32 value)",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? )",
                "System.Boolean C.Equals(C? )",
                "C..ctor(C )",
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void RecordProperties_07()
        {
            var comp = CreateCompilation(@"
record C1(object P, object get_P);
record C2(object get_P, object P);");
            comp.VerifyDiagnostics(
                // (2,18): error CS0102: The type 'C1' already contains a definition for 'get_P'
                // record C1(object P, object get_P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C1", "get_P").WithLocation(2, 18),
                // (3,32): error CS0102: The type 'C2' already contains a definition for 'get_P'
                // record C2(object get_P, object P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C2", "get_P").WithLocation(3, 32)
            );
        }

        [Fact]
        public void RecordProperties_08()
        {
            var comp = CreateCompilation(@"
record C1(object O1)
{
    public object O1 { get; } = O1;
    public object O2 { get; } = O1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_09()
        {
            var src =
@"record C(object P1, object P2, object P3, object P4)
{
    class P1 { }
    object P2 = 2;
    int P3(object o) => 3;
    int P4<T>(T t) => 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,17): error CS0102: The type 'C' already contains a definition for 'P1'
                // record C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(1, 17),
                // (1,28): error CS0102: The type 'C' already contains a definition for 'P2'
                // record C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P2").WithArguments("C", "P2").WithLocation(1, 28),
                // (1,39): error CS0102: The type 'C' already contains a definition for 'P3'
                // record C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(1, 39),
                // (1,50): error CS0102: The type 'C' already contains a definition for 'P4'
                // record C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P4").WithArguments("C", "P4").WithLocation(1, 50));
        }

        [Fact]
        public void RecordProperties_10()
        {
            var src =
@"record C(object P)
{
    const int P = 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,17): error CS0102: The type 'C' already contains a definition for 'P'
                // record C(object P)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(1, 17)
                );
        }

        [Fact]
        public void EmptyRecord()
        {
            var src = @"
record C(); ";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,9): error CS8850: A positional record must have both a 'data' modifier and non-empty parameter list
                // record C();
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "()").WithLocation(2, 9)
            );
        }

        [Fact(Skip = "record struct")]
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

        [Fact(Skip = "record struct")]
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

        [Fact(Skip = "record struct")]
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

        [Fact(Skip = "record struct")]
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

        [Fact(Skip = "record struct")]
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

        [Fact(Skip = "record struct")]
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
record C(int X)
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
record C(int X)
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
record C(int X)
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

            var root = comp.SyntaxTrees[0].GetRoot();
            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0)')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0)')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with { };')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with { }')
                  Left:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                  Right:
                    IInvocationOperation (virtual C C.<>Clone()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr4()
        {
            var src = @"
class B
{
    public B Clone() => null;
}
record C(int X) : B
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
record B
{
    public int X { get; init; }
}
record C : B
{
    public static void Main()
    {
        var c = new C();
        c = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr7()
        {
            var src = @"
record B
{
    public int X { get; }
    public virtual B Clone() => null;
}
record C : B
{
    public new int X { get; init; }
    public static void Main()
    {
        var c = new C();
        B b = c;
        b = b with { X = 0 };
        var b2 = c with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,22): error CS0200: Property or indexer 'B.X' cannot be assigned to -- it is read only
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "X").WithArguments("B.X").WithLocation(14, 22)
            );
        }

        [Fact]
        public void WithExpr8()
        {
            var src = @"
record B
{
    public int X { get; }
}
record C : B
{
    public string Y { get; }
    public static void Main()
    {
        var c = new C();
        B b = c;
        b = b with { };
        b = c with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr9()
        {
            var src = @"
record C(int X)
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
            );
        }

        [Fact]
        public void WithExpr11()
        {
            var src = @"
record C(int X)
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
record C(int X)
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
  IL_0011:  callvirt   ""C C.<>Clone()""
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
record C(int X, int Y)
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
  IL_000d:  callvirt   ""C C.<>Clone()""
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
record C(int X, int Y)
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
  IL_000d:  callvirt   ""C C.<>Clone()""
  IL_0012:  dup
  IL_0013:  ldc.i4.5
  IL_0014:  callvirt   ""void C.X.init""
  IL_0019:  dup
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  callvirt   ""C C.<>Clone()""
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
record C(int X, int Y)
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
record C(int X, int Y)
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr17()
        {
            var src = @"
record B
{
    public int X { get; }
    private B Clone() => null;
}
record C(int X) : B
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr18()
        {
            var src = @"
class B
{
    public int X { get; }
    protected B Clone() => null;
}
record C(int X) : B
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
record C(int X)
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
record C
{
    public event Action X;
    public static void Main()
    {
        var c = new C();
        c = c with { X = null };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
            );
        }

        [Fact]
        public void WithExpr21()
        {
            var src = @"
record B
{
    public class X { }
}
class C
{
    public static void Main()
    {
        var b = new B();
        b = b with { X = 0 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,22): error CS0572: 'X': cannot reference a type through an expression; try 'B.X' instead
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_BadTypeReference, "X").WithArguments("X", "B.X").WithLocation(11, 22),
                // (11,22): error CS1913: Member 'X' cannot be initialized. It is not a field or property.
                //         b = b with { X = 0 };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "X").WithArguments("X").WithLocation(11, 22)
            );
        }

        [Fact]
        public void WithExpr22()
        {
            var src = @"
record B
{
    public int X = 0;
}
class C
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
record C(int X)
{
    public static void Main()
    {
        var b = new B();
        b = b with { Y = 2 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS8858: The receiver type 'B' is not a valid record type.
                //         b = b with { Y = 2 };
                Diagnostic(ErrorCode.ERR_NoSingleCloneMethod, "b").WithArguments("B").WithLocation(12, 13),
                // (12,22): error CS0117: 'B' does not contain a definition for 'Y'
                //         b = b with { Y = 2 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Y").WithArguments("B", "Y").WithLocation(12, 22)
            );
        }

        [Fact]
        public void AccessibilityOfBaseCtor_01()
        {
            var src = @"
using System;

record Base
{
    protected Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}

    public static void Main()
    {
        var c = new C(1, 2);
    }
}

record C(int X, int Y) : Base(X, Y);
";
            CompileAndVerify(src, expectedOutput: @"
1
2
");
        }

        [Fact]
        public void AccessibilityOfBaseCtor_02()
        {
            var src = @"
using System;

record Base
{
    protected Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}

    public static void Main()
    {
        var c = new C(1, 2);
    }
}

record C(int X, int Y) : Base(X, Y) {}
";
            CompileAndVerify(src, expectedOutput: @"
1
2
");
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_03()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B(object P) : A;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_04()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B(object P) : A {}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_05()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B : A;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(44898, "https://github.com/dotnet/roslyn/issues/44898")]
        public void AccessibilityOfBaseCtor_06()
        {
            var src = @"
abstract record A
{
    protected A() {}
    protected A(A x) {}
};
record B : A {}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
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
record C(int X)
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
record C
{
    public int X { get; private set; }
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
                // (11,22): error CS0272: The property or indexer 'C.X' cannot be used in this context because the set accessor is inaccessible
                //         c = c with { X = 0 };
                Diagnostic(ErrorCode.ERR_InaccessibleSetter, "X").WithArguments("C.X").WithLocation(11, 22)
            );
        }

        [Fact]
        public void WithExprSideEffects1()
        {
            var src = @"
using System;
record C(int X, int Y, int Z)
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
  IL_0008:  callvirt   ""C C.<>Clone()""
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

            var comp = (CSharpCompilation)verifier.Compilation;
            var tree = comp.SyntaxTrees.First();
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);

            var withExpr1 = root.DescendantNodes().OfType<WithExpressionSyntax>().First();
            comp.VerifyOperationTree(withExpr1, @"
IWithOperation (OperationKind.With, Type: C) (Syntax: 'c with { Y  ...  = W(""X"") }')
  Value:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C.<>Clone()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ Y = W(""Y"" ...  = W(""X"") }')
      Initializers(2):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Y = W(""Y"")')
            Left:
              IPropertyReferenceOperation: System.Int32 C.Y { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Y')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'Y')
            Right:
              IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""Y"")')
                Instance Receiver:
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""Y""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Y"") (Syntax: '""Y""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = W(""X"")')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""X"")')
                Instance Receiver:
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""X""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""X"") (Syntax: '""X""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Skip(1).First();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0, 1, 2)')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0, 1, 2)')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X, System.Int32 Y, System.Int32 Z)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0, 1, 2)')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: '1')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Z) (OperationKind.Argument, Type: null) (Syntax: '2')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Value:
                    IInvocationOperation (virtual C C.<>Clone()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Y = W(""Y"")')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.Y { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'Y')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Right:
                    IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""Y"")')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""Y""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""Y"") (Syntax: '""Y""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = W(""X"")')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
                  Right:
                    IInvocationOperation (System.Int32 C.W(System.String s)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'W(""X"")')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: '""X""')
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""X"") (Syntax: '""X""')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with  ... = W(""X"") };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with  ...  = W(""X"") }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { Y  ...  = W(""X"") }')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void WithExprConversions1()
        {
            var src = @"
using System;
record C(long X)
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
  IL_0007:  callvirt   ""C C.<>Clone()""
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
record C(long X)
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
  IL_0010:  callvirt   ""C C.<>Clone()""
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
record C(long X)
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
record C(long X)
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
record C(object X)
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
record C
{
    private readonly long _x;
    public long X { get => _x; init { Console.WriteLine(""set""); _x = value; } }
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
  IL_000e:  callvirt   ""C C.<>Clone()""
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
record C
{
    public static int X { get; set; }
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,22): error CS0176: Member 'C.X' cannot be accessed with an instance reference; qualify it with a type name instead
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "X").WithArguments("C.X").WithLocation(9, 22)
            );
        }

        [Fact]
        public void WithExprMethodAsArgument()
        {
            var src = @"
record C
{
    public int X() => 0;
    public static void Main()
    {
        var c = new C();
        c = c with { };
        c = c with { X = 11 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,22): error CS1913: Member 'X' cannot be initialized. It is not a field or property.
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "X").WithArguments("X").WithLocation(9, 22)
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
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
                // (14,22): error CS0117: 'B' does not contain a definition for 'X'
                //         c = c with { X = 11 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "X").WithArguments("B", "X").WithLocation(14, 22)
            );
        }

        [Fact]
        public void WithExprBadMemberBadType()
        {
            var src = @"
record C
{
    public int X { get; init; }
    public static void Main()
    {
        var c = new C();
        c = c with { X = ""a"" };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         c = c with { X = "a" };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""a""").WithArguments("string", "int").WithLocation(8, 26)
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
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
        public void WithSemanticModel1()
        {
            var src = @"
record C(int X, string Y)
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

            comp.VerifyOperationTree(withExpr, @"
IWithOperation (OperationKind.With, Type: C) (Syntax: 'c with { X = 2 }')
  Value:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C.<>Clone()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 2 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C(0, ""a"")')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C(0, ""a"")')
              Right:
                IObjectCreationOperation (Constructor: C..ctor(System.Int32 X, System.String Y)) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C(0, ""a"")')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '0')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: '""a""')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""a"") (Syntax: '""a""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c with { X = 2 }')
                  Value:
                    IInvocationOperation (virtual C C.<>Clone()) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
                      Instance Receiver:
                        ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                      Arguments(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; init; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c with { X = 2 };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'c = c with { X = 2 }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c with { X = 2 }')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void NoCloneMethod()
        {
            var src = @"
class C
{
    int X { get; set; }

    public static void Main()
    {
        var c = new C();
        c = c with { X = 2 };
    }
}";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var withExpr = root.DescendantNodes().OfType<WithExpressionSyntax>().Single();

            comp.VerifyOperationTree(withExpr, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { X = 2 }')
  Value:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  CloneMethod: null
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C) (Syntax: '{ X = 2 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
            Left:
              IPropertyReferenceOperation: System.Int32 C.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')");

            var main = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            Assert.Equal("Main", main.Identifier.ToString());
            VerifyFlowGraph(comp, main, expectedFlowGraph: @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [C c]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'c = new C()')
              Left:
                ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'c = new C()')
              Right:
                IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value:
                    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Value:
                    IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
                      Children(1):
                          ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
                  Left:
                    IPropertyReferenceOperation: System.Int32 C.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'c = c with { X = 2 };')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: 'c = c with { X = 2 }')
                      Left:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                      Right:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'c')
            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void WithBadExprArg()
        {
            var src = @"
record C(int X, int Y)
{
    public C Clone() => null;
    public static void Main()
    {
        var c = new C(0, 0);
        c = c with { 5 };
        c = c with { { X = 2 } };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,22): error CS0747: Invalid initializer member declarator
                //         c = c with { 5 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "5").WithLocation(8, 22),
                // (9,22): error CS1513: } expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(9, 22),
                // (9,22): error CS1002: ; expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(9, 22),
                // (9,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.X'
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_ObjectRequired, "X").WithArguments("C.X").WithLocation(9, 24),
                // (9,30): error CS1002: ; expected
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(9, 30),
                // (9,33): error CS1597: Semicolon after method or accessor block is not valid
                //         c = c with { { X = 2 } };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(9, 33),
                // (11,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(11, 1)
            );

            var tree = comp.SyntaxTrees[0];
            var root = tree.GetRoot();
            var model = comp.GetSemanticModel(tree);
            VerifyClone(model);

            var withExpr1 = root.DescendantNodes().OfType<WithExpressionSyntax>().First();
            comp.VerifyOperationTree(withExpr1, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { 5 }')
  Value:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C.<>Clone()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ 5 }')
      Initializers(1):
          IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '5')
            Children(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsInvalid) (Syntax: '5')");

            var withExpr2 = root.DescendantNodes().OfType<WithExpressionSyntax>().Skip(1).Single();
            comp.VerifyOperationTree(withExpr2, @"
IWithOperation (OperationKind.With, Type: C, IsInvalid) (Syntax: 'c with { ')
  Value:
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C) (Syntax: 'c')
  CloneMethod: C C.<>Clone()
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: '{ ')
      Initializers(0)");
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_01()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        int y;
        _ = b with { X = y = 42 };
        y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_02()
        {
            var src = @"
record B(int X, string Y)
{
    static void M(B b)
    {
        int z;
        _ = b with { X = z = 42, Y = z.ToString() };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_03()
        {
            var src = @"
record B(int X, string Y)
{
    static void M(B b)
    {
        int z;
        _ = b with { Y = z.ToString(), X = z = 42 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,26): error CS0165: Use of unassigned local variable 'z'
                //         _ = b with { Y = z.ToString(), X = z = 42 };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(7, 26));
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_04()
        {
            var src = @"
record B(int X)
{
    static void M()
    {
        B b;
        _ = (b = new B(42)) with { X = b.X };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_05()
        {
            var src = @"
record B(int X)
{
    static void M()
    {
        B b;
        _ = new B(b.X) with { X = (b = new B(42)).X };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,19): error CS0165: Use of unassigned local variable 'b'
                //         _ = new B(b.X) with { X = new B(42).X };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b").WithArguments("b").WithLocation(7, 19));
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_06()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        int y;
        _ = b with { X = M(out y) };
        y.ToString();
    }

    static int M(out int y) { y = 42; return 43; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_DefiniteAssignment_07()
        {
            var src = @"
record B(int X)
{
    static void M(B b)
    {
        _ = b with { X = M(out int y) };
        y.ToString();
    }

    static int M(out int y) { y = 42; return 43; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_NullableAnalysis_01()
        {
            var src = @"
#nullable enable
record B(int X)
{
    static void M(B b)
    {
        string? s = null;
        _ = b with { X = M(out s) };
        s.ToString();
    }

    static int M(out string s) { s = ""a""; return 42; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_NullableAnalysis_02()
        {
            var src = @"
#nullable enable
record B(string X)
{
    static void M(B b, string? s)
    {
        b.X.ToString();
        _ = b with { X = s }; // 1
        b.X.ToString(); // 2
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,26): warning CS8601: Possible null reference assignment.
                //         _ = b with { X = s }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "s").WithLocation(8, 26));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_03()
        {
            var src = @"
#nullable enable
record B(string? X)
{
    public B Clone() => new B(X);

    static void M1(B b, string s, bool flag)
    {
        if (flag) { b.X.ToString(); } // 1
        _ = b with { X = s };
        if (flag) { b.X.ToString(); } // 2
    }

    static void M2(B b, string s, bool flag)
    {
        if (flag) { b.X.ToString(); } // 3
        b = b with { X = s };
        if (flag) { b.X.ToString(); }
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(9, 21),
                // (11,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(11, 21),
                // (16,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(16, 21));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_04()
        {
            var src = @"
#nullable enable
record B(int X)
{
    static void M1(B? b)
    {
        var b1 = b with { X = 42 }; // 1
        _ = b.ToString();
        _ = b1.ToString();
    }

    static void M2(B? b)
    {
        (b with { X = 42 }).ToString(); // 2
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (7,18): warning CS8602: Dereference of a possibly null reference.
                //         var b1 = b with { X = 42 }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b").WithLocation(7, 18),
                // (14,10): warning CS8602: Dereference of a possibly null reference.
                //         (b with { X = 42 }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b").WithLocation(14, 10));
        }

        [Fact, WorkItem(44763, "https://github.com/dotnet/roslyn/issues/44763")]
        public void WithExpr_NullableAnalysis_05()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    public B Clone() => new B(X, Y);

    static void M1(bool flag)
    {
        B b = new B(""hello"", null);
        if (flag)
        {
            b.X.ToString(); // shouldn't warn
            b.Y.ToString(); // 1
        }

        b = b with { Y = ""world"" };
        b.X.ToString(); // shouldn't warn
        b.Y.ToString();
    }
}";
            // records should propagate the nullability of the
            // constructor arguments to the corresponding properties.
            // https://github.com/dotnet/roslyn/issues/44763
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,13): warning CS8602: Dereference of a possibly null reference.
                //             b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(12, 13),
                // (13,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Y.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Y").WithLocation(13, 13),
                // (17,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(17, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_06()
        {
            var src = @"
#nullable enable
record B
{
    public string? X { get; init; }
    public string? Y { get; init; }

    static void M1(bool flag)
    {
        B b = new B { X = ""hello"", Y = null };
        if (flag)
        {
            b.X.ToString();
            b.Y.ToString(); // 1
        }

        b = b with { Y = ""world"" };

        b.X.ToString();
        b.Y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Y.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Y").WithLocation(14, 13)
            );
        }

        [Fact, WorkItem(44691, "https://github.com/dotnet/roslyn/issues/44691")]
        public void WithExpr_NullableAnalysis_07()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B([AllowNull] string X)
{
    public B Clone() => new B(X);

    static void M1(B b)
    {
        b.X.ToString();
        b = b with { X = null }; // ok
        b.X.ToString(); // ok
        b = new B((string?)null);
        b.X.ToString(); // ok
    }
}";
            // We should have a way to propagate attributes on
            // positional parameters to the corresponding properties.
            // https://github.com/dotnet/roslyn/issues/44691
            var comp = CreateCompilation(new[] { src, AllowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (12,26): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         b = b with { X = null }; // ok
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(12, 26),
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // ok
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(13, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_08()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    public B Clone() => new B(X, Y);

    static void M1(B b1)
    {
        B b2 = b1 with { X = ""hello"" };
        B b3 = b1 with { Y = ""world"" };
        B b4 = b2 with { Y = ""world"" };

        b1.X.ToString(); // 1
        b1.Y.ToString(); // 2
        b2.X.ToString();
        b2.Y.ToString(); // 3
        b3.X.ToString(); // 4
        b3.Y.ToString();
        b4.X.ToString();
        b4.Y.ToString();
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,9): warning CS8602: Dereference of a possibly null reference.
                //         b1.X.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b1.X").WithLocation(13, 9),
                // (14,9): warning CS8602: Dereference of a possibly null reference.
                //         b1.Y.ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b1.Y").WithLocation(14, 9),
                // (16,9): warning CS8602: Dereference of a possibly null reference.
                //         b2.Y.ToString(); // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b2.Y").WithLocation(16, 9),
                // (17,9): warning CS8602: Dereference of a possibly null reference.
                //         b3.X.ToString(); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b3.X").WithLocation(17, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_09()
        {
            var src = @"
#nullable enable
record B(string? X, string? Y)
{
    static void M1(B b1)
    {
        string? local = ""hello"";
        _ = b1 with
        {
            X = local = null,
            Y = local.ToString() // 1
        };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,17): warning CS8602: Dereference of a possibly null reference.
                //             Y = local.ToString() // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "local").WithLocation(11, 17));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_10()
        {
            var src = @"
#nullable enable
record B(string X, string Y)
{
    static string M0(out string? s) { s = null; return ""hello""; }

    static void M1(B b1)
    {
        string? local = ""world"";
        _ = b1 with
        {
            X = M0(out local),
            Y = local // 1
        };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,17): warning CS8601: Possible null reference assignment.
                //             Y = local // 1
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "local").WithLocation(13, 17));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_VariantClone()
        {
            var src = @"
#nullable enable

record A
{
    public string? Y { get; init; }
    public string? Z { get; init; }
}

record B(string? X) : A
{
    public new string Z { get; init; } = ""zed"";

    static void M1(B b1)
    {
        b1.Z.ToString();
        (b1 with { Y = ""hello"" }).Y.ToString();
        (b1 with { Y = ""hello"" }).Z.ToString();
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
            );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NullableClone()
        {
            var src = @"
#nullable enable

record B(string? X)
{
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { X = null }; // 1
        (b1 with { X = null }).ToString(); // 2
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (10,21): warning CS8602: Dereference of a possibly null reference.
                //         _ = b1 with { X = null }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(10, 21),
                // (11,18): warning CS8602: Dereference of a possibly null reference.
                //         (b1 with { X = null }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(11, 18));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_MaybeNullClone()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B(string? X)
{
    [return: MaybeNull]
    public B Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        _ = b1 with { X = null }; // 1
        (b1 with { X = null }).ToString(); // 2
    }
}
";
            var comp = CreateCompilation(new[] { src, MaybeNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,21): warning CS8602: Dereference of a possibly null reference.
                //         _ = b1 with { X = null }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(13, 21),
                // (14,18): warning CS8602: Dereference of a possibly null reference.
                //         (b1 with { X = null }).ToString(); // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "{ X = null }").WithLocation(14, 18));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NotNullClone()
        {
            var src = @"
#nullable enable
using System.Diagnostics.CodeAnalysis;

record B(string? X)
{
    [return: NotNull]
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        _ = b1 with { X = null };
        (b1 with { X = null }).ToString();
    }
}
";
            var comp = CreateCompilation(new[] { src, NotNullAttributeDefinition });
            comp.VerifyDiagnostics();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44859")]
        [WorkItem(44859, "https://github.com/dotnet/roslyn/issues/44859")]
        public void WithExpr_NullableAnalysis_NullableClone_NoInitializers()
        {
            var src = @"
#nullable enable

record B(string? X)
{
    public B? Clone() => new B(X);

    static void M1(B b1)
    {
        _ = b1 with { };
        (b1 with { }).ToString(); // 1
    }
}
";
            var comp = CreateCompilation(src);
            // Note: we expect to give a warning on `// 1`, but do not currently
            // due to limitations of object initializer analysis.
            // Tracking in https://github.com/dotnet/roslyn/issues/44759
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void WithExprNominalRecord()
        {
            var src = @"
using System;
record C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;
    public event Action E;

    public C() { }
    public C(C other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
        E = other.E;
    }

    public static void Main()
    {
        var c = new C() { X = 1, Y = ""2"", Z = 3, E = () => { } };
        var c2 = c with {};
        Console.WriteLine(c.Equals(c2));
        Console.WriteLine(ReferenceEquals(c, c2));
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
True
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
        public void WithExprNominalRecord2()
        {
            var comp1 = CreateCompilation(@"
public record C
{
    public int X { get; set; }
    public string Y { get; init; }
    public long Z;

    public C() { }
    public C(C other)
    {
        X = other.X;
        Y = other.Y;
        Z = other.Z;
    }
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
  IL_0001:  callvirt   ""C C.<>Clone()""
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
        public void WithExprAssignToRef1()
        {
            var src = @"
using System;
record C(int Y)
{
    private readonly int[] _a = new[] { 0 };
    public ref int X => ref _a[0];

    public C Clone() => new C(0);

    public static void Main()
    {
        var c = new C(0) { X = 5 };
        Console.WriteLine(c.X);
        c = c with { X = 1 };
        Console.WriteLine(c.X);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
5
1");
            verifier.VerifyIL("C.Main", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""C..ctor(int)""
  IL_0006:  dup
  IL_0007:  callvirt   ""ref int C.X.get""
  IL_000c:  ldc.i4.5
  IL_000d:  stind.i4
  IL_000e:  dup
  IL_000f:  callvirt   ""ref int C.X.get""
  IL_0014:  ldind.i4
  IL_0015:  call       ""void System.Console.WriteLine(int)""
  IL_001a:  callvirt   ""C C.<>Clone()""
  IL_001f:  dup
  IL_0020:  callvirt   ""ref int C.X.get""
  IL_0025:  ldc.i4.1
  IL_0026:  stind.i4
  IL_0027:  callvirt   ""ref int C.X.get""
  IL_002c:  ldind.i4
  IL_002d:  call       ""void System.Console.WriteLine(int)""
  IL_0032:  ret
}");
        }

        [Fact]
        public void WithExprAssignToRef2()
        {
            var src = @"
using System;
record C(int Y)
{
    private readonly int[] _a = new[] { 0 };
    public ref int X
    {
        get => ref _a[0];
        set { }
    }

    public C Clone() => new C(0);

    public static void Main()
    {
        var a = new[] { 0 };
        var c = new C(0) { X = ref a[0] };
        Console.WriteLine(c.X);
        c = c with { X = ref a[0] };
        Console.WriteLine(c.X);
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,9): error CS8147: Properties which return by reference cannot have set accessors
                //         set { }
                Diagnostic(ErrorCode.ERR_RefPropertyCannotHaveSetAccessor, "set").WithArguments("C.X.set").WithLocation(9, 9),
                // (17,32): error CS1525: Invalid expression term 'ref'
                //         var c = new C(0) { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a[0]").WithArguments("ref").WithLocation(17, 32),
                // (17,32): error CS1073: Unexpected token 'ref'
                //         var c = new C(0) { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(17, 32),
                // (19,26): error CS1073: Unexpected token 'ref'
                //         c = c with { X = ref a[0] };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(19, 26)
            );
        }

        [Fact]
        public void WithExpressionSameLHS()
        {
            var comp = CreateCompilation(@"
record C(int X)
{
    public static void Main()
    {
        var c = new C(0);
        c = c with { X = 1, X = 2};
    }
}");
            comp.VerifyDiagnostics(
                // (7,29): error CS1912: Duplicate initialization of member 'X'
                //         c = c with { X = 1, X = 2};
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "X").WithArguments("X").WithLocation(7, 29)
            );
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_01()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get; set; }
    internal object P2 { get; set; }
    protected internal object P3 { get; set; }
    protected object P4 { get; set; }
    private protected object P5 { get; set; }
    private object P6 { get; set; }
}
record B(object P1, object P2, object P3, object P4, object P5, object P6) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P6 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_02()
        {
            var source =
@"record A
{
    internal A() { }
    private protected object P1 { get; set; }
    private object P2 { get; set; }
    private record B(object P1, object P2) : A
    {
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "A.B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type A.B.EqualityContract { get; }" }, actualMembers);
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Inheritance_03(bool useCompilationReference)
        {
            var sourceA =
@"public record A
{
    public A() { }
    internal object P { get; set; }
}
public record B(object Q) : A
{
    public B() : this(null) { }
}
record C1(object P, object Q) : B
{
}";
            var comp = CreateCompilation(sourceA);
            AssertEx.Equal(new[] { "System.Type C1.EqualityContract { get; }" }, GetProperties(comp, "C1").ToTestDisplayStrings());
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();

            var sourceB =
@"record C2(object P, object Q) : B
{
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type C2.EqualityContract { get; }", "System.Object C2.P { get; init; }" }, GetProperties(comp, "C2").ToTestDisplayStrings());
        }

        [WorkItem(44616, "https://github.com/dotnet/roslyn/issues/44616")]
        [Fact]
        public void Inheritance_04()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get { return null; } set { } }
    public object P2 { get; init; }
    public object P3 { get; }
    public object P4 { set { } }
    public virtual object P5 { get; set; }
    public static object P6 { get; set; }
    public ref object P7 => throw null;
}
record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,50): error CS8866: Record member 'A.P4' must be a readable instance property of type 'object' to match positional parameter 'P4'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "object", "P4").WithLocation(12, 50),
                // (12,72): error CS8866: Record member 'A.P6' must be a readable instance property of type 'object' to match positional parameter 'P6'.
                // record B(object P1, object P2, object P3, object P4, object P5, object P6, object P7) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P6").WithArguments("A.P6", "object", "P6").WithLocation(12, 72));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_05()
        {
            var source =
@"record A
{
    internal A() { }
    public object P1 { get; set; }
    public int P2 { get; set; }
}
record B(int P1, object P2) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8866: Record member 'A.P1' must be a readable instance property of type 'int' to match positional parameter 'P1'.
                // record B(int P1, object P2) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "int", "P1").WithLocation(7, 14),
                // (7,25): error CS8866: Record member 'A.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record B(int P1, object P2) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("A.P2", "object", "P2").WithLocation(7, 25));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_06()
        {
            var source =
@"record A
{
    internal int X { get; set; }
    internal int Y { set { } }
    internal int Z;
}
record B(int X, int Y, int Z) : A
{
}
class Program
{
    static void Main()
    {
        var b = new B(1, 2, 3);
        b.X = 4;
        b.Y = 5;
        b.Z = 6;
        ((A)b).X = 7;
        ((A)b).Y = 8;
        ((A)b).Z = 9;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,21): error CS8866: Record member 'A.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record B(int X, int Y, int Z) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("A.Y", "int", "Y").WithLocation(7, 21),
                // (7,28): error CS8866: Record member 'A.Z' must be a readable instance property of type 'int' to match positional parameter 'Z'.
                // record B(int X, int Y, int Z) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Z").WithArguments("A.Z", "int", "Z").WithLocation(7, 28));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [WorkItem(44785, "https://github.com/dotnet/roslyn/issues/44785")]
        [Fact]
        public void Inheritance_07()
        {
            var source =
@"abstract record A
{
    public abstract int X { get; }
    public virtual int Y { get; }
}
abstract record B1(int X, int Y) : A
{
}
record B2(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,8): error CS0534: 'B2' does not implement inherited abstract member 'A.X.get'
                // record B2(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B2").WithArguments("B2", "A.X.get").WithLocation(9, 8));

            AssertEx.Equal(new[] { "System.Type B1.EqualityContract { get; }" }, GetProperties(comp, "B1").ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.Type B2.EqualityContract { get; }" }, GetProperties(comp, "B2").ToTestDisplayStrings());

            var b1Ctor = comp.GetTypeByMetadataName("B1")!.GetMembersUnordered().OfType<SynthesizedRecordConstructor>().Single();
            Assert.Equal("B1..ctor(System.Int32 X, System.Int32 Y)", b1Ctor.ToTestDisplayString());
            Assert.Equal(Accessibility.Protected, b1Ctor.DeclaredAccessibility);
        }

        [WorkItem(44785, "https://github.com/dotnet/roslyn/issues/44785")]
        [Fact]
        public void Inheritance_08()
        {
            var source =
@"abstract record A
{
    public abstract int X { get; }
    public virtual int Y { get; }
    public virtual int Z { get; }
}
abstract record B : A
{
    public override abstract int Y { get; }
}
record C(int X, int Y, int Z) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,8): error CS0534: 'C' does not implement inherited abstract member 'B.Y.get'
                // record C(int X, int Y, int Z) : B
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "B.Y.get").WithLocation(11, 8),
                // (11,8): error CS0534: 'C' does not implement inherited abstract member 'A.X.get'
                // record C(int X, int Y, int Z) : B
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "C").WithArguments("C", "A.X.get").WithLocation(11, 8));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_09()
        {
            var source =
@"abstract record C(int X, int Y)
{
    public abstract int X { get; }
    public virtual int Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C C.<>Clone()",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 C.X { get; }",
                "System.Int32 C.X.get",
                "System.Int32 C.<Y>k__BackingField",
                "System.Int32 C.Y { get; }",
                "System.Int32 C.Y.get",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? )",
                "System.Boolean C.Equals(C? )",
                "C..ctor(C )",
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_10()
        {
            var source =
@"using System;
interface IA
{
    int X { get; }
}
interface IB
{
    int Y { get; }
}
record C(int X, int Y) : IA, IB
{
}
class Program
{
    static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(""{0}, {1}"", c.X, c.Y);
        Console.WriteLine(""{0}, {1}"", ((IA)c).X, ((IB)c).Y);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"1, 2
1, 2");
        }

        [Fact]
        public void Inheritance_11()
        {
            var source =
@"interface IA
{
    int X { get; }
}
interface IB
{
    object Y { get; set; }
}
record C(object X, object Y) : IA, IB
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,32): error CS0738: 'C' does not implement interface member 'IA.X'. 'C.X' cannot implement 'IA.X' because it does not have the matching return type of 'int'.
                // record C(object X, object Y) : IA, IB
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType, "IA").WithArguments("C", "IA.X", "C.X", "int").WithLocation(9, 32),
                // (9,36): error CS8854: 'C' does not implement interface member 'IB.Y.set'. 'C.Y.init' cannot implement 'IB.Y.set'.
                // record C(object X, object Y) : IA, IB
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "IB").WithArguments("C", "IB.Y.set", "C.Y.init").WithLocation(9, 36)
            );
        }

        [Fact]
        public void Inheritance_12()
        {
            var source =
@"record A
{
    public object X { get; }
    public object Y { get; }
}
record B(object X, object Y) : A
{
    public object X { get; }
    public object Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): warning CS0108: 'B.X' hides inherited member 'A.X'. Use the new keyword if hiding was intended.
                //     public object X { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "X").WithArguments("B.X", "A.X").WithLocation(8, 19),
                // (9,19): warning CS0108: 'B.Y' hides inherited member 'A.Y'. Use the new keyword if hiding was intended.
                //     public object Y { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Y").WithArguments("B.Y", "A.Y").WithLocation(9, 19));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.X { get; }",
                "System.Object B.Y { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_13()
        {
            var source =
@"record A(object X, object Y)
{
    internal A() : this(null, null) { }
}
record B(object X, object Y) : A
{
    public object X { get; }
    public object Y { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,19): warning CS0108: 'B.X' hides inherited member 'A.X'. Use the new keyword if hiding was intended.
                //     public object X { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "X").WithArguments("B.X", "A.X").WithLocation(7, 19),
                // (8,19): warning CS0108: 'B.Y' hides inherited member 'A.Y'. Use the new keyword if hiding was intended.
                //     public object Y { get; }
                Diagnostic(ErrorCode.WRN_NewRequired, "Y").WithArguments("B.Y", "A.Y").WithLocation(8, 19));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.X { get; }",
                "System.Object B.Y { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_14()
        {
            var source =
@"record A
{
    public object P1 { get; }
    public object P2 { get; }
    public object P3 { get; }
    public object P4 { get; }
}
record B : A
{
    public new int P1 { get; }
    public new int P2 { get; }
}
record C(object P1, int P2, object P3, int P4) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,17): error CS8866: Record member 'B.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record C(object P1, int P2, object P3, int P4) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("B.P1", "object", "P1").WithLocation(13, 17),
                // (13,44): error CS8866: Record member 'A.P4' must be a readable instance property of type 'int' to match positional parameter 'P4'.
                // record C(object P1, int P2, object P3, int P4) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "int", "P4").WithLocation(13, 44));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_15()
        {
            var source =
@"record C(int P1, object P2)
{
    public object P1 { get; set; }
    public int P2 { get; set; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,14): error CS8866: Record member 'C.P1' must be a readable instance property of type 'int' to match positional parameter 'P1'.
                // record C(int P1, object P2)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("C.P1", "int", "P1").WithLocation(1, 14),
                // (1,25): error CS8866: Record member 'C.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record C(int P1, object P2)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("C.P2", "object", "P2").WithLocation(1, 25));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; set; }",
                "System.Int32 C.P2 { get; set; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_16()
        {
            var source =
@"record A
{
    public int P1 { get; }
    public int P2 { get; }
    public int P3 { get; }
    public int P4 { get; }
}
record B(object P1, int P2, object P3, int P4) : A
{
    public new object P1 { get; }
    public new object P2 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,25): error CS8866: Record member 'B.P2' must be a readable instance property of type 'int' to match positional parameter 'P2'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("B.P2", "int", "P2").WithLocation(8, 25),
                // (8,36): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(8, 36));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P1 { get; }",
                "System.Object B.P2 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_17()
        {
            var source =
@"record A
{
    public object P1 { get; }
    public object P2 { get; }
    public object P3 { get; }
    public object P4 { get; }
}
record B(object P1, int P2, object P3, int P4) : A
{
    public new int P1 { get; }
    public new int P2 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): error CS8866: Record member 'B.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("B.P1", "object", "P1").WithLocation(8, 17),
                // (8,44): error CS8866: Record member 'A.P4' must be a readable instance property of type 'int' to match positional parameter 'P4'.
                // record B(object P1, int P2, object P3, int P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("A.P4", "int", "P4").WithLocation(8, 44));

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Int32 B.P1 { get; }",
                "System.Int32 B.P2 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_18()
        {
            var source =
@"record C(object P1, object P2, object P3, object P4, object P5)
{
    public object P1 { get { return null; } set { } }
    public object P2 { get; }
    public object P3 { set { } }
    public static object P4 { get; set; }
    public ref object P5 => throw null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,39): error CS8866: Record member 'C.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record C(object P1, object P2, object P3, object P4, object P5)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("C.P3", "object", "P3").WithLocation(1, 39),
                // (1,50): error CS8866: Record member 'C.P4' must be a readable instance property of type 'object' to match positional parameter 'P4'.
                // record C(object P1, object P2, object P3, object P4, object P5)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P4").WithArguments("C.P4", "object", "P4").WithLocation(1, 50));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; set; }",
                "System.Object C.P2 { get; }",
                "System.Object C.P3 { set; }",
                "System.Object C.P4 { get; set; }",
                "ref System.Object C.P5 { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_19()
        {
            var source =
@"#pragma warning disable 8618
#nullable enable
record A
{
    internal A() { }
    public object P1 { get; }
    public dynamic[] P2 { get; }
    public object? P3 { get; }
    public object[] P4 { get; }
    public (int X, int Y) P5 { get; }
    public (int, int)[] P6 { get; }
    public nint P7 { get; }
    public System.UIntPtr[] P8 { get; }
}
record B(dynamic P1, object[] P2, object P3, object?[] P4, (int, int) P5, (int X, int Y)[] P6, System.IntPtr P7, nuint[] P8) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_20()
        {
            var source =
@"#pragma warning disable 8618
#nullable enable
record C(dynamic P1, object[] P2, object P3, object?[] P4, (int, int) P5, (int X, int Y)[] P6, System.IntPtr P7, nuint[] P8)
{
    public object P1 { get; }
    public dynamic[] P2 { get; }
    public object? P3 { get; }
    public object[] P4 { get; }
    public (int X, int Y) P5 { get; }
    public (int, int)[] P6 { get; }
    public nint P7 { get; }
    public System.UIntPtr[] P8 { get; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P1 { get; }",
                "dynamic[] C.P2 { get; }",
                "System.Object? C.P3 { get; }",
                "System.Object[] C.P4 { get; }",
                "(System.Int32 X, System.Int32 Y) C.P5 { get; }",
                "(System.Int32, System.Int32)[] C.P6 { get; }",
                "nint C.P7 { get; }",
                "System.UIntPtr[] C.P8 { get; }"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Inheritance_21(bool useCompilationReference)
        {
            var sourceA =
@"public record A
{
    public object P1 { get; }
    internal object P2 { get; }
}
public record B : A
{
    internal new object P1 { get; }
    public new object P2 { get; }
}";
            var comp = CreateCompilation(sourceA);
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();

            var sourceB =
@"record C(object P1, object P2) : B
{
}
class Program
{
    static void Main()
    {
        var c = new C(1, 2);
        System.Console.WriteLine(""({0}, {1})"", c.P1, c.P2);
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);

            var verifier = CompileAndVerify(comp, expectedOutput: "(, )");
            verifier.VerifyIL("C..ctor(object, object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""B..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ret
}");

            verifier.VerifyIL("Program.Main",
@"{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (C V_0) //c
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  ldc.i4.2
  IL_0007:  box        ""int""
  IL_000c:  newobj     ""C..ctor(object, object)""
  IL_0011:  stloc.0
  IL_0012:  ldstr      ""({0}, {1})""
  IL_0017:  ldloc.0
  IL_0018:  callvirt   ""object A.P1.get""
  IL_001d:  ldloc.0
  IL_001e:  callvirt   ""object B.P2.get""
  IL_0023:  call       ""void System.Console.WriteLine(string, object, object)""
  IL_0028:  ret
}");
        }

        [Fact]
        public void Inheritance_22()
        {
            var source =
@"record A
{
    public ref object P1 => throw null;
    public object P2 => throw null;
}
record B : A
{
    public new object P1 => throw null;
    public new ref object P2 => throw null;
}
record C(object P1, object P2) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_23()
        {
            var source =
@"record A
{
    public static object P1 { get; }
    public object P2 { get; }
}
record B : A
{
    public new object P1 { get; }
    public new static object P2 { get; }
}
record C(object P1, object P2) : B
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,28): error CS8866: Record member 'B.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record C(object P1, object P2) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("B.P2", "object", "P2").WithLocation(11, 28));
            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_24()
        {
            var source =
@"record A
{
    public object get_P() => null;
    public object set_Q() => null;
}
record B(object P, object Q) : A
{
}
record C(object P)
{
    public object get_P() => null;
    public object set_Q() => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,17): error CS0082: Type 'C' already reserves a member called 'get_P' with the same parameter types
                // record C(object P)
                Diagnostic(ErrorCode.ERR_MemberReserved, "P").WithArguments("get_P", "C").WithLocation(9, 17));

            var expectedMembers = new[]
            {
                "A B.<>Clone()",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "B..ctor(System.Object P, System.Object Q)",
                "System.Object B.<P>k__BackingField",
                "System.Object B.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.P.init",
                "System.Object B.P { get; init; }",
                "System.Object B.<Q>k__BackingField",
                "System.Object B.Q.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Q.init",
                "System.Object B.Q { get; init; }",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? )",
                "System.Boolean B.Equals(A? )",
                "System.Boolean B.Equals(B? )",
                "B..ctor(B )",
                "void B.Deconstruct(out System.Object P, out System.Object Q)"
            };
            AssertEx.Equal(expectedMembers, comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings());

            expectedMembers = new[]
            {
                "C C.<>Clone()",
                "System.Type C.EqualityContract.get",
                "System.Type C.EqualityContract { get; }",
                "C..ctor(System.Object P)",
                "System.Object C.<P>k__BackingField",
                "System.Object C.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) C.P.init",
                "System.Object C.P { get; init; }",
                "System.Object C.get_P()",
                "System.Object C.set_Q()",
                "System.Int32 C.GetHashCode()",
                "System.Boolean C.Equals(System.Object? )",
                "System.Boolean C.Equals(C? )",
                "C..ctor(C )",
                "void C.Deconstruct(out System.Object P)"
            };
            AssertEx.Equal(expectedMembers, comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_25()
        {
            var sourceA =
@"public record A
{
    public class P1 { }
    internal object P2 = 2;
    public int P3(object o) => 3;
    internal int P4<T>(T t) => 4;
}";
            var sourceB =
@"record B(object P1, object P2, object P3, object P4) : A
{
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "object", "P1").WithLocation(1, 17),
                // (1,28): error CS8866: Record member 'A.P2' must be a readable instance property of type 'object' to match positional parameter 'P2'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P2").WithArguments("A.P2", "object", "P2").WithLocation(1, 28),
                // (1,39): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(1, 39));
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P4 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P1' must be a readable instance property of type 'object' to match positional parameter 'P1'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P1").WithArguments("A.P1", "object", "P1").WithLocation(1, 17),
                // (1,39): error CS8866: Record member 'A.P3' must be a readable instance property of type 'object' to match positional parameter 'P3'.
                // record B(object P1, object P2, object P3, object P4) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P3").WithArguments("A.P3", "object", "P3").WithLocation(1, 39));
            actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.P2 { get; init; }",
                "System.Object B.P4 { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_26()
        {
            var sourceA =
@"public record A
{
    internal const int P = 4;
}";
            var sourceB =
@"record B(object P) : A
{
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB });
            comp.VerifyDiagnostics(
                // (1,17): error CS8866: Record member 'A.P' must be a readable instance property of type 'object' to match positional parameter 'P'.
                // record B(object P) : A
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P").WithArguments("A.P", "object", "P").WithLocation(1, 17));
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, GetProperties(comp, "B").ToTestDisplayStrings());

            comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }", "System.Object B.P { get; init; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_27()
        {
            var source =
@"record A
{
    public object P { get; }
    public object Q { get; set; }
}
record B(object get_P, object set_Q) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.get_P { get; init; }",
                "System.Object B.set_Q { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_28()
        {
            var source =
@"interface I
{
    object P { get; }
}
record A : I
{
    object I.P => null;
}
record B(object P) : A
{
}
record C(object P) : I
{
    object I.P => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }", "System.Object B.P { get; init; }" }, GetProperties(comp, "B").ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.Type C.EqualityContract { get; }", "System.Object C.P { get; init; }", "System.Object C.I.P { get; }" }, GetProperties(comp, "C").ToTestDisplayStrings());
        }

        [Fact]
        public void Inheritance_29()
        {
            var sourceA =
@"Public Class A
    Public Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Property Q(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record B(object P, object Q) : A
{
    object P { get; }
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            compB.VerifyDiagnostics(
                // (1,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "B").WithArguments("A").WithLocation(1, 8),
                // (1,32): error CS8864: Records may only inherit from object or another record
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(1, 32)
            );

            var actualMembers = GetProperties(compB, "B").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type B.EqualityContract { get; }",
                "System.Object B.Q { get; init; }",
                "System.Object B.P { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Inheritance_30()
        {
            var sourceA =
@"Public Class A
    Public ReadOnly Overloads Property P() As Object
        Get
            Return Nothing
        End Get
    End Property
    Public ReadOnly Overloads Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads Property Q(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Overloads Property Q() As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record B(object P, object Q) : A
{
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            compB.VerifyDiagnostics(
                // (1,8): error CS8867: No accessible copy constructor found in base type 'A'.
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "B").WithArguments("A").WithLocation(1, 8),
                // (1,32): error CS8864: Records may only inherit from object or another record
                // record B(object P, object Q) : A
                Diagnostic(ErrorCode.ERR_BadRecordBase, "A").WithLocation(1, 32)
            );

            var actualMembers = GetProperties(compB, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void Inheritance_31()
        {
            var sourceA =
@"Public Class A
    Public ReadOnly Property P() As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Property Q(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Property R(o As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Sub New(a as A)
    End Sub
End Class
Public Class B
    Inherits A
    Public ReadOnly Overloads Property P(o As Object) As Object
        Get
            Return Nothing
        End Get
    End Property
    Public Overloads Property Q() As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Overloads Property R(x As Object, y As Object) As Object
        Get
            Return Nothing
        End Get
        Set
        End Set
    End Property
    Public Sub New(b as B)
        MyBase.New(b)
    End Sub
End Class
";
            var compA = CreateVisualBasicCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();

            var sourceB =
@"record C(object P, object Q, object R) : B
{
}";
            var compB = CreateCompilation(new[] { sourceB, IsExternalInitTypeDefinition }, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            compB.VerifyDiagnostics(
                // (1,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'b' of 'B.B(B)'
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "(object P, object Q, object R)").WithArguments("b", "B.B(B)").WithLocation(1, 9),
                // (1,42): error CS8864: Records may only inherit from object or another record
                // record C(object P, object Q, object R) : B
                Diagnostic(ErrorCode.ERR_BadRecordBase, "B").WithLocation(1, 42)
            );

            var actualMembers = GetProperties(compB, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.R { get; init; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [InlineData(false)]
        [InlineData(true)]
        public void CopyCtor(bool useCompilationReference)
        {
            var sourceA =
@"public record B(object N1, object N2)
{
}";
            var compA = CreateCompilation(sourceA);
            var verifierA = CompileAndVerify(compA, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);

            verifierA.VerifyIL("B..ctor(B)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""object B.<N1>k__BackingField""
  IL_000d:  stfld      ""object B.<N1>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""object B.<N2>k__BackingField""
  IL_0019:  stfld      ""object B.<N2>k__BackingField""
  IL_001e:  ret
}");

            var refA = useCompilationReference ? compA.ToMetadataReference() : compA.EmitToImageReference();

            var sourceB =
@"record C(object P1, object P2) : B(3, 4)
{
    static void Main()
    {
        var c1 = new C(1, 2);
        System.Console.Write((c1.P1, c1.P2, c1.N1, c1.N2));
        System.Console.Write("" "");

        var c2 = new C(c1);
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2));
        System.Console.Write("" "");

        var c3 = c1 with { P1 = 10, N1 = 30 };
        System.Console.Write((c3.P1, c3.P2, c3.N1, c3.N2));
    }
}";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            compB.VerifyDiagnostics();

            var verifierB = CompileAndVerify(compB, expectedOutput: "(1, 2, 3, 4) (1, 2, 3, 4) (10, 2, 30, 4)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            // call base copy constructor B..ctor(B)
            verifierB.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithOtherOverload()
        {
            var source =
@"public record B(object N1, object N2)
{
    public B(C c) : this(30, 40) => throw null;
}
public record C(object P1, object P2) : B(3, 4)
{
    static void Main()
    {
        var c1 = new C(1, 2);
        System.Console.Write((c1.P1, c1.P2, c1.N1, c1.N2));
        System.Console.Write("" "");

        var c2 = c1 with { P1 = 10, P2 = 20, N1 = 30, N2 = 40 };
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 3, 4) (10, 20, 30, 40)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            // call base copy constructor B..ctor(B)
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithObsoleteCopyConstructor()
        {
            var source =
@"public record B(object N1, object N2)
{
    [System.Obsolete(""Obsolete"", true)]
    public B(B b) { }
}
public record C(object P1, object P2) : B(3, 4) { }
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithParamsCopyConstructor()
        {
            var source =
@"public record B(object N1, object N2)
{
    public B(B b, params int[] i) : this(30, 40) { }
}
public record C(object P1, object P2) : B(3, 4) { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().Where(m => m.Name == ".ctor").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Object N1, System.Object N2)",
                "B..ctor(B b, params System.Int32[] i)",
                "B..ctor(B )"
            };
            AssertEx.Equal(expectedMembers, actualMembers);

            var verifier = CompileAndVerify(comp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithInitializers()
        {
            var source =
@"public record C(object N1, object N2)
{
    private int field = 42;
    public int Property = 43;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""object C.<N1>k__BackingField""
  IL_000d:  stfld      ""object C.<N1>k__BackingField""
  IL_0012:  ldarg.0
  IL_0013:  ldarg.1
  IL_0014:  ldfld      ""object C.<N2>k__BackingField""
  IL_0019:  stfld      ""object C.<N2>k__BackingField""
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  ldfld      ""int C.field""
  IL_0025:  stfld      ""int C.field""
  IL_002a:  ldarg.0
  IL_002b:  ldarg.1
  IL_002c:  ldfld      ""int C.Property""
  IL_0031:  stfld      ""int C.Property""
  IL_0036:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_NotInRecordType()
        {
            var source =
@"public class C
{
    public object Property { get; set; }
    public int field = 42;

    public C(C c)
    {
    }
}
public class D : C
{
    public int field2 = 43;
    public D(D d) : base(d)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   42
  IL_0003:  stfld      ""int C.field""
  IL_0008:  ldarg.0
  IL_0009:  call       ""object..ctor()""
  IL_000e:  ret
}");

            verifier.VerifyIL("D..ctor(D)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   43
  IL_0003:  stfld      ""int D.field2""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  call       ""C..ctor(C)""
  IL_000f:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1, object P2) : B(0, 1)
{
    public C(C c) // 1, 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,12): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(6, 12),
                // (6,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(6, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject()
        {
            var source =
@"public record C(int I)
{
    public int I { get; set; } = 42;
    public C(C c)
    {
    }
    public static void Main()
    {
        var c = new C(1);
        c.I = 2;
        var c2 = new C(c);
        System.Console.Write((c.I, c2.I));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(2, 0)");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject_WithFieldInitializer()
        {
            var source =
@"public record C(int I)
{
    public int I { get; set; } = 42;
    public int field = 43;
    public C(C c)
    {
        System.Console.Write("" RAN "");
    }
    public static void Main()
    {
        var c = new C(1);
        c.I = 2;
        c.field = 100;
        System.Console.Write((c.I, c.field));

        var c2 = new C(c);
        System.Console.Write((c2.I, c2.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(2, 100) RAN (0, 0)");
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldstr      "" RAN ""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_DerivesFromObject_GivesParameterToBase()
        {
            var source = @"
public record C(object I)
{
    public C(C c) : base(1) { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                //     public C(C c) : base(1) { }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("object", "1").WithLocation(4, 21),
                // (4,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : base(1) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(4, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_DerivesFromObject_WithSomeOtherConstructor()
        {
            var source = @"
public record C(object I)
{
    public C(int i) : this((object)null) { }
    public static void Main()
    {
        var c = new C((object)null);
        var c2 = new C(1);
        var c3 = new C(c);
        System.Console.Write(""RAN"");
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("C..ctor(int)", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       ""C..ctor(object)""
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_DerivesFromObject_UsesThis()
        {
            var source =
@"public record C(int I)
{
    public C(C c) : this(c.I)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : this(c.I)
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "this").WithLocation(3, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefined_DerivesFromObject_UsesBase()
        {
            var source =
@"public record C(int I)
{
    public C(C c) : base()
    {
        System.Console.Write(""RAN "");
    }
    public static void Main()
    {
        var c = new C(1);
        System.Console.Write(c.I);
        System.Console.Write("" "");
        var c2 = c with { I = 2 };
        System.Console.Write(c2.I);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "1 RAN 2", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  ldstr      ""RAN ""
  IL_000d:  call       ""void System.Console.Write(string)""
  IL_0012:  nop
  IL_0013:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_NoPositionalMembers()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1) : B(0, 1)
{
    public C(C c) // 1, 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,12): error CS1729: 'B' does not contain a constructor that takes 0 arguments
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("B", "0").WithLocation(6, 12),
                // (6,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) // 1, 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(6, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_UsesThis()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public record C(object P1, object P2) : B(0, 1)
{
    public C(C c) : this(1, 2) // 1
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : this(1, 2) // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "this").WithLocation(6, 21)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButDoesNotDelegateToBaseCopyCtor_UsesBase()
        {
            var source =
@"public record B(int i)
{
}
public record C(int j) : B(0)
{
    public C(C c) : base(1) // 1
    {
    }
}
#nullable enable
public record D(int j) : B(0)
{
    public D(D? d) : base(1) // 2
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) : base(1) // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(6, 21),
                // (13,22): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public D(D? d) : base(1) // 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(13, 22)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefined_WithFieldInitializers()
        {
            var source =
@"public record C(int I)
{
}
public record D(int J) : C(1)
{
    public int field = 42;
    public D(D d) : base(d)
    {
        System.Console.Write(""RAN "");
    }
    public static void Main()
    {
        var d = new D(2);
        System.Console.Write((d.I, d.J, d.field));
        System.Console.Write("" "");

        var d2 = d with { I = 10, J = 20 };
        System.Console.Write((d2.I, d2.J, d.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 42) RAN (10, 20, 42)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("D..ctor(D)", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""C..ctor(C)""
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ldstr      ""RAN ""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  nop
  IL_0014:  ret
}
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_Synthesized_WithFieldInitializers()
        {
            var source =
@"public record C(int I)
{
}
public record D(int J) : C(1)
{
    public int field = 42;
    public static void Main()
    {
        var d = new D(2);
        System.Console.Write((d.I, d.J, d.field));
        System.Console.Write("" "");

        var d2 = d with { I = 10, J = 20 };
        System.Console.Write((d2.I, d2.J, d.field));
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 42) (10, 20, 42)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("D..ctor(D)", @"
 {
      // Code size       33 (0x21)
      .maxstack  2
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  call       ""C..ctor(C)""
      IL_0007:  nop
      IL_0008:  ldarg.0
      IL_0009:  ldarg.1
      IL_000a:  ldfld      ""int D.<J>k__BackingField""
      IL_000f:  stfld      ""int D.<J>k__BackingField""
      IL_0014:  ldarg.0
      IL_0015:  ldarg.1
      IL_0016:  ldfld      ""int D.field""
      IL_001b:  stfld      ""int D.field""
      IL_0020:  ret
    }
");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_UserDefinedButPrivate()
        {
            var source =
@"public record B(object N1, object N2)
{
    private B(B b) { }
}
public record C(object P1, object P2) : B(0, 1)
{
    private C(C c) : base(2, 3) { } // 1
}
public record D(object P1, object P2) : B(0, 1)
{
    private D(D d) : base(d) { } // 2
}
public record E(object P1, object P2) : B(0, 1); // 3
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,22): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     private C(C c) : base(2, 3) { } // 1
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(7, 22),
                // (11,22): error CS0122: 'B.B(B)' is inaccessible due to its protection level
                //     private D(D d) : base(d) { } // 2
                Diagnostic(ErrorCode.ERR_BadAccess, "base").WithArguments("B.B(B)").WithLocation(11, 22),
                // (13,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record E(object P1, object P2) : B(0, 1); // 3
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "E").WithArguments("B").WithLocation(13, 15)
                );
            // Should we complain about private user-defined copy constructor on unsealed type (ie. will prevent inheritance)?
            // https://github.com/dotnet/roslyn/issues/45012
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleToCallerFromPE()
        {
            var sourceA =
@"public record B(object N1, object N2)
{
    internal B(B b) { }
}";
            var compA = CreateCompilation(sourceA);
            var refA = compA.EmitToImageReference();

            var sourceB = @"
record C(object P1, object P2) : B(3, 4); // 1
";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            compB.VerifyDiagnostics(
                // (2,8): error CS8867: No accessible copy constructor found in base type 'B'.
                // record C(object P1, object P2) : B(3, 4); // 1
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 8)
                );

            var sourceC = @"
record C(object P1, object P2) : B(3, 4)
{
    protected C(C c) : base(c) { } // 1, 2
}
";
            var compC = CreateCompilation(sourceC, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            compC.VerifyDiagnostics(
                // (4,24): error CS7036: There is no argument given that corresponds to the required formal parameter 'N2' of 'B.B(object, object)'
                //     protected C(C c) : base(c) { } // 1, 2
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "base").WithArguments("N2", "B.B(object, object)").WithLocation(4, 24),
                // (4,24): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     protected C(C c) : base(c) { } // 1, 2
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "base").WithLocation(4, 24)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleToCallerFromPE_WithIVT()
        {
            var sourceA = @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""AssemblyB"")]

public record B(object N1, object N2)
{
    internal B(B b) { }
}";
            var compA = CreateCompilation(new[] { sourceA, IsExternalInitTypeDefinition }, assemblyName: "AssemblyA", parseOptions: TestOptions.RegularPreview);
            var refA = compA.EmitToImageReference();

            var sourceB = @"
record C(int j) : B(3, 4);
";
            var compB = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview, assemblyName: "AssemblyB");
            compB.VerifyDiagnostics();

            var sourceC = @"
record C(int j) : B(3, 4)
{
    protected C(C c) : base(c) { }
}
";
            var compC = CreateCompilation(sourceC, references: new[] { refA }, parseOptions: TestOptions.RegularPreview, assemblyName: "AssemblyB");
            compC.VerifyDiagnostics();
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [WorkItem(45012, "https://github.com/dotnet/roslyn/issues/45012")]
        public void CopyCtor_UserDefinedButPrivate_InSealedType()
        {
            var source =
@"public record B(int i)
{
}
public sealed record C(int j) : B(0)
{
    private C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var copyCtor = comp.GetMembers("C..ctor")[0];
            Assert.Equal("C..ctor(C c)", copyCtor.ToTestDisplayString());
            Assert.True(copyCtor.DeclaredAccessibility == Accessibility.Private);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        [WorkItem(45012, "https://github.com/dotnet/roslyn/issues/45012")]
        public void CopyCtor_UserDefinedButInternal()
        {
            var source =
@"public record B(object N1, object N2)
{
}
public sealed record Sealed(object P1, object P2) : B(0, 1)
{
    internal Sealed(Sealed s) : base(s)
    {
    }
}
public record Unsealed(object P1, object P2) : B(0, 1)
{
    internal Unsealed(Unsealed s) : base(s)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var sealedCopyCtor = comp.GetMembers("Sealed..ctor")[0];
            Assert.Equal("Sealed..ctor(Sealed s)", sealedCopyCtor.ToTestDisplayString());
            Assert.True(sealedCopyCtor.DeclaredAccessibility == Accessibility.Internal);

            var unsealedCopyCtor = comp.GetMembers("Unsealed..ctor")[0];
            Assert.Equal("Unsealed..ctor(Unsealed s)", unsealedCopyCtor.ToTestDisplayString());
            Assert.True(unsealedCopyCtor.DeclaredAccessibility == Accessibility.Internal);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_BaseHasRefKind()
        {
            var source =
@"public record B(int i)
{
    public B(ref B b) => throw null; // 1, not recognized as copy constructor
}
public record C(int j) : B(1)
{
    internal C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,12): error CS8862: A constructor declared in a record with parameters must have 'this' constructor initializer.
                //     public B(ref B b) => throw null; // 1, not recognized as copy constructor
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "B").WithLocation(3, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_BaseHasRefKind_WithThisInitializer()
        {
            var source =
@"public record B(int i)
{
    public B(ref B b) : this(0) => throw null; // 1, not recognized as copy constructor
}
public record C(int j) : B(1)
{
    internal C(C c) : base(c)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().Where(m => m.Name == ".ctor").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "B..ctor(System.Int32 i)",
                "B..ctor(ref B b)",
                "B..ctor(B )"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_WithPrivateField()
        {
            var source =
@"public record B(object N1, object N2)
{
    private int field1 = 100;
    public int GetField1() => field1;
}
public record C(object P1, object P2) : B(3, 4)
{
    private int field2 = 200;
    public int GetField2() => field2;

    static void Main()
    {
        var c1 = new C(1, 2);
        var c2 = new C(c1);
        System.Console.Write((c2.P1, c2.P2, c2.N1, c2.N2, c2.GetField1(), c2.GetField2()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "(1, 2, 3, 4, 100, 200)", verify: ExecutionConditionUtil.IsCoreClr ? Verification.Skipped : Verification.Fails);
            verifier.VerifyIL("C..ctor(C)", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""B..ctor(B)""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  ldfld      ""object C.<P1>k__BackingField""
  IL_000e:  stfld      ""object C.<P1>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""object C.<P2>k__BackingField""
  IL_001a:  stfld      ""object C.<P2>k__BackingField""
  IL_001f:  ldarg.0
  IL_0020:  ldarg.1
  IL_0021:  ldfld      ""int C.field2""
  IL_0026:  stfld      ""int C.field2""
  IL_002b:  ret
}");
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_MissingInMetadata()
        {
            // IL for `public record B { }`
            var ilSource = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    .method public hidebysig specialname newslot virtual instance class B '<>Clone' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    // Removed copy constructor
    //.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }
}
";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                );

            var source2 = @"
public record C : B
{
    public C(C c) { }
}";
            var comp2 = CreateCompilationWithIL(new[] { source2, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.RegularPreview);
            comp2.VerifyDiagnostics(
                // (4,12): error CS8868: A copy constructor in a record must call a copy constructor of the base, or a parameterless object constructor if the record inherits from object.
                //     public C(C c) { }
                Diagnostic(ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor, "C").WithLocation(4, 12)
                );
        }

        [Fact, WorkItem(44902, "https://github.com/dotnet/roslyn/issues/44902")]
        public void CopyCtor_InaccessibleInMetadata()
        {
            // IL for `public record B { }`
            var ilSource = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    .method public hidebysig specialname newslot virtual instance class B '<>Clone' () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method family hidebysig newslot virtual instance class [mscorlib]System.Type get_EqualityContract () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance int32 GetHashCode () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig virtual instance bool Equals ( object '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public newslot virtual instance bool Equals ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    // Inaccessible copy constructor
    .method private hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }

    .property instance class [mscorlib]System.Type EqualityContract()
    {
        .get instance class [mscorlib]System.Type B::get_EqualityContract()
    }
}
";
            var source = @"
public record C : B {
}";
            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition }, ilSource: ilSource, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                // public record C : B {
                Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                );
        }

        [Fact, WorkItem(45077, "https://github.com/dotnet/roslyn/issues/45077")]
        public void CopyCtor_AmbiguitiesInMetadata()
        {
            // IL for a minimal `public record B { }` with injected copy constructors
            var ilSource_template = @"
.class public auto ansi beforefieldinit B extends [mscorlib]System.Object
{
    INJECT

    .method public hidebysig specialname newslot virtual instance class B '<>Clone' () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: newobj instance void B::.ctor(class B)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }
}
";

            var source = @"
public record C : B
{
    public static void Main()
    {
        var c = new C();
        _ = c with { };
    }
}";

            // We're going to inject various copy constructors into record B (at INJECT marker), and check which one is used
            // by derived record C
            // The RAN and THROW markers are shorthands for method bodies that print "RAN" and throw, respectively.

            // .ctor(B) vs. .ctor(modopt B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW
");

            // .ctor(modopt B) alone
            verify(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
RAN
");

            // .ctor(B) vs. .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int64) '' ) cil managed
THROW
");

            // .ctor(modopt B) vs. .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int64) '' ) cil managed
THROW
");

            // .ctor(B) vs. .ctor(modopt1 B) and .ctor(modopt2 B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
");
            // .ctor(B) vs. .ctor(modopt1 B) and .ctor(modreq B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modreq(int32) '' ) cil managed
THROW
");

            // .ctor(modeopt1 B) vs. .ctor(modopt2 B)
            verifyBoth(@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
", isError: true);

            // private .ctor(B) vs. .ctor(modopt1 B) and .ctor(modopt B)
            verifyBoth(@"
.method private hidebysig specialname rtspecialname instance void .ctor ( class B '' ) cil managed
RAN
",
@"
.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int64) '' ) cil managed
THROW

.method public hidebysig specialname rtspecialname instance void .ctor ( class B modopt(int32) '' ) cil managed
THROW
", isError: true);

            void verifyBoth(string inject1, string inject2, bool isError = false)
            {
                verify(inject1 + inject2, isError);
                verify(inject2 + inject1, isError);
            }

            void verify(string inject, bool isError = false)
            {
                var ranBody = @"
{
    IL_0000:  ldstr      ""RAN""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
}
";

                var throwBody = @"
{
    IL_0000: ldnull
    IL_0001: throw
}
";

                var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition },
                    ilSource: ilSource_template.Replace("INJECT", inject).Replace("RAN", ranBody).Replace("THROW", throwBody),
                    parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

                var expectedDiagnostics = isError ? new[] {
                    // (2,15): error CS8867: No accessible copy constructor found in base type 'B'.
                    // public record C : B
                    Diagnostic(ErrorCode.ERR_NoCopyConstructorInBaseType, "C").WithArguments("B").WithLocation(2, 15)
                } : new DiagnosticDescription[] { };

                comp.VerifyDiagnostics(expectedDiagnostics);
                if (expectedDiagnostics is null)
                {
                    CompileAndVerify(comp, expectedOutput: "RAN");
                }
            }
        }

        [Fact, WorkItem(45077, "https://github.com/dotnet/roslyn/issues/45077")]
        public void CopyCtor_AmbiguitiesInMetadata_GenericType()
        {
            // IL for a minimal `public record B<T> { }` with modopt in nested position of parameter type
            var ilSource = @"
.class public auto ansi beforefieldinit B`1<T> extends [mscorlib]System.Object implements class [mscorlib]System.IEquatable`1<class B`1<!T>>
{
    .method family hidebysig specialname rtspecialname instance void .ctor ( class B`1<!T modopt(int64)> '' ) cil managed
    {
        IL_0000:  ldstr      ""RAN""
        IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
        IL_000a:  ret
    }

    .method public hidebysig specialname newslot virtual instance class B`1<!T> '<>Clone' () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: newobj instance void class B`1<!T>::.ctor(class B`1<!0>)
        IL_0006: ret
    }

    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    }

    .method public newslot virtual instance bool Equals ( class B`1<!T> '' ) cil managed
    {
        IL_0000: ldnull
        IL_0001: throw
    }
}
";

            var source = @"
public record C<T> : B<T> { }

public class Program
{
    public static void Main()
    {
        var c = new C<string>();
        _ = c with { };
    }
}";


            var comp = CreateCompilationWithIL(new[] { source, IsExternalInitTypeDefinition },
                ilSource: ilSource,
                parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");
        }

        [Fact]
        public void Deconstruct_Simple()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public static void Main()
    {
        M(new B(1, 2));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""int B.X.get""
    IL_0007:  stind.i4
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int B.Y.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");

            var deconstruct = ((CSharpCompilation)verifier.Compilation).GetMember<MethodSymbol>("B.Deconstruct");
            Assert.Equal(2, deconstruct.ParameterCount);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[0].RefKind);
            Assert.Equal("X", deconstruct.Parameters[0].Name);

            Assert.Equal(RefKind.Out, deconstruct.Parameters[1].RefKind);
            Assert.Equal("Y", deconstruct.Parameters[1].Name);

            Assert.True(deconstruct.ReturnsVoid);
            Assert.False(deconstruct.IsVirtual);
            Assert.False(deconstruct.IsStatic);
            Assert.Equal(Accessibility.Public, deconstruct.DeclaredAccessibility);
        }

        [Fact]
        public void Deconstruct_PositionalAndNominalProperty()
        {
            var source =
@"using System;

record B(int X)
{
    public int Y { get; init; }

    public static void Main()
    {
        M(new B(1));
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Nested()
        {
            var source =
@"using System;

record B(int X, int Y);

record C(B B, int Z)
{
    public static void Main()
    {
        M(new C(new B(1, 2), 3));
    }

    static void M(C c)
    {
        switch (c)
        {
            case C(B(int x, int y), int z):
                Console.Write(x);
                Console.Write(y);
                Console.Write(z);
                break;
        }
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("B.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""int B.X.get""
    IL_0007:  stind.i4
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int B.Y.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");

            verifier.VerifyIL("C.Deconstruct", @"
{
    // Code size       17 (0x11)
    .maxstack  2
    IL_0000:  ldarg.1
    IL_0001:  ldarg.0
    IL_0002:  call       ""B C.B.get""
    IL_0007:  stind.ref
    IL_0008:  ldarg.2
    IL_0009:  ldarg.0
    IL_000a:  call       ""int C.Z.get""
    IL_000f:  stind.i4
    IL_0010:  ret
}");
        }

        [Fact]
        public void Deconstruct_PropertyCollision()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public int X => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "32");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_01()
        {
            var source = @"
record B(int X, int Y)
{
    public int X() => 3;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,14): error CS0102: The type 'B' already contains a definition for 'X'
                // record B(int X, int Y)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("B", "X").WithLocation(2, 14));

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_02()
        {
            var source = @"
record B
{
    public int X(int y) => y;
}

record C(int X, int Y) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,14): error CS8866: Record member 'B.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X, int Y) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("B.X", "int", "X").WithLocation(7, 14)
            );

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_03()
        {
            var source = @"
using System;

record B
{
    public int X() => 3;
}

record C(int X, int Y) : B
{
    public new int X { get; }

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "02");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_04()
        {
            var source = @"
record C(int X, int Y)
{
    public int X(int arg) => 3;

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                break;
        }
    }

    static void Main()
    {
        M(new C(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,14): error CS0102: The type 'C' already contains a definition for 'X'
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 14));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_FieldCollision()
        {
            var source = @"
using System;

record C(int X)
{
    int X;

    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(0));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,14): error CS0102: The type 'C' already contains a definition for 'X'
                // record C(int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(4, 14));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_EventCollision()
        {
            var source = @"
using System;

record C(Action X)
{
    event Action X;

    static void M(C c)
    {
        switch (c)
        {
            case C(Action x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(() => { }));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,17): error CS0102: The type 'C' already contains a definition for 'X'
                // record C(Action X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(4, 17));

            Assert.Equal(
                "void C.Deconstruct(out System.Action X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_WriteOnlyPropertyInBase()
        {
            var source = @"
using System;

record B
{
    public int X { set { } }
}

record C(int X) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,14): error CS8866: Record member 'B.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X) : B
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("B.X", "int", "X").WithLocation(9, 14));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_PrivateWriteOnlyPropertyInBase()
        {
            var source = @"
using System;

record B
{
    private int X { set { } }
}

record C(int X) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x):
                Console.Write(x);
                break;
        }
    }

    static void Main()
    {
        M(new C(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty()
        {
            var source = @"
record C
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,19): error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //             case C():
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "()").WithArguments("C", "Deconstruct").WithLocation(8, 19),
                // (8,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 0 out parameters and a void return type.
                //             case C():
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "()").WithArguments("C", "0").WithLocation(8, 19));

            Assert.Null(comp.GetMember("C.Deconstruct"));
        }

        [Fact]
        public void Deconstruct_Inheritance_01()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    internal B() : this(0, 1) { }
}

record C : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0101");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Null(comp.GetMember("C.Deconstruct"));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_02()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    // https://github.com/dotnet/roslyn/issues/44902
    internal B() : this(0, 1) { }
}

record C(int X, int Y, int Z) : B(X, Y)
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y, int z):
                Console.Write(x);
                Console.Write(y);
                Console.Write(z);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "01201");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y, out System.Int32 Z)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_03()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    internal B() : this(0, 1) { }
}

record C(int X, int Y) : B
{
    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (c)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0101");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Inheritance_04()
        {
            var source = @"
using System;

record A<T>(T P) { internal A() : this(default(T)) { } }
record B1(int P, object Q) : A<int>(P) { internal B1() : this(0, null) { } }
record B2(object P, object Q) : A<object>(P) { internal B2() : this(null, null) { } }
record B3<T>(T P, object Q) : A<T>(P) { internal B3() : this(default, 0) { } }

class C
{
    static void M0(A<int> arg)
    {
        switch (arg)
        {
            case A<int>(int x):
                Console.Write(x);
                break;
        }
    }

    static void M1(B1 arg)
    {
        switch (arg)
        {
            case B1(int p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void M2(B2 arg)
    {
        switch (arg)
        {
            case B2(object p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void M3(B3<int> arg)
    {
        switch (arg)
        {
            case B3<int>(int p, object q):
                Console.Write(p);
                Console.Write(q);
                break;
        }
    }

    static void Main()
    {
        M0(new A<int>(0));
        M1(new B1(1, 2));
        M2(new B2(3, 4));
        M3(new B3<int>(5, 6));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "0123456");
            verifier.VerifyDiagnostics();

            var comp = verifier.Compilation;
            Assert.Equal(
                "void A<T>.Deconstruct(out T P)",
                comp.GetMember("A.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B1.Deconstruct(out System.Int32 P, out System.Object Q)",
                comp.GetMember("B1.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B2.Deconstruct(out System.Object P, out System.Object Q)",
                comp.GetMember("B2.Deconstruct").ToTestDisplayString(includeNonNullable: false));
            Assert.Equal(
                "void B3<T>.Deconstruct(out T P, out System.Object Q)",
                comp.GetMember("B3.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_01()
        {
            var source = @"
using System;

record C(int X, int Y)
{
    public long X { get; init; }
    public long Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(0, 1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,14): error CS8866: Record member 'C.X' must be a readable instance property of type 'int' to match positional parameter 'X'.
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("C.X", "int", "X").WithLocation(4, 14),
                // (4,21): error CS8866: Record member 'C.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record C(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("C.Y", "int", "Y").WithLocation(4, 21));

            Assert.Equal(
                "void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_02()
        {
            var source = @"
#nullable enable
using System;

record C(string? X, string Y)
{
    public string X { get; init; } = null!;
    public string? Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(var x, string y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(""a"", ""b""));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            Assert.Equal(
                "void C.Deconstruct(out System.String? X, out System.String Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Conversion_03()
        {
            var source = @"
using System;

class Base { }
class Derived : Base { }

record C(Derived X, Base Y)
{
    public Base X { get; init; }
    public Derived Y { get; init; }

    static void M(C c)
    {
        switch (c)
        {
            case C(Derived x, Base y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    static void Main()
    {
        M(new C(new Derived(), new Base()));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,18): error CS8866: Record member 'C.X' must be a readable instance property of type 'Derived' to match positional parameter 'X'.
                // record C(Derived X, Base Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("C.X", "Derived", "X").WithLocation(7, 18),
                // (7,26): error CS8866: Record member 'C.Y' must be a readable instance property of type 'Base' to match positional parameter 'Y'.
                // record C(Derived X, Base Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("C.Y", "Base", "Y").WithLocation(7, 26));

            Assert.Equal(
                "void C.Deconstruct(out Derived X, out Base Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList()
        {
            var source = @"
record C()
{
    static void M(C c)
    {
        switch (c)
        {
            case C():
                break;
        }
    }

    static void Main()
    {
        M(new C());
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,9): error CS8850: A positional record must have both a 'data' modifier and non-empty parameter list
                // record C()
                Diagnostic(ErrorCode.ERR_BadRecordDeclaration, "()").WithLocation(2, 9));

            Assert.Equal(
                "void C.Deconstruct()",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_UserDefined()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public void Deconstruct(out int X, out int Y)
    {
        X = this.X + 1;
        Y = this.Y + 2;
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(0, 0));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_01()
        {
            var source =
@"using System;

record B(int X, int Y)
{
    public void Deconstruct(out int Z)
    {
        Z = X + Y;
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
        switch (b)
        {
            case B(int z):
                Console.Write(z);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "123");
            verifier.VerifyDiagnostics();

            var expectedSymbols = new[]
            {
                "void B.Deconstruct(out System.Int32 Z)",
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
            };
            Assert.Equal(expectedSymbols, verifier.Compilation.GetMembers("B.Deconstruct").Select(s => s.ToTestDisplayString(includeNonNullable: false)));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_02()
        {
            var source =
@"using System;

record B(int X)
{
    public int Deconstruct(out int a) => throw null;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'B', with 1 out parameters and a void return type.
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(int x)").WithArguments("B", "1").WithLocation(11, 19));

            Assert.Equal("System.Int32 B.Deconstruct(out System.Int32 a)", comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_03()
        {
            var source =
@"using System;

record B(int X)
{
    public void Deconstruct(int X)
    {
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            var expectedSymbols = new[]
            {
                "void B.Deconstruct(System.Int32 X)",
                "void B.Deconstruct(out System.Int32 X)",
            };
            Assert.Equal(expectedSymbols, verifier.Compilation.GetMembers("B.Deconstruct").Select(s => s.ToTestDisplayString(includeNonNullable: false)));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_04()
        {
            var source =
@"using System;

record B(int X)
{
    public void Deconstruct(ref int X)
    {
    }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,19): error CS1620: Argument 1 must be passed with the 'ref' keyword
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_BadArgRef, "(int x)").WithArguments("1", "ref").WithLocation(13, 19),
                // (13,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'B', with 1 out parameters and a void return type.
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(int x)").WithArguments("B", "1").WithLocation(13, 19));

            Assert.Equal("void B.Deconstruct(ref System.Int32 X)", comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_UserDefined_DifferentSignature_05()
        {
            var source =
@"using System;

record A(int X)
{
    public A() : this(0) { }
    public int Deconstruct(out int a, out int b) => throw null;
}
record B(int X, int Y) : A(X)
{
    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X, out System.Int32 Y)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/45010")]
        [WorkItem(45010, "https://github.com/dotnet/roslyn/issues/45010")]
        public void Deconstruct_ObsoleteProperty()
        {
            var source =
@"using System;

record B(int X)
{
    [Obsolete] int X { get; } = X;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "1");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/45009")]
        [WorkItem(45009, "https://github.com/dotnet/roslyn/issues/45009")]
        public void Deconstruct_RefProperty()
        {
            var source =
@"using System;

record B(int X)
{
    static int _x = 2;
    ref int X => ref _x;

    static void M(B b)
    {
        switch (b)
        {
            case B(int x):
                Console.Write(x);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "2");
            verifier.VerifyDiagnostics();

            Assert.Equal("void B.Deconstruct(out System.Int32 X)", verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Static()
        {
            var source = @"
using System;

record B(int X, int Y)
{
    static int Y { get; }

    static void M(B b)
    {
        switch (b)
        {
            case B(int x, int y):
                Console.Write(x);
                Console.Write(y);
                break;
        }
    }

    public static void Main()
    {
        M(new B(1, 2));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,21): error CS8866: Record member 'B.Y' must be a readable instance property of type 'int' to match positional parameter 'Y'.
                // record B(int X, int Y)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Y").WithArguments("B.Y", "int", "Y").WithLocation(4, 21));

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Overrides_01()
        {
            var source =
@"record A
{
    public sealed override bool Equals(object other) => false;
    public sealed override int GetHashCode() => 0;
    public sealed override string ToString() => null;
}
record B(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,8): error CS0239: 'B.GetHashCode()': cannot override inherited member 'A.GetHashCode()' because it is sealed
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.GetHashCode()", "A.GetHashCode()").WithLocation(7, 8),
                // (7,8): error CS0239: 'B.Equals(object?)': cannot override inherited member 'A.Equals(object)' because it is sealed
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "B").WithArguments("B.Equals(object?)", "A.Equals(object)").WithLocation(7, 8));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "A B.<>Clone()",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "B..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 B.<X>k__BackingField",
                "System.Int32 B.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.X.init",
                "System.Int32 B.X { get; init; }",
                "System.Int32 B.<Y>k__BackingField",
                "System.Int32 B.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Y.init",
                "System.Int32 B.Y { get; init; }",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? )",
                "System.Boolean B.Equals(A? )",
                "System.Boolean B.Equals(B? )",
                "B..ctor(B )",
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Overrides_02()
        {
            var source =
@"abstract record A
{
    public abstract override bool Equals(object other);
    public abstract override int GetHashCode();
    public abstract override string ToString();
}
record B(int X, int Y) : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,8): error CS0534: 'B' does not implement inherited abstract member 'A.ToString()'
                // record B(int X, int Y) : A
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "B").WithArguments("B", "A.ToString()").WithLocation(7, 8));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "A B.<>Clone()",
                "System.Type B.EqualityContract.get",
                "System.Type B.EqualityContract { get; }",
                "B..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 B.<X>k__BackingField",
                "System.Int32 B.X.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.X.init",
                "System.Int32 B.X { get; init; }",
                "System.Int32 B.<Y>k__BackingField",
                "System.Int32 B.Y.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B.Y.init",
                "System.Int32 B.Y { get; init; }",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? )",
                "System.Boolean B.Equals(A? )",
                "System.Boolean B.Equals(B? )",
                "B..ctor(B )",
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44692, "https://github.com/dotnet/roslyn/issues/44692")]
        [Fact]
        public void DuplicateProperty_01()
        {
            var src =
@"record C(object Q)
{
    public object P { get; }
    public object P { get; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0102: The type 'C' already contains a definition for 'P'
                //     public object P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 19));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.Q { get; init; }",
                "System.Object C.P { get; }",
                "System.Object C.P { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [WorkItem(44692, "https://github.com/dotnet/roslyn/issues/44692")]
        [Fact]
        public void DuplicateProperty_02()
        {
            var src =
@"record C(object P, object Q)
{
    public object P { get; }
    public int P { get; }
    public int Q { get; }
    public object Q { get; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (1,27): error CS8866: Record member 'C.Q' must be a readable instance property of type 'object' to match positional parameter 'Q'.
                // record C(object P, object Q)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "Q").WithArguments("C.Q", "object", "Q").WithLocation(1, 27),
                // (4,16): error CS0102: The type 'C' already contains a definition for 'P'
                //     public int P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C", "P").WithLocation(4, 16),
                // (6,19): error CS0102: The type 'C' already contains a definition for 'Q'
                //     public object Q { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("C", "Q").WithLocation(6, 19));

            var actualMembers = GetProperties(comp, "C").ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "System.Type C.EqualityContract { get; }",
                "System.Object C.P { get; }",
                "System.Int32 C.P { get; }",
                "System.Int32 C.Q { get; }",
                "System.Object C.Q { get; }",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void DuplicateProperty_03()
        {
            var src =
@"record A
{
    public object P { get; }
    public object P { get; }
    public object Q { get; }
    public int Q { get; }
}
record B(object Q) : A
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0102: The type 'A' already contains a definition for 'P'
                //     public object P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("A", "P").WithLocation(4, 19),
                // (6,16): error CS0102: The type 'A' already contains a definition for 'Q'
                //     public int Q { get; }
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "Q").WithArguments("A", "Q").WithLocation(6, 16));

            var actualMembers = GetProperties(comp, "B").ToTestDisplayStrings();
            AssertEx.Equal(new[] { "System.Type B.EqualityContract { get; }" }, actualMembers);
        }

        [Fact]
        public void NominalRecordWith()
        {
            var src = @"
using System;
record C
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithExprReference(bool emitRef)
        {
            var src = @"
public record C
{
    public int X { get; init; }
}
public record D(int Y) : C;";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var src2 = @"
using System;
class E
{
    public static void Main()
    {
        var c = new C() { X = 1 };
        var c2 = c with { X = 2 };
        Console.WriteLine(c.X);
        Console.WriteLine(c2.X);

        var d = new D(2) { X = 1 };
        var d2 = d with { X = 2, Y = 3 };
        Console.WriteLine(d.X + "" "" + d.Y);
        Console.WriteLine(d2.X + "" ""  + d2.Y);

        C c3 = d;
        C c4 = d2;
        c3 = c3 with { X = 3 };
        c4 = c4 with { X = 4 };

        d = (D)c3;
        d2 = (D)c4;
        Console.WriteLine(d.X + "" "" + d.Y);
        Console.WriteLine(d2.X + "" ""  + d2.Y);
    }
}";
            var verifier = CompileAndVerify(src2,
                references: new[] { emitRef ? comp.EmitToImageReference() : comp.ToMetadataReference() },
                expectedOutput: @"
1
2
1 2
2 3
3 2
4 3");
            verifier.VerifyIL("E.Main", @"
{
  // Code size      318 (0x13e)
  .maxstack  3
  .locals init (C V_0, //c
                D V_1, //d
                D V_2, //d2
                C V_3, //c3
                C V_4, //c4
                int V_5)
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.1
  IL_0007:  callvirt   ""void C.X.init""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""C C.<>Clone()""
  IL_0013:  dup
  IL_0014:  ldc.i4.2
  IL_0015:  callvirt   ""void C.X.init""
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""int C.X.get""
  IL_0020:  call       ""void System.Console.WriteLine(int)""
  IL_0025:  callvirt   ""int C.X.get""
  IL_002a:  call       ""void System.Console.WriteLine(int)""
  IL_002f:  ldc.i4.2
  IL_0030:  newobj     ""D..ctor(int)""
  IL_0035:  dup
  IL_0036:  ldc.i4.1
  IL_0037:  callvirt   ""void C.X.init""
  IL_003c:  stloc.1
  IL_003d:  ldloc.1
  IL_003e:  callvirt   ""C C.<>Clone()""
  IL_0043:  castclass  ""D""
  IL_0048:  dup
  IL_0049:  ldc.i4.2
  IL_004a:  callvirt   ""void C.X.init""
  IL_004f:  dup
  IL_0050:  ldc.i4.3
  IL_0051:  callvirt   ""void D.Y.init""
  IL_0056:  stloc.2
  IL_0057:  ldloc.1
  IL_0058:  callvirt   ""int C.X.get""
  IL_005d:  stloc.s    V_5
  IL_005f:  ldloca.s   V_5
  IL_0061:  call       ""string int.ToString()""
  IL_0066:  ldstr      "" ""
  IL_006b:  ldloc.1
  IL_006c:  callvirt   ""int D.Y.get""
  IL_0071:  stloc.s    V_5
  IL_0073:  ldloca.s   V_5
  IL_0075:  call       ""string int.ToString()""
  IL_007a:  call       ""string string.Concat(string, string, string)""
  IL_007f:  call       ""void System.Console.WriteLine(string)""
  IL_0084:  ldloc.2
  IL_0085:  callvirt   ""int C.X.get""
  IL_008a:  stloc.s    V_5
  IL_008c:  ldloca.s   V_5
  IL_008e:  call       ""string int.ToString()""
  IL_0093:  ldstr      "" ""
  IL_0098:  ldloc.2
  IL_0099:  callvirt   ""int D.Y.get""
  IL_009e:  stloc.s    V_5
  IL_00a0:  ldloca.s   V_5
  IL_00a2:  call       ""string int.ToString()""
  IL_00a7:  call       ""string string.Concat(string, string, string)""
  IL_00ac:  call       ""void System.Console.WriteLine(string)""
  IL_00b1:  ldloc.1
  IL_00b2:  stloc.3
  IL_00b3:  ldloc.2
  IL_00b4:  stloc.s    V_4
  IL_00b6:  ldloc.3
  IL_00b7:  callvirt   ""C C.<>Clone()""
  IL_00bc:  dup
  IL_00bd:  ldc.i4.3
  IL_00be:  callvirt   ""void C.X.init""
  IL_00c3:  stloc.3
  IL_00c4:  ldloc.s    V_4
  IL_00c6:  callvirt   ""C C.<>Clone()""
  IL_00cb:  dup
  IL_00cc:  ldc.i4.4
  IL_00cd:  callvirt   ""void C.X.init""
  IL_00d2:  stloc.s    V_4
  IL_00d4:  ldloc.3
  IL_00d5:  castclass  ""D""
  IL_00da:  stloc.1
  IL_00db:  ldloc.s    V_4
  IL_00dd:  castclass  ""D""
  IL_00e2:  stloc.2
  IL_00e3:  ldloc.1
  IL_00e4:  callvirt   ""int C.X.get""
  IL_00e9:  stloc.s    V_5
  IL_00eb:  ldloca.s   V_5
  IL_00ed:  call       ""string int.ToString()""
  IL_00f2:  ldstr      "" ""
  IL_00f7:  ldloc.1
  IL_00f8:  callvirt   ""int D.Y.get""
  IL_00fd:  stloc.s    V_5
  IL_00ff:  ldloca.s   V_5
  IL_0101:  call       ""string int.ToString()""
  IL_0106:  call       ""string string.Concat(string, string, string)""
  IL_010b:  call       ""void System.Console.WriteLine(string)""
  IL_0110:  ldloc.2
  IL_0111:  callvirt   ""int C.X.get""
  IL_0116:  stloc.s    V_5
  IL_0118:  ldloca.s   V_5
  IL_011a:  call       ""string int.ToString()""
  IL_011f:  ldstr      "" ""
  IL_0124:  ldloc.2
  IL_0125:  callvirt   ""int D.Y.get""
  IL_012a:  stloc.s    V_5
  IL_012c:  ldloca.s   V_5
  IL_012e:  call       ""string int.ToString()""
  IL_0133:  call       ""string string.Concat(string, string, string)""
  IL_0138:  call       ""void System.Console.WriteLine(string)""
  IL_013d:  ret
}");
        }

        private static ImmutableArray<Symbol> GetProperties(CSharpCompilation comp, string typeName)
        {
            return comp.GetMember<NamedTypeSymbol>(typeName).GetMembers().WhereAsArray(m => m.Kind == SymbolKind.Property);
        }

        [Fact]
        public void BaseArguments_01()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

record C(int X, int Y) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123");
            verifier.VerifyIL("C..ctor(int, int)", @"

{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  ldarg.2
  IL_0019:  call       ""Base..ctor(int, int)""
  IL_001e:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, symbol.ContainingSymbol.DeclaredAccessibility);
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var baseWithargs = tree.GetRoot().DescendantNodes().OfType<SimpleBaseTypeSyntax>().Single();
            Assert.Equal("Base(X, Y)", baseWithargs.ToString());
            Assert.Null(model.GetTypeInfo(baseWithargs).Type);
            Assert.Null(model.GetSymbolInfo(baseWithargs).Symbol);
        }

        [Fact]
        public void BaseArguments_02()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

record C(int X) : Base(Test(X, out var y), y)
{
    public static void Main()
    {
        var c = new C(1);
    }

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var yDecl = OutVarTests.GetOutVarDeclaration(tree, "y");
            var yRef = OutVarTests.GetReferences(tree, "y").ToArray();
            Assert.Equal(2, yRef.Length);
            OutVarTests.VerifyModelForOutVar(model, yDecl, yRef[0]);
            OutVarTests.VerifyNotAnOutLocal(model, yRef[1]);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Test(X, out var y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var y = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").First();
            Assert.Equal("y", y.Parent!.ToString());
            Assert.Equal("(Test(X, out var y), y)", y.Parent!.Parent!.ToString());
            Assert.Equal("Base(Test(X, out var y), y)", y.Parent!.Parent!.Parent!.ToString());

            symbol = model.GetSymbolInfo(y).Symbol;
            Assert.Equal(SymbolKind.Local, symbol!.Kind);
            Assert.Equal("System.Int32 y", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "y"));
            Assert.Contains("y", model.LookupNames(x.SpanStart));

            var test = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").First();
            Assert.Equal("(Test(X, out var y), y)", test.Parent!.Parent!.Parent!.ToString());

            symbol = model.GetSymbolInfo(test).Symbol;
            Assert.Equal(SymbolKind.Method, symbol!.Kind);
            Assert.Equal("System.Int32 C.Test(System.Int32 x, out System.Int32 y)", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "Test"));
            Assert.Contains("Test", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_03()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,16): error CS8861: Unexpected argument list.
                // record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_04()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C(int X, int Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_05()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C : Base(X, Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 24),
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            foreach (var x in xs)
            {
                Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

                var symbolInfo = model.GetSymbolInfo(x);
                Assert.Null(symbolInfo.Symbol);
                Assert.Empty(symbolInfo.CandidateSymbols);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
                Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
                Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
            }
        }

        [Fact]
        public void BaseArguments_06()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C(int X, int Y) : Base(X, Y)
{
}

partial record C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(17, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_07()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial record C : Base(X, Y)
{
}

partial record C(int X, int Y) : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,24): error CS8861: Unexpected argument list.
                // partial record C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(13, 24)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_08()
        {
            var src = @"
record Base
{
    public Base(int Y)
    {
    }

    public Base() {}
}

record C(int X) : Base(Y)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,24): error CS0120: An object reference is required for the non-static field, method, or property 'C.Y'
                // record C(int X) : Base(Y)
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Y").WithArguments("C.Y").WithLocation(11, 24)
                );
        }

        [Fact]
        public void BaseArguments_09()
        {
            var src = @"
record Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

record C(int X) : Base(this.X)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,24): error CS0027: Keyword 'this' is not available in the current context
                // record C(int X) : Base(this.X)
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(11, 24)
                );
        }

        [Fact]
        public void BaseArguments_10()
        {
            var src = @"
record Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

record C(dynamic X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,27): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                // record C(dynamic X) : Base(X)
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(X)").WithLocation(11, 27)
                );
        }

        [Fact]
        public void BaseArguments_11()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X) : Base(Test(X, out var y), y)
{
    int Z = y;

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,13): error CS0103: The name 'y' does not exist in the current context
                //     int Z = y;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(13, 13)
                );
        }

        [Fact]
        public void BaseArguments_12()
        {
            var src = @"
using System;

class Base
{
    public Base(int X)
    {
    }
}

class C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (11,7): error CS7036: There is no argument given that corresponds to the required formal parameter 'X' of 'Base.Base(int)'
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("X", "Base.Base(int)").WithLocation(11, 7),
                // (11,15): error CS8861: Unexpected argument list.
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(11, 15)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_13()
        {
            var src = @"
using System;

interface Base
{
}

struct C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (8,16): error CS8861: Unexpected argument list.
                // struct C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(8, 16)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_14()
        {
            var src = @"
using System;

interface Base
{
}

interface C : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (8,19): error CS8861: Unexpected argument list.
                // interface C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(8, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent!.Parent!.Parent!.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_15()
        {
            var src = @"
using System;

record Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

partial record C
{
}

partial record C(int X, int Y) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }
}

partial record C
{
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123");
            verifier.VerifyIL("C..ctor(int, int)", @"

{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   123
  IL_0011:  stfld      ""int C.Z""
  IL_0016:  ldarg.0
  IL_0017:  ldarg.1
  IL_0018:  ldarg.2
  IL_0019:  call       ""Base..ctor(int, int)""
  IL_001e:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent!.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_16()
        {
            var src = @"
using System;

record Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

record C(int X) : Base(() => X)
{
    public static void Main()
    {
        var c = new C(1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"1");
        }

        [Fact]
        public void BaseArguments_17()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X, int y)
    : Base(Test(X, out var y),
           Test(X, out var z))
{
    int Z = z;

    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (12,28): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     : Base(Test(X, out var y),
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(12, 28),
                // (15,13): error CS0103: The name 'z' does not exist in the current context
                //     int Z = z;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "z").WithArguments("z").WithLocation(15, 13)
                );
        }

        [Fact]
        public void BaseArguments_18()
        {
            var src = @"
record Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

record C(int X, int y)
    : Base(Test(X + 1, out var z),
           Test(X + 2, out var z))
{
    private static int Test(int x, out int y)
    {
        y = 2;
        return x;
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,32): error CS0128: A local variable or function named 'z' is already defined in this scope
                //            Test(X + 2, out var z))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "z").WithArguments("z").WithLocation(13, 32)
                );
        }

        [Fact(Skip = "record struct")]
        public void Equality_01()
        {
            var source =
@"using static System.Console;
data struct S;
class Program
{
    static void Main()
    {
        var x = new S();
        var y = new S();
        WriteLine(x.Equals(y));
        WriteLine(((object)x).Equals(y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True");
            verifier.VerifyIL("S.Equals(in S)",
@"{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""System.Type S.EqualityContract.get""
  IL_0006:  ldarg.1
  IL_0007:  ldobj      ""S""
  IL_000c:  stloc.0
  IL_000d:  ldloca.s   V_0
  IL_000f:  call       ""System.Type S.EqualityContract.get""
  IL_0014:  ceq
  IL_0016:  ret
}");
            verifier.VerifyIL("S.Equals(object)",
@"{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (S V_0)
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""S""
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  ldc.i4.0
  IL_0009:  ret
  IL_000a:  ldarg.0
  IL_000b:  ldarg.1
  IL_000c:  unbox.any  ""S""
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""bool S.Equals(in S)""
  IL_0019:  ret
}");
        }

        [Fact]
        public void Equality_02()
        {
            var source =
@"using static System.Console;
record C;
class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        WriteLine(x.Equals(y) && x.GetHashCode() == y.GetHashCode());
        WriteLine(((object)x).Equals(y));
        WriteLine(((System.IEquatable<C>)x).Equals(y));
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
True");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  ceq
  IL_0011:  ret
  IL_0012:  ldc.i4.0
  IL_0013:  ret
}");
            verifier.VerifyIL("C.Equals(object)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void Equality_03()
        {
            var source =
@"using static System.Console;
record C
{
    private static int _nextId = 0;
    private int _id;
    public C() { _id = _nextId++; }
}
class Program
{
    static void Main()
    {
        var x = new C();
        var y = new C();
        WriteLine(x.Equals(x));
        WriteLine(x.Equals(y));
        WriteLine(y.Equals(y));
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"True
False
True");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0028
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type C.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0028
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int C._id""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int C._id""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       40 (0x28)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<System.Type> System.Collections.Generic.EqualityComparer<System.Type>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Type C.EqualityContract.get""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<System.Type>.GetHashCode(System.Type)""
  IL_0010:  ldc.i4     0xa5555529
  IL_0015:  mul
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""int C._id""
  IL_0021:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_0026:  add
  IL_0027:  ret
}");
        }

        [Fact]
        public void Equality_04()
        {
            var source =
@"using static System.Console;
record A;
record B1(int P) : A
{
    internal B1() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
record B2(int P) : A
{
    internal B2() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
class Program
{
    static B1 NewB1(int p) => new B1 { P = p }; // Use record base call syntax instead
    static B2 NewB2(int p) => new B2 { P = p }; // Use record base call syntax instead
    static void Main()
    {
        WriteLine(new A().Equals(NewB1(1)));
        WriteLine(NewB1(1).Equals(new A()));
        WriteLine(NewB1(1).Equals(NewB2(1)));
        WriteLine(new A().Equals((A)NewB2(1)));
        WriteLine(((A)NewB2(1)).Equals(new A()));
        WriteLine(((A)NewB2(1)).Equals(NewB2(1)) && ((A)NewB2(1)).GetHashCode() == NewB2(1).GetHashCode());
        WriteLine(NewB2(1).Equals((A)NewB2(1)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"False
False
False
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  ceq
  IL_0011:  ret
  IL_0012:  ldc.i4.0
  IL_0013:  ret
}");
            verifier.VerifyIL("B1.Equals(B1)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int B1.<P>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int B1.<P>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("B1.GetHashCode()",
@"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int B1.<P>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_001c:  add
  IL_001d:  ret
}");
        }

        [Fact]
        public void Equality_05()
        {
            var source =
@"using static System.Console;
record A(int P)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int P { get; set; } // Use record base call syntax instead
}
record B1(int P) : A
{
    internal B1() : this(0) { } // Use record base call syntax instead
}
record B2(int P) : A
{
    internal B2() : this(0) { } // Use record base call syntax instead
}
class Program
{
    static A NewA(int p) => new A { P = p }; // Use record base call syntax instead
    static B1 NewB1(int p) => new B1 { P = p }; // Use record base call syntax instead
    static B2 NewB2(int p) => new B2 { P = p }; // Use record base call syntax instead
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(2)));
        WriteLine(NewA(1).Equals(NewA(1)) && NewA(1).GetHashCode() == NewA(1).GetHashCode());
        WriteLine(NewA(1).Equals(NewB1(1)));
        WriteLine(NewB1(1).Equals(NewA(1)));
        WriteLine(NewB1(1).Equals(NewB2(1)));
        WriteLine(NewA(1).Equals((A)NewB2(1)));
        WriteLine(((A)NewB2(1)).Equals(NewA(1)));
        WriteLine(((A)NewB2(1)).Equals(NewB2(1)));
        WriteLine(NewB2(1).Equals((A)NewB2(1)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"False
True
False
False
False
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0028
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0028
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int A.<P>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int A.<P>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}");
            verifier.VerifyIL("B1.Equals(B1)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  ret
}");
            verifier.VerifyIL("B1.GetHashCode()",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int A.GetHashCode()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void Equality_06()
        {
            var source =
@"
using System;
using static System.Console;
record A;
record B : A
{
    protected override Type EqualityContract => typeof(A);
    public override bool Equals(A a) => base.Equals(a);
    public virtual bool Equals(B b) => base.Equals((A)b);
}
record C : B;
class Program
{
    static void Main()
    {
        WriteLine(new A().Equals(new A()));
        WriteLine(new A().Equals(new B()));
        WriteLine(new A().Equals(new C()));
        WriteLine(new B().Equals(new A()));
        WriteLine(new B().Equals(new B()));
        WriteLine(new B().Equals(new C()));
        WriteLine(new C().Equals(new A()));
        WriteLine(new C().Equals(new B()));
        WriteLine(new C().Equals(new C()));
        WriteLine(((A)new C()).Equals(new A()));
        WriteLine(((A)new C()).Equals(new B()));
        WriteLine(((A)new C()).Equals(new C()));
        WriteLine(new C().Equals((A)new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True
False
True
True
False
False
False
True
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  ceq
  IL_0011:  ret
  IL_0012:  ldc.i4.0
  IL_0013:  ret
}");
            verifier.VerifyIL("C.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void Equality_07()
        {
            var source =
@"using System;
using static System.Console;
record A;
record B : A;
record C : B;
class Program
{
    static void Main()
    {
        WriteLine(new A().Equals(new A()));
        WriteLine(new A().Equals(new B()));
        WriteLine(new A().Equals(new C()));
        WriteLine(new B().Equals(new A()));
        WriteLine(new B().Equals(new B()));
        WriteLine(new B().Equals(new C()));
        WriteLine(new C().Equals(new A()));
        WriteLine(new C().Equals(new B()));
        WriteLine(new C().Equals(new C()));
        WriteLine(((A)new B()).Equals(new A()));
        WriteLine(((A)new B()).Equals(new B()));
        WriteLine(((A)new B()).Equals(new C()));
        WriteLine(((A)new C()).Equals(new A()));
        WriteLine(((A)new C()).Equals(new B()));
        WriteLine(((A)new C()).Equals(new C()));
        WriteLine(((B)new C()).Equals(new A()));
        WriteLine(((B)new C()).Equals(new B()));
        WriteLine(((B)new C()).Equals(new C()));
        WriteLine(new C().Equals((A)new C()));
        WriteLine(((IEquatable<A>)new B()).Equals(new A()));
        WriteLine(((IEquatable<A>)new B()).Equals(new B()));
        WriteLine(((IEquatable<A>)new B()).Equals(new C()));
        WriteLine(((IEquatable<A>)new C()).Equals(new A()));
        WriteLine(((IEquatable<A>)new C()).Equals(new B()));
        WriteLine(((IEquatable<A>)new C()).Equals(new C()));
        WriteLine(((IEquatable<B>)new C()).Equals(new A()));
        WriteLine(((IEquatable<B>)new C()).Equals(new B()));
        WriteLine(((IEquatable<B>)new C()).Equals(new C()));
        WriteLine(((IEquatable<C>)new C()).Equals(new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
False
True
False
False
False
True
False
True
False
False
False
True
False
False
True
True
False
True
False
False
False
True
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  ceq
  IL_0011:  ret
  IL_0012:  ldc.i4.0
  IL_0013:  ret
}");
            verifier.VerifyIL("B.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""B""
  IL_0007:  callvirt   ""bool B.Equals(B)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(B)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  ret
}");

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.get_EqualityContract"), isOverride: false);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.get_EqualityContract"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C.get_EqualityContract"), isOverride: true);

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.<>Clone"), isOverride: false);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.<>Clone"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C.<>Clone"), isOverride: true);

            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.GetHashCode"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.GetHashCode"), isOverride: true);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("C.GetHashCode"), isOverride: true);

            VerifyVirtualMethods(comp.GetMembers("A.Equals"), ("System.Boolean A.Equals(A? )", false), ("System.Boolean A.Equals(System.Object? )", true));
            VerifyVirtualMethods(comp.GetMembers("B.Equals"), ("System.Boolean B.Equals(B? )", false), ("System.Boolean B.Equals(A? )", true), ("System.Boolean B.Equals(System.Object? )", true));
            VerifyVirtualMethods(comp.GetMembers("C.Equals"), ("System.Boolean C.Equals(C? )", false), ("System.Boolean C.Equals(B? )", true), ("System.Boolean C.Equals(A? )", true), ("System.Boolean C.Equals(System.Object? )", true));
        }

        private static void VerifyVirtualMethod(MethodSymbol method, bool isOverride)
        {
            Assert.Equal(!isOverride, method.IsVirtual);
            Assert.Equal(isOverride, method.IsOverride);
            Assert.True(method.IsMetadataVirtual());
            Assert.Equal(!isOverride, method.IsMetadataNewSlot());
        }

        private static void VerifyVirtualMethods(ImmutableArray<Symbol> members, params (string displayString, bool isOverride)[] values)
        {
            Assert.Equal(members.Length, values.Length);
            for (int i = 0; i < members.Length; i++)
            {
                var method = (MethodSymbol)members[i];
                (string displayString, bool isOverride) = values[i];
                Assert.Equal(displayString, method.ToTestDisplayString(includeNonNullable: true));
                VerifyVirtualMethod(method, isOverride);
            }
        }

        [WorkItem(44895, "https://github.com/dotnet/roslyn/issues/44895")]
        [Fact]
        public void Equality_08()
        {
            var source =
@"
using System;
using static System.Console;
record A(int X)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int X { get; set; } // Use record base call syntax instead
}
record B : A
{
    internal B() { } // Use record base call syntax instead
    internal B(int X, int Y) : base(X) { this.Y = Y; }
    internal int Y { get; set; }
    protected override Type EqualityContract => typeof(A);
    public override bool Equals(A a) => base.Equals(a);
    public virtual bool Equals(B b) => base.Equals((A)b);
}
record C(int X, int Y, int Z) : B
{
    internal C() : this(0, 0, 0) { } // Use record base call syntax instead
    internal int Z { get; set; } // Use record base call syntax instead
}
class Program
{
    static A NewA(int x) => new A { X = x }; // Use record base call syntax instead
    static B NewB(int x, int y) => new B { X = x, Y = y };
    static C NewC(int x, int y, int z) => new C { X = x, Y = y, Z = z };
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(1)));
        WriteLine(NewA(1).Equals(NewB(1, 2)));
        WriteLine(NewA(1).Equals(NewC(1, 2, 3)));
        WriteLine(NewB(1, 2).Equals(NewA(1)));
        WriteLine(NewB(1, 2).Equals(NewB(1, 2)));
        WriteLine(NewB(1, 2).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewA(1)));
        WriteLine(NewC(1, 2, 3).Equals(NewB(1, 2)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(4, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 4)));
        WriteLine(((A)NewB(1, 2)).Equals(NewA(1)));
        WriteLine(((A)NewB(1, 2)).Equals(NewB(1, 2)));
        WriteLine(((A)NewB(1, 2)).Equals(NewC(1, 2, 3)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)) && NewC(1, 2, 3).GetHashCode() == NewC(1, 2, 3).GetHashCode());
        WriteLine(NewC(1, 2, 3).Equals((A)NewC(1, 2, 3)));
    }
}";
            // https://github.com/dotnet/roslyn/issues/44895: C.Equals() should compare B.Y.
            var verifier = CompileAndVerify(source, expectedOutput:
@"True
True
False
True
True
False
False
False
True
False
True
False
True
True
False
False
False
True
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0028
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0028
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int A.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int A.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}");
            verifier.VerifyIL("C.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            // https://github.com/dotnet/roslyn/issues/44895: C.Equals() should compare B.Y.
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int C.<Z>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int C.<Z>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("C.GetHashCode()",
@"{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  call       ""int B.GetHashCode()""
  IL_0006:  ldc.i4     0xa5555529
  IL_000b:  mul
  IL_000c:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int C.<Z>k__BackingField""
  IL_0017:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_001c:  add
  IL_001d:  ret
}");
        }

        [Fact]
        public void Equality_09()
        {
            var source =
@"using static System.Console;
record A(int X)
{
    internal A() : this(0) { } // Use record base call syntax instead
    internal int X { get; set; } // Use record base call syntax instead
}
record B(int X, int Y) : A
{
    internal B() : this(0, 0) { } // Use record base call syntax instead
    internal int Y { get; set; }
}
record C(int X, int Y, int Z) : B
{
    internal C() : this(0, 0, 0) { } // Use record base call syntax instead
    internal int Z { get; set; } // Use record base call syntax instead
}
class Program
{
    static A NewA(int x) => new A { X = x }; // Use record base call syntax instead
    static B NewB(int x, int y) => new B { X = x, Y = y };
    static C NewC(int x, int y, int z) => new C { X = x, Y = y, Z = z };
    static void Main()
    {
        WriteLine(NewA(1).Equals(NewA(1)));
        WriteLine(NewA(1).Equals(NewB(1, 2)));
        WriteLine(NewA(1).Equals(NewC(1, 2, 3)));
        WriteLine(NewB(1, 2).Equals(NewA(1)));
        WriteLine(NewB(1, 2).Equals(NewB(1, 2)));
        WriteLine(NewB(1, 2).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewA(1)));
        WriteLine(NewC(1, 2, 3).Equals(NewB(1, 2)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(4, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 3)));
        WriteLine(NewC(1, 2, 3).Equals(NewC(1, 4, 4)));
        WriteLine(((A)NewB(1, 2)).Equals(NewA(1)));
        WriteLine(((A)NewB(1, 2)).Equals(NewB(1, 2)));
        WriteLine(((A)NewB(1, 2)).Equals(NewC(1, 2, 3)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((A)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewA(1)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewB(1, 2)));
        WriteLine(((B)NewC(1, 2, 3)).Equals(NewC(1, 2, 3)));
        WriteLine(NewC(1, 2, 3).Equals((A)NewC(1, 2, 3)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
False
True
False
False
False
True
False
False
False
False
True
False
False
False
True
False
False
True
True");
            verifier.VerifyIL("A.Equals(A)",
@"{
  // Code size       42 (0x2a)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0028
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A.EqualityContract.get""
  IL_000f:  bne.un.s   IL_0028
  IL_0011:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""int A.<X>k__BackingField""
  IL_001c:  ldarg.1
  IL_001d:  ldfld      ""int A.<X>k__BackingField""
  IL_0022:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0027:  ret
  IL_0028:  ldc.i4.0
  IL_0029:  ret
}");
            verifier.VerifyIL("B.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""B""
  IL_0007:  callvirt   ""bool B.Equals(B)""
  IL_000c:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.Equals(A)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int B.<Y>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int B.<Y>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
            verifier.VerifyIL("C.Equals(A)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(B)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""C""
  IL_0007:  callvirt   ""bool C.Equals(C)""
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Equals(C)",
@"{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool B.Equals(B)""
  IL_0007:  brfalse.s  IL_0020
  IL_0009:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_000e:  ldarg.0
  IL_000f:  ldfld      ""int C.<Z>k__BackingField""
  IL_0014:  ldarg.1
  IL_0015:  ldfld      ""int C.<Z>k__BackingField""
  IL_001a:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_001f:  ret
  IL_0020:  ldc.i4.0
  IL_0021:  ret
}");
        }

        [Fact]
        public void Equality_11()
        {
            var source =
@"using System;
record A
{
    protected virtual Type EqualityContract => typeof(object);
}
record B1(object P) : A;
record B2(object P) : A;
class Program
{
    static void Main()
    {
        Console.WriteLine(new A().Equals(new A()));
        Console.WriteLine(new A().Equals(new B1((object)null)));
        Console.WriteLine(new B1((object)null).Equals(new A()));
        Console.WriteLine(new B1((object)null).Equals(new B1((object)null)));
        Console.WriteLine(new B1((object)null).Equals(new B2((object)null)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
False
True
False");
        }

        [Fact]
        public void Equality_12()
        {
            var source =
@"using System;
abstract record A
{
    public A() { }
    protected abstract Type EqualityContract { get; }
}
record B1(object P) : A;
record B2(object P) : A;
class Program
{
    static void Main()
    {
        var b1 = new B1((object)null);
        var b2 = new B2((object)null);
        Console.WriteLine(b1.Equals(b1));
        Console.WriteLine(b1.Equals(b2));
        Console.WriteLine(((A)b1).Equals(b1));
        Console.WriteLine(((A)b1).Equals(b2));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
True
False");
        }

        [Fact]
        public void Equality_13()
        {
            var source =
@"record A
{
    protected System.Type EqualityContract => typeof(A);
}
record B : A;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,8): error CS0506: 'B.EqualityContract': cannot override inherited member 'A.EqualityContract' because it is not marked virtual, abstract, or override
                // record B : A;
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.EqualityContract", "A.EqualityContract").WithLocation(5, 8));
        }

        [Fact]
        public void Equality_14()
        {
            var source =
@"record A;
record B : A
{
    protected sealed override System.Type EqualityContract => typeof(B);
}
record C : B;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,8): error CS0239: 'C.EqualityContract': cannot override inherited member 'B.EqualityContract' because it is sealed
                // record C : B;
                Diagnostic(ErrorCode.ERR_CantOverrideSealed, "C").WithArguments("C.EqualityContract", "B.EqualityContract").WithLocation(6, 8));

            var actualMembers = comp.GetMember<NamedTypeSymbol>("B").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "A B.<>Clone()",
                "System.Type B.EqualityContract { get; }",
                "System.Type B.EqualityContract.get",
                "System.Int32 B.GetHashCode()",
                "System.Boolean B.Equals(System.Object? )",
                "System.Boolean B.Equals(A? )",
                "System.Boolean B.Equals(B? )",
                "B..ctor(B )",
                "B..ctor()",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void Equality_15()
        {
            var source =
@"using System;
record A;
record B1 : A
{
    public B1(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(A);
    public virtual bool Equals(B1 o) => base.Equals((A)o);
}
record B2 : A
{
    public B2(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(B2);
    public virtual bool Equals(B2 o) => base.Equals((A)o);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new B1(1).Equals(new B1(2)));
        Console.WriteLine(new B1(1).Equals(new B2(1)));
        Console.WriteLine(new B2(1).Equals(new B2(2)));
    }
}";
            CompileAndVerify(source, expectedOutput:
@"True
False
True");
        }

        [Fact]
        public void Equality_16()
        {
            var source =
@"using System;
record A;
record B1 : A
{
    public B1(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(string);
    public override bool Equals(A a) => base.Equals(a);
    public virtual bool Equals(B1 b) => base.Equals((A)b);
}
record B2 : A
{
    public B2(int p) { P = p; }
    public int P { get; set; }
    protected override Type EqualityContract => typeof(string);
    public override bool Equals(A a) => base.Equals(a);
    public virtual bool Equals(B2 b) => base.Equals((A)b);
}
class Program
{
    static void Main()
    {
        Console.WriteLine(new B1(1).Equals(new B1(2)));
        Console.WriteLine(new B1(1).Equals(new B2(2)));
        Console.WriteLine(new B2(1).Equals(new B2(2)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"True
True
True");
        }

        [Fact]
        public void Equality_17()
        {
            var source =
@"using static System.Console;
record A;
record B1(int P) : A
{
    public override bool Equals(A other) => false;
}
record B2(int P) : A
{
    public override bool Equals(A other) => true;
}
class Program
{
    static void Main()
    {
        WriteLine(new B1(1).Equals(new B1(1)));
        WriteLine(new B1(1).Equals(new B1(2)));
        WriteLine(new B2(3).Equals(new B2(3)));
        WriteLine(new B2(3).Equals(new B2(4)));
        WriteLine(((A)new B1(1)).Equals(new B1(1)));
        WriteLine(((A)new B1(1)).Equals(new B1(2)));
        WriteLine(((A)new B2(3)).Equals(new B2(3)));
        WriteLine(((A)new B2(3)).Equals(new B2(4)));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            // init-only is unverifiable
            CompileAndVerify(comp, verify: Verification.Skipped, expectedOutput:
@"True
False
True
False
False
False
True
True");
            var actualMembers = comp.GetMember<NamedTypeSymbol>("B1").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "A B1.<>Clone()",
                "System.Type B1.EqualityContract.get",
                "System.Type B1.EqualityContract { get; }",
                "B1..ctor(System.Int32 P)",
                "System.Int32 B1.<P>k__BackingField",
                "System.Int32 B1.P.get",
                "void modreq(System.Runtime.CompilerServices.IsExternalInit) B1.P.init",
                "System.Int32 B1.P { get; init; }",
                "System.Boolean B1.Equals(A other)",
                "System.Int32 B1.GetHashCode()",
                "System.Boolean B1.Equals(System.Object? )",
                "System.Boolean B1.Equals(B1? )",
                "B1..ctor(B1 )",
                "void B1.Deconstruct(out System.Int32 P)"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Equality_18(bool useCompilationReference)
        {
            var sourceA = @"public record A;";
            var comp = CreateCompilation(sourceA);
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("A.get_EqualityContract"), isOverride: false);
            VerifyVirtualMethods(comp.GetMembers("A.Equals"), ("System.Boolean A.Equals(A? )", false), ("System.Boolean A.Equals(System.Object? )", true));
            var refA = useCompilationReference ? comp.ToMetadataReference() : comp.EmitToImageReference();

            var sourceB = @"record B : A;";
            comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            VerifyVirtualMethod(comp.GetMember<MethodSymbol>("B.get_EqualityContract"), isOverride: true);
            VerifyVirtualMethods(comp.GetMembers("B.Equals"), ("System.Boolean B.Equals(B? )", false), ("System.Boolean B.Equals(A? )", true), ("System.Boolean B.Equals(System.Object? )", true));
        }

        [Fact]
        public void Equality_19()
        {
            var source =
@"using static System.Console;
record A<T>;
record B : A<int>;
class Program
{
    static void Main()
    {
        WriteLine(new A<int>().Equals(new A<int>()));
        WriteLine(new A<int>().Equals(new B()));
        WriteLine(new B().Equals(new A<int>()));
        WriteLine(new B().Equals(new B()));
        WriteLine(((A<int>)new B()).Equals(new A<int>()));
        WriteLine(((A<int>)new B()).Equals(new B()));
        WriteLine(new B().Equals((A<int>)new B()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
False
False
True
False
True
True");
            verifier.VerifyIL("A<T>.Equals(A<T>)",
@"{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  brfalse.s  IL_0012
  IL_0003:  ldarg.0
  IL_0004:  callvirt   ""System.Type A<T>.EqualityContract.get""
  IL_0009:  ldarg.1
  IL_000a:  callvirt   ""System.Type A<T>.EqualityContract.get""
  IL_000f:  ceq
  IL_0011:  ret
  IL_0012:  ldc.i4.0
  IL_0013:  ret
}");
            verifier.VerifyIL("B.Equals(A<int>)",
@"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  isinst     ""B""
  IL_0007:  callvirt   ""bool B.Equals(B)""
  IL_000c:  ret
}");
            verifier.VerifyIL("B.Equals(B)",
@"{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A<int>.Equals(A<int>)""
  IL_0007:  ret
}");
        }

        [Fact]
        public void Equality_20()
        {
            var source =
@"
record C;
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // record C;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C;").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1)
                );
        }

        [Fact]
        public void Equality_21()
        {
            var source =
@"
record C;
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // record C;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record C;").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1)
                );
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44988")]
        [WorkItem(44988, "https://github.com/dotnet/roslyn/issues/44988")]
        public void Equality_22()
        {
            var source =
@"
record C
{
    int x = 0;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.MakeMemberMissing(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact]
        public void IEquatableT_01()
        {
            var source =
@"record A<T>;
record B : A<int>;
class Program
{
    static void F<T>(System.IEquatable<T> t)
    {
    }
    static void M<T>()
    {
        F(new A<T>());
        F(new B());
        F<A<int>>(new B());
        F<B>(new B());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'Program.F<T>(IEquatable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(new B());
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.IEquatable<T>)").WithLocation(11, 9));
        }

        [Fact]
        public void IEquatableT_02()
        {
            var source =
@"using System;
record A;
record B<T> : A;
record C : B<int>;
class Program
{
    static string F<T>(IEquatable<T> t)
    {
        return typeof(T).Name;
    }
    static void Main()
    {
        Console.WriteLine(F(new A()));
        Console.WriteLine(F<A>(new C()));
        Console.WriteLine(F<B<int>>(new C()));
        Console.WriteLine(F<C>(new C()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"A
A
B`1
C");
        }

        [Fact]
        public void IEquatableT_03()
        {
            var source =
@"#nullable enable
using System;
record A<T> : IEquatable<A<T>>
{
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B?>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B?>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B?>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_04()
        {
            var source =
@"using System;
record A<T>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    public virtual bool Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>
{
    public override bool Equals(A<object> other) => Report(""B.Equals(A<object>)"");
    public virtual bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(A<object>)
B.Equals(B)
A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(B)");

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_05()
        {
            var source =
@"using System;
record A<T> : IEquatable<A<T>>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    public virtual bool Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B>
{
    public override bool Equals(A<object> other) => Report(""B.Equals(A<object>)"");
    public virtual bool Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(A<object>)
B.Equals(B)
A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(B)");

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_06()
        {
            var source =
@"using System;
record A<T> : IEquatable<A<T>>
{
    internal static bool Report(string s) { Console.WriteLine(s); return false; }
    bool IEquatable<A<T>>.Equals(A<T> other) => Report(""A<T>.Equals(A<T>)"");
}
record B : A<object>, IEquatable<A<object>>, IEquatable<B>
{
    bool IEquatable<A<object>>.Equals(A<object> other) => Report(""B.Equals(A<object>)"");
    bool IEquatable<B>.Equals(B other) => Report(""B.Equals(B)"");
}
class Program
{
    static void Main()
    {
        var a = new A<object>();
        var b = new B();
        _ = a.Equals(b);
        _ = ((A<object>)b).Equals(b);
        _ = b.Equals(a);
        _ = b.Equals(b);
        _ = ((IEquatable<A<object>>)a).Equals(b);
        _ = ((IEquatable<A<object>>)b).Equals(b);
        _ = ((IEquatable<B>)b).Equals(b);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput:
@"A<T>.Equals(A<T>)
B.Equals(A<object>)
B.Equals(B)");

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_07()
        {
            var source =
@"using System;
record A<T> : IEquatable<B1>, IEquatable<B2>
{
    bool IEquatable<B1>.Equals(B1 other) => false;
    bool IEquatable<B2>.Equals(B2 other) => false;
}
record B1 : A<object>;
record B2 : A<int>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<B2>", "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<B2>", "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B1");
            AssertEx.Equal(new[] { "System.IEquatable<B1>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B2>", "System.IEquatable<A<System.Object>>", "System.IEquatable<B1>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B2");
            AssertEx.Equal(new[] { "System.IEquatable<B2>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<B1>", "System.IEquatable<A<System.Int32>>", "System.IEquatable<B2>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_08()
        {
            var source =
@"interface I<T>
{
}
record A<T> : I<A<T>>
{
}
record B : A<object>, I<A<object>>, I<B>
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "I<A<T>>", "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "I<A<T>>", "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "I<A<System.Object>>", "I<B>", "System.IEquatable<B>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Object>>", "I<A<System.Object>>", "I<B>", "System.IEquatable<B>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_09()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A<T>;
record B : A<int>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8));

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_10()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A<T> : System.IEquatable<A<T>>;
record B : A<int>, System.IEquatable<B>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.GetHashCode()': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.GetHashCode()").WithLocation(1, 8),
                // (1,8): error CS0115: 'A<T>.Equals(object?)': no suitable method found to override
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.Equals(object?)").WithLocation(1, 8),
                // (1,15): error CS8864: Records may only inherit from object or another record
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_BadRecordBase, "System.IEquatable<A<T>>").WithLocation(1, 15),
                // (1,22): error CS0234: The type or namespace name 'IEquatable<>' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // record A<T> : System.IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IEquatable<A<T>>").WithArguments("IEquatable<>", "System").WithLocation(1, 22),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,27): error CS0234: The type or namespace name 'IEquatable<>' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // record B : A<int>, System.IEquatable<B>;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IEquatable<B>").WithArguments("IEquatable<>", "System").WithLocation(2, 27));

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "System.IEquatable<B>", "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "System.IEquatable<B>", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_11()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"using System;
record A<T> : IEquatable<A<T>>;
record B : A<int>, IEquatable<B>;
";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.GetHashCode()': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.GetHashCode()").WithLocation(2, 8),
                // (2,8): error CS0115: 'A<T>.Equals(object?)': no suitable method found to override
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "A").WithArguments("A<T>.Equals(object?)").WithLocation(2, 8),
                // (2,15): error CS0246: The type or namespace name 'IEquatable<>' could not be found (are you missing a using directive or an assembly reference?)
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IEquatable<A<T>>").WithArguments("IEquatable<>").WithLocation(2, 15),
                // (2,15): error CS8864: Records may only inherit from object or another record
                // record A<T> : IEquatable<A<T>>;
                Diagnostic(ErrorCode.ERR_BadRecordBase, "IEquatable<A<T>>").WithLocation(2, 15),
                // (3,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(3, 8),
                // (3,8): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "B").WithArguments("System.IEquatable`1").WithLocation(3, 8),
                // (3,20): error CS0246: The type or namespace name 'IEquatable<>' could not be found (are you missing a using directive or an assembly reference?)
                // record B : A<int>, IEquatable<B>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IEquatable<B>").WithArguments("IEquatable<>").WithLocation(3, 20));

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());

            type = comp.GetMember<NamedTypeSymbol>("B");
            AssertEx.Equal(new[] { "IEquatable<B>", "System.IEquatable<B>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<System.Int32>>[missing]", "IEquatable<B>", "System.IEquatable<B>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_12()
        {
            var source0 =
@"namespace System
{
    public class Object
    {
        public virtual bool Equals(object other) => false;
        public virtual int GetHashCode() => 0;
    }
    public class String { }
    public abstract class ValueType { }
    public struct Void { }
    public struct Boolean { }
    public struct Int32 { }
    public interface IEquatable<T>
    {
        bool Equals(T other);
        void Other();
    }
}";
            var comp = CreateEmptyCompilation(source0);
            comp.VerifyDiagnostics();
            var ref0 = comp.EmitToImageReference();

            var source1 =
@"record A;
class Program
{
    static void Main()
    {
        System.IEquatable<A> a = new A();
        _ = a.Equals(null);
    }
}";
            comp = CreateEmptyCompilation(source1, references: new[] { ref0 }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,8): error CS0535: 'A' does not implement interface member 'IEquatable<A>.Other()'
                // record A;
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "A").WithArguments("A", "System.IEquatable<A>.Other()").WithLocation(1, 8));
        }

        [Fact]
        public void IEquatableT_13()
        {
            var source =
@"record A
{
    internal virtual bool Equals(A other) => false;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,8): error CS0737: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is not public.
                // record A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(1, 8));
        }

        [Fact]
        public void IEquatableT_14()
        {
            var source =
@"record A
{
    public bool Equals(A other) => false;
}
record B : A
{
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,8): error CS0506: 'B.Equals(A?)': cannot override inherited member 'A.Equals(A)' because it is not marked virtual, abstract, or override
                // record B : A
                Diagnostic(ErrorCode.ERR_CantOverrideNonVirtual, "B").WithArguments("B.Equals(A?)", "A.Equals(A)").WithLocation(5, 8));
        }

        [WorkItem(45026, "https://github.com/dotnet/roslyn/issues/45026")]
        [Fact]
        public void IEquatableT_15()
        {
            var source =
@"using System;
record R
{
    bool IEquatable<R>.Equals(R other) => false;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_16()
        {
            var source =
@"using System;
class A<T>
{
    record B<U> : IEquatable<B<T>>
    {
        bool IEquatable<B<T>>.Equals(B<T> other) => false;
        bool IEquatable<B<U>>.Equals(B<U> other) => false;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,12): error CS0695: 'A<T>.B<U>' cannot implement both 'IEquatable<A<T>.B<T>>' and 'IEquatable<A<T>.B<U>>' because they may unify for some type parameter substitutions
                //     record B<U> : IEquatable<B<T>>
                Diagnostic(ErrorCode.ERR_UnifyingInterfaceInstantiations, "B").WithArguments("A<T>.B<U>", "System.IEquatable<A<T>.B<T>>", "System.IEquatable<A<T>.B<U>>").WithLocation(4, 12));
        }

        [Fact]
        public void Initializers_01()
        {
            var src = @"
using System;

record C(int X)
{
    int Z = X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_02()
        {
            var src = @"
record C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,20): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 20)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; init; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_03()
        {
            var src = @"
record C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.X'
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "X").WithArguments("C.X").WithLocation(4, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Property, symbol!.Kind);
            Assert.Equal("System.Int32 C.X { get; init; }", symbol.ToTestDisplayString());
            Assert.Equal("C", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_04()
        {
            var src = @"
using System;

record C(int X)
{
    Func<int> Z = () => X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("() => X + 1", x.Parent!.Parent!.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol!.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("lambda expression", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void Initializers_05()
        {
            var src = @"
using System;

record Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

record C(int X) : Base(() => 100 + X++)
{
    Func<int> Y = () => 200 + X++;
    Func<int> Z = () => 300 + X++;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Y());
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
101
202
303
");
        }
    }
}

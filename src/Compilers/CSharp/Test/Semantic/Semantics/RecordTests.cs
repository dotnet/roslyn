// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class RecordTests : CompilingTestBase
    {
        private static CSharpCompilation CreateCompilation(CSharpTestSource source)
            => CSharpTestBase.CreateCompilation(source, parseOptions: TestOptions.RegularPreview);

        private CompilationVerifier CompileAndVerify(CSharpTestSource src, string expectedOutput)
            => base.CompileAndVerify(src, expectedOutput: expectedOutput, parseOptions: TestOptions.RegularPreview);

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
  // Code size       49 (0x31)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int C.<X>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int C.<X>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_002f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""int C.<Y>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""int C.<Y>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_002e:  ret
  IL_002f:  ldc.i4.0
  IL_0030:  ret
}");
        }
    }
}
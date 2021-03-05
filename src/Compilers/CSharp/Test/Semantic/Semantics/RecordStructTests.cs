// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    [CompilerTrait(CompilerFeature.RecordStructs)]
    public class RecordStructTests : CompilingTestBase
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

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void StructRecord1()
        {
            var src = @"
data struct Point(int X, int Y);";

            var verifier = CompileAndVerify(src).VerifyDiagnostics();
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

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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
False").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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
True").VerifyDiagnostics();

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

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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
s").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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
s").VerifyDiagnostics();
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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

        [Fact(Skip = "PROTOTYPE(record-structs)")]
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseExe);
            var verifier = CompileAndVerify(comp, expectedOutput:
@"True
True").VerifyDiagnostics();

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

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void RecordClone4_0()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E;
    public int Z;
}");
            comp.VerifyDiagnostics(
                // (3,21): error CS0171: Field 'S.E' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.E").WithLocation(3, 21),
                // (3,21): error CS0171: Field 'S.Z' must be fully assigned before control is returned to the caller
                // public data struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "(int x, int y)").WithArguments("S.Z").WithLocation(3, 21),
                // (5,25): warning CS0067: The event 'S.E' is never used
                //     public event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("S.E").WithLocation(5, 25)
            );

            var s = comp.GlobalNamespace.GetTypeMember("S");
            var clone = s.GetMethod(WellKnownMemberNames.CloneMethodName);
            Assert.Equal(0, clone.Arity);
            Assert.Equal(0, clone.ParameterCount);
            Assert.Equal(s, clone.ReturnType);

            var ctor = (MethodSymbol)s.GetMembers(".ctor")[1];
            Assert.Equal(1, ctor.ParameterCount);
            Assert.True(ctor.Parameters[0].Type.Equals(s, TypeCompareKind.ConsiderEverything));
        }

        [Fact(Skip = "PROTOTYPE(record-structs)")]
        public void RecordClone4_1()
        {
            var comp = CreateCompilation(@"
using System;
public data struct S(int x, int y)
{
    public event Action E = null;
    public int Z = 0;
}");
            comp.VerifyDiagnostics(
                // (5,25): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public event Action E = null;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "E").WithArguments("S").WithLocation(5, 25),
                // (5,25): warning CS0414: The field 'S.E' is assigned but its value is never used
                //     public event Action E = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("S.E").WithLocation(5, 25),
                // (6,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int Z = 0;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "Z").WithArguments("S").WithLocation(6, 16)
                );
        }

        [Fact]
        public void RecordStructLanguageVersion()
        {
            var src1 = @"
struct Point(int x, int y);
";
            var src2 = @"
record struct Point { }
";
            var src3 = @"
record struct Point(int x, int y);
";

            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS1514: { expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS1513: } expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS8803: Top-level statements must precede namespace and type declarations.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS8805: Program using top-level statements must be an executable.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 13),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 14),
                // (2,14): error CS0165: Use of unassigned local variable 'x'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 14),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 21),
                // (2,21): error CS0165: Use of unassigned local variable 'y'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 21)
                );

            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(2, 8)
                );

            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(2, 8)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS1514: { expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS1513: } expected
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 13),
                // (2,13): error CS8803: Top-level statements must precede namespace and type declarations.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS8805: Program using top-level statements must be an executable.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_SimpleProgramNotAnExecutable, "(int x, int y);").WithLocation(2, 13),
                // (2,13): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(int x, int y)").WithLocation(2, 13),
                // (2,14): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int x").WithLocation(2, 14),
                // (2,14): error CS0165: Use of unassigned local variable 'x'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int x").WithArguments("x").WithLocation(2, 14),
                // (2,21): error CS8185: A declaration is not allowed in this context.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_DeclarationExpressionNotPermitted, "int y").WithLocation(2, 21),
                // (2,21): error CS0165: Use of unassigned local variable 'y'
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(2, 21)
                );

            comp = CreateCompilation(src2, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordStructLanguageVersion_Nested()
        {
            var src1 = @"
class C
{
    struct Point(int x, int y);
}
";
            var src2 = @"
class D
{
    record struct Point { }
}
";
            var src3 = @"
struct E
{
    record struct Point(int x, int y);
}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,17): error CS1514: { expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 17),
                // (4,17): error CS1513: } expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 17),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31)
                );

            comp = CreateCompilation(src2, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(4, 12)
                );

            comp = CreateCompilation(src3, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'record structs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     record struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "struct").WithArguments("record structs").WithLocation(4, 12)
                );

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (4,17): error CS1514: { expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(4, 17),
                // (4,17): error CS1513: } expected
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(4, 17),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31),
                // (4,31): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 31)
                );

            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeDeclaration_IsStruct()
        {
            var src = @"
record struct Point(int x, int y);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var point = comp.GlobalNamespace.GetTypeMember("Point");
            Assert.True(point.IsValueType);
            Assert.False(point.IsReferenceType);
            Assert.False(point.IsRecord);
            Assert.True(point.IsRecordStruct);
            Assert.Equal(TypeKind.Struct, point.TypeKind);
            Assert.Equal(SpecialType.System_ValueType, point.BaseTypeNoUseSiteDiagnostics.SpecialType);

            Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.RecordStructDeclaration));
        }

        [Fact]
        public void TypeDeclaration_MayNotHaveBaseType()
        {
            var src = @"
record struct Point(int x, int y) : object;
record struct Point2(int x, int y) : System.ValueType;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,37): error CS0527: Type 'object' in interface list is not an interface
                // record struct Point(int x, int y) : object;
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "object").WithArguments("object").WithLocation(2, 37),
                // (3,38): error CS0527: Type 'ValueType' in interface list is not an interface
                // record struct Point2(int x, int y) : System.ValueType;
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "System.ValueType").WithArguments("System.ValueType").WithLocation(3, 38)
                );
        }

        [Fact]
        public void TypeDeclaration_MayNotHaveTypeConstraintsWithoutTypeParameters()
        {
            var src = @"
record struct Point(int x, int y) where T : struct;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,35): error CS0080: Constraints are not allowed on non-generic declarations
                // record struct Point(int x, int y) where T : struct;
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(2, 35)
                );
        }

        [Fact]
        public void TypeDeclaration_AllowedModifiers()
        {
            var src = @"
readonly partial record struct S1;
public record struct S2;
internal record struct S3;

public class Base
{
    public int S6;
}
public class C : Base
{
    private protected record struct S4;
    protected internal record struct S5;
    new record struct S6;
}
unsafe record struct S7;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (16,22): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // unsafe record struct S7;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S7").WithLocation(16, 22)
                );
            Assert.Equal(Accessibility.Internal, comp.GlobalNamespace.GetTypeMember("S1").DeclaredAccessibility);
            Assert.Equal(Accessibility.Public, comp.GlobalNamespace.GetTypeMember("S2").DeclaredAccessibility);
            Assert.Equal(Accessibility.Internal, comp.GlobalNamespace.GetTypeMember("S3").DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedAndInternal, comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("S4").DeclaredAccessibility);
            Assert.Equal(Accessibility.ProtectedOrInternal, comp.GlobalNamespace.GetTypeMember("C").GetTypeMember("S5").DeclaredAccessibility);
        }

        [Fact]
        public void TypeDeclaration_DisallowedModifiers()
        {
            var src = @"
abstract record struct S1;
volatile record struct S2;
extern record struct S3;
virtual record struct S4;
override record struct S5;
async record struct S6;
ref record struct S7;
unsafe record struct S8;
static record struct S9;
sealed record struct S10;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,24): error CS0106: The modifier 'abstract' is not valid for this item
                // abstract record struct S1;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("abstract").WithLocation(2, 24),
                // (3,24): error CS0106: The modifier 'volatile' is not valid for this item
                // volatile record struct S2;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S2").WithArguments("volatile").WithLocation(3, 24),
                // (4,22): error CS0106: The modifier 'extern' is not valid for this item
                // extern record struct S3;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S3").WithArguments("extern").WithLocation(4, 22),
                // (5,23): error CS0106: The modifier 'virtual' is not valid for this item
                // virtual record struct S4;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S4").WithArguments("virtual").WithLocation(5, 23),
                // (6,24): error CS0106: The modifier 'override' is not valid for this item
                // override record struct S5;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S5").WithArguments("override").WithLocation(6, 24),
                // (7,21): error CS0106: The modifier 'async' is not valid for this item
                // async record struct S6;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S6").WithArguments("async").WithLocation(7, 21),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                // ref record struct S7;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S7").WithArguments("ref").WithLocation(8, 19),
                // (9,22): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // unsafe record struct S8;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S8").WithLocation(9, 22),
                // (10,22): error CS0106: The modifier 'static' is not valid for this item
                // static record struct S9;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S9").WithArguments("static").WithLocation(10, 22),
                // (11,22): error CS0106: The modifier 'sealed' is not valid for this item
                // sealed record struct S10;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S10").WithArguments("sealed").WithLocation(11, 22)
                );
        }

        [Fact]
        public void TypeDeclaration_DuplicatesModifiers()
        {
            var src = @"
public public record struct S2;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public record struct S2;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8)
                );
        }

        [Fact]
        public void TypeDeclaration_BeforeTopLevelStatement()
        {
            var src = @"
record struct S;
System.Console.WriteLine();
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "System.Console.WriteLine();").WithLocation(3, 1)
                );
        }

        [Fact]
        public void TypeDeclaration_WithTypeParameters()
        {
            var src = @"
S<string> local = default;
local.ToString();

record struct S<T>;
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            Assert.Equal(new[] { "T" }, comp.GlobalNamespace.GetTypeMember("S").TypeParameters.ToTestDisplayStrings());
        }

        [Fact]
        public void TypeDeclaration_AllowedModifiersForMembers()
        {
            var src = @"
record struct S
{
    protected int Property { get; set; } // 1
    internal protected string field; // 2, 3
    abstract void M(); // 4
    virtual void M2() { } // 5
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS0666: 'S.Property': new protected member declared in struct
                //     protected int Property { get; set; } // 1
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Property").WithArguments("S.Property").WithLocation(4, 19),
                // (5,31): error CS0666: 'S.field': new protected member declared in struct
                //     internal protected string field; // 2, 3
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "field").WithArguments("S.field").WithLocation(5, 31),
                // (5,31): warning CS0649: Field 'S.field' is never assigned to, and will always have its default value null
                //     internal protected string field; // 2, 3
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("S.field", "null").WithLocation(5, 31),
                // (6,19): error CS0621: 'S.M()': virtual or abstract members cannot be private
                //     abstract void M(); // 4
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M").WithArguments("S.M()").WithLocation(6, 19),
                // (7,18): error CS0621: 'S.M2()': virtual or abstract members cannot be private
                //     virtual void M2() { } // 5
                Diagnostic(ErrorCode.ERR_VirtualPrivate, "M2").WithArguments("S.M2()").WithLocation(7, 18)
                );
        }

        [Fact]
        public void TypeDeclaration_ImplementInterface()
        {
            var src = @"
I i = (I)default(S);
System.Console.Write(i.M(""four""));

I i2 = (I)default(S2);
System.Console.Write(i2.M(""four""));

interface I
{
    int M(string s);
}
public record struct S : I
{
    public int M(string s)
        => s.Length;
}
public record struct S2 : I
{
    int I.M(string s)
        => s.Length + 1;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "45");

            AssertEx.Equal(new[] { "System.Int32 S.M(System.String s)", "S..ctor()" },
                comp.GetMember<NamedTypeSymbol>("S").GetMembers().ToTestDisplayStrings());
        }

        [Fact]
        public void TypeDeclaration_SatisfiesStructConstraint()
        {
            var src = @"
S s = default;
System.Console.Write(M(s));

static int M<T>(T t) where T : struct, I
    => t.Property;

public interface I
{
    int Property { get; }
}
public record struct S : I
{
    public int Property => 42;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void TypeDeclaration_AccessingThis()
        {
            var src = @"
S s = new S();
System.Console.Write(s.M());

public record struct S
{
    public int Property => 42;

    public int M()
        => this.Property;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyIL("S.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""int S.Property.get""
  IL_0006:  ret
}
");
        }

        [Fact]
        public void TypeDeclaration_NoBaseInitializer()
        {
            var src = @"
public record struct S
{
    public S(int i) : base() { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,12): error CS0522: 'S': structs cannot call base class constructors
                //     public S(int i) : base() { }
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S").WithArguments("S").WithLocation(4, 12)
                );
        }

        [Fact]
        public void TypeDeclaration_NoParameterlessConstructor()
        {
            var src = @"
public record struct S
{
    public S() { }
}
";
            // PROTOTYPE(record-structs): this will be allowed in C# 10
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,12): error CS0568: Structs cannot contain explicit parameterless constructors
                //     public S() { }
                Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "S").WithLocation(4, 12)
                );
        }

        [Fact]
        public void TypeDeclaration_NoInstanceInitializers()
        {
            var src = @"
public record struct S
{
    public int field = 42;
    public int Property { get; set; } = 43;
}
";
            // PROTOTYPE(record-structs): this will be allowed in C# 10
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int field = 42;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "field").WithArguments("S").WithLocation(4, 16),
                // (5,16): error CS0573: 'S': cannot have instance property or field initializers in structs
                //     public int Property { get; set; } = 43;
                Diagnostic(ErrorCode.ERR_FieldInitializerInStruct, "Property").WithArguments("S").WithLocation(5, 16)
                );
        }

        [Fact]
        public void TypeDeclaration_NoDestructor()
        {
            var src = @"
public record struct S
{
    ~S() { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,6): error CS0575: Only class types can contain destructors
                //     ~S() { }
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "S").WithArguments("S.~S()").WithLocation(4, 6)
                );
        }

        [Fact]
        public void TypeDeclaration_DifferentPartials()
        {
            var src = @"
partial record struct S1;
partial struct S1 { }

partial struct S2 { }
partial record struct S2;

partial record struct S3;
partial record S3 { }

partial record struct S4;
partial record class S4 { }

partial record struct S5;
partial class S5 { }

partial record struct S6;
partial interface S6 { }

partial record class C1;
partial struct C1 { }

partial record class C2;
partial record struct C2 { }

partial record class C3 { }
partial record C3;

partial record class C4;
partial class C4 { }

partial record class C5;
partial interface C5 { }
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,16): error CS0261: Partial declarations of 'S1' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial struct S1 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S1").WithArguments("S1").WithLocation(3, 16),
                // (6,23): error CS0261: Partial declarations of 'S2' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record struct S2;
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S2").WithArguments("S2").WithLocation(6, 23),
                // (9,16): error CS0261: Partial declarations of 'S3' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record S3 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S3").WithArguments("S3").WithLocation(9, 16),
                // (12,22): error CS0261: Partial declarations of 'S4' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record class S4 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S4").WithArguments("S4").WithLocation(12, 22),
                // (15,15): error CS0261: Partial declarations of 'S5' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial class S5 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S5").WithArguments("S5").WithLocation(15, 15),
                // (18,19): error CS0261: Partial declarations of 'S6' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial interface S6 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "S6").WithArguments("S6").WithLocation(18, 19),
                // (21,16): error CS0261: Partial declarations of 'C1' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial struct C1 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C1").WithArguments("C1").WithLocation(21, 16),
                // (24,23): error CS0261: Partial declarations of 'C2' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial record struct C2 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C2").WithArguments("C2").WithLocation(24, 23),
                // (30,15): error CS0261: Partial declarations of 'C4' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial class C4 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C4").WithArguments("C4").WithLocation(30, 15),
                // (33,19): error CS0261: Partial declarations of 'C5' must be all classes, all record classes, all structs, all record structs, or all interfaces
                // partial interface C5 { }
                Diagnostic(ErrorCode.ERR_PartialTypeKindConflict, "C5").WithArguments("C5").WithLocation(33, 19)
                );
        }

        [Fact]
        public void PartialRecordStruct_OnlyOnePartialHasParameterList()
        {
            var src = @"
partial record struct S(int i);
partial record struct S(int i);

partial record struct S2(int i);
partial record struct S2();

partial record struct S3();
partial record struct S3();
";
            // PROTOTYPE(record-structs): missing diagnostics (the check is done by noteRecordParameters which isn't hooked up yet)
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void PositionalMemberModifiers_RefOrOut()
        {
            var src = @"
record struct R(ref int P1, out int P2);
";

            // PROTOTYPE(record-structs): missing diagnostics
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_This()
        {
            var src = @"
record struct R(this int i);
";

            // PROTOTYPE(record-structs): missing diagnostic
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                );
        }

        [Fact, WorkItem(45591, "https://github.com/dotnet/roslyn/issues/45591")]
        public void Clone_DisallowedInSource()
        {
            // PROTOTYPE(record-structs): ported
            var src = @"
record struct C1(string Clone); // 1
record struct C2
{
    string Clone; // 2
}
record struct C3
{
    string Clone { get; set; } // 3
}
record struct C5
{
    void Clone() { } // 4
    void Clone(int i) { } // 5
}
record struct C6
{
    class Clone { } // 6
}
record struct C7
{
    delegate void Clone(); // 7
}
record struct C8
{
    event System.Action Clone;  // 8
}
record struct Clone
{
    Clone(int i) => throw null;
}
record struct C9 : System.ICloneable
{
    object System.ICloneable.Clone() => throw null;
}
";

            // PROTOTYPE(record-structs): missing diagnostic on #1
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone; // 2
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(5, 12),
                // (5,12): warning CS0169: The field 'C2.Clone' is never used
                //     string Clone; // 2
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Clone").WithArguments("C2.Clone").WithLocation(5, 12),
                // (9,12): error CS8859: Members named 'Clone' are disallowed in records.
                //     string Clone { get; set; } // 3
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(9, 12),
                // (13,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone() { } // 4
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(13, 10),
                // (14,10): error CS8859: Members named 'Clone' are disallowed in records.
                //     void Clone(int i) { } // 5
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(14, 10),
                // (18,11): error CS8859: Members named 'Clone' are disallowed in records.
                //     class Clone { } // 6
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(18, 11),
                // (22,19): error CS8859: Members named 'Clone' are disallowed in records.
                //     delegate void Clone(); // 7
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(22, 19),
                // (26,25): error CS8859: Members named 'Clone' are disallowed in records.
                //     event System.Action Clone;  // 8
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(26, 25),
                // (26,25): warning CS0067: The event 'C8.Clone' is never used
                //     event System.Action Clone;  // 8
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "Clone").WithArguments("C8.Clone").WithLocation(26, 25)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes()
        {
            var src = @"
class C<T> { }
static class C2 { }
ref struct RefLike{}

unsafe record struct C( // 1
    int* P1, // 2
    int*[] P2, // 3
    C<int*[]> P3,
    delegate*<int, int> P4, // 4
    void P5, // 5
    C2 P6, // 6, 7
    System.ArgIterator P7, // 8
    System.TypedReference P8, // 9
    RefLike P9); // 10
";

            // PROTOTYPE(record-structs): missing diagnostics (checked by synthesized equals)
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (11,5): error CS1536: Invalid parameter type 'void'
                //     void P5, // 5
                Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(11, 5)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_NominalMembers()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record struct C
{
    public int* f1; // 1
    public int*[] f2; // 2
    public C<int*[]> f3;
    public delegate*<int, int> f4; // 3
    public void f5; // 4
    public C2 f6; // 5
    public System.ArgIterator f7; // 6
    public System.TypedReference f8; // 7
    public RefLike f9; // 8
}
";

            // PROTOTYPE(record-structs): missing diagnostics (checked by synthesized equals)
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (12,12): error CS0670: Field cannot have void type
                //     public void f5; // 4
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void").WithLocation(12, 12),
                // (13,15): error CS0723: Cannot declare a variable of static type 'C2'
                //     public C2 f6; // 5
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "f6").WithArguments("C2").WithLocation(13, 15),
                // (14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7; // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // (15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8; // 7
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // (16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9; // 8
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(16, 12)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_NominalMembers_AutoProperties()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record struct C
{
    public int* f1 { get; set; } // 1
    public int*[] f2 { get; set; } // 2
    public C<int*[]> f3 { get; set; }
    public delegate*<int, int> f4 { get; set; } // 3
    public void f5 { get; set; } // 4
    public C2 f6 { get; set; } // 5, 6
    public System.ArgIterator f7 { get; set; } // 6
    public System.TypedReference f8 { get; set; } // 7
    public RefLike f9 { get; set; } // 8
}
";

            // PROTOTYPE(record-structs): missing diagnostics (checked by synthesized equals)
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (12,17): error CS0547: 'C.f5': property or indexer cannot have void type
                //     public void f5 { get; set; } // 4
                Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "f5").WithArguments("C.f5").WithLocation(12, 17),
                // (13,20): error CS0722: 'C2': static types cannot be used as return types
                //     public C2 f6 { get; set; } // 5, 6
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "get").WithArguments("C2").WithLocation(13, 20),
                // (13,25): error CS0721: 'C2': static types cannot be used as parameters
                //     public C2 f6 { get; set; } // 5, 6
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "set").WithArguments("C2").WithLocation(13, 25),
                // (14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7 { get; set; } // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // (15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8 { get; set; } // 7
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // (16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9 { get; set; } // 8
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(16, 12)
                );
        }

        [Fact]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_PointerTypeAllowedForParameterAndProperty()
        {
            var src = @"
class C<T> { }

unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
{
    int* P1
    {
        get { System.Console.Write(""P1 ""); return null; }
        init { }
    }
    int*[] P2
    {
        get { System.Console.Write(""P2 ""); return null; }
        init { }
    }
    C<int*[]> P3
    {
        get { System.Console.Write(""P3 ""); return null; }
        init { }
    }

    public unsafe static void Main()
    {
        var x = new C(null, null, null);
        var (x1, x2, x3) = x;
        System.Console.Write(""RAN"");
    }
}
";
            // PROTOTYPE(record-structs): missing primary constructor
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugExe);
            //comp.VerifyEmitDiagnostics(
            //    // (4,22): warning CS8907: Parameter 'P1' is unread. Did you forget to use it to initialize the property with that name?
            //    // unsafe record C(int* P1, int*[] P2, C<int*[]> P3)
            //    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P1").WithArguments("P1").WithLocation(4, 22),
            //    // (4,33): warning CS8907: Parameter 'P2' is unread. Did you forget to use it to initialize the property with that name?
            //    // unsafe record C(int* P1, int*[] P2, C<int*[]> P3)
            //    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P2").WithArguments("P2").WithLocation(4, 33),
            //    // (4,47): warning CS8907: Parameter 'P3' is unread. Did you forget to use it to initialize the property with that name?
            //    // unsafe record C(int* P1, int*[] P2, C<int*[]> P3)
            //    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P3").WithArguments("P3").WithLocation(4, 47)
            //    );

            //CompileAndVerify(comp, expectedOutput: "P1 P2 P3 RAN", verify: Verification.Skipped /* pointers */);
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        [WorkItem(48115, "https://github.com/dotnet/roslyn/issues/48115")]
        public void RestrictedTypesAndPointerTypes_StaticFields()
        {
            var src = @"
public class C<T> { }
public static class C2 { }
public ref struct RefLike{}

public unsafe record C
{
    public static int* f1;
    public static int*[] f2;
    public static C<int*[]> f3;
    public static delegate*<int, int> f4;
    public static C2 f6; // 1
    public static System.ArgIterator f7; // 2
    public static System.TypedReference f8; // 3
    public static RefLike f9; // 4
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // (12,22): error CS0723: Cannot declare a variable of static type 'C2'
                //     public static C2 f6; // 1
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "f6").WithArguments("C2").WithLocation(12, 22),
                // (13,19): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public static System.ArgIterator f7; // 2
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(13, 19),
                // (14,19): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public static System.TypedReference f8; // 3
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(14, 19),
                // (15,19): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public static RefLike f9; // 4
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(15, 19)
                );
        }

        // PROTOTYPE(record-structs): test `in` and `params` in positional parameters
    }
}

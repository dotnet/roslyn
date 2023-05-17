// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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

        [Fact]
        public void StructRecord1()
        {
            var src = @"
record struct Point(int X, int Y);";

            var verifier = CompileAndVerify(src).VerifyDiagnostics();
            verifier.VerifyIL("Point.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""Point""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""Point""
  IL_000f:  call       ""readonly bool Point.Equals(Point)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
            verifier.VerifyIL("Point.Equals(Point)", @"
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
record struct S(int X, int Y)
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

        [Fact]
        public void StructRecord3()
        {
            var src = @"
using System;
record struct S(int X, int Y)
{
    public bool Equals(S s) => false;
    public static void Main()
    {
        var s1 = new S(0, 1);
        Console.WriteLine(s1.Equals(s1));
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"False")
                .VerifyDiagnostics(
                    // (5,17): warning CS8851: 'S' defines 'Equals' but not 'GetHashCode'
                    //     public bool Equals(S s) => false;
                    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("S").WithLocation(5, 17));

            verifier.VerifyIL("S.Main", @"
{
  // Code size       23 (0x17)
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
  IL_0016:  ret
}");
        }

        [Fact]
        public void StructRecord5()
        {
            var src = @"
using System;
record struct S(int X, int Y)
{
    public bool Equals(S s)
    {
        Console.Write(""s"");
        return true;
    }
    public static void Main()
    {
        var s1 = new S(0, 1);
        s1.Equals((object)s1);
        s1.Equals(s1);
    }
}";
            CompileAndVerify(src, expectedOutput: @"ss")
                .VerifyDiagnostics(
                    // (5,17): warning CS8851: 'S' defines 'Equals' but not 'GetHashCode'
                    //     public bool Equals(S s)
                    Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("S").WithLocation(5, 17));
        }

        [Fact]
        public void StructRecordDefaultCtor()
        {
            const string src = @"
public record struct S(int X);";

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
        public void Equality_01()
        {
            var source =
@"using static System.Console;
record struct S;

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

            var verifier = CompileAndVerify(comp, expectedOutput: @"True
True").VerifyDiagnostics();

            verifier.VerifyIL("S.Equals(S)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");
            verifier.VerifyIL("S.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""S""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""S""
  IL_000f:  call       ""readonly bool S.Equals(S)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");
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

            var comp = CreateCompilation(new[] { src1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("primary constructors").WithLocation(2, 13),
                // (2,18): warning CS9113: Parameter 'x' is unread.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 18),
                // (2,25): warning CS9113: Parameter 'y' is unread.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(2, 25)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 8));

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 8));

            comp = CreateCompilation(new[] { src1, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,13): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("primary constructors").WithLocation(2, 13),
                // (2,18): warning CS9113: Parameter 'x' is unread.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 18),
                // (2,25): warning CS9113: Parameter 'y' is unread.
                // struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(2, 25)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseDll);
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
            var src4 = @"
namespace NS
{
    record struct Point { }
}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,17): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y)").WithArguments("primary constructors").WithLocation(4, 17),
                // (4,22): warning CS9113: Parameter 'x' is unread.
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(4, 22),
                // (4,29): warning CS9113: Parameter 'y' is unread.
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(4, 29)
                );

            comp = CreateCompilation(new[] { src2, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(4, 12));

            comp = CreateCompilation(new[] { src3, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(4, 12));

            comp = CreateCompilation(src4, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     record struct Point { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(4, 12));

            comp = CreateCompilation(src1);
            comp.VerifyDiagnostics(
                // (4,22): warning CS9113: Parameter 'x' is unread.
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(4, 22),
                // (4,29): warning CS9113: Parameter 'y' is unread.
                //     struct Point(int x, int y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(4, 29)
                );

            comp = CreateCompilation(src2);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src3);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src4);
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

            CompileAndVerify(comp, symbolValidator: validateModule, sourceSymbolValidator: validateModule);
            Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.RecordStructDeclaration));

            static void validateModule(ModuleSymbol module)
            {
                var isSourceSymbol = module is SourceModuleSymbol;

                var point = module.GlobalNamespace.GetTypeMember("Point");
                Assert.True(point.IsValueType);
                Assert.False(point.IsReferenceType);
                Assert.False(point.IsRecord);
                Assert.Equal(TypeKind.Struct, point.TypeKind);
                Assert.Equal(SpecialType.System_ValueType, point.BaseTypeNoUseSiteDiagnostics.SpecialType);
                Assert.Equal("Point", point.ToTestDisplayString());

                if (isSourceSymbol)
                {
                    Assert.True(point is SourceNamedTypeSymbol);
                    Assert.True(point.IsRecordStruct);
                    Assert.True(point.GetPublicSymbol().IsRecord);
                    Assert.Equal("record struct Point", point.ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
                }
                else
                {
                    Assert.True(point is PENamedTypeSymbol);
                    Assert.False(point.IsRecordStruct);
                    Assert.False(point.GetPublicSymbol().IsRecord);
                    Assert.Equal("struct Point", point.ToDisplayString(SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword)));
                }
            }
        }

        [Fact]
        public void TypeDeclaration_IsStruct_InConstraints()
        {
            var src = @"
record struct Point(int x, int y);

class C<T> where T : struct
{
    void M(C<Point> c) { }
}

class C2<T> where T : new()
{
    void M(C2<Point> c) { }
}

class C3<T> where T : class
{
    void M(C3<Point> c) { } // 1
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (16,22): error CS0452: The type 'Point' must be a reference type in order to use it as parameter 'T' in the generic type or method 'C3<T>'
                //     void M(C3<Point> c) { } // 1
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "c").WithArguments("C3<T>", "T", "Point").WithLocation(16, 22)
                );
        }

        [Fact]
        public void TypeDeclaration_IsStruct_Unmanaged()
        {
            var src = @"
record struct Point(int x, int y);
record struct Point2(string x, string y);

class C<T> where T : unmanaged
{
    void M(C<Point> c) { }
    void M2(C<Point2> c) { } // 1
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,23): error CS8377: The type 'Point2' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'C<T>'
                //     void M2(C<Point2> c) { } // 1
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "c").WithArguments("C<T>", "T", "Point2").WithLocation(8, 23)
                );
        }

        [Fact]
        public void IsRecord_Generic()
        {
            var src = @"
record struct Point<T>(T x, T y);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, symbolValidator: validateModule, sourceSymbolValidator: validateModule);

            static void validateModule(ModuleSymbol module)
            {
                var isSourceSymbol = module is SourceModuleSymbol;

                var point = module.GlobalNamespace.GetTypeMember("Point");
                Assert.True(point.IsValueType);
                Assert.False(point.IsReferenceType);
                Assert.False(point.IsRecord);
                Assert.Equal(TypeKind.Struct, point.TypeKind);
                Assert.Equal(SpecialType.System_ValueType, point.BaseTypeNoUseSiteDiagnostics.SpecialType);
                Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.RecordStructDeclaration));

                if (isSourceSymbol)
                {
                    Assert.True(point is SourceNamedTypeSymbol);
                    Assert.True(point.IsRecordStruct);
                    Assert.True(point.GetPublicSymbol().IsRecord);
                }
                else
                {
                    Assert.True(point is PENamedTypeSymbol);
                    Assert.False(point.IsRecordStruct);
                    Assert.False(point.GetPublicSymbol().IsRecord);
                }
            }
        }

        [Fact]
        public void IsRecord_Retargeting()
        {
            var src = @"
public record struct Point(int x, int y);
";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Mscorlib40);
            var comp2 = CreateCompilation("", targetFramework: TargetFramework.Mscorlib46, references: new[] { comp.ToMetadataReference() });
            var point = comp2.GlobalNamespace.GetTypeMember("Point");

            Assert.Equal("Point", point.ToTestDisplayString());
            Assert.IsType<RetargetingNamedTypeSymbol>(point);
            Assert.True(point.IsRecordStruct);
            Assert.True(point.GetPublicSymbol().IsRecord);
        }

        [Fact]
        public void IsRecord_AnonymousType()
        {
            var src = @"
class C
{
    void M()
    {
        var x = new { X = 1 };
    }
}
";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var creation = tree.GetRoot().DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().Single();
            var type = model.GetTypeInfo(creation).Type!;

            Assert.Equal("<anonymous type: System.Int32 X>", type.ToTestDisplayString());
            Assert.IsType<AnonymousTypeManager.AnonymousTypePublicSymbol>(((Symbols.PublicModel.NonErrorNamedTypeSymbol)type).UnderlyingNamedTypeSymbol);
            Assert.False(type.IsRecord);
        }

        [Fact]
        public void IsRecord_ErrorType()
        {
            var src = @"
class C
{
    Error M() => throw null;
}
";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var type = model.GetDeclaredSymbol(method)!.ReturnType;

            Assert.Equal("Error", type.ToTestDisplayString());
            Assert.IsType<ExtendedErrorTypeSymbol>(((Symbols.PublicModel.ErrorTypeSymbol)type).UnderlyingNamedTypeSymbol);
            Assert.False(type.IsRecord);
        }

        [Fact]
        public void IsRecord_Pointer()
        {
            var src = @"
class C
{
    int* M() => throw null;
}
";
            var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseDll);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var type = model.GetDeclaredSymbol(method)!.ReturnType;

            Assert.Equal("System.Int32*", type.ToTestDisplayString());
            Assert.IsType<PointerTypeSymbol>(((Symbols.PublicModel.PointerTypeSymbol)type).UnderlyingTypeSymbol);
            Assert.False(type.IsRecord);
        }

        [Fact]
        public void IsRecord_Dynamic()
        {
            var src = @"
class C
{
    void M(dynamic d)
    {
    }
}
";
            var comp = CreateCompilation(src);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var type = model.GetDeclaredSymbol(method)!.GetParameterType(0);

            Assert.Equal("dynamic", type.ToTestDisplayString());
            Assert.IsType<DynamicTypeSymbol>(((Symbols.PublicModel.DynamicTypeSymbol)type).UnderlyingTypeSymbol);
            Assert.False(type.IsRecord);
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

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugDll);
            comp.VerifyDiagnostics();
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

            AssertEx.Equal(new[] {
                "System.Int32 S.M(System.String s)",
                "readonly System.String S.ToString()",
                "readonly System.Boolean S.PrintMembers(System.Text.StringBuilder builder)",
                "System.Boolean S.op_Inequality(S left, S right)",
                "System.Boolean S.op_Equality(S left, S right)",
                "readonly System.Int32 S.GetHashCode()",
                "readonly System.Boolean S.Equals(System.Object obj)",
                "readonly System.Boolean S.Equals(S other)",
                "S..ctor()" },
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
        public void TypeDeclaration_ParameterlessConstructor_01()
        {
            var src =
@"record struct S0();
record struct S1;
record struct S2
{
    public S2() { }
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct S0();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(1, 8),
                // (2,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct S1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 8),
                // (3,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct S2
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(3, 8),
                // (5,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(5, 12));

            var verifier = CompileAndVerify(src);
            verifier.VerifyIL("S0..ctor()",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyMissing("S1..ctor()");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Fact]
        public void TypeDeclaration_ParameterlessConstructor_02()
        {
            var src =
@"record struct S1
{
    S1() { }
}
record struct S2
{
    internal S2() { }
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct S1
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(1, 8),
                // (3,5): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     S1() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S1").WithArguments("parameterless struct constructors", "10.0").WithLocation(3, 5),
                // (3,5): error CS8958: The parameterless struct constructor must be 'public'.
                //     S1() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S1").WithLocation(3, 5),
                // (5,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct S2
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(5, 8),
                // (7,14): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     internal S2() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S2").WithArguments("parameterless struct constructors", "10.0").WithLocation(7, 14),
                // (7,14): error CS8958: The parameterless struct constructor must be 'public'.
                //     internal S2() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S2").WithLocation(7, 14));

            comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,5): error CS8918: The parameterless struct constructor must be 'public'.
                //     S1() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S1").WithLocation(3, 5),
                // (7,14): error CS8918: The parameterless struct constructor must be 'public'.
                //     internal S2() { }
                Diagnostic(ErrorCode.ERR_NonPublicParameterlessStructConstructor, "S2").WithLocation(7, 14));
        }

        [Fact]
        public void TypeDeclaration_ParameterlessConstructor_OtherConstructors()
        {
            var src = @"
record struct S1
{
    public S1() { }
    S1(object o) { } // ok because no record parameter list
}
record struct S2
{
    S2(object o) { }
}
record struct S3()
{
    S3(object o) { } // 1
}
record struct S4()
{
    S4(object o) : this() { }
}
record struct S5(object o)
{
    public S5() { } // 2
}
record struct S6(object o)
{
    public S6() : this(null) { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (13,5): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     S3(object o) { } // 1
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S3").WithLocation(13, 5),
                // (21,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S5() { } // 2
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S5").WithLocation(21, 12)
                );
        }

        [Fact]
        public void TypeDeclaration_ParameterlessConstructor_Initializers()
        {
            var src = @"
var s1 = new S1();
var s2 = new S2(null);
var s2b = new S2();
var s3 = new S3();
var s4 = new S4(new object());
var s5 = new S5();
var s6 = new S6(""s6.other"");

System.Console.Write((s1.field, s2.field, s2b.field is null, s3.field, s4.field, s5.field, s6.field, s6.other));

record struct S1
{
    public string field = ""s1"";
    public S1() { }
}
record struct S2
{
    public string field = ""s2"";
    public S2(object o) { }
}
record struct S3()
{
    public string field = ""s3"";
}
record struct S4
{
    public string field = ""s4"";
    public S4(object o) : this() { }
}
record struct S5()
{
    public string field = ""s5"";
    public S5(object o) : this() { }
}
record struct S6(string other)
{
    public string field = ""s6.field"";
    public S6() : this(""ignored"") { }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(s1, s2, True, s3, s4, s5, s6.field, s6.other)");
        }

        [Fact]
        public void TypeDeclaration_InstanceInitializers_01()
        {
            var src = @"
public record struct S
{
    public int field = 42;
    public int Property { get; set; } = 43;
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,15): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // public record struct S
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 15),
                // (2,22): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // public record struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(2, 22),
                // (4,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int field = 42;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "field").WithArguments("struct field initializers", "10.0").WithLocation(4, 16),
                // (5,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int Property { get; set; } = 43;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Property").WithArguments("struct field initializers", "10.0").WithLocation(5, 16));

            comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,22): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // public record struct S
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "S").WithLocation(2, 22));
        }

        [Fact]
        public void TypeDeclaration_InstanceInitializers_02()
        {
            var src = @"
public record struct S
{
    public S() { }
    public int field = 42;
    public int Property { get; set; } = 43;
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,15): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // public record struct S
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 15),
                // (4,12): error CS8773: Feature 'parameterless struct constructors' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public S() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "S").WithArguments("parameterless struct constructors", "10.0").WithLocation(4, 12),
                // (5,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int field = 42;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "field").WithArguments("struct field initializers", "10.0").WithLocation(5, 16),
                // (6,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int Property { get; set; } = 43;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Property").WithArguments("struct field initializers", "10.0").WithLocation(6, 16));

            comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void TypeDeclaration_InstanceInitializers_03()
        {
            var src = @"
public record struct S()
{
    public int field = 42;
    public int Property { get; set; } = 43;
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (2,15): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // public record struct S()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(2, 15));

            comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
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
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "S").WithLocation(4, 6)
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
        public void PartialRecord_OnlyOnePartialHasParameterList()
        {
            var src = @"
partial record struct S(int i);
partial record struct S(int i);

partial record struct S2(int i);
partial record struct S2();

partial record struct S3();
partial record struct S3();
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,24): error CS8863: Only a single partial type declaration may have a parameter list
                // partial record struct S(int i);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int i)").WithLocation(3, 24),
                // (6,25): error CS8863: Only a single partial type declaration may have a parameter list
                // partial record struct S2();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(6, 25),
                // (9,25): error CS8863: Only a single partial type declaration may have a parameter list
                // partial record struct S3();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(9, 25)
                );
        }

        [Fact]
        public void PartialRecord_ParametersInScopeOfBothParts()
        {
            var src = @"
var c = new C(2);
System.Console.Write((c.P1, c.P2));

public partial record struct C(int X)
{
    public int P1 { get; set; } = X;
}
public partial record struct C
{
    public int P2 { get; set; } = X;
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "(2, 2)", verify: Verification.Skipped /* init-only */)
                .VerifyDiagnostics(
                    // (5,30): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'C'. To specify an ordering, all instance fields must be in the same declaration.
                    // public partial record struct C(int X)
                    Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "C").WithArguments("C").WithLocation(5, 30)
                    );
        }

        [Fact]
        public void PartialRecord_DuplicateMemberNames()
        {
            var src = @"
public partial record struct C(int X)
{
    public void M(int i) { }
}
public partial record struct C
{
    public void M(string s) { }
}
";
            var comp = CreateCompilation(src);
            var expectedMemberNames = new string[]
            {
                ".ctor",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "M",
                "M",
                "ToString",
                "PrintMembers",
                "op_Inequality",
                "op_Equality",
                "GetHashCode",
                "Equals",
                "Equals",
                "Deconstruct",
                ".ctor",
            };
            AssertEx.Equal(expectedMemberNames, comp.GetMember<NamedTypeSymbol>("C").GetPublicSymbol().MemberNames);
        }

        [Fact]
        public void RecordInsideGenericType()
        {
            var src = @"
var c = new C<int>.Nested(2);
System.Console.Write(c.T);

public class C<T>
{
    public record struct Nested(T T);
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "2");
        }

        [Fact]
        public void PositionalMemberModifiers_RefOrOut()
        {
            var src = @"
record struct R(ref int P1, out int P2);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0177: The out parameter 'P2' must be assigned to before control leaves the current method
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "R").WithArguments("P2").WithLocation(2, 15),
                // (2,17): error CS0631: ref and out are not valid in this context
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(2, 17),
                // (2,29): error CS0631: ref and out are not valid in this context
                // record struct R(ref int P1, out int P2);
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out").WithLocation(2, 29)
                );
        }

        [Fact, WorkItem(45008, "https://github.com/dotnet/roslyn/issues/45008")]
        public void PositionalMemberModifiers_This()
        {
            var src = @"
record struct R(this int i);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,17): error CS0027: Keyword 'this' is not available in the current context
                // record struct R(this int i);
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(2, 17)
                );
        }

        [Fact, WorkItem(45591, "https://github.com/dotnet/roslyn/issues/45591")]
        public void Clone_DisallowedInSource()
        {
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

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,25): error CS8859: Members named 'Clone' are disallowed in records.
                // record struct C1(string Clone); // 1
                Diagnostic(ErrorCode.ERR_CloneDisallowedInRecord, "Clone").WithLocation(2, 25),
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

unsafe record struct C(
    int* P1, // 1
    int*[] P2,
    C<int*[]> P3,
    delegate*<int, int> P4, // 2
    void P5, // 3
    C2 P6, // 4, 5
    System.ArgIterator P7, // 6
    System.TypedReference P8, // 7
    RefLike P9, // 8
    delegate*<void> P10); // 9
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // 0.cs(7,10): error CS8908: The type 'int*' may not be used for a field of a record.
                //     int* P1, // 1
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P1").WithArguments("int*").WithLocation(7, 10),
                // 0.cs(10,25): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     delegate*<int, int> P4, // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P4").WithArguments("delegate*<int, int>").WithLocation(10, 25),
                // 0.cs(11,5): error CS1536: Invalid parameter type 'void'
                //     void P5, // 3
                Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(11, 5),
                // 0.cs(12,5): error CS0721: 'C2': static types cannot be used as parameters
                //     C2 P6, // 4, 5
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "C2").WithArguments("C2").WithLocation(12, 5),
                // 0.cs(12,5): error CS0722: 'C2': static types cannot be used as return types
                //     C2 P6, // 4, 5
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C2").WithArguments("C2").WithLocation(12, 5),
                // 0.cs(13,5): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     System.ArgIterator P7, // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(13, 5),
                // 0.cs(14,5): error CS0610: Field or property cannot be of type 'TypedReference'
                //     System.TypedReference P8, // 7
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(14, 5),
                // 0.cs(15,5): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     RefLike P9, // 8
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(15, 5),
                // 0.cs(16,21): error CS8908: The type 'delegate*<void>' may not be used for a field of a record.
                //     delegate*<void> P10); // 9
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P10").WithArguments("delegate*<void>").WithLocation(16, 21)
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
    public int*[] f2;
    public C<int*[]> f3;
    public delegate*<int, int> f4; // 2
    public void f5; // 3
    public C2 f6; // 4
    public System.ArgIterator f7; // 5
    public System.TypedReference f8; // 6
    public RefLike f9; // 7
    public delegate*<void> f10; // 8
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // 0.cs(8,17): error CS8908: The type 'int*' may not be used for a field of a record.
                //     public int* f1; // 1
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f1").WithArguments("int*").WithLocation(8, 17),
                // 0.cs(11,32): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     public delegate*<int, int> f4; // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f4").WithArguments("delegate*<int, int>").WithLocation(11, 32),
                // 0.cs(12,12): error CS0670: Field cannot have void type
                //     public void f5; // 3
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void").WithLocation(12, 12),
                // 0.cs(13,15): error CS0723: Cannot declare a variable of static type 'C2'
                //     public C2 f6; // 4
                Diagnostic(ErrorCode.ERR_VarDeclIsStaticClass, "f6").WithArguments("C2").WithLocation(13, 15),
                // 0.cs(14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7; // 5
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // 0.cs(15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8; // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // 0.cs(16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9; // 7
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "RefLike").WithArguments("RefLike").WithLocation(16, 12),
                // 0.cs(17,28): error CS8908: The type 'delegate*<void>' may not be used for a field of a record.
                //     public delegate*<void> f10; // 8
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f10").WithArguments("delegate*<void>").WithLocation(17, 28)
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
    public int*[] f2 { get; set; }
    public C<int*[]> f3 { get; set; }
    public delegate*<int, int> f4 { get; set; } // 2
    public void f5 { get; set; } // 3
    public C2 f6 { get; set; } // 4
    public System.ArgIterator f7 { get; set; } // 5
    public System.TypedReference f8 { get; set; } // 6
    public RefLike f9 { get; set; } // 7
    public delegate*<void>[] f10 { get; set; } // 8
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, options: TestOptions.UnsafeDebugDll);
            comp.VerifyEmitDiagnostics(
                // 0.cs(8,17): error CS8908: The type 'int*' may not be used for a field of a record.
                //     public int* f1 { get; set; } // 1
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f1").WithArguments("int*").WithLocation(8, 17),
                // 0.cs(11,32): error CS8908: The type 'delegate*<int, int>' may not be used for a field of a record.
                //     public delegate*<int, int> f4 { get; set; } // 2
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "f4").WithArguments("delegate*<int, int>").WithLocation(11, 32),
                // 0.cs(12,17): error CS0547: 'C.f5': property or indexer cannot have void type
                //     public void f5 { get; set; } // 3
                Diagnostic(ErrorCode.ERR_PropertyCantHaveVoidType, "f5").WithArguments("C.f5").WithLocation(12, 17),
                // 0.cs(13,12): error CS0722: 'C2': static types cannot be used as return types
                //     public C2 f6 { get; set; } // 4
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "C2").WithArguments("C2").WithLocation(13, 12),
                // 0.cs(14,12): error CS0610: Field or property cannot be of type 'ArgIterator'
                //     public System.ArgIterator f7 { get; set; } // 5
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(14, 12),
                // 0.cs(15,12): error CS0610: Field or property cannot be of type 'TypedReference'
                //     public System.TypedReference f8 { get; set; } // 6
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference").WithLocation(15, 12),
                // 0.cs(16,12): error CS8345: Field or auto-implemented property cannot be of type 'RefLike' unless it is an instance member of a ref struct.
                //     public RefLike f9 { get; set; } // 7
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
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.UnsafeDebugExe);
            comp.VerifyEmitDiagnostics(
                // (4,29): warning CS8907: Parameter 'P1' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P1").WithArguments("P1").WithLocation(4, 29),
                // (4,40): warning CS8907: Parameter 'P2' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P2").WithArguments("P2").WithLocation(4, 40),
                // (4,54): warning CS8907: Parameter 'P3' is unread. Did you forget to use it to initialize the property with that name?
                // unsafe record struct C(int* P1, int*[] P2, C<int*[]> P3)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P3").WithArguments("P3").WithLocation(4, 54)
                );

            CompileAndVerify(comp, expectedOutput: "P1 P2 P3 RAN", verify: Verification.Skipped /* pointers */);
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

        [Fact]
        public void RecordProperties_01()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    int Z = 345;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
        Console.Write(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"12345").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4     0x159
  IL_0014:  stfld      ""int C.Z""
  IL_0019:  ret
}
");

            var c = verifier.Compilation.GlobalNamespace.GetTypeMember("C");
            Assert.False(c.IsReadOnly);
            var x = (IPropertySymbol)c.GetMember("X");
            Assert.Equal("readonly System.Int32 C.X.get", x.GetMethod.ToTestDisplayString());
            Assert.Equal("void C.X.set", x.SetMethod.ToTestDisplayString());
            Assert.False(x.SetMethod!.IsInitOnly);

            var xBackingField = (IFieldSymbol)c.GetMember("<X>k__BackingField");
            Assert.Equal("System.Int32 C.<X>k__BackingField", xBackingField.ToTestDisplayString());
            Assert.False(xBackingField.IsReadOnly);
        }

        [Fact]
        public void RecordProperties_01_EmptyParameterList()
        {
            var src = @"
using System;
record struct C()
{
    int Z = 345;
    public static void Main()
    {
        var c = new C();
        Console.Write(c.Z);
    }
}";
            CreateCompilation(src).VerifyEmitDiagnostics();
        }

        [Fact]
        public void RecordProperties_01_Readonly()
        {
            var src = @"
using System;
readonly record struct C(int X, int Y)
{
    readonly int Z = 345;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
        Console.Write(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"12345").VerifyDiagnostics();

            var c = verifier.Compilation.GlobalNamespace.GetTypeMember("C");
            Assert.True(c.IsReadOnly);
            var x = (IPropertySymbol)c.GetMember("X");
            Assert.Equal("System.Int32 C.X.get", x.GetMethod.ToTestDisplayString());
            Assert.Equal("void modreq(System.Runtime.CompilerServices.IsExternalInit) C.X.init", x.SetMethod.ToTestDisplayString());
            Assert.True(x.SetMethod!.IsInitOnly);

            var xBackingField = (IFieldSymbol)c.GetMember("<X>k__BackingField");
            Assert.Equal("System.Int32 C.<X>k__BackingField", xBackingField.ToTestDisplayString());
            Assert.True(xBackingField.IsReadOnly);
        }

        [Fact]
        public void RecordProperties_01_ReadonlyMismatch()
        {
            var src = @"
readonly record struct C(int X)
{
    public int X { get; set; } = X; // 1
}
record struct C2(int X)
{
    public int X { get; init; } = X;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,16): error CS8341: Auto-implemented instance properties in readonly structs must be readonly.
                //     public int X { get; set; } = X; // 1
                Diagnostic(ErrorCode.ERR_AutoPropsInRoStruct, "X").WithLocation(4, 16)
                );
        }

        [Fact]
        public void RecordProperties_02()
        {
            var src = @"
using System;
record struct C(int X, int Y)
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

    private int X1 = X;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(5, 12),
                // (5,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public C(int a, int b)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12),
                // (11,21): error CS0121: The call is ambiguous between the following methods or properties: 'C.C(int, int)' and 'C.C(int, int)'
                //         var c = new C(1, 2);
                Diagnostic(ErrorCode.ERR_AmbigCall, "C").WithArguments("C.C(int, int)", "C.C(int, int)").WithLocation(11, 21)
                );
        }

        [Fact]
        public void RecordProperties_03()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; }

    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.X);
        Console.WriteLine(c.Y);
    }
}";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (3,15): error CS0843: Auto-implemented property 'C.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "C").WithArguments("C.X", "11.0").WithLocation(3, 15),
                // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                );

            var verifier = CompileAndVerify(src, parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics(
                // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                );
            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int C.<Y>k__BackingField""
  IL_000e:  ret
}
");
        }

        [Fact]
        public void RecordProperties_03_InitializedWithY()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; } = Y;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: "22")
                .VerifyDiagnostics(
                    // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(int X, int Y)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                    );
        }

        [Fact]
        public void RecordProperties_04()
        {
            var src = @"
using System;
record struct C(int X, int Y)
{
    public int X { get; } = 3;

    public static void Main()
    {
        var c = new C(1, 2);
        Console.Write(c.X);
        Console.Write(c.Y);
    }
}";
            CompileAndVerify(src, expectedOutput: "32")
                .VerifyDiagnostics(
                    // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(int X, int Y)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                    );
        }

        [Fact]
        public void RecordProperties_05()
        {
            var src = @"
record struct C(int X, int X)
{
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,28): error CS0100: The parameter name 'X' is a duplicate
                // record struct C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "X").WithArguments("X").WithLocation(2, 28),
                // (2,28): error CS0102: The type 'C' already contains a definition for 'X'
                // record struct C(int X, int X)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("C", "X").WithLocation(2, 28)
                );

            var expectedMembers = new[]
            {
                "System.Int32 C.X { get; set; }",
                "System.Int32 C.X { get; set; }"
            };
            AssertEx.Equal(expectedMembers,
                comp.GetMember<NamedTypeSymbol>("C").GetMembers().OfType<PropertySymbol>().ToTestDisplayStrings());

            var expectedMemberNames = new[] {
                ".ctor",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "<X>k__BackingField",
                "get_X",
                "set_X",
                "X",
                "ToString",
                "PrintMembers",
                "op_Inequality",
                "op_Equality",
                "GetHashCode",
                "Equals",
                "Equals",
                "Deconstruct",
                ".ctor"
            };
            AssertEx.Equal(expectedMemberNames, comp.GetMember<NamedTypeSymbol>("C").GetPublicSymbol().MemberNames);
        }

        [Fact]
        public void RecordProperties_06()
        {
            var src = @"
record struct C(int X, int Y)
{
    public void get_X() { }
    public void set_X() { }
    int get_Y(int value) => value;
    int set_Y(int value) => value;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,21): error CS0082: Type 'C' already reserves a member called 'get_X' with the same parameter types
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "X").WithArguments("get_X", "C").WithLocation(2, 21),
                // (2,28): error CS0082: Type 'C' already reserves a member called 'set_Y' with the same parameter types
                // record struct C(int X, int Y)
                Diagnostic(ErrorCode.ERR_MemberReserved, "Y").WithArguments("set_Y", "C").WithLocation(2, 28)
                );

            var actualMembers = comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "C..ctor(System.Int32 X, System.Int32 Y)",
                "System.Int32 C.<X>k__BackingField",
                "readonly System.Int32 C.X.get",
                "void C.X.set",
                "System.Int32 C.X { get; set; }",
                "System.Int32 C.<Y>k__BackingField",
                "readonly System.Int32 C.Y.get",
                "void C.Y.set",
                "System.Int32 C.Y { get; set; }",
                "void C.get_X()",
                "void C.set_X()",
                "System.Int32 C.get_Y(System.Int32 value)",
                "System.Int32 C.set_Y(System.Int32 value)",
                "readonly System.String C.ToString()",
                "readonly System.Boolean C.PrintMembers(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C left, C right)",
                "System.Boolean C.op_Equality(C left, C right)",
                "readonly System.Int32 C.GetHashCode()",
                "readonly System.Boolean C.Equals(System.Object obj)",
                "readonly System.Boolean C.Equals(C other)",
                "readonly void C.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                "C..ctor()",
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void RecordProperties_07()
        {
            var comp = CreateCompilation(@"
record struct C1(object P, object get_P);
record struct C2(object get_P, object P);");
            comp.VerifyDiagnostics(
                // (2,25): error CS0102: The type 'C1' already contains a definition for 'get_P'
                // record struct C1(object P, object get_P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C1", "get_P").WithLocation(2, 25),
                // (3,39): error CS0102: The type 'C2' already contains a definition for 'get_P'
                // record struct C2(object get_P, object P);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P").WithArguments("C2", "get_P").WithLocation(3, 39)
                );
        }

        [Fact]
        public void RecordProperties_08()
        {
            var comp = CreateCompilation(@"
record struct C1(object O1)
{
    public object O1 { get; } = O1;
    public object O2 { get; } = O1;
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RecordProperties_09()
        {
            var src = @"
record struct C(object P1, object P2, object P3, object P4)
{
    class P1 { }
    object P2 = 2;
    int P3(object o) => 3;
    int P4<T>(T t) => 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,24): error CS0102: The type 'C' already contains a definition for 'P1'
                // record struct C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P1").WithArguments("C", "P1").WithLocation(2, 24),
                // (2,35): warning CS8907: Parameter 'P2' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(object P1, object P2, object P3, object P4)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P2").WithArguments("P2").WithLocation(2, 35),
                // (6,9): error CS0102: The type 'C' already contains a definition for 'P3'
                //     int P3(object o) => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P3").WithArguments("C", "P3").WithLocation(6, 9),
                // (7,9): error CS0102: The type 'C' already contains a definition for 'P4'
                //     int P4<T>(T t) => 4;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "P4").WithArguments("C", "P4").WithLocation(7, 9)
                );
        }

        [Fact]
        public void RecordProperties_10()
        {
            var src = @"
record struct C(object P)
{
    const int P = 4;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,24): error CS8866: Record member 'C.P' must be a readable instance property or field of type 'object' to match positional parameter 'P'.
                // record struct C(object P)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P").WithArguments("C.P", "object", "P").WithLocation(2, 24),
                // (2,24): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(object P)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(2, 24)
                );
        }

        [Fact]
        public void RecordProperties_11_UnreadPositionalParameter()
        {
            var source = @"
record struct C1(object O1, object O2, object O3) // 1, 2
{
    public object O1 { get; init; }
    public object O2 { get; init; } = M(O2);
    public object O3 { get; init; } = M(O3 = null);
    private static object M(object o) => o;
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                    // (2,15): error CS0843: Auto-implemented property 'C1.O1' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                    // record struct C1(object O1, object O2, object O3) // 1, 2
                    Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "C1").WithArguments("C1.O1", "11.0").WithLocation(2, 15),
                    // (2,25): warning CS8907: Parameter 'O1' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C1(object O1, object O2, object O3) // 1, 2
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O1").WithArguments("O1").WithLocation(2, 25),
                    // (2,47): warning CS8907: Parameter 'O3' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C1(object O1, object O2, object O3) // 1, 2
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O3").WithArguments("O3").WithLocation(2, 47)
                );

            var verifier = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11, verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (2,25): warning CS8907: Parameter 'O1' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1, 2
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O1").WithArguments("O1").WithLocation(2, 25),
                // (2,47): warning CS8907: Parameter 'O3' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1, 2
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O3").WithArguments("O3").WithLocation(2, 47));
            verifier.VerifyIL("C1..ctor(object, object, object)", @"
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      ""object C1.<O1>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  call       ""object C1.M(object)""
  IL_000e:  stfld      ""object C1.<O2>k__BackingField""
  IL_0013:  ldarg.0
  IL_0014:  ldnull
  IL_0015:  dup
  IL_0016:  starg.s    V_3
  IL_0018:  call       ""object C1.M(object)""
  IL_001d:  stfld      ""object C1.<O3>k__BackingField""
  IL_0022:  ret
}
");
        }

        [Fact]
        public void RecordProperties_11_UnreadPositionalParameter_InRefOut()
        {
            var comp = CreateCompilation(@"
record struct C1(object O1, object O2, object O3) // 1
{
    public object O1 { get; init; } = MIn(in O1);
    public object O2 { get; init; } = MRef(ref O2);
    public object O3 { get; init; } = MOut(out O3);

    static object MIn(in object o) => o;
    static object MRef(ref object o) => o;
    static object MOut(out object o) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (2,47): warning CS8907: Parameter 'O3' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C1(object O1, object O2, object O3) // 1
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "O3").WithArguments("O3").WithLocation(2, 47)
                );
        }

        [Fact]
        public void RecordProperties_SelfContainedStruct()
        {
            var comp = CreateCompilation(@"
record struct C(C c);
");
            comp.VerifyDiagnostics(
                // (2,19): error CS0523: Struct member 'C.c' of type 'C' causes a cycle in the struct layout
                // record struct C(C c);
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "c").WithArguments("C.c", "C").WithLocation(2, 19)
                );
        }

        [Fact]
        public void RecordProperties_PropertyInValueType()
        {
            var corlib_cs = @"
namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public bool X { get; set; }
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Char { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(char c) => null;
        public StringBuilder Append(object o) => null;
    }
}
";
            var corlibRef = CreateEmptyCompilation(corlib_cs).EmitToImageReference();

            {
                var src = @"
record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
                var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
                comp.VerifyEmitDiagnostics(
                    // (2,22): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // record struct C(bool X)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 22)
                    );

                Assert.Null(comp.GlobalNamespace.GetTypeMember("C").GetMember("X"));
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
                var x = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                Assert.Equal("System.Boolean System.ValueType.X { get; set; }", model.GetSymbolInfo(x!).Symbol.ToTestDisplayString());
            }

            {
                var src = @"
readonly record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
                var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
                comp.VerifyEmitDiagnostics(
                    // (2,31): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                    // readonly record struct C(bool X)
                    Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 31)
                    );

                Assert.Null(comp.GlobalNamespace.GetTypeMember("C").GetMember("X"));
                var tree = comp.SyntaxTrees.Single();
                var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
                var x = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().Expression;
                Assert.Equal("System.Boolean System.ValueType.X { get; set; }", model.GetSymbolInfo(x!).Symbol.ToTestDisplayString());
            }
        }

        [Fact]
        public void RecordProperties_PropertyInValueType_Static()
        {
            var corlib_cs = @"
namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public static bool X { get; set; }
    }
    public class Attribute { }
    public class AttributeUsageAttribute : Attribute
    {
        public AttributeUsageAttribute(AttributeTargets t) { }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
    }
    public struct Enum { }
    public enum AttributeTargets { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Char { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(char c) => null;
        public StringBuilder Append(object o) => null;
    }
}
";
            var corlibRef = CreateEmptyCompilation(corlib_cs).EmitToImageReference();
            var src = @"
record struct C(bool X)
{
    bool M()
    {
        return X;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview, references: new[] { corlibRef });
            comp.VerifyEmitDiagnostics(
                // (2,22): error CS8866: Record member 'System.ValueType.X' must be a readable instance property or field of type 'bool' to match positional parameter 'X'.
                // record struct C(bool X)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("System.ValueType.X", "bool", "X").WithLocation(2, 22),
                // (2,22): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(bool X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(2, 22)
                );
        }

        [Fact]
        public void StaticCtor()
        {
            var src = @"
record R(int x)
{
    static void Main() { }

    static R()
    {
        System.Console.Write(""static ctor"");
    }
}
";

            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "static ctor", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void StaticCtor_ParameterlessPrimaryCtor()
        {
            var src = @"
record struct R(int I)
{
    static R() { }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void StaticCtor_CopyCtor()
        {
            var src = @"
record struct R(int I)
{
    static R(R r) { }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,12): error CS0132: 'R.R(R)': a static constructor must be parameterless
                //     static R(R r) { }
                Diagnostic(ErrorCode.ERR_StaticConstParam, "R").WithArguments("R.R(R)").WithLocation(4, 12)
                );
        }

        [Fact]
        public void InterfaceImplementation_NotReadonly()
        {
            var source = @"
I r = new R(42);
r.P2 = 43;
r.P3 = 44;
System.Console.Write((r.P1, r.P2, r.P3));

interface I
{
    int P1 { get; set; }
    int P2 { get; set; }
    int P3 { get; set; }
}
record struct R(int P1) : I
{
    public int P2 { get; set; } = 0;
    int I.P3 { get; set; } = 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44)");
        }

        [Fact]
        public void InterfaceImplementation_NotReadonly_InitOnlyInterface()
        {
            var source = @"
interface I
{
    int P1 { get; init; }
}
record struct R(int P1) : I;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,27): error CS8854: 'R' does not implement interface member 'I.P1.init'. 'R.P1.set' cannot implement 'I.P1.init'.
                // record struct R(int P1) : I;
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("R", "I.P1.init", "R.P1.set").WithLocation(6, 27)
                );
        }

        [Fact]
        public void InterfaceImplementation_Readonly()
        {
            var source = @"
I r = new R(42) { P2 = 43 };
System.Console.Write((r.P1, r.P2));

interface I
{
    int P1 { get; init; }
    int P2 { get; init; }
}
readonly record struct R(int P1) : I
{
    public int P2 { get; init; } = 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43)", verify: Verification.Skipped /* init-only */);
        }

        [Fact]
        public void InterfaceImplementation_Readonly_SetInterface()
        {
            var source = @"
interface I
{
    int P1 { get; set; }
}
readonly record struct R(int P1) : I;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,36): error CS8854: 'R' does not implement interface member 'I.P1.set'. 'R.P1.init' cannot implement 'I.P1.set'.
                // readonly record struct R(int P1) : I;
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly, "I").WithArguments("R", "I.P1.set", "R.P1.init").WithLocation(6, 36)
                );
        }

        [Fact]
        public void InterfaceImplementation_Readonly_PrivateImplementation()
        {
            var source = @"
I r = new R(42) { P2 = 43, P3 = 44 };
System.Console.Write((r.P1, r.P2, r.P3));

interface I
{
    int P1 { get; init; }
    int P2 { get; init; }
    int P3 { get; init; }
}
readonly record struct R(int P1) : I
{
    public int P2 { get; init; } = 0;
    int I.P3 { get; init; } = 0; // not practically initializable
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,28): error CS0117: 'R' does not contain a definition for 'P3'
                // I r = new R(42) { P2 = 43, P3 = 44 };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "P3").WithArguments("R", "P3").WithLocation(2, 28)
                );
        }

        [Fact]
        public void Initializers_01()
        {
            var src = @"
using System;

record struct C(int X)
{
    int Z = X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

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

            var recordDeclaration = tree.GetRoot().DescendantNodes().OfType<RecordDeclarationSyntax>().Single();
            Assert.Equal("C", recordDeclaration.Identifier.ValueText);
            Assert.Null(model.GetOperation(recordDeclaration));
        }

        [Fact]
        public void Initializers_02()
        {
            var src = @"
record struct C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,20): error CS9105: Cannot use primary constructor parameter 'int X' in this context.
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "X").WithArguments("int X").WithLocation(4, 20)
                );

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
        public void Initializers_03()
        {
            var src = @"
record struct C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,19): error CS9105: Cannot use primary constructor parameter 'int X' in this context.
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "X").WithArguments("int X").WithLocation(4, 19),
                // (4,19): error CS0133: The expression being assigned to 'C.Z' must be constant
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "X + 1").WithArguments("C.Z").WithLocation(4, 19)
                );

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
        public void Initializers_04()
        {
            var src = @"
using System;

record struct C(int X)
{
    Func<int> Z = () => X + 1;

    public static void Main()
    {
        var c = new C(1);
        Console.WriteLine(c.Z());
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"2").VerifyDiagnostics();

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
        public void SynthesizedRecordPointerProperty()
        {
            var src = @"
record struct R(int P1, int* P2, delegate*<int> P3);";

            var comp = CreateCompilation(src);
            var p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P1");
            Assert.False(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P2");
            Assert.True(p.HasPointerType);

            p = comp.GlobalNamespace.GetTypeMember("R").GetMember<SourcePropertySymbolBase>("P3");
            Assert.True(p.HasPointerType);
        }

        [Fact]
        public void PositionalMemberModifiers_In()
        {
            var src = @"
var r = new R(42);
int i = 43;
var r2 = new R(in i);
System.Console.Write((r.P1, r2.P1));

record struct R(in int P1);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "(42, 43)");

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(in System.Int32 P1)",
                "R..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void PositionalMemberModifiers_Params()
        {
            var src = @"
var r = new R(42, 43);
var r2 = new R(new[] { 44, 45 });
System.Console.Write((r.Array[0], r.Array[1], r2.Array[0], r2.Array[1]));

record struct R(params int[] Array);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44, 45)");

            var actualMembers = comp.GetMember<NamedTypeSymbol>("R").Constructors.ToTestDisplayStrings();
            var expectedMembers = new[]
            {
                "R..ctor(params System.Int32[] Array)",
                "R..ctor()"
            };
            AssertEx.Equal(expectedMembers, actualMembers);
        }

        [Fact]
        public void PositionalMemberDefaultValue()
        {
            var src = @"
var r = new R(); // This uses the parameterless constructor
System.Console.Write(r.P);

record struct R(int P = 42);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "0");
        }

        [Fact]
        public void PositionalMemberDefaultValue_PassingOneArgument()
        {
            var src = @"
var r = new R(41);
System.Console.Write(r.O);
System.Console.Write("" "");
System.Console.Write(r.P);

record struct R(int O, int P = 42);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "41 42");
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer()
        {
            var src = @"
var r = new R(0);
System.Console.Write(r.P);

record struct R(int O, int P = 1)
{
    public int P { get; init; } = 42;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,28): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int O, int P = 1)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(5, 28)
                );
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int, int)", @"
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int R.<O>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   42
  IL_000a:  stfld      ""int R.<P>k__BackingField""
  IL_000f:  ret
}");
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithoutInitializer()
        {
            var src = @"
record struct R(int P = 42)
{
    public int P { get; init; }

    public static void Main()
    {
        var r = new R();
        System.Console.Write(r.P);
    }
}
";
            var comp = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (2,15): error CS0843: Auto-implemented property 'R.P' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                // record struct R(int P = 42)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "R").WithArguments("R.P", "11.0").WithLocation(2, 15),
                // (2,21): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int P = 42)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(2, 21)
                );

            var verifier = CompileAndVerify(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.Regular11, verify: Verification.Skipped);
            verifier.VerifyDiagnostics(
                // (2,21): warning CS8907: Parameter 'P' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int P = 42)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "P").WithArguments("P").WithLocation(2, 21)
                );
            verifier.VerifyIL("R..ctor(int)", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int R.<P>k__BackingField""
  IL_0007:  ret
}
");
        }

        [Fact]
        public void PositionalMemberDefaultValue_AndPropertyWithInitializer_CopyingParameter()
        {
            var src = @"
var r = new R(0);
System.Console.Write(r.P);

record struct R(int O, int P = 42)
{
    public int P { get; init; } = P;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42", verify: Verification.Skipped /* init-only */);

            verifier.VerifyIL("R..ctor(int, int)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int R.<O>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.2
  IL_0009:  stfld      ""int R.<P>k__BackingField""
  IL_000e:  ret
}");
        }

        [Fact]
        public void RecordWithConstraints_NullableWarning()
        {
            var src = @"
#nullable enable
var r = new R<string?>(""R"");
var r2 = new R2<string?>(""R2"");
System.Console.Write((r.P, r2.P));

record struct R<T>(T P) where T : class;
record struct R2<T>(T P) where T : class { }
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (3,15): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                // var r = new R<string?>("R");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R<T>", "T", "string?").WithLocation(3, 15),
                // (4,17): warning CS8634: The type 'string?' cannot be used as type parameter 'T' in the generic type or method 'R2<T>'. Nullability of type argument 'string?' doesn't match 'class' constraint.
                // var r2 = new R2<string?>("R2");
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint, "string?").WithArguments("R2<T>", "T", "string?").WithLocation(4, 17)
                );
            CompileAndVerify(comp, expectedOutput: "(R, R2)");
        }

        [Fact]
        public void RecordWithConstraints_ConstraintError()
        {
            var src = @"
record struct R<T>(T P) where T : class;
record struct R2<T>(T P) where T : class { }

public class C
{
    public static void Main()
    {
        _ = new R<int>(1);
        _ = new R2<int>(2);
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (9,19): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R<T>'
                //         _ = new R<int>(1);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R<T>", "T", "int").WithLocation(9, 19),
                // (10,20): error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'R2<T>'
                //         _ = new R2<int>(2);
                Diagnostic(ErrorCode.ERR_RefConstraintNotSatisfied, "int").WithArguments("R2<T>", "T", "int").WithLocation(10, 20)
                );
        }

        [Fact]
        public void CyclicBases4()
        {
            var text =
@"
record struct A<T> : B<A<T>> { }
record struct B<T> : A<B<T>>
{
    A<T> F() { return null; }
}
";
            var comp = CreateCompilation(text);
            comp.GetDeclarationDiagnostics().Verify(
                // (3,22): error CS0527: Type 'A<B<T>>' in interface list is not an interface
                // record struct B<T> : A<B<T>>
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "A<B<T>>").WithArguments("A<B<T>>").WithLocation(3, 22),
                // (2,22): error CS0527: Type 'B<A<T>>' in interface list is not an interface
                // record struct A<T> : B<A<T>> { }
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "B<A<T>>").WithArguments("B<A<T>>").WithLocation(2, 22)
                );
        }

        [Fact]
        public void PartialClassWithDifferentTupleNamesInImplementedInterfaces()
        {
            var source = @"
public interface I<T> { }
public partial record C1 : I<(int a, int b)> { }
public partial record C1 : I<(int notA, int notB)> { }

public partial record C2 : I<(int a, int b)> { }
public partial record C2 : I<(int, int)> { }

public partial record C3 : I<(int a, int b)> { }
public partial record C3 : I<(int a, int b)> { }

public partial record C4 : I<(int a, int b)> { }
public partial record C4 : I<(int b, int a)> { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS8140: 'I<(int notA, int notB)>' is already listed in the interface list on type 'C1' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C1 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C1").WithArguments("I<(int notA, int notB)>", "I<(int a, int b)>", "C1").WithLocation(3, 23),
                // (6,23): error CS8140: 'I<(int, int)>' is already listed in the interface list on type 'C2' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C2 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C2").WithArguments("I<(int, int)>", "I<(int a, int b)>", "C2").WithLocation(6, 23),
                // (12,23): error CS8140: 'I<(int b, int a)>' is already listed in the interface list on type 'C4' with different tuple element names, as 'I<(int a, int b)>'.
                // public partial record C4 : I<(int a, int b)> { }
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList, "C4").WithArguments("I<(int b, int a)>", "I<(int a, int b)>", "C4").WithLocation(12, 23)
                );
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced()
        {
            var test = @"
partial public record struct C  // CS0267
{
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public record struct C  // CS0267
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(2, 1)
                );
        }

        [Fact]
        public void SealedStaticRecord()
        {
            var source = @"
sealed static record struct R;
";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,29): error CS0106: The modifier 'sealed' is not valid for this item
                // sealed static record struct R;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("sealed").WithLocation(2, 29),
                // (2,29): error CS0106: The modifier 'static' is not valid for this item
                // sealed static record struct R;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 29)
                );
        }

        [Fact]
        public void CS0513ERR_AbstractInConcreteClass02()
        {
            var text = @"
record struct C
{
    public abstract event System.Action E;
    public abstract int this[int x] { get; set; }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (5,25): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract int this[int x] { get; set; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "this").WithArguments("abstract").WithLocation(5, 25),
                // (4,41): error CS0106: The modifier 'abstract' is not valid for this item
                //     public abstract event System.Action E;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("abstract").WithLocation(4, 41)
                );
        }

        [Fact]
        public void CS0574ERR_BadDestructorName()
        {
            var test = @"
public record struct @iii
{
    ~iiii(){}
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,6): error CS0574: Name of destructor must match name of type
                //     ~iiii(){}
                Diagnostic(ErrorCode.ERR_BadDestructorName, "iiii").WithLocation(4, 6),
                // (4,6): error CS0575: Only class types can contain destructors
                //     ~iiii(){}
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "iiii").WithLocation(4, 6)
                );
        }

        [Fact]
        public void StaticRecordWithConstructorAndDestructor()
        {
            var text = @"
static record struct R(int I)
{
    public R() : this(0) { }
    ~R() { }
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (2,22): error CS0106: The modifier 'static' is not valid for this item
                // static record struct R(int I)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("static").WithLocation(2, 22),
                // (5,6): error CS0575: Only class types can contain destructors
                //     ~R() { }
                Diagnostic(ErrorCode.ERR_OnlyClassesCanContainDestructors, "R").WithLocation(5, 6)
                );
        }

        [Fact]
        public void RecordWithPartialMethodExplicitImplementation()
        {
            var source =
@"record struct R
{
    partial void M();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,18): error CS0751: A partial method must be declared within a partial type
                //     partial void M();
                Diagnostic(ErrorCode.ERR_PartialMethodOnlyInPartialClass, "M").WithLocation(3, 18)
                );
        }

        [Fact]
        public void RecordWithPartialMethodRequiringBody()
        {
            var source =
@"partial record struct R
{
    public partial int M();
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (3,24): error CS8795: Partial method 'R.M()' must have an implementation part because it has accessibility modifiers.
                //     public partial int M();
                Diagnostic(ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation, "M").WithArguments("R.M()").WithLocation(3, 24)
                );
        }

        [Fact]
        public void CanDeclareIteratorInRecord()
        {
            var source = @"
using System.Collections.Generic;

foreach(var i in new X(42).GetItems())
{
    System.Console.Write(i);
}

public record struct X(int a)
{
    public IEnumerable<int> GetItems() { yield return a; yield return a + 1; }
}";

            var comp = CreateCompilation(source).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "4243");
        }

        [Fact]
        public void ParameterlessConstructor()
        {
            var src = @"
System.Console.Write(new C().Property);

record struct C()
{
    public int Property { get; set; } = 42;
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void XmlDoc()
        {
            var src = @"
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public record struct C(int I1);

namespace System.Runtime.CompilerServices
{
    /// <summary>Ignored</summary>
    public static class IsExternalInit
    {
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(
@"<member name=""T:C"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", cMember.GetDocumentationCommentXml());
            var constructor = cMember.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:C.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", constructor.GetDocumentationCommentXml());

            Assert.Equal("", constructor.GetParameters()[0].GetDocumentationCommentXml());

            var property = cMember.GetMembers("I1").Single();
            AssertEx.Equal(
@"<member name=""P:C.I1"">
    <summary>Description for I1</summary>
</member>
", property.GetDocumentationCommentXml());
        }

        [Fact]
        public void XmlDoc_Cref()
        {
            var src = @"
/// <summary>Summary</summary>
/// <param name=""I1"">Description for <see cref=""I1""/></param>
public record struct C(int I1)
{
    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    public void M(int x) { }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Ignored</summary>
    public static class IsExternalInit
    {
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics(
                // (7,52): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(7, 52)
                );

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("I1", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            Assert.Equal(SymbolKind.Property, model.GetSymbolInfo(cref).Symbol!.Kind);

            var property = comp.GetMember("C.I1");
            AssertEx.Equal(
@"<member name=""P:C.I1"">
    <summary>Description for <see cref=""P:C.I1""/></summary>
</member>
", property.GetDocumentationCommentXml());
        }

        [Fact]
        public void XmlDoc_Cref_OtherMember()
        {
            var src = @"
/// <summary>Summary</summary>
/// <param name=""I1"">Description for <see cref=""I2""/></param>
public record struct C(int I1, int I2)
{
    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    public void M(int x) { }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Ignored</summary>
    public static class IsExternalInit
    {
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics(
                // (4,36): warning CS1573: Parameter 'I2' has no matching param tag in the XML comment for 'C.C(int, int)' (but other parameters do)
                // public record struct C(int I1, int I2)
                Diagnostic(ErrorCode.WRN_MissingParamTag, "I2").WithArguments("I2", "C.C(int, int)").WithLocation(4, 36),
                // (7,52): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(7, 52)
                );

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("I2", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            Assert.Equal(SymbolKind.Property, model.GetSymbolInfo(cref).Symbol!.Kind);

            var property = comp.GetMember("C.I1");
            AssertEx.Equal(
@"<member name=""P:C.I1"">
    <summary>Description for <see cref=""P:C.I2""/></summary>
</member>
", property.GetDocumentationCommentXml());
        }

        [Fact]
        public void XmlDoc_SeeAlso_InsideParamTag()
        {
            var src = @"
/// <summary>Summary</summary>
/// <param name=""I1"">Description for <seealso cref=""I2""/>something like I2</seealso></param>
public record struct C(int I1, int I2)
{
    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    public void M(int x) { }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>Ignored</summary>
    public static class IsExternalInit
    {
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularWithDocumentationComments);
            comp.VerifyDiagnostics(
                // (3,77): warning CS1570: XML comment has badly formed XML -- 'End tag 'seealso' does not match the start tag 'param'.'
                // /// <param name="I1">Description for <seealso cref="I2"/>something like I2</seealso></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, "seealso").WithArguments("seealso", "param").WithLocation(3, 77),
                // (3,85): warning CS1570: XML comment has badly formed XML -- 'End tag was not expected at this location.'
                // /// <param name="I1">Description for <seealso cref="I2"/>something like I2</seealso></param>
                Diagnostic(ErrorCode.WRN_XMLParseError, "<").WithLocation(3, 85),
                // (7,52): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(7, 52)
                );

            var tree = comp.SyntaxTrees.Single();
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var cref = docComments.First().DescendantNodes().OfType<XmlCrefAttributeSyntax>().First().Cref;
            Assert.Equal("I2", cref.ToString());

            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            Assert.Equal(SymbolKind.Property, model.GetSymbolInfo(cref).Symbol!.Kind);

            var property = comp.GetMember("C.I1");
            AssertEx.Equal(
@"<!-- Badly formed XML comment ignored for member ""P:C.I1"" -->
", property.GetDocumentationCommentXml());
        }

        [Fact]
        public void Deconstruct_Simple()
        {
            var source =
@"using System;

record struct B(int X, int Y)
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
  IL_0002:  call       ""readonly int B.X.get""
  IL_0007:  stind.i4
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  call       ""readonly int B.Y.get""
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

record struct B(int X)
{
    public int Y { get; init; } = 0;

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
                "readonly void B.Deconstruct(out System.Int32 X)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Nested()
        {
            var source =
@"using System;

record struct B(int X, int Y);

record struct C(B B, int Z)
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
  IL_0002:  call       ""readonly int B.X.get""
  IL_0007:  stind.i4
  IL_0008:  ldarg.2
  IL_0009:  ldarg.0
  IL_000a:  call       ""readonly int B.Y.get""
  IL_000f:  stind.i4
  IL_0010:  ret
}");

            verifier.VerifyIL("C.Deconstruct", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  call       ""readonly B C.B.get""
  IL_0007:  stobj      ""B""
  IL_000c:  ldarg.2
  IL_000d:  ldarg.0
  IL_000e:  call       ""readonly int C.Z.get""
  IL_0013:  stind.i4
  IL_0014:  ret
}");
        }

        [Fact]
        public void Deconstruct_PropertyCollision()
        {
            var source =
@"using System;

record struct B(int X, int Y)
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
            verifier.VerifyDiagnostics(
                // (3,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct B(int X, int Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(3, 21)
                );

            Assert.Equal(
                "void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                verifier.Compilation.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_MethodCollision_01()
        {
            var source = @"
record struct B(int X, int Y)
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
                // (4,16): error CS0102: The type 'B' already contains a definition for 'X'
                //     public int X() => 3;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("B", "X").WithLocation(4, 16)
                );

            Assert.Equal(
                "readonly void B.Deconstruct(out System.Int32 X, out System.Int32 Y)",
                comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_FieldCollision()
        {
            var source = @"
using System;

record struct C(int X)
{
    int X = 0;

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
                // (4,21): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(int X)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(4, 21),
                // (6,9): warning CS0414: The field 'C.X' is assigned but its value is never used
                //     int X = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "X").WithArguments("C.X").WithLocation(6, 9));

            Assert.Equal(
                "readonly void C.Deconstruct(out System.Int32 X)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty()
        {
            var source = @"
record struct C
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
        public void Deconstruct_Conversion_02()
        {
            var source = @"
#nullable enable
using System;

record struct C(string? X, string Y)
{
    public string X { get; init; } = null!;
    public string? Y { get; init; } = string.Empty;

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
            comp.VerifyDiagnostics(
                // (5,25): warning CS8907: Parameter 'X' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(string? X, string Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "X").WithArguments("X").WithLocation(5, 25),
                // (5,35): warning CS8907: Parameter 'Y' is unread. Did you forget to use it to initialize the property with that name?
                // record struct C(string? X, string Y)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "Y").WithArguments("Y").WithLocation(5, 35)
                );

            Assert.Equal(
                "readonly void C.Deconstruct(out System.String? X, out System.String Y)",
                comp.GetMember("C.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList()
        {
            var source = @"
record struct C()
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

            AssertEx.Equal(new[] {
                "C..ctor()",
                "void C.M(C c)",
                "void C.Main()",
                "readonly System.String C.ToString()",
                "readonly System.Boolean C.PrintMembers(System.Text.StringBuilder builder)",
                "System.Boolean C.op_Inequality(C left, C right)",
                "System.Boolean C.op_Equality(C left, C right)",
                "readonly System.Int32 C.GetHashCode()",
                "readonly System.Boolean C.Equals(System.Object obj)",
                "readonly System.Boolean C.Equals(C other)" },
                comp.GetMember<NamedTypeSymbol>("C").GetMembers().ToTestDisplayStrings());
        }

        [Fact]
        public void Deconstruct_Empty_WithParameterList_UserDefined_01()
        {
            var source =
@"using System;

record struct C(int I)
{
    public void Deconstruct()
    {
    }

    static void M(C c)
    {
        switch (c)
        {
            case C():
                Console.Write(12);
                break;
        }
    }

    public static void Main()
    {
        M(new C(42));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: "12");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void Deconstruct_GeneratedAsReadOnly()
        {
            var src = @"
record struct A(int I, string S);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var method = comp.GetMember<SynthesizedRecordDeconstruct>("A.Deconstruct");
            Assert.True(method.IsDeclaredReadOnly);
        }

        [Fact]
        public void Deconstruct_WithNonReadOnlyGetter_GeneratedAsNonReadOnly()
        {
            var src = @"
record struct A(int I, string S)
{
    public int I { get => 0; }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,21): warning CS8907: Parameter 'I' is unread. Did you forget to use it to initialize the property with that name?
                // record struct A(int I, string S)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "I").WithArguments("I").WithLocation(2, 21));
            var method = comp.GetMember<SynthesizedRecordDeconstruct>("A.Deconstruct");
            Assert.False(method.IsDeclaredReadOnly);
        }

        [Fact]
        public void Deconstruct_UserDefined()
        {
            var source =
@"using System;

record struct B(int X, int Y)
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
        public void Deconstruct_UserDefined_DifferentSignature_02()
        {
            var source =
@"using System;

record struct B(int X)
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
                // (5,16): error CS8874: Record member 'B.Deconstruct(out int)' must return 'void'.
                //     public int Deconstruct(out int a) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Deconstruct").WithArguments("B.Deconstruct(out int)", "void").WithLocation(5, 16),
                // (11,19): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'B', with 1 out parameters and a void return type.
                //             case B(int x):
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(int x)").WithArguments("B", "1").WithLocation(11, 19));

            Assert.Equal("System.Int32 B.Deconstruct(out System.Int32 a)", comp.GetMember("B.Deconstruct").ToTestDisplayString(includeNonNullable: false));
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        public void Deconstruct_UserDefined_Accessibility_07(string accessibility)
        {
            var source =
$@"
record struct A(int X)
{{
    {accessibility} void Deconstruct(out int a)
        => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,11): error CS8873: Record member 'A.Deconstruct(out int)' must be public.
                //      void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Fact]
        public void Deconstruct_UserDefined_Static_08()
        {
            var source =
@"
record struct A(int X)
{
    public static void Deconstruct(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS8877: Record member 'A.Deconstruct(out int)' may not be static.
                //     public static void Deconstruct(out int a)
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Deconstruct").WithArguments("A.Deconstruct(out int)").WithLocation(4, 24)
                );
        }

        [Fact]
        public void OutVarInPositionalParameterDefaultValue()
        {
            var source =
@"
record struct A(int X = A.M(out int a) + a)
{
    public static int M(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,25): error CS1736: Default parameter value for 'X' must be a compile-time constant
                // record struct A(int X = A.M(out int a) + a)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "A.M(out int a) + a").WithArguments("X").WithLocation(2, 25)
                );
        }

        [Fact]
        public void FieldConsideredUnassignedIfInitializationViaProperty()
        {
            var source = @"
record struct Pos(int X)
{
    private int x;
    public int X { get { return x; } set { x = value; } } = X;
}

record struct Pos2(int X)
{
    private int x = X; // value isn't validated by setter
    public int X { get { return x; } set { x = value; } }
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0171: Field 'Pos.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                // record struct Pos(int X)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "Pos").WithArguments("Pos.x", "11.0").WithLocation(2, 15),
                // (5,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int X { get { return x; } set { x = value; } } = X;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "X").WithLocation(5, 16)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyEmitDiagnostics(
                // (5,16): error CS8050: Only auto-implemented properties can have initializers.
                //     public int X { get { return x; } set { x = value; } } = X;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "X").WithLocation(5, 16)
                );
        }

        [Fact]
        public void IEquatableT_01()
        {
            var source =
@"record struct A<T>;
class Program
{
    static void F<T>(System.IEquatable<T> t)
    {
    }
    static void M<T>()
    {
        F(new A<T>());
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void IEquatableT_02()
        {
            var source =
@"using System;
record struct A;
record struct B<T>;

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "TrueTrue").VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_02_ImplicitImplementation()
        {
            var source =
@"using System;
record struct A
{
    public bool Equals(A other)
    {
        System.Console.Write(""A.Equals(A) "");
        return false;
    }
}
record struct B<T>
{
    public bool Equals(B<T> other)
    {
        System.Console.Write(""B.Equals(B) "");
        return true;
    }
}

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write("" "");
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "A.Equals(A) False B.Equals(B) True").VerifyDiagnostics(
                // (4,17): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 17),
                // (12,17): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(B<T> other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(12, 17)
                );
        }

        [Fact]
        public void IEquatableT_02_ExplicitImplementation()
        {
            var source =
@"using System;
record struct A
{
    bool IEquatable<A>.Equals(A other)
    {
        System.Console.Write(""A.Equals(A) "");
        return false;
    }
}
record struct B<T>
{
    bool IEquatable<B<T>>.Equals(B<T> other)
    {
        System.Console.Write(""B.Equals(B) "");
        return true;
    }
}

class Program
{
    static bool F<T>(IEquatable<T> t, T t2)
    {
        return t.Equals(t2);
    }
    static void Main()
    {
        Console.Write(F(new A(), new A()));
        Console.Write("" "");
        Console.Write(F(new B<int>(), new B<int>()));
    }
}";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "A.Equals(A) False B.Equals(B) True").VerifyDiagnostics();
        }

        [Fact]
        public void IEquatableT_03()
        {
            var source = @"
record struct A<T> : System.IEquatable<A<T>>;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void IEquatableT_MissingIEquatable()
        {
            var source = @"
record struct A<T>;
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(WellKnownType.System_IEquatable_T);
            comp.VerifyEmitDiagnostics(
                    // (2,15): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                    // record struct A<T>;
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 15),
                    // (2,15): error CS0518: Predefined type 'System.IEquatable`1' is not defined or imported
                    // record struct A<T>;
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.IEquatable`1").WithLocation(2, 15)
                    );

            var type = comp.GetMember<NamedTypeSymbol>("A");
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.InterfacesNoUseSiteDiagnostics().ToTestDisplayStrings());
            AssertEx.Equal(new[] { "System.IEquatable<A<T>>[missing]" }, type.AllInterfacesNoUseSiteDiagnostics.ToTestDisplayStrings());
        }

        [Fact]
        public void RecordEquals_01()
        {
            var source = @"
var a1 = new B();
var a2 = new B();
System.Console.WriteLine(a1.Equals(a2));

record struct B
{
    public bool Equals(B other)
    {
        System.Console.WriteLine(""B.Equals(B)"");
        return false;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,17): warning CS8851: 'B' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(B other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("B").WithLocation(8, 17)
                );

            CompileAndVerify(comp, expectedOutput:
@"
B.Equals(B)
False
");
        }

        [Fact]
        public void RecordEquals_01_NoInParameters()
        {
            var source = @"
var a1 = new B();
var a2 = new B();
System.Console.WriteLine(a1.Equals(in a2));

record struct B;
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,39): error CS1615: Argument 1 may not be passed with the 'in' keyword
                // System.Console.WriteLine(a1.Equals(in a2));
                Diagnostic(ErrorCode.ERR_BadArgExtraRef, "a2").WithArguments("1", "in").WithLocation(4, 39)
                );
        }

        [Theory]
        [InlineData("protected")]
        [InlineData("private protected")]
        [InlineData("internal protected")]
        public void RecordEquals_10(string accessibility)
        {
            var source =
$@"
record struct A
{{
    {accessibility} bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,29): error CS0666: 'A.Equals(A)': new protected member declared in struct
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.ERR_ProtectedInStruct, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,29): error CS8873: Record member 'A.Equals(A)' must be public.
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,29): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     internal protected bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Theory]
        [InlineData("")]
        [InlineData("private")]
        [InlineData("internal")]
        public void RecordEquals_11(string accessibility)
        {
            var source =
$@"
record struct A
{{
    {accessibility} bool Equals(A x)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,...): error CS8873: Record member 'A.Equals(A)' must be public.
                //      { accessibility } bool Equals(A x)
                Diagnostic(ErrorCode.ERR_NonPublicAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 11 + accessibility.Length),
                // (4,11): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //      bool Equals(A x)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 11 + accessibility.Length)
                );
        }

        [Fact]
        public void RecordEquals_12()
        {
            var source = @"
A a1 = new A();
A a2 = new A();

System.Console.Write(a1.Equals(a2));
System.Console.Write(a1.Equals((object)a2));

record struct A
{
    public bool Equals(B other) => throw null;
}
class B
{
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueTrue");
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");

            verifier.VerifyIL("A.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""A""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""A""
  IL_000f:  call       ""readonly bool A.Equals(A)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}");

            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("readonly System.Boolean A.Equals(A other)", recordEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, recordEquals.DeclaredAccessibility);
            Assert.False(recordEquals.IsAbstract);
            Assert.False(recordEquals.IsVirtual);
            Assert.False(recordEquals.IsOverride);
            Assert.False(recordEquals.IsSealed);
            Assert.True(recordEquals.IsImplicitlyDeclared);

            var objectEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordObjEquals>().Single();
            Assert.Equal("readonly System.Boolean A.Equals(System.Object obj)", objectEquals.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, objectEquals.DeclaredAccessibility);
            Assert.False(objectEquals.IsAbstract);
            Assert.False(objectEquals.IsVirtual);
            Assert.True(objectEquals.IsOverride);
            Assert.False(objectEquals.IsSealed);
            Assert.True(objectEquals.IsImplicitlyDeclared);

            MethodSymbol gethashCode = comp.GetMembers("A." + WellKnownMemberNames.ObjectGetHashCode).OfType<SynthesizedRecordGetHashCode>().Single();
            Assert.Equal("readonly System.Int32 A.GetHashCode()", gethashCode.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, gethashCode.DeclaredAccessibility);
            Assert.False(gethashCode.IsStatic);
            Assert.False(gethashCode.IsAbstract);
            Assert.False(gethashCode.IsVirtual);
            Assert.True(gethashCode.IsOverride);
            Assert.False(gethashCode.IsSealed);
            Assert.True(gethashCode.IsImplicitlyDeclared);
        }

        [Fact]
        public void RecordEquals_13()
        {
            var source = @"
record struct A
{
    public int Equals(A other)
        => throw null;

    bool System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,16): error CS8874: Record member 'A.Equals(A)' must return 'bool'.
                //     public int Equals(A other)
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "Equals").WithArguments("A.Equals(A)", "bool").WithLocation(4, 16),
                // (4,16): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public int Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 16)
                );
        }

        [Fact]
        public void RecordEquals_14()
        {
            var source = @"
record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.MakeTypeMissing(SpecialType.System_Boolean);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,1): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, @"record struct A
{
    public bool Equals(A other)
        => throw null;

    System.Boolean System.IEquatable<A>.Equals(A x) => throw null;
}").WithArguments("System.Boolean").WithLocation(2, 1),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (2,15): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                // record struct A
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "A").WithArguments("System.Boolean").WithLocation(2, 15),
                // (4,12): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "bool").WithArguments("System.Boolean").WithLocation(4, 12),
                // (4,17): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 17)
                );
        }

        [Fact]
        public void RecordEquals_19()
        {
            var source = @"
record struct A
{
    public static bool Equals(A x) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record struct A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 15),
                // (4,24): error CS8877: Record member 'A.Equals(A)' may not be static.
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A x) => throw null;
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void RecordEquals_RecordEqualsInValueType()
        {
            var src = @"
public record struct A;

namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual int GetHashCode() => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public bool Equals(A x) => throw null;
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Char { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(char c) => null;
        public StringBuilder Append(object o) => null;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());

            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1)
                );

            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.Equal("readonly System.Boolean A.Equals(A other)", recordEquals.ToTestDisplayString());
        }

        [Fact]
        public void RecordEquals_FourFields()
        {
            var source = @"
A a1 = new A(1, ""hello"");

System.Console.Write(a1.Equals(a1));
System.Console.Write(a1.Equals((object)a1));
System.Console.Write("" - "");

A a2 = new A(1, ""hello"") { fieldI = 100 };

System.Console.Write(a1.Equals(a2));
System.Console.Write(a1.Equals((object)a2));
System.Console.Write(a2.Equals(a1));
System.Console.Write(a2.Equals((object)a1));
System.Console.Write("" - "");

A a3 = new A(1, ""world"");

System.Console.Write(a1.Equals(a3));
System.Console.Write(a1.Equals((object)a3));
System.Console.Write(a3.Equals(a1));
System.Console.Write(a3.Equals((object)a1));

record struct A(int I, string S)
{
    public int fieldI = 42;
    public string fieldS = ""hello"";
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "TrueTrue - FalseFalseFalseFalse - FalseFalseFalseFalse");
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size       97 (0x61)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int A.<I>k__BackingField""
  IL_000b:  ldarg.1
  IL_000c:  ldfld      ""int A.<I>k__BackingField""
  IL_0011:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0016:  brfalse.s  IL_005f
  IL_0018:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_001d:  ldarg.0
  IL_001e:  ldfld      ""string A.<S>k__BackingField""
  IL_0023:  ldarg.1
  IL_0024:  ldfld      ""string A.<S>k__BackingField""
  IL_0029:  callvirt   ""bool System.Collections.Generic.EqualityComparer<string>.Equals(string, string)""
  IL_002e:  brfalse.s  IL_005f
  IL_0030:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0035:  ldarg.0
  IL_0036:  ldfld      ""int A.fieldI""
  IL_003b:  ldarg.1
  IL_003c:  ldfld      ""int A.fieldI""
  IL_0041:  callvirt   ""bool System.Collections.Generic.EqualityComparer<int>.Equals(int, int)""
  IL_0046:  brfalse.s  IL_005f
  IL_0048:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_004d:  ldarg.0
  IL_004e:  ldfld      ""string A.fieldS""
  IL_0053:  ldarg.1
  IL_0054:  ldfld      ""string A.fieldS""
  IL_0059:  callvirt   ""bool System.Collections.Generic.EqualityComparer<string>.Equals(string, string)""
  IL_005e:  ret
  IL_005f:  ldc.i4.0
  IL_0060:  ret
}");

            verifier.VerifyIL("A.Equals(object)", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  isinst     ""A""
  IL_0006:  brfalse.s  IL_0015
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  unbox.any  ""A""
  IL_000f:  call       ""readonly bool A.Equals(A)""
  IL_0014:  ret
  IL_0015:  ldc.i4.0
  IL_0016:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size       86 (0x56)
  .maxstack  3
  IL_0000:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0005:  ldarg.0
  IL_0006:  ldfld      ""int A.<I>k__BackingField""
  IL_000b:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_0010:  ldc.i4     0xa5555529
  IL_0015:  mul
  IL_0016:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""string A.<S>k__BackingField""
  IL_0021:  callvirt   ""int System.Collections.Generic.EqualityComparer<string>.GetHashCode(string)""
  IL_0026:  add
  IL_0027:  ldc.i4     0xa5555529
  IL_002c:  mul
  IL_002d:  call       ""System.Collections.Generic.EqualityComparer<int> System.Collections.Generic.EqualityComparer<int>.Default.get""
  IL_0032:  ldarg.0
  IL_0033:  ldfld      ""int A.fieldI""
  IL_0038:  callvirt   ""int System.Collections.Generic.EqualityComparer<int>.GetHashCode(int)""
  IL_003d:  add
  IL_003e:  ldc.i4     0xa5555529
  IL_0043:  mul
  IL_0044:  call       ""System.Collections.Generic.EqualityComparer<string> System.Collections.Generic.EqualityComparer<string>.Default.get""
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""string A.fieldS""
  IL_004f:  callvirt   ""int System.Collections.Generic.EqualityComparer<string>.GetHashCode(string)""
  IL_0054:  add
  IL_0055:  ret
}");
        }

        [Fact]
        public void RecordEquals_StaticField()
        {
            var source = @"
record struct A
{
    public static int field = 42;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp);
            verifier.VerifyIL("A.Equals(A)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  ret
}");

            verifier.VerifyIL("A.GetHashCode()", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RecordEquals_GeneratedAsReadOnly()
        {
            var src = @"
record struct A(int I, string S);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var recordEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordEquals>().Single();
            Assert.True(recordEquals.IsDeclaredReadOnly);
        }

        [Fact]
        public void ObjectEquals_06()
        {
            var source = @"
record struct A
{
    public static new bool Equals(object obj) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,28): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public static new bool Equals(object obj) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(4, 28)
                );
        }

        [Fact]
        public void ObjectEquals_UserDefined()
        {
            var source = @"
record struct A
{
    public override bool Equals(object obj) => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,26): error CS0111: Type 'A' already defines a member called 'Equals' with the same parameter types
                //     public override bool Equals(object obj) => throw null;
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Equals").WithArguments("Equals", "A").WithLocation(4, 26)
                );
        }

        [Fact]
        public void ObjectEquals_GeneratedAsReadOnly()
        {
            var src = @"
record struct A(int I, string S);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var objectEquals = comp.GetMembers("A.Equals").OfType<SynthesizedRecordObjEquals>().Single();
            Assert.True(objectEquals.IsDeclaredReadOnly);
        }

        [Fact]
        public void GetHashCode_UserDefined()
        {
            var source = @"
System.Console.Write(new A().GetHashCode());

record struct A
{
    public override int GetHashCode() => 42;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void GetHashCode_GetHashCodeInValueType()
        {
            var src = @"
public record struct A;

namespace System
{
    public class Object
    {
        public virtual bool Equals(object x) => throw null;
        public virtual string ToString() => throw null;
    }
    public class Exception { }
    public class ValueType
    {
        public virtual int GetHashCode() => throw null;
    }
    public class Attribute { }
    public class String { }
    public struct Void { }
    public struct Boolean { }
    public struct Char { }
    public struct Int32 { }
    public interface IEquatable<T> { }
}
namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T>
    {
        public static EqualityComparer<T> Default => throw null;
        public abstract int GetHashCode(T t);
    }
}
namespace System.Text
{
    public class StringBuilder
    {
        public StringBuilder Append(string s) => null;
        public StringBuilder Append(char c) => null;
        public StringBuilder Append(object o) => null;
    }
}
";
            var comp = CreateEmptyCompilation(src, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute());

            comp.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (2,22): error CS8869: 'A.GetHashCode()' does not override expected method from 'object'.
                // public record struct A;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "A").WithArguments("A.GetHashCode()").WithLocation(2, 22)
                );
        }

        [Fact]
        public void GetHashCode_MissingEqualityComparer_EmptyRecord()
        {
            var src = @"
public record struct A;
";
            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_EqualityComparer_T);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void GetHashCode_MissingEqualityComparer_NonEmptyRecord()
        {
            var src = @"
public record struct A(int I);
";
            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Collections_Generic_EqualityComparer_T);

            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.GetHashCode'
                // public record struct A(int I);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "public record struct A(int I);").WithArguments("System.Collections.Generic.EqualityComparer`1", "GetHashCode").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Collections.Generic.EqualityComparer`1.get_Default'
                // public record struct A(int I);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "public record struct A(int I);").WithArguments("System.Collections.Generic.EqualityComparer`1", "get_Default").WithLocation(2, 1)
                );
        }

        [Fact]
        public void GetHashCode_GeneratedAsReadOnly()
        {
            var src = @"
record struct A(int I, string S);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var method = comp.GetMember<SynthesizedRecordGetHashCode>("A.GetHashCode");
            Assert.True(method.IsDeclaredReadOnly);
        }

        [Fact]
        public void GetHashCodeIsDefinedButEqualsIsNot()
        {
            var src = @"
public record struct C
{
    public object Data;
    public override int GetHashCode() { return 0; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void EqualsIsDefinedButGetHashCodeIsNot()
        {
            var src = @"
public record struct C
{
    public object Data;
    public bool Equals(C c) { return false; }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (5,17): warning CS8851: 'C' defines 'Equals' but not 'GetHashCode'
                //     public bool Equals(C c) { return false; }
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("C").WithLocation(5, 17));
        }

        [Fact]
        public void EqualityOperators_01()
        {
            var source = @"
record struct A(int X)
{
    public bool Equals(ref A other)
        => throw null;

    static void Main()
    {
        Test(default, default);
        Test(default, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
        var a = new A(11);
        Test(a, a);
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: @"
True True False False
True True False False
True True False False
False False True True
True True False False
").VerifyDiagnostics();

            var comp = (CSharpCompilation)verifier.Compilation;
            MethodSymbol op = comp.GetMembers("A." + WellKnownMemberNames.EqualityOperatorName).OfType<SynthesizedRecordEqualityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Equality(A left, A right)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            op = comp.GetMembers("A." + WellKnownMemberNames.InequalityOperatorName).OfType<SynthesizedRecordInequalityOperator>().Single();
            Assert.Equal("System.Boolean A.op_Inequality(A left, A right)", op.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, op.DeclaredAccessibility);
            Assert.True(op.IsStatic);
            Assert.False(op.IsAbstract);
            Assert.False(op.IsVirtual);
            Assert.False(op.IsOverride);
            Assert.False(op.IsSealed);
            Assert.True(op.IsImplicitlyDeclared);

            verifier.VerifyIL("bool A.op_Equality(A, A)", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarga.s   V_0
  IL_0002:  ldarg.1
  IL_0003:  call       ""readonly bool A.Equals(A)""
  IL_0008:  ret
}
");

            verifier.VerifyIL("bool A.op_Inequality(A, A)", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  call       ""bool A.op_Equality(A, A)""
  IL_0007:  ldc.i4.0
  IL_0008:  ceq
  IL_000a:  ret
}
");
        }

        [Fact]
        public void EqualityOperators_03()
        {
            var source =
@"
record struct A
{
    public static bool operator==(A r1, A r2)
        => throw null;
    public static bool operator==(A r1, string r2)
        => throw null;
    public static bool operator!=(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool operator==(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "==").WithArguments("op_Equality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_04()
        {
            var source = @"
record struct A
{
    public static bool operator!=(A r1, A r2)
        => throw null;
    public static bool operator!=(string r1, A r2)
        => throw null;
    public static bool operator==(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,32): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool operator!=(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "!=").WithArguments("op_Inequality", "A").WithLocation(4, 32)
                );
        }

        [Fact]
        public void EqualityOperators_05()
        {
            var source = @"
record struct A
{
    public static bool op_Equality(A r1, A r2)
        => throw null;
    public static bool op_Equality(string r1, A r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Equality' with the same parameter types
                //     public static bool op_Equality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Equality").WithArguments("op_Equality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_06()
        {
            var source = @"
record struct A
{
    public static bool op_Inequality(A r1, A r2)
        => throw null;
    public static bool op_Inequality(A r1, string r2)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS0111: Type 'A' already defines a member called 'op_Inequality' with the same parameter types
                //     public static bool op_Inequality(A r1, A r2)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "op_Inequality").WithArguments("op_Inequality", "A").WithLocation(4, 24)
                );
        }

        [Fact]
        public void EqualityOperators_07()
        {
            var source = @"
record struct A
{
    public static bool Equals(A other)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,15): error CS0736: 'A' does not implement interface member 'IEquatable<A>.Equals(A)'. 'A.Equals(A)' cannot implement an interface member because it is static.
                // record struct A
                Diagnostic(ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic, "A").WithArguments("A", "System.IEquatable<A>.Equals(A)", "A.Equals(A)").WithLocation(2, 15),
                // (4,24): error CS8877: Record member 'A.Equals(A)' may not be static.
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "Equals").WithArguments("A.Equals(A)").WithLocation(4, 24),
                // (4,24): warning CS8851: 'A' defines 'Equals' but not 'GetHashCode'
                //     public static bool Equals(A other)
                Diagnostic(ErrorCode.WRN_RecordEqualsWithoutGetHashCode, "Equals").WithArguments("A").WithLocation(4, 24)
                );
        }

        [Theory]
        [CombinatorialData]
        public void EqualityOperators_09(bool useImageReference)
        {
            var source1 = @"
public record struct A(int X);
";
            var comp1 = CreateCompilation(source1);

            var source2 =
@"
class Program
{
    static void Main()
    {
        Test(default, default);
        Test(default, new A(0));
        Test(new A(1), new A(1));
        Test(new A(2), new A(3));
    }

    static void Test(A a1, A a2)
    {
        System.Console.WriteLine(""{0} {1} {2} {3}"", a1 == a2, a2 == a1, a1 != a2, a2 != a1);
    }
}
";
            CompileAndVerify(source2, references: new[] { useImageReference ? comp1.EmitToImageReference() : comp1.ToMetadataReference() }, expectedOutput: @"
True True False False
True True False False
True True False False
False False True True
").VerifyDiagnostics();
        }

        [Fact]
        public void GetSimpleNonTypeMembers_DirectApiCheck()
        {
            var src = @"
public record struct RecordB();
";
            var comp = CreateCompilation(src);
            var b = comp.GlobalNamespace.GetTypeMember("RecordB");
            AssertEx.SetEqual(new[] { "System.Boolean RecordB.op_Equality(RecordB left, RecordB right)" },
                b.GetSimpleNonTypeMembers("op_Equality").ToTestDisplayStrings());
        }

        [Fact]
        public void ToString_NestedRecord()
        {
            var src = @"
var c1 = new Outer.C1(42);
System.Console.Write(c1.ToString());

public class Outer
{
    public record struct C1(int I1);
}
";

            var compDebug = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.DebugExe);
            var compRelease = CreateCompilation(new[] { src, IsExternalInitTypeDefinition }, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(compDebug, expectedOutput: "C1 { I1 = 42 }");
            compDebug.VerifyEmitDiagnostics();

            CompileAndVerify(compRelease, expectedOutput: "C1 { I1 = 42 }");
            compRelease.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_TopLevelRecord_Empty()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1;
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  call       ""readonly bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0030
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.s   32
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(char)""
  IL_002f:  pop
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.s   125
  IL_0033:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(char)""
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   ""string object.ToString()""
  IL_003f:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilder()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeTypeMissing(WellKnownType.System_Text_StringBuilder);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record struct C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "record struct C1;").WithArguments("System.Text.StringBuilder").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1),
                // (2,15): error CS0518: Predefined type 'System.Text.StringBuilder' is not defined or imported
                // record struct C1;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C1").WithArguments("System.Text.StringBuilder").WithLocation(2, 15)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderCtor()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__ctor);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder..ctor'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", ".ctor").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_MissingStringBuilderAppendString()
        {
            var src = @"
record struct C1;
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1;").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_OneProperty_MissingStringBuilderAppendString()
        {
            var src = @"
record struct C1(int P);
";

            var comp = CreateCompilation(src);
            comp.MakeMemberMissing(WellKnownMember.System_Text_StringBuilder__AppendString);
            comp.VerifyEmitDiagnostics(
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1),
                // (2,1): error CS0656: Missing compiler required member 'System.Text.StringBuilder.Append'
                // record struct C1(int P);
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "record struct C1(int P);").WithArguments("System.Text.StringBuilder", "Append").WithLocation(2, 1)
                );
        }

        [Fact]
        public void ToString_RecordWithIndexer()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

record struct C1(int I1)
{
    private int field = 44;
    public int this[int i] => 0;
    public int PropertyWithoutGetter { set { } }
    public int P2 { get => 43; }
    public event System.Action a = null;

    private int field1 = 100;
    internal int field2 = 100;

    private int Property1 { get; set; } = 100;
    internal int Property2 { get; set; } = 100;
}
";

            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "C1 { I1 = 42, P2 = 43 }");
            comp.VerifyEmitDiagnostics(
                // (7,17): warning CS0414: The field 'C1.field' is assigned but its value is never used
                //     private int field = 44;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field").WithArguments("C1.field").WithLocation(7, 17),
                // (11,32): warning CS0414: The field 'C1.a' is assigned but its value is never used
                //     public event System.Action a = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "a").WithArguments("C1.a").WithLocation(11, 32),
                // (13,17): warning CS0414: The field 'C1.field1' is assigned but its value is never used
                //     private int field1 = 100;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field1").WithArguments("C1.field1").WithLocation(13, 17)
                );
        }

        [Fact]
        public void ToString_PrivateGetter()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1
{
    public int P1 { private get => 43; set => throw null; }
}
";

            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "C1 { P1 = 43 }");
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ValueType()
        {
            var src = @"
var c1 = new C1() { field = 42 };
System.Console.Write(c1.ToString());

record struct C1
{
    public int field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal(Accessibility.Private, print.DeclaredAccessibility);
            Assert.False(print.IsOverride);
            Assert.False(print.IsVirtual);
            Assert.False(print.IsAbstract);
            Assert.False(print.IsSealed);
            Assert.True(print.IsImplicitlyDeclared);

            var toString = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.ObjectToString);
            Assert.Equal(Accessibility.Public, toString.DeclaredAccessibility);
            Assert.True(toString.IsOverride);
            Assert.False(toString.IsVirtual);
            Assert.False(toString.IsAbstract);
            Assert.False(toString.IsSealed);
            Assert.True(toString.IsImplicitlyDeclared);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field = ""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""int C1.field""
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  constrained. ""int""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0026:  pop
  IL_0027:  ldc.i4.1
  IL_0028:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ConstrainedValueType()
        {
            var src = @"
var c1 = new C1<int>() { field = 42 };
System.Console.Write(c1.ToString());

record struct C1<T> where T : struct
{
    public T field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = 42 }");

            v.VerifyIL("C1<T>." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field = ""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""T C1<T>.field""
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  constrained. ""T""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0026:  pop
  IL_0027:  ldc.i4.1
  IL_0028:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_OneField_ReferenceType()
        {
            var src = @"
var c1 = new C1() { field = ""hello"" };
System.Console.Write(c1.ToString());

record struct C1
{
    public string field;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { field = hello }");

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""field = ""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  ldfld      ""string C1.field""
  IL_0013:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0018:  pop
  IL_0019:  ldc.i4.1
  IL_001a:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_TwoFields_ReferenceType()
        {
            var src = @"
var c1 = new C1(42) { field1 = ""hi"", field2 = null };
System.Console.Write(c1.ToString());

record struct C1(int I)
{
    public string field1 = null;
    public string field2 = null;

    private string field3 = null;
    internal string field4 = null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (10,20): warning CS0414: The field 'C1.field3' is assigned but its value is never used
                //     private string field3 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "field3").WithArguments("C1.field3").WithLocation(10, 20)
                );
            var v = CompileAndVerify(comp, expectedOutput: "C1 { I = 42, field1 = hi, field2 =  }");

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       91 (0x5b)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""I = ""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  call       ""readonly int C1.I.get""
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  constrained. ""int""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0026:  pop
  IL_0027:  ldarg.1
  IL_0028:  ldstr      "", field1 = ""
  IL_002d:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0032:  pop
  IL_0033:  ldarg.1
  IL_0034:  ldarg.0
  IL_0035:  ldfld      ""string C1.field1""
  IL_003a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_003f:  pop
  IL_0040:  ldarg.1
  IL_0041:  ldstr      "", field2 = ""
  IL_0046:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_004b:  pop
  IL_004c:  ldarg.1
  IL_004d:  ldarg.0
  IL_004e:  ldfld      ""string C1.field2""
  IL_0053:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(object)""
  IL_0058:  pop
  IL_0059:  ldc.i4.1
  IL_005a:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_Readonly()
        {
            var src = @"
var c1 = new C1(42);
System.Console.Write(c1.ToString());

readonly record struct C1(int I);
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var v = CompileAndVerify(comp, expectedOutput: "C1 { I = 42 }", verify: Verification.Skipped /* init-only */);

            v.VerifyIL("C1." + WellKnownMemberNames.PrintMembersMethodName, @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldstr      ""I = ""
  IL_0006:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_000b:  pop
  IL_000c:  ldarg.1
  IL_000d:  ldarg.0
  IL_000e:  call       ""int C1.I.get""
  IL_0013:  stloc.0
  IL_0014:  ldloca.s   V_0
  IL_0016:  constrained. ""int""
  IL_001c:  callvirt   ""string object.ToString()""
  IL_0021:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0026:  pop
  IL_0027:  ldc.i4.1
  IL_0028:  ret
}
");
            v.VerifyIL("C1." + WellKnownMemberNames.ObjectToString, @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.Text.StringBuilder V_0)
  IL_0000:  newobj     ""System.Text.StringBuilder..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""C1""
  IL_000c:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_0011:  pop
  IL_0012:  ldloc.0
  IL_0013:  ldstr      "" { ""
  IL_0018:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(string)""
  IL_001d:  pop
  IL_001e:  ldarg.0
  IL_001f:  ldloc.0
  IL_0020:  call       ""bool C1.PrintMembers(System.Text.StringBuilder)""
  IL_0025:  brfalse.s  IL_0030
  IL_0027:  ldloc.0
  IL_0028:  ldc.i4.s   32
  IL_002a:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(char)""
  IL_002f:  pop
  IL_0030:  ldloc.0
  IL_0031:  ldc.i4.s   125
  IL_0033:  callvirt   ""System.Text.StringBuilder System.Text.StringBuilder.Append(char)""
  IL_0038:  pop
  IL_0039:  ldloc.0
  IL_003a:  callvirt   ""string object.ToString()""
  IL_003f:  ret
}
");
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());

record struct C1
{
    public override string ToString() => ""RAN"";
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "RAN");

            var print = comp.GetMember<MethodSymbol>("C1." + WellKnownMemberNames.PrintMembersMethodName);
            Assert.Equal("readonly System.Boolean C1." + WellKnownMemberNames.PrintMembersMethodName + "(System.Text.StringBuilder builder)", print.ToTestDisplayString());
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_New()
        {
            var src = @"
record struct C1
{
    public new string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,23): error CS8869: 'C1.ToString()' does not override expected method from 'object'.
                //     public new string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_DoesNotOverrideMethodFromObject, "ToString").WithArguments("C1.ToString()").WithLocation(4, 23)
                );
        }

        [Fact]
        public void ToString_TopLevelRecord_UserDefinedToString_Sealed()
        {
            var src = @"
record struct C1
{
    public sealed override string ToString() => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,35): error CS0106: The modifier 'sealed' is not valid for this item
                //     public sealed override string ToString() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "ToString").WithArguments("sealed").WithLocation(4, 35)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WithNullableStringBuilder()
        {
            var src = @"
#nullable enable
record struct C1
{
    private bool PrintMembers(System.Text.StringBuilder? builder) => throw null!;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_ErrorReturnType()
        {
            var src = @"
record struct C1
{
    private Error PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                //     private Error PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(4, 13)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongReturnType()
        {
            var src = @"
record struct C1
{
    private int PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,17): error CS8874: Record member 'C1.PrintMembers(StringBuilder)' must return 'bool'.
                //     private int PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_SignatureMismatchInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)", "bool").WithLocation(4, 17)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers()
        {
            var src = @"
var c1 = new C1();
System.Console.Write(c1.ToString());
System.Console.Write("" - "");
c1.M();

record struct C1
{
    private bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append(""RAN"");
        return true;
    }

    public void M()
    {
        var builder = new System.Text.StringBuilder();
        if (PrintMembers(builder))
        {
            System.Console.Write(builder.ToString());
        }
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { RAN } - RAN");
        }

        [Fact]
        public void ToString_CallingSynthesizedPrintMembers()
        {
            var src = @"
var c1 = new C1(1, 2, 3);
System.Console.Write(c1.ToString());
System.Console.Write("" - "");
c1.M();

record struct C1(int I, int I2, int I3)
{
    public void M()
    {
        var builder = new System.Text.StringBuilder();
        if (PrintMembers(builder))
        {
            System.Console.Write(builder.ToString());
        }
    }
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "C1 { I = 1, I2 = 2, I3 = 3 } - I = 1, I2 = 2, I3 = 3");
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_WrongAccessibility()
        {
            var src = @"
var c = new C1();
System.Console.Write(c.ToString());

record struct C1
{
    internal bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (7,19): error CS8879: Record member 'C1.PrintMembers(StringBuilder)' must be private.
                //     internal bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_NonPrivateAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(7, 19)
                );
        }

        [Fact]
        public void ToString_UserDefinedPrintMembers_Static()
        {
            var src = @"
record struct C1
{
    static private bool PrintMembers(System.Text.StringBuilder builder) => throw null;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8877: Record member 'C1.PrintMembers(StringBuilder)' may not be static.
                //     static private bool PrintMembers(System.Text.StringBuilder builder) => throw null;
                Diagnostic(ErrorCode.ERR_StaticAPIInRecord, "PrintMembers").WithArguments("C1.PrintMembers(System.Text.StringBuilder)").WithLocation(4, 25)
                );
        }

        [Fact]
        public void ToString_GeneratedAsReadOnly()
        {
            var src = @"
record struct A(int I, string S);
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var method = comp.GetMember<SynthesizedRecordToString>("A.ToString");
            Assert.True(method.IsDeclaredReadOnly);
        }

        [Fact]
        public void ToString_WihtNonReadOnlyGetter_GeneratedAsNonReadOnly()
        {
            var src = @"
record struct A(int I, string S)
{
    public double T => 0.1;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var method = comp.GetMember<SynthesizedRecordToString>("A.ToString");
            Assert.False(method.IsDeclaredReadOnly);
        }

        [Fact]
        public void AmbigCtor_WithPropertyInitializer()
        {
            // Scenario causes ambiguous ctor for record class, but not record struct
            var src = @"
record struct R(R X)
{
    public R X { get; init; } = X;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,14): error CS0523: Struct member 'R.X' of type 'R' causes a cycle in the struct layout
                //     public R X { get; init; } = X;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "X").WithArguments("R.X", "R").WithLocation(4, 14)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var parameterSyntax = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
            var parameter = model.GetDeclaredSymbol(parameterSyntax)!;
            Assert.Equal("R X", parameter.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, parameter.Kind);
            Assert.Equal("R..ctor(R X)", parameter.ContainingSymbol.ToTestDisplayString());

            var initializerSyntax = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();
            var initializer = model.GetSymbolInfo(initializerSyntax.Value).Symbol!;
            Assert.Equal("R X", initializer.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, initializer.Kind);
            Assert.Equal("R..ctor(R X)", initializer.ContainingSymbol.ToTestDisplayString());

            var src2 = @"
record struct R(R X);
";
            var comp2 = CreateCompilation(src2);
            comp2.VerifyEmitDiagnostics(
                // (2,19): error CS0523: Struct member 'R.X' of type 'R' causes a cycle in the struct layout
                // record struct R(R X);
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "X").WithArguments("R.X", "R").WithLocation(2, 19)
                );
        }

        [Fact]
        public void GetDeclaredSymbolOnAnOutLocalInPropertyInitializer()
        {
            var src = @"
record struct R(int I)
{
    public int I { get; init; } = M(out int i);
    static int M(out int i) => throw null;
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,21): warning CS8907: Parameter 'I' is unread. Did you forget to use it to initialize the property with that name?
                // record struct R(int I)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "I").WithArguments("I").WithLocation(2, 21)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var outVarSyntax = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Single();
            var outVar = model.GetDeclaredSymbol(outVarSyntax)!;
            Assert.Equal("System.Int32 i", outVar.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, outVar.Kind);
            Assert.Equal("System.Int32 R.<I>k__BackingField", outVar.ContainingSymbol.ToTestDisplayString());
        }

        [Fact]
        public void AnalyzerActions_01()
        {
            // Test RegisterSyntaxNodeAction
            var text1 = @"
record struct A([Attr1]int X = 0) : I1
{
    private int M() => 3;
    A(string S) : this(4) => throw null;
}

interface I1 {}

class Attr1 : System.Attribute {}
";
            var analyzer = new AnalyzerActions_01_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCountRecordStructDeclarationA);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCountSimpleBaseTypeI1onA);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCountParameterListAPrimaryCtor);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCountConstructorDeclaration);
            Assert.Equal(1, analyzer.FireCountStringParameterList);
            Assert.Equal(1, analyzer.FireCountThisConstructorInitializer);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
        }

        private class AnalyzerActions_01_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount0;
            public int FireCountRecordStructDeclarationA;
            public int FireCount3;
            public int FireCountSimpleBaseTypeI1onA;
            public int FireCount5;
            public int FireCountParameterListAPrimaryCtor;
            public int FireCount7;
            public int FireCountConstructorDeclaration;
            public int FireCountStringParameterList;
            public int FireCountThisConstructorInitializer;
            public int FireCount11;
            public int FireCount12;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Fail, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.ThisConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Fail, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.RecordStructDeclaration);
                context.RegisterSyntaxNodeAction(Handle7, SyntaxKind.IdentifierName);
                context.RegisterSyntaxNodeAction(Handle8, SyntaxKind.SimpleBaseType);
                context.RegisterSyntaxNodeAction(Handle9, SyntaxKind.ParameterList);
                context.RegisterSyntaxNodeAction(Handle10, SyntaxKind.ArgumentList);
            }

            protected void Handle1(SyntaxNodeAnalysisContext context)
            {
                var literal = (LiteralExpressionSyntax)context.Node;

                switch (literal.ToString())
                {
                    case "0":
                        Interlocked.Increment(ref FireCount0);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "3":
                        Interlocked.Increment(ref FireCount7);
                        Assert.Equal("System.Int32 A.M()", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "4":
                        Interlocked.Increment(ref FireCount12);
                        Assert.Equal("A..ctor(System.String S)", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(literal.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle2(SyntaxNodeAnalysisContext context)
            {
                var equalsValue = (EqualsValueClauseSyntax)context.Node;

                switch (equalsValue.ToString())
                {
                    case "= 0":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(equalsValue.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle3(SyntaxNodeAnalysisContext context)
            {
                var initializer = (ConstructorInitializerSyntax)context.Node;

                switch (initializer.ToString())
                {
                    case ": this(4)":
                        Interlocked.Increment(ref FireCountThisConstructorInitializer);
                        Assert.Equal("A..ctor(System.String S)", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(initializer.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle4(SyntaxNodeAnalysisContext context)
            {
                Interlocked.Increment(ref FireCountConstructorDeclaration);
                Assert.Equal("A..ctor(System.String S)", context.ContainingSymbol.ToTestDisplayString());
            }

            protected void Fail(SyntaxNodeAnalysisContext context)
            {
                Assert.True(false);
            }

            protected void Handle6(SyntaxNodeAnalysisContext context)
            {
                var record = (RecordDeclarationSyntax)context.Node;
                Assert.Equal(SyntaxKind.RecordStructDeclaration, record.Kind());

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "A":
                        Interlocked.Increment(ref FireCountRecordStructDeclarationA);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(record.SyntaxTree, context.ContainingSymbol!.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle7(SyntaxNodeAnalysisContext context)
            {
                var identifier = (IdentifierNameSyntax)context.Node;

                switch (identifier.Identifier.ValueText)
                {
                    case "Attr1":
                        Interlocked.Increment(ref FireCount5);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                }
            }

            protected void Handle8(SyntaxNodeAnalysisContext context)
            {
                var baseType = (SimpleBaseTypeSyntax)context.Node;

                switch (baseType.ToString())
                {
                    case "I1":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "A":
                                Interlocked.Increment(ref FireCountSimpleBaseTypeI1onA);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;

                    case "System.Attribute":
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle9(SyntaxNodeAnalysisContext context)
            {
                var parameterList = (ParameterListSyntax)context.Node;

                switch (parameterList.ToString())
                {
                    case "([Attr1]int X = 0)":
                        Interlocked.Increment(ref FireCountParameterListAPrimaryCtor);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "(string S)":
                        Interlocked.Increment(ref FireCountStringParameterList);
                        Assert.Equal("A..ctor(System.String S)", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "()":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle10(SyntaxNodeAnalysisContext context)
            {
                var argumentList = (ArgumentListSyntax)context.Node;

                switch (argumentList.ToString())
                {
                    case "(4)":
                        Interlocked.Increment(ref FireCount11);
                        Assert.Equal("A..ctor(System.String S)", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_02()
        {
            // Test RegisterSymbolAction
            var text1 = @"
record struct A(int X = 0)
{}

record struct C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_02_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
        }

        private class AnalyzerActions_02_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle, SymbolKind.Method);
                context.RegisterSymbolAction(Handle, SymbolKind.Property);
                context.RegisterSymbolAction(Handle, SymbolKind.Parameter);
                context.RegisterSymbolAction(Handle, SymbolKind.NamedType);
            }

            private void Handle(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "System.Int32 A.X { get; set; }":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount6);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount7);
                        break;
                    case "System.Runtime.CompilerServices.IsExternalInit":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_03()
        {
            // Test RegisterSymbolStartAction
            var text1 = @"
readonly record struct A(int X = 0)
{}

readonly record struct C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_03_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(0, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(0, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
        }

        private class AnalyzerActions_03_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;
            public int FireCount10;
            public int FireCount11;
            public int FireCount12;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Property);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.Parameter);
                context.RegisterSymbolStartAction(Handle1, SymbolKind.NamedType);
            }

            private void Handle1(SymbolStartAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        context.RegisterSymbolEndAction(Handle2);
                        break;
                    case "System.Int32 A.X { get; init; }":
                        Interlocked.Increment(ref FireCount2);
                        context.RegisterSymbolEndAction(Handle3);
                        break;
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount4);
                        context.RegisterSymbolEndAction(Handle4);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount9);

                        Assert.Equal(0, FireCount1);
                        Assert.Equal(0, FireCount2);
                        Assert.Equal(0, FireCount6);
                        Assert.Equal(0, FireCount7);

                        context.RegisterSymbolEndAction(Handle5);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount10);

                        Assert.Equal(0, FireCount4);
                        Assert.Equal(0, FireCount8);

                        context.RegisterSymbolEndAction(Handle6);
                        break;
                    case "System.Runtime.CompilerServices.IsExternalInit":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SymbolAnalysisContext context)
            {
                Assert.Equal("A..ctor([System.Int32 X = 0])", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount6);
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                Assert.Equal("System.Int32 A.X { get; init; }", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount7);
            }

            private void Handle4(SymbolAnalysisContext context)
            {
                Assert.Equal("C..ctor([System.Int32 Z = 4])", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount8);
            }

            private void Handle5(SymbolAnalysisContext context)
            {
                Assert.Equal("A", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount11);

                Assert.Equal(1, FireCount1);
                Assert.Equal(1, FireCount2);
                Assert.Equal(1, FireCount6);
                Assert.Equal(1, FireCount7);
            }

            private void Handle6(SymbolAnalysisContext context)
            {
                Assert.Equal("C", context.Symbol.ToTestDisplayString());
                Interlocked.Increment(ref FireCount12);

                Assert.Equal(1, FireCount4);
                Assert.Equal(1, FireCount8);
            }
        }

        [Fact]
        public void AnalyzerActions_04()
        {
            // Test RegisterOperationAction
            var text1 = @"
record struct A([Attr1(100)]int X = 0) : I1
{}

interface I1 {}
";

            var analyzer = new AnalyzerActions_04_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount14);
        }

        private class AnalyzerActions_04_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount6;
            public int FireCount7;
            public int FireCount14;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationAction(HandleConstructorBody, OperationKind.ConstructorBody);
                context.RegisterOperationAction(HandleInvocation, OperationKind.Invocation);
                context.RegisterOperationAction(HandleLiteral, OperationKind.Literal);
                context.RegisterOperationAction(HandleParameterInitializer, OperationKind.ParameterInitializer);
                context.RegisterOperationAction(Fail, OperationKind.PropertyInitializer);
                context.RegisterOperationAction(Fail, OperationKind.FieldInitializer);
            }

            protected void HandleConstructorBody(OperationAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal(SyntaxKind.RecordDeclaration, context.Operation.Syntax.Kind());
                        VerifyOperationTree((CSharpCompilation)context.Compilation, context.Operation, @"");

                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void HandleInvocation(OperationAnalysisContext context)
            {
                Assert.True(false);
            }

            protected void HandleLiteral(OperationAnalysisContext context)
            {
                switch (context.Operation.Syntax.ToString())
                {
                    case "100":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount6);
                        break;
                    case "0":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount7);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void HandleParameterInitializer(OperationAnalysisContext context)
            {
                switch (context.Operation.Syntax.ToString())
                {
                    case "= 0":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount14);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Fail(OperationAnalysisContext context)
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void AnalyzerActions_05()
        {
            // Test RegisterOperationBlockAction
            var text1 = @"
record struct A([Attr1(100)]int X = 0) : I1
{}

interface I1 {}
";

            var analyzer = new AnalyzerActions_05_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
        }

        private class AnalyzerActions_05_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockAction(Handle);
            }

            private void Handle(OperationBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_07()
        {
            // Test RegisterCodeBlockAction
            var text1 = @"
record struct A([Attr1(100)]int X = 0) : I1
{
    int M() => 3;
}

interface I1 {}
";
            var analyzer = new AnalyzerActions_07_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_07_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount4;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockAction(Handle);
            }

            private void Handle(CodeBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":

                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 A.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount4);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_08()
        {
            // Test RegisterCodeBlockStartAction
            var text1 = @"
record struct A([Attr1]int X = 0) : I1
{
    private int M() => 3;
    A(string S) : this(4) => throw null;
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_08_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount400);
            Assert.Equal(1, analyzer.FireCount500);

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(0, analyzer.FireCountRecordStructDeclarationA);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(0, analyzer.FireCountSimpleBaseTypeI1onA);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(0, analyzer.FireCountParameterListAPrimaryCtor);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(0, analyzer.FireCountConstructorDeclaration);
            Assert.Equal(0, analyzer.FireCountStringParameterList);
            Assert.Equal(1, analyzer.FireCountThisConstructorInitializer);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount1000);
            Assert.Equal(1, analyzer.FireCount4000);
            Assert.Equal(1, analyzer.FireCount5000);
        }

        private class AnalyzerActions_08_Analyzer : AnalyzerActions_01_Analyzer
        {
            public int FireCount100;
            public int FireCount400;
            public int FireCount500;
            public int FireCount1000;
            public int FireCount4000;
            public int FireCount5000;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterCodeBlockStartAction<SyntaxKind>(Handle);
            }

            private void Handle(CodeBlockStartAnalysisContext<SyntaxKind> context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount100);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 A.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount400);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "A..ctor(System.String S)":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount500);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Fail, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.ThisConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Fail, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.RecordStructDeclaration);
                context.RegisterSyntaxNodeAction(Handle7, SyntaxKind.IdentifierName);
                context.RegisterSyntaxNodeAction(Handle8, SyntaxKind.SimpleBaseType);
                context.RegisterSyntaxNodeAction(Handle9, SyntaxKind.ParameterList);
                context.RegisterSyntaxNodeAction(Handle10, SyntaxKind.ArgumentList);

                context.RegisterCodeBlockEndAction(Handle11);
            }

            private void Handle11(CodeBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":

                        switch (context.CodeBlock)
                        {
                            case RecordDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 A.M()":
                        switch (context.CodeBlock)
                        {
                            case MethodDeclarationSyntax { Identifier: { ValueText: "M" } }:
                                Interlocked.Increment(ref FireCount4000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "A..ctor(System.String S)":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount5000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_09()
        {
            var text1 = @"
record A([Attr1(100)]int X = 0) : I1
{}

record B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

record C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_09_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
        }

        private class AnalyzerActions_09_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSymbolAction(Handle1, SymbolKind.Method);
                context.RegisterSymbolAction(Handle2, SymbolKind.Property);
                context.RegisterSymbolAction(Handle3, SymbolKind.Parameter);
            }

            private void Handle1(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle2(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "System.Int32 A.X { get; init; }":
                        Interlocked.Increment(ref FireCount5);
                        break;
                    case "System.Int32 B.Y { get; init; }":
                        Interlocked.Increment(ref FireCount6);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void Handle3(SymbolAnalysisContext context)
            {
                switch (context.Symbol.ToTestDisplayString())
                {
                    case "[System.Int32 X = 0]":
                        Interlocked.Increment(ref FireCount7);
                        break;
                    case "[System.Int32 Y = 1]":
                        Interlocked.Increment(ref FireCount8);
                        break;
                    case "[System.Int32 Z = 4]":
                        Interlocked.Increment(ref FireCount9);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void WithExprOnStruct_LangVersion()
        {
            var src = @"
var b = new B() { X = 1 };
var b2 = b.M();
System.Console.Write(b2.X);
System.Console.Write("" "");
System.Console.Write(b.X);

public struct B
{
    public int X { get; set; }
    public B M()
    /*<bind>*/{
        return this with { X = 42 };
    }/*</bind>*/
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (13,16): error CS8773: Feature 'with on structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         return this with { X = 42 };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "this with { X = 42 }").WithArguments("with on structs", "10.0").WithLocation(13, 16)
                );

            comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42 1");
            verifier.VerifyIL("B.M", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldobj      ""B""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldc.i4.s   42
  IL_000b:  call       ""void B.X.set""
  IL_0010:  ldloc.0
  IL_0011:  ret
}");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var with = tree.GetRoot().DescendantNodes().OfType<WithExpressionSyntax>().Single();
            var type = model.GetTypeInfo(with);
            Assert.Equal("B", type.Type.ToTestDisplayString());

            var operation = model.GetOperation(with);

            VerifyOperationTree(comp, operation, @"
IWithOperation (OperationKind.With, Type: B) (Syntax: 'this with { X = 42 }')
  Operand:
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
  CloneMethod: null
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: B) (Syntax: '{ X = 42 }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 42')
            Left:
              IPropertyReferenceOperation: System.Int32 B.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'X')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 42')
              Left:
                IPropertyReferenceOperation: System.Int32 B.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExprOnStruct_ControlFlow_DuplicateInitialization()
        {
            var src = @"
public struct B
{
    public int X { get; set; }

    public B M()
    /*<bind>*/{
        return this with { X = 42, X = 43 };
    }/*</bind>*/
}";
            var expectedDiagnostics = new[]
            {
                // (8,36): error CS1912: Duplicate initialization of member 'X'
                //         return this with { X = 42, X = 43 };
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "X").WithArguments("X").WithLocation(8, 36)
            };
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 42')
              Left:
                IPropertyReferenceOperation: System.Int32 B.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'X = 43')
              Left:
                IPropertyReferenceOperation: System.Int32 B.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'X')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 43) (Syntax: '43')
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExprOnStruct_ControlFlow_NestedInitializer()
        {
            var src = @"
public struct C
{
    public int Y { get; set; }
}
public struct B
{
    public C X { get; set; }

    public B M()
    /*<bind>*/{
        return this with { X = { Y = 1 } };
    }/*</bind>*/
}";

            var expectedDiagnostics = new[]
            {
                // (12,32): error CS1525: Invalid expression term '{'
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(12, 32),
                // (12,32): error CS1513: } expected
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(12, 32),
                // (12,32): error CS1002: ; expected
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(12, 32),
                // (12,34): error CS0103: The name 'Y' does not exist in the current context
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Y").WithArguments("Y").WithLocation(12, 34),
                // (12,34): warning CS0162: Unreachable code detected
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Y").WithLocation(12, 34),
                // (12,40): error CS1002: ; expected
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(12, 40),
                // (12,43): error CS1597: Semicolon after method or accessor block is not valid
                //         return this with { X = { Y = 1 } };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(12, 43),
                // (14,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(14, 1)
            };
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid) (Syntax: 'X = ')
              Left:
                IPropertyReferenceOperation: C B.X { get; set; } (OperationKind.PropertyReference, Type: C) (Syntax: 'X')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
              Right:
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
        Next (Return) Block[B3]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
            Leaving: {R1}
}
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'Y = 1 ')
          Expression:
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'Y = 1')
              Left:
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'Y')
                  Children(0)
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExprOnStruct_ControlFlow_NonAssignmentExpression()
        {
            var src = @"
public struct B
{
    public int X { get; set; }

    public B M(int i, int j)
    /*<bind>*/{
        return this with { i, j++, M2(), X = 2};
    }/*</bind>*/

    static int M2() => 0;
}";
            var expectedDiagnostics = new[]
            {
                // (8,28): error CS0747: Invalid initializer member declarator
                //         return this with { i, j++, M2(), X = 2};
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "i").WithLocation(8, 28),
                // (8,31): error CS0747: Invalid initializer member declarator
                //         return this with { i, j++, M2(), X = 2};
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "j++").WithLocation(8, 31),
                // (8,36): error CS0747: Invalid initializer member declarator
                //         return this with { i, j++, M2(), X = 2};
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "M2()").WithLocation(8, 36)
            };
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value:
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
              Children(1):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'j++')
              Children(1):
                  IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32, IsInvalid) (Syntax: 'j++')
                    Target:
                      IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2()')
              Children(1):
                  IInvocationOperation (System.Int32 B.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
                    Instance Receiver:
                      null
                    Arguments(0)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'X = 2')
              Left:
                IPropertyReferenceOperation: System.Int32 B.X { get; set; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'X')
                  Instance Receiver:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
              Right:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        Next (Return) Block[B2]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'this')
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void ObjectCreationInitializer_ControlFlow_WithCoalescingExpressionForValue()
        {
            var src = @"
public struct B
{
    public string X;

    public void M(string hello)
    /*<bind>*/{
        var x = new B() { X = Identity((string)null) ?? Identity(hello) };
    }/*</bind>*/

    T Identity<T>(T t) => t;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [B x]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new B() { X ... ty(hello) }')
              Value:
                IObjectCreationOperation (Constructor: B..ctor()) (OperationKind.ObjectCreation, Type: B) (Syntax: 'new B() { X ... ty(hello) }')
                  Arguments(0)
                  Initializer:
                    null
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity((string)null)')
                      Value:
                        IInvocationOperation ( System.String B.Identity<System.String>(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity((string)null)')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Identity')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '(string)null')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null) (Syntax: '(string)null')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand:
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'Identity((string)null)')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((string)null)')
                    Leaving: {R3}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity((string)null)')
                      Value:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((string)null)')
                Next (Regular) Block[B5]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(hello)')
                  Value:
                    IInvocationOperation ( System.String B.Identity<System.String>(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity(hello)')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Identity')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'hello')
                            IParameterReferenceOperation: hello (OperationKind.ParameterReference, Type: System.String) (Syntax: 'hello')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'X = Identit ... tity(hello)')
                  Left:
                    IFieldReferenceOperation: System.String B.X (OperationKind.FieldReference, Type: System.String) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B() { X ... ty(hello) }')
                  Right:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((s ... tity(hello)')
            Next (Regular) Block[B6]
                Leaving: {R2}
    }
    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: B, IsImplicit) (Syntax: 'x = new B() ... ty(hello) }')
              Left:
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: B, IsImplicit) (Syntax: 'x = new B() ... ty(hello) }')
              Right:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'new B() { X ... ty(hello) }')
        Next (Regular) Block[B7]
            Leaving: {R1}
}
Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void WithExprOnStruct_ControlFlow_WithCoalescingExpressionForValue()
        {
            var src = @"
var b = new B() { X = string.Empty };
var b2 = b.M(""hello"");
System.Console.Write(b2.X);

public struct B
{
    public string X;

    public B M(string hello)
    /*<bind>*/{
        return Identity(this) with { X = Identity((string)null) ?? Identity(hello) };
    }/*</bind>*/

    T Identity<T>(T t) => t;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "hello");
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(this)')
              Value:
                IInvocationOperation ( B B.Identity<B>(B t)) (OperationKind.Invocation, Type: B) (Syntax: 'Identity(this)')
                  Instance Receiver:
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Identity')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'this')
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B) (Syntax: 'this')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity((string)null)')
                      Value:
                        IInvocationOperation ( System.String B.Identity<System.String>(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity((string)null)')
                          Instance Receiver:
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Identity')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '(string)null')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null) (Syntax: '(string)null')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand:
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'Identity((string)null)')
                      Operand:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((string)null)')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity((string)null)')
                      Value:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((string)null)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(hello)')
                  Value:
                    IInvocationOperation ( System.String B.Identity<System.String>(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity(hello)')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: 'Identity')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'hello')
                            IParameterReferenceOperation: hello (OperationKind.ParameterReference, Type: System.String) (Syntax: 'hello')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'X = Identit ... tity(hello)')
                  Left:
                    IFieldReferenceOperation: System.String B.X (OperationKind.FieldReference, Type: System.String) (Syntax: 'X')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'Identity(this)')
                  Right:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity((s ... tity(hello)')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (0)
        Next (Return) Block[B7]
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: B, IsImplicit) (Syntax: 'Identity(this)')
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExprOnStruct_OnParameter()
        {
            var src = @"
var b = new B() { X = 1 };
var b2 = B.M(b);
System.Console.Write(b2.X);

public struct B
{
    public int X { get; set; }
    public static B M(B b)
    {
        return b with { X = 42 };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyIL("B.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  call       ""void B.X.set""
  IL_000b:  ldloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnThis()
        {
            var src = @"
record struct C
{
    public int X { get; set; }

    C(string ignored)
    {
        _ = this with { X = 42 }; // 1
        this = default;
    }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,13): error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         _ = this with { X = 42 }; // 1
                Diagnostic(ErrorCode.ERR_UseDefViolationThisUnsupportedVersion, "this").WithArguments("11.0").WithLocation(8, 13)
                );

            var verifier = CompileAndVerify(src, parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("C..ctor(string)", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int C.<X>k__BackingField""
  IL_0007:  ldarg.0
  IL_0008:  ldobj      ""C""
  IL_000d:  stloc.0
  IL_000e:  ldloca.s   V_0
  IL_0010:  ldc.i4.s   42
  IL_0012:  call       ""void C.X.set""
  IL_0017:  ldarg.0
  IL_0018:  initobj    ""C""
  IL_001e:  ret
}
");
        }

        [Fact]
        public void WithExprOnStruct_OnTStructParameter()
        {
            var src = @"
var b = new B() { X = 1 };
var b2 = B.M(b);
System.Console.Write(b2.X);

public interface I
{
    int X { get; set; }
}

public struct B : I
{
    public int X { get; set; }
    public static T M<T>(T b) where T : struct, I
    {
        return b with { X = 42 };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyIL("B.M<T>(T)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""void I.X.set""
  IL_0011:  ldloc.0
  IL_0012:  ret
}");

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var with = tree.GetRoot().DescendantNodes().OfType<WithExpressionSyntax>().Single();
            var type = model.GetTypeInfo(with);
            Assert.Equal("T", type.Type.ToTestDisplayString());
        }

        [Fact]
        public void WithExprOnStruct_OnRecordStructParameter()
        {
            var src = @"
var b = new B(1);
var b2 = B.M(b);
System.Console.Write(b2.X);

public record struct B(int X)
{
    public static B M(B b)
    {
        return b with { X = 42 };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42");
            verifier.VerifyIL("B.M", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  call       ""void B.X.set""
  IL_000b:  ldloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnRecordStructParameter_Readonly()
        {
            var src = @"
var b = new B(1, 2);
var b2 = B.M(b);
System.Console.Write(b2.X);
System.Console.Write(b2.Y);

public readonly record struct B(int X, int Y)
{
    public static B M(B b)
    {
        return b with { X = 42, Y = 43 };
    }
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "4243", verify: Verification.Skipped /* init-only */);
            verifier.VerifyIL("B.M", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (B V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  call       ""void B.X.init""
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldc.i4.s   43
  IL_000f:  call       ""void B.Y.init""
  IL_0014:  ldloc.0
  IL_0015:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnTuple()
        {
            var src = @"
class C
{
    static void Main()
    {
        var b = (1, 2);
        var b2 = M(b);
        System.Console.Write(b2.Item1);
        System.Console.Write(b2.Item2);
    }

    static (int, int) M((int, int) b)
    {
        return b with { Item1 = 42, Item2 = 43 };
    }
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "4243");
            verifier.VerifyIL("C.M", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldc.i4.s   43
  IL_000f:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0014:  ldloc.0
  IL_0015:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnTuple_WithNames()
        {
            var src = @"
var b = (1, 2);
var b2 = M(b);
System.Console.Write(b2.Item1);
System.Console.Write(b2.Item2);

static (int, int) M((int X, int Y) b)
{
    return b with { X = 42, Y = 43 };
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "4243");
            verifier.VerifyIL("Program.<<Main>$>g__M|0_0(System.ValueTuple<int, int>)", @"
{
  // Code size       22 (0x16)
  .maxstack  2
  .locals init (System.ValueTuple<int, int> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  stfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldc.i4.s   43
  IL_000f:  stfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0014:  ldloc.0
  IL_0015:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnTuple_LongTuple()
        {
            var src = @"
var b = (1, 2, 3, 4, 5, 6, 7, 8);
var b2 = M(b);
System.Console.Write(b2.Item7);
System.Console.Write(b2.Item8);

static (int, int, int, int, int, int, int, int) M((int, int, int, int, int, int, int, int) b)
{
    return b with { Item7 = 42, Item8 = 43 };
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "4243");
            verifier.VerifyIL("Program.<<Main>$>g__M|0_0(System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>)", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>> V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  ldc.i4.s   42
  IL_0006:  stfld      ""int System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Item7""
  IL_000b:  ldloca.s   V_0
  IL_000d:  ldflda     ""System.ValueTuple<int> System.ValueTuple<int, int, int, int, int, int, int, System.ValueTuple<int>>.Rest""
  IL_0012:  ldc.i4.s   43
  IL_0014:  stfld      ""int System.ValueTuple<int>.Item1""
  IL_0019:  ldloc.0
  IL_001a:  ret
}");
        }

        [Fact]
        public void WithExprOnStruct_OnReadonlyField()
        {
            var src = @"
var b = new B { X = 1 }; // 1

public struct B
{
    public readonly int X;
    public B M()
    {
        return this with { X = 42 }; // 2
    }
    public static B M2(B b)
    {
        return b with { X = 42 }; // 3
    }
    public B(int i)
    {
        this = default;
        _ = this with { X = 42 }; // 4
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,17): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                // var b = new B { X = 1 }; // 1
                Diagnostic(ErrorCode.ERR_AssgReadonly, "X").WithLocation(2, 17),
                // (9,28): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         return this with { X = 42 }; // 2
                Diagnostic(ErrorCode.ERR_AssgReadonly, "X").WithLocation(9, 28),
                // (13,25): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         return b with { X = 42 }; // 3
                Diagnostic(ErrorCode.ERR_AssgReadonly, "X").WithLocation(13, 25),
                // (18,25): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //         _ = this with { X = 42 }; // 4
                Diagnostic(ErrorCode.ERR_AssgReadonly, "X").WithLocation(18, 25)
                );
        }

        [Fact]
        public void WithExprOnStruct_OnEnum()
        {
            var src = @"
public enum E { }
class C
{
    static E M(E e)
    {
        return e with { };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void WithExprOnStruct_OnPointer()
        {
            var src = @"
unsafe class C
{
    static int* M(int* i)
    {
        return i with { };
    }
}";
            var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (6,16): error CS8858: The receiver type 'int*' is not a valid record type and is not a struct type.
                //         return i with { };
                Diagnostic(ErrorCode.ERR_CannotClone, "i").WithArguments("int*").WithLocation(6, 16)
                );
        }

        [Fact]
        public void WithExprOnStruct_OnInterface()
        {
            var src = @"
public interface I
{
    int X { get; set; }
}
class C
{
    static I M(I i)
    {
        return i with { X = 42 };
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (10,16): error CS8858: The receiver type 'I' is not a valid record type and is not a value type.
                //         return i with { X = 42 };
                Diagnostic(ErrorCode.ERR_CannotClone, "i").WithArguments("I").WithLocation(10, 16)
                );
        }

        [Fact]
        public void WithExprOnStruct_OnRefStruct()
        {
            // Similar to test RefLikeObjInitializers but with `with` expressions
            var text = @"
using System;

class Program
{
    static S2 Test1()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        // error
        return new S2() with { Field1 = outer, Field2 = inner };
    }

    static S2 Test2()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        S2 result;

        // error
        result = new S2() with { Field1 = inner, Field2 = outer };

        return result;
    }

    static S2 Test3()
    {
        S1 outer = default;
        S1 inner = stackalloc int[1];

        return new S2() with { Field1 = outer, Field2 = outer };
    }

    public ref struct S1
    {
        public static implicit operator S1(Span<int> o) => default;
    }

    public ref struct S2
    {
        public S1 Field1;
        public S1 Field2;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (12,48): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         return new S2() with { Field1 = outer, Field2 = inner };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "Field2 = inner").WithArguments("inner").WithLocation(12, 48),
                // (23,34): error CS8352: Cannot use variable 'inner' in this context because it may expose referenced variables outside of their declaration scope
                //         result = new S2() with { Field1 = inner, Field2 = outer };
                Diagnostic(ErrorCode.ERR_EscapeVariable, "Field1 = inner").WithArguments("inner").WithLocation(23, 34)
                );
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void WithExprOnStruct_OnRefStruct_ReceiverMayWrap(LanguageVersion languageVersion)
        {
            // Similar to test LocalWithNoInitializerEscape but wrapping method is used as receiver for `with` expression
            var text = @"using System;

class Program
{
    static void Main()
    {
        S1 sp;
        Span<int> local = stackalloc int[1];
        sp = MayWrap(ref local) with { }; // 1, 2
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public ref int this[int i] => throw null;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSpan(new[] { text, UnscopedRefAttributeDefinition }, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (9,26): error CS8352: Cannot use variable 'local' in this context because it may expose referenced variables outside of their declaration scope
                //         sp = MayWrap(ref local) with { }; // 1, 2
                Diagnostic(ErrorCode.ERR_EscapeVariable, "local").WithArguments("local").WithLocation(9, 26),
                // (9,14): error CS8347: Cannot use a result of 'Program.MayWrap(ref Span<int>)' in this context because it may expose variables referenced by parameter 'arg' outside of their declaration scope
                //         sp = MayWrap(ref local) with { }; // 1, 2
                Diagnostic(ErrorCode.ERR_EscapeCall, "MayWrap(ref local)").WithArguments("Program.MayWrap(ref System.Span<int>)", "arg").WithLocation(9, 14));
        }

        [Fact]
        public void WithExprOnStruct_OnRefStruct_ReceiverMayWrap_02()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        Span<int> local = stackalloc int[1];
        S1 sp = MayWrap(ref local) with { };
    }

    static S1 MayWrap(ref Span<int> arg)
    {
        return default;
    }

    ref struct S1
    {
        public ref int this[int i] => throw null;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(text, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
        }

        [Fact]
        public void WithExpr_NullableAnalysis_01()
        {
            var src = @"
#nullable enable
record struct B(int X)
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
record struct B(string X)
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
record struct B(string? X)
{
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
                // (7,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(7, 21),
                // (9,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 2
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(9, 21),
                // (14,21): warning CS8602: Dereference of a possibly null reference.
                //         if (flag) { b.X.ToString(); } // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(14, 21));
        }

        [Fact, WorkItem(44763, "https://github.com/dotnet/roslyn/issues/44763")]
        public void WithExpr_NullableAnalysis_05()
        {
            var src = @"
#nullable enable
record struct B(string? X, string? Y)
{
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
                // (10,13): warning CS8602: Dereference of a possibly null reference.
                //             b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(10, 13),
                // (11,13): warning CS8602: Dereference of a possibly null reference.
                //             b.Y.ToString(); // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.Y").WithLocation(11, 13),
                // (15,9): warning CS8602: Dereference of a possibly null reference.
                //         b.X.ToString(); // shouldn't warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "b.X").WithLocation(15, 9));
        }

        [Fact]
        public void WithExpr_NullableAnalysis_06()
        {
            var src = @"
#nullable enable
struct B
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

        [Fact]
        public void WithExprAssignToRef1()
        {
            var src = @"
using System;
record struct C(int Y)
{
    private readonly int[] _a = new[] { 0 };
    public ref int X => ref _a[0];

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
1").VerifyDiagnostics();

            verifier.VerifyIL("C.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (C V_0, //c
                C V_1)
  IL_0000:  ldloca.s   V_1
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""C..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  call       ""ref int C.X.get""
  IL_000f:  ldc.i4.5
  IL_0010:  stind.i4
  IL_0011:  ldloc.1
  IL_0012:  stloc.0
  IL_0013:  ldloca.s   V_0
  IL_0015:  call       ""ref int C.X.get""
  IL_001a:  ldind.i4
  IL_001b:  call       ""void System.Console.WriteLine(int)""
  IL_0020:  ldloc.0
  IL_0021:  stloc.1
  IL_0022:  ldloca.s   V_1
  IL_0024:  call       ""ref int C.X.get""
  IL_0029:  ldc.i4.1
  IL_002a:  stind.i4
  IL_002b:  ldloc.1
  IL_002c:  stloc.0
  IL_002d:  ldloca.s   V_0
  IL_002f:  call       ""ref int C.X.get""
  IL_0034:  ldind.i4
  IL_0035:  call       ""void System.Console.WriteLine(int)""
  IL_003a:  ret
}");
        }

        [Fact]
        public void WithExpressionSameLHS()
        {
            var comp = CreateCompilation(@"
record struct C(int X)
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

        [Fact]
        public void WithExpr_AnonymousType_ChangeAllProperties()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = Identity(a) with { A = Identity(30), B = Identity(40) };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t)
    {
        System.Console.Write($""Identity({t}) "");
        return t;
    }
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (9,17): error CS8773: Feature 'with on anonymous types' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         var b = Identity(a) with { A = Identity(30), B = Identity(40) };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "Identity(a) with { A = Identity(30), B = Identity(40) }").WithArguments("with on anonymous types", "10.0").WithLocation(9, 17)
                );

            comp = CreateCompilation(src, parseOptions: TestOptions.Regular10);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "Identity({ A = 10, B = 20 }) Identity(30) Identity(40) { A = 30, B = 40 }");
            verifier.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  ldc.i4.s   20
  IL_0004:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0009:  call       ""<anonymous type: int A, int B> C.Identity<<anonymous type: int A, int B>>(<anonymous type: int A, int B>)""
  IL_000e:  pop
  IL_000f:  ldc.i4.s   30
  IL_0011:  call       ""int C.Identity<int>(int)""
  IL_0016:  ldc.i4.s   40
  IL_0018:  call       ""int C.Identity<int>(int)""
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0024:  call       ""void System.Console.Write(object)""
  IL_0029:  ret
}
");
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.Identity<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(30)')
                  Value:
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(40)')
                  Value:
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(40)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '40')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 40) (Syntax: '40')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ... ntity(40) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ... ntity(40) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a) ... ntity(40) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
            Next (Regular) Block[B3]
                Leaving: {R3}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeAllProperties_ReverseOrder()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = Identity(a) with { B = Identity(40), A = Identity(30) };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t)
    {
        System.Console.Write($""Identity({t}) "");
        return t;
    }
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "Identity({ A = 10, B = 20 }) Identity(40) Identity(30) { A = 30, B = 40 }");
            verifier.VerifyIL("C.M", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  ldc.i4.s   20
  IL_0004:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0009:  call       ""<anonymous type: int A, int B> C.Identity<<anonymous type: int A, int B>>(<anonymous type: int A, int B>)""
  IL_000e:  pop
  IL_000f:  ldc.i4.s   40
  IL_0011:  call       ""int C.Identity<int>(int)""
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.s   30
  IL_0019:  call       ""int C.Identity<int>(int)""
  IL_001e:  ldloc.0
  IL_001f:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0024:  call       ""void System.Console.Write(object)""
  IL_0029:  ret
}
");
            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.Identity<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(40)')
                  Value:
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(40)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '40')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 40) (Syntax: '40')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(30)')
                  Value:
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ... ntity(30) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ... ntity(30) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a) ... ntity(30) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
            Next (Regular) Block[B3]
                Leaving: {R3}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeNoProperty()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = M2(a) with { };
        System.Console.Write(b);
    }/*</bind>*/

    static T M2<T>(T t)
    {
        System.Console.Write(""M2 "");
        return t;
    }
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "M2 { A = 10, B = 20 }");
            verifier.VerifyIL("C.M", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (<>f__AnonymousType0<int, int> V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  ldc.i4.s   20
  IL_0004:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0009:  call       ""<anonymous type: int A, int B> C.M2<<anonymous type: int A, int B>>(<anonymous type: int A, int B>)""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""int <>f__AnonymousType0<int, int>.A.get""
  IL_0015:  ldloc.0
  IL_0016:  callvirt   ""int <>f__AnonymousType0<int, int>.B.get""
  IL_001b:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0020:  call       ""void System.Console.Write(object)""
  IL_0025:  ret
}
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [3] [4]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2(a)')
                      Value:
                        IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.M2<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'M2(a)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2(a) with { }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a)')
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'M2(a) with { }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a)')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = M2(a) with { }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = M2(a) with { }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'M2(a) with { }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a) with { }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a)')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'M2(a) with { }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a) with { }')
                            Right:
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'M2(a)')
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeOneProperty()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = a with { B = Identity(30) };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "{ A = 10, B = 30 }");
            verifier.VerifyIL("C.M", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  ldc.i4.s   20
  IL_0004:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0009:  ldc.i4.s   30
  IL_000b:  call       ""int C.Identity<int>(int)""
  IL_0010:  stloc.0
  IL_0011:  callvirt   ""int <>f__AnonymousType0<int, int>.A.get""
  IL_0016:  ldloc.0
  IL_0017:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_001c:  call       ""void System.Console.Write(object)""
  IL_0021:  ret
}
");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var withExpr = tree.GetRoot().DescendantNodes().OfType<WithExpressionSyntax>().Single();
            var operation = model.GetOperation(withExpr);

            VerifyOperationTree(comp, operation, @"
IWithOperation (OperationKind.With, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a with { B  ... ntity(30) }')
  Operand:
    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
  CloneMethod: null
  Initializer:
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: '{ B = Identity(30) }')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'B = Identity(30)')
            Left:
              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                Instance Receiver:
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'B')
            Right:
              IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                Instance Receiver:
                  null
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [3] [4]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value:
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(30)')
                      Value:
                        IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = a with  ... ntity(30) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = a with  ... ntity(30) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a with { B  ... ntity(30) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a with { B  ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a')
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeOneProperty_WithMethodCallForTarget()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = Identity(a) with { B = 30 };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "{ A = 10, B = 30 }");
            verifier.VerifyIL("C.M", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldc.i4.s   10
  IL_0002:  ldc.i4.s   20
  IL_0004:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_0009:  call       ""<anonymous type: int A, int B> C.Identity<<anonymous type: int A, int B>>(<anonymous type: int A, int B>)""
  IL_000e:  ldc.i4.s   30
  IL_0010:  stloc.0
  IL_0011:  callvirt   ""int <>f__AnonymousType0<int, int>.A.get""
  IL_0016:  ldloc.0
  IL_0017:  newobj     ""<>f__AnonymousType0<int, int>..ctor(int, int)""
  IL_001c:  call       ""void System.Console.Write(object)""
  IL_0021:  ret
}
");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [3] [4]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a)')
                      Value:
                        IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.Identity<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '30')
                      Value:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ...  { B = 30 }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = Identit ...  { B = 30 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a) ...  { B = 30 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                            Right:
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ...  { B = 30 }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeOneProperty_WithCoalescingExpressionForTarget()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, B = 20 };
        var b = (Identity(a) ?? Identity2(a)) with { B = 30 };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t) => t;
    static T Identity2<T>(T t) => t;
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "{ A = 10, B = 30 }");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 B> a] [<anonymous type: System.Int32 A, System.Int32 B> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'a = new { A ... 0, B = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'new { A = 10, B = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20) (Syntax: 'B = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'new { A = 10, B = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4} {R5}
    }
    .locals {R3}
    {
        CaptureIds: [4] [5]
        .locals {R4}
        {
            CaptureIds: [2]
            .locals {R5}
            {
                CaptureIds: [3]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a)')
                          Value:
                            IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.Identity<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity(a)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                                    ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Jump if True (Regular) to Block[B4]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'Identity(a)')
                          Operand:
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                        Leaving: {R5}
                    Next (Regular) Block[B3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a)')
                          Value:
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a)')
                    Next (Regular) Block[B5]
                        Leaving: {R5}
            }
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity2(a)')
                      Value:
                        IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 B> C.Identity2<<anonymous type: System.Int32 A, System.Int32 B>>(<anonymous type: System.Int32 A, System.Int32 B> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'Identity2(a)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'a')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (2)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '30')
                      Value:
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... dentity2(a)')
                Next (Regular) Block[B6]
                    Leaving: {R4}
        }
        Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = (Identi ...  { B = 30 }')
                      Left:
                        ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'b = (Identi ...  { B = 30 }')
                      Right:
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: '(Identity(a ...  { B = 30 }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                Left:
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                Right:
                                  IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... dentity2(a)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                Left:
                                  IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 B>.B { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                    Instance Receiver:
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: '(Identity(a ...  { B = 30 }')
                                Right:
                                  IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 B>, IsImplicit) (Syntax: 'Identity(a) ... dentity2(a)')
                Next (Regular) Block[B7]
                    Leaving: {R3}
        }
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
                  Expression:
                    IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitReference)
                              Operand:
                                ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 B>) (Syntax: 'b')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B8]
                Leaving: {R1}
    }
    Block[B8] - Exit
        Predecessors: [B7]
        Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ChangeOneProperty_WithCoalescingExpressionForValue()
        {
            var src = @"
C.M(""hello"", ""world"");

public class C
{
    public static void M(string hello, string world)
    /*<bind>*/{
        var x = new { A = hello, B = string.Empty };
        var y = x with { B =  Identity(null) ?? Identity2(world) };
        System.Console.Write(y);
    }/*</bind>*/

    static string Identity(string t) => t;
    static string Identity2(string t) => t;
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics();
            CompileAndVerify(comp, expectedOutput: "{ A = hello, B = world }");

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.String A, System.String B> x] [<anonymous type: System.String A, System.String B> y]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'hello')
                  Value:
                    IParameterReferenceOperation: hello (OperationKind.ParameterReference, Type: System.String) (Syntax: 'hello')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'string.Empty')
                  Value:
                    IFieldReferenceOperation: System.String System.String.Empty (Static) (OperationKind.FieldReference, Type: System.String) (Syntax: 'string.Empty')
                      Instance Receiver:
                        null
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x = new { A ... ing.Empty }')
                  Left:
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x = new { A ... ing.Empty }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.String A, System.String B>) (Syntax: 'new { A = h ... ing.Empty }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'A = hello')
                            Left:
                              IPropertyReferenceOperation: System.String <anonymous type: System.String A, System.String B>.A { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'new { A = h ... ing.Empty }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'hello')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'B = string.Empty')
                            Left:
                              IPropertyReferenceOperation: System.String <anonymous type: System.String A, System.String B>.B { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'B')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'new { A = h ... ing.Empty }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'string.Empty')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [3] [5]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value:
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: <anonymous type: System.String A, System.String B>) (Syntax: 'x')
                Next (Regular) Block[B3]
                    Entering: {R5}
            .locals {R5}
            {
                CaptureIds: [4]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(null)')
                          Value:
                            IInvocationOperation (System.String C.Identity(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity(null)')
                              Instance Receiver:
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'null')
                                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        (ImplicitReference)
                                      Operand:
                                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Jump if True (Regular) to Block[B5]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'Identity(null)')
                          Operand:
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity(null)')
                        Leaving: {R5}
                    Next (Regular) Block[B4]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(null)')
                          Value:
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'Identity(null)')
                    Next (Regular) Block[B6]
                        Leaving: {R5}
            }
            Block[B5] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity2(world)')
                      Value:
                        IInvocationOperation (System.String C.Identity2(System.String t)) (OperationKind.Invocation, Type: System.String) (Syntax: 'Identity2(world)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'world')
                                IParameterReferenceOperation: world (OperationKind.ParameterReference, Type: System.String) (Syntax: 'world')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B4] [B5]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                      Value:
                        IPropertyReferenceOperation: System.String <anonymous type: System.String A, System.String B>.A { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x')
                Next (Regular) Block[B7]
                    Leaving: {R4}
        }
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'y = x with  ... y2(world) }')
                  Left:
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'y = x with  ... y2(world) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.String A, System.String B>) (Syntax: 'x with { B  ... y2(world) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                            Left:
                              IPropertyReferenceOperation: System.String <anonymous type: System.String A, System.String B>.A { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                            Right:
                              IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                            Left:
                              IPropertyReferenceOperation: System.String <anonymous type: System.String A, System.String B>.B { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x with { B  ... y2(world) }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.String A, System.String B>, IsImplicit) (Syntax: 'x')
            Next (Regular) Block[B8]
                Leaving: {R3}
    }
    Block[B8] - Block
        Predecessors: [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(y);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(y)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'y')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: <anonymous type: System.String A, System.String B>) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ErrorMember()
        {
            var src = @"
public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10 };
        var b = a with { Error = Identity(20) };
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            var expectedDiagnostics = new[]
            {
                // (7,26): error CS0117: '<anonymous type: int A>' does not contain a definition for 'Error'
                //         var b = a with { Error = Identity(20) };
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Error").WithArguments("<anonymous type: int A>", "Error").WithLocation(7, 26)
            };
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> a] [<anonymous type: System.Int32 A> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        .locals {R4}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value:
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'a')
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(20)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '20')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a with { Er ... ntity(20) }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { Er ... ntity(20) }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ntity(20) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ntity(20) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>, IsInvalid) (Syntax: 'a with { Er ... ntity(20) }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { Er ... ntity(20) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { Er ... ntity(20) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { Er ... ntity(20) }')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')
            Next (Regular) Block[B4]
                Leaving: {R3} {R1}
    }
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_ToString()
        {
            var src = @"
public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10 };
        var b = a with { ToString = Identity(20) };
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            var expectedDiagnostics = new[]
            {
                // (7,26): error CS1913: Member 'ToString' cannot be initialized. It is not a field or property.
                //         var b = a with { ToString = Identity(20) };
                Diagnostic(ErrorCode.ERR_MemberCannotBeInitialized, "ToString").WithArguments("ToString").WithLocation(7, 26)
            };
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> a] [<anonymous type: System.Int32 A> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        .locals {R4}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value:
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'a')
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(20)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '20')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a with { To ... ntity(20) }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { To ... ntity(20) }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ntity(20) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ntity(20) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>, IsInvalid) (Syntax: 'a with { To ... ntity(20) }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { To ... ntity(20) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { To ... ntity(20) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { To ... ntity(20) }')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')
            Next (Regular) Block[B4]
                Leaving: {R3} {R1}
    }
}
Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_NestedInitializer()
        {
            var src = @"
C.M();

public class C
{
    public static void M()
    /*<bind>*/{
        var nested = new { A = 10 };
        var a = new { Nested = nested };
        var b = a with { Nested = { A = 20 } };
        System.Console.Write(b);
    }/*</bind>*/
}";
            var expectedDiagnostics = new[]
            {
                // (10,35): error CS1525: Invalid expression term '{'
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(10, 35),
                // (10,35): error CS1513: } expected
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "{").WithLocation(10, 35),
                // (10,35): error CS1002: ; expected
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(10, 35),
                // (10,37): error CS0103: The name 'A' does not exist in the current context
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(10, 37),
                // (10,44): error CS1002: ; expected
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(10, 44),
                // (10,47): error CS1597: Semicolon after method or accessor block is not valid
                //         var b = a with { Nested = { A = 20 } };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(10, 47),
                // (11,29): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //         System.Console.Write(b);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(11, 29),
                // (11,31): error CS8124: Tuple must contain at least two elements.
                //         System.Console.Write(b);
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(11, 31),
                // (11,32): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //         System.Console.Write(b);
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(11, 32),
                // (13,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(13, 1)
            };
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> nested] [<anonymous type: <anonymous type: System.Int32 A> Nested> a] [<anonymous type: <anonymous type: System.Int32 A> Nested> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'nested = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: nested (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'nested = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'nested')
                  Value:
                    ILocalReferenceOperation: nested (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'nested')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsImplicit) (Syntax: 'a = new { N ...  = nested }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsImplicit) (Syntax: 'a = new { N ...  = nested }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>) (Syntax: 'new { Nested = nested }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>) (Syntax: 'Nested = nested')
                            Left:
                              IPropertyReferenceOperation: <anonymous type: System.Int32 A> <anonymous type: <anonymous type: System.Int32 A> Nested>.Nested { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'Nested')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsImplicit) (Syntax: 'new { Nested = nested }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'nested')
            Next (Regular) Block[B3]
                Leaving: {R3}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [2]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (3)
                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>) (Syntax: 'a')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
                  Value:
                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                      Children(0)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsInvalid, IsImplicit) (Syntax: 'b = a with { Nested = ')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsInvalid, IsImplicit) (Syntax: 'b = a with { Nested = ')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsInvalid) (Syntax: 'a with { Nested = ')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { Nested = ')
                            Left:
                              IPropertyReferenceOperation: <anonymous type: System.Int32 A> <anonymous type: <anonymous type: System.Int32 A> Nested>.Nested { get; } (OperationKind.PropertyReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { Nested = ')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsInvalid, IsImplicit) (Syntax: 'a with { Nested = ')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: <anonymous type: System.Int32 A> Nested>, IsImplicit) (Syntax: 'a')
            Next (Regular) Block[B4]
                Leaving: {R4}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'A = 20 ')
              Expression:
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: 'A = 20')
                  Left:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'A')
                      Children(0)
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_NonAssignmentExpression()
        {
            var src = @"
public class C
{
    public static void M(int i, int j)
    /*<bind>*/{
        var a = new { A = 10 };
        var b = a with { i, j++, M2(), A = 20 };
    }/*</bind>*/

    static int M2() => 0;
}";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            var expectedDiagnostics = new[]
            {
                // (7,26): error CS0747: Invalid initializer member declarator
                //         var b = a with { i, j++, M2(), A = 20 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "i").WithLocation(7, 26),
                // (7,29): error CS0747: Invalid initializer member declarator
                //         var b = a with { i, j++, M2(), A = 20 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "j++").WithLocation(7, 29),
                // (7,34): error CS0747: Invalid initializer member declarator
                //         var b = a with { i, j++, M2(), A = 20 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "M2()").WithLocation(7, 34)
            };
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> a] [<anonymous type: System.Int32 A> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (6)
                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'a')
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i')
                  Children(1):
                      IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'j++')
                  Children(1):
                      IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32, IsInvalid) (Syntax: 'j++')
                        Target:
                          IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'M2()')
                  Children(1):
                      IInvocationOperation (System.Int32 C.M2()) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'M2()')
                        Instance Receiver:
                          null
                        Arguments(0)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ), A = 20 }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with  ... ), A = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>, IsInvalid) (Syntax: 'a with { i, ... ), A = 20 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { i, ... ), A = 20 }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { i, ... ), A = 20 }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { i, ... ), A = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')
            Next (Regular) Block[B3]
                Leaving: {R3} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_IndexerAccess()
        {
            var src = @"
public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10 };
        var b = a with { [0] = 20 };
    }/*</bind>*/
}";

            var expectedDiagnostics = new[]
            {
                // (7,26): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         var b = a with { [0] = 20 };
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "[0]").WithLocation(7, 26),
                // (7,26): error CS0747: Invalid initializer member declarator
                //         var b = a with { [0] = 20 };
                Diagnostic(ErrorCode.ERR_InvalidInitializerElementInitializer, "[0] = 20").WithLocation(7, 26)
            };
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> a] [<anonymous type: System.Int32 A> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [2]
        .locals {R4}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value:
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'a')

                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: '20')

                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a with { [0] = 20 }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { [0] = 20 }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')

                Next (Regular) Block[B3]
                    Leaving: {R4}
        }

        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with { [0] = 20 }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = a with { [0] = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>, IsInvalid) (Syntax: 'a with { [0] = 20 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { [0] = 20 }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'a with { [0] = 20 }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'a with { [0] = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B4]
                Leaving: {R3} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_CannotSet()
        {
            var src = @"
public class C
{
    public static void M()
    {
        var a = new { A = 10 };
        a.A = 20;

        var b = new { B = a };
        b.B.A = 30;
    }
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (7,9): error CS0200: Property or indexer '<anonymous type: int A>.A' cannot be assigned to -- it is read only
                //         a.A = 20;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "a.A").WithArguments("<anonymous type: int A>.A").WithLocation(7, 9),
                // (10,9): error CS0200: Property or indexer '<anonymous type: int A>.A' cannot be assigned to -- it is read only
                //         b.B.A = 30;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "b.B.A").WithArguments("<anonymous type: int A>.A").WithLocation(10, 9)
                );
        }

        [Fact]
        public void WithExpr_AnonymousType_DuplicateMemberInDeclaration()
        {
            var src = @"
public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10, A = 20 };
        var b = Identity(a) with { A = Identity(30) };
        System.Console.Write(b);
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";
            var expectedDiagnostics = new[]
            {
                // (6,31): error CS0833: An anonymous type cannot have multiple properties with the same name
                //         var a = new { A = 10, A = 20 };
                Diagnostic(ErrorCode.ERR_AnonymousTypeDuplicatePropertyName, "A = 20").WithLocation(6, 31)
            };

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A, System.Int32 $1> a] [<anonymous type: System.Int32 A, System.Int32 $1> b]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '20')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: '20')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'a = new { A ... 0, A = 20 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'a = new { A ... 0, A = 20 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsInvalid) (Syntax: 'new { A = 10, A = 20 }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 $1>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'new { A = 10, A = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 20, IsInvalid) (Syntax: 'A = 20')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 $1>.$1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsInvalid, IsImplicit) (Syntax: 'new { A = 10, A = 20 }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 20, IsInvalid, IsImplicit) (Syntax: '20')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3} {R4}
    }
    .locals {R3}
    {
        CaptureIds: [3] [4]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a)')
                      Value:
                        IInvocationOperation (<anonymous type: System.Int32 A, System.Int32 $1> C.Identity<<anonymous type: System.Int32 A, System.Int32 $1>>(<anonymous type: System.Int32 A, System.Int32 $1> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A, System.Int32 $1>) (Syntax: 'Identity(a)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                                ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>) (Syntax: 'a')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(30)')
                      Value:
                        IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                          Instance Receiver:
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                      Value:
                        IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 $1>.$1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'Identity(a)')
                Next (Regular) Block[B3]
                    Leaving: {R4}
        }
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'b = Identit ... ntity(30) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'b = Identit ... ntity(30) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A, System.Int32 $1>) (Syntax: 'Identity(a) ... ntity(30) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 $1>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'Identity(a)')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A, System.Int32 $1>.$1 { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'Identity(a) ... ntity(30) }')
                            Right:
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>, IsImplicit) (Syntax: 'Identity(a)')
            Next (Regular) Block[B4]
                Leaving: {R3}
    }
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(b);')
              Expression:
                IInvocationOperation (void System.Console.Write(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(b)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'b')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            ILocalReferenceOperation: b (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A, System.Int32 $1>) (Syntax: 'b')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact]
        public void WithExpr_AnonymousType_DuplicateInitialization()
        {
            var src = @"
public class C
{
    public static void M()
    /*<bind>*/{
        var a = new { A = 10 };
        var b = Identity(a) with { A = Identity(30), A = Identity(40) };
    }/*</bind>*/

    static T Identity<T>(T t) => t;
}";
            var expectedDiagnostics = new[]
            {
                // (7,54): error CS1912: Duplicate initialization of member 'A'
                //         var b = Identity(a) with { A = Identity(30), A = Identity(40) };
                Diagnostic(ErrorCode.ERR_MemberAlreadyInitialized, "A").WithArguments("A").WithLocation(7, 54)
            };

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(expectedDiagnostics);

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [<anonymous type: System.Int32 A> a] [<anonymous type: System.Int32 A> b]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '10')
                  Value:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Left:
                    ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'a = new { A = 10 }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>) (Syntax: 'new { A = 10 }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 10) (Syntax: 'A = 10')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'A')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'new { A = 10 }')
                            Right:
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 10, IsImplicit) (Syntax: '10')
            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (4)
                IInvocationOperation (<anonymous type: System.Int32 A> C.Identity<<anonymous type: System.Int32 A>>(<anonymous type: System.Int32 A> t)) (OperationKind.Invocation, Type: <anonymous type: System.Int32 A>) (Syntax: 'Identity(a)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: 'a')
                        ILocalReferenceOperation: a (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>) (Syntax: 'a')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Identity(30)')
                  Value:
                    IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(30)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '30')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IInvocationOperation (System.Int32 C.Identity<System.Int32>(System.Int32 t)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Identity(40)')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: t) (OperationKind.Argument, Type: null) (Syntax: '40')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 40) (Syntax: '40')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = Identit ... ntity(40) }')
                  Left:
                    ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'b = Identit ... ntity(40) }')
                  Right:
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: System.Int32 A>, IsInvalid) (Syntax: 'Identity(a) ... ntity(40) }')
                      Initializers(1):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Left:
                              IPropertyReferenceOperation: System.Int32 <anonymous type: System.Int32 A>.A { get; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                                Instance Receiver:
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: System.Int32 A>, IsInvalid, IsImplicit) (Syntax: 'Identity(a) ... ntity(40) }')
                            Right:
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: <anonymous type: System.Int32 A>, IsImplicit) (Syntax: 'Identity(a)')
            Next (Regular) Block[B3]
                Leaving: {R3} {R1}
    }
}
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(src, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.RegularPreview);
        }

        [Fact, WorkItem(53849, "https://github.com/dotnet/roslyn/issues/53849")]
        public void WithExpr_AnonymousType_ValueIsLoweredToo()
        {
            var src = @"
var x = new { Property = 42 };
var adjusted = x with { Property = x.Property + 2 };

System.Console.WriteLine(adjusted);
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ Property = 44 }");
            verifier.VerifyDiagnostics();
        }

        [Fact, WorkItem(53849, "https://github.com/dotnet/roslyn/issues/53849")]
        public void WithExpr_AnonymousType_ValueIsLoweredToo_NestedWith()
        {
            var src = @"
var x = new { Property = 42 };
var container = new { Item = x };
var adjusted = container with { Item = x with { Property = x.Property + 2 } };

System.Console.WriteLine(adjusted);
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview);
            var verifier = CompileAndVerify(comp, expectedOutput: "{ Item = { Property = 44 } }");
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void AttributesOnPrimaryConstructorParameters_01()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class A : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public readonly record struct Test(
    [field: A]
    [property: B]
    [param: C]
    [D]
    int P1)
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var prop1 = @class.GetMember<PropertySymbol>("P1");
                AssertEx.SetEqual(new[] { "B" }, getAttributeStrings(prop1));

                var field1 = @class.GetMember<FieldSymbol>("<P1>k__BackingField");
                AssertEx.SetEqual(new[] { "A" }, getAttributeStrings(field1));

                var param1 = @class.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.AsSingleton()?.Name == "P1").Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(new[] { source, IsExternalInitTypeDefinition }, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator,
                parseOptions: TestOptions.RegularPreview,
                // init-only is unverifiable
                verify: Verification.Skipped,
                options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes().Where(a => a.AttributeClass!.Name is "A" or "B" or "C" or "D"));
            }
        }

        [Fact]
        public void FieldAsPositionalMember()
        {
            var source = @"
var a = new A(42);
System.Console.Write(a.X);
System.Console.Write("" - "");
a.Deconstruct(out int x);
System.Console.Write(x);

record struct A(int X)
{
    public int X = X;
}
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyEmitDiagnostics(
                // (8,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct A(int X)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(8, 8),
                // (8,17): error CS8773: Feature 'positional fields in records' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct A(int X)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "int X").WithArguments("positional fields in records", "10.0").WithLocation(8, 17));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
            var verifier = CompileAndVerify(comp, expectedOutput: "42 - 42");
            verifier.VerifyIL("A.Deconstruct", @"
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int A.X""
  IL_0007:  stind.i4
  IL_0008:  ret
}
");
        }

        [Fact]
        public void FieldAsPositionalMember_Readonly()
        {
            var source = @"
readonly record struct A(int X)
{
    public int X = X; // 1
}
readonly record struct B(int X)
{
    public readonly int X = X;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,16): error CS8340: Instance fields of readonly structs must be readonly.
                //     public int X = X; // 1
                Diagnostic(ErrorCode.ERR_FieldsInRoStruct, "X").WithLocation(4, 16)
                );
        }

        [Fact]
        public void FieldAsPositionalMember_Fixed()
        {
            var src = @"
unsafe record struct C(int[] P)
{
    public fixed int P[2];
    public int[] X = P;
}";
            var comp = CreateCompilation(src, options: TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.RegularPreview);
            comp.VerifyEmitDiagnostics(
                // (2,30): error CS8866: Record member 'C.P' must be a readable instance property or field of type 'int[]' to match positional parameter 'P'.
                // unsafe record struct C(int[] P)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "P").WithArguments("C.P", "int[]", "P").WithLocation(2, 30),
                // (4,22): error CS8908: The type 'int*' may not be used for a field of a record.
                //     public fixed int P[2];
                Diagnostic(ErrorCode.ERR_BadFieldTypeInRecord, "P").WithArguments("int*").WithLocation(4, 22)
                );
        }

        [Fact]
        public void FieldAsPositionalMember_WrongType()
        {
            var source = @"
record struct A(int X)
{
    public string X = null;
    public int Y = X;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,21): error CS8866: Record member 'A.X' must be a readable instance property or field of type 'int' to match positional parameter 'X'.
                // record struct A(int X)
                Diagnostic(ErrorCode.ERR_BadRecordMemberForPositionalParameter, "X").WithArguments("A.X", "int", "X").WithLocation(2, 21)
                );
        }

        [Fact]
        public void FieldAsPositionalMember_DuplicateFields()
        {
            var source = @"
record struct A(int X)
{
    public int X = 0;
    public int X = 0;
    public int Y = X;
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,16): error CS0102: The type 'A' already contains a definition for 'X'
                //     public int X = 0;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "X").WithArguments("A", "X").WithLocation(5, 16)
                );
        }

        [Fact]
        public void SyntaxFactory_TypeDeclaration()
        {
            var expected = @"record struct Point
{
}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expected, SyntaxFactory.TypeDeclaration(SyntaxKind.RecordStructDeclaration, "Point").NormalizeWhitespace().ToString());
        }

        [Fact]
        public void InterfaceWithParameters()
        {
            var src = @"
public interface I
{
}

record struct R(int X) : I()
{
}

record struct R2(int X) : I(X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,27): error CS8861: Unexpected argument list.
                // record struct R(int X) : I()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(6, 27),
                // (10,28): error CS8861: Unexpected argument list.
                // record struct R2(int X) : I(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X)").WithLocation(10, 28)
                );
        }

        [Fact]
        public void InterfaceWithParameters_NoPrimaryConstructor()
        {
            var src = @"
public interface I
{
}

record struct R : I()
{
}

record struct R2 : I(0)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,20): error CS8861: Unexpected argument list.
                // record struct R : I()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(6, 20),
                // (10,21): error CS8861: Unexpected argument list.
                // record struct R2 : I(0)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(0)").WithLocation(10, 21)
                );
        }

        [Fact]
        public void InterfaceWithParameters_Struct()
        {
            var src = @"
public interface I
{
}

struct C : I()
{
}

struct C2 : I(0)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS8861: Unexpected argument list.
                // struct C : I()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(6, 13),
                // (10,14): error CS8861: Unexpected argument list.
                // struct C2 : I(0)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(0)").WithLocation(10, 14)
                );
        }

        [Fact]
        public void BaseArguments_Speculation()
        {
            var src = @"
record struct R1(int X) : Error1(0, 1)
{
}
record struct R2(int X) : Error2()
{
}
record struct R3(int X) : Error3
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,27): error CS0246: The type or namespace name 'Error1' could not be found (are you missing a using directive or an assembly reference?)
                // record struct R1(int X) : Error1(0, 1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error1").WithArguments("Error1").WithLocation(2, 27),
                // (2,33): error CS8861: Unexpected argument list.
                // record struct R1(int X) : Error1(0, 1)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(0, 1)").WithLocation(2, 33),
                // (5,27): error CS0246: The type or namespace name 'Error2' could not be found (are you missing a using directive or an assembly reference?)
                // record struct R2(int X) : Error2()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error2").WithArguments("Error2").WithLocation(5, 27),
                // (5,33): error CS8861: Unexpected argument list.
                // record struct R2(int X) : Error2()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(5, 33),
                // (8,27): error CS0246: The type or namespace name 'Error3' could not be found (are you missing a using directive or an assembly reference?)
                // record struct R3(int X) : Error3
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error3").WithArguments("Error3").WithLocation(8, 27)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var baseWithargs =
                tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().First();
            Assert.Equal("Error1(0, 1)", baseWithargs.ToString());

            var speculativeBase =
                baseWithargs.WithArgumentList(baseWithargs.ArgumentList.WithArguments(baseWithargs.ArgumentList.Arguments.RemoveAt(1)));
            Assert.Equal("Error1(0)", speculativeBase.ToString());

            Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBase, out _));

            var baseWithoutargs =
                tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Skip(1).First();
            Assert.Equal("Error2()", baseWithoutargs.ToString());

            Assert.False(model.TryGetSpeculativeSemanticModel(baseWithoutargs.ArgumentList.OpenParenToken.SpanStart, speculativeBase, out _));

            var baseWithoutParens = tree.GetRoot().DescendantNodes().OfType<SimpleBaseTypeSyntax>().Single();
            Assert.Equal("Error3", baseWithoutParens.ToString());

            Assert.False(model.TryGetSpeculativeSemanticModel(baseWithoutParens.SpanStart + 2, speculativeBase, out _));
        }

        [Fact, WorkItem(54413, "https://github.com/dotnet/roslyn/issues/54413")]
        public void ValueTypeCopyConstructorLike_NoThisInitializer()
        {
            var src = @"
record struct Value(string Text)
{
    private Value(int X) { } // 1
    private Value(Value original) { } // 2
}

record class Boxed(string Text)
{
    private Boxed(int X) { } // 3
    private Boxed(Boxed original) { } // 4
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     private Value(int X) { } // 1
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "Value").WithLocation(4, 13),
                // (5,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     private Value(Value original) { } // 2
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "Value").WithLocation(5, 13),
                // (10,13): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     private Boxed(int X) { } // 3
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "Boxed").WithLocation(10, 13),
                // (11,13): error CS8878: A copy constructor 'Boxed.Boxed(Boxed)' must be public or protected because the record is not sealed.
                //     private Boxed(Boxed original) { } // 4
                Diagnostic(ErrorCode.ERR_CopyConstructorWrongAccessibility, "Boxed").WithArguments("Boxed.Boxed(Boxed)").WithLocation(11, 13)
                );
        }

        [Fact]
        public void ValueTypeCopyConstructorLike()
        {
            var src = @"
System.Console.Write(new Value(new Value(0)));

record struct Value(int I)
{
    public Value(Value original) : this(42) { }
}
";
            var comp = CreateCompilation(src);
            CompileAndVerify(comp, expectedOutput: "Value { I = 42 }");
        }

        [Fact]
        public void ExplicitConstructors_01()
        {
            var source =
@"using static System.Console;
record struct S1
{
}
record struct S2
{
    public S2() { }
}
record struct S3
{
    public S3(object o) { }
}
class Program
{
    static void Main()
    {
        WriteLine(new S1());
        WriteLine(new S2());
        WriteLine(new S3());
        WriteLine(new S3(null));
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"S1 { }
S2 { }
S3 { }
S3 { }
");
            verifier.VerifyMissing("S1..ctor()");
            verifier.VerifyIL("S2..ctor()",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyMissing("S3..ctor()");
            verifier.VerifyIL("S3..ctor(object)",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }

        [Fact]
        public void ExplicitConstructors_02()
        {
            var source =
@"record struct S1
{
    public S1(object o) { }
}
record struct S2()
{
    public S2(object o) { }
}
record struct S3(char A)
{
    public S3(object o) { }
}
record struct S4(char A, char B)
{
    public S4(object o) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S2(object o) { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S2").WithLocation(7, 12),
                // (11,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S3(object o) { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S3").WithLocation(11, 12),
                // (15,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S4(object o) { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S4").WithLocation(15, 12));
        }

        [Fact]
        public void ExplicitConstructors_03()
        {
            var source =
@"using static System.Console;
record struct S1
{
    public S1(object o) : this() { }
}
record struct S2()
{
    public S2(object o) : this() { }
}
class Program
{
    static void Main()
    {
        WriteLine(new S1());
        WriteLine(new S2());
    }
}";
            CompileAndVerify(source, expectedOutput:
@"S1 { }
S2 { }
");
        }

        [Fact]
        public void ExplicitConstructors_04()
        {
            var source =
@"using static System.Console;
record struct S0
{
    internal object F = 0;
    public S0() { }
}
record struct S1
{
    internal object F = 1;
    public S1(object o) : this() { F = o; }
}
record struct S2()
{
    internal object F = 2;
    public S2(object o) : this() { F = o; }
}
class Program
{
    static void Main()
    {
        WriteLine(new S0().F);
        WriteLine(new S1().F);
        WriteLine(new S1(-1).F);
        WriteLine(new S2().F);
        WriteLine(new S2(-2).F);
    }
}";
            CompileAndVerify(source, expectedOutput:
@"0

-1
2
-2
");
        }

        [Fact]
        [WorkItem(58328, "https://github.com/dotnet/roslyn/issues/58328")]
        public void ExplicitConstructors_05()
        {
            var source =
@"record struct S3(char A)
{
    public S3(object o) : this() { }
}
record struct S4(char A, char B)
{
    public S4(object o) : this() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,27): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S3(object o) : this() { }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(3, 27),
                // (7,27): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S4(object o) : this() { }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(7, 27));
        }

        [Fact]
        [WorkItem(58328, "https://github.com/dotnet/roslyn/issues/58328")]
        public void ExplicitConstructors_06()
        {
            var source =
@"record struct S3(char A)
{
    internal object F = 3;
    public S3(object o) : this() { F = o; }
}
record struct S4(char A, char B)
{
    internal object F = 4;
    public S4(object o) : this() { F = o; }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,27): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S3(object o) : this() { F = o; }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(4, 27),
                // (9,27): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S4(object o) : this() { F = o; }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(9, 27));
        }

        [Fact]
        public void ExplicitConstructors_07()
        {
            var source =
@"using static System.Console;
record struct S1
{
    public S1(object o) : this() { }
    public S1() { }
}
record struct S3(char A)
{
    public S3(object o) : this() { }
    public S3() : this('a') { }
}
record struct S4(char A, char B)
{
    public S4(object o) : this() { }
    public S4() : this('a', 'b') { }
}
class Program
{
    static void Main()
    {
        WriteLine(new S1());
        WriteLine(new S1(1));
        WriteLine(new S3());
        WriteLine(new S3(3));
        WriteLine(new S4());
        WriteLine(new S4(4));
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput:
@"S1 { }
S1 { }
S3 { A = a }
S3 { A = a }
S4 { A = a, B = b }
S4 { A = a, B = b }
");
            verifier.VerifyIL("S1..ctor()",
@"{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
            verifier.VerifyIL("S1..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S1..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S3..ctor()",
@"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   97
  IL_0003:  call       ""S3..ctor(char)""
  IL_0008:  ret
}");
            verifier.VerifyIL("S3..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S3..ctor()""
  IL_0006:  ret
}");
            verifier.VerifyIL("S4..ctor()",
@"{
  // Code size       11 (0xb)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   97
  IL_0003:  ldc.i4.s   98
  IL_0005:  call       ""S4..ctor(char, char)""
  IL_000a:  ret
}");
            verifier.VerifyIL("S4..ctor(object)",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""S4..ctor()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void ExplicitConstructors_08()
        {
            var source =
@"record struct S2()
{
    public S2(object o) : this() { }
    public S2() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,27): error CS2121: The call is ambiguous between the following methods or properties: 'S2.S2()' and 'S2.S2()'
                //     public S2(object o) : this() { }
                Diagnostic(ErrorCode.ERR_AmbigCall, "this").WithArguments("S2.S2()", "S2.S2()").WithLocation(3, 27),
                // (4,12): error CS2111: Type 'S2' already defines a member called 'S2' with the same parameter types
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "S2").WithArguments("S2", "S2").WithLocation(4, 12),
                // (4,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S2() { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "S2").WithLocation(4, 12));
        }

        [Fact]
        public void ExplicitConstructors_09()
        {
            var source =
@"record struct S1
{
    public S1(object o) : base() { }
}
record struct S2()
{
    public S2(object o) : base() { }
}
record struct S3(char A)
{
    public S3(object o) : base() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (3,12): error CS0522: 'S1': structs cannot call base class constructors
                //     public S1(object o) : base() { }
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S1").WithArguments("S1").WithLocation(3, 12),
                // (7,12): error CS0522: 'S2': structs cannot call base class constructors
                //     public S2(object o) : base() { }
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S2").WithArguments("S2").WithLocation(7, 12),
                // (7,27): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S2(object o) : base() { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "base").WithLocation(7, 27),
                // (11,12): error CS0522: 'S3': structs cannot call base class constructors
                //     public S3(object o) : base() { }
                Diagnostic(ErrorCode.ERR_StructWithBaseConstructorCall, "S3").WithArguments("S3").WithLocation(11, 12),
                // (11,27): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     public S3(object o) : base() { }
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "base").WithLocation(11, 27));
        }

        [Fact]
        [WorkItem(58328, "https://github.com/dotnet/roslyn/issues/58328")]
        public void ExplicitConstructors_10()
        {
            var source =
@"record struct S(object F)
{
    public object F;
    public S(int i) : this() { F = i; }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,15): error CS0171: Field 'S.F' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                // record struct S(object F)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.F", "11.0").WithLocation(1, 15),
                // (1,24): warning CS8907: Parameter 'F' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S(object F)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "F").WithArguments("F").WithLocation(1, 24),
                // (4,23): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S(int i) : this() { F = i; }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(4, 23));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (1,24): warning CS8907: Parameter 'F' is unread. Did you forget to use it to initialize the property with that name?
                // record struct S(object F)
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "F").WithArguments("F").WithLocation(1, 24),
                // (4,23): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S(int i) : this() { F = i; }
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(4, 23));
        }

        [Fact]
        public void ExplicitConstructors_11()
        {
            var source =
@"record struct S(int X)
{
    static internal int F = 1;
    static S() : this() { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,18): error CS0514: 'S': static constructor cannot have an explicit 'this' or 'base' constructor call
                //     static S() : this() { }
                Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("S").WithLocation(4, 18));
        }

        [Fact]
        public void StructNamedRecord()
        {
            var source = "struct record { } ";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // struct record { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(1, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 8)
                );
        }

        [Fact]
        public void ClassNamedRecord()
        {
            var source = "class record { } ";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class record { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(1, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 7)
                );
        }

        [Fact]
        public void StructNamedRecord_WithTypeParameters()
        {
            var source = "struct record<T> { } ";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // struct record { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(1, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record<T> { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 8)
                );
        }

        [Fact]
        public void ClassNamedRecord_WithTypeParameters()
        {
            var source = "class record<T> { } ";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class record<T> { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(1, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record<T> { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (1,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record<T> { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(1, 7)
                );
        }

        [Fact]
        public void StructNamedRecord_WithBaseList()
        {
            var source = @"
interface I { }
struct record : I { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // struct record : I { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(3, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 8)
                );
        }

        [Fact]
        public void StructNamedRecord_WithBaseList_Generic()
        {
            var source = @"
interface I { }
struct record<T> : I { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // struct record<T> : I { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(3, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record<T> : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 8)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,8): warning CS8860: Types and aliases should not be named 'record'.
                // struct record<T> : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 8)
                );
        }

        [Fact]
        public void ClassNamedRecord_WithBaseList_Generic()
        {
            var source = @"
interface I { }
class record<T> : I { }
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics(
                // (3,7): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class record<T> : I { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "record").WithArguments("record").WithLocation(3, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (3,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record<T> : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 7)
                );

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,7): warning CS8860: Types and aliases should not be named 'record'.
                // class record<T> : I { }
                Diagnostic(ErrorCode.WRN_RecordNamedDisallowed, "record").WithLocation(3, 7)
                );
        }

        [Fact]
        [WorkItem(64238, "https://github.com/dotnet/roslyn/issues/64238")]
        public void NoMethodBodiesInComImportType()
        {
            var source1 =
@"
[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid(""00112233-4455-6677-8899-aabbccddeeff"")]
record struct R1(int x);
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.Net60);

            compilation1.VerifyDiagnostics(
                // (2,2): error CS0592: Attribute 'System.Runtime.InteropServices.ComImport' is not valid on this declaration type. It is only valid on 'class, interface' declarations.
                // [System.Runtime.InteropServices.ComImport]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.InteropServices.ComImport").WithArguments("System.Runtime.InteropServices.ComImport", "class, interface").WithLocation(2, 2)
                );
        }
    }
}

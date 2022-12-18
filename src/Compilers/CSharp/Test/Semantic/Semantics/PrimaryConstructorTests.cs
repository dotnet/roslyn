// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class PrimaryConstructorTests : CompilingTestBase
    {
        [Theory]
        [CombinatorialData]
        public void LanguageVersion_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
()
{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("class", "class")]
        [InlineData("struct", "interface")]
        public void LanguageVersion_02(string typeKeyword, string baseKeyword)
        {
            var src1 = @"
" + typeKeyword + @" Point :
Base()
{}

" + baseKeyword + @" Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,5): error CS8861: Unexpected argument list.
                // Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 5)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,5): error CS8861: Unexpected argument list.
                // Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 5)
                );

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,5): error CS8861: Unexpected argument list.
                // Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 5)
                );
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersion_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
;";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ";").WithArguments("primary constructors").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LanguageVersion_04_Class()
        {
            var src1 = @"
class Point
()
: Base()
;

class Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LanguageVersion_04_Struct()
        {
            var src1 = @"
struct Point
()
: Base()
;

interface Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1),
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );
        }

        [Fact]
        public void LanguageVersion_05_Class()
        {
            var src1 = @"
class Point
()
: Base()
{}

class Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LanguageVersion_05_Struct()
        {
            var src1 = @"
struct Point
()
: Base()
{}

interface Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1),
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );
        }

        [Theory]
        [InlineData("class", "class")]
        [InlineData("struct", "interface")]
        public void LanguageVersion_06(string typeKeyword, string baseKeyword)
        {
            var src1 = @"
" + typeKeyword + @" Point
: Base()
;

" + baseKeyword + @" Base{}
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 7),
                // (4,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ";").WithArguments("primary constructors").WithLocation(4, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 7)
                );

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(3, 7)
                );
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersion_07([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
()
;
";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS8652: The feature 'primary constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "()").WithArguments("primary constructors").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.RegularNext, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorSymbol_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(keyword + @" C(int x, string y);");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            Assert.Equal(keyword != "struct" ? 1 : 2, c.InstanceConstructors.Length);

            var ctor = c.InstanceConstructors[0];

            Assert.Equal(Accessibility.Public, ctor.DeclaredAccessibility);
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var y = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, y.Type.SpecialType);
            Assert.Equal("y", y.Name);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var parameters = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().ToArray();

            Assert.Equal("int x", parameters[0].ToString());
            Assert.Same(x, model.GetDeclaredSymbol(parameters[0]).GetSymbol());

            Assert.Equal("string y", parameters[1].ToString());
            Assert.Same(y, model.GetDeclaredSymbol(parameters[1]).GetSymbol());

            // PROTOTYPE(PrimaryConstructors): Adjust wording for WRN_UnreadRecordParameter?
            var verifier = CompileAndVerify(comp).VerifyDiagnostics(
                // (1,14): warning CS8907: Parameter 'x' is unread. Did you forget to use it to initialize the property with that name?
                // struct C(int x, string y);
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "x").WithArguments("x").WithLocation(1, 14),
                // (1,24): warning CS8907: Parameter 'y' is unread. Did you forget to use it to initialize the property with that name?
                // struct C(int x, string y);
                Diagnostic(ErrorCode.WRN_UnreadRecordParameter, "y").WithArguments("y").WithLocation(1, 24)
                );

            if (c.TypeKind == TypeKind.Struct)
            {
                verifier.VerifyIL("C..ctor(int, string)",
@"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}

");
            }
            else
            {
                verifier.VerifyIL("C..ctor(int, string)",
@"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}

");
            }
        }

        [Fact]
        public void ConstructorSymbol_01_InAbstractClass()
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
abstract class C(int x, string y);
");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            comp.VerifyEmitDiagnostics();

            var ctor = c.InstanceConstructors.Single();

            Assert.Equal(Accessibility.Protected, ctor.DeclaredAccessibility);
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var y = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, y.Type.SpecialType);
            Assert.Equal("y", y.Name);
        }

        [Fact]
        public void StructDefaultCtor()
        {
            const string src = @"
#pragma warning disable CS8907 // Parameter is unread.

public struct S(int X);
";

            const string src2 = @"
class C
{
    public S M() => new S();
}";
            var comp = CreateCompilation(src + src2);
            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(src);
            var comp2 = CreateCompilation(src2, references: new[] { comp.EmitToImageReference() });
            comp2.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorSymbol_02([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
" + keyword + @" C(C x);");

            comp.VerifyEmitDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");
            Assert.Equal(c.TypeKind != TypeKind.Struct ? 1 : 2, c.InstanceConstructors.Length);

            var ctor = c.InstanceConstructors[0];
            Assert.Equal(1, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal("C", x.Type.ToTestDisplayString());
            Assert.Equal("x", x.Name);

            if (c.TypeKind == TypeKind.Struct)
            {
                Assert.True(c.InstanceConstructors[1].IsDefaultValueTypeConstructor());
            }
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorSymbol_03([CombinatorialValues("class", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
" + keyword + @"
C()
{
    static C() {}
}
");

            comp.VerifyEmitDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");

            var ctor = c.InstanceConstructors.Single();
            Assert.IsType<SynthesizedPrimaryConstructor>(ctor);
            Assert.False(ctor.IsDefaultValueTypeConstructor());

            Assert.Equal(1, c.StaticConstructors.Length);
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorSymbol_04([CombinatorialValues("class", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.

static " + keyword + @" C(int x, string y);
");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            if (keyword == "struct")
            {
                comp.VerifyDiagnostics(
                    // (4,15): error CS0106: The modifier 'static' is not valid for this item
                    // static struct C(int x, string y);
                    Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("static").WithLocation(4, 15)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (4,14): error CS0710: Static classes cannot have instance constructors
                    // static class C(int x, string y);
                    Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "C").WithLocation(4, 14)
                    );
            }

            Assert.Equal(keyword != "struct" ? 1 : 2, c.InstanceConstructors.Length);

            var ctor = c.InstanceConstructors[0];
            Assert.Equal(2, ctor.ParameterCount);

            var x = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, x.Type.SpecialType);
            Assert.Equal("x", x.Name);

            var y = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, y.Type.SpecialType);
            Assert.Equal("y", y.Name);
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorConflict([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C(int a, string b)
    {
    }
}");
            // PROTOTYPE(PrimaryConstructors): Adjust wording for ERR_UnexpectedOrMissingConstructorInitializerInRecord?
            comp.VerifyDiagnostics(
                // (5,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, string b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(5, 12),
                // (5,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public C(int a, string b)
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctor = c.InstanceConstructors.Where(m => m.ParameterCount == 2).Last();

            var a = ctor.Parameters[0];
            Assert.Equal(SpecialType.System_Int32, a.Type.SpecialType);
            Assert.Equal("a", a.Name);

            var b = ctor.Parameters[1];
            Assert.Equal(SpecialType.System_String, b.Type.SpecialType);
            Assert.Equal("b", b.Name);
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorOverloading_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C(int a, int b) // overload
    {
    }
}");
            // PROTOTYPE(PrimaryConstructors): Adjust wording for ERR_UnexpectedOrMissingConstructorInitializerInRecord?
            comp.VerifyDiagnostics(
                // (5,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public C(int a, int b) // overload
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctors = c.InstanceConstructors;
            Assert.Equal(keyword == "class " ? 2 : 3, ctors.Length);

            foreach (MethodSymbol ctor in ctors)
            {
                if (ctor.ParameterCount == 2)
                {
                    var p1 = ctor.Parameters[0];
                    Assert.Equal(SpecialType.System_Int32, p1.Type.SpecialType);
                    var p2 = ctor.Parameters[1];
                    if (ctor is SynthesizedPrimaryConstructor)
                    {
                        Assert.Equal("x", p1.Name);
                        Assert.Equal("y", p2.Name);
                        Assert.Equal(SpecialType.System_String, p2.Type.SpecialType);
                    }
                    else
                    {
                        Assert.Equal("a", p1.Name);
                        Assert.Equal("b", p2.Name);
                        Assert.Equal(SpecialType.System_Int32, p2.Type.SpecialType);
                    }
                }
                else if (ctor.ParameterCount == 0)
                {
                    Assert.True(ctor.IsDefaultValueTypeConstructor());
                }
                else
                {
                    Assert.NotEqual("class ", keyword);
                    Assert.Equal(1, ctor.ParameterCount);
                    Assert.True(c.Equals(ctor.Parameters[0].Type, TypeCompareKind.ConsiderEverything));
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void ConstructorOverloading_02([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C() // overload
    {
    }
}");

            comp.VerifyDiagnostics(
                // (5,12): error CS8862: A constructor declared in a record with parameter list must have 'this' constructor initializer.
                //     public C() // overload
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 12)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");
            var ctors = c.InstanceConstructors;
            Assert.Equal(2, ctors.Length);

            foreach (MethodSymbol ctor in ctors)
            {
                if (ctor.ParameterCount == 2)
                {
                    var p1 = ctor.Parameters[0];
                    Assert.Equal(SpecialType.System_Int32, p1.Type.SpecialType);
                    var p2 = ctor.Parameters[1];
                    Assert.IsType<SynthesizedPrimaryConstructor>(ctor);
                    Assert.Equal("x", p1.Name);
                    Assert.Equal("y", p2.Name);
                    Assert.Equal(SpecialType.System_String, p2.Type.SpecialType);
                }
                else
                {
                    Assert.False(ctor.IsDefaultValueTypeConstructor());
                    Assert.Equal(0, ctor.ParameterCount);
                }
            }
        }

        [Theory]
        [CombinatorialData]
        public void Members_01([CombinatorialValues("class", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS8907 // Parameter 'x' is unread.
" + keyword + @"
C(int x, int y);
");

            var c = comp.GlobalNamespace.GetTypeMember("C");

            Assert.All(c.GetMembers(), m => Assert.True(m is MethodSymbol { MethodKind: MethodKind.Constructor }));
            Assert.IsType<SynthesizedPrimaryConstructor>(c.InstanceConstructors[0]);

            switch (c.TypeKind)
            {
                case TypeKind.Class:
                    Assert.Equal(1, c.InstanceConstructors.Count());
                    break;

                default:
                    Assert.Equal(2, c.InstanceConstructors.Count());
                    Assert.True(c.InstanceConstructors[1].IsDefaultValueTypeConstructor());
                    break;
            }

            Assert.Empty(c.InterfacesNoUseSiteDiagnostics());
        }

        [Theory]
        [CombinatorialData]
        public void PartialTypes_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter 'x' is unread.

partial " + keyword + @" C(int X, int Y)
{
}

partial " + keyword + @" C(int U, int V)
{
}
";
            var comp = CreateCompilation(src);
            // PROTOTYPE(PrimaryConstructors): Adjust wording for ERR_MultipleRecordParameterLists?
            comp.VerifyDiagnostics(
                // (8,17): error CS8863: Only a single record partial declaration may have a parameter list
                // partial struct C(int U, int V)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int U, int V)").WithLocation(8, 17)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");

            switch (c.TypeKind)
            {
                case TypeKind.Class:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;

                default:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor()" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;
            }
        }

        [Theory]
        [CombinatorialData]
        public void PartialTypes_02([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter 'x' is unread.

partial " + keyword + @" C(int X, int Y)
{
}

partial " + keyword + @" C(int U)
{
}
";
            var comp = CreateCompilation(src);
            // PROTOTYPE(PrimaryConstructors): Adjust wording for ERR_MultipleRecordParameterLists?
            comp.VerifyDiagnostics(
                // (8,17): error CS8863: Only a single record partial declaration may have a parameter list
                // partial struct C(int U)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int U)").WithLocation(8, 17)
                );

            var c = comp.GlobalNamespace.GetTypeMember("C");

            switch (c.TypeKind)
            {
                case TypeKind.Class:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;

                default:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor()" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;
            }
        }

        [Theory]
        [CombinatorialData]
        public void PartialTypes_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter 'x' is unread.

partial " + keyword + @" C;

partial " + keyword + @" C(int X, int Y)
{
}

partial " + keyword + @" C;
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics();

            var c = comp.GlobalNamespace.GetTypeMember("C");

            switch (c.TypeKind)
            {
                case TypeKind.Class:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;

                default:
                    Assert.Equal(new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor()" }, c.Constructors.Select(m => m.ToTestDisplayString()));
                    break;
            }
        }

        [Theory]
        [CombinatorialData]
        public void GetDeclaredSymbolOnAnOutLocalInPropertyInitializer([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter 'x' is unread.

" + keyword + @" R(int I)
{
    public int I { get; set; } = M(out int i) ? i : 0;
    static bool M(out int i) => throw null; 
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var outVarSyntax = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Single();
            var outVar = model.GetDeclaredSymbol(outVarSyntax);
            Assert.Equal("System.Int32 i", outVar.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, outVar.Kind);
            Assert.Equal("System.Int32 R.<I>k__BackingField", outVar.ContainingSymbol.ToTestDisplayString());
        }

        [Theory]
        [CombinatorialData]
        public void ParametersInScopeInInitializers([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
var c = new C(2);
System.Console.Write((c.P1, c.P2));

public partial " + keyword + @" C(int X)
{
    public int P1 { get; set; } = X;
}
public partial " + keyword + @" C
{
    public int P2 { get; set; } = X;
}
";
            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            var verifier = CompileAndVerify(comp, expectedOutput: "(2, 2)");

            if (keyword == "struct")
            {
                verifier.VerifyDiagnostics(
                    // (5,23): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'C'. To specify an ordering, all instance fields must be in the same declaration.
                    // public partial struct C(int X)
                    Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "C").WithArguments("C").WithLocation(5, 23)
                    );
            }
            else
            {
                verifier.VerifyDiagnostics();
            }

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            foreach (var x in xs)
            {
                Assert.Equal("= X", x.Parent.ToString());
                var symbol = model.GetSymbolInfo(x).Symbol;
                Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
                Assert.Equal(SymbolKind.Parameter, symbol.Kind);
                Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
                Assert.Same(symbol, model.LookupSymbols(x.SpanStart, name: "X").Single());
                Assert.Contains("X", model.LookupNames(x.SpanStart));
            }
        }

        [Fact]
        public void BaseArguments_01()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

class C(int X, int Y = 123) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }

    C(int X, int Y, int Z = 124) : this(X, Y) {}
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"

{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   123
  IL_0003:  stfld      ""int C.Z""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  ldarg.2
  IL_000b:  call       ""Base..ctor(int, int)""
  IL_0010:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal(Accessibility.Public, symbol.ContainingSymbol.DeclaredAccessibility);
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(X, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                model = comp.GetSemanticModel(tree);
                Assert.Equal("Base..ctor(System.Int32 X, System.Int32 Y)", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup(baseWithargs));
                model = comp.GetSemanticModel(tree);

                var operation = model.GetOperation(baseWithargs);

                VerifyOperationTree(comp, operation,
@"
IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
        IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
        IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

                Assert.Null(model.GetOperation(baseWithargs.Type));
                Assert.Null(model.GetOperation(baseWithargs.Parent));
                Assert.Same(operation.Parent.Parent, model.GetOperation(baseWithargs.Parent.Parent));
                Assert.Equal(SyntaxKind.ClassDeclaration, baseWithargs.Parent.Parent.Kind());

                VerifyOperationTree(comp, operation.Parent.Parent,
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'class C(int ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'class C(int ... }')
  ExpressionBody: 
    null
");

                Assert.Null(operation.Parent.Parent.Parent);
                VerifyFlowGraph(comp, operation.Parent.Parent.Syntax, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
          Expression: 
            IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                    IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                    IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");

                var equalsValue = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().First();

                Assert.Equal("= 123", equalsValue.ToString());
                model.VerifyOperationTree(equalsValue,
@"
IParameterInitializerOperation (Parameter: [System.Int32 Y = 123]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 123')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 123) (Syntax: '123')
");
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": this(X, Y)", baseWithargs.ToString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", model.GetSymbolInfo((SyntaxNode)baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", model.GetSymbolInfo(baseWithargs).Symbol.ToTestDisplayString());
                Assert.Equal("C..ctor(System.Int32 X, [System.Int32 Y = 123])", CSharpExtensions.GetSymbolInfo(model, baseWithargs).Symbol.ToTestDisplayString());

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                model.VerifyOperationTree(baseWithargs,
@"
IInvocationOperation ( C..ctor(System.Int32 X, [System.Int32 Y = 123])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this(X, Y)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this(X, Y)')
  Arguments(2):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
        IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
        IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

                var equalsValue = tree.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Last();

                Assert.Equal("= 124", equalsValue.ToString());
                model.VerifyOperationTree(equalsValue,
@"
IParameterInitializerOperation (Parameter: [System.Int32 Z = 124]) (OperationKind.ParameterInitializer, Type: null) (Syntax: '= 124')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 124) (Syntax: '124')
");

                model.VerifyOperationTree(baseWithargs.Parent,
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'C(int X, in ... is(X, Y) {}')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this(X, Y)')
      Expression: 
        IInvocationOperation ( C..ctor(System.Int32 X, [System.Int32 Y = 123])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{}')
  ExpressionBody: 
    null
");
            }
        }

        [Fact]
        public void BaseArguments_02()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

class C(int X) : Base(Test(X, out var y), y)
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
2").VerifyDiagnostics();

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var yDecl = OutVarTests.GetOutVarDeclaration(tree, "y");
            var yRef = OutVarTests.GetReferences(tree, "y").ToArray();
            Assert.Equal(2, yRef.Length);
            OutVarTests.VerifyModelForOutVar(model, yDecl, yRef[0]);
            OutVarTests.VerifyNotAnOutLocal(model, yRef[1]);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Test(X, out var y)", x.Parent.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var y = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "y").First();
            Assert.Equal("y", y.Parent.ToString());
            Assert.Equal("(Test(X, out var y), y)", y.Parent.Parent.ToString());
            Assert.Equal("Base(Test(X, out var y), y)", y.Parent.Parent.Parent.ToString());

            symbol = model.GetSymbolInfo(y).Symbol;
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal("System.Int32 y", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "y"));
            Assert.Contains("y", model.LookupNames(x.SpanStart));

            var test = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Test").First();
            Assert.Equal("(Test(X, out var y), y)", test.Parent.Parent.Parent.ToString());

            symbol = model.GetSymbolInfo(test).Symbol;
            Assert.Equal(SymbolKind.Method, symbol.Kind);
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

class Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

class C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,15): error CS8861: Unexpected argument list.
                // class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(13, 15)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

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

class Base
{
    public Base(int X, int Y)
    {
    }
    public Base() {}
}
#pragma warning disable CS8907 // Parameter is unread.

partial class C(int X, int Y)
{
}

partial class C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,23): error CS8861: Unexpected argument list.
                // partial class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(17, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", classDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[0]));
            Assert.Equal("C", classDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_05()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial class C : Base(X, Y)
{
}

partial class C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,23): error CS8861: Unexpected argument list.
                // partial class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(13, 23),
                // (17,23): error CS8861: Unexpected argument list.
                // partial class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(17, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            foreach (var x in xs)
            {
                Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

                var symbolInfo = model.GetSymbolInfo(x);
                Assert.Null(symbolInfo.Symbol);
                Assert.Empty(symbolInfo.CandidateSymbols);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
                Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
                Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
            }

            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", classDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[0]));

            Assert.Equal("C", classDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_06()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial class C(int X, int Y) : Base(X, Y)
{
}

partial class C : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (17,23): error CS8861: Unexpected argument list.
                // partial class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(17, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", classDeclarations[0].Identifier.ValueText);
            model.VerifyOperationTree(classDeclarations[0],
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'partial cla ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'partial cla ... }')
  ExpressionBody: 
    null
");
            Assert.Equal("C", classDeclarations[1].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[1]));
        }

        [Fact]
        public void BaseArguments_07()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

partial class C : Base(X, Y)
{
}

partial class C(int X, int Y) : Base(X, Y)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (13,23): error CS8861: Unexpected argument list.
                // partial class C : Base(X, Y)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X, Y)").WithLocation(13, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var xs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ToArray();
            Assert.Equal(2, xs.Length);

            var x = xs[1];
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X, System.Int32 Y)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Same(symbol.ContainingSymbol, model.GetEnclosingSymbol(x.SpanStart));
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            x = xs[0];
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));

            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Skip(1).ToArray();

            Assert.Equal("C", classDeclarations[0].Identifier.ValueText);
            Assert.Null(model.GetOperation(classDeclarations[0]));

            Assert.Equal("C", classDeclarations[1].Identifier.ValueText);
            model.VerifyOperationTree(classDeclarations[1],
@"
IConstructorBodyOperation (OperationKind.ConstructorBody, Type: null) (Syntax: 'partial cla ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Base(X, Y)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 X, System.Int32 Y)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Base(X, Y)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: 'Base(X, Y)')
          Arguments(2):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: 'X')
                IParameterReferenceOperation: X (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'X')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Y) (OperationKind.Argument, Type: null) (Syntax: 'Y')
                IParameterReferenceOperation: Y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'Y')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'partial cla ... }')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void BaseArguments_08()
        {
            var src = @"
class Base
{
    public Base(int Y)
    {
    }
    public Base() {}
}
#pragma warning disable CS8907 // Parameter is unread.

class C(int X) : Base(Y)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,23): error CS0120: An object reference is required for the non-static field, method, or property 'C.Y'
                // class C(int X) : Base(Y)
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Y").WithArguments("C.Y").WithLocation(11, 23)
                );
        }

        [Fact]
        public void BaseArguments_09()
        {
            var src = @"
class Base
{
    public Base(int X)
    {
    }
    public Base() {}
}
#pragma warning disable CS8907 // Parameter is unread.

class C(int X) : Base(this.X)
{
    public int Y = 0;
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,23): error CS0027: Keyword 'this' is not available in the current context
                // class C(int X) : Base(this.X)
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(11, 23)
                );
        }

        [Fact]
        public void BaseArguments_10()
        {
            var src = @"
class Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

class C(dynamic X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,26): error CS1975: The constructor call needs to be dynamically dispatched, but cannot be because it is part of a constructor initializer. Consider casting the dynamic arguments.
                // class C(dynamic X) : Base(X)
                Diagnostic(ErrorCode.ERR_NoDynamicPhantomOnBaseCtor, "(X)").WithLocation(11, 26)
                );
        }

        [Fact]
        public void BaseArguments_11()
        {
            var src = @"
class Base
{
    public Base(int X, int Y)
    {
    }

    public Base() {}
}

class C(int X) : Base(Test(X, out var y), y)
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
                // (11,7): error CS7036: There is no argument given that corresponds to the required parameter 'X' of 'Base.Base(int)'
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("X", "Base.Base(int)").WithLocation(11, 7),
                // (11,15): error CS8861: Unexpected argument list.
                // class C : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X)").WithLocation(11, 15)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_13_Struct()
        {
            var src = @"
using System;

interface Base
{
}

#pragma warning disable CS8907 // Parameter is unread.

struct C(int X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (10,23): error CS8861: Unexpected argument list.
                // struct C(int X) : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X)").WithLocation(10, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_13_Class()
        {
            var src = @"
using System;

interface Base
{
}

#pragma warning disable CS8907 // Parameter is unread.

class C(int X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);

            comp.VerifyEmitDiagnostics(
                // (10,22): error CS8861: Unexpected argument list.
                // class C(int X) : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X)").WithLocation(10, 22),
                // (10,22): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                // class C(int X) : Base(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("object", "1").WithLocation(10, 22)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Equal("System.Int32 X", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Equal("System.Int32 X", model.LookupSymbols(x.SpanStart, name: "X").Single().ToTestDisplayString());
            Assert.Contains("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_15()
        {
            var src = @"
using System;

class Base
{
    public Base(int X, int Y)
    {
        Console.WriteLine(X);
        Console.WriteLine(Y);
    }

    public Base() {}
}

partial class C
{
}

partial class C(int X, int Y) : Base(X, Y)
{
    int Z = 123;
    public static void Main()
    {
        var c = new C(1, 2);
        Console.WriteLine(c.Z);
    }
}

partial class C
{
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"
1
2
123").VerifyDiagnostics();

            verifier.VerifyIL("C..ctor(int, int)", @"
{
  // Code size       17 (0x11)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   123
  IL_0003:  stfld      ""int C.Z""
  IL_0008:  ldarg.0
  IL_0009:  ldarg.1
  IL_000a:  ldarg.2
  IL_000b:  call       ""Base..ctor(int, int)""
  IL_0010:  ret
}
");

            var comp = CreateCompilation(src);

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").ElementAt(1);
            Assert.Equal("Base(X, Y)", x.Parent.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
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

class Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

class C(int X) : Base(() => X)
{
    public static void Main()
    {
        var c = new C(1);
    }
}";
            var verifier = CompileAndVerify(src, expectedOutput: @"1").VerifyDiagnostics();
        }

        [Fact]
        public void BaseArguments_17()
        {
            var src = @"
class Base
{
    public Base(int X, int Y)
    {
    }
    public Base() {}
}
#pragma warning disable CS8907 // Parameter is unread.

class C(int X, int y)
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
class Base
{
    public Base(int X, int Y)
    {
    }
    public Base() {}
}
#pragma warning disable CS8907 // Parameter is unread.

class C(int X, int y)
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

        [Fact]
        public void BaseArguments_19()
        {
            var src = @"
class Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

class C(int X, int Y) : Base(GetInt(X, out var xx) + xx, Y), I
{
    C(int X, int Y, int Z) : this(X, Y, Z, 1) { return; }

    static int GetInt(int x1, out int x2)
    {
        throw null;
    }
}

interface I {}
";

            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (11,29): error CS1729: 'Base' does not contain a constructor that takes 2 arguments
                // class C(int X, int Y) : Base(GetInt(X, out var xx) + xx, Y)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(GetInt(X, out var xx) + xx, Y)").WithArguments("Base", "2").WithLocation(11, 29),
                // (13,30): error CS1729: 'C' does not contain a constructor that takes 4 arguments
                //     C(int X, int Y, int Z) : this(X, Y, Z, 1) {}
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "this").WithArguments("C", "4").WithLocation(13, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            SymbolInfo symbolInfo;
            PrimaryConstructorBaseTypeSyntax speculativePrimaryInitializer;
            ConstructorInitializerSyntax speculativeBaseInitializer;

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(GetInt(X, out var xx) + xx, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "Base..ctor(System.Int32 X)", "Base..ctor()" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                model = comp.GetSemanticModel(tree);
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                model = comp.GetSemanticModel(tree);
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                model = comp.GetSemanticModel(tree);
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                model = comp.GetSemanticModel(tree);
                Assert.Empty(model.GetMemberGroup(baseWithargs));
                model = comp.GetSemanticModel(tree);

                SemanticModel speculativeModel;
                speculativePrimaryInitializer = baseWithargs.WithArgumentList(baseWithargs.ArgumentList.WithArguments(baseWithargs.ArgumentList.Arguments.RemoveAt(1)));

                speculativeBaseInitializer = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, speculativePrimaryInitializer.ArgumentList);
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.False(model.TryGetSpeculativeSemanticModel(tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart,
                                                                  speculativeBaseInitializer, out _));

                var otherBasePosition = ((BaseListSyntax)baseWithargs.Parent).Types[1].SpanStart;
                Assert.False(model.TryGetSpeculativeSemanticModel(otherBasePosition, speculativePrimaryInitializer, out _));

                Assert.True(model.TryGetSpeculativeSemanticModel(baseWithargs.SpanStart, speculativePrimaryInitializer, out speculativeModel));
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo((SyntaxNode)speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo(speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", CSharpExtensions.GetSymbolInfo(speculativeModel, speculativePrimaryInitializer).Symbol.ToTestDisplayString());

                Assert.True(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out speculativeModel));

                var xxDecl = OutVarTests.GetOutVarDeclaration(speculativePrimaryInitializer.SyntaxTree, "xx");
                var xxRef = OutVarTests.GetReferences(speculativePrimaryInitializer.SyntaxTree, "xx").ToArray();
                Assert.Equal(1, xxRef.Length);
                OutVarTests.VerifyModelForOutVar(speculativeModel, xxDecl, xxRef);

                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo((SyntaxNode)speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", speculativeModel.GetSymbolInfo(speculativePrimaryInitializer).Symbol.ToTestDisplayString());
                Assert.Equal("Base..ctor(System.Int32 X)", CSharpExtensions.GetSymbolInfo(speculativeModel, speculativePrimaryInitializer).Symbol.ToTestDisplayString());

                Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (PrimaryConstructorBaseTypeSyntax)null, out _));
                Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, baseWithargs, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(otherBasePosition, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, otherBasePosition, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.SpanStart, speculativePrimaryInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single().ArgumentList.OpenParenToken.SpanStart,
                                                                         (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": this(X, Y, Z, 1)", baseWithargs.ToString());
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "C..ctor(System.Int32 X, System.Int32 Y)", "C..ctor(System.Int32 X, System.Int32 Y, System.Int32 Z)" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
        }

        [Fact]
        public void BaseArguments_20()
        {
            var src = @"
class Base
{
    public Base(int X)
    {
    }

    public Base() {}
}

class C : Base(GetInt(X, out var xx) + xx, Y), I
{
    C(int X, int Y, int Z) : base(X, Y, Z, 1) { return; }

    static int GetInt(int x1, out int x2)
    {
        throw null;
    }
}

interface I {}
";

            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (11,15): error CS8861: Unexpected argument list.
                // class C : Base(GetInt(X, out var xx) + xx, Y), I
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(GetInt(X, out var xx) + xx, Y)").WithLocation(11, 15),
                // (13,30): error CS1729: 'Base' does not contain a constructor that takes 4 arguments
                //     C(int X, int Y, int Z) : base(X, Y, Z, 1) { return; }
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("Base", "4").WithLocation(13, 30)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            SymbolInfo symbolInfo;
            PrimaryConstructorBaseTypeSyntax speculativePrimaryInitializer;
            ConstructorInitializerSyntax speculativeBaseInitializer;

            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single();
                Assert.Equal("Base(GetInt(X, out var xx) + xx, Y)", baseWithargs.ToString());
                Assert.Equal("Base", model.GetTypeInfo(baseWithargs.Type).Type.ToTestDisplayString());
                Assert.Equal(TypeInfo.None, model.GetTypeInfo(baseWithargs));
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs));
                Assert.Empty(model.GetMemberGroup(baseWithargs));

                speculativePrimaryInitializer = baseWithargs.WithArgumentList(baseWithargs.ArgumentList.WithArguments(baseWithargs.ArgumentList.Arguments.RemoveAt(1)));

                speculativeBaseInitializer = SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, speculativePrimaryInitializer.ArgumentList);
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.False(model.TryGetSpeculativeSemanticModel(tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single().SpanStart,
                                                                  speculativeBaseInitializer, out _));

                var otherBasePosition = ((BaseListSyntax)baseWithargs.Parent).Types[1].SpanStart;
                Assert.False(model.TryGetSpeculativeSemanticModel(otherBasePosition, speculativePrimaryInitializer, out _));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.SpanStart, speculativePrimaryInitializer, out _));
                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (PrimaryConstructorBaseTypeSyntax)null, out _));
                Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, baseWithargs, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(otherBasePosition, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, otherBasePosition, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single().ArgumentList.OpenParenToken.SpanStart,
                                                                         (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
            {
                var baseWithargs = tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();
                Assert.Equal(": base(X, Y, Z, 1)", baseWithargs.ToString());
                symbolInfo = model.GetSymbolInfo((SyntaxNode)baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                string[] candidates = new[] { "Base..ctor(System.Int32 X)", "Base..ctor()" };
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = model.GetSymbolInfo(baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));
                symbolInfo = CSharpExtensions.GetSymbolInfo(model, baseWithargs);
                Assert.Null(symbolInfo.Symbol);
                Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
                Assert.Equal(candidates, symbolInfo.CandidateSymbols.Select(m => m.ToTestDisplayString()));

                Assert.Empty(model.GetMemberGroup((SyntaxNode)baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(model.GetMemberGroup(baseWithargs).Select(m => m.ToTestDisplayString()));
                Assert.Empty(CSharpExtensions.GetMemberGroup(model, baseWithargs).Select(m => m.ToTestDisplayString()));

                Assert.False(model.TryGetSpeculativeSemanticModel(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer, out _));

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativePrimaryInitializer);
                Assert.Equal(SymbolInfo.None, symbolInfo);

                symbolInfo = model.GetSpeculativeSymbolInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativeBaseInitializer, SpeculativeBindingOption.BindAsExpression);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                symbolInfo = CSharpExtensions.GetSpeculativeSymbolInfo(model, baseWithargs.ArgumentList.OpenParenToken.SpanStart, speculativeBaseInitializer);
                Assert.Equal("Base..ctor(System.Int32 X)", symbolInfo.Symbol.ToTestDisplayString());

                Assert.Equal(TypeInfo.None, model.GetSpeculativeTypeInfo(baseWithargs.ArgumentList.OpenParenToken.SpanStart, (SyntaxNode)speculativePrimaryInitializer, SpeculativeBindingOption.BindAsExpression));
            }
        }

        [Fact]
        public void BaseArguments_21()
        {
            var src = @"
using System;

class Base
{
}

#pragma warning disable CS8907 // Parameter is unread.

struct C(int X) : Base(X)
{
}
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (10,19): error CS0527: Type 'Base' in interface list is not an interface
                // struct C(int X) : Base(X)
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Base").WithArguments("Base").WithLocation(10, 19),
                // (10,23): error CS8861: Unexpected argument list.
                // struct C(int X) : Base(X)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(X)").WithLocation(10, 23)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("Base(X)", x.Parent.Parent.Parent.ToString());

            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.Empty(symbolInfo.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Same("<global namespace>", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Fact]
        public void BaseArguments_Struct_Speculation()
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
struct R1(int X) : Error1(0, 1)
{
}
struct R2(int X) : Error2()
{
}
struct R3(int X) : Error3
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (2,20): error CS0246: The type or namespace name 'Error1' could not be found (are you missing a using directive or an assembly reference?)
                // struct R1(int X) : Error1(0, 1)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error1").WithArguments("Error1").WithLocation(2, 20),
                // (2,26): error CS8861: Unexpected argument list.
                // struct R1(int X) : Error1(0, 1)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(0, 1)").WithLocation(2, 26),
                // (5,20): error CS0246: The type or namespace name 'Error2' could not be found (are you missing a using directive or an assembly reference?)
                // struct R2(int X) : Error2()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error2").WithArguments("Error2").WithLocation(5, 20),
                // (5,26): error CS8861: Unexpected argument list.
                // struct R2(int X) : Error2()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(5, 26),
                // (8,20): error CS0246: The type or namespace name 'Error3' could not be found (are you missing a using directive or an assembly reference?)
                // struct R3(int X) : Error3
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error3").WithArguments("Error3").WithLocation(8, 20)
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

        [Fact]
        public void BaseErrorTypeWithParameters()
        {
            var src = @"
class  R2(int X) : Error(X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,20): error CS0246: The type or namespace name 'Error' could not be found (are you missing a using directive or an assembly reference?)
                // class  R2(int X) : Error(X)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Error").WithArguments("Error").WithLocation(2, 20),
                // (2,25): error CS1729: 'Error' does not contain a constructor that takes 1 arguments
                // class  R2(int X) : Error(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("Error", "1").WithLocation(2, 25)
                );
        }

        [Fact]
        public void BaseDynamicTypeWithParameters()
        {
            var src = @"
class  R(int X) : dynamic(X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS1965: 'R': cannot derive from the dynamic type
                // class  R(int X) : dynamic(X)
                Diagnostic(ErrorCode.ERR_DeriveFromDynamic, "dynamic").WithArguments("R").WithLocation(2, 19),
                // (2,26): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                // class  R(int X) : dynamic(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("object", "1").WithLocation(2, 26)
                );
        }

        [Fact]
        public void BaseTypeParameterTypeWithParameters()
        {
            var src = @"
class C<T>
{
    class  R(int X) : T(X)
    {
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (4,23): error CS0689: Cannot derive from 'T' because it is a type parameter
                //     class  R(int X) : T(X)
                Diagnostic(ErrorCode.ERR_DerivingFromATyVar, "T").WithArguments("T").WithLocation(4, 23),
                // (4,24): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                //     class  R(int X) : T(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("object", "1").WithLocation(4, 24)
                );
        }

        [Fact]
        public void BaseObjectTypeWithParameters()
        {
            var src = @"
class  R(int X) : object(X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,25): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                // class  R(int X) : object(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("object", "1").WithLocation(2, 25)
                );
        }

        [Fact]
        public void BaseValueTypeTypeWithParameters()
        {
            var src = @"
class  R(int X) : System.ValueType(X)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,19): error CS0644: 'R' cannot derive from special class 'ValueType'
                // class  R(int X) : System.ValueType(X)
                Diagnostic(ErrorCode.ERR_DeriveFromEnumOrValueType, "System.ValueType").WithArguments("R", "System.ValueType").WithLocation(2, 19),
                // (2,35): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                // class  R(int X) : System.ValueType(X)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "(X)").WithArguments("object", "1").WithLocation(2, 35)
                );
        }

        [Fact]
        public void BaseInterfaceWithArguments_NoPrimaryConstructor()
        {
            var src = @"
public interface I
{
}

class  R : I()
{
}

class  R2 : I(0)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (6,13): error CS8861: Unexpected argument list.
                // class  R : I()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(6, 13),
                // (10,14): error CS8861: Unexpected argument list.
                // class  R2 : I(0)
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(0)").WithLocation(10, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Initializers_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
using System;

" + keyword + @" C(int X)
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
            Assert.Equal("= X + 1", x.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("System.Int32 X", symbol.ToTestDisplayString());
            Assert.Equal("C..ctor(System.Int32 X)", symbol.ContainingSymbol.ToTestDisplayString());
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Contains(symbol, model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.Contains("X", model.LookupNames(x.SpanStart));

            var typeDeclaration = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
            Assert.Equal("C", typeDeclaration.Identifier.ValueText);
            Assert.Null(model.GetOperation(typeDeclaration));
        }

        [Theory]
        [CombinatorialData]
        public void Initializers_02([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter is unread.

" + keyword + @" C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,20): error CS0103: The name 'X' does not exist in the current context
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "X").WithArguments("X").WithLocation(6, 20)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Null(symbol);
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Theory]
        [CombinatorialData]
        public void Initializers_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter is unread.

" + keyword + @" C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,19): error CS0103: The name 'X' does not exist in the current context
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "X").WithArguments("X").WithLocation(6, 19)
                );

            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);

            var x = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "X").First();
            Assert.Equal("= X + 1", x.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Null(symbol);
            Assert.Equal("System.Int32 C.Z", model.GetEnclosingSymbol(x.SpanStart).ToTestDisplayString());
            Assert.Empty(model.LookupSymbols(x.SpanStart, name: "X"));
            Assert.DoesNotContain("X", model.LookupNames(x.SpanStart));
        }

        [Theory]
        [CombinatorialData]
        public void Initializers_04([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
using System;

" + keyword + @" C(int X)
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
            Assert.Equal("() => X + 1", x.Parent.Parent.ToString());

            var symbol = model.GetSymbolInfo(x).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
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

class Base
{
    public Base(Func<int> X)
    {
        Console.WriteLine(X());
    }

    public Base() {}
}

class C(int X) : Base(() => 100 + X++)
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
").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ParameterMemberModifiers_Ref([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
" + keyword + @" R1(ref int P1)
{
    public string P1 {get;} = (P1++).ToString();
}

public class C
{
    public static void Main()
    {
        int i = 43;
        var r1 = new R1(ref i);
        System.Console.Write((r1.P1, i));
    }
}

" + keyword + @" R2(ref int P2);
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "(43, 44)").VerifyDiagnostics();

            // PROTOTYPE(PrimaryConstructors): Report a warning about unused ref parameter for R2?
        }

        [Theory]
        [CombinatorialData]
        public void ParameterModifiers_Out([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
" + keyword + @" R(out int P2);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (2,8): error CS0177: The out parameter 'P2' must be assigned to before control leaves the current method
                // class  R(out int P2);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "R").WithArguments("P2").WithLocation(2, 8)
                );
        }

        [Fact]
        public void ParameterModifiers_Out_WithBase()
        {
            var src = @"
class Base(int I)
{
    public int I = I;
}

class R(out int P2) : Base((P2 = 1) + 1);

public class C
{
    public static void Main()
    {
        int i;
        var r = new R(out i);
        System.Console.Write((r.I, i));
    }
}
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "(2, 1)").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ParameterModifiers_Out_WithInitializer([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
" + keyword + @" R(out int P2)
{
    public int I = (P2 = 1) + 1;
}

public class C
{
    public static void Main()
    {
        int i;
        var r = new R(out i);
        System.Console.Write((r.I, i));
    }
}
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "(2, 1)").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ParameterMemberModifiers_In([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
" + keyword + @" R(in int P1)
{
    public string P1 {get;} = P1.ToString();
}

public class C
{
    public static void Main()
    {
        var r = new R(42);
        int i = 43;
        var r2 = new R(in i);
        System.Console.Write((r.P1, r2.P1));
    }
}
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "(42, 43)").VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ParameterModifiers_This([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter is unread.
" + keyword + @" R(this int i);
";

            var comp = CreateCompilation(src);
            comp.VerifyEmitDiagnostics(
                // (3,10): error CS0027: Keyword 'this' is not available in the current context
                // class  R(this int i);
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 10)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterModifiers_Params([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
" + keyword + @" R(params int[] Array)
{
    public int[] Array = Array;
}

public class C
{
    public static void Main()
    {
        var r = new R(42, 43);
        var r2 = new R(new[] { 44, 45 });
        System.Console.Write((r.Array[0], r.Array[1], r2.Array[0], r2.Array[1]));
    }
}
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "(42, 43, 44, 45)");
        }

        [Theory]
        [CombinatorialData]
        public void ParameterDefaultValue([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter is unread.

" + keyword + @" R(int x, int P = 42)
{
    public string P { get; } = P.ToString();

    public static void Main()
    {
        var r = new R(1);
        System.Console.Write(r.P);
    }
}
";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics(); ;
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructorParameters_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class C : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = true) ]
public class D : System.Attribute
{
}

#pragma warning disable CS8907 // Parameter is unread.

public " + keyword + @" Test(
    [param: C]
    [D]
    int P1)
{
}
";
            Action<ModuleSymbol> symbolValidator = moduleSymbol =>
            {
                var @class = moduleSymbol.GlobalNamespace.GetMember<NamedTypeSymbol>("Test");

                var param1 = @class.InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single().Parameters[0];
                AssertEx.SetEqual(new[] { "C", "D" }, getAttributeStrings(param1));
            };

            var comp = CompileAndVerify(source, sourceSymbolValidator: symbolValidator, symbolValidator: symbolValidator);
            comp.VerifyDiagnostics();

            IEnumerable<string> getAttributeStrings(Symbol symbol)
            {
                return GetAttributeStrings(symbol.GetAttributes());
            }
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructorParameters_02([CombinatorialValues("class ", "struct")] string keyword)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

#pragma warning disable CS8907 // Parameter is unread.

public " + keyword + @" Test1(
    [field: A]
    [property: A]
    int P1)
{
}

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class C : System.Attribute
{
}
[System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true) ]
public class D : System.Attribute
{
}

public " + keyword + @" Test2(
    [C]
    [D]
    int P1)
{
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (10,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [field: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(10, 6),
                // (11,6): warning CS0657: 'property' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [property: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "property").WithArguments("property", "param").WithLocation(11, 6),
                // (26,6): error CS0592: Attribute 'C' is not valid on this declaration type. It is only valid on 'field' declarations.
                //     [C]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "C").WithArguments("C", "field").WithLocation(26, 6),
                // (27,6): error CS0592: Attribute 'D' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                //     [D]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "D").WithArguments("D", "property, indexer").WithLocation(27, 6)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Test1").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single().Parameters[0].GetAttributes());
            Assert.Equal(2, comp.GetTypeByMetadataName("Test2").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single().Parameters[0].GetAttributes().Count());
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructorParameters_09_CallerMemberName([CombinatorialValues("class ", "struct")] string keyword)
        {
            string source = @"
#pragma warning disable CS8907 // Parameter is unread.

using System.Runtime.CompilerServices;
" + keyword + @" R(int x, [CallerMemberName] string S = """")
{
    public string S = S;
}

class C
{
    public static void Main()
    {
        var r = new R(1);
        System.Console.Write(r.S);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: "Main");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AnalyzerActions_01_Class()
        {
            var text1 = @"
class A([Attr1]int X = 0) : I1
{}

class B([Attr2]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3]int Z = 4) : base(5)
    {}
}

interface I1 {}

class Attr1 : System.Attribute {}
class Attr2 : System.Attribute {}
class Attr3 : System.Attribute {}
";

            var analyzer = new AnalyzerActions_01_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);
            Assert.Equal(1, analyzer.FireCount18);
            Assert.Equal(1, analyzer.FireCount19);
            Assert.Equal(1, analyzer.FireCount20);
            Assert.Equal(1, analyzer.FireCount21);
            Assert.Equal(1, analyzer.FireCount22);
            Assert.Equal(1, analyzer.FireCount23);
            Assert.Equal(1, analyzer.FireCount24);
            Assert.Equal(1, analyzer.FireCount25);
            Assert.Equal(1, analyzer.FireCount26);
            Assert.Equal(1, analyzer.FireCount27);
            Assert.Equal(1, analyzer.FireCount28);
            Assert.Equal(1, analyzer.FireCount29);
            Assert.Equal(1, analyzer.FireCount30);
            Assert.Equal(1, analyzer.FireCount31);
        }

        private class AnalyzerActions_01_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount0;
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
            public int FireCount7;
            public int FireCount8;
            public int FireCount9;
            public int FireCount11;
            public int FireCount12;
            public int FireCount13;
            public int FireCount15;
            public int FireCount16;
            public int FireCount17;
            public int FireCount18;
            public int FireCount19;
            public int FireCount20;
            public int FireCount21;
            public int FireCount22;
            public int FireCount23;
            public int FireCount24;
            public int FireCount25;
            public int FireCount26;
            public int FireCount27;
            public int FireCount28;
            public int FireCount29;
            public int FireCount30;
            public int FireCount31;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Handle5, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.ClassDeclaration);
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
                    case "1":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "2":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;

                    case "3":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal("System.Int32 B.M()", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "4":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "5":
                        Interlocked.Increment(ref FireCount5);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(literal.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle2(SyntaxNodeAnalysisContext context)
            {
                var equalsValue = (EqualsValueClauseSyntax)context.Node;

                switch (equalsValue.ToString())
                {
                    case "= 0":
                        Interlocked.Increment(ref FireCount15);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "= 1":
                        Interlocked.Increment(ref FireCount16);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "= 4":
                        Interlocked.Increment(ref FireCount6);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(equalsValue.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle3(SyntaxNodeAnalysisContext context)
            {
                var initializer = (ConstructorInitializerSyntax)context.Node;

                switch (initializer.ToString())
                {
                    case ": base(5)":
                        Interlocked.Increment(ref FireCount7);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(initializer.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle4(SyntaxNodeAnalysisContext context)
            {
                Interlocked.Increment(ref FireCount8);
                Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
            }

            protected void Handle5(SyntaxNodeAnalysisContext context)
            {
                var baseType = (PrimaryConstructorBaseTypeSyntax)context.Node;

                switch (baseType.ToString())
                {
                    case "A(2)":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "B..ctor([System.Int32 Y = 1])":
                                Interlocked.Increment(ref FireCount9);
                                break;
                            case "B":
                                Interlocked.Increment(ref FireCount17);
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

                Assert.Same(baseType.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle6(SyntaxNodeAnalysisContext context)
            {
                var @class = (ClassDeclarationSyntax)context.Node;

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "B":
                        Interlocked.Increment(ref FireCount11);
                        break;
                    case "A":
                        Interlocked.Increment(ref FireCount12);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount13);
                        break;
                    case "Attr1":
                    case "Attr2":
                    case "Attr3":
                        break;
                    default:
                        Assert.True(false);
                        break;
                }

                Assert.Same(@class.SyntaxTree, context.ContainingSymbol.DeclaringSyntaxReferences.Single().SyntaxTree);
            }

            protected void Handle7(SyntaxNodeAnalysisContext context)
            {
                var identifier = (IdentifierNameSyntax)context.Node;

                switch (identifier.Identifier.ValueText)
                {
                    case "A":
                        switch (identifier.Parent.ToString())
                        {
                            case "A(2)":
                                Interlocked.Increment(ref FireCount18);
                                Assert.Equal("B", context.ContainingSymbol.ToTestDisplayString());
                                break;
                            case "A":
                                Interlocked.Increment(ref FireCount19);
                                Assert.Equal(SyntaxKind.SimpleBaseType, identifier.Parent.Kind());
                                Assert.Equal("C", context.ContainingSymbol.ToTestDisplayString());
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "Attr1":
                        Interlocked.Increment(ref FireCount24);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "Attr2":
                        Interlocked.Increment(ref FireCount25);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "Attr3":
                        Interlocked.Increment(ref FireCount26);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
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
                                Interlocked.Increment(ref FireCount20);
                                break;
                            case "B":
                                Interlocked.Increment(ref FireCount21);
                                break;
                            case "C":
                                Interlocked.Increment(ref FireCount22);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "A":
                        switch (context.ContainingSymbol.ToTestDisplayString())
                        {
                            case "C":
                                Interlocked.Increment(ref FireCount23);
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
                        Interlocked.Increment(ref FireCount27);
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "([Attr2]int Y = 1)":
                        Interlocked.Increment(ref FireCount28);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "([Attr3]int Z = 4)":
                        Interlocked.Increment(ref FireCount29);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
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
                    case "(2)":
                        Interlocked.Increment(ref FireCount30);
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    case "(5)":
                        Interlocked.Increment(ref FireCount31);
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_02_Class()
        {
            var text1 = @"
class A(int X = 0)
{}

class C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_02_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
        }

        private class AnalyzerActions_02_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
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
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_03_Class()
        {
            var text1 = @"
class A(int X = 0)
{}

class C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_03_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(0, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
        }

        private class AnalyzerActions_03_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
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
                        Assert.Equal(0, FireCount6);

                        context.RegisterSymbolEndAction(Handle5);
                        break;
                    case "C":
                        Interlocked.Increment(ref FireCount10);

                        Assert.Equal(0, FireCount4);
                        Assert.Equal(0, FireCount8);

                        context.RegisterSymbolEndAction(Handle6);
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
                Assert.Equal(1, FireCount6);
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
        public void AnalyzerActions_04_Class()
        {
            var text1 = @"
class A([Attr1(100)]int X = 0) : I1
{}

class B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_04_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);
        }

        private class AnalyzerActions_04_Class_Analyzer : DiagnosticAnalyzer
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
            public int FireCount13;
            public int FireCount14;
            public int FireCount15;
            public int FireCount16;
            public int FireCount17;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationAction(Handle1, OperationKind.ConstructorBody);
                context.RegisterOperationAction(Handle2, OperationKind.Invocation);
                context.RegisterOperationAction(Handle3, OperationKind.Literal);
                context.RegisterOperationAction(Handle4, OperationKind.ParameterInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.PropertyInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.FieldInitializer);
            }

            protected void Handle1(OperationAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1);
                        Assert.Equal(SyntaxKind.ClassDeclaration, context.Operation.Syntax.Kind());
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal(SyntaxKind.ClassDeclaration, context.Operation.Syntax.Kind());
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal(SyntaxKind.ConstructorDeclaration, context.Operation.Syntax.Kind());
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle2(OperationAnalysisContext context)
            {
                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal(SyntaxKind.PrimaryConstructorBaseType, context.Operation.Syntax.Kind());
                        VerifyOperationTree((CSharpCompilation)context.Compilation, context.Operation,
@"
IInvocationOperation ( A..ctor([System.Int32 X = 0])) (OperationKind.Invocation, Type: System.Void) (Syntax: 'A(2)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: 'A(2)')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '2')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount5);
                        Assert.Equal(SyntaxKind.BaseConstructorInitializer, context.Operation.Syntax.Kind());
                        VerifyOperationTree((CSharpCompilation)context.Compilation, context.Operation,
@"
IInvocationOperation ( A..ctor([System.Int32 X = 0])) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(5)')
  Instance Receiver: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: A, IsImplicit) (Syntax: ': base(5)')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: X) (OperationKind.Argument, Type: null) (Syntax: '5')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle3(OperationAnalysisContext context)
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
                    case "200":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount8);
                        break;
                    case "1":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount9);
                        break;
                    case "2":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount10);
                        break;
                    case "300":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount11);
                        break;
                    case "4":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount12);
                        break;
                    case "5":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount13);
                        break;
                    case "3":
                        Assert.Equal("System.Int32 B.M()", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount17);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle4(OperationAnalysisContext context)
            {
                switch (context.Operation.Syntax.ToString())
                {
                    case "= 0":
                        Assert.Equal("A..ctor([System.Int32 X = 0])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount14);
                        break;
                    case "= 1":
                        Assert.Equal("B..ctor([System.Int32 Y = 1])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount15);
                        break;
                    case "= 4":
                        Assert.Equal("C..ctor([System.Int32 Z = 4])", context.ContainingSymbol.ToTestDisplayString());
                        Interlocked.Increment(ref FireCount16);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            protected void Handle5(OperationAnalysisContext context)
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void AnalyzerActions_05_Class()
        {
            var text1 = @"
class A([Attr1(100)]int X = 0) : I1
{}

class B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_05_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_05_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;

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
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_06_Class()
        {
            var text1 = @"
class A([Attr1(100)]int X = 0) : I1
{}

class B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_06_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount200);
            Assert.Equal(1, analyzer.FireCount300);
            Assert.Equal(1, analyzer.FireCount400);

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
            Assert.Equal(1, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount14);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(1, analyzer.FireCount17);

            Assert.Equal(1, analyzer.FireCount1000);
            Assert.Equal(1, analyzer.FireCount2000);
            Assert.Equal(1, analyzer.FireCount3000);
            Assert.Equal(1, analyzer.FireCount4000);
        }

        private class AnalyzerActions_06_Class_Analyzer : AnalyzerActions_04_Class_Analyzer
        {
            public int FireCount100;
            public int FireCount200;
            public int FireCount300;
            public int FireCount400;

            public int FireCount1000;
            public int FireCount2000;
            public int FireCount3000;
            public int FireCount4000;

            private static readonly DiagnosticDescriptor Descriptor =
               new DiagnosticDescriptor("XY0000", "Test", "Test", "Test", DiagnosticSeverity.Warning, true, "Test", "Test");

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterOperationBlockStartAction(Handle);
            }

            private void Handle(OperationBlockStartAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount100);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount200);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount300);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount400);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        RegisterOperationAction(context);
                        context.RegisterOperationBlockEndAction(Handle6);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }

            private void RegisterOperationAction(OperationBlockStartAnalysisContext context)
            {
                context.RegisterOperationAction(Handle1, OperationKind.ConstructorBody);
                context.RegisterOperationAction(Handle2, OperationKind.Invocation);
                context.RegisterOperationAction(Handle3, OperationKind.Literal);
                context.RegisterOperationAction(Handle4, OperationKind.ParameterInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.PropertyInitializer);
                context.RegisterOperationAction(Handle5, OperationKind.FieldInitializer);
            }

            private void Handle6(OperationBlockAnalysisContext context)
            {
                switch (context.OwningSymbol.ToTestDisplayString())
                {
                    case "A..ctor([System.Int32 X = 0])":
                        Interlocked.Increment(ref FireCount1000);
                        Assert.Equal(2, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 0", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr1(100)", context.OperationBlocks[1].Syntax.ToString());

                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        Interlocked.Increment(ref FireCount2000);
                        Assert.Equal(3, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 1", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr2(200)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[2].Kind);
                        Assert.Equal("A(2)", context.OperationBlocks[2].Syntax.ToString());

                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        Interlocked.Increment(ref FireCount3000);
                        Assert.Equal(4, context.OperationBlocks.Length);

                        Assert.Equal(OperationKind.ParameterInitializer, context.OperationBlocks[0].Kind);
                        Assert.Equal("= 4", context.OperationBlocks[0].Syntax.ToString());

                        Assert.Equal(OperationKind.Attribute, context.OperationBlocks[1].Kind);
                        Assert.Equal("Attr3(300)", context.OperationBlocks[1].Syntax.ToString());

                        Assert.Equal(OperationKind.Block, context.OperationBlocks[2].Kind);

                        Assert.Equal(OperationKind.Invocation, context.OperationBlocks[3].Kind);
                        Assert.Equal(": base(5)", context.OperationBlocks[3].Syntax.ToString());

                        break;
                    case "System.Int32 B.M()":
                        Interlocked.Increment(ref FireCount4000);
                        Assert.Equal(1, context.OperationBlocks.Length);
                        Assert.Equal(OperationKind.Block, context.OperationBlocks[0].Kind);
                        break;
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_07_Class()
        {
            var text1 = @"
class A([Attr1(100)]int X = 0) : I1
{}

class B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_07_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_07_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
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
                            case ClassDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case ClassDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount2);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount3);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
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
        public void AnalyzerActions_08_Class()
        {
            var text1 = @"
class A([Attr1]int X = 0) : I1
{}

class B([Attr2]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_08_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount200);
            Assert.Equal(1, analyzer.FireCount300);
            Assert.Equal(1, analyzer.FireCount400);

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(0, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(0, analyzer.FireCount11);
            Assert.Equal(0, analyzer.FireCount12);
            Assert.Equal(0, analyzer.FireCount13);
            Assert.Equal(1, analyzer.FireCount15);
            Assert.Equal(1, analyzer.FireCount16);
            Assert.Equal(0, analyzer.FireCount17);
            Assert.Equal(0, analyzer.FireCount18);
            Assert.Equal(0, analyzer.FireCount19);
            Assert.Equal(0, analyzer.FireCount20);
            Assert.Equal(0, analyzer.FireCount21);
            Assert.Equal(0, analyzer.FireCount22);
            Assert.Equal(0, analyzer.FireCount23);
            Assert.Equal(1, analyzer.FireCount24);
            Assert.Equal(1, analyzer.FireCount25);
            Assert.Equal(1, analyzer.FireCount26);
            Assert.Equal(0, analyzer.FireCount27);
            Assert.Equal(0, analyzer.FireCount28);
            Assert.Equal(0, analyzer.FireCount29);
            Assert.Equal(1, analyzer.FireCount30);
            Assert.Equal(1, analyzer.FireCount31);

            Assert.Equal(1, analyzer.FireCount1000);
            Assert.Equal(1, analyzer.FireCount2000);
            Assert.Equal(1, analyzer.FireCount3000);
            Assert.Equal(1, analyzer.FireCount4000);
        }

        private class AnalyzerActions_08_Class_Analyzer : AnalyzerActions_01_Class_Analyzer
        {
            public int FireCount100;
            public int FireCount200;
            public int FireCount300;
            public int FireCount400;

            public int FireCount1000;
            public int FireCount2000;
            public int FireCount3000;
            public int FireCount4000;

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
                            case ClassDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount100);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case ClassDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount200);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount300);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
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
                    default:
                        Assert.True(false);
                        break;
                }

                context.RegisterSyntaxNodeAction(Handle1, SyntaxKind.NumericLiteralExpression);
                context.RegisterSyntaxNodeAction(Handle2, SyntaxKind.EqualsValueClause);
                context.RegisterSyntaxNodeAction(Handle3, SyntaxKind.BaseConstructorInitializer);
                context.RegisterSyntaxNodeAction(Handle4, SyntaxKind.ConstructorDeclaration);
                context.RegisterSyntaxNodeAction(Handle5, SyntaxKind.PrimaryConstructorBaseType);
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.ClassDeclaration);
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
                            case ClassDeclarationSyntax { Identifier: { ValueText: "A" } }:
                                Interlocked.Increment(ref FireCount1000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "B..ctor([System.Int32 Y = 1])":
                        switch (context.CodeBlock)
                        {
                            case ClassDeclarationSyntax { Identifier: { ValueText: "B" } }:
                                Interlocked.Increment(ref FireCount2000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "C..ctor([System.Int32 Z = 4])":
                        switch (context.CodeBlock)
                        {
                            case ConstructorDeclarationSyntax { Identifier: { ValueText: "C" } }:
                                Interlocked.Increment(ref FireCount3000);
                                break;
                            default:
                                Assert.True(false);
                                break;
                        }
                        break;
                    case "System.Int32 B.M()":
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
                    default:
                        Assert.True(false);
                        break;
                }
            }
        }

        [Fact]
        public void AnalyzerActions_09_Class()
        {
            var text1 = @"
class A([Attr1(100)]int X = 0) : I1
{}

class B([Attr2(200)]int Y = 1) : A(2), I1
{
    int M() => 3;
}

class C : A, I1
{
    C([Attr3(300)]int Z = 4) : base(5)
    {}
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_09_Class_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount2);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
        }

        private class AnalyzerActions_09_Class_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount2;
            public int FireCount3;
            public int FireCount4;
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
        public void AnalyzerActions_01_Struct()
        {
            // Test RegisterSyntaxNodeAction
            var text1 = @"
struct A([Attr1]int X = 0) : I1
{
    private int M() => 3;
    A(string S) : this(4) => throw null;
}

interface I1 {}

class Attr1 : System.Attribute {}
";
            var analyzer = new AnalyzerActions_01_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(1, analyzer.FireCountStructDeclarationA);
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

        private class AnalyzerActions_01_Struct_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount0;
            public int FireCountStructDeclarationA;
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
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.StructDeclaration);
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
                var record = (StructDeclarationSyntax)context.Node;
                Assert.Equal(SyntaxKind.StructDeclaration, record.Kind());

                switch (context.ContainingSymbol.ToTestDisplayString())
                {
                    case "A":
                        Interlocked.Increment(ref FireCountStructDeclarationA);
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
        public void AnalyzerActions_02_Struct()
        {
            // Test RegisterSymbolAction
            var text1 = @"
struct A(int X = 0)
{}

struct C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_02_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(1, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
        }

        private class AnalyzerActions_02_Struct_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
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
        public void AnalyzerActions_03_Struct()
        {
            // Test RegisterSymbolStartAction
            var text1 = @"
readonly struct A(int X = 0)
{}

readonly struct C
{
    C(int Z = 4)
    {}
}
";

            var analyzer = new AnalyzerActions_03_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(0, analyzer.FireCount3);
            Assert.Equal(1, analyzer.FireCount4);
            Assert.Equal(0, analyzer.FireCount5);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount8);
            Assert.Equal(1, analyzer.FireCount9);
            Assert.Equal(1, analyzer.FireCount10);
            Assert.Equal(1, analyzer.FireCount11);
            Assert.Equal(1, analyzer.FireCount12);
        }

        private class AnalyzerActions_03_Struct_Analyzer : DiagnosticAnalyzer
        {
            public int FireCount1;
            public int FireCount3;
            public int FireCount4;
            public int FireCount5;
            public int FireCount6;
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
                        Assert.Equal(0, FireCount6);

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
                Assert.Equal(1, FireCount6);
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
        public void AnalyzerActions_04_Struct()
        {
            // Test RegisterOperationAction
            var text1 = @"
struct A([Attr1(100)]int X = 0) : I1
{}

interface I1 {}
";

            var analyzer = new AnalyzerActions_04_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(0, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount6);
            Assert.Equal(1, analyzer.FireCount7);
            Assert.Equal(1, analyzer.FireCount14);
        }

        private class AnalyzerActions_04_Struct_Analyzer : DiagnosticAnalyzer
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
        public void AnalyzerActions_05_Struct()
        {
            // Test RegisterOperationBlockAction
            var text1 = @"
struct A([Attr1(100)]int X = 0) : I1
{}

interface I1 {}
";

            var analyzer = new AnalyzerActions_05_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
        }

        private class AnalyzerActions_05_Struct_Analyzer : DiagnosticAnalyzer
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
        public void AnalyzerActions_07_Struct()
        {
            // Test RegisterCodeBlockAction
            var text1 = @"
struct A([Attr1(100)]int X = 0) : I1
{
    int M() => 3;
}

interface I1 {}
";
            var analyzer = new AnalyzerActions_07_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount1);
            Assert.Equal(1, analyzer.FireCount4);
        }

        private class AnalyzerActions_07_Struct_Analyzer : DiagnosticAnalyzer
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
                            case StructDeclarationSyntax { Identifier: { ValueText: "A" } }:
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
        public void AnalyzerActions_08_Struct()
        {
            // Test RegisterCodeBlockStartAction
            var text1 = @"
struct A([Attr1]int X = 0) : I1
{
    private int M() => 3;
    A(string S) : this(4) => throw null;
}

interface I1 {}
";

            var analyzer = new AnalyzerActions_08_Struct_Analyzer();
            var comp = CreateCompilation(text1);
            comp.GetAnalyzerDiagnostics(new[] { analyzer }, null).Verify();

            Assert.Equal(1, analyzer.FireCount100);
            Assert.Equal(1, analyzer.FireCount400);
            Assert.Equal(1, analyzer.FireCount500);

            Assert.Equal(1, analyzer.FireCount0);
            Assert.Equal(0, analyzer.FireCountStructDeclarationA);
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

        private class AnalyzerActions_08_Struct_Analyzer : AnalyzerActions_01_Struct_Analyzer
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
                            case StructDeclarationSyntax { Identifier: { ValueText: "A" } }:
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
                context.RegisterSyntaxNodeAction(Handle6, SyntaxKind.StructDeclaration);
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
                            case StructDeclarationSyntax { Identifier: { ValueText: "A" } }:
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

        [Theory]
        [CombinatorialData]
        public void XmlDoc([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS8907 // Parameter is unread.

/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
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
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Cref([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for <see cref=""I1""/></param>
public " + keyword + @" C(int I1)
{
    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    public void M(int x) { }
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (3,49): warning CS1574: XML comment has cref attribute 'I1' that could not be resolved
                // /// <param name="I1">Description for <see cref="I1"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "I1").WithArguments("I1").WithLocation(3, 49),
                // (3,49): warning CS1574: XML comment has cref attribute 'I1' that could not be resolved
                // /// <param name="I1">Description for <see cref="I1"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "I1").WithArguments("I1").WithLocation(3, 49),
                // (7,52): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(7, 52)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Error([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""Error""></param>
/// <param name=""I1""></param>
public " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (3,18): warning CS1572: XML comment has a param tag for 'Error', but there is no parameter by that name
                // /// <param name="Error"></param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "Error").WithArguments("Error").WithLocation(3, 18),
                // (3,18): warning CS1572: XML comment has a param tag for 'Error', but there is no parameter by that name
                // /// <param name="Error"></param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "Error").WithArguments("Error").WithLocation(3, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Duplicate([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1""></param>
/// <param name=""I1""></param>
public " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (4,12): warning CS1571: XML comment has a duplicate param tag for 'I1'
                // /// <param name="I1"></param>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""I1""").WithArguments("I1").WithLocation(4, 12),
                // (4,12): warning CS1571: XML comment has a duplicate param tag for 'I1'
                // /// <param name="I1"></param>
                Diagnostic(ErrorCode.WRN_DuplicateParamTag, @"name=""I1""").WithArguments("I1").WithLocation(4, 12)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_ParamRef([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary <paramref name=""I1""/></summary>
/// <param name=""I1"">Description for I1</param>
public " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("C");
            var constructor = cMember.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:C.#ctor(System.Int32)"">
    <summary>Summary <paramref name=""I1""/></summary>
    <param name=""I1"">Description for I1</param>
</member>
", constructor.GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_ParamRef_Error([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary <paramref name=""Error""/></summary>
/// <param name=""I1"">Description for I1</param>
public " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (2,38): warning CS1734: XML comment on 'C' has a paramref tag for 'Error', but there is no parameter by that name
                // /// <summary>Summary <paramref name="Error"/></summary>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "Error").WithArguments("Error", "C").WithLocation(2, 38),
                // (2,38): warning CS1734: XML comment on 'C.C(int)' has a paramref tag for 'Error', but there is no parameter by that name
                // /// <summary>Summary <paramref name="Error"/></summary>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "Error").WithArguments("Error", "C.C(int)").WithLocation(2, 38)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_WithExplicitProperty([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public " + keyword + @" C(int I1)
{
    /// <summary>Property summary</summary>
    public int I1 { get; set; } = I1;
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

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
            Assert.Equal(
@"<member name=""P:C.I1"">
    <summary>Property summary</summary>
</member>
", property.GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_EmptyParameterList([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
public " + keyword + @" C();
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(
@"<member name=""T:C"">
    <summary>Summary</summary>
</member>
", cMember.GetDocumentationCommentXml());

            var constructor = cMember.GetMembers(".ctor").OfType<MethodSymbol>().Where(m => m.Parameters.IsEmpty).Single();
            Assert.Equal(
@"<member name=""M:C.#ctor"">
    <summary>Summary</summary>
</member>
", constructor.GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_ParamListSecond([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
public partial " + keyword + @" C;

/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics();

            var c = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(
@"<member name=""T:C"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", c.GetDocumentationCommentXml());

            var cConstructor = c.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:C.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", cConstructor.GetDocumentationCommentXml());

            Assert.Equal("", cConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_ParamListFirst([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" D(int I1);

public partial " + keyword + @" D;
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics();

            var d = comp.GetMember<NamedTypeSymbol>("D");
            Assert.Equal(
@"<member name=""T:D"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", d.GetDocumentationCommentXml());

            var dConstructor = d.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:D.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", dConstructor.GetDocumentationCommentXml());

            Assert.Equal("", dConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_ParamListFirst_XmlDocSecond([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
public partial " + keyword + @" E(int I1);

/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" E;
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (2,23): warning CS1591: Missing XML comment for publicly visible type or member 'E.E(int)'
                // public partial class  E(int I1);
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "E").WithArguments("E.E(int)").WithLocation(2, 23),
                // (5,18): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                // /// <param name="I1">Description for I1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(5, 18)
                );

            var e = comp.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(
@"<member name=""T:E"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", e.GetDocumentationCommentXml());

            var eConstructor = e.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal("", eConstructor.GetDocumentationCommentXml());
            Assert.Equal("", eConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_ParamListSecond_XmlDocFirst([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" E;

public partial " + keyword + @" E(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (3,18): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                // /// <param name="I1">Description for I1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(3, 18),
                // (6,23): warning CS1591: Missing XML comment for publicly visible type or member 'E.E(int)'
                // public partial class  E(int I1);
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "E").WithArguments("E.E(int)").WithLocation(6, 23)
                );

            var e = comp.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(
@"<member name=""T:E"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", e.GetDocumentationCommentXml());

            var eConstructor = e.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal("", eConstructor.GetDocumentationCommentXml());
            Assert.Equal("", eConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_DuplicateParameterList_XmlDocSecond([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
public partial " + keyword + @" C(int I1);

/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            // PROTOTYPE(PrimaryConstructors): Adjust wording for ERR_MultipleRecordParameterLists
            comp.VerifyDiagnostics(
                // (2,23): warning CS1591: Missing XML comment for publicly visible type or member 'C.C(int)'
                // public partial class  C(int I1);
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C.C(int)").WithLocation(2, 23),
                // (5,18): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                // /// <param name="I1">Description for I1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(5, 18),
                // (6,24): error CS8863: Only a single record partial declaration may have a parameter list
                // public partial class  C(int I1);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int I1)").WithLocation(6, 24)
                );

            var c = comp.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(
@"<member name=""T:C"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", c.GetDocumentationCommentXml());

            var cConstructor = c.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(1, cConstructor.DeclaringSyntaxReferences.Count());
            Assert.Equal("", cConstructor.GetDocumentationCommentXml());
            Assert.Equal("", cConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_DuplicateParameterList_XmlDocFirst([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" D(int I1);

public partial " + keyword + @" D(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (6,24): error CS8863: Only a single record partial declaration may have a parameter list
                // public partial class  D(int I1);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int I1)").WithLocation(6, 24)
                );

            var d = comp.GetMember<NamedTypeSymbol>("D");
            Assert.Equal(
@"<member name=""T:D"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", d.GetDocumentationCommentXml());

            var dConstructor = d.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:D.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", dConstructor.GetDocumentationCommentXml());

            Assert.Equal("", dConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_DuplicateParameterList_XmlDocOnBoth([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary1</summary>
/// <param name=""I1"">Description1 for I1</param>
public partial " + keyword + @" E(int I1);

/// <summary>Summary2</summary>
/// <param name=""I1"">Description2 for I1</param>
public partial " + keyword + @" E(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (7,18): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                // /// <param name="I1">Description2 for I1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(7, 18),
                // (8,24): error CS8863: Only a single record partial declaration may have a parameter list
                // public partial class  E(int I1);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int I1)").WithLocation(8, 24)
                );

            var e = comp.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(
@"<member name=""T:E"">
    <summary>Summary1</summary>
    <param name=""I1"">Description1 for I1</param>
    <summary>Summary2</summary>
    <param name=""I1"">Description2 for I1</param>
</member>
", e.GetDocumentationCommentXml());

            var eConstructor = e.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(1, eConstructor.DeclaringSyntaxReferences.Count());
            Assert.Equal(
@"<member name=""M:E.#ctor(System.Int32)"">
    <summary>Summary1</summary>
    <param name=""I1"">Description1 for I1</param>
</member>
", eConstructor.GetDocumentationCommentXml());
            Assert.Equal("", eConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_DifferentParameterLists_XmlDocSecond([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
public partial " + keyword + @" E(int I1);

/// <summary>Summary2</summary>
/// <param name=""S1"">Description2 for S1</param>
public partial " + keyword + @" E(string S1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (2,23): warning CS1591: Missing XML comment for publicly visible type or member 'E.E(int)'
                // public partial class  E(int I1);
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "E").WithArguments("E.E(int)").WithLocation(2, 23),
                // (5,18): warning CS1572: XML comment has a param tag for 'S1', but there is no parameter by that name
                // /// <param name="S1">Description2 for S1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "S1").WithArguments("S1").WithLocation(5, 18),
                // (6,24): error CS8863: Only a single record partial declaration may have a parameter list
                // public partial class  E(string S1);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(string S1)").WithLocation(6, 24)
                );

            var e = comp.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(
@"<member name=""T:E"">
    <summary>Summary2</summary>
    <param name=""S1"">Description2 for S1</param>
</member>
", e.GetDocumentationCommentXml());

            var eConstructor = e.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(1, eConstructor.DeclaringSyntaxReferences.Count());
            Assert.Equal("", eConstructor.GetDocumentationCommentXml());
            Assert.Equal("", eConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Partial_DifferentParameterLists_XmlDocOnBoth([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary1</summary>
/// <param name=""I1"">Description1 for I1</param>
public partial " + keyword + @" E(int I1);

/// <summary>Summary2</summary>
/// <param name=""S1"">Description2 for S1</param>
public partial " + keyword + @" E(string S1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (7,18): warning CS1572: XML comment has a param tag for 'S1', but there is no parameter by that name
                // /// <param name="S1">Description2 for S1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "S1").WithArguments("S1").WithLocation(7, 18),
                // (8,24): error CS8863: Only a single record partial declaration may have a parameter list
                // public partial class  E(string S1);
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(string S1)").WithLocation(8, 24)
                );

            var e = comp.GetMember<NamedTypeSymbol>("E");
            Assert.Equal(
@"<member name=""T:E"">
    <summary>Summary1</summary>
    <param name=""I1"">Description1 for I1</param>
    <summary>Summary2</summary>
    <param name=""S1"">Description2 for S1</param>
</member>
", e.GetDocumentationCommentXml());

            var eConstructor = e.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(1, eConstructor.DeclaringSyntaxReferences.Count());
            Assert.Equal(
@"<member name=""M:E.#ctor(System.Int32)"">
    <summary>Summary1</summary>
    <param name=""I1"">Description1 for I1</param>
</member>
", eConstructor.GetDocumentationCommentXml());
            Assert.Equal("", eConstructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Nested([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
public class Outer
{
    /// <summary>Summary</summary>
    /// <param name=""I1"">Description for I1</param>
    public " + keyword + @" C(int I1);
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics();

            var cMember = comp.GetMember<NamedTypeSymbol>("Outer.C");
            Assert.Equal(
@"<member name=""T:Outer.C"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", cMember.GetDocumentationCommentXml());

            var constructor = cMember.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:Outer.C.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
</member>
", constructor.GetDocumentationCommentXml());

            Assert.Equal("", constructor.GetParameters()[0].GetDocumentationCommentXml());
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Nested_ReferencingOuterParam([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS8907 // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""O1"">Description for O1</param>
public " + keyword + @" Outer(object O1)
{
    /// <summary>Summary</summary>
    public int P1 { get; set; }

    /// <summary>Summary</summary>
    /// <param name=""I1"">Description for I1</param>
    /// <param name=""O1"">Error O1</param>
    /// <param name=""P1"">Error P1</param>
    /// <param name=""C"">Error C</param>
    public " + keyword + @" C(int I1);
}
";
            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (11,22): warning CS1572: XML comment has a param tag for 'O1', but there is no parameter by that name
                //     /// <param name="O1">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "O1").WithArguments("O1").WithLocation(11, 22),
                // (11,22): warning CS1572: XML comment has a param tag for 'O1', but there is no parameter by that name
                //     /// <param name="O1">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "O1").WithArguments("O1").WithLocation(11, 22),
                // (12,22): warning CS1572: XML comment has a param tag for 'P1', but there is no parameter by that name
                //     /// <param name="P1">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "P1").WithArguments("P1").WithLocation(12, 22),
                // (12,22): warning CS1572: XML comment has a param tag for 'P1', but there is no parameter by that name
                //     /// <param name="P1">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "P1").WithArguments("P1").WithLocation(12, 22),
                // (13,22): warning CS1572: XML comment has a param tag for 'C', but there is no parameter by that name
                //     /// <param name="C">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "C").WithArguments("C").WithLocation(13, 22),
                // (13,22): warning CS1572: XML comment has a param tag for 'C', but there is no parameter by that name
                //     /// <param name="C">Error</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "C").WithArguments("C").WithLocation(13, 22)
                );

            var cMember = comp.GetMember<NamedTypeSymbol>("Outer.C");
            var constructor = cMember.GetMembers(".ctor").OfType<SynthesizedPrimaryConstructor>().Single();
            Assert.Equal(
@"<member name=""M:Outer.C.#ctor(System.Int32)"">
    <summary>Summary</summary>
    <param name=""I1"">Description for I1</param>
    <param name=""O1"">Error O1</param>
    <param name=""P1"">Error P1</param>
    <param name=""C"">Error C</param>
</member>
", constructor.GetDocumentationCommentXml());
        }

        [Fact]
        public void NoMethodBodiesInComImportType_Class()
        {
            var source1 =
@"
#pragma warning disable CS8907 // Parameter 'x' is unread.

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid(""00112233-4455-6677-8899-aabbccddeeff"")]
class R1(int x);
";
            var compilation1 = CreateCompilation(source1, targetFramework: TargetFramework.Net60);

            compilation1.VerifyDiagnostics(
                // (6,7): error CS0669: A class with the ComImport attribute cannot have a user-defined constructor
                // class R1(int x);
                Diagnostic(ErrorCode.ERR_ComImportWithUserCtor, "R1").WithLocation(6, 7)
                );
        }

        [Fact]
        public void NoMethodBodiesInComImportType_Struct()
        {
            var source1 =
@"
#pragma warning disable CS8907 // Parameter 'x' is unread.

[System.Runtime.InteropServices.ComImport]
[System.Runtime.InteropServices.Guid(""00112233-4455-6677-8899-aabbccddeeff"")]
struct R1(int x);
";
            var compilation1 = CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: TargetFramework.Net60);

            compilation1.VerifyDiagnostics(
                // (4,2): error CS0592: Attribute 'System.Runtime.InteropServices.ComImport' is not valid on this declaration type. It is only valid on 'class, interface' declarations.
                // [System.Runtime.InteropServices.ComImport]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "System.Runtime.InteropServices.ComImport").WithArguments("System.Runtime.InteropServices.ComImport", "class, interface").WithLocation(4, 2)
                );
        }

        [Fact]
        public void AttributedDerived_SemanticInfoOnBaseParameter()
        {
            var source = """
                #pragma warning disable CS8907 // Parameter is unread.

                public class Base(int X);
                [Attr]
                public class Derived(int X) : Base(X);

                class Attr : System.Attribute {}
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithFeature("run-nullable-analysis", "never"), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var xReference = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single().ArgumentList.Arguments[0].Expression;

            AssertEx.Equal("System.Int32 X", model.GetSymbolInfo(xReference).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void AttributedDerived_BaseParameterNotVisibleInBody()
        {
            var source = """
                #pragma warning disable CS8907 // Parameter is unread.
                
                public class Base(int X);
                [Attr()]
                public class Derived() : Base(M(out var y))
                {
                    static int M(out int y) => y = 1;
                }

                class Attr : System.Attribute {}
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithFeature("run-nullable-analysis", "never"), targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var mCall = tree.GetRoot().DescendantNodes().OfType<PrimaryConstructorBaseTypeSyntax>().Single().ArgumentList.Arguments[0].Expression;
            var attrApplication = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();
            var mDefinition = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.Contains("System.Int32 y", model.LookupSymbols(mCall.SpanStart).Select(s => s.ToTestDisplayString()));
            Assert.DoesNotContain("System.Int32 y", model.LookupSymbols(attrApplication.ArgumentList!.OpenParenToken.SpanStart + 1).Select(s => s.ToTestDisplayString()));
            Assert.DoesNotContain("System.Int32 y", model.LookupSymbols(mDefinition.SpanStart).Select(s => s.ToTestDisplayString()));
        }

        [Theory]
        [CombinatorialData]
        public void OutVarInParameterDefaultValue([CombinatorialValues("class ", "struct")] string keyword)
        {
            var source =
@"#pragma warning disable CS8907 // Parameter is unread.
" + keyword + @" A(int X = A.M(out int a) + a)
{
    public static int M(out int a)
        => throw null;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,18): error CS1736: Default parameter value for 'X' must be a compile-time constant
                // struct A(int X = A.M(out int a) + a)
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "A.M(out int a) + a").WithArguments("X").WithLocation(2, 18)
                );
        }

        [Fact]
        public void GetSimpleNonTypeMembers_DirectApiCheck_Class()
        {
            var src = @"
public class A();

public class B
{
    public B() {}
}
public class C(int x);

public class D
{
    public D(int x) {}
}
";
            var comp = CreateCompilation(src);
            AssertEx.SetEqual(new string[] { "A..ctor()" },
                comp.GlobalNamespace.GetTypeMember("A").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "B..ctor()" },
                comp.GlobalNamespace.GetTypeMember("B").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "C..ctor(System.Int32 x)" },
                comp.GlobalNamespace.GetTypeMember("C").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "D..ctor(System.Int32 x)" },
                comp.GlobalNamespace.GetTypeMember("D").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
        }

        [Fact]
        public void GetSimpleNonTypeMembers_DirectApiCheck_Struct()
        {
            var src = @"
public struct A();

public struct B
{
    public B() {}
}
public struct C(int x);

public struct D
{
    public D(int x) {}
}
";
            var comp = CreateCompilation(src);
            AssertEx.SetEqual(new string[] { "A..ctor()" },
                comp.GlobalNamespace.GetTypeMember("A").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "B..ctor()" },
                comp.GlobalNamespace.GetTypeMember("B").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "C..ctor(System.Int32 x)", "C..ctor()" },
                comp.GlobalNamespace.GetTypeMember("C").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
            AssertEx.SetEqual(new string[] { "D..ctor(System.Int32 x)", "D..ctor()" },
                comp.GlobalNamespace.GetTypeMember("D").GetSimpleNonTypeMembers(".ctor").ToTestDisplayStrings());
        }

        [Fact]
        public void MemberNames_DirectApiCheck_Class()
        {
            var src = @"
public class A();

public class B
{
    public B() {}
}
public class C(int x);

public class D
{
    public D(int x) {}
}
";
            var comp = CreateCompilation(src);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("A").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("B").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("C").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("D").MemberNames);
        }

        [Fact]
        public void MemberNames_DirectApiCheck_Struct()
        {
            var src = @"
public struct A();

public struct B
{
    public B() {}
}
public struct C(int x);

public struct D
{
    public D(int x) {}
}
";
            var comp = CreateCompilation(src);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("A").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("B").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("C").MemberNames);
            AssertEx.SetEqual(new string[] { ".ctor" },
                comp.GlobalNamespace.GetTypeMember("D").MemberNames);
        }

        [Fact]
        public void FieldInitializers_10()
        {
            var source = @"
#pragma warning disable CS8907 // Parameter is unread.

using System;

#pragma warning disable 649
struct S1(object X)
{
    public object X = 1;
    public object Y;

    public override string ToString() => $""S1 {{ X = {X}, Y = {Y} }}"";
}
struct S2(object X)
{
    public object X { get; } = 2;
    public object Y { get; }

    public override string ToString() => $""S2 {{ X = {X}, Y = {Y} }}"";
}
struct S3(object Y)
{
    public object X { get; set; }
    public object Y { get; set; } = 3;

    public override string ToString() => $""S3 {{ X = {X}, Y = {Y} }}"";
}

class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(""a""));
        Console.WriteLine(new S2(""b""));
        Console.WriteLine(new S3(""c""));
    }
}
";

            var verifier = CompileAndVerify(source, expectedOutput:
@"S1 { X = 1, Y =  }
S2 { X = 2, Y =  }
S3 { X = , Y = 3 }
", verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
        }

        [Fact]
        public void FieldInitializers_11()
        {
            var source = @"
#pragma warning disable CS8907 // Parameter is unread.

using System;

#pragma warning disable 649
struct S1(object X)
{
    public object X;
    public object Y = 1;

    public override string ToString() => $""S1 {{ X = {X}, Y = {Y} }}"";
}
struct S2(object X)
{
    public object X { get; }
    public object Y { get; } = 2;

    public override string ToString() => $""S2 {{ X = {X}, Y = {Y} }}"";
}
struct S3(object Y)
{
    public object X { get; set; } = 3;
    public object Y { get; set; }

    public override string ToString() => $""S3 {{ X = {X}, Y = {Y} }}"";
}

class Program
{
    static void Main()
    {
        Console.WriteLine(new S1(""a""));
        Console.WriteLine(new S2(""b""));
        Console.WriteLine(new S3(""c""));
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput:
@"S1 { X = , Y = 1 }
S2 { X = , Y = 2 }
S3 { X = 3, Y =  }", verify: Verification.Skipped);
            verifier.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(CoreClrOnly))] // For conversion from Span<T> to ReadOnlySpan<T>.
        public void FieldInitializer_EscapeAnalysis_06()
        {
            var source =
@"using System;
struct Example()
{
    public Span<byte> Field = stackalloc byte[512];
    public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
}";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (4,12): error CS8345: Field or auto-implemented property cannot be of type 'Span<byte>' unless it is an instance member of a ref struct.
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "Span<byte>").WithArguments("System.Span<byte>").WithLocation(4, 12),
                // (4,31): error CS8353: A result of a stackalloc expression of type 'Span<byte>' cannot be used in this context because it may be exposed outside of the containing method
                //     public Span<byte> Field = stackalloc byte[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc byte[512]").WithArguments("System.Span<byte>").WithLocation(4, 31),
                // (5,12): error CS8345: Field or auto-implemented property cannot be of type 'ReadOnlySpan<int>' unless it is an instance member of a ref struct.
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_FieldAutoPropCantBeByRefLike, "ReadOnlySpan<int>").WithArguments("System.ReadOnlySpan<int>").WithLocation(5, 12),
                // (5,50): error CS8353: A result of a stackalloc expression of type 'Span<int>' cannot be used in this context because it may be exposed outside of the containing method
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 50));
        }
    }
}

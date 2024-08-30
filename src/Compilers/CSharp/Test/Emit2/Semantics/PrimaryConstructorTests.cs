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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    using static PrimaryConstructorTests.TestFlags;

    public class PrimaryConstructorTests : CompilingTestBase
    {
        [Flags]
        public enum TestFlags
        {
            Success = 0,
            Captured = 1 << 0,
            BadReference = 1 << 1,
            NotUsedWarning = 1 << 2,
            BadAttributeValue = 1 << 3,
            BadConstant = 1 << 4,
            BadDefaultValue = 1 << 5,
            TwoBodies = 1 << 6,
            Shadows = 1 << 7,
            InNestedMethod = 1 << 8,
            AttributesNotAllowed = 1 << 9,
            NotInScope = 1 << 10,
        }

        private static string UnreadParameterWarning()
        {
            return ((int)ErrorCode.WRN_UnreadPrimaryConstructorParameter).ToString();
        }

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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
        public void LanguageVersion_03([CombinatorialValues("class", "struct", "interface", "enum")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
;";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, ";").WithArguments("primary constructors", "12.0").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();

            var members = comp.GetTypeByMetadataName("Point").GetMembers();

            switch (keyword)
            {
                case "class":
                case "struct":
                case "enum":
                    Assert.Equal(MethodKind.Constructor, members.Cast<MethodSymbol>().Single().MethodKind);
                    break;
                case "interface":
                    Assert.Empty(members);
                    break;
                default:
                    Assert.True(false);
                    break;
            }
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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1),
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1),
                // (4,7): error CS8861: Unexpected argument list.
                // : Base()
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "()").WithLocation(4, 7)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
                // (4,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, ";").WithArguments("primary constructors", "12.0").WithLocation(4, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
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
                // (3,1): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                // ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(3, 1)
                );

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersion_08([CombinatorialValues("class ", "struct", "interface", "enum")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
{}";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void LanguageVersion_09([CombinatorialValues("class", "struct", "interface", "enum")] string keyword)
        {
            var src1 = @"
" + keyword + @" Point
{};";
            var comp = CreateCompilation(src1, parseOptions: TestOptions.Regular11, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, parseOptions: TestOptions.Regular12, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(src1, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68782")]
        public void BindIdentifier()
        {
            var comp = CreateCompilation(@"
class C
{
    public void Test()
    {
        foreach (ref read)
    }
}");

            comp.VerifyDiagnostics(
                // (6,18): error CS1525: Invalid expression term 'ref'
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref read").WithArguments("ref").WithLocation(6, 18),
                // (6,26): error CS1515: 'in' expected
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_InExpected, ")").WithLocation(6, 26),
                // (6,26): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, ")").WithLocation(6, 26),
                // (6,26): error CS1525: Invalid expression term ')'
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 26),
                // (6,27): error CS1525: Invalid expression term '}'
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 27),
                // (6,27): error CS1002: ; expected
                //         foreach (ref read)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 27)
                );
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

            var verifier = CompileAndVerify(comp).VerifyDiagnostics(
                // (1,14): warning CS9113: Parameter 'x' is unread.
                // class  C(int x, string y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(1, 14),
                // (1,24): warning CS9113: Parameter 'y' is unread.
                // class  C(int x, string y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(1, 24)
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
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
            string src = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

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
        public void ConstructorSymbol_05_NoParameters([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(keyword + @" C();");
            var c = comp.GlobalNamespace.GetTypeMember("C");

            Assert.Equal(1, c.InstanceConstructors.Length);

            var ctor = c.InstanceConstructors[0];

            Assert.Equal(Accessibility.Public, ctor.DeclaredAccessibility);
            Assert.Equal(0, ctor.ParameterCount);
            Assert.False(ctor.IsImplicitlyDeclared);
            Assert.False(ctor.IsImplicitConstructor);
            Assert.False(ctor.IsImplicitInstanceConstructor);
            Assert.False(ctor.IsDefaultValueTypeConstructor());

            var verifier = CompileAndVerify(comp,
                symbolValidator: (m) =>
                                 {
                                     Assert.False(m.GlobalNamespace.GetTypeMember("C").InstanceConstructors.Single().IsDefaultValueTypeConstructor());
                                 }).VerifyDiagnostics();

            if (c.TypeKind == TypeKind.Struct)
            {
                verifier.VerifyIL("C..ctor()",
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
                verifier.VerifyIL("C..ctor()",
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

        [Theory]
        [CombinatorialData]
        public void ConstructorConflict([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C(int a, string b)
    {
    }
}");

            comp.VerifyDiagnostics(
                // (5,12): error CS0111: Type 'C' already defines a member called 'C' with the same parameter types
                //     public C(int a, string b)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "C").WithArguments("C", "C").WithLocation(5, 12),
                // (5,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C(int a, int b) // overload
    {
    }
}");

            comp.VerifyDiagnostics(
                // (5,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    public C() // overload
    {
    }
}");

            comp.VerifyDiagnostics(
                // (5,12): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
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
        [WorkItem("https://github.com/dotnet/roslyn/issues/68345")]
        public void ConstructorOverloading_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
" + keyword + @" C(int x, string y)
{
    internal C(C other) // overload
    {
    }
}");

            comp.VerifyDiagnostics(
                // (5,14): error CS8862: A constructor declared in a type with parameter list must have 'this' constructor initializer.
                //     internal C(C other) // overload
                Diagnostic(ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord, "C").WithLocation(5, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void Members_01([CombinatorialValues("class", "struct")] string keyword)
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

partial " + keyword + @" C(int X, int Y)
{
}

partial " + keyword + @" C(int U, int V)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,17): error CS8863: Only a single partial type declaration may have a parameter list
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

partial " + keyword + @" C(int X, int Y)
{
}

partial " + keyword + @" C(int U)
{
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,17): error CS8863: Only a single partial type declaration may have a parameter list
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
    public Base(int X) {}

    public Base(long X) {}

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

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

            var comp = (CSharpCompilation)verifier.Compilation;

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

            Assert.Empty(((SynthesizedPrimaryConstructor)symbol.GetSymbol().ContainingSymbol).GetCapturedParameters());
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

" + keyword + @" C(int X)
{
    static int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,20): error CS9105: Cannot use primary constructor parameter 'int X' in this context.
                //     static int Z = X + 1;
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "X").WithArguments("int X").WithLocation(6, 20)
                );

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
        }

        [Theory]
        [CombinatorialData]
        public void Initializers_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

" + keyword + @" C(int X)
{
    const int Z = X + 1;
}";

            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (6,19): error CS9105: Cannot use primary constructor parameter 'int X' in this context.
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "X").WithArguments("int X").WithLocation(6, 19),
                // (6,19): error CS0133: The expression being assigned to 'C.Z' must be constant
                //     const int Z = X + 1;
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "X + 1").WithArguments("C.Z").WithLocation(6, 19)
                );

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

" + keyword + @" R3(ref int P3)
{
    public int P3 {get;} = (P3 = 1);
}

struct S
{
    public int F;
}
" + keyword + @" R4(ref S P4)
{
    int F = (P4.F = 1);
}

";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "(43, 44)").VerifyDiagnostics(
                // (17,19): warning CS9113: Parameter 'P2' is unread.
                // class  R2(ref int P2);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "P2").WithArguments("P2").WithLocation(17, 19),
                // (19,19): warning CS9113: Parameter 'P3' is unread.
                // class  R3(ref int P3)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "P3").WithArguments("P3").WithLocation(19, 19),
                // (28,17): warning CS9113: Parameter 'P4' is unread.
                // class  R4(ref S P4)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "P4").WithArguments("P4").WithLocation(28, 17)
                );
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
            CompileAndVerify(comp, expectedOutput: "42").VerifyDiagnostics();
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

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
        public void AttributesOnPrimaryConstructorParameters_03([CombinatorialValues("class ", "struct")] string keyword)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

public " + keyword + @" Test1(
    [field: A]
    int P1)
{
    int M1() => P1;
}

[System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true) ]
public class C : System.Attribute
{
}

public " + keyword + @" Test2(
    [C]
    int P1)
{
    int M1() => P1;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,6): warning CS0657: 'field' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'param'. All attributes in this block will be ignored.
                //     [field: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "field").WithArguments("field", "param").WithLocation(8, 6),
                // (20,6): error CS0592: Attribute 'C' is not valid on this declaration type. It is only valid on 'field' declarations.
                //     [C]
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "C").WithArguments("C", "field").WithLocation(20, 6)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Test1").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single().Parameters[0].GetAttributes());
            Assert.Equal(1, comp.GetTypeByMetadataName("Test2").InstanceConstructors.Where(c => !c.IsDefaultValueTypeConstructor()).Single().Parameters[0].GetAttributes().Count());
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructorParameters_09_CallerMemberName([CombinatorialValues("class ", "struct")] string keyword)
        {
            string source = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_01([CombinatorialValues("class", "struct", "record", "record class", "record struct")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[method: A]
" + declaration + @" C
    ();
";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyDiagnostics();
            verify(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);

            if (declaration is "class" or "struct")
            {
                comp.VerifyDiagnostics(
                    // (9,5): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                    //     ();
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "()").WithArguments("primary constructors", "12.0").WithLocation(9, 5)
                    );
            }
            else
            {
                comp.VerifyDiagnostics(
                    // (7,2): error CS9058: Feature 'primary constructors' is not available in C# 11.0. Please use language version 12.0 or greater.
                    // [method: A]
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "method").WithArguments("primary constructors", "12.0").WithLocation(7, 2)
                    );
            }

            verify(comp);

            static void verify(CSharpCompilation comp)
            {
                var c = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C");
                Assert.Empty(c.GetAttributes());
                Assert.True(c.HasPrimaryConstructor);
                Assert.Equal("A", c.PrimaryConstructor.GetAttributes().Single().ToString());
                Assert.True(c.Constructors.Where(ctor => ctor != c.PrimaryConstructor).All(ctor => ctor.GetAttributes().IsEmpty));
            }
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_02([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[return: A]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type, method'. All attributes in this block will be ignored.
                // [return: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type, method").WithLocation(7, 2)
                );

            var c = comp.GetTypeByMetadataName("C");
            Assert.Empty(c.GetAttributes());
            Assert.True(c.Constructors.All(ctor => ctor.GetAttributes().IsEmpty));
            Assert.True(c.Constructors.All(ctor => ctor.GetReturnTypeAttributes().IsEmpty));
        }

        [Fact]
        public void AttributesOnPrimaryConstructor_03()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[method: A]
[return: A]
interface I();
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(7, 2),
                // (8,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [return: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type").WithLocation(8, 2),
                // (9,12): error CS9122: Unexpected parameter list.
                // interface I();
                Diagnostic(ErrorCode.ERR_UnexpectedParameterList, "()").WithLocation(9, 12)
                );

            var i = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("I");
            Assert.Empty(i.GetAttributes());
            Assert.False(i.HasPrimaryConstructor);
            Assert.Null(i.PrimaryConstructor);
            Assert.Empty(i.Constructors);
        }

        [Fact]
        public void AttributesOnPrimaryConstructor_04()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[method: A]
[return: A]
enum E();
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(7, 2),
                // (8,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [return: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type").WithLocation(8, 2),
                // (9,7): error CS1514: { expected
                // enum E();
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(9, 7),
                // (9,7): error CS1513: } expected
                // enum E();
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(9, 7),
                // (9,7): error CS8803: Top-level statements must precede namespace and type declarations.
                // enum E();
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "();").WithLocation(9, 7),
                // (9,8): error CS1525: Invalid expression term ')'
                // enum E();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 8)
                );

            var e = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("E");
            Assert.Empty(e.GetAttributes());
            Assert.False(e.HasPrimaryConstructor);
            Assert.Null(e.PrimaryConstructor);
            Assert.True(e.Constructors.All(ctor => ctor.GetAttributes().IsEmpty));
            Assert.True(e.Constructors.All(ctor => ctor.GetReturnTypeAttributes().IsEmpty));
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_05([CombinatorialValues("class C;", "struct C;", "record C;", "record class C;", "record struct C;", "interface C;", "enum C;")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[method: A]
[return: A]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(7, 2),
                // (8,2): warning CS0657: 'return' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [return: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "return").WithArguments("return", "type").WithLocation(8, 2)
                );

            var c = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C");
            Assert.Empty(c.GetAttributes());
            Assert.False(c.HasPrimaryConstructor);
            Assert.Null(c.PrimaryConstructor);
            Assert.True(c.Constructors.All(ctor => ctor.GetAttributes().IsEmpty));
            Assert.True(c.Constructors.All(ctor => ctor.GetReturnTypeAttributes().IsEmpty));
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_06([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[type: A]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            Assert.Equal("A", c.GetAttributes().Single().ToString());
            Assert.True(c.Constructors.All(ctor => ctor.GetAttributes().IsEmpty));
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_07([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[A]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var c = comp.GetTypeByMetadataName("C");
            Assert.Equal("A", c.GetAttributes().Single().ToString());
            Assert.True(c.Constructors.All(ctor => ctor.GetAttributes().IsEmpty));
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_08([CombinatorialValues("class", "struct", "record", "record class", "record struct")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[method: A]
partial " + declaration + @" C1();

[method: B]
partial " + declaration + @" C1;

[method: B]
partial " + declaration + @" C2;

[method: A]
partial " + declaration + @" C2();
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (15,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: B]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(15, 2),
                // (18,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: B]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(18, 2)
                );

            var c1 = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C1");
            Assert.Empty(c1.GetAttributes());
            Assert.True(c1.HasPrimaryConstructor);
            Assert.Equal("A", c1.PrimaryConstructor.GetAttributes().Single().ToString());
            Assert.True(c1.Constructors.Where(ctor => ctor != c1.PrimaryConstructor).All(ctor => ctor.GetAttributes().IsEmpty));

            var c2 = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C2");
            Assert.Empty(c2.GetAttributes());
            Assert.True(c2.HasPrimaryConstructor);
            Assert.Equal("A", c2.PrimaryConstructor.GetAttributes().Single().ToString());
            Assert.True(c2.Constructors.Where(ctor => ctor != c2.PrimaryConstructor).All(ctor => ctor.GetAttributes().IsEmpty));
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_09([CombinatorialValues("class", "struct", "record", "record class", "record struct")] string declaration)
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
}

[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class B : System.Attribute
{
}

[method: A]
partial " + declaration + @" C1();

#line 100
[method: B]
partial " + declaration + @" C1
#line 200
    ();

[method: B]
partial " + declaration + @" C2();

#line 300
[method: A]
partial " + declaration + @" C2
#line 400
    ();
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (100,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: B]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(100, 2),
                // (200,5): error CS8863: Only a single partial type declaration may have a parameter list
                //     ();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(200, 5),
                // (300,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: A]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(300, 2),
                // (400,5): error CS8863: Only a single partial type declaration may have a parameter list
                //     ();
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "()").WithLocation(400, 5)
                );

            var c1 = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C1");
            Assert.Empty(c1.GetAttributes());
            Assert.True(c1.HasPrimaryConstructor);
            Assert.Equal("A", c1.PrimaryConstructor.GetAttributes().Single().ToString());
            Assert.True(c1.Constructors.Where(ctor => ctor != c1.PrimaryConstructor).All(ctor => ctor.GetAttributes().IsEmpty));

            var c2 = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C2");
            Assert.Empty(c2.GetAttributes());
            Assert.True(c2.HasPrimaryConstructor);
            Assert.Equal("B", c2.PrimaryConstructor.GetAttributes().Single().ToString());
            Assert.True(c2.Constructors.Where(ctor => ctor != c2.PrimaryConstructor).All(ctor => ctor.GetAttributes().IsEmpty));
        }

        [Fact]
        public void AttributesOnPrimaryConstructor_10_NameofParameter()
        {
            string source = @"
[System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true) ]
public class A : System.Attribute
{
    public A(string x){}
}

[method: A(nameof(someParam))]
class C(int someParam)
{
    int X = someParam;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var c = (SourceNamedTypeSymbol)comp.GetTypeByMetadataName("C");
            Assert.Equal(@"A(""someParam"")", c.PrimaryConstructor.GetAttributes().Single().ToString());
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68349")]
        public void AttributesOnPrimaryConstructor_11([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
_ = new C();

[method: System.Obsolete(""Obsolete!!!"", error: true)]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,5): error CS0619: 'C.C()' is obsolete: 'Obsolete!!!'
                // _ = new C();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new C()").WithArguments("C.C()", "Obsolete!!!").WithLocation(2, 5)
                );

            var c = (SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C");
            Assert.True(c.AnyMemberHasAttributes);
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_12([CombinatorialValues("class", "struct", "record", "record class", "record struct")] string declaration)
        {
            string source = @"
_ = new C1();

partial " + declaration + @" C1();

#line 100
[method: System.Obsolete(""Obsolete!!!"", error: true)]
partial " + declaration + @" C1
#line 200
    ;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (100,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: System.Obsolete("Obsolete!!!", error: true)]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(100, 2)
                );

            var c1 = (SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C1");
            Assert.False(c1.AnyMemberHasAttributes);
        }

        [Theory]
        [CombinatorialData]
        public void AttributesOnPrimaryConstructor_13([CombinatorialValues("class", "struct", "record", "record class", "record struct")] string declaration)
        {
            string source = @"
_ = new C1();

[method: System.Obsolete(""Obsolete!!!"", error: true)]
partial " + declaration + @" C1();

partial " + declaration + @" C1;
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,5): error CS0619: 'C1.C1()' is obsolete: 'Obsolete!!!'
                // _ = new C1();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "new C1()").WithArguments("C1.C1()", "Obsolete!!!").WithLocation(2, 5)
                );

            var c1 = (SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C1");
            Assert.True(c1.AnyMemberHasAttributes);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68349")]
        public void AttributesOnPrimaryConstructor_14([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
_ = new C();

[System.Obsolete(""Obsolete!!!"", error: true)]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,9): error CS0619: 'C' is obsolete: 'Obsolete!!!'
                // _ = new C();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "Obsolete!!!").WithLocation(2, 9)
                );

            var c = (SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C");
            Assert.False(c.AnyMemberHasAttributes);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68349")]
        public void AttributesOnPrimaryConstructor_15([CombinatorialValues("class C();", "struct C();", "record C();", "record class C();", "record struct C();")] string declaration)
        {
            string source = @"
_ = new C();

[type: System.Obsolete(""Obsolete!!!"", error: true)]
" + declaration + @"
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,9): error CS0619: 'C' is obsolete: 'Obsolete!!!'
                // _ = new C();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "Obsolete!!!").WithLocation(2, 9)
                );

            var c = (SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("C");
            Assert.False(c.AnyMemberHasAttributes);
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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

        [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1931501")]
        public void XmlDoc_InsideType(
            [CombinatorialValues("class ", "struct")] string keyword,
            [CombinatorialValues("x", "p")] string identifier,
            [CombinatorialValues("param", "paramref")] string tag)
        {
            var source = $$"""
                {{keyword}} C(int p)
                {
                    /// <{{tag}} name="{{identifier}}"></{{tag}}>
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (1,14): warning CS9113: Parameter 'p' is unread.
                // struct C(int p)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p").WithArguments("p").WithLocation(1, 14),
                // (3,5): warning CS1587: XML comment is not placed on a valid language element
                //     /// <param name="x"></param>
                Diagnostic(ErrorCode.WRN_UnprocessedXMLComment, "/").WithLocation(3, 5));

            var tree = comp.SyntaxTrees.Single();
            var doc = tree.GetRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().Single();
            var x = doc.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
            Assert.Equal(identifier, x.Identifier.ValueText);

            var model = comp.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(x);
            Assert.Null(symbolInfo.Symbol);
            Assert.True(symbolInfo.IsEmpty);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Empty(symbolInfo.CandidateSymbols);
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Cref([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for <see cref=""I1""/></param>
public " + keyword + @" C(int I1)
{
    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    public void M1(int x) { }

    /// <summary>Summary</summary>
    /// <param name=""x"">Description for <see cref=""x""/></param>
    /// <param name=""y""/>
    public void M2(int y) { }

    /// <summary>Summary</summary>
    /// <param name=""I1"">Description for <see cref=""I1""/></param>
    /// <param name=""z""/>
    public void M3(int z) { }
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
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(7, 52),
                // (11,22): warning CS1572: XML comment has a param tag for 'x', but there is no parameter by that name
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "x").WithArguments("x").WithLocation(11, 22),
                // (11,52): warning CS1574: XML comment has cref attribute 'x' that could not be resolved
                //     /// <param name="x">Description for <see cref="x"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "x").WithArguments("x").WithLocation(11, 52),
                // (16,22): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                //     /// <param name="I1">Description for <see cref="I1"/></param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(16, 22),
                // (16,53): warning CS1574: XML comment has cref attribute 'I1' that could not be resolved
                //     /// <param name="I1">Description for <see cref="I1"/></param>
                Diagnostic(ErrorCode.WRN_BadXMLRef, "I1").WithArguments("I1").WithLocation(16, 53)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_Error([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
        public void XmlDoc_ParamRef_InsideType([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
/// <summary></summary>
public " + keyword + @" C(int I1)
{
    /// <summary>Summary <paramref name=""I1""/></summary>
    void M1(int x) {}

    /// <summary>Summary <paramref name=""x""/></summary>
    void M2(int y) {}
}
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (5,42): warning CS1734: XML comment on 'C.M1(int)' has a paramref tag for 'I1', but there is no parameter by that name
                //     /// <summary>Summary <paramref name="I1"/></summary>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "I1").WithArguments("I1", "C.M1(int)").WithLocation(5, 42),
                // (8,42): warning CS1734: XML comment on 'C.M2(int)' has a paramref tag for 'x', but there is no parameter by that name
                //     /// <summary>Summary <paramref name="x"/></summary>
                Diagnostic(ErrorCode.WRN_UnmatchedParamRefTag, "x").WithArguments("x", "C.M2(int)").WithLocation(8, 42)
                );
        }

        [Theory]
        [CombinatorialData]
        public void XmlDoc_WithExplicitProperty([CombinatorialValues("class ", "struct")] string keyword)
        {
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
public partial " + keyword + @" C(int I1);

/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" C(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            comp.VerifyDiagnostics(
                // (2,23): warning CS1591: Missing XML comment for publicly visible type or member 'C.C(int)'
                // public partial class  C(int I1);
                Diagnostic(ErrorCode.WRN_MissingXMLComment, "C").WithArguments("C.C(int)").WithLocation(2, 23),
                // (5,18): warning CS1572: XML comment has a param tag for 'I1', but there is no parameter by that name
                // /// <param name="I1">Description for I1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "I1").WithArguments("I1").WithLocation(5, 18),
                // (6,24): error CS8863: Only a single partial type declaration may have a parameter list
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
/// <summary>Summary</summary>
/// <param name=""I1"">Description for I1</param>
public partial " + keyword + @" D(int I1);

public partial " + keyword + @" D(int I1);
";

            var comp = CreateCompilation(src, parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));
            comp.VerifyDiagnostics(
                // (6,24): error CS8863: Only a single partial type declaration may have a parameter list
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
                // (8,24): error CS8863: Only a single partial type declaration may have a parameter list
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
                // (6,24): error CS8863: Only a single partial type declaration may have a parameter list
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
                // (8,24): error CS8863: Only a single partial type declaration may have a parameter list
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
            var src = @"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'x' is unread.

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
            var source = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

public class Base(int X);
[Attr]
public class Derived(int X) : Base(X);

class Attr : System.Attribute {}
";

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
            var source = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
                
public class Base(int X);
[Attr()]
public class Derived() : Base(M(out var y))
{
    static int M(out int y) => y = 1;
}

class Attr : System.Attribute {}
";

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
@"#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.
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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter is unread.

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
                Diagnostic(ErrorCode.ERR_EscapeStackAlloc, "stackalloc int[512]").WithArguments("System.Span<int>").WithLocation(5, 50),
                // (5,50): error CS8347: Cannot use a result of 'Span<int>.implicit operator ReadOnlySpan<int>(Span<int>)' in this context because it may expose variables referenced by parameter 'span' outside of their declaration scope
                //     public ReadOnlySpan<int> Property { get; } = stackalloc int[512];
                Diagnostic(ErrorCode.ERR_EscapeCall, "stackalloc int[512]").WithArguments("System.Span<int>.implicit operator System.ReadOnlySpan<int>(System.Span<int>)", "span").WithLocation(5, 50));
        }

        public static IEnumerable<object[]> ParameterScope_MemberData()
        {
            var data = new (string tag, TestFlags flags, string nestedSource)[]
                {
                    // Simple references in members    
                    ("0001", Success | Shadows, "public int F = p1;"),
                    ("0002", Success | Shadows, "public int P {get;} = p1;"),
                    ("0003", Success | Shadows, "public event System.Action E = () => p1.ToString();"),
                    ("0004", BadReference | NotUsedWarning | Shadows, "public static int F = p1;"),
                    ("0005", BadReference | NotUsedWarning | Shadows | BadConstant, "public const int F = p1;"),
                    ("0006", BadReference | NotUsedWarning | Shadows, "public static int P {get;} = p1;"),
                    ("0007", BadReference | NotUsedWarning | Shadows, "public static event System.Action E = () => p1.ToString();"),
                    ("0008", BadReference | NotUsedWarning, "static C1() { p1 = 0; }"),
                    ("0009", BadReference | NotUsedWarning, "static void M() { p1 = 0; }"),
                    ("0011", BadReference | NotUsedWarning, "static int P { get { return p1; } }"),
                    ("0012", BadReference | NotUsedWarning, "static int P { set { p1 = 0; } }"),
                    ("0013", BadReference | NotUsedWarning, "static int P { set {} get { return p1; } }"),
                    ("0014", BadReference | NotUsedWarning, "static int P { get => 0; set { p1 = 0; } }"),
                    ("0015", BadReference | NotUsedWarning, "static event System.Action E { add { p1 = 0; } remove {} }"),
                    ("0016", BadReference | NotUsedWarning, "static event System.Action E { add {} remove { p1 = 0; } }"),
                    ("0017", Captured | Success, "void M() { p1 = 0; }"),
                    ("0018", Captured | Success, "int P { get { return p1; } }"),
                    ("0019", Captured | Success, "int P { set { p1 = 0; } }"),
                    ("0020", Captured | Success, "int P { set {} get { return p1; } }"),
                    ("0021", Captured | Success, "int P { get => 0; set { p1 = 0; } }"),
                    ("0022", Captured | Success, "event System.Action E { add { p1 = 0; } remove {} }"),
                    ("0023", Captured | Success, "event System.Action E { add {} remove { p1 = 0; } }"),
                    ("0024", Captured | Success, "int this[int x] { get { return p1; } }"),
                    ("0025", Captured | Success, "int this[int x] { set { p1 = 0; } }"),
                    ("0026", Captured | Success, "int this[int x] { set {} get { return p1; } }"),
                    ("0027", Captured | Success, "int this[int x] { get => 0; set { p1 = 0; } }"),
                    ("0028", Captured | Success, "~C1() { p1 = 0; }"),
                    ("0029", Captured | BadReference, "public C1() : this(p1) {}"),

                    // Same in a nested type
                    ("0101", BadReference | NotUsedWarning, "class Nested { public int F = p1; }"),
                    ("0102", BadReference | NotUsedWarning, "class Nested { public int P {get;} = p1; }"),
                    ("0103", BadReference | NotUsedWarning, "class Nested { public event System.Action E = () => p1.ToString(); }"),
                    ("0104", BadReference | NotUsedWarning, "class Nested { public static int F = p1; }"),
                    ("0106", BadReference | NotUsedWarning, "class Nested { public static int P {get;} = p1; }"),
                    ("0107", BadReference | NotUsedWarning, "class Nested { public static event System.Action E = () => p1.ToString(); }"),
                    ("0108", BadReference | NotUsedWarning, "class Nested { static Nested() { p1 = 0; } }"),
                    ("0109", BadReference | NotUsedWarning, "class Nested { static void M() { p1 = 0; } }"),
                    ("0111", BadReference | NotUsedWarning, "class Nested { static int P { get { return p1; } } }"),
                    ("0112", BadReference | NotUsedWarning, "class Nested { static int P { set { p1 = 0; } } }"),
                    ("0113", BadReference | NotUsedWarning, "class Nested { static int P { set {} get { return p1; } } }"),
                    ("0114", BadReference | NotUsedWarning, "class Nested { static int P { get => 0; set { p1 = 0; } } }"),
                    ("0115", BadReference | NotUsedWarning, "class Nested { static event System.Action E { add { p1 = 0; } remove {} } }"),
                    ("0116", BadReference | NotUsedWarning, "class Nested { static event System.Action E { add {} remove { p1 = 0; } } }"),
                    ("0117", BadReference | NotUsedWarning, "class Nested { void M() { p1 = 0; } }"),
                    ("0118", BadReference | NotUsedWarning, "class Nested { int P { get { return p1; } } }"),
                    ("0119", BadReference | NotUsedWarning, "class Nested { int P { set { p1 = 0; } } }"),
                    ("0120", BadReference | NotUsedWarning, "class Nested { int P { set {} get { return p1; } } }"),
                    ("0121", BadReference | NotUsedWarning, "class Nested { int P { get => 0; set { p1 = 0; } } }"),
                    ("0122", BadReference | NotUsedWarning, "class Nested { event System.Action E { add { p1 = 0; } remove {} } }"),
                    ("0123", BadReference | NotUsedWarning, "class Nested { event System.Action E { add {} remove { p1 = 0; } } }"),
                    ("0124", BadReference | NotUsedWarning, "class Nested { int this[int x] { get { return p1; } } }"),
                    ("0125", BadReference | NotUsedWarning, "class Nested { int this[int x] { set { p1 = 0; } } }"),
                    ("0126", BadReference | NotUsedWarning, "class Nested { int this[int x] { set {} get { return p1; } } }"),
                    ("0127", BadReference | NotUsedWarning, "class Nested { int this[int x] { get => 0; set { p1 = 0; } } }"),
                    ("0128", BadReference | NotUsedWarning, "class Nested { ~Nested() { p1 = 0; } }"),
                    ("0129", BadReference | NotUsedWarning, "class Nested { public Nested() : this(p1) {} Nested(int x) {} }"),
                    ("0130", BadReference | NotUsedWarning, "class Nested { public Nested() { p1 = 0; } }"),

                    // In attributes on members
                    ("0301", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public int F = 0;"),
                    ("0302", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public int P {get;} = 0;"),
                    ("0303", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public event System.Action E = () => 0.ToString();"),
                    ("0304", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public static int F = 0;"),
                    ("0305", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public const int F = 0;"),
                    ("0306", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public static int P {get;} = 0;"),
                    ("0307", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public static event System.Action E = () => 0.ToString();"),
                    ("0308", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static C1() {}"),
                    ("0309", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static void M() {}"),
                    ("0311", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static int P { get { return 0; } }"),
                    ("0312", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static int P { set {} }"),
                    ("0313", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static int P { set {} get { return 0; } }"),
                    ("0314", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static int P { get => 0; set {} }"),
                    ("0315", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static event System.Action E { add {} remove {} }"),
                    ("0316", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] static event System.Action E { add {} remove {} }"),
                    ("0317", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] void M() {}"),
                    ("0318", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int P { get { return 0; } }"),
                    ("0319", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int P { set {} }"),
                    ("0320", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int P { set {} get { return 0; } }"),
                    ("0321", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int P { get => 0; set {} }"),
                    ("0322", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] event System.Action E { add {} remove {} }"),
                    ("0323", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] event System.Action E { add {} remove {} }"),
                    ("0324", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int this[int x] { get { return 0; } }"),
                    ("0325", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int this[int x] { set {} }"),
                    ("0326", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int this[int x] { set {} get { return 0; } }"),
                    ("0327", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] int this[int x] { get => 0; set {} }"),
                    ("0328", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] ~C1() {}"),
                    ("0329", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] public C1() : this(0) {}"),
                    ("0330", BadReference | NotUsedWarning | BadAttributeValue, "static int P { [Attr1(p1)] get { return 0; } }"),
                    ("0331", BadReference | NotUsedWarning | BadAttributeValue, "static int P { [Attr1(p1)] set {} }"),
                    ("0332", BadReference | NotUsedWarning | BadAttributeValue, "static event System.Action E { [Attr1(p1)] add {} remove {} }"),
                    ("0333", BadReference | NotUsedWarning | BadAttributeValue, "static event System.Action E { add {} [Attr1(p1)] remove {} }"),
                    ("0334", BadReference | NotUsedWarning | BadAttributeValue, "[Attr1(p1)] class Nested {}"),
                    ("0335", BadReference | NotUsedWarning | BadAttributeValue, "class Nested([Attr1(p1)] int p2) { public int F = p2; }"),

                    // Same in nested type
                    ("0401", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public int F = 0; }"),
                    ("0402", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public int P {get;} = 0; }"),
                    ("0403", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public event System.Action E = () => 0.ToString(); }"),
                    ("0404", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public static int F = 0; }"),
                    ("0406", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public static int P {get;} = 0; }"),
                    ("0407", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public static event System.Action E = () => 0.ToString(); }"),
                    ("0408", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static Nested() {} }"),
                    ("0409", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static void M() {} }"),
                    ("0411", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static int P { get { return 0; } } }"),
                    ("0412", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static int P { set {} } }"),
                    ("0413", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static int P { set {} get { return 0; } } }"),
                    ("0414", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static int P { get => 0; set {} } }"),
                    ("0415", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static event System.Action E { add {} remove {} } }"),
                    ("0416", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] static event System.Action E { add {} remove {} } }"),
                    ("0417", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] void M() {} }"),
                    ("0418", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int P { get { return 0; } } }"),
                    ("0419", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int P { set {} } }"),
                    ("0420", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int P { set {} get { return 0; } } }"),
                    ("0421", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int P { get => 0; set {} } }"),
                    ("0422", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] event System.Action E { add {} remove {} } }"),
                    ("0423", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] event System.Action E { add {} remove {} } }"),
                    ("0424", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int this[int x] { get { return 0; } } }"),
                    ("0425", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int this[int x] { set {} } }"),
                    ("0426", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int this[int x] { set {} get { return 0; } } }"),
                    ("0427", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] int this[int x] { get => 0; set {} } }"),
                    ("0428", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] ~Nested() {} }"),
                    ("0430", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] public Nested() {} }"),
                    ("0431", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { static int P { [Attr1(p1)] get { return 0; } } }"),
                    ("0432", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { static int P { [Attr1(p1)] set {} } }"),
                    ("0433", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { static event System.Action E { [Attr1(p1)] add {} remove {} } }"),
                    ("0434", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { static event System.Action E { add {} [Attr1(p1)] remove {} } }"),
                    ("0435", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { [Attr1(p1)] class Nested2 {} }"),
                    ("0436", BadReference | NotUsedWarning | BadAttributeValue, "class Nested { class Nested2([Attr1(p1)] int p2) { public int F = p2; } }"),

                    // In nameof in attributes on members
                    ("0501", NotUsedWarning, "[Attr1(nameof(p1))] public int F = 0;"),
                    ("0502", NotUsedWarning, "[Attr1(nameof(p1))] public int P {get;} = 0;"),
                    ("0503", NotUsedWarning, "[Attr1(nameof(p1))] public event System.Action E = () => 0.ToString();"),
                    ("0504", NotUsedWarning, "[Attr1(nameof(p1))] public static int F = 0;"),
                    ("0505", NotUsedWarning, "[Attr1(nameof(p1))] public const int F = 0;"),
                    ("0506", NotUsedWarning, "[Attr1(nameof(p1))] public static int P {get;} = 0;"),
                    ("0507", NotUsedWarning, "[Attr1(nameof(p1))] public static event System.Action E = () => 0.ToString();"),
                    ("0508", NotUsedWarning, "[Attr1(nameof(p1))] static C1() {}"),
                    ("0509", NotUsedWarning, "[Attr1(nameof(p1))] static void M() {}"),
                    ("0511", NotUsedWarning, "[Attr1(nameof(p1))] static int P { get { return 0; } }"),
                    ("0512", NotUsedWarning, "[Attr1(nameof(p1))] static int P { set {} }"),
                    ("0513", NotUsedWarning, "[Attr1(nameof(p1))] static int P { set {} get { return 0; } }"),
                    ("0514", NotUsedWarning, "[Attr1(nameof(p1))] static int P { get => 0; set {} }"),
                    ("0515", NotUsedWarning, "[Attr1(nameof(p1))] static event System.Action E { add {} remove {} }"),
                    ("0516", NotUsedWarning, "[Attr1(nameof(p1))] static event System.Action E { add {} remove {} }"),
                    ("0517", NotUsedWarning, "[Attr1(nameof(p1))] void M() {}"),
                    ("0518", NotUsedWarning, "[Attr1(nameof(p1))] int P { get { return 0; } }"),
                    ("0519", NotUsedWarning, "[Attr1(nameof(p1))] int P { set {} }"),
                    ("0520", NotUsedWarning, "[Attr1(nameof(p1))] int P { set {} get { return 0; } }"),
                    ("0521", NotUsedWarning, "[Attr1(nameof(p1))] int P { get => 0; set {} }"),
                    ("0522", NotUsedWarning, "[Attr1(nameof(p1))] event System.Action E { add {} remove {} }"),
                    ("0523", NotUsedWarning, "[Attr1(nameof(p1))] event System.Action E { add {} remove {} }"),
                    ("0524", NotUsedWarning, "[Attr1(nameof(p1))] int this[int x] { get { return 0; } }"),
                    ("0525", NotUsedWarning, "[Attr1(nameof(p1))] int this[int x] { set {} }"),
                    ("0526", NotUsedWarning, "[Attr1(nameof(p1))] int this[int x] { set {} get { return 0; } }"),
                    ("0527", NotUsedWarning, "[Attr1(nameof(p1))] int this[int x] { get => 0; set {} }"),
                    ("0528", NotUsedWarning, "[Attr1(nameof(p1))] ~C1() {}"),
                    ("0529", NotUsedWarning, "[Attr1(nameof(p1))] public C1() : this(0) {}"),
                    ("0530", NotUsedWarning, "static int P { [Attr1(nameof(p1))] get { return 0; } }"),
                    ("0531", NotUsedWarning, "static int P { [Attr1(nameof(p1))] set {} }"),
                    ("0532", NotUsedWarning, "static event System.Action E { [Attr1(nameof(p1))] add {} remove {} }"),
                    ("0533", NotUsedWarning, "static event System.Action E { add {} [Attr1(nameof(p1))] remove {} }"),
                    ("0534", NotUsedWarning, "[Attr1(nameof(p1))] class Nested {}"),
                    ("0535", NotUsedWarning, "class Nested([Attr1(nameof(p1))] int p2) { public int F = p2; }"),

                    // Same in nested type
                    ("0601", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public int F = 0; }"),
                    ("0602", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public int P {get;} = 0; }"),
                    ("0603", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public event System.Action E = () => 0.ToString(); }"),
                    ("0604", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public static int F = 0; }"),
                    ("0606", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public static int P {get;} = 0; }"),
                    ("0607", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public static event System.Action E = () => 0.ToString(); }"),
                    ("0608", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static Nested() {} }"),
                    ("0609", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static void M() {} }"),
                    ("0611", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static int P { get { return 0; } } }"),
                    ("0612", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static int P { set {} } }"),
                    ("0613", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static int P { set {} get { return 0; } } }"),
                    ("0614", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static int P { get => 0; set {} } }"),
                    ("0615", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static event System.Action E { add {} remove {} } }"),
                    ("0616", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] static event System.Action E { add {} remove {} } }"),
                    ("0617", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] void M() {} }"),
                    ("0618", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int P { get { return 0; } } }"),
                    ("0619", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int P { set {} } }"),
                    ("0620", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int P { set {} get { return 0; } } }"),
                    ("0621", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int P { get => 0; set {} } }"),
                    ("0622", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] event System.Action E { add {} remove {} } }"),
                    ("0623", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] event System.Action E { add {} remove {} } }"),
                    ("0624", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int this[int x] { get { return 0; } } }"),
                    ("0625", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int this[int x] { set {} } }"),
                    ("0626", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int this[int x] { set {} get { return 0; } } }"),
                    ("0627", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] int this[int x] { get => 0; set {} } }"),
                    ("0628", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] ~Nested() {} }"),
                    ("0630", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] public Nested() {} }"),
                    ("0631", NotUsedWarning, "class Nested { static int P { [Attr1(nameof(p1))] get { return 0; } } }"),
                    ("0632", NotUsedWarning, "class Nested { static int P { [Attr1(nameof(p1))] set {} } }"),
                    ("0633", NotUsedWarning, "class Nested { static event System.Action E { [Attr1(nameof(p1))] add {} remove {} } }"),
                    ("0634", NotUsedWarning, "class Nested { static event System.Action E { add {} [Attr1(nameof(p1))] remove {} } }"),
                    ("0635", NotUsedWarning, "class Nested { [Attr1(nameof(p1))] class Nested2 {} }"),
                    ("0636", NotUsedWarning, "class Nested { class Nested2([Attr1(nameof(p1))] int p2) { public int F = p2; } }"),

                    // In default parameter values in members
                    ("0709", BadReference | NotUsedWarning | BadDefaultValue, "static void M(int x = p1) {}"),
                    ("0717", BadReference | NotUsedWarning | BadDefaultValue, "void M(int x = p1) {}"),
                    ("0724", BadReference | NotUsedWarning | BadDefaultValue, "int this[int y, int x = p1] { get { return x; } }"),
                    ("0730", BadReference | NotUsedWarning | BadDefaultValue, "public C1(int y, int x = p1) : this(0) {}"),
                    ("0731", BadReference | NotUsedWarning | BadDefaultValue, "class Nested(int y, int x = p1) { public int F = x + y; }"),

                    // Same in nested type
                    ("0809", BadReference | NotUsedWarning | BadDefaultValue, "class Nested { static void M(int x = p1) {} }"),
                    ("0817", BadReference | NotUsedWarning | BadDefaultValue, "class Nested { void M(int x = p1) {} }"),
                    ("0824", BadReference | NotUsedWarning | BadDefaultValue, "class Nested { int this[int y, int x = p1] { get { return x; } } }"),
                    ("0830", BadReference | NotUsedWarning | BadDefaultValue, "class Nested { public Nested(int x = p1) {} }"),
                    ("0831", BadReference | NotUsedWarning | BadDefaultValue, "class Nested { class Nested2(int y, int x = p1) { public int F = x + y; } }"),

                    // In nameof default parameter values in members
                    ("0909", NotUsedWarning, "static void M(string x = nameof(p1)) {}"),
                    ("0917", NotUsedWarning, "void M(string x = nameof(p1)) {}"),
                    ("0924", NotUsedWarning, "int this[int y, string x = nameof(p1)] { get { return y; } }"),
                    ("0930", NotUsedWarning, "public C1(int y, string x = nameof(p1)) : this(0) {}"),
                    ("0931", NotUsedWarning, "class Nested(int y, string x = nameof(p1)) { public int F = x.Length + y; }"),

                    // Same in nested type
                    ("1009", NotUsedWarning, "class Nested { static void M(string x = nameof(p1)) {} }"),
                    ("1017", NotUsedWarning, "class Nested { void M(string x = nameof(p1)) {} }"),
                    ("1024", NotUsedWarning, "class Nested { int this[int y, string x = nameof(p1)] { get { return y; } } }"),
                    ("1030", NotUsedWarning, "class Nested { public Nested(string x = nameof(p1)) {} }"),
                    ("1031", NotUsedWarning, "class Nested { class Nested2(int y, string x = nameof(p1)) { public int F = x.Length + y; } }"),

                    // In lambdas

                    ("1101", Captured | BadReference | InNestedMethod, "public C1() : this(() => p1) {} C1(System.Func<int> x) : this(0) {}"),
                    ("1102", Captured | BadReference | InNestedMethod, "public C1() : this(() => (System.Func<int>)(() => p1)) {} C1(System.Func<System.Func<int>> x) : this(0) {}"),
                    ("1103", Captured | BadReference | InNestedMethod, "public C1() : this(() => { return local(); int local() => p1; }) {} C1(System.Func<int> x) : this(0) {}"),

                    ("1401", Success | Shadows, "public System.Func<int> F = (() => p1);"),
                    ("1402", Success | Shadows, "public System.Func<System.Func<int>> F = (() => (System.Func<int>)(() => p1));"),
                    ("1403", Success | Shadows, "public System.Func<int> F = () => { return local(); int local() => p1; };"),
                    ("1404", Success | Shadows, "public System.Func<int> P {get;} = (() => p1);"),
                    ("1405", Success | Shadows, "public System.Func<System.Func<int>> P {get;} = (() => (System.Func<int>)(() => p1));"),
                    ("1406", Success | Shadows, "public System.Func<int> P {get;} = () => { return local(); int local() => p1; };"),
                    ("1407", Success | Shadows, "public event System.Func<System.Func<int>> E = (() => (System.Func<int>)(() => p1));"),
                    ("1408", Success | Shadows, "public event System.Func<int> E = () => { return local(); int local() => p1; };"),
                    ("1409", Captured | InNestedMethod, "public System.Func<int> M() { return (() => p1); }"),
                    ("1410", Captured | InNestedMethod, "public System.Func<System.Func<int>> M() { return (() => (System.Func<int>)(() => p1)); }"),
                    ("1411", Captured | InNestedMethod, "public System.Func<int> M() { return () => { return local(); int local() => p1; }; }"),
                    ("1412", Captured | InNestedMethod, "public void M() { local(); int local() { return p1; } }"),
                    ("1413", Captured | InNestedMethod, "public void M() { local1(); void local1() { local2(); int local2() { return p1; } } }"),

                    // In expression body
                    ("1502", Captured | Success, "public int P => p1;"),
                    ("1506", BadReference | NotUsedWarning, "public static int P => p1;"),
                    ("1508", BadReference | NotUsedWarning, "static C1() => p1 = 0;"),
                    ("1509", BadReference | NotUsedWarning, "static void M() => p1 = 0;"),
                    ("1511", BadReference | NotUsedWarning, "static int P { get => p1; }"),
                    ("1512", BadReference | NotUsedWarning, "static int P { set => p1 = 0; }"),
                    ("1513", BadReference | NotUsedWarning, "static int P { set {} get => p1; }"),
                    ("1514", BadReference | NotUsedWarning, "static int P { get => 0; set => p1 = 0; }"),
                    ("1515", BadReference | NotUsedWarning, "static event System.Action E { add => p1 = 0; remove {} }"),
                    ("1516", BadReference | NotUsedWarning, "static event System.Action E { add {} remove => p1 = 0; }"),
                    ("1517", Captured | Success, "void M() => p1 = 0;"),
                    ("1518", Captured | Success, "int P { get => p1; }"),
                    ("1519", Captured | Success, "int P { set => p1 = 0; }"),
                    ("1520", Captured | Success, "int P { set {} get => p1; }"),
                    ("1521", Captured | Success, "int P { get => 0; set => p1 = 0; }"),
                    ("1524", Captured | Success, "int this[int x] { get => p1; }"),
                    ("1525", Captured | Success, "int this[int x] { set => p1 = 0; }"),
                    ("1526", Captured | Success, "int this[int x] { set {} get => p1; }"),
                    ("1527", Captured | Success, "int this[int x] { get => 0; set => p1 = 0; }"),
                    ("1528", Captured | Success, "~C1() => p1 = 0;"),

                    ("1602", BadReference | NotUsedWarning, "class Nested { public int P => p1; }"),
                    ("1606", BadReference | NotUsedWarning, "class Nested { public static int P => p1; }"),
                    ("1608", BadReference | NotUsedWarning, "class Nested { static Nested() => p1 = 0; }"),
                    ("1609", BadReference | NotUsedWarning, "class Nested { static void M() => p1 = 0; }"),
                    ("1611", BadReference | NotUsedWarning, "class Nested { static int P { get => p1; } }"),
                    ("1612", BadReference | NotUsedWarning, "class Nested { static int P { set => p1 = 0; } }"),
                    ("1613", BadReference | NotUsedWarning, "class Nested { static int P { set {} get => p1; } }"),
                    ("1614", BadReference | NotUsedWarning, "class Nested { static int P { get => 0; set => p1 = 0; } }"),
                    ("1617", BadReference | NotUsedWarning, "class Nested { void M() => p1 = 0; }"),
                    ("1618", BadReference | NotUsedWarning, "class Nested { int P { get => p1; } }"),
                    ("1619", BadReference | NotUsedWarning, "class Nested { int P { set => p1 = 0; } }"),
                    ("1620", BadReference | NotUsedWarning, "class Nested { int P { set {} get => p1; } }"),
                    ("1621", BadReference | NotUsedWarning, "class Nested { int P { get => 0; set => p1 = 0; } }"),
                    ("1624", BadReference | NotUsedWarning, "class Nested { int this[int x] { get => p1; } }"),
                    ("1625", BadReference | NotUsedWarning, "class Nested { int this[int x] { set => p1 = 0; } }"),
                    ("1626", BadReference | NotUsedWarning, "class Nested { int this[int x] { set {} get => p1; } }"),
                    ("1627", BadReference | NotUsedWarning, "class Nested { int this[int x] { get => 0; set => p1 = 0; } }"),
                    ("1628", BadReference | NotUsedWarning, "class Nested { ~Nested() => p1 = 0; }"),
                    ("1629", BadReference | NotUsedWarning, "class Nested { public Nested() : this(p1) {} Nested(int x) {} }"),
                    ("1630", BadReference | NotUsedWarning, "class Nested { public Nested() => p1 = 0; }"),

                    // In expression body when block body is also present
                    ("1708", NotUsedWarning | TwoBodies, "static C1() {} => p1 = 0;"),
                    ("1709", NotUsedWarning | TwoBodies, "static void M() {} => p1 = 0;"),
                    ("1711", NotUsedWarning | TwoBodies, "static int P { get { return 0; } => p1; }"),
                    ("1712", NotUsedWarning | TwoBodies, "static int P { set {} => p1 = 0; }"),
                    ("1713", NotUsedWarning | TwoBodies, "static int P { set {} get { return 0; } => p1; }"),
                    ("1714", NotUsedWarning | TwoBodies, "static int P { get => 0; set {} => p1 = 0; }"),
                    ("1715", NotUsedWarning | TwoBodies, "static event System.Action E { add {} => p1 = 0; remove {} }"),
                    ("1716", NotUsedWarning | TwoBodies, "static event System.Action E { add {} remove {} => p1 = 0; }"),
                    ("1717", Captured | TwoBodies, "void M() {} => p1 = 0;"),
                    ("1718", Captured | TwoBodies, "int P { get { return 0; } => p1; }"),
                    ("1719", Captured | TwoBodies, "int P { set {} => p1 = 0; }"),
                    ("1720", Captured | TwoBodies, "int P { set {} get { return 0; } => p1; }"),
                    ("1721", Captured | TwoBodies, "int P { get => 0; set {} => p1 = 0; }"),
                    ("1724", Captured | TwoBodies, "int this[int x] { get { return 0; } => p1; }"),
                    ("1725", Captured | TwoBodies, "int this[int x] { set {} => p1 = 0; }"),
                    ("1726", Captured | TwoBodies, "int this[int x] { set {} get { return 0; } => p1; }"),
                    ("1727", Captured | TwoBodies, "int this[int x] { get => 0; set {} => p1 = 0; }"),
                    ("1728", Captured | TwoBodies, "~C1() {} => p1 = 0;"),
                    ("1730", Captured | TwoBodies, "public C1() : this(0) {} => p1 = 0;"),

                    ("1808", NotUsedWarning | TwoBodies, "class Nested { static Nested() {} => p1 = 0; }"),
                    ("1809", NotUsedWarning | TwoBodies, "class Nested { static void M() {} => p1 = 0; }"),
                    ("1811", NotUsedWarning | TwoBodies, "class Nested { static int P { get { return 0; } => p1; } }"),
                    ("1812", NotUsedWarning | TwoBodies, "class Nested { static int P { set {} => p1 = 0; } }"),
                    ("1813", NotUsedWarning | TwoBodies, "class Nested { static int P { set {} get { return 0; } => p1; } }"),
                    ("1814", NotUsedWarning | TwoBodies, "class Nested { static int P { get => 0; set {} => p1 = 0; } }"),
                    ("1817", NotUsedWarning | TwoBodies, "class Nested { void M() {} => p1 = 0; }"),
                    ("1818", NotUsedWarning | TwoBodies, "class Nested { int P { get { return 0; } => p1; } }"),
                    ("1819", NotUsedWarning | TwoBodies, "class Nested { int P { set {} => p1 = 0; } }"),
                    ("1820", NotUsedWarning | TwoBodies, "class Nested { int P { set {} get { return 0; } => p1; } }"),
                    ("1821", NotUsedWarning | TwoBodies, "class Nested { int P { get => 0; set {} => p1 = 0; } }"),
                    ("1824", NotUsedWarning | TwoBodies, "class Nested { int this[int x] { get { return 0; } => p1; } }"),
                    ("1825", NotUsedWarning | TwoBodies, "class Nested { int this[int x] { set {} => p1 = 0; } }"),
                    ("1826", NotUsedWarning | TwoBodies, "class Nested { int this[int x] { set {} get { return 0; } => p1; } }"),
                    ("1827", NotUsedWarning | TwoBodies, "class Nested { int this[int x] { get => 0; set {} => p1 = 0; } }"),
                    ("1828", NotUsedWarning | TwoBodies, "class Nested { ~Nested() {} => p1 = 0; }"),
                    ("1830", NotUsedWarning | TwoBodies, "class Nested { public Nested() {} => p1 = 0; }"),

                    // In attributes inside method bodies
                    ("1901", NotUsedWarning | AttributesNotAllowed | Shadows, "public System.Action F = () => { [Attr1(p1)] return; };"),
                    ("1902", NotUsedWarning | AttributesNotAllowed | Shadows, "public System.Action P {get;} = () => { [Attr1(p1)] return; };"),
                    ("1903", NotUsedWarning | AttributesNotAllowed | Shadows, "public event System.Action E = () => { [Attr1(p1)] return; };"),
                    ("1904", NotUsedWarning | AttributesNotAllowed | Shadows, "public static System.Action F = () => { [Attr1(p1)] return; };"),
                    ("1906", NotUsedWarning | AttributesNotAllowed | Shadows, "public static System.Action P {get;} = () => { [Attr1(p1)] return; };"),
                    ("1907", NotUsedWarning | AttributesNotAllowed | Shadows, "public static event System.Action E = () => { [Attr1(p1)] return; };"),
                    ("1908", NotUsedWarning | AttributesNotAllowed, "static C1() { [Attr1(p1)] return; }"),
                    ("1909", NotUsedWarning | AttributesNotAllowed, "static void M() { [Attr1(p1)] return; }"),
                    ("1911", NotUsedWarning | AttributesNotAllowed, "static int P { get {  [Attr1(p1)] return 0; } }"),
                    ("1912", NotUsedWarning | AttributesNotAllowed, "static int P { set { [Attr1(p1)] return; } }"),
                    ("1913", NotUsedWarning | AttributesNotAllowed, "static int P { set {} get {  [Attr1(p1)] return 0; } }"),
                    ("1914", NotUsedWarning | AttributesNotAllowed, "static int P { get => 0; set { [Attr1(p1)] return; } }"),
                    ("1915", NotUsedWarning | AttributesNotAllowed, "static event System.Action E { add { [Attr1(p1)] return; } remove {} }"),
                    ("1916", NotUsedWarning | AttributesNotAllowed, "static event System.Action E { add {} remove { [Attr1(p1)] return; } }"),
                    ("1917", NotUsedWarning | AttributesNotAllowed, "void M() { [Attr1(p1)] return; }"),
                    ("1918", NotUsedWarning | AttributesNotAllowed, "int P { get {  [Attr1(p1)] return 0; } }"),
                    ("1919", NotUsedWarning | AttributesNotAllowed, "int P { set { [Attr1(p1)] return; } }"),
                    ("1920", NotUsedWarning | AttributesNotAllowed, "int P { set {} get {  [Attr1(p1)] return 0; } }"),
                    ("1921", NotUsedWarning | AttributesNotAllowed, "int P { get => 0; set { [Attr1(p1)] return; } }"),
                    ("1922", NotUsedWarning | AttributesNotAllowed, "event System.Action E { add { [Attr1(p1)] return; } remove {} }"),
                    ("1923", NotUsedWarning | AttributesNotAllowed, "event System.Action E { add {} remove { [Attr1(p1)] return; } }"),
                    ("1924", NotUsedWarning | AttributesNotAllowed, "int this[int x] { get {  [Attr1(p1)] return 0; } }"),
                    ("1925", NotUsedWarning | AttributesNotAllowed, "int this[int x] { set { [Attr1(p1)] return; } }"),
                    ("1926", NotUsedWarning | AttributesNotAllowed, "int this[int x] { set {} get { [Attr1(p1)] return 0; } }"),
                    ("1927", NotUsedWarning | AttributesNotAllowed, "int this[int x] { get => 0; set { [Attr1(p1)] return; } }"),
                    ("1928", NotUsedWarning | AttributesNotAllowed, "~C1() { [Attr1(p1)] return; }"),
                    ("1929", NotUsedWarning | AttributesNotAllowed, "public C1() : this(() => { [Attr1(p1)] return; }) {} C1(System.Action x) : this(0) {}"),
                    ("1930", NotUsedWarning | AttributesNotAllowed, "public C1() : this(0) { [Attr1(p1)] return; }"),
                    ("1935", NotUsedWarning | AttributesNotAllowed, "class Nested() : NestedBase(() => { [Attr1(p1)] return; }) {} class NestedBase(System.Action x) { object F = x; } "),

                    ("2001", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public System.Action F = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2002", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public System.Action P {get;} = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2003", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public event System.Action E = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2004", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public static System.Action F = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2006", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public static System.Action P {get;} = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2007", BadReference | NotUsedWarning | BadAttributeValue | Shadows, "public static event System.Action E = () => { [Attr1(p1)] void local(){} local(); };"),
                    ("2008", BadReference | NotUsedWarning | BadAttributeValue, "static C1() { [Attr1(p1)] void local(){} local(); }"),
                    ("2009", BadReference | NotUsedWarning | BadAttributeValue, "static void M() { [Attr1(p1)] void local(){} local(); }"),
                    ("2011", BadReference | NotUsedWarning | BadAttributeValue, "static int P { get {  [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2012", BadReference | NotUsedWarning | BadAttributeValue, "static int P { set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2013", BadReference | NotUsedWarning | BadAttributeValue, "static int P { set {} get {  [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2014", BadReference | NotUsedWarning | BadAttributeValue, "static int P { get => 0; set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2015", BadReference | NotUsedWarning | BadAttributeValue, "static event System.Action E { add { [Attr1(p1)] void local(){} local(); } remove {} }"),
                    ("2016", BadReference | NotUsedWarning | BadAttributeValue, "static event System.Action E { add {} remove { [Attr1(p1)] void local(){} local(); } }"),
                    ("2017", BadReference | NotUsedWarning | BadAttributeValue, "void M() { [Attr1(p1)] void local(){} local(); }"),
                    ("2018", BadReference | NotUsedWarning | BadAttributeValue, "int P { get {  [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2019", BadReference | NotUsedWarning | BadAttributeValue, "int P { set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2020", BadReference | NotUsedWarning | BadAttributeValue, "int P { set {} get {  [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2021", BadReference | NotUsedWarning | BadAttributeValue, "int P { get => 0; set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2022", BadReference | NotUsedWarning | BadAttributeValue, "event System.Action E { add { [Attr1(p1)] void local(){} local(); } remove {} }"),
                    ("2023", BadReference | NotUsedWarning | BadAttributeValue, "event System.Action E { add {} remove { [Attr1(p1)] void local(){} local(); } }"),
                    ("2024", BadReference | NotUsedWarning | BadAttributeValue, "int this[int x] { get { [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2025", BadReference | NotUsedWarning | BadAttributeValue, "int this[int x] { set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2026", BadReference | NotUsedWarning | BadAttributeValue, "int this[int x] { set {} get { [Attr1(p1)] void local(){} local(); return 0; } }"),
                    ("2027", BadReference | NotUsedWarning | BadAttributeValue, "int this[int x] { get => 0; set { [Attr1(p1)] void local(){} local(); } }"),
                    ("2028", BadReference | NotUsedWarning | BadAttributeValue, "~C1() { [Attr1(p1)] void local(){} local(); }"),
                    ("2029", BadReference | NotUsedWarning | BadAttributeValue, "public C1() : this(() => { [Attr1(p1)] void local(){} local(); }) {} C1(System.Action x) : this(0) {}"),
                    ("2030", BadReference | NotUsedWarning | BadAttributeValue, "public C1() : this(0) { [Attr1(p1)] void local(){} local(); }"),
                    ("2035", BadReference | NotUsedWarning | BadAttributeValue, "class Nested() : NestedBase(() => { [Attr1(p1)] void local(){} local(); }) {} class NestedBase(System.Action x) { object F = x; } "),

                    // Same with nameof
                    ("2101", Success | Shadows, "public System.Action F = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2102", Success | Shadows, "public System.Action P {get;} = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2103", Success | Shadows, "public event System.Action E = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2104", NotUsedWarning | Shadows, "public static System.Action F = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2106", NotUsedWarning | Shadows, "public static System.Action P {get;} = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2107", NotUsedWarning | Shadows, "public static event System.Action E = () => { [Attr1(nameof(p1))] void local(){} local(); };"),
                    ("2108", NotUsedWarning, "static C1() { [Attr1(nameof(p1))] void local(){} local(); }"),
                    ("2109", NotUsedWarning, "static void M() { [Attr1(nameof(p1))] void local(){} local(); }"),
                    ("2111", NotUsedWarning, "static int P { get {  [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2112", NotUsedWarning, "static int P { set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2113", NotUsedWarning, "static int P { set {} get {  [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2114", NotUsedWarning, "static int P { get => 0; set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2115", NotUsedWarning, "static event System.Action E { add { [Attr1(nameof(p1))] void local(){} local(); } remove {} }"),
                    ("2116", NotUsedWarning, "static event System.Action E { add {} remove { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2117", NotUsedWarning, "void M() { [Attr1(nameof(p1))] void local(){} local(); }"),
                    ("2118", NotUsedWarning, "int P { get {  [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2119", NotUsedWarning, "int P { set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2120", NotUsedWarning, "int P { set {} get {  [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2121", NotUsedWarning, "int P { get => 0; set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2122", NotUsedWarning, "event System.Action E { add { [Attr1(nameof(p1))] void local(){} local(); } remove {} }"),
                    ("2123", NotUsedWarning, "event System.Action E { add {} remove { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2124", NotUsedWarning, "int this[int x] { get { [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2125", NotUsedWarning, "int this[int x] { set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2126", NotUsedWarning, "int this[int x] { set {} get { [Attr1(nameof(p1))] void local(){} local(); return 0; } }"),
                    ("2127", NotUsedWarning, "int this[int x] { get => 0; set { [Attr1(nameof(p1))] void local(){} local(); } }"),
                    ("2128", NotUsedWarning, "~C1() { [Attr1(nameof(p1))] void local(){} local(); }"),
                    ("2129", NotUsedWarning, "public C1() : this(() => { [Attr1(nameof(p1))] void local(){} local(); }) {} C1(System.Action x) : this(0) {}"),
                    ("2130", NotUsedWarning, "public C1() : this(0) { [Attr1(nameof(p1))] void local(){} local(); }"),
                    ("2135", NotUsedWarning, "class Nested() : NestedBase(() => { [Attr1(nameof(p1))] void local(){} local(); }) {} class NestedBase(System.Action x) { object F = x; } "),

                    // In parameter default values in method bodies
                    ("2201", BadReference | BadDefaultValue | Shadows, "public System.Action F = () => { void local(int x = p1){} local(); };"),
                    ("2202", BadReference | BadDefaultValue | Shadows, "public System.Action P {get;} = () => { void local(int x = p1){} local(); };"),
                    ("2203", BadReference | BadDefaultValue | Shadows, "public event System.Action E = () => { void local(int x = p1){} local(); };"),
                    ("2204", BadReference | NotUsedWarning | BadDefaultValue | Shadows, "public static System.Action F = () => { void local(int x = p1){} local(); };"),
                    ("2206", BadReference | NotUsedWarning | BadDefaultValue | Shadows, "public static System.Action P {get;} = () => { void local(int x = p1){} local(); };"),
                    ("2207", BadReference | NotUsedWarning | BadDefaultValue | Shadows, "public static event System.Action E = () => { void local(int x = p1){} local(); };"),
                    ("2208", BadReference | NotUsedWarning | BadDefaultValue, "static C1() { void local(int x = p1){} local(); }"),
                    ("2209", BadReference | NotUsedWarning | BadDefaultValue, "static void M() { void local(int x = p1){} local(); }"),
                    ("2211", BadReference | NotUsedWarning | BadDefaultValue, "static int P { get {  void local(int x = p1){} local(); return 0; } }"),
                    ("2212", BadReference | NotUsedWarning | BadDefaultValue, "static int P { set { void local(int x = p1){} local(); } }"),
                    ("2213", BadReference | NotUsedWarning | BadDefaultValue, "static int P { set {} get {  void local(int x = p1){} local(); return 0; } }"),
                    ("2214", BadReference | NotUsedWarning | BadDefaultValue, "static int P { get => 0; set { void local(int x = p1){} local(); } }"),
                    ("2215", BadReference | NotUsedWarning | BadDefaultValue, "static event System.Action E { add { void local(int x = p1){} local(); } remove {} }"),
                    ("2216", BadReference | NotUsedWarning | BadDefaultValue, "static event System.Action E { add {} remove { void local(int x = p1){} local(); } }"),
                    ("2217", BadReference | NotUsedWarning | BadDefaultValue, "void M() { void local(int x = p1){} local(); }"),
                    ("2218", BadReference | NotUsedWarning | BadDefaultValue, "int P { get {  void local(int x = p1){} local(); return 0; } }"),
                    ("2219", BadReference | NotUsedWarning | BadDefaultValue, "int P { set { void local(int x = p1){} local(); } }"),
                    ("2220", BadReference | NotUsedWarning | BadDefaultValue, "int P { set {} get {  void local(int x = p1){} local(); return 0; } }"),
                    ("2221", BadReference | NotUsedWarning | BadDefaultValue, "int P { get => 0; set { void local(int x = p1){} local(); } }"),
                    ("2222", BadReference | NotUsedWarning | BadDefaultValue, "event System.Action E { add { void local(int x = p1){} local(); } remove {} }"),
                    ("2223", BadReference | NotUsedWarning | BadDefaultValue, "event System.Action E { add {} remove { void local(int x = p1){} local(); } }"),
                    ("2224", BadReference | NotUsedWarning | BadDefaultValue, "int this[int x] { get { void local(int x = p1){} local(); return 0; } }"),
                    ("2225", BadReference | NotUsedWarning | BadDefaultValue, "int this[int x] { set { void local(int x = p1){} local(); } }"),
                    ("2226", BadReference | NotUsedWarning | BadDefaultValue, "int this[int x] { set {} get { void local(int x = p1){} local(); return 0; } }"),
                    ("2227", BadReference | NotUsedWarning | BadDefaultValue, "int this[int x] { get => 0; set { void local(int x = p1){} local(); } }"),
                    ("2228", BadReference | NotUsedWarning | BadDefaultValue, "~C1() { void local(int x = p1){} local(); }"),
                    ("2229", BadReference | NotUsedWarning | BadDefaultValue, "public C1() : this(() => { void local(int x = p1){} local(); }) {} C1(System.Action x) : this(0) {}"),
                    ("2230", BadReference | NotUsedWarning | BadDefaultValue, "public C1() : this(0) { void local(int x = p1){} local(); }"),
                    ("2235", BadReference | NotUsedWarning | BadDefaultValue, "class Nested() : NestedBase(() => { void local(int x = p1){} local(); }) {} class NestedBase(System.Action x) { object F = x; } "),

                    // Same with nameof
                    ("2301", Success | Shadows, "public System.Action F = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2302", Success | Shadows, "public System.Action P {get;} = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2303", Success | Shadows, "public event System.Action E = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2304", NotUsedWarning | Shadows, "public static System.Action F = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2306", NotUsedWarning | Shadows, "public static System.Action P {get;} = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2307", NotUsedWarning | Shadows, "public static event System.Action E = () => { void local(string x = nameof(p1)){} local(); };"),
                    ("2308", NotUsedWarning, "static C1() { void local(string x = nameof(p1)){} local(); }"),
                    ("2309", NotUsedWarning, "static void M() { void local(string x = nameof(p1)){} local(); }"),
                    ("2311", NotUsedWarning, "static int P { get {  void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2312", NotUsedWarning, "static int P { set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2313", NotUsedWarning, "static int P { set {} get {  void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2314", NotUsedWarning, "static int P { get => 0; set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2315", NotUsedWarning, "static event System.Action E { add { void local(string x = nameof(p1)){} local(); } remove {} }"),
                    ("2316", NotUsedWarning, "static event System.Action E { add {} remove { void local(string x = nameof(p1)){} local(); } }"),
                    ("2317", NotUsedWarning, "void M() { void local(string x = nameof(p1)){} local(); }"),
                    ("2318", NotUsedWarning, "int P { get {  void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2319", NotUsedWarning, "int P { set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2320", NotUsedWarning, "int P { set {} get {  void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2321", NotUsedWarning, "int P { get => 0; set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2322", NotUsedWarning, "event System.Action E { add { void local(string x = nameof(p1)){} local(); } remove {} }"),
                    ("2323", NotUsedWarning, "event System.Action E { add {} remove { void local(string x = nameof(p1)){} local(); } }"),
                    ("2324", NotUsedWarning, "int this[int x] { get { void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2325", NotUsedWarning, "int this[int x] { set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2326", NotUsedWarning, "int this[int x] { set {} get { void local(string x = nameof(p1)){} local(); return 0; } }"),
                    ("2327", NotUsedWarning, "int this[int x] { get => 0; set { void local(string x = nameof(p1)){} local(); } }"),
                    ("2328", NotUsedWarning, "~C1() { void local(string x = nameof(p1)){} local(); }"),
                    ("2329", NotUsedWarning, "public C1() : this(() => { void local(string x = nameof(p1)){} local(); }) {} C1(System.Action x) : this(0) {}"),
                    ("2330", NotUsedWarning, "public C1() : this(0) { void local(string x = nameof(p1)){} local(); }"),
                    ("2335", NotUsedWarning, "class Nested() : NestedBase(() => { void local(string x = nameof(p1)){} local(); }) {} class NestedBase(System.Action x) { object F = x; } "),

                    // References in nameof in members    
                    ("2401", Success | Shadows, "public int F = nameof(p1).Length;"),
                    ("2402", Success | Shadows, "public int P {get;} = nameof(p1).Length;"),
                    ("2403", Success | Shadows, "public event System.Action E = () => nameof(p1).Length.ToString();"),
                    ("2404", NotUsedWarning | Shadows, "public static int F = nameof(p1).Length;"),
                    ("2405", NotUsedWarning | Shadows, "public const string F = nameof(p1);"),
                    ("2406", NotUsedWarning | Shadows, "public static int P {get;} = nameof(p1).Length;"),
                    ("2407", NotUsedWarning | Shadows, "public static event System.Action E = () => nameof(p1).Length.ToString();"),
                    ("2408", NotUsedWarning, "static C1() { _ = nameof(p1); }"),
                    ("2409", NotUsedWarning, "static void M() { _ = nameof(p1); }"),
                    ("2411", NotUsedWarning, "static int P { get { return nameof(p1).Length; } }"),
                    ("2412", NotUsedWarning, "static int P { set { _ = nameof(p1); } }"),
                    ("2413", NotUsedWarning, "static int P { set {} get { return nameof(p1).Length; } }"),
                    ("2414", NotUsedWarning, "static int P { get => 0; set { _ = nameof(p1); } }"),
                    ("2415", NotUsedWarning, "static event System.Action E { add { _ = nameof(p1); } remove {} }"),
                    ("2416", NotUsedWarning, "static event System.Action E { add {} remove { _ = nameof(p1); } }"),
                    ("2417", NotUsedWarning, "void M() { _ = nameof(p1); }"),
                    ("2418", NotUsedWarning, "int P { get { return nameof(p1).Length; } }"),
                    ("2419", NotUsedWarning, "int P { set { _ = nameof(p1); } }"),
                    ("2420", NotUsedWarning, "int P { set {} get { return nameof(p1).Length; } }"),
                    ("2421", NotUsedWarning, "int P { get => 0; set { _ = nameof(p1); } }"),
                    ("2422", NotUsedWarning, "event System.Action E { add { _ = nameof(p1); } remove {} }"),
                    ("2423", NotUsedWarning, "event System.Action E { add {} remove { _ = nameof(p1); } }"),
                    ("2424", NotUsedWarning, "int this[int x] { get { return nameof(p1).Length; } }"),
                    ("2425", NotUsedWarning, "int this[int x] { set { _ = nameof(p1); } }"),
                    ("2426", NotUsedWarning, "int this[int x] { set {} get { return nameof(p1).Length; } }"),
                    ("2427", NotUsedWarning, "int this[int x] { get => 0; set { _ = nameof(p1); } }"),
                    ("2428", NotUsedWarning, "~C1() { _ = nameof(p1); }"),
                    ("2429", NotUsedWarning, "public C1() : this(nameof(p1).Length) {}"),

                    // Same in a nested type
                    ("2501", NotUsedWarning, "class Nested { public int F = nameof(p1).Length; }"),
                    ("2502", NotUsedWarning, "class Nested { public int P {get;} = nameof(p1).Length; }"),
                    ("2503", NotUsedWarning, "class Nested { public event System.Action E = () => nameof(p1).Length.ToString(); }"),
                    ("2504", NotUsedWarning, "class Nested { public static int F = nameof(p1).Length; }"),
                    ("2506", NotUsedWarning, "class Nested { public static int P {get;} = nameof(p1).Length; }"),
                    ("2507", NotUsedWarning, "class Nested { public static event System.Action E = () => nameof(p1).Length.ToString(); }"),
                    ("2508", NotUsedWarning, "class Nested { static Nested() { _ = nameof(p1); } }"),
                    ("2509", NotUsedWarning, "class Nested { static void M() { _ = nameof(p1); } }"),
                    ("2511", NotUsedWarning, "class Nested { static int P { get { return nameof(p1).Length; } } }"),
                    ("2512", NotUsedWarning, "class Nested { static int P { set { _ = nameof(p1); } } }"),
                    ("2513", NotUsedWarning, "class Nested { static int P { set {} get { return nameof(p1).Length; } } }"),
                    ("2514", NotUsedWarning, "class Nested { static int P { get => 0; set { _ = nameof(p1); } } }"),
                    ("2515", NotUsedWarning, "class Nested { static event System.Action E { add { _ = nameof(p1); } remove {} } }"),
                    ("2516", NotUsedWarning, "class Nested { static event System.Action E { add {} remove { _ = nameof(p1); } } }"),
                    ("2517", NotUsedWarning, "class Nested { void M() { _ = nameof(p1); } }"),
                    ("2518", NotUsedWarning, "class Nested { int P { get { return nameof(p1).Length; } } }"),
                    ("2519", NotUsedWarning, "class Nested { int P { set { _ = nameof(p1); } } }"),
                    ("2520", NotUsedWarning, "class Nested { int P { set {} get { return nameof(p1).Length; } } }"),
                    ("2521", NotUsedWarning, "class Nested { int P { get => 0; set { _ = nameof(p1); } } }"),
                    ("2522", NotUsedWarning, "class Nested { event System.Action E { add { _ = nameof(p1); } remove {} } }"),
                    ("2523", NotUsedWarning, "class Nested { event System.Action E { add {} remove { _ = nameof(p1); } } }"),
                    ("2524", NotUsedWarning, "class Nested { int this[int x] { get { return nameof(p1).Length; } } }"),
                    ("2525", NotUsedWarning, "class Nested { int this[int x] { set { _ = nameof(p1); } } }"),
                    ("2526", NotUsedWarning, "class Nested { int this[int x] { set {} get { return nameof(p1).Length; } } }"),
                    ("2527", NotUsedWarning, "class Nested { int this[int x] { get => 0; set { _ = nameof(p1); } } }"),
                    ("2528", NotUsedWarning, "class Nested { ~Nested() { _ = nameof(p1); } }"),
                    ("2529", NotUsedWarning, "class Nested { public Nested() : this(nameof(p1).Length) {} Nested(int x) {} }"),
                    ("2530", NotUsedWarning, "class Nested { public Nested() { _ = nameof(p1); } }"),
                };

            foreach (var keyword in new[] { "class", "struct" })
            {
                foreach (var isPartial in new[] { false, true })
                {
                    foreach (var isRecord in new[] { false, true })
                    {
                        foreach (var shadow in new[] { false, true })
                        {
                            foreach (var d in data)
                            {
                                yield return new object[] { keyword, shadow, isPartial, isRecord, d.tag, d.flags, d.nestedSource };
                            }
                        }

                        // Scenarios with instance constructors that have different expectations between shadow and regular scenario
                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10001", Captured | BadReference, "public C1() : this(0) { p1 = 0; }" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10002", Captured, "public C1() : this(0) { p1 = 0; }" };
                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10003", Captured | BadReference, "public C1() : this(0) => p1 = 0;" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10004", Captured, "public C1() : this(0) => p1 = 0;" };

                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10003", Captured | BadReference | InNestedMethod, "public C1() : this(0) { local(); int local() { return p1; } }" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10004", Captured | InNestedMethod, "public C1() : this(0) { local(); int local() { return p1; } }" };

                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10005", Captured | BadReference | InNestedMethod, "public C1() : this(0) { local1(); void local1() { local2(); int local2() { return p1; } } }" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10006", Captured | InNestedMethod, "public C1() : this(0) { local1(); void local1() { local2(); int local2() { return p1; } } }" };

                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10007", Captured | BadReference | InNestedMethod, "public C1() : this(0) => M(() => p1); void M(System.Func<int> x){}" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10008", Captured | InNestedMethod, "public C1() : this(0) => M(() => p1); void M(System.Func<int> x){}" };

                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10009", Captured | BadReference | InNestedMethod, "public C1() : this(0) => M(() => (System.Func<int>)(() => p1)); void M(System.Func<System.Func<int>> x){}" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10010", Captured | InNestedMethod, "public C1() : this(0) => M(() => (System.Func<int>)(() => p1)); void M(System.Func<System.Func<int>> x){}" };

                        yield return new object[] { keyword, /*shadow:*/ false, isPartial, isRecord, "10011", Captured | BadReference | InNestedMethod, "public C1() : this(0) => M(() => { return local(); int local() => p1; }); void M(System.Func<int> x){}" };
                        yield return new object[] { keyword, /*shadow:*/ true, isPartial, isRecord, "10012", Captured | InNestedMethod, "public C1() : this(0) => M(() => { return local(); int local() => p1; }); void M(System.Func<int> x){}" };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ParameterScope_MemberData))]
        public void ParameterScope(string keyword, bool shadow, bool isPartial, bool isRecord, string tag, TestFlags flags, string nestedSource)
        {
            _ = tag;

            string recordKeyword = "";

            if (isRecord)
            {
                if (!shadow)
                {
                    // Records always shadow
                    return;
                }

                recordKeyword = "record ";
            }

            string source;

            if (isPartial)
            {
                source = @"
partial " + recordKeyword + keyword + @" C1 (int
#line 1000
p1
);

partial " + recordKeyword + keyword + @" C1  
{
";
            }
            else
            {
                source = @"
" + recordKeyword + keyword + @" C1 (int
#line 1000
p1
)
{
";
            }

            source += nestedSource.Replace("p1", @"
#line 2000
p1
");
            if (shadow && !isRecord)
            {
                source += @"
int p1 { get; set; }
";
            }

            source += @"
}

class Attr1 : System.Attribute
{
    public Attr1(int x){} 
    public Attr1(string x){} 
}
";

            AssertParameterScope(keyword, shadow, isRecord, flags, source);
        }

        private static void AssertParameterScope(string keyword, bool shadow, bool isRecord, TestFlags flags, string source)
        {
            bool isCaptured = !shadow && (flags & TestFlags.Captured) != 0;

            if (isCaptured)
            {
                Assert.Equal((TestFlags)0, flags & TestFlags.NotUsedWarning);
            }

            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var p1 = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "p1").Single();

            var symbolInfo = model.GetSymbolInfo(p1);
            var symbol = symbolInfo.Symbol;

            if ((flags & TestFlags.NotInScope) != 0)
            {
                Assert.Null(symbol);
                Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
                Assert.Empty(symbolInfo.CandidateSymbols);

                Assert.Empty(model.LookupSymbols(p1.SpanStart, name: "p1"));
                Assert.DoesNotContain("p1", model.LookupNames(p1.SpanStart));
            }
            else
            {
                if (shadow && (flags & TestFlags.Shadows) == 0)
                {
                    if (symbol is null)
                    {
                        if (!isRecord || keyword == "struct" || symbolInfo.CandidateReason != CandidateReason.NotAVariable)
                        {
                            Assert.Equal(CandidateReason.StaticInstanceMismatch, symbolInfo.CandidateReason);
                        }

                        symbol = symbolInfo.CandidateSymbols.Single();
                    }

                    Assert.Equal(SymbolKind.Property, symbol.Kind);
                    Assert.Equal((!isRecord || keyword == "struct") ? "System.Int32 C1.p1 { get; set; }" : "System.Int32 C1.p1 { get; init; }", symbol.ToTestDisplayString());
                    Assert.Equal("C1", symbol.ContainingSymbol.ToTestDisplayString());
                }
                else
                {
                    Assert.Equal(SymbolKind.Parameter, symbol.Kind);
                    Assert.Equal("System.Int32 p1", symbol.ToTestDisplayString());
                    Assert.Equal("C1..ctor(System.Int32 p1)", symbol.ContainingSymbol.ToTestDisplayString());
                }

                Assert.Same(symbol, model.LookupSymbols(p1.SpanStart, name: "p1").Single());
                Assert.Contains("p1", model.LookupNames(p1.SpanStart));
            }

            var capturedParameters = comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters();

            if (isCaptured)
            {
                Assert.False(isRecord);
                Assert.Same(symbol.GetSymbol(), capturedParameters.Single().Key);
            }
            else
            {
                Assert.Empty(capturedParameters);
            }

            var diagnosticsToCheck = (IEnumerable<Diagnostic>)comp.GetEmitDiagnostics();

            var builder = ArrayBuilder<DiagnosticDescription>.GetInstance();

            if (!shadow || (flags & TestFlags.Shadows) != 0)
            {
                if (!isRecord && (flags & TestFlags.NotUsedWarning) != 0)
                {
                    builder.Add(
                        // (1000,1): warning CS9113: Parameter 'p1' is unread.
                        // p1
                        Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(1000, 1)
                        );
                }

                if ((flags & TestFlags.BadReference) != 0)
                {
                    builder.Add(
                        // (2000,1): error CS9105: Cannot use primary constructor parameter 'int p1' in this context.
                        // p1
                        Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p1").WithArguments("int p1").WithLocation(2000, 1)
                        );
                }

                if ((flags & TestFlags.BadAttributeValue) != 0)
                {
                    builder.Add(
                        // (2000,1): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                        // p1
                        Diagnostic(ErrorCode.ERR_BadAttributeArgument, "p1").WithLocation(2000, 1)
                        );
                }

                if ((flags & TestFlags.BadConstant) != 0)
                {
                    builder.Add(
                        // (2000,1): error CS0133: The expression being assigned to 'C1.F' must be constant
                        // p1
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, "p1").WithArguments("C1.F").WithLocation(2000, 1)
                        );
                }

                if ((flags & TestFlags.BadDefaultValue) != 0)
                {
                    builder.Add(
                        // (2000,1): error CS1736: Default parameter value for 'x' must be a compile-time constant
                        // p1
                        Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p1").WithArguments("x").WithLocation(2000, 1)
                        );
                }

                if ((flags & (TestFlags.InNestedMethod)) != 0 && (flags & TestFlags.BadReference) == 0 && keyword == "struct")
                {
                    builder.Add(
                        // (2000,1): error CS9111: Anonymous methods, lambda expressions, query expressions, and local functions inside an instance member of a struct cannot access primary constructor parameter
                        // p1
                        Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember, "p1").WithLocation(2000, 1)
                        );
                }
            }
            else
            {
                if ((flags & TestFlags.BadReference) != 0 &&
                    diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_ObjectRequired).Any())
                {
                    builder.Add(
                        // (2000,1): error CS0120: An object reference is required for the non-static field, method, or property 'C1.p1'
                        // p1
                        Diagnostic(ErrorCode.ERR_ObjectRequired, "p1").WithArguments("C1.p1").WithLocation(2000, 1)
                        );
                }

                if ((flags & TestFlags.BadDefaultValue) != 0 &&
                    diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_DefaultValueMustBeConstant).Any())
                {
                    builder.Add(
                        // (2000,1): error CS1736: Default parameter value for 'x' must be a compile-time constant
                        // p1
                        Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "p1").WithArguments("x").WithLocation(2000, 1)
                        );
                }

                // This logic is a work around https://github.com/dotnet/roslyn/issues/66049
                if ((flags & (TestFlags.BadDefaultValue)) != 0 &&
                    (flags & (TestFlags.InNestedMethod)) == 0 && keyword == "struct" &&
                         diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_ThisStructNotInAnonMeth).Any())
                {
                    builder.Add(
                        // (2000,1): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                        // p1
                        Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "p1").WithLocation(2000, 1)
                        );
                }

                if ((flags & (TestFlags.BadReference | TestFlags.BadDefaultValue | TestFlags.BadAttributeValue)) != 0)
                {
                    Assert.NotEmpty(builder);
                }

                if ((flags & (TestFlags.InNestedMethod)) != 0 && keyword == "struct" &&
                    diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_ThisStructNotInAnonMeth).Any())
                {
                    builder.Add(
                        // (2000,1): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                        // p1
                        Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "p1").WithLocation(2000, 1)
                        );
                }

                if (!isRecord && ((flags & TestFlags.NotUsedWarning) != 0 || (flags & TestFlags.Captured) != 0))
                {
                    builder.Add(
                        // (1000,1): warning CS9113: Parameter 'p1' is unread.
                        // p1
                        Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(1000, 1)
                        );
                }

                if ((flags & TestFlags.BadConstant) != 0)
                {
                    builder.Add(
                        // (2000,1): error CS0133: The expression being assigned to 'C1.F' must be constant
                        // p1
                        Diagnostic(ErrorCode.ERR_NotConstantExpression, "p1").WithArguments("C1.F").WithLocation(2000, 1)
                        );
                }
            }

            if ((flags & TestFlags.NotInScope) != 0)
            {
                builder.Add(
                    // (2000,1): error CS0103: The name 'p1' does not exist in the current context
                    // p1
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "p1").WithArguments("p1").WithLocation(2000, 1)
                    );
            }

            if (isRecord && keyword != "struct" &&
                diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_PredefinedTypeNotFound).Any())
            {
                builder.Add(
                    // (1000,1): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                    // p1
                    Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "p1").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(1000, 1)
                    );
            }

            if (symbolInfo.CandidateReason == CandidateReason.NotAVariable &&
                diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_AssignmentInitOnly).Any())
            {
                builder.Add(
                    // (2000,1): error CS8852: Init-only property or indexer 'C1.p1' can only be assigned in an object initializer, or on 'this' or 'base' in an instance constructor or an 'init' accessor.
                    // p1
                    Diagnostic(ErrorCode.ERR_AssignmentInitOnly, "p1").WithArguments("C1.p1").WithLocation(2000, 1)
                    );
            }

            if ((flags & TestFlags.TwoBodies) != 0)
            {
                Assert.Equal(1, diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_BlockBodyAndExpressionBody).Count());

                diagnosticsToCheck = diagnosticsToCheck.Where(d => d.Code is not (int)ErrorCode.ERR_BlockBodyAndExpressionBody);
            }

            if ((flags & TestFlags.AttributesNotAllowed) != 0)
            {
                Assert.Equal(1, diagnosticsToCheck.Where(d => d.Code is (int)ErrorCode.ERR_AttributesNotAllowed).Count());

                diagnosticsToCheck = diagnosticsToCheck.Where(d => d.Code is not (int)ErrorCode.ERR_AttributesNotAllowed);
            }

            diagnosticsToCheck.Where(d => d.Code is not ((int)ErrorCode.ERR_OnlyClassesCanContainDestructors)).
                 Verify(builder.ToArrayAndFree());
        }

        public static IEnumerable<object[]> ParameterScope_AttributesOnType_MemberData()
        {
            var data = new (string tag, TestFlags flags, string attribute)[]
                {
                    ("0001", BadReference | BadAttributeValue | NotUsedWarning, "[Attr1(p1)]"),
                    ("0002", NotUsedWarning, "[Attr1(nameof(p1))]"),
                };

            foreach (var keyword in new[] { "class", "struct" })
            {
                foreach (var isPartial in new[] { false, true })
                {
                    foreach (var isRecord in new[] { false, true })
                    {
                        foreach (var shadow in new[] { false, true })
                        {
                            foreach (var d in data)
                            {
                                yield return new object[] { keyword, shadow, isPartial, isRecord, d.tag, d.flags, d.attribute };
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ParameterScope_AttributesOnType_MemberData))]
        public void ParameterScope_AttributesOnType(string keyword, bool shadow, bool isPartial, bool isRecord, string tag, TestFlags flags, string attribute)
        {
            _ = tag;

            string recordKeyword = "";

            if (isRecord)
            {
                if (!shadow)
                {
                    // Records always shadow
                    return;
                }

                recordKeyword = "record ";
            }

            string source;

            if (isPartial)
            {
                source = @"
partial " + recordKeyword + keyword + @" C1 (int
#line 1000
p1
);
";
            }
            else
            {
                source = @"";
            }

            source += attribute.Replace("p1", @"
#line 2000
p1
");

            if (isPartial)
            {
                source += @"
partial " + recordKeyword + keyword + @" C1  
{
";
            }
            else
            {
                source += @"
" + recordKeyword + keyword + @" C1 (int
#line 1000
p1
)
{
";
            }

            if (shadow && !isRecord)
            {
                source += @"
int p1 { get; set; }
";
            }

            source += @"
}

class Attr1 : System.Attribute
{
    public Attr1(int x){} 
    public Attr1(string x){} 
}
";

            AssertParameterScope(keyword, shadow, isRecord, flags, source);
        }

        public static IEnumerable<object[]> ParameterScope_AttributesOnParameters_MemberData()
        {
            var data = new (string tag, TestFlags flags, string attribute)[]
                {
                    ("0001", NotInScope | NotUsedWarning, "[Attr1(p1)]"),
                    ("0002", NotUsedWarning | Shadows, "[Attr1(nameof(p1))]"),
                };

            foreach (var keyword in new[] { "class", "struct" })
            {
                foreach (var isRecord in new[] { false, true })
                {
                    foreach (var shadow in new[] { false, true })
                    {
                        foreach (var d in data)
                        {
                            yield return new object[] { keyword, shadow, isRecord, d.tag, d.flags, d.attribute };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ParameterScope_AttributesOnParameters_MemberData))]
        public void ParameterScope_AttributesOnParameters(string keyword, bool shadow, bool isRecord, string tag, TestFlags flags, string attribute)
        {
            _ = tag;

            string recordKeyword = "";

            if (isRecord)
            {
                if (!shadow)
                {
                    // Records always shadow
                    return;
                }

                recordKeyword = "record ";
            }

            string source = @"
" + recordKeyword + keyword + @" C1 (
";

            source += attribute.Replace("p1", @"
#line 2000
p1
");

            source += @"
int
#line 1000
p1
)
{
";

            if (shadow && !isRecord)
            {
                source += @"
int p1 { get; set; }
";
            }

            source += @"
}

class Attr1 : System.Attribute
{
    public Attr1(int x){} 
    public Attr1(string x){} 
}
";

            AssertParameterScope(keyword, shadow, isRecord, flags, source);
        }

        public static IEnumerable<object[]> ParameterScope_DefaultOnParameter_MemberData()
        {
            var data = new (string tag, TestFlags flags, string attribute)[]
                {
                    ("0001", NotInScope | NotUsedWarning, "p1"),
                    ("0002", NotInScope | NotUsedWarning, "nameof(p1)"),
                };

            foreach (var keyword in new[] { "class", "struct" })
            {
                foreach (var isRecord in new[] { false, true })
                {
                    foreach (var shadow in new[] { false, true })
                    {
                        foreach (var d in data)
                        {
                            yield return new object[] { keyword, shadow, isRecord, d.tag, d.flags, d.attribute };
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(ParameterScope_DefaultOnParameter_MemberData))]
        public void ParameterScope_DefaultOnParameter(string keyword, bool shadow, bool isRecord, string tag, TestFlags flags, string attribute)
        {
            _ = tag;

            string recordKeyword = "";

            if (isRecord)
            {
                if (!shadow)
                {
                    // Records always shadow
                    return;
                }

                recordKeyword = "record ";
            }

            string source = @"
" + recordKeyword + keyword + @" C1 (
int
#line 1000
p1
=
";

            source += attribute.Replace("p1", @"
#line 2000
p1
");

            source += @"
)
{
";

            if (shadow && !isRecord)
            {
                source += @"
int p1 { get; set; }
";
            }

            source += @"
}
";

            AssertParameterScope(keyword, shadow, isRecord, flags, source);
        }

        [Fact]
        public void ParameterScope_TypeParameterShadows_01()
        {
            var src = @"
class C1<T>(T a, int T) : Base(T)
{
    T A = a;
    int T1 = T;
    event System.Action T2 = T;
    int T3 {get;} = T;

    void M()
    {
        var x = T;
        T++;
    }
}

class Base
{
    public Base(int x){}
}
";
            var comp = CreateCompilation(src);

            // Even though specification for the feature says that enclosing type's type parameter should be found before 
            // primary constructor parameter, even in field initializers and base clause arguments, we already
            // have pre-existing behavior with records that is not consistent with that. Specifically, primary constructor
            // parameters come first in field initializers and base clause arguments for records.
            // Keeping this behavior for now.
            comp.VerifyDiagnostics(
                // (6,30): error CS0029: Cannot implicitly convert type 'int' to 'System.Action'
                //     event System.Action T2 = T;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "T").WithArguments("int", "System.Action").WithLocation(6, 30),
                // (11,17): error CS0119: 'T' is a type, which is not valid in the given context
                //         var x = T;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type").WithLocation(11, 17),
                // (12,9): error CS0118: 'T' is a type but is used like a variable
                //         T++;
                Diagnostic(ErrorCode.ERR_BadSKknown, "T").WithArguments("T", "type", "variable").WithLocation(12, 9)
                );
        }

        [Fact]
        public void ParameterScope_TypeParameterShadows_02()
        {
            var src = @"
class C1(int T)
{
    void M1()
    {
        T++;
    }

    void M2<T>()
    {
        T++; // M2
    }

    void M3()
    {
        local<int>();

        int local<T>() => T++;
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (11,9): error CS0118: 'T' is a type but is used like a variable
                //         T++; // M2
                Diagnostic(ErrorCode.ERR_BadSKknown, "T").WithArguments("T", "type", "variable").WithLocation(11, 9),
                // (18,27): error CS0118: 'T' is a type but is used like a variable
                //         int local<T>() => T++;
                Diagnostic(ErrorCode.ERR_BadSKknown, "T").WithArguments("T", "type", "variable").WithLocation(18, 27)
                );
        }

        [Fact]
        public void ParameterScope_ParameterShadows_01()
        {
            var src = @"
class C1(int p1) : Base(
#line 100
    (string p1) => p1++,
    () =>
    {
        local(string.Empty);
#line 200
        void local(string p1) => p1++;
    },
    p1
    )
{
#line 300
    System.Action<string> x = (string p1) => p1++;

    System.Action y = () =>
                      {
                          local(string.Empty);
#line 400
                          void local(string p1) => p1++;
                      };

    void M1(string p1)
    {
#line 500
        p1++;
    }

    void M2()
    {
#line 600
        System.Action<string> x = (string p1) => p1++;
        local(string.Empty);
#line 700
        void local(string p1) => p1++;
    }

    C1(string p1)
#line 800
        : this(p1++)
    {
#line 900
        p1++;
    }
}

class Base
{
    public Base(System.Action<string> x, System.Action y, int z){}
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (100,20): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //     (string p1) => p1++,
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(100, 20),
                // (200,34): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         void local(string p1) => p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(200, 34),
                // (300,46): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //     System.Action<string> x = (string p1) => p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(300, 46),
                // (400,52): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //                           void local(string p1) => p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(400, 52),
                // (500,9): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(500, 9),
                // (600,50): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         System.Action<string> x = (string p1) => p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(600, 50),
                // (700,34): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         void local(string p1) => p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(700, 34),
                // (800,16): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         : this(p1++)
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(800, 16),
                // (900,9): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(900, 9)
                );
        }

        [Fact]
        public void ParameterScope_LocalShadows_01()
        {
            var src = @"
class C1(int p1) : Base(
    () =>
    {
        string p1 = null;
#line 100
        p1++;
    },
    () =>
    {
        local();
        void local()
        {
            string p1 = null;
#line 200
            p1++;
        }
    },
    p1
    )
{
    System.Action x = () =>
                      {
                          string p1 = null;
#line 300
                          p1++;
                      };

    System.Action y = () =>
                      {
                          local();
                          void local()
                          {
                              string p1 = null;
#line 400
                              p1++;
                          }
                      };

    void M1()
    {
        string p1 = null;
#line 500
        p1++;
    }

    void M2()
    {
        System.Action x = () =>
                          {
                              string p1 = null;
#line 600
                              p1++;
                          };
        local();
        void local()
        {
            string p1 = null;
#line 700
            p1++;
        }
    }

    C1() : this(1)
    {
        string p1 = null;
#line 800
        p1++;
    }
}

class Base
{
    public Base(System.Action x, System.Action y, int z){}
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (100,9): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(100, 9),
                // (200,13): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //             p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(200, 13),
                // (300,27): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //                           p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(300, 27),
                // (400,31): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //                               p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(400, 31),
                // (500,9): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(500, 9),
                // (600,31): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //                               p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(600, 31),
                // (700,13): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //             p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(700, 13),
                // (800,9): error CS0023: Operator '++' cannot be applied to operand of type 'string'
                //         p1++;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "p1++").WithArguments("++", "string").WithLocation(800, 9)
                );
        }

        [Fact]
        public void ParameterScope_LocalShadows_02()
        {
            var src = @"
class C1(int p1) : Base(
    () =>
    {
#line 100
        p1++;
        void p1(){}
    },
    p1
    )
{
    System.Action y = () =>
                      {
                          void p1(){}
#line 200
                          p1++;
                      };

    void M1()
    {
        void p1(){}
#line 300
        p1++;
    }

    void M2()
    {
        System.Action x = () =>
                          {
                              void p1(){}
#line 400
                              p1++;
                          };
        local();
        void local()
        {
            void p1(){}
#line 500
            p1++;
        }
    }

    C1() : this(1)
    {
        void p1(){}
#line 600
        p1++;
    }
}

class Base
{
    public Base(System.Action y, int z){}
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (100,9): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //         p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(100, 9),
                // (200,27): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //                           p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(200, 27),
                // (300,9): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //         p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(300, 9),
                // (400,31): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //                               p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(400, 31),
                // (500,13): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //             p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(500, 13),
                // (600,9): error CS1656: Cannot assign to 'p1' because it is a 'method group'
                //         p1++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "p1").WithArguments("p1", "method group").WithLocation(600, 9)
                );
        }

        [Fact]
        public void ParameterScope_LocalShadows_03_InAttribute()
        {
            var src = @"
class C1(int p1)
{
    [MyAttribute(out string p1, p1++)]
    int P1 = p1++;
}

class MyAttribute : System.Attribute
{
    public MyAttribute(out string x, int y)
    {
        x = string.Empty;
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (4,6): error CS1729: 'MyAttribute' does not contain a constructor that takes 3 arguments
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "MyAttribute(out string p1, p1++)").WithArguments("MyAttribute", "3").WithLocation(4, 6),
                // (4,18): error CS1041: Identifier expected; 'out' is a keyword
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(4, 18),
                // (4,22): error CS1525: Invalid expression term 'string'
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "string").WithArguments("string").WithLocation(4, 22),
                // (4,29): error CS1003: Syntax error, ',' expected
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_SyntaxError, "p1").WithArguments(",").WithLocation(4, 29),
                // (4,29): error CS9105: Cannot use primary constructor parameter 'int p1' in this context.
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p1").WithArguments("int p1").WithLocation(4, 29),
                // (4,33): error CS9105: Cannot use primary constructor parameter 'int p1' in this context.
                //     [MyAttribute(out string p1, p1++)]
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p1").WithArguments("int p1").WithLocation(4, 33)
                );
        }

        [Fact]
        public void Nameof_01()
        {
            var src = @"
using System;

class C1 (int p1)
{
    bool M1(string x)
    {
        return x is nameof(p1);
    }

    public static void Main()
    {
        Console.Write(new C1(0).M1(""p1""));
        Console.Write(new C1(0).M1(""p2""));
    }
}
";
            var verifier = CompileAndVerify(src, expectedOutput: @"TrueFalse").VerifyDiagnostics(
                // (4,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(4, 15)
                );

            Assert.Empty(((CSharpCompilation)verifier.Compilation).GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_001()
        {
            var source = @"
class Base
{
    public System.Func<int> Z;
    public Base(int x, int y, System.Func<int> z)
    {
        System.Console.Write(z() - 1);
        Z = z;
    }
}

partial class C1
{
    public int F1 = p2 + 1;
}

partial class C1 (int p1, int p2, int p3) : Base(p1, p2, () => p1)
{
    public int F2 = p2 + 2;
    public int P1 => p1;
}

partial class C1
{
    public int F3 = p2 + 3;
    public int P2 => ++p1;
    public int M1() { return p1++; }
    event System.Action E1 { add { p1++; } remove { void local() { p1--; } local(); }}
    event System.Action E2 = () => p3++;
    public System.Action M2() => () => p1++;
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1,-2);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.P1);
        System.Console.Write(c1.P2);
        System.Console.Write(c1.Z());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var p1s = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "p1").ToArray();
            Assert.Equal(8, p1s.Length);

            foreach (var p1 in p1s)
            {
                var symbol = model.GetSymbolInfo(p1).Symbol;
                Assert.Equal(SymbolKind.Parameter, symbol.Kind);
                Assert.Equal("C1..ctor(System.Int32 p1, System.Int32 p2, System.Int32 p3)", symbol.ContainingSymbol.ToTestDisplayString());
                Assert.Contains("p1", model.LookupNames(p1.SpanStart));
                Assert.Contains(symbol, model.LookupSymbols(p1.SpanStart, name: "p1"));
            }

            var verifier = CompileAndVerify(comp, expectedOutput: @"122123124125125", verify: Verification.Fails).VerifyDiagnostics(
                // (17,50): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // partial class C1 (int p1, int p2, int p3) : Base(p1, p2, () => p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(17, 50)
                );

            verifier.VerifyTypeIL("C1", @"
.class private auto ansi beforefieldinit C1
	extends Base
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass1_0'
		extends [mscorlib]System.Object
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 p3
		.field public class C1 '<>4__this'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x221b
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [mscorlib]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass1_0'::.ctor
		.method assembly hidebysig 
			instance void '<.ctor>b__0' () cil managed 
		{
			// Method begins at RVA 0x2224
			// Code size 17 (0x11)
			.maxstack 3
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C1/'<>c__DisplayClass1_0'::p3
			IL_0006: stloc.0
			IL_0007: ldarg.0
			IL_0008: ldloc.0
			IL_0009: ldc.i4.1
			IL_000a: add
			IL_000b: stfld int32 C1/'<>c__DisplayClass1_0'::p3
			IL_0010: ret
		} // end of method '<>c__DisplayClass1_0'::'<.ctor>b__0'
		.method assembly hidebysig 
			instance int32 '<.ctor>b__1' () cil managed 
		{
			// Method begins at RVA 0x2241
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld class C1 C1/'<>c__DisplayClass1_0'::'<>4__this'
			IL_0006: ldfld int32 C1::'<p1>P'
			IL_000b: ret
		} // end of method '<>c__DisplayClass1_0'::'<.ctor>b__1'
	} // end of class <>c__DisplayClass1_0
	// Fields
	.field public int32 F1
	.field private int32 '<p1>P'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.field public int32 F2
	.field public int32 F3
	.field private class [mscorlib]System.Action E2
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 p1,
			int32 p2,
			int32 p3
		) cil managed 
	{
		// Method begins at RVA 0x2084
		// Code size 98 (0x62)
		.maxstack 5
		.locals init (
			[0] class C1/'<>c__DisplayClass1_0'
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 C1::'<p1>P'
		IL_0007: newobj instance void C1/'<>c__DisplayClass1_0'::.ctor()
		IL_000c: stloc.0
		IL_000d: ldloc.0
		IL_000e: ldarg.3
		IL_000f: stfld int32 C1/'<>c__DisplayClass1_0'::p3
		IL_0014: ldloc.0
		IL_0015: ldarg.0
		IL_0016: stfld class C1 C1/'<>c__DisplayClass1_0'::'<>4__this'
		IL_001b: ldarg.0
		IL_001c: ldarg.2
		IL_001d: ldc.i4.1
		IL_001e: add
		IL_001f: stfld int32 C1::F1
		IL_0024: ldarg.0
		IL_0025: ldarg.2
		IL_0026: ldc.i4.2
		IL_0027: add
		IL_0028: stfld int32 C1::F2
		IL_002d: ldarg.0
		IL_002e: ldarg.2
		IL_002f: ldc.i4.3
		IL_0030: add
		IL_0031: stfld int32 C1::F3
		IL_0036: ldarg.0
		IL_0037: ldloc.0
		IL_0038: ldftn instance void C1/'<>c__DisplayClass1_0'::'<.ctor>b__0'()
		IL_003e: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_0043: stfld class [mscorlib]System.Action C1::E2
		IL_0048: ldarg.0
		IL_0049: ldarg.0
		IL_004a: ldfld int32 C1::'<p1>P'
		IL_004f: ldarg.2
		IL_0050: ldloc.0
		IL_0051: ldftn instance int32 C1/'<>c__DisplayClass1_0'::'<.ctor>b__1'()
		IL_0057: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_005c: call instance void Base::.ctor(int32, int32, class [mscorlib]System.Func`1<int32>)
		IL_0061: ret
	} // end of method C1::.ctor
	.method public hidebysig specialname 
		instance int32 get_P1 () cil managed 
	{
		// Method begins at RVA 0x20f2
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C1::'<p1>P'
		IL_0006: ret
	} // end of method C1::get_P1
	.method public hidebysig specialname 
		instance int32 get_P2 () cil managed 
	{
		// Method begins at RVA 0x20fc
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stloc.0
		IL_000a: ldloc.0
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::get_P2
	.method public hidebysig 
		instance int32 M1 () cil managed 
	{
		// Method begins at RVA 0x211c
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: stloc.0
		IL_0008: ldloc.0
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::M1
	.method private hidebysig specialname 
		instance void add_E1 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x213a
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::add_E1
	.method private hidebysig specialname 
		instance void remove_E1 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x214a
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void C1::'<remove_E1>g__local|12_0'()
		IL_0006: ret
	} // end of method C1::remove_E1
	.method private hidebysig specialname 
		instance void add_E2 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2154
		// Code size 41 (0x29)
		.maxstack 3
		.locals init (
			[0] class [mscorlib]System.Action,
			[1] class [mscorlib]System.Action,
			[2] class [mscorlib]System.Action
		)
		IL_0000: ldarg.0
		IL_0001: ldfld class [mscorlib]System.Action C1::E2
		IL_0006: stloc.0
		// loop start (head: IL_0007)
			IL_0007: ldloc.0
			IL_0008: stloc.1
			IL_0009: ldloc.1
			IL_000a: ldarg.1
			IL_000b: call class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
			IL_0010: castclass [mscorlib]System.Action
			IL_0015: stloc.2
			IL_0016: ldarg.0
			IL_0017: ldflda class [mscorlib]System.Action C1::E2
			IL_001c: ldloc.2
			IL_001d: ldloc.1
			IL_001e: call !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&, !!0, !!0)
			IL_0023: stloc.0
			IL_0024: ldloc.0
			IL_0025: ldloc.1
			IL_0026: bne.un.s IL_0007
		// end loop
		IL_0028: ret
	} // end of method C1::add_E2
	.method private hidebysig specialname 
		instance void remove_E2 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x218c
		// Code size 41 (0x29)
		.maxstack 3
		.locals init (
			[0] class [mscorlib]System.Action,
			[1] class [mscorlib]System.Action,
			[2] class [mscorlib]System.Action
		)
		IL_0000: ldarg.0
		IL_0001: ldfld class [mscorlib]System.Action C1::E2
		IL_0006: stloc.0
		// loop start (head: IL_0007)
			IL_0007: ldloc.0
			IL_0008: stloc.1
			IL_0009: ldloc.1
			IL_000a: ldarg.1
			IL_000b: call class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate, class [mscorlib]System.Delegate)
			IL_0010: castclass [mscorlib]System.Action
			IL_0015: stloc.2
			IL_0016: ldarg.0
			IL_0017: ldflda class [mscorlib]System.Action C1::E2
			IL_001c: ldloc.2
			IL_001d: ldloc.1
			IL_001e: call !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action>(!!0&, !!0, !!0)
			IL_0023: stloc.0
			IL_0024: ldloc.0
			IL_0025: ldloc.1
			IL_0026: bne.un.s IL_0007
		// end loop
		IL_0028: ret
	} // end of method C1::remove_E2
	.method public hidebysig 
		instance class [mscorlib]System.Action M2 () cil managed 
	{
		// Method begins at RVA 0x21c1
		// Code size 13 (0xd)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldftn instance void C1::'<M2>b__16_0'()
		IL_0007: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_000c: ret
	} // end of method C1::M2
	.method private hidebysig 
		instance void '<remove_E1>g__local|12_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x21cf
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: sub
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::'<remove_E1>g__local|12_0'
	.method private hidebysig 
		instance void '<M2>b__16_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x213a
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::'<M2>b__16_0'
	// Events
	.event [mscorlib]System.Action E1
	{
		.addon instance void C1::add_E1(class [mscorlib]System.Action)
		.removeon instance void C1::remove_E1(class [mscorlib]System.Action)
	}
	.event [mscorlib]System.Action E2
	{
		.addon instance void C1::add_E2(class [mscorlib]System.Action)
		.removeon instance void C1::remove_E2(class [mscorlib]System.Action)
	}
	// Properties
	.property instance int32 P1()
	{
		.get instance int32 C1::get_P1()
	}
	.property instance int32 P2()
	{
		.get instance int32 C1::get_P2()
	}
} // end of class C1
".Replace("[mscorlib]", ExecutionConditionUtil.IsDesktop ? "[mscorlib]" : "[netstandard]"));
        }

        [Fact]
        public void ParameterCapturing_002()
        {
            var source = @"
#pragma warning disable CS0282 // There is no defined ordering between fields in multiple declarations of partial struct

partial struct C1
{
    public int F1 = p2 + 1;
}

partial struct C1 (int p1, int p2)
{
    public int F2 = p2 + 2;
    public int P1 => p1;
}

partial struct C1
{
    public int F3 = p2 + 3;
    public int P2 => ++p1;
    public int M1() { return p1++; }
    event System.Action E1 { add { p1++; } remove {}}
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.P1);
        System.Console.Write(c1.P2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var p1s = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "p1").ToArray();
            Assert.Equal(4, p1s.Length);

            foreach (var p1 in p1s)
            {
                var symbol = model.GetSymbolInfo(p1).Symbol;
                Assert.Equal(SymbolKind.Parameter, symbol.Kind);
                Assert.Equal("C1..ctor(System.Int32 p1, System.Int32 p2)", symbol.ContainingSymbol.ToTestDisplayString());
                Assert.Contains("p1", model.LookupNames(p1.SpanStart));
                Assert.Contains(symbol, model.LookupSymbols(p1.SpanStart, name: "p1"));
            }

            var verifier = CompileAndVerify(comp, expectedOutput: @"123124125").VerifyDiagnostics();

            verifier.VerifyTypeIL("C1", @"
    .class private sequential ansi sealed beforefieldinit C1
	extends [netstandard]System.ValueType
{
	// Fields
	.field public int32 F1
	.field private int32 '<p1>P'
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.field public int32 F2
	.field public int32 F3
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 p1,
			int32 p2
		) cil managed 
	{
		// Method begins at RVA 0x2067
		// Code size 35 (0x23)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 C1::'<p1>P'
		IL_0007: ldarg.0
		IL_0008: ldarg.2
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 C1::F1
		IL_0010: ldarg.0
		IL_0011: ldarg.2
		IL_0012: ldc.i4.2
		IL_0013: add
		IL_0014: stfld int32 C1::F2
		IL_0019: ldarg.0
		IL_001a: ldarg.2
		IL_001b: ldc.i4.3
		IL_001c: add
		IL_001d: stfld int32 C1::F3
		IL_0022: ret
	} // end of method C1::.ctor
	.method public hidebysig specialname 
		instance int32 get_P1 () cil managed 
	{
		// Method begins at RVA 0x208b
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C1::'<p1>P'
		IL_0006: ret
	} // end of method C1::get_P1
	.method public hidebysig specialname 
		instance int32 get_P2 () cil managed 
	{
		// Method begins at RVA 0x2094
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stloc.0
		IL_000a: ldloc.0
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::get_P2
	.method public hidebysig 
		instance int32 M1 () cil managed 
	{
		// Method begins at RVA 0x20b4
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: stloc.0
		IL_0008: ldloc.0
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::M1
	.method private hidebysig specialname 
		instance void add_E1 (
			class [netstandard]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x20d2
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::add_E1
	.method private hidebysig specialname 
		instance void remove_E1 (
			class [netstandard]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x20e2
		// Code size 1 (0x1)
		.maxstack 8
		IL_0000: ret
	} // end of method C1::remove_E1
	// Events
	.event [netstandard]System.Action E1
	{
		.addon instance void C1::add_E1(class [netstandard]System.Action)
		.removeon instance void C1::remove_E1(class [netstandard]System.Action)
	}
	// Properties
	.property instance int32 P1()
	{
		.get instance int32 C1::get_P1()
	}
	.property instance int32 P2()
	{
		.get instance int32 C1::get_P2()
	}
} // end of class C1
".Replace("[netstandard]", ExecutionConditionUtil.IsDesktop ? "[mscorlib]" : "[netstandard]"));
        }

        [Fact]
        public void ParameterCapturing_003_PartialMethod()
        {
            var source = @"
partial class C1 (int p1)
{
    public partial int M1();
}

partial class C1
{
    public partial int M1() { return p1++; }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.M1());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"123124").VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_004_MembersWithoutBody()
        {
            var source = @"
#pragma warning disable CS0626 // Method, operator, or accessor 'C1.M1()' is marked external and has no attributes on it.
#pragma warning disable CS0067 // The event 'C1.E1' is never used

abstract class C1 (int p1)
{
    extern int M1();
    public event System.Action E1;
    public abstract void M2();
    int P1 {get; set;}
    int P2 => 0;
    public abstract int P3 {get; set;}
    public abstract event System.Action E2;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (5,24): warning CS9113: Parameter 'p1' is unread.
                // abstract class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(5, 24)
                );
        }

        [Fact]
        public void ParameterCapturing_005_NotInterestingIdentifiers()
        {
            var source = @"
#pragma warning disable CS0219 // The variable 'x' is assigned but its value is never used
#pragma warning disable CS8321 // The local function 'local' is declared but never used

class C1 (int p1, System.Action p2 = null, int global = 0, int C2 = default)
{
    void M1(C2 c2)
    {
        int x;
        x = 0;
        c2?.p2();
        c2.p2();
        _ = new C1(p1: 1);

        if ((E)x is E.p2)
        {
            goto p1;
        }

        global::System.Console.WriteLine(new {p1 = 0}); 
p1:
        void local<T>() where T : C2 {}

        switch ((E)x)
        {
            case E.p2: break;
        }

        _ = new C2() { p1 = 11 };
    }
}

class C2
{
    public void p2() {}
    public int p1;
}

enum E
{
    p2,
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // Warnings indicate that we do not consider any primary constructor parameters referenced and not capturing them
            comp.VerifyEmitDiagnostics(
                // (5,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1, System.Action p2 = null, int global = 0, int C2 = default)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(5, 15),
                // (5,33): warning CS9113: Parameter 'p2' is unread.
                // class C1 (int p1, System.Action p2 = null, int global = 0, int C2 = default)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p2").WithArguments("p2").WithLocation(5, 33),
                // (5,48): warning CS9113: Parameter 'global' is unread.
                // class C1 (int p1, System.Action p2 = null, int global = 0, int C2 = default)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "global").WithArguments("global").WithLocation(5, 48),
                // (5,64): warning CS9113: Parameter 'C2' is unread.
                // class C1 (int p1, System.Action p2 = null, int global = 0, int C2 = default)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "C2").WithArguments("C2").WithLocation(5, 64)
                );
        }

        [Fact]
        public void ParameterCapturing_006_Invoked()
        {
            var source = @"
class C1 (System.Action p1)
{
    public void M1() => p1();
}

class Program
{
    static void Main()
    {
        var c1 = new C1(() => System.Console.Write(123));
        c1.M1();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_007_IsExpression()
        {
            var source = @"
class C1 (int p1, C2 p2)
{
    bool M1(int x)
    {
        return x is p1 || x is p2.F;
    }
}

class C2
{
    public int F = 11;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // No unused warnings because we detected capturing
            comp.VerifyEmitDiagnostics(
                // (6,21): error CS9135: A constant value of type 'int' is expected
                //         return x is p1 || x is p2.F;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "p1").WithArguments("int").WithLocation(6, 21),
                // (6,32): error CS9135: A constant value of type 'int' is expected
                //         return x is p1 || x is p2.F;
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "p2.F").WithArguments("int").WithLocation(6, 32)
                );
        }

        [Fact]
        public void ParameterCapturing_008_GotoCase()
        {
            var source = @"
class C1 (int p1)
{
    void M1(int x)
    {
        switch (x)
        {
            case 1:
                break;
            case 2:
                goto case p1;
        }
    }

    void M2(int x)
    {
        switch (x)
        {
            case System.Int32.MinValue:
                break;
            case 3:
                goto case System.Int32.MinValue;
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // No unused warnings because we detected capturing
            comp.VerifyEmitDiagnostics(
                // (10,13): error CS8070: Control cannot fall out of switch from final case label ('case 2:')
                //             case 2:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 2:").WithArguments("case 2:").WithLocation(10, 13),
                // (11,17): error CS0150: A constant value is expected
                //                 goto case p1;
                Diagnostic(ErrorCode.ERR_ConstantExpected, "goto case p1;").WithLocation(11, 17)
                );
        }

        [Fact]
        public void ParameterCapturing_009_Lambda()
        {
            var source1 = @"
class C1 (int p1)
{
    public System.Func<int> M21() => delegate => p1;
}
";
            var comp1 = CreateCompilation(source1, options: TestOptions.ReleaseDll);

            comp1.VerifyEmitDiagnostics(
                // (4,38): error CS1041: Identifier expected; 'delegate' is a keyword
                //     public System.Func<int> M21() => delegate => p1;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "delegate").WithArguments("", "delegate").WithLocation(4, 38)
                );

            var source = @"
class C1 (int p1, int p2, int p3, int p4, int p5)
{
    public System.Func<int> M1() => () => p1;
    public System.Func<int> M2() => int () => { return p2; };
    public System.Func<int, int> M3() => x => p3 + x;
    public System.Func<int, int> M4() => x => { return p4 - x; };
    public System.Func<int> M5() => delegate { return p5; };
}

class Program
{
    static void Main()
    {
        var c1 = new C1(10, 20, 30 , 40, 50);
        System.Console.Write(c1.M1()());
        System.Console.Write(c1.M2()());
        System.Console.Write(c1.M3()(3));
        System.Console.Write(c1.M4()(4));
        System.Console.Write(c1.M5()());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"1020333650").VerifyDiagnostics();
        }

        public static IEnumerable<object[]> ParameterCapturing_010_ShadowingInMethodBody_MemberData()
        {
            var data = new (string tag, string code)[]
                {
                    ("0001", "int M1(int p1) { return p1; }"),
                    ("0002", "int M1() { int p1 = 10; return p1; }"),
                    ("0003", "void M1() { using var p1 = (System.IDisposable)null; p1.Dispose(); }"),
                    ("0004", "int M1() { M2(out var p1); return p1; } void M2(out int x) { x = 0; }"),

                    ("0101", "void M1(int p1) { var d = (int x) => p1; d(0); }"),
                    ("0102", "void M1(int p1) { System.Func<int, int> d = (x) => p1; d(0); }"),
                    ("0103", "void M1(int p1) { System.Func<int, int> d = x => p1; d(0); }"),
                    ("0104", "void M1(int p1) { System.Func<int, int> d = delegate(int x) { return p1; }; d(0); }"),
                    ("0105", "void M1(int p1) { System.Func<int, int> d = delegate { return p1; }; d(0); }"),

                    ("0201", "void M1(int p1) { var d = (int x) => { int local() => p1; return local(); }; d(0); }"),
                    ("0202", "void M1(int p1) { System.Func<int, int> d = (x) => { int local() => p1; return local(); }; d(0); }"),
                    ("0203", "void M1(int p1) { System.Func<int, int> d = x => { int local() => p1; return local(); }; d(0); }"),
                    ("0204", "void M1(int p1) { System.Func<int, int> d = delegate(int x) { int local() => p1; return local(); }; d(0); }"),
                    ("0205", "void M1(int p1) { System.Func<int, int> d = delegate { int local() => p1; return local(); }; d(0); }"),

                    ("0301", "void M1(int p1) { var d = (int x) => { var d = (int x) => p1; return d(0); }; d(0); }"),
                    ("0302", "void M1(int p1) { System.Func<int, int> d = (x) => { var d = (int x) => p1; return d(0); }; d(0); }"),
                    ("0303", "void M1(int p1) { System.Func<int, int> d = x => { var d = (int x) => p1; return d(0); }; d(0); }"),
                    ("0304", "void M1(int p1) { System.Func<int, int> d = delegate(int x) { var d = (int x) => p1; return d(0); }; d(0); }"),
                    ("0305", "void M1(int p1) { System.Func<int, int> d = delegate { var d = (int x) => p1; return d(0); }; d(0); }"),

                    ("0401", "int M1(int p1) { int local() => p1; return local(); }"),
                    ("0402", "int M1(int p1) { int local() { int local() => p1; return local(); }; return local(); }"),
                    ("0403", "int M1(int p1) { int local() { var d = (int x) => p1; return d(0); }; return local(); }"),

                    ("0501", "int M1() { var d = () => { int p1 = 10; return p1; }; return d(); }"),
                    ("0502", "void M1() { var d = () => { using var p1 = (System.IDisposable)null; p1.Dispose(); }; d(); }"),
                    ("0503", "int M1() { var d = () => { M2(out var p1); return p1; }; return d(); } void M2(out int x) { x = 0; }"),

                    ("0601", "int M1() { int local() { int p1 = 10; return p1; }; return local(); }"),
                    ("0602", "void M1() { void local() { using var p1 = (System.IDisposable)null; p1.Dispose(); }; local(); }"),
                    ("0603", "int M1() { int local() { M2(out var p1); return p1; }; return local(); } void M2(out int x) { x = 0; }"),

                    ("0701", "int M1() { var d = (int p1) => p1; return d(0); }"),
                    ("0702", "void M1() { var d = (int p1) => { var d = (int x) => p1; d(0); }; d(0); }"),
                    ("0703", "void M1() { var d = (int p1) => { var d = (int x) => { int local() => p1; return local(); }; d(0); }; d(0); }"),
                    ("0704", "void M1() { var d = (int p1) => { var d = (int x) => { var d = (int x) => p1; return d(0); }; d(0); }; d(0); }"),
                    ("0705", "int M1() { var d = (int p1) => { int local() => p1; return local(); }; return d(0); }"),
                    ("0706", "int M1() { var d = (int p1) => { int local() { int local() => p1; return local(); }; return local(); }; return d(0); }"),
                    ("0707", "int M1() { var d = (int p1) => { int local() { var d = (int x) => p1; return d(0); }; return local(); }; return d(0); }"),

                    ("0801", "int M1() { System.Func<int, int> d = p1 => p1; return d(0); }"),
                    ("0802", "void M1() { System.Action<int> d = p1 => { var d = (int x) => p1; d(0); }; d(0); }"),
                    ("0803", "void M1() { System.Action<int> d = p1 => { var d = (int x) => { int local() => p1; return local(); }; d(0); }; d(0); }"),
                    ("0804", "void M1() { System.Action<int> d = p1 => { var d = (int x) => { var d = (int x) => p1; return d(0); }; d(0); }; d(0); }"),
                    ("0805", "int M1() { System.Func<int, int> d = p1 => { int local() => p1; return local(); }; return d(0); }"),
                    ("0806", "int M1() { System.Func<int, int> d = p1 => { int local() { int local() => p1; return local(); }; return local(); }; return d(0); }"),
                    ("0807", "int M1() { System.Func<int, int> d = p1 => { int local() { var d = (int x) => p1; return d(0); }; return local(); }; return d(0); }"),

                    ("0901", "int M1() { System.Func<int, int> d = delegate(int p1) { return p1; }; return d(0); }"),
                    ("0902", "void M1() { System.Action<int> d = delegate(int p1) { var d = (int x) => p1; d(0); }; d(0); }"),
                    ("0903", "void M1() { System.Action<int> d = delegate(int p1) { var d = (int x) => { int local() => p1; return local(); }; d(0); }; d(0); }"),
                    ("0904", "void M1() { System.Action<int> d = delegate(int p1) { var d = (int x) => { var d = (int x) => p1; return d(0); }; d(0); }; d(0); }"),
                    ("0905", "int M1() { System.Func<int, int> d = delegate(int p1) { int local() => p1; return local(); }; return d(0); }"),
                    ("0906", "int M1() { System.Func<int, int> d = delegate(int p1) { int local() { int local() => p1; return local(); }; return local(); }; return d(0); }"),
                    ("0907", "int M1() { System.Func<int, int> d = delegate(int p1) { int local() { var d = (int x) => p1; return d(0); }; return local(); }; return d(0); }"),

                    ("1001", "int M1() { int local(int p1) => p1; return local(0); }"),
                    ("1002", "void M1() { void local(int p1) { var d = (int x) => p1; d(0); }; local(0); }"),
                    ("1003", "void M1() { void local(int p1) { var d = (int x) => { int local() => p1; return local(); }; d(0); }; local(0); }"),
                    ("1004", "void M1() { void local(int p1) { var d = (int x) => { var d = (int x) => p1; return d(0); }; d(0); }; local(0); }"),
                    ("1005", "int M1() { int local(int p1) { int local() => p1; return local(); }; return local(0); }"),
                    ("1006", "int M1() { int local(int p1) { int local() { int local() => p1; return local(); }; return local(); }; return local(0); }"),
                    ("1007", "int M1() { int local(int p1) { int local() { var d = (int x) => p1; return d(0); }; return local(); }; return local(0); }"),

                    ("1101", "object M1() => from p1 in new[] {1} select p1;"),
                    ("1102", "object M1() => from p1 in new[] {1} group p1 by p1;"),
                    ("1103", "object M1() => from p1 in new[] {1} where p1 > 0 select p1;"),
                    ("1104", "object M1() => from p1 in new[] {1} orderby p1 group p1 by p1;"),
                    ("1105", "object M1() => from p1 in new[] {1} orderby p1, p1 select p1;"),
                    ("1106", "object M1() => from p1 in new[] {1} let x = p1 select p1;"),
                    ("1107", "object M1() => from p1 in new[] {1} let x = p1 group p1 by p1;"),
                    ("1108", "object M1() => from p1 in new[] {1} let x = p1 where p1 > 0 select p1;"),
                    ("1109", "object M1() => from p1 in new[] {1} let x = p1 orderby p1 group p1 by p1;"),
                    ("1110", "object M1() => from p1 in new[] {1} let x = p1 orderby p1, p1 select p1;"),
                    ("1111", "object M1() => from p1 in new[] {1} where p1 > 0 let x = p1 select p1;"),
                    ("1112", "object M1() => from p1 in new[] {1} orderby p1 let x = p1 group p1 by p1;"),
                    ("1113", "object M1() => from p1 in new[] {1} from x in new[] {p1} select p1;"),
                    ("1114", "object M1() => from p1 in new[] {1} from x in new[] {p1} group p1 by p1;"),
                    ("1115", "object M1() => from p1 in new[] {1} from x in new[] {p1} where p1 > 0 select p1;"),
                    ("1116", "object M1() => from p1 in new[] {1} from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1117", "object M1() => from p1 in new[] {1} from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1118", "object M1() => from p1 in new[] {1} where p1 > 0 from x in new[] {p1} select p1;"),
                    ("1119", "object M1() => from p1 in new[] {1} orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1120", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x select p1;"),
                    ("1121", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1122", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x where p1 > 0 select p1;"),
                    ("1123", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x orderby p1 group p1 by p1;"),
                    ("1124", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x orderby p1, p1 select p1;"),
                    ("1125", "object M1() => from p1 in new[] {1} where p1 > 0 join x in new[] {2} on p1 equals x select p1;"),
                    ("1126", "object M1() => from p1 in new[] {1} orderby p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1127", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1128", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                    ("1129", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x into z where p1 > 0 select p1;"),
                    ("1130", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x into z orderby p1 group p1 by p1;"),
                    ("1131", "object M1() => from p1 in new[] {1} join x in new[] {2} on p1 equals x into z orderby p1, p1 select p1;"),
                    ("1132", "object M1() => from p1 in new[] {1} where p1 > 0 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1133", "object M1() => from p1 in new[] {1} orderby p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),

                    ("1201", "object M1() => from y in new[] {1} from p1 in new[] {y} select p1;"),
                    ("1202", "object M1() => from y in new[] {1} from p1 in new[] {y} group p1 by p1;"),
                    ("1203", "object M1() => from y in new[] {1} from p1 in new[] {y} where p1 > 0 select p1;"),
                    ("1204", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1 group p1 by p1;"),
                    ("1205", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1, p1 select p1;"),
                    ("1206", "object M1() => from y in new[] {1} from p1 in new[] {y} let x = p1 select p1;"),
                    ("1207", "object M1() => from y in new[] {1} from p1 in new[] {y} let x = p1 group p1 by p1;"),
                    ("1208", "object M1() => from y in new[] {1} from p1 in new[] {y} let x = p1 where p1 > 0 select p1;"),
                    ("1209", "object M1() => from y in new[] {1} from p1 in new[] {y} let x = p1 orderby p1 group p1 by p1;"),
                    ("1210", "object M1() => from y in new[] {1} from p1 in new[] {y} let x = p1 orderby p1, p1 select p1;"),
                    ("1211", "object M1() => from y in new[] {1} from p1 in new[] {y} where p1 > 0 let x = p1 select p1;"),
                    ("1212", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1 let x = p1 group p1 by p1;"),
                    ("1213", "object M1() => from y in new[] {1} from p1 in new[] {y} from x in new[] {p1} select p1;"),
                    ("1214", "object M1() => from y in new[] {1} from p1 in new[] {y} from x in new[] {p1} group p1 by p1;"),
                    ("1215", "object M1() => from y in new[] {1} from p1 in new[] {y} from x in new[] {p1} where p1 > 0 select p1;"),
                    ("1216", "object M1() => from y in new[] {1} from p1 in new[] {y} from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1217", "object M1() => from y in new[] {1} from p1 in new[] {y} from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1218", "object M1() => from y in new[] {1} from p1 in new[] {y} where p1 > 0 from x in new[] {p1} select p1;"),
                    ("1219", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1220", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x select p1;"),
                    ("1221", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1222", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x where p1 > 0 select p1;"),
                    ("1223", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x orderby p1 group p1 by p1;"),
                    ("1224", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x orderby p1, p1 select p1;"),
                    ("1225", "object M1() => from y in new[] {1} from p1 in new[] {y} where p1 > 0 join x in new[] {2} on p1 equals x select p1;"),
                    ("1226", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1227", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1228", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                    ("1229", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x into z where p1 > 0 select p1;"),
                    ("1230", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x into z orderby p1 group p1 by p1;"),
                    ("1231", "object M1() => from y in new[] {1} from p1 in new[] {y} join x in new[] {2} on p1 equals x into z orderby p1, p1 select p1;"),
                    ("1232", "object M1() => from y in new[] {1} from p1 in new[] {y} where p1 > 0 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1233", "object M1() => from y in new[] {1} from p1 in new[] {y} orderby p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),

                    ("1301", "object M1() => from y in new[] {1} let p1 = y select p1;"),
                    ("1302", "object M1() => from y in new[] {1} let p1 = y group p1 by p1;"),
                    ("1303", "object M1() => from y in new[] {1} let p1 = y where p1 > 0 select p1;"),
                    ("1304", "object M1() => from y in new[] {1} let p1 = y orderby p1 group p1 by p1;"),
                    ("1305", "object M1() => from y in new[] {1} let p1 = y orderby p1, p1 select p1;"),
                    ("1306", "object M1() => from y in new[] {1} let p1 = y let x = p1 select p1;"),
                    ("1307", "object M1() => from y in new[] {1} let p1 = y let x = p1 group p1 by p1;"),
                    ("1308", "object M1() => from y in new[] {1} let p1 = y let x = p1 where p1 > 0 select p1;"),
                    ("1309", "object M1() => from y in new[] {1} let p1 = y let x = p1 orderby p1 group p1 by p1;"),
                    ("1310", "object M1() => from y in new[] {1} let p1 = y let x = p1 orderby p1, p1 select p1;"),
                    ("1311", "object M1() => from y in new[] {1} let p1 = y where p1 > 0 let x = p1 select p1;"),
                    ("1312", "object M1() => from y in new[] {1} let p1 = y orderby p1 let x = p1 group p1 by p1;"),
                    ("1313", "object M1() => from y in new[] {1} let p1 = y from x in new[] {p1} select p1;"),
                    ("1314", "object M1() => from y in new[] {1} let p1 = y from x in new[] {p1} group p1 by p1;"),
                    ("1315", "object M1() => from y in new[] {1} let p1 = y from x in new[] {p1} where p1 > 0 select p1;"),
                    ("1316", "object M1() => from y in new[] {1} let p1 = y from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1317", "object M1() => from y in new[] {1} let p1 = y from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1318", "object M1() => from y in new[] {1} let p1 = y where p1 > 0 from x in new[] {p1} select p1;"),
                    ("1319", "object M1() => from y in new[] {1} let p1 = y orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1320", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x select p1;"),
                    ("1321", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1322", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x where p1 > 0 select p1;"),
                    ("1323", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x orderby p1 group p1 by p1;"),
                    ("1324", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x orderby p1, p1 select p1;"),
                    ("1325", "object M1() => from y in new[] {1} let p1 = y where p1 > 0 join x in new[] {2} on p1 equals x select p1;"),
                    ("1326", "object M1() => from y in new[] {1} let p1 = y orderby p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1327", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1328", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                    ("1329", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x into z where p1 > 0 select p1;"),
                    ("1330", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x into z orderby p1 group p1 by p1;"),
                    ("1331", "object M1() => from y in new[] {1} let p1 = y join x in new[] {2} on p1 equals x into z orderby p1, p1 select p1;"),
                    ("1332", "object M1() => from y in new[] {1} let p1 = y where p1 > 0 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1333", "object M1() => from y in new[] {1} let p1 = y orderby p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),

                    ("1401", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 select p1;"),
                    ("1402", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 group p1 by p1;"),
                    ("1403", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 where p1 > 0 select p1;"),
                    ("1404", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1 group p1 by p1;"),
                    ("1405", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1, p1 select p1;"),
                    ("1406", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 let x = p1 select p1;"),
                    ("1407", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 let x = p1 group p1 by p1;"),
                    ("1408", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 let x = p1 where p1 > 0 select p1;"),
                    ("1409", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 let x = p1 orderby p1 group p1 by p1;"),
                    ("1410", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 let x = p1 orderby p1, p1 select p1;"),
                    ("1411", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 where p1 > 0 let x = p1 select p1;"),
                    ("1412", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1 let x = p1 group p1 by p1;"),
                    ("1413", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 from x in new[] {p1} select p1;"),
                    ("1414", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 from x in new[] {p1} group p1 by p1;"),
                    ("1415", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 from x in new[] {p1} where p1 > 0 select p1;"),
                    ("1416", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1417", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1418", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 where p1 > 0 from x in new[] {p1} select p1;"),
                    ("1419", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1420", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x select p1;"),
                    ("1421", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1422", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x where p1 > 0 select p1;"),
                    ("1423", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x orderby p1 group p1 by p1;"),
                    ("1424", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x orderby p1, p1 select p1;"),
                    ("1425", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 where p1 > 0 join x in new[] {2} on p1 equals x select p1;"),
                    ("1426", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1427", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1428", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                    ("1429", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x into z where p1 > 0 select p1;"),
                    ("1430", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x into z orderby p1 group p1 by p1;"),
                    ("1431", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 join x in new[] {2} on p1 equals x into z orderby p1, p1 select p1;"),
                    ("1432", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 where p1 > 0 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1433", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 orderby p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),

                    ("1501", "object M1() => from y in new[] {1} join p1 in new[] {2} on y equals p1 into u select u;"),

                    ("1601", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 select p1;"),
                    ("1602", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 group p1 by p1;"),
                    ("1603", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 where p1.Count() > 0 select p1;"),
                    ("1604", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1 group p1 by p1;"),
                    ("1605", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1, p1 select p1;"),
                    ("1606", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 let x = p1 select p1;"),
                    ("1607", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 let x = p1 group p1 by p1;"),
                    ("1608", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 let x = p1 where p1.Count() > 0 select p1;"),
                    ("1609", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 let x = p1 orderby p1 group p1 by p1;"),
                    ("1610", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 let x = p1 orderby p1, p1 select p1;"),
                    ("1611", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 where p1.Count() > 0 let x = p1 select p1;"),
                    ("1612", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1 let x = p1 group p1 by p1;"),
                    ("1613", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 from x in new[] {p1} select p1;"),
                    ("1614", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 from x in new[] {p1} group p1 by p1;"),
                    ("1615", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 from x in new[] {p1} where p1.Count() > 0 select p1;"),
                    ("1616", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1617", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1618", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 where p1.Count() > 0 from x in new[] {p1} select p1;"),
                    ("1619", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1620", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x select p1;"),
                    ("1621", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x group p1 by p1;"),
                    ("1622", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x where p1.Count() > 0 select p1;"),
                    ("1623", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x orderby p1 group p1 by p1;"),
                    ("1624", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x orderby p1, p1 select p1;"),
                    ("1625", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 where p1.Count() > 0 join x in new[] {2} on p1.Count() equals x select p1;"),
                    ("1626", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1 join x in new[] {2} on p1.Count() equals x group p1 by p1;"),
                    ("1627", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x into z select p1;"),
                    ("1628", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x into z group p1 by p1;"),
                    ("1629", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x into z where p1.Count() > 0 select p1;"),
                    ("1630", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x into z orderby p1 group p1 by p1;"),
                    ("1631", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 join x in new[] {2} on p1.Count() equals x into z orderby p1, p1 select p1;"),
                    ("1632", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 where p1.Count() > 0 join x in new[] {2} on p1.Count() equals x into z select p1;"),
                    ("1633", "object M1() => from y in new[] {1} join u in new[] {2} on y equals u into p1 orderby p1 join x in new[] {2} on p1.Count() equals x into z group p1 by p1;"),

                    ("1701", "object M1() => from y in new[] {1} select y + 1 into p1 select p1;"),
                    ("1702", "object M1() => from y in new[] {1} select y + 1 into p1 group p1 by p1;"),
                    ("1703", "object M1() => from y in new[] {1} select y + 1 into p1 where p1 > 0 select p1;"),
                    ("1704", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1 group p1 by p1;"),
                    ("1705", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1, p1 select p1;"),
                    ("1706", "object M1() => from y in new[] {1} select y + 1 into p1 let x = p1 select p1;"),
                    ("1707", "object M1() => from y in new[] {1} select y + 1 into p1 let x = p1 group p1 by p1;"),
                    ("1708", "object M1() => from y in new[] {1} select y + 1 into p1 let x = p1 where p1 > 0 select p1;"),
                    ("1709", "object M1() => from y in new[] {1} select y + 1 into p1 let x = p1 orderby p1 group p1 by p1;"),
                    ("1710", "object M1() => from y in new[] {1} select y + 1 into p1 let x = p1 orderby p1, p1 select p1;"),
                    ("1711", "object M1() => from y in new[] {1} select y + 1 into p1 where p1 > 0 let x = p1 select p1;"),
                    ("1712", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1 let x = p1 group p1 by p1;"),
                    ("1713", "object M1() => from y in new[] {1} select y + 1 into p1 from x in new[] {p1} select p1;"),
                    ("1714", "object M1() => from y in new[] {1} select y + 1 into p1 from x in new[] {p1} group p1 by p1;"),
                    ("1715", "object M1() => from y in new[] {1} select y + 1 into p1 from x in new[] {p1} where p1 > 0 select p1;"),
                    ("1716", "object M1() => from y in new[] {1} select y + 1 into p1 from x in new[] {p1} orderby p1 group p1 by p1;"),
                    ("1717", "object M1() => from y in new[] {1} select y + 1 into p1 from x in new[] {p1} orderby p1, p1 select p1;"),
                    ("1718", "object M1() => from y in new[] {1} select y + 1 into p1 where p1 > 0 from x in new[] {p1} select p1;"),
                    ("1719", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1 from x in new[] {p1} group p1 by p1;"),
                    ("1720", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x select p1;"),
                    ("1721", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1722", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x where p1 > 0 select p1;"),
                    ("1723", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x orderby p1 group p1 by p1;"),
                    ("1724", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x orderby p1, p1 select p1;"),
                    ("1725", "object M1() => from y in new[] {1} select y + 1 into p1 where p1 > 0 join x in new[] {2} on p1 equals x select p1;"),
                    ("1726", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1 join x in new[] {2} on p1 equals x group p1 by p1;"),
                    ("1727", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1728", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                    ("1729", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x into z where p1 > 0 select p1;"),
                    ("1730", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x into z orderby p1 group p1 by p1;"),
                    ("1731", "object M1() => from y in new[] {1} select y + 1 into p1 join x in new[] {2} on p1 equals x into z orderby p1, p1 select p1;"),
                    ("1732", "object M1() => from y in new[] {1} select y + 1 into p1 where p1 > 0 join x in new[] {2} on p1 equals x into z select p1;"),
                    ("1733", "object M1() => from y in new[] {1} select y + 1 into p1 orderby p1 join x in new[] {2} on p1 equals x into z group p1 by p1;"),
                };

            foreach (var d in data)
            {
                yield return new object[] { d.tag, d.code };
            }
        }

        [Theory]
        [MemberData(nameof(ParameterCapturing_010_ShadowingInMethodBody_MemberData))]
        public void ParameterCapturing_010_ShadowingInMethodBody(string tag, string member)
        {
            _ = tag;

            var source = @"
using System.Linq;

#line 1000
class C1 (int p1)
{
" + member + @"
}
";

            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.GetEmitDiagnostics().Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective).Verify(
                // (1000,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(1000, 15)
                );

            Assert.Empty(comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_011_EscapedIdentifier()
        {
            var source = @"
class C1 (int p1)
{
    public int M1() { return @p1++; }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.M1());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"123124").VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_012_LambdaWithDiscard()
        {
            var source = @"
class C1 (int _)
{
    public System.Func<int, int> M1() => (_) => _;
}

class C2 (int _)
{
    public System.Func<int, int, int> M2() => (_, _) => _;
}

class Program
{
    static void Main()
    {
        var c1 = new C1(10);
        System.Console.Write(c1.M1()(-1));
        var c2 = new C2(20);
        System.Console.Write(c2.M2()(-2, -3));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"-120").VerifyDiagnostics(
                // (2,15): warning CS9113: Parameter '_' is unread.
                // class C1 (int _)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "_").WithArguments("_").WithLocation(2, 15)
                );

            Assert.Equal(1, comp.GetTypeByMetadataName("C2").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
            Assert.Empty(comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_013_Nameof()
        {
            var source = @"
class C1 (int p1)
{
    public string M1() => nameof(p1);
}

class C2 (int p2)
{
    public string M2() => nameof(p2);

    string nameof(int x) => ""x""; 
}

class C3 (System.Func<int, int> nameof)
{
    public int M3(int x) => nameof(x);
}

class C4 (System.Func<int, int> nameof, int p4)
{
    public int M4() => nameof(p4);
}

class Program
{
    static void Main()
    {
        var c1 = new C1(10);
        System.Console.Write(c1.M1());
        var c2 = new C2(20);
        System.Console.Write(c2.M2());
        var c3 = new C3(x => x);
        System.Console.Write(c3.M3(30));
        var c4 = new C4(x => x, 40);
        System.Console.Write(c4.M4());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"p1x3040").VerifyDiagnostics(
                // (2,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 15)
                );

            Assert.Empty(comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
            Assert.Equal(1, comp.GetTypeByMetadataName("C2").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
            Assert.Equal(1, comp.GetTypeByMetadataName("C3").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
            Assert.Equal(2, comp.GetTypeByMetadataName("C4").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
        }

        [Fact]
        public void ParameterCapturing_014_Nameof()
        {
            var source = @"
class C3 (int nameof)
{
    public int M3(int x) => nameof(x);
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics(
                // (4,29): error CS0149: Method name expected
                //     public int M3(int x) => nameof(x);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "nameof").WithLocation(4, 29)
                );

            Assert.Equal(1, comp.GetTypeByMetadataName("C3").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
        }

        [Fact]
        public void ParameterCapturing_015_AmbiguousDeclaration()
        {
            var source = @"
class C1 (int p1, C1 p1)
{
    object M1(int x)
    {
        return p1;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // No unused warnings because we detected capturing
            comp.VerifyEmitDiagnostics(
                    // (2,22): error CS0100: The parameter name 'p1' is a duplicate
                    // class C1 (int p1, C1 p1)
                    Diagnostic(ErrorCode.ERR_DuplicateParamName, "p1").WithArguments("p1").WithLocation(2, 22),
                    // (6,16): error CS0229: Ambiguity between 'int p1' and 'C1 p1'
                    //         return p1;
                    Diagnostic(ErrorCode.ERR_AmbigMember, "p1").WithArguments("int p1", "C1 p1").WithLocation(6, 16)
                );

            Assert.Equal(2, comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
        }

        public static IEnumerable<object[]> ParameterCapturing_016_Query_MemberData()
        {
            var data = new (string tag, string code)[]
                {
                    ("0001", "=> (from x in new[] {p1} select x).Single();"),
                    ("0002", "=> (from x in new[] {1} let z = p1 select z).Single();"),
                    ("0003", "=> (from x in new[] {1} select p1).Single();"),
                    ("0004", "=> (from x in new[] {1, 123, 2} where x == p1 select x).Single();"),
                    ("0005", "=> (from x in new[] {123} from y in new[] {p1} where x == y select x).Single();"),
                    ("0006", "=> (from x in new[] {123} join y in new[] {p1} on x equals y select x).Single();"),
                    ("0007", "=> (from x in new[] {123} join y in new[] {0} on x equals y + p1 select x).Single();"),
                    ("0008", "=> (from x in new[] {0} join y in new[] {123} on x + p1 equals y select y).Single();"),
                    ("0009", "=> (from x in new[] {123} join y in new[] {p1} on x equals y into z select x).Single();"),
                    ("0010", "=> (from x in new[] {123} join y in new[] {0} on x equals y + p1 into z select x).Single();"),
                    ("0011", "=> (from x in new[] {0} join y in new[] {123} on x + p1 equals y into z select z.Single()).Single();"),
                    ("0012", "=> (from x in new[] {1} group x by p1).Single().Key;"),
                    ("0013", "=> (from x in new[] {1} group p1 by x).Single().Single();"),
                    ("0014", "=> (from x in new[] {1} orderby p1 select x + 122).Single();"),
                    ("0015", "=> (from x in new[] {1} orderby p1, x select x + 122).Single();"),
                    ("0016", "=> (from x in new[] {1} orderby x, p1 select x + 122).Single();"),
                    ("0017", "=> (from x in new[] {1} select x into y select p1).Single();"),

                    ("0101", "=> (from p1 in new[] {1} select p1 + 1 into y from x in new[] {p1} select x).Single();"),
                    ("0102", "=> (from p1 in new[] {1} select p1 + 1 into y let z = p1 select z).Single();"),
                    ("0103", "=> (from p1 in new[] {1} select p1 + 1 into y select p1).Single();"),
                    ("0104", "=> (from p1 in new[] {1, 122, 2} select p1 + 1 into x where x == p1 select x).Single();"),
                    ("0105", "=> (from p1 in new[] {122} select p1 + 1 into x from y in new[] {p1} where x == y select x).Single();"),
                    ("0106", "=> (from p1 in new[] {122} select p1 + 1 into x join y in new[] {p1} on x equals y select x).Single();"),
                    ("0107", "=> (from p1 in new[] {122} select p1 + 1 into x join y in new[] {0} on x equals y + p1 select x).Single();"),
                    ("0108", "=> (from p1 in new[] {-1} select p1 + 1 into x join y in new[] {123} on x + p1 equals y select y).Single();"),
                    ("0109", "=> (from p1 in new[] {122} select p1 + 1 into x join y in new[] {p1} on x equals y into z select x).Single();"),
                    ("0110", "=> (from p1 in new[] {122} select p1 + 1 into x join y in new[] {0} on x equals y + p1 into z select x).Single();"),
                    ("0111", "=> (from p1 in new[] {-1} select p1 + 1 into x  join y in new[] {123} on x + p1 equals y into z select z.Single()).Single();"),
                    ("0112", "=> (from p1 in new[] {1} select p1 + 1 into x group x by p1).Single().Key;"),
                    ("0113", "=> (from p1 in new[] {1} select p1 + 1 into x group p1 by x).Single().Single();"),
                    ("0114", "=> (from p1 in new[] {0} select p1 + 1 into x orderby p1 select x + 122).Single();"),
                    ("0115", "=> (from p1 in new[] {0} select p1 + 1 into x orderby p1, x select x + 122).Single();"),
                    ("0116", "=> (from p1 in new[] {0} select p1 + 1 into x orderby x, p1 select x + 122).Single();"),
                    ("0117", "=> (from p1 in new[] {1} select p1 + 1 into x select x into y select p1).Single();"),

                    ("0201", "=> (from y in new[] {1} join p1 in new[] {1} on y equals p1 into u from x in new[] {p1} select x).Single();"),
                    ("0202", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u let z = p1 select z).Single();"),
                    ("0203", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u select p1).Single();"),
                    ("0204", "=> (from x in new[] {1, 123, 2} join p1 in new[] {1} on x equals p1 into u where x == p1 select x).Single();"),
                    ("0205", "=> (from x in new[] {123} join p1 in new[] {1} on x equals p1 into u from y in new[] {p1} where x == y select x).Single();"),
                    ("0206", "=> (from x in new[] {123} join p1 in new[] {1} on x equals p1 into u join y in new[] {p1} on x equals y select x).Single();"),
                    ("0207", "=> (from x in new[] {123} join p1 in new[] {1} on x equals p1 into u join y in new[] {0} on x equals y + p1 select x).Single();"),
                    ("0208", "=> (from x in new[] {0} join p1 in new[] {1} on x equals p1 into u join y in new[] {123} on x + p1 equals y select y).Single();"),
                    ("0209", "=> (from x in new[] {123} join p1 in new[] {1} on x equals p1 into u join y in new[] {p1} on x equals y into z select x).Single();"),
                    ("0210", "=> (from x in new[] {123} join p1 in new[] {1} on x equals p1 into u join y in new[] {0} on x equals y + p1 into z select x).Single();"),
                    ("0211", "=> (from x in new[] {0} join p1 in new[] {1} on x equals p1 into u join y in new[] {123} on x + p1 equals y into z select z.Single()).Single();"),
                    ("0212", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u group x by p1).Single().Key;"),
                    ("0213", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u group p1 by x).Single().Single();"),
                    ("0214", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u orderby p1 select x + 122).Single();"),
                    ("0215", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u orderby p1, x select x + 122).Single();"),
                    ("0216", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u orderby x, p1 select x + 122).Single();"),
                    ("0217", "=> (from x in new[] {1} join p1 in new[] {1} on x equals p1 into u select x into y select p1).Single();"),

                    ("0301", "=> (from p1 in new[] {1} join x in new[] {p1} on 1 equals 1 select x).Single();"),
                    ("0302", "=> (from p1 in new[] {1} join x in new[] {123} on 123 equals p1 select x).Single();"),
                    ("0303", "=> (from x in new[] {123} join p1 in new[] {1} on p1 equals 123 select x).Single();"),
                    ("0304", "=> (from p1 in new[] {1} join x in new[] {123} on p1 + 122 equals p1 select x).Single();"),
                    ("0305", "=> (from x in new[] {123} join p1 in new[] {1} on p1 equals p1 + 122 select x).Single();"),
                };

            foreach (var d in data)
            {
                yield return new object[] { d.tag, d.code };
            }
        }

        [Theory]
        [MemberData(nameof(ParameterCapturing_016_Query_MemberData))]
        public void ParameterCapturing_016_Query(string tag, string code)
        {
            _ = tag;

            var source = @"
using System.Linq;

class C1 (int p1)
{
    public int M1()
" + code + @"
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123);
        System.Console.Write(c1.M1());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics();

            Assert.Equal(1, comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_017_ColorColor_MemberAccess_Static_FieldPropertyEvent(
            [CombinatorialValues(
                @"const string Red = ""Red"";",
                @"static string Red { get; } = ""Red"";",
                @"static event System.Func<string> Red = () => ""Red"";",
                @"static string Red = ""Red"";"
                )]
            string member)
        {
            var source = @"
class Color
{
    public " + member + @"

    public class C1 (Color Color)
    {
        public object M1() {var val = Color.Red; return val; }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        object val = c1.M1();
        System.Console.Write(val is System.Func<string> d ? d() : val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_018_ColorColor_MemberAccess_Static_Method()
        {
            var source = @"
class Color
{
    public static string Red() => ""Red"";

    public class C1 (Color Color)
    {
        public object M1() => Color.Red();
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        object val = c1.M1();
        System.Console.Write(val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_019_ColorColor_MemberAccess_Type()
        {
            var source = @"
class Color
{
    public class Red;

    public class C1 (Color Color)
    {
        public object M1(object input)
        {
            switch(input)
            {
                case Color.Red: return ""Red"";
            }

            return ""Blue"";
        }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        object val = c1.M1(new Color.Red());
        System.Console.Write(val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_020_ColorColor_MemberAccess_Instance_FieldPropertyEvent(
            [CombinatorialValues(
                @"string Red { get; } = ""Red"";",
                @"event System.Func<string> Red = () => ""Red"";",
                @"string Red = ""Red"";"
                )]
            string member)
        {
            var source = @"
class Color
{
    public " + member + @"

    public class C1 (Color Color)
    {
        public object M1() {var val = Color.Red; return val; }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(new Color());
        object val = c1.M1();
        System.Console.Write(val is System.Func<string> d ? d() : val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_021_ColorColor_MemberAccess_Instance_Method()
        {
            var source = @"
class Color
{
    public string Red() => ""Red"";

    public class C1 (Color Color)
    {
        public object M1() => Color.Red();
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(new Color());
        object val = c1.M1();
        System.Console.Write(val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_022_ColorColor_MemberAccess_Instance_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""0"");
    }
    
    public void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""1"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"0").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_023_ColorColor_MemberAccess_InstanceAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_024_ColorColor_MemberAccess_InstanceAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public static void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""static"");
    }
    
    public void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""instance"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_025_ColorColor_MemberAccess_ExtensionAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1(this Color @this, S1 x, int y = 0)
    {
        System.Console.WriteLine(""extension"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_026_ColorColor_MemberAccess_ExtensionAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public static void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1<T>(this Color @this, T x) where T : unmanaged
    {
        System.Console.WriteLine(""extension"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_027_ColorColor_MemberAccess_InstanceInapplicableAndStaticApplicableDueToArguments_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(string.Empty);
    }
}

class Color
{
    public void M1(int x)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1(string x)
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(string.Empty);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_028_ColorColor_MemberAccess_InstanceApplicableAndStaticInapplicableDueToArguments_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(0);
    }
}

class Color
{
    public void M1(int x)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1(string x)
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(0);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_029_ColorColor_MemberAccess_ExtensionApplicableAndStaticInapplicableDueToArgument_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(string.Empty);
    }
}

class Color
{
    public static void M1(int x)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1(this Color @this, string x)
    {
        System.Console.WriteLine(""extension"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(string.Empty);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_030_ColorColor_MemberAccess_ExtensionInapplicableAndStaticApplicableDueToArguments_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(0);
    }
}

class Color
{
    public static void M1(int x)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1(this Color @this, string x)
    {
        System.Console.WriteLine(""extension"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(0);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_030_ColorColor_MemberAccess_Extension_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(string.Empty);
    }
}

class Color
{
}

static class Extension
{
    static public void M1(this Color @this, string x)
    {
        System.Console.WriteLine(""extension"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"extension").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_031_ColorColor_MemberAccess_InstanceAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x, int y = 0) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_032_ColorColor_MemberAccess_InstanceAndStatic_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
    
    public void M1<T>(T x, int y = 0) where T : unmanaged
    {
        System.Console.WriteLine(""instance"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_034_ColorColor_MemberAccess_InstanceAndStaticAmbiguity_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1<T>(T x, int y = 0)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            // If we treat receiver as value, we capture the parameter and 'S1' becomes managed. Then static method becomes inapplicable due to constraint and we would call instance method.
            // If we treat receiver as type, we don't capture the parameter and 'S1' remains unmanaged. Then both methods applicable, but we would call static method because optional parameter isn't needed for it.
            // Neither choice leads to an error, but each would result in distinct behavior.
            // We decided to treat this as an ambiguity.
            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_035_ColorColor_MemberAccess_ExtensionInapplicableBasedOnReceiverAndStaticApplicable_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(0);
    }
}

class Color
{
    public static void M1(int x)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1(this S1 @this, int x)
    {
        System.Console.WriteLine(""extension"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_036_ColorColor_MemberAccess_InstanceInapplicableAndStaticApplicableDueToArity_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1<int>(0);
    }
}

class Color
{
    public void M1(int x)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x)
    {
        System.Console.WriteLine(""static"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_037_ColorColor_MemberAccess_InstanceApplicableAndStaticInapplicableDueToArity_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1<int>(0);
    }
}

class Color
{
    public void M1<T>(T x)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1(int x)
    {
        System.Console.WriteLine(""static"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"instance").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_038_ColorColor_MemberAccess_ExtensionApplicableAndStaticInapplicableDueToArity_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1<int>(0);
    }
}

class Color
{
    public static void M1(int x)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1<T>(this Color @this, T x)
    {
        System.Console.WriteLine(""extension"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"extension").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_039_ColorColor_MemberAccess_ExtensionInapplicableAndStaticApplicableDueToArity_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1<int>(0);
    }
}

class Color
{
    public static void M1<T>(T x)
    {
        System.Console.WriteLine(""static"");
    }
}

static class Extension
{
    static public void M1(this Color @this, int x)
    {
        System.Console.WriteLine(""extension"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_040_ColorColor_MemberAccess_InstanceAndStatic_Nameof_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        System.Console.WriteLine(nameof(Color.M1));
    }
}

class Color
{
    public void M1(S1 x, int y = 0)
    {
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"M1").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_041_ColorColor_MemberAccess_ExtensionAndStatic_Nameof_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        System.Console.WriteLine(nameof(Color.M1));
    }
}

class Color
{
    public static void M1<T>(T x) where T : unmanaged
    {
    }
}

static class Extension
{
    static public void M1(this Color @this, S1 x, int y = 0)
    {
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"M1").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_042_ColorColor_MemberAccess_InstanceAndStatic_InStaticInitializer_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public static string F = Color.M1(new S1());
}

class Color
{
    public string M1(S1 x, int y = 0) => ""instance"";
    
    public static string M1<T>(T x) where T : unmanaged => ""static"";
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(S1.F);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_043_ColorColor_MemberAccess_InstanceAndStatic_InStaticInitializer_Method()
        {
            var source = @"
struct S1(Color Color)
{
    static int F = Color.M1(new S1());
}

class Color
{
    public int M1(S1 x) => 0;
    
    public static int M1<T>(T x) where T : unmanaged => 0;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17),
                // (4,20): error CS9105: Cannot use primary constructor parameter 'Color Color' in this context.
                //     static int F = Color.M1(new S1());
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "Color").WithArguments("Color Color").WithLocation(4, 20)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_044_ColorColor_MemberAccess_InstanceAndStatic_InStaticMethod_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public static string Test() => Color.M1(new S1());
}

class Color
{
    public string M1(S1 x, int y = 0) => ""instance"";
    
    public static string M1<T>(T x) where T : unmanaged => ""static"";
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(S1.Test());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_045_ColorColor_MemberAccess_InstanceAndStatic_InStaticMethod_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public static int Test() => Color.M1(new S1());
}

class Color
{
    public int M1(S1 x) => 0;
    
    public static int M1<T>(T x) where T : unmanaged => 0;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17),
                // (4,33): error CS9105: Cannot use primary constructor parameter 'Color Color' in this context.
                //     public static int Test() => Color.M1(new S1());
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "Color").WithArguments("Color Color").WithLocation(4, 33)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_046_ColorColor_MemberAccess_InstanceAndStatic_InInstanceInitializer_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public string F = Color.M1(new S1());
}

class Color
{
    public string M1(S1 x, int y = 0) => ""instance"";
    
    public static string M1<T>(T x) => ""static"";
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new S1(new Color()).F);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_047_ColorColor_MemberAccess_InstanceAndStatic_InInstanceInitializer_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public string F = Color.M1(new S1());
}

class Color
{
    public string M1(S1 x) => ""instance"";
    
    public static string M1<T>(T x) where T : unmanaged => ""static"";
}

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new S1(new Color()).F);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"instance").VerifyDiagnostics();

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_048_ColorColor_MemberAccess_InstanceAndStatic_InConstructorBody_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public S1() : this(new Color())
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_049_ColorColor_MemberAccess_InstanceAndStatic_InConstructorInitializer_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public S1() : this(
        Color.M1(
            new S1()))
    {
    }
}

class Color
{
    public Color M1(S1 x, int y = 0) => null;
    
    public static Color M1<T>(T x) where T : unmanaged => null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_050_ColorColor_MemberAccess_InstanceAndStatic_InBaseInitializer_Method()
        {
            var source = @"
class S1(Color Color)
    : Base(Color.M1(default(S1)))
{
}

class Color
{
    public string M1(S1 x, int y = 0) => ""instance"";
    
    public static string M1<T>(T x) => ""static"";
}

class Base
{
    public Base(string x)
    {
        System.Console.WriteLine(x);
    }
}

class Program
{
    static void Main()
    {
        _ = new S1(new Color());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,16): warning CS9113: Parameter 'Color' is unread.
                // class S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 16)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_051_ColorColor_MemberAccess_InstanceAndStatic_InBaseInitializer_Method()
        {
            var source = @"
class S1(Color Color)
    : Base(Color.M1(null))
{
}

class Color
{
    public string M1(S1 x) => ""instance"";
    
    public static string M1<T>(T x) => ""static"";
}

class Base
{
    public Base(string x)
    {
        System.Console.WriteLine(x);
    }
}

class Program
{
    static void Main()
    {
        _ = new S1(new Color());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"instance").VerifyDiagnostics();

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_052_ColorColor_QualifiedName_Static_FieldPropertyEvent(
            [CombinatorialValues(
                @"static string Red { get; } = ""Red"";",
                @"static event System.Func<string> Red = () => ""Red"";",
                @"static string Red = ""Red"";"
                )]
            string member)
        {
            var source = @"
class Color
{
    public " + member + @"

    public class C1 (Color Color)
    {
        public void M1(object x)
        {
            if (x is Color.Red)
            {}
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28),
                // (10,22): error CS0150: A constant value is expected
                //             if (x is Color.Red)
                Diagnostic(ErrorCode.ERR_ConstantExpected, "Color.Red").WithLocation(10, 22)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_053_ColorColor_MemberAccess_Constant()
        {
            var source = @"
class Color
{
    public const string Red = ""Red"";

    public class C1 (Color Color)
    {
        public void M1(object x)
        {
            if (x is Color.Red)
            {
                System.Console.Write(x);
            }
        }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        c1.M1(""Red"");
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_054_ColorColor_QualifiedName_Type()
        {
            var source = @"
class Color
{
    public class Red;

    public class C1 (Color Color)
    {
        public object M1(object input)
        {
            if (input is Color.Red)
            {
                return ""Red"";
            }

            return ""Blue"";
        }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new Color.C1(default);
        object val = c1.M1(new Color.Red());
        System.Console.Write(val);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"Red").VerifyDiagnostics(
                // (6,28): warning CS9113: Parameter 'Color' is unread.
                //     public class C1 (Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 28)
                );

            Assert.Empty(comp.GetTypeByMetadataName("Color+C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        public static IEnumerable<object[]> ParameterCapturing_055_ColorColor_Query_Method_MemberData()
        {
            var data = new (string tag, string method, string query)[]
                {
                    ("01", "Cast", "from int i in Color select i"),
                    ("02", "Select", "from i in Color select i"),
                    ("03", "SelectMany", "from i in Color from j in new[] {1} select i + j"),
                    ("04", "Select", "from i in Color let j = i + 1 select i + j"),
                    ("05", "Where", "from i in Color where i > 0 select i"),
                    ("06", "Join", "from i in Color join j in new[] {1} on i equals j select i + j"),
                    ("07", "GroupJoin", "from i in Color join j in new[] {1} on i equals j into g select i + g.Count()"),
                    ("08", "OrderBy", "from i in Color orderby i select i"),
                    ("09", "OrderBy", "from i in Color orderby i ascending select i"),
                    ("10", "OrderBy", "from i in Color orderby i, i select i"),
                    ("11", "OrderBy", "from i in Color orderby i ascending, i select i"),
                    ("12", "OrderByDescending", "from i in Color orderby i descending select i"),
                    ("13", "OrderByDescending", "from i in Color orderby i descending, i select i"),
                    ("14", "GroupBy1", "from i in Color group i by i"),
                    ("15", "GroupBy2", "from i in Color group i + 1 by i"),
                };

            foreach (var isStatic in new[] { false, true })
            {
                foreach (var d in data)
                {
                    yield return new object[] { isStatic, d.tag, d.method, d.query };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ParameterCapturing_055_ColorColor_Query_Method_MemberData))]
        public void ParameterCapturing_055_ColorColor_Query_Method(bool isStatic, string tag, string methodName, string query)
        {
            _ = tag;

            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;

public class C1 (Color Color)
{
    public object M1() => " + query + @";
}

public class Color
{
";
            if (isStatic)
            {
                source += "static ";
            }

            switch (methodName)
            {
                case "Cast":
                    source += @"
    public IEnumerable<T> Cast<T>()
    {
        System.Console.Write(""Cast"");
        return new T[] {};
    }
";
                    break;

                case "Where":
                    source += @"
    public IEnumerable<int> Where(Func<int, bool> predicate)
    {
        System.Console.Write(""Where"");
        return new int[] {};
    }
";
                    break;

                case "Select":
                    source += @"
    public IEnumerable<V> Select<V>(Func<int, V> selector)
    {
        System.Console.Write(""Select"");
        return new V[] {};
    }
";
                    break;

                case "SelectMany":
                    source += @"
    public IEnumerable<V> SelectMany<U, V>(Func<int, IEnumerable<U>> selector, Func<int,U,V> resultSelector)
    {
        System.Console.Write(""SelectMany"");
        return new V[] {};
    }
";
                    break;

                case "Join":
                    source += @"
	public IEnumerable<V> Join<U,K,V>(IEnumerable<U> inner, Func<int,K> outerKeySelector, Func<U,K> innerKeySelector, Func<int,U,V> resultSelector)
    {
        System.Console.Write(""Join"");
        return new V[] {};
    }
";
                    break;

                case "GroupJoin":
                    source += @"
	public IEnumerable<V> GroupJoin<U,K,V>(IEnumerable<U> inner, Func<int,K> outerKeySelector, Func<U,K> innerKeySelector, Func<int,IEnumerable<U>,V> resultSelector)
    {
        System.Console.Write(""GroupJoin"");
        return new V[] {};
    }
";
                    break;

                case "OrderBy":
                    source += @"
	public IOrderedEnumerable<int> OrderBy<K>(Func<int,K> keySelector)
    {
        System.Console.Write(""OrderBy"");
        return (new int[] {}).OrderBy(keySelector);
    }
";
                    break;

                case "OrderByDescending":
                    source += @"
	public IOrderedEnumerable<int> OrderByDescending<K>(Func<int,K> keySelector)
    {
        System.Console.Write(""OrderByDescending"");
        return (new int[] {}).OrderByDescending(keySelector);
    }
";
                    break;

                case "GroupBy1":
                    source += @"
	public IEnumerable<K> GroupBy<K>(Func<int,K> keySelector)
    {
        System.Console.Write(""GroupBy1"");
        return new K[] {};
    }
";
                    break;

                case "GroupBy2":
                    source += @"
	public IEnumerable<K> GroupBy<K,E>(Func<int,K> keySelector, Func<int,E> elementSelector)
    {
        System.Console.Write(""GroupBy2"");
        return new K[] {};
    }
";
                    break;
            }

            source += @"
}

class Program
{
    static void Main()
    {
        var c1 = new C1(" + (isStatic ? "default" : "new Color()") + @");
        c1.M1();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: methodName);
            var diagnostics = verifier.Diagnostics.Where(d => d.Code is not (int)ErrorCode.HDN_UnusedUsingDirective);

            if (isStatic)
            {
                diagnostics.Verify(
                    // (6,24): warning CS9113: Parameter 'Color' is unread.
                    // public class C1 (Color Color)
                    Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(6, 24)
                    );
            }
            else
            {
                diagnostics.Verify();
            }

            Assert.Equal(isStatic, comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().IsEmpty());
        }

        [Fact]
        public void ParameterCapturing_056_CapturingOfAManagedParameterMakesStructManaged()
        {
            var source = @"
struct S1(string p1)
{
    public string Test() => p1;
}

struct S2(string p1)
{
}

class Program
{
    static void Main()
    {
        Test(new S1());
        Test(new S2());
        Test(new S3());
    }

    static void Test<T>(T x) where T : unmanaged {}
}

struct S3(int p1)
{
    public int Test() => p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            comp.VerifyEmitDiagnostics(
                // (7,18): warning CS9113: Parameter 'p1' is unread.
                // struct S2(string p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 18),
                // (15,9): error CS8377: The type 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Program.Test<T>(T)'
                //         Test(new S1());
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "Test").WithArguments("Program.Test<T>(T)", "T", "S1").WithLocation(15, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
            Assert.Empty(comp.GetTypeByMetadataName("S2").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
            Assert.NotEmpty(comp.GetTypeByMetadataName("S3").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_057_ColorColor_MemberAccess_InstanceAndStatic_Property()
        {
            //  public class Color
            //  {
            //      public int P => 1;
            //      public static string P => "1";
            //  }
            var ilSource = @"
.class public auto ansi beforefieldinit Color
    extends System.Object
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void System.Object::.ctor()
        IL_0006: ret
    }

    .method public hidebysig specialname 
        instance int32 get_P () cil managed 
    {
        .maxstack 8

        IL_0000: ldc.i4.1
        IL_0001: ret
    }

    .property instance int32 P()
    {
        .get instance int32 Color::get_P()
    }

    .method public hidebysig specialname static 
        string get_P () cil managed 
    {
        .maxstack 8

        IL_0000: ldstr ""1""
        IL_0005: ret
    }

    .property string P()
    {
        .get string Color::get_P()
    }
}
";

            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        _ = Color.P;
    }
}
";
            var comp = CreateCompilationWithIL(source, ilSource, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,19): error CS0229: Ambiguity between 'Color.P' and 'Color.P'
                //         _ = Color.P;
                Diagnostic(ErrorCode.ERR_AmbigMember, "P").WithArguments("Color.P", "Color.P").WithLocation(6, 19)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_058_ColorColor_MemberAccess_InstanceAndStatic_Property()
        {
            var source = @"
interface I1
{
    int P => 1;
}

interface I2
{
    static string P => ""1"";
}

interface Color : I1, I2
{}

struct S1(Color Color)
{
    public void Test()
    {
        _ = Color.P;
    }
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (19,19): error CS0229: Ambiguity between 'I1.P' and 'I2.P'
                //         _ = Color.P;
                Diagnostic(ErrorCode.ERR_AmbigMember, "P").WithArguments("I1.P", "I2.P").WithLocation(19, 19)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_059_ColorColor_MemberAccess_InstanceAndExtension_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

class Color
{
    public void M1(S1 x, int y = 0)
    {
        System.Console.WriteLine(""0"");
    }
}

static class Extension
{
    static public void M1(this Color @this, S1 x)
    {
        System.Console.WriteLine(""extension"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"0").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_060_CaseDifferences()
        {
            var source1 = @"
class C1 (int p1, string P1)
{
    public string M1() => P1;
}

class C2 (int p2, string P2)
{
    public int M2() => p2;
}

class C3 (int p3, string P3)
{
    public string M31() => P3;
    public int M32() => p3;
}

class Program
{
    static void Main()
    {
        var c1 = new C1(1, ""_10_"");
        System.Console.Write(c1.M1());
        var c2 = new C2(2, ""_20_"");
        System.Console.Write(c2.M2());
        var c3 = new C3(3, ""_30_"");
        System.Console.Write(c3.M31());
        System.Console.Write(c3.M32());
    }
}
";
            var comp1 = CreateCompilation(source1, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp1, expectedOutput: @"_10_2_30_3").VerifyDiagnostics(
                // (2,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1, string P1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 15),
                // (7,26): warning CS9113: Parameter 'P2' is unread.
                // class C2 (int p2, string P2)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "P2").WithArguments("P2").WithLocation(7, 26)
                );

            Assert.Equal("System.String P1", comp1.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Single().Key.ToTestDisplayString());
            Assert.Equal("System.Int32 p2", comp1.GetTypeByMetadataName("C2").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Single().Key.ToTestDisplayString());
            Assert.Equal(2, comp1.GetTypeByMetadataName("C3").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters().Count);

            var source2 = @"
class C1 (int p1)
{
    public string M1() => P1;
}

class C2 (string P2)
{
    public int M2() => p2;
}
";
            var comp2 = CreateCompilation(source2);

            comp2.VerifyEmitDiagnostics(
                // (2,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 15),
                // (4,27): error CS0103: The name 'P1' does not exist in the current context
                //     public string M1() => P1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P1").WithArguments("P1").WithLocation(4, 27),
                // (7,18): warning CS9113: Parameter 'P2' is unread.
                // class C2 (string P2)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "P2").WithArguments("P2").WithLocation(7, 18),
                // (9,24): error CS0103: The name 'p2' does not exist in the current context
                //     public int M2() => p2;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(9, 24)
                );

            Assert.Empty(comp2.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
            Assert.Empty(comp2.GetTypeByMetadataName("C2").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_061_ColorColor_MemberAccess_InstanceAndStaticDisambiguation_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        (Color).M1(this);
    }
}

class Color
{
    public void M1<T>(T x, int y = 0)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"instance").VerifyDiagnostics();

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_062_ColorColor_MemberAccess_InstanceAndStaticDisambiguation_Method()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        global::Color.M1(this);
    }
}

class Color
{
    public void M1<T>(T x, int y = 0)
    {
        System.Console.WriteLine(""instance"");
    }
    
    public static void M1<T>(T x) where T : unmanaged
    {
        System.Console.WriteLine(""static"");
    }
}

class Program
{
    static void Main()
    {
        new S1(new Color()).Test();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"static").VerifyDiagnostics(
                // (2,17): warning CS9113: Parameter 'Color' is unread.
                // struct S1(Color Color)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Color").WithArguments("Color").WithLocation(2, 17)
                );

            Assert.Empty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_063_ColorColor_MemberAccess_InstanceAndStaticAmbiguity_Method_ApplicabilityDueToArgumentsNotConsidered()
        {
            var source = @"
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
        Color.M2(this);
    }
}

class Color
{
    public void M1<T>(T x, int y)
    {
    }
    
    public static void M1<T>(T x)
    {
    }

    public void M2<T>(T x)
    {
    }
    
    public static void M2<T>(T x, int y)
    {
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(6, 9),
                // (7,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M2(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(7, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_064_OnlyCapturedParameterUsedInLambda_InPrimaryConstructor()
        {
            var source = @"
partial class C1
{
    public System.Func<int> F1 = Execute1(() => p1++);
    public int F2 = p2;
}

partial class C1 (int p1, int p2)
{
    public int M1() { return p1++; }

    public static int F3;

    static System.Func<int> Execute1(System.Func<int> f)
    {
        F3 = f();
        return f;
    }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1);
        System.Console.Write(C1.F3);
        System.Console.Write(c1.F1());
        System.Console.Write(c1.M1());
        System.Console.Write(c1.F1());
        System.Console.Write(c1.F2);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"123124125126-1", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C1..ctor(int, int)",
@"
{
  // Code size       44 (0x2c)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C1.<p1>P""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.0
  IL_0009:  ldftn      ""int C1.<.ctor>b__2_0()""
  IL_000f:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0014:  call       ""System.Func<int> C1.Execute1(System.Func<int>)""
  IL_0019:  stfld      ""System.Func<int> C1.F1""
  IL_001e:  ldarg.0
  IL_001f:  ldarg.2
  IL_0020:  stfld      ""int C1.F2""
  IL_0025:  ldarg.0
  IL_0026:  call       ""object..ctor()""
  IL_002b:  ret
}
");
        }

        [Fact]
        public void ParameterCapturing_065_CapturedAndNotCapturedParameterUsedInLambda_InPrimaryConstructor()
        {
            var source = @"
partial class C1
{
    public System.Func<int> F1 = Execute1(() => p1++);
    public System.Func<int> F2 = Execute2(() => p2--);
}

partial class C1 (int p1, int p2)
{
    public int M1() { return p1++; }

    public static int F3;
    public static int F4;

    static System.Func<int> Execute1(System.Func<int> f)
    {
        F3 = f();
        return f;
    }

    static System.Func<int> Execute2(System.Func<int> f)
    {
        F4 = f();
        return f;
    }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1);
        System.Console.Write(C1.F3);
        System.Console.Write(c1.F1());
        System.Console.Write(c1.M1());
        System.Console.Write(c1.F1());
        System.Console.Write(C1.F4);
        System.Console.Write(c1.F2());
        System.Console.Write(c1.F2());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"123124125126-1-2-3", verify: Verification.Fails).VerifyDiagnostics();

            verifier.VerifyIL("C1..ctor(int, int)",
@"
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (C1.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C1.<p1>P""
  IL_0007:  newobj     ""C1.<>c__DisplayClass2_0..ctor()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldarg.0
  IL_000f:  stfld      ""C1 C1.<>c__DisplayClass2_0.<>4__this""
  IL_0014:  ldloc.0
  IL_0015:  ldarg.2
  IL_0016:  stfld      ""int C1.<>c__DisplayClass2_0.p2""
  IL_001b:  ldarg.0
  IL_001c:  ldloc.0
  IL_001d:  ldftn      ""int C1.<>c__DisplayClass2_0.<.ctor>b__0()""
  IL_0023:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0028:  call       ""System.Func<int> C1.Execute1(System.Func<int>)""
  IL_002d:  stfld      ""System.Func<int> C1.F1""
  IL_0032:  ldarg.0
  IL_0033:  ldloc.0
  IL_0034:  ldftn      ""int C1.<>c__DisplayClass2_0.<.ctor>b__1()""
  IL_003a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_003f:  call       ""System.Func<int> C1.Execute2(System.Func<int>)""
  IL_0044:  stfld      ""System.Func<int> C1.F2""
  IL_0049:  ldarg.0
  IL_004a:  call       ""object..ctor()""
  IL_004f:  ret
}
");
        }

        [Fact]
        public void ParameterCapturing_066_OnlyNotCapturedParameterUsedInLambda_InPrimaryConstructor()
        {
            var source = @"
partial class C1
{
    public int F1 = p1;
    public System.Func<int> F2 = Execute2(() => p2--);
}

partial class C1 (int p1, int p2)
{
    public int M1() { return p1++; }

    public static int F4;

    static System.Func<int> Execute2(System.Func<int> f)
    {
        F4 = f();
        return f;
    }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1);
        System.Console.Write(c1.F1);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.M1());
        System.Console.Write(C1.F4);
        System.Console.Write(c1.F2());
        System.Console.Write(c1.F2());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"123123124-1-2-3", verify: Verification.Passes).VerifyDiagnostics(
                // (4,21): warning CS9124: Parameter 'int p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
                //     public int F1 = p1;
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, "p1").WithArguments("int p1").WithLocation(4, 21)
                );

            verifier.VerifyIL("C1..ctor(int, int)",
@"
{
  // Code size       62 (0x3e)
  .maxstack  3
  .locals init (C1.<>c__DisplayClass2_0 V_0) //CS$<>8__locals0
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int C1.<p1>P""
  IL_0007:  newobj     ""C1.<>c__DisplayClass2_0..ctor()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ldarg.2
  IL_000f:  stfld      ""int C1.<>c__DisplayClass2_0.p2""
  IL_0014:  ldarg.0
  IL_0015:  ldarg.0
  IL_0016:  ldfld      ""int C1.<p1>P""
  IL_001b:  stfld      ""int C1.F1""
  IL_0020:  ldarg.0
  IL_0021:  ldloc.0
  IL_0022:  ldftn      ""int C1.<>c__DisplayClass2_0.<.ctor>b__0()""
  IL_0028:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_002d:  call       ""System.Func<int> C1.Execute2(System.Func<int>)""
  IL_0032:  stfld      ""System.Func<int> C1.F2""
  IL_0037:  ldarg.0
  IL_0038:  call       ""object..ctor()""
  IL_003d:  ret
}
");
        }

        [Fact]
        public void ParameterCapturing_067_CapturedAndPassedToBase_ByValue()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(int p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,25): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1) : Base(p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(2, 25)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_068_CapturedAndPassedToBase_In()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class C2(int p2) : Base(in p2)
{
    void M()
    {
        _ = p2; 
    }
}

class Base
{
    public Base(in int p){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,25): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1) : Base(p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(2, 25),
                // (10,28): warning CS9107: Parameter 'int p2' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C2(int p2) : Base(in p2)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p2").WithArguments("int p2").WithLocation(10, 28)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_069_CapturedAndPassedToBase_Ref()
        {
            var source = @"
class C1(int p1) : Base(ref p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(ref int p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_070_CapturedAndPassedToBase_Out()
        {
            var source = @"
class C1(int p1) : Base(out p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(out int p1){ throw null; }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_071_CapturedAndPassedToBase_Multiple()
        {
            var source = @"
class C1(int p1, int p2) : Base(p1, in p2)
{
    void M()
    {
        _ = p1; 
        _ = p2; 
    }
}

class Base
{
    public Base(int p1, in int p2){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,33): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1, int p2) : Base(p1, in p2)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(2, 33),
                // (2,40): warning CS9107: Parameter 'int p2' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1, int p2) : Base(p1, in p2)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p2").WithArguments("int p2").WithLocation(2, 40)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_072_CapturedAndPassedToBase_NonIdentityConversion()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(long p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_073_CapturedAndPassedToBase_IdentityConversion()
        {
            var source = @"
class C1(int p1) : Base((int)p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(int p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,30): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1) : Base((int)p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(2, 30)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_074_CapturedAndPassedToBase_Params_Expanded()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class C2(int p2) : Base(1, p2)
{
    void M()
    {
        _ = p2; 
    }
}

class C3(int p3) : Base(1, 2, p3)
{
    void M()
    {
        _ = p3; 
    }
}

class Base
{
    public Base(params int[] p){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_075_CapturedAndPassedToBase_Params_NotExpanded()
        {
            var source = @"
class C1(int[] p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(params int[] p){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,27): warning CS9107: Parameter 'int[] p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int[] p1) : Base(p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int[] p1").WithLocation(2, 27)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_076_CapturedAndPassedToBase_InExpression()
        {
            var source = @"
class C1(int p1) : Base(p1 + 0)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(int p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_077_CapturedAndPassedToBase_NonIdentityConversion()
        {
            var source = @"
class C1(string p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(object p1){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_078_CapturedAndPassedToBase_Params_Expanded()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class C2(int p1) : Base(p1, 1)
{
    void M()
    {
        _ = p1; 
    }
}

class C3(int p1) : Base(p1, 1, 2)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(int p1, params int[] p2){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (2,25): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C1(int p1) : Base(p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(2, 25),
                // (10,25): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C2(int p1) : Base(p1, 1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(10, 25),
                // (18,25): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // class C3(int p1) : Base(p1, 1, 2)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(18, 25)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_079_CapturedAndPassedToBase_Params_Expanded()
        {
            var source = @"
class C1(int p1) : Base(p1)
{
    void M()
    {
        _ = p1; 
    }
}

class C2(int p1) : Base(p1, 1)
{
    void M()
    {
        _ = p1; 
    }
}

class C3(int p1) : Base(p1, 1, 2)
{
    void M()
    {
        _ = p1; 
    }
}

class Base
{
    public Base(long p1, params int[] p2){}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_080_NullableAnalysis()
        {
            var source = @"
#nullable enable
class C1(string? p1, string p2)
{
    void M1()
    {
#line 2000
        p1.ToString();
        p1 = """";
        p1.ToString();
    }

    void M2()
    {
        p2.ToString();
    }

    void M3()
    {
#line 4000
        p1.ToString();
        p1 = """";
        p1.ToString();
    }

    void M4()
    {
        p1 = """";
        p1.ToString();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2000,9): warning CS8602: Dereference of a possibly null reference.
                //         p1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "p1").WithLocation(2000, 9),
                // (4000,9): warning CS8602: Dereference of a possibly null reference.
                //         p1.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "p1").WithLocation(4000, 9)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_081_AddressOfFixedSizeBuffer([CombinatorialValues("class", "struct")] string keyword)
        {
            CreateCompilation(@"
unsafe struct S
{
    public fixed int Buf[1];
}

unsafe " + keyword + @" C(S s)
{
    S s_f;
    void M()
    {
        fixed (int* a = &s.Buf) {}
        fixed (int* b = &s_f.Buf) {}
        int* c = &s.Buf;
        int* d = &s_f.Buf;
    }
}", options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,26): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         fixed (int* a = &s.Buf) {}
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s.Buf").WithLocation(12, 26),
                // (13,26): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         fixed (int* b = &s_f.Buf) {}
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s_f.Buf").WithLocation(13, 26),
                // (14,19): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int* c = &s.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s.Buf").WithLocation(14, 19),
                // (15,19): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int* d = &s_f.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s_f.Buf").WithLocation(15, 19)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_082_FixedFixedSizeBuffer([CombinatorialValues("class", "struct")] string keyword)
        {
            CreateCompilation(@"
unsafe struct S
{
    public fixed int Buf[1];
}

unsafe " + keyword + @" C(S s)
{
    S s_f;
    void M()
    {
        int* c = s.Buf;
        int* d = s_f.Buf;
    }
}", options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,18): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int* c = s.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s.Buf").WithLocation(12, 18),
                // (13,18): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int* d = s_f.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s_f.Buf").WithLocation(13, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_083_FixedFixedSizeBuffer([CombinatorialValues("class", "struct")] string keyword)
        {
            CreateCompilation(@"
unsafe struct S
{
    public fixed int Buf[1];
}

unsafe " + keyword + @" C(S s)
{
    S s_f;
    void M()
    {
        fixed (int* a = s.Buf) {}
        fixed (int* b = s_f.Buf) {}
    }
}", options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // The warning is unexpected - https://github.com/dotnet/roslyn/issues/66487
                // (9,7): warning CS0169: The field 'C.s_f' is never used
                //     S s_f;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s_f").WithArguments("C.s_f").WithLocation(9, 7)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_084_NoPointerDerefMoveableFixedSizeBuffer([CombinatorialValues("class", "struct")] string keyword)
        {
            CreateCompilation(@"
unsafe struct S
{
    public fixed int Buf[1];
}

unsafe " + keyword + @" C(S s)
{
    S s_f;
    void M()
    {
        int x = *s.Buf;
        int y = *s_f.Buf;
    }
}", options: TestOptions.UnsafeDebugDll).VerifyEmitDiagnostics(
                // (12,18): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int x = *s.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s.Buf").WithLocation(12, 18),
                // (13,18): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //         int y = *s_f.Buf;
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "s_f.Buf").WithLocation(13, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_085_AddressOfVariablesThatRequireFixing([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C(int s)
{
    int s_f;
    void M()
    {
        int* c = &s;
        int* d = &s_f;
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (7,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* c = &s;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s").WithLocation(7, 18),
                // (8,18): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //         int* d = &s_f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s_f").WithLocation(8, 18)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_086_AddressOfVariablesThatRequireFixing([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C(int s)
{
    int s_f;
    void M()
    {
        fixed (int* a = &s) {}
        fixed (int* b = &s_f) {}
    }
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void AddressOfParameters_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int* p1 = &x;
    int* p2 = &s.f;
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // Warnings are not expected https://github.com/dotnet/roslyn/issues/66495
                // (2,22): warning CS9113: Parameter 'x' is unread.
                // unsafe class  C1(int x, S s)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 22),
                // (2,27): warning CS9113: Parameter 's' is unread.
                // unsafe class  C1(int x, S s)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "s").WithArguments("s").WithLocation(2, 27)
                );
        }

        [Fact]
        public void AddressOfParameters_02()
        {
            var text = @"
unsafe class Base
{
    public Base(int* x, int* y) {}
}

unsafe class C1(int x, S s) : Base(&x, &s.f);

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // Warnings are not expected https://github.com/dotnet/roslyn/issues/66495
                // (7,21): warning CS9113: Parameter 'x' is unread.
                // unsafe class C1(int x, S s) : Base(&x, &s.f);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(7, 21),
                // (7,26): warning CS9113: Parameter 's' is unread.
                // unsafe class C1(int x, S s) : Base(&x, &s.f);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "s").WithArguments("s").WithLocation(7, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_087_AddressOfParameters([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int* p1 = &x;
    int* p2 = &s.f;

    void M()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (4,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p1 = &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(4, 15),
                // (5,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(5, 15)
                );
        }

        [Fact]
        public void ParameterCapturing_088_AddressOfParameters()
        {
            var text = @"
unsafe class Base
{
    public Base(int* x, int* y) {}
}

unsafe class C1(int x, S s) : Base(&x, &s.f)
{
    void M()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (7,36): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(7, 36),
                // (7,40): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(7, 40)
                );
        }

        [Theory]
        [CombinatorialData]
        public void AddressOfCapturedParameters_InLambdaOnly_01([CombinatorialValues("class ", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int y = M(() => 
              {
                  int* p1 = &x;
                  int* p2 = &s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (6,29): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //                   int* p1 = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x").WithLocation(6, 29),
                // (7,29): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //                   int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.f").WithArguments("s").WithLocation(7, 29),

                // The following warnings are not expected https://github.com/dotnet/roslyn/issues/66495
                // (2,22): warning CS9113: Parameter 'x' is unread.
                // unsafe class  C1(int x, S s)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 22),
                // (2,27): warning CS9113: Parameter 's' is unread.
                // unsafe class  C1(int x, S s)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "s").WithArguments("s").WithLocation(2, 27)
                );
        }

        [Fact]
        public void AddressOfCapturedParameters_InLambdaOnly_02()
        {
            var text = @"
class Base(System.Action a)
{
    System.Action aa = a;
}

unsafe class C1(int x, S s) : Base(() => 
                                   {
                                       int* p1 = &x;
                                       int* p2 = &s.f;
                                   });

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (9,50): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //                                        int* p1 = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x").WithLocation(9, 50),
                // (10,50): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //                                        int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.f").WithArguments("s").WithLocation(10, 50),

                // The following warnings are not expected https://github.com/dotnet/roslyn/issues/66495
                // (7,21): warning CS9113: Parameter 'x' is unread.
                // unsafe class C1(int x, S s) : Base(() => 
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(7, 21),
                // (7,26): warning CS9113: Parameter 's' is unread.
                // unsafe class C1(int x, S s) : Base(() => 
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "s").WithArguments("s").WithLocation(7, 26)
                );
        }

        [Theory]
        [CombinatorialData]
        public void AddressOfCapturedParameters_InLambdaOnly_03([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int* p1 = &x;
    int* p2 = &s.f;

    int y = M(() => 
              {
                  _ = x + s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (4,15): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //     int* p1 = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x").WithLocation(4, 15),
                // (5,15): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //     int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.f").WithArguments("s").WithLocation(5, 15)
                );
        }

        [Fact]
        public void AddressOfCapturedParameters_InLambdaOnly_04()
        {
            var text = @"
class Base(System.Action a)
{
    System.Action aa = a;
}

unsafe class C1(int x, S s) : Base(() => 
                                   {
                                       _ = x + s.f;
                                   })
{
    int* p1 = &x;
    int* p2 = &s.f;
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (12,15): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //     int* p1 = &x;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x").WithLocation(12, 15),
                // (13,15): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                //     int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.f").WithArguments("s").WithLocation(13, 15)
                );
        }

        [Fact]
        public void AddressOfCapturedParameters_InLambdaOnly_05()
        {
            var text = @"
unsafe class Base
{
    public Base(int* x, int* y) {}
}

unsafe class C1(int x, S s) : Base(&x, &s.f)
{
    int y = M(() => 
              {
                  _ = x + s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (7,36): error CS1686: Local 'x' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&x").WithArguments("x").WithLocation(7, 36),
                // (7,40): error CS1686: Local 's' or its members cannot have their address taken and be used inside an anonymous method or lambda expression
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_LocalCantBeFixedAndHoisted, "&s.f").WithArguments("s").WithLocation(7, 40)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_089_AddressOfCapturedParameters([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int y = M(() => 
              {
                  int* p1 = &x;
                  int* p2 = &s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }

    void M1()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            var expected = new[] {
                // (6,29): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //                   int* p1 = &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(6, 29),
                // (7,29): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //                   int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(7, 29)
                };

            if (keyword == "struct")
            {
                expected = expected.Concat(
                    // (6,30): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                    //                   int* p1 = &x;
                    Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "x").WithLocation(6, 30)
                    ).Concat(
                    // (7,30): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                    //                   int* p2 = &s.f;
                    Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "s").WithLocation(7, 30)
                    ).ToArray();
            }

            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_090_AddressOfCapturedParameters()
        {
            var text = @"
class Base(System.Action a)
{
    System.Action aa = a;
}

unsafe class C1(int x, S s) : Base(() => 
                                   {
                                       int* p1 = &x;
                                       int* p2 = &s.f;
                                   })
{

    void M1()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (9,50): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //                                        int* p1 = &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(9, 50),
                // (10,50): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //                                        int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(10, 50)
                );
        }

        [Theory]
        [CombinatorialData]
        public void ParameterCapturing_091_AddressOfCapturedParameters([CombinatorialValues("class", "struct")] string keyword)
        {
            var text = @"
unsafe " + keyword + @" C1(int x, S s)
{
    int* p1 = &x;
    int* p2 = &s.f;

    int y = M(() => 
              {
                  _ = x + s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }

    void M1()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            var expected = new[] {
                // (4,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p1 = &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(4, 15),
                // (5,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(5, 15)
                };

            if (keyword == "struct")
            {
                expected = expected.Concat(
                    // (9,23): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                    //                   _ = x + s.f;
                    Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "x").WithLocation(9, 23)
                    ).Concat(
                    // (9,27): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                    //                   _ = x + s.f;
                    Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "s").WithLocation(9, 27)
                    ).ToArray();
            }

            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_092_AddressOfCapturedParameters()
        {
            var text = @"
class Base(System.Action a)
{
    System.Action aa = a;
}

unsafe class C1(int x, S s) : Base(() => 
                                   {
                                       _ = x + s.f;
                                   })
{
    int* p1 = &x;
    int* p2 = &s.f;

    void M1()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (12,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p1 = &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(12, 15),
                // (13,15): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     int* p2 = &s.f;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(13, 15)
                );
        }

        [Fact]
        public void ParameterCapturing_093_AddressOfCapturedParameters()
        {
            var text = @"
unsafe class Base
{
    public Base(int* x, int* y) {}
}

unsafe class C1(int x, S s) : Base(&x, &s.f)
{
    int y = M(() => 
              {
                  _ = x + s.f;
              });

    static int M(System.Action a)
    {
        return 0;
    }

    void M1()
    {
        _ = x + s.f;
    }
}

struct S
{
    public int f;    
}
";
            CreateCompilation(text, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(
                // (7,36): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(7, 36),
                // (7,40): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // unsafe class C1(int x, S s) : Base(&x, &s.f)
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&s.f").WithLocation(7, 40)
                );
        }

        [Fact]
        public void ParameterCapturing_094_DefiniteAssignment()
        {
            var text = @"
class C1(int x, S s, string y)
{
    void M1()
    {
        _ = x + s.f + y.Length;
    }

    void M2()
    {
        _ = s.f + y.Length + x;
    }

    void M3()
    {
        _ = y.Length + x + s.f;
    }

    void M4()
    {
        _ = x;
    }

    void M5()
    {
        _ = s.f;
    }

    void M6()
    {
        _ = y.Length;
    }
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_095_DefiniteAssignment()
        {
            var text = @"
class C1(out int x, out S s, out string y)
{
    void M1()
    {
        _ = x + s.f + y.Length;
    }

    void M2()
    {
        _ = s.f + y.Length + x;
    }

    void M3()
    {
        _ = y.Length + x + s.f;
    }

    void M4()
    {
        _ = x;
    }

    void M5()
    {
        _ = s.f;
    }

    void M6()
    {
        _ = y.Length;
    }
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (2,7): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("x").WithLocation(2, 7),
                // (2,7): error CS0177: The out parameter 's' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("s").WithLocation(2, 7),
                // (2,7): error CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("y").WithLocation(2, 7),
                // (6,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(6, 13),
                // (6,17): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(6, 17),
                // (6,23): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(6, 23),
                // (11,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(11, 13),
                // (11,19): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(11, 19),
                // (11,30): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(11, 30),
                // (16,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(16, 13),
                // (16,24): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(16, 24),
                // (16,28): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(16, 28),
                // (21,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(21, 13),
                // (26,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(26, 13),
                // (31,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(31, 13)
                );
        }

        [Fact]
        public void DefiniteAssignment_01()
        {
            var text = @"
class C1(out int x, out S s, out string y)
{
    int f1 = x + s.f + y.Length;
    int f2 = s.f + y.Length + x;
    int f3 = y.Length + x + s.f;
    int f4 = x;
    int f5 = s.f;
    int f6 = y.Length;
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (2,7): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("x").WithLocation(2, 7),
                // (2,7): error CS0177: The out parameter 's' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("s").WithLocation(2, 7),
                // (2,7): error CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("y").WithLocation(2, 7),
                // (4,14): error CS0269: Use of unassigned out parameter 'x'
                //     int f1 = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x").WithArguments("x").WithLocation(4, 14),
                // (4,18): error CS0170: Use of possibly unassigned field 'f'
                //     int f1 = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.f").WithArguments("f").WithLocation(4, 18),
                // (4,24): error CS0269: Use of unassigned out parameter 'y'
                //     int f1 = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y").WithLocation(4, 24)
                );
        }

        [Fact]
        public void DefiniteAssignment_02()
        {
            var text = @"
class C1(out int x, out S s, out string y)
{
    int xx = x = 1;
    S ss = s = default;
    string yy = y = """";

    int f1 = x + s.f + y.Length;
    int f2 = s.f + y.Length + x;
    int f3 = y.Length + x + s.f;
    int f4 = x;
    int f5 = s.f;
    int f6 = y.Length;
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_03()
        {
            var text = @"
class C1(out int x, out int y)
{
    int f1 = y;
    int xx = x = 1;
    int f2 = x;
    int yy = y = 1;
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (4,14): error CS0269: Use of unassigned out parameter 'y'
                //     int f1 = y;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y").WithLocation(4, 14)
                );
        }

        [Fact]
        public void DefiniteAssignment_04()
        {
            var text = @"
class Base
{
    public Base(int x, int y, int z) {}
}

class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (7,7): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("x").WithLocation(7, 7),
                // (7,7): error CS0177: The out parameter 's' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("s").WithLocation(7, 7),
                // (7,7): error CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "C1").WithArguments("y").WithLocation(7, 7),
                // (7,51): error CS0269: Use of unassigned out parameter 'x'
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x").WithArguments("x").WithLocation(7, 51),
                // (7,54): error CS0170: Use of possibly unassigned field 'f'
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.f").WithArguments("f").WithLocation(7, 54),
                // (7,59): error CS0269: Use of unassigned out parameter 'y'
                // class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y").WithLocation(7, 59)
                );
        }

        [Fact]
        public void DefiniteAssignment_05()
        {
            var text = @"
class Base
{
    public Base(int x, int y, int z) {}
}

partial class C1(out int x, out S s, out string y) : Base(x, s.f, y.Length);

partial class C1
{
    int xx = x = 1;
    S ss = s = default;
    string yy = y = """";
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_06()
        {
            var text = @"
class Base
{
    public Base(int x) {}
}

class C1(out int x) : Base(x = 1);
";
            CreateCompilation(text).VerifyEmitDiagnostics();
        }

        [Fact]
        public void DefiniteAssignment_07()
        {
            var text1 = @"
partial class C1(out int x, out int y)
{
    int f1 = y;
    int xx = x = 1;
}
";

            var text2 = @"
partial class C1
{
    int f2 = x;
    int yy = y = 1;
}
";
            CreateCompilation(new[] { text1, text2 }).VerifyEmitDiagnostics(
                // (4,14): error CS0269: Use of unassigned out parameter 'y'
                //     int f1 = y;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "y").WithArguments("y").WithLocation(4, 14)
                );

            CreateCompilation(new[] { text2, text1 }).VerifyEmitDiagnostics(
                // (4,14): error CS0269: Use of unassigned out parameter 'x'
                //     int f2 = x;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "x").WithArguments("x").WithLocation(4, 14)
                );
        }

        [Fact]
        public void ParameterCapturing_096_NullableAnalysis_LocalFunction()
        {
            var source =
@"
#nullable enable

class C(string? x)
{
    void F1()
    {
        x = """";
        f();
        x = """";
        g();
        void f()
        {
            x.ToString(); // warn
            x = null;
            f();
        }
        void g()
        {
            x.ToString();
            x = null;
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(14, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_097_NullableAnalysis_LocalFunction()
        {
            var source =
@"
#nullable enable

class C(string? x)
{
    void F1()
    {
        x = """";
        f();
        h();
        void f()
        {
            x.ToString();
        }
        void g()
        {
            x.ToString(); // warn
        }
        void h()
        {
            x = null;
            g();
        }
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (17,13): warning CS8602: Dereference of a possibly null reference.
                //             x.ToString(); // warn
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(17, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_098_Lock()
        {
            var source = @"
class C1 (string p1)
{
    public void M1()
    {
        lock (p1)
        {
            System.Console.Write(p1);
            p1 = null;
        }
    }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(""123"");
        c1.M1();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics(
                // (9,13): warning CS0728: Possibly incorrect assignment to local 'p1' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                //             p1 = null;
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "p1").WithArguments("p1").WithLocation(9, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_099_Using()
        {
            var source = @"
class C1 (System.IDisposable p1)
{
    public void M1()
    {
        using (p1)
        {
            p1 = null;
        }
    }
}

class MyDisposable : System.IDisposable
{
    public void Dispose()
    {
        System.Console.Write(""disposed"");
    }
}

class Program
{
    static void Main()
    {
        var c1 = new C1(new MyDisposable());
        c1.M1();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"disposed").VerifyDiagnostics(
                // (8,13): warning CS0728: Possibly incorrect assignment to local 'p1' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                //             p1 = null;
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "p1").WithArguments("p1").WithLocation(8, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_099_MultiplePathsToNode_SwitchDispatch_02()
        {
            var source = @"
using System;

class Program(string x, int y)
{
    static void Main()
    {
        Console.Write(new Program("""", 0).M0()); // 0
        Console.Write(new Program("""", 1).M0()); // 1
        Console.Write(new Program("""", 2).M0()); // 2
        Console.Write(new Program("""", 3).M0()); // 3
        Console.Write(new Program(""a"", 2).M0()); // 2
        Console.Write(new Program(""a"", 10).M0()); // 3
    }

    int M0()
    {
        return (x, y) switch
        {
            ("""", 0) => M1(0),
            ("""", 1) => M1(1),
            (_, 2) => M1(2),
            _ => M1(3)
        };
    }

    static int M1(int z)
    {
        Console.Write(' ');
        return z;
    }
}
";
            var verifier = CompileAndVerify(source, expectedOutput: " 0 1 2 3 2 3", options: TestOptions.DebugExe);
            verifier.VerifyDiagnostics();

            verifier.VerifyIL("Program.M0", @"{
  // Code size      104 (0x68)
  .maxstack  2
  .locals init (int V_0,
                string V_1,
                int V_2,
                int V_3)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""string Program.<x>P""
  IL_0007:  stloc.1
  IL_0008:  ldarg.0
  IL_0009:  ldfld      ""int Program.<y>P""
  IL_000e:  stloc.2
  IL_000f:  ldc.i4.1
  IL_0010:  brtrue.s   IL_0013
  IL_0012:  nop
  IL_0013:  ldloc.1
  IL_0014:  ldstr      """"
  IL_0019:  call       ""bool string.op_Equality(string, string)""
  IL_001e:  brfalse.s  IL_0034
  IL_0020:  ldloc.2
  IL_0021:  switch    (
        IL_003a,
        IL_0043,
        IL_004c)
  IL_0032:  br.s       IL_0055
  IL_0034:  ldloc.2
  IL_0035:  ldc.i4.2
  IL_0036:  beq.s      IL_004c
  IL_0038:  br.s       IL_0055
  IL_003a:  ldc.i4.0
  IL_003b:  call       ""int Program.M1(int)""
  IL_0040:  stloc.0
  IL_0041:  br.s       IL_005e
  IL_0043:  ldc.i4.1
  IL_0044:  call       ""int Program.M1(int)""
  IL_0049:  stloc.0
  IL_004a:  br.s       IL_005e
  IL_004c:  ldc.i4.2
  IL_004d:  call       ""int Program.M1(int)""
  IL_0052:  stloc.0
  IL_0053:  br.s       IL_005e
  IL_0055:  ldc.i4.3
  IL_0056:  call       ""int Program.M1(int)""
  IL_005b:  stloc.0
  IL_005c:  br.s       IL_005e
  IL_005e:  ldc.i4.1
  IL_005f:  brtrue.s   IL_0062
  IL_0061:  nop
  IL_0062:  ldloc.0
  IL_0063:  stloc.3
  IL_0064:  br.s       IL_0066
  IL_0066:  ldloc.3
  IL_0067:  ret
}");
        }

        [Fact]
        public void ParameterCapturing_100_DefiniteAssignment()
        {
            var text = @"
class C1(ref int x, ref S s, ref string y)
{
    void M1()
    {
        _ = x + s.f + y.Length;
    }

    void M2()
    {
        _ = s.f + y.Length + x;
    }

    void M3()
    {
        _ = y.Length + x + s.f;
    }

    void M4()
    {
        _ = x;
    }

    void M5()
    {
        _ = s.f;
    }

    void M6()
    {
        _ = y.Length;
    }
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(6, 13),
                // (6,17): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(6, 17),
                // (6,23): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(6, 23),
                // (11,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(11, 13),
                // (11,19): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(11, 19),
                // (11,30): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(11, 30),
                // (16,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(16, 13),
                // (16,24): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(16, 24),
                // (16,28): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(16, 28),
                // (21,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(21, 13),
                // (26,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(26, 13),
                // (31,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(31, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_101_DefiniteAssignment()
        {
            var text = @"
class C1(in int x, in S s, in string y)
{
    void M1()
    {
        _ = x + s.f + y.Length;
    }

    void M2()
    {
        _ = s.f + y.Length + x;
    }

    void M3()
    {
        _ = y.Length + x + s.f;
    }

    void M4()
    {
        _ = x;
    }

    void M5()
    {
        _ = s.f;
    }

    void M6()
    {
        _ = y.Length;
    }
}

struct S
{
    public int f;    
    public S(int x) => f = x; 
}
";
            CreateCompilation(text).VerifyEmitDiagnostics(
                // (6,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(6, 13),
                // (6,17): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(6, 17),
                // (6,23): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = x + s.f + y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(6, 23),
                // (11,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(11, 13),
                // (11,19): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(11, 19),
                // (11,30): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = s.f + y.Length + x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(11, 30),
                // (16,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(16, 13),
                // (16,24): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(16, 24),
                // (16,28): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = y.Length + x + s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(16, 28),
                // (21,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         _ = x;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(21, 13),
                // (26,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 's' inside an instance member
                //         _ = s.f;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "s").WithArguments("s").WithLocation(26, 13),
                // (31,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         _ = y.Length;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(31, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_102_NullableAnalysis_this_Assignment()
        {
            var source =
@"
#nullable enable

struct C(string? x)
{
    public string? y;

    void M1()
    {
#line 1000
        x = """";
        x.ToString();
        y = """";
        y.ToString();

        var save1 = this;
        this = default;

#line 2000
        x.ToString();
        y.ToString();
        this = save1;

#line 3000
        x.ToString();
        y.ToString();
    }

    void M2()
    {
        var save2 = this;

#line 4000
        x = """";
        x.ToString();
        y = """";
        y.ToString();

        this = save2;

#line 5000
        x.ToString();
        y.ToString();
    }
}";
            var comp = CreateCompilation(source);

            comp.VerifyDiagnostics(
                // (2000,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(2000, 9),
                // (2001,9): warning CS8602: Dereference of a possibly null reference.
                //         y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(2001, 9),
                // (5000,9): warning CS8602: Dereference of a possibly null reference.
                //         x.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(5000, 9),
                // (5001,9): warning CS8602: Dereference of a possibly null reference.
                //         y.ToString();
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "y").WithLocation(5001, 9)
                );
        }

        [Fact]
        public void ParameterCapturing_103_Deconstruction()
        {
            var source = @"
class C1 (int p1, int p2)
{
    public void M1()
    {
        (p1, p2) = (p2, p1);
    }

    public int P1 => p1;
    public int P2 => p2;
}

class Program
{
    static void Main()
    {
        var c1 = new C1(1, 2);
        c1.M1();
        System.Console.Write((c1.P1, c1.P2));
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"(2, 1)").VerifyDiagnostics();

            verifier.VerifyIL("C1.M1",
@"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0,
                int V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C1.<p2>P""
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldfld      ""int C1.<p1>P""
  IL_000d:  stloc.1
  IL_000e:  ldarg.0
  IL_000f:  ldloc.0
  IL_0010:  stfld      ""int C1.<p1>P""
  IL_0015:  ldarg.0
  IL_0016:  ldloc.1
  IL_0017:  stfld      ""int C1.<p2>P""
  IL_001c:  ret
}
");
        }

        [Fact]
        public void ParameterCapturing_104_Cycle()
        {
            var source =
@"
struct S1(S1 x)
{
    S1 P => x;
}";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (2,14): error CS9121: Struct primary constructor parameter 'S1 x' of type 'S1' causes a cycle in the struct layout
                // struct S1(S1 x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S1 x", "S1").WithLocation(2, 14)
                );
        }

        [Fact]
        public void ParameterCapturing_105_Cycle()
        {
            var source =
@"
struct S1(S2 x)
{
    S2 P => x;
}

struct S2(S1 x)
{
    S1 P => x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (2,14): error CS9121: Struct primary constructor parameter 'S2 x' of type 'S2' causes a cycle in the struct layout
                // struct S1(S2 x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S2 x", "S2").WithLocation(2, 14),
                // (7,14): error CS9121: Struct primary constructor parameter 'S1 x' of type 'S1' causes a cycle in the struct layout
                // struct S2(S1 x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S1 x", "S1").WithLocation(7, 14)
                );
        }

        [Fact]
        public void ParameterCapturing_106_Cycle()
        {
            var source =
@"
struct S1<T>(S1<S1<int>> x)
{
    S1<S1<int>> P => x;
}";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (2,26): error CS9121: Struct primary constructor parameter 'S1<S1<int>> x' of type 'S1<S1<int>>' causes a cycle in the struct layout
                // struct S1<T>(S1<S1<int>> x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S1<S1<int>> x", "S1<S1<int>>").WithLocation(2, 26)
                );
        }

        [Fact]
        public void ParameterCapturing_107_Cycle()
        {
            var source1 =
@"
struct S1<T>(T x)
{
    T P => x;
}
";
            var source2 =
@"
struct S2(S1<S2> x)
{
    S1<S2> P => x;
}
";
            var comp = CreateCompilation(source1 + source2);

            comp.VerifyEmitDiagnostics(
                // (7,18): error CS9121: Struct primary constructor parameter 'S1<S2> x' of type 'S1<S2>' causes a cycle in the struct layout
                // struct S2(S1<S2> x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S1<S2> x", "S1<S2>").WithLocation(7, 18)
                );

            comp = CreateCompilation(source2 + source1);

            comp.VerifyEmitDiagnostics(
                // (2,18): error CS9121: Struct primary constructor parameter 'S1<S2> x' of type 'S1<S2>' causes a cycle in the struct layout
                // struct S2(S1<S2> x)
                Diagnostic(ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter, "x").WithArguments("S1<S2> x", "S1<S2>").WithLocation(2, 18)
                );
        }

        [Fact]
        public void ParameterCapturing_108_Cycle()
        {
            var source =
@"
class C(C x)
{
    C P => x;
}";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_109_Cycle()
        {
            var source =
@"
unsafe struct S1(S1* x)
{
    S1 P => *x;
}";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_110_Cycle()
        {
            var source =
@"
unsafe struct S1(S2* x)
{
    S2 P => *x;
}

unsafe struct S2(S1* x)
{
    S1 P => *x;
}

unsafe struct S3(S4* x)
{
    S4 P => *x;
}

struct S4(S3 x)
{
    S3 P => x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics();

            comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);
            System.Threading.Tasks.Parallel.For(0, 100, (int i) => comp.VerifyDiagnostics());
        }

        [Fact]
        public void ParameterCapturing_111_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x)
{
    void M()
    {
        x = 1;
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9114: A primary constructor parameter of a readonly type cannot be assigned to (except in init-only setter of the type or a variable initializer)
                //         x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter, "x").WithLocation(6, 9)
                );

            Assert.All(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetBackingFields(), f => Assert.True(f.IsReadOnly));
        }

        [Fact]
        public void ParameterCapturing_112_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x, ref int y, out int z)
{
    public readonly int z = (x = x + 1) + (y = 2) + (z = 3);

    public int X 
    {
        get
        {
            return x;
        }
        init
        {
            x = value;
        }
    }
}

class Program
{
    static void Main()
    {
        int y = 0;
        int z;
        var s1 = new S1(0, ref y, out z) { X = -1 };
        System.Console.WriteLine(y);
        System.Console.WriteLine(z);
        System.Console.WriteLine(s1.z);
        System.Console.WriteLine(s1.X);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
2
3
6
-1
", verify: Verification.Skipped).VerifyDiagnostics(
                // (2,35): warning CS9113: Parameter 'y' is unread.
                // readonly struct S1(int x, ref int y, out int z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(2, 35)
                );
        }

        [Fact]
        public void ParameterCapturing_113_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly void M()
    {
        x = 1;
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'x' because it is read-only
                //         x = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x").WithArguments("x").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParameterCapturing_114_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly int X 
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
        }
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (12,13): error CS1604: Cannot assign to 'x' because it is read-only
                //             x = value;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x").WithArguments("x").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_115_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    int X 
    {
        get
        {
            return x;
        }
        readonly set
        {
            x = value;
        }
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (12,13): error CS1604: Cannot assign to 'x' because it is read-only
                //             x = value;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x").WithArguments("x").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_116_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly int X 
    {
        get
        {
            return x;
        }
        init
        {
            x = value;
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_117_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(in int x, ref int y, out int z)
{
    public readonly int a = x + y + (z = 3);
    void M1()
    {
        x = 1;
    }
    void M2()
    {
        y = 1;
    }
    void M3()
    {
        z = 1;
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (7,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         x = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(7, 9),
                // (7,9): error CS8331: Cannot assign to variable 'x' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         x = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField, "x").WithArguments("variable", "x").WithLocation(7, 9),
                // (11,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         y = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(11, 9),
                // (15,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'z' inside an instance member
                //         z = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "z").WithArguments("z").WithLocation(15, 9)
                );

            Assert.All(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetBackingFields(), f => Assert.True(f.IsReadOnly));
        }

        [Fact]
        public void ParameterCapturing_118_ReadonlyContext()
        {
            var source =
@"
unsafe readonly struct S1(int x)
{
    readonly int y;
    void M()
    {
        fixed (void* p = &x)
        {}
        fixed (void* p = &y)
        {}
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_119_ReadonlyContext()
        {
            var source =
@"
unsafe readonly struct S1(int x)
{
    readonly int y;
    void* M1() => &x;
    void* M2() => &y;
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics(
                // (5,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     void* M1() => &x;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(5, 19),
                // (6,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     void* M2() => &y;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&y").WithLocation(6, 19)
                );
        }

        [Fact]
        public void ParameterCapturing_120_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x)
{
    ref int M1() => ref x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS9115: A primary constructor parameter of a readonly type cannot be returned by writable reference
                //     ref int M1() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter, "x").WithLocation(4, 25)
                );
        }

        [Fact]
        public void ParameterCapturing_121_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x)
{
    readonly object y = ref int () => ref x;
    int M1() => x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,43): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                //     readonly object y = ref int () => ref x;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "x").WithLocation(4, 43),
                // (4,43): error CS9115: A primary constructor parameter of a readonly type cannot be returned by writable reference
                //     readonly object y = ref int () => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter, "x").WithLocation(4, 43)
                );
        }

        [Fact]
        public void ParameterCapturing_122_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x)
{
    void M1() => M2(out x);
    static int M2(out int x) => throw null;

    void M3() => M4(ref x);
    static int M4(ref int x) => throw null;

    void M5() => M6(in x);
    static int M6(in int x) => throw null;

    readonly int y = M2(out x) + M4(ref x) + M6(in x);

    int Z
    {
        get => x;
        init
        {
            M2(out x);
            M4(ref x);
            M6(in x);
        }
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS9116: A primary constructor parameter of a readonly type cannot be used as a ref or out value (except in init-only setter of the type or a variable initializer)
                //     void M1() => M2(out x);
                Diagnostic(ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter, "x").WithLocation(4, 25),
                // (7,25): error CS9116: A primary constructor parameter of a readonly type cannot be used as a ref or out value (except in init-only setter of the type or a variable initializer)
                //     void M3() => M4(ref x);
                Diagnostic(ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter, "x").WithLocation(7, 25)
                );
        }

        [Fact]
        public void ParameterCapturing_123_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(int x)
{
    readonly int y = M2(out x);

    public int M1() => x;

    static int M2(out int x)
    {
        x = 123;
        return 0;
    }
}

class Program
{
    static void Main()
    {
        var s1 = new S1(0);
        System.Console.Write(s1.M1());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_124_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    void M()
    {
        x.F = 1;
    }

    readonly int y = x.F = 2;

    int Z
    {
        get => x.F;
        init
        {
            x.F = value;
        }
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9117: Members of primary constructor parameter 'S2 x' of a readonly type cannot be modified (except in init-only setter of the type or a variable initializer)
                //         x.F = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(6, 9)
                );

            Assert.All(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetBackingFields(), f => Assert.True(f.IsReadOnly));
        }

        [Fact]
        public void ParameterCapturing_125_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x, ref S2 y, out S2 z)
{
    public readonly int z = (x.F = x.F + 1) + (y.F = 2) + (z = new S2() { F = 3 }).F;

    public S2 X 
    {
        get
        {
            return x;
        }
        init
        {
            x = value;
        }
    }
}

struct S2
{
    public int F;
}

class Program
{
    static void Main()
    {
        S2 y = default;
        S2 z;
        var s1 = new S1(default, ref y, out z) { X = new S2() { F = -1 } };
        System.Console.WriteLine(y.F);
        System.Console.WriteLine(z.F);
        System.Console.WriteLine(s1.z);
        System.Console.WriteLine(s1.X.F);
    }
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition }, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"
2
3
6
-1
", verify: Verification.Skipped).VerifyDiagnostics(
                // (2,33): warning CS9113: Parameter 'y' is unread.
                // readonly struct S1(S2 x, ref S2 y, out S2 z)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "y").WithArguments("y").WithLocation(2, 33)
                );
        }

        [Fact]
        public void ParameterCapturing_126_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    readonly void M()
    {
        x.F = 1;
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'x.F' because it is read-only
                //         x.F = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x.F").WithArguments("x.F").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParameterCapturing_127_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    readonly int X 
    {
        get
        {
            return x.F;
        }
        set
        {
            x.F = value;
        }
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (12,13): error CS1604: Cannot assign to 'x.F' because it is read-only
                //             x.F = value;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x.F").WithArguments("x.F").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_128_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    int X 
    {
        get
        {
            return x.F;
        }
        readonly set
        {
            x.F = value;
        }
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (12,13): error CS1604: Cannot assign to 'x.F' because it is read-only
                //             x.F = value;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x.F").WithArguments("x.F").WithLocation(12, 13)
                );
        }

        [Fact]
        public void ParameterCapturing_129_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    readonly int X 
    {
        get
        {
            return x.F;
        }
        init
        {
            x.F = value;
        }
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_130_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(in S2 x, ref S2 y, out S2 z)
{
    public readonly int a = x.F + y.F + (z = new S2() { F = 3 }).F;
    void M1()
    {
        x.F = 1;
    }
    void M2()
    {
        y.F = 1;
    }
    void M3()
    {
        z.F = 1;
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (7,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //         x.F = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(7, 9),
                // (7,9): error CS8332: Cannot assign to a member of variable 'x' or use it as the right hand side of a ref assignment because it is a readonly variable
                //         x.F = 1;
                Diagnostic(ErrorCode.ERR_AssignReadonlyNotField2, "x.F").WithArguments("variable", "x").WithLocation(7, 9),
                // (11,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'y' inside an instance member
                //         y.F = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "y").WithArguments("y").WithLocation(11, 9),
                // (15,9): error CS9109: Cannot use ref, out, or in primary constructor parameter 'z' inside an instance member
                //         z.F = 1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "z").WithArguments("z").WithLocation(15, 9)
                );

            Assert.All(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetBackingFields(), f => Assert.True(f.IsReadOnly));
        }

        [Fact]
        public void ParameterCapturing_131_ReadonlyContext()
        {
            var source =
@"
unsafe readonly struct S1(S2 x)
{
    readonly S2 y;
    void M()
    {
        fixed (void* p = &x.F)
        {}
        fixed (void* p = &y.F)
        {}
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_132_ReadonlyContext()
        {
            var source =
@"
unsafe readonly struct S1(S2 x)
{
    readonly S2 y;
    void* M1() => &x.F;
    void* M2() => &y.F;
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll);

            comp.VerifyEmitDiagnostics(
                // (5,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     void* M1() => &x.F;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x.F").WithLocation(5, 19),
                // (6,19): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                //     void* M2() => &y.F;
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&y.F").WithLocation(6, 19)
                );
        }

        [Fact]
        public void ParameterCapturing_133_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    ref int M1() => ref x.F;
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS9118: Members of primary constructor parameter 'S2 x' of a readonly type cannot be returned by writable reference
                //     ref int M1() => ref x.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(4, 25)
                );
        }

        [Fact]
        public void ParameterCapturing_134_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    readonly object y = ref int () => ref x.F;
    int M1() => x.F;
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,43): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                //     readonly object y = ref int () => ref x.F;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "x").WithLocation(4, 43),
                // (4,43): error CS9118: Members of primary constructor parameter 'S2 x' of a readonly type cannot be returned by writable reference
                //     readonly object y = ref int () => ref x.F;
                Diagnostic(ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(4, 43)
                );
        }

        [Fact]
        public void ParameterCapturing_135_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    void M1() => M2(out x.F);
    static int M2(out int x) => throw null;

    void M3() => M4(ref x.F);
    static int M4(ref int x) => throw null;

    void M5() => M6(in x.F);
    static int M6(in int x) => throw null;

    readonly int y = M2(out x.F) + M4(ref x.F) + M6(in x.F);

    int Z
    {
        get => x.F;
        init
        {
            M2(out x.F);
            M4(ref x.F);
            M6(in x.F);
        }
    }
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(new[] { source, IsExternalInitTypeDefinition });

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS9119: Members of primary constructor parameter 'S2 x' of a readonly type cannot be used as a ref or out value (except in init-only setter of the type or a variable initializer)
                //     void M1() => M2(out x.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(4, 25),
                // (7,25): error CS9119: Members of primary constructor parameter 'S2 x' of a readonly type cannot be used as a ref or out value (except in init-only setter of the type or a variable initializer)
                //     void M3() => M4(ref x.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(7, 25)
                );
        }

        [Fact]
        public void ParameterCapturing_136_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    readonly int y = M2(out x.F);

    public int M1() => x.F;

    static int M2(out int x)
    {
        x = 123;
        return 0;
    }
}

struct S2
{
    public int F;
}

class Program
{
    static void Main()
    {
        var s1 = new S1(default);
        System.Console.Write(s1.M1());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"123", verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_137_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly void M1() => M2(out x);

    static void M2(out int x) => throw null;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,34): error CS1605: Cannot use 'x' as a ref or out value because it is read-only
                //     readonly void M1() => M2(out x);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "x").WithArguments("x").WithLocation(4, 34)
                );
        }

        [Fact]
        public void ParameterCapturing_138_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    readonly void M1() => M2(out x.F);

    static void M2(out int x) => throw null;
}

struct S2
{
    public int F;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,34): error CS1605: Cannot use 'x.F' as a ref or out value because it is read-only
                //     readonly void M1() => M2(out x.F);
                Diagnostic(ErrorCode.ERR_RefReadonlyLocal, "x.F").WithArguments("x.F").WithLocation(4, 34)
                );
        }

        [Fact]
        public void ParameterCapturing_139_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly void M()
    {
        x++;
    }
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS1604: Cannot assign to 'x' because it is read-only
                //         x++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x").WithArguments("x").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParameterCapturing_140_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly ref int M1() => ref x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,34): error CS9120: Cannot return primary constructor parameter 'x' by reference.
                //     readonly ref int M1() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(4, 34)
                );
        }

        [Fact]
        public void ParameterCapturing_141_ReadonlyContext()
        {
            var source =
@"
readonly struct S1(S2 x)
{
    void M(ref int y)
    {
        x.F = ref y; 
    }
}

ref struct S2
{
    public ref int F;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9110: Cannot use primary constructor parameter 'x' that has ref-like type inside an instance member
                //         x.F = ref y; 
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, "x").WithArguments("x").WithLocation(6, 9),
                // (6,9): error CS9117: Members of primary constructor parameter 'S2 x' of a readonly type cannot be modified (except in init-only setter of the type or a variable initializer)
                //         x.F = ref y; 
                Diagnostic(ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter2, "x.F").WithArguments("S2 x").WithLocation(6, 9)
                );

            Assert.All(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetBackingFields(), f => Assert.True(f.IsReadOnly));
        }

        [Fact]
        public void ParameterCapturing_142_ReadonlyContext()
        {
            var source =
@"
struct S1(S2 x)
{
    readonly void M(ref int y)
    {
        x.F = ref y; 
    }
}

ref struct S2
{
    public ref int F;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);

            comp.VerifyEmitDiagnostics(
                // (6,9): error CS9110: Cannot use primary constructor parameter 'x' that has ref-like type inside an instance member
                //         x.F = ref y; 
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, "x").WithArguments("x").WithLocation(6, 9),
                // (6,9): error CS1604: Cannot assign to 'x.F' because it is read-only
                //         x.F = ref y; 
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocal, "x.F").WithArguments("x.F").WithLocation(6, 9)
                );
        }

        [Fact]
        public void ParameterCapturing_143_ReadonlyContext()
        {
            var source =
@"
struct S1(ref int x)
{
    readonly void M1(ref int y) =>  x = ref y;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,37): error CS9109: Cannot use ref, out, or in primary constructor parameter 'x' inside an instance member
                //     readonly void M1(ref int y) =>  x = ref y;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "x").WithArguments("x").WithLocation(4, 37)
                );
        }

        [Fact]
        public void ParameterCapturing_144_ReadonlyContext()
        {
            var source =
@"
struct S1(int x)
{
    readonly void M1(ref int y) =>  y = ref x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,45): error CS1510: A ref or out value must be an assignable variable
                //     readonly void M1(ref int y) =>  y = ref x;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "x").WithLocation(4, 45)
                );
        }

        [Fact]
        public void ParameterCapturing_145_ReturnByRef()
        {
            var source =
@"
class C1(int x, int z)
{
    ref int M1() => ref x;
    ref readonly int M2() => ref x;

    int y = 0;
    ref int M3() => ref y;
    ref readonly int M4() => ref y;

    object u1 = ref int() => ref x;
    object u2 = ref readonly int() => ref x;

    object v1 = ref int() => ref y;
    object v2 = ref readonly int() => ref y;

    object w1 = ref int() => ref z;
    object w2 = ref readonly int() => ref z;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     ref int M1() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(4, 25),
                // (5,34): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     ref readonly int M2() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(5, 34),
                // (11,34): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     object u1 = ref int() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(11, 34),
                // (12,43): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     object u2 = ref readonly int() => ref x;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(12, 43),
                // (14,34): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C1.y'
                //     object v1 = ref int() => ref y;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y").WithArguments("C1.y").WithLocation(14, 34),
                // (15,43): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C1.y'
                //     object v2 = ref readonly int() => ref y;
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "y").WithArguments("C1.y").WithLocation(15, 43),
                // (17,34): error CS8166: Cannot return a parameter by reference 'z' because it is not a ref parameter
                //     object w1 = ref int() => ref z;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "z").WithArguments("z").WithLocation(17, 34),
                // (18,43): error CS8166: Cannot return a parameter by reference 'z' because it is not a ref parameter
                //     object w2 = ref readonly int() => ref z;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "z").WithArguments("z").WithLocation(18, 43)
                );
        }

        [Fact]
        public void ParameterCapturing_146_RefSafety()
        {
            var source =
@"
class C1(int x)
{
    ref int M1() => ref M2(ref x);
    static ref int M2(ref int x) => ref x;
}
";
            var comp = CreateCompilation(source);

            comp.VerifyEmitDiagnostics(
                // (4,25): error CS8347: Cannot use a result of 'C1.M2(ref int)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //     ref int M1() => ref M2(ref x);
                Diagnostic(ErrorCode.ERR_EscapeCall, "M2(ref x)").WithArguments("C1.M2(ref int)", "x").WithLocation(4, 25),
                // (4,32): error CS8166: Cannot return a parameter by reference 'x' because it is not a ref parameter
                //     ref int M1() => ref M2(ref x);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "x").WithArguments("x").WithLocation(4, 32)
                );
        }

        [Fact]
        public void ParameterCapturing_147_SynthesizedAttributes()
        {
            var source = @"
class C1 (int p1)
{
    int M1() => p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp,
                symbolValidator: (m) =>
                {
                    var attr = m.GlobalNamespace.GetTypeMember("C1").GetMembers().OfType<FieldSymbol>().Single().GetAttributes();
                    Assert.Equal(2, attr.Length);
                    Assert.Equal("System.Runtime.CompilerServices.CompilerGeneratedAttribute", attr[0].ToString());
                    Assert.Equal("System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)", attr[1].ToString());
                }
                ).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_148_SynthesizedAttributes()
        {
            var source = @"
class C1 (nint p1)
{
    nint M1() => p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp,
                symbolValidator: (m) =>
                {
                    var attr = m.GlobalNamespace.GetTypeMember("C1").GetMembers().OfType<FieldSymbol>().Single().GetAttributes();
                    Assert.Equal(3, attr.Length);
                    Assert.Equal("System.Runtime.CompilerServices.NativeIntegerAttribute", attr[0].ToString());
                }
                ).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_149_SynthesizedAttributes()
        {
            var source = @"
class C1 ((int i1, int i2) p1)
{
    object M1() => p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp,
                symbolValidator: (m) =>
                {
                    var attr = m.GlobalNamespace.GetTypeMember("C1").GetMembers().OfType<FieldSymbol>().Single().GetAttributes();
                    Assert.Equal(3, attr.Length);
                    Assert.Equal("System.Runtime.CompilerServices.TupleElementNamesAttribute({\"i1\", \"i2\"})", attr[0].ToString());
                }
                ).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_150_SynthesizedAttributes()
        {
            var source = @"
class C1 (dynamic p1)
{
    object M1() => p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));

            CompileAndVerify(comp,
                symbolValidator: (m) =>
                {
                    var attr = m.GlobalNamespace.GetTypeMember("C1").GetMembers().OfType<FieldSymbol>().Single().GetAttributes();
                    Assert.Equal(3, attr.Length);
                    Assert.Equal("System.Runtime.CompilerServices.DynamicAttribute", attr[0].ToString());
                }
                ).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_151_LambdasCaptureOnlyCapturedParameter()
        {
            var source = @"
class Base
{
    public System.Func<int> Z;
    public Base(int x, int y, System.Func<int> z)
    {
        System.Console.Write(z() - 1);
        Z = z;
    }
}

partial class C1
{
    public int F1 = p2 + 1;
}

partial class C1 (int p1, int p2) : Base(p1, p2, () => p1)
{
    public int F2 = p2 + 2;
    public int P1 => p1;
}

partial class C1
{
    public int F3 = p2 + 3;
    public int P2 => ++p1;
    public int M1() { return p1++; }
    event System.Action E1 { add { p1++; } remove { void local() { p1--; } local(); }}
    public System.Action M2() => () => p1++;
}

class Program
{
    static void Main()
    {
        var c1 = new C1(123,-1);
        System.Console.Write(c1.M1());
        System.Console.Write(c1.P1);
        System.Console.Write(c1.P2);
        System.Console.Write(c1.Z());
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier = CompileAndVerify(comp, expectedOutput: @"122123124125125", verify: Verification.Fails).VerifyDiagnostics(
                // (17,42): warning CS9107: Parameter 'int p1' is captured into the state of the enclosing type and its value is also passed to the base constructor. The value might be captured by the base class as well.
                // partial class C1 (int p1, int p2) : Base(p1, p2, () => p1)
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase, "p1").WithArguments("int p1").WithLocation(17, 42)
                );

            verifier.VerifyTypeIL("C1", @"
.class private auto ansi beforefieldinit C1
	extends Base
{
	// Fields
	.field public int32 F1
	.field private int32 '<p1>P'
	.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.field public int32 F2
	.field public int32 F3
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 p1,
			int32 p2
		) cil managed 
	{
		// Method begins at RVA 0x2083
		// Code size 60 (0x3c)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 C1::'<p1>P'
		IL_0007: ldarg.0
		IL_0008: ldarg.2
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 C1::F1
		IL_0010: ldarg.0
		IL_0011: ldarg.2
		IL_0012: ldc.i4.2
		IL_0013: add
		IL_0014: stfld int32 C1::F2
		IL_0019: ldarg.0
		IL_001a: ldarg.2
		IL_001b: ldc.i4.3
		IL_001c: add
		IL_001d: stfld int32 C1::F3
		IL_0022: ldarg.0
		IL_0023: ldarg.0
		IL_0024: ldfld int32 C1::'<p1>P'
		IL_0029: ldarg.2
		IL_002a: ldarg.0
		IL_002b: ldftn instance int32 C1::'<.ctor>b__1_0'()
		IL_0031: newobj instance void class [mscorlib]System.Func`1<int32>::.ctor(object, native int)
		IL_0036: call instance void Base::.ctor(int32, int32, class [mscorlib]System.Func`1<int32>)
		IL_003b: ret
	} // end of method C1::.ctor
	.method public hidebysig specialname 
		instance int32 get_P1 () cil managed 
	{
		// Method begins at RVA 0x20c0
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C1::'<p1>P'
		IL_0006: ret
	} // end of method C1::get_P1
	.method public hidebysig specialname 
		instance int32 get_P2 () cil managed 
	{
		// Method begins at RVA 0x20c8
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stloc.0
		IL_000a: ldloc.0
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::get_P2
	.method public hidebysig 
		instance int32 M1 () cil managed 
	{
		// Method begins at RVA 0x20e8
		// Code size 18 (0x12)
		.maxstack 3
		.locals init (
			[0] int32
		)
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: stloc.0
		IL_0008: ldloc.0
		IL_0009: ldc.i4.1
		IL_000a: add
		IL_000b: stfld int32 C1::'<p1>P'
		IL_0010: ldloc.0
		IL_0011: ret
	} // end of method C1::M1
	.method private hidebysig specialname 
		instance void add_E1 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2106
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::add_E1
	.method private hidebysig specialname 
		instance void remove_E1 (
			class [mscorlib]System.Action 'value'
		) cil managed 
	{
		// Method begins at RVA 0x2116
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void C1::'<remove_E1>g__local|12_0'()
		IL_0006: ret
	} // end of method C1::remove_E1
	.method public hidebysig 
		instance class [mscorlib]System.Action M2 () cil managed 
	{
		// Method begins at RVA 0x211e
		// Code size 13 (0xd)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldftn instance void C1::'<M2>b__13_0'()
		IL_0007: newobj instance void [mscorlib]System.Action::.ctor(object, native int)
		IL_000c: ret
	} // end of method C1::M2
	.method private hidebysig 
		instance int32 '<.ctor>b__1_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x20c0
		// Code size 7 (0x7)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldfld int32 C1::'<p1>P'
		IL_0006: ret
	} // end of method C1::'<.ctor>b__1_0'
	.method private hidebysig 
		instance void '<remove_E1>g__local|12_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x212c
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: sub
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::'<remove_E1>g__local|12_0'
	.method private hidebysig 
		instance void '<M2>b__13_0' () cil managed 
	{
		.custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x2106
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.0
		IL_0002: ldfld int32 C1::'<p1>P'
		IL_0007: ldc.i4.1
		IL_0008: add
		IL_0009: stfld int32 C1::'<p1>P'
		IL_000e: ret
	} // end of method C1::'<M2>b__13_0'
	// Events
	.event [mscorlib]System.Action E1
	{
		.addon instance void C1::add_E1(class [mscorlib]System.Action)
		.removeon instance void C1::remove_E1(class [mscorlib]System.Action)
	}
	// Properties
	.property instance int32 P1()
	{
		.get instance int32 C1::get_P1()
	}
	.property instance int32 P2()
	{
		.get instance int32 C1::get_P2()
	}
} // end of class C1
".Replace("[mscorlib]", ExecutionConditionUtil.IsDesktop ? "[mscorlib]" : "[netstandard]"));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68384")]
        public void ParameterCapturing_152_NullableAnalysis()
        {
            var source = @"
#nullable enable

class B(string s)
{
    public string S { get; } = s;
}

class C(B b)
    : B(b.S)
{
    string T() => b.S;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68384")]
        public void ParameterCapturing_153_NullableAnalysis()
        {
            var source = @"
#nullable enable

class B(System.Func<string> s)
{
    public string S { get; } = s();
}

class C(B b)
    : B(() => b.S)
{
    string T() => b.S;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68384")]
        public void ParameterCapturing_154_NullableAnalysis()
        {
            var source = @"
#nullable enable

class B(System.Func<string> s)
{
    public string S { get; } = s();
}

class C(B b)
    : B(() =>
        {
            string local() => b.S;
            return local();
        })
{
    string T() => b.S;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_155_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    int F = p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (4,13): warning CS9124: Parameter 'int p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
                //     int F = p1;
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, "p1").WithArguments("int p1").WithLocation(4, 13)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_156_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    int F = (int)p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (4,18): warning CS9124: Parameter 'int p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
                //     int F = (int)p1;
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, "p1").WithArguments("int p1").WithLocation(4, 18)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_157_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    long F = p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_158_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    long F = (long)p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ParameterCapturing_159_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    static int F = p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (4,20): error CS9105: Cannot use primary constructor parameter 'int p1' in this context.
                //     static int F = p1;
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p1").WithArguments("int p1").WithLocation(4, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_160_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
ref struct C1([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int p1)
{
    ref int F = ref p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.NetCoreApp);

            var expected = new[] {
                // (8,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'p1' inside an instance member
                //         _ = p1; 
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "p1").WithArguments("p1").WithLocation(8, 13)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_161_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
ref struct C1([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int p1)
{
    ref readonly int F = ref p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.NetCoreApp);

            var expected = new[] {
                // (8,13): error CS9109: Cannot use ref, out, or in primary constructor parameter 'p1' inside an instance member
                //         _ = p1; 
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "p1").WithArguments("p1").WithLocation(8, 13)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_162_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    int F { get; } = p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (4,22): warning CS9124: Parameter 'int p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
                //     int F { get; } = p1;
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, "p1").WithArguments("int p1").WithLocation(4, 22)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_163_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(System.Action p1)
{
    event System.Action F = p1;

    void M()
    {
        _ = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (4,29): warning CS9124: Parameter 'Action p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
                //     event System.Action F = p1;
                Diagnostic(ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer, "p1").WithArguments("System.Action p1").WithLocation(4, 29)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ParameterCapturing_164_WRN_CapturedPrimaryConstructorParameterInFieldInitializer()
        {
            var source = @"
class C1(int p1)
{
    int F = p1;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/68796")]
        public void ParameterCapturing_165_ColorColor_MemberAccess_InstanceAndStatic_MethodAndExtensionMethod_Generic()
        {
            var source = """
struct S1(Color Color)
{
    public void Test()
    {
        Color.M1(this);
    }
}

static class E
{
    public static void M1<T>(this T c, S1 x, int y = 0)
    {
    }
}

class Color
{
    public static void M1<T>(T x) where T : unmanaged
    {
    }
}
""";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (5,9): error CS9106: Identifier 'Color' is ambiguous between type 'Color' and parameter 'Color Color' in this context.
                //         Color.M1(this);
                Diagnostic(ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver, "Color").WithArguments("Color", "Color", "Color Color").WithLocation(5, 9)
                );

            Assert.NotEmpty(comp.GetTypeByMetadataName("S1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact]
        public void ParameterCapturing_166_InAsyncMethod()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).M().Result);
    }
}

class C(int y)
{
    public async Task<int> M()
    {
        await Task.Yield();
        return y;
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "123");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__2'
    	extends [netstandard]System.ValueType
    	implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 '<>1__state'
    	.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
    	.field public class C '<>4__this'
    	.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
    	// Methods
    	.method private final hidebysig newslot virtual 
    		instance void MoveNext () cil managed 
    	{
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
    		// Method begins at RVA 0x20dc
    		// Code size 163 (0xa3)
    		.maxstack 3
    		.locals init (
    			[0] int32,
    			[1] class C,
    			[2] int32,
    			[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
    			[4] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
    			[5] class [netstandard]System.Exception
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: stloc.0
    		IL_0007: ldarg.0
    		IL_0008: ldfld class C C/'<M>d__2'::'<>4__this'
    		IL_000d: stloc.1
    		.try
    		{
    			IL_000e: ldloc.0
    			IL_000f: brfalse.s IL_0049
    			IL_0011: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
    			IL_0016: stloc.s 4
    			IL_0018: ldloca.s 4
    			IL_001a: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
    			IL_001f: stloc.3
    			IL_0020: ldloca.s 3
    			IL_0022: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
    			IL_0027: brtrue.s IL_0065
    			IL_0029: ldarg.0
    			IL_002a: ldc.i4.0
    			IL_002b: dup
    			IL_002c: stloc.0
    			IL_002d: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0032: ldarg.0
    			IL_0033: ldloc.3
    			IL_0034: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0039: ldarg.0
    			IL_003a: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_003f: ldloca.s 3
    			IL_0041: ldarg.0
    			IL_0042: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype C/'<M>d__2'>(!!0&, !!1&)
    			IL_0047: leave.s IL_00a2
    			IL_0049: ldarg.0
    			IL_004a: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_004f: stloc.3
    			IL_0050: ldarg.0
    			IL_0051: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0056: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
    			IL_005c: ldarg.0
    			IL_005d: ldc.i4.m1
    			IL_005e: dup
    			IL_005f: stloc.0
    			IL_0060: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0065: ldloca.s 3
    			IL_0067: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
    			IL_006c: ldloc.1
    			IL_006d: ldfld int32 C::'<y>P'
    			IL_0072: stloc.2
    			IL_0073: leave.s IL_008e
    		} // end .try
    		catch [netstandard]System.Exception
    		{
    			IL_0075: stloc.s 5
    			IL_0077: ldarg.0
    			IL_0078: ldc.i4.s -2
    			IL_007a: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_007f: ldarg.0
    			IL_0080: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_0085: ldloc.s 5
    			IL_0087: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
    			IL_008c: leave.s IL_00a2
    		} // end handler
    		IL_008e: ldarg.0
    		IL_008f: ldc.i4.s -2
    		IL_0091: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0096: ldarg.0
    		IL_0097: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_009c: ldloc.2
    		IL_009d: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
    		IL_00a2: ret
    	} // end of method '<M>d__2'::MoveNext
    	.method private final hidebysig newslot virtual 
    		instance void SetStateMachine (
    			class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
    		) cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		// Method begins at RVA 0x219c
    		// Code size 13 (0xd)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_0006: ldarg.1
    		IL_0007: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		IL_000c: ret
    	} // end of method '<M>d__2'::SetStateMachine
    } // end of class <M>d__2
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2087
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance class [netstandard]System.Threading.Tasks.Task`1<int32> M () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
    		01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
    	)
    	// Method begins at RVA 0x2098
    	// Code size 55 (0x37)
    	.maxstack 2
    	.locals init (
    		[0] valuetype C/'<M>d__2'
    	)
    	IL_0000: ldloca.s 0
    	IL_0002: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
    	IL_0007: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_000c: ldloca.s 0
    	IL_000e: ldarg.0
    	IL_000f: stfld class C C/'<M>d__2'::'<>4__this'
    	IL_0014: ldloca.s 0
    	IL_0016: ldc.i4.m1
    	IL_0017: stfld int32 C/'<M>d__2'::'<>1__state'
    	IL_001c: ldloca.s 0
    	IL_001e: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0023: ldloca.s 0
    	IL_0025: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<valuetype C/'<M>d__2'>(!!0&)
    	IL_002a: ldloca.s 0
    	IL_002c: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0031: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
    	IL_0036: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "123");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__2'
    	extends [netstandard]System.Object
    	implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 '<>1__state'
    	.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
    	.field public class C '<>4__this'
    	.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
    	// Methods
    	.method public hidebysig specialname rtspecialname 
    		instance void .ctor () cil managed 
    	{
    		// Method begins at RVA 0x2083
    		// Code size 8 (0x8)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance void [netstandard]System.Object::.ctor()
    		IL_0006: nop
    		IL_0007: ret
    	} // end of method '<M>d__2'::.ctor
    	.method private final hidebysig newslot virtual 
    		instance void MoveNext () cil managed 
    	{
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
    		// Method begins at RVA 0x20e0
    		// Code size 173 (0xad)
    		.maxstack 3
    		.locals init (
    			[0] int32,
    			[1] int32,
    			[2] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
    			[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
    			[4] class C/'<M>d__2',
    			[5] class [netstandard]System.Exception
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: stloc.0
    		.try
    		{
    			IL_0007: ldloc.0
    			IL_0008: brfalse.s IL_000c
    			IL_000a: br.s IL_000e
    			IL_000c: br.s IL_004b
    			IL_000e: nop
    			IL_000f: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
    			IL_0014: stloc.3
    			IL_0015: ldloca.s 3
    			IL_0017: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
    			IL_001c: stloc.2
    			IL_001d: ldloca.s 2
    			IL_001f: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
    			IL_0024: brtrue.s IL_0067
    			IL_0026: ldarg.0
    			IL_0027: ldc.i4.0
    			IL_0028: dup
    			IL_0029: stloc.0
    			IL_002a: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_002f: ldarg.0
    			IL_0030: ldloc.2
    			IL_0031: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0036: ldarg.0
    			IL_0037: stloc.s 4
    			IL_0039: ldarg.0
    			IL_003a: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_003f: ldloca.s 2
    			IL_0041: ldloca.s 4
    			IL_0043: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, class C/'<M>d__2'>(!!0&, !!1&)
    			IL_0048: nop
    			IL_0049: leave.s IL_00ac
    			IL_004b: ldarg.0
    			IL_004c: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0051: stloc.2
    			IL_0052: ldarg.0
    			IL_0053: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0058: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
    			IL_005e: ldarg.0
    			IL_005f: ldc.i4.m1
    			IL_0060: dup
    			IL_0061: stloc.0
    			IL_0062: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0067: ldloca.s 2
    			IL_0069: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
    			IL_006e: nop
    			IL_006f: ldarg.0
    			IL_0070: ldfld class C C/'<M>d__2'::'<>4__this'
    			IL_0075: ldfld int32 C::'<y>P'
    			IL_007a: stloc.1
    			IL_007b: leave.s IL_0097
    		} // end .try
    		catch [netstandard]System.Exception
    		{
    			IL_007d: stloc.s 5
    			IL_007f: ldarg.0
    			IL_0080: ldc.i4.s -2
    			IL_0082: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0087: ldarg.0
    			IL_0088: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_008d: ldloc.s 5
    			IL_008f: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
    			IL_0094: nop
    			IL_0095: leave.s IL_00ac
    		} // end handler
    		IL_0097: ldarg.0
    		IL_0098: ldc.i4.s -2
    		IL_009a: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_009f: ldarg.0
    		IL_00a0: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_00a5: ldloc.1
    		IL_00a6: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
    		IL_00ab: nop
    		IL_00ac: ret
    	} // end of method '<M>d__2'::MoveNext
    	.method private final hidebysig newslot virtual 
    		instance void SetStateMachine (
    			class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
    		) cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		// Method begins at RVA 0x21ac
    		// Code size 1 (0x1)
    		.maxstack 8
    		IL_0000: ret
    	} // end of method '<M>d__2'::SetStateMachine
    } // end of class <M>d__2
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    .custom instance void [netstandard]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggerBrowsableState) = (
    	01 00 00 00 00 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x208c
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: nop
    	IL_000e: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance class [netstandard]System.Threading.Tasks.Task`1<int32> M () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
    		01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
    	)
    	.custom instance void [netstandard]System.Diagnostics.DebuggerStepThroughAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x209c
    	// Code size 56 (0x38)
    	.maxstack 2
    	.locals init (
    		[0] class C/'<M>d__2'
    	)
    	IL_0000: newobj instance void C/'<M>d__2'::.ctor()
    	IL_0005: stloc.0
    	IL_0006: ldloc.0
    	IL_0007: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
    	IL_000c: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0011: ldloc.0
    	IL_0012: ldarg.0
    	IL_0013: stfld class C C/'<M>d__2'::'<>4__this'
    	IL_0018: ldloc.0
    	IL_0019: ldc.i4.m1
    	IL_001a: stfld int32 C/'<M>d__2'::'<>1__state'
    	IL_001f: ldloc.0
    	IL_0020: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0025: ldloca.s 0
    	IL_0027: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<class C/'<M>d__2'>(!!0&)
    	IL_002c: ldloc.0
    	IL_002d: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0032: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
    	IL_0037: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_167_InIterator()
        {
            var source = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        foreach (var x in new C(123).M())
        {
            System.Console.Write(x);
        }
    }
}

class C(int y)
{
    public IEnumerable<int> M()
    {
        yield return 9;
        yield return y;
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "9123");

            verifier1.VerifyTypeIL("C",
(@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__2'
    	extends [netstandard]System.Object
    	implements class [netstandard]System.Collections.Generic.IEnumerable`1<int32>,
    		        [netstandard]System.Collections.IEnumerable,
    		        class [netstandard]System.Collections.Generic.IEnumerator`1<int32>,
" +
(RuntimeUtilities.IsCoreClrRuntime ?
@"			           [netstandard]System.Collections.IEnumerator,
			           [netstandard]System.IDisposable" :
@"			           [netstandard]System.IDisposable,
			           [netstandard]System.Collections.IEnumerator") + @"
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field private int32 '<>1__state'
    	.field private int32 '<>2__current'
    	.field private int32 '<>l__initialThreadId'
    	.field public class C '<>4__this'
    	// Methods
    	.method public hidebysig specialname rtspecialname 
    		instance void .ctor (
    			int32 '<>1__state'
    		) cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		// Method begins at RVA 0x20df
    		// Code size 25 (0x19)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance void [netstandard]System.Object::.ctor()
    		IL_0006: ldarg.0
    		IL_0007: ldarg.1
    		IL_0008: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_000d: ldarg.0
    		IL_000e: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
    		IL_0013: stfld int32 C/'<M>d__2'::'<>l__initialThreadId'
    		IL_0018: ret
    	} // end of method '<M>d__2'::.ctor
    	.method private final hidebysig newslot virtual 
    		instance void System.IDisposable.Dispose () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.IDisposable::Dispose()
    		// Method begins at RVA 0x20f9
    		// Code size 1 (0x1)
    		.maxstack 8
    		IL_0000: ret
    	} // end of method '<M>d__2'::System.IDisposable.Dispose
    	.method private final hidebysig newslot virtual 
    		instance bool MoveNext () cil managed 
    	{
    		.override method instance bool [netstandard]System.Collections.IEnumerator::MoveNext()
    		// Method begins at RVA 0x20fc
    		// Code size 95 (0x5f)
    		.maxstack 2
    		.locals init (
    			[0] int32,
    			[1] class C
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: stloc.0
    		IL_0007: ldarg.0
    		IL_0008: ldfld class C C/'<M>d__2'::'<>4__this'
    		IL_000d: stloc.1
    		IL_000e: ldloc.0
    		IL_000f: switch (IL_0022, IL_003a, IL_0056)
    		IL_0020: ldc.i4.0
    		IL_0021: ret
    		IL_0022: ldarg.0
    		IL_0023: ldc.i4.m1
    		IL_0024: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0029: ldarg.0
    		IL_002a: ldc.i4.s 9
    		IL_002c: stfld int32 C/'<M>d__2'::'<>2__current'
    		IL_0031: ldarg.0
    		IL_0032: ldc.i4.1
    		IL_0033: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0038: ldc.i4.1
    		IL_0039: ret
    		IL_003a: ldarg.0
    		IL_003b: ldc.i4.m1
    		IL_003c: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0041: ldarg.0
    		IL_0042: ldloc.1
    		IL_0043: ldfld int32 C::'<y>P'
    		IL_0048: stfld int32 C/'<M>d__2'::'<>2__current'
    		IL_004d: ldarg.0
    		IL_004e: ldc.i4.2
    		IL_004f: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0054: ldc.i4.1
    		IL_0055: ret
    		IL_0056: ldarg.0
    		IL_0057: ldc.i4.m1
    		IL_0058: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_005d: ldc.i4.0
    		IL_005e: ret
    	} // end of method '<M>d__2'::MoveNext
    	.method private final hidebysig specialname newslot virtual 
    		instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.get_Current' () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance !0 class [netstandard]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
    		// Method begins at RVA 0x2167
    		// Code size 7 (0x7)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>2__current'
    		IL_0006: ret
    	} // end of method '<M>d__2'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'
    	.method private final hidebysig newslot virtual 
    		instance void System.Collections.IEnumerator.Reset () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.Collections.IEnumerator::Reset()
    		// Method begins at RVA 0x216f
    		// Code size 6 (0x6)
    		.maxstack 8
    		IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
    		IL_0005: throw
    	} // end of method '<M>d__2'::System.Collections.IEnumerator.Reset
    	.method private final hidebysig specialname newslot virtual 
    		instance object System.Collections.IEnumerator.get_Current () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance object [netstandard]System.Collections.IEnumerator::get_Current()
    		// Method begins at RVA 0x2176
    		// Code size 12 (0xc)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>2__current'
    		IL_0006: box [netstandard]System.Int32
    		IL_000b: ret
    	} // end of method '<M>d__2'::System.Collections.IEnumerator.get_Current
    	.method private final hidebysig newslot virtual 
    		instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> 'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator' () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance class [netstandard]System.Collections.Generic.IEnumerator`1<!0> class [netstandard]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
    		// Method begins at RVA 0x2184
    		// Code size 55 (0x37)
    		.maxstack 2
    		.locals init (
    			[0] class C/'<M>d__2'
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: ldc.i4.s -2
    		IL_0008: bne.un.s IL_0022
    		IL_000a: ldarg.0
    		IL_000b: ldfld int32 C/'<M>d__2'::'<>l__initialThreadId'
    		IL_0010: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
    		IL_0015: bne.un.s IL_0022
    		IL_0017: ldarg.0
    		IL_0018: ldc.i4.0
    		IL_0019: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_001e: ldarg.0
    		IL_001f: stloc.0
    		IL_0020: br.s IL_0035
    		IL_0022: ldc.i4.0
    		IL_0023: newobj instance void C/'<M>d__2'::.ctor(int32)
    		IL_0028: stloc.0
    		IL_0029: ldloc.0
    		IL_002a: ldarg.0
    		IL_002b: ldfld class C C/'<M>d__2'::'<>4__this'
    		IL_0030: stfld class C C/'<M>d__2'::'<>4__this'
    		IL_0035: ldloc.0
    		IL_0036: ret
    	} // end of method '<M>d__2'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'
    	.method private final hidebysig newslot virtual 
    		instance class [netstandard]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance class [netstandard]System.Collections.IEnumerator [netstandard]System.Collections.IEnumerable::GetEnumerator()
    		// Method begins at RVA 0x21c7
    		// Code size 7 (0x7)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> C/'<M>d__2'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'()
    		IL_0006: ret
    	} // end of method '<M>d__2'::System.Collections.IEnumerable.GetEnumerator
    	// Properties
    	.property instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.Current'()
    	{
    		.get instance int32 C/'<M>d__2'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'()
    	}
    	.property instance object System.Collections.IEnumerator.Current()
    	{
    		.get instance object C/'<M>d__2'::System.Collections.IEnumerator.get_Current()
    	}
    } // end of class <M>d__2
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x20c0
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> M () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
    		01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
    	)
    	// Method begins at RVA 0x20cf
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldc.i4.s -2
    	IL_0002: newobj instance void C/'<M>d__2'::.ctor(int32)
    	IL_0007: dup
    	IL_0008: ldarg.0
    	IL_0009: stfld class C C/'<M>d__2'::'<>4__this'
    	IL_000e: ret
    } // end of method C::M
} // end of class C
").Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "9123");

            verifier2.VerifyTypeIL("C",
(@"
.class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<M>d__2'
		extends [netstandard]System.Object
		implements class [netstandard]System.Collections.Generic.IEnumerable`1<int32>,
		           [netstandard]System.Collections.IEnumerable,
		           class [netstandard]System.Collections.Generic.IEnumerator`1<int32>,
" +
(RuntimeUtilities.IsCoreClrRuntime ?
@"			           [netstandard]System.Collections.IEnumerator,
			           [netstandard]System.IDisposable" :
@"			           [netstandard]System.IDisposable,
			           [netstandard]System.Collections.IEnumerator") + @"
    {
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field private int32 '<>1__state'
		.field private int32 '<>2__current'
		.field private int32 '<>l__initialThreadId'
		.field public class C '<>4__this'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor (
				int32 '<>1__state'
			) cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			// Method begins at RVA 0x20ed
			// Code size 26 (0x1a)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ldarg.0
			IL_0008: ldarg.1
			IL_0009: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_000e: ldarg.0
			IL_000f: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
			IL_0014: stfld int32 C/'<M>d__2'::'<>l__initialThreadId'
			IL_0019: ret
		} // end of method '<M>d__2'::.ctor
		.method private final hidebysig newslot virtual 
			instance void System.IDisposable.Dispose () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [netstandard]System.IDisposable::Dispose()
			// Method begins at RVA 0x2108
			// Code size 1 (0x1)
			.maxstack 8
			IL_0000: ret
		} // end of method '<M>d__2'::System.IDisposable.Dispose
		.method private final hidebysig newslot virtual 
			instance bool MoveNext () cil managed 
		{
			.override method instance bool [netstandard]System.Collections.IEnumerator::MoveNext()
			// Method begins at RVA 0x210c
			// Code size 102 (0x66)
			.maxstack 2
			.locals init (
				[0] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
			IL_0006: stloc.0
			IL_0007: ldloc.0
			IL_0008: switch (IL_001b, IL_001d, IL_001f)
			IL_0019: br.s IL_0021
			IL_001b: br.s IL_0023
			IL_001d: br.s IL_003c
			IL_001f: br.s IL_005d
			IL_0021: ldc.i4.0
			IL_0022: ret
			IL_0023: ldarg.0
			IL_0024: ldc.i4.m1
			IL_0025: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_002a: nop
			IL_002b: ldarg.0
			IL_002c: ldc.i4.s 9
			IL_002e: stfld int32 C/'<M>d__2'::'<>2__current'
			IL_0033: ldarg.0
			IL_0034: ldc.i4.1
			IL_0035: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_003a: ldc.i4.1
			IL_003b: ret
			IL_003c: ldarg.0
			IL_003d: ldc.i4.m1
			IL_003e: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_0043: ldarg.0
			IL_0044: ldarg.0
			IL_0045: ldfld class C C/'<M>d__2'::'<>4__this'
			IL_004a: ldfld int32 C::'<y>P'
			IL_004f: stfld int32 C/'<M>d__2'::'<>2__current'
			IL_0054: ldarg.0
			IL_0055: ldc.i4.2
			IL_0056: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_005b: ldc.i4.1
			IL_005c: ret
			IL_005d: ldarg.0
			IL_005e: ldc.i4.m1
			IL_005f: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_0064: ldc.i4.0
			IL_0065: ret
		} // end of method '<M>d__2'::MoveNext
		.method private final hidebysig specialname newslot virtual 
			instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.get_Current' () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance !0 class [netstandard]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
			// Method begins at RVA 0x217e
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__2'::'<>2__current'
			IL_0006: ret
		} // end of method '<M>d__2'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'
		.method private final hidebysig newslot virtual 
			instance void System.Collections.IEnumerator.Reset () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance void [netstandard]System.Collections.IEnumerator::Reset()
			// Method begins at RVA 0x2186
			// Code size 6 (0x6)
			.maxstack 8
			IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
			IL_0005: throw
		} // end of method '<M>d__2'::System.Collections.IEnumerator.Reset
		.method private final hidebysig specialname newslot virtual 
			instance object System.Collections.IEnumerator.get_Current () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance object [netstandard]System.Collections.IEnumerator::get_Current()
			// Method begins at RVA 0x218d
			// Code size 12 (0xc)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__2'::'<>2__current'
			IL_0006: box [netstandard]System.Int32
			IL_000b: ret
		} // end of method '<M>d__2'::System.Collections.IEnumerator.get_Current
		.method private final hidebysig newslot virtual 
			instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> 'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator' () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance class [netstandard]System.Collections.Generic.IEnumerator`1<!0> class [netstandard]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
			// Method begins at RVA 0x219c
			// Code size 55 (0x37)
			.maxstack 2
			.locals init (
				[0] class C/'<M>d__2'
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
			IL_0006: ldc.i4.s -2
			IL_0008: bne.un.s IL_0022
			IL_000a: ldarg.0
			IL_000b: ldfld int32 C/'<M>d__2'::'<>l__initialThreadId'
			IL_0010: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
			IL_0015: bne.un.s IL_0022
			IL_0017: ldarg.0
			IL_0018: ldc.i4.0
			IL_0019: stfld int32 C/'<M>d__2'::'<>1__state'
			IL_001e: ldarg.0
			IL_001f: stloc.0
			IL_0020: br.s IL_0035
			IL_0022: ldc.i4.0
			IL_0023: newobj instance void C/'<M>d__2'::.ctor(int32)
			IL_0028: stloc.0
			IL_0029: ldloc.0
			IL_002a: ldarg.0
			IL_002b: ldfld class C C/'<M>d__2'::'<>4__this'
			IL_0030: stfld class C C/'<M>d__2'::'<>4__this'
			IL_0035: ldloc.0
			IL_0036: ret
		} // end of method '<M>d__2'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'
		.method private final hidebysig newslot virtual 
			instance class [netstandard]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
		{
			.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
				01 00 00 00
			)
			.override method instance class [netstandard]System.Collections.IEnumerator [netstandard]System.Collections.IEnumerable::GetEnumerator()
			// Method begins at RVA 0x21df
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> C/'<M>d__2'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'()
			IL_0006: ret
		} // end of method '<M>d__2'::System.Collections.IEnumerable.GetEnumerator
		// Properties
		.property instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.Current'()
		{
			.get instance int32 C/'<M>d__2'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'()
		}
		.property instance object System.Collections.IEnumerator.Current()
		{
			.get instance object C/'<M>d__2'::System.Collections.IEnumerator.get_Current()
		}
	} // end of class <M>d__2
	// Fields
	.field private int32 '<y>P'
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.custom instance void [netstandard]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggerBrowsableState) = (
		01 00 00 00 00 00 00 00
	)
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 y
		) cil managed 
	{
		// Method begins at RVA 0x20cd
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 C::'<y>P'
		IL_0007: ldarg.0
		IL_0008: call instance void [netstandard]System.Object::.ctor()
		IL_000d: nop
		IL_000e: ret
	} // end of method C::.ctor
	.method public hidebysig 
		instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> M () cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
			01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
		)
		// Method begins at RVA 0x20dd
		// Code size 15 (0xf)
		.maxstack 8
		IL_0000: ldc.i4.s -2
		IL_0002: newobj instance void C/'<M>d__2'::.ctor(int32)
		IL_0007: dup
		IL_0008: ldarg.0
		IL_0009: stfld class C C/'<M>d__2'::'<>4__this'
		IL_000e: ret
	} // end of method C::M
} // end of class C
").Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_168_InLambda_NoDisplayClass()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).M());
    }
}

class C(int y)
{
    public int M()
    {
        System.Func<int> x = () => y;
        return x();
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "123");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2082
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M () cil managed 
    {
    	// Method begins at RVA 0x2091
    	// Code size 18 (0x12)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldftn instance int32 C::'<M>b__2_0'()
    	IL_0007: newobj instance void class [netstandard]System.Func`1<int32>::.ctor(object, native int)
    	IL_000c: callvirt instance !0 class [netstandard]System.Func`1<int32>::Invoke()
    	IL_0011: ret
    } // end of method C::M
    .method private hidebysig 
    	instance int32 '<M>b__2_0' () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x20a4
    	// Code size 7 (0x7)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldfld int32 C::'<y>P'
    	IL_0006: ret
    } // end of method C::'<M>b__2_0'
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "123");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    .custom instance void [netstandard]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggerBrowsableState) = (
    	01 00 00 00 00 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2087
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: nop
    	IL_000e: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M () cil managed 
    {
    	// Method begins at RVA 0x2098
    	// Code size 25 (0x19)
    	.maxstack 2
    	.locals init (
    		[0] class [netstandard]System.Func`1<int32>,
    		[1] int32
    	)
    	IL_0000: nop
    	IL_0001: ldarg.0
    	IL_0002: ldftn instance int32 C::'<M>b__2_0'()
    	IL_0008: newobj instance void class [netstandard]System.Func`1<int32>::.ctor(object, native int)
    	IL_000d: stloc.0
    	IL_000e: ldloc.0
    	IL_000f: callvirt instance !0 class [netstandard]System.Func`1<int32>::Invoke()
    	IL_0014: stloc.1
    	IL_0015: br.s IL_0017
    	IL_0017: ldloc.1
    	IL_0018: ret
    } // end of method C::M
    .method private hidebysig 
    	instance int32 '<M>b__2_0' () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x20bd
    	// Code size 7 (0x7)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldfld int32 C::'<y>P'
    	IL_0006: ret
    } // end of method C::'<M>b__2_0'
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_169_InLambda_WithDisplayClass()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).M(9000));
    }
}

class C(int y)
{
    public int M(int a)
    {
        System.Func<int> x = () => y + a;
        return x();
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "9123");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass2_0'
    	extends [netstandard]System.Object
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public class C '<>4__this'
    	.field public int32 a
    	// Methods
    	.method public hidebysig specialname rtspecialname 
    		instance void .ctor () cil managed 
    	{
    		// Method begins at RVA 0x207f
    		// Code size 7 (0x7)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance void [netstandard]System.Object::.ctor()
    		IL_0006: ret
    	} // end of method '<>c__DisplayClass2_0'::.ctor
    	.method assembly hidebysig 
    		instance int32 '<M>b__0' () cil managed 
    	{
    		// Method begins at RVA 0x20bb
    		// Code size 19 (0x13)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldfld class C C/'<>c__DisplayClass2_0'::'<>4__this'
    		IL_0006: ldfld int32 C::'<y>P'
    		IL_000b: ldarg.0
    		IL_000c: ldfld int32 C/'<>c__DisplayClass2_0'::a
    		IL_0011: add
    		IL_0012: ret
    	} // end of method '<>c__DisplayClass2_0'::'<M>b__0'
    } // end of class <>c__DisplayClass2_0
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2087
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M (
    		int32 a
    	) cil managed 
    {
    	// Method begins at RVA 0x2096
    	// Code size 36 (0x24)
    	.maxstack 8
    	IL_0000: newobj instance void C/'<>c__DisplayClass2_0'::.ctor()
    	IL_0005: dup
    	IL_0006: ldarg.0
    	IL_0007: stfld class C C/'<>c__DisplayClass2_0'::'<>4__this'
    	IL_000c: dup
    	IL_000d: ldarg.1
    	IL_000e: stfld int32 C/'<>c__DisplayClass2_0'::a
    	IL_0013: ldftn instance int32 C/'<>c__DisplayClass2_0'::'<M>b__0'()
    	IL_0019: newobj instance void class [netstandard]System.Func`1<int32>::.ctor(object, native int)
    	IL_001e: callvirt instance !0 class [netstandard]System.Func`1<int32>::Invoke()
    	IL_0023: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "9123");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass2_0'
    	extends [netstandard]System.Object
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public class C '<>4__this'
    	.field public int32 a
    	// Methods
    	.method public hidebysig specialname rtspecialname 
    		instance void .ctor () cil managed 
    	{
    		// Method begins at RVA 0x2083
    		// Code size 8 (0x8)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance void [netstandard]System.Object::.ctor()
    		IL_0006: nop
    		IL_0007: ret
    	} // end of method '<>c__DisplayClass2_0'::.ctor
    	.method assembly hidebysig 
    		instance int32 '<M>b__0' () cil managed 
    	{
    		// Method begins at RVA 0x20d5
    		// Code size 19 (0x13)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldfld class C C/'<>c__DisplayClass2_0'::'<>4__this'
    		IL_0006: ldfld int32 C::'<y>P'
    		IL_000b: ldarg.0
    		IL_000c: ldfld int32 C/'<>c__DisplayClass2_0'::a
    		IL_0011: add
    		IL_0012: ret
    	} // end of method '<>c__DisplayClass2_0'::'<M>b__0'
    } // end of class <>c__DisplayClass2_0
    // Fields
    .field private int32 '<y>P'
    .custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    	01 00 00 00
    )
    .custom instance void [netstandard]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [netstandard]System.Diagnostics.DebuggerBrowsableState) = (
    	01 00 00 00 00 00 00 00
    )
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x208c
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::'<y>P'
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: nop
    	IL_000e: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M (
    		int32 a
    	) cil managed 
    {
    	// Method begins at RVA 0x209c
    	// Code size 45 (0x2d)
    	.maxstack 2
    	.locals init (
    		[0] class C/'<>c__DisplayClass2_0',
    		[1] class [netstandard]System.Func`1<int32>,
    		[2] int32
    	)
    	IL_0000: newobj instance void C/'<>c__DisplayClass2_0'::.ctor()
    	IL_0005: stloc.0
    	IL_0006: ldloc.0
    	IL_0007: ldarg.0
    	IL_0008: stfld class C C/'<>c__DisplayClass2_0'::'<>4__this'
    	IL_000d: ldloc.0
    	IL_000e: ldarg.1
    	IL_000f: stfld int32 C/'<>c__DisplayClass2_0'::a
    	IL_0014: nop
    	IL_0015: ldloc.0
    	IL_0016: ldftn instance int32 C/'<>c__DisplayClass2_0'::'<M>b__0'()
    	IL_001c: newobj instance void class [netstandard]System.Func`1<int32>::.ctor(object, native int)
    	IL_0021: stloc.1
    	IL_0022: ldloc.1
    	IL_0023: callvirt instance !0 class [netstandard]System.Func`1<int32>::Invoke()
    	IL_0028: stloc.2
    	IL_0029: br.s IL_002b
    	IL_002b: ldloc.2
    	IL_002c: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_170_InAsyncLambda()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).F().Result);
    }
}

class C(int y)
{
    public System.Func<Task<int>> F = async Task<int> () =>
                                      {
                                          await Task.Yield();
                                          return y;
                                      };
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "123");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Nested Types
		.class nested private auto ansi sealed beforefieldinit '<<-ctor>b__0>d'
			extends [netstandard]System.ValueType
			implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
		{
			// Fields
			.field public int32 '<>1__state'
			.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
			.field public class C/'<>c__DisplayClass0_0' '<>4__this'
			.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
			// Methods
			.method private final hidebysig newslot virtual 
				instance void MoveNext () cil managed 
			{
				.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
				// Method begins at RVA 0x2104
				// Code size 163 (0xa3)
				.maxstack 3
				.locals init (
					[0] int32,
					[1] class C/'<>c__DisplayClass0_0',
					[2] int32,
					[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
					[4] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
					[5] class [netstandard]System.Exception
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
				IL_0006: stloc.0
				IL_0007: ldarg.0
				IL_0008: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>4__this'
				IL_000d: stloc.1
				.try
				{
					IL_000e: ldloc.0
					IL_000f: brfalse.s IL_0049
					IL_0011: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
					IL_0016: stloc.s 4
					IL_0018: ldloca.s 4
					IL_001a: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
					IL_001f: stloc.3
					IL_0020: ldloca.s 3
					IL_0022: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
					IL_0027: brtrue.s IL_0065
					IL_0029: ldarg.0
					IL_002a: ldc.i4.0
					IL_002b: dup
					IL_002c: stloc.0
					IL_002d: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_0032: ldarg.0
					IL_0033: ldloc.3
					IL_0034: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_0039: ldarg.0
					IL_003a: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
					IL_003f: ldloca.s 3
					IL_0041: ldarg.0
					IL_0042: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'>(!!0&, !!1&)
					IL_0047: leave.s IL_00a2
					IL_0049: ldarg.0
					IL_004a: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_004f: stloc.3
					IL_0050: ldarg.0
					IL_0051: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_0056: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
					IL_005c: ldarg.0
					IL_005d: ldc.i4.m1
					IL_005e: dup
					IL_005f: stloc.0
					IL_0060: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_0065: ldloca.s 3
					IL_0067: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
					IL_006c: ldloc.1
					IL_006d: ldfld int32 C/'<>c__DisplayClass0_0'::y
					IL_0072: stloc.2
					IL_0073: leave.s IL_008e
				} // end .try
				catch [netstandard]System.Exception
				{
					IL_0075: stloc.s 5
					IL_0077: ldarg.0
					IL_0078: ldc.i4.s -2
					IL_007a: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_007f: ldarg.0
					IL_0080: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
					IL_0085: ldloc.s 5
					IL_0087: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
					IL_008c: leave.s IL_00a2
				} // end handler
				IL_008e: ldarg.0
				IL_008f: ldc.i4.s -2
				IL_0091: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
				IL_0096: ldarg.0
				IL_0097: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
				IL_009c: ldloc.2
				IL_009d: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
				IL_00a2: ret
			} // end of method '<<-ctor>b__0>d'::MoveNext
			.method private final hidebysig newslot virtual 
				instance void SetStateMachine (
					class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
				) cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
				// Method begins at RVA 0x21c4
				// Code size 13 (0xd)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
				IL_0006: ldarg.1
				IL_0007: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
				IL_000c: ret
			} // end of method '<<-ctor>b__0>d'::SetStateMachine
		} // end of class <<-ctor>b__0>d
		// Fields
		.field public int32 y
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2084
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance class [netstandard]System.Threading.Tasks.Task`1<int32> '<.ctor>b__0' () cil managed 
		{
			.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
				01 00 25 43 2b 3c 3e 63 5f 5f 44 69 73 70 6c 61
				79 43 6c 61 73 73 30 5f 30 2b 3c 3c 2d 63 74 6f
				72 3e 62 5f 5f 30 3e 64 00 00
			)
			// Method begins at RVA 0x20c0
			// Code size 55 (0x37)
			.maxstack 2
			.locals init (
				[0] valuetype C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'
			)
			IL_0000: ldloca.s 0
			IL_0002: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
			IL_0007: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_000c: ldloca.s 0
			IL_000e: ldarg.0
			IL_000f: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>4__this'
			IL_0014: ldloca.s 0
			IL_0016: ldc.i4.m1
			IL_0017: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
			IL_001c: ldloca.s 0
			IL_001e: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_0023: ldloca.s 0
			IL_0025: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<valuetype C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'>(!!0&)
			IL_002a: ldloca.s 0
			IL_002c: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_0031: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
			IL_0036: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>b__0'
	} // end of class <>c__DisplayClass0_0
	// Fields
	.field public class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>> F
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 y
		) cil managed 
	{
		// Method begins at RVA 0x208c
		// Code size 38 (0x26)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldarg.1
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::y
		IL_000d: ldarg.0
		IL_000e: ldloc.0
		IL_000f: ldftn instance class [netstandard]System.Threading.Tasks.Task`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>b__0'()
		IL_0015: newobj instance void class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>>::.ctor(object, native int)
		IL_001a: stfld class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>> C::F
		IL_001f: ldarg.0
		IL_0020: call instance void [netstandard]System.Object::.ctor()
		IL_0025: ret
	} // end of method C::.ctor
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "123");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Nested Types
		.class nested private auto ansi sealed beforefieldinit '<<-ctor>b__0>d'
			extends [netstandard]System.Object
			implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
		{
			// Fields
			.field public int32 '<>1__state'
			.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
			.field public class C/'<>c__DisplayClass0_0' '<>4__this'
			.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
			// Methods
			.method public hidebysig specialname rtspecialname 
				instance void .ctor () cil managed 
			{
				// Method begins at RVA 0x2088
				// Code size 8 (0x8)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: call instance void [netstandard]System.Object::.ctor()
				IL_0006: nop
				IL_0007: ret
			} // end of method '<<-ctor>b__0>d'::.ctor
			.method private final hidebysig newslot virtual 
				instance void MoveNext () cil managed 
			{
				.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
				// Method begins at RVA 0x210c
				// Code size 173 (0xad)
				.maxstack 3
				.locals init (
					[0] int32,
					[1] int32,
					[2] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
					[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
					[4] class C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d',
					[5] class [netstandard]System.Exception
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
				IL_0006: stloc.0
				.try
				{
					IL_0007: ldloc.0
					IL_0008: brfalse.s IL_000c
					IL_000a: br.s IL_000e
					IL_000c: br.s IL_004b
					IL_000e: nop
					IL_000f: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
					IL_0014: stloc.3
					IL_0015: ldloca.s 3
					IL_0017: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
					IL_001c: stloc.2
					IL_001d: ldloca.s 2
					IL_001f: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
					IL_0024: brtrue.s IL_0067
					IL_0026: ldarg.0
					IL_0027: ldc.i4.0
					IL_0028: dup
					IL_0029: stloc.0
					IL_002a: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_002f: ldarg.0
					IL_0030: ldloc.2
					IL_0031: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_0036: ldarg.0
					IL_0037: stloc.s 4
					IL_0039: ldarg.0
					IL_003a: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
					IL_003f: ldloca.s 2
					IL_0041: ldloca.s 4
					IL_0043: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, class C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'>(!!0&, !!1&)
					IL_0048: nop
					IL_0049: leave.s IL_00ac
					IL_004b: ldarg.0
					IL_004c: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_0051: stloc.2
					IL_0052: ldarg.0
					IL_0053: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>u__1'
					IL_0058: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
					IL_005e: ldarg.0
					IL_005f: ldc.i4.m1
					IL_0060: dup
					IL_0061: stloc.0
					IL_0062: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_0067: ldloca.s 2
					IL_0069: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
					IL_006e: nop
					IL_006f: ldarg.0
					IL_0070: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>4__this'
					IL_0075: ldfld int32 C/'<>c__DisplayClass0_0'::y
					IL_007a: stloc.1
					IL_007b: leave.s IL_0097
				} // end .try
				catch [netstandard]System.Exception
				{
					IL_007d: stloc.s 5
					IL_007f: ldarg.0
					IL_0080: ldc.i4.s -2
					IL_0082: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
					IL_0087: ldarg.0
					IL_0088: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
					IL_008d: ldloc.s 5
					IL_008f: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
					IL_0094: nop
					IL_0095: leave.s IL_00ac
				} // end handler
				IL_0097: ldarg.0
				IL_0098: ldc.i4.s -2
				IL_009a: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
				IL_009f: ldarg.0
				IL_00a0: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
				IL_00a5: ldloc.1
				IL_00a6: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
				IL_00ab: nop
				IL_00ac: ret
			} // end of method '<<-ctor>b__0>d'::MoveNext
			.method private final hidebysig newslot virtual 
				instance void SetStateMachine (
					class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
				) cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
				// Method begins at RVA 0x21d8
				// Code size 1 (0x1)
				.maxstack 8
				IL_0000: ret
			} // end of method '<<-ctor>b__0>d'::SetStateMachine
		} // end of class <<-ctor>b__0>d
		// Fields
		.field public int32 y
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x2088
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance class [netstandard]System.Threading.Tasks.Task`1<int32> '<.ctor>b__0' () cil managed 
		{
			.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
				01 00 25 43 2b 3c 3e 63 5f 5f 44 69 73 70 6c 61
				79 43 6c 61 73 73 30 5f 30 2b 3c 3c 2d 63 74 6f
				72 3e 62 5f 5f 30 3e 64 00 00
			)
			.custom instance void [netstandard]System.Diagnostics.DebuggerStepThroughAttribute::.ctor() = (
				01 00 00 00
			)
			// Method begins at RVA 0x20c8
			// Code size 56 (0x38)
			.maxstack 2
			.locals init (
				[0] class C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'
			)
			IL_0000: newobj instance void C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::.ctor()
			IL_0005: stloc.0
			IL_0006: ldloc.0
			IL_0007: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
			IL_000c: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_0011: ldloc.0
			IL_0012: ldarg.0
			IL_0013: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>4__this'
			IL_0018: ldloc.0
			IL_0019: ldc.i4.m1
			IL_001a: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>1__state'
			IL_001f: ldloc.0
			IL_0020: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_0025: ldloca.s 0
			IL_0027: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<class C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'>(!!0&)
			IL_002c: ldloc.0
			IL_002d: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>b__0>d'::'<>t__builder'
			IL_0032: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
			IL_0037: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>b__0'
	} // end of class <>c__DisplayClass0_0
	// Fields
	.field public class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>> F
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 y
		) cil managed 
	{
		// Method begins at RVA 0x2094
		// Code size 39 (0x27)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldarg.1
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::y
		IL_000d: ldarg.0
		IL_000e: ldloc.0
		IL_000f: ldftn instance class [netstandard]System.Threading.Tasks.Task`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>b__0'()
		IL_0015: newobj instance void class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>>::.ctor(object, native int)
		IL_001a: stfld class [netstandard]System.Func`1<class [netstandard]System.Threading.Tasks.Task`1<int32>> C::F
		IL_001f: ldarg.0
		IL_0020: call instance void [netstandard]System.Object::.ctor()
		IL_0025: nop
		IL_0026: ret
	} // end of method C::.ctor
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_171_InIterator()
        {
            var source = """
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        foreach (var x in new C(123).F())
        {
            System.Console.Write(x);
        }
    }
}

class C(int y)
{
    public System.Func<IEnumerable<int>> F = IEnumerable<int>() =>
                                             {
                                                 IEnumerable<int> local()
                                                 {
                                                     yield return 9;
                                                     yield return y;
                                                 };

                                                 return local();
                                             };
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "9123");

            verifier1.VerifyTypeIL("C",
(@"
.class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Nested Types
		.class nested private auto ansi sealed beforefieldinit '<<-ctor>g__local|1>d'
			extends [netstandard]System.Object
			implements class [netstandard]System.Collections.Generic.IEnumerable`1<int32>,
			           [netstandard]System.Collections.IEnumerable,
			           class [netstandard]System.Collections.Generic.IEnumerator`1<int32>,
" +
(RuntimeUtilities.IsCoreClrRuntime ?
@"			           [netstandard]System.Collections.IEnumerator,
			           [netstandard]System.IDisposable" :
@"			           [netstandard]System.IDisposable,
			           [netstandard]System.Collections.IEnumerator") + @"
		{
			// Fields
			.field private int32 '<>1__state'
			.field private int32 '<>2__current'
			.field private int32 '<>l__initialThreadId'
			.field public class C/'<>c__DisplayClass0_0' '<>4__this'
			// Methods
			.method public hidebysig specialname rtspecialname 
				instance void .ctor (
					int32 '<>1__state'
				) cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				// Method begins at RVA 0x2112
				// Code size 25 (0x19)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: call instance void [netstandard]System.Object::.ctor()
				IL_0006: ldarg.0
				IL_0007: ldarg.1
				IL_0008: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_000d: ldarg.0
				IL_000e: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
				IL_0013: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>l__initialThreadId'
				IL_0018: ret
			} // end of method '<<-ctor>g__local|1>d'::.ctor
			.method private final hidebysig newslot virtual 
				instance void System.IDisposable.Dispose () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.IDisposable::Dispose()
				// Method begins at RVA 0x212c
				// Code size 1 (0x1)
				.maxstack 8
				IL_0000: ret
			} // end of method '<<-ctor>g__local|1>d'::System.IDisposable.Dispose
			.method private final hidebysig newslot virtual 
				instance bool MoveNext () cil managed 
			{
				.override method instance bool [netstandard]System.Collections.IEnumerator::MoveNext()
				// Method begins at RVA 0x2130
				// Code size 95 (0x5f)
				.maxstack 2
				.locals init (
					[0] int32,
					[1] class C/'<>c__DisplayClass0_0'
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0006: stloc.0
				IL_0007: ldarg.0
				IL_0008: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_000d: stloc.1
				IL_000e: ldloc.0
				IL_000f: switch (IL_0022, IL_003a, IL_0056)
				IL_0020: ldc.i4.0
				IL_0021: ret
				IL_0022: ldarg.0
				IL_0023: ldc.i4.m1
				IL_0024: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0029: ldarg.0
				IL_002a: ldc.i4.s 9
				IL_002c: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0031: ldarg.0
				IL_0032: ldc.i4.1
				IL_0033: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0038: ldc.i4.1
				IL_0039: ret
				IL_003a: ldarg.0
				IL_003b: ldc.i4.m1
				IL_003c: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0041: ldarg.0
				IL_0042: ldloc.1
				IL_0043: ldfld int32 C/'<>c__DisplayClass0_0'::y
				IL_0048: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_004d: ldarg.0
				IL_004e: ldc.i4.2
				IL_004f: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0054: ldc.i4.1
				IL_0055: ret
				IL_0056: ldarg.0
				IL_0057: ldc.i4.m1
				IL_0058: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_005d: ldc.i4.0
				IL_005e: ret
			} // end of method '<<-ctor>g__local|1>d'::MoveNext
			.method private final hidebysig specialname newslot virtual 
				instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.get_Current' () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance !0 class [netstandard]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
				// Method begins at RVA 0x219b
				// Code size 7 (0x7)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0006: ret
			} // end of method '<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'
			.method private final hidebysig newslot virtual 
				instance void System.Collections.IEnumerator.Reset () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.Collections.IEnumerator::Reset()
				// Method begins at RVA 0x21a3
				// Code size 6 (0x6)
				.maxstack 8
				IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
				IL_0005: throw
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerator.Reset
			.method private final hidebysig specialname newslot virtual 
				instance object System.Collections.IEnumerator.get_Current () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance object [netstandard]System.Collections.IEnumerator::get_Current()
				// Method begins at RVA 0x21aa
				// Code size 12 (0xc)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0006: box [netstandard]System.Int32
				IL_000b: ret
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerator.get_Current
			.method private final hidebysig newslot virtual 
				instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> 'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator' () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance class [netstandard]System.Collections.Generic.IEnumerator`1<!0> class [netstandard]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
				// Method begins at RVA 0x21b8
				// Code size 55 (0x37)
				.maxstack 2
				.locals init (
					[0] class C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0006: ldc.i4.s -2
				IL_0008: bne.un.s IL_0022
				IL_000a: ldarg.0
				IL_000b: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>l__initialThreadId'
				IL_0010: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
				IL_0015: bne.un.s IL_0022
				IL_0017: ldarg.0
				IL_0018: ldc.i4.0
				IL_0019: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_001e: ldarg.0
				IL_001f: stloc.0
				IL_0020: br.s IL_0035
				IL_0022: ldc.i4.0
				IL_0023: newobj instance void C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::.ctor(int32)
				IL_0028: stloc.0
				IL_0029: ldloc.0
				IL_002a: ldarg.0
				IL_002b: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_0030: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_0035: ldloc.0
				IL_0036: ret
			} // end of method '<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'
			.method private final hidebysig newslot virtual 
				instance class [netstandard]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance class [netstandard]System.Collections.IEnumerator [netstandard]System.Collections.IEnumerable::GetEnumerator()
				// Method begins at RVA 0x21fb
				// Code size 7 (0x7)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: call instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'()
				IL_0006: ret
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerable.GetEnumerator
			// Properties
			.property instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.Current'()
			{
				.get instance int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'()
			}
			.property instance object System.Collections.IEnumerator.Current()
			{
				.get instance object C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::System.Collections.IEnumerator.get_Current()
			}
		} // end of class <<-ctor>g__local|1>d
		// Fields
		.field public int32 y
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20c0
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> '<.ctor>b__0' () cil managed 
		{
			// Method begins at RVA 0x20fa
			// Code size 7 (0x7)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>g__local|1'()
			IL_0006: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>b__0'
		.method assembly hidebysig 
			instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> '<.ctor>g__local|1' () cil managed 
		{
			.custom instance void [netstandard]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
				01 00 2b 43 2b 3c 3e 63 5f 5f 44 69 73 70 6c 61
				79 43 6c 61 73 73 30 5f 30 2b 3c 3c 2d 63 74 6f
				72 3e 67 5f 5f 6c 6f 63 61 6c 7c 31 3e 64 00 00
			)
			// Method begins at RVA 0x2102
			// Code size 15 (0xf)
			.maxstack 8
			IL_0000: ldc.i4.s -2
			IL_0002: newobj instance void C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::.ctor(int32)
			IL_0007: dup
			IL_0008: ldarg.0
			IL_0009: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
			IL_000e: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>g__local|1'
	} // end of class <>c__DisplayClass0_0
	// Fields
	.field public class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>> F
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 y
		) cil managed 
	{
		// Method begins at RVA 0x20c8
		// Code size 38 (0x26)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldarg.1
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::y
		IL_000d: ldarg.0
		IL_000e: ldloc.0
		IL_000f: ldftn instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>b__0'()
		IL_0015: newobj instance void class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>>::.ctor(object, native int)
		IL_001a: stfld class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>> C::F
		IL_001f: ldarg.0
		IL_0020: call instance void [netstandard]System.Object::.ctor()
		IL_0025: ret
	} // end of method C::.ctor
} // end of class C
").Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "9123");

            verifier2.VerifyTypeIL("C",
(@"
.class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Nested Types
		.class nested private auto ansi sealed beforefieldinit '<<-ctor>g__local|1>d'
			extends [netstandard]System.Object
			implements class [netstandard]System.Collections.Generic.IEnumerable`1<int32>,
			           [netstandard]System.Collections.IEnumerable,
			           class [netstandard]System.Collections.Generic.IEnumerator`1<int32>,
" +
(RuntimeUtilities.IsCoreClrRuntime ?
@"			           [netstandard]System.Collections.IEnumerator,
			           [netstandard]System.IDisposable" :
@"			           [netstandard]System.IDisposable,
			           [netstandard]System.Collections.IEnumerator") + @"
		{
			// Fields
			.field private int32 '<>1__state'
			.field private int32 '<>2__current'
			.field private int32 '<>l__initialThreadId'
			.field public class C/'<>c__DisplayClass0_0' '<>4__this'
			// Methods
			.method public hidebysig specialname rtspecialname 
				instance void .ctor (
					int32 '<>1__state'
				) cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				// Method begins at RVA 0x2147
				// Code size 26 (0x1a)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: call instance void [netstandard]System.Object::.ctor()
				IL_0006: nop
				IL_0007: ldarg.0
				IL_0008: ldarg.1
				IL_0009: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_000e: ldarg.0
				IL_000f: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
				IL_0014: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>l__initialThreadId'
				IL_0019: ret
			} // end of method '<<-ctor>g__local|1>d'::.ctor
			.method private final hidebysig newslot virtual 
				instance void System.IDisposable.Dispose () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.IDisposable::Dispose()
				// Method begins at RVA 0x2162
				// Code size 1 (0x1)
				.maxstack 8
				IL_0000: ret
			} // end of method '<<-ctor>g__local|1>d'::System.IDisposable.Dispose
			.method private final hidebysig newslot virtual 
				instance bool MoveNext () cil managed 
			{
				.override method instance bool [netstandard]System.Collections.IEnumerator::MoveNext()
				// Method begins at RVA 0x2164
				// Code size 102 (0x66)
				.maxstack 2
				.locals init (
					[0] int32
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0006: stloc.0
				IL_0007: ldloc.0
				IL_0008: switch (IL_001b, IL_001d, IL_001f)
				IL_0019: br.s IL_0021
				IL_001b: br.s IL_0023
				IL_001d: br.s IL_003c
				IL_001f: br.s IL_005d
				IL_0021: ldc.i4.0
				IL_0022: ret
				IL_0023: ldarg.0
				IL_0024: ldc.i4.m1
				IL_0025: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_002a: nop
				IL_002b: ldarg.0
				IL_002c: ldc.i4.s 9
				IL_002e: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0033: ldarg.0
				IL_0034: ldc.i4.1
				IL_0035: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_003a: ldc.i4.1
				IL_003b: ret
				IL_003c: ldarg.0
				IL_003d: ldc.i4.m1
				IL_003e: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0043: ldarg.0
				IL_0044: ldarg.0
				IL_0045: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_004a: ldfld int32 C/'<>c__DisplayClass0_0'::y
				IL_004f: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0054: ldarg.0
				IL_0055: ldc.i4.2
				IL_0056: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_005b: ldc.i4.1
				IL_005c: ret
				IL_005d: ldarg.0
				IL_005e: ldc.i4.m1
				IL_005f: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0064: ldc.i4.0
				IL_0065: ret
			} // end of method '<<-ctor>g__local|1>d'::MoveNext
			.method private final hidebysig specialname newslot virtual 
				instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.get_Current' () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance !0 class [netstandard]System.Collections.Generic.IEnumerator`1<int32>::get_Current()
				// Method begins at RVA 0x21d6
				// Code size 7 (0x7)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0006: ret
			} // end of method '<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'
			.method private final hidebysig newslot virtual 
				instance void System.Collections.IEnumerator.Reset () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance void [netstandard]System.Collections.IEnumerator::Reset()
				// Method begins at RVA 0x21de
				// Code size 6 (0x6)
				.maxstack 8
				IL_0000: newobj instance void [netstandard]System.NotSupportedException::.ctor()
				IL_0005: throw
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerator.Reset
			.method private final hidebysig specialname newslot virtual 
				instance object System.Collections.IEnumerator.get_Current () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance object [netstandard]System.Collections.IEnumerator::get_Current()
				// Method begins at RVA 0x21e5
				// Code size 12 (0xc)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>2__current'
				IL_0006: box [netstandard]System.Int32
				IL_000b: ret
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerator.get_Current
			.method private final hidebysig newslot virtual 
				instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> 'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator' () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance class [netstandard]System.Collections.Generic.IEnumerator`1<!0> class [netstandard]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator()
				// Method begins at RVA 0x21f4
				// Code size 55 (0x37)
				.maxstack 2
				.locals init (
					[0] class C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'
				)
				IL_0000: ldarg.0
				IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_0006: ldc.i4.s -2
				IL_0008: bne.un.s IL_0022
				IL_000a: ldarg.0
				IL_000b: ldfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>l__initialThreadId'
				IL_0010: call int32 [netstandard]System.Environment::get_CurrentManagedThreadId()
				IL_0015: bne.un.s IL_0022
				IL_0017: ldarg.0
				IL_0018: ldc.i4.0
				IL_0019: stfld int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>1__state'
				IL_001e: ldarg.0
				IL_001f: stloc.0
				IL_0020: br.s IL_0035
				IL_0022: ldc.i4.0
				IL_0023: newobj instance void C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::.ctor(int32)
				IL_0028: stloc.0
				IL_0029: ldloc.0
				IL_002a: ldarg.0
				IL_002b: ldfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_0030: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
				IL_0035: ldloc.0
				IL_0036: ret
			} // end of method '<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'
			.method private final hidebysig newslot virtual 
				instance class [netstandard]System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () cil managed 
			{
				.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
					01 00 00 00
				)
				.override method instance class [netstandard]System.Collections.IEnumerator [netstandard]System.Collections.IEnumerable::GetEnumerator()
				// Method begins at RVA 0x2237
				// Code size 7 (0x7)
				.maxstack 8
				IL_0000: ldarg.0
				IL_0001: call instance class [netstandard]System.Collections.Generic.IEnumerator`1<int32> C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator'()
				IL_0006: ret
			} // end of method '<<-ctor>g__local|1>d'::System.Collections.IEnumerable.GetEnumerator
			// Properties
			.property instance int32 'System.Collections.Generic.IEnumerator<System.Int32>.Current'()
			{
				.get instance int32 C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'System.Collections.Generic.IEnumerator<System.Int32>.get_Current'()
			}
			.property instance object System.Collections.IEnumerator.Current()
			{
				.get instance object C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::System.Collections.IEnumerator.get_Current()
			}
		} // end of class <<-ctor>g__local|1>d
		// Fields
		.field public int32 y
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20cc
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> '<.ctor>b__0' () cil managed 
		{
			// Method begins at RVA 0x210c
			// Code size 14 (0xe)
			.maxstack 1
			.locals init (
				[0] class [netstandard]System.Collections.Generic.IEnumerable`1<int32>
			)
			IL_0000: nop
			IL_0001: nop
			IL_0002: nop
			IL_0003: ldarg.0
			IL_0004: call instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>g__local|1'()
			IL_0009: stloc.0
			IL_000a: br.s IL_000c
			IL_000c: ldloc.0
			IL_000d: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>b__0'
		.method assembly hidebysig 
			instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> '<.ctor>g__local|1' () cil managed 
		{
			.custom instance void [netstandard]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
				01 00 2b 43 2b 3c 3e 63 5f 5f 44 69 73 70 6c 61
				79 43 6c 61 73 73 30 5f 30 2b 3c 3c 2d 63 74 6f
				72 3e 67 5f 5f 6c 6f 63 61 6c 7c 31 3e 64 00 00
			)
			// Method begins at RVA 0x2128
			// Code size 19 (0x13)
			.maxstack 2
			.locals init (
				[0] class C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d',
				[1] class [netstandard]System.Collections.Generic.IEnumerable`1<int32>
			)
			IL_0000: ldc.i4.s -2
			IL_0002: newobj instance void C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::.ctor(int32)
			IL_0007: stloc.0
			IL_0008: ldloc.0
			IL_0009: ldarg.0
			IL_000a: stfld class C/'<>c__DisplayClass0_0' C/'<>c__DisplayClass0_0'/'<<-ctor>g__local|1>d'::'<>4__this'
			IL_000f: ldloc.0
			IL_0010: stloc.1
			IL_0011: ldloc.1
			IL_0012: ret
		} // end of method '<>c__DisplayClass0_0'::'<.ctor>g__local|1'
	} // end of class <>c__DisplayClass0_0
	// Fields
	.field public class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>> F
	// Methods
	.method public hidebysig specialname rtspecialname 
		instance void .ctor (
			int32 y
		) cil managed 
	{
		// Method begins at RVA 0x20d8
		// Code size 39 (0x27)
		.maxstack 3
		.locals init (
			[0] class C/'<>c__DisplayClass0_0'
		)
		IL_0000: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0005: stloc.0
		IL_0006: ldloc.0
		IL_0007: ldarg.1
		IL_0008: stfld int32 C/'<>c__DisplayClass0_0'::y
		IL_000d: ldarg.0
		IL_000e: ldloc.0
		IL_000f: ldftn instance class [netstandard]System.Collections.Generic.IEnumerable`1<int32> C/'<>c__DisplayClass0_0'::'<.ctor>b__0'()
		IL_0015: newobj instance void class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>>::.ctor(object, native int)
		IL_001a: stfld class [netstandard]System.Func`1<class [netstandard]System.Collections.Generic.IEnumerable`1<int32>> C::F
		IL_001f: ldarg.0
		IL_0020: call instance void [netstandard]System.Object::.ctor()
		IL_0025: nop
		IL_0026: ret
	} // end of method C::.ctor
} // end of class C
").Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_172_InAsyncMethod()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).M().Result);
    }
}

class C(int y)
{
    private int Y = y;

    public async Task<int> M()
    {
        await Task.Yield();
        return 124;
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "124");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__2'
    	extends [netstandard]System.ValueType
    	implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 '<>1__state'
    	.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
    	.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
    	// Methods
    	.method private final hidebysig newslot virtual 
    		instance void MoveNext () cil managed 
    	{
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
    		// Method begins at RVA 0x20d4
    		// Code size 151 (0x97)
    		.maxstack 3
    		.locals init (
    			[0] int32,
    			[1] int32,
    			[2] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
    			[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
    			[4] class [netstandard]System.Exception
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: stloc.0
    		.try
    		{
    			IL_0007: ldloc.0
    			IL_0008: brfalse.s IL_0041
    			IL_000a: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
    			IL_000f: stloc.3
    			IL_0010: ldloca.s 3
    			IL_0012: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
    			IL_0017: stloc.2
    			IL_0018: ldloca.s 2
    			IL_001a: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
    			IL_001f: brtrue.s IL_005d
    			IL_0021: ldarg.0
    			IL_0022: ldc.i4.0
    			IL_0023: dup
    			IL_0024: stloc.0
    			IL_0025: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_002a: ldarg.0
    			IL_002b: ldloc.2
    			IL_002c: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0031: ldarg.0
    			IL_0032: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_0037: ldloca.s 2
    			IL_0039: ldarg.0
    			IL_003a: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, valuetype C/'<M>d__2'>(!!0&, !!1&)
    			IL_003f: leave.s IL_0096
    			IL_0041: ldarg.0
    			IL_0042: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0047: stloc.2
    			IL_0048: ldarg.0
    			IL_0049: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_004e: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
    			IL_0054: ldarg.0
    			IL_0055: ldc.i4.m1
    			IL_0056: dup
    			IL_0057: stloc.0
    			IL_0058: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_005d: ldloca.s 2
    			IL_005f: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
    			IL_0064: ldc.i4.s 124
    			IL_0066: stloc.1
    			IL_0067: leave.s IL_0082
    		} // end .try
    		catch [netstandard]System.Exception
    		{
    			IL_0069: stloc.s 4
    			IL_006b: ldarg.0
    			IL_006c: ldc.i4.s -2
    			IL_006e: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0073: ldarg.0
    			IL_0074: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_0079: ldloc.s 4
    			IL_007b: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
    			IL_0080: leave.s IL_0096
    		} // end handler
    		IL_0082: ldarg.0
    		IL_0083: ldc.i4.s -2
    		IL_0085: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_008a: ldarg.0
    		IL_008b: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_0090: ldloc.1
    		IL_0091: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
    		IL_0096: ret
    	} // end of method '<M>d__2'::MoveNext
    	.method private final hidebysig newslot virtual 
    		instance void SetStateMachine (
    			class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
    		) cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		// Method begins at RVA 0x2188
    		// Code size 13 (0xd)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_0006: ldarg.1
    		IL_0007: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		IL_000c: ret
    	} // end of method '<M>d__2'::SetStateMachine
    } // end of class <M>d__2
    // Fields
    .field private int32 Y
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2087
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::Y
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance class [netstandard]System.Threading.Tasks.Task`1<int32> M () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
    		01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
    	)
    	// Method begins at RVA 0x2098
    	// Code size 47 (0x2f)
    	.maxstack 2
    	.locals init (
    		[0] valuetype C/'<M>d__2'
    	)
    	IL_0000: ldloca.s 0
    	IL_0002: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
    	IL_0007: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_000c: ldloca.s 0
    	IL_000e: ldc.i4.m1
    	IL_000f: stfld int32 C/'<M>d__2'::'<>1__state'
    	IL_0014: ldloca.s 0
    	IL_0016: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_001b: ldloca.s 0
    	IL_001d: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<valuetype C/'<M>d__2'>(!!0&)
    	IL_0022: ldloca.s 0
    	IL_0024: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0029: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
    	IL_002e: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "124");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<M>d__2'
    	extends [netstandard]System.Object
    	implements [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 '<>1__state'
    	.field public valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> '<>t__builder'
    	.field public class C '<>4__this'
    	.field private valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter '<>u__1'
    	// Methods
    	.method public hidebysig specialname rtspecialname 
    		instance void .ctor () cil managed 
    	{
    		// Method begins at RVA 0x2083
    		// Code size 8 (0x8)
    		.maxstack 8
    		IL_0000: ldarg.0
    		IL_0001: call instance void [netstandard]System.Object::.ctor()
    		IL_0006: nop
    		IL_0007: ret
    	} // end of method '<M>d__2'::.ctor
    	.method private final hidebysig newslot virtual 
    		instance void MoveNext () cil managed 
    	{
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::MoveNext()
    		// Method begins at RVA 0x20e0
    		// Code size 164 (0xa4)
    		.maxstack 3
    		.locals init (
    			[0] int32,
    			[1] int32,
    			[2] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter,
    			[3] valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable,
    			[4] class C/'<M>d__2',
    			[5] class [netstandard]System.Exception
    		)
    		IL_0000: ldarg.0
    		IL_0001: ldfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0006: stloc.0
    		.try
    		{
    			IL_0007: ldloc.0
    			IL_0008: brfalse.s IL_000c
    			IL_000a: br.s IL_000e
    			IL_000c: br.s IL_004b
    			IL_000e: nop
    			IL_000f: call valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable [netstandard]System.Threading.Tasks.Task::Yield()
    			IL_0014: stloc.3
    			IL_0015: ldloca.s 3
    			IL_0017: call instance valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter [netstandard]System.Runtime.CompilerServices.YieldAwaitable::GetAwaiter()
    			IL_001c: stloc.2
    			IL_001d: ldloca.s 2
    			IL_001f: call instance bool [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::get_IsCompleted()
    			IL_0024: brtrue.s IL_0067
    			IL_0026: ldarg.0
    			IL_0027: ldc.i4.0
    			IL_0028: dup
    			IL_0029: stloc.0
    			IL_002a: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_002f: ldarg.0
    			IL_0030: ldloc.2
    			IL_0031: stfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0036: ldarg.0
    			IL_0037: stloc.s 4
    			IL_0039: ldarg.0
    			IL_003a: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_003f: ldloca.s 2
    			IL_0041: ldloca.s 4
    			IL_0043: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::AwaitUnsafeOnCompleted<valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter, class C/'<M>d__2'>(!!0&, !!1&)
    			IL_0048: nop
    			IL_0049: leave.s IL_00a3
    			IL_004b: ldarg.0
    			IL_004c: ldfld valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0051: stloc.2
    			IL_0052: ldarg.0
    			IL_0053: ldflda valuetype [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter C/'<M>d__2'::'<>u__1'
    			IL_0058: initobj [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter
    			IL_005e: ldarg.0
    			IL_005f: ldc.i4.m1
    			IL_0060: dup
    			IL_0061: stloc.0
    			IL_0062: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_0067: ldloca.s 2
    			IL_0069: call instance void [netstandard]System.Runtime.CompilerServices.YieldAwaitable/YieldAwaiter::GetResult()
    			IL_006e: nop
    			IL_006f: ldc.i4.s 124
    			IL_0071: stloc.1
    			IL_0072: leave.s IL_008e
    		} // end .try
    		catch [netstandard]System.Exception
    		{
    			IL_0074: stloc.s 5
    			IL_0076: ldarg.0
    			IL_0077: ldc.i4.s -2
    			IL_0079: stfld int32 C/'<M>d__2'::'<>1__state'
    			IL_007e: ldarg.0
    			IL_007f: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    			IL_0084: ldloc.s 5
    			IL_0086: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetException(class [netstandard]System.Exception)
    			IL_008b: nop
    			IL_008c: leave.s IL_00a3
    		} // end handler
    		IL_008e: ldarg.0
    		IL_008f: ldc.i4.s -2
    		IL_0091: stfld int32 C/'<M>d__2'::'<>1__state'
    		IL_0096: ldarg.0
    		IL_0097: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    		IL_009c: ldloc.1
    		IL_009d: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::SetResult(!0)
    		IL_00a2: nop
    		IL_00a3: ret
    	} // end of method '<M>d__2'::MoveNext
    	.method private final hidebysig newslot virtual 
    		instance void SetStateMachine (
    			class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine stateMachine
    		) cil managed 
    	{
    		.custom instance void [netstandard]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = (
    			01 00 00 00
    		)
    		.override method instance void [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine::SetStateMachine(class [netstandard]System.Runtime.CompilerServices.IAsyncStateMachine)
    		// Method begins at RVA 0x21a0
    		// Code size 1 (0x1)
    		.maxstack 8
    		IL_0000: ret
    	} // end of method '<M>d__2'::SetStateMachine
    } // end of class <M>d__2
    // Fields
    .field private int32 Y
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x208c
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::Y
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: nop
    	IL_000e: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance class [netstandard]System.Threading.Tasks.Task`1<int32> M () cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.AsyncStateMachineAttribute::.ctor(class [netstandard]System.Type) = (
    		01 00 09 43 2b 3c 4d 3e 64 5f 5f 32 00 00
    	)
    	.custom instance void [netstandard]System.Diagnostics.DebuggerStepThroughAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x209c
    	// Code size 56 (0x38)
    	.maxstack 2
    	.locals init (
    		[0] class C/'<M>d__2'
    	)
    	IL_0000: newobj instance void C/'<M>d__2'::.ctor()
    	IL_0005: stloc.0
    	IL_0006: ldloc.0
    	IL_0007: call valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Create()
    	IL_000c: stfld valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0011: ldloc.0
    	IL_0012: ldarg.0
    	IL_0013: stfld class C C/'<M>d__2'::'<>4__this'
    	IL_0018: ldloc.0
    	IL_0019: ldc.i4.m1
    	IL_001a: stfld int32 C/'<M>d__2'::'<>1__state'
    	IL_001f: ldloc.0
    	IL_0020: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0025: ldloca.s 0
    	IL_0027: call instance void valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::Start<class C/'<M>d__2'>(!!0&)
    	IL_002c: ldloc.0
    	IL_002d: ldflda valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32> C/'<M>d__2'::'<>t__builder'
    	IL_0032: call instance class [netstandard]System.Threading.Tasks.Task`1<!0> valuetype [netstandard]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<int32>::get_Task()
    	IL_0037: ret
    } // end of method C::M
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        public void ParameterCapturing_173_InLocalFunction()
        {
            var source = """
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        System.Console.WriteLine(new C(123).M(124, 125));
    }
}

class C(int y)
{
    private int Y = y;

    public int M(int a, int b)
    {
        int local() => b;
        return local();
    }
}
""";
            var comp1 = CreateCompilation(source, options: TestOptions.ReleaseExe);

            var verifier1 = CompileAndVerify(comp1, expectedOutput: "125");

            verifier1.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass2_0'
    	extends [netstandard]System.ValueType
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 b
    } // end of class <>c__DisplayClass2_0
    // Fields
    .field private int32 Y
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x2086
    	// Code size 14 (0xe)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::Y
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M (
    		int32 a,
    		int32 b
    	) cil managed 
    {
    	// Method begins at RVA 0x2098
    	// Code size 16 (0x10)
    	.maxstack 2
    	.locals init (
    		[0] valuetype C/'<>c__DisplayClass2_0'
    	)
    	IL_0000: ldloca.s 0
    	IL_0002: ldarg.2
    	IL_0003: stfld int32 C/'<>c__DisplayClass2_0'::b
    	IL_0008: ldloca.s 0
    	IL_000a: call int32 C::'<M>g__local|2_0'(valuetype C/'<>c__DisplayClass2_0'&)
    	IL_000f: ret
    } // end of method C::M
    .method assembly hidebysig static 
    	int32 '<M>g__local|2_0' (
    		valuetype C/'<>c__DisplayClass2_0'& ''
    	) cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x20b4
    	// Code size 7 (0x7)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldfld int32 C/'<>c__DisplayClass2_0'::b
    	IL_0006: ret
    } // end of method C::'<M>g__local|2_0'
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));

            var comp2 = CreateCompilation(source, options: TestOptions.DebugExe);

            var verifier2 = CompileAndVerify(comp2, expectedOutput: "125");

            verifier2.VerifyTypeIL("C",
@"
.class private auto ansi beforefieldinit C
    extends [netstandard]System.Object
{
    // Nested Types
    .class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass2_0'
    	extends [netstandard]System.ValueType
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Fields
    	.field public int32 b
    } // end of class <>c__DisplayClass2_0
    // Fields
    .field private int32 Y
    // Methods
    .method public hidebysig specialname rtspecialname 
    	instance void .ctor (
    		int32 y
    	) cil managed 
    {
    	// Method begins at RVA 0x208b
    	// Code size 15 (0xf)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldarg.1
    	IL_0002: stfld int32 C::Y
    	IL_0007: ldarg.0
    	IL_0008: call instance void [netstandard]System.Object::.ctor()
    	IL_000d: nop
    	IL_000e: ret
    } // end of method C::.ctor
    .method public hidebysig 
    	instance int32 M (
    		int32 a,
    		int32 b
    	) cil managed 
    {
    	// Method begins at RVA 0x209c
    	// Code size 22 (0x16)
    	.maxstack 2
    	.locals init (
    		[0] valuetype C/'<>c__DisplayClass2_0',
    		[1] int32
    	)
    	IL_0000: ldloca.s 0
    	IL_0002: ldarg.2
    	IL_0003: stfld int32 C/'<>c__DisplayClass2_0'::b
    	IL_0008: nop
    	IL_0009: nop
    	IL_000a: ldloca.s 0
    	IL_000c: call int32 C::'<M>g__local|2_0'(valuetype C/'<>c__DisplayClass2_0'&)
    	IL_0011: stloc.1
    	IL_0012: br.s IL_0014
    	IL_0014: ldloc.1
    	IL_0015: ret
    } // end of method C::M
    .method assembly hidebysig static 
    	int32 '<M>g__local|2_0' (
    		valuetype C/'<>c__DisplayClass2_0'& ''
    	) cil managed 
    {
    	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
    		01 00 00 00
    	)
    	// Method begins at RVA 0x20be
    	// Code size 7 (0x7)
    	.maxstack 8
    	IL_0000: ldarg.0
    	IL_0001: ldfld int32 C/'<>c__DisplayClass2_0'::b
    	IL_0006: ret
    } // end of method C::'<M>g__local|2_0'
} // end of class C
".Replace("[netstandard]", RuntimeUtilities.IsCoreClrRuntime ? "[netstandard]" : "[mscorlib]"));
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71400")]
        public void ParameterCapturing_174_InInterfaceImplementation()
        {
            var source =
@"
using System;

interface I
{
    event EventHandler E;
}

class C1(int i) : I
{
    public event EventHandler E
    {
        add { Console.WriteLine(""C1"" + i++); }
        remove { }
    }
}

class C2(int i) : I
{
    public event EventHandler E
    {
        add { }
        remove { Console.WriteLine(""C2"" + i++); }
    }
}

class C3(int i) : I
{
    event EventHandler I.E
    {
        add { Console.WriteLine(""C3"" + i++); }
        remove { }
    }
}

class C4(int i) : I
{
    event EventHandler I.E
    {
        add { }
        remove { Console.WriteLine(""C4"" + i++); }
    }
}

class Program
{
    static void Main()
    {
        I c1 = new C1(123);
        c1.E += null;
        c1.E += null;

        I c2 = new C2(123);
        c2.E -= null;
        c2.E -= null;

        I c3 = new C3(123);
        c3.E += null;
        c3.E += null;

        I c4 = new C4(123);
        c4.E -= null;
        c4.E -= null;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"C1123
C1124
C2123
C2124
C3123
C3124
C4123
C4124").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71400")]
        public void ParameterCapturing_175_InInterfaceImplementation()
        {
            var source =
@"
using System;

interface I
{
    int P { get; set; }
}

class C1(int i) : I
{
    public int P
    {
        get { Console.WriteLine(""C1"" + i++); return 0; }
        set { }
    }
}

class C2(int i) : I
{
    public int P
    {
        get {  return 0; }
        set { Console.WriteLine(""C2"" + i++); }
    }
}

class C3(int i) : I
{
    int I.P
    {
        get { Console.WriteLine(""C3"" + i++);  return 0; }
        set { }
    }
}

class C4(int i) : I
{
    int I.P
    {
        get {  return 0; }
        set { Console.WriteLine(""C4"" + i++); }
    }
}

class Program
{
    static void Main()
    {
        I c1 = new C1(123);
        _ = c1.P;
        _ = c1.P;

        I c2 = new C2(123);
        c2.P = 0;
        c2.P = 0;

        I c3 = new C3(123);
        _ = c3.P;
        _ = c3.P;

        I c4 = new C4(123);
        c4.P = 0;
        c4.P = 0;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"C1123
C1124
C2123
C2124
C3123
C3124
C4123
C4124").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71400")]
        public void ParameterCapturing_176_InInterfaceImplementation()
        {
            var source =
@"
using System;

interface I
{
    void M();
}

class C1(int i) : I
{
    public void M() { Console.WriteLine(""C1"" + i++); }
}

class C3(int i) : I
{
    void I.M() { Console.WriteLine(""C3"" + i++); }
}

class Program
{
    static void Main()
    {
        I c1 = new C1(123);
        c1.M();
        c1.M();

        I c3 = new C3(123);
        c3.M();
        c3.M();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput:
@"C1123
C1124
C3123
C3124").VerifyDiagnostics();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/71400")]
        public void ParameterCapturing_177_InInterfaceImplementation()
        {
            var source =
@"
#nullable enable

using System;
using System.ComponentModel;

internal class MyClass(MyOtherClass otherClass, string key, int value) : INotifyPropertyChanged
{
    public string MyProperty { get; private set; } = GetValue(otherClass.MyProperty, key, value);
    private PropertyChangedEventHandler? _propertyChanged;


    private static string GetValue(string myProperty, string key, int value)
    {
        throw new NotImplementedException();
    }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            lock (this)
            {
                if (_propertyChanged is null)
                {
                    otherClass.SomethingChanged += OnSomethingChanged;
                }
                _propertyChanged += value;

            }
        }
        remove
        {
            lock (this)
            {
                if (_propertyChanged is null)
                {
                    return;
                }
                _propertyChanged -= value;
                if (_propertyChanged is null)
                {
                    otherClass.SomethingChanged -= OnSomethingChanged;
                }
            }
        }
    }

    private void OnSomethingChanged(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}

internal class MyOtherClass
{
    public event EventHandler? SomethingChanged;
    public string MyProperty { get; set; }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            CompileAndVerify(comp).VerifyDiagnostics(
                // (57,32): warning CS0067: The event 'MyOtherClass.SomethingChanged' is never used
                //     public event EventHandler? SomethingChanged;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "SomethingChanged").WithArguments("MyOtherClass.SomethingChanged").WithLocation(57, 32),
                // (58,19): warning CS8618: Non-nullable property 'MyProperty' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     public string MyProperty { get; set; }
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "MyProperty").WithArguments("property", "MyProperty").WithLocation(58, 19)
                );
        }

        [Fact]
        public void CycleDueToIndexerNameAttribute_01()
        {
            var source = @"
class C1 (int p1)
{
    [System.Runtime.CompilerServices.IndexerNameAttribute(nameof(p1))]
    int this[int x]
    {
        get => x;
        set {}
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 15)
                );

            Assert.Equal("p1", comp.GetTypeByMetadataName("C1").Indexers.Single().MetadataName);
        }

        [Fact]
        public void CycleDueToIndexerNameAttribute_02()
        {
            var source = @"
class C1 (int p1)
{
    [System.Runtime.CompilerServices.IndexerNameAttribute(nameof(p1))]
    int this[int x]
    {
        get => x;
        set {}
    }

    void M(int x)
    {
        x = 1;
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // (2,15): warning CS9113: Parameter 'p1' is unread.
                // class C1 (int p1)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 15)
                );

            Assert.Equal("p1", comp.GetTypeByMetadataName("C1").Indexers.Single().MetadataName);
        }

        [Fact]
        public void CycleDueToIndexerNameAttribute_03()
        {
            var source = @"
class C1 (int p1)
{
    [System.Runtime.CompilerServices.IndexerNameAttribute(nameof(p1))]
    int this[int x]
    {
        get => p1;
        set {}
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics();

            Assert.Equal("p1", comp.GetTypeByMetadataName("C1").Indexers.Single().MetadataName);
            Assert.Single(comp.GetTypeByMetadataName("C1").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69663")]
        public void Indexer_SymbolInfo()
        {
            var source1 = """
                C c = null;
                _ = c[2];
                """;
            var source2 = """
                class C(int p)
                {
                    public int this[int i] => p;
                }
                """;
            var comp = CreateCompilation(new[] { source1, source2 });
            var tree = comp.SyntaxTrees[0];
            var indexer = tree.GetRoot().DescendantNodes().OfType<ElementAccessExpressionSyntax>().Single();
            Assert.Equal("c[2]", indexer.ToString());
            var model = comp.GetSemanticModel(tree);
            model.GetDiagnostics().Verify();
            var info = model.GetSymbolInfo(indexer);
            AssertEx.Equal("System.Int32 C.this[System.Int32 i] { get; }", info.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void IllegalCapturingInStruct_01()
        {
            var source = @"
struct C1(int p1, int p2, int p3, int p4)
{
    public void M1() { local(); int local() { return p1; } }
    public void M2() { var d = () => { return p2; }; d(); }
    System.Func<int> F1 = () => p3;
    public void M3() { local(); string local() { return nameof(p4); } }
    public void M4() { var d = () => { return nameof(p4); }; d(); }
    int F2 = p4;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (4,54): error CS9111: Anonymous methods, lambda expressions, query expressions, and local functions inside an instance member of a struct cannot access primary constructor parameter
                //     public void M1() { local(); int local() { return p1; } }
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember, "p1").WithLocation(4, 54),
                // (5,47): error CS9111: Anonymous methods, lambda expressions, query expressions, and local functions inside an instance member of a struct cannot access primary constructor parameter
                //     public void M2() { var d = () => { return p2; }; d(); }
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember, "p2").WithLocation(5, 47)
                );
        }

        [Fact]
        public void IllegalCapturingInStruct_02()
        {
            var source = @"
struct C1(int p1)
{
    System.Func<int> x = () => p1;
    System.Func<string> y = () => nameof(p1);

    public int M1() => p1;
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (4,32): error CS9112: Anonymous methods, lambda expressions, query expressions, and local functions inside a struct cannot access primary constructor parameter also used inside an instance member
                //     System.Func<int> x = () => p1;
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured, "p1").WithLocation(4, 32)
                );
        }

        public static IEnumerable<object[]> IllegalCapturingDueToRefness_01_MemberData()
        {
            var data1 = new (string tag, TestFlags flags, string code, object err)[]
                {
                    ("0001", BadReference, "(ref int p1) { void M1() { p1 = 1; } }", ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef),
                    ("0002", BadReference, "(ref int p1) { void M1() { local(); void local() { p1 = 1; } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0003", BadReference, "(ref int p1) { void M1() { local1(); void local1() { local2(); void local2() { p1 = 1; } } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0004", BadReference, "(in int p1) { void M1() { p1.ToString(); } }", ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef),
                    ("0005", BadReference, "(in int p1) { void M1() { local(); void local() { p1.ToString(); } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0006", BadReference, "(in int p1) { void M1() { local1(); void local1() { local2(); void local2() { p1.ToString(); } } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0007", BadReference, "(out int p1) { int x = p1 = 0; void M1() { p1 = 1; } }", ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef),
                    ("0008", BadReference, "(out int p1) { int x = p1 = 0; void M1() { local(); void local() { p1 = 1; } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0009", BadReference, "(out int p1) { int x = p1 = 0; void M1() { local1(); void local1() { local2(); void local2() { p1 = 1; } } } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0010", BadReference, "(S1 p1) { void M1() { p1.M(); } } ref struct S1 { public void M(){} }", ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike),
                    ("0011", BadReference, "(S1 p1) { void M1() { local(); void local() { p1.M(); } } } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),
                    ("0012", BadReference, "(S1 p1) { void M1() { local1(); void local1() { local2(); void local2() { p1.M(); } } } } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),
                    ("0013", BadReference, "(ref int p1) { void M1() { _ = (System.Action)(() => { p1 = 1; }); } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0014", BadReference, "(in int p1) { void M1() { _ = (System.Action)(() => { p1.ToString(); }); } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0015", BadReference, "(out int p1) { int x = p1 = 0; void M1() { _ = (System.Action)(() => { p1 = 1; }); } }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0016", BadReference, "(S1 p1) { void M1() { _ = (System.Action)(() => { p1.M(); }); } } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),

                    ("0101", Success, "(ref int p1) { void M1() { nameof(p1).ToString(); } }", null),
                    ("0102", Success, "(ref int p1) { void M1() { local(); void local() { nameof(p1).ToString(); } } }", null),
                    ("0103", Success, "(ref int p1) { void M1() { local1(); void local1() { local2(); void local2() {nameof(p1).ToString(); } } } }", null),
                    ("0104", Success, "(in int p1) { void M1() { nameof(p1).ToString(); } }", null),
                    ("0105", Success, "(in int p1) { void M1() { local(); void local() { nameof(p1).ToString(); } } }", null),
                    ("0106", Success, "(in int p1) { void M1() { local1(); void local1() { local2(); void local2() { nameof(p1).ToString(); } } } }", null),
                    ("0107", Success, "(out int p1) { int x = p1 = 0; void M1() { nameof(p1).ToString(); } }", null),
                    ("0108", Success, "(out int p1) { int x = p1 = 0; void M1() { local(); void local() { nameof(p1).ToString(); } } }", null),
                    ("0109", Success, "(out int p1) { int x = p1 = 0; void M1() { local1(); void local1() { local2(); void local2() { nameof(p1).ToString(); } } } }", null),
                    ("0110", Success, "(S1 p1) { void M1() { nameof(p1).ToString(); } } ref struct S1 { public void M(){} }", null),
                    ("0111", Success, "(S1 p1) { void M1() { local(); void local() { nameof(p1).ToString(); } } } ref struct S1{ public void M(){} }", null),
                    ("0112", Success, "(S1 p1) { void M1() { local1(); void local1() { local2(); void local2() { nameof(p1).ToString(); } } } } ref struct S1{ public void M(){} }", null),

                    ("0201", BadReference, "(ref int p1) { System.Action F = () => p1 = 0; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0202", BadReference, "(ref int p1) { System.Action P { get; } = () => p1 = 0; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0203", BadReference, "(ref int p1) { public event System.Action E = () => p1 = 0; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0204", BadReference, "(in int p1) { System.Action F = () => p1.ToString(); }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0205", BadReference, "(in int p1) { System.Action P { get; } = () => p1.ToString(); }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0206", BadReference, "(in int p1) { public event System.Action E = () => p1.ToString(); }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0207", BadReference, "(out int p1) { System.Action F = (p1 = 0) == 0 ? () => p1 = 0 : null; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0208", BadReference, "(out int p1) { System.Action P { get; } = (p1 = 0) == 0 ? () => p1 = 0 : null; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0209", BadReference, "(out int p1) { public event System.Action E = (p1 = 0) == 0 ? () => p1 = 0 : null; }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("0210", BadReference, "(S1 p1) { System.Action F = () => p1.ToString(); } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),
                    ("0211", BadReference, "(S1 p1) { System.Action P { get; } = () => p1.ToString(); } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),
                    ("0212", BadReference, "(S1 p1) { public event System.Action E = () => p1.ToString(); } ref struct S1{ public void M(){} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),

                    ("0301", Success, "(ref int p1) { System.Action F = () => nameof(p1).ToString(); }", null),
                    ("0302", Success, "(ref int p1) { System.Action P { get; } = () => nameof(p1).ToString(); }", null),
                    ("0303", Success, "(ref int p1) { public event System.Action E = () => nameof(p1).ToString(); }", null),
                    ("0304", Success, "(in int p1) { System.Action F = () => nameof(p1).ToString(); }", null),
                    ("0305", Success, "(in int p1) { System.Action P { get; } = () => nameof(p1).ToString(); }", null),
                    ("0306", Success, "(in int p1) { public event System.Action E = () => nameof(p1).ToString(); }", null),
                    ("0307", Success, "(out int p1) { System.Action F = (p1 = 0) == 0 ? () => nameof(p1).ToString() : null; }", null),
                    ("0308", Success, "(out int p1) { System.Action P { get; } = (p1 = 0) == 0 ? () => nameof(p1).ToString() : null; }", null),
                    ("0309", Success, "(out int p1) { public event System.Action E = (p1 = 0) == 0 ? () => nameof(p1).ToString() : null; }", null),
                    ("0310", Success, "(S1 p1) { System.Action F = () => nameof(p1).ToString(); } ref struct S1{ public void M(){} }", null),
                    ("0311", Success, "(S1 p1) { System.Action P { get; } = () => nameof(p1).ToString(); } ref struct S1{ public void M(){} }", null),
                    ("0312", Success, "(S1 p1) { public event System.Action E = () => nameof(p1).ToString(); } ref struct S1{ public void M(){} }", null),
                };

            var data2 = new (string tag, TestFlags flags, string code, object err)[]
                {
                    ("1001", BadReference, "(ref int p1) : Base(() => p1 = 0); class Base { public Base(System.Action x) {} }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("1002", BadReference, "(in int p1) : Base(() => p1.ToString()); class Base { public Base(System.Action x) {} }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("1003", BadReference, "(out int p1) : Base(p1 = 0, () => p1 = 0); class Base { public Base(int y, System.Action x) {} }", ErrorCode.ERR_AnonDelegateCantUse),
                    ("1004", BadReference, "(S1 p1) : Base(() => p1.ToString()); ref struct S1{ public void M(){} } class Base { public Base(System.Action x) {} }", ErrorCode.ERR_AnonDelegateCantUseRefLike),

                    ("1011", Success, "(ref int p1) : Base(() => nameof(p1).ToString()); class Base { public Base(System.Action x) {} }", null),
                    ("1012", Success, "(in int p1) : Base(() => nameof(p1).ToString()); class Base { public Base(System.Action x) {} }", null),
                    ("1013", Success, "(out int p1) : Base(p1 = 0, () => nameof(p1).ToString()); class Base { public Base(int y, System.Action x) {} }", null),
                    ("1014", Success, "(S1 p1) : Base(() => nameof(p1).ToString()); ref struct S1{ public void M(){} } class Base { public Base(System.Action x) {} }", null),
                };

            foreach (var keyword in new[] { "class", "struct", "ref struct" })
            {
                foreach (var d in data1)
                {
                    yield return new object[] { keyword, d.tag, d.flags, d.code, d.err };
                }
            }

            foreach (var d in data2)
            {
                yield return new object[] { "class", d.tag, d.flags, d.code, d.err };
            }
        }

        [Theory]
        [MemberData(nameof(IllegalCapturingDueToRefness_01_MemberData))]
        public void IllegalCapturingDueToRefness_01(string keyword, string tag, TestFlags flags, string code, object err)
        {
            _ = tag;

            int i = code.LastIndexOf("p1");
            var source = @"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'p1' is unread.

" + keyword + " C1" + code.Substring(0, i) + @"
#line 2000
p1
" + code.Substring(i + 2);

            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            if (flags == TestFlags.BadReference)
            {
                switch ((ErrorCode)err)
                {
                    case ErrorCode.ERR_AnonDelegateCantUse:
                        comp.VerifyEmitDiagnostics(
                            // (2000,1): error CS1628: Cannot use ref, out, or in parameter 'p1' inside an anonymous method, lambda expression, query expression, or local function
                            // p1
                            Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p1").WithArguments("p1").WithLocation(2000, 1)
                            );
                        break;

                    case ErrorCode.ERR_AnonDelegateCantUseRefLike:
                        comp.VerifyEmitDiagnostics(
                            // (2000,1): error CS9108: Cannot use parameter 'p1' that has ref-like type inside an anonymous method, lambda expression, query expression, or local function
                            // p1
                            Diagnostic(ErrorCode.ERR_AnonDelegateCantUseRefLike, "p1").WithArguments("p1").WithLocation(2000, 1)
                            );
                        break;

                    case ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef:
                        comp.VerifyEmitDiagnostics(
                            // (2000,1): error CS9109: Cannot use ref, out, or in primary constructor parameter 'p1' inside an instance member
                            // p1
                            Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef, "p1").WithArguments("p1").WithLocation(2000, 1)
                            );
                        break;

                    case ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike:
                        comp.VerifyEmitDiagnostics(
                            // (2000,1): error CS9110: Cannot use primary constructor parameter 'p1' that has ref-like type inside an instance member
                            // p1
                            Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, "p1").WithArguments("p1").WithLocation(2000, 1)
                            );
                        break;

                    default:
                        Assert.True(false);
                        break;
                }
            }
            else
            {
                Assert.Null(err);
                comp.VerifyEmitDiagnostics();
            }
        }

        [Fact]
        public void IllegalCapturingDueToRefness_02()
        {
            var source = @"#pragma warning disable CS0649 // Field 'R1.F1' is never assigned to, and will always have its default value 0
ref struct R1
{
    public int F1;
    public ref int F2;
}
ref struct R2(R1 r)
{
    int M1() => r.F1;
    ref int M2() => ref r.F2;
}
";
            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (9,17): error CS9110: Cannot use primary constructor parameter 'r' that has ref-like type inside an instance member
                //     int M1() => r.F1;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, "r").WithArguments("r").WithLocation(9, 17),
                // (10,25): error CS9110: Cannot use primary constructor parameter 'r' that has ref-like type inside an instance member
                //     ref int M2() => ref r.F2;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike, "r").WithArguments("r").WithLocation(10, 25)
                );
        }

        [Fact]
        public void IllegalCapturingInNestedStatic_01()
        {
            var source = @"
class C1(int p1, int p2, int p3, int p4) : Base(static () => p4) 
{
    public void M1() { local(); static int local() { return p1; } }
    public void M2() { var d = static () => { return p2; }; d(); }
    System.Func<int> F = static () => p3;
}

class C2(int p1, int p2, int p3, int p4) : Base(static () => nameof(p4).Length) 
{
    public void M1() { local(); static string local() { return nameof(p1); } }
    public void M2() { var d = static () => { return nameof(p2); }; d(); }
    System.Func<string> F = static () => nameof(p3);
}

class Base
{
    public Base(System.Func<int> x) {}
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics(
                // (2,62): error CS8820: A static anonymous function cannot contain a reference to 'p4'.
                // class C1(int p1, int p2, int p3, int p4) : Base(static () => p4) 
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "p4").WithArguments("p4").WithLocation(2, 62),
                // (4,61): error CS8421: A static local function cannot contain a reference to 'p1'.
                //     public void M1() { local(); static int local() { return p1; } }
                Diagnostic(ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable, "p1").WithArguments("p1").WithLocation(4, 61),
                // (5,54): error CS8820: A static anonymous function cannot contain a reference to 'p2'.
                //     public void M2() { var d = static () => { return p2; }; d(); }
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "p2").WithArguments("p2").WithLocation(5, 54),
                // (6,39): error CS8820: A static anonymous function cannot contain a reference to 'p3'.
                //     System.Func<int> F = static () => p3;
                Diagnostic(ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable, "p3").WithArguments("p3").WithLocation(6, 39),
                // (9,14): warning CS9113: Parameter 'p1' is unread.
                // class C2(int p1, int p2, int p3, int p4) : Base(static () => nameof(p4).Length) 
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(9, 14),
                // (9,22): warning CS9113: Parameter 'p2' is unread.
                // class C2(int p1, int p2, int p3, int p4) : Base(static () => nameof(p4).Length) 
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p2").WithArguments("p2").WithLocation(9, 22)
                );
        }

        [Fact]
        public void ParameterScope_TypeShadows()
        {
            var src = @"
class C(int X)
{
    void M()
    {
        X.ToString();
        X.M();
    }

    class X
    {
        public void M(){}
    }
}
";

            var comp = CreateCompilation(src);

            Assert.Empty(comp.GetTypeByMetadataName("C").InstanceConstructors.OfType<SynthesizedPrimaryConstructor>().Single().GetCapturedParameters());

            comp.VerifyEmitDiagnostics(
                // (2,13): warning CS9113: Parameter 'X' is unread.
                // class C(int X)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "X").WithArguments("X").WithLocation(2, 13),
                // (6,9): error CS0120: An object reference is required for the non-static field, method, or property 'object.ToString()'
                //         X.ToString();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "X.ToString").WithArguments("object.ToString()").WithLocation(6, 9),
                // (7,9): error CS0120: An object reference is required for the non-static field, method, or property 'C.X.M()'
                //         X.M();
                Diagnostic(ErrorCode.ERR_ObjectRequired, "X.M").WithArguments("C.X.M()").WithLocation(7, 9)
                );
        }

        [Fact]
        public void ConstructorCallsDefaultConstructor()
        {
            var source =
@"
#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'A' is unread. Did you forget to use it to initialize the property with that name?

struct S3(char A)
{
    public S3(object o) : this()
    { }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,27): error CS8982: A constructor declared in a 'struct' with parameter list must have a 'this' initializer that calls the primary constructor or an explicitly declared constructor.
                //     public S3(object o) : this()
                Diagnostic(ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor, "this").WithLocation(6, 27)
                );
        }

        [Fact]
        public void StructLayout_01()
        {
            string source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
struct S(int x, int y)
{
    int X = x;
    int Y => y;

    [FieldOffset(8)]
    int Z = 0;
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // We might want to adjust the warning depending on what we decide to do for
                // https://github.com/dotnet/csharplang/blob/main/proposals/primary-constructors.md#field-targeting-attributes-for-captured-primary-constructor-parameters.
                //
                // If we decide to support attributes for capture fields, consider testing
                //     ERR_MarshalUnmanagedTypeNotValidForFields
                //     ERR_StructOffsetOnBadStruct
                //     ERR_DoNotUseFixedBufferAttr

                // (5,21): error CS0625: 'S.<y>P': instance field in types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                // struct S(int x, int y)
                Diagnostic(ErrorCode.ERR_MissingStructOffset, "y").WithArguments("S.<y>P").WithLocation(5, 21),
                // (7,9): error CS0625: 'S.X': instance field in types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                //     int X = x;
                Diagnostic(ErrorCode.ERR_MissingStructOffset, "X").WithArguments("S.X").WithLocation(7, 9)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67162")]
        public void StructLayout_02()
        {
            string source1 = @"
public partial struct S(int x)
{
    int X => x;
}
";
            string source2 = @"
public partial struct S
{
    public int Y;
}
";
            verify1(source1, source2, validate2);
            verify1(source1 + source2, "", validate2);
            verify1(source2, source1, validate3);
            verify1(source2 + source1, "", validate3);

            void verify1(string source1, string source2, Action<ModuleSymbol> validator)
            {
                var comp = CreateCompilation(new[] { source1, source2 });
                CompileAndVerify(comp, symbolValidator: validator, sourceSymbolValidator: validator).VerifyDiagnostics(
                    // 0.cs(2,23): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'S'. To specify an ordering, all instance fields must be in the same declaration.
                    // public partial struct S(int x)
                    Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "S").WithArguments("S").WithLocation(2, 23)
                    );
            }

            void validate2(ModuleSymbol m)
            {
                var fields = m.GlobalNamespace.GetTypeMember("S").GetMembers().OfType<FieldSymbol>().ToArray();
                Assert.Equal(2, fields.Length);
                Assert.Equal("<x>P", fields[0].Name);
                Assert.Equal("Y", fields[1].Name);
            }

            void validate3(ModuleSymbol m)
            {
                var fields = m.GlobalNamespace.GetTypeMember("S").GetMembers().OfType<FieldSymbol>().ToArray();
                Assert.Equal(2, fields.Length);
                Assert.Equal("Y", fields[0].Name);
                Assert.Equal("<x>P", fields[1].Name);
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67162")]
        public void StructLayout_03()
        {
            string source1 = @"
public partial struct S(int x)
{
}
";
            string source2 = @"
public partial struct S
{
    int X = x;
}
";
            verify1(source1, source2, validate2);
            verify1(source1 + source2, "", validate2);
            verify1(source2, source1, validate2);
            verify1(source2 + source1, "", validate2);

            void verify1(string source1, string source2, Action<ModuleSymbol> validator)
            {
                var comp = CreateCompilation(new[] { source1, source2 });
                CompileAndVerify(comp, symbolValidator: validator, sourceSymbolValidator: validator).VerifyDiagnostics();
            }

            void validate2(ModuleSymbol m)
            {
                Assert.Equal(1, m.GlobalNamespace.GetTypeMember("S").GetMembers().OfType<FieldSymbol>().Count());
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67162")]
        public void StructLayout_04()
        {
            string source1 = @"
public partial struct S(int x)
{
    int X => x;
    public int Y;
}
";
            string source2 = @"
public partial struct S
{
}
";
            verify1(source1, source2, validate2);
            verify1(source1 + source2, "", validate2);
            verify1(source2, source1, validate2);
            verify1(source2 + source1, "", validate2);

            void verify1(string source1, string source2, Action<ModuleSymbol> validator)
            {
                var comp = CreateCompilation(new[] { source1, source2 });
                CompileAndVerify(comp, symbolValidator: validator, sourceSymbolValidator: validator).VerifyDiagnostics();
            }

            void validate2(ModuleSymbol m)
            {
                var fields = m.GlobalNamespace.GetTypeMember("S").GetMembers().OfType<FieldSymbol>().ToArray();
                Assert.Equal(2, fields.Length);
                Assert.Equal("<x>P", fields[0].Name);
                Assert.Equal("Y", fields[1].Name);
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67162")]
        public void StructLayout_05()
        {
            string source1 = @"
public partial struct S(int x)
{
    int X => x;
}
";
            string source2 = @"
public partial struct S
{
    public int Y;
}
";
            string source3 = @"
public partial struct S
{
    public int Z;
}
";
            verify1(source1, source2, source3);
            verify1(source1 + source2, source3, "");
            verify1(source1 + source2 + source3, "", "");

            verify1(source1, source3, source2);
            verify1(source1 + source3, source2, "");
            verify1(source1 + source3 + source2, "", "");

            verify1(source2, source1, source3);
            verify1(source2 + source1, source3, "");
            verify1(source2 + source1 + source3, "", "");

            verify1(source2, source3, source1);
            verify1(source2 + source3, source1, "");
            verify1(source2 + source3 + source1, "", "");

            verify1(source3, source1, source2);
            verify1(source3 + source1, source2, "");
            verify1(source3 + source1 + source2, "", "");

            verify1(source3, source2, source1);
            verify1(source3 + source2, source1, "");
            verify1(source3 + source2 + source1, "", "");

            void verify1(string source1, string source2, string source3)
            {
                var comp = CreateCompilation(new[] { source1, source2, source3 });
                comp.VerifyDiagnostics(
                    // 0.cs(2,23): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'S'. To specify an ordering, all instance fields must be in the same declaration.
                    // public partial struct S(int x)
                    Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "S").WithArguments("S").WithLocation(2, 23)
                    );
            }
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67099")]
        public void NullableWarningsForAssignment_01_NotCaptured()
        {
            var source = @"
#nullable enable
class C1(string p1, string p2)
{
    string? F1 = (p1 = null);
    string F2 = p1;

    string M1() => p2;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (5,24): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //     string? F1 = (p1 = null);
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(5, 24),
                // (6,17): warning CS8601: Possible null reference assignment.
                //     string F2 = p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p1").WithLocation(6, 17)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67099")]
        public void NullableWarningsForAssignment_02_Captured()
        {
            var source = @"
#pragma warning disable CS9124 // Parameter 'string p1' is captured into the state of the enclosing type and its value is also used to initialize a field, property, or event.
#nullable enable
class C1(string p1)
{
    string? F1 = (p1 = null);
    string F2 = p1;

    void M1()
    {
        p1 = null;
    }

    void M2()
    {
        p1.ToString();
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,24): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //     string? F1 = (p1 = null);
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(6, 24),
                // (7,17): warning CS8601: Possible null reference assignment.
                //     string F2 = p1;
                Diagnostic(ErrorCode.WRN_NullReferenceAssignment, "p1").WithLocation(7, 17),
                // (11,14): warning CS8625: Cannot convert null literal to non-nullable reference type.
                //         p1 = null;
                Diagnostic(ErrorCode.WRN_NullAsNonNullable, "null").WithLocation(11, 14)
                );
        }

        [Theory]
        [CombinatorialData]
        public void RestrictedType_01([CombinatorialValues("class", "struct")] string declaration)
        {
            var src1 = @"
" + declaration + @" C1
(System.ArgIterator a)
{
    void M()
    {
        _ = a;
    }
}

" + declaration + @" C2
(System.ArgIterator b)
{
    void M()
    {
        System.Action d = () => _ = b;
    }
}

" + declaration + @" C3
(System.ArgIterator c)
{
    System.Action d = () => _ = c;
}

#pragma warning disable CS" + UnreadParameterWarning() + @" // Parameter 'z' is unread.
" + declaration + @" C4(System.ArgIterator z)
{
}
";
            var comp = CreateCompilation(src1, targetFramework: TargetFramework.Mscorlib461Extended);
            comp.VerifyDiagnostics(
                // (7,13): error CS9136: Cannot use primary constructor parameter of type 'ArgIterator' inside an instance member
                //         _ = a;
                Diagnostic(ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefAny, "a").WithArguments("System.ArgIterator").WithLocation(7, 13),
                // (16,37): error CS4013: Instance of type 'ArgIterator' cannot be used inside a nested function, query expression, iterator block or async method
                //         System.Action d = () => _ = b;
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "b").WithArguments("System.ArgIterator").WithLocation(16, 37),
                // (23,33): error CS4013: Instance of type 'ArgIterator' cannot be used inside a nested function, query expression, iterator block or async method
                //     System.Action d = () => _ = c;
                Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "c").WithArguments("System.ArgIterator").WithLocation(23, 33)
                );
        }

        [Theory]
        [CombinatorialData]
        public void RestrictedType_02([CombinatorialValues("record", "record class", "record struct")] string keyword)
        {
            var src1 = @"
" + keyword + @" C1
(System.ArgIterator x)
{
}
";
            var comp = CreateCompilation(src1, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (3,2): error CS0610: Field or property cannot be of type 'ArgIterator'
                // (System.ArgIterator x)
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator").WithLocation(3, 2)
                );
        }

        [Fact]
        public void SemanticModel_GetDeclaredSymbols1()
        {
            var src1 = """
                using System;

                [method: Obsolete("")]
                class Point(int i)
                {
                    public int I { get; } = i;
                }
                """;
            var comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(tree);
            var typeDeclaration = tree.GetRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Single();

            var symbols = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration);
            Assert.Equal(2, symbols.Length);

            var namedType = symbols.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor = symbols.OfType<IMethodSymbol>().Single();

            Assert.Same(primaryConstructor, namedType.GetSymbol<SourceMemberContainerTypeSymbol>().PrimaryConstructor.GetPublicSymbol());
            Assert.Equal(1, primaryConstructor.GetAttributes().Length);
        }

        [Fact]
        public void SemanticModel_GetDeclaredSymbols2()
        {
            var src1 = """
                using System;

                [method: Obsolete("")]
                partial class Point(int i)
                {
                    public int I { get; } = i;
                }

                partial class Point
                {
                }
                """;
            var comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var typeDeclaration1 = root.ChildNodes().OfType<TypeDeclarationSyntax>().First();

            var symbols1 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration1);
            Assert.Equal(2, symbols1.Length);

            var namedType1 = symbols1.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor1 = symbols1.OfType<IMethodSymbol>().Single();

            Assert.Same(primaryConstructor1, namedType1.GetSymbol<SourceMemberContainerTypeSymbol>().PrimaryConstructor.GetPublicSymbol());
            Assert.Equal(1, primaryConstructor1.GetAttributes().Length);

            var typeDeclaration2 = root.ChildNodes().OfType<TypeDeclarationSyntax>().Last();
            var symbols2 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration2);
            Assert.Equal(1, symbols2.Length);

            var namedType2 = symbols2.OfType<INamedTypeSymbol>().Single();
            Assert.Equal(namedType1, namedType2);
        }

        [Fact]
        public void SemanticModel_GetDeclaredSymbols3()
        {
            var src1 = """
                using System;

                [method: Obsolete("")]
                partial class Point(int i)
                {
                    public int I { get; } = i;
                }

                partial class Point(int i)
                {
                }
                """;
            var comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,20): error CS8863: Only a single partial type declaration may have a parameter list
                // partial class Point(int i)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int i)").WithLocation(9, 20));

            var tree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var typeDeclaration1 = root.ChildNodes().OfType<TypeDeclarationSyntax>().First();

            var symbols1 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration1);
            Assert.Equal(2, symbols1.Length);

            var namedType1 = symbols1.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor1 = symbols1.OfType<IMethodSymbol>().Single();

            Assert.Same(primaryConstructor1, namedType1.GetSymbol<SourceMemberContainerTypeSymbol>().PrimaryConstructor.GetPublicSymbol());
            Assert.Equal(1, primaryConstructor1.GetAttributes().Length);

            var typeDeclaration2 = root.ChildNodes().OfType<TypeDeclarationSyntax>().Last();
            var symbols2 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration2);
            Assert.Equal(1, symbols2.Length);

            var namedType2 = symbols2.OfType<INamedTypeSymbol>().Single();
            Assert.Equal(namedType1, namedType2);
        }

        [Fact]
        public void SemanticModel_GetDeclaredSymbols4()
        {
            var src1 = """
                using System;

                [method: Obsolete("")]
                partial class Point
                {
                    public int I { get; } = i;
                }

                partial class Point(int i)
                {
                }
                """;
            var comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
                // (3,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: Obsolete("")]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(3, 2));

            var tree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var typeDeclaration1 = root.ChildNodes().OfType<TypeDeclarationSyntax>().First();

            var symbols1 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration1);
            Assert.Equal(1, symbols1.Length);

            var namedType1 = symbols1.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor1 = namedType1.GetSymbol<SourceMemberContainerTypeSymbol>().PrimaryConstructor.GetPublicSymbol();
            Assert.Empty(primaryConstructor1.GetAttributes());

            var typeDeclaration2 = root.ChildNodes().OfType<TypeDeclarationSyntax>().Last();
            var symbols2 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration2);
            Assert.Equal(2, symbols2.Length);

            var namedType2 = symbols2.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor2 = symbols2.OfType<IMethodSymbol>().Single();
            Assert.Equal(namedType1, namedType2);
            Assert.Equal(primaryConstructor1, primaryConstructor2);
        }

        [Fact]
        public void SemanticModel_GetDeclaredSymbols5()
        {
            var src1 = """
                using System;

                partial class Point(int i)
                {
                    public int I { get; } = i;
                }
                
                [method: Obsolete("")]
                partial class Point(int i)
                {
                }
                """;
            var comp = CreateCompilation(src1, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1),
                // (8,2): warning CS0657: 'method' is not a valid attribute location for this declaration. Valid attribute locations for this declaration are 'type'. All attributes in this block will be ignored.
                // [method: Obsolete("")]
                Diagnostic(ErrorCode.WRN_AttributeLocationOnBadDeclaration, "method").WithArguments("method", "type").WithLocation(8, 2),
                // (9,20): error CS8863: Only a single partial type declaration may have a parameter list
                // partial class Point(int i)
                Diagnostic(ErrorCode.ERR_MultipleRecordParameterLists, "(int i)").WithLocation(9, 20));

            var tree = comp.SyntaxTrees.Single();
            var semanticModel = comp.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var typeDeclaration1 = root.ChildNodes().OfType<TypeDeclarationSyntax>().First();

            var symbols1 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration1);
            Assert.Equal(2, symbols1.Length);

            var namedType1 = symbols1.OfType<INamedTypeSymbol>().Single();
            var primaryConstructor1 = symbols1.OfType<IMethodSymbol>().Single();

            Assert.Same(primaryConstructor1, namedType1.GetSymbol<SourceMemberContainerTypeSymbol>().PrimaryConstructor.GetPublicSymbol());
            Assert.Equal(0, primaryConstructor1.GetAttributes().Length);

            var typeDeclaration2 = root.ChildNodes().OfType<TypeDeclarationSyntax>().Last();
            var symbols2 = semanticModel.GetDeclaredSymbolsForNode(typeDeclaration2);
            Assert.Equal(1, symbols2.Length);

            var namedType2 = symbols2.OfType<INamedTypeSymbol>().Single();
            Assert.Equal(namedType1, namedType2);
        }

        [Fact]
        public void ShadowedByMemberFromBase_01_NoArgumentList()
        {
            var source = @"
class Base
{
    protected string p1 = """";
}

class C1(int p1) : Base
{
    void M()
    {
        local();

        void local()
        {
            string a = p1; 
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (15,24): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //             string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(15, 24)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_02_EmptyArgumentList()
        {
            var source = @"
class Base
{
    protected string p1 = """";
}

class C1(int p1) : Base()
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_03_WithArgument()
        {
            var source = @"
class Base(int p1)
{
    protected string p1 = p1.ToString();
}

class C1(int p1, int p2) : Base(p2)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_04_WithArgument()
        {
            var source = @"
class Base(int p1)
{
    protected string p1 = p1.ToString();
}

class C1(int p1) : Base(p1)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ShadowedByMemberFromBase_05_WithIdentityConvertedArgument()
        {
            var source = @"
class Base(int p1)
{
    protected string p1 = p1.ToString();
}

class C1(int p1) : Base((int)p1)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ShadowedByMemberFromBase_06_WithNonIdentityConvertedArgument()
        {
            var source = @"
class Base(long p1)
{
    protected string p1 = p1.ToString();
}

class C1(int p1) : Base(p1)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_07_NotFirstArgument()
        {
            var source = @"
class Base(int p1, int p2)
{
    protected string p1 = (p1+p2).ToString();
}

class C1(int p1) : Base(0, p1)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ShadowedByMemberFromBase_08_WithParamsArgument()
        {
            var source = @"
class Base(params int[] p1)
{
    protected string p1 = p1.ToString();
}

class C1(int p1) : Base(p1)
{
    void M()
    {
        string a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_09_ColorColorResolvingToADifferentType()
        {
            var source = @"
class Base
{
    protected Type1 Type1 = null;
}

class C1(int Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}

class Type1
{
    public static string M() => null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'Type1' is unread.
                // class C1(int Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int Type1' is shadowed by a member from base.
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "Type1").WithArguments("int Type1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_10_ColorColorResolvingToTheSameType()
        {
            var source = @"
class Base
{
    protected Type1 Type1 = null;
}

class C1(Type1 Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}

class Type1
{
    public static string M() => null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,16): warning CS9113: Parameter 'Type1' is unread.
                // class C1(Type1 Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(7, 16)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_11_ColorColorResolvingToAValue()
        {
            var source = @"
class Base
{
    protected Type1 Type1 = null;
}

class C1(int Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}

class Type1
{
    public string M() => null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'Type1' is unread.
                // class C1(int Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int Type1' is shadowed by a member from base.
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "Type1").WithArguments("int Type1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_12_ColorColorResolvingToAValue()
        {
            var source = @"
class Base
{
    protected Type1 Type1 = null;
}

class C1(Type1 Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}

class Type1
{
    public string M() => null;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,16): warning CS9113: Parameter 'Type1' is unread.
                // class C1(Type1 Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(7, 16),
                // (11,20): warning CS9179: Primary constructor parameter 'Type1 Type1' is shadowed by a member from base.
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "Type1").WithArguments("Type1 Type1").WithLocation(11, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_13_TypeVsParameter()
        {
            var source = @"
class Base
{
    public class Type1
    {
        public static string M() => null;
    }
}

class C1(Base.Type1 Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (10,21): warning CS9113: Parameter 'Type1' is unread.
                // class C1(Base.Type1 Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(10, 21),
                // (14,20): warning CS9179: Primary constructor parameter 'Base.Type1 Type1' is shadowed by a member from base.
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "Type1").WithArguments("Base.Type1 Type1").WithLocation(14, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_14_TypeVsParameter()
        {
            var source = @"
class Base
{
    public class Type1
    {
        public string M() => null;
    }
}

class C1(Base.Type1 Type1) : Base
{
    void M()
    {
        string a = Type1.M(); 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (10,21): warning CS9113: Parameter 'Type1' is unread.
                // class C1(Base.Type1 Type1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Type1").WithArguments("Type1").WithLocation(10, 21),
                // (14,20): warning CS9179: Primary constructor parameter 'Base.Type1 Type1' is shadowed by a member from base.
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "Type1").WithArguments("Base.Type1 Type1").WithLocation(14, 20),
                // (14,20): error CS0120: An object reference is required for the non-static field, method, or property 'Base.Type1.M()'
                //         string a = Type1.M(); 
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Type1.M").WithArguments("Base.Type1.M()").WithLocation(14, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_15_MethodVsParameter()
        {
            var source = @"
class Base
{
    protected string p1() => """";
}

class C1(int p1) : Base
{
    void M()
    {
        var a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,17): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         var a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 17)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_16_MethodVsParameter()
        {
            var source = @"
class Base
{
}

class C1(int p1) : Base
{
    void M()
    {
        var a = p1; 
    }

    protected string p1() => """";
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (6,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(6, 14)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_17_MethodVsParameter_ShadowedByMemberFromTheSameType()
        {
            var source = @"
class Base
{
    protected string p1(int x) => """";
}

class C1(int p1) : Base
{
    void M()
    {
        var a = p1(1); 
    }

    protected string p1() => """";
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_18_MethodVsParameter()
        {
            var source = @"
class Base
{
    protected string p1() => """";
}

class C1(int p1) : Base
{
    void M()
    {
        int a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,17): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         int a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 17),
                // (11,17): error CS0428: Cannot convert method group 'p1' to non-delegate type 'int'. Did you intend to invoke the method?
                //         int a = p1; 
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "p1").WithArguments("p1", "int").WithLocation(11, 17)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_19_MultipleReferences()
        {
            var source = @"
class Base
{
    protected string p1 = """";
}

class C1(int p1) : Base
{
    void M1()
    {
        string a = p1; 
    }

    void M2()
    {
        string b = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20),
                // (16,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string b = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(16, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_20_NoReferences()
        {
            var source = @"
class Base
{
    protected string p1 = """";
}

class C1(int p1) : Base
{
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_21_InStaticMethod()
        {
            var source = @"
class Base
{
    protected static string p1 = """";
}

class C1(int p1) : Base
{
    static void M1()
    {
        string a = p1; 
    }
}

class C2(string p2)
{
    static void M1()
    {
        string a = p2; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (15,17): warning CS9113: Parameter 'p2' is unread.
                // class C2(string p2)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p2").WithArguments("p2").WithLocation(15, 17),
                // (19,20): error CS9105: Cannot use primary constructor parameter 'string p2' in this context.
                //         string a = p2; 
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p2").WithArguments("string p2").WithLocation(19, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_22_InNestedType()
        {
            var source = @"
class Base
{
    protected static string p1 = """";
}

class C1(int p1) : Base
{
    class Nested
    {
        void M()
        {
            string a = p1; 
        }
    }
}

class C2(string p2)
{
    class Nested
    {
        void M()
        {
            string a = p2; 
        }
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (18,17): warning CS9113: Parameter 'p2' is unread.
                // class C2(string p2)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p2").WithArguments("p2").WithLocation(18, 17),
                // (24,24): error CS9105: Cannot use primary constructor parameter 'string p2' in this context.
                //             string a = p2; 
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p2").WithArguments("string p2").WithLocation(24, 24)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_23_InOtherConstructor()
        {
            var source = @"
class Base
{
    protected string p1 = """";
}

class C1(int p1) : Base
{
    C1() : this(1)
    {
        string a = p1; 
    }
}

class C2(string p2)
{
    C2() : this(""1"")
    {
        string a = p2; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (7,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(7, 14),
                // (11,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(11, 20),
                // (19,20): error CS9105: Cannot use primary constructor parameter 'string p2' in this context.
                //         string a = p2; 
                Diagnostic(ErrorCode.ERR_InvalidPrimaryConstructorParameterReference, "p2").WithArguments("string p2").WithLocation(19, 20)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void ShadowedByMemberFromBase_24_OrderOfDeclarations()
        {
            var source1 = @"
partial class C1(int p1) : Base("""");
";
            var source2 = @"
partial class C1
{
    void M()
    {
        string a = p1; 
    }
}
";
            var source3 = @"
class Base(string p1)
{
    protected string p1 = p1;
}
";
            var comp = CreateCompilation(source1 + source2 + source3, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,22): warning CS9113: Parameter 'p1' is unread.
                // partial class C1(int p1) : Base;
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 22),
                // (8,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(8, 20)
                );

            comp = CreateCompilation(source2 + source1 + source3, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (6,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(6, 20),
                // (10,22): warning CS9113: Parameter 'p1' is unread.
                // partial class C1(int p1) : Base;
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(10, 22)
                );
        }

        [Fact]
        public void ShadowedByMemberFromBase_25_OrderOfBinding()
        {
            var source1 = @"
partial class C1(int p1) : Base("""");
";
            var source2 = @"
partial class C1
{
    void M()
    {
        string a = p1; 
    }
}
";
            var source3 = @"
class Base(string p1)
{
    protected string p1 = p1;
}
";
            var comp = CreateCompilation(new[] { source1, source2, source3 }, options: TestOptions.ReleaseDll);
            var tree = comp.SyntaxTrees[1];
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);

            model.GetDiagnostics().Verify(
                // 1.cs(6,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(6, 20)
                );

            comp.VerifyDiagnostics(
                // 0.cs(2,22): warning CS9113: Parameter 'p1' is unread.
                // partial class C1(int p1) : Base;
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(2, 22),
                // 1.cs(6,20): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         string a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(6, 20)
                );
        }

        [Fact]
        public void ShadowedByMemberFromBase_26_PropertyVsParameterInIndirectBase()
        {
            var source = @"
class Base0
{
    protected string p1 => """";
}

class Base1 : Base0
{
}

class C1(int p1) : Base1
{
    void M()
    {
        var a = p1; 
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);

            var expected = new[] {
                // (11,14): warning CS9113: Parameter 'p1' is unread.
                // class C1(int p1) : Base1
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "p1").WithArguments("p1").WithLocation(11, 14),
                // (15,17): warning CS9179: Primary constructor parameter 'int p1' is shadowed by a member from base.
                //         var a = p1; 
                Diagnostic(ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase, "p1").WithArguments("int p1").WithLocation(15, 17)
                };

            comp.VerifyDiagnostics(expected);
            comp.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67371")]
        public void AssignToRefField_01()
        {
            var source = @"
ref struct S1
{
    public ref int R1;

    public S1(ref int x)
    {
        R1 = ref x;
    }
}

ref struct S2
{
    public ref int R2;

    public S2([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int x)
    {
        R2 = ref x;
    }
}

ref struct S3(ref int x)
{
    public ref int R3 = ref x;
}

ref struct S4([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int x)
{
    public ref int R4 = ref x;
}

ref struct S5
{
    public ref int R5;

    public S5(scoped ref int x)
    {
        R5 = ref x;
    }
}

ref struct S6(scoped ref int x)
{
    public ref int R6 = ref x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (38,9): error CS8374: Cannot ref-assign 'x' to 'R5' because 'x' has a narrower escape scope than 'R5'.
                //         R5 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "R5 = ref x").WithArguments("R5", "x").WithLocation(38, 9),
                // (44,25): error CS8374: Cannot ref-assign 'x' to 'R6' because 'x' has a narrower escape scope than 'R6'.
                //     public ref int R6 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "ref x").WithArguments("R6", "x").WithLocation(44, 25)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67371")]
        public void AssignToRefField_02()
        {
            var source = @"
ref struct S1
{
    public ref readonly int R1;

    public S1(ref int x)
    {
        R1 = ref x;
    }
}

ref struct S2
{
    public ref readonly int R2;

    public S2([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int x)
    {
        R2 = ref x;
    }
}

ref struct S3(ref int x)
{
    public ref readonly int R3 = ref x;
}

ref struct S4([System.Diagnostics.CodeAnalysis.UnscopedRef] ref int x)
{
    public ref readonly int R4 = ref x;
}

ref struct S5
{
    public ref readonly int R5;

    public S5(scoped ref int x)
    {
        R5 = ref x;
    }
}

ref struct S6(scoped ref int x)
{
    public ref readonly int R6 = ref x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (38,9): error CS8374: Cannot ref-assign 'x' to 'R5' because 'x' has a narrower escape scope than 'R5'.
                //         R5 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "R5 = ref x").WithArguments("R5", "x").WithLocation(38, 9),
                // (44,34): error CS8374: Cannot ref-assign 'x' to 'R6' because 'x' has a narrower escape scope than 'R6'.
                //     public ref readonly int R6 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "ref x").WithArguments("R6", "x").WithLocation(44, 34)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67371")]
        public void AssignToRefField_03()
        {
            var source = @"
ref struct S1
{
    public ref readonly int R1;

    public S1(in int x)
    {
        R1 = ref x;
    }
}

ref struct S2
{
    public ref readonly int R2;

    public S2([System.Diagnostics.CodeAnalysis.UnscopedRef] in int x)
    {
        R2 = ref x;
    }
}

ref struct S3(in int x)
{
    public ref readonly int R3 = ref x;
}

ref struct S4([System.Diagnostics.CodeAnalysis.UnscopedRef] in int x)
{
    public ref readonly int R4 = ref x;
}

ref struct S5
{
    public ref readonly int R5;

    public S5(scoped in int x)
    {
        R5 = ref x;
    }
}

ref struct S6(scoped in int x)
{
    public ref readonly int R6 = ref x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (38,9): error CS8374: Cannot ref-assign 'x' to 'R5' because 'x' has a narrower escape scope than 'R5'.
                //         R5 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "R5 = ref x").WithArguments("R5", "x").WithLocation(38, 9),
                // (44,34): error CS8374: Cannot ref-assign 'x' to 'R6' because 'x' has a narrower escape scope than 'R6'.
                //     public ref readonly int R6 = ref x;
                Diagnostic(ErrorCode.ERR_RefAssignNarrower, "ref x").WithArguments("R6", "x").WithLocation(44, 34)
                );
        }

        [Fact]
        public void GenericParameterType()
        {
            var source =
@"
struct S1<ST>(ST x)
{
    public ST X => x;
}

class C1<CT>(CT x)
{
    public CT X => x;
}


class Program
{
    static void Main()
    {
        System.Console.Write(new S1<int>(123).X);
        System.Console.Write(new C1<string>(""C1"").X);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"123C1").VerifyDiagnostics();
        }

        [Fact]
        public void ParameterIsPointer_01()
        {
            var source =
@"
unsafe struct S1(int* x)
{
    public int X => *x;
}

unsafe class C1(int* x)
{
    public int X => *x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            CompileAndVerify(comp, verify: Verification.Skipped).VerifyDiagnostics();
        }

        [Fact]
        public void ParameterIsPointer_02()
        {
            var source =
@"
struct S1(int* x)
{
    public int X => *x;
}

class C1(int* x)
{
    public int X => *x;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // struct S1(int* x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 11),
                // (4,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public int X => *x;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(4, 22),
                // (7,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // class C1(int* x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(7, 10),
                // (9,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     public int X => *x;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(9, 22)
                );
        }

        [Fact]
        public void OrderOfEvaluation_01()
        {
            var source =
@"
class C1(int x) : Base(M(x, ""3""))
{
    int F1 = M(x, ""1"");
    int F2 = M(x, ""2"");

    static int M(int a, string b)
    {
        System.Console.Write(b);
        return a;
    }
}

class Base
{
    public Base(int x)
    {
    }
}

class Program
{
    static void Main()
    {
        new C1(0);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Fact]
        public void OrderOfEvaluation_02()
        {
            var source =
@"
struct S1(int x)
{
    int F1 = M(x, ""1"");
    int F2 = M(x, ""2"");

    static int M(int a, string b)
    {
        System.Console.Write(b);
        return a;
    }
}

class Program
{
    static void Main()
    {
        new S1(0);
    }
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"12").VerifyDiagnostics();
        }

        [Fact]
        public void OrderOfFieldsInMetadata_01()
        {
            var source =
@"
class C1(int x, int y)
{
    int P1 => x;
    int P2 => y;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp,
                             symbolValidator: (m) =>
                                              {
                                                  var c1 = m.GlobalNamespace.GetTypeMember("C1");
                                                  AssertEx.Equal(new[] { "<x>P", "<y>P" }, c1.GetMembers().OfType<FieldSymbol>().Select(f => f.Name));
                                              });
        }

        [Fact]
        public void OrderOfFieldsInMetadata_02()
        {
            var source =
@"
class C1(int x, int y)
{
    int a = 1;
    int P1 => x;
    int P2 => y;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp,
                             symbolValidator: (m) =>
                             {
                                 var c1 = m.GlobalNamespace.GetTypeMember("C1");
                                 AssertEx.Equal(new[] { "<x>P", "<y>P", "a" }, c1.GetMembers().OfType<FieldSymbol>().Select(f => f.Name));
                             });
        }

        [Fact]
        public void OrderOfFieldsInMetadata_03()
        {
            var source =
@"
partial class C1
{
    int b = 2;
}

partial class C1(int x, int y)
{
    int a = 1;
    int P1 => x;
    int P2 => y;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp,
                             symbolValidator: (m) =>
                             {
                                 var c1 = m.GlobalNamespace.GetTypeMember("C1");
                                 AssertEx.Equal(new[] { "b", "<x>P", "<y>P", "a" }, c1.GetMembers().OfType<FieldSymbol>().Select(f => f.Name));
                             });
        }

        [Fact]
        public void OrderOfFieldsInMetadata_04()
        {
            var source =
@"
partial class C1(int x, int y)
{
    int a = 1;
    int P1 => x;
    int P2 => y;
}

partial class C1
{
    int b = 2;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp,
                             symbolValidator: (m) =>
                             {
                                 var c1 = m.GlobalNamespace.GetTypeMember("C1");
                                 AssertEx.Equal(new[] { "<x>P", "<y>P", "a", "b" }, c1.GetMembers().OfType<FieldSymbol>().Select(f => f.Name));
                             });
        }

        [Fact]
        public void OrderOfFieldsInMetadata_05()
        {
            var source =
@"
partial class C1
{
    int b = 2;
}

partial class C1(int x, int y)
{
    int a = 1;
    int P1 => x;
    int P2 => y;
}

partial class C1
{
    int c = 3;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All));
            CompileAndVerify(comp,
                             symbolValidator: (m) =>
                             {
                                 var c1 = m.GlobalNamespace.GetTypeMember("C1");
                                 AssertEx.Equal(new[] { "b", "<x>P", "<y>P", "a", "c" }, c1.GetMembers().OfType<FieldSymbol>().Select(f => f.Name));
                             });
        }

        [Fact]
        public void OnStaticType()
        {
            var source =
@"
static struct S1(int x)
{
}

static class C1(int x)
{
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (2,15): error CS0106: The modifier 'static' is not valid for this item
                // static struct S1(int x)
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("static").WithLocation(2, 15),
                // (2,22): warning CS9113: Parameter 'x' is unread.
                // static struct S1(int x)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(2, 22),
                // (6,14): error CS0710: Static classes cannot have instance constructors
                // static class C1(int x)
                Diagnostic(ErrorCode.ERR_ConstructorInStaticClass, "C1").WithLocation(6, 14),
                // (6,21): warning CS9113: Parameter 'x' is unread.
                // static class C1(int x)
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "x").WithArguments("x").WithLocation(6, 21)
                );
        }

        [Fact]
        public void ManagedTypeDueToCapturing()
        {
            var source1 =
@"
class Test<T> where T : unmanaged
{
    static void M()
    {
        new Test<S1>();
        new Test<S2>();
    }
}
";
            var source2 =
@"
public struct S1(string x)
{
    string P => x;
    public int y;
}

#pragma warning disable CS9113 // Parameter 'x' is unread.
public struct S2(string x)
{
    public int y;
}
";
            var comp = CreateCompilation(new[] { source1, source2 }, options: TestOptions.ReleaseDll);

            comp.GetSemanticModel(comp.SyntaxTrees[0]).GetDiagnostics().Verify(
                // 0.cs(6,18): error CS8377: The type 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         new Test<S1>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "S1").WithArguments("Test<T>", "T", "S1").WithLocation(6, 18)
                );

            comp.GetSemanticModel(comp.SyntaxTrees[1]).GetDiagnostics().Verify();

            comp.VerifyDiagnostics(
                // 0.cs(6,18): error CS8377: The type 'S1' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'Test<T>'
                //         new Test<S1>();
                Diagnostic(ErrorCode.ERR_UnmanagedConstraintNotSatisfied, "S1").WithArguments("Test<T>", "T", "S1").WithLocation(6, 18)
                );
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/72626")]
        public void CompoundAssignment_CapturedParameterAsReceiverOfTargetField()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

internal partial class EditorDocumentManagerListener
{
    internal TestAccessor GetTestAccessor() => new(this);

    internal sealed class TestAccessor(EditorDocumentManagerListener instance)
    {
        public Task ProjectChangedTask => instance._projectChangedTask;

        public event EventHandler OnChangedOnDisk
        {
            add => instance._onChangedOnDisk += value;
            remove => instance._onChangedOnDisk -= value;
        }

        public event EventHandler OnChangedInEditor
        {
            add => instance._onChangedInEditor += value;
            remove => instance._onChangedInEditor -= value;
        }

        public event EventHandler OnOpened
        {
            add => instance._onOpened += value;
            remove => instance._onOpened -= value;
        }

        public event EventHandler OnClosed
        {
            add => instance._onClosed += value;
            remove => instance._onClosed -= value;
        }
    }

    private Task _projectChangedTask = Task.CompletedTask;
    private EventHandler _onChangedOnDisk;
    private EventHandler _onChangedInEditor;
    private EventHandler _onOpened;
    private EventHandler _onClosed;
}
";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseDll);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74726")]
        public void PrimaryCtorNullableFieldWarning(string typeKind)
        {
            var source = $$"""
                #nullable enable

                public {{typeKind}} C()
                {
                    public string Text { get; set; } // 1
                    public C(bool ignored) : this() { }
                }

                public {{typeKind}} C2
                {
                    public string Text { get; set; }
                    public C2() { } // 2
                }

                public {{typeKind}} C3()
                {
                    public string Text { get; set; } = "a";
                    public C3(bool ignored) : this() { }
                }

                public {{typeKind}} C4
                {
                    public string Text { get; set; }
                    public C4() { Text = "a"; }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     public string Text { get; set; } // 1
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(5, 19),
                // (12,12): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     public C2() { } // 2
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C2").WithArguments("property", "Text").WithLocation(12, 12)
                );
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/74726")]
        public void RecordPrimaryCtorNullableFieldWarning(string typeKind)
        {
            var source = $$"""
                #nullable enable

                public record {{typeKind}} C()
                {
                    public string Text { get; set; } // 1
                    public C(bool ignored) : this() { }
                }

                public record {{typeKind}} C2
                {
                    public string Text { get; set; }
                    public C2() { } // 2
                }

                public record {{typeKind}} C3()
                {
                    public string Text { get; set; } = "a";
                    public C3(bool ignored) : this() { }
                }

                public record {{typeKind}} C4
                {
                    public string Text { get; set; }
                    public C4() { Text = "a"; }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,19): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     public string Text { get; set; } // 1
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "Text").WithArguments("property", "Text").WithLocation(5, 19),
                // (12,12): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
                //     public C2() { } // 2
                Diagnostic(ErrorCode.WRN_UninitializedNonNullableField, "C2").WithArguments("property", "Text").WithLocation(12, 12)
                );
        }
    }
}

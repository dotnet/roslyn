// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class RefStructs : ParsingTests
    {
        public RefStructs(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void RefStructSimple()
        {
            var text = @"
class Program
{
    ref struct S1{}

    public ref struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefStructSimpleLangVer()
        {
            var text = @"
class Program
{
    ref struct S1{}

    public ref struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,5): error CS8107: Feature 'ref structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     ref struct S1{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "ref").WithArguments("ref structs", "7.2").WithLocation(4, 5),
                // (6,12): error CS8107: Feature 'ref structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     public ref struct S2{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "ref").WithArguments("ref structs", "7.2").WithLocation(6, 12)
            );
        }

        [Fact]
        public void RefStructErr()
        {
            var text = @"
class Program
{
    ref class S1{}

    public ref unsafe struct S2{}

    ref interface I1{};

    public ref delegate ref int D1();
}
";

            var comp = CreateCompilationWithMscorlib461(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,15): error CS0106: The modifier 'ref' is not valid for this item
                //     ref class S1{}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("ref").WithLocation(4, 15),
                // (6,30): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S2").WithLocation(6, 30),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                //     ref interface I1{};
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("ref").WithLocation(8, 19),
                // (10,33): error CS0106: The modifier 'ref' is not valid for this item
                //     public ref delegate ref int D1();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D1").WithArguments("ref").WithLocation(10, 33)
            );
        }

        [Theory]
        [InlineData("ref class C { }", "C", SyntaxKind.ClassDeclaration)]
        [InlineData("ref interface I { }", "I", SyntaxKind.InterfaceDeclaration)]
        [InlineData("ref enum E { }", "E", SyntaxKind.EnumDeclaration)]
        [InlineData("ref delegate void D();", "D", SyntaxKind.DelegateDeclaration)]
        public void RefTypeDeclarationLookahead_InvalidTypeKind(string source, string typeName, SyntaxKind declarationKind)
        {
            var tree = ParseTree(source, TestOptions.Regular);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var declaration = Assert.Single(root.Members);
            Assert.Equal(declarationKind, declaration.Kind());
            Assert.Equal(SyntaxKind.RefKeyword, declaration.GetFirstToken().Kind());

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, typeName).WithArguments("ref"));
        }

        [Fact]
        public void RefTypeDeclarationLookahead_FunctionPointerRemainsType()
        {
            var root = ParseTree("class C { ref delegate*<void> F; }", TestOptions.Regular).GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var field = Assert.IsType<FieldDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(field.Declaration.Type);
            Assert.IsType<FunctionPointerTypeSyntax>(refType.Type);
        }

        [Theory]
        [InlineData("record", LanguageVersion.CSharp8, SyntaxKind.MethodDeclaration)]
        [InlineData("record", LanguageVersion.CSharp9, SyntaxKind.RecordDeclaration)]
        [InlineData("union", LanguageVersion.CSharp14, SyntaxKind.MethodDeclaration)]
        [InlineData("union", LanguageVersion.CSharp15, SyntaxKind.UnionDeclaration)]
        [InlineData("extension", LanguageVersion.CSharp13, SyntaxKind.MethodDeclaration)]
        [InlineData("extension", LanguageVersion.CSharp14, SyntaxKind.ExtensionBlockDeclaration)]
        public void RefReadonlyContextualKeywordParsing(
            string contextualKeyword, LanguageVersion languageVersion, SyntaxKind expectedMemberKind)
        {
            var source = $"class C {{ ref readonly {contextualKeyword} M(); }}";
            var tree = ParseTree(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var member = Assert.Single(containingType.Members);
            Assert.Equal(expectedMemberKind, member.Kind());

            if (member is MethodDeclarationSyntax method)
            {
                tree.GetDiagnostics().Verify();
                var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
                Assert.Equal(SyntaxKind.ReadOnlyKeyword, refType.ReadOnlyKeyword.Kind());
                Assert.Equal(contextualKeyword, Assert.IsType<IdentifierNameSyntax>(refType.Type).Identifier.Text);
            }
            else
            {
                Assert.Equal(SyntaxKind.RefKeyword, member.GetFirstToken().Kind());
            }
        }

        [Fact]
        public void RefReadonlyRecordReturnType_BreakingParsingChange()
        {
            var source = """
                #pragma warning disable CS8860
                class record { }

                class C
                {
                    public ref readonly record M() => throw null;
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (6,32): error CS0106: The modifier 'readonly' is not valid for this item
                //     public ref readonly record M() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("readonly").WithLocation(6, 32),
                // (6,32): error CS0106: The modifier 'ref' is not valid for this item
                //     public ref readonly record M() => throw null;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("ref").WithLocation(6, 32),
                // (6,36): error CS1514: { expected
                //     public ref readonly record M() => throw null;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "=>").WithLocation(6, 36),
                // (6,36): error CS1513: } expected
                //     public ref readonly record M() => throw null;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(6, 36),
                // (6,36): error CS1519: Invalid token '=>' in a member declaration
                //     public ref readonly record M() => throw null;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=>").WithArguments("=>").WithLocation(6, 36));
        }

        [Fact]
        public void RefTypeDeclarationLookahead_FunctionPointerRemainsReturnType()
        {
            var tree = ParseTree("unsafe class C { ref delegate*<void> M(); }", TestOptions.Regular);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var method = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
            Assert.IsType<FunctionPointerTypeSyntax>(refType.Type);
        }

        [Fact]
        public void PartialRefStruct()
        {
            var text = @"
class Program
{
    partial ref struct S {}
    partial ref struct S {}
}
";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (4,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 13),
                // (5,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 13),
                // (5,24): error CS0102: The type 'Program' already contains a definition for 'S'
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("Program", "S").WithLocation(5, 24));
        }

        [Fact]
        public void RefPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    ref partial struct S {}
    ref partial struct S {}
}");
            comp.VerifyDiagnostics();
        }

        [Theory]
        [InlineData("unsafe")]
        [InlineData("readonly")]
        [InlineData("partial")]
        [InlineData("unsafe readonly")]
        [InlineData("partial readonly")]
        [InlineData("readonly partial")]
        public void RefTypeDeclarationLookahead_SkipsNonScopedModifiers(string modifiers)
        {
            var tree = ParseTree($"class C {{ ref {modifiers} struct S {{}} }}", TestOptions.Regular);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var refStruct = Assert.IsType<StructDeclarationSyntax>(Assert.Single(containingType.Members));
            Assert.Equal($"ref {modifiers}", string.Join(" ", refStruct.Modifiers.Select(static token => token.Text)));
        }

        [Theory]
        [InlineData("")]
        [InlineData("readonly ")]
        public void RefTypeDeclarationLookahead_DoesNotSkipScoped(string prefix)
        {
            var tree = ParseTree($"class C {{ ref {prefix}scoped struct S {{}} }}", TestOptions.Regular);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, Assert.Single(tree.GetDiagnostics()).Code);

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            Assert.Collection(
                containingType.Members,
                member =>
                {
                    var incompleteMember = Assert.IsType<IncompleteMemberSyntax>(member);
                    var refType = Assert.IsType<RefTypeSyntax>(incompleteMember.Type);
                    Assert.Equal($"ref {prefix}scoped", refType.ToString());
                },
                member => Assert.IsType<StructDeclarationSyntax>(member));
        }

        [Fact]
        public void RefPartialReadonlyStruct()
        {
            UsingTree("class C { ref partial readonly struct S {} }");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "S");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            var comp = CreateCompilation(@"
class C
{
    ref partial readonly struct S {}
    ref partial readonly struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,9): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 9),
                // (5,9): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 9));
        }

        [Fact]
        public void RefReadonlyPartialStruct_RefFirst()
        {
            const string text = "class C { ref readonly partial struct S {} }";

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "S");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReadonlyPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    partial ref readonly struct S {}
    partial ref readonly struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 13),
                // (5,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 13),
                // (5,33): error CS0102: The type 'C' already contains a definition for 'S'
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("C", "S").WithLocation(5, 33));
        }

        [Fact]
        public void ReadonlyPartialRefStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly partial ref struct S {}
    readonly partial ref struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,22): error CS1585: Member modifier 'ref' must precede the member type and name
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 22),
                // (5,22): error CS1585: Member modifier 'ref' must precede the member type and name
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 22),
                // (5,33): error CS0102: The type 'C' already contains a definition for 'S'
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("C", "S").WithLocation(5, 33));
        }

        [Fact]
        public void ReadonlyRefPartialStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    readonly ref partial struct S {}
    readonly ref partial struct S {}
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StackAllocParsedAsSpan_Declaration()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    unsafe public void M()
    {
        int* a = stackalloc int[10];
        var b = stackalloc int[10];
        Span<int> c = stackalloc int [10];
    }
}", TestOptions.UnsafeDebugDll).GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_LocalFunction()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        unsafe void local()
        {
            int* x = stackalloc int[10];
        }
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_MethodCall()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        Visit(stackalloc int [10]);
    }
    public void Visit(Span<int> s) { }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_DotAccess()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        Console.WriteLine((stackalloc int [10]).Length);
    }
}").GetParseDiagnostics().Verify();
        }

        [Fact]
        public void StackAllocParsedAsSpan_Cast()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void M()
    {
        void* x = (void*)(stackalloc int[10]);
    }
}").GetParseDiagnostics().Verify();
        }
    }
}

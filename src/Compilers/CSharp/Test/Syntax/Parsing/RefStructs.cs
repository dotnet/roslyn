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
                // (6,12): error CS1585: Member modifier 'ref' must precede the member type and name
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(6, 12),
                // (10,16): error CS1031: Type expected
                //     public ref delegate ref int D1();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(10, 16),
                // (4,15): error CS0106: The modifier 'ref' is not valid for this item
                //     ref class S1{}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("ref").WithLocation(4, 15),
                // (6,30): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S2").WithLocation(6, 30),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                //     ref interface I1{};
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("ref").WithLocation(8, 19)
            );
        }

        [Theory]
        [InlineData("class", "C", SyntaxKind.ClassDeclaration)]
        [InlineData("interface", "I", SyntaxKind.InterfaceDeclaration)]
        public void RefTypeDeclarationLookahead_InvalidTypeKind(string typeKind, string typeName, SyntaxKind declarationKind)
        {
            var source = $"ref {typeKind} {typeName} {{ }}";
            var tree = ParseTree(source, TestOptions.Regular);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var declaration = Assert.IsAssignableFrom<TypeDeclarationSyntax>(Assert.Single(root.Members));
            Assert.Equal(declarationKind, declaration.Kind());
            Assert.Equal(SyntaxKind.RefKeyword, Assert.Single(declaration.Modifiers).Kind());

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, typeName).WithArguments("ref"));
        }

        [Theory]
        [InlineData("enum E { }")]
        [InlineData("delegate void D();")]
        public void RefTypeDeclarationLookahead_OtherReservedDeclarationKinds(string declaration)
        {
            var root = ParseTree($"ref {declaration}", TestOptions.Regular).GetCompilationUnitRoot();
            Assert.IsType<IncompleteMemberSyntax>(root.Members[0]);
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
        [InlineData("record", LanguageVersion.CSharp8)]
        [InlineData("record", LanguageVersion.CSharp9)]
        [InlineData("union", LanguageVersion.CSharp14)]
        [InlineData("union", LanguageVersion.CSharp15)]
        [InlineData("extension", LanguageVersion.CSharp13)]
        [InlineData("extension", LanguageVersion.CSharp14)]
        public void RefReadonlyContextualKeywordRemainsReturnType(string contextualKeyword, LanguageVersion languageVersion)
        {
            var source = $"class C {{ ref readonly {contextualKeyword} M(); }}";
            var tree = ParseTree(source, TestOptions.Regular.WithLanguageVersion(languageVersion));
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var method = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, refType.ReadOnlyKeyword.Kind());
            Assert.Equal(contextualKeyword, Assert.IsType<IdentifierNameSyntax>(refType.Type).Identifier.Text);
        }

        [Theory]
        [InlineData("record", LanguageVersion.CSharp8)]
        [InlineData("record", LanguageVersion.CSharp9)]
        [InlineData("union", LanguageVersion.CSharp14)]
        [InlineData("union", LanguageVersion.CSharp15)]
        [InlineData("extension", LanguageVersion.CSharp13)]
        [InlineData("extension", LanguageVersion.CSharp14)]
        public void RefReadonlyContextualKeywordRemainsPropertyType(string contextualKeyword, LanguageVersion languageVersion)
        {
            var source = $"class C {{ ref readonly {contextualKeyword} A {{ }} }}";
            var tree = ParseTree(source, TestOptions.Regular.WithLanguageVersion(languageVersion));
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var property = Assert.IsType<PropertyDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(property.Type);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, refType.ReadOnlyKeyword.Kind());
            Assert.Equal(contextualKeyword, Assert.IsType<IdentifierNameSyntax>(refType.Type).Identifier.Text);
        }

        [Theory]
        [InlineData("class R { }", LanguageVersion.CSharp15, "ClassDeclaration", "ClassDeclaration", "ClassDeclaration")]
        [InlineData("struct R { }", LanguageVersion.CSharp15, "StructDeclaration", "StructDeclaration", "StructDeclaration")]
        [InlineData("interface R { }", LanguageVersion.CSharp15, "InterfaceDeclaration", "InterfaceDeclaration", "InterfaceDeclaration")]
        [InlineData("enum R { }", LanguageVersion.CSharp15, "EnumDeclaration", "IncompleteMember, EnumDeclaration", "IncompleteMember, EnumDeclaration")]
        [InlineData("delegate void R();", LanguageVersion.CSharp15, "DelegateDeclaration", "IncompleteMember, DelegateDeclaration", "IncompleteMember, DelegateDeclaration")]
        [InlineData("record R { }", LanguageVersion.CSharp8, "PropertyDeclaration", "PropertyDeclaration", "PropertyDeclaration")]
        [InlineData("record R { }", LanguageVersion.CSharp9, "RecordDeclaration", "RecordDeclaration", "PropertyDeclaration")]
        [InlineData("union R { }", LanguageVersion.CSharp14, "PropertyDeclaration", "PropertyDeclaration", "PropertyDeclaration")]
        [InlineData("union R { }", LanguageVersion.CSharp15, "UnionDeclaration", "UnionDeclaration", "PropertyDeclaration")]
        public void ModifierParsing_AtCompilationUnitAndTypeMemberLevel(
            string declaration,
            LanguageVersion languageVersion,
            string expectedReadonlyMembers,
            string expectedRefMembers,
            string expectedRefReadonlyMembers)
        {
            verify("readonly", expectedReadonlyMembers);
            verify("ref", expectedRefMembers);
            verify("ref readonly", expectedRefReadonlyMembers);

            void verify(string modifiers, string expectedMembers)
            {
                var text = $"{modifiers} {declaration}";
                verifyMembers(ParseTree(text, TestOptions.Regular.WithLanguageVersion(languageVersion)).GetCompilationUnitRoot().Members, expectedMembers);

                var containingTypeSource = $"class C {{ {text} }}";
                var containingTypeRoot = ParseTree(containingTypeSource, TestOptions.Regular.WithLanguageVersion(languageVersion)).GetCompilationUnitRoot();
                var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(containingTypeRoot.Members));
                verifyMembers(containingType.Members, expectedMembers);
            }

            static void verifyMembers(SyntaxList<MemberDeclarationSyntax> members, string expectedMembers)
                => Assert.Equal(expectedMembers, string.Join(", ", members.Select(static member => member.Kind())));
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp13, "GlobalStatement", "ConstructorDeclaration", "IncompleteMember, GlobalStatement, GlobalStatement", "IncompleteMember, IncompleteMember")]
        [InlineData(LanguageVersion.CSharp14, "GlobalStatement", "ExtensionBlockDeclaration", "IncompleteMember, GlobalStatement, GlobalStatement", "IncompleteMember, IncompleteMember")]
        public void ExtensionModifierParsing_AtCompilationUnitAndTypeMemberLevel(
            LanguageVersion languageVersion,
            string expectedTopLevelReadonlyMembers,
            string expectedNestedReadonlyMembers,
            string expectedTopLevelRefMembers,
            string expectedNestedRefMembers)
        {
            verify("readonly", expectedTopLevelReadonlyMembers, expectedNestedReadonlyMembers);
            verify("ref", expectedTopLevelRefMembers, expectedNestedRefMembers);
            verify("ref readonly", expectedTopLevelRefMembers, expectedNestedRefMembers);

            void verify(string modifiers, string expectedTopLevelMembers, string expectedNestedMembers)
            {
                var declaration = $"{modifiers} extension(object o) {{ }}";
                var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
                var root = ParseTree(declaration, options).GetCompilationUnitRoot();
                Assert.Equal(expectedTopLevelMembers, string.Join(", ", root.Members.Select(static member => member.Kind())));

                root = ParseTree($"static class C {{ {declaration} }}", options).GetCompilationUnitRoot();
                var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
                Assert.Equal(expectedNestedMembers, string.Join(", ", containingType.Members.Select(static member => member.Kind())));
            }
        }

        [Fact]
        public void RefReadonlyRecordReturnType_BindsAsBefore()
        {
            var source = """
                #pragma warning disable CS8860
                class record { }

                class C
                {
                    public ref readonly record M() => throw null;
                }
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular9).VerifyDiagnostics();
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
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (4,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 13),
                // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
                // (5,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 13));
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

        [Fact]
        public void RefReadonlyStruct_RemainsRejected()
        {
            CreateCompilation("""
                ref readonly struct R { }
                class C
                {
                    ref readonly struct S { }
                }
                """).VerifyDiagnostics(
                // (1,1): error CS1585: Member modifier 'ref' must precede the member type and name
                // ref readonly struct R { }
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(1, 1),
                // (4,5): error CS1585: Member modifier 'ref' must precede the member type and name
                //     ref readonly struct S { }
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 5));
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
                // (4,5): error CS1585: Member modifier 'ref' must precede the member type and name
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 5),
                // (4,9): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 9),
                // (5,5): error CS1585: Member modifier 'ref' must precede the member type and name
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 5),
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

            CreateCompilation(text).VerifyDiagnostics(
                // (1,11): error CS1585: Member modifier 'ref' must precede the member type and name
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(1, 11));
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
                // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
                // (4,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 13),
                // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5),
                // (5,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 13));
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
                // (4,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 14),
                // (4,22): error CS1585: Member modifier 'ref' must precede the member type and name
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(4, 22),
                // (5,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 14),
                // (5,22): error CS1585: Member modifier 'ref' must precede the member type and name
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 22));
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

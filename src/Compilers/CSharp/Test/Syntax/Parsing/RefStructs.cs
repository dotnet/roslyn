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
                // (6,12): error CS1585: Member modifier 'ref' must precede the member type and name
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(6, 12),
                // (8,19): error CS0106: The modifier 'ref' is not valid for this item
                //     ref interface I1{};
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I1").WithArguments("ref").WithLocation(8, 19),
                // (10,33): error CS0106: The modifier 'ref' is not valid for this item
                //     public ref delegate ref int D1();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D1").WithArguments("ref").WithLocation(10, 33)
            );
        }

        [Fact]
        public void RefModifierRecovery_Class()
        {
            const string source = "ref class C { }";
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("ref"));
        }

        [Fact]
        public void RefModifierRecovery_Interface()
        {
            const string source = "ref interface I { }";
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("ref"));
        }

        [Fact]
        public void RefModifierRecovery_Enum()
        {
            const string source = "ref enum E { }";
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("ref"));
        }

        [Fact]
        public void RefModifierRecovery_Delegate()
        {
            const string source = "ref delegate void D();";
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("ref"));
        }

        [Fact]
        public void RefFunctionPointerRemainsReturnType()
        {
            const string source = "unsafe class C { ref delegate*<void> M() => throw null; }";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            var root = ParseTree(source, TestOptions.Regular).GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
            var method = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
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
            var source = $$"""
                #pragma warning disable CS8860, CS8981
                class @{{contextualKeyword}} { }
                interface C { ref readonly {{contextualKeyword}} M(); }
                """;
            var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
            CreateCompilation(source, parseOptions: options).VerifyDiagnostics();

            var tree = ParseTree(source, options);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<InterfaceDeclarationSyntax>(root.Members[1]);
            var method = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, refType.ReadOnlyKeyword.Kind());
            Assert.Equal(contextualKeyword, Assert.IsType<IdentifierNameSyntax>(refType.Type).Identifier.Text);
        }

        [Theory]
        [InlineData("record", LanguageVersion.CSharp8)]
        [InlineData("union", LanguageVersion.CSharp14)]
        public void RefContextualKeywordRemainsReturnTypeBeforeFeature(string contextualKeyword, LanguageVersion languageVersion)
        {
            var source = $$"""
                class {{contextualKeyword}} { }

                class C
                {
                    private {{contextualKeyword}} _field = new {{contextualKeyword}}();
                    public ref {{contextualKeyword}} M() => ref _field;
                }
                """;
            var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
            CreateCompilation(source, parseOptions: options).VerifyDiagnostics(
                // (1,7): warning CS8981: The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, contextualKeyword).WithArguments(contextualKeyword).WithLocation(1, 7));

            var root = ParseTree(source, options).GetCompilationUnitRoot();
            var containingType = Assert.IsType<ClassDeclarationSyntax>(root.Members[1]);
            var method = Assert.IsType<MethodDeclarationSyntax>(containingType.Members[1]);
            var refType = Assert.IsType<RefTypeSyntax>(method.ReturnType);
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
            var source = $$"""
                #pragma warning disable CS8860, CS8981
                class @{{contextualKeyword}} { }
                interface C { ref readonly {{contextualKeyword}} A { get; } }
                """;
            var options = TestOptions.Regular.WithLanguageVersion(languageVersion);
            CreateCompilation(source, parseOptions: options).VerifyDiagnostics();

            var tree = ParseTree(source, options);
            tree.GetDiagnostics().Verify();

            var root = tree.GetCompilationUnitRoot();
            var containingType = Assert.IsType<InterfaceDeclarationSyntax>(root.Members[1]);
            var property = Assert.IsType<PropertyDeclarationSyntax>(Assert.Single(containingType.Members));
            var refType = Assert.IsType<RefTypeSyntax>(property.Type);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, refType.ReadOnlyKeyword.Kind());
            Assert.Equal(contextualKeyword, Assert.IsType<IdentifierNameSyntax>(refType.Type).Identifier.Text);
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
                // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5));
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
        public void RefModifierRecovery_WithScoped(string prefix)
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
                    Assert.Equal($"ref {prefix}", incompleteMember.Modifiers.ToFullString());
                    Assert.Equal("scoped", Assert.IsType<IdentifierNameSyntax>(incompleteMember.Type).Identifier.Text);
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
                // (5,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
                //     readonly partial ref struct S {}
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 14));
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

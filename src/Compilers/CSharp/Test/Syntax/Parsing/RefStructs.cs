// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using System.Linq;
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
                // (4,9): error CS1031: Type expected
                //     ref class S1{}
                Diagnostic(ErrorCode.ERR_TypeExpected, "class"),
                // (6,16): error CS1031: Type expected
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe"),
                // (8,9): error CS1031: Type expected
                //     ref interface I1{};
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(8, 9),
                // (10,16): error CS1031: Type expected
                //     public ref delegate ref int D1();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(10, 16),
                // (6,30): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public ref unsafe struct S2{}
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "S2")
            );
        }

        [Fact]
        public void RefModifierRecovery_Class()
        {
            const string source = "ref class C { }";
            UsingTree(source,
                // (1,5): error CS1031: Type expected
                // ref class C { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 5));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref class C { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 5));
        }

        [Fact]
        public void RefModifierRecovery_Interface()
        {
            const string source = "ref interface I { }";
            UsingTree(source,
                // (1,5): error CS1031: Type expected
                // ref interface I { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 5));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref interface I { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 5));
        }

        [Fact]
        public void RefModifierRecovery_Enum()
        {
            const string source = "ref enum E { }";
            UsingTree(source,
                // (1,5): error CS1031: Type expected
                // ref enum E { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 5));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref enum E { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 5));
        }

        [Fact]
        public void RefModifierRecovery_Delegate()
        {
            const string source = "ref delegate void D();";
            UsingTree(source,
                // (1,5): error CS1031: Type expected
                // ref delegate void D();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 5));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.DelegateDeclaration);
                {
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
                // (1,5): error CS1031: Type expected
                // ref delegate void D();
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 5));
        }

        [Fact]
        public void RefFunctionPointerRemainsReturnType()
        {
            const string source = "unsafe class C { ref delegate*<void> M() => throw null; }";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.FunctionPointerType);
                            {
                                N(SyntaxKind.DelegateKeyword);
                                N(SyntaxKind.AsteriskToken);
                                N(SyntaxKind.FunctionPointerParameterList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.FunctionPointerParameter);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.VoidKeyword);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.ThrowExpression);
                            {
                                N(SyntaxKind.ThrowKeyword);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RefFunctionPointerFieldRemainsType()
        {
            const string source = "class C { ref delegate*<void> F; }";
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.FunctionPointerType);
                                {
                                    N(SyntaxKind.DelegateKeyword);
                                    N(SyntaxKind.AsteriskToken);
                                    N(SyntaxKind.FunctionPointerParameterList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.FunctionPointerParameter);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.VoidKeyword);
                                            }
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "F");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(source).VerifyDiagnostics(
                // (1,31): error CS9064: Target runtime doesn't support ref fields.
                // class C { ref delegate*<void> F; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, "F").WithLocation(1, 31),
                // (1,31): error CS9059: A ref field can only be declared in a ref struct.
                // class C { ref delegate*<void> F; }
                Diagnostic(ErrorCode.ERR_RefFieldInNonRefStruct, "F").WithLocation(1, 31),
                // (1,31): warning CS0169: The field 'C.F' is never used
                // class C { ref delegate*<void> F; }
                Diagnostic(ErrorCode.WRN_UnreferencedField, "F").WithArguments("C.F").WithLocation(1, 31));
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
            UsingTree(source, options);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, $"@{contextualKeyword}");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, contextualKeyword);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData("record", LanguageVersion.CSharp8)]
        [InlineData("union", LanguageVersion.CSharp14)]
        [InlineData("extension", LanguageVersion.CSharp13)]
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
                // (1,7): warning CS8981: The type name 'extension' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class extension { }
                // (1,7): warning CS8981: The type name 'union' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class union { }
                // (1,7): warning CS8981: The type name 'record' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class record { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, contextualKeyword).WithArguments(contextualKeyword).WithLocation(1, 7));

            UsingTree(source, options);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, contextualKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, contextualKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_field");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ObjectCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, contextualKeyword);
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, contextualKeyword);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_field");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
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
            UsingTree(source, options);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, $"@{contextualKeyword}");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, contextualKeyword);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Class_Readonly()
        {
            const string source = "readonly class R { } class C { readonly class R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,16): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly class R { } class C { readonly class R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 16),
                // (1,47): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly class R { } class C { readonly class R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 47));
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Class_Ref()
        {
            const string source = "ref class R { } class C { ref class R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref class R { } class C { ref class R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 5),
                // (1,31): error CS1031: Type expected
                // ref class R { } class C { ref class R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 31));
            UsingTree(source,
            // (1,5): error CS1031: Type expected
            // ref class R { } class C { ref class R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 5),
            // (1,31): error CS1031: Type expected
            // ref class R { } class C { ref class R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 31));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Class_RefReadonly()
        {
            const string source = "ref readonly class R { } class C { ref readonly class R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly class R { } class C { ref readonly class R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 14),
                // (1,49): error CS1031: Type expected
                // ref readonly class R { } class C { ref readonly class R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 49));
            UsingTree(source,
            // (1,14): error CS1031: Type expected
            // ref readonly class R { } class C { ref readonly class R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 14),
            // (1,49): error CS1031: Type expected
            // ref readonly class R { } class C { ref readonly class R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "class").WithLocation(1, 49));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Struct_Readonly()
        {
            const string source = "readonly struct R { } class C { readonly struct R { } }";

            CreateCompilation(source).VerifyDiagnostics();
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Struct_Ref()
        {
            const string source = "ref struct R { } class C { ref struct R { } }";

            CreateCompilation(source).VerifyDiagnostics();
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Struct_RefReadonly()
        {
            const string source = "ref readonly struct R { } class C { ref readonly struct R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly struct R { } class C { ref readonly struct R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 14),
                // (1,50): error CS1031: Type expected
                // ref readonly struct R { } class C { ref readonly struct R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 50));
            UsingTree(source,
            // (1,14): error CS1031: Type expected
            // ref readonly struct R { } class C { ref readonly struct R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 14),
            // (1,50): error CS1031: Type expected
            // ref readonly struct R { } class C { ref readonly struct R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 50));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Interface_Readonly()
        {
            const string source = "readonly interface R { } class C { readonly interface R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,20): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly interface R { } class C { readonly interface R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 20),
                // (1,55): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly interface R { } class C { readonly interface R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 55));
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Interface_Ref()
        {
            const string source = "ref interface R { } class C { ref interface R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref interface R { } class C { ref interface R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 5),
                // (1,35): error CS1031: Type expected
                // ref interface R { } class C { ref interface R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 35));
            UsingTree(source,
            // (1,5): error CS1031: Type expected
            // ref interface R { } class C { ref interface R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 5),
            // (1,35): error CS1031: Type expected
            // ref interface R { } class C { ref interface R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 35));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.InterfaceKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Interface_RefReadonly()
        {
            const string source = "ref readonly interface R { } class C { ref readonly interface R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly interface R { } class C { ref readonly interface R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 14),
                // (1,53): error CS1031: Type expected
                // ref readonly interface R { } class C { ref readonly interface R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 53));
            UsingTree(source,
            // (1,14): error CS1031: Type expected
            // ref readonly interface R { } class C { ref readonly interface R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 14),
            // (1,53): error CS1031: Type expected
            // ref readonly interface R { } class C { ref readonly interface R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "interface").WithLocation(1, 53));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.InterfaceKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Enum_Readonly()
        {
            const string source = "readonly enum R { } class C { readonly enum R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,15): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly enum R { } class C { readonly enum R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 15),
                // (1,45): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly enum R { } class C { readonly enum R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 45));
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EnumDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.EnumKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Enum_Ref()
        {
            const string source = "ref enum R { } class C { ref enum R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref enum R { } class C { ref enum R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 5),
                // (1,30): error CS1031: Type expected
                // ref enum R { } class C { ref enum R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 30));
            UsingTree(source,
            // (1,5): error CS1031: Type expected
            // ref enum R { } class C { ref enum R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 5),
            // (1,30): error CS1031: Type expected
            // ref enum R { } class C { ref enum R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 30));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.EnumDeclaration);
                    {
                        N(SyntaxKind.EnumKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Enum_RefReadonly()
        {
            const string source = "ref readonly enum R { } class C { ref readonly enum R { } }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly enum R { } class C { ref readonly enum R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 14),
                // (1,48): error CS1031: Type expected
                // ref readonly enum R { } class C { ref readonly enum R { } }
                Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 48));
            UsingTree(source,
            // (1,14): error CS1031: Type expected
            // ref readonly enum R { } class C { ref readonly enum R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 14),
            // (1,48): error CS1031: Type expected
            // ref readonly enum R { } class C { ref readonly enum R { } }
            Diagnostic(ErrorCode.ERR_TypeExpected, "enum").WithLocation(1, 48));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.EnumDeclaration);
                {
                    N(SyntaxKind.EnumKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.EnumDeclaration);
                    {
                        N(SyntaxKind.EnumKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Delegate_Readonly()
        {
            const string source = "readonly delegate void R(); class C { readonly delegate void R(); }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,24): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly delegate void R(); class C { readonly delegate void R(); }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 24),
                // (1,62): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly delegate void R(); class C { readonly delegate void R(); }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 62));
            UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Delegate_Ref()
        {
            const string source = "ref delegate void R(); class C { ref delegate void R(); }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,5): error CS1031: Type expected
                // ref delegate void R(); class C { ref delegate void R(); }
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 5),
                // (1,38): error CS1031: Type expected
                // ref delegate void R(); class C { ref delegate void R(); }
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 38));
            UsingTree(source,
            // (1,5): error CS1031: Type expected
            // ref delegate void R(); class C { ref delegate void R(); }
            Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 5),
            // (1,38): error CS1031: Type expected
            // ref delegate void R(); class C { ref delegate void R(); }
            Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 38));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Delegate_RefReadonly()
        {
            const string source = "ref readonly delegate void R(); class C { ref readonly delegate void R(); }";

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly delegate void R(); class C { ref readonly delegate void R(); }
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 14),
                // (1,56): error CS1031: Type expected
                // ref readonly delegate void R(); class C { ref readonly delegate void R(); }
                Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 56));
            UsingTree(source,
            // (1,14): error CS1031: Type expected
            // ref readonly delegate void R(); class C { ref readonly delegate void R(); }
            Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 14),
            // (1,56): error CS1031: Type expected
            // ref readonly delegate void R(); class C { ref readonly delegate void R(); }
            Diagnostic(ErrorCode.ERR_TypeExpected, "delegate").WithLocation(1, 56));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp8_Readonly()
        {
            const string source = "readonly record R { } class C { readonly record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)).VerifyDiagnostics(
                // (1,10): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 10),
                // (1,17): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 17),
                // (1,17): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 17),
                // (1,17): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 17),
                // (1,42): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 42),
                // (1,49): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 49),
                // (1,49): error CS0548: 'C.R': property or indexer must have at least one accessor
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 49));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp8_Ref()
        {
            const string source = "ref record R { } class C { ref record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)).VerifyDiagnostics(
                // (1,5): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 5),
                // (1,12): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 12),
                // (1,12): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 12),
                // (1,32): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 32),
                // (1,39): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 39));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp8_RefReadonly()
        {
            const string source = "ref readonly record R { } class C { ref readonly record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 14),
                // (1,21): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 21),
                // (1,21): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 21),
                // (1,50): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 50),
                // (1,57): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 57));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp8));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp9_Readonly()
        {
            const string source = "readonly record R { } class C { readonly record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)).VerifyDiagnostics(
                // (1,17): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 17),
                // (1,49): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly record R { } class C { readonly record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 49));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp9_Ref()
        {
            const string source = "ref record R { } class C { ref record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)).VerifyDiagnostics(
                // (1,12): error CS0106: The modifier 'ref' is not valid for this item
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("ref").WithLocation(1, 12),
                // (1,39): error CS0106: The modifier 'ref' is not valid for this item
                // ref record R { } class C { ref record R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("ref").WithLocation(1, 39));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Record_CSharp9_RefReadonly()
        {
            const string source = "ref readonly record R { } class C { ref readonly record R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 14),
                // (1,21): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 21),
                // (1,21): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 21),
                // (1,50): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 50),
                // (1,57): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref readonly record R { } class C { ref readonly record R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 57));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp14_Readonly()
        {
            const string source = "readonly union R { } class C { readonly union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14)).VerifyDiagnostics(
                // (1,10): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 10),
                // (1,16): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 16),
                // (1,16): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 16),
                // (1,16): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 16),
                // (1,41): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 41),
                // (1,47): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("readonly").WithLocation(1, 47),
                // (1,47): error CS0548: 'C.R': property or indexer must have at least one accessor
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 47));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "union");
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "union");
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp14_Ref()
        {
            const string source = "ref union R { } class C { ref union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14)).VerifyDiagnostics(
                // (1,5): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 5),
                // (1,11): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 11),
                // (1,11): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 11),
                // (1,31): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 31),
                // (1,37): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 37));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "union");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "union");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp14_RefReadonly()
        {
            const string source = "ref readonly union R { } class C { ref readonly union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 14),
                // (1,20): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 20),
                // (1,20): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 20),
                // (1,49): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 49),
                // (1,55): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 55));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "union");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "union");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp15_Readonly()
        {
            const string source = "readonly union R { } class C { readonly union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15)).VerifyDiagnostics(
                // (1,16): error CS0518: Predefined type 'System.Runtime.CompilerServices.IUnion' is not defined or imported
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.Runtime.CompilerServices.IUnion").WithLocation(1, 16),
                // (1,16): error CS9370: A union declaration must specify at least one case type.
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_UnionDeclarationNeedsCaseTypes, "R").WithLocation(1, 16),
                // (1,16): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.UnionAttribute..ctor'
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.Runtime.CompilerServices.UnionAttribute", ".ctor").WithLocation(1, 16),
                // (1,47): error CS0518: Predefined type 'System.Runtime.CompilerServices.IUnion' is not defined or imported
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.Runtime.CompilerServices.IUnion").WithLocation(1, 47),
                // (1,47): error CS9370: A union declaration must specify at least one case type.
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_UnionDeclarationNeedsCaseTypes, "R").WithLocation(1, 47),
                // (1,47): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.UnionAttribute..ctor'
                // readonly union R { } class C { readonly union R { } }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.Runtime.CompilerServices.UnionAttribute", ".ctor").WithLocation(1, 47));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UnionDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.UnionKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UnionDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.UnionKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp15_Ref()
        {
            const string source = "ref union R { } class C { ref union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15)).VerifyDiagnostics(
                // (1,11): error CS0106: The modifier 'ref' is not valid for this item
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("ref").WithLocation(1, 11),
                // (1,11): error CS0518: Predefined type 'System.Runtime.CompilerServices.IUnion' is not defined or imported
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.Runtime.CompilerServices.IUnion").WithLocation(1, 11),
                // (1,11): error CS9370: A union declaration must specify at least one case type.
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_UnionDeclarationNeedsCaseTypes, "R").WithLocation(1, 11),
                // (1,11): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.UnionAttribute..ctor'
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.Runtime.CompilerServices.UnionAttribute", ".ctor").WithLocation(1, 11),
                // (1,37): error CS0106: The modifier 'ref' is not valid for this item
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "R").WithArguments("ref").WithLocation(1, 37),
                // (1,37): error CS0518: Predefined type 'System.Runtime.CompilerServices.IUnion' is not defined or imported
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "R").WithArguments("System.Runtime.CompilerServices.IUnion").WithLocation(1, 37),
                // (1,37): error CS9370: A union declaration must specify at least one case type.
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_UnionDeclarationNeedsCaseTypes, "R").WithLocation(1, 37),
                // (1,37): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.UnionAttribute..ctor'
                // ref union R { } class C { ref union R { } }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "R").WithArguments("System.Runtime.CompilerServices.UnionAttribute", ".ctor").WithLocation(1, 37));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.UnionDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.UnionKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UnionDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.UnionKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ModifierParsing_Union_CSharp15_RefReadonly()
        {
            const string source = "ref readonly union R { } class C { ref readonly union R { } }";

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15)).VerifyDiagnostics(
                // (1,14): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 14),
                // (1,20): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "R").WithLocation(1, 20),
                // (1,20): error CS0548: '<invalid-global-code>.R': property or indexer must have at least one accessor
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("<invalid-global-code>.R").WithLocation(1, 20),
                // (1,49): error CS0246: The type or namespace name 'union' could not be found (are you missing a using directive or an assembly reference?)
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "union").WithArguments("union").WithLocation(1, 49),
                // (1,55): error CS0548: 'C.R': property or indexer must have at least one accessor
                // ref readonly union R { } class C { ref readonly union R { } }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "R").WithArguments("C.R").WithLocation(1, 55));
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "union");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "union");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
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

            UsingTree(source, TestOptions.Regular9);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.ThrowExpression);
                            {
                                N(SyntaxKind.ThrowKeyword);
                                N(SyntaxKind.NullLiteralExpression);
                                {
                                    N(SyntaxKind.NullKeyword);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

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
            UsingTree("class C { ref partial struct S {} }");
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
    ref partial struct S {}
    ref partial struct S {}
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void RefModifierRecovery_ThroughInvalidStructModifier_Unsafe()
        {
            const string source = "class C { ref unsafe struct S {} }";

            UsingTree(source,
                // (1,15): error CS1031: Type expected
                // class C { ref unsafe struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe").WithLocation(1, 15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.UnsafeKeyword);
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

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (1,15): error CS1031: Type expected
                // class C { ref unsafe struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe").WithLocation(1, 15));
        }

        [Fact]
        public void RefModifierRecovery_ThroughInvalidStructModifier_Readonly()
        {
            const string source = "class C { ref readonly struct S {} }";

            UsingTree(source,
                // (1,24): error CS1031: Type expected
                // class C { ref readonly struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
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

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (1,24): error CS1031: Type expected
                // class C { ref readonly struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 24));
        }

        [Fact]
        public void RefModifierRecovery_ThroughInvalidStructModifiers_UnsafeReadonly()
        {
            const string source = "class C { ref unsafe readonly struct S {} }";

            UsingTree(source,
                // (1,15): error CS1031: Type expected
                // class C { ref unsafe readonly struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe").WithLocation(1, 15));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.UnsafeKeyword);
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

            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (1,15): error CS1031: Type expected
                // class C { ref unsafe readonly struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "unsafe").WithLocation(1, 15));
        }

        [Fact]
        public void RefReadonlyStruct_RemainsRejected()
        {
            const string source = """
                ref readonly struct R { }
                class C
                {
                    ref readonly struct S { }
                }
                """;

            UsingTree(
                source,
                // (1,14): error CS1031: Type expected
                // ref readonly struct R { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 14),
                // (4,18): error CS1031: Type expected
                //     ref readonly struct S { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(4, 18));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
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

            CreateCompilation(source).VerifyDiagnostics(
                // (1,14): error CS1031: Type expected
                // ref readonly struct R { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(1, 14),
                // (4,18): error CS1031: Type expected
                //     ref readonly struct S { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(4, 18));
        }

        [Fact]
        public void RefModifierRecovery_WithScoped()
        {
            const string source = "class C { ref scoped struct S {} }";
            UsingTree(
                source,
                TestOptions.Regular,
                // (1,22): error CS1519: Invalid token 'struct' in a member declaration
                // class C { ref scoped struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "struct").WithArguments("struct"));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
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

            CreateCompilation(source).VerifyDiagnostics(
                // (1,22): error CS1519: Invalid token 'struct' in a member declaration
                // class C { ref scoped struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "struct").WithArguments("struct").WithLocation(1, 22));
        }

        [Fact]
        public void RefModifierRecovery_WithReadonlyScoped()
        {
            const string source = "class C { ref readonly scoped struct S {} }";
            UsingTree(
                source,
                TestOptions.Regular,
                // (1,31): error CS1519: Invalid token 'struct' in a member declaration
                // class C { ref readonly scoped struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "struct").WithArguments("struct"));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
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

            CreateCompilation(source).VerifyDiagnostics(
                // (1,31): error CS1519: Invalid token 'struct' in a member declaration
                // class C { ref readonly scoped struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "struct").WithArguments("struct").WithLocation(1, 31));
        }

        [Fact]
        public void RefPartialReadonlyStruct()
        {
            var comp = CreateCompilation(@"
class C
{
    ref partial readonly struct S {}
    ref partial readonly struct S {}
}");
            comp.VerifyDiagnostics(
                // (4,17): error CS1585: Member modifier 'readonly' must precede the member type and name
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "readonly").WithArguments("readonly").WithLocation(4, 17),
                // (5,17): error CS1585: Member modifier 'readonly' must precede the member type and name
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "readonly").WithArguments("readonly").WithLocation(5, 17),
                // (5,33): error CS0102: The type 'C' already contains a definition for 'S'
                //     ref partial readonly struct S {}
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "S").WithArguments("C", "S").WithLocation(5, 33));
        }

        [Fact]
        public void RefReadonlyPartialStruct_RefFirst()
        {
            const string text = "class C { ref readonly partial struct S {} }";

            UsingTree(text,
                // (1,24): error CS1031: Type expected
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 24),
                // (1,24): error CS1525: Invalid expression term 'partial'
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 24),
                // (1,24): error CS1002: ; expected
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(1, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
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
                // (1,24): error CS1031: Type expected
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 24),
                // (1,24): error CS1525: Invalid expression term 'partial'
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 24),
                // (1,24): error CS1002: ; expected
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(1, 24),
                // (1,24): error CS9064: Target runtime doesn't support ref fields.
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, "").WithLocation(1, 24),
                // (1,24): error CS9059: A ref field can only be declared in a ref struct.
                // class C { ref readonly partial struct S {} }
                Diagnostic(ErrorCode.ERR_RefFieldInNonRefStruct, "").WithLocation(1, 24));
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
                // (4,26): error CS1031: Type expected
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(4, 26),
                // (5,13): error CS1585: Member modifier 'ref' must precede the member type and name
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(5, 13),
                // (5,26): error CS1031: Type expected
                //     partial ref readonly struct S {}
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(5, 26),
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
            const string text = @"
class C
{
    readonly ref partial struct S {}
    readonly ref partial struct S {}
}";
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
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "S");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.StructDeclaration);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.RefKeyword);
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

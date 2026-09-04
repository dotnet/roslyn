// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed partial class ModifierParserRecoveryTests(ITestOutputHelper output) : ParsingTests(output)
{
    private void UsingLambdaExpression(
        string source,
        DiagnosticDescription[]? expectedParsingDiagnostics = null,
        DiagnosticDescription[]? expectedBindingDiagnostics = null,
        string targetType = "System.Action",
        string? declarations = null)
    {
        expectedParsingDiagnostics ??= [];
        expectedBindingDiagnostics ??= expectedParsingDiagnostics;

        UsingExpression(source, expectedParsingDiagnostics);

        var compilationSource = declarations is null
            ? $$"""
                class C
                {
                    void M()
                    {
                        {{targetType}} x = {{source}};
                    }
                }
                """
            : $$"""
                {{declarations}}

                class C
                {
                    void M()
                    {
                        {{targetType}} x = {{source}};
                    }
                }
                """;

        CreateCompilation(compilationSource).VerifyDiagnostics(expectedBindingDiagnostics);
    }

    #region partial modifier

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_BeforeAccessibilityOnClass(LanguageVersion languageVersion)
    {
        var src = "partial public class C { }";
        var options = TestOptions.Regular.WithLanguageVersion(languageVersion);

        UsingTree(src, options);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src, parseOptions: options).VerifyDiagnostics(
            // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            // partial public class C { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_BeforeAccessibilityOnUnion()
    {
        const string src = "partial public union U(int);";
        var options = TestOptions.RegularPreview;

        UsingTree(src, options);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(
            [src, UnionAttributeSource, IUnionSource],
            parseOptions: options).VerifyDiagnostics(
            // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            // partial public union U(int);
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_BeforeAccessibilityOnNamespace()
    {
        const string src = "partial public namespace N { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS1671: A namespace declaration cannot have modifiers or attributes
            // partial public namespace N { }
            Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "partial").WithLocation(1, 1));
    }

    [Theory]
    [InlineData("record", SyntaxKind.RecordDeclaration, SyntaxKind.RecordKeyword)]
    [InlineData("union", SyntaxKind.UnionDeclaration, SyntaxKind.UnionKeyword)]
    public void PartialPartial_ContextualTypeDeclaration(
        string keyword,
        SyntaxKind declarationKind,
        SyntaxKind keywordKind)
    {
        var src = $"partial partial {keyword} C;";

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(declarationKind);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PartialKeyword);
                N(keywordKind);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialPartial_ContextualTypeDeclaration_BindingDiagnostics()
    {
        CreateCompilation("partial partial record C;", parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "partial").WithArguments("partial").WithLocation(1, 9));
    }

    [Fact]
    public void PartialPartial_MethodReturningPartial_CSharp13()
    {
        UsingTree(
            "class C { partial partial M(); }",
            TestOptions.Regular13);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
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
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void PartialPartial_PartialConstructor(LanguageVersion languageVersion)
    {
        UsingTree(
            "class C { partial partial M(); }",
            TestOptions.Regular.WithLanguageVersion(languageVersion));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PartialKeyword);
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

        CreateCompilation(
            "partial class M { partial partial M(); }",
            parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // (1,27): error CS1004: Duplicate 'partial' modifier
            // partial class M { partial partial M(); }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "partial").WithArguments("partial").WithLocation(1, 27),
            // (1,35): error CS9275: Partial member 'M.M()' must have an implementation part.
            // partial class M { partial partial M(); }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "M").WithArguments("M.M()").WithLocation(1, 35));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void PartialPartialPartial_ConstructorDeclaration(LanguageVersion languageVersion)
    {
        const string source = "class Holder { partial partial partial M(); }";
        var parseOptions = TestOptions.Regular.WithLanguageVersion(languageVersion);

        UsingTree(source, parseOptions);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Holder");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PartialKeyword);
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

        CreateCompilation(source, parseOptions: parseOptions).VerifyDiagnostics(
            // (1,24): error CS1004: Duplicate 'partial' modifier
            // class Holder { partial partial partial M(); }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "partial").WithArguments("partial").WithLocation(1, 24),
            // (1,40): error CS1520: Method must have a return type
            // class Holder { partial partial partial M(); }
            Diagnostic(ErrorCode.ERR_MemberNeedsType, "M").WithLocation(1, 40),
            // (1,40): error CS0751: A partial member must be declared within a partial type
            // class Holder { partial partial partial M(); }
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "M").WithLocation(1, 40),
            // (1,40): error CS9275: Partial member 'Holder.Holder()' must have an implementation part.
            // class Holder { partial partial partial M(); }
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "M").WithArguments("Holder.Holder()").WithLocation(1, 40));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void PartialPartialPartial_TopLevelMethod(LanguageVersion languageVersion)
    {
        const string source = "partial partial partial int M();";
        var parseOptions = TestOptions.Regular.WithLanguageVersion(languageVersion);

        UsingTree(source, parseOptions);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.IdentifierToken, "M");
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

        CreateCompilation(source, parseOptions: parseOptions).VerifyDiagnostics(
            // (1,9): error CS1004: Duplicate 'partial' modifier
            // partial partial partial int M();
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "partial").WithArguments("partial").WithLocation(1, 9),
            // (1,29): error CS9348: A compilation unit cannot directly contain members such as fields, methods or properties
            // partial partial partial int M();
            Diagnostic(ErrorCode.ERR_CompilationUnitUnexpected, "M").WithLocation(1, 29),
            // (1,29): error CS0751: A partial member must be declared within a partial type
            // partial partial partial int M();
            Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "M").WithLocation(1, 29),
            // (1,29): error CS8796: Partial method '<invalid-global-code>.M()' must have accessibility modifiers because it has a non-void return type.
            // partial partial partial int M();
            Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "M").WithArguments("<invalid-global-code>.M()").WithLocation(1, 29));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void PartialPartial_TopLevel(LanguageVersion languageVersion)
    {
        const string source = "partial partial C();";
        UsingTree(
            source,
            TestOptions.Regular.WithLanguageVersion(languageVersion),
            // (1,17): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
            // partial partial C();
            Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "C").WithLocation(1, 17),
            // (1,19): error CS1525: Invalid expression term ')'
            // partial partial C();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 19));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp13)]
    [InlineData(LanguageVersion.Preview)]
    public void ManyPartialModifiers_MakesProgress(LanguageVersion languageVersion)
    {
        var modifiers = string.Concat(Enumerable.Repeat("partial ", 10_000));
        var source = $"class C {{ {modifiers}int M(); }}";

        var root = SyntaxFactory.ParseSyntaxTree(
            source,
            options: TestOptions.Regular.WithLanguageVersion(languageVersion)).GetRoot();

        Assert.Equal(source, root.ToFullString());
    }

    [Fact]
    public void Partial_ConversionOperators()
    {
        const string source = """
            partial class C
            {
                public static partial implicit operator int(C c) => 0;
                public static partial explicit operator C(int i) => new();
            }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (3,19): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     public static partial implicit operator int(C c) => 0;
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 19),
            // (4,19): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     public static partial explicit operator C(int i) => new();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 19));
    }

    [Fact]
    public void Partial_RefReturn()
    {
        var src = """
            partial class C
            {
                private static partial ref int M();
                private static partial ref int M() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_BeforeAccessibilityOnMethod(LanguageVersion languageVersion)
    {
        var src = """
            partial class C
            {
                partial public void M();
                partial public void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public void M();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public void M() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5));
    }

    /// <summary>
    /// Backcompat carve-out: the trailing sequence <c>partial async</c> on the implementing
    /// half of an ordinary method has always been accepted (via a long-standing compiler bug
    /// that became part of the public contract). This must keep working on every language
    /// version without triggering a misplaced-modifier diagnostic.
    /// </summary>
    [Theory]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp13)]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_AsyncBackcompat_AllLangvers(LanguageVersion langVer)
    {
        var src = """
            using System.Threading.Tasks;
            partial class C
            {
                public partial Task M();
                public partial async Task M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular.WithLanguageVersion(langVer)).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("partial", LanguageVersion.CSharp13)]
    [InlineData("async", LanguageVersion.CSharp13)]
    [InlineData("required", LanguageVersion.CSharp10)]
    [InlineData("file", LanguageVersion.CSharp10)]
    [InlineData("closed", LanguageVersion.CSharp14)]
    [InlineData("safe", LanguageVersion.CSharp14)]
    public void Partial_ContextualModifierAsReturnType_OlderLangVersion(
        string typeName,
        LanguageVersion languageVersion)
    {
        var src = $$"""
            #pragma warning disable 8981

            class {{typeName}} { }

            partial class C
            {
                private partial {{typeName}} M();
                private partial {{typeName}} M() => new();
            }
            """;

        CreateCompilation(
            src,
            parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics();
    }

    /// <summary>
    /// When <c>partial</c> is neither last nor second-to-last immediately before <c>async</c>,
    /// it falls outside the historical <c>partial async</c> carve-out and remains an error.
    /// </summary>
    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_NonCanonicalWithAsync_AllLangversError(LanguageVersion languageVersion)
    {
        var src = """
            partial class C
            {
                partial public void M();
                partial public async void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public void M();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public async void M() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_BeforeAccessibilityOnProperty(LanguageVersion languageVersion)
    {
        var src = """
            partial class C
            {
                partial public int P { get; set; }
                partial public int P { get => 0; set { } }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public int P { get; set; }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public int P { get => 0; set { } }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5));
    }

    [Fact]
    public void Partial_AsIdentifier_TopLevelAssignment_NotConsumedAsModifier()
    {
        var src = "partial = 1;";

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS0103: The name 'partial' does not exist in the current context
            // partial = 1;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "partial").WithArguments("partial").WithLocation(1, 1));
    }

    [Fact]
    public void PartialThenFile_NoDeclHead_FallsBackToIdentifier()
    {
        UsingTree("partial file;");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "file");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialThenContextualChain_NoDeclHead_FallsBackToIdentifier()
    {
        UsingTree(
            "partial file async required;",
            // (1,1): error CS1031: Type expected
            // partial file async required;
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 1),
            // (1,1): error CS1525: Invalid expression term 'partial'
            // partial file async required;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, ',' expected
            // partial file async required;
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 1));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    M(SyntaxKind.VariableDeclaration);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialAsyncTypeName()
    {
        UsingTree("class C { partial async x; }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
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
    public void PartialAsyncReturnTypeAndPartialMethodName()
    {
        UsingTree("""
            partial class C
            {
                partial async partial();
            }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "async");
                    }
                    N(SyntaxKind.IdentifierToken, "partial");
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
    public void PartialAsyncConstructorName()
    {
        var src = """
            partial class async
            {
                partial async();
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "async");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
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

        CreateCompilation(src).VerifyDiagnostics(
            // (1,15): warning CS8981: The type name 'async' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // partial class async
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "async").WithArguments("async").WithLocation(1, 15),
            // (3,13): error CS9275: Partial member 'async.async()' must have an implementation part.
            //     partial async();
            Diagnostic(ErrorCode.ERR_PartialMemberMissingImplementation, "async").WithArguments("async.async()").WithLocation(3, 13));
    }

    [Fact]
    public void PartialAsyncConstructorName_CSharp13()
    {
        UsingTree("""
            partial class async
            {
                partial async();
            }
            """, TestOptions.Regular13);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "async");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.IdentifierToken, "async");
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
    [InlineData("required", SyntaxKind.RequiredKeyword, 43)]
    [InlineData("file", SyntaxKind.FileKeyword, 35)]
    [InlineData("closed", SyntaxKind.ClosedKeyword, 39)]
    public void PartialContextualModifierConstructorName(
        string name,
        SyntaxKind keywordKind,
        int closeParenColumn)
    {
        var source = $"partial class {name} {{ partial {name}(); }}";
        UsingTree(
            source,
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, closeParenColumn),
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(1, closeParenColumn + 1));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, name);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(keywordKind);
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialSafeConstructorName()
    {
        UsingTree("partial class safe { partial safe(); }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "safe");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "safe");
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
    public void PartialAsyncConstructorNameInNamespace()
    {
        UsingTree("namespace N { partial async(); }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
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
    public void PartialAsyncConstructorNameAfterPublic()
    {
        UsingDeclaration("public partial async();");
        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.PublicKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierToken, "async");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialAsyncConstructorNameAfterStatic()
    {
        UsingDeclaration("static partial async();");
        N(SyntaxKind.ConstructorDeclaration);
        {
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.IdentifierToken, "async");
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void PartialStaticParenthesizedLambda()
    {
        const string source = "partial static () => { }";
        UsingLambdaExpression(
            source,
            expectedBindingDiagnostics:
            [
                // (5,27): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 27),
            ]);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialParenthesizedLambda()
    {
        const string source = "partial () => new partial()";
        UsingLambdaExpression(
            source,
            targetType: "System.Func<partial>",
            declarations: """
                #pragma warning disable CS8981

                class partial
                {
                }
                """);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void StaticPartialParenthesizedLambda()
    {
        const string source = "static partial () => new partial()";
        UsingLambdaExpression(
            source,
            targetType: "System.Func<partial>",
            declarations: """
                #pragma warning disable CS8981

                class partial
                {
                }
                """);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "partial");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void PartialStaticAsyncParenthesizedLambda()
    {
        const string source = "partial static async () => { }";
        UsingLambdaExpression(
            source,
            expectedBindingDiagnostics:
            [
                // (5,27): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
                //         System.Action x = partial static async () => { };
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 27),
            ]);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialAsyncStaticParenthesizedLambda()
    {
        const string source = "partial async static () => { }";
        UsingLambdaExpression(
            source,
            expectedBindingDiagnostics:
            [
                // (5,27): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
                //         System.Action x = partial async static () => { };
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 27),
            ]);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.PartialKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void PartialAsyncParenthesizedLambda()
    {
        const string source = "partial async () => { }";
        UsingLambdaExpression(
            source,
            expectedParsingDiagnostics:
            [
                // (1,1): error CS1525: Invalid expression term 'partial'
                // partial async () => { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 1),
                // (1,1): error CS1073: Unexpected token 'partial'
                // partial async () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "").WithArguments("partial").WithLocation(1, 1),
            ],
            expectedBindingDiagnostics:
            [
                // (5,27): error CS1525: Invalid expression term 'partial'
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(5, 27),
                // (5,27): error CS1002: ; expected
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(5, 27),
                // (5,27): error CS1513: } expected
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "partial").WithLocation(5, 27),
                // (5,35): error CS1520: Method must have a return type
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "async").WithLocation(5, 35),
                // (5,35): error CS0751: A partial member must be declared within a partial type
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "async").WithLocation(5, 35),
                // (5,35): error CS9276: Partial member 'C.C()' must have a definition part.
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "async").WithArguments("C.C()").WithLocation(5, 35),
                // (5,47): error CS1525: Invalid expression term '{'
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(5, 47),
                // (5,47): error CS1002: ; expected
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(5, 47),
                // (5,47): error CS1519: Invalid token '{' in a member declaration
                //         System.Action x = partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(5, 47),
                // (6,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 5),
                // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1),
            ]);
        M(SyntaxKind.IdentifierName);
        {
            M(SyntaxKind.IdentifierToken);
        }
        EOF();
    }

    [Fact]
    public void AsyncPartialParenthesizedLambda()
    {
        const string source = "async partial () => { }";
        UsingLambdaExpression(
            source,
            targetType: "System.Func<System.Threading.Tasks.Task>",
            declarations: """
                #pragma warning disable CS8981

                using partial = System.Threading.Tasks.Task;
                """);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void StaticPartialAsyncParenthesizedLambda()
    {
        const string source = "static partial async () => { }";
        UsingLambdaExpression(
            source,
            expectedParsingDiagnostics:
            [
                // (1,1): error CS1525: Invalid expression term 'static'
                // static partial async () => { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(1, 1),
                // (1,1): error CS1073: Unexpected token 'static'
                // static partial async () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "").WithArguments("static").WithLocation(1, 1),
            ],
            expectedBindingDiagnostics:
            [
                // (5,27): error CS1525: Invalid expression term 'static'
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "static").WithArguments("static").WithLocation(5, 27),
                // (5,27): error CS1002: ; expected
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 27),
                // (5,27): error CS0106: The modifier 'static' is not valid for this item
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(5, 27),
                // (5,34): error CS1031: Type expected
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(5, 34),
                // (5,34): error CS1525: Invalid expression term 'partial'
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(5, 34),
                // (5,34): error CS1002: ; expected
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(5, 34),
                // (5,34): error CS1513: } expected
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "partial").WithLocation(5, 34),
                // (5,42): error CS1520: Method must have a return type
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_MemberNeedsType, "async").WithLocation(5, 42),
                // (5,42): error CS0751: A partial member must be declared within a partial type
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "async").WithLocation(5, 42),
                // (5,42): error CS9276: Partial member 'C.C()' must have a definition part.
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_PartialMemberMissingDefinition, "async").WithArguments("C.C()").WithLocation(5, 42),
                // (5,54): error CS1525: Invalid expression term '{'
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(5, 54),
                // (5,54): error CS1002: ; expected
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(5, 54),
                // (5,54): error CS1519: Invalid token '{' in a member declaration
                //         System.Action x = static partial async () => { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(5, 54),
                // (6,5): error CS1022: Type or namespace definition, or end-of-file expected
                //     }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 5),
                // (7,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(7, 1),
            ]);
        M(SyntaxKind.IdentifierName);
        {
            M(SyntaxKind.IdentifierToken);
        }
        EOF();
    }

    [Fact]
    public void StaticAsyncPartialParenthesizedLambda()
    {
        const string source = "static async partial () => { }";
        UsingLambdaExpression(
            source,
            targetType: "System.Func<System.Threading.Tasks.Task>",
            declarations: """
                #pragma warning disable CS8981

                using partial = System.Threading.Tasks.Task;
                """);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void AsyncPartialStaticParenthesizedLambda()
    {
        const string source = "async partial static () => { }";
        UsingLambdaExpression(
            source,
            expectedParsingDiagnostics:
            [
                // (1,1): error CS1073: Unexpected token 'partial'
                // async partial static () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "async").WithArguments("partial").WithLocation(1, 1),
            ],
            expectedBindingDiagnostics:
            [
                // (5,27): error CS0103: The name 'async' does not exist in the current context
                //         System.Action x = async partial static () => { };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "async").WithArguments("async").WithLocation(5, 27),
                // (5,33): error CS1002: ; expected
                //         System.Action x = async partial static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "partial").WithLocation(5, 33),
                // (5,33): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
                //         System.Action x = async partial static () => { };
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 33),
                // (5,33): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         System.Action x = async partial static () => { };
                Diagnostic(ErrorCode.ERR_IllegalStatement, "partial static () => { }").WithLocation(5, 33),
            ]);
        N(SyntaxKind.IdentifierName);
        {
            N(SyntaxKind.IdentifierToken, "async");
        }
        EOF();
    }

    [Fact]
    public void AsyncStaticPartialParenthesizedLambda()
    {
        const string source = "async static partial () => { }";
        UsingLambdaExpression(
            source,
            targetType: "System.Func<System.Threading.Tasks.Task>",
            declarations: """
                #pragma warning disable CS8981

                using partial = System.Threading.Tasks.Task;
                """);
        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.StaticKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "partial");
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    #endregion partial modifier
}

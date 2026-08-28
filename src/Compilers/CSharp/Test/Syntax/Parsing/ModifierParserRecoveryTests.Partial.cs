// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Tests parser recovery when <c>partial</c> appears in a non-canonical modifier position.
/// <para>
/// The parser accepts <c>partial</c> in any position of the modifier list when the declaration is
/// otherwise unambiguous. The binder continues to report
/// <c>ERR_PartialMisplaced</c> at non-canonical positions, so this parser recovery does not change
/// which programs are accepted. Modifiers that are not legal on a declaration at all (e.g.,
/// <c>partial enum</c>) continue to produce the same binding error.
/// </para>
/// <para>
/// The tests exercise parser shape directly and use compilation diagnostics where needed to verify
/// that improved recovery does not make misplaced modifiers legal.
/// </para>
/// </summary>
public sealed partial class ModifierParserRecoveryTests : ParsingTests
{
    public ModifierParserRecoveryTests(ITestOutputHelper output) : base(output) { }

    #region partial modifier

    // ---------- partial on type declarations ----------

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

    // ---------- partial on methods ----------

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

    // ---------- partial on properties ----------

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

    // ---------- parser recovery: 'partial' as identifier ----------

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

    [Theory]
    [InlineData("file")]
    [InlineData("file async required")]
    public void PartialThenContextualChain_NoDeclHead_FallsBackToIdentifier(string chain)
    {
        var src = $"partial {chain};";
        var tree = SyntaxFactory.ParseSyntaxTree(src);
        var root = tree.GetCompilationUnitRoot();
        foreach (var member in root.Members)
        {
            Assert.False(
                member is MemberDeclarationSyntax mem && mem.Modifiers.Any(SyntaxKind.PartialKeyword),
                $"'partial' should not have been consumed as a modifier; got: {member.Kind()}");
        }
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

    #endregion partial modifier
}

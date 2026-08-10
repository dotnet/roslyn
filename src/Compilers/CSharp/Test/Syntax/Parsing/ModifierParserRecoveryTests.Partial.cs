// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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

    public static TheoryData<SyntaxKind> AllModifierKindsExceptPartialAndRef()
    {
        var data = new TheoryData<SyntaxKind>();
        foreach (SyntaxKind kind in Enum.GetValues<SyntaxKind>())
        {
            if (kind is SyntaxKind.PartialKeyword or SyntaxKind.RefKeyword)
                continue;

            var asReserved = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser.GetModifierExcludingScoped(kind, contextualKind: SyntaxKind.None);
            var asContextual = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser.GetModifierExcludingScoped(SyntaxKind.IdentifierToken, contextualKind: kind);
            if (asReserved != DeclarationModifiers.None || asContextual != DeclarationModifiers.None)
                data.Add(kind);
        }
        return data;
    }

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

    [Theory]
    [InlineData(LanguageVersion.CSharp14)]
    [InlineData(LanguageVersion.Preview)]
    public void Partial_InMiddleOfTypeModifierList(LanguageVersion languageVersion)
    {
        var src = "public partial static class C { }";
        var options = TestOptions.Regular.WithLanguageVersion(languageVersion);

        UsingTree(src, options);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src, parseOptions: options).VerifyDiagnostics(
            // (1,8): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            // public partial static class C { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 8));
    }

    [Theory]
    [InlineData("partial public class C")]
    [InlineData("partial public struct C")]
    [InlineData("partial public interface C")]
    [InlineData("partial public record C")]
    [InlineData("partial public record class C")]
    [InlineData("partial public record struct C")]
    public void Partial_FirstPosition_TypeKinds_AllLangversError(string decl)
    {
        var src = decl + " { }";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1));

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_WithFileModifier()
    {
        var src = "partial file class C { }";

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            // partial file class C { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(1, 1));
    }

    // ---------- partial on methods ----------

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

    [Fact]
    public void Partial_InMiddleOfMethodModifierList()
    {
        var src = """
            partial class C
            {
                public partial static void M();
                public partial static void M() { }
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     public partial static void M();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 12),
            // (4,12): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     public partial static void M() { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 12));
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

    // ---------- partial on events ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnEvent()
    {
        var src = """
            using System;
            partial class C
            {
                partial public event Action E;
                partial public event Action E { add { } remove { } }
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public event Action E;
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(4, 5),
            // (5,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public event Action E { add { } remove { } }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(5, 5));
    }

    // ---------- partial on constructors ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnConstructor()
    {
        var src = """
            partial class C
            {
                partial public C();
                partial public C() { }
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public C();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(3, 5),
            // (4,5): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', 'event', an instance constructor name, or a method or property return type.
            //     partial public C() { }
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

    [Theory, MemberData(nameof(AllModifierKindsExceptPartialAndRef))]
    public void PartialThenModifier_OnClass(SyntaxKind modifier)
    {
        var src = $"partial {SyntaxFacts.GetText(modifier)} class C {{ }}";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(modifier);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(AllModifierKindsExceptPartialAndRef))]
    public void PartialThenModifier_OnMethod(SyntaxKind modifier)
    {
        var src = $$"""partial class C { partial {{SyntaxFacts.GetText(modifier)}} void M(); }""";

        UsingTree(src);
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
                    N(modifier);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
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

    [Fact]
    public void MultiModifier_PartialFirst_ThreeOthers()
    {
        UsingTree("partial public static unsafe class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MultiModifier_PartialBetweenContextualAndReserved()
    {
        UsingTree("file partial sealed class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.SealedKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MultiModifier_PartialLast_InChainOfContextuals()
    {
        UsingTree("file partial class C { partial async void M() { } }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
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
    public void PartialAsIdentifier_BareSemicolon()
    {
        UsingTree("partial;");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialAsIdentifier_InExpressionContext()
    {
        var tree = SyntaxFactory.ParseSyntaxTree("_ = partial + 1;");
        var root = tree.GetCompilationUnitRoot();
        Assert.All(root.Members, m => Assert.IsType<GlobalStatementSyntax>(m));
    }

    [Fact]
    public void PartialAsIdentifier_PartialIdentifierSemicolon_NotAMember()
    {
        var tree = SyntaxFactory.ParseSyntaxTree("partial X;");
        var root = tree.GetCompilationUnitRoot();
        foreach (var member in root.Members)
        {
            Assert.False(
                member is MemberDeclarationSyntax mem && mem.Modifiers.Any(SyntaxKind.PartialKeyword),
                $"'partial' should not have been consumed as a modifier; got: {member.Kind()}");
        }
    }

    public static TheoryData<string> ContextualModifierChains()
    {
        return new TheoryData<string>
        {
            "file",
            "async",
            "required",
            "file async",
            "file required",
            "async required",
            "file async required",
        };
    }

    [Theory]
    [MemberData(nameof(ContextualModifierChains))]
    public void PartialThenContextualChain_OnClass(string chain)
    {
        var src = $"partial {chain} class C {{ }}";
        var tree = SyntaxFactory.ParseSyntaxTree(src);
        var root = tree.GetCompilationUnitRoot();
        var classDecl = Assert.Single(root.Members.OfType<ClassDeclarationSyntax>());
        Assert.Contains(classDecl.Modifiers, m => m.IsKind(SyntaxKind.PartialKeyword));
        foreach (var modText in chain.Split(' '))
        {
            Assert.Contains(classDecl.Modifiers, m => m.Text == modText);
        }
    }

    [Theory]
    [MemberData(nameof(ContextualModifierChains))]
    public void PartialThenContextualChainThenReserved_OnClass(string chain)
    {
        var src = $"partial {chain} public class C {{ }}";
        var tree = SyntaxFactory.ParseSyntaxTree(src);
        var root = tree.GetCompilationUnitRoot();
        var classDecl = Assert.Single(root.Members.OfType<ClassDeclarationSyntax>());
        Assert.Contains(classDecl.Modifiers, m => m.IsKind(SyntaxKind.PartialKeyword));
        Assert.Contains(classDecl.Modifiers, m => m.IsKind(SyntaxKind.PublicKeyword));
        foreach (var modText in chain.Split(' '))
        {
            Assert.Contains(classDecl.Modifiers, m => m.Text == modText);
        }
    }

    [Theory]
    [MemberData(nameof(ContextualModifierChains))]
    public void PartialThenContextualChain_OnMethod(string chain)
    {
        var src = $"class C {{ public partial {chain} void M() {{ }} }}";
        var tree = SyntaxFactory.ParseSyntaxTree(src);
        var classDecl = (ClassDeclarationSyntax)tree.GetCompilationUnitRoot().Members.Single();
        var method = (MethodDeclarationSyntax)classDecl.Members.Single();
        Assert.Contains(method.Modifiers, m => m.IsKind(SyntaxKind.PartialKeyword));
        foreach (var modText in chain.Split(' '))
        {
            Assert.Contains(method.Modifiers, m => m.Text == modText);
        }
    }

    [Fact]
    public void PartialPartial_OnClass()
    {
        UsingTree("partial partial class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [MemberData(nameof(ContextualModifierChains))]
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

    #endregion partial modifier
}

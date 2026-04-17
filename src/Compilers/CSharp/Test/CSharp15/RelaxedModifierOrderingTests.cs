// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Tests for the relaxed-modifier-ordering feature (<see cref="MessageID.IDS_FeatureRelaxedModifierOrdering"/>).
/// Tracking: https://github.com/dotnet/csharplang/issues/8966.
/// <para>
/// The parser accepts the affected modifiers (currently <c>partial</c>) in any position of the
/// modifier list on every language version.  On language versions that predate the feature the
/// binder reports <c>ERR_FeatureInPreview</c> (or equivalent) at the non-canonical modifier
/// location; on preview+ the binder accepts the declaration silently.  Modifiers that are not
/// legal on a declaration at all (e.g., <c>partial enum</c>) continue to produce the
/// pre-existing <c>ERR_PartialMisplaced</c> regardless of language version.
/// </para>
/// <para>
/// Each test exercises the parser shape via <c>UsingTree</c> and then exercises binding via
/// <c>CreateCompilation(...).VerifyDiagnostics(...)</c>.
/// </para>
/// </summary>
public sealed partial class RelaxedModifierOrderingTests : ParsingTests
{
    public RelaxedModifierOrderingTests(ITestOutputHelper output) : base(output) { }

    #region partial modifier

    // ---------- partial on type declarations ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnClass_Preview()
    {
        var src = "partial public class C { }";

        UsingTree(src, TestOptions.RegularPreview);
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

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Partial_BeforeAccessibilityOnClass_CSharp14_FeatureError()
    {
        var src = "partial public class C { }";

        UsingTree(src, TestOptions.Regular14);
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

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // partial public class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_InMiddleOfTypeModifierList_Preview()
    {
        var src = "public partial static class C { }";

        UsingTree(src, TestOptions.RegularPreview);
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

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Partial_InMiddleOfTypeModifierList_CSharp14_FeatureError()
    {
        var src = "public partial static class C { }";

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,8): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // public partial static class C { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 8));
    }

    [Fact]
    public void Partial_CanonicalPositionStillLegal_AllLangvers()
    {
        var src = "public static partial class C { }";

        foreach (var options in new[] { TestOptions.Regular9, TestOptions.Regular13, TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics();
        }
    }

    [Theory]
    [InlineData("partial class C")]
    [InlineData("partial struct C")]
    [InlineData("partial interface C")]
    [InlineData("partial record C")]
    [InlineData("partial record class C")]
    [InlineData("partial record struct C")]
    public void Partial_LastPosition_TypeKinds_AllLangvers(string decl)
    {
        var src = "public " + decl + " { }";

        // Canonical ordering remains legal regardless of language version for every partial-capable type kind.
        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics();
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("partial public class C")]
    [InlineData("partial public struct C")]
    [InlineData("partial public interface C")]
    [InlineData("partial public record C")]
    [InlineData("partial public record class C")]
    [InlineData("partial public record struct C")]
    public void Partial_FirstPosition_TypeKinds_PreviewLegal_CSharp14FeatureError(string decl)
    {
        var src = decl + " { }";

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_WithFileModifier_Preview()
    {
        // 'file' is another contextual modifier; confirm the forward-scan handles chains
        // of contextual modifiers before committing to 'partial' as a modifier.
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

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    // ---------- partial on methods ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnMethod_Preview()
    {
        var src = """
            partial class C
            {
                partial public void M();
                partial public void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Partial_BeforeAccessibilityOnMethod_CSharp14_FeatureError()
    {
        var src = """
            partial class C
            {
                partial public void M();
                partial public void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public void M();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(3, 5),
            // (4,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public void M() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(4, 5));
    }

    [Fact]
    public void Partial_InMiddleOfMethodModifierList_Preview()
    {
        var src = """
            partial class C
            {
                public partial static void M();
                public partial static void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    /// <summary>
    /// Backcompat carve-out: the trailing sequence <c>partial async</c> on the implementing
    /// half of an ordinary method has always been accepted (via a long-standing compiler bug
    /// that became part of the public contract).  This must keep working on every language
    /// version, including those predating the relaxed-ordering feature, without triggering a
    /// feature-availability diagnostic.
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
    /// Exercises the interaction between the historical <c>partial async</c> carve-out and the
    /// new relaxed-ordering feature.  When <c>partial</c> is neither last nor second-to-last
    /// immediately before <c>async</c>, it falls outside the carve-out and the new feature gate
    /// must fire on pre-preview language versions.  The implementing half also stresses the
    /// forward-scan in <c>IsPartialModifierInDeclarationHead</c>: after eating <c>partial</c>
    /// the helper must walk through both contextual (<c>public</c> is non-contextual here; use
    /// the declaration half above for a pure all-non-contextual case) and commit to a
    /// declaration head.
    /// </summary>
    [Fact]
    public void Partial_NonCanonicalWithAsync_CSharp14_FeatureError()
    {
        var src = """
            partial class C
            {
                partial public void M();
                partial public async void M() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public void M();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(3, 5),
            // (4,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public async void M() { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(4, 5));

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    // ---------- partial on properties ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnProperty_Preview()
    {
        var src = """
            partial class C
            {
                partial public int P { get; set; }
                partial public int P { get => 0; set { } }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void Partial_BeforeAccessibilityOnProperty_CSharp14_FeatureError()
    {
        var src = """
            partial class C
            {
                partial public int P { get; set; }
                partial public int P { get => 0; set { } }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public int P { get; set; }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(3, 5),
            // (4,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     partial public int P { get => 0; set { } }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(4, 5));
    }

    // ---------- partial on events ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnEvent_Preview()
    {
        var src = """
            using System;
            partial class C
            {
                partial public event Action E;
                partial public event Action E { add { } remove { } }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    // ---------- partial on constructors ----------

    [Fact]
    public void Partial_BeforeAccessibilityOnConstructor_Preview()
    {
        var src = """
            partial class C
            {
                partial public C();
                partial public C() { }
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    // ---------- still-invalid placements (partial on a non-partial-capable declaration) ----------

    /// <summary>
    /// 'partial' on an enum/delegate/namespace is still reported via <c>ERR_PartialMisplaced</c>
    /// on every language version, through <c>ModifierUtils.ReportPartialError</c>.  The
    /// relaxed-ordering feature does not make these legal.
    /// </summary>
    [Fact]
    public void Partial_OnEnum_StillErrorsOnPreview()
    {
        var src = "public partial enum E { }";

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (1,21): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
            // public partial enum E { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(1, 21));
    }

    [Fact]
    public void Partial_OnDelegate_StillErrorsOnPreview()
    {
        var src = "public partial delegate void D();";

        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (1,30): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
            // public partial delegate void D();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "D").WithLocation(1, 30));
    }

    // ---------- parser recovery: 'partial' as identifier ----------

    /// <summary>
    /// When 'partial' is not followed by a declaration head (modulo other modifier tokens), it
    /// must remain available as an identifier so the fall-through global-statement recovery
    /// path in the parser still works.  This pins the behavior of the negative branch of
    /// <c>IsPartialModifierInDeclarationHead</c>.
    /// </summary>
    [Fact]
    public void Partial_AsIdentifier_TopLevelAssignment_NotConsumedAsModifier()
    {
        var src = "partial = 1;";

        // Parser must not consume 'partial' as a modifier; it should fall through to the
        // global-statement recovery path and parse 'partial' as an identifier in an assignment
        // expression.  Parse tree must be clean.
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

        // Binding will then produce the "name not in context" error since 'partial' is an
        // unresolved identifier.  The critical point is that the parser did not try to treat
        // 'partial' as a modifier and commit to a broken declaration.
        CreateCompilation(src, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (1,1): error CS0103: The name 'partial' does not exist in the current context
            // partial = 1;
            Diagnostic(ErrorCode.ERR_NameNotInContext, "partial").WithArguments("partial").WithLocation(1, 1));
    }

    #endregion partial modifier
}

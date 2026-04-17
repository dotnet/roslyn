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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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
        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 1));
    }

    [Fact]
    public void Partial_WithFileModifier_Preview()
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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
    /// When <c>partial</c> is neither last nor second-to-last immediately before <c>async</c>,
    /// it falls outside the historical <c>partial async</c> carve-out and the relaxed-ordering
    /// feature gate must fire on pre-preview language versions.
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics();
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

        CreateCompilation(src).VerifyDiagnostics(
            // (1,21): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
            // public partial enum E { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(1, 21));
    }

    [Fact]
    public void Partial_OnDelegate_StillErrorsOnPreview()
    {
        var src = "public partial delegate void D();";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,30): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
            // public partial delegate void D();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "D").WithLocation(1, 30));
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

    [Fact]
    public void Branch_Class()
    {
        UsingTree("partial class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
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

    [Fact]
    public void Branch_Struct()
    {
        UsingTree("partial struct S { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_Interface()
    {
        UsingTree("partial interface I { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.InterfaceDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.InterfaceKeyword);
                N(SyntaxKind.IdentifierToken, "I");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_Record()
    {
        UsingTree("partial record R { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_RecordClass()
    {
        UsingTree("partial record class R { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_RecordStruct()
    {
        UsingTree("partial record struct R { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordStructDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "R");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_Record_PreRecordsFeature_NotConsumedAsModifier()
    {
        var tree = SyntaxFactory.ParseSyntaxTree("partial record R { }", TestOptions.Regular8);
        var root = tree.GetCompilationUnitRoot();
        Assert.DoesNotContain(root.Members, m => m is RecordDeclarationSyntax);
    }

    [Fact]
    public void Branch_Union_Preview()
    {
        UsingTree("partial union U { }", TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Branch_Union_PreUnionsFeature_NotConsumedAsModifier()
    {
        var tree = SyntaxFactory.ParseSyntaxTree("partial union U { }", TestOptions.Regular14);
        var root = tree.GetCompilationUnitRoot();
        Assert.DoesNotContain(root.Members, m => m is TypeDeclarationSyntax { Keyword.Text: "union" });
    }

    [Fact]
    public void Branch_Namespace()
    {
        UsingTree("partial namespace N { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
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

        CreateCompilation("partial namespace N { }").VerifyDiagnostics(
            // (1,1): error CS1671: A namespace declaration cannot have modifiers or attributes
            // partial namespace N { }
            Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "partial").WithLocation(1, 1));
    }

    [Fact]
    public void Branch_Enum()
    {
        UsingTree("partial enum E { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.EnumDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.EnumKeyword);
                N(SyntaxKind.IdentifierToken, "E");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation("partial enum E { }").VerifyDiagnostics(
            // (1,14): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
            // partial enum E { }
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(1, 14));
    }

    [Fact]
    public void Branch_Delegate()
    {
        UsingTree("partial delegate void D();");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.DelegateDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
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

        CreateCompilation("partial delegate void D();").VerifyDiagnostics(
            // (1,23): error CS0267: The 'partial' modifier can only appear on a class, record, struct, interface, event, instance constructor, method or property.
            // partial delegate void D();
            Diagnostic(ErrorCode.ERR_PartialMisplaced, "D").WithLocation(1, 23));
    }

    [Fact]
    public void Branch_Event()
    {
        var src = """
            using System;
            partial class C
            {
                partial event Action E;
                partial event Action E { add { } remove { } }
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.EventFieldDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Action");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "E");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EventDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.EventKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Action");
                    }
                    N(SyntaxKind.IdentifierToken, "E");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.AddAccessorDeclaration);
                        {
                            N(SyntaxKind.AddKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.RemoveAccessorDeclaration);
                        {
                            N(SyntaxKind.RemoveKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
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
    public void Branch_Constructor_Preview()
    {
        var src = """
            partial class C
            {
                partial C();
                partial C() { }
            }
            """;

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
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
    public void Branch_Constructor_PrePartialCtorsFeature_NotConsumedAsModifier()
    {
        var src = """
            partial class C
            {
                partial C();
            }
            """;

        var tree = SyntaxFactory.ParseSyntaxTree(src, TestOptions.Regular13);
        var root = tree.GetCompilationUnitRoot();
        var type = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(root.Members));
        Assert.DoesNotContain(type.Members, m => m is ConstructorDeclarationSyntax);
    }

    [Fact]
    public void Branch_Method_Void()
    {
        UsingTree("""
            partial class C { partial void M(); }
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
    public void Branch_Method_PredefinedReturn()
    {
        UsingTree("""
            partial class C { partial int M(); partial int M() => 0; }
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
                N(SyntaxKind.MethodDeclaration);
                {
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
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
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
    public void Branch_Method_GenericReturn()
    {
        var src = """
            using System.Collections.Generic;
            partial class C { public partial List<int> M(); public partial List<int> M() => null; }
            """;
        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Branch_Method_ArrayReturn()
    {
        var src = """
            partial class C { public partial int[] M(); public partial int[] M() => null; }
            """;
        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Branch_Property()
    {
        UsingTree("""
            partial class C { partial int P { get; set; } partial int P { get => 0; set { } } }
            """);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
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
    public void Branch_Indexer()
    {
        var src = """
            partial class C { public partial int this[int i] { get; set; } public partial int this[int i] { get => 0; set { } } }
            """;
        CreateCompilation(src).VerifyDiagnostics();
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
    public void ModifierThenPartial_OnClass(SyntaxKind modifier)
    {
        var src = $"{SyntaxFacts.GetText(modifier)} partial class C {{ }}";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(modifier);
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

    [Theory, MemberData(nameof(AllModifierKindsExceptPartialAndRef))]
    public void ModifierThenPartial_OnMethod(SyntaxKind modifier)
    {
        var src = $$"""partial class C { {{SyntaxFacts.GetText(modifier)}} partial void M(); }""";

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
                    N(modifier);
                    N(SyntaxKind.PartialKeyword);
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
    public void PartialThenFullContextualChain_BeforeDeclHead()
    {
        UsingTree("partial file async required class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.RequiredKeyword);
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
    public void PartialThenFullContextualChainThenReserved()
    {
        UsingTree("partial file async required public class C { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PublicKeyword);
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

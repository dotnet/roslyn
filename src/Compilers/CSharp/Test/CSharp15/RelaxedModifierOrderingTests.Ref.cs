// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Tests for the parser's handling of the <c>ref</c> modifier in non-canonical positions.
/// Tracking: https://github.com/dotnet/csharplang/issues/8966.
/// <para>
/// The parser is permissive: it accepts <c>ref</c> in any modifier-list position on every
/// language version when the token cannot be parsed as a return-type prefix.  This lets the
/// binder produce a single targeted diagnostic instead of cascading parse errors.  The
/// language itself does <em>not</em> relax where <c>ref</c> may appear:
/// <list type="bullet">
/// <item>On a type declaration, the binder unconditionally reports
/// <c>ERR_RefMisplacedOnType</c> (<c>CS9389</c>) at the <c>ref</c> token whenever it isn't
/// immediately before <c>struct</c>, <c>record struct</c>, or <c>union</c> (or before a
/// trailing <c>partial struct</c>) -- regardless of language version.</item>
/// <item>If <c>ref</c> appears as a modifier on a member, the binder reports the targeted
/// <c>ERR_RefNotMemberModifier</c> (<c>CS9388</c>) on the <c>ref</c> token.  <c>ref</c> belongs
/// to the return type for members and must appear immediately before it.</item>
/// <item>If <c>ref</c> appears on a type kind that does not accept it (class, interface, enum,
/// delegate, namespace), the pre-existing <c>ERR_BadMemberFlag</c> diagnostic is reported.</item>
/// </list>
/// </para>
/// <para>
/// Canonical <c>ref T</c> and <c>ref readonly T</c> return-type parsing is unaffected: those
/// forms continue to parse with <c>ref</c> as part of the return type, not as a modifier.
/// </para>
/// </summary>
public sealed partial class RelaxedModifierOrderingTests
{
    #region ref modifier

    // ---------- ref on struct (canonical positions, legal on every language version) ----------

    [Fact]
    public void Ref_CanonicalPosition_OnStruct_AllLangvers()
    {
        var src = "public ref struct S { }";

        foreach (var options in new[] { TestOptions.Regular9, TestOptions.Regular13, TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics();
        }
    }

    [Fact]
    public void Ref_CanonicalPosition_BeforeTrailingPartial_AllLangvers()
    {
        // Historical form: `ref` immediately before `partial struct` is also considered canonical
        // and must bind without a feature diagnostic on older language versions.
        var src = "public ref partial struct S { }";

        foreach (var options in new[] { TestOptions.Regular9, TestOptions.Regular13, TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics();
        }
    }

    [Fact]
    public void Ref_CanonicalPosition_OnReadonlyStruct_AllLangvers()
    {
        var src = "public readonly ref struct S { }";

        foreach (var options in new[] { TestOptions.Regular9, TestOptions.Regular13, TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics();
        }
    }

    [Fact]
    public void Ref_FirstPosition_OnStruct_AllLangvers()
    {
        // Parser is permissive and accepts `ref` in any modifier-list position.  The binder
        // unconditionally errors on non-canonical positions regardless of language version --
        // the language doesn't relax where `ref` may appear on a type declaration.
        var src = "ref public struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        foreach (var options in new[] { TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics(
                // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                // ref public struct S { }
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1));
        }
    }

    [Fact]
    public void Ref_InMiddleOfTypeModifierList_AllLangvers()
    {
        var src = "public ref unsafe struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        foreach (var options in new[] { TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (1,8): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                // public ref unsafe struct S { }
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 8));
        }
    }

    [Fact]
    public void Ref_BeforeReadonly_OnStruct_AcceptedByParserButErrorsInBinder()
    {
        // `ref readonly struct S` is non-canonical (canonical is `readonly ref struct S`).
        // Parser accepts both `ref` and `readonly` as modifiers without confusing this with
        // the return-type form `ref readonly T`, but the binder unconditionally rejects the
        // non-canonical position regardless of language version.
        var src = "public ref readonly struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        foreach (var options in new[] { TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics(
                // (1,8): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                // public ref readonly struct S { }
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 8));
        }
    }

    [Theory]
    [InlineData("ref struct S")]
    [InlineData("ref partial struct S")]
    public void Ref_LastPosition_TypeKinds_AllLangvers(string decl)
    {
        var src = "public " + decl + " { }";

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics();
        CreateCompilation(src).VerifyDiagnostics();
    }

    [Theory]
    [InlineData("ref public struct S")]
    [InlineData("ref public partial struct S")]
    public void Ref_FirstPosition_TypeKinds_AlwaysError(string decl)
    {
        // `ref` in non-canonical position always errors regardless of language version: the
        // parser accepts the placement to enable a clean diagnostic, but the language doesn't
        // relax where `ref` may appear.
        var src = decl + " { }";

        foreach (var options in new[] { TestOptions.Regular14, TestOptions.RegularPreview })
        {
            CreateCompilation(src, parseOptions: options).VerifyDiagnostics(
                // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
                Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1));
        }
    }

    // ---------- ref on record struct ----------
    //
    // `ref record struct S` is parsed permissively (the parser treats `ref` as a modifier on the
    // record-struct declaration head) but the binder rejects it: `ref` is only valid on plain
    // structs and unions, not on record structs.  This matches the pre-existing behavior (see
    // RecordStructTests.ModifiersErrors_01).

    [Fact]
    public void Ref_FirstPosition_OnRecordStruct()
    {
        // `ref public record struct S` errors with both the position diagnostic
        // (`ref` not in canonical place) and the kind diagnostic (`ref` isn't valid on record
        // structs at all, since they aren't `ref struct`s).
        var src = "ref public record struct S(int X);";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordStructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref public record struct S(int X);
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1),
            // (1,26): error CS0106: The modifier 'ref' is not valid for this item
            // ref public record struct S(int X);
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("ref").WithLocation(1, 26));
    }

    // ---------- ref on type kinds that do not accept it ----------

    [Fact]
    public void Ref_OnClass_StillErrorsOnPreview()
    {
        var src = "public ref class C { }";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,18): error CS0106: The modifier 'ref' is not valid for this item
            // public ref class C { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("ref").WithLocation(1, 18));
    }

    [Fact]
    public void Ref_OnClass_FirstPosition_CSharp14_FeatureError()
    {
        // On older language versions the non-canonical placement still drives a feature-error
        // diagnostic in addition to the not-valid-for-this-item error.
        var src = "ref public class C { }";

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // ref public class C { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1),
            // (1,18): error CS0106: The modifier 'ref' is not valid for this item
            // ref public class C { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("ref").WithLocation(1, 18));
    }

    [Fact]
    public void Ref_OnInterface_StillErrors()
    {
        var src = "public ref interface I { }";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,22): error CS0106: The modifier 'ref' is not valid for this item
            // public ref interface I { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "I").WithArguments("ref").WithLocation(1, 22));
    }

    [Fact]
    public void Ref_OnEnum_StillErrors()
    {
        var src = "public ref enum E { A }";

        // `ref enum` is now parsed permissively; the binder rejects the `ref` modifier.
        CreateCompilation(src).VerifyDiagnostics(
            // (1,17): error CS0106: The modifier 'ref' is not valid for this item
            // public ref enum E { A }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "E").WithArguments("ref").WithLocation(1, 17));
    }

    [Fact]
    public void Ref_OnDelegate_StillErrors()
    {
        var src = "public ref delegate void D();";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,26): error CS0106: The modifier 'ref' is not valid for this item
            // public ref delegate void D();
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "D").WithArguments("ref").WithLocation(1, 26));
    }

    [Fact]
    public void Ref_OnNamespace_Errors()
    {
        // The parser now consumes `ref` before `namespace` (so we can produce a targeted error
        // from the binder instead of a cascading parse error).  Namespaces don't accept
        // modifiers of any kind; expect the binder to surface that.
        var src = "ref namespace N { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.RefKeyword);
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
            // ref namespace N { }
            Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "ref").WithLocation(1, 1));
    }

    // ---------- ref as a misplaced modifier on members (ERR_RefNotMemberModifier) ----------

    [Fact]
    public void Ref_BeforeAccessibility_OnMethod_Errors()
    {
        // `ref public int M()` parses with `ref` as a modifier (because `public` is a reserved
        // modifier keyword that proves we're in a declaration head).  The binder then reports
        // ERR_RefNotMemberModifier on the `ref` token because `ref` belongs to the return type
        // for members.
        var src = """
            class C
            {
                ref public int M() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref public int M() => throw null;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5));
    }

    [Fact]
    public void Ref_InMiddleOfMethodModifierList_Errors()
    {
        var src = """
            class C
            {
                public ref static int M() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,12): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     public ref static int M() => throw null;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 12));
    }

    [Fact]
    public void Ref_BeforeAccessibility_OnMethod_CSharp14_SingleError()
    {
        // On older language versions the misplaced-modifier diagnostic fires immediately; we do
        // not additionally surface the feature-availability diagnostic because `ref` is not a
        // legal modifier on members in any version.
        var src = """
            class C
            {
                ref public int M() => throw null;
            }
            """;

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref public int M() => throw null;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5));
    }

    [Fact]
    public void Ref_BeforeAccessibility_OnField_Errors()
    {
        var src = """
            class C
            {
                ref public int x;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref public int x;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5),
            // (3,20): warning CS0649: Field 'C.x' is never assigned to, and will always have its default value 0
            //     ref public int x;
            Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("C.x", "0").WithLocation(3, 20));
    }

    [Fact]
    public void Ref_BeforeAccessibility_OnProperty_Errors()
    {
        var src = """
            class C
            {
                ref public int P => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref public int P => throw null;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5));
    }

    [Fact]
    public void Ref_BeforeReadonly_OnMethod_Errors()
    {
        // `ref readonly public int M()` -- both `ref` and `readonly` are misplaced modifiers
        // (the return type is plain `int`).  We report ERR_RefNotMemberModifier on `ref` and
        // ERR_BadMemberFlag on `readonly` (since `readonly` is not valid on methods).
        var src = """
            class C
            {
                ref readonly public int M() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref readonly public int M() => throw null;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5),
            // (3,29): error CS0106: The modifier 'readonly' is not valid for this item
            //     ref readonly public int M() => throw null;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "M").WithArguments("readonly").WithLocation(3, 29));
    }

    [Fact]
    public void Ref_OnEvent_Errors()
    {
        // `ref event ...` is syntactically unambiguous: `event` is a reserved keyword so this can
        // only be an event declaration.  The parser consumes `ref` as a modifier and the binder
        // reports ERR_RefNotMemberModifier on the `ref` token.
        var src = """
            class C
            {
                ref event System.Action E;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics(
            // (3,5): error CS9388: The 'ref' keyword is not a member modifier; it must appear immediately before the member's return type.
            //     ref event System.Action E;
            Diagnostic(ErrorCode.ERR_RefNotMemberModifier, "ref").WithLocation(3, 5),
            // (3,29): warning CS0067: The event 'C.E' is never used
            //     ref event System.Action E;
            Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(3, 29));
    }

    // ---------- return-type parsing must remain unchanged ----------

    [Fact]
    public void Ref_ReturnType_StillWorks_SimpleType()
    {
        // `ref int M()` must parse as a method with a `ref int` return type, not with `ref` as
        // a modifier.  Verify the shape explicitly.
        var src = """
            class C
            {
                public ref int M() => throw null;
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Ref_ReturnType_StillWorks_RefReadonly()
    {
        // `ref readonly int M()` must parse with `ref readonly int` as the return type.
        var src = """
            class C
            {
                public ref readonly int M() => throw null;
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Ref_ReturnType_StillWorks_TopLevelRef()
    {
        // `ref int M()` at the top of the modifier chain must still parse as a return type.
        var src = """
            class C
            {
                ref int M() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Ref_ReturnType_StillWorks_UserDefinedType()
    {
        var src = """
            class C { }
            class D
            {
                public ref C M() => throw null;
                public ref readonly C M2() => throw null;
            }
            """;

        CreateCompilation(src).VerifyDiagnostics();
    }

    // ---------- IsRefModifierInDeclarationHead branch coverage ----------

    [Fact]
    public void Branch_Ref_Struct()
    {
        // Canonical `ref struct` at the head of the modifier chain: ref eaten, no readonly, no
        // other modifiers, CheckDefinitelyAtMemberDeclarationHead reports type-decl head = true.
        var src = "ref struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics();
    }

    [Fact]
    public void Branch_Ref_Readonly_Struct()
    {
        // `ref readonly struct` -- readonly is eaten, struct reached.  Not a return-type form.
        // The binder still rejects the non-canonical `ref` position regardless of language version.
        var src = "ref readonly struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref readonly struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1));
    }

    [Fact]
    public void Branch_Ref_Reserved_Modifier()
    {
        // `ref` followed by a reserved-keyword modifier is unambiguously a modifier position.
        var src = "ref static partial class C { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.StaticKeyword);
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
    public void Branch_Ref_ContextualModifier_Chain()
    {
        // `ref` followed only by contextual modifiers (which could be identifiers) then a type
        // keyword commits to `ref` being a modifier via CheckDefinitelyAtMemberDeclarationHead.
        // The binder still rejects the non-canonical `ref` position.
        var src = "ref file partial struct S { }";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref file partial struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1));
    }

    [Fact]
    public void Branch_Ref_TopLevel_IntType_StaysReturnType()
    {
        // No modifiers after `ref`, next token is `int`: canonical return-type form.  `ref`
        // must NOT be consumed as a modifier.
        var src = """
            class C
            {
                ref int M() => throw null;
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
    public void Branch_Ref_TopLevel_UserTypeName_StaysReturnType()
    {
        // `ref T M()` where T is a user-defined type name must also stay as return type.
        var src = """
            class C
            {
                ref C M() => throw null;
            }
            """;

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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
    public void Branch_Ref_Union_Preview()
    {
        // `ref union` requires the unions feature-gated declaration-head recognition.
        var src = "ref public union U { }";

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PublicKeyword);
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
    public void Branch_Ref_Union_PreUnionsFeature_NotConsumedAsModifier()
    {
        // Without the unions feature, `union` does not start a declaration head, so `ref` here
        // cannot be committed to as a modifier and will attempt the return-type parse.
        var src = "ref public union U { }";

        // On C# 14 `union` is just an identifier; `ref public union` is not a well-formed
        // top-level construct.  We simply verify the compiler produces errors (not a crash).
        var comp = CreateCompilation(src, parseOptions: TestOptions.Regular14);
        Assert.NotEmpty(comp.GetDiagnostics());
    }

    // ---------- partial interaction: relaxed 'ref' with relaxed 'partial' ----------

    [Fact]
    public void Ref_WithPartial_NonCanonicalOrdering_Preview()
    {
        var src = "partial ref struct S { }";

        UsingTree(src, TestOptions.RegularPreview);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        // `partial` in first position here is the relaxed form; `ref` immediately before `struct`
        // is canonical.  We expect only the `partial`-relaxation feature gate to fire on older
        // language versions, not any `ref` diagnostic.
        CreateCompilation(src).VerifyDiagnostics();
        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // partial ref struct S { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 1));
    }

    [Fact]
    public void Ref_WithPartial_BothNonCanonical_TwoDiagnostics()
    {
        // `ref` errors unconditionally on non-canonical positions; `partial` is gated on the
        // relaxed-modifier-ordering feature so it only errors on older language versions.
        var src = "ref partial public struct S { }";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref partial public struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1));

        CreateCompilation(src, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref partial public struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1),
            // (1,5): error CS9202: Feature 'relaxed modifier ordering' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // ref partial public struct S { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "partial").WithArguments("relaxed modifier ordering").WithLocation(1, 5));
    }

    // ---------- permutation coverage: ref + other modifiers on struct ----------

    public static TheoryData<SyntaxKind> TypeDeclarationModifiersExceptRefPartialAndScoped()
    {
        // Modifiers that can legally appear on a struct (or are interesting to pair with `ref`
        // as a modifier) and whose parse shape is stable.  We exclude `ref`, `partial`, and
        // `scoped` because they have independent test coverage and/or special scanner rules.
        var data = new TheoryData<SyntaxKind>();
        foreach (var kind in new[]
        {
            SyntaxKind.PublicKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.FileKeyword,
        })
        {
            data.Add(kind);
        }
        return data;
    }

    [Theory, MemberData(nameof(TypeDeclarationModifiersExceptRefPartialAndScoped))]
    public void RefThenModifier_OnStruct(SyntaxKind modifier)
    {
        var src = $"ref {SyntaxFacts.GetText(modifier)} struct S {{ }}";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(modifier);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, MemberData(nameof(TypeDeclarationModifiersExceptRefPartialAndScoped))]
    public void ModifierThenRef_OnStruct(SyntaxKind modifier)
    {
        var src = $"{SyntaxFacts.GetText(modifier)} ref struct S {{ }}";

        UsingTree(src);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(modifier);
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    // ---------- `ref` must not be mistaken for an identifier ----------

    [Fact]
    public void Ref_BareToken_NotAnIdentifier()
    {
        // `ref` is a reserved keyword, so top-level `ref;` must be an error rather than anything
        // that looks like a declaration head.  This primarily protects against regressions in
        // the lookahead logic.
        var src = "ref;";

        var comp = CreateCompilation(src);
        Assert.NotEmpty(comp.GetDiagnostics());
    }

    // ---------- multi-modifier scenarios ----------

    [Fact]
    public void MultiModifier_RefFirst_ThreeOthers()
    {
        UsingTree("ref public static unsafe struct S { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.UnsafeKeyword);
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
    public void MultiModifier_RefBetweenContextualAndReserved()
    {
        UsingTree("file ref readonly struct S { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.FileKeyword);
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation("file ref readonly struct S { }").VerifyDiagnostics(
            // (1,6): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // file ref readonly struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 6));
    }

    [Fact]
    public void MultiModifier_RefLast_InChainOfContextuals()
    {
        // `partial ref struct S` inside a namespace -- `partial` is contextual, `ref` is
        // reserved, `struct` follows.  (Using a namespace keeps us out of the top-level
        // heuristics that govern `file` / other contextual modifiers.)
        var src = "namespace N { partial ref struct S { } }";

        UsingTree(src);
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
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RefKeyword);
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

        CreateCompilation(src).VerifyDiagnostics();
    }

    // ---------- duplicate ref ----------

    [Fact]
    public void Ref_Duplicated_OnStruct_Errors()
    {
        var src = "ref ref struct S { }";

        CreateCompilation(src).VerifyDiagnostics(
            // (1,1): error CS9389: The 'ref' modifier on a type declaration must appear immediately before 'struct', 'record struct', or 'union'.
            // ref ref struct S { }
            Diagnostic(ErrorCode.ERR_RefMisplacedOnType, "ref").WithLocation(1, 1),
            // (1,5): error CS1004: Duplicate 'ref' modifier
            // ref ref struct S { }
            Diagnostic(ErrorCode.ERR_DuplicateModifier, "ref").WithArguments("ref").WithLocation(1, 5));
    }

    #endregion ref modifier
}

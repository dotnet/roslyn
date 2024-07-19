// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordTests : CSharpTestBase
    {
        [Theory]
        [CombinatorialData]
        public void Field_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                class A { public static int field; }
                class B1 : A { int _f = {{identifier}}; }
                class B2 : A { object F() => {{identifier}}; }
                class C1 : A { object P => {{identifier}}; }
                class C2 : A { object P { get => {{identifier}}; } }
                class C3 : A { object P { get { return {{identifier}}; } } }
                class C4 : A { object P { set { {{identifier}} = 0; } } }
                class C5 : A { object P { get; set; } = {{identifier}}; }
                class D1 : A { object this[int i] => {{identifier}}; }
                class D2 : A { object this[int i] { get => {{identifier}}; } }
                class D3 : A { object this[int i] { get { return {{identifier}}; } } }
                class D4 : A { object this[int i] { set { {{identifier}} = 0; } } }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,28): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    // class C1 : A { object P => field; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 28),
                    // (5,34): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    // class C2 : A { object P { get => field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(5, 34),
                    // (6,40): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    // class C3 : A { object P { get { return field; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(6, 40),
                    // (7,33): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    // class C4 : A { object P { set { field = 0; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(7, 33));
            }
        }

        [Fact]
        public void Parameter_01()
        {
            string source = """
                class A
                {
                    object this[int field]
                    {
                        get { return field; }
                        set { _ = field; }
                    }
                }
                class B
                {
                    object this[int @field]
                    {
                        get { return @field; }
                        set { _ = @field; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            // No diagnostics expected for field in indexers.
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Event_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = $$"""
                #pragma warning disable 649
                using System;
                class C
                {
                    static object field;
                    event EventHandler E1
                    {
                        add { _ = field ?? @field; }
                        remove { _ = @field ?? field; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                #pragma warning disable 649
                interface I
                {
                    object P { get; set; }
                    object this[int i] { get; set; }
                }
                class C : I
                {
                    int field;
                    object I.P { get => {{identifier}}; set { _ = {{identifier}}; } }
                    object I.this[int i] { get => {{identifier}}; set { _ = {{identifier}}; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,25): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object I.P { get => field; set { _ = field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 25),
                    // (10,42): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object I.P { get => field; set { _ = field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 42));
            }
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                #pragma warning disable 649
                interface I
                {
                    object P { get; set; }
                    object this[int i] { get; set; }
                }
                class C : I
                {
                    int field;
                    object I.P { get => this.{{identifier}}; set { _ = this.{{identifier}}; } }
                    object I.this[int i] { get => this.{{identifier}}; set { _ = this.{{identifier}}; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_04(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                #pragma warning disable 649
                using System;
                interface I
                {
                    event EventHandler E;
                }
                class C : I
                {
                    int field;
                    event EventHandler I.E
                    {
                        add { _ = this.{{identifier}}; }
                        remove { _ = this.{{identifier}}; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_IdentifierNameSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8981
                class field { }
                class C
                {
                    object P1 { get { return new field(); } }
                    object P2 { get { return new @field(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_GenericNameSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8981
                class field<T> { }
                class C
                {
                    object P1 { get { return new field<object>(); } }
                    object P2 { get { return new @field<object>(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_Invocation(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    Func<object> field;
                    Func<object> P1 { get { _ = field(); return null; } }
                    Func<object> P2 { get { _ = @field(); return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (6,33): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     Func<object> P1 { get { _ = field(); return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(6, 33));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_Index(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649
                class C
                {
                    object[] field;
                    object[] P1 { get { _ = field[0]; return null; } }
                    object[] P2 { get { _ = @field[0]; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (5,29): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object[] P1 { get { _ = field[0]; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(5, 29));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_TupleElementSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 219
                class C
                {
                    object P3 { set { (int field, int value) t = default; } }
                    object P4 { set { (int @field, int @value) t = default; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_FromClauseSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System.Linq;
                class C
                {
                    object P1 { get { _ = from field in new int[0] select field; return null; } }
                    object P2 { get { _ = from @field in new int[0] select @field; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,59): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 59));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LetClauseSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System.Linq;
                class C
                {
                    object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    object P2 { get { _ = from i in new int[0] let @field = i select @field; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,69): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 69));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_JoinClauseSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System.Linq;
                class C
                {
                    object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    object P2 { get { _ = from x in new int[0] join @field in new int[0] on x equals @field select x; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,85): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 85));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_JoinIntoClauseSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System.Linq;
                class C
                {
                    object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    object P2 { get { _ = from x in new int[0] join y in new int[0] on x equals y into @field select @field; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,101): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 101));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_QueryContinuationSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                using System.Linq;
                class C
                {
                    object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    object P2 { get { _ = from x in new int[0] select x into @field select @field; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,75): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 75));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LocalFunctionStatementSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8321
                class C
                {
                    object P1 { get { object field() => null; return null; } }
                    object P2 { get { object @field() => null; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_VariableDeclaratorSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 219
                class C
                {
                    object P1 { get { int field = 0; return null; } }
                    object P2 { get { int @field = 0; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_SingleVariableDesignationSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    static void F(out object value) { value = null; }
                    object P1 { get { F(out var field); return null; } }
                    object P2 { get { F(out var @field); return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LabeledStatementSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 164
                class C
                {
                    object P1 { get { field: return null; } }
                    object P2 { get { @field: return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_ForEachStatementSyntax_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    object P1 { get { foreach (var field in new int[0]) { } return null; } }
                    object P2 { get { foreach (var @field in new int[0]) { } return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_ForEachStatementSyntax_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                    object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (3,44): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(3, 44),
                // (4,45): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(4, 45));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_CatchDeclarationSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 168
                using System;
                class C
                {
                    object P1 { get { try { } catch (Exception field) { } return null; } }
                    object P2 { get { try { } catch (Exception @field) { } return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_TypeParameterSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8321, 8981
                class C
                {
                    object P1 { get { void F1<field>() { } return null; } }
                    object P2 { get { void F2<@field>() { } return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_ParameterSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8321
                class C
                {
                    object P1 { get { object F1(object field) => field; return null; } }
                    object P2 { get { object F2(object @field) => @field; return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,50): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { object F1(object field) => field; return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 50));
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_AttributeTargetSpecifierSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = $$"""
                #pragma warning disable 657
                using System;
                class A : Attribute
                {
                }
                class C
                {
                    [field: A] object P1 { get; set; }
                    object P2 { [field: A] get; set; }
                    object P3 { [@field: A] get; set; }
                    [field: A] event EventHandler E1 { add { } remove { } }
                    event EventHandler E2 { [field: A] add { } remove { } }
                    event EventHandler E3 { [@field: A] add { } remove { } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Deconstruction(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    void Deconstruct(out object x, out object y) => throw null;
                    static object P1
                    {
                        set
                        {
                            object @field;
                            object @value;
                            (field, @value) = new C();
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (9,20): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             object @value;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(9, 20),
                // (10,14): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             (field, @value) = new C();
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 14));
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    static object field;
                    object P
                    {
                        set
                        {
                            Func<object> f;
                            f = () => field;
                            f = () => @field;
                            f = () => C.field;
                            f = () => C.@field;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (11,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             f = () => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(11, 23));
        }

        [Theory]
        [CombinatorialData]
        public void LocalFunction_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    static object field;
                    object P
                    {
                        set
                        {
                            object F1() => field;
                            object F2() => @field;
                            object F3() => C.field;
                            object F4() => C.@field;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (9,28): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             object F1() => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(9, 28));
        }

        [Fact]
        public void Lambda_Local()
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    object P
                    {
                        get
                        {
                            Func<object> f;
                            f = () => { object field = 1; return null; };
                            f = () => { object @field = 2; return null; };
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Lambda_Parameter_01()
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    object P
                    {
                        get
                        {
                            Func<object, object> f;
                            f = field => null;
                            f = @field => null;
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Lambda_Parameter_02()
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    object P
                    {
                        set
                        {
                            Action<object, object> a;
                            a = (field, @value) => { };
                            a = (@field, value) => { };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Lambda_Parameter_03()
        {
            string source = """
                #pragma warning disable 649
                using System;
                class C
                {
                    object P
                    {
                        set
                        {
                            Action<object, object> a;
                            a = delegate (object field, object @value) { };
                            a = delegate (object @field, object value) { };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void LocalFunction_Local()
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            object F1() { object field = 1; return null; };
                            object F2() { object @field = 2; return null; };
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void LocalFunction_Parameter()
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            object F1(object field) => null;
                            object F2(object @field) => null;
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                using System;
                class A : Attribute
                {
                    public A(string s) { }
                }
                class C
                {
                    const int field = 0;
                    [A(nameof({{identifier}}))]
                    object P
                    {
                        [A(nameof({{identifier}}))] get { return null; }
                        [A(nameof({{identifier}}))] set { }
                    }
                    [A(nameof({{identifier}}))]
                    event EventHandler E
                    {
                        [A(nameof({{identifier}}))] add { }
                        [A(nameof({{identifier}}))] remove { }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (!escapeIdentifier && languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (12,19): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //         [A(nameof(field))] get { return null; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(12, 19),
                    // (12,19): error CS8081: Expression does not have a name.
                    //         [A(nameof(field))] get { return null; }
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(12, 19),
                    // (13,19): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //         [A(nameof(field))] set { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(13, 19),
                    // (13,19): error CS8081: Expression does not have a name.
                    //         [A(nameof(field))] set { }
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(13, 19));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                using System;
                class A : Attribute
                {
                    public A(string s) { }
                }
                class C
                {
                    const int value = 0;
                    [A(nameof({{identifier}}))]
                    object P
                    {
                        [A(nameof({{identifier}}))] get { return null; }
                        [A(nameof({{identifier}}))] set { }
                    }
                    [A(nameof({{identifier}}))]
                    event EventHandler E
                    {
                        [A(nameof({{identifier}}))] add { }
                        [A(nameof({{identifier}}))] remove { }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_03(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                using System;
                class A : Attribute
                {
                    public A(string s) { }
                }
                class C
                {
                    const int value = 0;
                    object P
                    {
                        [param: A(nameof({{identifier}}))] set { }
                    }
                    event EventHandler E
                    {
                        [param: A(nameof({{identifier}}))] add { }
                        [param: A(nameof({{identifier}}))] remove { }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_LocalFunction_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@field" : "field";
            string source = $$"""
                #pragma warning disable 649, 8321
                using System;
                class A : Attribute
                {
                    public A(string s) { }
                }
                class C
                {
                    object P1
                    {
                        get
                        {
                            [A(nameof({{identifier}}))] void F1(int {{identifier}}) { }
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (13,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             [A(nameof(field))] void F1(int field) { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(13, 23),
                    // (13,23): error CS8081: Expression does not have a name.
                    //             [A(nameof(field))] void F1(int field) { }
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(13, 23),
                    // (21,23): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //             [A(nameof(value))] void F2(int value) { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(21, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (13,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             [A(nameof(field))] void F1(int field) { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(13, 23));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_LocalFunction_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string fieldIdentifier = escapeIdentifier ? "@field" : "field";
            string valueIdentifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                #pragma warning disable 649, 8321
                using System;
                [AttributeUsage(AttributeTargets.All)]
                class A : Attribute
                {
                    public A(string s) { }
                }
                class C
                {
                    event EventHandler E
                    {
                        add { void F1([A(nameof({{fieldIdentifier}}))] int {{fieldIdentifier}}) { } }
                        remove { void F2([A(nameof({{valueIdentifier}}))] int {{valueIdentifier}}) { } }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (13,36): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         remove { void F2([A(nameof(value))] int value) { } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(13, 36));
            }
        }

        [Fact]
        public void FieldInInitializer_01()
        {
            string source = """
                class C
                {
                    object P { get; } = F(field);
                    static object F(object value) => value;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,27): error CS0103: The name 'field' does not exist in the current context
                //     object P { get; } = F(field);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 27));
        }

        [Fact]
        public void FieldInInitializer_02()
        {
            string source = """
                class C
                {
                    object P { get => null; } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,33): error CS0103: The name 'field' does not exist in the current context
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 33));
        }

        [Fact]
        public void FieldInInitializer_03()
        {
            string source = """
                class C
                {
                    object P { set { } } = field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,28): error CS0103: The name 'field' does not exist in the current context
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "field").WithArguments("field").WithLocation(3, 28));
        }

        [Fact]
        public void Field_NameOf_01()
        {
            string source = """
                class C
                {
                    object P => nameof(field);
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P => nameof(field);
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(3, 24),
                // (3,24): error CS8081: Expression does not have a name.
                //     object P => nameof(field);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(3, 24));
        }

        [Fact]
        public void Field_NameOf_02()
        {
            string source = """
                class C
                {
                    object P { set { _ = nameof(field); } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,33): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P { set { _ = nameof(field); } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(3, 33),
                // (3,33): error CS8081: Expression does not have a name.
                //     object P { set { _ = nameof(field); } }
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(3, 33));
        }
    }
}

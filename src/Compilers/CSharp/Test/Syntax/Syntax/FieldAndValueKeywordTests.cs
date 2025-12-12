// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldKeywordTests : CSharpTestBase
    {
        private sealed class CSharp13_CSharp14_Preview()
            : CombinatorialValuesAttribute(LanguageVersion.CSharp13, LanguageVersion.CSharp14, LanguageVersion.Preview);

        [Theory]
        [CombinatorialData]
        public void Field_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion,
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
            if (!escapeIdentifier && languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,28): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    // class C1 : A { object P => field; }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 28),
                    // (5,34): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    // class C2 : A { object P { get => field; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(5, 34),
                    // (6,40): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    // class C3 : A { object P { get { return field; } } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(6, 40),
                    // (7,33): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    // class C4 : A { object P { set { field = 0; } } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(7, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Parameter_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            // No diagnostics expected for field in indexers.
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Event_01(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion,
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
            if (!escapeIdentifier && languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,25): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object I.P { get => field; set { _ = field; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(10, 25),
                    // (10,42): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object I.P { get => field; set { _ = field; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(10, 42));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_02(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion,
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion,
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,33): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     Func<object> P1 { get { _ = field(); return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(6, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_Index(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,29): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object[] P1 { get { _ = field[0]; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(5, 29));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_TupleElementSyntax(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "from field in new int[0]").WithArguments("14.0").WithLocation(4, 27),
                    // (4,59): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 59));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LetClauseSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,48): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "let field = i").WithArguments("14.0").WithLocation(4, 48),
                    // (4,69): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 69));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_JoinClauseSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,48): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "join field in new int[0] on x equals field").WithArguments("14.0").WithLocation(4, 48),
                    // (4,85): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 85));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_JoinIntoClauseSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,83): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "into field").WithArguments("14.0").WithLocation(4, 83),
                    // (4,101): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 101));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_QueryContinuationSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,57): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "into field select field").WithArguments("14.0").WithLocation(4, 57),
                    // (4,75): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 75));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LocalFunctionStatementSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,23): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { object field() => null; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "object field() => null;").WithArguments("14.0").WithLocation(4, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_VariableDeclaratorSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { int field = 0; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = 0").WithArguments("14.0").WithLocation(4, 27));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_SingleVariableDesignationSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,33): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { F(out var field); return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(4, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_LabeledStatementSyntax(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    object P1 { get { foreach (var field in new int[0]) { } return null; } }
                    object P2 { get { foreach (var @field in new int[0]) { } return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,23): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { foreach (var field in new int[0]) { } return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "foreach (var field in new int[0]) { }").WithArguments("14.0").WithLocation(3, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_ForEachStatementSyntax_02(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                    object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,37): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(3, 37),
                    // (3,44): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(3, 44),
                    // (4,45): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(4, 45));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,44): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(3, 44),
                    // (4,45): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(4, 45));
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_CatchDeclarationSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,37): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { try { } catch (Exception field) { } return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "(Exception field)").WithArguments("14.0").WithLocation(5, 37));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_UsingDeclarationSyntax_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class C
                {
                    object P1 { set { using (var field = GetDisposable()) { } } }
                    object P2 { set { using (var @field = GetDisposable()) { } } }
                    static IDisposable GetDisposable() => null;
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,34): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { set { using (var field = GetDisposable()) { } } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = GetDisposable()").WithArguments("14.0").WithLocation(4, 34));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_UsingDeclarationSyntax_02(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                using System;
                class C
                {
                    object P1 { set { using var field = GetDisposable(); } }
                    object P2 { set { using var @field = GetDisposable(); } }
                    static IDisposable GetDisposable() => null;
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,33): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { set { using var field = GetDisposable(); } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = GetDisposable()").WithArguments("14.0").WithLocation(4, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_TypeParameterSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8321, 8981
                class C
                {
                    object P1 { get { object F1<field>() { return default(@field); } return null; } }
                    object P2 { get { object F2<@field>() { return default(field); } return null; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,33): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { object F1<field>() { return default(@field); } return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(4, 33));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_ParameterSyntax(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,33): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { get { object F1(object field) => field; return null; } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "object field").WithArguments("14.0").WithLocation(4, 33),
                    // (4,50): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { object F1(object field) => field; return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 50));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_AttributeTargetSpecifierSyntax(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
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
        public void Deconstruction_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 168 // variable is declared but never used
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
                            (@field, value) = new C();
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,20): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //             object @value;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(10, 20),
                    // (11,14): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             (field, @value) = new C();
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(11, 14));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,20): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //             object @value;
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(10, 20));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Deconstruction_02(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 168 // variable is declared but never used
                class C
                {
                    void Deconstruct(out object x, out object y) => throw null;
                    object P1 { set { var (field, @value) = new C(); } }
                    object P2 { set { var (@field, value) = new C(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,28): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //     object P1 { set { var (field, @value) = new C(); } }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(5, 28),
                    // (5,35): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P1 { set { var (field, @value) = new C(); } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(5, 35),
                    // (6,36): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P2 { set { var (@field, value) = new C(); } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 36));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (5,35): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P1 { set { var (field, @value) = new C(); } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(5, 35),
                    // (6,36): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P2 { set { var (@field, value) = new C(); } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 36));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Local(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    object P
                    {
                        get
                        {
                            object field = 1;
                            return @field;
                        }
                        set
                        {
                            object @field = 2;
                            _ = field;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,9): warning CS9266: The 'get' accessor of property 'C.P' should use 'field' because the other accessor is using it.
                    //         get
                    Diagnostic(ErrorCode.WRN_AccessorDoesNotUseBackingField, "get").WithArguments("get", "C.P").WithLocation(5, 9),
                    // (7,20): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             object field = 1;
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = 1").WithArguments("14.0").WithLocation(7, 20),
                    // (13,17): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             _ = field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(13, 17));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void DeclarationExpression(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 219
                class C
                {
                    object P1
                    {
                        get
                        {
                            var (field, value) = (1, 2);
                            return field;
                        }
                    }
                    object P2
                    {
                        get
                        {
                            var (@field, @value) = (3, 4);
                            return @field;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (8,18): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             var (field, value) = (1, 2);
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(8, 18),
                    // (9,20): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             return field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(9, 20));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (11,23): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             f = () => field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(11, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void LocalFunction_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,28): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             object F1() => field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(9, 28));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_Local(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
                            f = () => { object field = 1; return field; };
                            f = () => { object @field = 2; return @field; };
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,32): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             f = () => { object field = 1; return field; };
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = 1").WithArguments("14.0").WithLocation(10, 32),
                    // (10,50): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             f = () => { object field = 1; return field; };
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(10, 50));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_Parameter_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
                            f = field => @field;
                            f = @field => field;
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,17): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             f = field => @field;
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(10, 17),
                    // (11,27): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             f = @field => field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(11, 27));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_Parameter_02(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,18): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             a = (field, @value) => { };
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field").WithArguments("14.0").WithLocation(10, 18));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Lambda_Parameter_03(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
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
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,27): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             a = delegate (object field, object @value) { };
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "object field").WithArguments("14.0").WithLocation(10, 27));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void LocalFunction_Local(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            object F1() { object field = 1; return field; };
                            object F2() { object @field = 2; return @field; };
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (8,34): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             object F1() { object field = 1; return field; };
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "field = 1").WithArguments("14.0").WithLocation(8, 34),
                    // (8,52): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             object F1() { object field = 1; return field; };
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(8, 52));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void LocalFunction_Parameter(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            object F1(object field) => @field;
                            object F2(object @field) => field;
                            return null;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (8,23): error CS9273: In language version preview, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             object F1(object field) => @field;
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "object field").WithArguments("14.0").WithLocation(8, 23),
                    // (9,41): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             object F2(object @field) => field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(9, 41));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void TypeParameter(
            [CombinatorialValues(LanguageVersion.CSharp13, LanguageVersion.CSharp14)] LanguageVersion languageVersion)
        {
            string source = """
                class C1<field>
                {
                    object P1 => default(@field);
                }
                class C2<@field>
                {
                    object P2 => default(field);
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (1,10): warning CS8981: The type name 'field' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class C1<field>
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "field").WithArguments("field").WithLocation(1, 10));
        }

        [Theory]
        [CombinatorialData]
        public void ParameterDefaultValue(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    const int field = 0;
                    object P
                    {
                        set
                        {
                            static void F1(int v = field) { }
                            static void F2(int v = @field) { }
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (9,36): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             static void F1(int v = field) { }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(9, 36),
                    // (9,36): error CS1736: Default parameter value for 'v' must be a compile-time constant
                    //             static void F1(int v = field) { }
                    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "field").WithArguments("v").WithLocation(9, 36));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_01(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion, bool escapeIdentifier)
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
            if (!escapeIdentifier && languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (12,19): warning CS9258: In language version 14.0, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //         [A(nameof(field))] get { return null; }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(12, 19),
                    // (12,19): error CS8081: Expression does not have a name.
                    //         [A(nameof(field))] get { return null; }
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(12, 19),
                    // (13,19): warning CS9258: In language version 14.0, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //         [A(nameof(field))] set { }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(13, 19),
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
        public void Attribute_LocalFunction(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion, bool escapeIdentifier)
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
                            [A(nameof({{identifier}}))]
                            void F1(int {{identifier}}) { }
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
            else if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (13,23): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //             [A(nameof(field))]
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(13, 23),
                    // (13,23): error CS8081: Expression does not have a name.
                    //             [A(nameof(field))]
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(13, 23),
                    // (14,21): error CS9273: In language version 14.0, 'field' is a keyword within a property accessor. Rename the variable or use the identifier '@field' instead.
                    //             void F1(int field) { }
                    Diagnostic(ErrorCode.ERR_VariableDeclarationNamedField, "int field").WithArguments("14.0").WithLocation(14, 21));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }

        [Fact]
        public void NameOf_01()
        {
            string source = """
                class C
                {
                    object P => nameof(field);
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS8081: Expression does not have a name.
                //     object P => nameof(field);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(3, 24));
        }

        [Fact]
        public void NameOf_02()
        {
            string source = """
                class C
                {
                    object P { set { _ = nameof(field); } }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,33): error CS8081: Expression does not have a name.
                //     object P { set { _ = nameof(field); } }
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(3, 33));
        }

        [Theory]
        [CombinatorialData]
        public void NameOf_03(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string source = """
                class C
                {
                    static int field;
                    object P => nameof(field);
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,16): warning CS0169: The field 'C.field' is never used
                    //     static int field;
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "field").WithArguments("C.field").WithLocation(3, 16),
                    // (4,24): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P => nameof(field);
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(4, 24),
                    // (4,24): error CS8081: Expression does not have a name.
                    //     object P => nameof(field);
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(4, 24));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (3,16): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value 0
                    //     static int field;
                    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "0").WithLocation(3, 16));
            }
        }

        [Theory]
        [CombinatorialData]
        public void BaseClassMember(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion)
        {
            string sourceA = """
                public class Base
                {
                    protected string field;
                }
                """;
            var comp = CreateCompilation(sourceA);
            var refA = comp.EmitToImageReference();

            string sourceB1 = """
                class Derived : Base
                {
                    string P => field; // synthesized backing field
                }
                """;
            comp = CreateCompilation(sourceB1, references: [refA], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (3,17): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     string P => field;
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(3, 17));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
            verify(comp, synthesizeField: languageVersion > LanguageVersion.CSharp13);

            string sourceB2 = """
                class Derived : Base
                {
                    string P => @field; // Base.field
                }
                """;
            comp = CreateCompilation(sourceB2, references: [refA], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
            verify(comp, synthesizeField: false);

            string sourceB3 = """
                class Derived : Base
                {
                    string P => this.field; // Base.field
                }
                """;
            comp = CreateCompilation(sourceB3, references: [refA], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
            verify(comp, synthesizeField: false);

            string sourceB4 = """
                class Derived : Base
                {
                    string P => base.field; // Base.field
                }
                """;
            comp = CreateCompilation(sourceB4, references: [refA], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
            verify(comp, synthesizeField: false);

            string sourceB5 = """
                class Derived : Base
                {
                #pragma warning disable 9258 // 'field' is a contextual keyword
                    string P => field; // synthesized backing field
                }
                """;
            comp = CreateCompilation(sourceB5, references: [refA], parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
            verify(comp, synthesizeField: languageVersion > LanguageVersion.CSharp13);

            static void verify(CSharpCompilation comp, bool synthesizeField)
            {
                var syntaxTree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(syntaxTree);
                var expr = syntaxTree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;

                var symbolInfo = model.GetSymbolInfo(expr);
                string expectedSymbol = synthesizeField ? "System.String Derived.<P>k__BackingField" : "System.String Base.field";
                Assert.Equal(expectedSymbol, symbolInfo.Symbol.ToTestDisplayString());

                var actualFields = comp.GetMember<NamedTypeSymbol>("Derived").GetMembers().Where(m => m.Kind == SymbolKind.Field).ToTestDisplayStrings();
                string[] expectedFields = synthesizeField ? ["System.String Derived.<P>k__BackingField"] : [];
                AssertEx.Equal(expectedFields, actualFields);
            }
        }

        [Theory]
        [CombinatorialData]
        public void Conditional(
            [CSharp13_CSharp14_Preview] LanguageVersion languageVersion,
            bool useDEBUG)
        {
            string source = """
                using System.Diagnostics;
                class C
                {
                    const int field = 0;
                    object P1 { get { M(field); return null; } }
                    object P2 { set { M(field); } }
                    [Conditional("DEBUG")]
                    static void M( object o) { }
                }
                """;
            var parseOptions = TestOptions.Regular.WithLanguageVersion(languageVersion);
            if (useDEBUG)
            {
                parseOptions = parseOptions.WithPreprocessorSymbols("DEBUG");
            }
            var comp = CreateCompilation(source, parseOptions: parseOptions);
            if (languageVersion > LanguageVersion.CSharp13)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,25): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P1 { get { M(field); return null; } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(5, 25),
                    // (6,25): warning CS9258: In language version preview, the 'field' keyword binds to a synthesized backing field for the property. To avoid generating a synthesized backing field, and to refer to the existing member, use 'this.field' or '@field' instead.
                    //     object P2 { set { M(field); } }
                    Diagnostic(ErrorCode.WRN_FieldIsAmbiguous, "field").WithArguments(languageVersion.ToDisplayString()).WithLocation(6, 25));
            }
            else
            {
                comp.VerifyEmitDiagnostics();
            }
        }
    }
}

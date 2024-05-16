// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FieldAndValueKeywordTests : CSharpTestBase
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
            if (escapeIdentifier || languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,28): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    // class C1 : A { object P => field; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 28),
                    // (5,34): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    // class C2 : A { object P { get => field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(5, 34),
                    // (6,40): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    // class C3 : A { object P { get { return field; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(6, 40),
                    // (7,33): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    // class C4 : A { object P { set { field = 0; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(7, 33));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Value_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                #pragma warning disable 649
                class A { public static int value; }
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
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Value_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                #pragma warning disable 649
                class A { public int value; }
                class C1 : A { object P => this.{{identifier}}; }
                class C2 : A { object P { get => this.{{identifier}}; } }
                class C3 : A { object P { get { return this.{{identifier}}; } } }
                class C4 : A { object P { set { this.{{identifier}} = 0; } } }
                class D1 : A { object this[int i] => this.{{identifier}}; }
                class D2 : A { object this[int i] { get => this.{{identifier}}; } }
                class D3 : A { object this[int i] { get { return this.{{identifier}}; } } }
                class D4 : A { object this[int i] { set { this.{{identifier}} = 0; } } }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier || languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (6,38): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                    // class C4 : A { object P { set { this.value = 0; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 38),
                    // (10,48): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                    // class D4 : A { object this[int i] { set { this.value = 0; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(10, 48));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Value_03(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            bool escapeIdentifier)
        {
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                class A
                {
                    object P { get { return null; } set { _ = {{identifier}}; } }
                    object this[int i] { get { return null; } set { _ = {{identifier}}; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
        }

        [Theory]
        [CombinatorialData]
        public void Keyword_01(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("field", "value")] string identifier,
            bool escapeIdentifier)
        {
            string source = $$"""
                namespace A.{{identifier}}.B
                {
                    class C { }
                }
                class D
                {
                    object P { set { _ = new A.{{(escapeIdentifier ? "@" : "") + identifier}}.B.C(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier || languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (7,32): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //     object P { set { _ = new A.field.B.C(); } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, identifier).WithArguments(identifier, "preview").WithLocation(7, 32));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Keyword_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("field", "value")] string identifier,
            bool escapeIdentifier)
        {
            string source = $$"""
                #pragma warning disable 649
                class C
                {
                    static object {{identifier}};
                    static object P1 { set { _ = C.{{(escapeIdentifier ? "@" : "") + identifier}}; } }
                    object P2 { init { _ = C.{{(escapeIdentifier ? "@" : "") + identifier}}; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion), targetFramework: TargetFramework.Net70);
            if (escapeIdentifier || languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (5,36): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //     static object P1 { set { _ = C.field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, identifier).WithArguments(identifier, "preview").WithLocation(5, 36),
                    // (6,30): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //     object P2 { init { _ = C.field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, identifier).WithArguments(identifier, "preview").WithLocation(6, 30));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Keyword_03(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 649
                class C
                {
                    object field;
                    object value;
                    object P
                    {
                        set
                        {
                            _ = nameof(field);
                            _ = nameof(@field);
                            _ = nameof(this.value);
                            _ = nameof(this.@value);
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,24): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //             _ = nameof(field);
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 24),
                    // (12,29): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                    //             _ = nameof(this.value);
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(12, 29));
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

        [Fact]
        public void Parameter_02()
        {
            string source = """
                class A
                {
                    object this[int value]
                    {
                        get { return value; }
                        set { _ = value; }
                    }
                }
                class B
                {
                    object this[int @value]
                    {
                        get { return @value; }
                        set { _ = @value; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (3,21): error CS0316: The parameter name 'value' conflicts with an automatically-generated parameter name
                //     object this[int value]
                Diagnostic(ErrorCode.ERR_DuplicateGeneratedName, "value").WithArguments("value").WithLocation(3, 21),
                // (6,19): error CS0229: Ambiguity between 'int value' and 'object value'
                //         set { _ = value; }
                Diagnostic(ErrorCode.ERR_AmbigMember, "value").WithArguments("int value", "object value").WithLocation(6, 19),
                // (6,19): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //         set { _ = value; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 19),
                // (11,21): error CS0316: The parameter name 'value' conflicts with an automatically-generated parameter name
                //     object this[int @value]
                Diagnostic(ErrorCode.ERR_DuplicateGeneratedName, "@value").WithArguments("value").WithLocation(11, 21),
                // (14,19): error CS0229: Ambiguity between 'int value' and 'object value'
                //         set { _ = @value; }
                Diagnostic(ErrorCode.ERR_AmbigMember, "@value").WithArguments("int value", "object value").WithLocation(14, 19));
        }

        [Fact]
        public void Local_01()
        {
            string source = """
                class C
                {
                    static object P
                    {
                        get { int field = 1; return field; }
                    }
                    static object Q
                    {
                        get { int @field = 3; return @field; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (5,19): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //         get { int field = 1; return field; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(5, 19),
                // (5,37): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //         get { int field = 1; return field; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(5, 37));
        }

        [Fact]
        public void Local_02()
        {
            string source = """
                class C
                {
                    static object P
                    {
                        set { int value = 2; _ = value; }
                    }
                    static object Q
                    {
                        set { int @value = 4; _ = @value; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (5,19): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         set { int value = 2; _ = value; }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(5, 19),
                // (5,19): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //         set { int value = 2; _ = value; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 2").WithArguments("value", "preview").WithLocation(5, 19),
                // (5,34): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //         set { int value = 2; _ = value; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(5, 34),
                // (9,19): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         set { int @value = 4; _ = @value; }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(9, 19));
        }

        // PROTOTYPE: Confirm field and value should not be contextual keywords in event accessors.
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
                    static object value;
                    event EventHandler E1
                    {
                        add { _ = field ?? @field; }
                        remove { _ = @field ?? field; }
                    }
                    event EventHandler E2
                    {
                        add { _ = C.value ?? C.@value; }
                        remove { _ = C.@value ?? C.value; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics();
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
                    static object value;
                    object P
                    {
                        set
                        {
                            Func<object> f;
                            f = () => field;
                            f = () => @field;
                            f = () => C.field;
                            f = () => C.@field;
                            f = () => value;
                            f = () => C.value;
                            f = () => @value;
                            f = () => C.@value;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (12,23): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //             f = () => field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(12, 23),
                    // (14,25): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //             f = () => C.field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(14, 25),
                    // (17,25): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                    //             f = () => C.value;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 25));
            }
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
                    static object value;
                    object P
                    {
                        set
                        {
                            object F1() => field;
                            object F2() => @field;
                            object F3() => C.field;
                            object F4() => C.@field;
                            object G1() { return value; }
                            object G2() { return C.value; }
                            object G3() { return @value; }
                            object G4() { return C.@value; }
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics();
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,28): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //             object F1() => field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 28),
                    // (12,30): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                    //             object F3() => C.field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(12, 30),
                    // (15,36): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                    //             object G2() { return C.value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(15, 36));
            }
        }

        [Fact]
        public void Lambda_Local_01()
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
                        set
                        {
                            Func<object> f;
                            f = () => { object value = 1; return value; };
                            f = () => { object @value = 2; return @value; };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (10,32): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             f = () => { object field = 1; return field; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(10, 32),
                // (10,50): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             f = () => { object field = 1; return field; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 50),
                // (17,32): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             f = () => { object value = 1; return value; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 1").WithArguments("value", "preview").WithLocation(17, 32),
                // (17,50): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             f = () => { object value = 1; return value; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 50));
        }

        [Fact]
        public void Lambda_Local_02()
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
                            Action a;
                            a = () => { object field = 1; };
                            a = () => { object @field = 2; };
                            return null;
                        }
                        set
                        {
                            Action a;
                            a = () => { object value = 1; };
                            a = () => { object @value = 2; };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (10,32): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             a = () => { object field = 1; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(10, 32),
                // (17,32): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             a = () => { object value = 1; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 1").WithArguments("value", "preview").WithLocation(17, 32));
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
                            f = field => field;
                            f = @field => @field;
                            return null;
                        }
                        set
                        {
                            Func<object, object> f;
                            f = value => value;
                            f = @value => @value;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (10,17): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             f = field => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 17),
                // (10,26): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             f = field => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 26),
                // (17,17): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             f = value => value;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 17),
                // (17,26): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             f = value => value;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 26));
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
                        get
                        {
                            Action<object> a;
                            a = field => { };
                            a = @field => { };
                            return null;
                        }
                        set
                        {
                            Action<object> a;
                            a = value => { };
                            a = @value => { };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (10,17): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             a = field => { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 17),
                // (17,17): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             a = value => { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 17));
        }

        [Fact]
        public void LocalFunction_Local_01()
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
                        set
                        {
                            object G1() { object value = 1; return value; };
                            object G2() { object @value = 2; return @value; };
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (8,34): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             object F1() { object field = 1; return field; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(8, 34),
                // (8,52): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             object F1() { object field = 1; return field; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(8, 52),
                // (14,34): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             object G1() { object value = 1; return value; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 1").WithArguments("value", "preview").WithLocation(14, 34),
                // (14,52): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             object G1() { object value = 1; return value; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(14, 52));
        }

        [Fact]
        public void LocalFunction_Local_02()
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            void F1() { object field = 1; }
                            void F2() { object @field = 1; }
                            return null;
                        }
                        set
                        {
                            void G1() { object value = 1; }
                            void G2() { object @value = 1; }
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (8,32): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             void F1() { object field = 1; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(8, 32),
                // (14,32): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             void G1() { object value = 1; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 1").WithArguments("value", "preview").WithLocation(14, 32));
        }

        [Fact]
        public void LocalFunction_Parameter_01()
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            object F1(object field) => field;
                            object F2(object @field) => @field;
                            return null;
                        }
                        set
                        {
                            object G1(object value) => value;
                            object G2(object @value) => @value;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (8,23): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             object F1(object field) => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field").WithArguments("field", "preview").WithLocation(8, 23),
                // (8,40): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             object F1(object field) => field;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(8, 40),
                // (14,23): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             object G1(object value) => value;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object value").WithArguments("value", "preview").WithLocation(14, 23),
                // (14,40): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             object G1(object value) => value;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(14, 40));
        }

        [Fact]
        public void LocalFunction_Parameter_02()
        {
            string source = """
                #pragma warning disable 649, 8321
                class C
                {
                    object P
                    {
                        get
                        {
                            void F1(object field) { }
                            void F2(object @field) { }
                            return null;
                        }
                        set
                        {
                            void G1(object value) { }
                            void G2(object @value) { }
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular12);
            comp.VerifyEmitDiagnostics(
                // (8,21): info CS9248: 'field' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@field' to avoid a breaking change when compiling with language version preview or later.
                //             void F1(object field) { }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field").WithArguments("field", "preview").WithLocation(8, 21),
                // (14,21): info CS9248: 'value' is a contextual keyword, with a specific meaning, starting in language version preview. Use '@value' to avoid a breaking change when compiling with language version preview or later.
                //             void G1(object value) { }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object value").WithArguments("value", "preview").WithLocation(14, 21));
        }
    }
}

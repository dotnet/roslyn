// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (languageVersion > LanguageVersion.CSharp12)
            {
                // PROTOTYPE: Should report: CS0236: A field initializer cannot reference the non-static field, method, or property 'field'
                comp.VerifyEmitDiagnostics(
                    // (8,41): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                    // class C5 : A { object P { get; set; } = field; }
                    Diagnostic(ErrorCode.WRN_AssignmentToSelf, "field").WithLocation(8, 41));
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
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,38): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    // class C4 : A { object P { set { this.value = 0; } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 38),
                    // (10,48): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    // class D4 : A { object this[int i] { set { this.value = 0; } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(10, 48));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (6,38): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    // class C4 : A { object P { set { this.value = 0; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 38),
                    // (10,48): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
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
                // (6,19): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //         set { _ = value; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 19),
                // (11,21): error CS0316: The parameter name 'value' conflicts with an automatically-generated parameter name
                //     object this[int @value]
                Diagnostic(ErrorCode.ERR_DuplicateGeneratedName, "@value").WithArguments("value").WithLocation(11, 21),
                // (14,19): error CS0229: Ambiguity between 'int value' and 'object value'
                //         set { _ = @value; }
                Diagnostic(ErrorCode.ERR_AmbigMember, "@value").WithArguments("int value", "object value").WithLocation(14, 19));
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
                    event EventHandler E3
                    {
                        add { _ = value ?? @value; }
                        remove { _ = @value ?? value; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (14,21): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //         add { _ = C.value ?? C.@value; }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(14, 21),
                    // (15,36): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //         remove { _ = C.@value ?? C.value; }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(15, 36));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (14,21): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         add { _ = C.value ?? C.@value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(14, 21),
                    // (15,36): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         remove { _ = C.@value ?? C.value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(15, 36));
            }
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
            string identifier = escapeIdentifier ? "@value" : "value";
            string source = $$"""
                #pragma warning disable 649
                interface I
                {
                    object P { get; set; }
                    object this[int i] { get; set; }
                }
                class C : I
                {
                    int value;
                    object I.P { get => this.{{identifier}}; set { _ = this.{{identifier}}; } }
                    object I.this[int i] { get => this.{{identifier}}; set { _ = this.{{identifier}}; } }
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
                    // (10,52): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object I.P { get => this.value; set { _ = this.value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(10, 52),
                    // (11,62): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object I.this[int i] { get => this.value; set { _ = this.value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(11, 62));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,52): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object I.P { get => this.value; set { _ = this.value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(10, 52),
                    // (11,62): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object I.this[int i] { get => this.value; set { _ = this.value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(11, 62));
            }
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_03(
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
            if (escapeIdentifier)
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (10,30): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object I.P { get => this.field; set { _ = this.field; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(10, 30),
                    // (10,52): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object I.P { get => this.field; set { _ = this.field; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(10, 52));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,30): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object I.P { get => this.field; set { _ = this.field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 30),
                    // (10,52): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object I.P { get => this.field; set { _ = this.field; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 52));
            }
        }

        [Theory]
        [CombinatorialData]
        public void ExplicitImplementation_04(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion,
            [CombinatorialValues("field", "value")] string identifier,
            bool escapeIdentifier)
        {
            string qualifiedIdentifier = (escapeIdentifier ? "@" : "") + identifier;
            string source = $$"""
                #pragma warning disable 649
                using System;
                interface I
                {
                    event EventHandler E;
                }
                class C : I
                {
                    int {{identifier}};
                    event EventHandler I.E
                    {
                        add { _ = this.{{qualifiedIdentifier}}; }
                        remove { _ = this.{{qualifiedIdentifier}}; }
                    }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (escapeIdentifier || identifier == "field")
            {
                comp.VerifyEmitDiagnostics();
            }
            else if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (12,24): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //         add { _ = this.value; }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(12, 24),
                    // (13,27): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //         remove { _ = this.value; }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(13, 27));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (12,24): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         add { _ = this.value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(12, 24),
                    // (13,27): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         remove { _ = this.value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(13, 27));
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_IdentifierNameSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8981
                class field { }
                class value { }
                class C
                {
                    object P1 { get { return new field(); } }
                    object P2 { get { return new @field(); } }
                    object P3 { set { _ = new value(); } }
                    object P4 { set { _ = new @value(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,34): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { return new field(); } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(6, 34),
                    // (8,31): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = new value(); } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(8, 31));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (6,34): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { return new field(); } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(6, 34),
                    // (8,31): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = new value(); } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(8, 31));
            }
        }

        [Theory]
        [CombinatorialData]
        public void IdentifierToken_GenericNameSyntax(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion)
        {
            string source = """
                #pragma warning disable 8981
                class field<T> { }
                class value<T> { }
                class C
                {
                    object P1 { get { return new field<object>(); } }
                    object P2 { get { return new @field<object>(); } }
                    object P3 { set { _ = new value<object>(); } }
                    object P4 { set { _ = new @value<object>(); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (6,34): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { return new field<object>(); } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(6, 34),
                    // (8,31): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = new value<object>(); } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(8, 31));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (6,34): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { return new field<object>(); } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field<object>").WithArguments("field", "preview").WithLocation(6, 34),
                    // (8,31): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = new value<object>(); } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value<object>").WithArguments("value", "preview").WithLocation(8, 31));
            }
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
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,28): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P3 { set { (int field, int value) t = default; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 28),
                    // (4,39): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { (int field, int value) t = default; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(4, 39));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,24): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P3 { set { (int field, int value) t = default; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "int field").WithArguments("field", "preview").WithLocation(4, 24),
                    // (4,35): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { (int field, int value) t = default; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "int value").WithArguments("value", "preview").WithLocation(4, 35));
            }
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
                    object P3 { set { _ = from value in new int[0] select value; } }
                    object P4 { set { _ = from @value in new int[0] select @value; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,32): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 32),
                    // (6,32): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = from value in new int[0] select value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 32),
                    // (6,32): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from value in new int[0] select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 32),
                    // (7,32): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from @value in new int[0] select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 32));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "from field in new int[0]").WithArguments("field", "preview").WithLocation(4, 27),
                    // (4,59): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from field in new int[0] select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 59),
                    // (6,27): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from value in new int[0] select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "from value in new int[0]").WithArguments("value", "preview").WithLocation(6, 27),
                    // (6,32): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from value in new int[0] select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 32),
                    // (6,59): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from value in new int[0] select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 59),
                    // (7,32): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from @value in new int[0] select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 32));
            }
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
                    object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    object P4 { set { _ = from i in new int[0] let @value = i select @value; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,52): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 52),
                    // (6,52): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 52),
                    // (6,52): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 52),
                    // (7,52): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from i in new int[0] let @value = i select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 52));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,48): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "let field = i").WithArguments("field", "preview").WithLocation(4, 48),
                    // (4,69): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from i in new int[0] let field = i select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 69),
                    // (6,48): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "let value = i").WithArguments("value", "preview").WithLocation(6, 48),
                    // (6,52): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 52),
                    // (6,69): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from i in new int[0] let value = i select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 69),
                    // (7,52): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from i in new int[0] let @value = i select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 52));
            }
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
                    object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    object P4 { set { _ = from x in new int[0] join @value in new int[0] on x equals @value select x; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,53): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 53),
                    // (6,53): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 53),
                    // (6,53): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 53),
                    // (7,53): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] join @value in new int[0] on x equals @value select x; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 53));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,48): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "join field in new int[0] on x equals field").WithArguments("field", "preview").WithLocation(4, 48),
                    // (4,85): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join field in new int[0] on x equals field select x; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 85),
                    // (6,48): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "join value in new int[0] on x equals value").WithArguments("value", "preview").WithLocation(6, 48),
                    // (6,53): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 53),
                    // (6,85): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join value in new int[0] on x equals value select x; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 85),
                    // (7,53): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] join @value in new int[0] on x equals @value select x; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 53));
            }
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
                    object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    object P4 { set { _ = from x in new int[0] join y in new int[0] on x equals y into @value select @value; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,88): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 88),
                    // (6,88): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 88),
                    // (6,88): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 88),
                    // (7,88): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] join y in new int[0] on x equals y into @value select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 88));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,83): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "into field").WithArguments("field", "preview").WithLocation(4, 83),
                    // (4,101): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] join y in new int[0] on x equals y into field select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 101),
                    // (6,83): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "into value").WithArguments("value", "preview").WithLocation(6, 83),
                    // (6,88): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 88),
                    // (6,101): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] join y in new int[0] on x equals y into value select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 101),
                    // (7,88): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] join y in new int[0] on x equals y into @value select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 88));
            }
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
                    object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    object P4 { set { _ = from x in new int[0] select x into @value select @value; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,62): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 62),
                    // (6,62): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 62),
                    // (6,62): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 62),
                    // (7,62): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] select x into @value select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 62));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,57): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "into field select field").WithArguments("field", "preview").WithLocation(4, 57),
                    // (4,75): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { _ = from x in new int[0] select x into field select field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 75),
                    // (6,57): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "into value select value").WithArguments("value", "preview").WithLocation(6, 57),
                    // (6,62): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "value").WithArguments("value").WithLocation(6, 62),
                    // (6,75): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { _ = from x in new int[0] select x into value select value; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 75),
                    // (7,62): error CS1931: The range variable 'value' conflicts with a previous declaration of 'value'
                    //     object P4 { set { _ = from x in new int[0] select x into @value select @value; } }
                    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "@value").WithArguments("value").WithLocation(7, 62));
            }
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
                    object P3 { set { void value() { } } }
                    object P4 { set { void @value() { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,30): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { object field() => null; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 30),
                    // (6,28): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { void value() { } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 28),
                    // (6,28): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { void value() { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 28),
                    // (7,28): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { void @value() { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(7, 28));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { object field() => null; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field() => null;").WithArguments("field", "preview").WithLocation(4, 23),
                    // (6,23): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { void value() { } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "void value() { }").WithArguments("value", "preview").WithLocation(6, 23),
                    // (6,28): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { void value() { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 28),
                    // (7,28): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { void @value() { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(7, 28));
            }
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
                    object P3 { set { int value = 0; } }
                    object P4 { set { int @value = 0; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { int field = 0; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 27),
                    // (6,27): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { int value = 0; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 27),
                    // (6,27): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { int value = 0; } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 27),
                    // (7,27): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { int @value = 0; } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(7, 27));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,27): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { int field = 0; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 0").WithArguments("field", "preview").WithLocation(4, 27),
                    // (6,27): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { int value = 0; } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 27),
                    // (6,27): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { int value = 0; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 0").WithArguments("value", "preview").WithLocation(6, 27),
                    // (7,27): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { int @value = 0; } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(7, 27));
            }
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
                    object P3 { set { F(out var value); } }
                    object P4 { set { F(out var @value); } }
                    object P5 { set { F(out value); } }
                    object P6 { set { F(out @value); } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (4,33): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { F(out var field); return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 33),
                // (6,33): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P3 { set { F(out var value); } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(6, 33),
                // (6,33): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //     object P3 { set { F(out var value); } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 33),
                // (7,33): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P4 { set { F(out var @value); } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(7, 33));
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
                    object P3 { set { value: return; } }
                    object P4 { set { @value: return; } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,23): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { field: return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 23),
                    // (6,23): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { value: return; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { field: return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field: return null;").WithArguments("field", "preview").WithLocation(4, 23),
                    // (6,23): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { value: return; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value: return;").WithArguments("value", "preview").WithLocation(6, 23));
            }
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
                    object P3 { set { foreach (var value in new int[0]) { } } }
                    object P4 { set { foreach (var @value in new int[0]) { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyEmitDiagnostics(
                // (3,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { get { foreach (var field in new int[0]) { } return null; } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "foreach (var field in new int[0]) { }").WithArguments("field", "preview").WithLocation(3, 23),
                // (5,23): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //     object P3 { set { foreach (var value in new int[0]) { } } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "foreach (var value in new int[0]) { }").WithArguments("value", "preview").WithLocation(5, 23),
                // (5,36): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P3 { set { foreach (var value in new int[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(5, 36),
                // (6,36): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P4 { set { foreach (var @value in new int[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(6, 36));
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
                // (3,37): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(3, 37),
                // (3,44): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P1 { set { foreach (var (field, @value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(3, 44),
                // (4,45): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //     object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(4, 45),
                // (4,45): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //     object P2 { set { foreach (var (@field, value) in new (int, int)[0]) { } } }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(4, 45));
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
                    object P3 { set { try { } catch (Exception value) { } } }
                    object P4 { set { try { } catch (Exception @value) { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (5,48): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { try { } catch (Exception field) { } return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(5, 48),
                    // (7,48): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { try { } catch (Exception value) { } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(7, 48),
                    // (7,48): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { try { } catch (Exception value) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(7, 48),
                    // (8,48): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { try { } catch (Exception @value) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(8, 48));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (5,37): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { try { } catch (Exception field) { } return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "(Exception field)").WithArguments("field", "preview").WithLocation(5, 37),
                    // (7,37): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { try { } catch (Exception value) { } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "(Exception value)").WithArguments("value", "preview").WithLocation(7, 37),
                    // (7,48): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P3 { set { try { } catch (Exception value) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "value").WithArguments("value").WithLocation(7, 48),
                    // (8,48): error CS0136: A local or parameter named 'value' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                    //     object P4 { set { try { } catch (Exception @value) { } } }
                    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "@value").WithArguments("value").WithLocation(8, 48));
            }
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
                    object P3 { set { void F3<value>() { } } }
                    object P4 { set { void F4<@value>() { } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,31): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { void F1<field>() { } return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 31),
                    // (6,31): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { void F3<value>() { } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 31));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,31): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { void F1<field>() { } return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 31),
                    // (6,31): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { void F3<value>() { } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 31));
            }
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
                    object P3 { set { object F3(object value) { return value; } } }
                    object P4 { set { object F4(object @value) { return @value; } } }
                }
                """;
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            if (languageVersion > LanguageVersion.CSharp12)
            {
                comp.VerifyEmitDiagnostics(
                    // (4,40): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //     object P1 { get { object F1(object field) => field; return null; } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 40),
                    // (6,40): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //     object P3 { set { object F3(object value) { return value; } } }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(6, 40));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (4,33): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { object F1(object field) => field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field").WithArguments("field", "preview").WithLocation(4, 33),
                    // (4,50): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //     object P1 { get { object F1(object field) => field; return null; } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(4, 50),
                    // (6,33): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { object F3(object value) { return value; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object value").WithArguments("value", "preview").WithLocation(6, 33),
                    // (6,56): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //     object P3 { set { object F3(object value) { return value; } } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(6, 56));
            }
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
                            (@field, value) = new C();
                        }
                    }
                    static object P2
                    {
                        set
                        {
                            (value, @value) = new C();
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
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 14),
                // (11,22): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             (@field, value) = new C();
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(11, 22));
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
                comp.VerifyEmitDiagnostics(
                    // (14,25): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //             f = () => C.field;
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(14, 25),
                    // (17,25): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //             f = () => C.value;
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(17, 25));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (12,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             f = () => field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(12, 23),
                    // (14,25): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             f = () => C.field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(14, 25),
                    // (17,25): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
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
                comp.VerifyEmitDiagnostics(
                    // (12,30): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                    //             object F3() => C.field;
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(12, 30),
                    // (15,36): error CS9259: 'value' cannot be used as an identifier in this context; use '@value' instead.
                    //             object G2() { return C.value; }
                    Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "value").WithArguments("value").WithLocation(15, 36));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (10,28): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             object F1() => field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 28),
                    // (12,30): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             object F3() => C.field;
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(12, 30),
                    // (15,36): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //             object G2() { return C.value; }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(15, 36));
            }
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
                // (10,32): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             f = () => { object field = 1; return null; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(10, 32),
                // (17,32): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
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
                            f = field => null;
                            f = @field => null;
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
                // (10,17): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             f = field => null;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 17),
                // (17,17): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             a = value => { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(17, 17));
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
            comp.VerifyEmitDiagnostics(
                // (10,18): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             a = (field, @value) => { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(10, 18),
                // (11,26): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             a = (@field, value) => { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(11, 26));
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
            comp.VerifyEmitDiagnostics(
                // (10,27): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             a = delegate (object field, object @value) { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field").WithArguments("field", "preview").WithLocation(10, 27),
                // (11,42): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             a = delegate (object @field, object value) { };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object value").WithArguments("value", "preview").WithLocation(11, 42));
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
                // (8,34): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             object F1() { object field = 1; return null; };
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field = 1").WithArguments("field", "preview").WithLocation(8, 34),
                // (14,32): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             void G1() { object value = 1; }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value = 1").WithArguments("value", "preview").WithLocation(14, 32));
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
                // (8,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                //             object F1(object field) => null;
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object field").WithArguments("field", "preview").WithLocation(8, 23),
                // (14,21): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                //             void G1(object value) { }
                Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "object value").WithArguments("value", "preview").WithLocation(14, 21));
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
            comp.VerifyEmitDiagnostics();
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
            string fieldIdentifier = escapeIdentifier ? "@field" : "field";
            string valueIdentifier = escapeIdentifier ? "@value" : "value";
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
                            [A(nameof({{fieldIdentifier}}))] void F1(int @field) { }
                            return null;
                        }
                    }
                    object P2
                    {
                        set
                        {
                            [A(nameof({{valueIdentifier}}))] void F2(int @value) { }
                        }
                    }
                    object P3
                    {
                        set
                        {
                            [A(nameof({{valueIdentifier}}))] void F3() { }
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
                    // (13,23): error CS8081: Expression does not have a name.
                    //             [A(nameof(field))] void F1(int @field) { }
                    Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(13, 23));
            }
            else
            {
                comp.VerifyEmitDiagnostics(
                    // (13,23): info CS9258: 'field' is a contextual keyword in property accessors starting in language version preview. Use '@field' instead.
                    //             [A(nameof(field))] void F1(int @field) { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "field").WithArguments("field", "preview").WithLocation(13, 23),
                    // (21,23): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //             [A(nameof(value))] void F2(int @value) { }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(21, 23));
            }
        }

        [Theory]
        [CombinatorialData]
        public void Attribute_LocalFunction_02(
            [CombinatorialValues(LanguageVersion.CSharp12, LanguageVersion.Preview)] LanguageVersion languageVersion, bool escapeIdentifier)
        {
            string fieldIdentifier = escapeIdentifier ? "@field" : "field";
            string valueIdentifier = escapeIdentifier ? "@value" : "value";
            string source = $$$"""
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
                        add { void F1([A(nameof({{{fieldIdentifier}}}))] int @field) { } }
                        remove { void F2([A(nameof({{{valueIdentifier}}}))] int @value) { } }
                    }
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
                    // (13,36): info CS9258: 'value' is a contextual keyword in property accessors starting in language version preview. Use '@value' instead.
                    //         remove { void F2([A(nameof(value))] int @value) { } }
                    Diagnostic(ErrorCode.INF_IdentifierConflictWithContextualKeyword, "value").WithArguments("value", "preview").WithLocation(13, 36));
            }
        }

        [Fact]
        public void FieldInInitializer_01()
        {
            string source = """
                class C
                {
                    object P { get; } = field;
                }
                """;
            // PROTOTYPE: Should report: CS0236: A field initializer cannot reference the non-static field, method, or property 'field'
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,25): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //     object P { get; } = field;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "field").WithLocation(3, 25));
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
            // PROTOTYPE: Should report: CS0236: A field initializer cannot reference the non-static field, method, or property 'field'
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,33): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //     object P { get => null; } = field;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "field").WithLocation(3, 33));
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
            // PROTOTYPE: Should report: CS0236: A field initializer cannot reference the non-static field, method, or property 'field'
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8050: Only auto-implemented properties can have initializers.
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.ERR_InitializerOnNonAutoProperty, "P").WithLocation(3, 12),
                // (3,28): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //     object P { set { } } = field;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "field").WithLocation(3, 28));
        }

        [Fact]
        public void FieldInInitializer_04()
        {
            string source = """
                class C
                {
                    object P { get; } = F(field);
                    static object F(object value) => value;
                }
                """;
            // PROTOTYPE: Should report: CS0236: A field initializer cannot reference the non-static field, method, or property 'field'
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void Field_02()
        {
            string source = """
                class C
                {
                    public object P => field = 1;
                    public object Q { get => field = 2; }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        System.Console.WriteLine((c.P, c.Q));
                    }
                }
                """;
            // PROTOTYPE: Should succeed. Use CompileAndVerify(), check expectedOutput,
            // and verify IL for C.P.get and C.Q.get.
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,24): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     public object P => field = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(3, 24),
                // (4,30): error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
                //     public object Q { get => field = 2; }
                Diagnostic(ErrorCode.ERR_AssgReadonly, "field").WithLocation(4, 30));
        }

        [Fact]
        public void Field_03()
        {
            string source = """
                class C
                {
                    public object P => Initialize(out field, 1);
                    public object Q { get => Initialize(out field, 2); }
                    static object Initialize(out object field, object value)
                    {
                        field = value;
                        return field;
                    }
                }
                class Program
                {
                    static void Main()
                    {
                        var c = new C();
                        System.Console.WriteLine((c.P, c.Q));
                    }
                }
                """;
            // PROTOTYPE: Should succeed. Use CompileAndVerify(), check expectedOutput,
            // and verify IL for C.P.get and C.Q.get.
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,39): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //     public object P => Initialize(out field, 1);
                Diagnostic(ErrorCode.ERR_RefReadonly, "field").WithLocation(3, 39),
                // (4,45): error CS0192: A readonly field cannot be used as a ref or out value (except in a constructor)
                //     public object Q { get => Initialize(out field, 2); }
                Diagnostic(ErrorCode.ERR_RefReadonly, "field").WithLocation(4, 45));
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
                    object P { get; set; } = nameof(field);
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,37): error CS8081: Expression does not have a name.
                //     object P { get; set; } = nameof(field);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "field").WithLocation(3, 37));
        }

        [Fact]
        public void Field_NameOf_03()
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

        [Fact]
        public void FieldReference_01()
        {
            string source = """
                class C
                {
                    static C _other = new();
                    object P => _other.field;
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (4,24): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                //     object P => _other.field;
                Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(4, 24));
        }

        [Fact]
        public void FieldReference_02()
        {
            string source = """
                class C
                {
                    C P
                    {
                        get { return null; }
                        set { field = value.field; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,29): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                //         set { field = value.field; }
                Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(6, 29));
        }

        [Fact]
        public void FieldReference_03()
        {
            string source = """
                class C
                {
                    int P
                    {
                        set { _ = this is { field: 0 }; }
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (5,29): error CS9259: 'field' cannot be used as an identifier in this context; use '@field' instead.
                //         set { _ = this is { field: 0 }; }
                Diagnostic(ErrorCode.ERR_ContextualKeywordAsIdentifier, "field").WithArguments("field").WithLocation(5, 29));
        }

        [Fact]
        public void RefProperty()
        {
            string source = """
                class C
                {
                    ref int P => ref field;
                }
                """;
            // PROTOTYPE: Should report: error CS8145: Auto-implemented properties cannot return by reference
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }
    }
}

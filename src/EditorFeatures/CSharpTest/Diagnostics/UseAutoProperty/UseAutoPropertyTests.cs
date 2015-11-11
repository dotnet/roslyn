// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UseAutoProperty;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseAutoProperty
{
    public class UseAutoPropertyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(
                new UseAutoPropertyAnalyzer(), new UseAutoPropertyCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterFromField()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } } }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestCSharp5_1()
        {
            await TestAsync(
@"class Class { [|int i|]; public int P { get { return i; } } }",
@"class Class { public int P { get; private set; } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestCSharp5_2()
        {
            await TestMissingAsync(
@"class Class { [|readonly int i|]; int P { get { return i; } } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestInitializer()
        {
            await TestAsync(
@"class Class { [|int i = 1|]; int P { get { return i; } } }",
@"class Class { int P { get; } = 1; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestInitializer_CSharp5()
        {
            await TestMissingAsync(
@"class Class { [|int i = 1|]; int P { get { return i; } } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterFromProperty()
        {
            await TestAsync(
@"class Class { int i; [|int P { get { return i; } }|] }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleSetter()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { set { i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetter()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } set { i = value; } } }",
@"class Class { int P { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleGetterWithThis()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return this.i; } } }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSingleSetterWithThis()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { set { this.i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetterWithThis()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return this.i; } set { this.i = value; } } }",
@"class Class { int P { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterWithMutipleStatements()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { get { ; return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSetterWithMutipleStatements()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { set { ; i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement()
        {
            await TestMissingAsync(@"class Class { [|int i|]; int P { get { return i; } set { ; i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestGetterAndSetterUseDifferentFields()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int j; int P { get { return i; } set { j = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldAndPropertyHaveDifferentStaticInstance()
        {
            await TestMissingAsync(
@"class Class { [|static int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldUseInRefArgument1()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { get { return i; } } void M(ref int x) { M(ref i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldUseInRefArgument2()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { get { return i; } } void M(ref int x) { M(ref this.i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldUseInOutArgument()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; int P { get { return i; } } void M(out x) { M(out i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithVirtualProperty()
        {
            await TestMissingAsync(
@"class Class { [|int i|]; public virtual int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithConstField()
        {
            await TestMissingAsync(
@"class Class { [|const int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators1()
        {
            await TestAsync(
@"class Class { int [|i|], j, k; int P { get { return i; } } }",
@"class Class { int j, k; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators2()
        {
            await TestAsync(
@"class Class { int i, [|j|], k; int P { get { return j; } } }",
@"class Class { int i, k; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldWithMultipleDeclarators3()
        {
            await TestAsync(
@"class Class { int i, j, [|k|]; int P { get { return k; } } }",
@"class Class { int i, j; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestFieldAndPropertyInDifferentParts()
        {
            await TestAsync(
@"partial class Class { [|int i|]; } partial class Class { int P { get { return i; } } }",
@"partial class Class { } partial class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestNotWithFieldWithAttribute()
        {
            await TestMissingAsync(
@"class Class { [|[A]int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestUpdateReferences()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } } public Class() { i = 1; } }",
@"class Class { int P { get; } public Class() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestUpdateReferencesConflictResolution()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } } public Class(int P) { i = 1; } }",
@"class Class { int P { get; } public Class(int P) { this.P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInConstructor()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } } public Class() { i = 1; } }",
@"class Class { int P { get; } public Class() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInNotInConstructor1()
        {
            await TestAsync(
@"class Class { [|int i|]; int P { get { return i; } } public Foo() { i = 1; } }",
@"class Class { int P { get; set; } public Foo() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestWriteInNotInConstructor2()
        {
            await TestAsync(
@"class Class { [|int i|]; public int P { get { return i; } } public Foo() { i = 1; } }",
@"class Class { public int P { get; private set; } public Foo() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestAlreadyAutoPropertyWithGetterWithNoBody()
        {
            await TestMissingAsync(@"class Class { public int [|P|] { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public async Task TestAlreadyAutoPropertyWithGetterAndSetterWithNoBody()
        {
            await TestMissingAsync(@"class Class { public int [|P|] { get; set; } }");
        }
    }
}
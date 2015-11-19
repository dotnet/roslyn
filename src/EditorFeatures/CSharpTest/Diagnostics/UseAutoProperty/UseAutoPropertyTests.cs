// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public void TestSingleGetterFromField()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } } }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestCSharp5_1()
        {
            Test(
@"class Class { [|int i|]; public int P { get { return i; } } }",
@"class Class { public int P { get; private set; } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestCSharp5_2()
        {
            TestMissing(
@"class Class { [|readonly int i|]; int P { get { return i; } } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestInitializer()
        {
            Test(
@"class Class { [|int i = 1|]; int P { get { return i; } } }",
@"class Class { int P { get; } = 1; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestInitializer_CSharp5()
        {
            TestMissing(
@"class Class { [|int i = 1|]; int P { get { return i; } } }",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleGetterFromProperty()
        {
            Test(
@"class Class { int i; [|int P { get { return i; } }|] }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleSetter()
        {
            TestMissing(
@"class Class { [|int i|]; int P { set { i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestGetterAndSetter()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } set { i = value; } } }",
@"class Class { int P { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleGetterWithThis()
        {
            Test(
@"class Class { [|int i|]; int P { get { return this.i; } } }",
@"class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleSetterWithThis()
        {
            TestMissing(
@"class Class { [|int i|]; int P { set { this.i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestGetterAndSetterWithThis()
        {
            Test(
@"class Class { [|int i|]; int P { get { return this.i; } set { this.i = value; } } }",
@"class Class { int P { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestGetterWithMutipleStatements()
        {
            TestMissing(
@"class Class { [|int i|]; int P { get { ; return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSetterWithMutipleStatements()
        {
            TestMissing(
@"class Class { [|int i|]; int P { set { ; i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSetterWithMultipleStatementsAndGetterWithSingleStatement()
        {
            TestMissing(@"class Class { [|int i|]; int P { get { return i; } set { ; i = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestGetterAndSetterUseDifferentFields()
        {
            TestMissing(
@"class Class { [|int i|]; int j; int P { get { return i; } set { j = value; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldAndPropertyHaveDifferentStaticInstance()
        {
            TestMissing(
@"class Class { [|static int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldUseInRefArgument1()
        {
            TestMissing(
@"class Class { [|int i|]; int P { get { return i; } } void M(ref int x) { M(ref i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldUseInRefArgument2()
        {
            TestMissing(
@"class Class { [|int i|]; int P { get { return i; } } void M(ref int x) { M(ref this.i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldUseInOutArgument()
        {
            TestMissing(
@"class Class { [|int i|]; int P { get { return i; } } void M(out x) { M(out i); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestNotWithVirtualProperty()
        {
            TestMissing(
@"class Class { [|int i|]; public virtual int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestNotWithConstField()
        {
            TestMissing(
@"class Class { [|const int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldWithMultipleDeclarators1()
        {
            Test(
@"class Class { int [|i|], j, k; int P { get { return i; } } }",
@"class Class { int j, k; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldWithMultipleDeclarators2()
        {
            Test(
@"class Class { int i, [|j|], k; int P { get { return j; } } }",
@"class Class { int i, k; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldWithMultipleDeclarators3()
        {
            Test(
@"class Class { int i, j, [|k|]; int P { get { return k; } } }",
@"class Class { int i, j; int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestFieldAndPropertyInDifferentParts()
        {
            Test(
@"partial class Class { [|int i|]; } partial class Class { int P { get { return i; } } }",
@"partial class Class { } partial class Class { int P { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestNotWithFieldWithAttribute()
        {
            TestMissing(
@"class Class { [|[A]int i|]; int P { get { return i; } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestUpdateReferences()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } } public Class() { i = 1; } }",
@"class Class { int P { get; } public Class() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestUpdateReferencesConflictResolution()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } } public Class(int P) { i = 1; } }",
@"class Class { int P { get; } public Class(int P) { this.P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestWriteInConstructor()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } } public Class() { i = 1; } }",
@"class Class { int P { get; } public Class() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestWriteInNotInConstructor1()
        {
            Test(
@"class Class { [|int i|]; int P { get { return i; } } public Foo() { i = 1; } }",
@"class Class { int P { get; set; } public Foo() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestWriteInNotInConstructor2()
        {
            Test(
@"class Class { [|int i|]; public int P { get { return i; } } public Foo() { i = 1; } }",
@"class Class { public int P { get; private set; } public Foo() { P = 1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestAlreadyAutoPropertyWithGetterWithNoBody()
        {
            TestMissing(@"class Class { public int [|P|] { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestAlreadyAutoPropertyWithGetterAndSetterWithNoBody()
        {
            TestMissing(@"class Class { public int [|P|] { get; set; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleLine1()
        {
            Test(
@"class Class
{
    [|int i|];
    int P { get { return i; } }
}",
@"class Class
{
    int P { get; }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleLine2()
        {
            Test(
@"class Class
{
    [|int i|];
    int P
    {
        get { return i; }
    }
}",
@"class Class
{
    int P { get; }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
        public void TestSingleLine3()
        {
            Test(
@"class Class
{
    [|int i|];
    int P
    {
        get { return i; }
        set { i = value; }
    }
}",
@"class Class
{
    int P { get; set; }
}", compareTokens: false);
        }
    }
}

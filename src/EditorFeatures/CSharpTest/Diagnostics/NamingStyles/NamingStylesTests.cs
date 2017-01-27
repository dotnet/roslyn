// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamingStyles;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpNamingStyleDiagnosticAnalyzer(),
                new NamingStyleCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_CorrectName()
        {
            await TestMissingAsync(
@"class [|C|]
{
}",
                options: ClassNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_NameGetsCapitalized()
        {
            await TestAsync(
@"class [|c|]
{
}",
@"class C
{
}",
                options: ClassNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_CorrectName()
        {
            await TestMissingAsync(
@"class C
{
    void [|M|]()
    {
    }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NameGetsCapitalized()
        {
            await TestAsync(
@"class C
{
    void [|m|]()
    {
    }
}",
@"class C
{
    void M()
    {
    }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_ConstructorsAreIgnored()
        {
            await TestMissingAsync(
@"class c
{
    public [|c|]()
    {
    }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_PropertyAccessorsAreIgnored()
        {
            await TestMissingAsync(
@"class C
{
    public int P { [|get|]; set; }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_IndexerNameIsIgnored()
        {
            await TestMissingAsync(
@"class C
{
    public int [|this|][int index]
    {
        get
        {
            return 1;
        }
    }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestCamelCaseParameters()
        {
            await TestAsync(
@"class C
{
    public void M(int [|X|])
    {
    }
}",
@"class C
{
    public void M(int x)
    {
    }
}",
                options: ParameterNamesAreCamelCase);
		}
		
        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InInterfaceWithImplicitImplementation()
        {
            await TestAsync(
@"interface I
{
    void [|m|]();
}

class C : I
{
    public void m() { }
}",
@"interface I
{
    void M();
}

class C : I
{
    public void M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InInterfaceWithExplicitImplementation()
        {
            await TestAsync(
@"interface I
{
    void [|m|]();
}

class C : I
{
    void I.m() { }
}",
@"interface I
{
    void M();
}

class C : I
{
    void I.M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInImplicitInterfaceImplementation()
        {
            await TestMissingAsync(
@"interface I
{
    void m();
}

class C : I
{
    public void [|m|]() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInExplicitInterfaceImplementation()
        {
            await TestMissingAsync(
@"interface I
{
    void m();
}

class C : I
{
    void I.[|m|]() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_InAbstractType()
        {
            await TestAsync(
@"
abstract class C
{
    public abstract void [|m|]();
}

class D : C
{
    public override void m() { }
}",
@"
abstract class C
{
    public abstract void M();
}

class D : C
{
    public override void M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NotInAbstractMethodImplementation()
        {
            await TestMissingAsync(
@"
abstract class C
{
    public abstract void m();
}

class D : C
{
    public override void [|m|]() { }
}",
                options: MethodNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseProperty_InInterface()
        {
            await TestAsync(
@"
interface I
{
    int [|p|] { get; set; }
}

class C : I
{
    public int p { get { return 1; } set { } }
}",
@"
interface I
{
    int P { get; set; }
}

class C : I
{
    public int P { get { return 1; } set { } }
}",
                options: PropertyNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseProperty_NotInImplicitInterfaceImplementation()
        {
            await TestMissingAsync(
@"
interface I
{
    int p { get; set; }
}

class C : I
{
    public int [|p|] { get { return 1; } set { } }
}",
                options: PropertyNamesArePascalCase);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_OverrideInternalMethod()
        {
            await TestMissingAsync(
@"
abstract class C
{
    internal abstract void m();
}

class D : C
{
    internal override void [|m|]() { }
}",
                options: MethodNamesArePascalCase);
        }
    }
}
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace) =>
            new Tuple<DiagnosticAnalyzer, CodeFixProvider>(new CSharpNamingStyleDiagnosticAnalyzer(), new CSharpNamingStyleCodeFixProvider());

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_CorrectName()
        {
            await TestMissingAsync(
                @"class [|C|] { }",
                options: ClassNamesArePascalCase);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseClass_NameGetsCapitalized()
        {
            await TestAsync(
                @"class [|c|] { }",
                @"class C { }",
                options: ClassNamesArePascalCase);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_CorrectName()
        {
            await TestMissingAsync(
@"class C 
{
    void [|M|]() { }
}",
                options: MethodNamesArePascalCase);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_NameGetsCapitalized()
        {
            await TestAsync(
@"class C 
{
    void [|m|]() { }
}",
@"class C 
{
    void M() { }
}",
                options: MethodNamesArePascalCase);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_ConstructorsAreIgnored()
        {
            await TestMissingAsync(
@"class c
{
    public [|c|]() { }
}",
                options: MethodNamesArePascalCase);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        public async Task TestPascalCaseMethod_PropertyAccessorsAreIgnored()
        {
            await TestMissingAsync(
@"class C
{
    public int P { [|get|]; set; }
}",
                options: MethodNamesArePascalCase);
        }
    }
}
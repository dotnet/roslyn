// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses.IfStatement;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses.IfStatement
{
    public partial class RemoveUnnecessaryParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

        private static readonly CSharpParseOptions CSharp8 =
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestWithIgnoreAll()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$if (!(1)) {}
    }
}", new TestParameters(options: IgnoreAllParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestWithOtherCSharp7()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$if (!(1)) {}
    }
}", new TestParameters(
    options: RemoveAllUnnecessaryParentheses,
    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCSharp8()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = $$if (!(1)) {}
    }
}",
@"class C
{
    void M()
    {
        int x = $$if !(1) {}
    }
}", parameters: new TestParameters(
    options: RemoveAllUnnecessaryParentheses,
    parseOptions: CSharp8));
        }
    }
}

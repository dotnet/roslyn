// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UsePatternCombinators;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternCombinators
{
    public class CSharpUsePatternMatchingDiagnosticAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUsePatternCombinatorsDiagnosticAnalyzer(), new CSharpUsePatternCombinatorsCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task Test00()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void Missing(int i, object o)
    {
        if (i == 0) { }
        if (i > 0) { }
        if (i is C) { }
        if (i is C c) { }
        if (!(i > 0)) { }
        if (p != null) { }
    }
    void Good(int i, object o)
    {
        if (!(o is C c)) { }
        if (!(o is C)) { }
        if ({|FixAllInDocument:i == 1 || 2 == i|}) { }
        if (i != 1 || 2 != i) { }
        if (!(i != 1 || 2 != i)) { }
        if (i < 1 && 2 <= i) { }
        if (i < 1 && 2 <= i && i is not 0) { }
    }
}",
@"class C
{
    void Missing(int i, object o)
    {
        if (i == 0) { }
        if (i > 0) { }
        if (i is C) { }
        if (i is C c) { }
        if (!(i > 0)) { }
        if (p != null) { }
    }
    void Good(int i, object o)
    {
        if (o is not C c) { }
        if (o is not C) { }
        if (i is 1 or 2) { }
        if (i is not (1 and 2)) { }
        if (i is 1 and 2) { }
        if (i is < 1 and >= 2) { }
        if (i is < 1 and >= 2 and not 0) { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestReturn()
        {
            await TestInRegularAndScript1Async(
                @"class C
{
    bool M(int variable)
    {
        return [|variable == 0 ||
               variable == 1 ||
               variable == 2|];
    }
}",
@"class C
{
    bool M(int variable)
    {
        return variable is 0 or
               1 or
               2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUsePatternCombinators)]
        public async Task TestReturn_Not()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    bool M(int variable)
    {
        return [|variable != 0 &&
               variable != 1 &&
               variable != 2|];
    }
}",
@"class C
{
    bool M(int variable)
    {
        return variable is not (0 or
               1 or
               2);
    }
}");
        }
    }
}

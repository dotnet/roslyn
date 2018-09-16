// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCompoundAssignment
{
    public class UseCompoundAssignmentTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseCompoundAssignmentDiagnosticAnalyzer(), new CSharpUseCompoundAssignmentCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestAddExpression()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M(int a)
    {
        a [||]= a + 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a += 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestSubtractExpression()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M(int a)
    {
        a [||]= a - 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a -= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestMultiplyExpression()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M(int a)
    {
        a [||]= a * 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a *= 10;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseCompoundAssignment)]
        public async Task TestDivideExpression()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M(int a)
    {
        a [||]= a / 10;
    }
}",
@"public class C
{
    void M(int a)
    {
        a /= 10;
    }
}");
        }
    }
}

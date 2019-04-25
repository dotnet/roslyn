// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAnonymousTypeMemberName
{
    public class AddAnonymousTypeMemberNameTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddAnonymousTypeMemberNameCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
        public async Task Test1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        var v = new { [||]this.GetType() };
    }
}",
@"
class C
{
    void M()
    {
        var v = new { {|Rename:Type|} = this.GetType() };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
        public async Task TestExistingName()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        var v = new { Type = 1, [||]this.GetType() };
    }
}",
@"
class C
{
    void M()
    {
        var v = new { Type = 1, {|Rename:Type1|} = this.GetType() };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        var v = new { {|FixAllInDocument:|}new { this.GetType(), this.ToString() } };
    }
}",
@"
class C
{
    void M()
    {
        var v = new { P = new { Type = this.GetType(), V = this.ToString() } };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        var v = new { new { {|FixAllInDocument:|}this.GetType(), this.ToString() } };
    }
}",
@"
class C
{
    void M()
    {
        var v = new { P = new { Type = this.GetType(), V = this.ToString() } };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
        public async Task TestFixAll3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        var v = new { {|FixAllInDocument:|}new { this.GetType(), this.GetType() } };
    }
}",
@"
class C
{
    void M()
    {
        var v = new { P = new { Type = this.GetType(), Type1 = this.GetType() } };
    }
}");
        }
    }
}

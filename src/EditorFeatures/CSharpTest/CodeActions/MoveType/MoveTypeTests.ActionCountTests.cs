// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
public partial class MoveTypeTests : CSharpMoveTypeTestsBase
{
    [Fact]
    public async Task MoveType_ActionCounts_RenameOnly()
    {
        // Fixes offered will be rename type to match file, rename file to match type.
        await TestActionCountAsync(@"namespace N1
{
    class Class1[||]
    {
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableBeforeHeader()
    {
        await TestActionCountAsync(@"namespace N1
{
    [||]
    class Class1
    {
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableBeforeAttributeOnHeader()
    {
        await TestActionCountAsync(@"namespace N1
{
    [||][X]
    class Class1
    {
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableOnHeaderIncludingWhitespaceAndAttribute()
    {
        await TestActionCountAsync(@"namespace N1
{[|
    [X]
    class Class1
    {|]
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableAfterHeader()
    {
        await TestActionCountAsync(@"namespace N1
{
    class Class1
    [||]{
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableIncludingDocumentationCommentAndHeader()
    {
        await TestActionCountAsync(@"namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    class Class1|]
    {
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_AvailableIncludingDocumentationCommentAndAttributeAndHeader()
    {
        await TestActionCountAsync(@"using System;
namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    [Obsolete]
    class Class1|]
    {
    }
}", count: 2);
    }

    [Fact]
    public async Task MoveType_NotAvailableBeforeType()
    {
        await TestMissingInRegularAndScriptAsync(@"[|namespace N1
{|]
    class Class1
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableInsideType()
    {
        await TestMissingInRegularAndScriptAsync(@"namespace N1
{
    class Class1
    {[|
        void M()
        {
        }|]
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableAfterType()
    {
        await TestMissingInRegularAndScriptAsync(@"namespace N1
{
    class Class1
    {
        void M()
        {
        }
    }

    [|class test1|]
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableAroundDocumentationCommentAboveHeader()
    {
        await TestMissingInRegularAndScriptAsync(@"namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>|]
    class Class1
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableAroundAttributeAboveHeader()
    {
        await TestMissingInRegularAndScriptAsync(@"using System;
namespace N1
{
    [|[Obsolete]|]
    class Class1
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableAroundDocumentationCommentAndAttributeAboveHeader()
    {
        await TestMissingInRegularAndScriptAsync(@"using System;
namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    [Obsolete]|]
    class Class1
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_NotAvailableInsideDocumentationCommentAndAttributeAboveHeader()
    {
        await TestMissingInRegularAndScriptAsync(@"using System;
namespace N1
{
    /// <summary>
    /// [|Documentation comment.
    /// </summary>
    [Obso|]lete]
    class Class1
    {
    }
}");
    }

    [Fact]
    public async Task MoveType_ActionCounts_MoveOnly()
    {
        // Fixes offered will be move type to new file.
        await TestActionCountAsync(@"namespace N1
{
    class Class1[||]
    {
    }

    class test1 /* this matches file name assigned by TestWorkspace*/
    {
    }
}", count: 1);
    }

    [Fact]
    public async Task MoveType_ActionCounts_RenameAndMove()
    {
        // Fixes offered will be move type, rename type to match file, rename file to match type.
        await TestActionCountAsync(@"namespace N1
{
    class Class1[||]
    {
    }

    class Class2
    {
    }
}", count: 3);
    }

    [Fact]
    public async Task MoveType_ActionCounts_All()
    {
        // Fixes offered will be
        // 1. move type to InnerType.cs
        // 2. move type to OuterType.InnerType.cs
        // 3. rename file to InnerType.cs
        // 4. rename file to OuterType.InnerType.cs
        // 5. rename type to test1 (which is the default document name given by TestWorkspace).
        await TestActionCountAsync(@"namespace N1
{
    class OuterType
    {
        class InnerType[||]
        {
        }
    }

    class Class1
    {
    }
}", count: 5);
    }
}

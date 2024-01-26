// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.MoveType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveType)]
    public partial class MoveTypeTests : CSharpMoveTypeTestsBase
    {
        [Fact]
        public async Task MoveType_ActionCounts_RenameOnly()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }
}";
            // Fixes offered will be rename type to match file, rename file to match type.
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableBeforeHeader()
        {
            var code =
@"namespace N1
{
    [||]
    class Class1
    {
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableBeforeAttributeOnHeader()
        {
            var code =
@"namespace N1
{
    [||][X]
    class Class1
    {
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableOnHeaderIncludingWhitespaceAndAttribute()
        {
            var code =
@"namespace N1
{[|
    [X]
    class Class1
    {|]
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableAfterHeader()
        {
            var code =
@"namespace N1
{
    class Class1
    [||]{
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableIncludingDocumentationCommentAndHeader()
        {
            var code =
@"namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    class Class1|]
    {
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_AvailableIncludingDocumentationCommentAndAttributeAndHeader()
        {
            var code =
@"using System;
namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    [Obsolete]
    class Class1|]
    {
    }
}";
            await TestActionCountAsync(code, count: 2);
        }

        [Fact]
        public async Task MoveType_NotAvailableBeforeType()
        {
            var code =
@"[|namespace N1
{|]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableInsideType()
        {
            var code =
@"namespace N1
{
    class Class1
    {[|
        void M()
        {
        }|]
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableAfterType()
        {
            var code =
@"namespace N1
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
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableAroundDocumentationCommentAboveHeader()
        {
            var code =
@"namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>|]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableAroundAttributeAboveHeader()
        {
            var code =
@"using System;
namespace N1
{
    [|[Obsolete]|]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableAroundDocumentationCommentAndAttributeAboveHeader()
        {
            var code =
@"using System;
namespace N1
{
    [|/// <summary>
    /// Documentation comment.
    /// </summary>
    [Obsolete]|]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_NotAvailableInsideDocumentationCommentAndAttributeAboveHeader()
        {
            var code =
@"using System;
namespace N1
{
    /// <summary>
    /// [|Documentation comment.
    /// </summary>
    [Obso|]lete]
    class Class1
    {
    }
}";
            await TestMissingInRegularAndScriptAsync(code);
        }

        [Fact]
        public async Task MoveType_ActionCounts_MoveOnly()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }

    class test1 /* this matches file name assigned by TestWorkspace*/
    {
    }
}";
            // Fixes offered will be move type to new file.
            await TestActionCountAsync(code, count: 1);
        }

        [Fact]
        public async Task MoveType_ActionCounts_RenameAndMove()
        {
            var code =
@"namespace N1
{
    class Class1[||]
    {
    }

    class Class2
    {
    }
}";
            // Fixes offered will be move type, rename type to match file, rename file to match type.
            await TestActionCountAsync(code, count: 3);
        }

        [Fact]
        public async Task MoveType_ActionCounts_All()
        {
            var code =
@"namespace N1
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
}";
            // Fixes offered will be
            // 1. move type to InnerType.cs
            // 2. move type to OuterType.InnerType.cs
            // 3. rename file to InnerType.cs
            // 4. rename file to OuterType.InnerType.cs
            // 5. rename type to test1 (which is the default document name given by TestWorkspace).
            await TestActionCountAsync(code, count: 5);
        }
    }
}

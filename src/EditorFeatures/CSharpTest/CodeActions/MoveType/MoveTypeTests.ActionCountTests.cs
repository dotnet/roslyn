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
    public Task MoveType_ActionCounts_RenameOnly()
        => TestActionCountAsync("""
            namespace N1
            {
                class Class1[||]
                {
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableBeforeHeader()
        => TestActionCountAsync("""
            namespace N1
            {
                [||]
                class Class1
                {
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableBeforeAttributeOnHeader()
        => TestActionCountAsync("""
            namespace N1
            {
                [||][X]
                class Class1
                {
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableOnHeaderIncludingWhitespaceAndAttribute()
        => TestActionCountAsync("""
            namespace N1
            {[|
                [X]
                class Class1
                {|]
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableAfterHeader()
        => TestActionCountAsync("""
            namespace N1
            {
                class Class1
                [||]{
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableIncludingDocumentationCommentAndHeader()
        => TestActionCountAsync("""
            namespace N1
            {
                [|/// <summary>
                /// Documentation comment.
                /// </summary>
                class Class1|]
                {
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_AvailableIncludingDocumentationCommentAndAttributeAndHeader()
        => TestActionCountAsync("""
            using System;
            namespace N1
            {
                [|/// <summary>
                /// Documentation comment.
                /// </summary>
                [Obsolete]
                class Class1|]
                {
                }
            }
            """, count: 2);

    [Fact]
    public Task MoveType_NotAvailableBeforeType()
        => TestMissingInRegularAndScriptAsync("""
            [|namespace N1
            {|]
                class Class1
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableInsideType()
        => TestMissingInRegularAndScriptAsync("""
            namespace N1
            {
                class Class1
                {[|
                    void M()
                    {
                    }|]
                }
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableAfterType()
        => TestMissingInRegularAndScriptAsync("""
            namespace N1
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
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableAroundDocumentationCommentAboveHeader()
        => TestMissingInRegularAndScriptAsync("""
            namespace N1
            {
                [|/// <summary>
                /// Documentation comment.
                /// </summary>|]
                class Class1
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableAroundAttributeAboveHeader()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            namespace N1
            {
                [|[Obsolete]|]
                class Class1
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableAroundDocumentationCommentAndAttributeAboveHeader()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            namespace N1
            {
                [|/// <summary>
                /// Documentation comment.
                /// </summary>
                [Obsolete]|]
                class Class1
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_NotAvailableInsideDocumentationCommentAndAttributeAboveHeader()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            namespace N1
            {
                /// <summary>
                /// [|Documentation comment.
                /// </summary>
                [Obso|]lete]
                class Class1
                {
                }
            }
            """);

    [Fact]
    public Task MoveType_ActionCounts_MoveOnly()
        => TestActionCountAsync("""
            namespace N1
            {
                class Class1[||]
                {
                }

                class test1 /* this matches file name assigned by TestWorkspace*/
                {
                }
            }
            """, count: 1);

    [Fact]
    public Task MoveType_ActionCounts_RenameAndMove()
        => TestActionCountAsync("""
            namespace N1
            {
                class Class1[||]
                {
                }

                class Class2
                {
                }
            }
            """, count: 3);

    [Fact]
    public Task MoveType_ActionCounts_All()
        => TestActionCountAsync("""
            namespace N1
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
            }
            """, count: 5);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting;

[Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
public sealed class RegionHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(RegionHighlighter);

    [Fact]
    public Task TestExample1_1()
        => TestAsync(
            """
            class C
            {
                {|Cursor:[|#region|]|} Main
                static void Main()
                {
                }
                [|#endregion|]
            }
            """);

    [Fact]
    public Task TestExample1_2()
        => TestAsync(
            """
            class C
            {
                [|#region|] Main
                static void Main()
                {
                }
                {|Cursor:[|#endregion|]|}
            }
            """);

    [Fact]
    public Task TestNestedExample1_1()
        => TestAsync(
            """
            class C
            {
                {|Cursor:[|#region|]|} Main
                static void Main()
                {
                    #region body
                    #endregion
                }
                [|#endregion|]
            }
            """);

    [Fact]
    public Task TestNestedExample1_2()
        => TestAsync(
            """
            class C
            {
                #region Main
                static void Main()
                {
                    {|Cursor:[|#region|]|} body
                    [|#endregion|]
                }
                #endregion
            }
            """);

    [Fact]
    public Task TestNestedExample1_3()
        => TestAsync(
            """
            class C
            {
                #region Main
                static void Main()
                {
                    [|#region|] body
                    {|Cursor:[|#endregion|]|}
                }
                #endregion
            }
            """);

    [Fact]
    public Task TestNestedExample1_4()
        => TestAsync(
            """
            class C
            {
                [|#region|] Main
                static void Main()
                {
                    #region body
                    #endregion
                }
                {|Cursor:[|#endregion|]|}
            }
            """);
}

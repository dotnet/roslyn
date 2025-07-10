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
public sealed class CheckedStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(CheckedStatementHighlighter);

    [Fact]
    public Task TestExample1_1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    short x = 0;
                    short y = 100;
                    while (true)
                    {
                        {|Cursor:[|checked|]|}
                        {
                            x++;
                        }

                        unchecked
                        {
                            y++;
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestExample1_2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    short x = 0;
                    short y = 100;
                    while (true)
                    {
                        checked
                        {
                            x++;
                        }

                        {|Cursor:[|unchecked|]|}
                        {
                            y++;
                        }
                    }
                }
            }
            """);
}

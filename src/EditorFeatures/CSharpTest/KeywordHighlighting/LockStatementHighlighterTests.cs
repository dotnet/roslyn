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

public class LockStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(LockStatementHighlighter);

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
    public async Task TestExample1_1()
    {
        await TestAsync(
            """
            class Account
            {
                object lockObj = new object();
                int balance;

                int Withdraw(int amount)
                {
                    {|Cursor:[|lock|]|} (lockObj)
                    {
                        if (balance >= amount)
                        {
                            balance = balance – amount;
                            return amount;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
            """);
    }
}

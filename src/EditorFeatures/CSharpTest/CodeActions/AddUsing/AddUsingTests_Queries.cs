// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
public sealed partial class AddUsingTests
{
    [Fact]
    public Task TestSimpleQuery()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static void Main(string[] args)
                {
                    var q = [|from x in args
                            select x|]}
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    var q = from x in args
                            select x}
            }
            """);

    [Fact]
    public Task TestSimpleWhere()
        => TestInRegularAndScriptAsync(
            """
            class Test
            {
                public void SimpleWhere()
                {
                    int[] numbers = {
                        1,
                        2,
                        3
                    };
                    var lowNums = [|from n in numbers
                                  where n < 5
                                  select n|];
                }
            }
            """,
            """
            using System.Linq;

            class Test
            {
                public void SimpleWhere()
                {
                    int[] numbers = {
                        1,
                        2,
                        3
                    };
                    var lowNums = from n in numbers
                                  where n < 5
                                  select n;
                }
            }
            """);
}

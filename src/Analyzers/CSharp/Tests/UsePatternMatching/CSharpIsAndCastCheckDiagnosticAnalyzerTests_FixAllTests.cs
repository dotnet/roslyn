// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UsePatternMatching
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInlineTypeCheck)]
    public partial class CSharpIsAndCastCheckDiagnosticAnalyzerTests
    {
        [Fact]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        if (x is string)
                        {
                            {|FixAllInDocument:var|} v1 = (string)x;
                        }

                        if (x is bool)
                        {
                            var v2 = (bool)x;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (x is string v1)
                        {
                        }

                        if (x is bool v2)
                        {
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task FixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        if (x is string)
                        {
                            var v1 = (string)x;
                        }

                        if (x is bool)
                        {
                            {|FixAllInDocument:var|} v2 = (bool)x;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (x is string v1)
                        {
                        }

                        if (x is bool v2)
                        {
                        }
                    }
                }
                """);
        }
    }
}

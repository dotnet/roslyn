// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.UseCoalesceExpression;
using Microsoft.CodeAnalysis.CSharp.AddAccessibilityModifiers;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCoalesceExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCoalesceExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer,
        UseCoalesceExpressionForIfNullStatementCheckCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseCoalesceExpression)]
    public class UseCoalesceExpressionForIfNullStatementCheckTests
    {
        [Fact]
        public async Task TestLocalDeclaration1()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C;
                        [|if|] (item == null)
                            throw new System.InvalidOperationException();
                    }

                    object FindItem() => null;
                }
                """,
                FixedCode = """
                class C
                {
                    void M()
                    {
                        var item = FindItem() as C ?? throw new System.InvalidOperationException();
                    }
                
                    object FindItem() => null;
                }
                """
            }.RunAsync();
        }
    }
}

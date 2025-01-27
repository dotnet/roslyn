// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddAnonymousTypeMemberName;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddAnonymousTypeMemberName;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddAnonymousTypeMemberName)]
public class AddAnonymousTypeMemberNameTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public AddAnonymousTypeMemberNameTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpAddAnonymousTypeMemberNameCodeFixProvider());

    [Fact]
    public async Task Test1()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    var v = new { [||]this.GetType() };
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var v = new { {|Rename:Type|} = this.GetType() };
                }
            }
            """);
    }

    [Fact]
    public async Task TestExistingName()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                void M()
                {
                    var v = new { Type = 1, [||]this.GetType() };
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var v = new { Type = 1, {|Rename:Type1|} = this.GetType() };
                }
            }
            """);
    }

    [Fact]
    public async Task TestFixAll1()
    {
        await TestInRegularAndScript1Async(
        """
        class C
        {
            void M()
            {
                var v = new { {|FixAllInDocument:|}new { this.GetType(), this.ToString() } };
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                var v = new { Value = new { Type = this.GetType(), V = this.ToString() } };
            }
        }
        """);
    }

    [Fact]
    public async Task TestFixAll2()
    {
        await TestInRegularAndScript1Async(
        """
        class C
        {
            void M()
            {
                var v = new { new { {|FixAllInDocument:|}this.GetType(), this.ToString() } };
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                var v = new { Value = new { Type = this.GetType(), V = this.ToString() } };
            }
        }
        """);
    }

    [Fact]
    public async Task TestFixAll3()
    {
        await TestInRegularAndScript1Async(
        """
        class C
        {
            void M()
            {
                var v = new { {|FixAllInDocument:|}new { this.GetType(), this.GetType() } };
            }
        }
        """,
        """
        class C
        {
            void M()
            {
                var v = new { Value = new { Type = this.GetType(), Type1 = this.GetType() } };
            }
        }
        """);
    }
}

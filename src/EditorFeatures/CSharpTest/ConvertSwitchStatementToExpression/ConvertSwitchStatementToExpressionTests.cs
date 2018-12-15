// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertSwitchStatementToExpression
{
    public partial class ConvertSwitchStatementToExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new ConvertSwitchStatementToExpressionDiagnosticAnalyzer(), new ConvertSwitchStatementToExpressionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturn()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                return 4;
            case 2:
                return 5;
            case 3:
                return 6;
            default:
                return 7;
        }
    }
}",
@"class Program
{
    int M(int i)
    {
        return i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => 7
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNested()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    int M(int i, int j)
    {
        [||]switch (i)
        {
            case 1:
                switch (j)
                {
                    case 7:
                        return 10;
                    case 8:
                        return 11;
                    case 9:
                        return 12;
                }
                break;
            case 2:
                return 5;
            case 3:
                return 6;
            default:
                throw null;
        }
    }
}",
@"class Program
{
    int M(int i, int j)
    {
        return i switch
        {
            1 => j switch
            {
                7 => 10,
                8 => 11,
                9 => 12
            },
            2 => 5,
            3 => 6,
            _ => throw null
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestSingleAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void M(int i)
    {
        int j;
        [||]switch (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j = 5;
                break;
            case 3:
                j = 6;
                break;
        }
    }
}",
@"class Program
{
    void M(int i)
    {
        int j;
        j = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMultiSingleAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void M(int i)
    {
        int j, k;
        [||]switch (i)
        {
            case 1:
                j = 4;
                k = 5;
                break;
            case 2:
                j = 6;
                k = 7;
                break;
            case 3:
                j = 8;
                k = 9;
                break;
        }
    }
}",
@"class Program
{
    void M(int i)
    {
        int j, k;
        (j, k) = i switch
        {
            1 => (4, 5),
            2 => (6, 7),
            3 => (8, 9)
        };
    }
}");
        }
    }
}

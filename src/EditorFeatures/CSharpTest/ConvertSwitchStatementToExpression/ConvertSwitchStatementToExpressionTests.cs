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
        public async Task TestAssignmnet_Array()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    var array = new int[1];
    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                array[1] = 4;
                break;
            case 2:
                array[1] = 5;
                break;
            case 3:
                array[1] = 6;
                break;
            default:
                array[1] = 7;
                break;
        }
    }
}",
@"class Program
{
    var array = new int[1];
    int M(int i)
    {
        array[1] = i switch
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
        public async Task TestMissingOnDefalutBreak_01()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            default:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefalutBreak_02()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case _:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefalutBreak_03()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case var _:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefalutBreak_04()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case var x:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAllBreak()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case 1:
                break;
            case 2:
                break;
            case 3:
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAllThrow()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case 1:
                throw null;
            default:
                throw null;
        }
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
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        var j = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_UseBeforeAssignment()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void M(int i)
    {
        int j = 123;
        M(i);
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
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        int j = 123;
        M(i);
        j = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null
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
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        var (j, k) = i switch
        {
            1 => (4, 5),
            2 => (6, 7),
            3 => (8, 9),
            _ => throw null
        };
    }
}");
        }
    }
}

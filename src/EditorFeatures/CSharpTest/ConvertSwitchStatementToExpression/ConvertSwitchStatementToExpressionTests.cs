﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        private Task TestInCSharp8(string actual, string expected)
            => TestInRegularAndScriptAsync(actual, expected, parseOptions: TestOptions.Regular8);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturn()
        {
            await TestInCSharp8(
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
            _ => 7,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestReturnAndThrow()
        {
            await TestInCSharp8(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                return 4;
            default: 
                throw null;
            case 2:
                return 5;
            case 3:
                return 6;
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
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_Array()
        {
            await TestInCSharp8(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                array[0] = 4;
                break;
            case 2:
                array[0] = 5;
                break;
            case 3:
                array[0] = 6;
                break;
            default:
                array[0] = 7;
                break;
        }
    }
}",
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        array[0] = i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => 7,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDifferentIndexerArgs()
        {
            await TestMissingAsync(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                array[1] = 4;
                break;
            case 2:
                array[2] = 5;
                break;
            case 3:
                array[2] = 6;
                break;
            default:
                array[2] = 7;
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnQualifiedName()
        {
            await TestMissingAsync(
@"class Program
{
    int[] array = new int[1];

    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                this.array[2] = 4;
                break;
            case 2:
                array[2] = 5;
                break;
            case 3:
                array[2] = 6;
                break;
            default:
                array[2] = 7;
                break;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnDefaultBreak_01()
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
        public async Task TestMissingOnDefaultBreak_02()
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
        public async Task TestMissingOnDefaultBreak_03()
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
        public async Task TestMissingOnDefaultBreak_04()
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
        public async Task TestAllThrow()
        {
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        [||]switch (i)
        {
            case 1:
                throw null;
            default:
                throw new Exception();
        }
    }
}",
@"class Program
{
    void M(int i)
    {
        throw i switch
        {
            1 => null,
            _ => new Exception(),
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment()
        {
            await TestInCSharp8(
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
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnNextStatementMismatch()
        {
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        int j = 0;
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
        return j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnAssignmentMismatch()
        {
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        int j = 0;
        [||]switch (i)
        {
            case 1:
                j = 4;
                break;
            case 2:
                j += 5;
                break;
        }
        return j;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_Compound()
        {
            await TestInCSharp8(
@"class Program
{
    void M(int i)
    {
        int j = 0;
        [||]switch (i)
        {
            case 1:
                j += 4;
                break;
            case 2:
                j += 5;
                break;
            case 3:
                j += 6;
                break;
        }
        throw null;
    }
}",
@"class Program
{
    void M(int i)
    {
        int j = 0;
        j += i switch
        {
            1 => 4,
            2 => 5,
            3 => 6,
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestAssignment_UseBeforeAssignment()
        {
            await TestInCSharp8(
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
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiAssignment()
        {
            await TestMissingAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingONMultiCaseSection()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        int j;
        [||]switch (i)
        {
            case 1:
            case 2:
                j = 4;
                break;
        }
        throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnMultiCompoundAssignment()
        {
            await TestMissingAsync(
@"class Program
{
    void M(int i)
    {
        int j = 0, k = 0;
        [||]switch (i)
        {
            case 1:
                j += 4;
                k += 5;
                break;
            case 2:
                j += 6;
                k += 7;
                break;
            case 3:
                j += 8;
                k += 9;
                break;
        }
        throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestMissingOnGoto()
        {
            await TestMissingAsync(
@"class Program
{
    int M(int i)
    {
        [||]switch (i)
        {
            case 1:
                return 0;
            case 2:
                goto default;
            default:
                return 2;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestTrivia()
        {
            await TestInCSharp8(
@"class Program
{
    int M(int i)
    {
        // leading switch
        [||]switch (i) // trailing switch
        {
            // leading label
            case 1:
                return 4; // trailing body
            case 2:
                return 5;
            case 3:
                return 6;
        }
        
        // leading next statement
        throw null; // leading next statement
    }
}",
@"class Program
{
    int M(int i)
    {
        // leading switch
        return i switch // trailing switch
        {
            // leading label
            1 => 4, // trailing body
            2 => 5,
            3 => 6,

            // leading next statement
            _ => throw null, // leading next statement
        };
    }
}");
        }
    }
}

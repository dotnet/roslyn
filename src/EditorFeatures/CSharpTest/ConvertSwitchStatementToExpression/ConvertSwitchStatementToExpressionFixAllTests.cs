// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertSwitchStatementToExpression
{
    public partial class ConvertSwitchStatementToExpressionTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNested_01()
        {
            await TestInCSharp8(
@"class Program
{
    int M(int i, int j)
    {
        int r;
        {|FixAllInDocument:switch|} (i)
        {
            case 1:
                r = 1;
                break;
            case 2:
                r = 2;
                break;
            case 3:
                r = 3;
                break;
            default:
                r = 4;
                break;
        }
        int x, y;
        switch (i)
        {
            case 1:
                x = 1;
                y = 1;
                break;
            case 2:
                x = 1;
                y = 1;
                break;
            case 3:
                x = 1;
                y = 1;
                break;
            default:
                x = 1;
                y = 1;
                break;
        }
        switch (i)
        {
            default:
                throw null;
            case 1:
                switch (j)
                {
                    case 10:
                        return 10;
                    case 20:
                        return 20;
                    case 30:
                        return 30;
                }
                return 0;
            case 2:
                switch (j)
                {
                    case 10:
                        return 10;
                    case 20:
                        return 20;
                    case 30:
                        return 30;
                    case var _:
                        return 0;
                }
            case 3:
                switch (j)
                {
                    case 10:
                        return 10;
                    case 20:
                        return 20;
                    case 30:
                        return 30;
                    case var v:
                        return 0;
                }
        }
    }
}",
@"class Program
{
    int M(int i, int j)
    {
        var r = i switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => 4,
        };
        int x, y;
        switch (i)
        {
            case 1:
                x = 1;
                y = 1;
                break;
            case 2:
                x = 1;
                y = 1;
                break;
            case 3:
                x = 1;
                y = 1;
                break;
            default:
                x = 1;
                y = 1;
                break;
        }
        return i switch
        {
            1 => j switch
            {
                10 => 10,
                20 => 20,
                30 => 30,
                _ => 0,
            },
            2 => j switch
            {
                10 => 10,
                20 => 20,
                30 => 30,
                var _ => 0,
            },
            3 => j switch
            {
                10 => 10,
                20 => 20,
                30 => 30,
                var v => 0,
            },
            _ => throw null,
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNested_02()
        {
            await TestInCSharp8(
@"class Program
{
    System.Action<int> M(int i, int j)
    {
        {|FixAllInDocument:switch|} (i)
        {
            // 1
            default: // 2
                return () =>
                {
                    switch (j)
                    {
                        default:
                            return 3;
                    }
                };
        }
    }
}",
@"class Program
{
    System.Action<int> M(int i, int j)
    {
        return i switch
        {
            // 1
            // 2
            _ => () =>
                              {
                                  switch (j)
                                  {
                                      default:
                                          return 3;
                                  }
                              }

            ,
        };
    }
}");
        }

        [WorkItem(37907, "https://github.com/dotnet/roslyn/issues/37907")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertSwitchStatementToExpression)]
        public async Task TestNested_03()
        {
            await TestInCSharp8(
@"using System;

class Program
{
    public static void Main() { }
    public DayOfWeek StatusValue() => DayOfWeek.Monday;
    public short Value => 0;
    public bool ValueBoolean()
    {
        bool value;
        {|FixAllInDocument:switch|} (StatusValue())
        {
            case DayOfWeek.Monday:
                switch (Value)
                {
                    case 0:
                        value = false;
                        break;
                    case 1:
                        value = true;
                        break;
                    default:
                        throw new Exception();
                }
                break;
            default:
                throw new Exception();
        }
        return value;
    }
}",
@"using System;

class Program
{
    public static void Main() { }
    public DayOfWeek StatusValue() => DayOfWeek.Monday;
    public short Value => 0;
    public bool ValueBoolean()
    {
        var value = (StatusValue()) switch
        {
            DayOfWeek.Monday => Value switch
            {
                0 => false,
                1 => true,
                _ => throw new Exception(),
            },
            _ => throw new Exception(),
        };
        return value;
    }
}");
        }
    }
}

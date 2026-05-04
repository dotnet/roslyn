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
public sealed class ReturnStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
{
    internal override Type GetHighlighterType()
        => typeof(ReturnStatementHighlighter);

    [Fact]
    public Task TestInLambda()
        => TestAsync(
            """
            static double CalculateArea(double radius)
            {
                Func<double, double> f = r => {
                    if (Double.IsNan(r))
                    {
                        {|Cursor:[|return|]|} Double.NaN;
                    }
                    else
                    {
                        [|return|] r * r * Math.PI;
                    }
                };
                return calcArea(radius);
            }
            """);

    [Fact]
    public Task TestInLambda_NotOnReturnValue()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            return {|Cursor:Double.NaN|};
                        }
                        else
                        {
                            return r * r * Math.PI;
                        }
                    };
                    return calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInLambda_OnSemicolon()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            [|return|] Double.NaN;{|Cursor:|}
                        }
                        else
                        {
                            [|return|] r * r * Math.PI;
                        }
                    };
                    return calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInLambda_SecondOccurence()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            [|return|] Double.NaN;
                        }
                        else
                        {
                            {|Cursor:[|return|]|} r * r * Math.PI;
                        }
                    };
                    return calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInLambda_SecondOccurence_NotOnReturnValue()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            return Double.NaN;
                        }
                        else
                        {
                            return {|Cursor:r * r * Math.PI|};
                        }
                    };
                    return calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInLambda_SecondOccurence_OnSemicolon()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            [|return|] Double.NaN;
                        }
                        else
                        {
                            [|return|] r * r * Math.PI;{|Cursor:|}
                        }
                    };
                    return calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInMethodWithLambda()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            return Double.NaN;
                        }
                        else
                        {
                            return r * r * Math.PI;
                        }
                    };
                    {|Cursor:[|return|]|} calcArea(radius);
                }
            }
            """);

    [Fact]
    public Task TestInMethodWithLambda_NotOnReturnValue()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            return Double.NaN;
                        }
                        else
                        {
                            return r * r * Math.PI;
                        }
                    };
                    return {|Cursor:calcArea(radius)|};
                }
            }
            """);

    [Fact]
    public Task TestInMethodWithLambda_OnSemicolon()
        => TestAsync(
            """
            class C
            {
                static double CalculateArea(double radius)
                {
                    Func<double, double> f = r => {
                        if (Double.IsNan(r))
                        {
                            return Double.NaN;
                        }
                        else
                        {
                            return r * r * Math.PI;
                        }
                    };
                    [|return|] calcArea(radius);{|Cursor:|}
                }
            }
            """);

    [Fact]
    public Task TestInConstructor()
        => TestAsync(
            """
            class C
            {
                C()
                {
                    {|Cursor:[|return|]|};
                    [|return|];
                }
            }
            """);

    [Fact]
    public Task TestInDestructor()
        => TestAsync(
            """
            class C
            {
                ~C()
                {
                    {|Cursor:[|return|]|};
                    [|return|];
                }
            }
            """);

    [Fact]
    public Task TestInOperator()
        => TestAsync(
            """
            class C
            {
                public static string operator +(C a)
                {
                    {|Cursor:[|return|]|} null;
                    [|return|] null;
                }
            }
            """);

    [Fact]
    public Task TestInConversionOperator()
        => TestAsync(
            """
            class C
            {
                public static explicit operator string(C a)
                {
                    {|Cursor:[|return|]|} null;
                    [|return|] null;
                }
            }
            """);

    [Fact]
    public Task TestInGetter()
        => TestAsync(
            """
            class C
            {
                int P
                {
                    get
                    {
                        {|Cursor:[|return|]|} 0;
                        [|return|] 0;
                    }
                    set
                    {
                        return;
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task TestInSetter()
        => TestAsync(
            """
            class C
            {
                int P
                {
                    get
                    {
                        return 0;
                        return 0;
                    }
                    set
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestInInit()
        => TestAsync(
            """
            class C
            {
                int P
                {
                    get
                    {
                        return 0;
                        return 0;
                    }
                    init
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestInAdder()
        => TestAsync(
            """
            class C
            {
                event EventHandler E
                {
                    add
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    }
                    remove
                    {
                        return;
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task TestInRemover()
        => TestAsync(
            """
            class C
            {
                event EventHandler E
                {
                    add
                    {
                        return;
                        return;
                    }
                    remove
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    }
                }
            }
            """);

    [Fact]
    public Task TestInLocalFunction()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    void F()
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    }

                    return;
                }
            }
            """);

    [Fact]
    public Task TestInSimpleLambda()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    Action<string> f = s =>
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    };

                    return;
                }
            }
            """);

    [Fact]
    public Task TestInParenthesizedLambda()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    Action<string> f = (s) =>
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    };

                    return;
                }
            }
            """);

    [Fact]
    public Task TestInAnonymousMethod()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    Action<string> f = delegate
                    {
                        {|Cursor:[|return|]|};
                        [|return|];
                    };

                    return;
                }
            }
            """);

    [Fact]
    public Task TestInTopLevelStatements()
        => TestAsync(
            """
            if (args.Length > 0) [|return|] 0;
            {|Cursor:[|return|]|} 1;
            """);
}

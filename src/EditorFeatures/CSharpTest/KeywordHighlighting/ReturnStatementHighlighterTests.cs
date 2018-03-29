// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class ReturnStatementHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new ReturnStatementHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda()
        {
            await TestAsync(
@"static double CalculateArea(double radius)
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda_NotOnReturnValue()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda_OnSemicolon()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda_SecondOccurence()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda_SecondOccurence_NotOnReturnValue()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLambda_SecondOccurence_OnSemicolon()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInMethodWithLambda()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInMethodWithLambda_NotOnReturnValue()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInMethodWithLambda_OnSemicolon()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInConstructor()
        {
            await TestAsync(
@"class C
{
    C()
    {
        {|Cursor:[|return|]|};
        [|return|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInDestructor()
        {
            await TestAsync(
@"class C
{
    ~C()
    {
        {|Cursor:[|return|]|};
        [|return|];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInOperator()
        {
            await TestAsync(
@"class C
{
    public static string operator +(C a)
    {
        {|Cursor:[|return|]|} null;
        [|return|] null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInConversionOperator()
        {
            await TestAsync(
@"class C
{
    public static explicit operator string(C a)
    {
        {|Cursor:[|return|]|} null;
        [|return|] null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInGetter()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInSetter()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInAdder()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInRemover()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInLocalFunction()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInSimpleLambda()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInParenthesizedLambda()
        {
            await TestAsync(
@"class C
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestInAnonymousMethod()
        {
            await TestAsync(
@"class C
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
}");
        }
    }
}

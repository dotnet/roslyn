// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
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
        public async Task TestExample1_1()
        {
            await TestAsync(
                @"static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            [|{|Cursor:return|}|] Double.NaN;
        }
        else {
            [|return|] r * r * Math.PI;
        }
    };

    return calcArea(radius);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_2()
        {
            await TestAsync(
        @"class C {
static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            return {|Cursor:Double.NaN|};
        }
        else {
            return r * r * Math.PI;
        }
    };

    return calcArea(radius);
}
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_3()
        {
            await TestAsync(
@"class C {
    static double CalculateArea(double radius) {
        Func<double, double> f = r => {
            if (Double.IsNan(r)) {
                [|return|] Double.NaN;{|Cursor:|}
            }
            else {
                [|return|] r * r * Math.PI;
            }
        };

        return calcArea(radius);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_4()
        {
            await TestAsync(
        @"class C {
static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            [|return|] Double.NaN;
        }
        else {
            {|Cursor:[|return|]|} r * r * Math.PI;
        }
    };

    return calcArea(radius);
}
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_5()
        {
            await TestAsync(
        @"class C {
static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            return Double.NaN;
        }
        else {
            return {|Cursor:r * r * Math.PI|};
        }
    };

    return calcArea(radius);
}
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_6()
        {
            await TestAsync(
@"class C {
    static double CalculateArea(double radius) {
        Func<double, double> f = r => {
            if (Double.IsNan(r)) {
                [|return|] Double.NaN;
            }
            else {
                [|return|] r * r * Math.PI;{|Cursor:|}
            }
        };

        return calcArea(radius);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_7()
        {
            await TestAsync(
        @"class C {
static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            return Double.NaN;
        }
        else {
            return r * r * Math.PI;
        }
    };

    {|Cursor:[|return|]|} calcArea(radius);
}
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_8()
        {
            await TestAsync(
        @"class C {
static double CalculateArea(double radius) {
    Func<double, double> f = r => {
        if (Double.IsNan(r)) {
            return Double.NaN;
        }
        else {
            return r * r * Math.PI;
        }
    };

    return {|Cursor:calcArea(radius)|};
}
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_9()
        {
            await TestAsync(
@"class C {
    static double CalculateArea(double radius) {
        Func<double, double> f = r => {
            if (Double.IsNan(r)) {
                return Double.NaN;
            }
            else {
                return r * r * Math.PI;
            }
        };

        [|return|] calcArea(radius);{|Cursor:|}
    }
}");
        }
    }
}

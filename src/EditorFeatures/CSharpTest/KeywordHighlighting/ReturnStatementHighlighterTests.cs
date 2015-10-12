// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_2()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_3()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_4()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_5()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_6()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_7()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_8()
        {
            Test(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_9()
        {
            Test(
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

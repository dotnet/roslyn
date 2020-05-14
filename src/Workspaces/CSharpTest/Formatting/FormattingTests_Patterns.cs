// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingTests_Patterns : CSharpFormattingTestBase
    {
        [Theory, Trait(Traits.Feature, Traits.Features.Formatting)]
        [CombinatorialData]
        public async Task FormatRelationalPatterns1(
            [CombinatorialValues("<", "<=", ">", ">=")] string operatorText,
            BinaryOperatorSpacingOptions spacing)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  {operatorText}  3  or  {operatorText}  5;
    }}
}}
";

            var expectedSingle = $@"
class A
{{
    bool Method(int value)
    {{
        return value is {operatorText} 3 or {operatorText} 5;
    }}
}}
";
            var expectedIgnore = $@"
class A
{{
    bool Method(int value)
    {{
        return value is  {operatorText}  3  or  {operatorText}  5;
    }}
}}
";
            var expectedRemove = $@"
class A
{{
    bool Method(int value)
    {{
        return value is {operatorText}3 or {operatorText}5;
    }}
}}
";

            var expected = spacing switch
            {
                BinaryOperatorSpacingOptions.Single => expectedSingle,
                BinaryOperatorSpacingOptions.Ignore => expectedIgnore,
                BinaryOperatorSpacingOptions.Remove => expectedRemove,
                _ => throw ExceptionUtilities.Unreachable,
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Formatting)]
        [CombinatorialData]
        public async Task FormatRelationalPatterns2(
            [CombinatorialValues("<", "<=", ">", ">=")] string operatorText,
            BinaryOperatorSpacingOptions spacing,
            bool spaceWithinExpressionParentheses)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  (  {operatorText}  3  )  or  (  {operatorText}  5  )  ;
    }}
}}
";

            var expectedSingleFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText} 3) or ({operatorText} 5);
    }}
}}
";
            var expectedIgnoreFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText}  3)  or  ({operatorText}  5);
    }}
}}
";
            var expectedRemoveFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ({operatorText}3) or ({operatorText}5);
    }}
}}
";
            var expectedSingleTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText} 3 ) or ( {operatorText} 5 );
    }}
}}
";
            var expectedIgnoreTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText}  3 )  or  ( {operatorText}  5 );
    }}
}}
";
            var expectedRemoveTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( {operatorText}3 ) or ( {operatorText}5 );
    }}
}}
";

            var expected = (spacing, spaceWithinExpressionParentheses) switch
            {
                (BinaryOperatorSpacingOptions.Single, false) => expectedSingleFalse,
                (BinaryOperatorSpacingOptions.Ignore, false) => expectedIgnoreFalse,
                (BinaryOperatorSpacingOptions.Remove, false) => expectedRemoveFalse,
                (BinaryOperatorSpacingOptions.Single, true) => expectedSingleTrue,
                (BinaryOperatorSpacingOptions.Ignore, true) => expectedIgnoreTrue,
                (BinaryOperatorSpacingOptions.Remove, true) => expectedRemoveTrue,
                _ => throw ExceptionUtilities.Unreachable,
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
                { CSharpFormattingOptions2.SpaceWithinExpressionParentheses, spaceWithinExpressionParentheses },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Formatting)]
        [CombinatorialData]
        public async Task FormatNotPatterns1(BinaryOperatorSpacingOptions spacing)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  not  3  or  not  5;
    }}
}}
";

            var expectedSingle = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3 or not 5;
    }}
}}
";
            var expectedIgnore = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3  or  not 5;
    }}
}}
";
            var expectedRemove = $@"
class A
{{
    bool Method(int value)
    {{
        return value is not 3 or not 5;
    }}
}}
";

            var expected = spacing switch
            {
                BinaryOperatorSpacingOptions.Single => expectedSingle,
                BinaryOperatorSpacingOptions.Ignore => expectedIgnore,
                BinaryOperatorSpacingOptions.Remove => expectedRemove,
                _ => throw ExceptionUtilities.Unreachable,
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.Formatting)]
        [CombinatorialData]
        public async Task FormatNotPatterns2(
            BinaryOperatorSpacingOptions spacing,
            bool spaceWithinExpressionParentheses)
        {
            var content = $@"
class A
{{
    bool Method(int value)
    {{
        return value  is  (  not  3  )  or  (  not  5  );
    }}
}}
";

            var expectedSingleFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3) or (not 5);
    }}
}}
";
            var expectedIgnoreFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3)  or  (not 5);
    }}
}}
";
            var expectedRemoveFalse = $@"
class A
{{
    bool Method(int value)
    {{
        return value is (not 3) or (not 5);
    }}
}}
";
            var expectedSingleTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 ) or ( not 5 );
    }}
}}
";
            var expectedIgnoreTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 )  or  ( not 5 );
    }}
}}
";
            var expectedRemoveTrue = $@"
class A
{{
    bool Method(int value)
    {{
        return value is ( not 3 ) or ( not 5 );
    }}
}}
";

            var expected = (spacing, spaceWithinExpressionParentheses) switch
            {
                (BinaryOperatorSpacingOptions.Single, false) => expectedSingleFalse,
                (BinaryOperatorSpacingOptions.Ignore, false) => expectedIgnoreFalse,
                (BinaryOperatorSpacingOptions.Remove, false) => expectedRemoveFalse,
                (BinaryOperatorSpacingOptions.Single, true) => expectedSingleTrue,
                (BinaryOperatorSpacingOptions.Ignore, true) => expectedIgnoreTrue,
                (BinaryOperatorSpacingOptions.Remove, true) => expectedRemoveTrue,
                _ => throw ExceptionUtilities.Unreachable,
            };

            var changingOptions = new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpFormattingOptions2.SpacingAroundBinaryOperator, spacing },
                { CSharpFormattingOptions2.SpaceWithinExpressionParentheses, spaceWithinExpressionParentheses },
            };
            await AssertFormatAsync(expected, content, changedOptionSet: changingOptions);
        }
    }
}

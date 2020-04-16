// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class EditorConfigOptionParserTests
    {
        [Theory,
        InlineData("expressions", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData("type_casts", (int)SpacingWithinParenthesesOption.TypeCasts),
        InlineData("control_flow_statements", (int)SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("expressions, type_casts", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData("type_casts, expressions, , ", (int)SpacingWithinParenthesesOption.TypeCasts),
        InlineData("expressions ,  ,  , control_flow_statements", (int)SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("expressions ,  ,  , control_flow_statements", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData(",  ,  , control_flow_statements", (int)SpacingWithinParenthesesOption.ControlFlowStatements)]
        public void TestParseParenthesesSpaceOptionsTrue(string value, int parenthesesSpacingOption)
        {
            Assert.True(DetermineIfSpaceOptionIsSet(value, (SpacingWithinParenthesesOption)parenthesesSpacingOption),
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("expressions", (int)SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("type_casts", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData("control_flow_statements", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData("", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData(",,,", (int)SpacingWithinParenthesesOption.Expressions),
        InlineData("*", (int)SpacingWithinParenthesesOption.Expressions)]
        public void TestParseParenthesesSpaceOptionsFalse(string value, int parenthesesSpacingOption)
        {
            Assert.False(DetermineIfSpaceOptionIsSet(value, (SpacingWithinParenthesesOption)parenthesesSpacingOption),
                        $"Expected option {value} to be parsed as un-set.");
        }

        [Theory,
        InlineData("ignore", BinaryOperatorSpacingOptions.Ignore),
        InlineData("none", BinaryOperatorSpacingOptions.Remove),
        InlineData("before_and_after", BinaryOperatorSpacingOptions.Single)]
        public void TestParseEditorConfigSpacingAroundBinaryOperatorTrue(string value, BinaryOperatorSpacingOptions expectedResult)
        {
            Assert.True(ParseEditorConfigSpacingAroundBinaryOperator(value) == expectedResult,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("ignore,"),
        InlineData("non"),
        InlineData("before_and_after,ignore")]
        public void TestParseEditorConfigSpacingAroundBinaryOperatorFalse(string value)
        {
            Assert.True(ParseEditorConfigSpacingAroundBinaryOperator(value) == BinaryOperatorSpacingOptions.Single,
                        $"Expected option {value} to be parsed as default option.");
        }

        [Theory,
        InlineData("flush_left", LabelPositionOptions.LeftMost),
        InlineData("no_change", LabelPositionOptions.NoIndent),
        InlineData("one_less_than_current", LabelPositionOptions.OneLess)]
        public void TestParseEditorConfigLabelPositioningTrue(string value, LabelPositionOptions expectedValue)
        {
            Assert.True(ParseEditorConfigLabelPositioning(value) == expectedValue,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("left_most,"),
        InlineData("*"),
        InlineData("one_less_thancurrent")]
        public void TestParseEditorConfigLabelPositioningFalse(string value)
        {
            Assert.True(ParseEditorConfigLabelPositioning(value) == LabelPositionOptions.NoIndent,
                        $"Expected option {value} to be parsed default");
        }

        [Theory,
        InlineData("all", (int)NewLineOption.Types),
        InlineData("all,none", (int)NewLineOption.Types),
        InlineData("none,all", (int)NewLineOption.Types),
        InlineData("types", (int)NewLineOption.Types),
        InlineData("types,methods", (int)NewLineOption.Types),
        InlineData(",, types", (int)NewLineOption.Types),
        InlineData("accessors", (int)NewLineOption.Accessors),
        InlineData("methods", (int)NewLineOption.Methods),
        InlineData("properties", (int)NewLineOption.Properties),
        InlineData("indexers", (int)NewLineOption.Indexers),
        InlineData("events", (int)NewLineOption.Events),
        InlineData("anonymous_methods", (int)NewLineOption.AnonymousMethods),
        InlineData("control_blocks", (int)NewLineOption.ControlBlocks),
        InlineData("anonymous_types", (int)NewLineOption.AnonymousTypes),
        InlineData("object_collection_array_initalizers", (int)NewLineOption.ObjectCollectionsArrayInitializers),
        InlineData("object_collection_array_initializers", (int)NewLineOption.ObjectCollectionsArrayInitializers),
        InlineData("lambdas", (int)NewLineOption.Lambdas),
        InlineData("local_functions", (int)NewLineOption.LocalFunction)]
        public void TestParseNewLineOptionTrue(string value, int option)
        {
            Assert.True(DetermineIfNewLineOptionIsSet(value, (NewLineOption)option),
                        $"Expected option {value} to be set");
        }

        [Theory,
        InlineData("Accessors", (int)NewLineOption.Accessors),
        InlineData("none,types", (int)NewLineOption.Types),
        InlineData("methods", (int)NewLineOption.Types),
        InlineData("methods, properties", (int)NewLineOption.Types),
        InlineData(",,,", (int)NewLineOption.Types)]
        public void TestParseNewLineOptionFalse(string value, int option)
        {
            Assert.False(DetermineIfNewLineOptionIsSet(value, (NewLineOption)option),
                        $"Expected option {value} to be un-set");
        }

        [Theory,
        InlineData("ignore"),
        InlineData("ignore "),
        InlineData(" ignore"),
        InlineData(" ignore ")]
        public void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetTrue(string value)
        {
            Assert.True(DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be set");
        }

        [Theory,
        InlineData("do_not_ignore"),
        InlineData(", "),
        InlineData(" ignor ")]
        public void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetFalse(string value)
        {
            Assert.False(DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be un-set");
        }
    }
}

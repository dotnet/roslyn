// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class EditorConfigOptionParserTests
    {
        [Theory,
        InlineData("expressions", SpacingWithinParenthesesOption.Expressions),
        InlineData("type_casts", SpacingWithinParenthesesOption.TypeCasts),
        InlineData("control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("expressions, type_casts", SpacingWithinParenthesesOption.Expressions),
        InlineData("type_casts, expressions, , ", SpacingWithinParenthesesOption.TypeCasts),
        InlineData("expressions ,  ,  , control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("expressions ,  ,  , control_flow_statements", SpacingWithinParenthesesOption.Expressions),
        InlineData(",  ,  , control_flow_statements", SpacingWithinParenthesesOption.ControlFlowStatements)]
        static void TestParseParenthesesSpaceOptionsTrue(string value, CSharpFormattingOptions.SpacingWithinParenthesesOption parenthesesSpacingOption)
        {
            Assert.True(DetermineIfSpaceOptionIsSet(value, parenthesesSpacingOption),
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("expressions", SpacingWithinParenthesesOption.ControlFlowStatements),
        InlineData("type_casts", SpacingWithinParenthesesOption.Expressions),
        InlineData("control_flow_statements", SpacingWithinParenthesesOption.Expressions),
        InlineData("", SpacingWithinParenthesesOption.Expressions),
        InlineData(",,,", SpacingWithinParenthesesOption.Expressions),
        InlineData("*", SpacingWithinParenthesesOption.Expressions)]
        static void TestParseParenthesesSpaceOptionsFalse(string value, SpacingWithinParenthesesOption parenthesesSpacingOption)
        {
            Assert.False(DetermineIfSpaceOptionIsSet(value, parenthesesSpacingOption),
                        $"Expected option {value} to be parsed as un-set.");
        }

        [Theory,
        InlineData("ignore", BinaryOperatorSpacingOptions.Ignore),
        InlineData("none", BinaryOperatorSpacingOptions.Remove),
        InlineData("before_and_after", BinaryOperatorSpacingOptions.Single)]
        static void TestParseEditorConfigSpacingAroundBinaryOperatorTrue(string value, BinaryOperatorSpacingOptions expectedResult)
        {
            Assert.True(ParseEditorConfigSpacingAroundBinaryOperator(value) == expectedResult,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("ignore,"),
        InlineData("non"),
        InlineData("before_and_after,ignore")]
        static void TestParseEditorConfigSpacingAroundBinaryOperatorFalse(string value)
        {
            Assert.True(ParseEditorConfigSpacingAroundBinaryOperator(value) == BinaryOperatorSpacingOptions.Single,
                        $"Expected option {value} to be parsed as default option.");
        }

        [Theory,
        InlineData("flush_left", LabelPositionOptions.LeftMost),
        InlineData("no_change", LabelPositionOptions.NoIndent),
        InlineData("one_less_than_current", LabelPositionOptions.OneLess)]
        static void TestParseEditorConfigLabelPositioningTrue(string value, LabelPositionOptions expectedValue)
        {
            Assert.True(ParseEditorConfigLabelPositioning(value) == expectedValue,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("left_most,"),
        InlineData("*"),
        InlineData("one_less_thancurrent")]
        static void TestParseEditorConfigLabelPositioningFalse(string value)
        {
            Assert.True(ParseEditorConfigLabelPositioning(value) == LabelPositionOptions.NoIndent,
                        $"Expected option {value} to be parsed default");
        }

        [Theory,
        InlineData("all", NewLineOption.Types),
        InlineData("all,none", NewLineOption.Types),
        InlineData("none,all", NewLineOption.Types),
        InlineData("types", NewLineOption.Types),
        InlineData("types,methods", NewLineOption.Types),
        InlineData(",, types", NewLineOption.Types),
        InlineData("accessors", NewLineOption.Accessors),
        InlineData("methods", NewLineOption.Methods),
        InlineData("properties", NewLineOption.Properties),
        InlineData("indexers", NewLineOption.Indexers),
        InlineData("events", NewLineOption.Events),
        InlineData("anonymous_methods", NewLineOption.AnonymousMethods),
        InlineData("control_blocks", NewLineOption.ControlBlocks),
        InlineData("anonymous_types", NewLineOption.AnonymousTypes),
        InlineData("object_collection_array_initalizers", NewLineOption.ObjectCollectionsArrayInitializers),
        InlineData("object_collection_array_initializers", NewLineOption.ObjectCollectionsArrayInitializers),
        InlineData("lambdas", NewLineOption.Lambdas),
        InlineData("local_functions", NewLineOption.LocalFunction)]
        static void TestParseNewLineOptionTrue(string value, NewLineOption option)
        {
            Assert.True(DetermineIfNewLineOptionIsSet(value, option),
                        $"Expected option {value} to be set");
        }

        [Theory,
        InlineData("Accessors", NewLineOption.Accessors),
        InlineData("none,types", NewLineOption.Types),
        InlineData("methods", NewLineOption.Types),
        InlineData("methods, properties", NewLineOption.Types),
        InlineData(",,,", NewLineOption.Types)]
        static void TestParseNewLineOptionFalse(string value, NewLineOption option)
        {
            Assert.False(DetermineIfNewLineOptionIsSet(value, option),
                        $"Expected option {value} to be un-set");
        }

        [Theory,
        InlineData("ignore"),
        InlineData("ignore "),
        InlineData(" ignore"),
        InlineData(" ignore ")]
        static void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetTrue(string value)
        {
            Assert.True(DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be set");
        }

        [Theory,
        InlineData("do_not_ignore"),
        InlineData(", "),
        InlineData(" ignor ")]
        static void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetFalse(string value)
        {
            Assert.False(DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be un-set");
        }
    }
}

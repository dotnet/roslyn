// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class EditorConfigOptionParserTests
    {
        [Theory]
        [InlineData("expressions", SpacePlacementWithinParentheses.Expressions, "expressions")]
        [InlineData("type_casts", SpacePlacementWithinParentheses.TypeCasts, "type_casts")]
        [InlineData("control_flow_statements", SpacePlacementWithinParentheses.ControlFlowStatements, "control_flow_statements")]
        [InlineData("false", SpacePlacementWithinParentheses.None, "false")]
        [InlineData("", SpacePlacementWithinParentheses.None, "false")]
        [InlineData(",,,", SpacePlacementWithinParentheses.None, "false")]
        [InlineData("*", SpacePlacementWithinParentheses.None, "false")]
        [InlineData("expressions, type_casts", SpacePlacementWithinParentheses.Expressions | SpacePlacementWithinParentheses.TypeCasts, "expressions,type_casts")]
        [InlineData("type_casts, expressions, , ", SpacePlacementWithinParentheses.Expressions | SpacePlacementWithinParentheses.TypeCasts, "expressions,type_casts")]
        [InlineData("expressions ,  ,  , control_flow_statements", SpacePlacementWithinParentheses.Expressions | SpacePlacementWithinParentheses.ControlFlowStatements, "expressions,control_flow_statements")]
        [InlineData("expressions ,  , type_casts , control_flow_statements, type_casts", SpacePlacementWithinParentheses.Expressions | SpacePlacementWithinParentheses.ControlFlowStatements | SpacePlacementWithinParentheses.TypeCasts, "expressions,type_casts,control_flow_statements")]
        [InlineData(",  , x , control_flow_statements", SpacePlacementWithinParentheses.ControlFlowStatements, "control_flow_statements")]
        [InlineData("none,expressions", SpacePlacementWithinParentheses.Expressions, "expressions")]
        [InlineData("all,expressions", SpacePlacementWithinParentheses.Expressions, "expressions")]
        [InlineData("false,expressions", SpacePlacementWithinParentheses.Expressions, "expressions")]
        internal void TestParseSpacingWithinParenthesesList(string list, SpacePlacementWithinParentheses value, string roundtrip)
        {
            Assert.Equal(value, CSharpFormattingOptions2.ParseSpacingWithinParenthesesList(list));
            Assert.Equal(roundtrip, CSharpFormattingOptions2.ToEditorConfigValue(value));
        }

        [Theory,
        InlineData("ignore", BinaryOperatorSpacingOptions.Ignore),
        InlineData("none", BinaryOperatorSpacingOptions.Remove),
        InlineData("before_and_after", BinaryOperatorSpacingOptions.Single)]
        public void TestParseEditorConfigSpacingAroundBinaryOperatorTrue(string value, BinaryOperatorSpacingOptions expectedResult)
        {
            Assert.True(CSharpFormattingOptions2.ParseEditorConfigSpacingAroundBinaryOperator(value) == expectedResult,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("ignore,"),
        InlineData("non"),
        InlineData("before_and_after,ignore")]
        public void TestParseEditorConfigSpacingAroundBinaryOperatorFalse(string value)
        {
            Assert.True(CSharpFormattingOptions2.ParseEditorConfigSpacingAroundBinaryOperator(value) == BinaryOperatorSpacingOptions.Single,
                        $"Expected option {value} to be parsed as default option.");
        }

        [Theory,
        InlineData("flush_left", LabelPositionOptions.LeftMost),
        InlineData("no_change", LabelPositionOptions.NoIndent),
        InlineData("one_less_than_current", LabelPositionOptions.OneLess)]
        public void TestParseEditorConfigLabelPositioningTrue(string value, LabelPositionOptions expectedValue)
        {
            Assert.True(CSharpFormattingOptions2.ParseEditorConfigLabelPositioning(value) == expectedValue,
                        $"Expected option {value} to be parsed as set.");
        }

        [Theory,
        InlineData("left_most,"),
        InlineData("*"),
        InlineData("one_less_thancurrent")]
        public void TestParseEditorConfigLabelPositioningFalse(string value)
        {
            Assert.True(CSharpFormattingOptions2.ParseEditorConfigLabelPositioning(value) == LabelPositionOptions.NoIndent,
                        $"Expected option {value} to be parsed default");
        }

        [Theory]
        [InlineData("all",
            NewLineBeforeOpenBracePlacement.Types |
            NewLineBeforeOpenBracePlacement.Methods |
            NewLineBeforeOpenBracePlacement.Properties |
            NewLineBeforeOpenBracePlacement.AnonymousMethods |
            NewLineBeforeOpenBracePlacement.ControlBlocks |
            NewLineBeforeOpenBracePlacement.AnonymousTypes |
            NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers |
            NewLineBeforeOpenBracePlacement.LambdaExpressionBody |
            NewLineBeforeOpenBracePlacement.Accessors, "all")]
        [InlineData("all,none", NewLineBeforeOpenBracePlacement.All, "all")]
        [InlineData("none,all", NewLineBeforeOpenBracePlacement.All, "all")]
        [InlineData("types", NewLineBeforeOpenBracePlacement.Types, "types")]
        [InlineData("types,methods", NewLineBeforeOpenBracePlacement.Types | NewLineBeforeOpenBracePlacement.Methods, "types,methods")]
        [InlineData("methods,types", NewLineBeforeOpenBracePlacement.Types | NewLineBeforeOpenBracePlacement.Methods, "types,methods")]
        [InlineData("methods, properties", NewLineBeforeOpenBracePlacement.Methods | NewLineBeforeOpenBracePlacement.Properties, "methods,properties")]
        [InlineData(",, types", NewLineBeforeOpenBracePlacement.Types, "types")]
        [InlineData("accessors", NewLineBeforeOpenBracePlacement.Accessors, "accessors")]
        [InlineData("methods", NewLineBeforeOpenBracePlacement.Methods, "methods")]
        [InlineData("properties", NewLineBeforeOpenBracePlacement.Properties, "properties")]
        [InlineData("anonymous_methods", NewLineBeforeOpenBracePlacement.AnonymousMethods, "anonymous_methods")]
        [InlineData("control_blocks", NewLineBeforeOpenBracePlacement.ControlBlocks, "control_blocks")]
        [InlineData("anonymous_types", NewLineBeforeOpenBracePlacement.AnonymousTypes, "anonymous_types")]
        [InlineData("object_collection_array_initalizers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, "object_collection_array_initializers")]
        [InlineData("object_collection_array_initializers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, "object_collection_array_initializers")]
        [InlineData("lambdas", NewLineBeforeOpenBracePlacement.LambdaExpressionBody, "lambdas")]
        [InlineData("Accessors", NewLineBeforeOpenBracePlacement.None, "none")]
        [InlineData("none,types", NewLineBeforeOpenBracePlacement.None, "none")]
        [InlineData(",,,", NewLineBeforeOpenBracePlacement.None, "none")]
        internal void TestParseNewLineBeforeOpenBracePlacementList(string list, NewLineBeforeOpenBracePlacement value, string roundtrip)
        {
            Assert.Equal(value, CSharpFormattingOptions2.ParseNewLineBeforeOpenBracePlacementList(list));
            Assert.Equal(roundtrip, CSharpFormattingOptions2.ToEditorConfigValue(value));
        }

        [Theory,
        InlineData("ignore"),
        InlineData("ignore "),
        InlineData(" ignore"),
        InlineData(" ignore ")]
        public void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetTrue(string value)
        {
            Assert.True(CSharpFormattingOptions2.DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be set");
        }

        [Theory,
        InlineData("do_not_ignore"),
        InlineData(", "),
        InlineData(" ignor ")]
        public void TestDetermineIfIgnoreSpacesAroundVariableDeclarationIsSetFalse(string value)
        {
            Assert.False(CSharpFormattingOptions2.DetermineIfIgnoreSpacesAroundVariableDeclarationIsSet(value),
                        $"Expected option {value} to be un-set");
        }
    }
}

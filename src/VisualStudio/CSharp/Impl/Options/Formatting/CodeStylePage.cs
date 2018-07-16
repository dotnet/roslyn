// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    [Guid(Guids.CSharpOptionPageCodeStyleIdString)]
    internal class CodeStylePage : AbstractOptionPage
    {
        protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider)
        {
            return new GridOptionPreviewControl(serviceProvider, (o, s) => new StyleViewModel(o, s), GetCurrentEditorConfigOptionsCSharp, LanguageNames.CSharp);
        }

        internal static void GetCurrentEditorConfigOptionsCSharp(OptionSet optionSet, StringBuilder editorconfig)
        {
            editorconfig.AppendLine();
            editorconfig.AppendLine("# C# Coding Conventions");

            editorconfig.AppendLine("# var preferences");
            // csharp_style_var_for_built_in_types
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, editorconfig);
            // csharp_style_var_when_type_is_apparent
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, editorconfig);
            // csharp_style_var_elsewhere
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWherePossible, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Expression-bodied members");
            // csharp_style_expression_bodied_methods
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedMethods, editorconfig);
            // csharp_style_expression_bodied_constructors
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, editorconfig);
            // csharp_style_expression_bodied_operators
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedOperators, editorconfig);
            // csharp_style_expression_bodied_properties
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedProperties, editorconfig);
            // csharp_style_expression_bodied_indexers
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, editorconfig);
            // csharp_style_expression_bodied_accessors
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Pattern matching preferences");
            // csharp_style_pattern_matching_over_is_with_cast_check
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, editorconfig);
            // csharp_style_pattern_matching_over_as_with_null_check
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Null-checking preferences");
            // csharp_style_throw_expression
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferThrowExpression, editorconfig);
            // csharp_style_conditional_delegate_call
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferConditionalDelegateCall, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Modifier preferences");
            // csharp_preferred_modifier_order
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferredModifierOrder, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Expression-level preferences");
            // csharp_prefer_braces
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferBraces, editorconfig);
            // csharp_style_deconstructed_variable_declaration
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferDeconstructedVariableDeclaration, editorconfig);
            // csharp_prefer_simple_default_expression
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferSimpleDefaultExpression, editorconfig);
            // csharp_style_pattern_local_over_anonymous_function
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, editorconfig);
            // csharp_style_inlined_variable_declaration
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInlinedVariableDeclaration, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# C# Formatting Rules");
            editorconfig.AppendLine("# New line preferences");
            // csharp_new_line_before_open_brace
            CSharpNewLineBeforeOpenBrace_GenerateEditorconfig(optionSet, editorconfig);
            // csharp_new_line_before_else
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForElse, editorconfig);
            // csharp_new_line_before_catch
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForCatch, editorconfig);
            // csharp_new_line_before_finally
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForFinally, editorconfig);
            // csharp_new_line_before_members_in_object_initializers
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForMembersInObjectInit, editorconfig);
            // csharp_new_line_before_members_in_anonymous_types
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, editorconfig);
            // csharp_new_line_between_query_expression_clauses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.NewLineForClausesInQuery, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Indentation preferences");
            // csharp_indent_case_contents
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.IndentSwitchCaseSection, editorconfig);
            // chsarp_indent_switch_labels
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.IndentSwitchSection, editorconfig);
            // csharp_indent_labels
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.LabelPositioning, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Space preferences");
            // csharp_space_after_cast
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterCast, editorconfig);
            // csharp_space_after_keywords_in_control_flow_statements
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, editorconfig);
            // csharp_space_between_method_call_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses, editorconfig);
            // csharp_space_between_method_declaration_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, editorconfig);
            // csharp_space_between_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, editorconfig);
            // csharp_space_before_colon_in_inheritance_clause
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, editorconfig);
            // csharp_space_after_colon_in_inheritance_clause
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, editorconfig);
            // csharp_space_around_binary_operators
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpacingAroundBinaryOperator, editorconfig);
            // csharp_space_between_method_declaration_empty_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, editorconfig);
            // csharp_space_between_method_call_name_and_opening_parenthesis
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterMethodCallName, editorconfig);
            // csharp_space_between_method_call_empty_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, editorconfig);

            editorconfig.AppendLine();
            editorconfig.AppendLine("# Wrapping preferences");
            // csharp_preserve_single_line_statements
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, editorconfig);
            // csharp_preserve_single_line_blocks
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.WrappingPreserveSingleLine, editorconfig);
        }

        private static void CSharpFormattingOptions_GenerateEditorconfig(OptionSet optionSet, Option<bool> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<bool>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);

            editorconfig.Append(" = ");

            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForElse).ToString().ToLower());
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<bool>> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<CodeStyleOption<bool>>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);

            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
            editorconfig.AppendLine(curSetting.Value.ToString().ToLower() + ":" + curSetting.Notification.ToString().ToLower());
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<bool>> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<CodeStyleOption<bool>>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);

            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option);
            editorconfig.AppendLine(curSetting.Value.ToString().ToLower() + ":" + curSetting.Notification.ToString().ToLower());
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<string>> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<CodeStyleOption<string>>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);
            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option);
            editorconfig.AppendLine(curSetting.Value.ToString().ToLower() + ":" + curSetting.Notification.ToString().ToLower());
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<ExpressionBodyPreference>> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<CodeStyleOption<ExpressionBodyPreference>>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);

            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option);
            if (curSetting.Value == ExpressionBodyPreference.Never)
            {
                editorconfig.AppendLine("false" + ":" + curSetting.Notification.ToString().ToLower());
            }
            else
            {
                editorconfig.AppendLine("true" + ":" + curSetting.Notification.ToString().ToLower());
            }
        }

        private static void CSharpFormattingOptions_GenerateEditorconfig(OptionSet optionSet, Option<LabelPositionOptions> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<LabelPositionOptions>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);

            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option);
            if (curSetting == LabelPositionOptions.LeftMost)
            {
                editorconfig.AppendLine("flush_left");
            }
            else if (curSetting == LabelPositionOptions.OneLess)
            {
                editorconfig.AppendLine("one_less_than_current");
            }
            else
            {
                editorconfig.AppendLine("no_change");
            }
        }

        private static void CSharpFormattingOptions_GenerateEditorconfig(OptionSet optionSet, Option<BinaryOperatorSpacingOptions> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<BinaryOperatorSpacingOptions>)option.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);
            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option);
            if (curSetting == BinaryOperatorSpacingOptions.Single)
            {
                editorconfig.AppendLine("before_and_after");
            }
            else if (curSetting == BinaryOperatorSpacingOptions.Remove)
            {
                editorconfig.AppendLine("none");
            }
            else
            {
                editorconfig.AppendLine("ignore");
            }
        }

        private static void CSharpFormattingOptions_GenerateEditorconfig(OptionSet optionSet, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<bool>)CSharpFormattingOptions.SpaceWithinOtherParentheses.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);
            editorconfig.Append(" = ");

            var firstRule = true;
            if (optionSet.GetOption(CSharpFormattingOptions.SpaceWithinOtherParentheses))
            {
                editorconfig.Append("control_flow_statements");
                firstRule = false;
            }
            if (optionSet.GetOption(CSharpFormattingOptions.SpaceWithinExpressionParentheses))
            {
                if (firstRule)
                {
                    firstRule = false;
                }
                else
                {
                    editorconfig.Append(",");
                }

                editorconfig.Append("expressions");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.SpaceWithinCastParentheses))
            {
                if (firstRule)
                {
                    firstRule = false;
                }
                else
                {
                    editorconfig.Append(",");
                }

                editorconfig.Append("type_casts");
            }

            if (firstRule)
            {
                editorconfig.AppendLine("false");
            }
            else
            {
                editorconfig.AppendLine();
            }
        }

        private static void CSharpNewLineBeforeOpenBrace_GenerateEditorconfig(OptionSet optionSet, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<bool>)CSharpFormattingOptions.NewLinesForBracesInAccessors.StorageLocations.OfType<IEditorConfigStorageLocation>().SingleOrDefault()).KeyName);;
            editorconfig.Append(" = ");

            const int totalRules = 9;
            var rulesApplied = 0;
            var ruleString = new StringBuilder();
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAccessors))
            {
                ++rulesApplied;
                ruleString.Append("accessors");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("anonymous_methods");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("anonymous_types");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInControlBlocks))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("control_blocks");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("lambdas");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInMethods))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("methods");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("object_collection");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInProperties))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("properties");
            }
            if (optionSet.GetOption(CSharpFormattingOptions.NewLinesForBracesInTypes))
            {
                if (ruleString.Length != 0)
                {
                    ruleString.Append(",");
                }

                ++rulesApplied;
                ruleString.Append("types");
            }

            if (rulesApplied == totalRules)
            {
                editorconfig.AppendLine("all");
            }
            else if (rulesApplied == 0)
            {
                editorconfig.AppendLine("none");
            }
            else
            {
                editorconfig.AppendLine(ruleString.ToString());
            }
        }
    }
}

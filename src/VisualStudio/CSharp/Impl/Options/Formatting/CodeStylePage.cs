// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
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

        internal static void Generate_Editorconfig(
            OptionSet optionSet,
            string language,
            StringBuilder editorconfig)
        {
            GridOptionPreviewControl.Generate_Editorconfig(optionSet, language, editorconfig, GetCurrentEditorConfigOptionsCSharp);
        }

        private static void GetCurrentEditorConfigOptionsCSharp(OptionSet optionSet, StringBuilder editorconfig)
        {
            editorconfig.AppendLine();

            editorconfig.AppendLine("# C# Coding Conventions");
            editorconfig.AppendLine("# " + CSharpVSResources.var_preferences_colon);
            // csharp_style_var_for_built_in_types
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, editorconfig);
            // csharp_style_var_when_type_is_apparent
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, editorconfig);
            // csharp_style_var_elsewhere
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWherePossible, editorconfig);
            editorconfig.AppendLine();

            editorconfig.AppendLine("# Expression-bodied members:");
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

            editorconfig.AppendLine("# Pattern matching preferences:");
            // csharp_style_pattern_matching_over_is_with_cast_check
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, editorconfig);
            // csharp_style_pattern_matching_over_as_with_null_check
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, editorconfig);
            editorconfig.AppendLine();

            editorconfig.AppendLine("# Null-checking preferences:");
            // csharp_style_throw_expression
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferThrowExpression, editorconfig);
            // csharp_style_conditional_delegate_call
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferConditionalDelegateCall, editorconfig);
            editorconfig.AppendLine();

            editorconfig.AppendLine("# Modifier preferences:");
            // csharp_preferred_modifier_order
            CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferredModifierOrder, editorconfig);
            editorconfig.AppendLine();

            editorconfig.AppendLine("# Expression-level preferences:");
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
            editorconfig.AppendLine("# New line preferences:");
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

            editorconfig.AppendLine("# Indentation preferences:");
            // csharp_indent_case_contents
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.IndentSwitchCaseSection, editorconfig);
            // chsarp_indent_switch_labels
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.IndentSwitchSection, editorconfig);
            // csharp_indent_labels
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.LabelPositioning, editorconfig);
            editorconfig.AppendLine();

            editorconfig.AppendLine("# Space preferences:");
            // csharp_space_after_cast
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterCast, editorconfig);
            // csharp_space_after_keywords_in_control_flow_statements
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, editorconfig);
            // csharp_space_between_method_call_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceWithinMethodCallParentheses, editorconfig);
            // csharp_space_between_method_declaration_parameter_list_parentheses
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, editorconfig);
            // csharp_space_between_parentheses
            CSharpSpaceBetweenParentheses_GenerateEditorconfig(optionSet, editorconfig);
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

            editorconfig.AppendLine("# Wrapping preferences:");
            // csharp_preserve_single_line_statements
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, editorconfig);
            // csharp_preserve_single_line_blocks
            CSharpFormattingOptions_GenerateEditorconfig(optionSet, CSharpFormattingOptions.WrappingPreserveSingleLine, editorconfig);
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<bool>> option, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<bool>>>().FirstOrDefault();
            if (element != null)
            {
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig);

                var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
                editorconfig.AppendLine(curSetting.Value.ToString().ToLower() + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification));
            }
        }

        private static void CSharpCodeStyleOptions_GenerateEditorconfig<T>(OptionSet optionSet, Option<CodeStyleOption<T>> option, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<CodeStyleOption<T>>>().FirstOrDefault();
            if (element != null)
            {
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig);

                var curSetting = optionSet.GetOption(option);
                if (typeof(T) == typeof(ExpressionBodyPreference))
                {
                    switch((ExpressionBodyPreference)(object) curSetting.Value)
                    {
                        case ExpressionBodyPreference.Never:
                            editorconfig.AppendLine("false" + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification));
                            break;
                        case ExpressionBodyPreference.WhenPossible:
                            editorconfig.AppendLine("true" + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification));
                            break;
                        case ExpressionBodyPreference.WhenOnSingleLine:
                            editorconfig.AppendLine("when_on_single_line" + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification));
                            break;
                        default:
                            throw new NotSupportedException();
                    };
                }
                else if (typeof(T) == typeof(bool) || typeof(T) == typeof(string))
                {
                    editorconfig.AppendLine(curSetting.Value.ToString().ToLowerInvariant() + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static void CSharpFormattingOptions_GenerateEditorconfig<T>(OptionSet optionSet, Option<T> option, StringBuilder editorconfig)
        {
            var element = option.StorageLocations.OfType<EditorConfigStorageLocation<T>>().FirstOrDefault();
            if (element != null)
            {
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig);

                var curSetting = optionSet.GetOption(option);
                if (typeof(T) == typeof(LabelPositionOptions))
                {
                    switch((LabelPositionOptions)(object) curSetting)
                    {
                        case LabelPositionOptions.LeftMost:
                            editorconfig.AppendLine("flush_left");
                            break;
                        case LabelPositionOptions.OneLess:
                            editorconfig.AppendLine("one_less_than_current");
                            break;
                        case LabelPositionOptions.NoIndent:
                            editorconfig.AppendLine("no_change");
                            break;
                        default:
                            throw new NotSupportedException();
                    };
                }
                else if (typeof(T) == typeof(BinaryOperatorSpacingOptions))
                {
                    switch((BinaryOperatorSpacingOptions)(object) curSetting)
                    {
                        case BinaryOperatorSpacingOptions.Single:
                            editorconfig.AppendLine("before_and_after");
                            break;
                        case BinaryOperatorSpacingOptions.Remove:
                            editorconfig.AppendLine("none");
                            break;
                        case BinaryOperatorSpacingOptions.Ignore:
                            editorconfig.AppendLine("ignore");
                            break;
                        default:
                            throw new NotSupportedException();
                    };
                }
                else if (typeof(T) == typeof(bool))
                {
                    editorconfig.AppendLine(optionSet.GetOption(option).ToString().ToLowerInvariant());
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static void CSharpSpaceBetweenParentheses_GenerateEditorconfig(OptionSet optionSet, StringBuilder editorconfig)
        {
            var element = CSharpFormattingOptions.SpaceWithinOtherParentheses.StorageLocations.OfType<EditorConfigStorageLocation<bool>>().FirstOrDefault();
            if (element != null)
            {
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig);

                var valuesApplied = JoinMultipleValues(OptionToValue(), optionSet, editorconfig);

                if (valuesApplied == 0)
                {
                    editorconfig.AppendLine("false");
                }
                else
                {
                    editorconfig.AppendLine();
                }
            }

            Dictionary<Option<bool>, string> OptionToValue()
            {
                return new Dictionary<Option<bool>, string>()
                {
                { CSharpFormattingOptions.SpaceWithinOtherParentheses, "control_flow_statements" },
                { CSharpFormattingOptions.SpaceWithinExpressionParentheses, "expressions" },
                { CSharpFormattingOptions.SpaceWithinCastParentheses, "type_casts" }
                };
            }
        }

        private static void CSharpNewLineBeforeOpenBrace_GenerateEditorconfig(OptionSet optionSet, StringBuilder editorconfig)
        {
            var element = CSharpFormattingOptions.NewLinesForBracesInAccessors.StorageLocations.OfType<EditorConfigStorageLocation<bool>>().FirstOrDefault();
            if (element != null)
            {
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig);

                var allOptions = OptionToValue();
                var ruleString = new StringBuilder();
                var valuesApplied = JoinMultipleValues(allOptions, optionSet, ruleString);

                if (valuesApplied == allOptions.Count())
                {
                    editorconfig.AppendLine("all");
                }
                else if (valuesApplied == 0)
                {
                    editorconfig.AppendLine("none");
                }
                else
                {
                    editorconfig.AppendLine(ruleString.ToString());
                }
            }

            Dictionary<Option<bool>, string> OptionToValue()
            {
                return new Dictionary<Option<bool>, string>()
                {
                { CSharpFormattingOptions.NewLinesForBracesInAccessors, "accessors" },
                { CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, "anonymous_methods" },
                { CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, "anonymous_types" },
                { CSharpFormattingOptions.NewLinesForBracesInControlBlocks, "control_blocks" },
                { CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, "lambdas" },
                { CSharpFormattingOptions.NewLinesForBracesInMethods, "methods" },
                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, "object_collection" },
                { CSharpFormattingOptions.NewLinesForBracesInProperties, "properties" },
                { CSharpFormattingOptions.NewLinesForBracesInTypes, "types" }
                };
            }
        }

        private static int JoinMultipleValues(Dictionary<Option<bool>, string> allOptions, OptionSet optionSet, StringBuilder ruleString)
        {
            var valuesApplied = 0;
            foreach (var curValue in allOptions)
            {
                if (optionSet.GetOption(curValue.Key))
                {
                    if (ruleString.Length != 0)
                    {
                        ruleString.Append(",");
                    }
                    ruleString.Append(curValue.Value);
                    ++valuesApplied;
                }
            }
            return valuesApplied;
        }
    }
}

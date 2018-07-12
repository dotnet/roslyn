// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            return new GridOptionPreviewControl(serviceProvider, (o, s) => new StyleViewModel(o, s), GetCurrentEditorConfigOptionsString, LanguageNames.CSharp);
        }

        internal static string GetCurrentEditorConfigOptionsString(OptionSet optionSet)
        {
            var editorconfig = new StringBuilder();
            // Core EditorConfig Options
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# Core EditorConfig Options   #");
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# You can uncomment the next line if this is your top-most .editorconfig file.");
            editorconfig.AppendLine("# root = true");

            editorconfig.AppendLine();
            editorconfig.AppendLine("# C# files");
            editorconfig.AppendLine("[*.cs]");
            editorconfig.Append("indent_style = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, FormattingOptions.UseTabs));
            editorconfig.Append("indent_size = ");
            editorconfig.AppendLine(optionSet.GetOption(FormattingOptions.IndentationSize, LanguageNames.CSharp).ToString());
            editorconfig.Append("insert_final_newline = ");
            editorconfig.AppendLine(optionSet.GetOption(FormattingOptions.InsertFinalNewLine).ToString().ToLower());

            editorconfig.AppendLine();
            // .NET Coding Conventions
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# .NET Coding Conventions     #");
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# Organize usings");
            editorconfig.Append("dotnet_sort_system_directives_first = ");
            editorconfig.AppendLine(optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp).ToString().ToLower());

            editorconfig.AppendLine();
            // this. preferences
            editorconfig.AppendLine("# this. preferences");
            // dotnet_style_qualification_for_field
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyFieldAccess, editorconfig);
            // dotnet_style_qualification_for_property
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyPropertyAccess, editorconfig);
            // dotnet_style_qualification_for_method
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyMethodAccess, editorconfig);
            // dotnet_style_qualification_for_event
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.QualifyEventAccess, editorconfig);

            editorconfig.AppendLine();
            // Language keywords vs. BCL types preferences
            editorconfig.AppendLine("# Language keywords vs BCL types preferences");
            // dotnet_style_predefined_type_for_locals_parameters_members
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, editorconfig);
            // dotnet_style_predefined_type_for_member_access
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, editorconfig);

            editorconfig.AppendLine();
            // Parentheses preferences
            editorconfig.AppendLine("# Parentheses preferences");
            editorconfig.Append("dotnet_style_parentheses_in_arithmetic_binary_operators = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.ArithmeticBinaryParentheses));
            editorconfig.Append("dotnet_style_parentheses_in_relational_binary_operators = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RelationalBinaryParentheses));
            editorconfig.Append("dotnet_style_parentheses_in_other_binary_operators = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherBinaryParentheses));
            editorconfig.Append("dotnet_style_parentheses_in_other_operators = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.OtherParentheses));

            editorconfig.AppendLine();
            // Modifier preferences
            editorconfig.AppendLine("# Modifier preferences");
            editorconfig.Append("dotnet_style_require_accessibility_modifiers = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.RequireAccessibilityModifiers));
            // dotnet_style_readonly_field
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferReadonly, editorconfig);

            editorconfig.AppendLine();
            // Expression-level preferences
            editorconfig.AppendLine("# Expression-level preferences");
            // dotnet_style_object_initializer
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferObjectInitializer, editorconfig);
            // dotnet_style_collection_initializer
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCollectionInitializer, editorconfig);
            // dotnet_style_explicit_tuple_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferExplicitTupleNames, editorconfig);
            // dotnet_style_null_propagation
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferNullPropagation, editorconfig);
            // dotnet_style_coalesce_expression
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferCoalesceExpression, editorconfig);
            // dotnet_style_prefer_is_null_check_over_reference_equality_method
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, editorconfig);
            // dotnet_prefer_inferred_tuple_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredTupleNames, editorconfig);
            // dotnet_prefer_inferred_anonymous_type_member_names
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, editorconfig);
            // dotnet_style_prefer_auto_properties
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferAutoProperties, editorconfig);
            // dotnet_style_prefer_conditional_expression_over_assignment
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverAssignment, editorconfig);
            // dotnet_style_prefer_conditional_expression_over_return
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferConditionalExpressionOverReturn, editorconfig);

            editorconfig.AppendLine();
            // C# Coding Conventions
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# C# Coding Conventions       #");
            editorconfig.AppendLine("###############################");

            // Var preferences
            editorconfig.AppendLine("# var preferences");
            editorconfig.Append("csharp_style_var_for_built_in_types = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes));
            editorconfig.Append("csharp_style_var_when_type_is_apparent = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWhereApparent));
            editorconfig.Append("csharp_style_var_elsewhere = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.UseImplicitTypeWherePossible));

            editorconfig.AppendLine();
            // Expression-bodied members
            editorconfig.AppendLine("# Expression-bodied members");
            editorconfig.Append("csharp_style_expression_bodied_methods = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedMethods));
            editorconfig.Append("csharp_style_expression_bodied_constructors = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedConstructors));
            editorconfig.Append("csharp_style_expression_bodied_operators = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedOperators));
            editorconfig.Append("csharp_style_expression_bodied_properties = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedProperties));
            editorconfig.Append("csharp_style_expression_bodied_indexers = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedIndexers));
            editorconfig.Append("csharp_style_expression_bodied_accessors = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferExpressionBodiedAccessors));

            editorconfig.AppendLine();
            // Pattern matching preferences
            editorconfig.AppendLine("# Pattern matching preferences");
            editorconfig.Append("csharp_style_pattern_matching_over_is_with_cast_check = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck));
            editorconfig.Append("csharp_style_pattern_matching_over_as_with_null_check = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck));

            editorconfig.AppendLine();
            // Null-checking preferences
            editorconfig.AppendLine("# Null-checking preferences");
            // csharp_style_throw_expression
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferThrowExpression, editorconfig);
            editorconfig.Append("csharp_style_conditional_delegate_call = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferConditionalDelegateCall));

            editorconfig.AppendLine();
            // Modifier preferences
            editorconfig.AppendLine("# Modifier preferences");
            editorconfig.Append("csharp_preferred_modifier_order = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferredModifierOrder));

            editorconfig.AppendLine();
            // Expression-level preferences
            editorconfig.AppendLine("# Expression-level preferences");
            editorconfig.Append("csharp_prefer_braces = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferBraces));
            // csharp_style_deconstructed_variable_declaration
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferDeconstructedVariableDeclaration, editorconfig);
            editorconfig.Append("csharp_prefer_simple_default_expression = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferSimpleDefaultExpression));
            editorconfig.Append("csharp_style_pattern_local_over_anonymous_function = ");
            editorconfig.AppendLine(CSharpCodeStyleOptions_GenerateEditorconfig(optionSet, CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction));
            // csharp_style_inlined_variable_declaration
            DotNetCodeStyleOptions_GenerateEditorconfig(optionSet, CodeStyleOptions.PreferInlinedVariableDeclaration, editorconfig);

            editorconfig.AppendLine();
            // C# Formatting Rules
            editorconfig.AppendLine("###############################");
            editorconfig.AppendLine("# C# Formatting Rules         #");
            editorconfig.AppendLine("###############################");

            editorconfig.AppendLine();
            // New line preferences
            editorconfig.AppendLine("# New line preferences");
            editorconfig.Append("csharp_new_line_before_open_brace = ");
            editorconfig.AppendLine(CSharpNewLineBeforeOpenBrace_GenerateEditorconfig(optionSet));
            editorconfig.Append("csharp_new_line_before_else = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForElse).ToString().ToLower());
            editorconfig.Append("csharp_new_line_before_catch = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForCatch).ToString().ToLower());
            editorconfig.Append("csharp_new_line_before_finally = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForFinally).ToString().ToLower());
            editorconfig.Append("csharp_new_line_before_members_in_object_initializers = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForMembersInObjectInit).ToString().ToLower());
            editorconfig.Append("csharp_new_line_before_members_in_anonymous_types = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes).ToString().ToLower());
            editorconfig.Append("csharp_new_line_between_query_expression_clauses = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.NewLineForClausesInQuery).ToString().ToLower());

            editorconfig.AppendLine();
            // Indentation preferences
            editorconfig.AppendLine("# Indentation preferences");
            editorconfig.Append("csharp_indent_case_contents = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.IndentSwitchCaseSection).ToString().ToLower());
            editorconfig.Append("csharp_indent_labels = ");
            editorconfig.AppendLine(CSharpLabelPositioning_GenerateEditorconfig(optionSet.GetOption(CSharpFormattingOptions.LabelPositioning)));

            editorconfig.AppendLine();
            // Space preferences
            editorconfig.AppendLine("# Space preferences");
            editorconfig.Append("csharp_space_after_cast = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceAfterCast).ToString().ToLower());
            editorconfig.Append("csharp_space_after_keywords_in_control_flow_statements = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword).ToString().ToLower());
            editorconfig.Append("csharp_space_between_method_call_parameter_list_parentheses = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceWithinMethodCallParentheses).ToString().ToLower());
            editorconfig.Append("csharp_space_between_method_declaration_parameter_list_parentheses = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis).ToString().ToLower());
            editorconfig.Append("csharp_space_between_parentheses = ");
            CSharpSpaceBetweenParentheses_GenerateEditorconfig(optionSet, editorconfig);
            editorconfig.AppendLine();
            editorconfig.Append("csharp_space_before_colon_in_inheritance_clause = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration).ToString().ToLower());
            editorconfig.Append("csharp_space_after_colon_in_inheritance_clause = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration).ToString().ToLower());
            editorconfig.Append("csharp_space_around_binary_operators = ");
            editorconfig.AppendLine(CSharpSpacingAroundBinaryOperator_GenerateEditorconfig(optionSet.GetOption(CSharpFormattingOptions.SpacingAroundBinaryOperator)));
            editorconfig.Append("csharp_space_between_method_declaration_empty_parameter_list_parentheses = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses).ToString().ToLower());
            editorconfig.Append("csharp_space_between_method_call_name_and_opening_parenthesis = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceAfterMethodCallName).ToString().ToLower());
            editorconfig.Append("csharp_space_between_method_call_empty_parameter_list_parentheses = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses).ToString().ToLower());

            editorconfig.AppendLine();
            // Wrapping preferences
            editorconfig.AppendLine("# Wrapping preferences");
            editorconfig.Append("csharp_preserve_single_line_statements = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine).ToString().ToLower());
            editorconfig.Append("csharp_preserve_single_line_blocks = ");
            editorconfig.AppendLine(optionSet.GetOption(CSharpFormattingOptions.WrappingPreserveSingleLine).ToString().ToLower());

            return editorconfig.ToString();
        }

        private static void CSharpSpaceBetweenParentheses_GenerateEditorconfig(OptionSet optionSet, StringBuilder editorconfig)
        {
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
                editorconfig.Append("false");
            }
        }

        private static string CSharpNewLineBeforeOpenBrace_GenerateEditorconfig(OptionSet optionSet)
        {
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
                return "all";
            }
            else if (rulesApplied == 0)
            {
                return "none";
            }
            else
            {
                return ruleString.ToString();
            }
        }

        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<bool> option)
        {
            var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
            if (curSetting)
            {
                return "tab";
            }
            else
            {
                return "space";
            }
        }

        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<AccessibilityModifiersRequired>> option)
        {
            var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
            var editorconfig = "";
            if (curSetting.Value == AccessibilityModifiersRequired.ForNonInterfaceMembers)
            {
                editorconfig += "for_non_interface_members:" + curSetting.Notification.ToString().ToLower();
            }
            else if (curSetting.Value == AccessibilityModifiersRequired.OmitIfDefault)
            {
                editorconfig += "omit_if_default:" + curSetting.Notification.ToString().ToLower();
            }
            else
            {
                editorconfig += (curSetting.Value + ":" + curSetting.Notification).ToLower();
            }

            return editorconfig;
        }

        private static void DotNetCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<bool>> option, StringBuilder editorconfig)
        {
            editorconfig.Append(((EditorConfigStorageLocation<CodeStyleOption<bool>>) option.StorageLocations[0]).KeyName);
            editorconfig.Append(" = ");

            var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
            editorconfig.AppendLine(curSetting.Value.ToString().ToLower() + ":" + curSetting.Notification.ToString().ToLower());
        }

        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<bool>> option)
        {
            var curSetting = optionSet.GetOption(option);
            var editorconfig = curSetting.Value + ":" + curSetting.Notification;
            return editorconfig.ToLower();
        }
        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<string>> option)
        {
            var curSetting = optionSet.GetOption(option);
            var editorconfig = curSetting.Value + ":" + curSetting.Notification;
            return editorconfig.ToLower();
        }

        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, Option<CodeStyleOption<ExpressionBodyPreference>> option)
        {
            var curSetting = optionSet.GetOption(option);
            var editorconfig = "";
            if (curSetting.Value == ExpressionBodyPreference.Never)
            {
                editorconfig = "false" + ":" + curSetting.Notification.ToString().ToLower();
            }
            else
            {
                editorconfig = "true" + ":" + curSetting.Notification.ToString().ToLower();
            }

            return editorconfig;
        }
        private static string CSharpCodeStyleOptions_GenerateEditorconfig(OptionSet optionSet, PerLanguageOption<CodeStyleOption<ParenthesesPreference>> option)
        {
            var curSetting = optionSet.GetOption(option, LanguageNames.CSharp);
            var editorconfig = "";
            if (curSetting.Value == ParenthesesPreference.AlwaysForClarity)
            {
                editorconfig += "always_for_clarity:" + curSetting.Notification.ToString().ToLower();
            }
            else
            {
                editorconfig += "never_if_unnecessary:" + curSetting.Notification.ToString().ToLower();
            }

            return editorconfig;
        }


        private static string CSharpLabelPositioning_GenerateEditorconfig(LabelPositionOptions option)
        {
            if (option == LabelPositionOptions.LeftMost)
            {
                return "flush_left";
            }
            else if (option == LabelPositionOptions.OneLess)
            {
                return "one_less_than_current";
            }
            else
            {
                return "no_change";
            }
        }

        private static string CSharpSpacingAroundBinaryOperator_GenerateEditorconfig(BinaryOperatorSpacingOptions option)
        {
            if (option == BinaryOperatorSpacingOptions.Single)
            {
                return "before_and_after";
            }
            else if (option == BinaryOperatorSpacingOptions.Remove)
            {
                return "none";
            }
            else
            {
                return "ignore";
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// Interaction logic for FormattingSpacingOptionControl.xaml
    /// </summary>
    internal class SpacingViewModel : AbstractOptionPreviewViewModel
    {
        private const string s_methodPreview = @"
class C {
//[
    void Goo(){
        Goo(1);
    }

    void Goo(int x){
        Goo();
    }
//]
    void Goo(int x, int y){
        Goo();
    }
}";

        private const string s_bracketPreview = @"class C {
    void Goo(){
//[
        int[] x = new int[10];
//]
    }
}";
        private const string s_forDelimiterPreview = @"class C{
    void Goo(int x, object y) {
//[
        for (int i; i < x; i++) {
        }
//]
    }
}";

        private const string s_delimiterPreview = @"class C{
    void Goo(int x, object y) {
//[
            this.Goo(x, y);
//]
    }
}";

        private const string s_castPreview = @"class C{
    void Goo(object x) {
//[
        int y = (int)x;
//]
    }
}";

        private const string s_expressionPreview = @"class C{
    void Goo(int x, object y) {
//[
        var x = 3;
        var y = 4;
        var z = (x * y) - ((y -x) * 3);
//]
    }
}";

        private const string s_expressionSpacingPreview = @"
class c {
    int Goo(int x, int y) {
//[
        return x   *   (x-y);
//]
    }
}";
        private const string s_declarationSpacingPreview = @"class MyClass {
//[
    int         index = 0;
    string      text = ""Start"";

    void        Method(){
        int     i = 0;
        string  s = ""Hello"";
                }
//]
}";
        private const string s_baseColonPreview = @"//[
interface I {
}

class C : I {
}
//]";

        public SpacingViewModel(OptionStore optionStore, IServiceProvider serviceProvider) : base(optionStore, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_spacing_for_method_declarations });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, CSharpVSResources.Insert_space_between_method_name_and_its_opening_parenthesis2, s_methodPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, CSharpVSResources.Insert_space_within_parameter_list_parentheses, s_methodPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, CSharpVSResources.Insert_space_within_empty_parameter_list_parentheses, s_methodPreview, this, optionStore));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_spacing_for_method_calls });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterMethodCallName, CSharpVSResources.Insert_space_between_method_name_and_its_opening_parenthesis1, s_methodPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinMethodCallParentheses, CSharpVSResources.Insert_space_within_argument_list_parentheses, s_methodPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, CSharpVSResources.Insert_space_within_empty_argument_list_parentheses, s_methodPreview, this, optionStore));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_other_spacing_options });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, CSharpVSResources.Insert_space_after_keywords_in_control_flow_statements, s_forDelimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinExpressionParentheses, CSharpVSResources.Insert_space_within_parentheses_of_expressions, s_expressionPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinCastParentheses, CSharpVSResources.Insert_space_within_parentheses_of_type_casts, s_castPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinOtherParentheses, CSharpVSResources.Insert_spaces_within_parentheses_of_control_flow_statements, s_forDelimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterCast, CSharpVSResources.Insert_space_after_cast, s_castPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, CSharpVSResources.Ignore_spaces_in_declaration_statements, s_declarationSpacingPreview, this, optionStore));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_spacing_for_brackets });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, CSharpVSResources.Insert_space_before_open_square_bracket, s_bracketPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, CSharpVSResources.Insert_space_within_empty_square_brackets, s_bracketPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinSquareBrackets, CSharpVSResources.Insert_spaces_within_square_brackets, s_bracketPreview, this, optionStore));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_spacing_for_delimiters });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, CSharpVSResources.Insert_space_after_colon_for_base_or_interface_in_type_declaration, s_baseColonPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterComma, CSharpVSResources.Insert_space_after_comma, s_delimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterDot, CSharpVSResources.Insert_space_after_dot, s_delimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, CSharpVSResources.Insert_space_after_semicolon_in_for_statement, s_forDelimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, CSharpVSResources.Insert_space_before_colon_for_base_or_interface_in_type_declaration, s_baseColonPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeComma, CSharpVSResources.Insert_space_before_comma, s_delimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeDot, CSharpVSResources.Insert_space_before_dot, s_delimiterPreview, this, optionStore));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, CSharpVSResources.Insert_space_before_semicolon_in_for_statement, s_forDelimiterPreview, this, optionStore));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.Set_spacing_for_operators });

            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.Ignore_spaces_around_binary_operators, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Ignore, CSharpFormattingOptions2.SpacingAroundBinaryOperator, this, OptionStore));
            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.Remove_spaces_before_and_after_binary_operators, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Remove, CSharpFormattingOptions2.SpacingAroundBinaryOperator, this, OptionStore));
            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.Insert_space_before_and_after_binary_operators, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Single, CSharpFormattingOptions2.SpacingAroundBinaryOperator, this, OptionStore));
        }
    }
}

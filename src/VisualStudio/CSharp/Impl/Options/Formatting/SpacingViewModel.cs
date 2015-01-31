// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal override bool ShouldPersistOption(OptionKey key)
        {
            return key.Option.Feature == CSharpFormattingOptions.SpacingFeatureName;
        }

        private static readonly string s_methodPreview = @"
class C {
//[
    void Foo(){
        Foo(1);
    }

    void Foo(int x){
        Foo();
    }
//]
    void Foo(int x, int y){
        Foo();
    }
}";

        private static readonly string s_bracketPreview = @"class C {
    void Foo(){
//[
        int[] x = new int[10];
//]
    }
}";
        private static readonly string s_forDelimiterPreview = @"class C{
    void Foo(int x, object y) {
//[
        for (int i; i < x; i++) {
        }
//]
    }
}";

        private static readonly string s_delimiterPreview = @"class C{
    void Foo(int x, object y) {
//[
            this.Foo(x, y);
//]
    }
}";

        private static readonly string s_castPreview = @"class C{
    void Foo(object x) {
//[
        int y = (int)x;
//]
    }
}";

        private static readonly string s_expressionPreview = @"class C{
    void Foo(int x, object y) {
//[
        var x = 3;
        var y = 4;
        var z = (x * y) - ((y -x) * 3);
//]
    }
}";

        private static readonly string s_expressionSpacingPreview = @"
class c {
    int Foo(int x, int y) {
//[
        return x   *   (x-y);
//]
    }
}";
        private static readonly string s_declarationSpacingPreview = @"class MyClass {
//[
    int         index = 0;
    string      text = ""Start"";

    void        Method(){
        int     i = 0;
        string  s = ""Hello"";
                }
//]
}";
        private static readonly string s_baseColonPreview = @"//[
interface I {
}

class C : I {
}
//]";

        public SpacingViewModel(OptionSet options, IServiceProvider serviceProvider) : base(options, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetSpacingForMethodDeclarations });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, CSharpVSResources.SpacingAfterMethodDeclarationName, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, CSharpVSResources.SpaceWithinMethodDeclarationParenthesis, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, CSharpVSResources.SpaceBetweenEmptyMethodDeclarationParentheses, s_methodPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetSpacingForMethodCalls });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterMethodCallName, CSharpVSResources.SpaceAfterMethodCallName, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinMethodCallParentheses, CSharpVSResources.SpaceWithinMethodCallParentheses, s_methodPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, CSharpVSResources.SpaceBetweenEmptyMethodCallParentheses, s_methodPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetOtherSpacingOptions });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, CSharpVSResources.SpaceAfterControlFlowStatementKeyword, s_forDelimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinExpressionParentheses, CSharpVSResources.SpaceWithinExpressionParentheses, s_expressionPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinCastParentheses, CSharpVSResources.SpaceWithinCastParentheses, s_castPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinOtherParentheses, CSharpVSResources.SpaceWithinOtherParentheses, s_forDelimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterCast, CSharpVSResources.SpaceAfterCast, s_castPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, CSharpVSResources.SpacesIgnoreAroundVariableDeclaration, s_declarationSpacingPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetSpacingForBrackets });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, CSharpVSResources.SpaceBeforeOpenSquareBracket, s_bracketPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, CSharpVSResources.SpaceBetweenEmptySquareBrackets, s_bracketPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceWithinSquareBrackets, CSharpVSResources.SpaceWithinSquareBrackets, s_bracketPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetSpacingForDelimiters });

            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, CSharpVSResources.SpaceAfterColonInBaseTypeDeclaration, s_baseColonPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterComma, CSharpVSResources.SpaceAfterComma, s_delimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterDot, CSharpVSResources.SpaceAfterDot, s_delimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, CSharpVSResources.SpaceAfterSemicolonsInForStatement, s_forDelimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, CSharpVSResources.SpaceBeforeColonInBaseTypeDeclaration, s_baseColonPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeComma, CSharpVSResources.SpaceBeforeComma, s_delimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeDot, CSharpVSResources.SpaceBeforeDot, s_delimiterPreview, this, options));
            Items.Add(new CheckBoxOptionViewModel(CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, CSharpVSResources.SpaceBeforeSemicolonsInForStatement, s_forDelimiterPreview, this, options));

            Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetSpacingForOperators });

            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.BinaryOperatorSpaceIgnore, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Ignore, CSharpFormattingOptions.SpacingAroundBinaryOperator, this, Options));
            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.BinaryOperatorSpaceRemove, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Remove, CSharpFormattingOptions.SpacingAroundBinaryOperator, this, Options));
            Items.Add(new RadioButtonViewModel<BinaryOperatorSpacingOptions>(CSharpVSResources.BinaryOperatorSpace, s_expressionSpacingPreview, "binary", BinaryOperatorSpacingOptions.Single, CSharpFormattingOptions.SpacingAroundBinaryOperator, this, Options));
        }
    }
}

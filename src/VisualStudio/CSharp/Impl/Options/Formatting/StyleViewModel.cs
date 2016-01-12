// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    internal class StyleViewModel : AbstractOptionPreviewViewModel
    {
        internal override bool ShouldPersistOption(OptionKey key)
        {
            return key.Option.Feature == CSharpCodeStyleOptions.FeatureName || key.Option.Feature == SimplificationOptions.PerLanguageFeatureName;
        }

        private static readonly string s_declarationPreviewTrue = @"
class C{
    int x;
    void foo()
    {
//[
        this.x = 0;
//]
    }
}";

        private static readonly string s_declarationPreviewFalse = @"
class C{
    int x;
    void foo()
    {
//[
        x = 0;
//]
    }
}";

        private static readonly string s_varPreviewTrue = @"
class C{
    void foo()
    {
//[
        var x = 0;
//]
    }
}";

        private static readonly string s_varPreviewFalse = @"
class C{
    void foo()
    {
//[
        int x = 0;
//]
    }
}";

        private static readonly string s_intrinsicPreviewDeclarationTrue = @"
class Program
{
//[
    private int _member;
    static void M(int argument)
    {
        int local;
    }
//]
}";

        private static readonly string s_intrinsicPreviewDeclarationFalse = @"
using System;
class Program
{
//[
    private Int32 _member;
    static void M(Int32 argument)
    {
        Int32 local;
    }
//]
}";

        private static readonly string s_intrinsicPreviewMemberAccessTrue = @"
class Program
{
//[
    static void M()
    {
        var local = int.MaxValue;
    }
//]
}";

        private static readonly string s_intrinsicPreviewMemberAccessFalse = @"
using System;
class Program
{
//[
    static void M()
    {
        var local = Int32.MaxValue;
    }
//]
}";

        private static readonly string s_ImplicitTypingPreview = @"
class C{
    void foo()
    {
//[
        var x = 0;
//]
    }
}";

        private static readonly string s_ExplicitTypingPreview = @"
class C{
    void foo()
    {
//[
        int x = 0;
//]
    }
}";

        private static readonly string s_ImplicitTypingWhereApparentPreview = @"
class C{
    void foo()
    {
//[
        var x = new Program();
//]
    }
}";

        private static readonly string s_ExplicitTypingForIntrinsicTypesPreview = @"
class C{
    void foo()
    {
//[
        string s = ""Hello"";
//]
    }
}";
        internal StyleViewModel(OptionSet optionSet, IServiceProvider serviceProvider) : base(optionSet, serviceProvider, LanguageNames.CSharp)
        {
            Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.QualifyMemberAccessWithThisOrMe, CSharpVSResources.QualifyMemberAccessWithThis, s_declarationPreviewTrue, s_declarationPreviewFalse, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals, CSharpVSResources.UseVarWhenGeneratingLocals, s_varPreviewTrue, s_varPreviewFalse, this, optionSet));

            // TODO (BalajiK): Localize all strings from here.
            Items.Add(new HeaderItemViewModel() { Header = "Type Inference preference for local declarations everywhere:" });

            Items.Add(new RadioButtonViewModel<TypeInferencePreferenceOptions>("use implicit typing", s_ImplicitTypingPreview, "usevar", TypeInferencePreferenceOptions.ImplicitTyping, CSharpCodeStyleOptions.UseImplicitTypingForLocals, this, optionSet));
            Items.Add(new RadioButtonViewModel<TypeInferencePreferenceOptions>("use explicit typing", s_ExplicitTypingPreview, "usevar", TypeInferencePreferenceOptions.ExplicitTyping, CSharpCodeStyleOptions.UseImplicitTypingForLocals, this, optionSet));

            Items.Add(new HeaderItemViewModel() { Header = "Type Inference preference for local declarations in special cases:" });

            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent, "use var where typing is apparent", s_ImplicitTypingWhereApparentPreview, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.DoNotUseVarForIntrinsicTypes, "use intrinsic types", s_ExplicitTypingForIntrinsicTypesPreview, this, optionSet));
        }
    }
}

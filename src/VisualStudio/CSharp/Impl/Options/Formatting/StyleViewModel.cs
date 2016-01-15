// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

        private static readonly string s_varForIntrinsicsPreviewFalse = @"
using System;
class C{
    void foo()
    {
//[
        int x = 5; // intrinsic types
//]
    }
}";

        private static readonly string s_varForIntrinsicsPreviewTrue = @"
using System;
class C{
    void foo()
    {
//[
        var x = 5; // intrinsic types
//]
    }
}";

        private static readonly string s_varWhereApparentPreviewFalse = @"
using System;
class C{
    void foo()
    {
//[
        C cobj = new C(); // typing is apparent from assignment expression
//]
    }
}";

        private static readonly string s_varWhereApparentPreviewTrue = @"
using System;
class C{
    void foo()
    {
//[
        var cobj = new C(); // typing is apparent from assignment expression
//]
    }
}";

        private static readonly string s_varWherePossiblePreviewFalse = @"
using System;
class C{
    void foo()
    {
//[
        Action f = this.foo(); // everywhere else.
//]
    }
}";

        private static readonly string s_varWherePossiblePreviewTrue = @"
using System;
class C{
    void foo()
    {
//[
        var f = this.foo(); // everywhere else.
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
            Items.Add(new HeaderItemViewModel() { Header = "Type Inference preferences:" });

            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarForIntrinsicTypes, "use var for intrinsic types", s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarWhenTypeIsApparent, "use var where typing is apparent", s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet));
            Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarWherePossible, "use var where possible", s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet));
        }
    }
}

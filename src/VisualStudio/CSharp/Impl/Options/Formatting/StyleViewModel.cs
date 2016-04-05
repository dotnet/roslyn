// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    /// <summary>
    /// This is the view model for CodeStyle options page.
    /// </summary>
    /// <remarks>
    /// The codestyle options page is defined in <see cref="CodeStylePage"/>
    /// </remarks>
    internal class StyleViewModel : AbstractOptionPreviewViewModel
    {
        internal override bool ShouldPersistOption(OptionKey key)
        {
            return key.Option.Feature == CSharpCodeStyleOptions.FeatureName || key.Option.Feature == SimplificationOptions.PerLanguageFeatureName;
        }

        #region "Preview Text"

        private static readonly string s_fieldDeclarationPreviewTrue = @"
class C{
    int capacity;
    void Method()
    {
//[
        this.capacity = 0;
//]
    }
}";

        private static readonly string s_fieldDeclarationPreviewFalse = @"
class C{
    int capacity;
    void Method()
    {
//[
        capacity = 0;
//]
    }
}";

        private static readonly string s_propertyDeclarationPreviewTrue = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        this.Id = 0;
//]
    }
}";

        private static readonly string s_propertyDeclarationPreviewFalse = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        Id = 0;
//]
    }
}";

        private static readonly string s_eventDeclarationPreviewTrue = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        this.Elapsed += Handler;
//]
    }
}";

        private static readonly string s_eventDeclarationPreviewFalse = @"
using System;
class C{
    event EventHandler Elapsed;
    void Handler(object sender, EventArgs args)
    {
//[
        Elapsed += Handler;
//]
    }
}";

        private static readonly string s_methodDeclarationPreviewTrue = @"
using System;
class C{
    void Display()
    {
//[
        this.Display();
//]
    }
}";

        private static readonly string s_methodDeclarationPreviewFalse = @"
using System;
class C{
    void Display()
    {
//[
        Display();
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
    void Method()
    {
//[
        int x = 5; // built-in types
//]
    }
}";

        private static readonly string s_varForIntrinsicsPreviewTrue = @"
using System;
class C{
    void Method()
    {
//[
        var x = 5; // built-in types
//]
    }
}";

        private static readonly string s_varWhereApparentPreviewFalse = @"
using System;
class C{
    void Method()
    {
//[
        C cobj = new C(); // type is apparent from assignment expression
//]
    }
}";

        private static readonly string s_varWhereApparentPreviewTrue = @"
using System;
class C{
    void Method()
    {
//[
        var cobj = new C(); // type is apparent from assignment expression
//]
    }
}";

        private static readonly string s_varWherePossiblePreviewFalse = @"
using System;
class C{
    void Init()
    {
//[
        Action f = this.Init(); // everywhere else.
//]
    }
}";

        private static readonly string s_varWherePossiblePreviewTrue = @"
using System;
class C{
    void Init()
    {
//[
        var f = this.Init(); // everywhere else.
//]
    }
}";
        #endregion

        internal StyleViewModel(OptionSet optionSet, IServiceProvider serviceProvider) : base(optionSet, serviceProvider, LanguageNames.CSharp)
        {
            var collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(CodeStyleItems);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AbstractCodeStyleOptionViewModel.GroupName)));

            var qualifyGroupTitle = CSharpVSResources.QualifyGroupTitle;
            var predefinedTypesGroupTitle = CSharpVSResources.PredefinedTypesGroupTitle;
            var varGroupTitle = CSharpVSResources.VarGroupTitle;

            var qualifyMemberAccessPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.PreferThis, isChecked: true),
                new CodeStylePreference(CSharpVSResources.DoNotPreferThis, isChecked: false),
            };

            var predefinedTypesPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.PreferPredefinedType, isChecked: true),
                new CodeStylePreference(CSharpVSResources.PreferFrameworkType, isChecked: false),
            };

            var useVarPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.PreferVar, isChecked: true),
                new CodeStylePreference(CSharpVSResources.PreferExplicitType, isChecked: false),
            };

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.QualifyFieldAccess, CSharpVSResources.QualifyFieldAccessWithThis, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.QualifyPropertyAccess, CSharpVSResources.QualifyPropertyAccessWithThis, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.QualifyMethodAccess, CSharpVSResources.QualifyMethodAccessWithThis, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.QualifyEventAccess, CSharpVSResources.QualifyEventAccessWithThis, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CSharpVSResources.UseVarForIntrinsicTypes, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CSharpVSResources.UseVarWhenTypeIsApparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CSharpVSResources.UseVarWhenPossible, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));
        }
    }
}

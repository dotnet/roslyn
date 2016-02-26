// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting
{
    // Must work with GridOptionPreviewControl
    internal class StyleViewModel : AbstractOptionPreviewViewModel
    {
        public ObservableCollection<SimpleCodeStyleOptionViewModel> CodeStyleItems { get; set; }

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


            CodeStyleItems = new ObservableCollection<SimpleCodeStyleOptionViewModel>();

            ListCollectionView collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(CodeStyleItems);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SimpleCodeStyleOptionViewModel.GroupName)));

            //this works:
            //CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(SimplificationOptions.QualifyMemberAccessWithThisOrMe, CSharpVSResources.QualifyMemberAccessWithThis, s_declarationPreviewTrue, s_declarationPreviewFalse, this, optionSet));
            //CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet));
            //CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet));
            
            //CodeStyleItems.Add(SimpleCodeStyleOptionViewModel.Header("'var' preferences"));

            var useVarPreferences = new List<CodeStylePreference>
            {
                // TODO: move to resx for loc.
                new CodeStylePreference("Prefer 'var'", true),
                new CodeStylePreference("Prefer explicit type", false),
            };
            const string varGroupTitle = "'var' preferences:";

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CSharpVSResources.UseVarForIntrinsicTypes, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CSharpVSResources.UseVarWhenTypeIsApparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CSharpVSResources.UseVarWhenPossible, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, varGroupTitle, useVarPreferences));

            //trying this:
            //CodeStyleItems.Add(new StyleOptionViewModel(new CodeStyleItem(CSharpVSResources.UseVarForIntrinsicTypes, defaultPreferences)));
            //CodeStyleItems.Add(new StyleOptionViewModel(new CodeStyleItem(CSharpVSResources.UseVarWhenTypeIsApparent, defaultPreferences)));
            //CodeStyleItems.Add(new StyleOptionViewModel(new CodeStyleItem(CSharpVSResources.UseVarWhenPossible, defaultPreferences)));

            // badness ensues when i store it in Items. hrmm..

            // Old code:
            //Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.QualifyMemberAccessWithThisOrMe, CSharpVSResources.QualifyMemberAccessWithThis, s_declarationPreviewTrue, s_declarationPreviewFalse, this, optionSet));
            //Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet));
            //Items.Add(new CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, CSharpVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet));
            //Items.Add(new CheckBoxOptionViewModel(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals, CSharpVSResources.UseVarWhenGeneratingLocals, s_varPreviewTrue, s_varPreviewFalse, this, optionSet));

            //Items.Add(new HeaderItemViewModel() { Header = CSharpVSResources.SetTypeInferencePreferences });

            //var notificationOptions = new List<NotificationOptionViewModel>
            //                          {
            //                              new NotificationOptionViewModel(NotificationOption.None, KnownMonikers.None),
            //                              new NotificationOptionViewModel(NotificationOption.Info, KnownMonikers.StatusInformation),
            //                              new NotificationOptionViewModel(NotificationOption.Warning, KnownMonikers.StatusWarning),
            //                              new NotificationOptionViewModel(NotificationOption.Error, KnownMonikers.StatusError)
            //                          };

            //Items.Add(new CheckBoxWithComboOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CSharpVSResources.UseVarForIntrinsicTypes, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, notificationOptions));
            //Items.Add(new CheckBoxWithComboOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CSharpVSResources.UseVarWhenTypeIsApparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, notificationOptions));
            //Items.Add(new CheckBoxWithComboOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CSharpVSResources.UseVarWhenPossible, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, notificationOptions));
        }

        //private List<CodeStyleItem> _codeStyleItems;

        //public List<CodeStyleItem> CodeStyleItems
        //{
        //    get
        //    {
        //        return _codeStyleItems;
        //    }
        //}

        //public class CodeStyleItem // TODO: StyleOptionViewModel?
        //{
        //    public CodeStyleItem(string description)
        //    {
        //        this.Description = description;
        //        var selectedPreference = "No";
        //        this.Preferences = new List<string> { "Yes", selectedPreference };
        //        this.SelectedPreference = selectedPreference;
        //        var selectedNotificationPreference = "None";
        //        this.NotificationPreferences = new List<string> { selectedNotificationPreference, "Info", "Warning", "Error" };
        //        this.SelectedNotificationPreference = selectedNotificationPreference;
        //    }
        //    public string Description { get; set; }
        //    public List<string> Preferences { get; set; }
        //    public List<string> NotificationPreferences { get; set; }
        //    public string SelectedPreference { get; set; }
        //    public string SelectedNotificationPreference { get; set; }
        //}
    }
}

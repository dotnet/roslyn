// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
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

        private static readonly string s_preferThrowExpression = @"
using System;

class C
{
    private string s;

    public C(string s)
    {
//[
        // Prefer:
        this.s = s ?? throw new ArgumentNullException(nameof(s));

        // Over:
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        this.s = s;
//]
    }
}
";

        private static readonly string s_preferConditionalDelegateCall = @"
using System;

class C
{
    private string s;

    public C(string s)
    {
//[
        // Prefer:
        func?.Invoke(args);

        // Over:
        if (func != null)
        {
            func(args);
        }
//]
    }
}
";

        private static readonly string s_preferObjectInitializer = @"
using System;

class Customer
{
    private int Age;

    public Customer()
    {
//[
        // Prefer:
        var c = new Customer()
        {
            Age = 21
        };

        // Over:
        var c = new Customer();
        c.Age = 21;
//]
    }
}
";
        #endregion

        internal StyleViewModel(OptionSet optionSet, IServiceProvider serviceProvider) : base(optionSet, serviceProvider, LanguageNames.CSharp)
        {
            var collectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(CodeStyleItems);
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(AbstractCodeStyleOptionViewModel.GroupName)));

            var qualifyGroupTitle = CSharpVSResources.this_preferences_colon;
            var predefinedTypesGroupTitle = CSharpVSResources.predefined_type_preferences_colon;
            var varGroupTitle = CSharpVSResources.var_preferences_colon;
            var nullCheckingGroupTitle = CSharpVSResources.null_checking_colon;
            var expressionPreferencesGroupTitle = ServicesVSResources.Expression_preferences_colon;

            var qualifyMemberAccessPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Prefer_this, isChecked: true),
                new CodeStylePreference(CSharpVSResources.Do_not_prefer_this, isChecked: false),
            };

            var predefinedTypesPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(ServicesVSResources.Prefer_predefined_type, isChecked: true),
                new CodeStylePreference(ServicesVSResources.Prefer_framework_type, isChecked: false),
            };

            var typeStylePreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Prefer_var, isChecked: true),
                new CodeStylePreference(CSharpVSResources.Prefer_explicit_type, isChecked: false),
            };

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyFieldAccess, CSharpVSResources.Qualify_field_access_with_this, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyPropertyAccess, CSharpVSResources.Qualify_property_access_with_this, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyMethodAccess, CSharpVSResources.Qualify_method_access_with_this, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyEventAccess, CSharpVSResources.Qualify_event_access_with_this, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CSharpVSResources.For_built_in_types, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CSharpVSResources.When_variable_type_is_apparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CSharpVSResources.Elsewhere, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));

            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.PreferObjectInitializer, ServicesVSResources.Prefer_object_initializer, s_preferObjectInitializer, s_preferObjectInitializer, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CodeStyleOptions.PreferThrowExpression, CSharpVSResources.Prefer_throw_expression, s_preferThrowExpression, s_preferThrowExpression, this, optionSet, nullCheckingGroupTitle));
            CodeStyleItems.Add(new SimpleCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferConditionalDelegateCall, CSharpVSResources.Prefer_conditional_delegate_call, s_preferConditionalDelegateCall, s_preferConditionalDelegateCall, this, optionSet, nullCheckingGroupTitle));
        }
    }
}
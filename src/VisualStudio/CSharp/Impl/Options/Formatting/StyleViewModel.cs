﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
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

        private const string s_fieldDeclarationPreviewTrue = @"
class C{
    int capacity;
    void Method()
    {
//[
        this.capacity = 0;
//]
    }
}";

        private const string s_fieldDeclarationPreviewFalse = @"
class C{
    int capacity;
    void Method()
    {
//[
        capacity = 0;
//]
    }
}";

        private const string s_propertyDeclarationPreviewTrue = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        this.Id = 0;
//]
    }
}";

        private const string s_propertyDeclarationPreviewFalse = @"
class C{
    public int Id { get; set; }
    void Method()
    {
//[
        Id = 0;
//]
    }
}";

        private const string s_eventDeclarationPreviewTrue = @"
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

        private const string s_eventDeclarationPreviewFalse = @"
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

        private const string s_methodDeclarationPreviewTrue = @"
using System;
class C{
    void Display()
    {
//[
        this.Display();
//]
    }
}";

        private const string s_methodDeclarationPreviewFalse = @"
using System;
class C{
    void Display()
    {
//[
        Display();
//]
    }
}";

        private const string s_intrinsicPreviewDeclarationTrue = @"
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

        private const string s_intrinsicPreviewDeclarationFalse = @"
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

        private const string s_intrinsicPreviewMemberAccessTrue = @"
class Program
{
//[
    static void M()
    {
        var local = int.MaxValue;
    }
//]
}";

        private const string s_intrinsicPreviewMemberAccessFalse = @"
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

        private static readonly string s_varForIntrinsicsPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        int x = 5; // {ServicesVSResources.built_in_types}
//]
    }}
}}";

        private static readonly string s_varForIntrinsicsPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var x = 5; // {ServicesVSResources.built_in_types}
//]
    }}
}}";

        private static readonly string s_varWhereApparentPreviewFalse = $@"
using System;
class C{{
    void Method()
    {{
//[
        C cobj = new C(); // {ServicesVSResources.type_is_apparent_from_assignment_expression}
//]
    }}
}}";

        private static readonly string s_varWhereApparentPreviewTrue = $@"
using System;
class C{{
    void Method()
    {{
//[
        var cobj = new C(); // {ServicesVSResources.type_is_apparent_from_assignment_expression}
//]
    }}
}}";

        private static readonly string s_varWherePossiblePreviewFalse = $@"
using System;
class C{{
    void Init()
    {{
//[
        Action f = this.Init(); // {ServicesVSResources.everywhere_else}
//]
    }}
}}";

        private static readonly string s_varWherePossiblePreviewTrue = $@"
using System;
class C{{
    void Init()
    {{
//[
        var f = this.Init(); // {ServicesVSResources.everywhere_else}
//]
    }}
}}";

        private static readonly string s_preferThrowExpression = $@"
using System;

class C
{{
    private string s;

    public C(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        this.s = s ?? throw new ArgumentNullException(nameof(s));

        // {ServicesVSResources.Over_colon}
        if (s == null)
        {{
            throw new ArgumentNullException(nameof(s));
        }}

        this.s = s;
//]
    }}
}}
";

        private static readonly string s_preferCoalesceExpression = $@"
using System;

class C
{{
    private string s;

    public C(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = x ?? y;

        // {ServicesVSResources.Over_colon}
        var v = x != null ? x : y; // {ServicesVSResources.or}
        var v = x == null ? y : x;
//]
    }}
}}
";

        private static readonly string s_preferConditionalDelegateCall = $@"
using System;

class C
{{
    private string s;

    public C(string s)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        func?.Invoke(args);

        // {ServicesVSResources.Over_colon}
        if (func != null)
        {{
            func(args);
        }}
//]
    }}
}}
";

    private static readonly string s_preferNullPropagation = $@"
using System;

class C
{{
    public C(object o)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var v = o?.ToString();

        // {ServicesVSResources.Over_colon}
        var v = o == null ? null : o.ToString(); // {ServicesVSResources.or}
        var v = o != null ? o.ToString() : null;
//]
    }}
}}
";

        private static readonly string s_preferPatternMatchingOverAsWithNullCheck = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (o is string s)
        {{
        }}

        // {ServicesVSResources.Over_colon}
        var s = o as string;
        if (s != null)
        {{
        }}
//]
    }}
}}
";

        private static readonly string s_preferPatternMatchingOverIsWithCastCheck = $@"
class C
{{
    void M()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (o is int i)
        {{
        }}

        // {ServicesVSResources.Over_colon}
        if (o is int)
        {{
            var i = (int)o;
        }}
//]
    }}
}}
";

        private static readonly string s_preferObjectInitializer = $@"
using System;

class Customer
{{
    private int Age;

    public Customer()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var c = new Customer()
        {{
            Age = 21
        }};

        // {ServicesVSResources.Over_colon}
        var c = new Customer();
        c.Age = 21;
//]
    }}
}}
";

        private static readonly string s_preferCollectionInitializer = $@"
using System.Collections.Generic;

class Customer
{{
    private int Age;

    public Customer()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var list = new List<int>
        {{
            1,
            2,
            3
        }};

        // {ServicesVSResources.Over_colon}
        var list = new List<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);
//]
    }}
}}
";

        private static readonly string s_preferExplicitTupleName = $@"
class Customer
{{
    public Customer()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        (string name, int age) customer = GetCustomer();
        var name = customer.name;
        var age = customer.age;

        // {ServicesVSResources.Over_colon}
        (string name, int age) customer = GetCustomer();
        var name = customer.Item1;
        var age = customer.Item2;
//]
    }}
}}
";

        private static readonly string s_preferSimpleDefaultExpression = $@"
using System.Threading;

class Customer
{{
//[
    // {ServicesVSResources.Prefer_colon}
    void DoWork(CancellationToken cancellationToken = default) {{ }}

    // {ServicesVSResources.Over_colon}
    void DoWork(CancellationToken cancellationToken = default(CancellationToken)) {{ }}
//]
}}
";

        private static readonly string s_preferInferredTupleName = $@"
using System.Threading;

class Customer
{{
    public Customer(int age, string name)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var tuple = (age, name);

        // {ServicesVSResources.Over_colon}
        var tuple = (age: age, name: name);
//]
    }}
}}
";

        private static readonly string s_preferInferredAnonymousTypeMemberName = $@"
using System.Threading;

class Customer
{{
    public Customer(int age, string name)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        var anon = new {{ age, name }};

        // {ServicesVSResources.Over_colon}
        var anon = new {{ age = age, name = name }};
//]
    }}
}}
";

        private static readonly string s_preferInlinedVariableDeclaration = $@"
using System;

class Customer
{{
    public Customer(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (int.TryParse(value, out int i))
        {{
        }}

        // {ServicesVSResources.Over_colon}
        int i;
        if (int.TryParse(value, out i))
        {{
        }}
//]
    }}
}}
";

        private static readonly string s_preferBraces = $@"
using System;

class Customer
{{
    private int Age;

    public int GetAge()
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (test)
        {{
            this.Display();
        }}
        
        // {ServicesVSResources.Over_colon}
        if (test)
            this.Display();
//]
    }}
}}
";

        private static readonly string s_preferLocalFunctionOverAnonymousFunction = $@"
using System;

class Customer
{{
    public Customer(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        int fibonacci(int n)
        {{
            return n <= 1 ? 1 : fibonacci(n - 1) + fibonacci(n - 2);
        }}

        // {ServicesVSResources.Over_colon}
        Func<int, int> fibonacci = null;
        fibonacci = (int n) =>
        {{
            return n <= 1 ? 1 : fibonacci(n - 1) + fibonacci(n - 2);
        }};
//]
    }}
}}
";

        private static readonly string s_preferIsNullOverReferenceEquals = $@"
using System;

class Customer
{{
    public Customer(string value)
    {{
//[
        // {ServicesVSResources.Prefer_colon}
        if (value is null)
            return;

        // {ServicesVSResources.Over_colon}
        if (object.ReferenceEquals(value, null))
            return;
//]
    }}
}}
";

        private const string s_preferExpressionBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge() => this.Age;
}
//]
";

        private const string s_preferBlockBodyForMethods = @"
using System;

//[
class Customer
{
    private int Age;

    public int GetAge()
    {
        return this.Age;
    }
}
//]
";

        private const string s_preferExpressionBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age) => Age = age;
}
//]
";

        private const string s_preferBlockBodyForConstructors = @"
using System;

//[
class Customer
{
    private int Age;

    public Customer(int age)
    {
        Age = age;
    }
}
//]
";

        private const string s_preferExpressionBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
        => new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
//]
}
";

        private const string s_preferBlockBodyForOperators = @"
using System;

struct ComplexNumber
{
//[
    public static ComplexNumber operator +(ComplexNumber c1, ComplexNumber c2)
    {
        return new ComplexNumber(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
    }
//]
}
";

        private const string s_preferExpressionBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age => _age;
}
//]
";

        private const string s_preferBlockBodyForProperties = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age { get { return _age; } }
}
//]
";

        private const string s_preferExpressionBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get => _age;
        set => _age = value;
    }
}
//]
";

        private const string s_preferBlockBodyForAccessors = @"
using System;

//[
class Customer
{
    private int _age;
    public int Age
    {
        get { return _age; }
        set { _age = value; }
    }
}
//]
";

        private const string s_preferExpressionBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] => _values[i];
}
//]
";

        private const string s_preferBlockBodyForIndexers = @"
using System;

//[
class List<T>
{
    private T[] _values;
    public T this[int i] { get { return _values[i]; } }
}
//]
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
            var codeBlockPreferencesGroupTitle = ServicesVSResources.Code_block_preferences_colon;
            var expressionPreferencesGroupTitle = ServicesVSResources.Expression_preferences_colon;
            var variablePreferencesGroupTitle = ServicesVSResources.Variable_preferences_colon;

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

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyFieldAccess, CSharpVSResources.Qualify_field_access_with_this, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyPropertyAccess, CSharpVSResources.Qualify_property_access_with_this, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyMethodAccess, CSharpVSResources.Qualify_method_access_with_this, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyEventAccess, CSharpVSResources.Qualify_event_access_with_this, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, this, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences));

            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, s_intrinsicPreviewDeclarationTrue, s_intrinsicPreviewDeclarationFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, s_intrinsicPreviewMemberAccessTrue, s_intrinsicPreviewMemberAccessFalse, this, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences));

            // Use var
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes, CSharpVSResources.For_built_in_types, s_varForIntrinsicsPreviewTrue, s_varForIntrinsicsPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, CSharpVSResources.When_variable_type_is_apparent, s_varWhereApparentPreviewTrue, s_varWhereApparentPreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.UseImplicitTypeWherePossible, CSharpVSResources.Elsewhere, s_varWherePossiblePreviewTrue, s_varWherePossiblePreviewFalse, this, optionSet, varGroupTitle, typeStylePreferences));

            // Code block
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferBraces, ServicesVSResources.Prefer_braces, s_preferBraces, s_preferBraces, this, optionSet, codeBlockPreferencesGroupTitle));

            // Expression preferences
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferObjectInitializer, ServicesVSResources.Prefer_object_initializer, s_preferObjectInitializer, s_preferObjectInitializer, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferCollectionInitializer, ServicesVSResources.Prefer_collection_initializer, s_preferCollectionInitializer, s_preferCollectionInitializer, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferPatternMatchingOverIsWithCastCheck, CSharpVSResources.Prefer_pattern_matching_over_is_with_cast_check, s_preferPatternMatchingOverIsWithCastCheck, s_preferPatternMatchingOverIsWithCastCheck, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck, CSharpVSResources.Prefer_pattern_matching_over_as_with_null_check, s_preferPatternMatchingOverAsWithNullCheck, s_preferPatternMatchingOverAsWithNullCheck, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferExplicitTupleNames, ServicesVSResources.Prefer_explicit_tuple_name, s_preferExplicitTupleName, s_preferExplicitTupleName, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferSimpleDefaultExpression, ServicesVSResources.Prefer_simple_default_expression, s_preferSimpleDefaultExpression, s_preferSimpleDefaultExpression, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferInferredTupleNames, ServicesVSResources.Prefer_inferred_tuple_names, s_preferInferredTupleName, s_preferInferredTupleName, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferInferredAnonymousTypeMemberNames, ServicesVSResources.Prefer_inferred_anonymous_type_member_names, s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName, this, optionSet, expressionPreferencesGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferLocalOverAnonymousFunction, ServicesVSResources.Prefer_local_function_over_anonymous_function, s_preferLocalFunctionOverAnonymousFunction, s_preferLocalFunctionOverAnonymousFunction, this, optionSet, expressionPreferencesGroupTitle));

            AddExpressionBodyOptions(optionSet, expressionPreferencesGroupTitle);

            // Variable preferences
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferInlinedVariableDeclaration, ServicesVSResources.Prefer_inlined_variable_declaration, s_preferInlinedVariableDeclaration, s_preferInlinedVariableDeclaration, this, optionSet, variablePreferencesGroupTitle));

            // Null preferences.
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferThrowExpression, CSharpVSResources.Prefer_throw_expression, s_preferThrowExpression, s_preferThrowExpression, this, optionSet, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CSharpCodeStyleOptions.PreferConditionalDelegateCall, CSharpVSResources.Prefer_conditional_delegate_call, s_preferConditionalDelegateCall, s_preferConditionalDelegateCall, this, optionSet, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferCoalesceExpression, ServicesVSResources.Prefer_coalesce_expression, s_preferCoalesceExpression, s_preferCoalesceExpression, this, optionSet, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferNullPropagation, ServicesVSResources.Prefer_null_propagation, s_preferNullPropagation, s_preferNullPropagation, this, optionSet, nullCheckingGroupTitle));
            CodeStyleItems.Add(new BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, CSharpVSResources.Prefer_is_null_over_ReferenceEquals, s_preferIsNullOverReferenceEquals, s_preferIsNullOverReferenceEquals, this, optionSet, nullCheckingGroupTitle));
        }

        private void AddExpressionBodyOptions(OptionSet optionSet, string expressionPreferencesGroupTitle)
        {
            var expressionBodyPreferences = new List<CodeStylePreference>
            {
                new CodeStylePreference(CSharpVSResources.Never, isChecked: false),
                new CodeStylePreference(CSharpVSResources.When_possible, isChecked: false),
                new CodeStylePreference(CSharpVSResources.When_on_single_line, isChecked: false),
            };

            var enumValues = new[] { ExpressionBodyPreference.Never, ExpressionBodyPreference.WhenPossible, ExpressionBodyPreference.WhenOnSingleLine };

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedMethods,
                ServicesVSResources.Use_expression_body_for_methods,
                enumValues,
                new[] { s_preferBlockBodyForMethods, s_preferExpressionBodyForMethods, s_preferExpressionBodyForMethods },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedConstructors,
                ServicesVSResources.Use_expression_body_for_constructors,
                enumValues,
                new[] { s_preferBlockBodyForConstructors, s_preferExpressionBodyForConstructors, s_preferExpressionBodyForConstructors },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedOperators,
                ServicesVSResources.Use_expression_body_for_operators,
                enumValues,
                new[] { s_preferBlockBodyForOperators, s_preferExpressionBodyForOperators, s_preferExpressionBodyForOperators },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedProperties,
                ServicesVSResources.Use_expression_body_for_properties,
                enumValues,
                new[] { s_preferBlockBodyForProperties, s_preferExpressionBodyForProperties, s_preferExpressionBodyForProperties },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedIndexers,
                ServicesVSResources.Use_expression_body_for_indexers,
                enumValues,
                new[] { s_preferBlockBodyForIndexers, s_preferExpressionBodyForIndexers, s_preferExpressionBodyForIndexers },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));

            CodeStyleItems.Add(new EnumCodeStyleOptionViewModel<ExpressionBodyPreference>(
                CSharpCodeStyleOptions.PreferExpressionBodiedAccessors,
                ServicesVSResources.Use_expression_body_for_accessors,
                enumValues,
                new[] { s_preferBlockBodyForAccessors, s_preferExpressionBodyForAccessors, s_preferExpressionBodyForAccessors },
                this, optionSet, expressionPreferencesGroupTitle, expressionBodyPreferences));
        }
    }
}

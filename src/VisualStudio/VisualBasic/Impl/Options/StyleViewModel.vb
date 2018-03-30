﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Windows.Data
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class StyleViewModel
        Inherits AbstractOptionPreviewViewModel

#Region "Preview Text"

        Private Shared s_fieldDeclarationPreviewTrue As String = "
Class C
    Private capacity As Integer
    Sub Method()
        '//[
        Me.capacity = 0
        '//]
    End Sub
End Class
"

        Private Shared s_fieldDeclarationPreviewFalse As String = "
Class C
    Private capacity As Integer
    Sub Method()
        '//[
        capacity = 0
        '//]
    End Sub
End Class
"

        Private Shared s_propertyDeclarationPreviewTrue As String = "
Class C
    Public Property Id As Integer
    Sub Method()
        '//[
        Me.Id = 0
        '//]
    End Sub
End Class
"

        Private Shared s_propertyDeclarationPreviewFalse As String = "
Class C
    Public Property Id As Integer
    Sub Method()
        '//[
        Id = 0
        '//]
    End Sub
End Class
"

        Private Shared s_methodDeclarationPreviewTrue As String = "
Class C
    Sub Display()
        '//[
        Me.Display()
        '//]
    End Sub
End Class
"

        Private Shared s_methodDeclarationPreviewFalse As String = "
Class C
    Sub Display()
        '//[
        Display()
        '//]
    End Sub
End Class
"

        Private Shared s_eventDeclarationPreviewTrue As String = "
Imports System
Class C
    Public Event Elapsed As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        '//[
        AddHandler Me.Elapsed, AddressOf Handler
        '//]
    End Sub
End Class
"

        Private Shared s_eventDeclarationPreviewFalse As String = "
Imports System
Class C
    Public Event Elapsed As EventHandler
    Sub Handler(sender As Object, args As EventArgs)
        '//[
        AddHandler Elapsed, AddressOf Handler
        '//]
    End Sub
End Class
"

        Private _intrinsicDeclarationPreviewTrue As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Integer
    Sub M(argument As Integer)
        Dim local As Integer = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Private _intrinsicDeclarationPreviewFalse As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Int32
    Sub M(argument As Int32)
        Dim local As Int32 = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Private _intrinsicMemberAccessPreviewTrue As String = <a><![CDATA[
Imports System
Class Program
    '//[
    Sub M()
        Dim local = Integer.MaxValue
    End Sub
    '//]
End Class
]]></a>.Value

        Private _intrinsicMemberAccessPreviewFalse As String = <a><![CDATA[
Imports System
Class Program
    '//[
    Sub M()
        Dim local = Int32.MaxValue
    End Sub
    '//]
End Class
]]></a>.Value

        Private Shared ReadOnly s_preferObjectInitializer As String = $"
Imports System

Class Customer
    Private Age As Integer

    Sub New()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim c = New Customer() With {{
            .Age = 21
        }}

        ' {ServicesVSResources.Over_colon}
        Dim c = New Customer()
        c.Age = 21
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferCollectionInitializer As String = $"

Class Customer
    Private Age As Integer

    Sub New()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim list = New List(Of Integer) From {{
            1,
            2,
            3
        }}

        ' {ServicesVSResources.Over_colon}
        Dim list = New List(Of Integer)()
        list.Add(1)
        list.Add(2)
        list.Add(3)
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferExplicitTupleName As String = $"
Class Customer
    Public Sub New()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim customer As (name As String, age As Integer)
        Dim name = customer.name
        Dim age = customer.age

        ' {ServicesVSResources.Over_colon}
        Dim customer As (name As String, age As Integer)
        Dim name = customer.Item1
        Dim age = customer.Item2
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferInferredTupleName As String = $"
Class Customer
    Public Sub New(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim tuple = (name, age)

        ' {ServicesVSResources.Over_colon}
        Dim tuple = (name:=name, age:=age)
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferInferredAnonymousTypeMemberName As String = $"
Class Customer
    Public Sub New(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim anon = New With {{ name, age }}

        ' {ServicesVSResources.Over_colon}
        Dim anon = New With {{ .name = name, .age = age }}
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferCoalesceExpression As String = $"
Imports System

Class Customer
    Private Age As Integer

    Sub New()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = If(x, y)

        ' {ServicesVSResources.Over_colon}
        Dim v = If(x Is Nothing, y, x)    ' {ServicesVSResources.or}
        Dim v = If(x IsNot Nothing, x, y)
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferNullPropagation As String = $"
Imports System

Class Customer
    Private Age As Integer

    Sub New()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = o?.ToString()

        ' {ServicesVSResources.Over_colon}
        Dim v = If(o Is Nothing, Nothing, o.ToString())    ' {ServicesVSResources.or}
        Dim v = If(o IsNot Nothing, o.ToString(), Nothing)
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferAutoProperties As String = $"
Imports System

Class Customer
//[
    ' {ServicesVSResources.Prefer_colon}
    Public ReadOnly Property Age As Integer

    ' {ServicesVSResources.Over_colon}
    Private _age As Integer

    Public ReadOnly Property Age As Integer
        Get
            return _age
        End Get
    End Property
//]
End Class
"

        Private Shared ReadOnly s_preferIsNothingCheckOverReferenceEquals As String = $"
Imports System

Class Customer
    Sub New(value as object)
//[
        ' {ServicesVSResources.Prefer_colon}
        If value Is Nothing
            Return
        End If

        ' {ServicesVSResources.Over_colon}
        If Object.ReferenceEquals(value, Nothing)
            Return
        End If
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferReadonly As String = $"
Class Customer
//[
    ' {ServicesVSResources.Prefer_colon}
    ' 'value' can only be assigned in constructor
    Private ReadOnly value As Integer = 0

    ' {ServicesVSResources.Over_colon}
    ' 'value' can be assigned anywhere
    Private value As Integer = 0
//]
End Class"

#End Region

        Public Sub New(optionSet As OptionSet, serviceProvider As IServiceProvider)
            MyBase.New(optionSet, serviceProvider, LanguageNames.VisualBasic)

            Dim collectionView = DirectCast(CollectionViewSource.GetDefaultView(CodeStyleItems), ListCollectionView)
            collectionView.GroupDescriptions.Add(New PropertyGroupDescription(NameOf(AbstractCodeStyleOptionViewModel.GroupName)))

            Dim qualifyGroupTitle = BasicVSResources.Me_preferences_colon
            Dim qualifyMemberAccessPreferences = New List(Of CodeStylePreference) From
            {
                New CodeStylePreference(BasicVSResources.Prefer_Me, isChecked:=True),
                New CodeStylePreference(BasicVSResources.Do_not_prefer_Me, isChecked:=False)
            }

            Dim predefinedTypesGroupTitle = BasicVSResources.Predefined_type_preferences_colon
            Dim predefinedTypesPreferences = New List(Of CodeStylePreference) From
            {
                New CodeStylePreference(ServicesVSResources.Prefer_predefined_type, isChecked:=True),
                New CodeStylePreference(ServicesVSResources.Prefer_framework_type, isChecked:=False)
            }

            Dim codeBlockPreferencesGroupTitle = ServicesVSResources.Code_block_preferences_colon
            Dim expressionPreferencesGroupTitle = ServicesVSResources.Expression_preferences_colon
            Dim nothingPreferencesGroupTitle = BasicVSResources.nothing_checking_colon
            Dim fieldPreferencesGroupTitle = ServicesVSResources.Field_preferences_colon

            ' qualify with Me. group
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyFieldAccess, BasicVSResources.Qualify_field_access_with_Me, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyPropertyAccess, BasicVSResources.Qualify_property_access_with_Me, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyMethodAccess, BasicVSResources.Qualify_method_access_with_Me, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.QualifyEventAccess, BasicVSResources.Qualify_event_access_with_Me, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))

            ' predefined or framework type group
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, _intrinsicDeclarationPreviewTrue, _intrinsicDeclarationPreviewFalse, Me, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, _intrinsicMemberAccessPreviewTrue, _intrinsicMemberAccessPreviewFalse, Me, optionSet, predefinedTypesGroupTitle, predefinedTypesPreferences))

            ' Code block
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferAutoProperties, ServicesVSResources.analyzer_Prefer_auto_properties, s_preferAutoProperties, s_preferAutoProperties, Me, optionSet, codeBlockPreferencesGroupTitle))

            ' expression preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferObjectInitializer, ServicesVSResources.Prefer_object_initializer, s_preferObjectInitializer, s_preferObjectInitializer, Me, optionSet, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferCollectionInitializer, ServicesVSResources.Prefer_collection_initializer, s_preferCollectionInitializer, s_preferCollectionInitializer, Me, optionSet, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferExplicitTupleNames, ServicesVSResources.Prefer_explicit_tuple_name, s_preferExplicitTupleName, s_preferExplicitTupleName, Me, optionSet, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferInferredTupleNames, ServicesVSResources.Prefer_inferred_tuple_names, s_preferInferredTupleName, s_preferInferredTupleName, Me, optionSet, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, ServicesVSResources.Prefer_inferred_anonymous_type_member_names, s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName, Me, optionSet, expressionPreferencesGroupTitle))

            ' nothing preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferCoalesceExpression, ServicesVSResources.Prefer_coalesce_expression, s_preferCoalesceExpression, s_preferCoalesceExpression, Me, optionSet, nothingPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferNullPropagation, ServicesVSResources.Prefer_null_propagation, s_preferNullPropagation, s_preferNullPropagation, Me, optionSet, nothingPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, BasicVSResources.Prefer_Is_Nothing_over_ReferenceEquals, s_preferIsNothingCheckOverReferenceEquals, s_preferIsNothingCheckOverReferenceEquals, Me, optionSet, nothingPreferencesGroupTitle))

            ' field preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions.PreferReadonly, ServicesVSResources.Prefer_readonly, s_preferReadonly, s_preferReadonly, Me, optionSet, fieldPreferencesGroupTitle))
        End Sub
    End Class
End Namespace

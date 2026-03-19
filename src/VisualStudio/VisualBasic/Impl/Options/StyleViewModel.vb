' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Windows.Data
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class StyleViewModel
        Inherits AbstractOptionPreviewViewModel

#Region "Preview Text"

        Private Const s_fieldDeclarationPreviewTrue As String = "
Class C
    Private capacity As Integer
    Sub Method()
        '//[
        Me.capacity = 0
        '//]
    End Sub
End Class
"

        Private Const s_fieldDeclarationPreviewFalse As String = "
Class C
    Private capacity As Integer
    Sub Method()
        '//[
        capacity = 0
        '//]
    End Sub
End Class
"

        Private Const s_propertyDeclarationPreviewTrue As String = "
Class C
    Public Property Id As Integer
    Sub Method()
        '//[
        Me.Id = 0
        '//]
    End Sub
End Class
"

        Private Const s_propertyDeclarationPreviewFalse As String = "
Class C
    Public Property Id As Integer
    Sub Method()
        '//[
        Id = 0
        '//]
    End Sub
End Class
"

        Private Const s_methodDeclarationPreviewTrue As String = "
Class C
    Sub Display()
        '//[
        Me.Display()
        '//]
    End Sub
End Class
"

        Private Const s_methodDeclarationPreviewFalse As String = "
Class C
    Sub Display()
        '//[
        Display()
        '//]
    End Sub
End Class
"

        Private Const s_eventDeclarationPreviewTrue As String = "
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

        Private Const s_eventDeclarationPreviewFalse As String = "
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

        Private ReadOnly _intrinsicDeclarationPreviewTrue As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Integer
    Sub M(argument As Integer)
        Dim local As Integer = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Private ReadOnly _intrinsicDeclarationPreviewFalse As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Int32
    Sub M(argument As Int32)
        Dim local As Int32 = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Private ReadOnly _intrinsicMemberAccessPreviewTrue As String = <a><![CDATA[
Imports System
Class Program
    '//[
    Sub M()
        Dim local = Integer.MaxValue
    End Sub
    '//]
End Class
]]></a>.Value

        Private ReadOnly _intrinsicMemberAccessPreviewFalse As String = <a><![CDATA[
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

    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim c = New Customer() With {{
            .Age = 21
        }}
//]
    End Sub
    Sub M2()
//[
        ' {ServicesVSResources.Over_colon}
        Dim c = New Customer()
        c.Age = 21
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferCollectionInitializer As String = $"

Class Customer
    Private Age As Integer

    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim list = New List(Of Integer) From {{
            1,
            2,
            3
        }}
//]
    End Sub
    Sub M2()
//[
        ' {ServicesVSResources.Over_colon}
        Dim list = New List(Of Integer)()
        list.Add(1)
        list.Add(2)
        list.Add(3)
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferSimplifiedConditionalExpressions As String = $"

Class Customer
    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim x = A() AndAlso B()
//]
    End Sub

    Sub M2()
//[
        ' {ServicesVSResources.Over_colon}
        Dim x = If(A() AndAlso B(), True, False)
//]
    End Sub

    Function A() As Boolean
        Return True
    End Function

    Function B() As Boolean
        Return True
    End Function
End Class"

        Private Shared ReadOnly s_preferExplicitTupleName As String = $"
Class Customer
    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim customer As (name As String, age As Integer)
        Dim name = customer.name
        Dim age = customer.age
//]
    End Sub
    Sub M2()
//[
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
    Sub M1(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim tuple = (name, age)
//]
    End Sub
    Sub M2(name as String, age As Integer)
//[
        ' {ServicesVSResources.Over_colon}
        Dim tuple = (name:=name, age:=age)
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferInferredAnonymousTypeMemberName As String = $"
Class Customer
    Sub M1(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim anon = New With {{ name, age }}
//]
    End Sub
    Sub M2(name as String, age As Integer)
//[
        ' {ServicesVSResources.Over_colon}
        Dim anon = New With {{ .name = name, .age = age }}
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferConditionalExpressionOverIfWithAssignments As String = $"
Class Customer
    Public Sub New(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim s As String = If(expr, ""hello"", ""world"")

        ' {ServicesVSResources.Over_colon}
        Dim s As String
        If expr Then
            s = ""hello""
        Else
            s = ""world""
        End If
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferConditionalExpressionOverIfWithReturns As String = $"
Class Customer
    Public Sub New(name as String, age As Integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        Return If(expr, ""hello"", ""world"")

        ' {ServicesVSResources.Over_colon}
        If expr Then
            Return ""hello""
        Else
            Return ""world""
        End If
//]
    End Sub
end class
"

        Private Shared ReadOnly s_preferCoalesceExpression As String = $"
Imports System

Class Customer
    Private Age As Integer

    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = If(x, y)
//]
    End Sub
    Sub M2()
//[
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

    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = o?.ToString()
//]
    End Sub
    Sub M2()
//[
        ' {ServicesVSResources.Over_colon}
        Dim v = If(o Is Nothing, Nothing, o.ToString())    ' {ServicesVSResources.or}
        Dim v = If(o IsNot Nothing, o.ToString(), Nothing)
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferAutoProperties As String = $"
Imports System

Class Customer1
//[
    ' {ServicesVSResources.Prefer_colon}
    Public ReadOnly Property Age As Integer
//]
End Class
Class Customer2
//[
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

        Private Shared ReadOnly s_preferSystemHashCode As String = $"
Imports System

Class Customer1
    Dim a, b, c As Integer
//[
    ' {ServicesVSResources.Prefer_colon}
    ' {ServicesVSResources.Requires_System_HashCode_be_present_in_project}
    Public Overrides Function GetHashCode() As Integer
        Return System.HashCode.Combine(a, b, c)
    End Function
//]
End Class
Class Customer2
    Dim a, b, c As Integer
//[
    ' {ServicesVSResources.Over_colon}
    Public Overrides Function GetHashCode() As Integer
        Dim hashCode = 339610899
        hashCode = hashCode * -1521134295 + a.GetHashCode()
        hashCode = hashCode * -1521134295 + b.GetHashCode()
        hashCode = hashCode * -1521134295 + c.GetHashCode()
        return hashCode
    End Function
//]
End Class
"

        Private Shared ReadOnly s_preferIsNothingCheckOverReferenceEquals As String = $"
Imports System

Class Customer
    Sub M1(value as object)
//[
        ' {ServicesVSResources.Prefer_colon}
        If value Is Nothing
            Return
        End If
//]
    End Sub
    Sub M2(value as object)
//[
        ' {ServicesVSResources.Over_colon}
        If Object.ReferenceEquals(value, Nothing)
            Return
        End If
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferCompoundAssignments As String = $"
Imports System

Class Customer
    Sub M1(value as integer)
//[
        ' {ServicesVSResources.Prefer_colon}
        value += 10
//]
    End Sub
    Sub M2(value as integer)
//[
        ' {ServicesVSResources.Over_colon}
        value = value + 10
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferIsNotExpression As String = $"
Imports System

Class Customer
    Sub M1(value as object)
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim isSomething = value IsNot Nothing
//]
    End Sub
    Sub M2(value as object)
//[
        ' {ServicesVSResources.Over_colon}
        Dim isSomething = Not value Is Nothing
//]
    End Sub
End Class"

        Private Shared ReadOnly s_preferSimplifiedObjectCreation As String = $"
Imports System

Class Customer
    Sub M1()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim c As New Customer()
//]
    End Sub
    Sub M2()
//[
        ' {ServicesVSResources.Over_colon}
        Dim c As Customer = New Customer()
//]
    End Sub
End Class"

#Region "arithmetic binary parentheses"

        Private Shared ReadOnly s_arithmeticBinaryAlwaysForClarity As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = a + (b * c)

        ' {ServicesVSResources.Over_colon}
        Dim v = a + b * c
//]
    end sub
end class
"

        Private Shared ReadOnly s_arithmeticBinaryNeverIfUnnecessary As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = a + b * c

        ' {ServicesVSResources.Over_colon}
        Dim v = a + (b * c)
//]
    end sub
end class
"

#End Region

#Region "relational binary parentheses"

        Private Shared ReadOnly s_relationalBinaryAlwaysForClarity As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Keep_all_parentheses_in_colon}
        Dim v = (a < b) = (c > d)
//]
    end sub
end class
"

        Private Shared ReadOnly s_relationalBinaryNeverIfUnnecessary As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = a < b = c > d

        ' {ServicesVSResources.Over_colon}
        Dim v = (a < b) = (c > d)
//]
    end sub
end class
"

#End Region

#Region "other binary parentheses"

        Private ReadOnly s_otherBinaryAlwaysForClarity As String = $"
class C
    sub M()
//[
        // {ServicesVSResources.Prefer_colon}
        Dim v = a OrElse (b AndAlso c)

        // {ServicesVSResources.Over_colon}
        Dim v = a OrElse b AndAlso c
//]
    end sub
end class
"

        Private ReadOnly s_otherBinaryNeverIfUnnecessary As String = $"
class C
    sub M()
//[
        // {ServicesVSResources.Prefer_colon}
        Dim v = a OrElse b AndAlso c

        // {ServicesVSResources.Over_colon}
        Dim v = a OrElse (b AndAlso c)
//]
    end sub
end class
"

#End Region

#Region "other parentheses"

        Private Shared ReadOnly s_otherParenthesesAlwaysForClarity As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Keep_all_parentheses_in_colon}
        Dim v = (a.b).Length
//]
    end sub
end class
"

        Private Shared ReadOnly s_otherParenthesesNeverIfUnnecessary As String = $"
class C
    sub M()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim v = a.b.Length

        ' {ServicesVSResources.Over_colon}
        Dim v = (a.b).Length
//]
    end sub
end class
"

#End Region

        Private Shared ReadOnly s_preferReadonly As String = $"
Class Customer1
//[
    ' {ServicesVSResources.Prefer_colon}
    ' 'value' can only be assigned in constructor
    Private ReadOnly value As Integer = 0
//]
End Class
Class Customer2
//[
    ' {ServicesVSResources.Over_colon}
    ' 'value' can be assigned anywhere
    Private value As Integer = 0
//]
End Class"

        Private Shared ReadOnly s_allow_multiple_blank_lines_true As String = $"
Class Customer2
    Sub Method()
//[
        ' {ServicesVSResources.Allow_colon}
        If True Then
            DoWork()
        End If


        Return
//]
    End Sub
End Class"

        Private Shared ReadOnly s_allow_multiple_blank_lines_false As String = $"
Class Customer1
    Sub Method()
//[
        ' {ServicesVSResources.Require_colon}
        If True Then
            DoWork()
        End If

        Return
//]
    End Sub
End Class
Class Customer2
    Sub Method()
//[
        ' {ServicesVSResources.Over_colon}
        If True Then
            DoWork()
        End If


        Return
//]
    End Sub
End Class"

        Private Shared ReadOnly s_allow_statement_immediately_after_block_true As String = $"
Class Customer2
    Sub Method()
//[
        ' {ServicesVSResources.Allow_colon}
        If True Then
            DoWork()
        End If
        Return
//]
    End Sub
End Class"

        Private Shared ReadOnly s_allow_statement_immediately_after_block_false As String = $"
Class Customer1
    Sub Method()
//[
        ' {ServicesVSResources.Require_colon}
        If True Then
            DoWork()
        End If

        Return
//]
    End Sub
End Class
Class Customer2
    Sub Method()
//[
        ' {ServicesVSResources.Over_colon}
        If True Then
            DoWork()
        End If
        Return
//]
    End Sub
End Class"

#Region "unused parameters"

        Private Shared ReadOnly s_avoidUnusedParametersNonPublicMethods As String = $"
Public Class C1
//[
    ' {ServicesVSResources.Prefer_colon}
    Private Sub M()
    End Sub
//]
End Class

Public Class C2
//[
    ' {ServicesVSResources.Over_colon}
    Private Sub M(param As Integer)
    End Sub
//]
End Class
"

        Private Shared ReadOnly s_avoidUnusedParametersAllMethods As String = $"
Public Class C1
//[
    ' {ServicesVSResources.Prefer_colon}
    Public Sub M()
    End Sub
//]
End Class

Public Class C2
//[
    ' {ServicesVSResources.Over_colon}
    Public Sub M(param As Integer)
    End Sub
//]
End Class
"

#End Region

#Region "unused values"

        Private Shared ReadOnly s_avoidUnusedValueAssignmentUnusedLocal As String = $"
Class C1
    Function M() As Integer
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim unused = Computation()   ' {ServicesVSResources.Unused_value_is_explicitly_assigned_to_an_unused_local}
        Dim x = 1
//]
        Return x
    End Function
End Class

Class C2
    Function M() As Integer
//[
        ' {ServicesVSResources.Over_colon}
        Dim x = Computation()   ' {ServicesVSResources.Value_assigned_here_is_never_used}
        x = 1
//]
        Return x
    End Function
End Class
"

        Private Shared ReadOnly s_avoidUnusedValueExpressionStatementUnusedLocal As String = $"
Class C1
    Sub M()
//[
        ' {ServicesVSResources.Prefer_colon}
        Dim unused = Computation()   ' {ServicesVSResources.Unused_value_is_explicitly_assigned_to_an_unused_local}
//]
    End Sub
End Class

Class C2
    Sub M()
//[
        ' {ServicesVSResources.Over_colon}
        Computation()   ' {ServicesVSResources.Value_returned_by_invocation_is_implicitly_ignored}
//]
    End Sub
End Class
"
#End Region
#End Region

        Public Sub New(optionStore As OptionStore, serviceProvider As IServiceProvider)
            MyBase.New(optionStore, serviceProvider, LanguageNames.VisualBasic)

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
            Dim fieldPreferencesGroupTitle = ServicesVSResources.Modifier_preferences_colon
            Dim parameterPreferencesGroupTitle = ServicesVSResources.Parameter_preferences_colon
            Dim newLinePreferencesGroupTitle = ServicesVSResources.New_line_preferences_experimental_colon

            ' qualify with Me. group
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyFieldAccess, BasicVSResources.Qualify_field_access_with_Me, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, Me, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyPropertyAccess, BasicVSResources.Qualify_property_access_with_Me, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, Me, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyMethodAccess, BasicVSResources.Qualify_method_access_with_Me, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, Me, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.QualifyEventAccess, BasicVSResources.Qualify_event_access_with_Me, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, Me, optionStore, qualifyGroupTitle, qualifyMemberAccessPreferences))

            ' predefined or framework type group
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, ServicesVSResources.For_locals_parameters_and_members, _intrinsicDeclarationPreviewTrue, _intrinsicDeclarationPreviewFalse, Me, optionStore, predefinedTypesGroupTitle, predefinedTypesPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, ServicesVSResources.For_member_access_expressions, _intrinsicMemberAccessPreviewTrue, _intrinsicMemberAccessPreviewFalse, Me, optionStore, predefinedTypesGroupTitle, predefinedTypesPreferences))

            AddParenthesesOptions(optionStore)

            ' Code block
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferAutoProperties, ServicesVSResources.analyzer_Prefer_auto_properties, s_preferAutoProperties, s_preferAutoProperties, Me, optionStore, codeBlockPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferSystemHashCode, ServicesVSResources.Prefer_System_HashCode_in_GetHashCode, s_preferSystemHashCode, s_preferSystemHashCode, Me, optionStore, codeBlockPreferencesGroupTitle))

            ' expression preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferObjectInitializer, ServicesVSResources.Prefer_object_initializer, s_preferObjectInitializer, s_preferObjectInitializer, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCollectionInitializer, ServicesVSResources.Prefer_collection_initializer, s_preferCollectionInitializer, s_preferCollectionInitializer, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferSimplifiedBooleanExpressions, ServicesVSResources.Prefer_simplified_boolean_expressions, s_preferSimplifiedConditionalExpressions, s_preferSimplifiedConditionalExpressions, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferExplicitTupleNames, ServicesVSResources.Prefer_explicit_tuple_name, s_preferExplicitTupleName, s_preferExplicitTupleName, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferInferredTupleNames, ServicesVSResources.Prefer_inferred_tuple_names, s_preferInferredTupleName, s_preferInferredTupleName, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, ServicesVSResources.Prefer_inferred_anonymous_type_member_names, s_preferInferredAnonymousTypeMemberName, s_preferInferredAnonymousTypeMemberName, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferConditionalExpressionOverAssignment, ServicesVSResources.Prefer_conditional_expression_over_if_with_assignments, s_preferConditionalExpressionOverIfWithAssignments, s_preferConditionalExpressionOverIfWithAssignments, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferConditionalExpressionOverReturn, ServicesVSResources.Prefer_conditional_expression_over_if_with_returns, s_preferConditionalExpressionOverIfWithReturns, s_preferConditionalExpressionOverIfWithReturns, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCompoundAssignment, ServicesVSResources.Prefer_compound_assignments, s_preferCompoundAssignments, s_preferCompoundAssignments, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(VisualBasicCodeStyleOptions.PreferIsNotExpression, BasicVSResources.Prefer_IsNot_expression, s_preferIsNotExpression, s_preferIsNotExpression, Me, optionStore, expressionPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(VisualBasicCodeStyleOptions.PreferSimplifiedObjectCreation, BasicVSResources.Prefer_simplified_object_creation, s_preferSimplifiedObjectCreation, s_preferSimplifiedObjectCreation, Me, optionStore, expressionPreferencesGroupTitle))

            AddUnusedValueOptions(optionStore, expressionPreferencesGroupTitle)

            ' nothing preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferCoalesceExpression, ServicesVSResources.Prefer_coalesce_expression, s_preferCoalesceExpression, s_preferCoalesceExpression, Me, optionStore, nothingPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferNullPropagation, ServicesVSResources.Prefer_null_propagation, s_preferNullPropagation, s_preferNullPropagation, Me, optionStore, nothingPreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, BasicVSResources.Prefer_Is_Nothing_for_reference_equality_checks, s_preferIsNothingCheckOverReferenceEquals, s_preferIsNothingCheckOverReferenceEquals, Me, optionStore, nothingPreferencesGroupTitle))

            ' Field preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.PreferReadonly, ServicesVSResources.Prefer_readonly_fields, s_preferReadonly, s_preferReadonly, Me, optionStore, fieldPreferencesGroupTitle))

            ' Parameter preferences
            AddParameterOptions(optionStore, parameterPreferencesGroupTitle)

            ' New line preferences
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.AllowMultipleBlankLines, ServicesVSResources.Allow_multiple_blank_lines, s_allow_multiple_blank_lines_true, s_allow_multiple_blank_lines_false, Me, optionStore, newLinePreferencesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(CodeStyleOptions2.AllowStatementImmediatelyAfterBlock, ServicesVSResources.Allow_statement_immediately_after_block, s_allow_statement_immediately_after_block_true, s_allow_statement_immediately_after_block_true, Me, optionStore, newLinePreferencesGroupTitle))
        End Sub

        Private Sub AddParenthesesOptions(optionStore As OptionStore)
            AddParenthesesOption(
                optionStore, CodeStyleOptions2.ArithmeticBinaryParentheses,
                BasicVSResources.In_arithmetic_binary_operators,
                {s_arithmeticBinaryAlwaysForClarity, s_arithmeticBinaryNeverIfUnnecessary},
                defaultAddForClarity:=True)

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.OtherBinaryParentheses,
                BasicVSResources.In_other_binary_operators,
                {s_otherBinaryAlwaysForClarity, s_otherBinaryNeverIfUnnecessary},
                defaultAddForClarity:=True)

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.RelationalBinaryParentheses,
                BasicVSResources.In_relational_binary_operators,
                {s_relationalBinaryAlwaysForClarity, s_relationalBinaryNeverIfUnnecessary},
                defaultAddForClarity:=True)

            AddParenthesesOption(
                optionStore, CodeStyleOptions2.OtherParentheses,
                ServicesVSResources.In_other_operators,
                {s_otherParenthesesAlwaysForClarity, s_otherParenthesesNeverIfUnnecessary},
                defaultAddForClarity:=False)
        End Sub

        Private Sub AddUnusedValueOptions(optionStore As OptionStore, expressionPreferencesGroupTitle As String)
            Dim unusedValuePreferences = New List(Of CodeStylePreference) From
            {
                New CodeStylePreference(ServicesVSResources.Unused_local, isChecked:=True)
            }

            Dim enumValues =
            {
                UnusedValuePreference.UnusedLocalVariable
            }

            Me.CodeStyleItems.Add(New EnumCodeStyleOptionViewModel(Of UnusedValuePreference)(
                                    VisualBasicCodeStyleOptions.UnusedValueAssignment,
                                    ServicesVSResources.Avoid_unused_value_assignments,
                                    enumValues,
                                    {s_avoidUnusedValueAssignmentUnusedLocal},
                                    Me,
                                    optionStore,
                                    expressionPreferencesGroupTitle,
                                    unusedValuePreferences))

            Me.CodeStyleItems.Add(New EnumCodeStyleOptionViewModel(Of UnusedValuePreference)(
                                    VisualBasicCodeStyleOptions.UnusedValueExpressionStatement,
                                    ServicesVSResources.Avoid_expression_statements_that_implicitly_ignore_value,
                                    enumValues,
                                    {s_avoidUnusedValueExpressionStatementUnusedLocal},
                                    Me,
                                    optionStore,
                                    expressionPreferencesGroupTitle,
                                    unusedValuePreferences))

        End Sub

        Private Sub AddParameterOptions(optionStore As OptionStore, parameterPreferencesGroupTitle As String)
            Dim examples =
            {
                s_avoidUnusedParametersNonPublicMethods,
                s_avoidUnusedParametersAllMethods
            }

            AddUnusedParameterOption(optionStore, parameterPreferencesGroupTitle, examples)
        End Sub
    End Class
End Namespace

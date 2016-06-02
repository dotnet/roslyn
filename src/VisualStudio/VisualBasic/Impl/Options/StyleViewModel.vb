' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class StyleViewModel
        Inherits AbstractOptionPreviewViewModel

        Friend Overrides Function ShouldPersistOption(key As OptionKey) As Boolean
            Return key.Option.Feature = SimplificationOptions.PerLanguageFeatureName OrElse
                   key.Option.Feature = CodeStyleOptions.PerLanguageCodeStyleOption
        End Function

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

        Public Sub New(optionSet As OptionSet, serviceProvider As IServiceProvider)
            MyBase.New(optionSet, serviceProvider, LanguageNames.VisualBasic)

            Dim qualifyGroupTitle = BasicVSResources.QualifyGroupTitle
            Dim qualifyMemberAccessPreferences = New List(Of CodeStylePreference) From
            {
                New CodeStylePreference(BasicVSResources.PreferMe, isChecked:=True),
                New CodeStylePreference(BasicVSResources.DoNotPreferMe, isChecked:=False)
            }

            Dim predefinedTypesGroupTitle = BasicVSResources.PredefinedTypesGroupTitle
            Dim predefinedTypesPreferences = New List(Of CodeStylePreference) From
            {
                New CodeStylePreference(ServicesVSResources.PreferPredefinedType, isChecked:=True),
                New CodeStylePreference(ServicesVSResources.PreferFrameworkType, isChecked:=False)
            }

            Me.CodeStyleItems.Add(New SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyFieldAccess, BasicVSResources.QualifyFieldAccessWithMe, s_fieldDeclarationPreviewTrue, s_fieldDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyPropertyAccess, BasicVSResources.QualifyPropertyAccessWithMe, s_propertyDeclarationPreviewTrue, s_propertyDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyMethodAccess, BasicVSResources.QualifyMethodAccessWithMe, s_methodDeclarationPreviewTrue, s_methodDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New SimpleCodeStyleOptionViewModel(CodeStyleOptions.QualifyEventAccess, BasicVSResources.QualifyEventAccessWithMe, s_eventDeclarationPreviewTrue, s_eventDeclarationPreviewFalse, Me, optionSet, qualifyGroupTitle, qualifyMemberAccessPreferences))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, _intrinsicDeclarationPreviewTrue, _intrinsicDeclarationPreviewFalse, Me, optionSet, predefinedTypesGroupTitle))
            Me.CodeStyleItems.Add(New BooleanCodeStyleOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, _intrinsicMemberAccessPreviewTrue, _intrinsicMemberAccessPreviewFalse, Me, optionSet, predefinedTypesGroupTitle))
        End Sub
    End Class
End Namespace

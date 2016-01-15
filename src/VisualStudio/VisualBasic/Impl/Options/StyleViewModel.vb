' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Friend Class StyleViewModel
        Inherits AbstractOptionPreviewViewModel

        Friend Overrides Function ShouldPersistOption(key As OptionKey) As Boolean
            Return key.Option.Feature = SimplificationOptions.PerLanguageFeatureName
        End Function

        Private _mePreviewTrue As String = <a><![CDATA[
Class C
    Private x as Integer
    Private Sub S()
    '//[
        Me.x = 3
    '//]
    End Sub
]]></a>.Value

        Private _mePreviewFalse As String = <a><![CDATA[
Class C
    Private x as Integer
    Private Sub S()
    '//[
        x = 3
    '//]
    End Sub
]]></a>.Value

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

            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.QualifyFieldAccess, BasicVSResources.QualifyFieldAccessWithMe, _mePreviewTrue, _mePreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.QualifyPropertyAccess, BasicVSResources.QualifyPropertyAccessWithMe, _mePreviewTrue, _mePreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.QualifyMethodAccess, BasicVSResources.QualifyMethodAccessWithMe, _mePreviewTrue, _mePreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.QualifyEventAccess, BasicVSResources.QualifyEventAccessWithMe, _mePreviewTrue, _mePreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, _intrinsicDeclarationPreviewTrue, _intrinsicDeclarationPreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, _intrinsicMemberAccessPreviewTrue, _intrinsicMemberAccessPreviewFalse, Me, optionSet))
        End Sub
    End Class
End Namespace

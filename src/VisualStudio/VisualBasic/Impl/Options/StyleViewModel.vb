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

        Dim mePreviewTrue As String = <a><![CDATA[
Class C
    Private x as Integer
    Private Sub S()
    '//[
        Me.x = 3
    '//]
    End Sub
]]></a>.Value

        Dim mePreviewFalse As String = <a><![CDATA[
Class C
    Private x as Integer
    Private Sub S()
    '//[
        x = 3
    '//]
    End Sub
]]></a>.Value

        Dim intrinsicDeclarationPreviewTrue As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Integer
    Sub M(argument As Integer)
        Dim local As Integer = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Dim intrinsicDeclarationPreviewFalse As String = <a><![CDATA[
Class Program
    '//[
    Private _member As Int32
    Sub M(argument As Int32)
        Dim local As Int32 = 0
    End Sub
    '//]
End Class
]]></a>.Value

        Dim intrinsicMemberAccessPreviewTrue As String = <a><![CDATA[
Imports System
Class Program
    '//[
    Sub M()
        Dim local = Integer.MaxValue
    End Sub
    '//]
End Class
]]></a>.Value

        Dim intrinsicMemberAccessPreviewFalse As String = <a><![CDATA[
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

            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.QualifyMemberAccessWithThisOrMe, BasicVSResources.QualifyMemberAccessWithMe, mePreviewTrue, mePreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInDeclaration, intrinsicDeclarationPreviewTrue, intrinsicDeclarationPreviewFalse, Me, optionSet))
            Me.Items.Add(New CheckBoxOptionViewModel(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, BasicVSResources.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, intrinsicMemberAccessPreviewTrue, intrinsicMemberAccessPreviewFalse, Me, optionSet))
        End Sub
    End Class
End Namespace

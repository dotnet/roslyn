' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.Formatting
    <Guid(Guids.VisualBasicOptionPageCodeStyleIdString)>
    Friend Class CodeStylePage
        Inherits AbstractOptionPage

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider) As AbstractOptionPageControl
            Return New GridOptionPreviewControl(serviceProvider, Function(o, s) New StyleViewModel(o, s), AddressOf GetCurrentEditorConfigOptionsVB, LanguageNames.VisualBasic)
        End Function

        Friend Shared Sub Generate_Editorconfig(ByVal optionSet As OptionSet, ByVal language As String, ByVal editorconfig As StringBuilder)
            GridOptionPreviewControl.Generate_Editorconfig(optionSet, language, editorconfig, AddressOf GetCurrentEditorConfigOptionsVB)
        End Sub

        Private Shared Sub GetCurrentEditorConfigOptionsVB(ByVal optionSet As OptionSet, ByVal editorconfig As StringBuilder)
            editorconfig.AppendLine()
            editorconfig.AppendLine("# VB Coding Conventions")

            editorconfig.AppendLine("# Modifier preferences:")
            ' visual_basic_preferred_modifier_order
            VBCodeStyleOptions_GenerateEditorconfig(optionSet, VisualBasicCodeStyleOptions.PreferredModifierOrder, editorconfig)
        End Sub

        Private Shared Sub VBCodeStyleOptions_GenerateEditorconfig(ByVal optionSet As OptionSet, ByVal [option] As [Option](Of CodeStyleOption(Of String)), ByVal editorconfig As StringBuilder)
            Dim element = [option].StorageLocations.OfType(Of EditorConfigStorageLocation(Of CodeStyleOption(Of String)))().FirstOrDefault()
            If element IsNot Nothing Then
                GridOptionPreviewControl.AppendName(element.KeyName, editorconfig)

                Dim curSetting = optionSet.GetOption([option])
                editorconfig.AppendLine(curSetting.Value + ":" + GridOptionPreviewControl.NotificationOptionToString(curSetting.Notification))
            End If
        End Sub
    End Class
End Namespace

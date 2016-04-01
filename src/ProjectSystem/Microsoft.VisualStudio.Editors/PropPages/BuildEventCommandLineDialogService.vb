' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    '--------------------------------------------------------------------------
    ' BuildEventCommandLineDialogService:
    '   Build Event Service Class. Implements SVsBuildEventCommandLineDialogService 
    '   exposed via the IVsBuildEventCommandLineDialogService interface.
    '--------------------------------------------------------------------------
    <CLSCompliant(False)> _
    Friend NotInheritable Class BuildEventCommandLineDialogService
        Implements Interop.IVsBuildEventCommandLineDialogService

        Private _serviceProvider As IServiceProvider

        Friend Sub New(ByVal sp As IServiceProvider)
            _serviceProvider = sp
        End Sub

        Public Function EditCommandLine(ByVal WindowText As String, ByVal HelpID As String, ByVal OriginalCommandLine As String, ByVal MacroProvider As Interop.IVsBuildEventMacroProvider, ByRef Result As String) As Integer _
            Implements Interop.IVsBuildEventCommandLineDialogService.EditCommandLine

            Dim frm As New BuildEventCommandLineDialog
            Dim i As Integer
            Dim Count As Integer

            '// Initialize the title text
            frm.SetFormTitleText(WindowText)

            '// Initialize the command line
            frm.EventCommandLine = OriginalCommandLine

            '// Initialize helpTopicID
            If HelpID IsNot Nothing Then
                frm.HelpTopic = HelpID
            End If

            '// Initialize the token values
            Count = MacroProvider.GetCount()

            Dim Names(Count - 1) As String
            Dim Values(Count - 1) As String

            For i = 0 To Count - 1
                MacroProvider.GetExpandedMacro(i, Names(i), Values(i))
            Next

            frm.SetTokensAndValues(Names, Values)

            '// Show the form
            If (frm.ShowDialog(_serviceProvider) = System.Windows.Forms.DialogResult.OK) Then
                Result = frm.EventCommandLine
                Return 0
            Else
                Result = OriginalCommandLine
                Return 1
            End If

        End Function
    End Class

End Namespace

'------------------------------------------------------------------------------
' <copyright from='1997' to='2003' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
'

Imports System
Imports System.Diagnostics
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Runtime.InteropServices

Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Editors.Interop

Namespace Microsoft.VisualStudio.Editors.Package

    '*
    '* Control for the Tools - Options - Visual basic - Editor page
    '* The EditorToolsOptionsPage class loads and saves settings from persistent
    '* storage (EnvDTE.DTE)
    '*
    <Guid("0FD397F9-E4A5-4392-AFF2-36DC3FB133D2")> _
    Friend NotInheritable Class EditorToolsOptionsPage
        Inherits ScalingDialogPage

        ' our instance of the dialog itself
        '
        Private WithEvents _dialog As EditorToolsOptionsPanel

        ' Max tab size. Unfortunately this is defined as an anonymous enum in the TextMgr.idl, so we have to duplicate the
        ' value here...
        Private Const MAX_EDITOR_TAB_SIZE As Integer = 60

        Private Shared VBLangGUID As New Guid("E34ACDC0-BAAE-11D0-88BF-00A0C9110049")

        Private Const TextEditorCategory As String = "TextEditor"

        ' Pages under the FontsAndColors category
        Private Const TextEditorPage As String = "TextEditor"

        ' Pages under the TextEditor category
        Private Const BasicPage As String = "Basic"

        ' Items under TextEditor - Basic
        Private Const IndentSizeItem As String = "IndentSize"
        Private Const IndentStyleItem As String = "IndentStyle"
        Private Const ShowLineNumbersItem As String = "ShowLineNumbers"
        Private Const TabSizeItem As String = "TabSize"
        Private Const WordWrapItem As String = "WordWrap"

        '*
        '* Create new instance
        '*
        Public Sub New()
            MyBase.New()
            _dialog = New EditorToolsOptionsPanel
        End Sub


        '*
        '* Return Control that implements the UI (Panel) for the settings.
        '*
        Protected Overrides ReadOnly Property Window() As IWin32Window
            Get
                Return _dialog
            End Get
        End Property

        ''' <summary>
        ''' Validate data before trying to save it...
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnDeactivate(ByVal e As System.ComponentModel.CancelEventArgs)
            e.Cancel = Not ValidateSettings()
        End Sub

        ''' <summary>
        ''' Validate settings on this form
        ''' </summary>
        ''' <returns>True if validation successful, false otherwise</returns>
        ''' <remarks>Will show message box if anything is wrong</remarks>
        Private Function ValidateSettings() As Boolean
            If _dialog.TabSize < 1 OrElse _dialog.TabSize > MAX_EDITOR_TAB_SIZE Then
                ShowDialogBox(SR.GetString(SR.OptionPage_Editor_InvalidTabSize))
                _dialog._tabSizeTextBox.Focus()
                Return False
            End If

            If _dialog.IndentSize < 1 OrElse _dialog.IndentSize > MAX_EDITOR_TAB_SIZE Then
                ShowDialogBox(SR.GetString(SR.OptionPage_Editor_InvalidIndentSize))
                _dialog._indentSizeTextBox.Focus()
                Return False
            End If
            

            Return True
        End Function

        '*
        '* Load settings using the DTE from DTE service properties
        '*
        Protected Overrides Sub LoadSettings()
            Dim dte As EnvDTE.DTE

            Dim basicSpecificProperties As EnvDTE.Properties
            Dim fontsAndColorsProperties As EnvDTE.Properties
            Dim textEditorProperties As EnvDTE.Properties

            Try
                dte = CType(GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)
                Debug.WriteLineIf(dte Is Nothing, "Failed to get EnvDTE.DTE service")

                textEditorProperties = dte.Properties(TextEditorCategory, BasicPage)
                _dialog.IndentSize = CInt(textEditorProperties.Item(IndentSizeItem).Value)
                _dialog.IndentType = CType(textEditorProperties.Item(IndentStyleItem).Value, EnvDTE.vsIndentStyle)
                _dialog.TabSize = CInt(textEditorProperties.Item(TabSizeItem).Value)
                _dialog.WordWrap = CBool(textEditorProperties.Item(WordWrapItem).Value)
                _dialog.LineNumbers = CBool(textEditorProperties.Item(ShowLineNumbersItem).Value)

                _dialog.Enabled = True
            Catch ex As Exception
                Debug.Fail("EditorToolsOptionPage::LoadSettings Caught exception " & ex.Message & " " & ex.StackTrace)
                _dialog.Enabled = False
            Finally
                fontsAndColorsProperties = Nothing
                textEditorProperties = Nothing
                basicSpecificProperties = Nothing
                dte = Nothing
            End Try
        End Sub


        '*
        '* Save (persist) settings 
        '*
        Protected Overrides Sub SaveSettings()
            ' Don't save values if the dialog didn't load all values correctly
            If Not _dialog.Enabled Then Exit Sub

            Dim dte As EnvDTE.DTE
            Dim textEditorProperties As EnvDTE.Properties

            Try
                dte = CType(GetService(GetType(EnvDTE.DTE)), EnvDTE.DTE)

                textEditorProperties = dte.Properties(TextEditorCategory, BasicPage)

                textEditorProperties.Item(IndentSizeItem).Value = _dialog.IndentSize
                textEditorProperties.Item(IndentStyleItem).Value = _dialog.IndentType
                textEditorProperties.Item(TabSizeItem).Value = _dialog.TabSize
                textEditorProperties.Item(WordWrapItem).Value = _dialog.WordWrap
                textEditorProperties.Item(ShowLineNumbersItem).Value = _dialog.LineNumbers

                Dim prefs As LANGPREFERENCES = GetLanguagePrefs()
                prefs.uIndentSize = CUInt(_dialog.IndentSize)
                prefs.IndentStyle = CType(_dialog.IndentType, Microsoft.VisualStudio.TextManager.Interop.vsIndentStyle)
                prefs.fLineNumbers = CUInt(_dialog.LineNumbers)
                prefs.uTabSize = CUInt(_dialog.TabSize)
                prefs.fWordWrap = CUInt(_dialog.WordWrap)
                Apply(prefs)
            Catch ex As Exception
                Debug.Fail("EditorToolsOptionPage::SaveSettings Caught exception " & ex.Message)
            End Try
            textEditorProperties = Nothing
            dte = Nothing
        End Sub

        ''' <summary>
        ''' Use the IVsTextManager to get the current user preferences
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetLanguagePrefs() As LANGPREFERENCES
            Dim myPrefs As New Microsoft.VisualStudio.TextManager.Interop.LANGPREFERENCES
            Dim textMgr As IVsTextManager = DirectCast(Me.GetService(GetType(SVsTextManager)), IVsTextManager)
            If textMgr IsNot Nothing Then
                myPrefs.guidLang = VBLangGUID
                Dim langPrefs() As Microsoft.VisualStudio.TextManager.Interop.LANGPREFERENCES = {New Microsoft.VisualStudio.TextManager.Interop.LANGPREFERENCES}
                langPrefs(0) = myPrefs
                If NativeMethods.Succeeded(textMgr.GetUserPreferences(Nothing, Nothing, langPrefs, Nothing)) Then
                    myPrefs = langPrefs(0)
                Else
                    Debug.Fail("textMgr.GetUserPreferences")
                    Throw New InternalException()
                End If
            End If
            Return myPrefs
        End Function

        ''' <summary>
        ''' Use the IVsTextManager to set the specified user preferences
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub Apply(ByVal prefs As LANGPREFERENCES)
            Dim textMgr As IVsTextManager = DirectCast(Me.GetService(GetType(SVsTextManager)), IVsTextManager)
            If textMgr IsNot Nothing Then
                prefs.guidLang = VBLangGUID
                Dim langPrefs() As Microsoft.VisualStudio.TextManager.Interop.LANGPREFERENCES = {New Microsoft.VisualStudio.TextManager.Interop.LANGPREFERENCES}
                langPrefs(0) = prefs
                If Not NativeMethods.Succeeded(textMgr.SetUserPreferences(Nothing, Nothing, langPrefs, Nothing)) Then
                    Debug.Fail("textMgr.SetUserPreferences")
                    Throw New InternalException()
                End If
            End If
        End Sub
    End Class
End Namespace


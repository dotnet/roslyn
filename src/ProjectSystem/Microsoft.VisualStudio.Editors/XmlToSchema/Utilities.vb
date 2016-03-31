Option Infer On

Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Diagnostics.CodeAnalysis
Imports System.Drawing
Imports System.Security.Permissions
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.DesignerFramework
Imports VsShell = Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    ''' <summary>
    ''' Utility method for the XmlToSchema wizard.
    ''' </summary>
    Friend Module Utilities
        Public Sub ShowWarning(ByVal message As String)
            DesignUtil.ShowWarning(VBPackage.Instance, message)
        End Sub

        Public Sub ShowWarning(ByVal ex As Exception)
            ShowWarning(String.Format(SR.XmlToSchema_Error, ex.Message))
        End Sub

        Public Function FilterException(ByVal ex As Exception) As Boolean
            Return Not TypeOf ex Is AccessViolationException AndAlso _
                   Not TypeOf ex Is StackOverflowException AndAlso _
                   Not TypeOf ex Is OutOfMemoryException
        End Function
    End Module

    ''' <summary>
    ''' A common base class for all XmlToSchema wizard forms.
    ''' </summary>
    Friend MustInherit Class XmlToSchemaForm
        Inherits Form

        Private m_serviceProvider As IServiceProvider

        Protected Sub New()
            Me.HelpButton = True
        End Sub

        Protected Sub New(ByVal serviceProvider As IServiceProvider)
            m_serviceProvider = serviceProvider
            Me.HelpButton = True
        End Sub

        Public Property ServiceProvider() As IServiceProvider
            Get
                Return m_serviceProvider
            End Get
            Set(ByVal value As IServiceProvider)
                m_serviceProvider = value
            End Set
        End Property

        Protected ReadOnly Property DialogFont() As Font
            Get
                Dim hostLocale As VsShell.IUIHostLocale2 = CType(m_serviceProvider.GetService(GetType(VsShell.SUIHostLocale)), VsShell.IUIHostLocale2)
                If hostLocale IsNot Nothing Then
                    Dim fonts(1) As VsShell.UIDLGLOGFONT
                    If VSErrorHandler.Succeeded(hostLocale.GetDialogFont(fonts)) Then
                        Return Font.FromLogFont(fonts(0))
                    End If
                End If
                Debug.Fail("Couldn't get a IUIHostLocale2 ... cheating instead :)")
                Return Form.DefaultFont
            End Get
        End Property

        Protected Overrides Sub OnLoad(ByVal e As EventArgs)
            Debug.Assert(m_serviceProvider IsNot Nothing)
            If m_serviceProvider IsNot Nothing Then
                Me.Font = Me.DialogFont
            End If
            MyBase.OnLoad(e)
        End Sub

        Protected Overridable Function GetF1Keyword() As String
            Return "vb.XmlToSchemaWizard"
        End Function

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")> _
        Private Sub ShowHelp()
            Try
                Dim f1Word = GetF1Keyword()
                If m_serviceProvider IsNot Nothing AndAlso Not String.IsNullOrEmpty(f1Word) Then
                    Dim vshelp As VSHelp.Help = CType(m_serviceProvider.GetService(GetType(VSHelp.Help)), VSHelp.Help)
                    vshelp.DisplayTopicFromF1Keyword(f1Word)
                Else
                    Debug.Fail("Can not find ServiceProvider")
                End If
            Catch ex As System.Exception
                Debug.Fail("Unexpected exception during Help invocation " + ex.Message)
            End Try
        End Sub

        Protected NotOverridable Overrides Sub OnHelpButtonClicked(ByVal e As CancelEventArgs)
            ShowHelp()
        End Sub

        Protected NotOverridable Overrides Sub OnHelpRequested(ByVal hevent As HelpEventArgs)
            ShowHelp()
            hevent.Handled = True
        End Sub

        <SecurityPermission(SecurityAction.LinkDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        <SecurityPermission(SecurityAction.InheritanceDemand, Flags:=SecurityPermissionFlag.UnmanagedCode)> _
        Protected Overrides Sub WndProc(ByRef m As Message)
            Try
                Select Case (m.Msg)
                    Case Interop.win.WM_SYSCOMMAND
                        If CInt(m.WParam) = Interop.win.SC_CONTEXTHELP Then
                            ShowHelp()
                        Else
                            MyBase.WndProc(m)
                        End If
                    Case Else
                        MyBase.WndProc(m)
                End Select
            Catch ex As Exception
                If Not FilterException(ex) Then
                    Throw
                End If
            End Try
        End Sub
    End Class
End Namespace

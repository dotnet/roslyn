Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Diagnostics
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    '**************************************************************************
    ';DesignerMessageBox
    '
    'Remarks:
    '   This class provides the correct way of doing message box for designer packages.
    '**************************************************************************
    ' This class is converted from <DD>\wizard\vsdesigner\designer\microsoft\vsdesigner\VSDMessageBox.cs.
    Friend Class DesignerMessageBox

        Private Const MaxErrorMessageLength As Integer = 600

        '= FRIEND =============================================================


        ''' <summary>
        ''' Displays a message box for a specified exception, caption, buttons, icons, default button and help link.
        ''' </summary>
        ''' <param name="RootDesigner">A root designer inherited from BaseRootDesigner, which has the ability to get services.</param>
        ''' <param name="Caption">The text to display in the title bar of the message box.</param>
        ''' <param name="HelpLink">Link to the help topic for this message box.</param>
        ''' <remarks></remarks>
        Friend Shared Function Show(ByVal RootDesigner As BaseRootDesigner, ByVal Message As String, _
                ByVal Caption As String, ByVal Buttons As MessageBoxButtons, ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing _
        ) As DialogResult
            Return Show(DirectCast(RootDesigner, IServiceProvider), Message, Caption, Buttons, Icon, DefaultButton, HelpLink)
        End Function 'Show


        ''' <summary>
        ''' Displays a message box for a specified exception, caption, buttons, icons, default button and help link.
        ''' </summary>
        ''' <param name="ServiceProvider">The IServiceProvider, used to get devenv shell as the parent of the message box.</param>
        ''' <param name="ex">The exception to include in the message.</param>
        ''' <param name="Caption">The text to display in the title bar of the message box.</param>
        ''' <param name="HelpLink">Link to the help topic for this message box.</param>
        ''' <remarks></remarks>
        Friend Shared Sub Show(ByVal ServiceProvider As IServiceProvider, ByVal ex As Exception, _
                ByVal Caption As String, Optional ByVal HelpLink As String = Nothing)
            Show(ServiceProvider, Nothing, ex, Caption, HelpLink)
        End Sub


        ''' <summary>
        ''' Displays a message box for a specified exception, caption, buttons, icons, default button and help link.
        ''' </summary>
        ''' <param name="ServiceProvider">The IServiceProvider, used to get devenv shell as the parent of the message box.</param>
        ''' <param name="Message">The text to display in the message box.</param>
        ''' <param name="ex">The exception to include in the message.  The exception's message will be on a second line after errorMessage.</param>
        ''' <param name="Caption">The text to display in the title bar of the message box.</param>
        ''' <param name="HelpLink">Link to the help topic for this message box.</param>
        ''' <remarks>
        ''' The exception's message will be on a second line after errorMessage.
        ''' </remarks>
        Friend Shared Sub Show(ByVal ServiceProvider As IServiceProvider, ByVal Message As String, ByVal ex As Exception, _
                ByVal Caption As String, Optional ByVal HelpLink As String = Nothing)

            If ex Is Nothing Then
                Debug.Fail("ex should not be Nothing")
                Return
            End If

            'Pull out the original exception from target invocation exceptions (happen during serialization, etc.)
            If TypeOf ex Is TargetInvocationException Then
                ex = ex.InnerException
            End If

            If Common.Utils.IsCheckoutCanceledException(ex) Then
                'The user knows he just canceled the checkout.  We don't have to tell him.  (Yes, other editors and the
                '  Fx framework itself does it this way, too.)
                Return
            End If

            If HelpLink = "" AndAlso ex IsNot Nothing Then
                HelpLink = ex.HelpLink
            End If

            'Add the exception text to the message
            If ex IsNot Nothing Then
                If Message = "" Then
                    Message = ex.Message
                Else
                    Message = Message & vbCrLf & ex.Message
                End If

                ' limit the length of message to prevent a bad layout.
                If Message.Length > MaxErrorMessageLength Then
                    Message = Message.Substring(0, MaxErrorMessageLength)
                End If
            Else
                Debug.Assert(Message <> "")
            End If

            Show(ServiceProvider, Message, Caption, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, HelpLink)
        End Sub


        ''' <summary>
        ''' Displays a message box for a specified exception, caption, buttons, icons, default button and help link.
        ''' </summary>
        ''' <param name="ServiceProvider">The IServiceProvider, used to get devenv shell as the parent of the message box.</param>
        ''' <param name="Message">The text to display in the message box.</param>
        ''' <param name="Caption">The text to display in the title bar of the message box.</param>
        ''' <param name="Buttons">One of the MessageBoxButtons values that specifies which buttons to display in the message box.</param>
        ''' <param name="Icon">One of the MessageBoxIcon values that specifies which icon to display in the message box.</param>
        ''' <param name="HelpLink">Link to the help topic for this message box.</param>
        ''' <param name="DefaultButton">One of the MessageBoxDefaultButton values that specifies the default button of the message box.</param>
        ''' <remarks></remarks>
        Friend Shared Function Show(ByVal ServiceProvider As IServiceProvider, ByVal Message As String, _
                ByVal Caption As String, ByVal Buttons As MessageBoxButtons, ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing _
        ) As DialogResult
            Return ShowHelper(ServiceProvider, Message, Caption, Buttons, Icon, DefaultButton, HelpLink)
        End Function 'Show


        ''' <summary>
        ''' Displays a message box for a specified exception, caption, buttons, icons, default button and help link.
        ''' </summary>
        ''' <param name="ServiceProvider">The IServiceProvider, used to get devenv shell as the parent of the message box.</param>
        ''' <param name="Message">The text to display in the message box.</param>
        ''' <param name="Caption">The text to display in the title bar of the message box.</param>
        ''' <param name="Buttons">One of the MessageBoxButtons values that specifies which buttons to display in the message box.</param>
        ''' <param name="Icon">One of the MessageBoxIcon values that specifies which icon to display in the message box.</param>
        ''' <param name="HelpLink">Link to the help topic for this message box.</param>
        ''' <param name="DefaultButton">One of the MessageBoxDefaultButton values that specifies the default button of the message box.</param>
        ''' <remarks></remarks>
        Private Shared Function ShowHelper(ByVal ServiceProvider As IServiceProvider, ByVal Message As String, _
                ByVal Caption As String, ByVal Buttons As MessageBoxButtons, ByVal Icon As MessageBoxIcon, _
                Optional ByVal DefaultButton As MessageBoxDefaultButton = MessageBoxDefaultButton.Button1, _
                Optional ByVal HelpLink As String = Nothing _
        ) As DialogResult

            If HelpLink = "" Then
                'Giving an empty string will show the Help button, we don't want it. Null won't.
                HelpLink = Nothing
            End If

            If Caption = "" Then
                Caption = Nothing 'Causes "Error" to be the caption...
            End If

            If ServiceProvider IsNot Nothing Then
                Try
                    Return ShowInternal(CType(ServiceProvider.GetService(GetType(IUIService)), IUIService), _
                        CType(ServiceProvider.GetService(GetType(IVsUIShell)), IVsUIShell), _
                        Message, Caption, Buttons, Icon, DefaultButton, HelpLink)
                Catch ex As Exception
                    Debug.Fail(ex.ToString)
                End Try
            Else
                Debug.Fail("ServiceProvider is Nothing! Message box won't have parent!")
            End If

            ' If there is no IServiceProvider, message box has no parent.
            Return MessageBox.Show(Nothing, Message, Caption, Buttons, Icon, DefaultButton)
        End Function 'Show


        '= PROTECTED ==========================================================

        '**************************************************************************
        ';ShowInternal
        '
        'Summary:
        '   Our implementation to display a message box with the specified message, caption, buttons, icons, default button,
        '   and help link. Also correctly set the parent of the message box.
        'Params:
        '   UIService: The IUIService class used to show message in case there is no help link.
        '   VsUIShell: The VsUIShell class used to show message in case there is a help link.
        '   Other params: see above.
        'Returns:
        '   One of the DialogResult values.
        'Remarks: (from VSDMessageBox)
        '   The current implementation prevents us from specifying a caption when a helpLink is provided. This is because 
        '   IVsUIShell.ShowMessageBox will display the caption as part of the message itself, not in the title bar.
        '   So instead of this we cut this feature. When no help is needed, a standard MessageBox will be shown 
        '   but parented using the service provider if available, the caption will also be shown normally.
        '**************************************************************************
        Protected Shared Function ShowInternal(ByVal UIService As IUIService, ByVal VsUIShell As IVsUIShell, _
                ByVal Message As String, ByVal Caption As String, ByVal Buttons As MessageBoxButtons, _
                ByVal Icon As MessageBoxIcon, ByVal DefaultButton As MessageBoxDefaultButton, ByVal HelpLink As String) _
        As DialogResult
            If VsUIShell IsNot Nothing Then
                Dim Guid As Guid = System.Guid.Empty

                Dim OLEButtons As OLEMSGBUTTON = CType(Buttons, OLEMSGBUTTON)
                Dim OLEDefaultButton As OLEMSGDEFBUTTON = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                Select Case DefaultButton
                    Case MessageBoxDefaultButton.Button1
                        OLEDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                    Case MessageBoxDefaultButton.Button2
                        OLEDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND
                    Case MessageBoxDefaultButton.Button2
                        OLEDefaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_THIRD
                End Select

                'We pass in Nothing for the caption because if we pass in an actual caption,
                '  IVsUIShell doesn't show it as the actual caption, but just as an extra line
                '  at the front of the text.  The caption is always chosen by IVsUIShell, which
                '  is the best thing anyway, we shouldn't have to provide a caption (it changes
                '  by installed SKU/product, for instance).
                Dim Result As Integer
                VSErrorHandler.ThrowOnFailure(VsUIShell.ShowMessageBox(0, Guid, Nothing, Message, HelpLink, 0, _
                        OLEButtons, OLEDefaultButton, MessageBoxIconToOleIcon(Icon), CInt(False), Result))
                Return CType(Result, DialogResult)
            Else
                Debug.Fail("Could not retreive IVsUIShell, message box will not be parented")
            End If

            ' Either UIService or VsUIShell does not exist, show message box without parent.
            Return MessageBox.Show(Nothing, Message, Caption, Buttons, Icon, DefaultButton)
        End Function 'ShowInternal

        '= PRIVATE ============================================================

        '**************************************************************************
        ';MessageBoxIconToOleIcon
        '
        'Summary:
        '   Convert the values from Framework's MessageBoxIcon enum to OLEMSGICON.
        '   The reason is IVsUIShell.ShowMessageBox does not accept values from MessageBoxIcon or WinUser.h,
        '       but values from OLEMSGICON in oleipc.h
        'Params:
        '   Icon: One of the MessageBoxIcon values.
        'Returns:
        '   The appropriate OLEMSGICON value.
        '**************************************************************************
        Private Shared Function MessageBoxIconToOleIcon(ByVal icon As MessageBoxIcon) As OLEMSGICON
            Select Case icon
                Case MessageBoxIcon.Error
                    'case MessageBoxIcon.Hand:
                    'case MessageBoxIcon.Stop:
                    Return OLEMSGICON.OLEMSGICON_CRITICAL
                Case MessageBoxIcon.Exclamation
                    'case MessageBoxIcon.Warning:
                    Return OLEMSGICON.OLEMSGICON_WARNING
                    'case MessageBoxIcon.Asterisk:
                Case MessageBoxIcon.Information
                    Return OLEMSGICON.OLEMSGICON_INFO
                Case MessageBoxIcon.Question
                    Return OLEMSGICON.OLEMSGICON_QUERY
                Case Else
                    Return OLEMSGICON.OLEMSGICON_NOICON
            End Select
        End Function 'MessageBoxIconToOleIcon 

        '**************************************************************************
        ';EmptyOrSpace
        '
        'Summary:
        '   Check if a string is empty string or only contains spaces.
        'Params:
        '   Str: The string to check.
        'Returns:
        '   True if the string is empty string or only contains spaces. Otherwise false.
        '**************************************************************************
        Private Shared Function EmptyOrSpace(ByVal Str As String) As Boolean
            Return Str = "" OrElse Str.Trim.Length <= 0
        End Function

    End Class 'DesignerMessageBox 
End Namespace

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Editors.AppDesInterop
Imports win = Microsoft.VisualStudio.Editors.AppDesInterop.win
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports System.Windows.Forms
Imports System.ComponentModel.Design
Imports System.Reflection


Namespace Microsoft.VisualStudio.Editors.PropPageDesigner


    Public NotInheritable Class PropPageDesignerWindowPane
        Inherits AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase.DesignerWindowPaneBase


        ''' <summary>
        ''' Creates a new WinformsWindowPane.
        ''' </summary>
        ''' <param name="surface"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal surface As DesignSurface)
            MyBase.New(surface, SupportToolBox:=False)
        End Sub


        ''' <summary>
        ''' Retrieves the PropPageDesignerView associated with this window, if any.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetPropPageDesignerView() As PropPageDesignerView
            Dim PPDView As PropPageDesignerView = Nothing
            If Me.View IsNot Nothing AndAlso Me.View.Controls IsNot Nothing AndAlso Me.View.Controls.Count > 0 Then
                Return TryCast(Me.View.Controls(0), PropPageDesignerView)
            End If

            Return Nothing
        End Function


        ''' <summary>
        ''' This gives us a crack at messages before they are routed to the control that the message
        '''   is directed to (it's called from the IVsWindowPane.TranslateAccelerator method that the
        '''   base DesignerWindowPane class implements).
        ''' It also allows us to handle messages that the page does not handle, in order to implement
        '''   tabbing from the property page to the property page designer.
        ''' </summary>
        ''' <param name="m"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function PreProcessMessage(ByRef m As System.Windows.Forms.Message) As Boolean
            Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "PropPageDesignerWindowPane.PreProcessMessage", m)

            If Me.View Is Nothing Then
                Return False
            End If

            Dim DesignerView As PropPageDesigner.PropPageDesignerView = GetPropPageDesignerView()
            If DesignerView IsNot Nothing Then
                Dim KeyCode As Keys = DirectCast(m.WParam.ToInt32(), Keys) And Keys.KeyCode
                'Is the message intended for a window or control in the property page?
                If DesignerView.IsNativeHostedPropertyPageActivated AndAlso NativeMethods.IsChild(Me.View.Handle, m.HWnd) Then
                    Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ... Message is for a child of the property page.  Calling MyBase.PreProcessMessage", m)

                    'First crack goes to the child page, since the HWND for the message belongs to it.
                    Dim HandledByChild As Boolean = MyBase.PreProcessMessage(m)

                    If HandledByChild Then
                        Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ... Property page handled the message", m)
                        Return True
                    Else
                        'Page doesn't want it...
                        Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ... Property page did not handle the message", m)

                        Select Case m.Msg
                            Case win.WM_KEYDOWN
                                Dim ShiftIsDown As Boolean = (Control.ModifierKeys And Keys.Shift) <> 0
                                Dim ControlIsDown As Boolean = (Control.ModifierKeys And Keys.Control) <> 0

                                'Don't do tab processing for CTRL+TAB - the shell handles that for window switching
                                If KeyCode = Keys.Tab AndAlso Not ControlIsDown Then
                                    Dim Forward As Boolean = Not ShiftIsDown

                                    'The control did not process the Tab.  Unless the control is intending to
                                    '  use TAB as a key for user input, this means that the property page has
                                    '  reached the end of its tab cycle.  So we need to cause the tabbing to 
                                    '  move to the property page designer view.

                                    'See if the active control wants to handle TAB as an input key (like the DataGridView)
                                    Dim WantsTab As Boolean = False
                                    Dim FocusedHwnd As IntPtr = NativeMethods.GetFocus()
                                    If Not FocusedHwnd.Equals(IntPtr.Zero) Then
                                        WantsTab = (NativeMethods.SendMessage(New HandleRef(Me, FocusedHwnd), win.WM_GETDLGCODE, 0, 0).ToInt32() And win.DLGC_WANTTAB) <> 0

                                        'Not all WinForms controls respond correctly to WM_GETDLGCODE.
                                        '  One way to figure this out would be Control.FromChildHandle().IsInputKey, but
                                        '  that method is protected.
                                        If Not WantsTab Then
                                            Dim c As Control = Control.FromHandle(FocusedHwnd)
                                            If c IsNot Nothing Then
                                                Try
                                                    Dim Method As MethodInfo = GetType(Control).GetMethod("IsInputKey", BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.InvokeMethod)
                                                    If Method Is Nothing Then
                                                        Debug.Fail("Couldn't find IsInputKey method")
                                                    Else
                                                        Dim KeyData As Keys = Keys.Tab
                                                        If ShiftIsDown Then
                                                            KeyData = KeyData Or Keys.Shift
                                                        End If
                                                        WantsTab = DirectCast(Method.Invoke(c, New Object() {KeyData}), Boolean)
                                                    End If
                                                Catch ex As Exception When Not AppDesCommon.IsUnrecoverable(ex)
                                                    Debug.Fail("Exception calling IsInputKey late-bound: " & ex.ToString)
                                                End Try
                                            End If
                                        End If
                                    End If
                                    If WantsTab Then
                                        'The control wants to handle tab as a WM_CHAR
                                        Return False
                                    End If

                                    Common.Switches.TracePDMessageRouting(TraceLevel.Warning, "  ... Tabbing from the property page to the property page designer", m)

                                    'We hit the last tabbable control in the property page designer, set focus to the first (or last)
                                    '  control in the property page itself.
                                    Dim PropPageDesignerHwnd As IntPtr = NativeMethods.GetWindow(Me.View.Handle, win.GW_CHILD)
                                    'Dim NextTabStop As IntPtr

                                    Dim PPDView As PropPageDesignerView = Me.GetPropPageDesignerView()
                                    If PPDView Is Nothing Then
                                        Debug.Fail("Couldn't find the property page designer view")
                                        Return False
                                    End If

                                    If Not Common.Utils.FocusFirstOrLastTabItem(PPDView.ConfigurationPanel.Handle, Forward) Then
                                        'No focusable controls in the property page designer (could be disabled or invisible), set focus to the
                                        '  property page again
                                        If Not PPDView.FocusFirstOrLastPropertyPageControl(Forward) Then
                                            Debug.Fail("Hmmm....  Nobody seems to want to handle the tab key")
                                            Return False
                                        End If
                                    End If

                                    Return True
                                End If
                            Case win.WM_SYSCHAR
                                'The property page didn't handle the message.  Allow the property page designer to check if
                                '  it's one of its accelerators.
                                Dim PPDView As PropPageDesignerView = Me.GetPropPageDesignerView()
                                If PPDView Is Nothing Then
                                    Debug.Fail("Couldn't find the property page designer view")
                                    Return False
                                End If
                                Return PPDView.PreProcessMessage(m)
                        End Select
                    End If

                    'The message was for the property page or a child, the page did not handle it, and we're not interested
                    '  in it.
                    Return False
                End If

                Dim PropPageHwnd As IntPtr = DesignerView.Handle()
                Dim msg As Microsoft.VisualStudio.OLE.Interop.MSG
                With msg
                    .hwnd = m.HWnd
                    .message = CType(m.Msg, UInteger)
                    .wParam = m.WParam
                    .lParam = m.LParam
                End With

                If m.Msg = win.WM_KEYDOWN AndAlso KeyCode = Keys.Escape AndAlso Not PropPageHwnd.Equals(IntPtr.Zero) Then
                    If NativeMethods.IsDialogMessage(New HandleRef(Me, PropPageHwnd), msg) Then
                        Return True
                    End If
                ElseIf m.Msg = win.WM_SYSKEYDOWN Then
                    'Here, we have to translate accelerators instead of letting shell handle them. Otherwise the shell menus
                    'will handle the accelerator first if we have a duplicate accelerator. See Dev10 bug 818320.
                    'We must never handle messages in two cases: if this is a VK_MENU, which means the user just pressed alt
                    'and did nothing more), or if shift key is being held down. This is to allow Alt+Shift+letter
                    'to go to the menus or do whatever else they are bound to

                    'GetKeyState returns with the the highest of the 16-bit value set if the key is being pressed
                    Dim shiftBeingHeld As Boolean = (NativeMethods.GetKeyState(Keys.ShiftKey) And &H8000) <> 0

                    If KeyCode <> Keys.Menu AndAlso Not shiftBeingHeld Then
                        If NativeMethods.TranslateMessage(msg) Then
                            'It was translated, so return true to prevent further processing of the message by the shell
                            Return True
                        End If
                    End If
                End If
            End If

            'The message is not for the property page - handle normally.
            Common.Switches.TracePDMessageRouting(TraceLevel.Info, "  ... Message is not for the property page - handling normally", m)
            Return MyBase.PreProcessMessage(m)
        End Function

    End Class

End Namespace

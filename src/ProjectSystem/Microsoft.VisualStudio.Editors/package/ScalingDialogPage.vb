Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports system.Windows.Forms.Design
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Package

    Public MustInherit Class ScalingDialogPage
        Inherits DialogPage

        Private m_isInitialized As Boolean
        Private m_isDirty As Boolean

        ''' <summary>
        ''' Pick font to use in this dialog page
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property GetDialogFont() As Font
            Get
                Dim uiSvc As IUIService = CType(Me.Site.GetService(GetType(IUIService)), IUIService)
                If uiSvc IsNot Nothing Then
                    Return CType(uiSvc.Styles("DialogFont"), Font)
                End If

                Debug.Fail("Couldn't get a IUIService... cheating instead :)")

                Return Form.DefaultFont
            End Get
        End Property

        Protected MustOverride Sub LoadSettings()
        Protected MustOverride Sub SaveSettings()

        Private m_SettingsLoaded As Boolean = False

        ''' <summary>
        ''' If we haven't allready loaded our settings, do so!
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub LoadSettingsFromStorage()
            If Not m_SettingsLoaded OrElse Not Me.Dirty Then
                LoadSettings()
                m_SettingsLoaded = True
                Dirty = False
            End If
        End Sub

        ''' <summary>
        ''' If we have loaded our settings, store them, otherwise this is a NOOP
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub SaveSettingsToStorage()
            If m_SettingsLoaded AndAlso Me.Dirty Then
                SaveSettings()
                m_SettingsLoaded = False
                Dirty = False
            End If
        End Sub

        ''' <summary>
        ''' Close this dialog page
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnClosed(ByVal e As System.EventArgs)
            m_SettingsLoaded = False
            MyBase.OnClosed(e)
        End Sub


        ''' <summary>
        ''' Adjust scales to avoid truncation errors
        ''' </summary>
        ''' <param name="scale"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function AdjustScale(ByVal scale As Single) As Single
            Return scale
        End Function

        ' This should be the initial size of the option pages...
        ' Since the scaling logic differs between winforms and native dialogs, we
        ' use this size to determine how much to scale our controls...
        Private m_PreviousSize As Size = New Size(395, 289)

        ''' <summary>
        ''' Set font and scale page accordingly
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnActivate(ByVal e As System.ComponentModel.CancelEventArgs)
            ' Hack around for not being called to load settings @ startup
            LoadSettingsFromStorage()

            ' Update font & scale accordingly...
            Dim Dialog As Control = CType(Me.Window, Control)

            If Dialog Is Nothing Then
                Debug.Fail("Couldn't get Control to display to the user")
                Return
            End If


            If Dialog.Controls.Count() >= 1 Then
                Dim NewFont As Font = GetDialogFont()
                Dim dx As Single = AdjustScale(CSng(Dialog.Size.Width / m_PreviousSize.Width))
                Dim dy As Single = AdjustScale(CSng(Dialog.Size.Height / m_PreviousSize.Height))

                ' Set the font of the child controls and scale accordingly - we can't set it 
                ' on the top-most control, because that would re-create the window, and the VSIP 
                ' Package doesn't like that!

                Dialog.Size = m_PreviousSize
                Dialog.Scale(New SizeF(dx, dy))
                m_PreviousSize = Dialog.Size

                For Each child As Control In Dialog.Controls
                    If InheritsFont(child) Then
                        child.Font = NewFont
                    End If
                Next
            End If

            If Not m_isInitialized Then
                AttachHandlers(Dialog)
                m_isInitialized = True
            End If
        End Sub

        ''' <summary>
        ''' Does the specified control inherit its parents font?
        ''' </summary>
        ''' <param name="ctrl">The control to check</param>
        ''' <returns>True if this is the case, false otherwise</returns>
        ''' <remarks>
        ''' This gives an inheriting control the chance to stop the font change for a 
        ''' specific control (i.e. Font preview)
        ''' </remarks>
        Protected Overridable Function InheritsFont(ByVal ctrl As Control) As Boolean
            Return True
        End Function

        ''' <summary>
        ''' Show a simple message box
        ''' </summary>
        ''' <param name="Message">The message to be shown</param>
        ''' <remarks></remarks>
        Protected Sub ShowDialogBox(ByVal Message As String)
            DesignerFramework.DesignerMessageBox.Show(Me.Site, Message, DesignerFramework.DesignUtil.GetDefaultCaption(Site), MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub


        Friend Property Dirty() As Boolean
            Get
                Return m_isDirty
            End Get
            Set(ByVal value As Boolean)
                m_isDirty = value
            End Set
        End Property

        Private Sub AttachHandlers(ByVal ctrl As Control)
            For Each child As Control In ctrl.Controls
                If child.GetType() Is GetType(CheckBox) Then
                    Dim cb As CheckBox = DirectCast(child, CheckBox)
                    AddHandler cb.CheckedChanged, AddressOf Me.SetDirty
                ElseIf child.GetType() Is GetType(TextBox) Then
                    AddHandler child.TextChanged, AddressOf Me.SetDirty
                ElseIf child.GetType() Is GetType(ComboBox) Then
                    Dim cb As ComboBox = DirectCast(child, ComboBox)
                    AddHandler cb.SelectedValueChanged, AddressOf Me.SetDirty
                ElseIf child.GetType() Is GetType(RadioButton) Then
                    Dim rb As RadioButton = DirectCast(child, RadioButton)
                    AddHandler rb.CheckedChanged, AddressOf Me.SetDirty
                ElseIf child.GetType() Is GetType(Button) Then
                    Dim btn As Button = DirectCast(child, Button)
                    AddHandler btn.Click, AddressOf Me.SetDirty
                End If

                AttachHandlers(child)
            Next
        End Sub

    Private Sub SetDirty(ByVal sender As Object, ByVal e As System.EventArgs)
      Debug.WriteLine(String.Format("Control {0} dirtied", DirectCast(sender, Control).Name))
      Me.Dirty = True
    End Sub

    End Class
End Namespace

Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Diagnostics
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' A customized property control data for the target framework combo box
    ''' </summary>
    Class TargetFrameworkPropertyControlData
        Inherits PropertyControlData

        Private comboBox As ComboBox

        Public Sub New(ByVal id As Integer, ByVal name As String, ByVal comboBox As ComboBox, ByVal setter As SetDelegate, ByVal getter As GetDelegate, ByVal flags As ControlDataFlags, ByVal AssocControls As System.Windows.Forms.Control())
            MyBase.New(id, name, comboBox, setter, getter, flags, AssocControls)
            Me.comboBox = comboBox

            AddHandler Me.comboBox.DropDownClosed, AddressOf ComboBox_DropDownClosed
        End Sub

        Private Function IsInstallOtherFrameworksSelected() As Boolean
            Return Me.comboBox.SelectedIndex >= 0 AndAlso 
                   TypeOf Me.comboBox.Items(Me.comboBox.SelectedIndex) Is InstallOtherFrameworksComboBoxValue
        End Function

        Private Sub NativageToInstallOtherFrameworksFWLink()

            If Site Is Nothing Then
                ' Can't do anything without a site
                Debug.Fail("Why is there no site?")
                Return
            End If

            Dim serviceProvider As New Microsoft.VisualStudio.Shell.ServiceProvider(Site)
            Dim vsUIShellOpenDocument As IVsUIShellOpenDocument = TryCast(serviceProvider.GetService(GetType(SVsUIShellOpenDocument).GUID), IVsUIShellOpenDocument)

            If vsUIShellOpenDocument Is Nothing Then
                ' Can't do anything without a IVsUIShellOpenDocument
                Debug.Fail("Why is there no IVsUIShellOpenDocument service?")
                Return
            End If

            Dim flags As UInteger = 0
            Dim url As String = My.Resources.Strings.InstallOtherFrameworksFWLink
            Dim resolution As VSPREVIEWRESOLUTION = VSPREVIEWRESOLUTION.PR_Default
            Dim reserved As UInteger = 0

            If ErrorHandler.Failed(vsUIShellOpenDocument.OpenStandardPreviewer(flags, url, resolution, reserved)) Then
                ' Behavior for OpenStandardPreviewer with no flags is to show a message box if
                ' it fails (will always return S_OK)
                Debug.Fail("IVsUIShellOpenDocument.OpenStandardPreviewer failed!")
            End If

        End Sub


        Private Sub ComboBox_DropDownClosed(ByVal sender As Object, ByVal e As System.EventArgs)

            If IsInstallOtherFrameworksSelected() Then

                ' If the drop down is closed and the selection is still on the 'Install other frameworks...' value,
                ' move the selection back to the last target framework value.  This can happen if arrowing when the drop
                ' down is open (no commit) and pressing escape
                Me.comboBox.SelectedIndex = IndexOfLastCommittedValue

            End If

        End Sub

        Protected Overrides Sub ComboBox_SelectionChangeCommitted(ByVal sender As Object, ByVal e As System.EventArgs)

            If IsInstallOtherFrameworksSelected() Then

                ' If the user chooses 'Install other frameworks...', move the selection back to the last target
                ' framework value and navigate to the fwlink
                Me.comboBox.SelectedIndex = IndexOfLastCommittedValue
                NativageToInstallOtherFrameworksFWLink()

            ElseIf Me.comboBox.SelectedIndex <> IndexOfLastCommittedValue Then

                MyBase.ComboBox_SelectionChangeCommitted(sender, e)

                ' Keep track of what the user chose in case 'Install other frameworks...' is chosen later,
                ' which allows us to revert back to this value
                IndexOfLastCommittedValue = Me.comboBox.SelectedIndex

            End If

        End Sub

        ''' <summary>
        ''' Remove references to objects to prevent memory leaks
        ''' </summary>
        Public Sub Cleanup()

            ' Clear the reference to the COM service provider
            Me.Site = Nothing

            ' Clear the handler added in the constructor
            RemoveHandler Me.comboBox.DropDownClosed, AddressOf ComboBox_DropDownClosed
        End Sub

        ''' <summary>
        ''' Holds the site provided the parent page when the parent page is able to obtain it
        ''' </summary>
        Public Property Site As IServiceProvider = Nothing

        ''' <summary>
        ''' Holds the last commited property value.  This can change with user interaction in the combo box
        ''' or by programmacticaly setting the property value (i.e. DTE)
        ''' </summary>
        Public Property IndexOfLastCommittedValue As Integer = -1

    End Class

End Namespace

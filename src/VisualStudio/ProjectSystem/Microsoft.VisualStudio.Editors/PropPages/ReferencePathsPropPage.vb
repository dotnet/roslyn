Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports Microsoft.VisualBasic
Imports Microsoft.Win32
Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.PlatformUI
Imports VB = Microsoft.VisualBasic
Imports VSLangProj80

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class ReferencePathsPropPage
        'Inherits UserControl
        Inherits PropPageUserControlBase


        ' We map colors for all bitmap buttons on the page, because the default one is too dark in high-contrast mode, and it is difficult to know whether it is disabled
        Private MoveUpImageOriginal As Image
        Private MoveUpImage As Image
        Private MoveUpGreyImage As Image
        Private MoveDownImageOriginal As Image
        Private MoveDownImage As Image
        Private MoveDownGreyImage As Image
        Private RemoveFolderImageOriginal As Image
        Private RemoveFolderImage As Image
        Private RemoveFolderGreyImage As Image

        Private inContrastMode As Boolean   ' whether we are in ContrastMode

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            ' Scale buttons
            MoveUp.Size = DpiHelper.LogicalToDeviceUnits(MoveUp.Size)
            MoveDown.Size = DpiHelper.LogicalToDeviceUnits(MoveDown.Size)
            RemoveFolder.Size = DpiHelper.LogicalToDeviceUnits(RemoveFolder.Size)

            'Add any initialization after the InitializeComponent() call
            Me.MinimumSize = Me.Size

            ' Recalculate all images for the button from the default image we put in the resource file
            MoveUpImageOriginal = Me.MoveUp.Image
            MoveDownImageOriginal = Me.MoveDown.Image
            RemoveFolderImageOriginal = Me.RemoveFolder.Image

            ' Rescale images
            DpiHelper.LogicalToDeviceUnits(MoveUpImageOriginal)
            DpiHelper.LogicalToDeviceUnits(MoveDownImageOriginal)
            DpiHelper.LogicalToDeviceUnits(RemoveFolderImageOriginal)

            GenerateButtonImages()
            UpdateButtonImages()

            AddChangeHandlers()
            EnableReferencePathGroup()

            AddHandler SystemEvents.UserPreferenceChanged, AddressOf Me.SystemEvents_UserPreferenceChanged
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
                RemoveHandler SystemEvents.UserPreferenceChanged, AddressOf Me.SystemEvents_UserPreferenceChanged
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer
        Friend WithEvents FolderLabel As System.Windows.Forms.Label
        Friend WithEvents Folder As System.Windows.Forms.TextBox
        Friend WithEvents FolderBrowse As System.Windows.Forms.Button
        Friend WithEvents AddFolder As System.Windows.Forms.Button
        Friend WithEvents UpdateFolder As System.Windows.Forms.Button
        Friend WithEvents ReferencePathLabel As System.Windows.Forms.Label
        Friend WithEvents ReferencePath As System.Windows.Forms.ListBox
        Friend WithEvents RemoveFolder As System.Windows.Forms.Button
        Friend WithEvents MoveUp As System.Windows.Forms.Button
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents addUpdateTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents MoveDown As System.Windows.Forms.Button

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.










        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ReferencePathsPropPage))
            Me.FolderLabel = New System.Windows.Forms.Label
            Me.Folder = New System.Windows.Forms.TextBox
            Me.FolderBrowse = New System.Windows.Forms.Button
            Me.AddFolder = New System.Windows.Forms.Button
            Me.UpdateFolder = New System.Windows.Forms.Button
            Me.ReferencePath = New System.Windows.Forms.ListBox
            Me.MoveUp = New System.Windows.Forms.Button
            Me.MoveDown = New System.Windows.Forms.Button
            Me.RemoveFolder = New System.Windows.Forms.Button
            Me.ReferencePathLabel = New System.Windows.Forms.Label
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.addUpdateTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.addUpdateTableLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'FolderLabel
            '
            resources.ApplyResources(Me.FolderLabel, "FolderLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.FolderLabel, 2)
            Me.FolderLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.FolderLabel.Name = "FolderLabel"
            '
            'Folder
            '
            resources.ApplyResources(Me.Folder, "Folder")
            Me.Folder.Margin = New System.Windows.Forms.Padding(0, 3, 3, 2)
            Me.Folder.Name = "Folder"
            '
            'FolderBrowse
            '
            resources.ApplyResources(Me.FolderBrowse, "FolderBrowse")
            Me.FolderBrowse.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.FolderBrowse.Margin = New System.Windows.Forms.Padding(3, 2, 0, 2)
            Me.FolderBrowse.Name = "FolderBrowse"
            '
            'AddFolder
            '
            resources.ApplyResources(Me.AddFolder, "AddFolder")
            Me.AddFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.AddFolder.Margin = New System.Windows.Forms.Padding(0, 0, 3, 0)
            Me.AddFolder.Name = "AddFolder"
            '
            'UpdateFolder
            '
            resources.ApplyResources(Me.UpdateFolder, "UpdateFolder")
            Me.UpdateFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.UpdateFolder.Margin = New System.Windows.Forms.Padding(3, 0, 0, 0)
            Me.UpdateFolder.Name = "UpdateFolder"
            '
            'ReferencePath
            '
            resources.ApplyResources(Me.ReferencePath, "ReferencePath")
            Me.ReferencePath.FormattingEnabled = True
            Me.ReferencePath.Margin = New System.Windows.Forms.Padding(0, 3, 3, 0)
            Me.ReferencePath.Name = "ReferencePath"
            Me.overarchingTableLayoutPanel.SetRowSpan(Me.ReferencePath, 4)
            '
            'MoveUp
            '
            resources.ApplyResources(Me.MoveUp, "MoveUp")
            Me.MoveUp.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.MoveUp.Margin = New System.Windows.Forms.Padding(3, 3, 0, 1)
            Me.MoveUp.Name = "MoveUp"
            '
            'MoveDown
            '
            resources.ApplyResources(Me.MoveDown, "MoveDown")
            Me.MoveDown.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.MoveDown.Margin = New System.Windows.Forms.Padding(3, 1, 0, 3)
            Me.MoveDown.Name = "MoveDown"
            '
            'RemoveFolder
            '
            resources.ApplyResources(Me.RemoveFolder, "RemoveFolder")
            Me.RemoveFolder.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
            Me.RemoveFolder.Margin = New System.Windows.Forms.Padding(3, 3, 0, 0)
            Me.RemoveFolder.Name = "RemoveFolder"
            '
            'ReferencePathLabel
            '
            resources.ApplyResources(Me.ReferencePathLabel, "ReferencePathLabel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.ReferencePathLabel, 2)
            Me.ReferencePathLabel.Margin = New System.Windows.Forms.Padding(0, 9, 0, 0)
            Me.ReferencePathLabel.Name = "ReferencePathLabel"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            Me.overarchingTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.addUpdateTableLayoutPanel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FolderLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ReferencePath, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ReferencePathLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Folder, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FolderBrowse, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MoveUp, 1, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.MoveDown, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.RemoveFolder, 1, 6)
            Me.overarchingTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            Me.overarchingTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
            '
            'addUpdateTableLayoutPanel
            '
            resources.ApplyResources(Me.addUpdateTableLayoutPanel, "addUpdateTableLayoutPanel")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.addUpdateTableLayoutPanel, 2)
            Me.addUpdateTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.addUpdateTableLayoutPanel.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
            Me.addUpdateTableLayoutPanel.Controls.Add(Me.AddFolder, 0, 0)
            Me.addUpdateTableLayoutPanel.Controls.Add(Me.UpdateFolder, 1, 0)
            Me.addUpdateTableLayoutPanel.Margin = New System.Windows.Forms.Padding(0, 3, 0, 3)
            Me.addUpdateTableLayoutPanel.Name = "addUpdateTableLayoutPanel"
            Me.addUpdateTableLayoutPanel.RowStyles.Add(New System.Windows.Forms.RowStyle)
            '
            'ReferencePathsPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Margin = New System.Windows.Forms.Padding(0)
            Me.Name = "ReferencePathsPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.addUpdateTableLayoutPanel.ResumeLayout(False)
            Me.addUpdateTableLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

#End Region

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then
                    m_ControlData = New PropertyControlData() { _
                        New PropertyControlData(VsProjPropId.VBPROJPROPID_ReferencePath, "ReferencePath", Nothing, AddressOf Me.ReferencePathSet, AddressOf Me.ReferencePathGet, ControlDataFlags.PersistedInProjectUserFile)}
                End If
                Return m_ControlData
            End Get
        End Property

        ''' <summary>
        '''  Return true if the page can be resized...
        ''' </summary>
        Public Overrides ReadOnly Property PageResizable() As Boolean
            Get
                Return True
            End Get
        End Property

        Private Function ReferencePathSet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'enable when the enum comes online
            Dim RefPathList As String() = Split(DirectCast(value, String), ";")

            ReferencePath.BeginUpdate()
            Try
                Dim ItemText As String
                ReferencePath.Items.Clear()
                For i As Integer = 0 To RefPathList.Length - 1
                    ItemText = Trim(RefPathList(i))
                    If Len(ItemText) > 0 Then
                        ReferencePath.Items.Add(ItemText)
                    End If
                Next i
            Finally
                ReferencePath.EndUpdate()
            End Try
            Return True
        End Function

        Private Function ReferencePathGetValue() As String
            Dim RefPath As String
            Dim count As Integer = ReferencePath.Items.Count

            If count = 0 Then
                Return ""
            End If

            RefPath = DirectCast(ReferencePath.Items(0), String)
            For i As Integer = 1 To ReferencePath.Items.Count - 1
                RefPath &= ";" & DirectCast(ReferencePath.Items(i), String)
            Next i
            Return RefPath
        End Function

        Private Function ReferencePathGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            value = ReferencePathGetValue()
            Return True
        End Function

        ''' <summary>
        ''' Customizable processing done after base class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PostInitPage()
            MyBase.PostInitPage()
            EnableReferencePathGroup()
        End Sub

        Private Function IsValidFolderPath(ByRef Dir As String) As Boolean
            Return System.IO.Directory.Exists(Dir)
        End Function

        Private Sub AddFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles AddFolder.Click
            Dim FolderText As String = GetCurrentFolderPathAbsolute()
            If Len(FolderText) > 0 AndAlso ReferencePath.FindStringExact(FolderText) = -1 Then
                If IsValidFolderPath(FolderText) Then
                    Me.ReferencePath.SelectedIndex = Me.ReferencePath.Items.Add(FolderText)
                    SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
                Else
                    ShowErrorMessage(SR.GetString(SR.PPG_InvalidFolderPath))
                End If
            End If
        End Sub

        Private Sub UpdateFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles UpdateFolder.Click
            Dim FolderText As String = GetCurrentFolderPathAbsolute()
            Dim index As Integer = Me.ReferencePath.SelectedIndex

            If index >= 0 AndAlso Len(FolderText) > 0 Then
                If IsValidFolderPath(FolderText) Then
                    Me.ReferencePath.Items(index) = FolderText
                    SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
                    UpdateFolder.Enabled = False
                Else
                    ShowErrorMessage(SR.GetString(SR.PPG_InvalidFolderPath))
                End If
            End If
        End Sub

        Private Sub RemoveFolder_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RemoveFolder.Click
            '
            RemoveCurrentPath()
        End Sub

        Private Sub RemoveCurrentPath()
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex

            If SelectedIndex >= 0 Then
                ReferencePath.BeginUpdate()
                ReferencePath.Items.RemoveAt(SelectedIndex)
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub MoveUp_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveUp.Click
            '
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
            Dim SelectedItem As Object = ReferencePath.SelectedItem

            If SelectedIndex > 0 Then
                ReferencePath.BeginUpdate()
                'To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                'Insert item copy
                ReferencePath.Items.Insert(SelectedIndex - 1, SelectedItem)
                'Change selection
                ReferencePath.SelectedIndex = SelectedIndex - 1
                'Remove old copy
                ReferencePath.Items.RemoveAt(SelectedIndex + 1) 'add 1 because of insertion
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub MoveDown_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveDown.Click
            '
            Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
            Dim SelectedItem As Object = ReferencePath.SelectedItem

            If SelectedIndex <> -1 AndAlso SelectedIndex < (ReferencePath.Items.Count - 1) Then
                ReferencePath.BeginUpdate()
                'To prevent the flashing of the Remove button, Insert new item, change selection, then remove old item
                'Insert item copy
                ReferencePath.Items.Insert(SelectedIndex + 2, SelectedItem)
                'Change item selection
                ReferencePath.SelectedIndex = SelectedIndex + 2
                'Remove old location
                ReferencePath.Items.RemoveAt(SelectedIndex) 'add 1 because of insertion
                ReferencePath.EndUpdate()
                SetDirty(VsProjPropId.VBPROJPROPID_ReferencePath)
            End If
        End Sub

        Private Sub ReferencePath_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles ReferencePath.SelectedIndexChanged
            If Not m_fInsideInit Then
                Dim FolderText As String
                Dim SelectedIndex As Integer = Me.ReferencePath.SelectedIndex

                If SelectedIndex = -1 Then
                    FolderText = ""
                Else
                    FolderText = DirectCast(Me.ReferencePath.Items(SelectedIndex), String)
                End If
                If Me.Folder.Text <> FolderText Then
                    Me.Folder.Text = FolderText
                    If Me.Folder.Focused Then
                        'Set caret at end of text
                        Me.Folder.SelectionLength = 0
                        Me.Folder.SelectionStart = FolderText.Length
                    End If
                End If
                Me.EnableReferencePathGroup()
            End If
        End Sub

        ''' <summary>
        '''  process key event on the ReferencePath ListBox
        ''' </summary>
        Private Sub ReferencePath_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles ReferencePath.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim SelectedIndex As Integer = ReferencePath.SelectedIndex
                If SelectedIndex >= 0 Then
                    RemoveCurrentPath()

                    If SelectedIndex < ReferencePath.Items.Count Then
                        ReferencePath.SelectedIndex = SelectedIndex
                    ElseIf SelectedIndex > 0 Then
                        ReferencePath.SelectedIndex = SelectedIndex - 1
                    End If
                End If
            End If
        End Sub

        Private Sub EnableReferencePathGroup()
            Dim ItemIndices As ListBox.SelectedIndexCollection = Me.ReferencePath.SelectedIndices
            Dim SelectedCount As Integer = ItemIndices.Count
            Dim FolderText As String = GetCurrentFolderPathAbsolute()

            'Enable/Disable RemoveFolder button
            Me.RemoveFolder.Enabled = (SelectedCount > 0)

            'Enable/Disable Add/UpdateFolder buttons
            Dim HasFolderEntry As Boolean = (Len(FolderText) > 0)
            Me.AddFolder.Enabled = HasFolderEntry
            Me.UpdateFolder.Enabled = HasFolderEntry AndAlso (SelectedCount = 1) AndAlso String.Compare(FolderText, DirectCast(Me.ReferencePath.SelectedItem, String), StringComparison.OrdinalIgnoreCase) <> 0

            'Enable/Disable MoveUp/MoveDown buttons
            Me.MoveUp.Enabled = (SelectedCount = 1) AndAlso (ItemIndices.Item(0) > 0)
            Me.MoveDown.Enabled = (SelectedCount = 1) AndAlso (ItemIndices.Item(0) < (ReferencePath.Items.Count - 1))
        End Sub


        ''' <include file='doc\Control.uex' path='docs/doc[@for="Control.ProcessDialogKey"]/*' />
        ''' <summary>
        '''     Processes a dialog key. This method is called during message
        '''     pre-processing to handle dialog characters, such as TAB, RETURN, ESCAPE,
        '''     and arrow keys. This method is called only if the isInputKey() method
        '''     indicates that the control isn't interested in the key.
        ''' processDialogKey() simply sends the character to the parent's
        '''     processDialogKey() method, or returns false if the control has no
        '''     parent. The Form class overrides this method to perform actual
        '''     processing of dialog keys.
        ''' When overriding processDialogKey(), a control should return true to
        '''     indicate that it has processed the key. For keys that aren't processed
        '''     by the control, the result of "base.processDialogKey(...)" should be
        '''     returned.
        ''' Controls will seldom, if ever, need to override this method.
        ''' </summary>
        Protected Overrides Function ProcessDialogKey(ByVal KeyData As Keys) As Boolean
            If (KeyData And (Keys.Alt Or Keys.Control)) = Keys.None Then
                Dim keyCode As Keys = KeyData And Keys.KeyCode
                If keyCode = Keys.Enter Then
                    If ProcessEnterKey() Then
                        Return True
                    End If
                End If
            End If

            Return MyBase.ProcessDialogKey(KeyData)
        End Function


        ''' <summary>
        ''' Processes the ENTER key for this dialog.  We use this instead of KeyPress/Down events
        '''   because the OK key on the modal dialog base (PropPageHostDialog) grabs the ENTER key
        '''   and uses it to shut down the dialog.
        ''' </summary>
        ''' <returns>True iff the ENTER key is actually used.  False indicates it should be allowed
        '''   to be passed along and processed normally.</returns>
        ''' <remarks></remarks>
        Private Function ProcessEnterKey() As Boolean
            'If the focus is on the Folder textbox, and the AddFolder button is enabled, then 
            '  we interpret ENTER as meaning, "Add this folder", i.e., click on the AddFolder button.
            If ActiveControl Is Me.Folder AndAlso AddFolder.Enabled Then
                AddFolder.PerformClick()
                Return True
            End If

            Return False
        End Function

        Private Sub Folder_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Folder.TextChanged
            If Not m_fInsideInit Then
                EnableReferencePathGroup()
            End If
        End Sub

        ''' <summary>
        ''' Gets the absolute path to the path currently in the Folder textbox.  If the path is invalid (contains bad
        '''   characters, etc.), returns simply the current text.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetCurrentFolderPathAbsolute() As String
            Dim FolderText As String = Trim(Folder.Text)
            If FolderText.Length > 0 Then
                Try
                    'Interpret as relative to the project path, and make it absolute
                    FolderText = IO.Path.Combine(GetProjectPath(), FolderText)
                    FolderText = Utils.AppendBackslash(FolderText)
                Catch ex As Exception
                    Common.RethrowIfUnrecoverable(ex)
                End Try
            End If

            Return FolderText
        End Function

        Private Sub FolderBrowse_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles FolderBrowse.Click
            Dim value As String = Nothing
            If GetDirectoryViaBrowse(GetCurrentFolderPathAbsolute(), SR.GetString(SR.PPG_SelectReferencePath), value) Then
                Folder.Text = GetProjectRelativeDirectoryPath(value)
            End If
        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            If IsJSProject Then
                Return HelpKeywords.JSProjPropReferencePaths
            ElseIf IsCSProject Then
                Return HelpKeywords.CSProjPropReferencePaths
            Else
                Debug.Assert(IsVBProject, "Unknown project type")
                Return HelpKeywords.VBProjPropReferencePaths
            End If
        End Function

        '''<summary>
        ''' Handle button Enabled property changing event to reset its image
        '''</summary>
        Private Sub GraphicButton_OnEnabledChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles MoveUp.EnabledChanged, MoveDown.EnabledChanged, RemoveFolder.EnabledChanged
            UpdateButtonImages()
        End Sub

        '''<summary>
        ''' We change the image when button is disabled/enabled.
        ''' WinForm could generate default image for disabled button, but in high-contrast mode, it didn't work very well for our buttons
        '''</summary>
        Private Sub UpdateButtonImages()
            If MoveUp.Enabled Then
                MoveUp.Image = MoveUpImage
            Else
                MoveUp.Image = MoveUpGreyImage
            End If

            If MoveDown.Enabled Then
                MoveDown.Image = MoveDownImage
            Else
                MoveDown.Image = MoveDownGreyImage
            End If

            If RemoveFolder.Enabled Then
                RemoveFolder.Image = RemoveFolderImage
            Else
                RemoveFolder.Image = RemoveFolderGreyImage
            End If
        End Sub

        ''' <summary>
        '''  Generate button images in different system setting.
        ''' WinForm could generate default image for disabled button, but in high-contrast mode, it didn't work very well for our buttons
        ''' </summary>
        Private Sub GenerateButtonImages()
            Dim greyColor As Color = SystemColors.ControlDark

            If SystemInformation.HighContrast Then
                inContrastMode = True
                greyColor = SystemColors.Control
            Else
                inContrastMode = False
            End If

            Dim originalImage As Image = MoveUpImageOriginal
            MoveUpImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            MoveUpGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)

            originalImage = MoveDownImageOriginal
            MoveDownImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            MoveDownGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)

            originalImage = RemoveFolderImageOriginal
            RemoveFolderImage = Utils.MapBitmapColor(originalImage, Color.Black, SystemColors.ControlText)
            RemoveFolderGreyImage = Utils.MapBitmapColor(originalImage, Color.Black, greyColor)
        End Sub

        ''' <summary>
        '''  Handle SystemEvents, so we will update Buttom image when SystemColor was changed...
        ''' </summary>
        Private Sub SystemEvents_UserPreferenceChanged(ByVal sender As Object, ByVal e As UserPreferenceChangedEventArgs)
            Select Case e.Category
                Case UserPreferenceCategory.Accessibility
                    If inContrastMode <> SystemInformation.HighContrast Then
                        GenerateButtonImages()
                        UpdateButtonImages()
                    End If
                Case UserPreferenceCategory.Color
                    GenerateButtonImages()
                    UpdateButtonImages()
            End Select
        End Sub

    End Class

End Namespace

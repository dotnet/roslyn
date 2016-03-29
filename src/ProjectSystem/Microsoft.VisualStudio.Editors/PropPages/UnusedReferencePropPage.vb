Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Interop
Imports Microsoft.VisualStudio.Shell.Interop
Imports System
Imports System.Collections
Imports System.Diagnostics
Imports System.Reflection.AssemblyName
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class UnusedReferencePropPage
        'Inherits UserControl
        Inherits PropPageUserControlBase

        ' Rate to poll compiler for unused references in milliseconds 
        Const PollingRate As Integer = 500

        ' Whether we have generated the list
        Private m_UnusedReferenceListReady As Boolean

        ' Project hierarchy
        Private m_Hier As IVsHierarchy

        ' Compiler's reference usage provider interface
        Private m_RefUsageProvider As IVBReferenceUsageProvider

        ' Timer to poll compiler for unused references list
        Private m_GetUnusedRefsTimer As Timer

        Friend WithEvents ColHdr_Type As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Path As System.Windows.Forms.ColumnHeader
        Friend WithEvents UnusedReferenceList As System.Windows.Forms.ListView
        Friend WithEvents ColHdr_RefName As System.Windows.Forms.ColumnHeader
        Friend WithEvents ColHdr_Version As System.Windows.Forms.ColumnHeader
        Friend WithEvents UnusedReferencesListLabel As System.Windows.Forms.Label
        Friend WithEvents ColHdr_CopyLocal As System.Windows.Forms.ColumnHeader

        ' The host dialog...
        Friend WithEvents m_HostDialog As PropPageHostDialog

        ' helper object to sort the reference list
        Private m_ReferenceSorter As ListViewComparer

        ' keep the last status of the last call to GetUnusedReferences...
        ' we only update UI when the status was changed...
        Private m_LastStatus As ReferenceUsageResult = ReferenceUsageResult.ReferenceUsageUnknown

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()

            'support sorting
            m_ReferenceSorter = New ListViewComparer()
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub


        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(UnusedReferencePropPage))
            Me.ColHdr_Type = New System.Windows.Forms.ColumnHeader("")
            Me.ColHdr_Path = New System.Windows.Forms.ColumnHeader("")
            Me.UnusedReferenceList = New System.Windows.Forms.ListView
            Me.ColHdr_RefName = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns"))
            Me.ColHdr_Version = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns1"))
            Me.ColHdr_CopyLocal = New System.Windows.Forms.ColumnHeader(resources.GetString("UnusedReferenceList.Columns2"))
            Me.UnusedReferencesListLabel = New System.Windows.Forms.Label
            Me.SuspendLayout()
            '
            'ColHdr_Type
            '
            resources.ApplyResources(Me.ColHdr_Type, "ColHdr_Type")
            '
            'ColHdr_Path
            '
            resources.ApplyResources(Me.ColHdr_Path, "ColHdr_Path")
            '
            'UnusedReferenceList
            '
            resources.ApplyResources(Me.UnusedReferenceList, "UnusedReferenceList")
            Me.UnusedReferenceList.AutoArrange = False
            Me.UnusedReferenceList.BackColor = System.Drawing.SystemColors.Window
            Me.UnusedReferenceList.CheckBoxes = True
            Me.UnusedReferenceList.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColHdr_RefName, Me.ColHdr_Type, Me.ColHdr_Version, Me.ColHdr_CopyLocal, Me.ColHdr_Path})
            Me.UnusedReferenceList.FullRowSelect = True
            Me.UnusedReferenceList.Margin = New System.Windows.Forms.Padding(0, 3, 0, 0)
            Me.UnusedReferenceList.MultiSelect = False
            Me.UnusedReferenceList.Name = "UnusedReferenceList"
            Me.UnusedReferenceList.View = System.Windows.Forms.View.LargeIcon
            '
            'ColHdr_RefName
            '
            resources.ApplyResources(Me.ColHdr_RefName, "ColHdr_RefName")
            '
            'ColHdr_Version
            '
            resources.ApplyResources(Me.ColHdr_Version, "ColHdr_Version")
            '
            'ColHdr_CopyLocal
            '
            resources.ApplyResources(Me.ColHdr_CopyLocal, "ColHdr_CopyLocal")
            '
            'UnusedReferencesListLabel
            '
            resources.ApplyResources(Me.UnusedReferencesListLabel, "UnusedReferencesListLabel")
            Me.UnusedReferencesListLabel.Margin = New System.Windows.Forms.Padding(0)
            Me.UnusedReferencesListLabel.Name = "UnusedReferencesListLabel"
            '
            'UnusedReferencePropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.Controls.Add(Me.UnusedReferencesListLabel)
            Me.Controls.Add(Me.UnusedReferenceList)
            Me.Name = "UnusedReferencePropPage"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

#Region "Properties "
        ''' <summary>
        '''  Return true if the page can be resized...
        ''' </summary>
        Public Overrides ReadOnly Property PageResizable() As Boolean
            Get
                Return True
            End Get
        End Property

#End Region


#Region "Protected Methods "
        Protected Overrides Function GetF1HelpKeyword() As String

            Return HelpKeywords.VBProjPropUnusedReference

        End Function

        ''' <summary>
        ''' ;PreApplyPageChanges
        ''' Applies property page by removing all checked references.
        ''' </summary>
        ''' <remarks>Called by ApplyPageChanges so project is in batch edit mode.</remarks>
        Protected Overrides Sub PreApplyPageChanges()

            ' Remove all checked references.
            RemoveCheckedRefs()

        End Sub

        ''' <summary>
        ''' ;OnParentChanged
        ''' We need hook up events from the hosting dialog
        ''' </summary>
        ''' <remarks>Called by ApplyPageChanges so project is in batch edit mode.</remarks>
        Protected Overrides Sub OnParentChanged(ByVal e As EventArgs)
            m_HostDialog = TryCast(Me.ParentForm, PropPageHostDialog)
            If m_HostDialog IsNot Nothing Then
                With m_HostDialog
                    AddHandler .FormClosed, AddressOf dialog_Close
                End With
            End If

        End Sub


#End Region

#Region "Private Methods "
        ''' <summary>
        ''' ;InitDialog
        ''' Initialize proppage for use on a PropPageHostDialog: 
        ''' Initialize proppage variables and install custom dialog event handlers.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub InitDialog()
            Try
                ' Get our project hierarchy
                m_Hier = ProjectHierarchy

                ' Get reference usage provider interface
                m_RefUsageProvider = CType(ServiceProvider.GetService(NativeMethods.VBCompilerGuid), IVBReferenceUsageProvider)
            Catch ex As Exception
                Debug.Fail("An exception was thrown in UnusedReferencePropPage.InitDialog" & vbCrLf & ex.ToString)
                Throw
            End Try

            ' Disable remove button and clear IsDirty
            m_UnusedReferenceListReady = False
            EnableRemoveRefs(False)

            ' Begin requesting unused references
            BeginGetUnusedRefs()

        End Sub

        ''' <summary>
        ''' ;Abort
        ''' Abort requesting unused references from compiler.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub Abort()

            ' Stop polling
            m_GetUnusedRefsTimer.Stop()

            ' End get unused references operation
            m_RefUsageProvider.StopGetUnusedReferences(m_Hier)

            ' Reset mouse cursor
            Debug.Assert(Me.ParentForm IsNot Nothing)
            Me.ParentForm.Cursor = Cursors.Default
        End Sub

        ''' <summary>
        ''' GetReferenceList
        ''' get list of project's references as VSLangProj.References 
        ''' </summary>
        ''' <remarks>RefsList should alway be set before using this proppage</remarks>
        Private Function GetReferenceList() As ArrayList
            Dim theVSProject As VSLangProj.VSProject
            Dim ReferenceCount As Integer
            Dim refsList As ArrayList

            theVSProject = CType(DTEProject.Object, VSLangProj.VSProject)
            ReferenceCount = theVSProject.References.Count

            refsList = New System.Collections.ArrayList(ReferenceCount)

            For refIndex As Integer = 0 To ReferenceCount - 1
                Dim ref As VSLangProj.Reference = theVSProject.References.Item(refIndex + 1) '1-based

                ' Don't consider implicitly added references because they cannot be removed
                ' from the VB project
                If Not IsImplicitlyAddedReference(ref) Then
                    refsList.Add(ref)
                End If
            Next refIndex

            Return refsList
        End Function

        ''' <summary>
        ''' ;EnableRemoveRefs
        ''' Enable/disable remove button and set whether there 
        ''' are changes to apply (IsDirty).
        ''' </summary>
        ''' <param name="_enabled">Enable or disable</param>
        ''' <remarks>Use when proppage is on a PropPageHostDialog</remarks>
        Private Sub EnableRemoveRefs(ByVal _enabled As Boolean)

            If Me.ParentForm IsNot Nothing Then
                Debug.Assert(TypeOf Me.ParentForm Is PropPageHostDialog, "Unused references list should be on host dialog")

                Dim RemoveButton As Button = CType(Me.ParentForm, PropPageHostDialog).OK

                ' Enable/Disable group
                RemoveButton.Enabled = _enabled

                ' indicate if we have references to remove on apply()
                If (_enabled) Then
                    SetDirty(Me)
                    RemoveButton.DialogResult = System.Windows.Forms.DialogResult.OK
                Else
                    ClearIsDirty()
                End If
            End If

        End Sub

        ''' <summary>
        ''' ;UpdateStatus
        ''' Sets the proppage appearance according to current operation
        ''' </summary>
        ''' <param name="Status">Current status of proppage (equivalent to status of GetUnusedRefsList call)</param>
        ''' <remarks></remarks>
        Private Sub UpdateStatus(ByVal Status As ReferenceUsageResult)

            ' Only update status when necessary
            If Status <> m_LastStatus Then
                ' Remeber last status set
                m_LastStatus = Status

                ' Use a arrow and hourglass cursor if waiting
                Debug.Assert(Me.ParentForm IsNot Nothing)
                If Status = ReferenceUsageResult.ReferenceUsageWaiting Then
                    Me.ParentForm.Cursor = Cursors.AppStarting
                Else
                    Me.ParentForm.Cursor = Cursors.Default
                End If

                ' Are there any unused references?
                If Status = ReferenceUsageResult.ReferenceUsageOK AndAlso m_UnusedReferenceListReady AndAlso _
                    UnusedReferenceList IsNot Nothing AndAlso UnusedReferenceList.Items.Count > 0 Then
                    ' Do initial enabling of remove button
                    EnableRemoveRefs(True)
                Else
                    Dim StatusText As String

                    ' Get a status string
                    Select Case Status
                        Case ReferenceUsageResult.ReferenceUsageOK
                            StatusText = SR.GetString(SR.PropPage_UnusedReferenceNoUnusedReferences)
                        Case ReferenceUsageResult.ReferenceUsageWaiting
                            StatusText = SR.GetString(SR.PropPage_UnusedReferenceCompileWaiting)
                        Case ReferenceUsageResult.ReferenceUsageCompileFailed
                            StatusText = SR.GetString(SR.PropPage_UnusedReferenceCompileFail)
                        Case ReferenceUsageResult.ReferenceUsageError
                            StatusText = SR.GetString(SR.PropPage_UnusedReferenceError)
                        Case Else
                            Debug.Fail("Unexpected status")
                            StatusText = ""
                    End Select

                    ' Use listview to display status text
                    With UnusedReferenceList
                        .BeginUpdate()

                        ' Add status text without a check box to 2nd ("Reference Name") column
                        .CheckBoxes = False
                        .Items.Clear()
                        .Items.Add("").SubItems.Add(StatusText)

                        ' Autosized listview columns 
                        SetReferenceListColumnWidths()

                        .EndUpdate()
                    End With

                    ' Disable remove button
                    EnableRemoveRefs(False)
                End If
            End If

        End Sub

        ''' <summary>
        ''' ;UpdateUnusedReferenceList
        ''' Adds a ListViewItem for each reference in UnusedRefsList.
        ''' </summary>
        ''' <param name="UnusedRefsList"> a list of VSLangProj.Reference object.</param>
        ''' <remarks>Uses ReferencePropPage.ReferenceToListViewItem to extract reference properties</remarks>
        Private Sub UpdateUnusedReferenceList(ByVal UnusedRefsList As ArrayList)

            ' Add all unused references to list view
            UnusedReferenceList.BeginUpdate()

            Try
                Dim lviRef As ListViewItem

                ' Set checkboxes and clear before adding checked items, otherwise they become unchecked
                UnusedReferenceList.CheckBoxes = True
                UnusedReferenceList.Items.Clear()

                For refIndex As Integer = 0 To UnusedRefsList.Count - 1
                    ' Convert VSLangProj.Reference to formatted list view item and insert into references list
                    lviRef = ReferencePropPage.ReferenceToListViewItem( _
                        CType(UnusedRefsList(refIndex), VSLangProj.Reference), UnusedRefsList(refIndex))

                    lviRef.Checked = True

                    UnusedReferenceList.Items.Add(lviRef)
                Next

                ' We need reset order every time the dialog pops up
                UnusedReferenceList.ListViewItemSorter = m_ReferenceSorter
                m_ReferenceSorter.SortColumn = 0
                m_ReferenceSorter.Sorting = SortOrder.Ascending
                UnusedReferenceList.Sorting = SortOrder.Ascending

                UnusedReferenceList.Sort()

                ' Resize columns
                SetReferenceListColumnWidths()
            Finally
                UnusedReferenceList.EndUpdate()
            End Try

        End Sub

        Private Sub BeginGetUnusedRefs()

            ' Get a new timer
            m_GetUnusedRefsTimer = New Timer
            m_GetUnusedRefsTimer.Interval = PollingRate
            AddHandler m_GetUnusedRefsTimer.Tick, AddressOf GetUnusedRefsTimer_Tick

            m_LastStatus = ReferenceUsageResult.ReferenceUsageUnknown

            ' Begin requesting unused references
            m_GetUnusedRefsTimer.Start()

        End Sub

        ''' <summary>
        ''' ;GetUnusedRefs
        ''' Poll compiler for unused references list from compiler and update listview
        ''' when recieved.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub GetUnusedRefs()

            ' Request unused references from 
            Dim UnusedRefPathsString As String = Nothing
            Dim Result As ReferenceUsageResult = _
                m_RefUsageProvider.GetUnusedReferences(m_Hier, UnusedRefPathsString)

            Try
                If Result <> ReferenceUsageResult.ReferenceUsageWaiting Then
                    ' Stop polling
                    m_GetUnusedRefsTimer.Stop()
                End If

                If Result = ReferenceUsageResult.ReferenceUsageOK Then
                    Using New WaitCursor
                        ' Clear unused references list
                        Dim UnusedRefsList As New ArrayList

                        ' Split string of reference paths into array and iterate
                        Dim UnusedRefPaths As String() = UnusedRefPathsString.Split(ChrW(0))

                        If UnusedRefPaths.Length > 0 Then
                            Dim pathHash As New Hashtable()
                            Dim assemblyNameHash As Hashtable = Nothing

                            Dim RefsList As ArrayList = GetReferenceList()

                            ' Compare paths first.  This is a better match, since libs on disk
                            ' can be out of sync with the source for proj to proj references
                            ' and it is much faster. We should prevent calling GetAssemblyName, because it is very slow.

                            ' Prepare a hashtable for quick match
                            For iRef As Integer = 0 To RefsList.Count - 1 Step 1
                                Dim RefPath As String = CType(RefsList(iRef), VSLangProj.Reference).Path
                                If RefPath <> "" Then
                                    pathHash.Add(RefPath.ToUpper(System.Globalization.CultureInfo.InvariantCulture), iRef)
                                End If
                            Next

                            For Each UnusedRefPath As String In UnusedRefPaths
                                If UnusedRefPath.Length > 0 Then

                                    Dim formatedPath As String = UnusedRefPath.ToUpper(System.Globalization.CultureInfo.InvariantCulture)
                                    Dim refObj As Object = pathHash.Item(formatedPath)

                                    If refObj IsNot Nothing Then
                                        UnusedRefsList.Add(RefsList(CInt(refObj)))
                                        ' remove the one we matched, so we don't scan it and waste time to GetAssemblyName again...
                                        pathHash.Remove(formatedPath)
                                    ElseIf System.IO.File.Exists(UnusedRefPath) Then
                                        ' If we haven't matched any path, we need collect the assembly name and use it to do match...
                                        Dim UnusedRefName As String = GetAssemblyName(UnusedRefPath).FullName
                                        If UnusedRefName <> "" Then
                                            UnusedRefName = UnusedRefName.ToUpper(System.Globalization.CultureInfo.InvariantCulture)
                                            If assemblyNameHash Is Nothing Then
                                                assemblyNameHash = New Hashtable()
                                            End If
                                            assemblyNameHash.Add(UnusedRefName, Nothing)
                                        End If
                                    End If
                                End If
                            Next

                            If assemblyNameHash IsNot Nothing Then
                                ' try to match assemblyName...
                                For Each pathItem As DictionaryEntry In pathHash
                                    Dim RefPath As String = CStr(pathItem.Key)
                                    Dim iRef As Integer = CInt(pathItem.Value)
                                    If System.IO.File.Exists(RefPath) Then
                                        Dim assemblyName As System.Reflection.AssemblyName = GetAssemblyName(RefPath)
                                        If assemblyName IsNot Nothing Then
                                            Dim RefName As String = assemblyName.FullName.ToUpper(System.Globalization.CultureInfo.InvariantCulture)
                                            If assemblyNameHash.Contains(RefName) Then
                                                UnusedRefsList.Add(RefsList(iRef))
#If DEBUG Then
                                                assemblyNameHash.Item(RefName) = RefName
#End If
                                            End If
                                        End If
                                    End If
                                Next

#If DEBUG Then
                                For Each UnusedItem As DictionaryEntry In assemblyNameHash
                                    If UnusedItem.Value Is Nothing Then
                                        Debug.Fail("Could not find unused reference " & CStr(UnusedItem.Key))
                                    End If
                                Next
#End If
                            End If
                        End If

                        ' Update listview
                        UpdateUnusedReferenceList(UnusedRefsList)

                        m_UnusedReferenceListReady = True
                    End Using
                End If

            Catch ex As System.Exception
                Common.RethrowIfUnrecoverable(ex)
                Debug.Fail("An exception was thrown in GetUnusedRefs()" & vbCrLf & ex.ToString)

                Result = ReferenceUsageResult.ReferenceUsageError
            Finally
                ' Report status
                UpdateStatus(Result)
            End Try

        End Sub

        ''' <summary>
        ''' ;RemoveCheckRefs
        ''' Remove all references the user checked from the project.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub RemoveCheckedRefs()

            Dim ref As VSLangProj.Reference

            Dim ProjectReloaded As Boolean
            CheckoutProjectFile(ProjectReloaded)
            If ProjectReloaded Then
                Return
            End If

            Dim checkedItems As ListView.CheckedListViewItemCollection = UnusedReferenceList.CheckedItems
            If checkedItems.Count > 0 Then
                Using New WaitCursor
                    Dim batchEdit As ProjectBatchEdit = Nothing
                    If checkedItems.Count > 1 Then
                        batchEdit = New ProjectBatchEdit(m_Hier)
                    End If
                    Using batchEdit
                        ' Iterate all checked references to remove
                        For Each RefListItem As ListViewItem In UnusedReferenceList.CheckedItems
                            ref = DirectCast(RefListItem.Tag, VSLangProj.Reference)

                            ' Remove from project references
                            Debug.Assert(ref IsNot Nothing, "How did we get a bad reference object?")
                            ref.Remove()
                        Next
                    End Using
                End Using
            End If

        End Sub

        ''' <summary>
        ''' ;SetReferenceListColumnWidths
        ''' The Listview class does not support individual column widths, so we have to do it via sendmessage
        ''' </summary>
        ''' <remarks>Depends on ReferencePropPage.SetReferenceListColumnWidths</remarks>
        Private Sub SetReferenceListColumnWidths()

            UnusedReferenceList.View = View.Details

            ' Use ReferencePropPage's helper function for the common columns.
            ReferencePropPage.SetReferenceListColumnWidths(Me, Me.UnusedReferenceList, 0)

        End Sub

#Region "Event Handlers "

        Private Sub UnusedReferenceList_ItemCheck(ByVal sender As Object, ByVal e As System.Windows.Forms.ItemCheckEventArgs) Handles UnusedReferenceList.ItemCheck

            ' Since CheckIndicies is updated after this event, we enable remove button if
            ' there are more than one check references or there are none and one is being checked
            EnableRemoveRefs(e.NewValue = CheckState.Checked OrElse UnusedReferenceList.CheckedIndices.Count > 1)

        End Sub

        ''' <Summary>
        '''  When the customer clicks a column header, we should sort the reference list
        ''' </Summary>
        ''' <param name="sender">Event args</param>
        ''' <param name="e">Event args</param>
        Private Sub UnusedRerenceList_ColumnClick(ByVal sender As Object, ByVal e As ColumnClickEventArgs) Handles UnusedReferenceList.ColumnClick
            ListViewComparer.HandleColumnClick(UnusedReferenceList, m_ReferenceSorter, e)
        End Sub

        Private Sub GetUnusedRefsTimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs)

            ' Poll compiler
            GetUnusedRefs()

        End Sub


        ''' <Summary>
        '''  We need initialize the dialog when it pops up (everytime)
        ''' </Summary>
        ''' <param name="sender">Event args</param>
        ''' <param name="e">Event args</param>
        Private Sub dialog_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles m_HostDialog.Shown

            With CType(sender, PropPageHostDialog)
                ' Set dialog appearance
                .OK.Text = SR.GetString(SR.PropPage_UnusedReferenceRemoveButton)

                ' Allow dialog to be resized
                .FormBorderStyle = FormBorderStyle.Sizable

                ' Clean up the list: We don't want to see old list refreshes when we open the dialog again.
                UnusedReferenceList.Items.Clear()

                ' Supress column headers until something is added
                UnusedReferenceList.View = View.LargeIcon
            End With

            InitDialog()

        End Sub

        Private Sub dialog_Close(ByVal sender As Object, ByVal e As FormClosedEventArgs)

            ' Stop getting unused references list
            Me.Abort()

            ' Clean up the list
            UnusedReferenceList.Items.Clear()
        End Sub

#End Region

#End Region

    End Class

End Namespace


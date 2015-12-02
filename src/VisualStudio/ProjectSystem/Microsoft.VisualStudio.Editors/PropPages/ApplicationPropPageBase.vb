'******************************************************************************
'* ApplicationPropPageBase.vb
'*
'* Copyright (C) 1999-2004 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports EnvDTE
Imports Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Editors.Common
Imports System
Imports System.Collections
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports VSLangProj80
Imports VslangProj90
Imports Microsoft.VisualStudio.Editors.PropertyPages
Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Shell.Interop
Imports NativeMethods = Microsoft.VisualStudio.Editors.Interop.NativeMethods

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Contains functionality common to the application prop pages of VB, C# and J#
    '''   See comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ApplicationPropPageBase
        Inherits PropPageUserControlBase

        Private m_LastIconImage As String
        Protected m_DefaultIconText As String
        Protected m_DefaultManifestText As String
        Protected m_NoManifestText As String
        Protected m_DefaultIcon As Icon

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Me.SuspendLayout()
            Me.ResumeLayout(False)
        End Sub

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            'InitializeComponent()

            m_DefaultIconText = SR.GetString(SR.PPG_Application_DefaultIconText)
            m_DefaultManifestText = SR.GetString(SR.PPG_Application_DefaultManifestText)
            m_NoManifestText = SR.GetString(SR.PPG_Application_NoManifestText)
            m_DefaultIcon = System.Drawing.SystemIcons.Application
        End Sub


#Region "Application icon support"


        ''' <summary>
        ''' Retrieves the last set value for the Icon (as a path)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Protected ReadOnly Property LastIconImage() As String
            Get
                Return m_LastIconImage
            End Get
        End Property


        ''' <summary>
        ''' Obtains the icon path from the textbox
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function ApplicationIconGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim ApplicationIconCombobox As ComboBox = DirectCast(control, ComboBox)
            Dim ApplicationIconText As String = Trim(CStr(ApplicationIconCombobox.SelectedItem))

            'If no item selected, just use textbox value
            If ApplicationIconText = "" Then
                ApplicationIconText = Trim(ApplicationIconCombobox.Text)
            End If

            If IconEntryIsSpecial(ApplicationIconText) Then
                ApplicationIconText = ""
            End If

            value = ApplicationIconText
            Return True
        End Function


        ''' <summary>
        ''' Populates the given application icon combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindIconsInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        ''' <param name="ApplicationIconCombobox">The combobox that displays the list of icons</param>
        ''' <param name="CurrentIconValue">The current icon as a relative path.</param>
        ''' <remarks>
        ''' CurrentIconValue must be passed in because it's pulled from the control's current value, which is initially
        '''   set up by PropertyControlData), since clearing the list will clear the text value, too,
        '''   for a dropdown list.
        ''' </remarks>
        Protected Overridable Sub PopulateIconList(ByVal FindIconsInProject As Boolean, ByVal ApplicationIconCombobox As ComboBox, ByVal CurrentIconValue As String)
            Dim fInsideInitPrevious As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                ApplicationIconCombobox.Items.Clear()
                ApplicationIconCombobox.Items.Add(Me.m_DefaultIconText)
                If FindIconsInProject Then
                    For Each ProjectItem As EnvDTE.ProjectItem In DTEProject.ProjectItems
                        AddIconsFromProjectItem(ProjectItem, ApplicationIconCombobox)
                    Next
                End If

                If CurrentIconValue Is Nothing OrElse CurrentIconValue.Length = 0 Then
                    ApplicationIconCombobox.SelectedIndex = 0
                    Debug.Assert(IconEntryIsDefault(DirectCast(ApplicationIconCombobox.SelectedItem, String)), "First item should be the (Default Icon)")
                Else
                    'Can't simply set SelectedItem, because it uses a case-sensitive comparison
                    For Each Item As String In ApplicationIconCombobox.Items
                        If Item.Equals(CurrentIconValue, StringComparison.OrdinalIgnoreCase) Then
                            ApplicationIconCombobox.SelectedItem = Item
                            Exit For
                        End If
                    Next
                    If ApplicationIconCombobox.SelectedItem Is Nothing Then
                        'CurrentIcon is not in the last - add it after the default icon
                        Debug.Assert(ApplicationIconCombobox.Items.Count >= 1, "Where's the default icon in the list?")
                        Debug.Assert(IconEntryIsDefault(DirectCast(ApplicationIconCombobox.Items(0), String)), "First item should be the (Default Icon)")
                        ApplicationIconCombobox.Items.Insert(1, CurrentIconValue)
                        ApplicationIconCombobox.SelectedItem = CurrentIconValue
                    End If
                    Debug.Assert(TryCast(ApplicationIconCombobox.SelectedItem, String).Equals(CurrentIconValue, StringComparison.OrdinalIgnoreCase))
                End If
            Finally
                m_fInsideInit = fInsideInitPrevious
            End Try
        End Sub


        ''' <summary>
        ''' Adds an icon entry to the application icon combobox in its correct place
        ''' </summary>
        ''' <param name="ApplicationIconCombobox"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub AddIconEntryToCombobox(ByVal ApplicationIconCombobox As ComboBox, ByVal IconRelativePath As String)
            'By default, add the icon to the end of the combobox's dropdown list
            ApplicationIconCombobox.Items.Add(IconRelativePath)
        End Sub


        ''' <summary>
        ''' Adds the given filename to the project.
        ''' </summary>
        ''' <param name="IconFileName"></param>
        ''' <remarks></remarks>
        Private Function AddIconFileToProject(ByVal IconFileName As String) As ProjectItem
            'Note: we allow the project to set the Build Action property for the icon to the default (content),
            '  which causes it to get deployed.  That is the desired behavior.
            Return AddFileToProject(DTEProject.ProjectItems, IconFileName, True)
        End Function


        ''' <summary>
        ''' Allows the user to browse for an icon, and then adds it to the project and
        '''   to the given combobox.
        ''' </summary>
        ''' <param name="ApplicationIconCombobox">The combobox that displays the list of icons to choose from.</param>
        ''' <param name="ApplicationIconPictureBox">The picturebox that displays the image of the currently selected icon.</param>
        ''' <remarks></remarks>
        Protected Sub BrowseForAppIcon(ByVal ApplicationIconCombobox As ComboBox, ByVal ApplicationIconPictureBox As PictureBox)
            Dim sInitialDirectory As String
            Dim sFileName As String

            sInitialDirectory = Trim(ApplicationIconCombobox.Text)
            If sInitialDirectory = "" OrElse IconEntryIsSpecial(sInitialDirectory) Then
                sFileName = ""
                sInitialDirectory = ""
            Else
                sFileName = System.IO.Path.GetFileName(sInitialDirectory)
                sInitialDirectory = System.IO.Path.GetDirectoryName(sInitialDirectory)
            End If

            Dim fileNames As ArrayList = Utils.GetFilesViaBrowse(ServiceProvider, Me.Handle, sInitialDirectory, SR.GetString(SR.PPG_AddExistingFilesTitle), _
                        Common.CreateDialogFilter(SR.GetString(SR.PPG_AddIconFilesFilter), ".ico"), _
                        0, False, sFileName)

            If fileNames IsNot Nothing AndAlso fileNames.Count = 1 Then
                sFileName = CStr(fileNames(0))

                If System.IO.File.Exists(sFileName) Then
                    'Verify it's actually a usable .ico before adding it to the project
                    Dim ValidIcon As Boolean = False
                    Try
                        Dim Icon As New System.Drawing.Icon(sFileName)
                        ValidIcon = True
                        Icon.Dispose()
                    Catch ex As ArgumentException
                        ShowErrorMessage(SR.GetString(SR.PPG_Application_BadIcon_1Arg, sFileName))
                    Catch ex As Exception
                        Common.RethrowIfUnrecoverable(ex)
                        ShowErrorMessage(ex)
                    End Try
                    If Not ValidIcon Then
                        'Restore the previous setting (this is important because it might be the
                        '  special <browse> item in VB, etc.).
                        ApplicationIconCombobox.SelectedItem = m_LastIconImage
                        Return
                    End If

                    Dim ProjectItem As EnvDTE.ProjectItem = Nothing
                    Try
                        ProjectItem = AddIconFileToProject(sFileName)
                    Catch ex As Exception
                        Common.RethrowIfUnrecoverable(ex)
                        ShowErrorMessage(SR.GetString(SR.PPG_Application_CantAddIcon), ex)
                    End Try

                    If ProjectItem Is Nothing Then
                        'Could not copy
                        ApplicationIconCombobox.SelectedItem = m_LastIconImage
                    Else
                        Dim sRelativePath As String = GetProjectRelativeFilePath(ProjectItem.FileNames(1))

                        'Find the item in the list and select it
                        ApplicationIconCombobox.SelectedIndex = -1
                        For Index As Integer = 0 To ApplicationIconCombobox.Items.Count - 1
                            Dim ItemPath As String = DirectCast(ApplicationIconCombobox.Items.Item(Index), String)
                            If ItemPath.Equals(sRelativePath, StringComparison.OrdinalIgnoreCase) Then
                                ApplicationIconCombobox.SelectedIndex = Index
                                Exit For
                            End If
                        Next
                        If ApplicationIconCombobox.SelectedIndex = -1 Then
                            'Icon is not in the list, so add it to the list
                            'Now get the new path of copied file
                            sRelativePath = GetProjectRelativeFilePath(ProjectItem.FileNames(1))
                            AddIconEntryToCombobox(ApplicationIconCombobox, sRelativePath)
                            ApplicationIconCombobox.SelectedItem = sRelativePath
                        End If
                    End If
                    UpdateIconImage(ApplicationIconCombobox, ApplicationIconPictureBox, True)
                    SetDirty(VsProjPropId.VBPROJPROPID_ApplicationIcon, True)
                Else
                    Debug.Fail("File returned from browse dialog doesn't exist")
                End If
            Else
                'Restore the previous setting
                ApplicationIconCombobox.SelectedItem = m_LastIconImage
            End If
        End Sub


        ''' <summary>
        ''' Update the image displayed for the currently-selected application icon
        ''' </summary>
        ''' <param name="ApplicationIconCombobox">The combobox that displays the list of icons to choose from.</param>
        ''' <param name="ApplicationIconPictureBox">The picturebox that displays the image of the currently selected icon.</param>
        ''' <remarks></remarks>
        Protected Sub UpdateIconImage(ByVal ApplicationIconCombobox As ComboBox, ByVal ApplicationIconPictureBox As PictureBox, ByVal AddToProject As Boolean)
            If ApplicationIconCombobox.Enabled Then
                Dim ApplicationIconText As String = Trim(CStr(ApplicationIconCombobox.SelectedItem))

                If ApplicationIconText = "" Then
                    'If combobox item not selected, just get user typed text
                    ApplicationIconText = Trim(ApplicationIconCombobox.Text)
                End If

                If Not SetIconImagePath(ApplicationIconText, ApplicationIconCombobox, ApplicationIconPictureBox, AddToProject) Then
                    'Path did not exist, revert to previous
                    If Not SetIconImagePath(m_LastIconImage, ApplicationIconCombobox, ApplicationIconPictureBox, AddToProject) Then
                        'Still failed
                        If Not SetIconImagePath(m_DefaultIconText, ApplicationIconCombobox, ApplicationIconPictureBox, AddToProject) Then
                            Debug.Fail("should never happen")
                        End If
                    End If
                End If
            Else
                'The icon combobox is disabled (probably wrong project type or output type), so 
                '  just show a blank entry in the icon picturebox
                ApplicationIconPictureBox.Image = Nothing

                'vswhidbey 484471: clear the last icon cache so the next call
                'to SetIconImagePath will not exit early if the path is equal 
                m_LastIconImage = ""
            End If
        End Sub


        Private Function SetIconImagePath(ByVal path As String, ByVal ApplicationIconCombobox As ComboBox, ByVal ApplicationIconPictureBox As PictureBox, ByVal AddToProject As Boolean) As Boolean
            If path IsNot Nothing AndAlso path.Equals(m_LastIconImage, StringComparison.Ordinal) Then
                'PERF: Nothing to do if nothing has changed
                Return True
            End If

            'Check for a valid path
            If IconEntryIsSpecial(path) OrElse path = "" Then
                m_LastIconImage = m_DefaultIconText
                ApplicationIconPictureBox.Image = IconToImage(m_DefaultIcon, ApplicationIconPictureBox.ClientSize)
                ApplicationIconCombobox.SelectedItem = m_DefaultIconText
                Return True
            End If

            ' Verify all the characters in the path are valid 
            If path.IndexOfAny(IO.Path.GetInvalidPathChars()) >= 0 Then
                ShowErrorMessage(SR.GetString(SR.PPG_Application_CantAddIcon))
                Return False
            End If

            If Not IO.Path.IsPathRooted(path) Then
                path = IO.Path.Combine(GetProjectPath(), path)
            End If

            If System.IO.File.Exists(path) Then
                'System.Drawing.Image will hold on to any file that we give it, so that it can
                '  be lazy about getting the bits out of it.  This means the file will be locked, 
                '  which we don't want.  Make a copy of the file as a memory stream and use that to
                '  create the Image.
                Try
                    Dim IconContents As Byte() = IO.File.ReadAllBytes(path)
                    Dim IconStream As New IO.MemoryStream(IconContents, 0, IconContents.Length)
                    ApplicationIconPictureBox.Image = IconToImage(New Icon(IconStream), ApplicationIconPictureBox.ClientSize)
                Catch ex As Exception
                    Common.RethrowIfUnrecoverable(ex, True)

                    'This could mean a bad icon file, I/O problems, etc.  At any rate, it doesn't make sense to
                    '  display an error message (doesn't necessarily mean the user just selected it, it might have
                    '  been in the project file), so we'll just show a blank image
                    ApplicationIconPictureBox.Image = New Bitmap(1, 1)
                End Try

                Dim sRelativePath As String = GetProjectRelativeFilePath(path)

                'Find the item in the list and select it

                If ApplicationIconCombobox.Items.Count = 0 Then
                    'The combobox has not been filled with any entries yet (there should at least be the <default> entry).
                    '  We must do that before we continue, but don't bother adding icons from the project, that would 
                    '  take too long
                    PopulateIconList(False, ApplicationIconCombobox, sRelativePath)
                End If
                For Index As Integer = 0 To ApplicationIconCombobox.Items.Count - 1
                    Dim ItemPath As String = DirectCast(ApplicationIconCombobox.Items.Item(Index), String)
                    If ItemPath.Equals(sRelativePath, StringComparison.OrdinalIgnoreCase) Then
                        ApplicationIconCombobox.SelectedIndex = Index
                        Exit For
                    End If
                Next
                If ApplicationIconCombobox.SelectedIndex = -1 Then
                    'Icon is not in the project, so add it, if requested
                    If AddToProject Then
                        Dim ProjectItem As EnvDTE.ProjectItem = Nothing
                        Try
                            ProjectItem = AddIconFileToProject(path)
                        Catch ex As Exception
                            Common.RethrowIfUnrecoverable(ex)
                            ShowErrorMessage(SR.GetString(SR.PPG_Application_CantAddIcon), ex)
                            Return False
                        End Try

                        'Now get the new path of copied file
                        sRelativePath = GetProjectRelativeFilePath(ProjectItem.FileNames(1))

                    End If

                    'Add it to the combobox
                    ApplicationIconCombobox.Items.Add(sRelativePath)
                    ApplicationIconCombobox.SelectedItem = sRelativePath
                End If

                m_LastIconImage = sRelativePath
                Return True
            End If
            Return False

        End Function


        ''' <summary>
        ''' Converts an icon to a bitmap of the correct size
        ''' </summary>
        ''' <param name="Icon">The icon to convert</param>
        ''' <param name="PictureBoxSize">The size into which the image needs to fit</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IconToImage(ByVal Icon As Icon, ByVal PictureBoxSize As Size) As Image
            ' We want to fit the icon into the picture box, but we'll keep it square
            ' so as not to distort it.
            Dim width As Integer = PictureBoxSize.Width
            Dim height As Integer = PictureBoxSize.Height
            Dim side As Integer = Math.Min(width, height)
            Dim size As Size = New Size(side, side)

            ' Try to get a version of the icon matching the size. Note
            ' that this will find the closest match if an exact match
            ' can't be found.
            Using sizedIcon As Icon = New Icon(Icon, size)
                If sizedIcon.Size = size Then
                    Return sizedIcon.ToBitmap()
                End If

                ' If our icon is not the correct size, resize it here
                Using iconBitmap As Image = sizedIcon.ToBitmap()
                    Dim bitmap As Bitmap = New Bitmap(size.Width, size.Height, iconBitmap.PixelFormat)
                    Using bitmapGraphics As Graphics = Graphics.FromImage(bitmap)
                        bitmapGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic
                        bitmapGraphics.Clear(Color.Transparent)

                        bitmapGraphics.DrawImage(iconBitmap, 0, 0, size.Width, size.Height)
                    End Using

                    Return bitmap
                End Using
            End Using
        End Function


        ''' <summary>
        ''' Given a ProjectItem, adds that item to the icon combobox if it's an .ico file.  Also
        '''   adds any .ico files underneath it recursively.
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <remarks></remarks>
        Protected Sub AddIconsFromProjectItem(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal ApplicationIconCombobox As ComboBox)
            For Index As Short = 1 To ProjectItem.FileCount
                Dim FileName As String = ProjectItem.FileNames(Index)
                Dim ext As String = System.IO.Path.GetExtension(FileName)
                If ext.Equals(".ico", StringComparison.OrdinalIgnoreCase) Then
                    ApplicationIconCombobox.Items.Add(GetProjectRelativeFilePath(FileName))
                End If

                'Recurse into folders (we definitely want stuff in the Resources
                '  folder, for instance)
                For Each Child As EnvDTE.ProjectItem In ProjectItem.ProjectItems
                    AddIconsFromProjectItem(Child, ApplicationIconCombobox)
                Next
            Next
        End Sub


        ''' <summary>
        ''' Returns true if the text is the special "Browse" text for the icon combobox
        ''' </summary>
        ''' <param name="EntryText"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function IconEntryIsBrowse(ByVal EntryText As String) As Boolean
            Return False
        End Function

        ''' <summary>
        ''' Returns true if the text is the special "(Default Icon)" text for the icon combobox
        ''' </summary>
        ''' <param name="EntryText"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overridable Function IconEntryIsDefault(ByVal EntryText As String) As Boolean
            Return EntryText IsNot Nothing AndAlso EntryText.Equals(m_DefaultIconText, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Returns true if the text is a special value (like Browse or Default)
        ''' </summary>
        ''' <param name="EntryText"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function IconEntryIsSpecial(ByVal EntryText As String) As Boolean
            Return IconEntryIsBrowse(EntryText) OrElse IconEntryIsDefault(EntryText)
        End Function

#End Region

#Region "Application Manifest Support"

        Protected Function ApplicationManifestSupported() As Boolean
            Return Not GetPropertyControlData(VsProjPropId90.VBPROJPROPID_ApplicationManifest).IsMissing
        End Function

        ''' <summary>
        ''' Populates the given application manifest combobox with appropriate entries
        ''' </summary>
        ''' <param name="FindManifestInProject">If False, only the standard items are added (this is faster
        '''   and so may be appropriate for page initialization).</param>
        ''' <param name="ApplicationManifestCombobox">The combobox that displays the list of manifests</param>
        ''' <param name="CurrentManifestValue">The current manifest as a relative path.</param>
        ''' <remarks>
        ''' CurrentManifestValue must be passed in because it's pulled from the control's current value, which is initially
        '''   set up by PropertyControlData), since clearing the list will clear the text value, too,
        '''   for a dropdown list.
        ''' </remarks>
        Protected Overridable Sub PopulateManifestList(ByVal FindManifestInProject As Boolean, ByVal ApplicationManifestCombobox As ComboBox, ByVal CurrentManifestValue As String)
            Dim fInsideInitPrevious As Boolean = m_fInsideInit
            m_fInsideInit = True
            Try
                ApplicationManifestCombobox.Items.Clear()
                ApplicationManifestCombobox.Items.Add(Me.m_DefaultManifestText)
                ApplicationManifestCombobox.Items.Add(Me.m_NoManifestText)
                If FindManifestInProject Then
                    For Each ProjectItem As EnvDTE.ProjectItem In DTEProject.ProjectItems
                        AddManifestsFromProjectItem(ProjectItem, ApplicationManifestCombobox)
                    Next
                End If

                If String.IsNullOrEmpty(CurrentManifestValue) Then
                    ApplicationManifestCombobox.SelectedIndex = 0
                ElseIf String.Equals(CurrentManifestValue, prjApplicationManifestValues.prjApplicationManifest_Default, StringComparison.OrdinalIgnoreCase) Then
                    ApplicationManifestCombobox.SelectedIndex = 0
                ElseIf String.Equals(CurrentManifestValue, prjApplicationManifestValues.prjApplicationManifest_NoManifest, StringComparison.OrdinalIgnoreCase) Then
                    ApplicationManifestCombobox.SelectedIndex = 1
                Else
                    'Can't simply set SelectedItem, because it uses a case-sensitive comparison
                    For Each Item As String In ApplicationManifestCombobox.Items
                        ' Compare using oridinal ignore case as they are file paths.
                        If Item.Equals(CurrentManifestValue, StringComparison.OrdinalIgnoreCase) Then
                            ApplicationManifestCombobox.SelectedItem = Item
                            Exit For
                        End If
                    Next
                    If ApplicationManifestCombobox.SelectedItem Is Nothing Then
                        'CurrentIcon is not in the last - add it after the default manifest
                        Debug.Assert(ApplicationManifestCombobox.Items.Count >= 2, "Where are the default manifest values in the list?")
                        ApplicationManifestCombobox.Items.Insert(2, CurrentManifestValue)
                        ApplicationManifestCombobox.SelectedItem = CurrentManifestValue
                    End If
                End If
            Finally
                m_fInsideInit = fInsideInitPrevious
            End Try
        End Sub


        ''' <summary>
        ''' Given a ProjectItem, adds that item to the manifest combobox if it's an .manifest file.  Also
        '''   adds any .manifest files underneath it recursively.
        ''' </summary>
        ''' <param name="ProjectItem"></param>
        ''' <remarks></remarks>
        Protected Sub AddManifestsFromProjectItem(ByVal ProjectItem As EnvDTE.ProjectItem, ByVal ApplicationManifestCombobox As ComboBox)
            For Index As Short = 1 To ProjectItem.FileCount
                Dim FileName As String = ProjectItem.FileNames(Index)
                Dim ext As String = System.IO.Path.GetExtension(FileName)
                If ext.Equals(".manifest", StringComparison.OrdinalIgnoreCase) Then
                    ApplicationManifestCombobox.Items.Add(GetProjectRelativeFilePath(FileName))
                End If

                'Recurse into folders (we definitely want stuff in the Resources
                '  folder, for instance)
                For Each Child As EnvDTE.ProjectItem In ProjectItem.ProjectItems
                    AddManifestsFromProjectItem(Child, ApplicationManifestCombobox)
                Next
            Next
        End Sub

#End Region

        Protected Function ApplicationManifestEntryIsDefault(ByVal text As String) As Boolean
            Return text IsNot Nothing AndAlso text.Equals(m_DefaultManifestText, StringComparison.OrdinalIgnoreCase)
        End Function

    End Class

End Namespace

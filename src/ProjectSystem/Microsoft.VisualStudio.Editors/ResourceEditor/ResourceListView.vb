Option Explicit On
Option Strict On
Option Compare Binary

Imports System
Imports System.Collections
Imports System.Diagnostics
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.PlatformUI

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This is a virtualized listview capable of displaying Resource items as thumbnails.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ResourceListView
        Inherits DesignerFramework.DesignerListView

#Region "Fields"

        'This is the list of resources which we are actually displaying in the listview.
        '  Since we're using the listview in virtual mode (in order to accomplish
        '  delay-load of the images from disk), we keep track of the data ourselves.
        '  The base listview notifies us when it needs data to display.
        Private m_VirtualResourceList As New ArrayList 'Of Resource

        'A cache of thumbnails for the listview items that we are displaying.  This has
        '  knowledge of our imagelist, and manages it for us.
        Private m_ThumbnailCache As ThumbnailCache

        'The imagelist which contains all thumbnails currently cached for this listview.
        '  This is managed by our thumbnail cache.  Note that a listview always gets its
        '  images to display from an imagelist.  Since we don't want to create an imagelist
        '  large enough to hold a thumbnail for every resource that might be in the resx
        '  we use a cached system.  This also helps with scalability.
        Private m_ThumbnailImageList As ImageList

        'This imagelist contains state images.  These images are shown to the left of the
        '  listview item and are used to show error glyphs.
        Private m_StateImageList As ImageList

        'The width of the selection border that we draw around thumbnails in the "Thumbnails"
        '  view.
        Private Const DefaultSelectionBorderWidth As Integer = 2

        'The width of the non-selected border around thumbnails in the "Thumbnails" view.
        Private Const DefaultBorderWidth As Integer = 1

        'The width/height of images in "Thumbnails" view (not including the borders).
        Private Const DefaultLargeImageWidthHeight As Integer = 96

        'The width/height of images in the smaller views ("icons", "details").  No border is used
        '  in these cases.
        Private Const DefaultSmallImageWidthHeight As Integer = 20

        ' The above settings scaled up for High DPI
        Private SelectionBorderWidth As Integer = DpiHelper.LogicalToDeviceUnitsX(DefaultSelectionBorderWidth)
        Private BorderWidth As Integer = DpiHelper.LogicalToDeviceUnitsX(DefaultBorderWidth)
        Private LargeImageWidthHeight As Integer = DpiHelper.LogicalToDeviceUnitsX(DefaultLargeImageWidthHeight)
        Private SmallImageWidthHeight As Integer = DpiHelper.LogicalToDeviceUnitsX(DefaultSmallImageWidthHeight)

        'The default column width for the "Name" column in "Details" view
        Private Const DefaultColumnWidthName As Integer = 150 'Includes the size of the thumbnail icon

        'The default column width for the "Filename" column in "Details" view
        Private Const DefaultColumnWidthFilename As Integer = 300

        'The default column width for the "Image Type" column in "Details" view
        Private Const DefaultColumnWidthImageType As Integer = 150

        'The default column width for the "Size" column in "Details" view
        Private Const DefaultColumnWidthSize As Integer = 60

        'The default column width for the "Comment" column in "Details" view
        Private Const DefaultColumnWidthComment As Integer = 300

        'If this is turned on, attempted retrieval of listview items will simply return
        '  a blank entry.  This is useful when the resourc editor is being disposed of.
        Private m_DisableItemRetrieval As Boolean

        'The ResourceFile used to populate this grid.
        Private m_ResourceFile As ResourceFile

        'The index to the reserved error glyph at the beginning of the imagelist.
        Private Const IMAGELIST_INDEX_ERROR As Integer = 0

        'The index to the reserved SortUp glyph at the beginning of the imagelist.
        Private Const IMAGELIST_INDEX_SORT_UP As Integer = 1

        'The index to the reserved SortDown glyph at the beginning of the imagelist.
        Private Const IMAGELIST_INDEX_SORT_DOWN As Integer = 2

        ' The index means we haven't loaded the image yet
        Private Const IMAGELIST_INDEX_NEED_LOAD As Integer = -1

        'The index into the state imagelist for the (small) error glyph
        Private Const STATEIMAGELIST_INDEX_ERROR As Integer = 0

        'If true, and label editing fails validation, no messagebox will be shown, it
        '  will just fail silently.
        Private m_CancelLabelEditIfValidationFails As Boolean

        ' Current sorter
        Private m_Sorter As DetailViewSorter

        ' Current Image cache information
        ' OnCacheVirtualItems will tell us the window of the items we need cache. We need save it in m_ImageStartIndex and m_ImageEndIndex
        ' On each Idle message, we only load one image, so we save the position in m_IdleProcessingIndex, so we can continue later...
        Private m_ImageStartIndex As Integer            ' The first image we should load at Idle time
        Private m_ImageEndIndex As Integer              ' The last image we should load
        Private m_IdleProcessingIndex As Integer        ' Current Index, sometime we update this to speed up loading images...

        ' If we need load several blocks of images, we push the early requirement in the stack, and load them later
        Private m_CacheRequirementStack As Stack

        ' If the ListView asks for the image, we know it want to paint it, and we want to load it earlier than the others.
        '  In that senario, we update m_IdleProcessingIndex to load that item first.
        '  However, we shouldn't update it if we have already updated it before handling any Idle message.
        '  The reason is the first item required is often the focused item, which we want to paint first.
        Private m_NeedLoadVisibleItem As Boolean

        ' if it is true, we have already hooked up the Idle message
        Private m_OnIdleEnabled As Boolean

        ' Detail View Column Count
        Private Const DETAIL_VIEW_COLUMN_COUNT As Integer = 5


#End Region

        ''' <summary>
        ''' The view mode for the listview.  (Starting at non-zero helps find possible bugs in
        '''    case this enum is mixed up with the base's View enum.)
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum ResourceView
            Thumbnail = 1000
            List = 1001
            Details = 1002

        End Enum



        ''' <summary>
        ''' Column in the detail view
        ''' </summary>
        ''' <remarks>need update DETAIL_VIEW_COLUMN_COUNT, when a new column is added</remarks>
        Private Enum DetailViewColumn
            Name = 0
            FileName = 1
            Type = 2
            Size = 3
            Comment = 4
        End Enum


        '======================================================================
        '= Constructors/Destructors =                                         =
        '======================================================================



        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            MyBase.New()

            MyBase.HideSelection = False
            MyBase.VirtualMode = True

            'Default view
            View = ResourceView.List

            'Allow editing the Name by clicking on it.
            Me.LabelEdit = True

            'We want to use tooltips to show error messages
            Me.ShowItemToolTips = True

            Common.DTEUtils.ApplyListViewThemeStyles(Me.Handle)
        End Sub


        ''' <summary>
        ''' Overrides Dispose to do clean-up of managed resources
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
            StopIdleMessage()

            'Disable item retrieval so that if we get asked for more information
            '  after we've already started disposing, we won't try to fetch any
            '  real data.
            m_DisableItemRetrieval = True

            If Disposing Then
                m_ThumbnailCache = Nothing

                If m_ThumbnailImageList IsNot Nothing Then
                    m_ThumbnailImageList.Dispose()
                    m_ThumbnailImageList = Nothing
                End If

                If m_StateImageList IsNot Nothing Then
                    m_StateImageList.Dispose()
                    m_StateImageList = Nothing
                End If
            End If

            MyBase.Dispose(Disposing)
        End Sub




        '======================================================================
        '= Properties =                                                       =
        '======================================================================



        ''' <summary>
        ''' The current view of the listview (in ResourceView terms - shadows the
        '''   base ListView's View property)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Shadows Property View() As ResourceView
            Get
                Select Case MyBase.View
                    Case System.Windows.Forms.View.Details
                        Return ResourceView.Details
                    Case System.Windows.Forms.View.LargeIcon
                        Return ResourceView.Thumbnail
                    Case System.Windows.Forms.View.List
                        Return ResourceView.List
                    Case Else
                        Debug.Fail("MyBase.View should not have been any other value")
                End Select
            End Get
            Set(ByVal Value As ResourceView)
                Select Case Value
                    Case ResourceView.Details
                        InitializeColumns()
                        MyBase.View = System.Windows.Forms.View.Details
                    Case ResourceView.List
                        MyBase.View = System.Windows.Forms.View.List
                    Case ResourceView.Thumbnail
                        MyBase.View = System.Windows.Forms.View.LargeIcon
                    Case Else
                        Debug.Fail("Unrecognized view")
                End Select
            End Set
        End Property


        ''' <summary>
        ''' If this is turned on, attempted retrieval of listview items will simply return
        '''   a blank entry.  This is useful when the resource editor is being disposed of.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public Property DisableItemRetrieval() As Boolean
            Get
                Return m_DisableItemRetrieval
            End Get
            Set(ByVal Value As Boolean)
                m_DisableItemRetrieval = Value
            End Set
        End Property

        ''' <summary>
        ''' Gets the Parent of this ResourceListView control, which must be an instance of ResourceEditorView.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property ParentView() As ResourceEditorView
            Get
                If TypeOf Me.Parent Is ResourceEditorView Then
                    Return DirectCast(Me.Parent, ResourceEditorView)
                Else
                    Debug.Fail("Not parented to a ResourceEditorView?")
                    Throw New System.InvalidOperationException()
                End If
            End Get
        End Property


        ''' <summary>
        ''' Gets the ResourceFile that was used to populate this listview.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Can be called only after population</remarks>
        Private ReadOnly Property ResourceFile() As ResourceFile
            Get
                Debug.Assert(m_ResourceFile IsNot Nothing, "Has Populate not yet been called?  m_ResourceFile is nothing")
                Return m_ResourceFile
            End Get
        End Property


        '======================================================================
        '= Methods =                                                          =
        '======================================================================



#Region "UI-Related Features"
        Private m_ColumnInitialized As Boolean

        ''' <summary>
        ''' Initialize our columns for details view
        ''' </summary>
        ''' <remarks>
        ''' Initializes all columns.  We should not re-initialize columns (therefore changing
        '''   the widths the user has set).
        ''' </remarks>
        Private Sub InitializeColumns()

            '*****
            '***** WARNING: This order and number of columns *MUST* match the order in DetailViewColumn
            '*****

            If Not m_ColumnInitialized Then

                Columns.Add(SR.GetString(SR.RSE_DetailsCol_Name), DpiHelper.LogicalToDeviceUnitsX(DefaultColumnWidthName), HorizontalAlignment.Left)
                Columns.Add(SR.GetString(SR.RSE_DetailsCol_Filename), DpiHelper.LogicalToDeviceUnitsX(DefaultColumnWidthFilename), HorizontalAlignment.Left)
                Columns.Add(SR.GetString(SR.RSE_DetailsCol_ImageType), DpiHelper.LogicalToDeviceUnitsX(DefaultColumnWidthImageType), HorizontalAlignment.Left)
                Columns.Add(SR.GetString(SR.RSE_DetailsCol_Size), DpiHelper.LogicalToDeviceUnitsX(DefaultColumnWidthSize), HorizontalAlignment.Left)
                Columns.Add(SR.GetString(SR.RSE_DetailsCol_Comment), DpiHelper.LogicalToDeviceUnitsX(DefaultColumnWidthComment), HorizontalAlignment.Left)

                m_ColumnInitialized = True
            End If

            '*****
            '***** WARNING: This order and number of columns *MUST* match the order in DetailViewColumn
            '*****

        End Sub


        ''' <summary>
        ''' Clear all entries from the listview.
        ''' </summary>
        ''' <remarks></remarks>
        Public Overloads Sub Clear()
            StopIdleMessage()

            MyBase.VirtualListSize = 0
            m_ThumbnailCache = Nothing
            m_VirtualResourceList.Clear()

            '... Dispose old imagelist, if any
            If m_ThumbnailImageList IsNot Nothing Then
                m_ThumbnailImageList.Dispose()
                m_ThumbnailImageList = Nothing
            End If
            If m_StateImageList IsNot Nothing Then
                m_StateImageList.Dispose()
                m_StateImageList = Nothing
            End If
        End Sub


        ''' <summary>
        ''' Populates the listview from a ResourceFile, adding all resources
        '''   in it which are in the given category.
        ''' </summary>
        ''' <param name="ResourceFile">The resources from which the displayed resources will be pulled.</param>
        ''' <param name="CategoryToFilterOn">The category of resources to display.</param>
        ''' <remarks></remarks>
        Public Sub Populate(ByVal ResourceFile As ResourceFile, ByVal CategoryToFilterOn As Category)
            MyBase.VirtualListSize = 0
            m_ResourceFile = ResourceFile

            'Create and set up a new imagelist
            'The images and their sizes used below are already scaled up for High DPI, so we don't need to scale up the imagelist as well
            m_ThumbnailImageList = New ImageList

            '... Set the size of images we are working with
            'Also, add a single error glyph to the beginning of the imagelist.  This image will be "reserved", and thus not
            '  play into the recycling of thumbnails.  We will use it only under dire need, because every call to
            '  get the thumbnail image of that resource will try to load the value again.
            If View = ResourceView.Thumbnail Then
                m_ThumbnailImageList.ImageSize = New Size(LargeImageWidthHeight + BorderWidth + SelectionBorderWidth, LargeImageWidthHeight + BorderWidth + SelectionBorderWidth)
                m_ThumbnailImageList.Images.Add(Nothing, ParentView.CachedResources.ErrorGlyphLarge)
            Else
                m_ThumbnailImageList.ImageSize = New Size(SmallImageWidthHeight, SmallImageWidthHeight)
                m_ThumbnailImageList.Images.Add(Nothing, ParentView.CachedResources.ErrorGlyphSmall)
            End If

            ' Those images are used to indicate which column is being sorted
            ' We use the same system color as the ListView in the window explorer, so it will work in high contrast mode
            m_ThumbnailImageList.Images.Add("SortUp", MapBitmapColor(ParentView.CachedResources.SortUpGlyph, Color.Black,
                                                                     Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_GRAYTEXT, SystemColors.GrayText, UseVSTheme:=False)))
            m_ThumbnailImageList.Images.Add("SortDown", MapBitmapColor(ParentView.CachedResources.SortDownGlyph, Color.Black,
                                                                     Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_GRAYTEXT, SystemColors.GrayText, UseVSTheme:=False)))

            'We need to set the transparent color of the ImageList, or the selection 
            '  rectangle trick used in CreateThumbnail will not work.
            m_ThumbnailImageList.TransparentColor = Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_WINDOW, SystemColors.Window, UseVSTheme:=False)

            'Color depth
            m_ThumbnailImageList.ColorDepth = ColorDepth.Depth16Bit

            'New thumbnail cache based on this imagelist
            m_ThumbnailCache = New ThumbnailCache(m_ThumbnailImageList)

            'Go through all resources, and pick out the ones which belong to the specified category, 
            '  and add them to our virtual list
            Dim Categories As CategoryCollection = ResourceFile.RootComponent.RootDesigner.GetView().Categories
            For Each Entry As DictionaryEntry In ResourceFile
                Dim Resource As Resource = DirectCast(Entry.Value, Resource)
                If Resource.GetCategory(Categories) Is CategoryToFilterOn Then
                    Debug.Assert(Not Resource.ResourceTypeEditor.DisplayInStringTable,
                            "Why are we trying to display this type of resource in a listview?")
                    m_VirtualResourceList.Add(Resource)
                End If
            Next

            ' restore sort order
            m_Sorter = TryCast(CategoryToFilterOn.Sorter, DetailViewSorter)
            If m_Sorter Is Nothing Then
                m_Sorter = New DetailViewSorter(0, False)
                CategoryToFilterOn.Sorter = m_Sorter
            End If

            ' Clear old column header sort indicator
            For columnIndex As Integer = 0 To DETAIL_VIEW_COLUMN_COUNT - 1
                ClearColumnSortImage(columnIndex)
            Next

            ' Set column header sort indicator
            SetColumnSortImage(m_Sorter.ColumnIndex, m_Sorter.InReverseOrder)

            'Sort the list alphabetically to start with (when the user adds new entries later, we'll
            '  add them to the list at the end)
            m_VirtualResourceList.Sort(m_Sorter)

            'Set the base listview's correct imagelist property to point to the imagelist we have created
            If View = ResourceView.Thumbnail Then
                MyBase.SmallImageList = Nothing
                MyBase.LargeImageList = m_ThumbnailImageList
            Else
                MyBase.LargeImageList = Nothing
                MyBase.SmallImageList = m_ThumbnailImageList
            End If

            'Set up the state imagelist (for displaying error glyphs next to the listview items)
            m_StateImageList = New ImageList()
            m_StateImageList.ColorDepth = ColorDepth.Depth8Bit
            m_StateImageList.ImageSize = ParentView.CachedResources.ErrorGlyphState.Size
            m_StateImageList.Images.Add(ParentView.CachedResources.ErrorGlyphState)
            MyBase.StateImageList = m_StateImageList

            'Finally, let the base listview know how many resources it has, so it can start
            '  querying us for info on them.
            MyBase.VirtualListSize = m_VirtualResourceList.Count

            ParentView.RootDesigner.InvalidateFindLoop(False)
        End Sub


        ''' <summary>
        ''' Invalidates a single listitem corresponding to a given Resource.  This area of the listview will be
        '''   refreshed in a later paint message.
        ''' </summary>
        ''' <param name="Resource">The Resource which should be repainted.</param>
        ''' <param name="InvalidateThumbnail">If True, also invalidates the Resource's thumbnail image and other cached
        '''   info so that it is re-created on the next redraw.</param>
        ''' <remarks></remarks>
        Public Sub InvalidateResource(ByVal Resource As Resource, Optional ByVal InvalidateThumbnail As Boolean = False)
            Debug.Assert(Resource IsNot Nothing)
            If Not m_VirtualResourceList.Contains(Resource) Then
                Exit Sub
            End If

            If InvalidateThumbnail Then
                m_ThumbnailCache.InvalidateThumbnail(Resource)
            End If

            Dim ItemIndex As Integer = IndexOf(Resource)
            Dim itemRectangle As Rectangle = MyBase.GetItemRect(ItemIndex)
            If itemRectangle.IntersectsWith(Me.ClientRectangle) Then
                'Reload the image...
                RequireCacheImage(ItemIndex, ItemIndex)
                Invalidate(itemRectangle)
            End If
        End Sub


        ''' <summary>
        ''' Occurs when the user tries to go into label edit mode to change the resource name
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnBeforeLabelEdit(ByVal e As System.Windows.Forms.LabelEditEventArgs)
            If ParentView.ReadOnlyMode Then
                e.CancelEdit = True
                Return
            End If

            Try
                'See if we're able to check out the file before allowing the user to try to rename the resource.
                ParentView.RootDesigner.DesignerLoader.ManualCheckOut()
                ParentView.OnItemBeginEdit()
            Catch ex As Exception
                e.CancelEdit = True
                ParentView.DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Occurs when the label for an item is edited by the user.
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks>
        ''' We use this event to detect that the user has changed the name of a Resource by editing the label
        '''   on a listview item.
        ''' </remarks>
        Protected Overrides Sub OnAfterLabelEdit(ByVal e As System.Windows.Forms.LabelEditEventArgs)
            MyBase.OnAfterLabelEdit(e)
            ParentView.OnItemEndEdit()

            Try
                Dim Resource As Resource = GetResourceFromVirtualIndex(e.Item)
                If Resource IsNot Nothing Then
                    Dim Exception As Exception = Nothing
                    Dim ParsedName As String = Nothing

                    'Weirdly enough, if the label was not changed, we get back the value of e.Label = ""
                    '  (see VSWhidbey:154137, marked by design).
                    If e.Label <> "" Then
                        'Validate the new Name
                        If Resource.ValidateName(e.Label, Resource.Name, ParsedName, Exception) Then
                            Resource.Name = ParsedName
                            ParentView.PropertyGridUpdate()
                        Else
                            'Validation failed.
                            e.CancelEdit = True
                            If Not m_CancelLabelEditIfValidationFails Then
                                ParentView.DsMsgBox(Exception)
                            End If
                        End If
                    End If
                Else
                    Debug.Fail("Couldn't find resource from index")
                End If
            Catch ex As Exception
                ParentView.DsMsgBox(ex)
            End Try
        End Sub


        ''' <summary>
        ''' Causes the listview to go into label edit mode
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub BeginLabelEdit(ByVal Resource As Resource)
            If Me.LabelEdit AndAlso Resource IsNot Nothing Then
                Dim Index As Integer = IndexOf(Resource)
                SelectResource(Index, True)

                Dim HR As New System.Runtime.InteropServices.HandleRef(Me, Handle)
                If Interop.NativeMethods.IsWindowUnicode(Handle) Then
                    Interop.NativeMethods.SendMessage(HR, Interop.win.LVM_EDITLABELW, Index, 0)
                Else
                    Interop.NativeMethods.SendMessage(HR, Interop.win.LVM_EDITLABELA, Index, 0)
                End If
            End If
        End Sub


        ''' <summary>
        ''' Commits all pending changes that the user has made in the grid.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub CommitPendingChanges()
            If ParentView.IsInEditing Then
                ' Don't do this if we are not in edit mode. The function will refresh the window...
                'If name change validation fails, we want this to fail silently
                Dim CancelLabelEditIfValidationFailsSave As Boolean = m_CancelLabelEditIfValidationFails
                m_CancelLabelEditIfValidationFails = True
                Try
                    'The user might be in the middle of editing a resource name via editing the listview item's
                    '  label.  The easiest way to force this to commit is to flip the LabelEdit property off/on.

                    LabelEdit = False
                    LabelEdit = True
                Finally
                    m_CancelLabelEditIfValidationFails = CancelLabelEditIfValidationFailsSave
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Called when the user clicks on column header
        '''   We need sort the whole list
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnColumnClick(ByVal e As ColumnClickEventArgs)
            If e.Column <> m_Sorter.ColumnIndex Then
                SortOnColumn(e.Column, False)
            Else
                SortOnColumn(e.Column, Not m_Sorter.InReverseOrder)
            End If

            MyBase.OnColumnClick(e)
        End Sub

        ''' <summary>
        '''  Restore Sorter -- used when we need restore view state
        ''' </summary>
        ''' <param name="originalSorter"></param>
        ''' <remarks></remarks>
        Friend Sub RestoreSorter(ByVal originalSorter As IComparer)
            Dim listViewSorter As DetailViewSorter = TryCast(originalSorter, DetailViewSorter)
            If listViewSorter IsNot Nothing Then
                SortOnColumn(listViewSorter.ColumnIndex, listViewSorter.InReverseOrder)
            Else
                Debug.Fail("We only support DetailViewSorter")
            End If
        End Sub

        ''' <summary>
        '''  Reorder the whole list based on one column
        ''' </summary>
        ''' <param name="columnIndex"></param>
        ''' <param name="inReverseOrder"></param>
        ''' <remarks></remarks>
        Private Sub SortOnColumn(ByVal columnIndex As Integer, ByVal inReverseOrder As Boolean)
            Dim currentResource As Resource = Nothing
            Dim selectedResources() As Resource

            Using (New Common.WaitCursor)
                BeginUpdate()
                Try

                    ' we save the information about current selection, so we can restore it later
                    selectedResources = GetSelectedResources()
                    If Me.FocusedItem IsNot Nothing Then
                        currentResource = GetResourceFromVirtualIndex(Me.FocusedItem.Index)
                        Me.FocusedItem = Nothing
                    ElseIf selectedResources IsNot Nothing AndAlso selectedResources.Length > 0 Then
                        currentResource = selectedResources(0)
                    End If

                    SelectedIndices.Clear()

                    ' clear the old indicator
                    ClearColumnSortImage(m_Sorter.ColumnIndex)

                    m_Sorter.ColumnIndex = columnIndex
                    m_Sorter.InReverseOrder = inReverseOrder

                    ' set new sort indicator
                    SetColumnSortImage(columnIndex, inReverseOrder)

                    ' Sort the virtual list...ReferenceList.Sort()
                    m_VirtualResourceList.Sort(m_Sorter)

                Finally
                    EndUpdate()
                End Try

                'NOTE: we should leave the selection change after the EndUpdate, or the window will refresh twice...
                ' Restore current position...
                If currentResource IsNot Nothing Then
                    Dim currentIndex As Integer = IndexOf(currentResource)
                    Me.FocusedItem = Items(currentIndex)
                    Me.EnsureVisible(currentIndex)
                End If

                ' Restore selection
                If selectedResources IsNot Nothing AndAlso selectedResources.Length > 0 Then
                    HighlightResources(selectedResources)
                End If
            End Using
        End Sub


        ''' <summary>
        '''  Set the column header image to indicate the column has been used to sort the whole list
        ''' </summary>
        ''' <param name="columnIndex"></param>
        ''' <param name="inReverseOrder"></param>
        ''' <remarks></remarks>
        Private Sub SetColumnSortImage(ByVal columnIndex As Integer, ByVal inReverseOrder As Boolean)
            Dim headerHandle As IntPtr
            headerHandle = Interop.NativeMethods.SendMessage(Me.Handle, Interop.win.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero)
            If headerHandle <> IntPtr.Zero Then
                ' Use Win32 API to set the image to the column header object
                Dim headItem As New Interop.HDITEM2
                With headItem
                    .mask = Interop.win.HDI_IMAGE Or Interop.win.HDI_FORMAT
                    .fmt = Interop.win.HDF_STRING Or Interop.win.HDF_IMAGE Or Interop.win.HDF_BITMAP_ON_RIGHT

                    If inReverseOrder Then
                        .iImage = IMAGELIST_INDEX_SORT_DOWN
                    Else
                        .iImage = IMAGELIST_INDEX_SORT_UP
                    End If
                End With

                Dim hdPtr As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(headItem))
                Try
                    Marshal.StructureToPtr(headItem, hdPtr, False)
                    Interop.NativeMethods.SendMessage(headerHandle, Interop.win.HDM_SETITEMW, CType(columnIndex, IntPtr), hdPtr)
                Finally
                    Marshal.FreeCoTaskMem(hdPtr)
                End Try
            End If
        End Sub

        ''' <summary>
        '''  Remove the column header image
        ''' </summary>
        ''' <param name="columnIndex"></param>
        ''' <remarks></remarks>
        Private Sub ClearColumnSortImage(ByVal columnIndex As Integer)
            Dim headerHandle As IntPtr
            headerHandle = Interop.NativeMethods.SendMessage(Me.Handle, Interop.win.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero)
            If headerHandle <> IntPtr.Zero Then
                ' Use Win32 API to remove the image to the column header object
                Dim headItem As New Interop.HDITEM2
                With headItem
                    .mask = Interop.win.HDI_FORMAT
                    .fmt = Interop.win.HDF_STRING
                End With

                Dim hdPtr As IntPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(headItem))
                Try
                    Marshal.StructureToPtr(headItem, hdPtr, False)
                    Interop.NativeMethods.SendMessage(headerHandle, Interop.win.HDM_SETITEMW, CType(columnIndex, IntPtr), hdPtr)
                Finally
                    Marshal.FreeCoTaskMem(hdPtr)
                End Try
            End If
        End Sub

        ''' <summary>
        '''  Update the column header image when the system color is changed
        ''' </summary>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnSystemColorsChanged(ByVal e As EventArgs)
            MyBase.OnSystemColorsChanged(e)

            ' Update sort indicator...
            m_ThumbnailImageList.Images.Item(IMAGELIST_INDEX_SORT_UP) = MapBitmapColor(ParentView.CachedResources.SortUpGlyph, Color.Black,
                                                                                       Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_GRAYTEXT, SystemColors.GrayText, UseVSTheme:=False))
            m_ThumbnailImageList.Images.Item(IMAGELIST_INDEX_SORT_DOWN) = MapBitmapColor(ParentView.CachedResources.SortDownGlyph, Color.Black,
                                                                                         Common.ShellUtil.GetVSColor(Shell.Interop.__VSSYSCOLOREX3.VSCOLOR_GRAYTEXT, SystemColors.GrayText, UseVSTheme:=False))

            ' Reset column header sort indicator
            If Me.View = ResourceView.Details AndAlso m_Sorter IsNot Nothing Then
                SetColumnSortImage(m_Sorter.ColumnIndex, m_Sorter.InReverseOrder)
            End If
        End Sub

#End Region


#Region "Virtual ListView Handling"

        ''' <summary>
        ''' Called as a cache hint by Windows for what it thinks it will need to retrieve soon.
        '''   We don't use it to actually go and retrieve the items yet (that's possible), but rather
        '''   we use it simply to adjust our cache size parameters for optimum performance and memory
        '''   usage.  I.e., generally it gives us the set of items which are on the page about to be
        '''   displayed, so it's likely to be our page size.
        ''' </summary>
        ''' <param name="e">Event args</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnCacheVirtualItems(ByVal e As System.Windows.Forms.CacheVirtualItemsEventArgs)
            MyBase.OnCacheVirtualItems(e)

            If m_ThumbnailCache IsNot Nothing Then
                Const AbsoluteMinimumCacheSize As Integer = 30

                'First, set up the minimum working set - the number of pictures we're expecting the listview to
                '  be working with right now.  The cache will try to expand to at least this amount before recycling
                '  any images (otherwise there would be thrashing).
                'CacheVirtualItems is not guaranteed to be accurate as to what we be requested in OnRetrieveVirtualItem,
                '  so we enforce a minimum cache size here.
                'NOTE: OnCacheVirtualItems often called with a range of items (or one item) it needs paint, (but not a whole page.)
                '  We should never shrink the cache size because of this. 
                Dim cacheSize As Integer = Math.Max(e.EndIndex - e.StartIndex + 1, AbsoluteMinimumCacheSize)
                If cacheSize > m_ThumbnailCache.MinimumSizeBeforeRecycling Then
                    m_ThumbnailCache.MinimumSizeBeforeRecycling = cacheSize

                    'Now we determine a maximum cache size.  This selection is somewhat arbitrary, but can be
                    '  fine-tuned as we get experience to give the best performance/memory balance.
                    '
                    'Using a PagesInCache notion means we'll try to keep a certain number of pages of ImageList
                    '  items in the cache before we start recycling images again.  If this doesn't work well in
                    '  practice, we could tweak the constant or the concept.
                    ' NOTE: 3 pages cache looks too consertivative, for list/detail view, the tiny icon shouldn't cost a lot memory,
                    '  and Thumbnail view doesn't show many in one page
                    Const PagesInCache As Integer = 5
                    Const PagesInCachDetailView As Integer = 20
                    If Me.View = ResourceView.Details Then
                        m_ThumbnailCache.MaximumSuggestedCacheSize = m_ThumbnailCache.MinimumSizeBeforeRecycling * PagesInCachDetailView
                    Else
                        m_ThumbnailCache.MaximumSuggestedCacheSize = m_ThumbnailCache.MinimumSizeBeforeRecycling * PagesInCache
                    End If
                End If

                RequireCacheImage(e.StartIndex, e.EndIndex)
            Else
                Debug.Fail("Thumbnail cache not yet set up?")
            End If
        End Sub

        ''' <summary>
        '''  Turn on OnIdle Message to load the image for the items from StartIndex to EndIndex
        ''' </summary>
        ''' <param name="StartIndex"></param>
        ''' <param name="EndIndex"></param>
        ''' <remarks></remarks>
        Private Sub RequireCacheImage(ByVal StartIndex As Integer, ByVal EndIndex As Integer)
            If Not m_OnIdleEnabled Then
                m_ImageStartIndex = StartIndex
                m_ImageEndIndex = EndIndex

                ' only hook up the idle message when it is necessary...
                While m_ImageStartIndex <= m_ImageEndIndex
                    Dim Resource As Resource = GetResourceFromVirtualIndex(m_ImageStartIndex)
                    If Resource IsNot Nothing AndAlso Not m_ThumbnailCache.IsThumbnailInCache(Resource) Then
                        m_IdleProcessingIndex = m_ImageStartIndex
                        AddHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayLoadImages
                        m_OnIdleEnabled = True

                        ' suspend delaying checking for performance...
                        ResourceFile.SuspendDelayingCheckingForErrors(True)
                        Return
                    End If
                    m_ImageStartIndex = m_ImageStartIndex + 1
                End While
            Else
                ' Try to merge...
                If StartIndex <= m_ImageEndIndex + 1 AndAlso EndIndex >= m_ImageStartIndex - 1 Then
                    m_ImageStartIndex = Math.Min(StartIndex, m_ImageStartIndex)
                    m_ImageEndIndex = Math.Max(EndIndex, m_ImageEndIndex)
                    If Not m_NeedLoadVisibleItem Then
                        m_IdleProcessingIndex = StartIndex
                    End If
                    Return
                End If

                ' If we can't merge to the current job, we push one to the stack.
                If m_CacheRequirementStack Is Nothing Then
                    m_CacheRequirementStack = New Stack()
                End If

                ' Push the old items into the stack, so we can process new items first...
                m_CacheRequirementStack.Push(New ImageCacheRequirement(m_ImageStartIndex, m_ImageEndIndex))

                m_ImageStartIndex = StartIndex
                m_ImageEndIndex = EndIndex
                m_IdleProcessingIndex = m_ImageStartIndex
            End If
        End Sub

        ''' <summary>
        '''  Turn off OnIdle Message
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub StopIdleMessage()
            If m_OnIdleEnabled Then
                m_CacheRequirementStack = Nothing
                RemoveHandler System.Windows.Forms.Application.Idle, AddressOf OnDelayLoadImages
                m_OnIdleEnabled = False

                ResourceFile.SuspendDelayingCheckingForErrors(False)
            End If
        End Sub

        ''' <summary>
        '''  We will try to load image on idle time.
        ''' 
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e">Event arguments</param>
        ''' <remarks></remarks>
        Private Sub OnDelayLoadImages(ByVal sender As Object, ByVal e As EventArgs)
            Debug.Assert(m_ThumbnailCache IsNot Nothing)
            m_NeedLoadVisibleItem = False
            If m_ThumbnailCache IsNot Nothing Then
                Dim moreCacheRequirement As Boolean = True
                While moreCacheRequirement
                    If m_ImageStartIndex < 0 Then
                        Debug.Fail("why m_ImageStartIndex < 0")
                        m_ImageStartIndex = 0
                    End If
                    If m_ImageStartIndex < m_VirtualResourceList.Count Then
                        ' Make sure everything is under the limitation of the virtual item list...
                        If m_ImageEndIndex >= m_VirtualResourceList.Count Then
                            m_ImageEndIndex = m_VirtualResourceList.Count - 1
                        End If

                        If m_IdleProcessingIndex < m_ImageStartIndex OrElse m_IdleProcessingIndex > m_ImageEndIndex Then
                            m_IdleProcessingIndex = m_ImageStartIndex
                        End If

                        ' We could start from the middle of the range (to update FocusedItem first...)
                        Dim LoopStartIndex As Integer = m_IdleProcessingIndex
                        Dim i As Integer = m_IdleProcessingIndex
                        Dim nextItem As Integer
                        Do
                            nextItem = i + 1
                            If nextItem > m_ImageEndIndex Then
                                nextItem = m_ImageStartIndex
                            End If
                            m_IdleProcessingIndex = nextItem    ' We need keep the position where we start in the next idle message
                            ' m_IdleProcessingIndex could be updated when ListView reentry us in GetItemRect.

                            ' We skip items out of the View Port...
                            Dim itemRectangle As Rectangle = MyBase.GetItemRect(i)
                            If itemRectangle.IntersectsWith(Me.ClientRectangle) Then
                                Dim Resource As Resource = GetResourceFromVirtualIndex(i)
                                If Resource IsNot Nothing AndAlso Not m_ThumbnailCache.IsThumbnailInCache(Resource) Then
                                    If GetThumbnailIndex(Resource, False) <> IMAGELIST_INDEX_NEED_LOAD Then
                                        Invalidate(itemRectangle)
                                    End If
                                    Exit Sub
                                End If
                            End If

                            i = nextItem
                        Loop While i <> LoopStartIndex
                    End If

                    ' If we have done with the current block of images, we need handle other block in the stack...
                    moreCacheRequirement = False
                    If m_CacheRequirementStack IsNot Nothing AndAlso m_CacheRequirementStack.Count > 0 Then
                        Dim requirement As ImageCacheRequirement = DirectCast(m_CacheRequirementStack.Pop(), ImageCacheRequirement)
                        m_ImageStartIndex = requirement.StartIndex
                        m_ImageEndIndex = requirement.EndIndex
                        m_IdleProcessingIndex = m_ImageStartIndex
                        moreCacheRequirement = True
                    End If
                End While
            End If

            ' We loop and check everything from m_ImageStartIndex to m_ImageEndIndex
            ' If we haven't found anything to load, shutdown the OnIdle message
            ' (The reason we need repeat checking those we loaded early is that the cache could expire because the file was changed.)
            StopIdleMessage()
        End Sub

        ''' <summary>
        ''' Called by the base listview when it needs information about a listview item so that it can
        '''   be displayed, etc.
        ''' </summary>
        ''' <param name="e">Retrieval arguments</param>
        ''' <remarks></remarks>
        Protected Overrides Sub OnRetrieveVirtualItem(ByVal e As System.Windows.Forms.RetrieveVirtualItemEventArgs)
            MyBase.OnRetrieveVirtualItem(e)

            If ResourceFile Is Nothing Then
                Debug.Fail("No resource file")
                Exit Sub
            End If

            If DisableItemRetrieval Then
                'Sometimes we get called to retrieve an item after we've already started disposing.  No sense in trying to support that
                '  (there is a bug in on this, but it's good defensive programming anyway).
                e.Item = New ListViewItem
                Exit Sub
            End If

            If m_ThumbnailCache Is Nothing OrElse m_ThumbnailImageList Is Nothing OrElse e.ItemIndex >= m_VirtualResourceList.Count Then
                Debug.Fail("RetrieveVirtualItem: shouldn't be in here right now")
                e.Item = New ListViewItem 'defensive
                Exit Sub
            End If

            'Fetch the resource at the specified index
            Dim Resource As Resource = GetResourceFromVirtualIndex(e.ItemIndex)
            If Resource Is Nothing Then
                Debug.Fail("Resource found was Nothing!")
                Return 'defensive
            End If

            Dim DelayLoadingValue As Boolean = False

            'Look up the resource in the thumbnail cache.  This will create a new thumbnail if it's
            '  not already in the cache, and it returns to us the index in the imagelist for the
            '  thumbnail.
            Dim ImageListIndex As Integer = GetThumbnailIndex(Resource, True)

            If ImageListIndex = IMAGELIST_INDEX_NEED_LOAD Then
                ' Delay load the image...
                DelayLoadingValue = True
                If e.ItemIndex >= m_ImageStartIndex AndAlso e.ItemIndex <= m_ImageEndIndex Then
                    If Not m_NeedLoadVisibleItem Then
                        m_IdleProcessingIndex = e.ItemIndex
                        m_NeedLoadVisibleItem = True
                    End If
                Else
                    RequireCacheImage(e.ItemIndex, e.ItemIndex)
                End If
            End If

            'Create the base ListViewItem with the name and image index
            e.Item = New ListViewItem(Resource.Name, ImageListIndex)

            'Fill in any error information, if this resource has task list items
            e.Item.ToolTipText = ResourceFile.GetResourceTaskMessages(Resource)
            If ResourceFile.ResourceHasTasks(Resource) Then
                'This resource has some task list items.  Need to set its state to
                '  show the error glyph.
                e.Item.StateImageIndex = STATEIMAGELIST_INDEX_ERROR
            End If

            'We also need to fill in the sub items (the additional columns in a details view)
            If Me.View = ResourceView.Details Then


                Dim LinkFileName As String
                If Resource.IsLink Then
                    LinkFileName = Resource.RelativeLinkPathAndFileName
                Else
                    LinkFileName = ""
                End If

                'The order of items here corresponds to the order of the columns we established in InitializeUI(),
                '  except that we don't have to worry about the first column, because that corresponds to the main
                '  ListViewItem itself.
                'AddRange ignores any Nothing values, so we have to convert all Nothing values to 
                '  empty string.
                e.Item.SubItems.AddRange(New String() {
                    GetDetailViewColumn(Resource, 1, DelayLoadingValue),
                    GetDetailViewColumn(Resource, 2, DelayLoadingValue),
                    GetDetailViewColumn(Resource, 3, DelayLoadingValue),
                    GetDetailViewColumn(Resource, 4, DelayLoadingValue)})

                '*****
                '***** WARNING: The number of columns *MUST* match the code in InitializeColumns
                '*****

            End If

        End Sub

        ''' <summary>
        ''' Get value of a column of a Resource Item in the Detail View
        ''' </summary>
        ''' <param name="Resource">The Resource, of which the column value we want</param>
        ''' <param name="ColumnIndex">Column Index, start from 0</param>
        ''' <param name="OnlyCachedValue">if it is true, we won't do slow operations, like loading external files to get the value</param>
        ''' <returns>String Value of the column</returns>
        ''' <remarks>We only use OnlyCachedValue when we will load the real value on the Idle time</remarks>
        Friend Shared Function GetDetailViewColumn(ByVal Resource As Resource, ByVal ColumnIndex As Integer, Optional ByVal OnlyCachedValue As Boolean = False) As String
            '*****
            '***** WARNING: The number of columns *MUST* match the code in InitializeColumns
            '*****
            Select Case ColumnIndex
                Case DetailViewColumn.Name
                    Return Resource.Name
                Case DetailViewColumn.FileName
                    Dim LinkFileName As String
                    If Resource.IsLink Then
                        LinkFileName = Resource.RelativeLinkPathAndFileName
                    Else
                        LinkFileName = String.Empty
                    End If
                    Return NonNothingString(LinkFileName)
                Case DetailViewColumn.Type
                    If OnlyCachedValue Then
                        Return String.Empty
                    End If
                    Return NonNothingString(Resource.FriendlyTypeDescription)
                Case DetailViewColumn.Size
                    If OnlyCachedValue Then
                        Return String.Empty
                    End If
                    Return NonNothingString(Resource.FriendlySize)
                Case DetailViewColumn.Comment
                    Return NonNothingString(Resource.Comment)
                Case Else
                    Debug.Fail("MyBase.View should not have been any other value")
            End Select
            Return String.Empty
        End Function



        ''' <summary>
        ''' Given a particular Resource, creates or finds a thumbnail for it in the thumbnail cache,
        '''   and returns an index into the imagelist for that thumbnail.
        ''' </summary>
        ''' <param name="Resource">The Resource to look up/create a thumbnail for</param>
        ''' <param name="AllowDelayLoading">If it is true, we won't load image, and leave it to the idle time</param>
        ''' <returns>The index into the imagelist for the resource's thumbnail</returns>
        ''' <remarks></remarks>
        Private Function GetThumbnailIndex(ByVal Resource As Resource, ByVal AllowDelayLoading As Boolean) As Integer
            'Verify that the error glyphs aren't too big (we don't expand them, so being too small
            '  is okay)
            Debug.Assert(ParentView.CachedResources.ErrorGlyphLarge.Size.Width <= LargeImageWidthHeight _
                AndAlso ParentView.CachedResources.ErrorGlyphLarge.Size.Height <= LargeImageWidthHeight,
                "Large error glyph is too big")
            Debug.Assert(ParentView.CachedResources.ErrorGlyphSmall.Size.Width <= SmallImageWidthHeight _
                AndAlso ParentView.CachedResources.ErrorGlyphSmall.Size.Height <= SmallImageWidthHeight,
                "Small error glyph is too big")

            'Do we already have a thumbnail cached for this object (and is it stil valid)?
            Dim Found As Boolean
            Dim Index As Integer
            m_ThumbnailCache.GetCachedImageListIndex(Resource, Found, Index)
            If Found Then
                'Yes - return the cached index
                Return Index
            End If

            'No thumbnail is in the cache.  We'll have to create one and add it.
            'Get the source image from which we create a thumbnail.  This will either be the actual
            '  (or copied) image from the resource, or it will be the image of an error glpyh.
            Dim ThumbnailSourceImage As Image
            Dim IsSharedImage As Boolean = Resource.ResourceTypeEditor.IsImageForThumbnailShared
            If Not IsSharedImage AndAlso AllowDelayLoading Then
                Return IMAGELIST_INDEX_NEED_LOAD
            Else
                Try
                    ThumbnailSourceImage = Resource.ResourceTypeEditor.GetImageForThumbnail(Resource, Me.BackColor)
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)

                    'For some reason, we could not get a value for this resource.  So, we use an error glyph for the
                    '  source of the thumbnail image instead.  Note that an alternative would be to have a single
                    '  thumbnail in the thumbnail cache representing an error be used for all error cases.  However,
                    '  it is better to have an actual entry in the thumbnail cache for each resource.  That way, if
                    '  there is an error trying to load the resource file from disk, for instance, we will only try
                    '  once, that first time (and the next time the error glyph is in the image cache for that entry,
                    '  so we don't try again until the file has changed and the file watcher lets us know).  Otherwise,
                    '  we would keep trying on every cache hit (happens often with our virtual listview).
                    If View = ResourceView.Thumbnail Then
                        ThumbnailSourceImage = ParentView.CachedResources.ErrorGlyphLarge
                    Else
                        ThumbnailSourceImage = ParentView.CachedResources.ErrorGlyphSmall
                    End If
                End Try
            End If

            If IsSharedImage Then
                ' Try to reuse the image in the list first.
                m_ThumbnailCache.GetCachedImageListIndex(ThumbnailSourceImage, Found, Index)
                If Found Then
                    'Yes - return the cached index
                    m_ThumbnailCache.ReuseSharedImage(Resource, Index)
                    Return Index
                End If
            End If


            'Create a thumbnail of the correct size for the image.
            Dim ThumbnailSize As Size = m_ThumbnailImageList.ImageSize
            Dim Thumbnail As Bitmap

            Try
                'Currently we only draw borders for the "Thumbnails" view
                Dim DrawBorder As Boolean = (Me.View = ResourceView.Thumbnail)

                'NOTE: This is a slow operation, we should prevent to do so if it is possible...
                Thumbnail = CreateThumbnail(ThumbnailSourceImage, ThumbnailSize, DrawBorder, BorderWidth, SelectionBorderWidth, m_ThumbnailImageList.TransparentColor)
            Catch ex As Exception
                Debug.Fail("Failed creating thumbnail")
                Thumbnail = Nothing
            End Try
            Using Thumbnail
                If Thumbnail Is Nothing Then
                    'We'd prefer not to use this, but since CreateThumbnail isn't working, we have no choice.  Trouble is,
                    '  every call to GetThumbnailIndex() will retry to create the thumbnail.  But now we have no choice.
                    Return IMAGELIST_INDEX_ERROR
                ElseIf IsSharedImage Then
                    Index = m_ThumbnailCache.Add(ThumbnailSourceImage, Thumbnail, True)
                    m_ThumbnailCache.ReuseSharedImage(Resource, Index)
                    Return Index
                Else
                    Return m_ThumbnailCache.Add(Resource, Thumbnail, False)
                End If
            End Using
        End Function


        ''' <summary>
        ''' Given an index into our virtual resource list, retrieve the Resource for that index.
        '''   Note that resource indices change when resources are added/removed from this
        '''   listview.
        ''' </summary>
        ''' <param name="Index">The virtual index to retrieve</param>
        ''' <returns>The Resource from that index.</returns>
        ''' <remarks>Throws an exception if out of bounds</remarks>
        Public Function GetResourceFromVirtualIndex(ByVal Index As Integer) As Resource
            If Index < 0 OrElse Index >= m_VirtualResourceList.Count Then
                Debug.Fail("GetResourceFromVirtualIndex: virtual resource index out of bounds")
                Return Nothing
            End If

            Debug.Assert(TypeOf m_VirtualResourceList(Index) Is Resource)
            Return DirectCast(m_VirtualResourceList(Index), Resource)
        End Function


        ''' <summary>
        ''' Gets the ListViewItem index of a given resource
        ''' </summary>
        ''' <param name="Resource">The Resource to get the ListViewItem index of</param>
        ''' <returns>The index of that resource's ListViewItem</returns>
        ''' <remarks>Returns -1 if not found.</remarks>
        Public Function IndexOf(ByVal Resource As Resource) As Integer
            Debug.Assert(Resource IsNot Nothing)
            Return m_VirtualResourceList.IndexOf(Resource)
        End Function

#End Region


#Region "Selections and Highlighting"

        ''' <summary>
        ''' Gets all resources currently selected in this listview
        ''' </summary>
        ''' <returns>An array of the selected resources.</returns>
        ''' <remarks>Never returns Nothing, even if no resources are selected (returns an empty array)</remarks>
        Public Function GetSelectedResources() As Resource()
            Dim Selected() As Resource
            Dim i As Integer
            Dim SelectedListIndices As ListView.SelectedIndexCollection = MyBase.SelectedIndices
            ReDim Selected(SelectedListIndices.Count - 1)
            For Each SelectedIndex As Integer In SelectedListIndices
                Selected(i) = GetResourceFromVirtualIndex(SelectedIndex)
                Debug.Assert(Selected(i) IsNot Nothing)
                i += 1
            Next

            Debug.Assert(Selected IsNot Nothing)
            Return Selected
        End Function


        ''' <summary>
        ''' Highlights a given resource (selects it and scrolls the listview so that it's visible)
        ''' </summary>
        ''' <param name="Resources">The Resources to highlight</param>
        ''' <remarks></remarks>
        Friend Sub HighlightResources(ByVal Resources As ICollection)
            Dim firstOne As Boolean = True
            For Each Resource As Resource In Resources
                Dim IndexOfResource As Integer = IndexOf(Resource)
                If IndexOfResource >= 0 Then
                    SelectResource(IndexOfResource, True)
                    If firstOne Then
                        MyBase.Items(IndexOfResource).EnsureVisible()
                        If Me.FocusedItem Is Nothing OrElse Not Me.FocusedItem.Selected Then
                            Me.FocusedItem = MyBase.Items(IndexOfResource)
                        End If
                        firstOne = False
                    End If
                Else
                    Debug.Fail("HighlightResource: resource not found")
                End If
            Next
        End Sub

#If True Then 'CONSIDER rewriting now that virtualized listview has way to select/deselect
        <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack:=1, CharSet:=System.Runtime.InteropServices.CharSet.Auto)> _
        Private Structure LVITEM
            Public mask As Integer
            Public iItem As Integer
            Public iSubItem As Integer
            Public state As Integer
            Public stateMask As Integer
            Public pszText As String
            Public cchTextMax As Integer
            Public iImage As Integer
            Public lParam As IntPtr
            Public iIndent As Integer
            Public iGroupId As Integer
            Public cColumns As Integer
            Public puColumns As IntPtr
        End Structure

        Private Declare Auto Function SendMessage Lib "User32" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByRef lParam As LVITEM) As IntPtr

        Private Const LVIF_STATE As Integer = &H8
        Private Const LVIS_SELECTED As Integer = &H2
        Private Const LVM_SETITEMSTATE As Integer = (&H1000 + 43)


        Private Sub SetItemState(ByVal index As Integer, ByVal state As Integer, ByVal mask As Integer)
            If index < 0 OrElse index >= MyBase.VirtualListSize Then
                Debug.Fail("")
                Exit Sub
            End If

            If MyBase.Handle <> IntPtr.Zero Then
                Dim lvi As New LVITEM
                lvi.mask = LVIF_STATE
                lvi.state = state
                lvi.stateMask = mask
                SendMessage(MyBase.Handle, LVM_SETITEMSTATE, index, lvi)
            End If
        End Sub

        Public Sub SelectResource(ByVal ResourceIndex As Integer, ByVal Selected As Boolean)
            If ResourceIndex < 0 OrElse ResourceIndex >= MyBase.VirtualListSize Then
                Debug.Fail("SelectResource: index out of bounds")
                Exit Sub
            End If

            If Selected Then
                SetItemState(ResourceIndex, LVIS_SELECTED, LVIS_SELECTED)
            Else
                SetItemState(ResourceIndex, 0, LVIS_SELECTED)
            End If
        End Sub

        Public Sub SelectResource(ByVal Resource As Resource, ByVal Selected As Boolean)
            SelectResource(IndexOf(Resource), Selected)
        End Sub

        Public Sub UnselectAll()
            SelectedIndices.Clear()
        End Sub

        ''' <summary>
        ''' Unselect all resources in this listview.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SelectAll()
            For i As Integer = 0 To MyBase.VirtualListSize - 1
                SelectedIndices.Add(i)
            Next
            If Me.FocusedItem Is Nothing Then
                Me.FocusedItem = Items(0)
            End If
        End Sub

#End If

#End Region


#Region "Adding/Removing Resources"

        ''' <summary>
        ''' Adds a list of Resources to the set the of resources which the list is showing.
        ''' </summary>
        ''' <param name="Resources">An IList of Resources to add.  These must already exist in a ResourceFile.</param>
        ''' <remarks>
        '''   The resources are added to the end of the list, alphabetized.
        ''' </remarks>
        Public Sub AddResources(ByVal Resources As IList)
            UnselectAll()
            Debug.Assert(m_VirtualResourceList.Count = MyBase.VirtualListSize)

            ' Select the last item, or the list view could scroll a lot when we add a new item to it.
            If m_VirtualResourceList.Count > 0 Then
                Me.FocusedItem = Items(m_VirtualResourceList.Count - 1)
            End If

            'Alphabetize
            Dim AlphabetizedResources As New ArrayList(Resources)
            AlphabetizedResources.Sort(m_Sorter)

            '... and add them to the end of the list.
            Dim DisableItemRetrievalSave As Boolean = m_DisableItemRetrieval
            m_DisableItemRetrieval = True
            Try
                For Each NewResource As Resource In AlphabetizedResources
                    'First verify that the resource is really in our ResourceFile and that it's not already been added
                    If Not m_VirtualResourceList.Contains(NewResource) Then
                        If ResourceFile.Contains(NewResource) Then
                            m_VirtualResourceList.Add(NewResource)
                        Else
                            Debug.Fail("Trying to add a resource to the listview that's not in the ResourceFile")
                        End If
                    Else
                        Debug.Fail("The resource has already been added to the listview")
                    End If
                Next
            Finally
                m_DisableItemRetrieval = DisableItemRetrievalSave
            End Try

            'Update the virtual count and redraw with the new entries.
            MyBase.VirtualListSize = m_VirtualResourceList.Count
            MyBase.Invalidate()
        End Sub

        ''' <summary>
        ''' Removes a list of Resources from the set of resources that are being displayed in this
        '''   listview, if they're in it.  It does not remove them from any ResourceFile or 
        '''   otherwise do anything to the resources.
        ''' </summary>
        ''' <param name="Resources">The set of Resources to remove from view.</param>
        ''' <remarks></remarks>
        Public Sub RemoveResources(ByVal Resources As IList)
            UnselectAll()
            Debug.Assert(m_VirtualResourceList.Count = MyBase.VirtualListSize)

            Dim DisableItemRetrievalSave As Boolean = m_DisableItemRetrieval
            m_DisableItemRetrieval = True
            Try
                For Each Resource As Resource In Resources
                    If m_VirtualResourceList.Contains(Resource) Then
                        m_VirtualResourceList.Remove(Resource)
                    End If
                Next
            Finally
                m_DisableItemRetrieval = DisableItemRetrievalSave
            End Try

            MyBase.VirtualListSize = m_VirtualResourceList.Count
            MyBase.Invalidate()
        End Sub

#End Region

        ''' <summary>
        ''' A helper class to sort the resource list in the detail view
        ''' </summary>
        Private Class DetailViewSorter
            Implements IComparer

            ' which column is used to sort the list
            Private m_columnIndex As Integer
            Private m_reverseOrder As Boolean

            Public Sub New(ByVal columnIndex As Integer, ByVal reverseOrder As Boolean)
                m_columnIndex = columnIndex
                m_reverseOrder = reverseOrder
            End Sub

            ''' <Summary>
            ''' which column is used to sort the list 
            ''' </Summary>
            Friend Property ColumnIndex() As Integer
                Get
                    Return m_columnIndex
                End Get
                Set(ByVal value As Integer)
                    m_columnIndex = value
                End Set
            End Property

            ''' <Summary>
            ''' whether it is in reverseOrder
            ''' </Summary>
            Friend Property InReverseOrder() As Boolean
                Get
                    Return m_reverseOrder
                End Get
                Set(ByVal value As Boolean)
                    m_reverseOrder = value
                End Set
            End Property

            ''' <Summary>
            '''  Compare two list items
            ''' </Summary>
            Public Function Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
                Dim ret As Integer = String.Compare(GetColumnValue(x, m_columnIndex), GetColumnValue(y, m_columnIndex), StringComparison.CurrentCultureIgnoreCase)
                If ret = 0 AndAlso m_columnIndex <> 0 Then
                    ret = String.Compare(GetColumnValue(x, 0), GetColumnValue(y, 0), StringComparison.CurrentCultureIgnoreCase)
                End If
                If m_reverseOrder Then
                    ret = -ret
                End If
                Return ret
            End Function

            ''' <Summary>
            '''  Get String Value of one column
            ''' </Summary>
            Private Function GetColumnValue(ByVal obj As Object, ByVal column As Integer) As String
                If TypeOf obj Is Resource Then
                    Return ResourceListView.GetDetailViewColumn(DirectCast(obj, Resource), column)
                End If

                Debug.Fail("DetailViewSorter: obj was not a Resource")
                Return String.Empty
            End Function


        End Class

        ''' <Summary>
        '''  We save the index range of the items, whose images are quired by the ResourceListView.
        '''  We usually save the information directly in m_ImageStartIndex and m_ImageEndIndex field in the ListView,
        '''  However, when we need load several blocks of them, we have to save the block we haven't processed in a stack.
        '''   This class is the record we push to the stack.
        ''' </Summary>
        Private Class ImageCacheRequirement
            ' We should cache images from StartIndex to EndIndex (included)
            Public StartIndex As Integer
            Public EndIndex As Integer

            ''' <Summary>
            ''' Constructor.
            ''' </Summary>
            ''' <param name="StartIndex"></param>
            ''' <param name="EndIndex"></param>
            Public Sub New(ByVal StartIndex As Integer, ByVal EndIndex As Integer)
                Me.StartIndex = StartIndex
                Me.EndIndex = EndIndex
            End Sub
        End Class

    End Class

End Namespace

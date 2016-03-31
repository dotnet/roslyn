Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.ComponentModel.Design.Serialization
Imports System.Collections
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Globalization
Imports VB = Microsoft.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    Friend NotInheritable Class FindReplace


        ''' <summary>
        '''  The fields of the resource we're going to find the pattern in.
        ''' </summary>
        ''' <remarks></remarks>
        Public Enum Field
            Name
            Value
            Comment

            MinimumValue = Name
            MaximumValue = Comment
        End Enum



        'Pointer to the root designer
        Private m_RootDesigner As ResourceEditorRootDesigner

        'The find state object. Find state is an opaque object we hold on behalf of the find engine.
        '  Setting this object to Nothing will reset the find / replace loop.
        Private m_FindState As Object

        'The array of resources used in Find / Replace.  This array is in the order desired by search.
        Private m_ResourcesToSearch() As Resource

        ' if the last search is in a selection, the m_ResourcesToSearch contains Resources in the selection. We need refresh the list when necessary.
        Private m_LastSearchInSelection As Boolean

        'Where we started the most recent find / replace loop.  We will keep searching until we
        '  loop around and get back to this same index.
        Private m_StartIndex As Integer

        'Field where we started the most recent find / replace loop.  We will keep searching until we
        '  loop around and get back to this same index.
        Private m_StartField As Field

        'Where we are in the current target array list.
        Private m_CurrentIndex As Integer

        'Which field we are currently looking at in the CurrentIndex resource
        Private m_CurrentFieldInCurrentIndex As Field

        'Indicates that we're currently changing the selection programmatically, so we should disregard
        '  attempts to invalidate the find loop.
        Private m_ProgrammaticallyChangingSelection As Boolean

        'Last resource that had a match
        Private m_LastResourceWithMatchFound As Resource



        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="RootDesigner">Pointer to the root designer</param>
        ''' <remarks></remarks>
        Public Sub New(ByVal RootDesigner As ResourceEditorRootDesigner)
            If RootDesigner Is Nothing Then
                Throw New ArgumentNullException("RootDesigner")
            End If
            m_RootDesigner = RootDesigner
        End Sub


        ''' <summary>
        ''' Does debug-only tracing for this class.
        ''' </summary>
        ''' <param name="Message"></param>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Public Shared Sub Trace(ByVal Message As String)
            Debug.WriteLineIf(Switches.RSEFindReplace.TraceVerbose, "RSE Find/Replace: " & Message)
        End Sub


        ''' <summary>
        '''  Reset the array of Resources to find / replace in and the find / replace loop.
        ''' </summary>
        ''' <remarks>
        ''' This should be called when resources are added or removed, so that the search 
        '''   can be reset.
        ''' </remarks>
        Public Sub InvalidateFindLoop(ByVal ResourcesAddedOrRemoved As Boolean)
            If m_ProgrammaticallyChangingSelection Then
                Exit Sub
            End If

            Trace("** Find loop invalidated")

            m_FindState = Nothing

            'If resources have been added or removed, we must invalidate our sorted resource list
            If ResourcesAddedOrRemoved Then
                m_ResourcesToSearch = Nothing
            End If
        End Sub


        ''' <summary>
        ''' Specifies our editor's supported capabilities for Find / Replace.
        ''' </summary>
        ''' <param name="pfImage">Set to True if supporting GetSearchImage - seaching in a text image.</param>
        ''' <param name="pgrfOptions">Specifies supported options, syntax and options, taken from __VSFINDOPTIONS.</param>
        ''' <remarks></remarks>
        Public Sub GetCapabilities(ByVal pfImage() As Boolean, ByVal pgrfOptions() As UInteger)
            Trace("GetCapabilities called.")

            ' We don't support search for text images
            If pfImage IsNot Nothing Then
                pfImage(0) = False
            End If

            Dim Options As __VSFINDOPTIONS

            If pgrfOptions IsNot Nothing Then
                If HasView Then
                    Options = _
                        __VSFINDOPTIONS.FR_MatchCase Or _
                        __VSFINDOPTIONS.FR_WholeWord Or _
                        __VSFINDOPTIONS.FR_Backwards Or _
                        __VSFINDOPTIONS.FR_Wildcard Or _
                        __VSFINDOPTIONS.FR_RegExpr Or _
                        __VSFINDOPTIONS.FR_Find Or _
                        __VSFINDOPTIONS.FR_Document

                    If m_LastSearchInSelection AndAlso m_ResourcesToSearch IsNot Nothing _
                        OrElse View.GetSelectedResources().Length > 0 _
                    Then
                        'Find in selection supported if there are resources selected (or if we're currently in the middle of find
                        '  in selection).
                        Options = Options Or __VSFINDOPTIONS.FR_Selection
                    End If
                Else
                    'If there's no view (happens when there's an error loading the resx file), then
                    '  there's no search capability.
                    Options = 0
                End If

                pgrfOptions(0) = CType(Options, UInteger)
            End If
        End Sub


        ''' <summary>
        ''' Returns a value of a requested property.
        ''' </summary>
        ''' <param name="propid">Property identifier of the requested property, taken from VSFTPROPID enum.</param>
        ''' <param name="pvar">Property value.</param>
        ''' <returns>S_OK if success, otherwise an error code.</returns>
        ''' <remarks>All properties are optional to handle.</remarks>
        Public Function GetProperty(ByVal propid As UInteger, ByRef pvar As Object) As Integer
            Trace("IVsFindTarget.GetProperty: " + CType(propid, __VSFTPROPID).ToString())

            Select Case CType(propid, __VSFTPROPID)
                Case __VSFTPROPID.VSFTPROPID_BlockName
                    ' Only asked if FR_BLOCK is set. This one is used to find / replace in a block of code.
                    pvar = String.Empty
                    Return Interop.NativeMethods.E_NOTIMPL

                Case __VSFTPROPID.VSFTPROPID_DocName
                    ' Full path of filename or persistence moniker.
                    pvar = m_RootDesigner.GetResXFileNameAndPath()
                    Return Interop.NativeMethods.S_OK

                Case __VSFTPROPID.VSFTPROPID_IsDiskFile
                    ' Is this a file on disk? 
                    'VSIP docs say: Currently not used.  BscEdt sample returns True, saying it's assuming the file is always on disk.
                    pvar = True
                    Return Interop.NativeMethods.S_OK

                Case __VSFTPROPID.VSFTPROPID_IsFindInFilesForegroundOnly
                    ' NOTE: from StephSh - VSFTPROPID_IsFindInFilesForegroundOnly is to tell us the document should be 
                    ' searched on foreground only. This is more like a hack. Normally we do the search on background. 
                    ' For some reasons we do not marshal the pointers. This does not work all the time. 
                    ' For those cases that it does not work, we ask them to set this property so we can do the search on foreground.
                    pvar = False
                    Return Interop.NativeMethods.S_OK

                Case __VSFTPROPID.VSFTPROPID_WindowFrame
                    pvar = m_RootDesigner.GetService(GetType(IVsWindowFrame))
                    Return Interop.NativeMethods.S_OK

                Case Else
                    Return Interop.NativeMethods.E_NOTIMPL

            End Select
        End Function


        ''' <summary>
        ''' Gets the find state object that we hold for the find engine.
        ''' </summary>
        ''' <returns>m_FindState</returns>
        ''' <remarks>If m_FindState is set to Nothing, the shell will reset the next find loop.</remarks>
        Public Function GetFindState() As Object
            Return m_FindState
        End Function


        ''' <summary>
        ''' Sets the find state object that we hold for the find engine.
        ''' </summary>
        ''' <param name="pUnk">The find state object to hold.</param>
        ''' <remarks></remarks>
        Public Sub SetFindState(ByVal pUnk As Object)
            m_FindState = pUnk
        End Sub


        ''' <summary>
        ''' Searches for a text pattern.
        ''' </summary>
        ''' <param name="pszSearch">The search pattern.</param>
        ''' <param name="grfOptions">The options of the test (from __VSFINDOPTIONS).</param>
        ''' <param name="fResetStartPoint">1 means the find loop is reset, otherwise 0.</param>
        ''' <param name="pHelper">IVsFindHelper interface containing utiliy methods for Find.</param>
        ''' <param name="pResult">Search result, values are taken from __VSFINDRESULT.</param>
        ''' <remarks> 
        ''' Find works as follow:
        ''' - User clicks Find, shell will call our Find, passing in the string to search, the options for the search,
        '''      and a flag of whether the start point is reset (true for the first time, false for the rest of 'Find Next').
        ''' - We search for the text and return an enum value to the shell to display the dialog box if not found, etc...
        ''' - We are responsible for selecting the found object and keeping track of where we are in the object list.
        ''' </remarks>
        Public Sub Find(ByVal pszSearch As String, ByVal grfOptions As UInteger, ByVal fResetStartPoint As Integer, _
                            ByVal pHelper As IVsFindHelper, ByRef pResult As UInteger)
            Dim FindReset As Boolean = (fResetStartPoint <> 0)
            Dim FindBackwards As Boolean = CheckFindOption(grfOptions, __VSFINDOPTIONS.FR_Backwards)
            Dim FindInSelection As Boolean = CheckFindOption(grfOptions, __VSFINDOPTIONS.FR_Selection)
            Dim FindJustStarted As Boolean = False ' We just started a find loop.

            If Not HasView Then
                pResult = CType(__VSFINDRESULT.VSFR_NotFound, UInteger)
                Return
            End If

            ' First, check the state of the find / replace loop.
            If FindReset OrElse m_ResourcesToSearch Is Nothing Then
                If FindReset Then
                    If FindInSelection OrElse m_LastSearchInSelection Then
                        ' We always reset the list when the search type is changed. And we always sync to the selection when a new search in selection starts
                        m_ResourcesToSearch = Nothing
                    End If
                End If

                If m_ResourcesToSearch IsNot Nothing AndAlso m_ResourcesToSearch.Length <> View.ResourceFile.ResourcesHashTable.Count Then
                    Debug.Fail("The number of resources has changed, but InvalidateFindLoop() wasn't called.  This must be called when resources are added/removed.")
                    m_ResourcesToSearch = Nothing 'defensive - force re-generation anyway
                End If

                If m_ResourcesToSearch Is Nothing Then
                    Trace("Determining resources to search through and starting new loop.")
                    'Something changed since the last search (or it's the first search) - figure out which
                    '  resources we're going to search through.
                    m_ResourcesToSearch = GetResourcesToSearch(FindInSelection)
                Else
                    Trace("Resetting find loop using previous set of resources")
                End If

                ' This is a new find loop because either
                '   1) the user changed the find pattern
                '   2) we reached the beginning of the find loop, or
                '   3) the user selected something on the designer in the middle of a find loop

                ' Start the search over at the selected resource
                FindJustStarted = True
                m_LastResourceWithMatchFound = Nothing
                m_LastSearchInSelection = FindInSelection
                DetermineSearchStart(FindBackwards, FindInSelection)

                'Remember this location.
                m_StartIndex = m_CurrentIndex
                m_StartField = m_CurrentFieldInCurrentIndex
            Else
                ' In the middle of a find / replace loop. The next position was already calculated the last time.
                Trace("Starting again on current find/replace loop.")
                Debug.Assert(m_CurrentIndex >= 0 AndAlso m_CurrentIndex < m_ResourcesToSearch.Length, "Invalid current find index")
            End If

            ' Now, start the find / replace for this run.  We'll keep going until we reach the end or find a match.
            If m_ResourcesToSearch.Length > 0 Then

                ' Loop through the resource array to find the pattern.
                ' We exit the loop when we go back where we start, indicating by these condition:
                '   1. CurrentIndex and StartIndex and the start/current field are the same. (indicating we might reach the start of the loop). AND
                '   2. FindReset is FALSE. (indicating this is not a start of a loop, 
                '       since CurrentIndex = StartIndex at the start of a loop). AND
                Dim VeryFirstSearch As Boolean = FindJustStarted
                While VeryFirstSearch OrElse _
                        Not (m_CurrentIndex = m_StartIndex AndAlso m_CurrentFieldInCurrentIndex = m_StartField)
                    VeryFirstSearch = False

                    Debug.Assert(0 <= m_CurrentIndex AndAlso m_CurrentIndex < m_ResourcesToSearch.Length, _
                        "m_FindCurrentIndex out of range!!!")
                    Debug.Assert(m_ResourcesToSearch(m_CurrentIndex) IsNot Nothing, "Invalid resource!!!")

                    ' Get the text to search for
                    Dim Text As String = ""
                    Dim CurrentResource As Resource = m_ResourcesToSearch(m_CurrentIndex)
                    Select Case m_CurrentFieldInCurrentIndex
                        Case Field.Name
                            Text = CurrentResource.Name

                        Case Field.Value
                            'For value, we only search through actual bona fide strings and resources which are 
                            '  convertible to string
                            If Not CurrentResource.IsLink Then
                                If CurrentResource.ResourceTypeEditor.Equals(ResourceTypeEditors.String) _
                                    OrElse CurrentResource.ResourceTypeEditor.Equals(ResourceTypeEditors.StringConvertible) _
                                Then
                                    Try
                                        'Try to get a text-converted value from this resource to search through
                                        Text = CurrentResource.GetTypeConverter().ConvertToString(CurrentResource.GetValue())
                                        If Text Is Nothing Then
                                            Text = ""
                                        End If
                                    Catch ex As Exception
                                        RethrowIfUnrecoverable(ex)
                                        Text = ""
                                    End Try
                                End If
                            End If

                        Case Field.Comment
                            Text = CurrentResource.Comment

                        Case Else
                            Debug.Fail("Invalid field to find")
                            Text = ""
                    End Select

                    Dim MatchFound As Boolean
                    Trace("Search for '" & pszSearch & "' in resource '" & CurrentResource.Name & "', property '" & m_CurrentFieldInCurrentIndex.ToString() & "' (" & Text & ")")
                    If m_LastResourceWithMatchFound Is CurrentResource AndAlso ResourceIsSearchedAsSingleUnit(CurrentResource) Then
                        'Special case: This is a resource not shown in a string table, so we aren't able to individually highlight
                        '  the(Name And Comment) etc fields.  Since we already found a match in some field in this resource, force 
                        '  it to move to the next resource (instead of next field in this resource) for the next search attempt by
                        '  not searching again in this resource.  Otherwise we might end up stopping twice on the same resource with 
                        '  no change in UI.
                        MatchFound = False
                        Trace("    Skipping because we've already found a match in this non-stringtable resource")
                    Else
                        ' See if we have a match.
                        MatchFound = IsMatch(pszSearch, Text, pHelper, grfOptions)
                        If MatchFound Then
                            'Yes, we have a match!
                            Trace("    Match found!")

                            Debug.Assert(Not m_ProgrammaticallyChangingSelection)
                            m_ProgrammaticallyChangingSelection = True 'Guard because we'll get called back in InvalidateFindLoop from this.
                            Try
                                View.UnselectAllResources()
                                View.HighlightResource(CurrentResource, m_CurrentFieldInCurrentIndex, SelectInPropertyGrid:=True)
                            Finally
                                m_ProgrammaticallyChangingSelection = False
                            End Try

                            ' NOTE: We continue to calculate the next index for the next call, then exit this call after that if found.
                            MatchFound = True
                            m_LastResourceWithMatchFound = CurrentResource
                        Else
                            Trace("    No match.")
                        End If
                    End If

                    ' Advance the search position or field
                    IncrementCurrentIndexAndField(FindBackwards)

                    ' We found a match above, go ahead and return the result now.
                    If MatchFound Then
                        pResult = CType(__VSFINDRESULT.VSFR_Found, UInteger)
                        Return
                    End If
                End While

                ' We found nothing and looped back to where we started the find loop.
                If FindJustStarted Then
                    ' If we just started the find loop this call, return "The specified text was not found."
                    pResult = CType(__VSFINDRESULT.VSFR_NotFound, UInteger)
                Else
                    ' We were in the middle of a find loop already, so return "Find reached the starting point of the search."
                    pResult = CType(__VSFINDRESULT.VSFR_EndOfSearch, UInteger)
                
                    ' Select any resources that were selected when we started an in selection search
                    If FindInSelection Then
                        View.HighlightResources(m_ResourcesToSearch, True)
                    End If
                End If
            Else
                ' No resources to search through, return error.
                pResult = CType(__VSFINDRESULT.VSFR_Error, UInteger)
            End If
        End Sub

        ''' <summary>
        '''  Return TRUE or FALSE for a single option from its bit in grfOptions.
        ''' </summary>
        ''' <param name="grfOptions">The options passed in from the shell.</param>
        ''' <param name="Flag">The option we want to know.</param>
        ''' <returns>Value of the option, TRUE or FALSE.</returns>
        ''' <remarks></remarks>
        Private Function CheckFindOption(ByVal grfOptions As UInteger, ByVal Flag As __VSFINDOPTIONS) As Boolean
            Return (CType(grfOptions, __VSFINDOPTIONS) And Flag) <> 0
        End Function


        ''' <summary>
        ''' Returns true if the given resource is to be treated as a single unit for find matches.  I.e., if
        '''   true, then find will stop exactly once on that resource, even if multiple matches are found in different
        '''   fields in the same resource.
        ''' </summary>
        ''' <param name="Resource">The resource to check</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' This returns True for resources which are displayed in a listview, because there is no easy way to 
        '''   highlight separate parts of the resource.
        ''' </remarks>
        Private Function ResourceIsSearchedAsSingleUnit(ByVal Resource As Resource) As Boolean
            Return Not Resource.ResourceTypeEditor.DisplayInStringTable()
        End Function


        ''' <summary>
        ''' Increments the current index pointer to the next resource
        ''' </summary>
        ''' <param name="FindBackwards">True if we're searching backwards</param>
        ''' <remarks></remarks>
        Private Sub IncrementCurrentIndex(ByVal FindBackwards As Boolean)
            If FindBackwards Then
                m_CurrentIndex -= 1
                If m_CurrentIndex < 0 Then
                    m_CurrentIndex = m_ResourcesToSearch.Length - 1
                End If
            Else
                m_CurrentIndex += 1
                If m_CurrentIndex >= m_ResourcesToSearch.Length Then
                    m_CurrentIndex = 0
                End If
            End If
        End Sub


        ''' <summary>
        '''  Calculate the next index and field to search based on the current index/field and the direction of find
        ''' </summary>
        ''' <param name="FindBackwards">TRUE indicates finding backwards; otherwise, FALSE.</param>
        ''' <remarks></remarks>
        Private Sub IncrementCurrentIndexAndField(ByVal FindBackwards As Boolean)
            Dim NextFieldIndex As Integer = CInt(m_CurrentFieldInCurrentIndex)
            If FindBackwards Then
                NextFieldIndex = m_CurrentFieldInCurrentIndex - 1
                If NextFieldIndex < 0 Then
                    NextFieldIndex = Field.MaximumValue
                    IncrementCurrentIndex(FindBackwards)
                End If
            Else
                NextFieldIndex = m_CurrentFieldInCurrentIndex + 1
                If NextFieldIndex > Field.MaximumValue Then
                    NextFieldIndex = 0
                    IncrementCurrentIndex(FindBackwards)
                End If
            End If

            m_CurrentFieldInCurrentIndex = DirectCast(NextFieldIndex, Field)

            Debug.Assert(m_CurrentIndex >= 0 AndAlso m_CurrentIndex < m_ResourcesToSearch.Length)
            Debug.Assert(System.Enum.IsDefined(GetType(Field), m_CurrentFieldInCurrentIndex), "Field enum is not contiguous?")
        End Sub


        ''' <summary>
        '''  Function to find a match of a specified pattern in a specified text.
        ''' </summary>
        ''' <param name="SearchPattern">The pattern to search for.</param>
        ''' <param name="SearchText">The text to search in.</param>
        ''' <param name="Helper">The IVsFindHelper from VisualStudio</param>
        ''' <returns>TRUE if a match was found; otherwise, FALSE.</returns>
        ''' <remarks></remarks>
        Private Function IsMatch(ByVal SearchPattern As String, ByVal SearchText As String, _
                                    ByVal Helper As IVsFindHelper, ByVal grfFindOptions As UInteger) As Boolean
            If String.IsNullOrEmpty(SearchText) Then
                Return False
            End If

            'Use IVsFindHelper to do the hard work.
            Dim BufferFlags As __VSFINDBUFFERFLAGS = 0
            Dim iFound, cchFound As UInteger
            Dim bstrReplaceText As String = Nothing
            Dim fFound As Integer = 0
            Dim charArray As Char() = SearchText.ToCharArray()
            Dim textArray As UShort() = new UShort(charArray.Length-1) {}
            charArray.CopyTo(textArray, 0)
            Dim hr As Integer = Helper.FindInText(SearchPattern, Nothing, grfFindOptions, CUInt(BufferFlags), CUInt(textArray.Length), textArray, iFound, cchFound, bstrReplaceText, fFound)

            Return fFound <> 0
        End Function


        ''' <summary>
        ''' Returns the ordered array of resources that we will use in find / replace.
        ''' </summary>
        ''' <param name="FindInSelection">Whether or not to search only in the current selection</param>
        ''' <value>An ArrayList containing Resource instances ordered by category and names.</value>
        ''' <remarks></remarks>
        Private ReadOnly Property GetResourcesToSearch(ByVal FindInSelection As Boolean) As Resource()
            Get
                Dim ResourcesToSearch As ArrayList

                Trace("Getting list of resources to search through")

                'First collect all the resources to search through
                If FindInSelection Then
                    Dim SelectedResources() As Resource = View.GetSelectedResources()
                    ResourcesToSearch = New ArrayList(View.ResourceFile.ResourcesHashTable.Count)
                    For Each Resource As Resource In SelectedResources
                        ResourcesToSearch.Add(Resource)
                    Next
                Else
                    ResourcesToSearch = New ArrayList(View.ResourceFile.ResourcesHashTable.Count)
                    For Each Entry As DictionaryEntry In View.ResourceFile
                        Dim Resource As Resource = DirectCast(Entry.Value, Resource)
                        ResourcesToSearch.Add(Resource)
                    Next
                End If

                'Now sort them according to category and name
                Dim Comparer As New ResourceComparerForFind(View.Categories())
                Comparer.SortResources(ResourcesToSearch)

                Dim ReturnArray(ResourcesToSearch.Count - 1) As Resource
                ResourcesToSearch.CopyTo(ReturnArray)
                Trace("    (" & ReturnArray.Length & " resources to search through)")
                Return ReturnArray
            End Get
        End Property


        ''' <summary>
        ''' Determines the base resource where we want to start searching, if this is
        '''   a new search.
        ''' </summary>
        ''' <param name="FindBackwards">True if we're searching backwards</param>
        ''' <param name="FindInSelection">True if we're finding in a selection</param>
        ''' <remarks>Sets current index and field to the determined starting location.</remarks>
        Private Sub DetermineSearchStart(ByVal FindBackwards As Boolean, ByVal FindInSelection As Boolean)
            Debug.Assert(m_ResourcesToSearch IsNot Nothing)
            Dim StartingResource As Resource = Nothing

            'Generally, we actually start searching after (before for backwards) the currently-selected
            '  cell, rather than on it, unless we're searching inside a selection
            Dim StartAfterOrBeforeCell As Boolean = Not FindInSelection

            m_CurrentFieldInCurrentIndex = Field.MinimumValue

            If View.ResourceFile.ResourcesHashTable.Count = 0 Then
                'No resources at all.
                m_CurrentIndex = 0
                Exit Sub
            End If

            'Start searching after or before the selected resources, if there are any
            Dim SelectedResources() As Resource = View.GetSelectedResources()
            If SelectedResources.Length > 0 Then
                Dim Index As Integer = 0
                Dim StartAtBeginningOfSelection As Boolean

                'For find in selection and going forwards, we start at the beginning of the selection.
                'For not find in selection and going forwards, we start after the last resource in the selection.
                If FindInSelection Then
                    StartAtBeginningOfSelection = Not FindBackwards
                Else
                    StartAtBeginningOfSelection = FindBackwards
                End If

                If StartAtBeginningOfSelection Then
                    StartingResource = SelectedResources(0)
                Else
                    StartingResource = SelectedResources(SelectedResources.Length - 1)
                    m_CurrentFieldInCurrentIndex = Field.MaximumValue
                End If
            Else
                'If no selection, start at the current cell
                If View.CurrentCategory.CategoryDisplay = Category.Display.StringTable AndAlso View.StringTable.CurrentCell IsNot Nothing Then
                    Dim CurrentCellRow As Integer = View.StringTable.CurrentCell.RowIndex
                    If CurrentCellRow < View.StringTable.RowCountVirtual Then
                        m_CurrentFieldInCurrentIndex = View.StringTable.GetCurrentCellFindField()
                        StartingResource = View.StringTable.GetResourceFromRowIndex(View.StringTable.CurrentCell.RowIndex, AllowUncommittedRow:=False)
                    End If

                    If StartingResource Is Nothing Then
                        'If no selection and no current cell, start with the first resource in the current category
                        Dim CurrentCategory As Category = View.CurrentCategory
                        Dim Categories As CategoryCollection = View.Categories
                        For Each Resource As Resource In m_ResourcesToSearch
                            If Resource.GetCategory(Categories) Is CurrentCategory Then
                                StartingResource = Resource
                                Exit For
                            End If
                        Next

                        'We want to actually start the search with the first cell in this category, not after/before it.
                        StartAfterOrBeforeCell = False
                    End If
                End If
            End If

            If StartingResource IsNot Nothing Then
                m_CurrentIndex = System.Array.IndexOf(m_ResourcesToSearch, StartingResource)
                If m_CurrentIndex < 0 Then
                    Debug.Fail("Couldn't find resource that we just had our fingers on")
                    m_CurrentIndex = 0
                    m_CurrentFieldInCurrentIndex = Field.MinimumValue
                    Return
                End If

                If StartAfterOrBeforeCell Then
                    'Okay, we found the resource we were looking for.  But what we really want is to start the search at
                    '  the next location *after* (or before, for backwards searching) that one.  So do a quick increment.
                    If ResourceIsSearchedAsSingleUnit(StartingResource) Then
                        IncrementCurrentIndex(FindBackwards)
                    Else
                        IncrementCurrentIndexAndField(FindBackwards)
                    End If
                End If

                Trace("Determined new search start location: " & StartingResource.Name & "/" & m_CurrentFieldInCurrentIndex.ToString())
            Else
                'We still don't have anywhere reasonable to start searching (happens for instance if you start searching from a category
                '  that doesn't contain any resources).  Let's start at the very beginning.  A very good place to start.  Do re me...
                m_CurrentIndex = 0
                m_CurrentFieldInCurrentIndex = Field.MinimumValue
            End If
        End Sub


        ''' <summary>
        ''' Returns the resource editor view associated with this designer.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property View() As ResourceEditorView
            Get
                Return m_RootDesigner.GetView()
            End Get
        End Property


        ''' <summary>
        ''' Returns True iff there a View has already been created.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property HasView() As Boolean
            Get
                Return m_RootDesigner.HasView()
            End Get
        End Property

    End Class

End Namespace

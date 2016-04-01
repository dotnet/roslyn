' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This class is a cache of thumbnail images that are kept in an ImageList and intended to be used with
    '''   a listview working in virtual mode.  The maximum size and expected
    '''   working set of the imagelist can be specified, and thumbnails will be recycled as needed.
    '''   This class works by associated each thumbnail with an object key (a Resource object).  When you 
    '''   have a thumbnail that you want cached, you add it with an associated key.
    '''   Whenever you need an index into the imagelist 
    '''   you *must* call GetImageListIndex() to look for it first, because it's possible that the 
    '''   thumbnail has been recycled in order to make room for another thumbnail.
    '''
    ''' Note that we can't simply use bitmaps for the thumbnail images in the ListView, because it only
    '''   supports retrieving images from an ImageList (well, in Whidbey there's new support for bitmaps,
    '''   but it's not very efficient and still uses an ImageList).  If we created an ImageList large enough
    '''   to hold a thumbnail for every single resource, that would not scale very well with large resx files
    '''   (esp. since we're using a virtualized listview and doing other work to delay load images from disk),
    '''   therefore we need to take the caching approach.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ThumbnailCache

        'The ImageList which contains the thumbnail images
        Private _imageList As ImageList

        'The number of reserved images at the bottom of the list of images, set by the user.
        '   These could be used for overlays, common images, etc.  They will never
        '   be recycled, and will not be changed.
        Private _reservedImagesCount As Integer

        'Backs MinimumSizeBeforeRecycling property
        '
        'The Windows ListView sometimes retrieves items before suggesting a cache
        '  size.  We want to have a reasonable minimum in place as default.
        Private _minimumSizeBeforeRecycling As Integer = 10

        'Backs MaximumSuggestedCacheSize property
        Private _maximumSuggestedCacheSize As Integer = 30

        'Note that we can't simply use the key mechanism built into ImageList, because the only way to
        '  replace an old image in the ImageList (without shifting other indices) does not allow you 
        '  to simultaneously change the key.  When we replace an image in the imagelist, we're also
        '  using a different key.
        Private _keys As New Hashtable

        'An Mru List we maintained to release the most less used item in the ImageList when we need load a new image.
        ' For performance reason, we implement the list table inside an array of structures. Each item contains a point (index)
        ' to the pervious and next item.  We use the m_MruList(0) as a special item to save the head and tail of the queue, so
        ' the queue will be a loop. m_MruList(i+1) item record the state of the ImageList(i). When a item was used, we add it to
        ' the end of the queue, and release the item from the head of the queue when we need space.
        ' NOTE: reserved item and unused space in the imageList will not in the queue.
        ' (Their Previous/NextIndex will be 0).
        ' The array will grow when it is necessary, but will never shrink.
        Private _mruList() As MruListItem


        '=====================================================================




        ''' <summary>
        ''' Constructor.
        ''' </summary>
        ''' <param name="ImageList">The ImageList to store the thumbnail images into.</param>
        ''' <remarks>
        ''' IMPORTANT: Any images which are currently in the imagelist will be considered to be
        '''   reserved.  These could be used for overlays, common images, etc.  They will never
        '''   be recycled, and will not be changed.
        '''</remarks>
        Public Sub New(ByVal ImageList As ImageList)
            _reservedImagesCount = ImageList.Images.Count
            _imageList = ImageList
            _mruList = New MruListItem(_maximumSuggestedCacheSize + _reservedImagesCount) {}
        End Sub




        '=====================================================================




        ''' <summary>
        ''' This is the absolute minimum number of thumbnails before which the cache may recycle thumbnails.
        '''   It may never recycle thumbnails until the cache has grown to at least this size.
        '''   You can think of this as the expected working set.  I.e., if you're using a ListView, then you
        '''   should set this value to at least the number of listview items which can fit on the current page,
        '''   to ensure that there is no thrashing from displaying a single page.  If the cache cannot grow
        '''   but is less than this value, out of memory exceptions will be thrown.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Does not change the number of images in the cache, only affects future behavior.</remarks>
        Public Property MinimumSizeBeforeRecycling() As Integer
            Get
                Return _minimumSizeBeforeRecycling
            End Get
            Set(ByVal Value As Integer)
                If Value > 0 Then
                    _minimumSizeBeforeRecycling = Value
                Else
                    Debug.Fail("Bad MinimumSizeBeforeRecycling")
                End If
            End Set
        End Property


        ''' <summary>
        ''' The maximum size to which the thumbnail cache can grow.  MinimumSizeBeforeRecycling takes precedence - if 
        '''   MinimumSizeBeforeRecycling is greater than MaximumSuggestedCacheSize, then MinimumSizeBeforeRecycling is used
        '''   as the maximum cache size instead.
        ''' </summary>
        ''' <value></value>
        ''' <remarks>Method does not try to remove any images if you set this to a lower value than the current
        '''   number of images in the cache (because the caching hints from the ListView are not very accurate, no
        '''   need to get rid of items from the cache that we already had the memory to create).
        ''' </remarks>
        Public Property MaximumSuggestedCacheSize() As Integer
            Get
                Return _maximumSuggestedCacheSize
            End Get
            Set(ByVal Value As Integer)
                Debug.Assert(Value > 0)
                _maximumSuggestedCacheSize = Value
            End Set
        End Property


        ''' <summary>
        ''' Gets the number of thumbnails currently in the cache (not including the number of reserved images)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public ReadOnly Property ThumbnailCount() As Integer
            Get
                Debug.Assert(_imageList.Images.Count - _reservedImagesCount >= 0)
                Return _imageList.Images.Count - _reservedImagesCount
            End Get
        End Property


        ''' <summary>
        ''' Retrieves the actual, effective maximum size to which the cache can grow.  This is equal to 
        '''   the largest of the MaximumSuggestedCacheSize and MinimumSizeBeforeRecycling properties.
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property EffectiveMaximumSuggestedCacheSize() As Integer
            Get
                Return System.Math.Max(_minimumSizeBeforeRecycling, _maximumSuggestedCacheSize)
            End Get
        End Property




        '=====================================================================




        ''' <summary>
        ''' Adds a new thumbnail image into the ListImage cache.
        ''' </summary>
        ''' <param name="Key">An object key used to retrieve the thumbnail index later.</param>
        ''' <param name="Thumbnail">The thumbnail image to place into the cache.  After this call, the original 
        '''   image is no longer needed, as it is copied into the ImageList's memory.</param>
        ''' <param name="IsSharedImage"> the image was shared by many objects, we will reserve those images and won't release them.
        ''' </param>
        ''' <returns>The index of the new image in the cache.</returns>
        ''' <remarks>The key should not already exist in the cache.</remarks>
        Public Function Add(ByVal Key As Object, ByVal Thumbnail As Image, ByVal IsSharedImage As Boolean) As Integer
            'Verify that it's not already in the cache.
            Dim CurrentIndex As Integer
            Dim ThumbnailStillInCache As Boolean
            GetCachedImageListIndex(Key, ThumbnailStillInCache, CurrentIndex)
            If ThumbnailStillInCache Then
                'If the key's already there, simply replace the image.
                _imageList.Images(CurrentIndex) = Thumbnail
                Return CurrentIndex
            End If

            'Find out the index where we should place the new thumbnail.
            Dim Index As Integer
            Dim InsertAtEnd As Boolean
            Dim ThumbnailInserted As Boolean
            GetNextImageIndex(Index, InsertAtEnd)
            If InsertAtEnd Then
                Try
                    '... at the end of the list (i.e., we're expanding the ImageList's size)
                    _imageList.Images.Add(Thumbnail)
                    Debug.Assert(ThumbnailCount - 1 + _reservedImagesCount = Index)
                    ThumbnailInserted = True
                Catch ex As Exception
                    RethrowIfUnrecoverable(ex, IgnoreOutOfMemory:=True)

                    'Hmmm, can't add a new index?  Let's try again, requesting that we recycle an
                    '  old image if possible.  If it still fails, we can't do anything else
                    '  to thwart the exception.
                    GetNextImageIndex(Index, InsertAtEnd, TryForceRecycle:=True)
                    If InsertAtEnd Then
                        Throw ex 'Couldn't recycle an index - gotta rethrow that exception after all
                    End If

                    'Okay, we can recycle an image.  InsertAtEnd is not False, so we continue
                    '  through to the next If statement where the new thumbnail we get
                    '  added.
                End Try
            End If

            'Note: This If statement can't be an Else clause - see above logic
            If Not InsertAtEnd Then
                'We're recycling an image - go ahead and replace it with the new image
                Debug.Assert(Not ThumbnailInserted, "logic error")
                _imageList.Images(Index) = Thumbnail
                ThumbnailInserted = True
            End If
            Debug.Assert(ThumbnailInserted, "logic error - should have been inserted by now")

            'Enqueue the index that we used, so we can recycle it later if need be, in order of being added.
            _keys.Add(Key, Index)
            If IsSharedImage Then
                _reservedImagesCount = _reservedImagesCount + 1
            Else
                Try
                    UpdateMruList(Index, Key, True)
                Catch ex As Exception
                    'If we fail to enqueue, back out the addition of the key, these two lists must remain in sync.
                    _keys.Remove(Key)
                    Throw
                End Try
            End If

#If DEBUG Then
            Dim DebugIndex As Integer
            Dim DebugFound As Boolean
            DebugFound = GetCachedImageListIndexInternal(Key, DebugIndex)
            Debug.Assert(DebugFound AndAlso DebugIndex = Index)

            DebugCheckQueueInvariant()
#End If
            Debug.Assert(Index < ThumbnailCount + _reservedImagesCount)
            Return Index
        End Function


        ''' <summary>
        '''   Add cache that one object should reuse a shared image.
        '''    The reason is a resource item could switch between a normal image, and an image indicating something wrong.
        ''' </summary>
        ''' <param name="Key">An object key used to retrieve the thumbnail index later.</param>
        ''' <param name="Index"> The index of the shared image.
        ''' </param>
        ''' <remarks>The key should not already exist in the cache, and it must be an index to a shared image.</remarks>
        Public Sub ReuseSharedImage(ByVal Key As Object, ByVal Index As Integer)
            Debug.Assert(Not IsInMruList(Index))
            _keys.Add(Key, Index)
        End Sub

        ''' <summary>
        ''' Invalidates the thumbnail associated with this key.  I.e., if the thumbnail is found in the cache (via the
        '''   key, it is removed.  If if is not found, nothing happens.
        ''' </summary>
        ''' <param name="Key">Key to remove if found.</param>
        ''' <remarks></remarks>
        Public Sub InvalidateThumbnail(ByVal Key As Object)
            Dim Index As Integer
            If GetCachedImageListIndexInternal(Key, Index) Then
                RemoveKey(Key, Index)
            End If
        End Sub

        ''' <summary>
        '''  Check whether the object has already been cached.
        ''' </summary>
        ''' <param name="Key">Key to remove if found.</param>
        ''' <remarks></remarks>
        Public Function IsThumbnailInCache(ByVal Key As Object) As Boolean
            Return _keys.ContainsKey(Key)
        End Function

        ''' <summary>
        ''' Searches for a cached thumbnail by key.  May return False even if the key was added, since the
        '''   thumbnail may have been recycled.
        ''' </summary>
        ''' <param name="Key">The key to search for in the cache.</param>
        ''' <param name="ThumbnailFound">Returns true iff the key was found in the cache.</param>
        ''' <param name="Index">The index of the found image.  If ThumbnailFail is False, this value is undefined.</param>
        ''' <remarks></remarks>
        Public Sub GetCachedImageListIndex(ByVal Key As Object, ByRef ThumbnailFound As Boolean, ByRef Index As Integer)
            ThumbnailFound = GetCachedImageListIndexInternal(Key, Index)
            If ThumbnailFound AndAlso IsInMruList(Index) Then
                ' NOTE: It could be a reserved item (shared icon), in this case, it is not in the MRU list, and we shouldn't update the LIST.
                UpdateMruList(Index, Key, True)
            End If
        End Sub

        ''' <summary>
        ''' Searches for a cached thumbnail by key.  May return False even if the key was added, since the
        '''   thumbnail may have been recycled.
        '''  The internal function won't update the MRU list.
        ''' </summary>
        ''' <param name="Key">The key to search for in the cache.</param>
        ''' <param name="Index">The index of the found image.  If ThumbnailFail is False, this value is undefined.</param>
        ''' <returns>True if we found one</returns>
        ''' <remarks></remarks>
        Private Function GetCachedImageListIndexInternal(ByVal Key As Object, ByRef Index As Integer) As Boolean
            Dim ThumbnailFound As Boolean
            Debug.Assert(Key IsNot Nothing)
            Dim IndexAsObject As Object = _keys(Key)
            Debug.Assert(_keys.ContainsKey(Key) = (IndexAsObject IsNot Nothing))

            If IndexAsObject Is Nothing Then
                'Sorry, we no longer have a copy of that thumbnail...  You have to re-add it.
                ThumbnailFound = False
                Index = -1
            Else
                'Note that the Index might actually be higher than the current EffectMaximumSuggestedCacheSize.  That's okay,
                '  it just means that item was placed into the cache earlier when EffectMaximumSuggestedCacheSize was at
                '  a higher value.

                ThumbnailFound = True
                Index = DirectCast(IndexAsObject, Integer)
                Debug.Assert(Index < ThumbnailCount + _reservedImagesCount)
            End If
            Return ThumbnailFound
        End Function


        ''' <summary>
        ''' Gets the index where the next new thumbnail image should be inserted or recycled to.
        ''' </summary>
        ''' <param name="Index">[out] Returns the index to place the next image.</param>
        ''' <param name="InsertAtEnd">[out] Returns True if the next image should be inserted at the end of the ImageList's images.</param>
        ''' <param name="TryForceRecycle">[in] If True, and we have reached the minimum number of thumbnails before being allowed to recycle
        '''   images, then we will attempt to recycle an old image.  This can be useful if we haven't reached the maximum cache size, but we have
        '''   run out of memory trying to increase the current cache size.</param>
        ''' <remarks>
        '''   When you actually use the index returned from this list, you must enqueue it.
        '''</remarks>
        Private Sub GetNextImageIndex(ByRef Index As Integer, ByRef InsertAtEnd As Boolean, Optional ByVal TryForceRecycle As Boolean = False)
            Dim CurrentThumbnailCount As Integer = ThumbnailCount
            Dim OldestIndex As Integer = _mruList(0).NextIndex

            'If there's room for another, then add it to the end
            '  Note that we will allow the imagelist to grow until it has reached both 
            '  MinimumSizeBeforeRecycling and MaximumSuggestedCacheSize.
            If (TryForceRecycle AndAlso ThumbnailCount >= _minimumSizeBeforeRecycling) OrElse ThumbnailCount < EffectiveMaximumSuggestedCacheSize OrElse OldestIndex <= 0 Then
                InsertAtEnd = True
                Index = CurrentThumbnailCount + _reservedImagesCount
            Else
                'Otherwise we've reached our limit and need to recycle an old position
                InsertAtEnd = False
                Index = OldestIndex - 1

                ' take out of the queue...
                _mruList(_mruList(OldestIndex).NextIndex).PreviousIndex = 0
                _mruList(0).NextIndex = _mruList(OldestIndex).NextIndex
                _mruList(OldestIndex).NextIndex = 0
                Debug.Assert(_mruList(OldestIndex).PreviousIndex = 0)

                'We are removing an old thumbnail entry, so we need to remove its key
                If _mruList(OldestIndex).Key IsNot Nothing Then
                    _keys.Remove(_mruList(OldestIndex).Key)
                    _mruList(OldestIndex).Key = Nothing
                End If
            End If

            Debug.Assert(InsertAtEnd = (Index = ThumbnailCount + _reservedImagesCount))
            Debug.Assert(InsertAtEnd OrElse Index < ThumbnailCount + _reservedImagesCount, "Trying to return an image index that's not in the imagelist")
            Debug.Assert(Not InsertAtEnd OrElse Index - _reservedImagesCount < EffectiveMaximumSuggestedCacheSize, _
                "Trying to add an image to the end of the list past the suggested max cache size")
        End Sub


        ''' <summary>
        ''' Removes a specific key from the cache, given its key and index (the key must exist in the cache at 
        '''   that index).
        ''' </summary>
        ''' <param name="Key">Key to be removed.</param>
        ''' <param name="Index">Index at which that key is currently found.</param>
        ''' <remarks></remarks>
        Private Sub RemoveKey(ByVal Key As Object, ByVal Index As Integer)
            ' NOTE: It could be a reserved item (shared icon), in this case, it is not in the MRU list, and we shouldn't update the LIST.
            If IsInMruList(Index) Then
                UpdateMruList(Index, Nothing, False)
            End If

            'Now remove the key from the list of keys.
            Debug.Assert(_keys.ContainsKey(Key), "Couldn't find key to remove")
            _keys.Remove(Key)

            DebugCheckQueueInvariant()
        End Sub

        ''' <summary>
        '''  Update MRU list, and either put the item as the last item in the queue, or the first item in the queue (in case it expired.)
        ''' </summary>
        ''' <param name="Index">Index in the ImageList.</param>
        ''' <param name="Key">Key to be removed.</param>
        ''' <param name="BeLastestItem">It should be the last item in the MRU list, otherwise, it will be the first item, and will be recycle soon.</param>
        Private Sub UpdateMruList(ByVal Index As Integer, ByVal Key As Object, ByVal BeLastestItem As Boolean)
            Dim MruIndex As Integer = Index + 1

            ' Check whether we need grow the size of the MRU table...
            If MruIndex >= _mruList.Length Then
                Dim newLength As Integer = Math.Max(MruIndex, Math.Min(MruIndex * 2, _maximumSuggestedCacheSize + _reservedImagesCount))
                ReDim Preserve _mruList(newLength)
            End If

            _mruList(MruIndex).Key = Key
            If IsInMruList(Index) Then
                ' It is an item in the list...
                If (BeLastestItem AndAlso _mruList(0).PreviousIndex = MruIndex) OrElse (Not BeLastestItem AndAlso _mruList(0).NextIndex = MruIndex) Then
                    ' already in the position
                    Return
                End If

                ' take out from the current position
                _mruList(_mruList(MruIndex).NextIndex).PreviousIndex = _mruList(MruIndex).PreviousIndex
                _mruList(_mruList(MruIndex).PreviousIndex).NextIndex = _mruList(MruIndex).NextIndex
            End If

            ' insert it to the right position...
            If BeLastestItem Then
                _mruList(MruIndex).NextIndex = 0
                _mruList(MruIndex).PreviousIndex = _mruList(0).PreviousIndex
                _mruList(_mruList(0).PreviousIndex).NextIndex = MruIndex
                _mruList(0).PreviousIndex = MruIndex
            Else
                _mruList(MruIndex).PreviousIndex = 0
                _mruList(MruIndex).NextIndex = _mruList(0).NextIndex
                _mruList(_mruList(0).NextIndex).PreviousIndex = MruIndex
                _mruList(0).NextIndex = MruIndex
            End If
        End Sub

        ''' <summary>
        ''' Check whether an item is in the MRU list
        ''' </summary>
        ''' <param name="Index">Index in the ImageList.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function IsInMruList(ByVal Index As Integer) As Boolean
            Dim MruIndex As Integer = Index + 1
            If MruIndex >= _mruList.Length Then
                Return False
            Else If (_mruList(MruIndex).PreviousIndex = 0 AndAlso _mruList(0).NextIndex <> MruIndex) Then
                Debug.Assert(_mruList(MruIndex).NextIndex = 0)
                Return False
            End If
            Return True
        End Function

        ''' <summary>
        ''' Debug code that checks the integrity of the cache's data structures.
        ''' </summary>
        ''' <remarks></remarks>
        <Conditional("DEBUG")> _
        Private Sub DebugCheckQueueInvariant()
            Dim count As Integer = 0
            Dim prev As Integer = _mruList(0).PreviousIndex
            Dim i As Integer = 0
            Do
                Debug.Assert(_mruList(i).PreviousIndex = prev)
                If _mruList(i).Key IsNot Nothing Then
                    Debug.Assert(_keys.Contains(_mruList(i).Key))
                End If

                count = count + 1
                Debug.Assert(count < _mruList.Length)

                prev = i
                i = _mruList(i).NextIndex
            Loop While i <> 0

            For j As Integer = 0 To _mruList.Length - 2
                If Not IsInMruList(j) Then
                    count = count + 1
                End If
            Next

            Debug.Assert(count = _mruList.Length)
        End Sub


        ''' <summary>
        '''  MruListItem:
        '''   We use an array of this structure to implement a MRU list...
        ''' </summary>
        Private Structure MruListItem
            Public PreviousIndex As Integer        ' Index of the previous item in the list
            Public NextIndex As Integer            ' Index of the next item in the list
            Public Key As Object                   ' Key object we cached
        End Structure

    End Class

End Namespace

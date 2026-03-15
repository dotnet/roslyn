' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains the definition of the Scanner, which produces tokens from text 
'-----------------------------------------------------------------------------

Imports System.Text
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Scanner
        ''' <summary>
        ''' page represents a cached array of chars.
        ''' </summary>
        Private Class Page
            ''' <summary>
            ''' where page maps in the stream. Used to validate pages
            ''' </summary>
            Friend _pageStart As Integer

            ''' <summary>
            ''' page's buffer
            ''' </summary>
            Friend ReadOnly _arr As Char()

            Private ReadOnly _pool As ObjectPool(Of Page)
            Private Sub New(pool As ObjectPool(Of Page))
                _pageStart = -1
                _arr = New Char(s_PAGE_SIZE - 1) {}
                _pool = pool
            End Sub

            Friend Sub Free()
                _pageStart = -1
                _pool.Free(Me)
            End Sub

            Private Shared ReadOnly s_poolInstance As ObjectPool(Of Page) = CreatePool()
            Private Shared Function CreatePool() As ObjectPool(Of Page)
                Dim pool As ObjectPool(Of Page) = Nothing
                pool = New ObjectPool(Of Page)(Function() New Page(pool), 128)
                Return pool
            End Function
            Friend Shared Function GetInstance() As Page
                Dim instance = s_poolInstance.Allocate()
                Return instance
            End Function
        End Class

        ''' <summary>
        ''' current page we are reading.
        ''' </summary>
        Private _curPage As Page
        Private ReadOnly _pages(s_PAGE_NUM - 1) As Page

        Private Const s_PAGE_NUM_SHIFT = 2
        Private Const s_PAGE_NUM = CInt(2 ^ s_PAGE_NUM_SHIFT)
        Private Const s_PAGE_NUM_MASK = s_PAGE_NUM - 1

        Private Const s_PAGE_SHIFT = 11
        Private Const s_PAGE_SIZE = CInt(2 ^ s_PAGE_SHIFT)
        Private Const s_PAGE_MASK = s_PAGE_SIZE - 1
        Private Const s_NOT_PAGE_MASK = Not s_PAGE_MASK

        Private ReadOnly _buffer As SourceText
        Private ReadOnly _bufferLen As Integer

        ' created on demand. we may not need it
        Private _builder As StringBuilder

        ''' <summary>
        ''' gets a page for the position.
        ''' will initialize it if we have cache miss
        ''' </summary>
        Private Function GetPage(position As Integer) As Page
            Dim pageNum = (position >> s_PAGE_SHIFT) And s_PAGE_NUM_MASK

            Dim p = _pages(pageNum)
            Dim pageStart = position And s_NOT_PAGE_MASK

            If p Is Nothing Then
                p = Page.GetInstance
                _pages(pageNum) = p
            End If

            If p._pageStart <> pageStart Then
                _buffer.CopyTo(pageStart, p._arr, Math.Min(_bufferLen - pageStart, s_PAGE_SIZE))
                p._pageStart = pageStart
            End If

            _curPage = p
            Return p
        End Function

        ' PERF CRITICAL
        Private Function Peek(skip As Integer) As Char
            Debug.Assert(CanGet(skip))
            Debug.Assert(skip >= -MaxCharsLookBehind)

            Dim position = _lineBufferOffset
            Dim page = _curPage
            position += skip

            Dim ch = page._arr(position And s_PAGE_MASK)

            Dim start = page._pageStart
            Dim expectedStart = position And s_NOT_PAGE_MASK

            If start <> expectedStart Then
                page = GetPage(position)
                ch = page._arr(position And s_PAGE_MASK)
            End If

            Return ch
        End Function

        ' PERF CRITICAL
        Friend Function Peek() As Char
            Dim page = _curPage
            Dim position = _lineBufferOffset
            Dim ch = page._arr(position And s_PAGE_MASK)

            Dim start = page._pageStart
            Dim expectedStart = position And s_NOT_PAGE_MASK

            If start <> expectedStart Then
                page = GetPage(position)
                ch = page._arr(position And s_PAGE_MASK)
            End If

            Return ch
        End Function

        Friend Function GetChar() As String
            Return Intern(Peek())
        End Function

        Friend Function GetText(start As Integer, length As Integer) As String
            Dim page = _curPage
            Dim offsetInPage = start And s_PAGE_MASK

            If page._pageStart = (start And s_NOT_PAGE_MASK) AndAlso
                offsetInPage + length < s_PAGE_SIZE Then

                Return Intern(page._arr, offsetInPage, length)
            End If
            Return GetTextSlow(start, length)
        End Function

        Friend Function GetTextNotInterned(start As Integer, length As Integer) As String
            Dim page = _curPage
            Dim offsetInPage = start And s_PAGE_MASK

            If page._pageStart = (start And s_NOT_PAGE_MASK) AndAlso
                offsetInPage + length < s_PAGE_SIZE Then
                Dim arr() As Char = page._arr

                ' Always intern CR+LF since it occurs so frequently
                If length = 2 AndAlso arr(offsetInPage) = ChrW(13) AndAlso arr(offsetInPage + 1) = ChrW(10) Then
                    Return vbCrLf
                End If

                Return New String(arr, offsetInPage, length)
            End If
            Return GetTextSlow(start, length, suppressInterning:=True)
        End Function

        Private Function GetTextSlow(start As Integer, length As Integer, Optional suppressInterning As Boolean = False) As String
            Dim textOffset = start And s_PAGE_MASK

            Dim page = GetPage(start)
            If textOffset + length < s_PAGE_SIZE Then
                If suppressInterning Then
                    Return New String(page._arr, textOffset, length)
                Else
                    Return Intern(page._arr, textOffset, length)
                End If
            End If

            ' make a string builder that is big enough, but not too big
            If _builder Is Nothing Then
                _builder = New StringBuilder(Math.Min(length, 1024))
            End If

            Dim cnt = Math.Min(length, s_PAGE_SIZE - textOffset)
            _builder.Append(page._arr, textOffset, cnt)

            Dim dst = cnt
            length -= cnt
            start += cnt

            Do
                page = GetPage(start)
                cnt = Math.Min(length, s_PAGE_SIZE)
                _builder.Append(page._arr, 0, cnt)
                dst += cnt
                length -= cnt
                start += cnt
            Loop While length > 0

            Dim result As String
            If suppressInterning Then
                result = _builder.ToString
            Else
                result = _stringTable.Add(_builder)
            End If
            If result.Length < 1024 Then
                _builder.Clear()
            Else
                _builder = Nothing
            End If
            Return result
        End Function
    End Class
End Namespace

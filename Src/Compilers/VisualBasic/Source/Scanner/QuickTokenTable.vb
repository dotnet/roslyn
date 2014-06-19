'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'
' Contains the quick token table.
'-----------------------------------------------------------------------------

Option Compare Binary
Option Strict On

Imports System.Text
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Compilers.VisualBasic.InternalSyntax
    Friend Class QuickTokenTable

        ''' <summary>
        ''' Matches token with its spelling.
        ''' Used in shared token cache.
        ''' </summary>
        Private Class Entry
            Public ReadOnly text As String
            Public ReadOnly token As SyntaxToken

            Public Sub New(text As String, token As SyntaxToken)
                Debug.Assert(text.Length = token.FullWidth)
                Me.text = text
                Me.token = token
            End Sub
        End Class

        Private Structure LocalEntry
            Public hashCode As Integer
            Public len As Integer
            Public text As String
            Public token As SyntaxToken
        End Structure

        Private Structure SharedEntry
            Public hashCode As Integer
            Public entry As Entry
        End Structure

        ' Size of local cache.
        Private Const LOCAL_BITS As Integer = 11
        Private Const LOCAL_SIZE = (1 << LOCAL_BITS) - 1

        ' max size of shared cache.
        Private Const SHARED_SIZE = (1 << 16) - 1

        Private Const BUCKET_BITS As Integer = 6
        Private Const BUCKET_SIZE As Integer = (1 << BUCKET_BITS) - 1

        ' shared cache
        Private Shared _sharedTable As SharedEntry() = New SharedEntry(SHARED_SIZE + BUCKET_SIZE) {}

        ' local cache
        Private _localTable As LocalEntry() = New LocalEntry(LOCAL_SIZE) {}
        Private _random As Random

        Friend Function FindToken(charArray As Char(), start As Integer, len As Integer, hashCode As Integer) As SyntaxToken
            Dim arr = _localTable         ' capture array to avoid extra range checks
            Dim idx = hashCode And LOCAL_SIZE

            If arr(idx).hashCode = hashCode AndAlso arr(idx).len = len Then
                If EqualText(arr(idx).text, charArray, start) Then
                    Return arr(idx).token
                End If
            End If

            Dim e = FindSharedEntry(charArray, start, len, hashCode)
            If e IsNot Nothing Then
                arr(idx).hashCode = hashCode
                arr(idx).len = len
                arr(idx).text = e.text

                Dim tk = e.token
                arr(idx).token = tk
                Return tk
            End If

            Return Nothing
        End Function

        Friend Sub AddToken(charArray As Char(), start As Integer, len As Integer, hashCode As Integer, token As SyntaxToken)
            Debug.Assert(token.FullWidth = len AndAlso String.Equals(New String(charArray, start, len), token.ToFullString))

            Dim arr = _localTable
            Dim idx = hashCode And LOCAL_SIZE

            Dim text = New String(charArray, start, len)
            Dim e = New Entry(text, token)
            AddSharedEntry(hashCode, e)

            arr(idx).hashCode = hashCode
            arr(idx).len = len
            arr(idx).text = text
            arr(idx).token = token
        End Sub

        Private Function FindSharedEntry(charArray As Char(), start As Integer, len As Integer, hashCode As Integer) As Entry
            Dim arr = _sharedTable
            Dim idx = SharedIdxFromHash(hashCode)

            Dim e As Entry = Nothing
            For i = idx To idx + BUCKET_SIZE
                Dim hash = arr(i).hashCode
                If hash = hashCode Then
                    e = arr(i).entry

                    ' need a memory fence here to make sure e loads after its fields.
                    Threading.Thread.MemoryBarrier()

                    ' need to check for nothing in case hash stores before entry does (very unlikely)
                    If e IsNot Nothing Then
                        If EqualText(e.text, charArray, start, len) Then
                            Exit For
                        End If

                        ' this is not our e
                        e = Nothing
                    End If
                End If

                If hash = 0 Then
                    Exit For
                End If
            Next
            Return e
        End Function

        Private Function Rnd() As Integer
            Dim r = _random
            If r Is Nothing Then
                r = New Random
                _random = r
            End If
            Return r.Next
        End Function

        Private Sub AddSharedEntry(hashCode As Integer, e As Entry)
            Dim arr = _sharedTable
            Dim idx = SharedIdxFromHash(hashCode)

            For i = 0 To BUCKET_SIZE
                Dim curIdx = idx + i
                Dim dstHash = arr(curIdx).hashCode

                If dstHash = 0 Then
                    arr(curIdx).entry = e
                    arr(curIdx).hashCode = hashCode
                    Exit Sub
                End If
            Next

            Dim r = Rnd() And BUCKET_SIZE
            idx = idx + r
            arr(idx).entry = e
            arr(idx).hashCode = hashCode
        End Sub

        Private Shared Function SharedIdxFromHash(hash As Integer) As Integer
            Return (hash >> LOCAL_BITS) And SHARED_SIZE
        End Function

        ''' <summary>
        ''' Compare a string and a char array/length for exact equality.
        ''' </summary>
        Private Shared Function EqualText(text As String, charArray As Char(), start As Integer, len As Integer) As Boolean
            If text.Length <> len Then
                Return False
            End If

            'TODO: workaround for range check hoisting bug
            ' <<< FOR LOOP
            Dim i As Integer = 0
            GoTo enter
            Do
                If text(i) <> charArray(i + start) Then
                    Return False
                End If
                i += 1
enter:
            Loop While i < text.Length
            ' >>> FOR LOOP

            Return True
        End Function

        Private Shared Function EqualText(text As String, charArray As Char(), start As Integer) As Boolean
            'TODO: workaround for range check hoisting bug
            ' <<< FOR LOOP
            Dim i As Integer = 0
            GoTo enter
            Do
                If text(i) <> charArray(i + start) Then
                    Return False
                End If
                i += 1
enter:
            Loop While i < text.Length
            ' >>> FOR LOOP

            Return True
        End Function
    End Class
End Namespace

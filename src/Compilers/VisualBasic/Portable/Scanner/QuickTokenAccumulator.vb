' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------
' Contains quick token accumulator.
'-----------------------------------------------------------------------------

Option Compare Binary
Option Strict On

Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    '' The QuickTokenAccumulator is a small mini-tokenizer that may fail. It consumes characters and
    '' eventually either decides that either it found a complete token (including the trivia on either
    '' side), or it gives up and says that the full scanner should take an attempt. It also accumulates the 
    '' token into a character buffer and computes a hash code. The entire tokenization is done by a single
    '' routine without any memory allocations to keep it very fast.
    '' 
    '' Currently it only handles two cases:
    ''     optional-whitespace keyword-or-identifier optional-whitespace
    ''     optional-whitespace single-char-punctuation optional-whitespace
    '' 
    '' where the whitespace does not include newlines.
    ' 
    '' The VB tokenization rules are complex, and care needs to be taken in constructing the quick
    '' tokenization. For example "REM" begins a comment, so it can't be tokenized as a keyword or identifier.
    '' Similar problems arise with multi-character punctuation tokens, which can have embedded spaces.
    Partial Friend Class Scanner
        ''' <summary>
        ''' The possible states that the mini scanning can be in.
        ''' </summary>
        Private Enum AccumulatorState
            Initial
            InitialAllowLeadingMultilineTrivia
            Ident
            TypeChar
            FollowingWhite
            Punctuation
            CompoundPunctStart
            CR
            Done
            Bad
        End Enum

        ' Flags used to classify characters.
        <Flags()>
        Private Enum CharFlags As UShort
            White = 1 << 0   ' simple whitespace (space/tab)
            Letter = 1 << 1    ' letter, except for "R" (because of REM) and "_"
            IdentOnly = 1 << 2  ' allowed only in identifiers (cannot start one) - letter "R" (because of REM), "_"
            TypeChar = 1 << 3  ' legal type character (except !, which is contextually dictionary lookup
            Punct = 1 << 4     ' some simple punctuation (parens, braces, dot, comma, equals, question)
            CompoundPunctStart = 1 << 5 ' may be a part of compound punctuation. will be used only if followed by (not white) && (not punct)
            CR = 1 << 6  ' CR
            LF = 1 << 7  ' LF
            Digit = 1 << 8    ' digit 0-9
            Complex = 1 << 9  ' complex - causes scanning to abort
        End Enum

        'TODO: why : and ; are complex?  (8th row, 3 and 4)

        ' The following table classifies the first &H180 Unicode characters. 
        ' R and r are marked as COMPLEX so that quick-scanning doesn't stop after "REM".
        ' # is marked complex as it may start directives.
        ' < = > are complex because they might start a merge conflict marker.
        ' PERF: Use UShort instead of CharFlags so the compiler can use array literal initialization.
        '       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        Private Shared ReadOnly s_charProperties As UShort() = {
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.White, CharFlags.LF, CharFlags.Complex, CharFlags.Complex, CharFlags.CR, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, _
                                                                                                                                                                    _
            CharFlags.White, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.TypeChar, CharFlags.TypeChar, CharFlags.TypeChar, CharFlags.Complex,
            CharFlags.Punct, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.CompoundPunctStart, CharFlags.Punct, CharFlags.CompoundPunctStart,
            CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit, CharFlags.Digit,
            CharFlags.Digit, CharFlags.Digit, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Punct, _
                                                                                                                                                              _
            CharFlags.TypeChar, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.IdentOnly, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex, CharFlags.CompoundPunctStart, CharFlags.Complex, CharFlags.CompoundPunctStart, CharFlags.IdentOnly, _
                                                                                                                                                                                         _
            CharFlags.Complex, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.IdentOnly, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Punct, CharFlags.Complex, CharFlags.Punct, CharFlags.Complex, CharFlags.Complex,
                                                                                                                                                            _
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, _
                                                                                                                                                                    _
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex,
            CharFlags.Complex, CharFlags.Complex, CharFlags.Letter, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, CharFlags.Complex, _
                                                                                                                                                                   _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, _
                                                                                                                                                            _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Complex,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, _
                                                                                                                                                            _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, _
                                                                                                                                                            _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, _
                                                                                                                                                            _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, _
                                                                                                                                                            _
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter,
            CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter, CharFlags.Letter}

        ' Size of the above table.
        Private Const s_CHARPROP_LENGTH = &H180

        ' Maximum length of a token to scan
        Friend Const MAX_CACHED_TOKENSIZE = 42

        Shared Sub New()
            Debug.Assert(s_charProperties.Length = s_CHARPROP_LENGTH)
        End Sub

        Public Structure QuickScanResult
            Public Sub New(start As Integer, length As Integer, chars As Char(), hashCode As Integer, terminatorLength As Byte)
                Me.Start = start
                Me.Length = length
                Me.Chars = chars
                Me.HashCode = hashCode
                Me.TerminatorLength = terminatorLength
            End Sub

            Public ReadOnly Chars As Char()
            Public ReadOnly Start As Integer
            Public ReadOnly Length As Integer
            Public ReadOnly HashCode As Integer
            Public ReadOnly TerminatorLength As Byte

            Public ReadOnly Property Succeeded As Boolean
                Get
                    Return Me.Length > 0
                End Get
            End Property
        End Structure

        ' Attempt to scan a single token.
        ' If it succeeds, return True, and the characters, length, and hashcode of the token
        ' can be retrieved by other functions. 
        ' If it fails (the token is too complex), return False.
        Public Function QuickScanToken(allowLeadingMultilineTrivia As Boolean) As QuickScanResult
            Dim state As AccumulatorState = If(allowLeadingMultilineTrivia, AccumulatorState.InitialAllowLeadingMultilineTrivia, AccumulatorState.Initial)

            Dim offset = _lineBufferOffset
            Dim page = _curPage
            If page Is Nothing OrElse
                page._pageStart <> (offset And s_NOT_PAGE_MASK) Then

                page = GetPage(offset)
            End If

            Dim pageArr As Char() = page._arr
            Dim qtChars = pageArr

            Dim index = _lineBufferOffset And s_PAGE_MASK
            Dim qtStart = index

            Dim limit = index + Math.Min(MAX_CACHED_TOKENSIZE, _bufferLen - offset)
            limit = Math.Min(limit, pageArr.Length)

            Dim hashCode As Integer = Hash.FnvOffsetBias
            Dim terminatorLength As Byte = 0
            Dim unicodeValue As Integer = 0

            While index < limit
                ' Get current character.
                Dim c = pageArr(index)

                ' Get the flags for that character.
                unicodeValue = AscW(c)

                If unicodeValue >= s_CHARPROP_LENGTH Then
                    Exit While
                End If

                Dim flags = s_charProperties(unicodeValue)

                ' Advance the scanner state.
                Select Case state
                    Case AccumulatorState.InitialAllowLeadingMultilineTrivia
                        If flags = CharFlags.Letter Then
                            state = AccumulatorState.Ident
                        ElseIf flags = CharFlags.Punct Then
                            state = AccumulatorState.Punctuation
                        ElseIf flags = CharFlags.CompoundPunctStart Then
                            state = AccumulatorState.CompoundPunctStart
                        ElseIf (flags And (CharFlags.White Or CharFlags.CR Or CharFlags.LF)) <> 0 Then
                            ' stay in AccumulatorState.InitialNewStatement
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.Initial
                        If flags = CharFlags.Letter Then
                            state = AccumulatorState.Ident
                        ElseIf flags = CharFlags.Punct Then
                            state = AccumulatorState.Punctuation
                        ElseIf flags = CharFlags.CompoundPunctStart Then
                            state = AccumulatorState.CompoundPunctStart
                        ElseIf flags = CharFlags.White Then
                            ' stay in AccumulatorState.Initial
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.Ident
                        If (flags And (CharFlags.Letter Or CharFlags.IdentOnly Or CharFlags.Digit)) <> 0 Then
                            ' stay in Ident
                        ElseIf flags = CharFlags.White Then
                            state = AccumulatorState.FollowingWhite
                        ElseIf flags = CharFlags.CR Then
                            state = AccumulatorState.CR
                        ElseIf flags = CharFlags.LF Then
                            terminatorLength = 1
                            state = AccumulatorState.Done
                            Exit While
                        ElseIf flags = CharFlags.TypeChar Then
                            state = AccumulatorState.TypeChar
                        ElseIf flags = CharFlags.Punct Then
                            state = AccumulatorState.Done
                            Exit While
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.TypeChar
                        If flags = CharFlags.White Then
                            state = AccumulatorState.FollowingWhite
                        ElseIf flags = CharFlags.CR Then
                            state = AccumulatorState.CR
                        ElseIf flags = CharFlags.LF Then
                            terminatorLength = 1
                            state = AccumulatorState.Done
                            Exit While
                        ElseIf (flags And (CharFlags.Punct Or CharFlags.Digit Or CharFlags.TypeChar)) <> 0 Then
                            state = AccumulatorState.Done
                            Exit While
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.FollowingWhite
                        If flags = CharFlags.White Then
                            ' stay in FollowingWhite
                        ElseIf flags = CharFlags.CR Then
                            state = AccumulatorState.CR
                        ElseIf flags = CharFlags.LF Then
                            terminatorLength = 1
                            state = AccumulatorState.Done
                            Exit While
                        ElseIf (flags And (CharFlags.Complex Or CharFlags.IdentOnly)) <> 0 Then
                            state = AccumulatorState.Bad
                            Exit While
                        Else
                            state = AccumulatorState.Done
                            Exit While
                        End If

                    Case AccumulatorState.Punctuation
                        If flags = CharFlags.White Then
                            state = AccumulatorState.FollowingWhite
                        ElseIf flags = CharFlags.CR Then
                            state = AccumulatorState.CR
                        ElseIf flags = CharFlags.LF Then
                            terminatorLength = 1
                            state = AccumulatorState.Done
                            Exit While
                        ElseIf (flags And (CharFlags.Letter Or CharFlags.Punct)) <> 0 Then
                            state = AccumulatorState.Done
                            Exit While
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.CompoundPunctStart
                        If flags = CharFlags.White Then
                            ' stay in CompoundPunctStart
                        ElseIf (flags And (CharFlags.Letter Or CharFlags.Digit)) <> 0 Then
                            state = AccumulatorState.Done
                            Exit While
                        Else
                            state = AccumulatorState.Bad
                            Exit While
                        End If

                    Case AccumulatorState.CR
                        If flags = CharFlags.LF Then
                            terminatorLength = 2
                            state = AccumulatorState.Done
                            Exit While
                        Else
                            state = AccumulatorState.Bad
                        End If
                        Exit While
                    Case Else
                        Debug.Assert(False, "should not get here")
                End Select

                index += 1

                'FNV-like hash should work here 
                'since these strings are short and mostly ASCII
                hashCode = (hashCode Xor unicodeValue) * Hash.FnvPrime
            End While

            If state = AccumulatorState.Done AndAlso (terminatorLength = 0 OrElse Not Me._IsScanningXmlDoc) Then
                If terminatorLength <> 0 Then
                    index += 1
                    hashCode = (hashCode Xor unicodeValue) * Hash.FnvPrime
                End If
                Return New QuickScanResult(qtStart, index - qtStart, qtChars, hashCode, terminatorLength)
            Else
                Return Nothing
            End If
        End Function
    End Class
End Namespace

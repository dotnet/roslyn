Imports System.Globalization
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A class that encapsulates the VB case-insensitive identifier comparison rules.
    ''' </summary>
    Public NotInheritable Class IdentifierComparison

        ''' <summary>
        ''' This class seeks to perform one-to-one lowercase Unicode case 
        ''' mappings, which should be culture invariant.
        ''' </summary>
        ''' <remarks></remarks>
        Private NotInheritable Class OneToOneUnicodeComparer
            Inherits StringComparer

            ' PERF: Grab the TextInfo for the invariant culture since this will be accessed very frequently
            Private Shared invariantCultureTextInfo As TextInfo = CultureInfo.InvariantCulture.TextInfo

            ''' <summary>
            ''' ToLower implements the one-to-one Unicode lowercase mapping
            ''' as descriped in ftp://ftp.unicode.org/Public/UNIDATA/UnicodeData.txt.
            ''' The VB spec states that these mappings are used for case-insensitive
            ''' comparison
            ''' </summary>
            ''' <param name="c"></param>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Private Shared Function ToLower(c As Char) As Char
                Dim newChar As Char
                Select Case c
                    Case ChrW(&H130)
                        newChar = "i"c
                    Case Else
                        newChar = invariantCultureTextInfo.ToLower(c)
                End Select
                Return newChar
            End Function

            Public Overrides Function Compare(str1 As String, str2 As String) As Integer
                If str1 Is Nothing Then
                    If str2 Is Nothing Then
                        Return 0
                    Else
                        Return -1
                    End If
                ElseIf str2 Is Nothing Then
                    Return 1
                End If

                For i As Integer = 0 To Math.Min(str1.Length, str2.Length) - 1
                    Dim ordDiff As Integer = AscW(ToLower(str1(i))) - AscW(ToLower(str2(i)))
                    If ordDiff <> 0 Then
                        Return ordDiff
                    End If
                Next

                ' Return the smaller string, or 0 if they are equal in length
                Return str1.Length - str2.Length
            End Function

            Public Overrides Function Equals(str1 As String, str2 As String) As Boolean
                If str1 Is str2 Then
                    Return True
                End If

                If str1 Is Nothing Or str2 Is Nothing Then
                    Return False
                End If

                If str1.Length <> str2.Length Then
                    Return False
                End If

                For i As Integer = 0 To str1.Length - 1
                    If ToLower(str1(i)) <> ToLower(str2(i)) Then
                        Return False
                    End If
                Next
                Return True
            End Function

            Public Function EndsWith(value As String, possibleEnd As String) As Boolean
                If value Is possibleEnd Then
                    Return True
                End If

                If value Is Nothing Or possibleEnd Is Nothing Then
                    Return False
                End If

                Dim i As Integer = value.Length - 1
                Dim j As Integer = possibleEnd.Length - 1

                If i < j Then
                    Return False
                End If

                While j >= 0
                    If ToLower(value(i)) <> ToLower(possibleEnd(j)) Then
                        Return False
                    End If

                    i -= 1
                    j -= 1
                End While

                Return True
            End Function

            Public Overrides Function GetHashCode(str As String) As Integer
                Dim hashCode As Integer = 0
                If str IsNot Nothing Then
                    For i As Integer = 0 To str.Length - 1
                        hashCode = Hash.Combine(hashCode, ToLower(str(i)).GetHashCode())
                    Next
                End If
                Return hashCode
            End Function
        End Class

        ''' <summary>
        ''' Returns a StringComparer that compares strings according the VB identifier comparison rules.
        ''' </summary>
        Private Shared ReadOnly m_Comparer As OneToOneUnicodeComparer = New OneToOneUnicodeComparer()

        ''' <summary>
        ''' Returns a StringComparer that compares strings according the VB identifier comparison rules.
        ''' </summary>
        Public Shared ReadOnly Property Comparer As StringComparer
            Get
                Return m_Comparer
            End Get
        End Property

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Determines if two VB identifiers are equal according to the VB identifier comparison rules.
        ''' </summary>
        ''' <param name="left">First identifier to compare</param>
        ''' <param name="right">Second identifier to compare</param>
        ''' <returns>True if the identifiers should be considered the same.</returns>
        Public Shared Shadows Function Equals(left As String, right As String) As Boolean
            Return m_Comparer.Equals(left, right)
        End Function

        Public Shared Shadows Function EndsWith(ident1 As String, ident2 As String) As Boolean
            Return m_Comparer.EndsWith(ident1, ident2)
        End Function

        ''' <summary>
        ''' Compares two VB identifiers according to the VB identifier comparison rules.
        ''' </summary>
        ''' <param name="left">First identifier to compare</param>
        ''' <param name="right">Second identifier to compare</param>
        ''' <returns>-1 if ident1 &lt; ident2, 1 if ident1 &gt; ident2, 0 if they are equal.</returns>
        Public Shared Function Compare(left As String, right As String) As Integer
            Return m_Comparer.Compare(left, right)
        End Function

        ''' <summary>
        ''' Gets a case-insensitive hash code for VB identifiers.
        ''' </summary>
        ''' <param name="ident">identifier to get the hash code for</param>
        ''' <returns>The hash code for the given identifier</returns>
        Public Shared Shadows Function GetHashCode(ident As String) As Integer
            Debug.Assert(ident IsNot Nothing)

            Return m_Comparer.GetHashCode(ident)
        End Function
    End Class
End Namespace
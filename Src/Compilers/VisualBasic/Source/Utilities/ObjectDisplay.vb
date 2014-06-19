Imports System
Imports System.Globalization
Imports System.Text
Imports Roslyn.Compilers.Collections
Imports Roslyn.Utilities

Namespace Roslyn.Compilers.VisualBasic
    Friend Module ObjectDisplay
        Public Function FormatPrimitive(obj As Object, quoteStrings As Boolean, useHexadecimalNumbers As Boolean) As String
            If obj Is Nothing Then
                Return NullLiteral
            End If

            If TypeOf obj Is Boolean Then
                Return FormatLiteral(DirectCast(obj, Boolean))
            End If

            Dim str As String = TryCast(obj, String)
            If str IsNot Nothing Then
                Return FormatLiteral(str, quoteStrings)
            End If

            If TypeOf obj Is Char Then
                Return FormatLiteral(DirectCast(obj, Char), quoteStrings, useHexadecimalNumbers)
            End If

            If TypeOf obj Is SByte Then
                Return FormatLiteral(DirectCast(obj, SByte), useHexadecimalNumbers)
            End If

            If TypeOf obj Is Byte Then
                Return FormatLiteral(DirectCast(obj, Byte), useHexadecimalNumbers)
            End If

            If TypeOf obj Is Short Then
                Return FormatLiteral(DirectCast(obj, Short), useHexadecimalNumbers)
            End If

            If TypeOf obj Is UShort Then
                Return FormatLiteral(DirectCast(obj, UShort), useHexadecimalNumbers)
            End If

            If TypeOf obj Is Integer Then
                Return FormatLiteral(DirectCast(obj, Integer), useHexadecimalNumbers)
            End If

            If TypeOf obj Is UInteger Then
                Return FormatLiteral(DirectCast(obj, UInteger), useHexadecimalNumbers)
            End If

            If TypeOf obj Is Long Then
                Return FormatLiteral(DirectCast(obj, Long), useHexadecimalNumbers)
            End If

            If TypeOf obj Is ULong Then
                Return FormatLiteral(DirectCast(obj, ULong), useHexadecimalNumbers)
            End If

            If TypeOf obj Is Double Then
                Return FormatLiteral(DirectCast(obj, Double))
            End If

            If TypeOf obj Is Single Then
                Return FormatLiteral(DirectCast(obj, Single))
            End If

            If TypeOf obj Is Decimal Then
                Return FormatLiteral(DirectCast(obj, Decimal))
            End If

            If TypeOf obj Is DateTime Then
                Return FormatLiteral(DirectCast(obj, DateTime))
            End If

            Return Nothing
        End Function

        Public ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Public Function FormatLiteral(value As Boolean) As String
            Return If(value, "True", "False")
        End Function

        ''' <summary>
        ''' Formats string literal.
        ''' </summary>
        ''' <param name="value">Literal value.</param>
        ''' <param name="quote">True to double-quote the value. Also enables pretty-printing of non-printable chracters using ChrW function and vb* constants.</param>
        ''' <param name="nonPrintableSubstitute">If specified non-printable characters are replaced by this character.</param>
        ''' <param name="useHexadecimalNumbers">Use hexadecimal numbers as arguments to ChrW functions.</param>
        Public Function FormatLiteral(value As String, Optional quote As Boolean = True, Optional nonPrintableSubstitute As Char = Nothing, Optional useHexadecimalNumbers As Boolean = True) As String
            If value Is Nothing Then
                Throw New ArgumentNullException()
            End If

            Return VbStringDisplay.FormatString(value, quote, nonPrintableSubstitute, useHexadecimalNumbers)
        End Function

        Public Function FormatLiteral(c As Char, quote As Boolean, useHexadecimalNumbers As Boolean) As String
            Dim wellKnown = VbStringDisplay.GetWellKnownCharacterName(c)
            If wellKnown IsNot Nothing Then
                Return wellKnown
            End If

            If Not VbStringDisplay.IsPrintable(c) Then
                Dim codepoint = AscW(c)
                Return If(useHexadecimalNumbers, "ChrW(&H" & codepoint.ToString("X"), "ChrW(" & codepoint.ToString()) & ")"
            End If

            If quote Then
                Return """"c & EscapeQuote(c) & """"c & "c"
            Else
                Return c
            End If
        End Function

        Private Function EscapeQuote(c As Char) As String
            Return If(c = """", """""", c)
        End Function

        Public Function FormatLiteral(value As SByte, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X2"), (CType(value, Integer)).ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As Byte, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & value.ToString("X2")
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As Short, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & (If(value >= 0, value.ToString("X"), (CType(value, Integer)).ToString("X8")))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As UShort, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As Integer, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As UInteger, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X8"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As Long, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X16"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As ULong, useHexadecimalNumbers As Boolean) As String
            If useHexadecimalNumbers Then
                Return "&H" & If(value >= 0, value.ToString("X"), value.ToString("X16"))
            Else
                Return value.ToString(CultureInfo.InvariantCulture)
            End If
        End Function

        Public Function FormatLiteral(value As Double) As String
            Return value.ToString("R", CultureInfo.InvariantCulture)
        End Function

        Public Function FormatLiteral(value As Single) As String
            Return value.ToString("R", CultureInfo.InvariantCulture)
        End Function

        Public Function FormatLiteral(value As Decimal) As String
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        Public Function FormatLiteral(value As DateTime) As String
            Return value.ToString("G", DateTimeCultureInfo)
        End Function

        Private ReadOnly DateTimeCultureInfo As CultureInfo = CultureInfo.CreateSpecificCulture("en-us")
    End Module
End Namespace


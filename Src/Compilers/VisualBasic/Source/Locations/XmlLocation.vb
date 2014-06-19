Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A program location in an XML file.
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable>
    Friend Class XmlLocation
        Inherits Location
        Implements IEquatable(Of XmlLocation)

        Private ReadOnly _positionSpan As FileLinePositionSpan

        Public Sub New(path As String, lineNumber As Integer, columnNumber As Integer)
            Dim start As New LinePosition(lineNumber, columnNumber)
            Dim [end] As New LinePosition(lineNumber, columnNumber + 1)
            Me._positionSpan = New FileLinePositionSpan(path, start, [end])
        End Sub

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.XmlFile
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Return Me._positionSpan
        End Function

        Public Overloads Function Equals(other As XmlLocation) As Boolean Implements IEquatable(Of XmlLocation).Equals
            If Me Is other Then
                Return True
            End If

            Return other IsNot Nothing AndAlso other._positionSpan.Equals(Me._positionSpan)
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return Me.Equals(TryCast(obj, XmlLocation))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me._positionSpan.GetHashCode()
        End Function

    End Class

End Namespace

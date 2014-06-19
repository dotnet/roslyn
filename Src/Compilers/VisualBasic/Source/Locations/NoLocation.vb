Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A class that represents no location at all. Useful for errors in command line options, for example.
    ''' </summary>
    <Serializable()>
    Friend NotInheritable Class NoLocation
        Inherits Location
        Implements IObjectReference

        Public Shared Singleton As New NoLocation

        Private Sub New()
        End Sub

        Function GetRealObject(context As StreamingContext) As Object Implements IObjectReference.GetRealObject
            Return Singleton
        End Function

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.None
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            ' arbitrary number, since all NoLocation's are equal
            Return &H58914756
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Me Is obj
        End Function
    End Class
End Namespace

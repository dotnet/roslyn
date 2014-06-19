Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Common
Imports Microsoft.CodeAnalysis.Common.Semantics
Imports Microsoft.CodeAnalysis.Common.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    <Serializable()>
    Friend NotInheritable Class SerializedLocation
        Inherits Location
        Implements IEquatable(Of SerializedLocation)

        Private ReadOnly _kind As LocationKind

        Private ReadOnly _sourceSpan As TextSpan

        Private ReadOnly _fileSpan As FileLinePositionSpan

        Private ReadOnly _fileSpanUsingDirectives As FileLinePositionSpan

        Private Sub New(info As SerializationInfo, context As StreamingContext)
            _sourceSpan = DirectCast(info.GetValue("sourceSpan", GetType(TextSpan)), TextSpan)
            _fileSpan = DirectCast(info.GetValue("fileSpan", GetType(FileLinePositionSpan)), FileLinePositionSpan)
            _fileSpanUsingDirectives = DirectCast(info.GetValue("fileSpanUsingDirectives", GetType(FileLinePositionSpan)), FileLinePositionSpan)
            _kind = DirectCast(info.GetByte("kind"), LocationKind)
        End Sub

        Friend Shared Shadows Sub GetObjectData(location As CommonLocation, info As SerializationInfo, context As StreamingContext)
            Dim fileSpan = location.GetLineSpan(usePreprocessorDirectives:=False)
            Dim fileSpanUsingDirectives = location.GetLineSpan(usePreprocessorDirectives:=True)
            info.AddValue("sourceSpan", location.SourceSpan, GetType(TextSpan))
            info.AddValue("fileSpan", fileSpan, GetType(FileLinePositionSpan))
            info.AddValue("fileSpanUsingDirectives", fileSpanUsingDirectives, GetType(FileLinePositionSpan))
            info.AddValue("kind", DirectCast(location.Kind, Byte))
        End Sub

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return _kind
            End Get
        End Property

        Public Overrides ReadOnly Property SourceSpan As TextSpan
            Get
                Return _sourceSpan
            End Get
        End Property

        Public Overrides Function GetLineSpan(usePreprocessorDirectives As Boolean) As FileLinePositionSpan
            Return If(usePreprocessorDirectives, _fileSpanUsingDirectives, _fileSpan)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _fileSpan.GetHashCode()
        End Function

        Public Overloads Function Equals(other As SerializedLocation) As Boolean Implements IEquatable(Of Microsoft.CodeAnalysis.VisualBasic.SerializedLocation).Equals
            Return other IsNot Nothing AndAlso _fileSpan.Equals(other._fileSpan)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, SerializedLocation))
        End Function
    End Class
End Namespace



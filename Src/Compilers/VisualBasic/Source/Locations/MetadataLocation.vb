Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' A program location in metadata.
    ''' </summary>
    <Serializable()>
    Friend NotInheritable Class MetadataLocation
        Inherits VBLocation

        Private ReadOnly _module As ModuleSymbol

        Public Sub New([module] As ModuleSymbol)
            Debug.Assert([module] IsNot Nothing)
            _module = [module]
        End Sub

        Public Overrides ReadOnly Property Kind As LocationKind
            Get
                Return LocationKind.MetadataFile
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataModule As IModuleSymbol
            Get
                Return _module
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return _module.GetHashCode()
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, MetadataLocation))
        End Function

        Public Overloads Function Equals(other As MetadataLocation) As Boolean
            Return (other IsNot Nothing) AndAlso (other._module Is Me._module)
        End Function
    End Class
End Namespace

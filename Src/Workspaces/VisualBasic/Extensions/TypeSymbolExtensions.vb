Imports System.Collections.Generic
Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Services.Editor.VisualBasic.Extensions
    Friend Module TypeSymbolExtensions
#If False Then
        <Extension()>
        Public Function GetAllInterfacesIncludingThis([type] As TypeSymbol) As IList(Of NamedTypeSymbol)
            Dim allInterfaces = [type].AllInterfaces
            If TypeOf [type] Is NamedTypeSymbol Then
                Dim namedType = TryCast([type], NamedTypeSymbol)
                If namedType.TypeKind = TypeKind.[Interface] AndAlso Not allInterfaces.Contains(namedType) Then
                    Dim result = New List(Of NamedTypeSymbol)() From {namedType}
                    result.AddRange(allInterfaces.AsEnumerable())
                    Return result
                End If
            End If

            Return allInterfaces.AsList()
        End Function
#End If
    End Module
End Namespace
Imports System.Runtime.CompilerServices
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.Utilities

Namespace Roslyn.Services.Editor.VisualBasic.Extensions
    Friend Module SymbolContentBuilderExtensions
        <Extension()> _
        Public Sub AppendMinimalSymbol(builder As SymbolContentBuilder,
                                       currentSymbol As Symbol,
                                       currentLocation As Location,
                                       semanticModel As SemanticModel,
                                       Optional format As SymbolDisplayFormat = Nothing)
            Dim parts = currentSymbol.ToMinimalDisplayParts(currentLocation, semanticModel, format)
            builder.AddParts(parts)
        End Sub

        <Extension()> _
        Public Sub AppendSymbol(builder As SymbolContentBuilder,
                                currentSymbol As Symbol,
                                Optional format As SymbolDisplayFormat = Nothing)
            Dim parts = currentSymbol.ToDisplayParts(format)
            builder.AddParts(parts)
        End Sub
    End Module
End Namespace

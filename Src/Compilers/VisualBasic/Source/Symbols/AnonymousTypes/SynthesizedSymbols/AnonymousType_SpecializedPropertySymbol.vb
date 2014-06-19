Imports System.Collections.Generic
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private Class AnonymousTypeSpecializedPropertySymbol
            Inherits SubstitutedPropertySymbol

            Private ReadOnly _newName As String
            Private ReadOnly _newLocations As ReadOnlyArray(Of Location)

            Public Sub New(container As AnonymousTypeConstructedTypeSymbol,
                           originalDefinition As PropertySymbol,
                           getMethod As SubstitutedMethodSymbol,
                           setMethod As SubstitutedMethodSymbol,
                           field As AnonymousTypeField)

                MyBase.New(container, originalDefinition, getMethod, setMethod)

                Me._newName = field.Name
                Me._newLocations = ReadOnlyArray(Of Location).CreateFrom(field.Location)
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return _newName
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
                Get
                    Return _newLocations
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxNodes As ReadOnlyArray(Of SyntaxNode)
                Get
                    Return ReadOnlyArray(Of SyntaxNode).Empty
                End Get
            End Property

        End Class

    End Class

End Namespace
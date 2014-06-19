Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousTypeSpecializedPropertyAccessorSymbol
            Inherits SubstitutedMethodSymbol.SpecializedNonGenericMethod

            Public Sub New(container As SubstitutedNamedType, originalDefinition As MethodSymbol)
                MyBase.New(container, originalDefinition)
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return Binder.GetAccessorName(Me.AssociatedPropertyOrEvent.Name, Me.MethodKind)
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ReadOnlyArray(Of Location)
                Get
                    Return Me.AssociatedPropertyOrEvent.Locations
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
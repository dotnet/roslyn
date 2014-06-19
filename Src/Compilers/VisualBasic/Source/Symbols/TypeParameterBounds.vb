Imports System.Diagnostics

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' The effective "bounds" of a type parameter: the constraint types, and
    ''' effective interface set, determined from the declared constraints, with
    ''' any cycles removed. The fields are exposed by the TypeParameterSymbol
    ''' as ConstraintTypes and Interfaces.
    ''' </summary>
    Friend NotInheritable Class TypeParameterBounds
        Public Shared ReadOnly Empty As New TypeParameterBounds(ReadOnlyArray(Of TypeSymbol).Empty, ReadOnlyArray(Of NamedTypeSymbol).Empty)

        Public Sub New(constraintTypes As ReadOnlyArray(Of TypeSymbol), interfaces As ReadOnlyArray(Of NamedTypeSymbol))
            Debug.Assert(constraintTypes.IsNotNull)
            Me.ConstraintTypes = constraintTypes
            Me.Interfaces = interfaces
        End Sub

        ''' <summary>
        ''' The type parameters, classes, and interfaces explicitly declared as
        ''' constraint types on the containing type parameter, with cycles removed.
        ''' </summary>
        Public ReadOnly ConstraintTypes As ReadOnlyArray(Of TypeSymbol)

        ''' <summary>
        ''' The set of interfaces explicitly declared on the containing type
        ''' parameter and any type parameters on which the containing
        ''' type parameter depends, with duplicates removed.
        ''' </summary>
        Public ReadOnly Interfaces As ReadOnlyArray(Of NamedTypeSymbol)
    End Class

End Namespace

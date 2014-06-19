Imports System.Collections.Generic
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract
Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' An RefTypeSymbol represents a ByRef as the type
    ''' of a parameter.
    ''' </summary>
    Public NotInheritable Class ByRefTypeSymbol
        Inherits TypeSymbol

        Private _referencedType As TypeSymbol

        ''' <summary>
        ''' Create a new ByRefTypeSymbol.
        ''' </summary>
        ''' <param name="referencedType">The referenced type.</param>
        Public Sub New(ByVal referencedType As TypeSymbol)
            Requires(referencedType IsNot Nothing)

            _referencedType = referencedType
        End Sub

        ''' <summary>
        ''' Gets the underlying type that is being passed ByRef.
        ''' </summary>
        Public ReadOnly Property ReferencedType As TypeSymbol
            Get
                Return _referencedType
            End Get
        End Property


        Public Overrides ReadOnly Property BaseType As NamedTypeSymbol
            Get
                ' The base type of a ByRef type is the same as the referenced type.
                Return _referencedType.BaseType
            End Get
        End Property

        Public Overrides ReadOnly Property Interfaces As ReadOnlyArray(Of NamedTypeSymbol)
            Get
                ' The interfaces of a ByRef type are the same as the referenced type.
                Return _referencedType.Interfaces
            End Get
        End Property

        Public Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                Return _referencedType.IsReferenceType
            End Get
        End Property

        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                Return _referencedType.IsValueType
            End Get
        End Property

        Public Overrides Function GetMembers() As IEnumerable(Of Symbol)
            Return _referencedType.GetMembers()
        End Function

        Public Overrides Function GetMembers(ByVal name As String) As ReadOnlyArray(Of Symbol)
            Return _referencedType.GetMembers(name)
        End Function

        Public Overrides Function GetTypeMembers() As IEnumerable(Of NamedTypeSymbol)
            Return _referencedType.GetTypeMembers()
        End Function

        Public Overrides Function GetTypeMembers(ByVal name As String) As ReadOnlyArray(Of NamedTypeSymbol)
            Return _referencedType.GetTypeMembers(name)
        End Function

        Public Overrides Function GetTypeMembers(ByVal name As String, ByVal arity As Integer) As IEnumerable(Of NamedTypeSymbol)
            Return _referencedType.GetTypeMembers(name, arity)
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return "ByRef " + _referencedType.Name()
            End Get
        End Property

        Public Overrides Function GetFullName() As String
            Return "ByRef " + _referencedType.GetFullName()
        End Function

        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.ByRefType
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.ByRefType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As IEnumerable(Of Location)
            Get
                Return Enumerable.Empty(Of Location)()
            End Get
        End Property

        Public Overrides Function GetAttributes() As System.Collections.Generic.IEnumerable(Of SymbolAttribute)
            Return Enumerable.Empty(Of SymbolAttribute)()
        End Function

        Public Overrides Function GetAttributes(ByVal attributeType As NamedTypeSymbol) As System.Collections.Generic.IEnumerable(Of SymbolAttribute)
            Return Enumerable.Empty(Of SymbolAttribute)()
        End Function

        Protected Friend Overrides Function Accept(Of TResult, TArgument)(ByVal visitor As SymbolVisitor(Of TResult, TArgument), ByVal arg As TArgument) As TResult
            Return visitor.VisitByRefType(Me, arg)
        End Function

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property


        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function SubstituteTypeParameters(ByVal substitution As TypeSubstitution) As TypeSymbol
            ' Create a new byref symbol with substitutions applied.
            Dim newReferencedType As TypeSymbol = DirectCast(_referencedType.SubstituteTypeParameters(substitution), TypeSymbol)
            If Not newReferencedType.Equals(_referencedType) Then
                Return New ByRefTypeSymbol(newReferencedType)
            Else
                Return Me ' substitution had no effect on the referenced type
            End If
        End Function


    End Class
End Namespace


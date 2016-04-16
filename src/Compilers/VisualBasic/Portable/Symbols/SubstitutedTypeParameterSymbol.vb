' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A SubstitutedTypeParameterSymbol represents an alpha-renamed type parameter.
    ''' They are created only for open generic types and methods that are contained within a 
    ''' constructed generic type.
    ''' 
    ''' Alpha-renamed type parameters have their constraints substituted according
    ''' to type/method's containing type's TypeSubstitution.
    ''' For example:
    '''     Class A (Of T)
    '''         Class B(Of S As T)
    '''         End Class
    '''     End Class
    '''  
    ''' Given a type A(Of IComparable).B(Of ), alpha-renamed type parameter T will have type constraint IComparable.
    ''' The rest will be exactly as for the original type parameter T. In fact, OriginalDefinition will return symbol for T.
    ''' </summary>
    Friend Class SubstitutedTypeParameterSymbol
        Inherits TypeParameterSymbol

        ''' <summary>
        ''' Containing type or method.
        ''' The field is not read-only because it is initialized after construction through
        ''' SetContainingSymbol() method.
        ''' </summary>
        Private _containingSymbol As Symbol
        Private ReadOnly _originalDefinition As TypeParameterSymbol

        Public Sub New(originalDefinition As TypeParameterSymbol)
            Debug.Assert(originalDefinition.IsDefinition)
            _originalDefinition = originalDefinition
        End Sub

        Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
            Get
                Return _originalDefinition.TypeParameterKind
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _originalDefinition.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _originalDefinition.MetadataName
            End Get
        End Property

        Public Sub SetContainingSymbol(container As Symbol)
            Debug.Assert(_containingSymbol Is Nothing AndAlso container IsNot Nothing)

            Debug.Assert(TypeOf container Is SubstitutedNamedType.SpecializedGenericType OrElse
                         TypeOf container Is SubstitutedMethodSymbol.SpecializedGenericMethod OrElse
                         (TypeOf container Is UnboundGenericType AndAlso DirectCast(container, UnboundGenericType).Arity > 0 AndAlso
                          DirectCast(container, UnboundGenericType).ConstructedFrom Is container))

            _containingSymbol = container
        End Sub

        Public Overrides ReadOnly Property OriginalDefinition As TypeParameterSymbol
            Get
                Return _originalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property ReducedFrom As TypeParameterSymbol
            Get
                Return _originalDefinition.ReducedFrom
            End Get
        End Property

        Private ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return If(_containingSymbol.Kind = SymbolKind.Method,
                          DirectCast(_containingSymbol, SubstitutedMethodSymbol).TypeSubstitution,
                          DirectCast(_containingSymbol, NamedTypeSymbol).TypeSubstitution)
            End Get
        End Property

        Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return InternalSubstituteTypeParametersDistinct(TypeSubstitution, _originalDefinition.ConstraintTypesNoUseSiteDiagnostics)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Public Overloads Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return _originalDefinition.GetAttributes()
        End Function

        Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
            Get
                Return _originalDefinition.HasConstructorConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
            Get
                Return _originalDefinition.HasReferenceTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
            Get
                Return _originalDefinition.HasValueTypeConstraint
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _originalDefinition.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _originalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Ordinal As Integer
            Get
                Return _originalDefinition.Ordinal
            End Get
        End Property

        Public Overrides ReadOnly Property Variance As VarianceKind
            Get
                Return _originalDefinition.Variance
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.Ordinal.GetHashCode(), _containingSymbol.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean

            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, SubstitutedTypeParameterSymbol)

            Return other IsNot Nothing AndAlso Me.Ordinal = other.Ordinal AndAlso Me.ContainingSymbol.Equals(other.ContainingSymbol)
        End Function

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            If substitution IsNot Nothing Then
                If substitution.TargetGenericDefinition Is _containingSymbol Then
                    Return substitution.GetSubstitutionFor(Me)
                End If

                Throw ExceptionUtilities.Unreachable
            End If

            Return New TypeWithModifiers(Me)
        End Function

        Friend Overrides Sub EnsureAllConstraintsAreResolved()
            _originalDefinition.EnsureAllConstraintsAreResolved()
        End Sub

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class

End Namespace

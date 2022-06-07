' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' The base class for potentially constructible (i.e. with known arity) error type symbols
    ''' </summary>
    Friend MustInherit Class InstanceErrorTypeSymbol
        Inherits ErrorTypeSymbol

        Protected ReadOnly _arity As Integer
        Private _lazyTypeParameters As ImmutableArray(Of TypeParameterSymbol)

        Friend Sub New(arity As Integer)
            Debug.Assert(arity >= 0)
            _arity = arity

            If arity = 0 Then
                _lazyTypeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
            End If
        End Sub

        Public NotOverridable Overrides ReadOnly Property Arity As Integer
            Get
                Return _arity
            End Get
        End Property

        ' Instance types are always constructible if they have arity >= 1
        Friend Overrides ReadOnly Property CanConstruct As Boolean
            Get
                Return _arity > 0
            End Get
        End Property

        Public NotOverridable Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
            CheckCanConstructAndTypeArguments(typeArguments)

            Dim substitution = TypeSubstitution.Create(Me, Me.TypeParameters, typeArguments, allowAlphaRenamedTypeParametersAsArguments:=True)

            If substitution Is Nothing Then
                Return Me
            Else
                Return New SubstitutedErrorType(Me.ContainingSymbol, Me, substitution)
            End If
        End Function

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend NotOverridable Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Return New TypeWithModifiers(InternalSubstituteTypeParametersInInstanceErrorTypeSymbol(substitution))
        End Function

        Private Overloads Function InternalSubstituteTypeParametersInInstanceErrorTypeSymbol(substitution As TypeSubstitution) As NamedTypeSymbol
            If substitution IsNot Nothing Then
                ' The substitution might target one of this type's children.
                substitution = substitution.GetSubstitutionForGenericDefinitionOrContainers(Me)
            End If

            If substitution Is Nothing Then
                Return Me
            End If

            Dim container As Symbol = ContainingSymbol
            Dim containingType As NamedTypeSymbol = TryCast(container, NamedTypeSymbol)

            If containingType Is Nothing Then
                Debug.Assert(substitution.TargetGenericDefinition Is Me AndAlso substitution.Parent Is Nothing AndAlso substitution.Pairs.Length > 0)
                Return New SubstitutedErrorType(container, Me, substitution)
            Else
                Dim newContainer = DirectCast(containingType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)

                If substitution.TargetGenericDefinition Is Me Then
                    Debug.Assert(substitution IsNot Nothing)
                    Debug.Assert(newContainer.TypeSubstitution Is substitution.Parent) ' How can it be otherwise? The contained type didn't have any substitution before.
                Else
                    ' We don't alpha-rename type parameters of error type symbols because they don't have constraints
                    ' and, therefore, have nothing to substitute. Just use identity substitution.
                    Debug.Assert(newContainer.TypeSubstitution IsNot Nothing)
                    substitution = VisualBasic.Symbols.TypeSubstitution.Concat(Me, newContainer.TypeSubstitution, Nothing)
                End If

                Return New SubstitutedErrorType(newContainer, Me, substitution)
            End If
        End Function

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                If _lazyTypeParameters.IsDefault Then
                    Dim params = New TypeParameterSymbol(_arity - 1) {}

                    For i As Integer = 0 To _arity - 1 Step 1
                        params(i) = New ErrorTypeParameterSymbol(Me, i)
                    Next

                    ImmutableInterlocked.InterlockedCompareExchange(_lazyTypeParameters,
                                                params.AsImmutableOrNull(),
                                                Nothing)
                End If

                Return _lazyTypeParameters
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
            End Get
        End Property

        Public NotOverridable Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
            ' This is always the instance type, so the type arguments do not have any modifiers.
            Return GetEmptyTypeArgumentCustomModifiers(ordinal)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
            Get
                ' This is always the instance type, so the type arguments do not have any modifiers.
                Return False
            End Get
        End Property

        Public MustOverride Overrides Function GetHashCode() As Integer

        Public NotOverridable Overrides Function Equals(other As TypeSymbol, comparison As TypeCompareKind) As Boolean
            If other Is Me Then
                Return True
            End If

            If other Is Nothing Then
                Return False
            End If

            Dim otherInstance = TryCast(other, InstanceErrorTypeSymbol)

            If otherInstance Is Nothing AndAlso (comparison And TypeCompareKind.AllIgnoreOptionsForVB) = 0 Then
                Return False
            End If

            Dim otherTuple = TryCast(other, TupleTypeSymbol)
            If otherTuple IsNot Nothing Then
                Return otherTuple.Equals(Me, comparison)
            End If

            If otherInstance IsNot Nothing Then
                Return SpecializedEquals(otherInstance)
            End If

            Debug.Assert((comparison And TypeCompareKind.AllIgnoreOptionsForVB) <> 0)

            If Not Me.Equals(other.OriginalDefinition) Then
                Return False
            End If

            ' Delegate comparison to the other type to ensure symmetry
            Debug.Assert(TypeOf other Is SubstitutedErrorType)
            Return other.Equals(Me, comparison)
        End Function

        Protected MustOverride Function SpecializedEquals(other As InstanceErrorTypeSymbol) As Boolean

        Private NotInheritable Class ErrorTypeParameterSymbol
            Inherits TypeParameterSymbol

            Private ReadOnly _container As InstanceErrorTypeSymbol
            Private ReadOnly _ordinal As Integer

            Public Sub New(container As InstanceErrorTypeSymbol, ordinal As Integer)
                _container = container
                _ordinal = ordinal
            End Sub

            Public Overrides ReadOnly Property TypeParameterKind As TypeParameterKind
                Get
                    Return TypeParameterKind.Type
                End Get
            End Property

            Public Overrides ReadOnly Property Name As String
                Get
                    Return String.Empty
                End Get
            End Property

            Friend Overrides ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
                Get
                    Return ImmutableArray(Of TypeSymbol).Empty
                End Get
            End Property

            Friend Overrides Function GetConstraints() As ImmutableArray(Of TypeParameterConstraint)
                Return ImmutableArray(Of TypeParameterConstraint).Empty
            End Function

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _container
                End Get
            End Property

            Public Overrides ReadOnly Property HasConstructorConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property HasReferenceTypeConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property HasValueTypeConstraint As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return ImmutableArray(Of Location).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return ImmutableArray(Of SyntaxReference).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property Ordinal As Integer
                Get
                    Return _ordinal
                End Get
            End Property

            Public Overrides ReadOnly Property Variance As VarianceKind
                Get
                    Return VarianceKind.None
                End Get
            End Property

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(_container.GetHashCode(), _ordinal)
            End Function

            Public Overrides Function Equals(obj As TypeSymbol, comparison As TypeCompareKind) As Boolean
                If obj Is Nothing Then
                    Return False
                End If

                If obj Is Me Then
                    Return True
                End If

                Dim other = TryCast(obj, ErrorTypeParameterSymbol)

                Return other IsNot Nothing AndAlso other._ordinal = Me._ordinal AndAlso other._container.Equals(Me._container, comparison)
            End Function

            Friend Overrides Sub EnsureAllConstraintsAreResolved()
            End Sub

        End Class

    End Class

End Namespace


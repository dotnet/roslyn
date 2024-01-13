' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SubstitutedErrorType
        Inherits ErrorTypeSymbol

        ' The _fullInstanceType is the instance that is also contained in its
        ' instance type, etc. all the way out. 
        Private ReadOnly _fullInstanceType As InstanceErrorTypeSymbol
        Private ReadOnly _substitution As TypeSubstitution
        Private ReadOnly _container As Symbol

        ' True if the _substitution doesn't involved of my direct
        ' type parameters, only type parameters of containing types.
        Private ReadOnly Property IdentitySubstitutionOnMyTypeParameters As Boolean
            Get
                Return _substitution.Pairs.Length = 0
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _fullInstanceType.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return _fullInstanceType.MangleName
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _fullInstanceType.MetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _fullInstanceType.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property OriginalDefinition As NamedTypeSymbol
            Get
                Return _fullInstanceType
            End Get
        End Property

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return _fullInstanceType.ErrorInfo
            End Get
        End Property

        Private ReadOnly Property ConstructedFromItself As Boolean
            Get
                Return _fullInstanceType.Arity = 0 OrElse IdentitySubstitutionOnMyTypeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
            Get
                If ConstructedFromItself Then
                    Return Me
                End If

                If Me.ContainingSymbol Is Nothing OrElse Me.ContainingSymbol.IsDefinition Then
                    Return _fullInstanceType
                End If

                Dim substitution As TypeSubstitution = _substitution.Parent

                Debug.Assert(substitution IsNot Nothing AndAlso substitution.TargetGenericDefinition Is Me.ContainingType.OriginalDefinition)

                ' We don't alpha-rename type parameters of error type symbols because they don't have constraints
                ' and, therefore, have nothing to substitute. Just use identity substitution.
                substitution = VisualBasic.Symbols.TypeSubstitution.Concat(_fullInstanceType, substitution, Nothing)

                Return New SubstitutedErrorType(Me.ContainingSymbol, _fullInstanceType, substitution)
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return _substitution
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _fullInstanceType.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return _fullInstanceType.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _fullInstanceType.TypeParameters
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                ' TODO: Is creating a new list here every time the right approach? Should this be cached?

                If IdentitySubstitutionOnMyTypeParameters Then
                    Return StaticCast(Of TypeSymbol).From(TypeParameters)
                Else
                    Return _substitution.GetTypeArgumentsFor(_fullInstanceType, Nothing)
                End If
            End Get
        End Property

        Public Overrides Function GetTypeArgumentCustomModifiers(ordinal As Integer) As ImmutableArray(Of CustomModifier)
            If IdentitySubstitutionOnMyTypeParameters Then
                Return GetEmptyTypeArgumentCustomModifiers(ordinal)
            End If

            Return _substitution.GetTypeArgumentsCustomModifiersFor(_fullInstanceType.TypeParameters(ordinal))
        End Function

        Friend Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
            Get
                If IdentitySubstitutionOnMyTypeParameters Then
                    Return False
                End If

                Return _substitution.HasTypeArgumentsCustomModifiersFor(_fullInstanceType)
            End Get
        End Property

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend Overrides Function InternalSubstituteTypeParameters(additionalSubstitution As TypeSubstitution) As TypeWithModifiers
            Return New TypeWithModifiers(InternalSubstituteTypeParametersInSubstitutedErrorType(additionalSubstitution))
        End Function

        Private Overloads Function InternalSubstituteTypeParametersInSubstitutedErrorType(additionalSubstitution As TypeSubstitution) As NamedTypeSymbol
            If additionalSubstitution Is Nothing Then
                Return Me
            End If

            Dim container As Symbol = ContainingSymbol
            Dim containingType = TryCast(container, NamedTypeSymbol)

            If containingType Is Nothing Then
                Debug.Assert(_substitution.Parent Is Nothing)
                Dim substitution As TypeSubstitution = VisualBasic.Symbols.TypeSubstitution.AdjustForConstruct(Nothing, _substitution, additionalSubstitution)

                If substitution Is Nothing Then
                    Return _fullInstanceType
                End If

                If substitution Is _substitution Then
                    Return Me
                End If

                Return New SubstitutedErrorType(container, _fullInstanceType, substitution)
            Else
                Dim newContainer = DirectCast(containingType.InternalSubstituteTypeParameters(additionalSubstitution).AsTypeSymbolOnly(), NamedTypeSymbol)

                Dim newSubstitution = VisualBasic.Symbols.TypeSubstitution.AdjustForConstruct(newContainer.TypeSubstitution, _substitution, additionalSubstitution)

                If newSubstitution Is Nothing Then
                    Debug.Assert(newContainer.TypeSubstitution Is Nothing AndAlso newContainer.IsDefinition)
                    Return _fullInstanceType
                End If

                If newContainer Is containingType AndAlso newSubstitution Is _substitution Then
                    Return Me
                End If

                Return New SubstitutedErrorType(newContainer, _fullInstanceType, newSubstitution)
            End If
        End Function

        ' We can only construct if we have an identify substitution on all of our type parameters.
        Friend Overrides ReadOnly Property CanConstruct As Boolean
            Get
                Return Arity > 0 AndAlso IdentitySubstitutionOnMyTypeParameters
            End Get
        End Property

        Public Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
            CheckCanConstructAndTypeArguments(typeArguments)

            Dim substitution = TypeSubstitution.Create(_fullInstanceType, _fullInstanceType.TypeParameters, typeArguments, allowAlphaRenamedTypeParametersAsArguments:=True)

            If substitution Is Nothing Then
                Return Me
            Else
                Return New SubstitutedErrorType(_container, _fullInstanceType, TypeSubstitution.Concat(_fullInstanceType, _substitution.Parent, substitution))
            End If
        End Function

        Public Sub New(container As Symbol, fullInstanceType As InstanceErrorTypeSymbol, substitution As TypeSubstitution)
            MyBase.New()
            Debug.Assert(fullInstanceType IsNot Nothing)
            Debug.Assert(substitution IsNot Nothing)
            Debug.Assert(substitution.TargetGenericDefinition Is fullInstanceType)

            _container = container
            _fullInstanceType = fullInstanceType
            _substitution = substitution
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _container
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Dim hash As Integer = _fullInstanceType.GetHashCode()

            If Me._substitution.WasConstructedForModifiers() Then
                Return hash
            End If

            hash = Roslyn.Utilities.Hash.Combine(ContainingType, hash)

            If Not ConstructedFromItself Then
                For Each typeArgument In TypeArgumentsNoUseSiteDiagnostics
                    hash = Roslyn.Utilities.Hash.Combine(typeArgument, hash)
                Next
            End If

            Return hash
        End Function

        Public Overrides Function Equals(obj As TypeSymbol, comparison As TypeCompareKind) As Boolean

            If Me Is obj Then
                Return True
            End If

            If obj Is Nothing Then
                Return False
            End If

            If (comparison And TypeCompareKind.AllIgnoreOptionsForVB) = 0 AndAlso
               Not Me.GetType().Equals(obj.GetType()) Then
                Return False
            End If

            Dim otherTuple = TryCast(obj, TupleTypeSymbol)
            If otherTuple IsNot Nothing Then
                Return otherTuple.Equals(Me, comparison)
            End If

            If Not _fullInstanceType.Equals(obj.OriginalDefinition) Then
                Return False
            End If

            Dim containingType = Me.ContainingType

            If containingType IsNot Nothing AndAlso
               Not containingType.Equals(obj.ContainingType, comparison) Then
                Return False
            End If

            Dim other = DirectCast(obj, ErrorTypeSymbol)

            If Me.ConstructedFromItself AndAlso other Is other.ConstructedFrom Then
                ' No need to compare type arguments on those containers when they didn't add type arguments.
                Return True
            End If

            Dim arguments = TypeArgumentsNoUseSiteDiagnostics
            Dim otherArguments = other.TypeArgumentsNoUseSiteDiagnostics
            Dim count As Integer = arguments.Length

            For i As Integer = 0 To count - 1 Step 1
                If Not arguments(i).Equals(otherArguments(i), comparison) Then
                    Return False
                End If
            Next

            If (comparison And TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) = 0 AndAlso
               Not HasSameTypeArgumentCustomModifiers(Me, other) Then

                Return False
            End If

            Return True
        End Function

    End Class
End Namespace


' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ' TypeParameters are always directly contained in a NamedTypeDefinition,
    ' and always have referenced identity (assuming the NamedTypeDefinition also
    ' has reference identity, which seems true.)

    ''' <summary>
    ''' Represents a type parameter in a generic type or generic method.
    ''' </summary>
    Friend MustInherit Class TypeParameterSymbol
        Inherits TypeSymbol
        Implements ITypeParameterSymbol, ITypeParameterSymbolInternal

        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        ' Changes to the public interface of this class should remain synchronized with the C# version.
        ' Do not make any changes to the public interface without making the corresponding change
        ' to the C# version.
        ' !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        ''' <summary>
        ''' Get the original definition of this symbol. If this symbol is derived from another
        ''' symbol by (say) type substitution, this gets the original symbol, as it was defined
        ''' in source or metadata.
        ''' </summary>
        Public Overridable Shadows ReadOnly Property OriginalDefinition As TypeParameterSymbol
            Get
                ' Default implements returns Me.
                Return Me
            End Get
        End Property

        Protected NotOverridable Overrides ReadOnly Property OriginalTypeSymbolDefinition As TypeSymbol
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        ''' <summary>
        ''' Gets the ordinal order of this type parameter. The first type parameter has ordinal zero.
        ''' </summary>
        Public MustOverride ReadOnly Property Ordinal As Integer

        Friend Overridable Function GetConstraintsUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Return Nothing
        End Function

        ''' <summary>
        ''' Get the types that were directly specified as constraints on this type parameter.
        ''' Duplicates and cycles are removed, although the collection may include redundant
        ''' constraints where one constraint is a base type of another.
        ''' </summary>
        Friend MustOverride ReadOnly Property ConstraintTypesNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)

        Friend Function ConstraintTypesWithDefinitionUseSiteDiagnostics(<[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of TypeSymbol)
            Dim result = ConstraintTypesNoUseSiteDiagnostics

            Me.AddConstraintsUseSiteInfo(useSiteInfo)

            For Each constraint In result
                constraint.OriginalDefinition.AddUseSiteInfo(useSiteInfo)
            Next

            Return result
        End Function

        ''' <summary>
        ''' Returns whether the parameterless constructor constraint was specified.
        ''' </summary>
        Public MustOverride ReadOnly Property HasConstructorConstraint As Boolean Implements ITypeParameterSymbol.HasConstructorConstraint

        ' Type parameters do not have members.
        Public NotOverridable Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        ' Type parameters do not have members.
        Public NotOverridable Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public MustOverride ReadOnly Property TypeParameterKind As TypeParameterKind Implements ITypeParameterSymbol.TypeParameterKind

        ''' <summary>
        ''' The method that declares this type parameter 
        ''' </summary>
        Public ReadOnly Property DeclaringMethod As MethodSymbol
            Get
                Return TryCast(Me.ContainingSymbol, MethodSymbol)
            End Get
        End Property

        ''' <summary>
        ''' The type that declares this type parameter
        ''' </summary>
        Public ReadOnly Property DeclaringType As NamedTypeSymbol
            Get
                Return TryCast(Me.ContainingSymbol, NamedTypeSymbol)
            End Get
        End Property

        ' Type parameters do not have members.
        Public NotOverridable Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ' Type parameters do not have members.
        Public NotOverridable Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        ' Type parameters do not have members.
        Public NotOverridable Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitTypeParameter(Me, arg)
        End Function

        ' Only the compiler can create instances of TypeParameter.
        Friend Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.TypeParameter
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.TypeParameter
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Called by ConstraintTypes and Interfaces
        ''' to allow derived classes to ensure constraints within the containing
        ''' type or method are resolved in a consistent order, regardless of the
        ''' order the callers query individual type parameters.
        ''' </summary>
        Friend MustOverride Sub EnsureAllConstraintsAreResolved()

        ''' <summary>
        ''' Helper method to force type parameter constraints to be resolved.
        ''' </summary>
        Friend Shared Sub EnsureAllConstraintsAreResolved(typeParameters As ImmutableArray(Of TypeParameterSymbol))
            For Each typeParameter In typeParameters
                typeParameter.ResolveConstraints(ConsList(Of TypeParameterSymbol).Empty)
            Next
        End Sub

        ''' <summary>
        ''' Get all constraints, with optional source location for each. This method
        ''' is provided for constraint checking only, and should only be invoked
        ''' for SourceTypeParameterSymbol or ErrorTypeParameterSymbol directly.
        ''' There is currently no need to invoke this method for PETypeParameterSymbol.
        ''' </summary>
        Friend Sub GetConstraints(constraintsBuilder As ArrayBuilder(Of TypeParameterConstraint))
            constraintsBuilder.AddRange(GetConstraints())
        End Sub

        Friend Overridable Function GetConstraints() As ImmutableArray(Of TypeParameterConstraint)
            Throw ExceptionUtilities.Unreachable
        End Function

        ''' <summary>
        ''' Resolve constraints, binding and checking for conflicts as necessary. This is an
        ''' internal method for resolving dependent sets of constraints and handling cycles.
        ''' It will be called indirectly for core implementations (SourceTypeParameterSymbol and
        ''' PETypeParameterSymbol) as a result of accessing constraint properties on this class.
        ''' </summary>
        Friend Overridable Sub ResolveConstraints(inProgress As ConsList(Of TypeParameterSymbol))
            Throw ExceptionUtilities.Unreachable
        End Sub

        Friend Shared Function GetConstraintTypesOnly(constraints As ImmutableArray(Of TypeParameterConstraint)) As ImmutableArray(Of TypeSymbol)
            If constraints.IsEmpty Then
                Return ImmutableArray(Of TypeSymbol).Empty
            End If

            Dim builder = ArrayBuilder(Of TypeSymbol).GetInstance()
            For Each constraint In constraints
                Dim constraintType = constraint.TypeConstraint
                If constraintType IsNot Nothing Then
                    builder.Add(constraintType)
                End If
            Next
            Return builder.ToImmutableAndFree()
        End Function

        Public NotOverridable Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                If (Me.HasReferenceTypeConstraint) Then
                    Return True
                End If

                Return IsReferenceTypeIgnoringIsClass()
            End Get
        End Property

        ' From typedesc.cpp :
        ' > A recursive helper that helps determine whether this variable is constrained as ObjRef.
        ' > Please note that we do not check the gpReferenceTypeConstraint special constraint here
        ' > because this property does not propagate up the constraining hierarchy.
        ' > (e.g. "class A<S, T> where S : T, where T : class" does not guarantee that S is ObjRef)
        Private Function IsReferenceTypeIgnoringIsClass() As Boolean
            For Each constraint In Me.ConstraintTypesNoUseSiteDiagnostics
                If (ConstraintImpliesReferenceType(constraint)) Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Shared Function ConstraintImpliesReferenceType(constraint As TypeSymbol) As Boolean
            If (constraint.TypeKind = TypeKind.TypeParameter) Then
                Return DirectCast(constraint, TypeParameterSymbol).IsReferenceTypeIgnoringIsClass()
            Else
                If (constraint.IsReferenceType) Then
                    If (constraint.IsInterfaceType()) Then
                        Return False ' can be satisfied by valuetypes 
                    End If

                    Select Case (constraint.SpecialType)
                        Case SpecialType.System_Object,
                             SpecialType.System_ValueType,
                             SpecialType.System_Enum
                            Return False   ' can be satisfied by valuetypes     
                    End Select

                    Return True
                End If

                Return False
            End If
        End Function

        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                If Me.HasValueTypeConstraint Then
                    Return True
                End If

                ' usually value types cannot be used as type constraints. But there is a way to use a 
                ' value type as a type parameter which gets as a constraints to a overriding generic method - et voila: we
                ' have a value type as type constraint.
                '
                ' Source example:
                ' Class BaseGeneric(Of S)
                '    Public Overridable Sub MySub(Of T As S)(param As T) 
                '    End Sub
                ' End Class
                ' Class Derived
                '    Inherits BaseGeneric(Of MyStruct)
                '    Public Overrides Sub MySub(Of T As MyStruct)(param As T)
                '    End Sub
                ' End Class
                ' Structure MyStruct
                ' End Structure
                '
                ' therefore we need to check the type constraints for value types as well
                For Each constraint In Me.ConstraintTypesNoUseSiteDiagnostics
                    If (constraint.IsValueType) Then
                        Return True
                    End If
                Next

                Return False
            End Get
        End Property

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            If substitution IsNot Nothing Then
                Return substitution.GetSubstitutionFor(Me)
            End If

            Return New TypeWithModifiers(Me)
        End Function

        Public MustOverride ReadOnly Property HasReferenceTypeConstraint As Boolean Implements ITypeParameterSymbol.HasReferenceTypeConstraint

        Public MustOverride ReadOnly Property HasValueTypeConstraint As Boolean Implements ITypeParameterSymbol.HasValueTypeConstraint

        Public MustOverride ReadOnly Property AllowsRefLikeType As Boolean Implements ITypeParameterSymbol.AllowsRefLikeType

        Private ReadOnly Property HasUnmanagedTypeConstraint As Boolean Implements ITypeParameterSymbol.HasUnmanagedTypeConstraint
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property HasNotNullConstraint As Boolean Implements ITypeParameterSymbol.HasNotNullConstraint
            Get
                Return False
            End Get
        End Property

        Public MustOverride ReadOnly Property Variance As VarianceKind Implements ITypeParameterSymbol.Variance

        ''' <summary>
        ''' If this is a type parameter of a reduced extension method, gets the type parameter definition that
        ''' this type parameter was reduced from. Otherwise, returns Nothing.
        ''' </summary>
        Public Overridable ReadOnly Property ReducedFrom As TypeParameterSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Return an array of substituted type parameters with duplicates removed.
        ''' </summary>
        Friend Shared Function InternalSubstituteTypeParametersDistinct(substitution As TypeSubstitution, types As ImmutableArray(Of TypeSymbol)) As ImmutableArray(Of TypeSymbol)
            Return types.SelectAsArray(s_substituteFunc, substitution).Distinct()
        End Function

        Private Shared ReadOnly s_substituteFunc As Func(Of TypeSymbol, TypeSubstitution, TypeSymbol) = Function(type, substitution) type.InternalSubstituteTypeParameters(substitution).Type

        Friend Overrides ReadOnly Property EmbeddedSymbolKind As EmbeddedSymbolKind
            Get
                Return Me.ContainingSymbol.EmbeddedSymbolKind
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "ITypeParameterSymbol"

        Private ReadOnly Property ITypeParameterSymbol_ReferenceTypeConstraintNullableAnnotation As NullableAnnotation Implements ITypeParameterSymbol.ReferenceTypeConstraintNullableAnnotation
            Get
                Return NullableAnnotation.None
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_DeclaringMethod As IMethodSymbol Implements ITypeParameterSymbol.DeclaringMethod
            Get
                Return Me.DeclaringMethod
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_DeclaringType As INamedTypeSymbol Implements ITypeParameterSymbol.DeclaringType
            Get
                Return Me.DeclaringType
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_Ordinal As Integer Implements ITypeParameterSymbol.Ordinal
            Get
                Return Me.Ordinal
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_ConstraintTypes As ImmutableArray(Of ITypeSymbol) Implements ITypeParameterSymbol.ConstraintTypes
            Get
                Return StaticCast(Of ITypeSymbol).From(Me.ConstraintTypesNoUseSiteDiagnostics)
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_ConstraintNullableAnnotations As ImmutableArray(Of NullableAnnotation) Implements ITypeParameterSymbol.ConstraintNullableAnnotations
            Get
                Return Me.ConstraintTypesNoUseSiteDiagnostics.SelectAsArray(Function(t) NullableAnnotation.None)
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_OriginalDefinition As ITypeParameterSymbol Implements ITypeParameterSymbol.OriginalDefinition
            Get
                Return Me.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property ITypeParameterSymbol_ReducedFrom As ITypeParameterSymbol Implements ITypeParameterSymbol.ReducedFrom
            Get
                Return Me.ReducedFrom
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitTypeParameter(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitTypeParameter(Me)
        End Function

        Public Overrides Function Accept(Of TArgument, TResult)(visitor As SymbolVisitor(Of TArgument, TResult), argument As TArgument) As TResult
            Return visitor.VisitTypeParameter(Me, argument)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitTypeParameter(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitTypeParameter(Me)
        End Function

#End Region

    End Class
End Namespace

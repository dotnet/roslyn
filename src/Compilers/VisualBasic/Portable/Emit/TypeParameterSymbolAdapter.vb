' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class TypeParameterSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class TypeParameterSymbol
#End If
        Implements IGenericParameterReference
        Implements IGenericMethodParameterReference
        Implements IGenericTypeParameterReference
        Implements IGenericParameter
        Implements IGenericMethodParameter
        Implements IGenericTypeParameter

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements ITypeReference.IsEnum
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements ITypeReference.IsValueType
            Get
                Return False
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(context As EmitContext) As ITypeDefinition Implements ITypeReference.GetResolvedType
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceTypeCode As Cci.PrimitiveTypeCode Implements ITypeReference.TypeCode
            Get
                Return Cci.PrimitiveTypeCode.NotPrimitive
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceTypeDef As TypeDefinitionHandle Implements ITypeReference.TypeDef
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IGenericParameterAsGenericMethodParameter As IGenericMethodParameter Implements IGenericParameter.AsGenericMethodParameter
            Get
                CheckDefinitionInvariant()

                If AdaptedTypeParameterSymbol.ContainingSymbol.Kind = SymbolKind.Method Then
                    Return Me
                End If
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As IGenericMethodParameterReference Implements ITypeReference.AsGenericMethodParameterReference
            Get
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)

                If AdaptedTypeParameterSymbol.ContainingSymbol.Kind = SymbolKind.Method Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeInstanceReference As IGenericTypeInstanceReference Implements ITypeReference.AsGenericTypeInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IGenericParameterAsGenericTypeParameter As IGenericTypeParameter Implements IGenericParameter.AsGenericTypeParameter
            Get
                CheckDefinitionInvariant()

                If AdaptedTypeParameterSymbol.ContainingSymbol.Kind = SymbolKind.NamedType Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As IGenericTypeParameterReference Implements ITypeReference.AsGenericTypeParameterReference
            Get
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)

                If AdaptedTypeParameterSymbol.ContainingSymbol.Kind = SymbolKind.NamedType Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As EmitContext) As INamespaceTypeDefinition Implements ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNamespaceTypeReference As INamespaceTypeReference Implements ITypeReference.AsNamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNestedTypeDefinition(context As EmitContext) As INestedTypeDefinition Implements ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNestedTypeReference As INestedTypeReference Implements ITypeReference.AsNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsSpecializedNestedTypeReference As ISpecializedNestedTypeReference Implements ITypeReference.AsSpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsTypeDefinition(context As EmitContext) As ITypeDefinition Implements ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)
            Dim kind As SymbolKind = AdaptedTypeParameterSymbol.ContainingSymbol.Kind

            If (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule = AdaptedTypeParameterSymbol.ContainingModule Then
                If kind = SymbolKind.NamedType Then
                    visitor.Visit(DirectCast(Me, IGenericTypeParameter))
                Else
                    If kind = SymbolKind.Method Then
                        visitor.Visit(DirectCast(Me, IGenericMethodParameter))
                    Else
                        Throw ExceptionUtilities.UnexpectedValue(kind)
                    End If
                End If
            Else
                If kind = SymbolKind.NamedType Then
                    visitor.Visit(DirectCast(Me, IGenericTypeParameterReference))
                Else
                    If kind = SymbolKind.Method Then
                        visitor.Visit(DirectCast(Me, IGenericMethodParameterReference))
                    Else
                        Throw ExceptionUtilities.UnexpectedValue(kind)
                    End If
                End If
            End If
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)
            Return Nothing
        End Function

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                Return AdaptedTypeParameterSymbol.MetadataName
            End Get
        End Property

        Private ReadOnly Property IParameterListEntryIndex As UShort Implements IParameterListEntry.Index
            Get
                Return CType(AdaptedTypeParameterSymbol.Ordinal, UShort)
            End Get
        End Property

        Private ReadOnly Property IGenericMethodParameterReferenceDefiningMethod As IMethodReference Implements IGenericMethodParameterReference.DefiningMethod
            Get
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)
                Return DirectCast(AdaptedTypeParameterSymbol.ContainingSymbol, MethodSymbol).GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property IGenericTypeParameterReferenceDefiningType As ITypeReference Implements IGenericTypeParameterReference.DefiningType
            Get
                Debug.Assert(AdaptedTypeParameterSymbol.IsDefinition)
                Return DirectCast(AdaptedTypeParameterSymbol.ContainingSymbol, NamedTypeSymbol).GetCciAdapter()
            End Get
        End Property

        Private Iterator Function IGenericParameterGetConstraints(context As EmitContext) _
            As IEnumerable(Of TypeReferenceWithAttributes) Implements IGenericParameter.GetConstraints
            Dim _module = DirectCast(context.Module, PEModuleBuilder)
            Dim seenValueType = False
            For Each t In AdaptedTypeParameterSymbol.ConstraintTypesNoUseSiteDiagnostics
                If t.SpecialType = SpecialType.System_ValueType Then
                    seenValueType = True
                End If

                Dim typeRef As ITypeReference = _module.Translate(t,
                                                                  syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode),
                                                                  diagnostics:=context.Diagnostics)

                Yield t.GetTypeRefWithAttributes(AdaptedTypeParameterSymbol.DeclaringCompilation, typeRef)
            Next
            If AdaptedTypeParameterSymbol.HasValueTypeConstraint AndAlso Not seenValueType Then
                ' Add System.ValueType constraint to comply with Dev11 C# output
                Dim typeRef As INamedTypeReference = _module.GetSpecialType(CodeAnalysis.SpecialType.System_ValueType,
                                             DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), context.Diagnostics)

                Yield New Cci.TypeReferenceWithAttributes(typeRef)
            End If
        End Function

        Private ReadOnly Property IGenericParameterMustBeReferenceType As Boolean Implements IGenericParameter.MustBeReferenceType
            Get
                Return AdaptedTypeParameterSymbol.HasReferenceTypeConstraint
            End Get
        End Property

        Private ReadOnly Property IGenericParameterMustBeValueType As Boolean Implements IGenericParameter.MustBeValueType
            Get
                Return AdaptedTypeParameterSymbol.HasValueTypeConstraint
            End Get
        End Property

        Private ReadOnly Property IGenericParameterAllowByRefLike As Boolean Implements IGenericParameter.AllowsRefLikeType
            Get
                Return AdaptedTypeParameterSymbol.AllowsRefLikeType
            End Get
        End Property

        Private ReadOnly Property IGenericParameterMustHaveDefaultConstructor As Boolean Implements IGenericParameter.MustHaveDefaultConstructor
            Get
                '  add constructor constraint for value type constrained 
                '  type parameters to comply with Dev11 output
                Return AdaptedTypeParameterSymbol.HasConstructorConstraint OrElse AdaptedTypeParameterSymbol.HasValueTypeConstraint
            End Get
        End Property

        Private ReadOnly Property IGenericParameterVariance As TypeParameterVariance Implements IGenericParameter.Variance
            Get
                Select Case AdaptedTypeParameterSymbol.Variance
                    Case VarianceKind.None
                        Return TypeParameterVariance.NonVariant
                    Case VarianceKind.In
                        Return TypeParameterVariance.Contravariant
                    Case VarianceKind.Out
                        Return TypeParameterVariance.Covariant
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(AdaptedTypeParameterSymbol.Variance)
                End Select
            End Get
        End Property

        Private ReadOnly Property IGenericMethodParameterDefiningMethod As IMethodDefinition Implements IGenericMethodParameter.DefiningMethod
            Get
                CheckDefinitionInvariant()

                Return DirectCast(AdaptedTypeParameterSymbol.ContainingSymbol, MethodSymbol).GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property IGenericTypeParameterDefiningType As ITypeDefinition Implements IGenericTypeParameter.DefiningType
            Get
                CheckDefinitionInvariant()

                Return DirectCast(AdaptedTypeParameterSymbol.ContainingSymbol, NamedTypeSymbol).GetCciAdapter()
            End Get
        End Property
    End Class

    Partial Friend Class TypeParameterSymbol
#If DEBUG Then
        Private _lazyAdapter As TypeParameterSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As TypeParameterSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New TypeParameterSymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedTypeParameterSymbol As TypeParameterSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As TypeParameterSymbol
            Return Me
        End Function
#End If

        Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Return Nothing
        End Function
    End Class

#If DEBUG Then
    Partial Friend NotInheritable Class TypeParameterSymbolAdapter
        Friend ReadOnly Property AdaptedTypeParameterSymbol As TypeParameterSymbol

        Friend Sub New(underlyingTypeParameterSymbol As TypeParameterSymbol)
            AdaptedTypeParameterSymbol = underlyingTypeParameterSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedTypeParameterSymbol
            End Get
        End Property
    End Class
#End If
End Namespace

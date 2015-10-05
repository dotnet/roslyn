' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' An InstanceTypeSymbol is a NamedTypeSymbol that is a pure instance type, where the class
    ''' (and any containing classes) have no type substitutions applied.
    ''' This class provide shared implementation for types whose definition is (possibly lazily)
    ''' constructed from source or metadata. It provides the shared implementation between these two, primarily
    ''' the implementation of Construct and InternalSubstituteTypeParameters.
    ''' </summary>
    Friend MustInherit Class InstanceTypeSymbol
        Inherits NamedTypeSymbol

        Friend NotOverridable Overrides ReadOnly Property TypeArgumentsNoUseSiteDiagnostics As ImmutableArray(Of TypeSymbol)
            Get
                ' This is always the instance type, so the type arguments are the same as the type parameters.
                If Arity > 0 Then
                    Return StaticCast(Of TypeSymbol).From(Me.TypeParameters)
                Else
                    Return ImmutableArray(Of TypeSymbol).Empty
                End If
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property TypeArgumentsCustomModifiers As ImmutableArray(Of ImmutableArray(Of CustomModifier))
            Get
                ' This is always the instance type, so the type arguments do not have any modifiers.
                Return CreateEmptyTypeArgumentsCustomModifiers()
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasTypeArgumentsCustomModifiers As Boolean
            Get
                ' This is always the instance type, so the type arguments do not have any modifiers.
                Return False
            End Get
        End Property

        ' Instance types are always constructible if they have arity >= 1
        Friend Overrides ReadOnly Property CanConstruct As Boolean
            Get
                Return Arity > 0
            End Get
        End Property

        Public NotOverridable Overrides Function Construct(typeArguments As ImmutableArray(Of TypeSymbol)) As NamedTypeSymbol
            CheckCanConstructAndTypeArguments(typeArguments)

            Dim substitution = VisualBasic.Symbols.TypeSubstitution.Create(Me, Me.TypeParameters, typeArguments, allowAlphaRenamedTypeParametersAsArguments:=True)

            If substitution Is Nothing Then
                Return Me
            Else
                Debug.Assert(substitution.TargetGenericDefinition Is Me)
                Return New SubstitutedNamedType.ConstructedInstanceType(substitution)
            End If
        End Function

        ''' <summary>
        ''' Substitute the given type substitution within this type, returning a new type. If the
        ''' substitution had no effect, return Me. 
        ''' !!! Only code implementing construction of generic types is allowed to call this method !!!
        ''' !!! All other code should use Construct methods.                                        !!! 
        ''' </summary>
        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Return New TypeWithModifiers(InternalSubstituteTypeParametersInNamedType(substitution))
        End Function

        Private Overloads Function InternalSubstituteTypeParametersInNamedType(substitution As TypeSubstitution) As NamedTypeSymbol

            If substitution IsNot Nothing Then
                ' The substitution might target one of this type's children.
                substitution = substitution.GetSubstitutionForGenericDefinitionOrContainers(Me)
            End If

            If substitution Is Nothing Then
                Return Me
            End If

            ' Substitution targets either this type or one of its containers
            Dim newContainer As NamedTypeSymbol

            If substitution.TargetGenericDefinition Is Me Then

                If substitution.Parent Is Nothing Then
                    Debug.Assert(Me.Arity > 0)
                    Return New SubstitutedNamedType.ConstructedInstanceType(substitution)
                End If

                newContainer = DirectCast(Me.ContainingType.InternalSubstituteTypeParameters(substitution.Parent).AsTypeSymbolOnly(), NamedTypeSymbol)
            Else
                newContainer = DirectCast(Me.ContainingType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), NamedTypeSymbol)
            End If

            Debug.Assert(Me.ContainingType IsNot Nothing)

            If Me.Arity = 0 Then
                Debug.Assert(Not newContainer.IsDefinition)
                Return SubstitutedNamedType.SpecializedNonGenericType.Create(DirectCast(newContainer, NamedTypeSymbol), Me, substitution)
            End If

            ' First we need to create SpecializedGenericType to construct this guy from.
            Dim constructFrom = SubstitutedNamedType.SpecializedGenericType.Create(DirectCast(newContainer, NamedTypeSymbol), Me)

            If substitution.TargetGenericDefinition Is Me Then
                Debug.Assert(newContainer.TypeSubstitution Is substitution.Parent) ' How can it be otherwise? The contained type didn't have any substitution before.
                Return New SubstitutedNamedType.ConstructedSpecializedGenericType(constructFrom, substitution)
            End If

            Return constructFrom
        End Function

        Friend Overrides ReadOnly Property TypeSubstitution As TypeSubstitution
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ConstructedFrom As NamedTypeSymbol
            Get
                Return Me
            End Get
        End Property

#Region "Use-Site Diagnostics"

        Protected Function CalculateUseSiteErrorInfo() As DiagnosticInfo
            ' Check base type.
            Dim baseErrorInfo As DiagnosticInfo = DeriveUseSiteErrorInfoFromBase()

            If baseErrorInfo IsNot Nothing Then
                Return baseErrorInfo
            End If

            ' If we reach a type (Me) that is in an assembly with unified references, 
            ' we check if that type definition depends on a type from a unified reference.
            If Me.ContainingModule.HasUnifiedReferences Then
                Return GetUnificationUseSiteDiagnosticRecursive(Me, checkedTypes:=Nothing)
            End If

            Return Nothing
        End Function

        Private Function DeriveUseSiteErrorInfoFromBase() As DiagnosticInfo

            Dim base As NamedTypeSymbol = Me.BaseTypeNoUseSiteDiagnostics

            While base IsNot Nothing

                If base.IsErrorType() AndAlso TypeOf base Is NoPiaIllegalGenericInstantiationSymbol Then
                    Return base.GetUseSiteErrorInfo()
                End If

                base = base.BaseTypeNoUseSiteDiagnostics
            End While

            Return Nothing
        End Function

        Friend NotOverridable Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Debug.Assert(owner.ContainingModule.HasUnifiedReferences)

            If Not MarkCheckedIfNecessary(checkedTypes) Then
                Return Nothing
            End If

            Dim info = owner.ContainingModule.GetUnificationUseSiteErrorInfo(Me)
            If info IsNot Nothing Then
                Return info
            End If

            ' TODO (tomat): use-site errors should be reported on each part of a qualified name,
            ' we shouldn't need to walk containing types here (see bug 15793)
            ' containing type
            Dim containing = Me.ContainingType
            If containing IsNot Nothing Then
                info = containing.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes)
                If info IsNot Nothing Then
                    Return info
                End If
            End If

            ' base type
            Dim base = Me.BaseTypeNoUseSiteDiagnostics
            If base IsNot Nothing Then
                info = base.GetUnificationUseSiteDiagnosticRecursive(owner, checkedTypes)
                If info IsNot Nothing Then
                    Return info
                End If
            End If

            ' implemented interfaces, type parameter constraints, type arguments
            Return If(GetUnificationUseSiteDiagnosticRecursive(Me.InterfacesNoUseSiteDiagnostics, owner, checkedTypes),
                      GetUnificationUseSiteDiagnosticRecursive(Me.TypeParameters, owner, checkedTypes))
        End Function
#End Region
    End Class

End Namespace

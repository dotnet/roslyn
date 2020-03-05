﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to an instantiation of a generic type nested in an instantiation of another generic type.
    ''' e.g. 
    ''' A{int}.B{string}
    ''' A.B{int}.C.D{string}
    ''' </summary>
    Friend NotInheritable Class SpecializedGenericNestedTypeInstanceReference
        Inherits SpecializedNestedTypeReference
        Implements Cci.IGenericTypeInstanceReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)

            Debug.Assert(underlyingNamedType.IsDefinition)
            ' Definition doesn't have custom modifiers on type arguments
            Debug.Assert(Not underlyingNamedType.HasTypeArgumentsCustomModifiers)
        End Sub

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.IGenericTypeInstanceReference))
        End Sub

        Private Function IGenericTypeInstanceReferenceGetGenericArguments(context As EmitContext) As ImmutableArray(Of Cci.ITypeReference) Implements Cci.IGenericTypeInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim builder = ArrayBuilder(Of Cci.ITypeReference).GetInstance()
            For Each t In m_UnderlyingNamedType.TypeArgumentsNoUseSiteDiagnostics
                builder.Add(moduleBeingBuilt.Translate(t, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics))
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function IGenericTypeInstanceReferenceGetGenericType(context As EmitContext) As Cci.INamedTypeReference Implements Cci.IGenericTypeInstanceReference.GetGenericType
            Debug.Assert(m_UnderlyingNamedType.OriginalDefinition Is m_UnderlyingNamedType.OriginalDefinition.OriginalDefinition)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(m_UnderlyingNamedType.OriginalDefinition, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                                              diagnostics:=context.Diagnostics, needDeclaration:=True)
        End Function

        Public Overrides ReadOnly Property AsGenericTypeInstanceReference As Cci.IGenericTypeInstanceReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsNamespaceTypeReference As Cci.INamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsNestedTypeReference As Cci.INestedTypeReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedNestedTypeReference As Cci.ISpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

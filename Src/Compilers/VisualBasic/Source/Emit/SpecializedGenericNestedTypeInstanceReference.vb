' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to an instantiation of a generic type nested in an instantiation of another generic type.
    ''' e.g. 
    ''' A{int}.B{string}
    ''' A.B{int}.C.D{string}
    ''' </summary>
    Friend NotInheritable Class SpecializedGenericNestedTypeInstanceReference
        Inherits SpecializedNestedTypeReference
        Implements Microsoft.Cci.IGenericTypeInstanceReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)
        End Sub

        Public Overrides Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IGenericTypeInstanceReference))
        End Sub

        Private Function IGenericTypeInstanceReferenceGetGenericArguments(context As Microsoft.CodeAnalysis.Emit.Context) As ImmutableArray(Of Microsoft.Cci.ITypeReference) Implements Microsoft.Cci.IGenericTypeInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim builder = ArrayBuilder(Of ITypeReference).GetInstance()
            For Each t In m_UnderlyingNamedType.TypeArgumentsNoUseSiteDiagnostics
                builder.Add(moduleBeingBuilt.Translate(t, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics))
            Next

            Return builder.ToImmutableAndFree
        End Function

        Private ReadOnly Property IGenericTypeInstanceReferenceGenericType As Microsoft.Cci.INamedTypeReference Implements Microsoft.Cci.IGenericTypeInstanceReference.GenericType
            Get
                Debug.Assert(m_UnderlyingNamedType.OriginalDefinition Is m_UnderlyingNamedType.OriginalDefinition.OriginalDefinition)
                Return m_UnderlyingNamedType.OriginalDefinition
            End Get
        End Property

        Public Overrides ReadOnly Property AsGenericTypeInstanceReference As Microsoft.Cci.IGenericTypeInstanceReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsNamespaceTypeReference As Microsoft.Cci.INamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property AsNestedTypeReference As Microsoft.Cci.INestedTypeReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides ReadOnly Property AsSpecializedNestedTypeReference As Microsoft.Cci.ISpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

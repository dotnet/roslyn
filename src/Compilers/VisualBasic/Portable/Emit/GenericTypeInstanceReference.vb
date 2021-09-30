' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic type instantiation.
    ''' Subclasses represent nested and namespace types.
    ''' </summary>
    Friend MustInherit Class GenericTypeInstanceReference
        Inherits NamedTypeReference
        Implements Cci.IGenericTypeInstanceReference

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            MyBase.New(underlyingNamedType)

            Debug.Assert(underlyingNamedType.IsDefinition)
            ' Definition doesn't have custom modifiers on type arguments
            Debug.Assert(Not underlyingNamedType.HasTypeArgumentsCustomModifiers)
        End Sub

        Public NotOverridable Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.IGenericTypeInstanceReference))
        End Sub

        Private Function IGenericTypeInstanceReferenceGetGenericArguments(context As EmitContext) As ImmutableArray(Of Cci.ITypeReference) Implements Cci.IGenericTypeInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Dim builder = ArrayBuilder(Of ITypeReference).GetInstance()
            For Each t In m_UnderlyingNamedType.TypeArgumentsNoUseSiteDiagnostics
                builder.Add(moduleBeingBuilt.Translate(t, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics))
            Next

            Return builder.ToImmutableAndFree
        End Function

        Private Function IGenericTypeInstanceReferenceGetGenericType(context As EmitContext) As Cci.INamedTypeReference Implements Cci.IGenericTypeInstanceReference.GetGenericType
            Debug.Assert(m_UnderlyingNamedType.OriginalDefinition Is m_UnderlyingNamedType.OriginalDefinition.OriginalDefinition)
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(m_UnderlyingNamedType.OriginalDefinition, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode),
                                              diagnostics:=context.Diagnostics, needDeclaration:=True)
        End Function
    End Class
End Namespace

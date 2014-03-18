' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class TypeMemberReference
        Implements Microsoft.Cci.ITypeMemberReference

        Protected MustOverride ReadOnly Property UnderlyingSymbol As Symbol

        Public Overridable Function GetContainingType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeReference Implements Microsoft.Cci.ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property INamedEntityName As String Implements Microsoft.Cci.INamedEntity.Name
            Get
                Return UnderlyingSymbol.MetadataName
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return UnderlyingSymbol.ToString()
        End Function

        Private Function IReferenceAttributes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ICustomAttribute) Implements Microsoft.Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.ICustomAttribute)()
        End Function

        Public MustOverride Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor) Implements Microsoft.Cci.IReference.Dispatch

        Private Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IDefinition Implements Microsoft.Cci.IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace

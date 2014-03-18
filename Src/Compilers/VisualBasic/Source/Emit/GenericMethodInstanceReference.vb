' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic method instantiation, closed over type parameters, 
    ''' e.g. MyNamespace.Class.Method{T}()
    ''' </summary>
    Friend NotInheritable Class GenericMethodInstanceReference
        Inherits MethodReference
        Implements Microsoft.Cci.IGenericMethodInstanceReference

        Public Sub New(underlyingMethod As MethodSymbol)
            MyBase.New(underlyingMethod)
        End Sub

        Public Overrides Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IGenericMethodInstanceReference))
        End Sub

        Private Function IGenericMethodInstanceReferenceGetGenericArguments(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ITypeReference) Implements Microsoft.Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return From arg In m_UnderlyingMethod.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericMethod(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IMethodReference Implements Microsoft.Cci.IGenericMethodInstanceReference.GetGenericMethod
            Debug.Assert(Not m_UnderlyingMethod.ContainingType.IsOrInGenericType())
            ' NoPia method might come through here.
            Return DirectCast(context.Module, PEModuleBuilder).Translate(
                m_UnderlyingMethod.OriginalDefinition,
                DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                context.Diagnostics,
                needDeclaration:=True)
        End Function

        Public Overrides ReadOnly Property AsGenericMethodInstanceReference As Microsoft.Cci.IGenericMethodInstanceReference
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace

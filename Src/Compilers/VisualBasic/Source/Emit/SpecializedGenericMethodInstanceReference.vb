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
    ''' Represents a generic method of a generic type instantiation, closed over type parameters.
    ''' e.g. 
    ''' A{T}.M{S}()
    ''' A.B{T}.C.M{S}()
    ''' </summary>
    Friend NotInheritable Class SpecializedGenericMethodInstanceReference
        Inherits SpecializedMethodReference
        Implements Microsoft.Cci.IGenericMethodInstanceReference

        Private m_GenericMethod As SpecializedMethodReference

        Public Sub New(underlyingMethod As MethodSymbol)
            MyBase.New(underlyingMethod)

            Debug.Assert(underlyingMethod.ContainingType.IsOrInGenericType() AndAlso underlyingMethod.ContainingType.IsDefinition)
            m_GenericMethod = New SpecializedMethodReference(underlyingMethod)
        End Sub

        Public Function GetGenericMethod(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IMethodReference Implements Microsoft.Cci.IGenericMethodInstanceReference.GetGenericMethod
            Return m_GenericMethod
        End Function

        Public Function GetGenericArguments(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ITypeReference) Implements Microsoft.Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return From arg In m_UnderlyingMethod.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overrides ReadOnly Property AsGenericMethodInstanceReference As Microsoft.Cci.IGenericMethodInstanceReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Microsoft.Cci.IGenericMethodInstanceReference))
        End Sub

    End Class

End Namespace

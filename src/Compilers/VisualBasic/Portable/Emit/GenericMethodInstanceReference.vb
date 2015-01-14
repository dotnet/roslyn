' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a generic method instantiation, closed over type parameters, 
    ''' e.g. MyNamespace.Class.Method{T}()
    ''' </summary>
    Friend NotInheritable Class GenericMethodInstanceReference
        Inherits MethodReference
        Implements Cci.IGenericMethodInstanceReference

        Public Sub New(underlyingMethod As MethodSymbol)
            MyBase.New(underlyingMethod)
        End Sub

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.IGenericMethodInstanceReference))
        End Sub

        Private Function IGenericMethodInstanceReferenceGetGenericArguments(context As EmitContext) As IEnumerable(Of Cci.ITypeReference) Implements Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return From arg In m_UnderlyingMethod.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private Function IGenericMethodInstanceReferenceGetGenericMethod(context As EmitContext) As Cci.IMethodReference Implements Cci.IGenericMethodInstanceReference.GetGenericMethod
            Debug.Assert(Not m_UnderlyingMethod.ContainingType.IsOrInGenericType())
            ' NoPia method might come through here.
            Return DirectCast(context.Module, PEModuleBuilder).Translate(
                m_UnderlyingMethod.OriginalDefinition,
                DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode),
                context.Diagnostics,
                needDeclaration:=True)
        End Function

        Public Overrides ReadOnly Property AsGenericMethodInstanceReference As Cci.IGenericMethodInstanceReference
            Get
                Return Me
            End Get
        End Property
    End Class
End Namespace

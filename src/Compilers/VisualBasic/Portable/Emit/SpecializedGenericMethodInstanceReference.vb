' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a generic method of a generic type instantiation, closed over type parameters.
    ''' e.g. 
    ''' A{T}.M{S}()
    ''' A.B{T}.C.M{S}()
    ''' </summary>
    Friend NotInheritable Class SpecializedGenericMethodInstanceReference
        Inherits SpecializedMethodReference
        Implements Cci.IGenericMethodInstanceReference

        Private ReadOnly _genericMethod As SpecializedMethodReference

        Public Sub New(underlyingMethod As MethodSymbol)
            MyBase.New(underlyingMethod)

            Debug.Assert(underlyingMethod.ContainingType.IsOrInGenericType() AndAlso underlyingMethod.ContainingType.IsDefinition)
            _genericMethod = New SpecializedMethodReference(underlyingMethod)
        End Sub

        Public Function GetGenericMethod(context As EmitContext) As Cci.IMethodReference Implements Cci.IGenericMethodInstanceReference.GetGenericMethod
            Return _genericMethod
        End Function

        Public Function GetGenericArguments(context As EmitContext) As IEnumerable(Of Cci.ITypeReference) Implements Cci.IGenericMethodInstanceReference.GetGenericArguments
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)

            Return From arg In m_UnderlyingMethod.TypeArguments
                   Select moduleBeingBuilt.Translate(arg, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Public Overrides ReadOnly Property AsGenericMethodInstanceReference As Cci.IGenericMethodInstanceReference
            Get
                Return Me
            End Get
        End Property

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.IGenericMethodInstanceReference))
        End Sub

    End Class

End Namespace

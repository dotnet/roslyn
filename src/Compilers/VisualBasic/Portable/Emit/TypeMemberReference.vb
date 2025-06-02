' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class TypeMemberReference
        Implements Cci.ITypeMemberReference

        Protected MustOverride ReadOnly Property UnderlyingSymbol As Symbol

        Public Overridable Function GetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
        End Function

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                Return UnderlyingSymbol.MetadataName
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return UnderlyingSymbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat)
        End Function

        Private Function IReferenceAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Cci.ICustomAttribute)()
        End Function

        Public MustOverride Sub Dispatch(visitor As Cci.MetadataVisitor) Implements Cci.IReference.Dispatch

        Private Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition Implements Cci.IReference.AsDefinition
            Return Nothing
        End Function

        Private Function IReferenceGetInternalSymbol() As CodeAnalysis.Symbols.ISymbolInternal Implements Cci.IReference.GetInternalSymbol
            Return UnderlyingSymbol
        End Function

        Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides Function GetHashCode() As Integer
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace

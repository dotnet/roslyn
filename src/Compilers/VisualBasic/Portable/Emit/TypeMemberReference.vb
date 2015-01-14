' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class TypeMemberReference
        Implements Cci.ITypeMemberReference

        Protected MustOverride ReadOnly Property UnderlyingSymbol As Symbol

        Public Overridable Function GetContainingType(context As EmitContext) As Cci.ITypeReference Implements Cci.ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return moduleBeingBuilt.Translate(UnderlyingSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
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
    End Class
End Namespace

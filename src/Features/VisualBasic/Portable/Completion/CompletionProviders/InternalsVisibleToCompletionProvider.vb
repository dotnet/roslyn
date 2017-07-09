' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        Protected Overrides Function GetAssemblyScopedAttributeSyntaxNodesOfDocument(documentRoot As SyntaxNode) As IImmutableList(Of SyntaxNode)
            Dim builder As ImmutableList(Of SyntaxNode).Builder = Nothing
            Dim compilationUnit = TryCast(documentRoot, CompilationUnitSyntax)
            If Not compilationUnit Is Nothing Then
                For Each attributeStatement In compilationUnit.Attributes
                    For Each attributeList In attributeStatement.AttributeLists
                        builder = If(builder, ImmutableList.CreateBuilder(Of SyntaxNode)())
                        builder.AddRange(attributeList.Attributes)
                    Next
                Next
            End If

            Return If(builder Is Nothing, ImmutableList(Of SyntaxNode).Empty, builder.ToImmutable())
        End Function

        Protected Overrides Function GetConstructorArgumentOfInternalsVisibleToAttribute(internalsVisibleToAttribute As SyntaxNode) As SyntaxNode
            Dim attributeSyntax = TryCast(internalsVisibleToAttribute, AttributeSyntax)
            If Not attributeSyntax Is Nothing Then
                For Each argument In attributeSyntax.ArgumentList.Arguments
                    If Not argument.IsNamed Then
                        Return argument.GetExpression()
                    End If
                Next
            End If

            Return Nothing
        End Function
    End Class
End Namespace

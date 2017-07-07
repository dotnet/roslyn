' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        Private Shared ReadOnly ExtractAttribute As AttributeNodeExtractor = New AttributeNodeExtractor()
        Private Shared ReadOnly ExtractAttributeConstructorArgument As AttributeConstructorArgumentExtractor = New AttributeConstructorArgumentExtractor()

        Protected Overrides Function GetAssemblyScopedAttributeSyntaxNodesOfDocument(documentRoot As SyntaxNode) As IImmutableList(Of SyntaxNode)
            Dim result = TryCast(documentRoot, VisualBasicSyntaxNode).Accept(ExtractAttribute)
            Return result.ToImmutableListOrEmpty()
        End Function

        Protected Overrides Function GetConstructorArgumentOfInternalsVisibleToAttribute(internalsVisibleToAttribute As SyntaxNode) As SyntaxNode
            Return TryCast(internalsVisibleToAttribute, VisualBasicSyntaxNode).Accept(ExtractAttributeConstructorArgument)
        End Function

        Private Class AttributeNodeExtractor
            Inherits VisualBasicSyntaxVisitor(Of IEnumerable(Of SyntaxNode))

            Public Overrides Iterator Function VisitCompilationUnit(node As CompilationUnitSyntax) As IEnumerable(Of SyntaxNode)
                For Each attributeStatement In node.Attributes
                    For Each attribute In attributeStatement.Accept(Me)
                        Yield attribute
                    Next
                Next
            End Function

            Public Overrides Iterator Function VisitAttributesStatement(node As AttributesStatementSyntax) As IEnumerable(Of SyntaxNode)
                For Each attributeList In node.AttributeLists
                    For Each attribute In attributeList.Accept(Me)
                        Yield attribute
                    Next
                Next
            End Function

            Public Overrides Function VisitAttributeList(node As AttributeListSyntax) As IEnumerable(Of SyntaxNode)
                Return node.Attributes
            End Function
        End Class

        Private Class AttributeConstructorArgumentExtractor
            Inherits VisualBasicSyntaxVisitor(Of SyntaxNode)

            Public Overrides Function VisitAttribute(node As AttributeSyntax) As SyntaxNode
                Return node.ArgumentList.Accept(Me)
            End Function

            Public Overrides Function VisitArgumentList(node As ArgumentListSyntax) As SyntaxNode
                For Each argument In node.Arguments
                    If Not argument.IsNamed Then
                        Return argument.GetExpression()
                    End If
                Next
                Return Nothing
            End Function
        End Class
    End Class
End Namespace

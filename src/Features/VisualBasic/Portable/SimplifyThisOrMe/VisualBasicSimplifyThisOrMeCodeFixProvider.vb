' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyThisOrMe), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class VisualBasicSimplifyThisOrMeCodeFixProvider
        Inherits AbstractSimplifyThisOrMeCodeFixProvider(Of MemberAccessExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Remove_Me_qualification
        End Function

        Protected Overrides Function Rewrite(semanticModel As SemanticModel, root As SyntaxNode, memberAccessNodes As ISet(Of MemberAccessExpressionSyntax)) As SyntaxNode
            Dim rewriter = New Rewriter(semanticModel, memberAccessNodes)
            Return rewriter.Visit(root)
        End Function

        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly semanticModel As SemanticModel
            Private ReadOnly memberAccessNodes As ISet(Of MemberAccessExpressionSyntax)

            Public Sub New(semanticModel As SemanticModel, memberAccessNodes As ISet(Of MemberAccessExpressionSyntax))
                Me.semanticModel = semanticModel
                Me.memberAccessNodes = memberAccessNodes
            End Sub

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Return If(memberAccessNodes.Contains(node),
                    node.GetNameWithTriviaMoved(semanticModel),
                    MyBase.VisitMemberAccessExpression(node))
            End Function
        End Class
    End Class
End Namespace

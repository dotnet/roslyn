' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyThisOrMe), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class VisualBasicSimplifyThisOrMeCodeFixProvider
        Inherits AbstractSimplifyThisOrMeCodeFixProvider(Of MemberAccessExpressionSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Remove_Me_qualification
        End Function

        Protected Overrides Function Rewrite(root As SyntaxNode, memberAccessNodes As ISet(Of MemberAccessExpressionSyntax)) As SyntaxNode
            Return New Rewriter(memberAccessNodes).Visit(root)
        End Function

        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly memberAccessNodes As ISet(Of MemberAccessExpressionSyntax)

            Public Sub New(memberAccessNodes As ISet(Of MemberAccessExpressionSyntax))
                Me.memberAccessNodes = memberAccessNodes
            End Sub

            Public Overrides Function VisitMemberAccessExpression(node As MemberAccessExpressionSyntax) As SyntaxNode
                Return If(memberAccessNodes.Contains(node),
                    node.GetNameWithTriviaMoved(),
                    MyBase.VisitMemberAccessExpression(node))
            End Function
        End Class
    End Class
End Namespace

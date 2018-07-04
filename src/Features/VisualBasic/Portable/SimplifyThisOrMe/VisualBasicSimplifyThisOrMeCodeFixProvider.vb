' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.SimplifyThisOrMe
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SimplifyThisOrMe

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SimplifyThisOrMe), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.SpellCheck)>
    Partial Friend Class VisualBasicSimplifyThisOrMeCodeFixProvider
        Inherits AbstractSimplifyThisOrMeCodeFixProvider(Of MemberAccessExpressionSyntax)

        Public Sub New()
        End Sub

        Protected Overrides Function GetTitle() As String
            Return VBFeaturesResources.Remove_Me_qualification
        End Function

        Protected Overrides Function GetNameWithTriviaMoved(semanticModel As SemanticModel, memberAccess As MemberAccessExpressionSyntax) As SyntaxNode
            Dim replacementNode = memberAccess.Name
            replacementNode =
                replacementNode.WithIdentifier(
                    VisualBasicSimplificationService.TryEscapeIdentifierToken(
                        memberAccess.Name.Identifier,
                        semanticModel)).
                    WithLeadingTrivia(memberAccess.GetLeadingTriviaForSimplifiedMemberAccess()).
                    WithTrailingTrivia(memberAccess.GetTrailingTrivia())
            Return replacementNode
        End Function
    End Class
End Namespace

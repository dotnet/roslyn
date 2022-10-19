' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
    <ExportArgumentProvider(NameOf(ContextVariableArgumentProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(FirstBuiltInArgumentProvider))>
    <[Shared]>
    Friend Class ContextVariableArgumentProvider
        Inherits AbstractContextVariableArgumentProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property ThisOrMeKeyword As String
            Get
                Return SyntaxFacts.GetText(SyntaxKind.MeKeyword)
            End Get
        End Property

        Protected Overrides Function IsInstanceContext(syntaxTree As SyntaxTree, targetToken As SyntaxToken, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean
            ' Uses logic from MeKeywordRecommender, restricted by knowledge that this is an invocation within an
            ' expression
            If targetToken.GetInnermostDeclarationContext().IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock) Then
                If targetToken.GetContainingMemberBlockBegin().TypeSwitch(
                    Function(methodBase As MethodBaseSyntax) Not methodBase.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(propertyStatement As PropertyStatementSyntax) Not propertyStatement.Modifiers.Any(SyntaxKind.SharedKeyword),
                    Function(eventStatement As EventStatementSyntax) Not eventStatement.Modifiers.Any(SyntaxKind.SharedKeyword)) Then

                    Return True
                End If

                Dim containingMember = targetToken.GetContainingMember()
                If TypeOf containingMember Is FieldDeclarationSyntax Then
                    Dim fieldDecl = DirectCast(containingMember, FieldDeclarationSyntax)
                    If Not fieldDecl.Modifiers.Any(SyntaxKind.SharedKeyword) Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function
    End Class
End Namespace

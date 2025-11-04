' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.IntroduceParameter
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceParameter
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.IntroduceParameter), [Shared]>
    Friend Class VisualBasicIntroduceParameterCodeRefactoringProvider
        Inherits AbstractIntroduceParameterCodeRefactoringProvider(Of ExpressionSyntax, InvocationExpressionSyntax, ObjectCreationExpressionSyntax, IdentifierNameSyntax, ArgumentSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GenerateExpressionFromOptionalParameter(parameterSymbol As IParameterSymbol) As SyntaxNode
            Return GenerateExpression(VisualBasicSyntaxGenerator.Instance, parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference:=True)
        End Function

        Protected Overrides Function GetLocalDeclarationFromDeclarator(variableDecl As SyntaxNode) As SyntaxNode
            Return TryCast(variableDecl.Parent, LocalDeclarationStatementSyntax)
        End Function

        Protected Overrides Function UpdateArgumentListSyntax(node As SyntaxNode, arguments As SeparatedSyntaxList(Of ArgumentSyntax)) As SyntaxNode
            Return DirectCast(node, ArgumentListSyntax).WithArguments(arguments)
        End Function

        Protected Overrides Function IsDestructor(methodSymbol As IMethodSymbol) As Boolean
            Return methodSymbol.Name.Equals(WellKnownMemberNames.DestructorName)
        End Function

        Protected Overrides Function IsAnonymousObjectMemberDeclaratorNameIdentifier(expression As SyntaxNode) As Boolean
            ' Check if this expression is the name identifier in an anonymous object member declarator.
            ' In VB, the structure for anonymous objects with explicit names is: IdentifierNameSyntax -> NamedFieldInitializerSyntax
            ' We want to return true when expression is the identifier on the left side in something like 'New With { .a = value }'
            Dim identifier = TryCast(expression, IdentifierNameSyntax)
            If identifier Is Nothing Then
                Return False
            End If

            Dim namedFieldInit = TryCast(identifier.Parent, NamedFieldInitializerSyntax)
            If namedFieldInit Is Nothing Then
                Return False
            End If

            ' Check if this is part of an anonymous object (not a regular object initializer)
            ' Anonymous object initializers are inside AnonymousObjectCreationExpressionSyntax
            If Not (TypeOf namedFieldInit.Parent Is AnonymousObjectCreationExpressionSyntax) Then
                Return False
            End If

            ' Verify that the identifier is the Name part of NamedFieldInitializerSyntax
            Return namedFieldInit.Name Is identifier
        End Function
    End Class
End Namespace

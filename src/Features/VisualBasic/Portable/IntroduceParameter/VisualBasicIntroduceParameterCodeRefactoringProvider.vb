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
    End Class
End Namespace

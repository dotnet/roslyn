﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.IntroduceVariable
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.IntroduceVariable
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.IntroduceParameter), [Shared]>
    Friend Class VisualBasicIntroduceParameterCodeRefactoringProvider
        Inherits AbstractIntroduceParameterService(Of ExpressionSyntax, InvocationExpressionSyntax, ObjectCreationExpressionSyntax, IdentifierNameSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function GenerateExpressionFromOptionalParameter(parameterSymbol As IParameterSymbol) As SyntaxNode
            Return GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference:=True)
        End Function

        Protected Overrides Function GetLocalDeclarationFromDeclarator(variableDecl As SyntaxNode) As SyntaxNode
            Return TryCast(variableDecl.Parent, LocalDeclarationStatementSyntax)
        End Function

        Protected Overrides Function UpdateArgumentListSyntax(node As SyntaxNode, arguments As SeparatedSyntaxList(Of SyntaxNode)) As SyntaxNode
            Return DirectCast(node, ArgumentListSyntax).WithArguments(arguments)
        End Function

        Protected Overrides Function IsDestructor(methodSymbol As IMethodSymbol) As Boolean
            Return methodSymbol.Name.Equals(WellKnownMemberNames.DestructorName)
        End Function
    End Class
End Namespace

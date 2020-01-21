' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertTupleToStruct
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertTupleToStruct
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct), [Shared]>
    Friend Class VisualBasicConvertTupleToStructCodeRefactoringProvider
        Inherits AbstractConvertTupleToStructCodeRefactoringProvider(Of
            ExpressionSyntax,
            NameSyntax,
            IdentifierNameSyntax,
            LiteralExpressionSyntax,
            ObjectCreationExpressionSyntax,
            TupleExpressionSyntax,
            ArgumentSyntax,
            TupleTypeSyntax,
            TypeBlockSyntax,
            NamespaceBlockSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateObjectCreationExpression(
                nameNode As NameSyntax, openParen As SyntaxToken, arguments As SeparatedSyntaxList(Of ArgumentSyntax), closeParen As SyntaxToken) As ObjectCreationExpressionSyntax

            Return SyntaxFactory.ObjectCreationExpression(
                attributeLists:=Nothing, nameNode, SyntaxFactory.ArgumentList(openParen, arguments, closeParen), initializer:=Nothing)
        End Function
    End Class
End Namespace

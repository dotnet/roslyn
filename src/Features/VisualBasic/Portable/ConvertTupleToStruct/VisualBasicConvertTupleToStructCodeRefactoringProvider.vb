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
            ObjectCreationExpressionSyntax,
            TupleExpressionSyntax,
            TupleTypeSyntax,
            TypeBlockSyntax,
            NamespaceBlockSyntax)

        Protected Overrides Function CreateObjectCreationExpression(
            nameNode As NameSyntax, tuple As TupleExpressionSyntax) As ObjectCreationExpressionSyntax

            Return SyntaxFactory.ObjectCreationExpression(
                attributeLists:=Nothing, nameNode, CreateArgumentList(tuple), initializer:=Nothing)
        End Function

        Private Function CreateArgumentList(tuple As TupleExpressionSyntax) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(
                tuple.OpenParenToken,
                SyntaxFactory.SeparatedList(Of ArgumentSyntax)(tuple.Arguments.GetWithSeparators()),
                tuple.CloseParenToken)
        End Function
    End Class
End Namespace

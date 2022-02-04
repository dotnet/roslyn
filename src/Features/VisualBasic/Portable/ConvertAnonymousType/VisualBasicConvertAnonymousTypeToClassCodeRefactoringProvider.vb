' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertAnonymousType
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousType
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.ConvertAnonymousTypeToClass), [Shared]>
    Friend Class VisualBasicConvertAnonymousTypeToClassCodeRefactoringProvider
        Inherits AbstractConvertAnonymousTypeToClassCodeRefactoringProvider(Of
            ExpressionSyntax,
            NameSyntax,
            IdentifierNameSyntax,
            ObjectCreationExpressionSyntax,
            AnonymousObjectCreationExpressionSyntax,
            NamespaceBlockSyntax)

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Protected Overrides Function CreateObjectCreationExpression(
            nameNode As NameSyntax, anonymousObject As AnonymousObjectCreationExpressionSyntax) As ObjectCreationExpressionSyntax

            Return SyntaxFactory.ObjectCreationExpression(
                attributeLists:=Nothing, nameNode, CreateArgumentList(anonymousObject.Initializer), initializer:=Nothing)
        End Function

        Private Function CreateArgumentList(initializer As ObjectMemberInitializerSyntax) As ArgumentListSyntax
            Return SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTriviaFrom(initializer.OpenBraceToken),
                CreateArguments(initializer.Initializers),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTriviaFrom(initializer.CloseBraceToken))
        End Function

        Private Function CreateArguments(initializers As SeparatedSyntaxList(Of FieldInitializerSyntax)) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return SyntaxFactory.SeparatedList(Of ArgumentSyntax)(CreateArguments(initializers.GetWithSeparators()))
        End Function

        Private Function CreateArguments(list As SyntaxNodeOrTokenList) As SyntaxNodeOrTokenList
            Return New SyntaxNodeOrTokenList(list.Select(AddressOf CreateArgumentOrComma))
        End Function

        Private Function CreateArgumentOrComma(declOrComma As SyntaxNodeOrToken) As SyntaxNodeOrToken
            Return If(declOrComma.IsToken,
                       declOrComma,
                       CreateArgument(CType(declOrComma, FieldInitializerSyntax)))
        End Function

        Private Shared Function CreateArgument(initializer As FieldInitializerSyntax) As ArgumentSyntax
            Dim expression = If(TryCast(initializer, InferredFieldInitializerSyntax)?.Expression,
                                TryCast(initializer, NamedFieldInitializerSyntax)?.Expression)
            Return SyntaxFactory.SimpleArgument(expression)
        End Function
    End Class
End Namespace

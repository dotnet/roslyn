' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.UseNamedArguments
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseNamedArguments
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.IntroduceVariable)>
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicUseNamedArgumentsCodeRefactoringProvider)), [Shared]>
    Friend Class VisualBasicUseNamedArgumentsCodeRefactoringProvider
        Inherits AbstractUseNamedArgumentsCodeRefactoringProvider

        Private Class ArgumentAnalyzer
            Inherits Analyzer(Of ArgumentSyntax, SimpleArgumentSyntax, ArgumentListSyntax)

            Protected Overrides Function IsPositionalArgument(argument As SimpleArgumentSyntax) As Boolean
                Return argument.NameColonEquals Is Nothing
            End Function

            Protected Overrides Function GetArguments(argumentList As ArgumentListSyntax) As SeparatedSyntaxList(Of ArgumentSyntax)
                Return argumentList.Arguments
            End Function

            Protected Overrides Function GetReceiver(argument As SyntaxNode) As SyntaxNode
                If argument.Parent.IsParentKind(SyntaxKind.Attribute) Then
                    Return Nothing
                End If

                Return argument.Parent.Parent
            End Function

            Protected Overrides Function WithName(argument As SimpleArgumentSyntax, name As String) As SimpleArgumentSyntax
                Return argument.WithNameColonEquals(SyntaxFactory.NameColonEquals(name.ToIdentifierName()))
            End Function

            Protected Overrides Function WithArguments(argumentList As ArgumentListSyntax, namedArguments As IEnumerable(Of ArgumentSyntax), separators As IEnumerable(Of SyntaxToken)) As ArgumentListSyntax
                Return argumentList.WithArguments(SyntaxFactory.SeparatedList(namedArguments, separators))
            End Function

            Protected Overrides Function IsLegalToAddNamedArguments(parameters As ImmutableArray(Of IParameterSymbol), argumentCount As Integer) As Boolean
                Return Not parameters.LastOrDefault().IsParams OrElse parameters.Length > argumentCount
            End Function

            Protected Overrides Function IsCloseParenOrComma(token As SyntaxToken) As Boolean
                Return token.IsKind(SyntaxKind.CloseParenToken, SyntaxKind.CommaToken)
            End Function

            Protected Overrides Function SupportsNonTrailingNamedArguments(options As ParseOptions) As Boolean
                Return DirectCast(options, VisualBasicParseOptions).LanguageVersion >= LanguageVersion.VisualBasic15_5
            End Function
        End Class

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(New ArgumentAnalyzer(), attributeArgumentAnalyzer:=Nothing)
        End Sub
    End Class
End Namespace

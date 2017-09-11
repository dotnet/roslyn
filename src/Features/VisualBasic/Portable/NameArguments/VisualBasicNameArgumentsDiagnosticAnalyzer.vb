' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.NameArguments
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.NameArguments
    ''' <summary>
    ''' Reports arguments that should be named, based on style preferences.
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicNameArgumentsDiagnosticAnalyzer
        Inherits AbstractNameArgumentsDiagnosticAnalyzer

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(Sub(c As SyntaxNodeAnalysisContext) AnalyzeSyntax(c),
                                             SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression,
                                             SyntaxKind.Attribute)
        End Sub

        Protected Overrides Sub ReportDiagnosticIfNeeded(context As SyntaxNodeAnalysisContext, optionSet As OptionSet,
                                                      parameters As ImmutableArray(Of IParameterSymbol))

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax)
            Select Case context.Node.Kind()
                Case SyntaxKind.InvocationExpression
                    arguments = DirectCast(context.Node, InvocationExpressionSyntax).ArgumentList.Arguments

                Case SyntaxKind.ObjectCreationExpression
                    arguments = DirectCast(context.Node, ObjectCreationExpressionSyntax).ArgumentList.Arguments

                Case SyntaxKind.RaiseEventStatement
                    arguments = DirectCast(context.Node, RaiseEventStatementSyntax).ArgumentList.Arguments

                Case SyntaxKind.Attribute
                    arguments = DirectCast(context.Node, AttributeSyntax).ArgumentList.Arguments

                Case Else
                    Return
            End Select

            ReportDiagnosticIfNeeded(context, optionSet, parameters, arguments)
        End Sub

        Private Overloads Sub ReportDiagnosticIfNeeded(context As SyntaxNodeAnalysisContext, optionSet As OptionSet,
                                                       parameters As ImmutableArray(Of IParameterSymbol),
                                                       arguments As SeparatedSyntaxList(Of ArgumentSyntax))

            For i As Integer = 0 To arguments.Count - 1

                Dim argument = TryCast(arguments(i), SimpleArgumentSyntax)
                If argument Is Nothing Then
                    Continue For
                End If

                If argument.NameColonEquals IsNot Nothing OrElse
                    Not argument.Expression.IsAnyLiteralExpression() OrElse
                    parameters(i).IsParams Then

                    Continue For
                End If

                ReportDiagnostic(context, optionSet, argument, parameters(i).Name)
            Next
        End Sub

        Protected Overrides Function LanguageSupportsNonTrailingNamedArguments(options As ParseOptions) As Boolean
            Return DirectCast(options, VisualBasicParseOptions).LanguageVersion >= LanguageVersion.VisualBasic15_5
        End Function
    End Class
End Namespace

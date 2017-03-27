' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.ValidateFormatString

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class ValidateFormatStringDiagnosticAnalyzer
        Inherits AbstractValidateFormatStringDiagnosticAnalyzer

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.InvocationExpression)
        End Sub

        Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim invocationExpr = CType(context.Node, InvocationExpressionSyntax)
            If (invocationExpr.ArgumentList?.Arguments Is Nothing) Then
                Return
            End If

            Dim name As SimpleNameSyntax
            ' When calling string.Format(...), Format will be of type MemberAccessExpressionSyntax 
            Dim memberAccessExpr = TryCast(invocationExpr.Expression, MemberAccessExpressionSyntax)
            ' When using static System.String and calling Format(...), Format will be of type IdentifierNameSyntax
            Dim identifierNameSyntax = TryCast(invocationExpr.Expression, IdentifierNameSyntax)
            If (memberAccessExpr IsNot Nothing AndAlso memberAccessExpr.Name IsNot Nothing AndAlso memberAccessExpr.Name.ToString() = "Format") Then
                name = memberAccessExpr.Name
            ElseIf (identifierNameSyntax IsNot Nothing AndAlso identifierNameSyntax.ToString() = "Format") Then
                name = identifierNameSyntax
            Else
                Return
            End If


            Dim arguments = invocationExpr.ArgumentList.Arguments
            Dim numberOfArguments = arguments.Count

            Dim symbolInfo = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken)

            Dim method As IMethodSymbol = Nothing

            If (Not TryGetFormatMethod(context, numberOfArguments, symbolInfo, method)) Then
                Return
            End If

            Dim hasIFormatProvider = (method.Parameters(0).Type.ToString() = GetType(IFormatProvider).ToString())

            Dim formatString As String = Nothing
            Dim formatStringLocation As Location = Nothing

            If (Not TryGetFormatStringAndLocation(arguments, hasIFormatProvider, formatString, formatStringLocation)) Then
                Return
            End If

            ValidateAndReportDiagnostic(context, hasIFormatProvider, arguments.Count, formatString, formatStringLocation)
        End Sub

        Private Function TryGetFormatStringAndLocation(arguments As SeparatedSyntaxList(Of ArgumentSyntax), hasIFormatProvider As Boolean, ByRef formatString As String, ByRef formatStringLocation As Location) As Boolean
            formatString = Nothing
            formatStringLocation = Nothing

            Dim formatArgumentSyntax = GetFormatStringArgument(arguments, hasIFormatProvider)

            If (formatArgumentSyntax Is Nothing) Then
                Return False
            End If

            If (Not formatArgumentSyntax.IsKind(SyntaxKind.SimpleArgument)) Then
                Return False
            End If

            Dim simpleArguementSyntax = TryCast(formatArgumentSyntax, SimpleArgumentSyntax)
            If (simpleArguementSyntax Is Nothing) Then
                Return False
            End If

            If (simpleArguementSyntax.Expression Is Nothing) Then
                Return False
            End If

            formatString = simpleArguementSyntax.Expression.ToString
            formatStringLocation = formatArgumentSyntax.GetLocation

            Return True
        End Function

        Private Function GetFormatStringArgument(arguments As SeparatedSyntaxList(Of ArgumentSyntax), hasIFormatProvider As Boolean) As ArgumentSyntax
            For Each argument In arguments
                If (argument.IsNamed) Then
                    Dim simpleArgumentSyntax = TryCast(argument, SimpleArgumentSyntax)
                    If simpleArgumentSyntax IsNot Nothing AndAlso simpleArgumentSyntax.NameColonEquals.Name.Identifier.ValueText.Equals("format") Then
                        Return argument
                    End If
                End If
            Next
            ' If using positional arguments, the format string will be the first or second
            ' argument depending on whether there Is an IFormatProvider argument.
            Return If(hasIFormatProvider, arguments(1), arguments(0))
        End Function
    End Class
End Namespace

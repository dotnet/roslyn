' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression
    ''' <summary>
    ''' Looks for code of the forms:
    ''' 
    '''     if not x is ...
    ''' 
    ''' and converts it to:
    ''' 
    '''     if x isnot ...
    '''     
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Partial Friend Class VisualBasicUseIsNotExpressionDiagnosticAnalyzer
        Inherits AbstractBuiltInCodeStyleDiagnosticAnalyzer

        Public Sub New()
            MyBase.New(IDEDiagnosticIds.UseIsNotExpressionDiagnosticId,
                   VisualBasicCodeStyleOptions.PreferIsNotExpression,
                   LanguageNames.VisualBasic,
                   New LocalizableResourceString(
                        NameOf(VisualBasicAnalyzersResources.Use_IsNot_expression), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End Sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf SyntaxNodeAction, SyntaxKind.NotExpression)
        End Sub

        Private Sub SyntaxNodeAction(syntaxContext As SyntaxNodeAnalysisContext)
            Dim node = syntaxContext.Node
            Dim syntaxTree = node.SyntaxTree

            ' "x is not Type y" is only available in C# 9.0 and above. Don't offer this refactoring
            ' in projects targeting a lesser version.
            If DirectCast(syntaxTree.Options, VisualBasicParseOptions).LanguageVersion < LanguageVersion.VisualBasic14 Then
                Return
            End If

            Dim options = syntaxContext.Options
            Dim cancellationToken = syntaxContext.CancellationToken

            ' Bail immediately if the user has disabled this feature.
            Dim styleOption = options.GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, syntaxTree, cancellationToken)
            If Not styleOption.Value Then
                Return
            End If

            Dim notExpression = DirectCast(node, UnaryExpressionSyntax)
            Dim operand = notExpression.Operand

            ' Look for the form: not x is y, or not typeof x is y
            If Not operand.IsKind(SyntaxKind.IsExpression) AndAlso Not operand.IsKind(SyntaxKind.TypeOfIsExpression) Then
                Return
            End If

            Dim isKeyword = If(operand.IsKind(SyntaxKind.IsExpression),
                DirectCast(operand, BinaryExpressionSyntax).OperatorToken,
                DirectCast(operand, TypeOfExpressionSyntax).OperatorToken)

            ' Put a diagnostic with the appropriate severity on `is` keyword.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                isKeyword.GetLocation(),
                styleOption.Notification.Severity,
                ImmutableArray.Create(notExpression.GetLocation()),
                properties:=Nothing))
        End Sub
    End Class
End Namespace

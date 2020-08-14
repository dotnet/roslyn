' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

imports System.Collections.Immutable
imports Microsoft.CodeAnalysis.CodeStyle
imports Microsoft.CodeAnalysis.Diagnostics
imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
imports Microsoft.CodeAnalysis.VisualBasic.Syntax

namespace Microsoft.CodeAnalysis.VisualBasic.UseIsNotExpression
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
    partial friend class VisualBasicUseIsNotExpressionDiagnosticAnalyzer
        inherits AbstractBuiltInCodeStyleDiagnosticAnalyzer

        public sub new()
            MyBase.new(IDEDiagnosticIds.UseIsNotExpressionDiagnosticId,
                   VisualBasicCodeStyleOptions.PreferIsNotExpression,
                   LanguageNames.VisualBasic,
                   new LocalizableResourceString(
                        nameof(VisualBasicAnalyzersResources.Use_isnot_expression), VisualBasicAnalyzersResources.ResourceManager, GetType(VisualBasicAnalyzersResources)))
        End sub

        Public Overrides Function GetAnalyzerCategory() As DiagnosticAnalyzerCategory
            Return DiagnosticAnalyzerCategory.SemanticSpanAnalysis
        End Function

        Protected Overrides Sub InitializeWorker(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(addressof SyntaxNodeAction, SyntaxKind.NotExpression)
        End sub

        private sub SyntaxNodeAction(syntaxContext As SyntaxNodeAnalysisContext)
            dim node = syntaxContext.Node
            dim syntaxTree = node.SyntaxTree

            ' "x is not Type y" is only available in C# 9.0 and above. Don't offer this refactoring
            ' in projects targeting a lesser version.
            if directcast(syntaxTree.Options, VisualBasicparseoptions).LanguageVersion < LanguageVersion.VisualBasic14
                return
            End if

            dim options = syntaxContext.Options
            dim cancellationToken = syntaxContext.CancellationToken

            ' Bail immediately if the user has disabled this feature.
            dim styleOption = options.GetOption(VisualBasicCodeStyleOptions.PreferIsNotExpression, syntaxTree, cancellationToken)
            if Not styleOption.Value
                return
            End if

            Dim notExpression = DirectCast(node, UnaryExpressionSyntax)
            Dim operand = notExpression.Operand

            ' Look for the form: not x is y, or not typeof x is y
            If not operand.IsKind(SyntaxKind.IsExpression) AndAlso Not operand.IsKind(SyntaxKind.TypeOfIsExpression)
                Return
            End if

            Dim isKeyword = if(operand.IsKind(SyntaxKind.IsExpression),
                DirectCast(operand, BinaryExpressionSyntax).OperatorToken,
                DirectCast(operand, TypeOfExpressionSyntax).OperatorToken)

            ' Put a diagnostic with the appropriate severity on `is` keyword.
            syntaxContext.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                isKeyword.GetLocation(),
                styleOption.Notification.Severity,
                ImmutableArray.Create(notExpression.GetLocation()),
                properties:=nothing))
        End sub
    End Class
end namespace

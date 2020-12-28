' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private Class VisualBasicCodeGenerator
            Private Class SingleStatementCodeGenerator
                Inherits VisualBasicCodeGenerator

                Public Sub New(insertionPoint As InsertionPoint, selectionResult As SelectionResult, analyzerResult As AnalyzerResult)
                    MyBase.New(insertionPoint, selectionResult, analyzerResult)
                End Sub

                Public Shared Function IsExtractMethodOnSingleStatement(code As SelectionResult) As Boolean
                    Dim result = DirectCast(code, VisualBasicSelectionResult)
                    Dim firstStatement = result.GetFirstStatement()
                    Dim lastStatement = result.GetLastStatement()

                    Return firstStatement Is lastStatement OrElse firstStatement.Span.Contains(lastStatement.Span)
                End Function

                Protected Overrides Function CreateMethodName() As SyntaxToken
                    ' change this to more smarter one.
                    Dim semanticModel = CType(SemanticDocument.SemanticModel, SemanticModel)
                    Dim nameGenerator = New UniqueNameGenerator(semanticModel)
                    Dim containingScope = VBSelectionResult.GetContainingScope()
                    Return SyntaxFactory.Identifier(
                        nameGenerator.CreateUniqueMethodName(containingScope, "NewMethod"))
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As ImmutableArray(Of StatementSyntax)
                    Contract.ThrowIfFalse(IsExtractMethodOnSingleStatement(VBSelectionResult))

                    Return ImmutableArray.Create(Of StatementSyntax)(VBSelectionResult.GetFirstStatement())
                End Function

                Protected Overrides Function GetOutermostCallSiteContainerToProcess(cancellationToken As CancellationToken) As SyntaxNode
                    Dim callSiteContainer = GetCallSiteContainerFromOutermostMoveInVariable(cancellationToken)
                    If callSiteContainer IsNot Nothing Then
                        Return callSiteContainer
                    Else
                        Dim first = VBSelectionResult.GetFirstStatement()
                        Return first.Parent
                    End If
                End Function

                Protected Overrides Function GetFirstStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return VBSelectionResult.GetFirstStatement()
                End Function

                Protected Overrides Function GetLastStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    ' it is a single statement case. either first statement is same as last statement or
                    ' last statement belongs (embedded statement) to the first statement.
                    Return VBSelectionResult.GetFirstStatement()
                End Function

                Protected Overrides Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Return Task.FromResult(GetStatementContainingInvocationToExtractedMethodWorker().WithAdditionalAnnotations(CallSiteAnnotation))
                End Function
            End Class
        End Class
    End Class
End Namespace

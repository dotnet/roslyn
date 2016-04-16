' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As IEnumerable(Of StatementSyntax)
                    Contract.ThrowIfFalse(IsExtractMethodOnSingleStatement(VBSelectionResult))

                    Return SpecializedCollections.SingletonEnumerable(Of StatementSyntax)(VBSelectionResult.GetFirstStatement())
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

                Protected Overrides Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(callSiteAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Return Task.FromResult(GetStatementContainingInvocationToExtractedMethodWorker().WithAdditionalAnnotations(callSiteAnnotation))
                End Function
            End Class
        End Class
    End Class
End Namespace

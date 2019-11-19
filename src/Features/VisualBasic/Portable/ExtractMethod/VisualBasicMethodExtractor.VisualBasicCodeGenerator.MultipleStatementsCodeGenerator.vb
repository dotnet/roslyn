' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private Class VisualBasicCodeGenerator
            Private Class MultipleStatementsCodeGenerator
                Inherits VisualBasicCodeGenerator

                Public Sub New(insertionPoint As InsertionPoint, selectionResult As SelectionResult, analyzerResult As AnalyzerResult)
                    MyBase.New(insertionPoint, selectionResult, analyzerResult)
                End Sub

                Public Shared Function IsExtractMethodOnMultipleStatements(code As SelectionResult) As Boolean
                    Dim result = DirectCast(code, VisualBasicSelectionResult)
                    Dim first = result.GetFirstStatement()
                    Dim last = result.GetLastStatement()
                    If first IsNot last Then
                        Dim firstUnderContainer = result.GetFirstStatementUnderContainer()
                        Dim lastUnderContainer = result.GetLastStatementUnderContainer()
                        Contract.ThrowIfFalse(firstUnderContainer.Parent Is lastUnderContainer.Parent)
                        Return True
                    End If

                    Return False
                End Function

                Protected Overrides Function CreateMethodName(generateLocalFunction As Boolean) As SyntaxToken
                    ' change this to more smarter one.
                    Dim semanticModel = SemanticDocument.SemanticModel
                    Dim nameGenerator = New UniqueNameGenerator(semanticModel)
                    Dim containingScope = Me.VBSelectionResult.GetContainingScope()
                    Return SyntaxFactory.Identifier(nameGenerator.CreateUniqueMethodName(containingScope, "NewMethod"))
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As IEnumerable(Of StatementSyntax)
                    Dim firstStatementUnderContainer = Me.VBSelectionResult.GetFirstStatementUnderContainer()
                    Dim lastStatementUnderContainer = Me.VBSelectionResult.GetLastStatementUnderContainer()

                    Dim statements = firstStatementUnderContainer.Parent.GetStatements()

                    Dim firstStatementIndex = statements.IndexOf(firstStatementUnderContainer)
                    Contract.ThrowIfFalse(firstStatementIndex >= 0)

                    Dim lastStatementIndex = statements.IndexOf(lastStatementUnderContainer)
                    Contract.ThrowIfFalse(lastStatementIndex >= 0)

                    Dim nodes = statements.
                                Skip(firstStatementIndex).
                                Take(lastStatementIndex - firstStatementIndex + 1)

                    Return nodes
                End Function

                Protected Overrides Function GetOutermostCallSiteContainerToProcess(cancellationToken As CancellationToken) As SyntaxNode
                    Dim callSiteContainer = GetCallSiteContainerFromOutermostMoveInVariable(cancellationToken)
                    Return If(callSiteContainer, Me.VBSelectionResult.GetFirstStatementUnderContainer().Parent)
                End Function

                Protected Overrides Function GetFirstStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return Me.VBSelectionResult.GetFirstStatementUnderContainer()
                End Function

                Protected Overrides Function GetLastStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return Me.VBSelectionResult.GetLastStatementUnderContainer()
                End Function

                Protected Overrides Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(callSiteAnnotation As SyntaxAnnotation, cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Return Task.FromResult(GetStatementContainingInvocationToExtractedMethodWorker().WithAdditionalAnnotations(callSiteAnnotation))
                End Function
            End Class
        End Class
    End Class
End Namespace

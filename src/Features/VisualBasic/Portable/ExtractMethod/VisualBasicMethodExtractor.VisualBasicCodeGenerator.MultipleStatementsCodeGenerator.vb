﻿' Licensed to the .NET Foundation under one or more agreements.
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

                Protected Overrides Function CreateMethodName() As SyntaxToken
                    ' change this to more smarter one.
                    Dim semanticModel = SemanticDocument.SemanticModel
                    Dim nameGenerator = New UniqueNameGenerator(semanticModel)
                    Dim containingScope = Me.VBSelectionResult.GetContainingScope()
                    Return SyntaxFactory.Identifier(nameGenerator.CreateUniqueMethodName(containingScope, "NewMethod"))
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As ImmutableArray(Of StatementSyntax)
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

                    Return nodes.ToImmutableArray()
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

                Protected Overrides Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Return Task.FromResult(GetStatementContainingInvocationToExtractedMethodWorker().WithAdditionalAnnotations(CallSiteAnnotation))
                End Function
            End Class
        End Class
    End Class
End Namespace

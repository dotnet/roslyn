' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
    Partial Friend Class VisualBasicMethodExtractor
        Partial Private Class VisualBasicCodeGenerator
            Private Class SingleStatementCodeGenerator
                Inherits VisualBasicCodeGenerator

                Public Sub New(selectionResult As VisualBasicSelectionResult, analyzerResult As AnalyzerResult, options As VisualBasicCodeGenerationOptions)
                    MyBase.New(selectionResult, analyzerResult, options)
                End Sub

                Protected Overrides Function CreateMethodName() As SyntaxToken
                    ' change this to more smarter one.
                    Dim semanticModel = CType(SemanticDocument.SemanticModel, SemanticModel)
                    Dim nameGenerator = New UniqueNameGenerator(semanticModel)
                    Dim containingScope = Me.SelectionResult.GetContainingScope()
                    Return SyntaxFactory.Identifier(
                        nameGenerator.CreateUniqueMethodName(containingScope, "NewMethod"))
                End Function

                Protected Overrides Function GetInitialStatementsForMethodDefinitions() As ImmutableArray(Of StatementSyntax)
                    Contract.ThrowIfFalse(Me.SelectionResult.IsExtractMethodOnSingleStatement())

                    Return ImmutableArray.Create(Of StatementSyntax)(Me.SelectionResult.GetFirstStatement())
                End Function

                Protected Overrides Function GetFirstStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    Return Me.SelectionResult.GetFirstStatement()
                End Function

                Protected Overrides Function GetLastStatementOrInitializerSelectedAtCallSite() As StatementSyntax
                    ' it is a single statement case. either first statement is same as last statement or
                    ' last statement belongs (embedded statement) to the first statement.
                    Return Me.SelectionResult.GetFirstStatement()
                End Function

                Protected Overrides Function GetStatementOrInitializerContainingInvocationToExtractedMethodAsync(cancellationToken As CancellationToken) As Task(Of StatementSyntax)
                    Return Task.FromResult(GetStatementContainingInvocationToExtractedMethodWorker().WithAdditionalAnnotations(CallSiteAnnotation))
                End Function
            End Class
        End Class
    End Class
End Namespace

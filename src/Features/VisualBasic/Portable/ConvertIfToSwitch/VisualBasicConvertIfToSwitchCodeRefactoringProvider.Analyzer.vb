' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    Partial Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer

            Public Sub New(syntaxFacts As ISyntaxFacts, features As Feature)
                MyBase.New(syntaxFacts, features)
            End Sub

            Public Overrides Function HasUnreachableEndPoint(operation As IOperation) As Boolean
                Dim statements = operation.Syntax.GetStatements()
                Return Not (statements.Count = 0 OrElse operation.SemanticModel.AnalyzeControlFlow(statements.First(), statements.Last()).EndPointIsReachable)
            End Function

            Public Overrides Function CanConvert(operation As IConditionalOperation) As Boolean
                Select Case operation.Syntax.Kind
                    Case SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.SingleLineIfStatement,
                         SyntaxKind.ElseIfBlock
                        Return True
                    Case Else
                        Return False
                End Select
            End Function
        End Class
    End Class
End Namespace


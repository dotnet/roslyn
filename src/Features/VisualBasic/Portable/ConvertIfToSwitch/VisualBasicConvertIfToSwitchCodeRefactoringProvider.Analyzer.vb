' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 9.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertIfToSwitch
    Partial Friend NotInheritable Class VisualBasicConvertIfToSwitchCodeRefactoringProvider
        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer

            Public Overrides ReadOnly Property SupportsCaseGuard As Boolean = False
            Public Overrides ReadOnly Property SupportsRangePattern As Boolean = True
            Public Overrides ReadOnly Property SupportsTypePattern As Boolean = False
            Public Overrides ReadOnly Property SupportsSourcePattern As Boolean = False
            Public Overrides ReadOnly Property SupportsRelationalPattern As Boolean = True
            Public Overrides ReadOnly Property SupportsSwitchExpression As Boolean = False

            Public Sub New(syntaxFacts As ISyntaxFactsService)
                MyBase.New(syntaxFacts)
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


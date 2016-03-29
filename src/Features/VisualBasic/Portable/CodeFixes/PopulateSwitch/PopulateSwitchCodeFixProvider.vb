Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddLabelsToSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class PopulateSwitchCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(IDEDiagnosticIds.PopulateSwitchDiagnosticId)
            End Get
        End Property

        Private Shared Function GetSelectBlockNode(root As SyntaxNode, span As TextSpan) As SelectBlockSyntax
            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span)
                Return Nothing
            End If

            Return DirectCast(root.FindNode(span), SelectBlockSyntax)
        End Function
        
        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim node = GetSelectBlockNode(root, span)
            If node Is Nothing Then
                Return
            End If

            context.RegisterCodeFix(
                New MyCodeAction(
                    VBFeaturesResources.AddSelectLabels,
                    Function(c) AddMissingSwitchLabelsAsync(model, document, root, node)),
                context.Diagnostics)
        End Function
        
        Private Shared Async Function AddMissingSwitchLabelsAsync(model As SemanticModel, document As Document, root As SyntaxNode, selectBlock As SelectBlockSyntax) As Task(Of Document)

            Dim enumType = DirectCast(model.GetTypeInfo(selectBlock.SelectStatement.Expression).Type, INamedTypeSymbol)
            Dim fullyQualifiedEnumType = enumType.ToDisplayString()

            Dim containsElseCase = False

            Dim caseLabels = New List(Of ExpressionSyntax)
            For Each block In selectBlock.CaseBlocks
                For Each caseSyntax In block.CaseStatement.Cases

                    Dim simpleCaseClause = TryCast(caseSyntax, SimpleCaseClauseSyntax)
                    If Not simpleCaseClause Is Nothing
                        caseLabels.Add(simpleCaseClause.Value)
                        Continue For
                    End If

                    If caseSyntax.IsKind(SyntaxKind.ElseCaseClause)
                        containsElseCase = True
                    End If
                Next
            Next
            
            Dim numOfBlocksWithNoStatementsWithElse = 0
            If containsElseCase
                ' skip the `Else` block
                For i = selectBlock.CaseBlocks.Count - 2 To 0 Step -1
                    If Not selectBlock.CaseBlocks.ElementAt(i).Statements.Count = 0

                        ' insert the values immediately below the last item with statements
                        numOfBlocksWithNoStatementsWithElse = i + 1
                        Exit For
                    End If
                Next
            End If

            Dim missingLabels = GetMissingLabels(model, caseLabels, enumType)

            Dim exitSelectStatement = SyntaxFactory.ExitSelectStatement()
            Dim statements = SyntaxFactory.List(New List(Of StatementSyntax) From {exitSelectStatement})

            Dim newSections = SyntaxFactory.List(selectBlock.CaseBlocks)
            For Each label In missingLabels

                Dim caseStatement = SyntaxFactory.CaseStatement(SyntaxFactory.SimpleCaseClause(SyntaxFactory.ParseExpression(fullyQualifiedEnumType + "." + label)))
                Dim block = SyntaxFactory.CaseBlock(caseStatement, statements)
                
                ' ensure that the new cases are above the block with an else case, but below all other blocks
                If containsElseCase
                    ' this will not result in an InvalidOperationException because we know there
                    ' are at least `numOfBlocksWithNoStatementsWithElse` items in the select block
                    newSections = newSections.Insert(numOfBlocksWithNoStatementsWithElse, block)
                Else
                    newSections = newSections.Add(block)
                End If
            Next

            If Not containsElseCase
                newSections = newSections.Add(SyntaxFactory.CaseElseBlock(SyntaxFactory.CaseElseStatement(SyntaxFactory.ElseCaseClause()), statements))
            End If

            Dim newNode = selectBlock.WithCaseBlocks(newSections).WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
            Dim newRoot = root.ReplaceNode(selectBlock, newNode)

            ' This doesn't work, possibly because of https://github.com/dotnet/roslyn/issues/473
            ' It creates `Namespace.EnumName.Member`, rather than `EnumName.Member`
            ' Then, it immediately tells me that it can be simplified
            Return Await Simplifier.ReduceAsync(document.WithSyntaxRoot(newRoot)).ConfigureAwait(False)
        End Function

        Private Shared Function GetMissingLabels(model As SemanticModel, caseLabels As List(Of ExpressionSyntax), enumType As INamedTypeSymbol) As IEnumerable(Of String)

            Dim symbols As New List(Of ISymbol)

            For Each label In caseLabels
                
                ' these are the labels like `MyEnum.EnumMember`
                Dim memberAccessExpression = TryCast(label, MemberAccessExpressionSyntax)
                If Not memberAccessExpression Is Nothing

                    Dim symbol = model.GetSymbolInfo(memberAccessExpression).Symbol
                    If Not symbol Is Nothing
                        symbols.Add(symbol)
                        Continue For
                    End If
                End If
                
                ' these are the labels like `EnumMember` (such as when using `Imports Namespace.MyEnum;`)
                Dim identifierName = TryCast(label, IdentifierNameSyntax)
                If Not identifierName Is Nothing

                    Dim symbol = model.GetSymbolInfo(identifierName).Symbol
                    If Not symbol Is Nothing
                        symbols.Add(symbol)
                    End If
                End If
            Next

            Dim missingLabels As New List(Of String)

            For Each member In enumType.GetMembers()
                Dim field = TryCast(member, IFieldSymbol)
                If field Is Nothing OrElse (Not field.Type.SpecialType = SpecialType.None)
                    Continue For
                End If

                Dim memberExists = False
                For Each symbol In symbols
                    If symbol Is member
                        memberExists = True
                        Exit For
                    End If
                Next

                If Not memberExists
                    missingLabels.Add(member.Name)
                End If
            Next

            Return missingLabels
        End Function
        
        Public NotOverridable Overrides Function GetFixAllProvider() As FixAllProvider
            Return PopulateSwitchCodeFixAllProvider.Instance
        End Function
        
        Private Class MyCodeAction
            Inherits CodeAction.DocumentChangeAction

            Public Sub New(title As String, createChangedDocument As Func(Of CancellationToken, Task(Of Document)))
                MyBase.New(title, createChangedDocument)
            End Sub
        End Class
    End Class
End Namespace
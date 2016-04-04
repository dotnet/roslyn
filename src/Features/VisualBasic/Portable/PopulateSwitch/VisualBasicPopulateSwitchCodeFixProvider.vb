Imports System.Composition
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.PopulateSwitch
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.PopulateSwitch
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.PopulateSwitch), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.AddOverloads)>
    Partial Friend Class VisualBasicPopulateSwitchCodeFixProvider
        Inherits AbstractPopulateSwitchCodeFixProvider

        Protected Overrides Function GetSwitchExpression(node As SyntaxNode) As SyntaxNode

            Dim selectBlock = DirectCast(node, SelectBlockSyntax)
            Return selectBlock.SelectStatement.Expression
        End Function

        Protected Overrides Function GetMissingLabels(node As SyntaxNode, model As SemanticModel, enumType As INamedTypeSymbol, <Out> ByRef containsDefaultLabel As Boolean) As List(Of String)

            Dim caseLabels = GetCaseLabels(DirectCast(node, SelectBlockSyntax), containsDefaultLabel)
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

        Protected Overrides Function InsertPosition(sections As List(Of SyntaxNode)) As Integer

            Dim cases = sections.OfType(Of CaseBlockSyntax).ToList()
            Dim numOfBlocksWithNoStatementsWithElse = 0

            ' skip the `Else` block
            For i = cases.Count - 2 To 0 Step -1
                If Not cases.ElementAt(i).Statements.Count = 0

                    ' insert the values immediately below the last item with statements
                    numOfBlocksWithNoStatementsWithElse = i + 1
                    Exit For
                End If
            Next

            Return numOfBlocksWithNoStatementsWithElse
        End Function

        Protected Overrides Function GetSwitchSections(node As SyntaxNode) As List(Of SyntaxNode)

            Dim selectBlock = DirectCast(node, SelectBlockSyntax)
            Return New List(Of SyntaxNode)(selectBlock.CaseBlocks)
        End Function

        Protected Overrides Function NewSwitchNode(node As SyntaxNode, sections As List(Of SyntaxNode)) As SyntaxNode
            Dim selectBlock = DirectCast(node, SelectBlockSyntax)
            Return selectBlock.WithCaseBlocks(SyntaxFactory.List(sections)).WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)
        End Function

        Protected Overrides Function GetSwitchStatementNode(root As SyntaxNode, span As TextSpan) As SyntaxNode
            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span)
                Return Nothing
            End If

            Dim selectExpression = DirectCast(root.FindNode(span), ExpressionSyntax)
            Return DirectCast(selectExpression.Parent.Parent, SelectBlockSyntax)
        End Function

        Private Function GetCaseLabels(selectBlock As SelectBlockSyntax, <Out> ByRef containsDefaultLabel As Boolean) As List(Of ExpressionSyntax)

            containsDefaultLabel = False

            Dim caseLabels = New List(Of ExpressionSyntax)
            For Each block In selectBlock.CaseBlocks
                For Each caseSyntax In block.CaseStatement.Cases

                    Dim simpleCaseClause = TryCast(caseSyntax, SimpleCaseClauseSyntax)
                    If Not simpleCaseClause Is Nothing
                        caseLabels.Add(simpleCaseClause.Value)
                        Continue For
                    End If

                    If caseSyntax.IsKind(SyntaxKind.ElseCaseClause)
                        containsDefaultLabel = True
                    End If
                Next
            Next

            Return caseLabels
        End Function
    End Class
End Namespace
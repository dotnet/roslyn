' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.KeywordRecommenders.Declarations
    ''' <summary>
    ''' Recommends "End [block]" or, if after a End keyword, just the Block.
    ''' </summary>
    Friend Class EndBlockKeywordRecommender
        Inherits AbstractKeywordRecommender

        Protected Overrides Function RecommendKeywords(context As VisualBasicSyntaxContext, cancellationToken As CancellationToken) As IEnumerable(Of RecommendedKeyword)
            If context.IsPreProcessorDirectiveContext Then
                Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
            End If

            Dim targetToken = context.TargetToken

            If targetToken.IsKind(SyntaxKind.EndKeyword) AndAlso
               (targetToken.IsChildToken(Of EndBlockStatementSyntax)(Function(endBlock) endBlock.EndKeyword) OrElse
                targetToken.IsChildToken(Of StopOrEndStatementSyntax)(Function(endStatement) endStatement.StopOrEndKeyword)) Then
                ' We already have "End", so just recommend the closeable things

                ' NOTE: use the token to the left of "End" to locate parenting blocks, otherwise, this won't work
                ' for blocks that can't contain an End statement. For example, Enums and Interfaces -- in these cases
                ' the End statement is located outside of the block.
                targetToken = targetToken.GetPreviousToken()

                Dim keywords = From keyword In GetUnclosedBlockKeywords(targetToken.Parent)
                               Select SyntaxFacts.GetText(keyword)

                Dim keywordList = keywords.ToList()
                EnsureAllIfAny(keywordList, "Function", "Sub")
                Return keywordList.Select(Function(k) New RecommendedKeyword(k, GetToolTipForKeyword(k)))

            ElseIf context.FollowsEndOfStatement Then
                ' If you're in a case like this
                '
                '     End If
                '     |
                '
                ' our target token is the "If", even though it's closed. So we want to skip to the parent of the If
                ' block to start to figure out what blocks we still have
                Dim node = targetToken.Parent

                If TypeOf node Is EndBlockStatementSyntax Then
                    node = node.Parent.Parent
                End If

                If node IsNot Nothing Then
                    ' We don't have "End", so recommend everything with the End keyword
                    Return GetUnclosedBlockKeywords(node).Select(Function(k) New RecommendedKeyword("End " & SyntaxFacts.GetText(k),
                                                                                                    GetToolTipForKeyword(SyntaxFacts.GetText(k))))
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of RecommendedKeyword)()
        End Function

        Private Function GetToolTipForKeyword(keyword As String) As String
            Select Case keyword
                Case "Region", "Class", "Structure", "Namespace", "Module"
                    Return String.Format(VBFeaturesResources.EndBlockKeywordToolTip1, keyword)
                Case "Interface", "Enum"
                    Return String.Format(VBFeaturesResources.EndBlockKeywordToolTip2, keyword)
                Case "Select"
                    Return String.Format(VBFeaturesResources.EndStatementKeywordToolTip1, keyword & " Case")
                Case "SyncLock", "Try", "Using", "While", "With", "Sub", "Function", "Set", "Get", "RemoveHandler", "RaiseEvent"
                    Return String.Format(VBFeaturesResources.EndStatementKeywordToolTip1, keyword)
                Case "If", "Operator", "AddHandler"
                    Return String.Format(VBFeaturesResources.EndStatementKeywordToolTip2, keyword)
                Case Else
                    Return String.Empty
            End Select
        End Function

        Private Sub EnsureAllIfAny(collection As ICollection(Of String), ParamArray completions() As String)
            For Each item In completions
                If collection.Contains(item) Then
                    For Each item2 In completions
                        If Not collection.Contains(item2) Then
                            collection.Add(item2)
                        End If
                    Next
                    Exit For
                End If
            Next
        End Sub

        Private Function GetUnclosedBlockKeywords(node As SyntaxNode) As IEnumerable(Of SyntaxKind)
            Dim visitor As New MissingKeywordExtractor()

            Return From ancestor In node.GetAncestorsOrThis(Of SyntaxNode)()
                   Select missingKeyword = visitor.Visit(ancestor)
                   Where missingKeyword.HasValue
                   Select missingKeyword.Value
                   Distinct
        End Function

        Private Class MissingKeywordExtractor
            Inherits VisualBasicSyntaxVisitor(Of SyntaxKind?)

            Public Overrides Function VisitNamespaceBlock(node As NamespaceBlockSyntax) As SyntaxKind?
                If node.EndNamespaceStatement.IsMissing Then
                    Return SyntaxKind.NamespaceKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitModuleBlock(ByVal node As ModuleBlockSyntax) As SyntaxKind?
                If node.EndBlockStatement.IsMissing Then
                    Return SyntaxKind.ModuleKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitClassBlock(ByVal node As ClassBlockSyntax) As SyntaxKind?
                If node.EndBlockStatement.IsMissing Then
                    Return SyntaxKind.ClassKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitStructureBlock(ByVal node As StructureBlockSyntax) As SyntaxKind?
                If node.EndBlockStatement.IsMissing Then
                    Return SyntaxKind.StructureKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitInterfaceBlock(ByVal node As InterfaceBlockSyntax) As SyntaxKind?
                If node.EndBlockStatement.IsMissing Then
                    Return SyntaxKind.InterfaceKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitEnumBlock(node As EnumBlockSyntax) As SyntaxKind?
                If node.EndEnumStatement.IsMissing Then
                    Return SyntaxKind.EnumKeyword
                Else
                    Return Nothing
                End If
            End Function

            Private Function VisitMethodBlockBase(node As MethodBlockBaseSyntax) As SyntaxKind?
                If node.EndBlockStatement.IsMissing Then
                    Return node.BlockStatement.DeclarationKeyword.Kind
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As SyntaxKind?
                Return VisitMethodBlockBase(node)
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As SyntaxKind?
                Return VisitMethodBlockBase(node)
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As SyntaxKind?
                Return VisitMethodBlockBase(node)
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As SyntaxKind?
                Return VisitMethodBlockBase(node)
            End Function

            Public Overrides Function VisitMultiLineIfBlock(node As MultiLineIfBlockSyntax) As SyntaxKind?
                If node.EndIfStatement.IsMissing Then
                    Return SyntaxKind.IfKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As SyntaxKind?
                If node.EndPropertyStatement.IsMissing Then
                    Return node.PropertyStatement.DeclarationKeyword.Kind
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitSyncLockBlock(node As SyncLockBlockSyntax) As SyntaxKind?
                If node.EndSyncLockStatement.IsMissing Then
                    Return SyntaxKind.SyncLockKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitSelectBlock(node As SelectBlockSyntax) As SyntaxKind?
                If node.EndSelectStatement.IsMissing Then
                    Return SyntaxKind.SelectKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitUsingBlock(node As UsingBlockSyntax) As SyntaxKind?
                If node.EndUsingStatement.IsMissing Then
                    Return SyntaxKind.UsingKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitWhileBlock(node As WhileBlockSyntax) As SyntaxKind?
                If node.EndWhileStatement.IsMissing Then
                    Return SyntaxKind.WhileKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitWithBlock(node As WithBlockSyntax) As SyntaxKind?
                If node.EndWithStatement.IsMissing Then
                    Return SyntaxKind.WithKeyword
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitTryBlock(node As TryBlockSyntax) As SyntaxKind?
                If node.EndTryStatement.IsMissing Then
                    Return SyntaxKind.TryKeyword
                Else
                    Return Nothing
                End If
            End Function

        End Class
    End Class
End Namespace

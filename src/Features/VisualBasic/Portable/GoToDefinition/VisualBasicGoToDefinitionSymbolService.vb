' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.ExtractMethod
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionSymbolService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionSymbolService
        Inherits AbstractGoToDefinitionSymbolService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            Return symbol.FindRelatedExplicitlyDeclaredSymbol(compilation)
        End Function

        Protected Overrides Function GetTargetPositionIfControlFlow(semanticModel As SemanticModel, token As SyntaxToken) As Integer?
            Dim node = token.GetRequiredParent()

            If token.IsKind(SyntaxKind.ReturnKeyword, SyntaxKind.YieldKeyword) Then
                Return FindContainingReturnableConstruct(node).GetFirstToken().Span.Start
            End If

            Dim continueTarget = TryGetContinueTarget(node)
            If continueTarget IsNot Nothing Then
                Return continueTarget.GetFirstToken().Span.Start
            End If

            Dim exitTarget = TryGetExitTarget(node)
            If exitTarget IsNot Nothing Then
                Select Case node.Kind()
                    Case SyntaxKind.ExitSubStatement
                    Case SyntaxKind.ExitFunctionStatement
                    Case SyntaxKind.ExitPropertyStatement
                        Dim Symbol = semanticModel.GetDeclaredSymbol(exitTarget)
                        Return If(Symbol.Locations.FirstOrDefault()?.SourceSpan.Start, 0)
                End Select

                ' Exit Select, Exit While, Exit For, Exit ForEach, ...
                Return exitTarget.GetLastToken().Span.End
            End If

            If node.IsKind(SyntaxKind.GoToStatement) Then
                Dim goToStatement = DirectCast(node, GoToStatementSyntax)

                Dim gotoOperation = DirectCast(semanticModel.GetOperation(goToStatement), IBranchOperation)
                If gotoOperation Is Nothing Then
                    Return Nothing
                End If

                Debug.Assert(gotoOperation.BranchKind = BranchKind.GoTo)
                Dim target = gotoOperation.Target
                Return target.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.SpanStart
            End If

            Return Nothing
        End Function

        Private Shared Function TryGetExitTarget(node As SyntaxNode) As SyntaxNode
            Select Case node.Kind()
                Case SyntaxKind.ExitSelectStatement
                    Return FindContainingSelect(node)
                Case SyntaxKind.ExitWhileStatement
                    Return FindContainingWhile(node)
                Case SyntaxKind.ExitForStatement
                    Return FindContainingFor(node)
                Case SyntaxKind.ExitDoStatement
                    Return FindContainingDoLoop(node)
                Case SyntaxKind.ExitTryStatement
                    Return FindContainingTry(node)
                Case SyntaxKind.ExitPropertyStatement
                    Return FindContainingReturnableConstruct(node)
                Case SyntaxKind.ExitSubStatement
                    Return FindContainingReturnableConstruct(node)
                Case SyntaxKind.ExitFunctionStatement
                    Return FindContainingReturnableConstruct(node)
            End Select

            Return Nothing
        End Function

        Private Shared Function TryGetContinueTarget(node As SyntaxNode) As SyntaxNode
            Select Case node.Kind()
                Case SyntaxKind.ContinueWhileStatement
                    Return FindContainingWhile(node)
                Case SyntaxKind.ContinueForStatement
                    Return FindContainingFor(node)
                Case SyntaxKind.ContinueDoStatement
                    Return FindContainingDoLoop(node)
            End Select

            Return Nothing
        End Function

        Private Shared Function FindContainingSelect(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsKind(SyntaxKind.SelectBlock)
                node = node.Parent

                If node.IsReturnableConstruct() Then
                    Return Nothing
                End If
            End While

            Return node
        End Function

        Private Shared Function FindContainingWhile(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsKind(SyntaxKind.WhileBlock)
                node = node.Parent

                If node.IsReturnableConstruct() Then
                    Return Nothing
                End If
            End While

            Return node
        End Function

        Private Shared Function FindContainingFor(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsKind(SyntaxKind.ForBlock, SyntaxKind.ForEachBlock)
                node = node.Parent

                If node.IsReturnableConstruct() Then
                    Return Nothing
                End If
            End While

            Return node
        End Function

        Private Shared Function FindContainingDoLoop(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsKind(SyntaxKind.DoLoopUntilBlock, SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoUntilLoopBlock, SyntaxKind.DoWhileLoopBlock)
                node = node.Parent

                If node.IsReturnableConstruct() Then
                    Return Nothing
                End If
            End While

            Return node
        End Function

        Private Shared Function FindContainingTry(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsKind(SyntaxKind.TryBlock)
                node = node.Parent

                If node.IsReturnableConstruct() Then
                    Return Nothing
                End If
            End While

            Return node
        End Function

        Private Shared Function FindContainingReturnableConstruct(node As SyntaxNode) As SyntaxNode
            While node IsNot Nothing AndAlso Not node.IsReturnableConstruct()
                node = node.Parent

                If node.IsKind(SyntaxKind.ClassBlock, SyntaxKind.StructureBlock, SyntaxKind.InterfaceBlock) Then
                    Return Nothing
                End If
            End While

            Return node
        End Function
    End Class
End Namespace

Imports System.Collections.Generic

Namespace Roslyn.Compilers.VisualBasic
    Class ReturnStatementsWalker
        Inherits AbstractRegionControlFlowAnalysis

        ''' <summary>
        ''' A collection of return, exit sub, exit function, exit operator and exit property statements found within the region that return to the enclosing method.
        ''' </summary>
        Friend Overloads Shared Function Analyze(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo) As IEnumerable(Of StatementSyntax)
            Dim walker = New ReturnStatementsWalker(info, region)
            Try
                Return If(walker.Analyze(), walker.returnStatements.ToArray(), Enumerable.Empty(Of StatementSyntax)())
            Finally
                walker.Free()
            End Try
        End Function

        Dim returnStatements As ArrayBuilder(Of StatementSyntax) = ArrayBuilder(Of StatementSyntax).GetInstance()

        Private Overloads Function Analyze() As Boolean
            Return Scan()
        End Function

        Protected Overrides Sub Free()
            Me.returnStatements.Free()
            Me.returnStatements = Nothing
            MyBase.Free()
        End Sub

        Friend Sub New(info As FlowAnalysisInfo, region As FlowAnalysisRegionInfo)
            MyBase.New(info, region)
        End Sub

        Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
            Dim syntax = TryCast(node.Syntax, ReturnStatementSyntax)
            If syntax IsNot Nothing AndAlso Me._regionPlace = AbstractFlowAnalysis(Of ControlFlowAnalysis.LocalState).RegionPlace.Inside Then
                returnStatements.Add(syntax)
            End If
            Return MyBase.VisitReturnStatement(node)
        End Function

        Public Overrides Function VisitExitStatement(node As BoundExitStatement) As BoundNode
            Dim syntax = TryCast(node.Syntax, ExitStatementSyntax)
            If syntax IsNot Nothing Then
                Select Case syntax.Kind
                    Case SyntaxKind.ExitFunctionStatement,
                        SyntaxKind.ExitSubStatement,
                        SyntaxKind.ExitOperatorStatement,
                        SyntaxKind.ExitPropertyStatement
                        If Me._regionPlace = AbstractFlowAnalysis(Of ControlFlowAnalysis.LocalState).RegionPlace.Inside Then
                            returnStatements.Add(syntax)
                        End If
                End Select
            End If
            Return MyBase.VisitExitStatement(node)
        End Function
      
    End Class

End Namespace

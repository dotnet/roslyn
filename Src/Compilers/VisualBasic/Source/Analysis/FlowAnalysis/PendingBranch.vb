Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' A pending branch.  There are created for a return, break, continue, or goto
    ''' statement.  The idea is that we don't know if the branch will eventually reach
    ''' its destination because of an intervening finally block that cannot complete
    ''' normally.  So we store them up and handle them as we complete processing
    ''' each construct.  At the end of a block, if there are any pending branches to a label
    ''' in that block we process the branch.  Otherwise we relay it up to the enclosing
    ''' construct as a pending branch of the enclosing construct.
    ''' </summary>
    Friend Class PendingBranch
        Public ReadOnly Branch As BoundStatement
        Public State As FlowAnalysisLocalState

        Public Sub New(branch As BoundStatement, state As FlowAnalysisLocalState)
            Me.Branch = branch
            Me.State = state
        End Sub

    End Class

End Namespace

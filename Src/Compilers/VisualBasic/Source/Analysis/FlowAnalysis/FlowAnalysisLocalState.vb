Imports System.Diagnostics

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Represents the state of flow analysis at a given point in the program.  The reachability
    ''' state is represented as a single boolean (and is only meaningful at statement boundaries).
    ''' Each local variable that is tracked for definite assignment
    ''' is given an integral index managed by the client (FlowAnalysisWalker).  The variable
    ''' at index 0 is special; it is never associated with any actual variable, so when it is assigned
    ''' we can conclude that the code in unreachable according to CLR rules.  Once an "unreachable
    ''' statement" diagnostic has been produced, we unassign slot 0 as a flag to indicate that we
    ''' should suppress further diagnsotics.
    ''' 
    ''' There are two kinds of local states: those for "normal" contexts, and those for "conditional" contexts,
    ''' which result from boolean expressions and for which the language specification may have special rules
    ''' for "assigned when true" and "assigned when false".  Normal contexts use this.Assigned, and leave
    ''' this.AssignedWhenTrue and this.AssignedWhenFalse Null.  Conditional contexts use this.AssignedWhen*
    ''' and leave Assigned Null.  this.Split() turns a normal context into a conditional context, and
    ''' this.Merge() turns a conditional context into a normal context.
    ''' 
    ''' In a conditional context, this.AssignedWhenTrue[i] is true if the variable at index i
    ''' is "assigned when true" or "definitely assigned" at the current point of analysis.  Similarly,
    ''' this.AssignedWhenFalse[i] is true if the variable at index i is "assigned when false" or
    ''' "definitely assigned".  This simplifies the logic required to implement flow analysis.
    ''' 
    ''' The special slot (-1) is treated as always assigned to further simplify code in the client.
    ''' 
    ''' Note that FlowAnalysisLocalState is mutable.  Typically, the state is mutated as the analysis proceeds
    ''' through the text of a method.  Special handling is required for each construct that involves control-flow;
    ''' if a flow analysis state is to be reused (for example, the state at entry to each catch clause is the same
    ''' as the state at entry to the try block) then the state is cloned by the client (this.Clone()) so that a
    ''' fresh, unmutated copy can be used in each place.
    ''' </summary>
    Structure FlowAnalysisLocalState
        Friend Reachable As Boolean             ' is the code reachable, formally
        Friend Assigned As BitArray             ' used only for non-boolean states
        Friend AssignedWhenTrue As BitArray     ' used only for boolean states
        Friend AssignedWhenFalse As BitArray    ' used only for boolean states

        Public Sub New(reachable As Boolean, assigned As BitArray)
            Me.Reachable = reachable
            Me.Assigned = assigned
            Debug.Assert(Not assigned.IsNull)
            Me.AssignedWhenTrue = BitArray.Null 
            Me.AssignedWhenFalse = BitArray.Null 
        End Sub

        Public Sub New(reachable As Boolean, assignedWhenTrue As BitArray, assignedWhenFalse As BitArray)
            Me.Reachable = reachable
            Me.Assigned = BitArray.Null
            Debug.Assert(Not assignedWhenTrue.IsNull)
            Debug.Assert(Not assignedWhenFalse.IsNull)
            Me.AssignedWhenTrue = assignedWhenTrue
            Me.AssignedWhenFalse = assignedWhenFalse
        End Sub

        Public Shared Function ReachableState() As FlowAnalysisLocalState
            Dim result As FlowAnalysisLocalState
            result.Reachable = True
            result.Assigned = BitArray.Empty
            result.AssignedWhenTrue = BitArray.Null
            result.AssignedWhenFalse = BitArray.Null
            Return result
        End Function

        Public Sub Assign(slot As Integer)
            If slot =  - 1
                Return
            End If
            Assigned(slot) = True
        End Sub

        Public Sub Unassign(slot As Integer)
            If slot =  - 1
                Return
            End If
            Assigned(slot) = False
        End Sub

        Public Function IsAssigned(slot As Integer) As Boolean
            Return(slot =  - 1)OrElse Assigned(slot)
        End Function

        ''' <summary>
        ''' Create a new "unreachable" state, in which every variable slot (up to nextVariableSlot)
        ''' is treated as assigned.
        ''' </summary>
        ''' <param name = "nextVariableSlot"></param>
        ''' <returns></returns>
        Public Shared Function UnreachableState(nextVariableSlot As Integer) As FlowAnalysisLocalState
            Dim result As FlowAnalysisLocalState
            result.Reachable = False
            result.Assigned = BitArray.AllSet(nextVariableSlot)
            result.AssignedWhenTrue = BitArray.Null
            result.AssignedWhenFalse = BitArray.Null
            Return result
        End Function

        ''' <summary>
        ''' Clone a state (so one can be mutated without affecting the other).
        ''' </summary>
        ''' <returns></returns>
        Friend Function Clone() As FlowAnalysisLocalState
            Debug.Assert(Not Me.Assigned.IsNull)
            Debug.Assert(Me.AssignedWhenTrue.IsNull)
            Debug.Assert(Me.AssignedWhenFalse.IsNull)
            Dim result As FlowAnalysisLocalState
            result.Reachable = Me.Reachable
            result.Assigned = Me.Assigned.Clone()
            result.AssignedWhenFalse = BitArray.Null
            result.AssignedWhenTrue = BitArray.Null
            Return result
        End Function

        ''' <summary>
        ''' Turn this state into a conditional (boolean) state, to be used for control-flow.
        ''' </summary>
        Public Sub Split()
            If Me.Assigned.IsNull
                Return
            End If
            Me.AssignedWhenTrue = Me.Assigned.Clone()
            Me.AssignedWhenFalse = Me.Assigned
            Me.Assigned = BitArray.Null
        End Sub

        ''' <summary>
        ''' Turn this state into a non-conditional state (i.e. not to be used for control-flow).
        ''' </summary>
        Public Sub Merge()
            If Not Me.Assigned.IsNull
                Return
            End If
            Me.Assigned = Me.AssignedWhenTrue
            Me.Assigned.IntersectWith(Me.AssignedWhenFalse)
            Me.AssignedWhenFalse = BitArray.Null
            Me.AssignedWhenTrue = BitArray.Null
        End Sub

        ' when two control points merge.  Returns true if this state changed.
        Public Function [Join](other As FlowAnalysisLocalState) As Boolean
            Me.Merge()
            other.Merge()
            Me.Reachable = Me.Reachable OrElse other.Reachable
            Return Me.Assigned.IntersectWith(other.Assigned)
        End Function

    End Structure

End Namespace

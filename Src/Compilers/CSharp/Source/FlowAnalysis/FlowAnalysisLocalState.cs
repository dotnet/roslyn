using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Represents the state of flow analysis at a given point in the program.  The reachability
    /// state is represented as a single boolean (and is only meaningful at statement boundaries).
    /// Each local variable that is tracked for definite assignment
    /// is given an integral index managed by the client (FlowAnalysisWalker).  The variable
    /// at index 0 is special; it is never associated with any actual variable, so when it is assigned
    /// we can conclude that the code in unreachable according to CLR rules.  Once an "unreachable
    /// statement" diagnostic has been produced, we unassign slot 0 as a flag to indicate that we
    /// should suppress further diagnsotics.
    /// 
    /// There are two kinds of local states: those for "normal" contexts, and those for "conditional" contexts,
    /// which result from boolean expressions and for which the language specification may have special rules
    /// for "assigned when true" and "assigned when false".  Normal contexts use this.Assigned, and leave
    /// this.AssignedWhenTrue and this.AssignedWhenFalse Null.  Conditional contexts use this.AssignedWhen*
    /// and leave Assigned Null.  this.Split() turns a normal context into a conditional context, and
    /// this.Merge() turns a conditional context into a normal context.
    /// 
    /// In a conditional context, this.AssignedWhenTrue[i] is true if the variable at index i
    /// is "assigned when true" or "definitely assigned" at the current point of analysis.  Similarly,
    /// this.AssignedWhenFalse[i] is true if the variable at index i is "assigned when false" or
    /// "definitely assigned".  This simplifies the logic required to implement flow analysis.
    /// 
    /// The special slot (-1) is treated as always assigned to further simplify code in the client.
    /// 
    /// Note that FlowAnalysisLocalState is mutable.  Typically, the state is mutated as the analysis proceeds
    /// through the text of a method.  Special handling is required for each construct that involves control-flow;
    /// if a flow analysis state is to be reused (for example, the state at entry to each catch clause is the same
    /// as the state at entry to the try block) then the state is cloned by the client (this.Clone()) so that a
    /// fresh, unmutated copy can be used in each place.
    /// </summary>
    struct FlowAnalysisLocalState
    {
        internal bool Reachable;     // is the code reachable, formally

        internal BitArray Assigned;          // used only for non-boolean states
        internal BitArray AssignedWhenTrue;  // used only for boolean states
        internal BitArray AssignedWhenFalse; // used only for boolean states

        public FlowAnalysisLocalState(bool reachable, BitArray assigned)
        {
            this.Reachable = reachable;
            this.Assigned = assigned;
            Debug.Assert(!assigned.IsNull);
            this.AssignedWhenTrue = this.AssignedWhenFalse = BitArray.Null;
        }

        public FlowAnalysisLocalState(bool reachable, BitArray assignedWhenTrue, BitArray assignedWhenFalse)
        {
            this.Reachable = reachable;
            this.Assigned = BitArray.Null;
            Debug.Assert(!assignedWhenTrue.IsNull);
            Debug.Assert(!assignedWhenFalse.IsNull);
            this.AssignedWhenTrue = assignedWhenTrue;
            this.AssignedWhenFalse = assignedWhenFalse;
        }

        public static FlowAnalysisLocalState ReachableState()
        {
            FlowAnalysisLocalState result;
            result.Reachable = true;
            result.Assigned = BitArray.Empty;
            result.AssignedWhenTrue = result.AssignedWhenFalse = BitArray.Null;
            return result;
        }

        public void Assign(int slot)
        {
            if (slot == -1)
                return;
            Assigned[slot] = true;
        }

        public void Unassign(int slot)
        {
            if (slot == -1)
                return;
            Assigned[slot] = false;
        }

        public bool IsAssigned(int slot)
        {
            return (slot == -1) || Assigned[slot];
        }

        /// <summary>
        /// Create a new "unreachable" state, in which every variable slot (up to nextVariableSlot)
        /// is treated as assigned.
        /// </summary>
        /// <param name="nextVariableSlot"></param>
        /// <returns></returns>
        public static FlowAnalysisLocalState UnreachableState(int nextVariableSlot)
        {
            FlowAnalysisLocalState result;
            result.Reachable = false;
            result.Assigned = BitArray.AllSet(nextVariableSlot);
            result.AssignedWhenTrue = result.AssignedWhenFalse = BitArray.Null;
            return result;
        }

        /// <summary>
        /// Clone a state (so one can be mutated without affecting the other).
        /// </summary>
        /// <returns></returns>
        internal FlowAnalysisLocalState Clone()
        {
            Debug.Assert(!this.Assigned.IsNull);
            Debug.Assert(this.AssignedWhenTrue.IsNull);
            Debug.Assert(this.AssignedWhenFalse.IsNull);

            FlowAnalysisLocalState result;
            result.Reachable = this.Reachable;
            result.Assigned = this.Assigned.Clone();
            result.AssignedWhenFalse = result.AssignedWhenTrue = BitArray.Null;
            return result;
        }

        /// <summary>
        /// Turn this state into a conditional (boolean) state, to be used for control-flow.
        /// </summary>
        public void Split()
        {
            if (this.Assigned.IsNull)
                return;
            this.AssignedWhenTrue = this.Assigned.Clone();
            this.AssignedWhenFalse = this.Assigned;
            this.Assigned = BitArray.Null;
        }

        /// <summary>
        /// Turn this state into a non-conditional state (i.e. not to be used for control-flow).
        /// </summary>
        public void Merge()
        {
            if (!this.Assigned.IsNull)
                return;
            this.Assigned = this.AssignedWhenTrue;
            this.Assigned.IntersectWith(this.AssignedWhenFalse);
            this.AssignedWhenFalse = AssignedWhenTrue = BitArray.Null;
        }

        // when two control points merge.  Returns true if this state changed.
        public bool Join(FlowAnalysisLocalState other)
        {
            this.Merge();
            other.Merge();
            this.Reachable |= other.Reachable;
            return this.Assigned.IntersectWith(other.Assigned);
        }
    }
}
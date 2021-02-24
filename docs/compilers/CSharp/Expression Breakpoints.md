Expression Breakpoints
======================

The Roslyn C# compiler has infrastructure in place to produce breakpoints within expressions rather than only at statement boundaries. The first expression form that supports breakpoints is the *switch expression*. Because breakpoints locations correspond to sequence points in source, this is accomplished by producing additional sequence points. Sequence points are constrained to appear only at locations in the code where the evaluation stack is empty, so any expressions that have breakpoint support are translated using a `BoundSpillSequence`. That is processed by a compiler pass `SpillSequenceSpiller` to ensure that it only occurs where the stack is empty.

Lowering of an expression that requires breakpoint support is done with the assistance of three new sequence-point bound statement nodes, which are placed in the `BoundSpillSequence`

> ```xml
>   <!--
>     This is used to save the debugger's idea of what the enclosing sequence
>     point is at this location in the code so that it can be restored later
>     by a BoundRestorePreviousSequencePoint node. When this statement appears,
>     the previous non-hidden sequence point is saved and associated with the
>     given Identifier.
>     -->
>   <Node Name="BoundSavePreviousSequencePoint" Base="BoundStatement">
>     <Field Name="Identifier" Type="object"/>
>   </Node>
> 
>   <!--
>     This is used to restore the debugger's idea of what the enclosing statement
>     is to some previous location without introducing a place where a breakpoint
>     would cause the debugger to stop. The identifier must have
>     previously been given in a BoundSavePreviousSequencePoint statement. This is used
>     to implement breakpoints within expressions (e.g. a switch expression).
>     -->
>   <Node Name="BoundRestorePreviousSequencePoint" Base="BoundStatement">
>     <Field Name="Identifier" Type="object"/>
>   </Node>
> 
>   <!--
>     This is used to set the debugger's idea of what the enclosing statement
>     is without causing the debugger to stop here when single stepping.
>     -->
>   <Node Name="BoundStepThroughSequencePoint" Base="BoundStatement">
>     <Field Name="Span" Type="TextSpan"/>
>   </Node>
> ```

A `BoundSavePreviousSequencePoint` is used to save the "current statement" information at the start of the expression, so that it can be restored after the expression. This is needed so that code that appears after the expression does not appear, in the debugger, to be executing the instrumented expression.

Both the `BoundRestorePreviousSequencePoint` and `BoundStepThroughSequencePoint` are intended to change the debugger's idea of what the current statement is without triggering a location where single-stepping would cause the debugger to stop at that location. That is accomplished by the generation of the following sequence:

> ```none
>     ldc.i4 1
>     brtrue.s L
>     // sequence point
>     nop
>   L:
>     // hidden sequence point
> ```

This can be seen, for example, in test `SwitchExpressionSequencePoints`

The purpose of this instruction sequence is to cause there to be an unreachable IL opcode (the `nop`) having a sequence point, followed by a hidden sequence point to prevent the debugger from stopping at the following location. However, once it is executing the following code, the debugger's idea of the program's "current source location" will appear to be that location mentioned in the sequence point.

A `BoundStepThroughSequencePoint` also modifies the debugger's view of the "enclosing statement", but without creating a location where a breakpoint can be set. While evaluating the state machine of a *switch expression*, this is used to make the "current statement" appear to be the *switch expression*.

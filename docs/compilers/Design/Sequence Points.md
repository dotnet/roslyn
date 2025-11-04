Sequence Points design
======================

Sequence points are emitted in PDBs/symbols to inform the debugger of the correspondance between IL and source. A sequence point is comprised of an IL offset and an associated document span.  
When the debugger stops on an IL instruction, it finds the first preceding sequence point from the IL offset, finds the associated span and highlights it for the user.  

Sequence points are generally used in roslyn at the statement level. They are constrained to appear only at locations in the code where the evaluation stack is empty.  

We also have [infrastructure](https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Expression%20Breakpoints.md) allowing expression breakpoints by way of spilling into statements.  
That is used in `switch` expressions and allows the debugger to first stop with the operand highlighted and then stop again with the selected arm highlighted.

The debugger pauses automatically after jump instructions. So synthesized labels generally need a sequence point. Otherwise whatever preceeding sequence point exists will be used, which would result in highlighting the previous statement.  
Hidden sequence points can be used for this. They are sequence points with no associated span. When the debugger encounters hidden sequence points, it resumes the execution instead of pausing.

In cases where an entire synthesized method should be skipped over by the debugger, it can be marked with `[DebuggerHidden]`. This is used for state machine methods aside from the `MoveNext()` method.  

The `VerifyIL` test helpers conveniently allow displaying the IL intermixed with sequence points (displayed as source snippets):
```il
    // sequence point: {
    IL_0045:  nop
    // sequence point: Write("1 ");
    IL_0046:  ldstr      "1 "
    IL_004b:  call       "void System.Console.Write(string)"
    IL_0050:  nop
    // sequence point: await System.Threading.Tasks.Task.CompletedTask;
    IL_0051:  call       "System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get"
    IL_0056:  callvirt   "System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
    IL_005b:  stloc.1
    // sequence point: <hidden>
    IL_005c:  ldloca.s   V_1
```

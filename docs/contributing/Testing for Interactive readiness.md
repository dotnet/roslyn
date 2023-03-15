
# Compiler debugging/EnC checklist

1. Sequence points are emitted correctly and corresponding syntax nodes are recognized as breakpoint spans by the IDE
    - Verify manually by launching VS with the new compiler bits and step through and place breakpoints on all syntax nodes that should allow breakpoint
    - Add regression compiler tests that check emitted IL with sequence points (`VerifyIL` with `sequencePoints`)
    - Add IDE test via `src\EditorFeatures\CSharpTest\EditAndContinue\BreakpointSpansTests.cs` (implementation `src\Features\CSharp\Portable\EditAndContinue\BreakpointSpans.cs`, which deals with mapping syntax to sequence points)
2. Sequence points in relationship with closure allocations and other hidden code (for any syntax that produces sequence points)
    - The debugger supports manually moving the current IP (instruction pointer) using “Set Next Statement Ctlr+Shift+F10” command.
    - The statement can be set to any sequence point in the current method. 
    - Need to make sure that sequence points are emitted so that the right hidden code gets executed.
    - For example, the sequence point on an opening brace of a block that allocates a closure needs to precede the closure allocation instructions, so that when the IP is set to this sequence point the closure is properly allocated.
```
{               // closure allocated here
   var x = 1;
   F(() => x);
}
```
3. Conditional branching has to use stloc/ldloc pattern in DEBUG build
    - Check the instructions and sequence points emitted for e.g. an if statement in debug build. Instead of straightforward brtrue/brfalse we allocate a temp local, store the result of the condition evaluation to that local, emit hidden sequence point, load the result from the local and then branch. This supports EnC function remapping. Only need to think about this when implementing a feature that emits conditional branches with conditions that may contain arbitrary expressions. I believe there are helpers in the lowering phase that emit conditions in general, so it should be done automatically as long as the right helpers are used. But something to be aware of.
4. Closure and lambda scopes (PDB info)
    - If new syntax is introduced that represents some kind of lambda (anonymous methods, local functions, LINQ queries, etc.) update helpers in `src\Compilers\CSharp\Portable\Syntax\LambdaUtilities.cs` accordingly
    - If a new scope is introduce that can declare variables that can be lifted into a closure 
      - The bound node that represents the scope needs to be associated with syntax node recognized by helpers in `src\Compilers\CSharp\Portable\Syntax\LambdaUtilities.cs` (specifically `IsClosureScope`).
      - This requirement is enforced by an assertion in `SynthesizedClosureEnvironment` constructor.
    - Lambda and closure syntax offsets must be emitted to the PDB (encLambdaMap custom debug information)
      - The offset attribute of closure identifies the syntax node that's associated with the closure. This offset must be unique.
5. When a new symbol is introduced symbol matcher might need to be updated
    - Symbol matcher maps a symbol from one compilation to another.
    - Synthesized symbols like closures, state machines, anonymous types, lambdas, etc. also has to be mapped.
    - Impl: `src\Compilers\CSharp\Portable\Emitter\EditAndContinue\CSharpSymbolMatcher.cs`
    - Tests: `src\Compilers\CSharp\Test\Emit\Emit\EditAndContinue\SymbolMatcherTests.cs`
6. When a new syntax is introduced that may declare a user local variable or emits long lived synthesized variables (ie. state that survives between breakpoints, not just a temp within expression)
    - Validate that the variable slots can be mapped from new to previous compilation
    - This is implemented by `EncVariableSlotAllocator` using syntax offsets stored in PDB (`encLocalSlotMap` and `encLambdaMap` custom debug info).
    - The current mechanism might not be sufficient to support the mapping, in which case raise the issue with the IDE team to design additional PDB info to support the mapping.
7. Each new language feature should be covered by a test in Emit tests under Emit/EditAndContinue. 
    - Some features might just need a single test others multiple tests depending on impact on EnC.
    - PDB tests validate these scopes. (look for `<scope>` in PDB XML). `LocalsTests.cs` EE tests also validate the scoping.
8. When a new syntax is introduced that may declare a scope for local variables the corresponding IL scopes need to be emitted correctly in the PDB
    - These are used by the EE to determine which variables are in scope.
9. Test debugging experience of the feature
    - Is useful info displayed in Watch window?
    - Can I evaluate expressions using this feature in Watch window?
    - Some features might require adding more custom PDB information to make the debug experience good (e.g. async, iterators, dynamic, etc).
    - Design experience improvement and custom PDB info with IDE team.

This document provides guidance for thinking about language interactions and testing compiler changes.

# General concerns:
- Completeness of the specification as a guide for testing (is the spec complete enough to suggest what the compiler should do in each scenario?)
- *Ping* for new breaking changes and general ping for partner teams (Bill, Kathleen, Mads, IDE, Razor)
- Help review external documentation
- Backward and forward compatibility (interoperation with previous and future compilers, each in both directions)
- Error handling/recovery (missing libraries, including missing types in mscorlib; errors in parsing, ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)
- BCL (including mono) and other customer impact
- Determinism
- Loading from metadata (source vs. loaded from metadata)
- Public interface of compiler APIs (including semantic model APIs listed below):
    - GetDeclaredSymbol 
    - GetEnclosingSymbol 
    - GetSymbolInfo 
    - GetSpeculativeSymbolInfo 
    - GetTypeInfo 
    - GetSpeculativeTypeInfo 
    - GetMethodGroup 
    - GetConstantValue 
    - GetAliasInfo 
    - GetSpeculativeAliasInfo 
    - LookupSymbols 
    - AnalyzeStatementsControlFlow 
    - AnalyzeStatementControlFlow 
    - AnalyzeExpressionDataFlow 
    - AnalyzeStatementsDataFlow 
    - AnalyzeStatementDataFlow 
    - ClassifyConversion
    - GetOperation (`IOperation`)
    - GetCFG (`ControlFlowGraph`)
- VB/F# interop
- C++/CLI interop (particularly for metadata format changes, e.g. DIMs, static abstracts in interfaces, or generic attributes)
- Performance and stress testing
- Can build VS
- Check that `Obsolete` is honored for members used in binding/lowering
 
# Type and members
- Access modifiers (public, protected, internal, protected internal, private protected, private), static, ref
- type declarations (class, record class/struct with or without positional members, struct, interface, type parameter)
- methods
- fields
- properties (including get/set/init accessors)
- events (including add/remove accessors)
- Parameter modifiers (ref, out, in, params)
- Attributes (including generic attributes and security attributes)
- Generics (type arguments, variance, constraints including `class`, `struct`, `new()`, `unmanaged`, `notnull`, types and interfaces with nullability)
- Default and constant values
- Partial classes
- Literals
- Enum (implicit vs. explicit underlying type)
- Expression trees
- Iterators
- Initializers (object, collection, dictionary)
- Array (single- or multi-dimensional, jagged, initializer, fixed)
- Expression-bodied methods/properties/...
- Extension methods
- Partial method
- Named and optional parameters
- String interpolation
- Raw strings (including interpolation)
- Properties (read-write, read-only, init-only, write-only, auto-property, expression-bodied)
- Interfaces (implicit vs. explicit interface member implementation)
- Delegates
- Multi-declaration
- NoPIA
- Dynamic
- Ref structs, Readonly structs
- Readonly members on structs (methods, property/indexer accessors, custom event accessors)
- SkipLocalsInit
- Method override or explicit implementation with `where T : { class, struct, default }`
 
# Code
- Operators (see Eric's list below)
- Lambdas (capture of parameters or locals, target typing)
- Execution order
- Target typing (var, lambdas, integrals)
- Conversions (boxing/unboxing)
- Nullable (wrapping, unwrapping)
- Overload resolution, override/hide/implement (OHI)
- Inheritance (virtual, override, abstract, new)
- Anonymous types
- Tuple types and literals (elements with explicit or inferred names, long tuples), tuple equality
- Range literals (`1..2`) and Index operator (`^1`) 
- Deconstructions
- Local functions
- Unsafe code
- LINQ
- Constructors, properties, indexers, events, operators, and destructors.
- Async (task-like types) and async-iterator methods
- Lvalues: the synthesized fields are mutable 
    - Ref / out parameters
    - Compound operators (`+=`, `/=`, etc ..) 
    - Assignment exprs
- Ref return, ref readonly return, ref ternary, ref readonly local, ref local re-assignment, ref foreach
- `this = e;` in `struct` .ctor
- Stackalloc (including initializers)
- Patterns (constant, declaration, `var`, positional, property and extended property, discard, parenthesized, type, relational, `and`/`or`/`not`, list, slice)
- Switch expressions
- With expressions (on record classes and on value types)
- Nullability annotations (`?`, attributes) and analysis
- If you add a place an expression can appear in code, make sure `SpillSequenceSpiller` handles it. Test with a `switch` expression or `stackalloc` in that place.
- If you add a new expression form that requires spilling, test it in the catch filter.
- extension based Dispose, DisposeAsync, GetEnumerator, GetAsyncEnumerator, Deconstruct, GetAwaiter etc.

# Misc
- reserved keywords (sometimes contextual)
- pre-processing directives
- COM interop
- modopt and modreq
- ref assemblies
- extern alias
- UnmanagedCallersOnly
- telemetry

# Testing in interaction with other components
Interaction with IDE, Debugger, and EnC should be worked out with relevant teams. A few highlights:
- IDE
    - Colorization and formatting
    - Typing experience and dealing with incomplete code
    - Intellisense (squiggles, dot completion)
    - "go to", Find All References, and renaming
    - cref comments
    - UpgradeProject code fixer
    - More: [IDE Test Plan](https://github.com/dotnet/roslyn/blob/main/docs/contributing/IDE%20Test%20Plan.md)

- Debugger / EE
    - Stepping, setting breakpoints
    - Displaying Locals/Autos windows
        - Type and Value display
        - Expanding instance members
        - Locals/parameters/this in closure classes
        - Attributes on locals and expressions (e.g.: [Dynamic], [TupleElementNames])
    - Compiling expressions in Immediate/Watch windows or hovering over an expression
    - Compiling expressions in [DebuggerDisplay("...")]
	- Assigning values in Locals/Autos/Watch windows

- Edit-and-continue

- Live Unit Testing (instrumentation)

- Engage with VS Templates team (if applicable)

## Details on Edit-and-continue and debugging

1. Sequence points are emitted correctly and corresponding syntax nodes are recognized as breakpoint spans by the IDE
    - Verify manually by launching VS with the new compiler bits and step through and place breakpoints on all syntax nodes that should allow breakpoint
    - Add IDE test via `src\EditorFeatures\CSharpTest\EditAndContinue\BreakpointSpansTests.cs` (implementation `src\Features\CSharp\Portable\EditAndContinue\BreakpointSpans.cs`)
    - Add regression compiler tests that check emitted IL with sequence points (`VerifyIL` with `sequencePoints`)
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
5. Closure and lambda scopes (PDB info)
    - If new syntax is introduced that represents some kind of lambda (anonymous methods, local functions, LINQ queries, etc.) update helpers in `src\Compilers\CSharp\Portable\Syntax\LambdaUtilities.cs` accordingly
    - If a new scope is introduce that can declare variables that can be lifted into a closure 
      - The bound node that represents the scope needs to be associated with syntax node recognized by helpers in `src\Compilers\CSharp\Portable\Syntax\LambdaUtilities.cs` (specifically `IsClosureScope`).
      - This requirement is enforced by an assertion in `SynthesizedClosureEnvironment` constructor.
    - Lambda and closure syntax offsets must be emitted to the PDB (encLambdaMap custom debug information)
      - The offset attribute of closure identifies the syntax node that's associated with the closure. This offset must be unique.
6. When a new symbol is introduced symbol matcher might need to be updated
    - Symbol matcher maps a symbol from one compilation to another.
    - Synthesized symbols like closures, state machines, anonymous types, lambdas, etc. also has to be mapped.
    - Impl: `src\Compilers\CSharp\Portable\Emitter\EditAndContinue\CSharpSymbolMatcher.cs`
    - Tests: `src\Compilers\CSharp\Test\Emit\Emit\EditAndContinue\SymbolMatcherTests.cs`
7. When a new syntax is introduced that may declare a user local variable or emits long lived synthesized variables
    - Validate that the variable slots can be mapped from new to previous compilation
    - This is implemented by `EncVariableSlotAllocator` using syntax offsets stored in PDB (`encLocalSlotMap` and `encLambdaMap` custom debug info).
    - The current mechanism might not be sufficient to support the mapping, in which case raise the issue with the IDE team to design additional PDB info to support the mapping.
8. Each new language feature should be covered by a test in Emit tests under Emit/EditAndContinue. 
    - Some features might just need a single test others multiple tests depending on impact on EnC.
    - PDB tests validate these scopes. (look for `<scope>` in PDB XML). `LocalsTests.cs` EE tests also validate the scoping.
9. When a new syntax is introduced that may declare a scope for local variables the corresponding IL scopes need to be emitted correctly in the PDB
    - These are used by the EE to determine which variables are in scope.
10. Test debugging experience of the feature
    - Is useful info displayed in Watch window?
    - Can I evaluate expressions using this feature in Watch window?
    - Some features might require adding more custom PDB information to make the debug experience good (e.g. async, iterators, dynamic, etc).
    - Design experience improvement and custom PDB info with IDE team.

# Eric's cheatsheet

## Statements 
```
{ … }  
;   
label : … 
T x = whatever; // including `using` and `await` using variants
M(); 
++x; 
x++; 
--x; 
x--; 
new C(); 
if (…) … else … 
switch(…) { … case (…) when (…): … } 
while(…) … 
do … while(…); 
for( … ; … ; … ) … 
foreach(…) … // including `await` variant
fixed(…) … // (plain, or custom with `GetPinnableReference`)
goto … ; 
throw … ; 
return … ; 
try  { … } catch (…) when (…) { … } finally { … } 
checked { … } 
unchecked { … } 
lock(…) … 
using (…) … // including `await` variant
yield return …; 
yield break; 
break; 
continue; 
```

## Expression classifications 
  
Every expression can be classified as exactly one of these: 
  
- Value 
- Variable 
- Namespace 
- Type 
- Method group 
- Null literal
- Default literal
- Anonymous function 
- Property 
- Indexer 
- Event 
- Void-returning method call 
- Array initializer (\*) 
- __arglist (\*)  

(\*) Technically not an expression according to the spec. 
  
Note that only values, variables, properties, indexers and events have a type. 
  
## Variable classifications 
  
A variable is a storage location. These are all the different ways to refer to a storage location: 

- Static field 
- Instance field 
- Array element 
- Formal param, value 
- Formal param, ref 
- Formal param, out 
- Local variable 
- Pointer dereference 
- __refvalue 

## Operators 

``` c#
x.y 
f( ) 
a[e] 
x++ 
x-- 
new X() 
new() 
typeof(T) 
default(T)
default 
checked(e) 
unchecked(e) 
delegate ( ) { } 
+x 
-x 
!x 
~x 
^x
++x 
--x 
(X)x 
x * y 
x / y 
x % y 
x + y 
x - y 
x << y 
x >> y 
x < y 
x > y 
x <= y 
x >= y 
x is X 
x as X 
x == y 
x != y 
x & y 
x ^ y 
x | y 
x && y 
x || y 
x ?? y 
x ? : y : z
x = y 
x *= y 
x /= y 
x %= y 
x += y 
x -= y 
x <<= y 
x >>= y 
x &= y 
x ^= y 
x |= y 
x ??= y
x => { } 
sizeof( ) 
*x 
& x 
x->y 
e is pattern
e switch { ... }
await x
__arglist( ) 
__refvalue( x, X ) 
__reftype( x )
__makeref( x )
```

## Explicit conversions 
  
- Numeric 
- Enum 
- Nullable 
- Reference  
- Unboxing 
- Dynamic 
- Type parameter 
- User defined 
- Pointer to pointer 
- Pointer to integral 
- Integral to pointer 
- Tuple literal
- Tuple

## Implicit conversions 
  
- Identity 
- Numeric 
- Literal zero to enum (we actually do constant zero, not literal zero) 
- Nullable 
- Null literal 
- Reference 
- Boxing 
- Dynamic 
- Constant 
- Type parameter 
- User defined 
- Anonymous function 
- Method group  
- Pointer to void pointer 
- Null literal to pointer
- Interpolated string
- Tuple literal
- Tuple
- Default literal
- Implicit object creation (target-typed new)
- Function type (in type inference comparing function types of lambdas or method groups)

## Types 

- Class
- Interface 
- Delegate 
- Struct 
- Enum
- Nullable 
- Pointer
- Type parameter

## Members

- Class
- Struct
- Interface 
- Enum
- Delegate
- Namespace 
- Property 
- Event
- Constructor 
- Destructor 
- Method
- Interface method 
- Field
- User-defined indexer
- User-defined operator
- User-defined conversion

## Patterns
- Discard Pattern
- Var Pattern
- Declaration Pattern
- Constant Pattern
- Recursive Pattern
- Parenthesized Pattern
- `and` Pattern
- `or` Pattern
- `not` Pattern
- Relational Pattern
- Type Pattern

## Metadata table numbers / token prefixes 
 
If you look at a 32 bit integer token as a hex number, the first two digits identify the “table number” and the last six digits are an offset into that table. The table numbers are: 
```
00 Module 
01 TypeRef 
02 TypeDef 
03 FieldPtr 
04 Field 
05 MethodPtr 
06 Method 
07 ParamPtr 
08 Param 
09 InterfaceImpl 
0A MemberRef 
0B Constant 
0C CustomAttr 
0D FieldMarshal 
0E DeclSecurity 
0F ClassLayout 
10 FieldLayout 
11 StandAloneSig 
12 EventMap 
13 EventPtr 
14 Event 
15 PropertyMap 
16 PropertyPtr 
17 Property 
18 MethodSemantics 
19 MethodImpl 
1A ModuleRef 
1B TypeSpec 
1C ImplMap 
1D FieldRVA 
1E ENCLog 
1F ENCMap 
20 Assembly 
21 AssemblyProcessor 
22 AssemblyOS 
23 AssemblyRef 
24 AssemblyRefProcessor 
25 AssemblyRefOS 
26 File 
27 ExportedType 
28 ManifestResource 
29 NestedClass 
2A GenericParam 
2B MethodSpec 
2C GenericConstraint 
```

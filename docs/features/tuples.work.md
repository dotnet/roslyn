Tuples Work Items
==================

This is the TODO list for the development of the tuples language feature for C# 7.

# Known issues and scenarios
- [ ] Target typing
    - [ ] `(byte, float) x = (1, 2)` should work
    - [ ] `return (null, null)` should work in `(object, object) M()`
- [ ] OHI validation / fixing
- [ ] Write spec
- [ ] Honor feature flag
- [ ] Publish short-term nuget package for tuples library
- [ ] Get tuples library into corefx
- [ ] Add warning for re-using member names out of position

- [ ] Control/data flow (mostly testing)
- [ ] Validation with other C# features (evaluation order, dynamic, unsafe code/pointers, optional parameter constants, nullable)
- [ ] Semantic info and other IDE stuff
    - [ ] Debugger / watch window / expression evaluation / EnC
- [x] Update well-known tuple types to TN naming convention
- [ ] Generating and loading metadata for user-defined member names
- [ ] Figure out full behavior for reserved member names
- [ ] Support tuples 8+
- [ ] Interop with System.Tuple, KeyValuePair
- [ ] XML docs
- [ ] Debugger bugs
    - [ ] Tuple debug display is {(1, 2)} because ValueTuple.ToString() returns "(1, 2)"
    - [ ] Tuple-returning method declaration shows values if names match local variables

# Interaction with other language features:

General concerns:
- [ ] Error handling/recovery
    - [ ] Errors in parsing
    - [ ] Error handling for semantic errors (e.g. ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)
    - [ ] Missing types (mscorlib or others)
- [ ] Public interface of compiler APIs, backcompat scenarios
- [ ] backward and forward compatibility (i.e. interoperation with previous and future compilers, each in both directions)
- [ ] VB/F# interop, Mono issues
- [ ] BCL and other customer impact
- [ ] Determinism
- [ ] Loading from metadata (source vs. loaded from metadata)
- [ ] Atomicity
- [ ] Edit-and-continue
- [ ] Completeness of the specification as a guide for testing (e.g. is the spec complete enough to suggest what the compiler should do in each scenario?)
- [ ] Other external documentation
- [ ] Performance

Types and members:
- [ ] Access modifiers (public, protected, internal, protected internal, private), static modifier
- [ ] Parameter modifiers (ref, out, params)
- [ ] Attributes (including security attribute)
- [ ] Generics (type arguments, constraints, variance)
- [ ] default value
- [ ] partial classes
- [ ] literals
- [ ] enum (implicit vs. explicit underlying type)
- [ ] expression trees
- [ ] Iterators
- [ ] Initializers (object, collection, dictionary)
- [ ] array (single- or multi-dimensional, jagged, initilalizer)
- [ ] Expression-bodied methods/properties/...
- [ ] Extension methods
- [ ] Partial method
- [ ] Named and optional parameters
- [ ] String interpolation
- [ ] Properties (read-write, read-only, write-only, auto-property, expression-bodied)
- [ ] Interfaces (implicit vs. explicit interface member implementation)
- [ ] delegates
- [ ] Multi-declaration

Code:
- [ ] Operators (see Eric's list)
    - [ ] Operator overloading
- [ ] Lvalues: the synthesized fields are mutable
    - [ ] Ref / out parameters
    - [ ] Compound operators (+=, /=, etc ..)
    - [ ] Assignment exprs
- [ ] lambdas (capture of parameters or locals, target typing)
- [ ] execution order
- [ ] Target typing (var, lambdas, integrals)
- [ ] Type inference
- [ ] Conversions
    - [ ] Implicit (identity, implicit numeric, implicit enumeration, implicit nullable, null litaral, implicit reference, boxing, implicit dynamic, implicit constant, user-defined implicit conversion, anonymous function, method group)
    - [ ] Explicit (numeric, enumeration, nullable, reference, unboxing, dynamic, user-defined)
    - [ ] Anonymous functions
- [ ] nullable (wrapping, unwrapping)
- [ ] OHI
    - [ ] inheritance (virtual, override, abstract, new)
    - [ ] overload resolution
- [ ] Anonymous types
- [ ] Unsafe code
- [ ] LINQ
- [ ] constructors, properties, indexers, events, operators, and destructors.
- [ ] Async

Misc:
- [ ] reserved keywords (sometimes contextual)
- [ ] pre-processing directives
- [ ] COM interop

 # Interaction with Debugger:
- [ ] typing in immediate/watch window (that also covers hovering over a variable)
- [ ] displaying locals (that also covers autos)

# Interaction with IDE:
- [ ] Colorization
- [ ] Type ahead
- [ ] Intellisense (squiggles, dot completion)
- [ ] Renaming
- [ ] Interaction with incomplete code scenarios (e.g. while typing)
- [ ] More complete list: https://github.com/dotnet/roslyn/issues/8389



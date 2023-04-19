# Interaction with other language features:

General concerns:
- [ ] Error handling/recovery
    - [ ] Errors in parsing
    - [ ] Error handling for semantic errors (e.g. ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)
- [ ] Public interface of compiler APIs, backcompat scenarios
- [ ] Determinism
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
    - [ ] Implicit (identity, implicit numeric, implicit enumeration, implicit nullable, null literal, implicit reference, boxing, implicit dynamic, implicit constant, user-defined implicit conversion, anonymous function, method group)
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
- [X] Async
- [X] Var

Misc:
- [ ] reserved keywords (sometimes contextual)
- [ ] pre-processing directives
- [ ] COM interop




- SemanticModel.GetDeclaredSymbol 
- SemanticModel.GetEnclosingSymbol 
- SemanticModel.GetSymbolInfo 
- SemanticModel.GetSpeculativeSymbolInfo 
- SemanticModel.GetTypeInfo 
- SemanticModel.GetSpeculativeTypeInfo 
- SemanticModel.GetMethodGroup 
- SemanticModel.GetConstantValue 
- SemanticModel.GetAliasInfo 
- SemanticModel.GetSpeculativeAliasInfo 
- SemanticModel.LookupSymbols 
- SemanticModel.AnalyzeStatementsControlFlow 
- SemanticModel.AnalyzeStatementControlFlow 
- SemanticModel.AnalyzeExpressionDataFlow 
- SemanticModel.AnalyzeStatementsDataFlow 
- SemanticModel.AnalyzeStatementDataFlow 
- SemanticModel.ClassifyConversion


Operators

Lvalues: the synthesized fields are mutable 
- Ref / out parameters
- Compound operators (+=, /=, etc ..) 
- Assignment exprs



- Interaction with overload resolution
- backward and forward compatibility (i.e. interoperation with previous and future compilers, each in both directions)


- completeness of the specification as a guide for testing (e.g. is the spec complete enough to suggest what the compiler should do in each scenario?)
- other external documentation
- error handling for semantic errors (e.g. ambiguous lookup, inaccessible lookup, wrong kind of thing found, instance vs static thing found, wrong type for the context, value vs variable)



General concerns:
- Error handling/recovery (Missing libraries, including missing types in mscorlib; errors in parsing)
- Public interface of compiler APIs, backcompat scenarios
- VB/F# interop
- BCL and other customer impact
- Determinism
- Loading from metadata (source vs. loaded from metadata)
 
 
Type and members:
- Access modifiers (public, protected, internal, protected internal, private), static modifier
- Parameter modifiers (ref, out, params)
- Attributes (including security attribute)
- Generics (type arguments, constraints, variance)
- default value
- partial classes
- literals
- enum (implicit vs. explicit underlying type)
- epression trees
- Iterators
- Initializers (object, collection, dictionary)
- array (single- or multi-dimensional, jagged, initilalizer)
- Expression-bodied methods/properties/...
- Extension methods
- Partial method
- Named and optional parameters
- String interpolation
- Properties (read-write, read-only, write-only, auto-property, expression-bodied)
- Interfaces (implicit vs. explicit interface member implementation)
- delegates
- Multi-declaration
- NoPIA
 
Code:
- Operators (see Eric's list)
- lambdas (capture of parameters or locals, target typing)
- execution order
- Target typing (var, lambdas, integrals)
- conversions (boxing/unboxing)
- nullable (wrapping, unwrapping)
- OHI
- inheritance (virtual, override, abstract, new)
- overload resolution
- Anonymous types
- Unsafe code
- LINQ
- constructors, properties, indexers, events, operators, and destructors.
- Async
 
 
Misc:
- reserved keywords (sometimes contextual)
- pre-processing directives
- COM interop
 
Interaction with Debugger:

1. typing in immediate/watch window (that also covers hovering over a variable)
2. displaying locals (that also covers autos)
 
Interaction with IDE: 

1. Colorization
2. Type ahead
3. Intellisense (squiggles, dot completion)
4. Renaming, "go to"
5. More: https://github.com/dotnet/roslyn/issues/8389

- interaction with IDE in incomplete code scenarios (e.g. while typing)

- edit-and-continue

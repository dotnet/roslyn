work items remaining for wildcards
==================================

### Specs
- [ ] Gather together a specification document
  - [ ] Language behavior (e.g. [this](https://github.com/dotnet/roslyn/issues/14862) and [this](https://github.com/dotnet/roslyn/issues/14794) and [this](https://github.com/dotnet/roslyn/issues/14832))
  - [ ] [SemanticModel behavior](https://gist.github.com/gafter/ab10e413efe3a066209cbf14cb874988) (see also [here](https://gist.github.com/gafter/37305d619bd04511f4f66b86f6f2d3a5))
  - [ ] Warnings (for expression variables declared but not used)
  - [x] Debugging (value discarded is not visible in debugger)

### Compiler
- [x] Syntax model changes
- [x] Symbol changes
- [x] Parsing for the short-form wildcard pattern `_`
- [x] Implement binding of wildcards
  - [x] In a pattern
  - [x] In a deconstruction declaration
  - [x] In a deconstruction assignment expression
  - [ ] In an out argument (in every argument context) 
  - [x] Both the long form `var _` and the short form `_`
- [x] Type inference for wildcards in each context
- [ ] Implement semantic model changes
  - [ ] `GetTypeInfo` of a wildcard expression `_` should be the type of the discarded value 
  - [ ] `GetSymbolInfo` of a wildcard expression `_` should be an `IDiscardedSymbol` 
- [x] Implement lowering of wildcards
  - [x] In a pattern
  - [x] In a deconstruction declaration
  - [x] In a deconstruction assignment expression
  - [ ] In an out argument (in each argument context)

### Testing
- [ ] Symbol tests
- [ ] Syntax tests
- [ ] SemanticModel tests
- [ ] Language static semantic tests
- [ ] Runtime behavioral tests
- [ ] PDB tests
- [ ] Scripting tests
- [ ] EE tests
- [ ] In a pattern context
- [ ] In a deconstruction declaration context
- [ ] In a deconstruction assignment expression context
- [ ] In an out argument (in every argument context)
- [ ] Both the long form `var _` and the short form `_`, where permitted
- [ ] In the long/short form when there is/not a conflicting name in scope

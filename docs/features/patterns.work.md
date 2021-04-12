# Open issues for pattern-matching extensions for C# 8

### Specification
- [ ] The specification needs to be updated with the additional syntax forms being added. See https://github.com/dotnet/csharplang/issues/1054 for a summary of the proposed changes vs C# 7.

### Unimplemented parts
- [ ] Give an error when a wildcard is used but something named `_` is in scope.
- [ ] Implement the switch statement in the presence of recursive patterns
  - [x] Parsing
  - [x] Binding
  - [ ] Lowering
  - [ ] Code-gen
  - [ ] Debug info
  - [ ] Edit-and-continue 
- [ ] Implement the match expression
  - [ ] Parsing
  - [ ] Binding
  - [ ] Lowering
  - [ ] Code-gen

- [ ] It is an error if the name `var` binds to a type in a var pattern. Implement and test.

### Test plan needed
- [ ] We need a test plan for these additions. Here is a high-level list of some tests that are needed
  - [ ] Scoping mechanism works for pattern variables in recursive patterns
  - [ ] In scripts top-level pattern variables in recursive patterns become fields.
  - [ ] IDE scenarios need to be identified tested.
  - [ ] Fuzz generator should be extended to support recursive patterns.
  - [ ] Test each of the kinds of symbols that should be supported in property patterns
  - [ ] Test all of the ways that a name would not be valid to name a property in a property pattern
  - [ ] Need to design the data representation for edit-and-continue for temps in patterns
  - [ ] Need to precisely share temps per the edit-and-continue spec
  - [ ] Need bullets here or github issues for PROTOTYPE(patterns2) comments.

- [ ] Need to ensure good code quality, e.g. avoid redundant null checks preceding types tests
- [ ] Need to ensure a good tradeoff between decision tree size explosion and execution of redundant tests.

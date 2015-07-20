- [ ] Parser ambiguity research
	- Some thought done, not complete
	- Currently thought to be unambiguous past the parameter list (starting at `{` or `=>`), but that requires lots of lookahead.
	- See LanguageParser.cs for comments on both ambiguity in standard parsing, and ambiguity in error recovery.
- [x] N-level nested local functions
- [ ] Capture
	- [x] Initial implementation
	- [ ] Tests
	- [x] IDE integration
	- Works alongside lambdas and behaves very similarly
	- Implemented zero-allocation closures on functions never converted to a delegate
- [x] Standard parameter features
	- params
	- ref/out
	- named/optional
- [x] Visibility
	- May revisit design (currently shadows, may do overloads)
- [x] Generics
	- Nongeneric local functions in generic methods (same as lambdas).
	- Generic local functions in nongeneric methods.
	- Generic local functions in generic methods.
	- Arbitrary nesting of generic local functions.
	- Generic local functions with constraints.
- [x] Inferred return type
- [x] Iterators
- [x] Async
- [ ] API
- [ ] Editor
	- [x] Basic features (variable highlight, rename, etc.)
	- [ ] Advanced features (refactorings, analyzers)

Intentionally disabled features that might eventually be in the end result:
- Calling a local function with a dynamic argument (due to name mangling and potential conflicts with closures and possibly overloads/shadows)
- Referring to a local function in an expression tree (note that defining a local function itself in an expression tree is impossible)

TODO:

- Update error messages.
	- `LocalScopeBinder.ReportConflictWithLocal()` (twice)
- `LocalScopeBinder.EnsureSingleDefinition()`, handle case where 'name' exists in both `localsMap` and `localFunctionsMap`. Might be related to `LocalFunctionTests.NameConflictLocalVarLast()`
- Defining a local function with a dynamic parameter doesn't work at runtime.

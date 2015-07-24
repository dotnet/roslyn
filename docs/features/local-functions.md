- [ ] Parser ambiguity research
	- Some thought done, not complete
	- Currently thought to be unambiguous past the parameter list (starting at `{` or `=>`), but that requires lots of lookahead.
	- See LanguageParser.cs for comments on both ambiguity in standard parsing, and ambiguity in error recovery.
- [x] N-level nested local functions
- [x] Capture
	- Works alongside lambdas and behaves very similarly in fallback cases
	- Has zero-allocation closures (struct frames by ref) on functions never converted to a delegate and are not an iterator/async
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
- Definite assignment rules: Currently, all captured variables must be assigned at point of local function declaration. Might change to requiring assignment at point of local function use, not declaration (would allow mutually recursive local functions without nesting, etc.). Related to "Visibility" point in main list.

TODO:

- Update error messages.
	- `LocalScopeBinder.ReportConflictWithLocal()` (twice)
- `LocalScopeBinder.EnsureSingleDefinition()`, handle case where 'name' exists in both `localsMap` and `localFunctionsMap`. Might be related to `LocalFunctionTests.NameConflictLocalVarLast()`
- Defining a local function with a dynamic parameter doesn't work at runtime.
- Return type of `var` is strange - when normally compiling, it works fine, but intellisense/etc doesn't work.
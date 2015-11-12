Local Functions
===============

This feature is to support the definition of functions in block scope.

Local functions may use variables defined in the enclosing scope. The current implementation requires that every variable read inside a local function be definitely assigned, as if executing the local function at its point of definition. Also, the local function definition must have been "executed" at any use point.

After experimenting with that a bit (for example, it is not possible to define two mutually recursive local functions), we've since revised how we want the definite assignment to work. The revision (not yet implemented) is that all local variables read in a local function must be definitely assigned at each invocation of the local function. That's actually more subtle than it sounds, and there is a bunch of work remaining to make it work. Once it is done you'll be able to move your local functions to the end of its enclosing block.

The new definite assignment rules are incompatible with inferring the return type of a local function, so we'll likely be removing support for inferring the return type.

Unless you convert a local function to a delegate, capturing is done into frames that are value types. That means you don't get any GC pressure from using local functions with capturing.

We don't have a spec yet, but the feature is fairly straightforward.

--------------------

Below is a checklist of work on the feature
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
- Return type of `var` is broken - see LocalFunctionSymbol.cs for an explanation. Fixing it will require a large rewrite of much of return type analysis, as the current system assumes that all return types are known (mostly) without examining the method body.
Local Function Status
=====================

This is a checklist for current and outstanding work on the local functions
feature, as spec'd in [local-functions.md](./local-functions.md).

--------------------

Known issues
============

Compiler:

- [ ] Parser builds nodes for local functions when feature not enabled (#9940)
- [ ] Compiler crash: base call to state machine method, in state machine
  method (#9872)
- [ ] Need custom warning for unused local function (#9661)
- [ ] Generate quick action does not offer to generate local (#9352)
- [ ] Parser ambiguity research (#10388)
- [ ] Dynamic support (#10389)
- [ ] Referring to local function in expression tree (#10390)
- [ ] Resolve definite assignment rules (#10391)
- [ ] Remove support for `var` return type (#10392)
- [ ] Update error messages (#10393)

IDE:

- [ ] Some selections around local functions fail extract method refactoring
  [ ] (#8719)
- [ ] Extracting local function passed to an Action introduces compiler error
  [ ] (#8718)
- [ ] Ctrl+. on a delegate invocation crashes VS (via Extract method) (#8717)
- [ ] Inline temp introduces compiler error (#8716)
- [ ] Call hierarchy search never terminates on local functions (#8654)
- [ ] Nav bar doesn't support local functions (#8648)
- [ ] No outlining for local functions (#8647)
- [ ] Squiggles all over the place when using an unsupported modifier (#8645)
- [ ] Peek definition errors out on local function (#8644)
- [ ] Void keyword not recommended while declaring local function (#8616)
- [ ] Change signature doesn't update the signature of the local function (#8539)


Feature Completeness Progress
=============================

- [x] N-level nested local functions
- [x] Capture
    - Works alongside lambdas and behaves very similarly in fallback cases
    - Has zero-allocation closures (struct frames by ref) on functions never
      converted to a delegate and are not an iterator/async
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

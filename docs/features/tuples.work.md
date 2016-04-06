Tuples Work Items
==================

This is the TODO list for the development of the tuples language feature for C# 7.

# Known issues and scenarios
- [ ] Target typing
    - [ ] `(byte, float) x = (1, 2)` should work
    - [ ] `return (null, null)` should work in `(object, object) M()`
- [ ] OHI validation / fixing
- [ ] Control/data flow (mostly testing)
- [ ] Validation with other C# features (evaluation order, dynamic, unsafe code/pointers, optional parameter constants, nullable)
- [ ] Semantic info and other IDE stuff
    - [ ] Debugger / watch window / expression evaluation / EnC
- [x] Update well-known tuple types to TN naming convention
- [ ] Generating and loading metadata for user-defined member names
- [ ] Figure out full behavior for reserved member names
- [ ] Support tuples 8+
- [ ] Interop with System.Tuple, KeyValuePair
- [ ] Publish short-term nuget package for tuples library
- [ ] Get tuples library into corefx
- [ ] XML docs
- [ ] Debugger bugs
    - [ ] Tuple debug display is {(1, 2)} because ValueTuple.ToString() returns "(1, 2)"
    - [ ] Tuple-returning method declaration shows values if names match local variables



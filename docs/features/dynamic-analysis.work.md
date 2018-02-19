Dynamic Analysis
===============

This feature enables instrumentation of binaries to collect information for dynamic analysis.

--------------------

TODO:

- Generate code to compute MVIDs less often than once per invoked method.
- Synthesize helper types to contain fields for analysis payloads so that methods of generic types can be instrumented.
- Accurately identify which methods are tests.
- Integrate dynamic analysis instrumentation with lowering.
- Verify that instrumentation covers constructor initializers and field initializers.

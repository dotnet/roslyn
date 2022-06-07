# IOperation Test Hook

The compiler has a test hook designed to ensure that IOperation nodes and scenarios match up with information that can be retrieved from the semantic model,
and to help ensure that we have code coverage that ensures IOperation will not fail or violate invariants for scenarios we don't have direct testing on. This
works by running a hook whenever the test framework creates a `Compilation` object: for every syntax tree in the compilation, we enumerate every syntax node
and verify that GetOperation does not crash, and returns information that matches up with the `SemanticModel` (ie, `GetTypeInfo` matches the `IOperation` type,
for example). We also fetch and verify the control flow graph for every member body in the compilation and run the CFG verifier to ensure that all invariants
are presevered.

## Feature Development

For feature development, this means that as we shift around BoundNodes, introduce new nodes, or enable code that didn't exist before, the hook may hit failures
because the IOperation machinery doesn't handle that particular node, or had logic that now triggers a `Debug.Assert` because the new code violates some pre-existing
assumption of the factories. For fixing these issues, there are a couple of options:

1. Implement the IOperation changes in the same PR. This is the only acceptable way to do this in a production branch.
2. Add the new BoundKind to the catch-all switch case at the bottom of `CSharpOperationFactory/VisualBasicOperationFactory.Create`, with a prototype comment
to ensure that support is implemented before the feature branch is merged back to a production branch. Sometimes, the test hits other failure spots, such as
`SemanticModel` failures or mismatches. For these, the test can be marked with `ConditionalFact(typeof(NoIOperationValidation))` to skip the test in the test
hook. When doing so, please mark with a prototype comment (either as a comment above or as the reason string for ConditionalFactAttribute) so that it is fixed
before merging to production.

## Replicating Failures

In order to replicate test failures, there are 2 options:

1. Uncomment `src/Compilers/Test/Core/Compilation/CompilationExtensions.cs:7`, which defines `ROSLYN_TEST_IOPERATION`, and run your tests. Do _not_ check this in, as it
will enable the test hook for every test in every project and significantly slow down regular test runs.
2. Set the `ROSLYN_TEST_IOPERATION` environment variable and restart VS with it set.
3. Set a breakpoint at the start of `ValidateIOperations`, in `src/Compilers/Test/Core/Compilation/CompilationExtensions.cs`. When it breaks, use VS's jump to location or
drag the instruction pointer past the early check and return on `EnableVerifyIOperation`, which will run the code for the current test run.

In either case, after this has been done all tests will run the hook as part of running the test, and you can set breakpoints as normal. It is often helpful to
run until an exception has been hit and then see what nodes were being processed, as most tests have multiple methods and understanding the specific context is
useful for figuring out the earlier steps that may have gone wrong.

When a test failure is isolated, please add a _dedicated_ IOperation test for this to make it easier to avoid future regressions. Preferrably, don't replicate
the entire original test, just enough to hit the bug to ensure that it's protected against regressions.


Checked user-defined operators
=====================================

C# should support defining `checked` variants of the following user-defined operators so that users can opt into or out of overflow behavior as appropriate:
*  The `++` and `--` unary operators (https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#postfix-increment-and-decrement-operators and https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#prefix-increment-and-decrement-operators).
*  The `-` unary operator (https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#unary-minus-operator).
*  The `+`, `-`, `*`, and `/` binary operators (https://github.com/dotnet/csharplang/blob/main/spec/expressions.md#arithmetic-operators).
*  Explicit conversion operators.

Proposal: 
- https://github.com/dotnet/csharplang/issues/4665
- https://github.com/dotnet/csharplang/blob/main/proposals/checked-user-defined-operators.md

Feature branch: https://github.com/dotnet/roslyn/tree/features/CheckedUserDefinedOperators

Test plan: https://github.com/dotnet/roslyn/issues/59458

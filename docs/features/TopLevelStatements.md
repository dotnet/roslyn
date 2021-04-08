Top-level statements
=========================

Allow a sequence of *statements* to occur right before the *namespace_member_declaration*s of a *compilation_unit* (i.e. source file).

The semantics are that if such a sequence of *statements* is present, the following type declaration, modulo the actual type name and the method name, would be emitted:

``` c#
static class Program
{
    static async Task Main()
    {
        // statements
    }
}
```

Proposal: https://github.com/dotnet/csharplang/blob/main/proposals/top-level-statements.md
Open issues and TODOs are tracked at https://github.com/dotnet/roslyn/issues/41704.
Test plan: https://github.com/dotnet/roslyn/issues/43563.
See also https://github.com/dotnet/csharplang/issues/3117.

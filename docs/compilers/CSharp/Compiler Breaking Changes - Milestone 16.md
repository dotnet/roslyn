### This document lists known breaking changes in Roslyn between the last VS2017 update (15.*) and C# 8.0 (16.0)

*Breaks are formatted with a monotonically increasing numbered list to allow them to referenced via shorthand (i.e., "known break #1").
Each entry should include a short description of the break, followed by either a link to the issue describing the full details of the break or the full details of the break inline.*

1. It is no longer permitted to use a constant named `_` as a constant pattern. This change is made to permit the syntax `_` to be used for a *discard pattern*. See https://github.com/dotnet/csharplang/blob/master/meetings/2017/LDM-2017-11-20.md
  ``` c#
      const int _ = 1;
      switch (1) { case _: break; } // error: A constant named '_' cannot be used as a pattern.
  ```

2. A warning is issued when an *is-type* expression tests an expression against a type named `_`. This syntax could be confused with a use of the *discard pattern*.
  ``` c#
      class _ { }

      if (o is _) // warning: The name '_' refers to the type '_', not the discard pattern. Use '@_' for the type, or 'var _' to discard.
  ```

3. In C# 8.0, the parentheses of a switch statement are optional when the expression being switched on is a tuple expression, because the tuple expression has its own parentheses:
  ``` c#
      switch (a, b)
  ```
   Due to this the `OpenParenToken` and `CloseParenToken` fields of a `SwitchStatementSyntax` node may now sometimes be empty.

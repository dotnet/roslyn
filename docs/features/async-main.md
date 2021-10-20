# Async Task Main
## [dotnet/csharplang proposal](https://github.com/dotnet/csharplang/blob/main/proposals/async-main.md)

## Technical Details

* The compiler must recognize `Task` and `Task<int>` as valid entrypoint return types in addition to `void` and `int`.
* The compiler must allow `async` to be placed on a main method that returns a `Task` or a `Task<T>` (but not void).
* The compiler must generate a shim method `$EntrypointMain` that mimics the arguments of the user-defined main.
  * `static async Task Main(...)` -> `static void $EntrypointMain(...)`
  * `static async Task<int> Main(...)` -> `static int $EntrypointMain(...)`
  * The parameters between the user-defined main and the generated main should match exactly.
* The body of the generated main should be `return Main(args...).GetAwaiter().GetResult();`
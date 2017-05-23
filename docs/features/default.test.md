# General concerns
- [ ] Backward compatibility: report version error with earlier `/langversion`
- [ ] Semantic model behavior
    - [ ] `GetSymbolInfo`
    - [ ] `GetSpeculativeSymbolInfo`
    - [ ] `GetTypeInfo`
    - [ ] `GetSpeculativeTypeInfo`
    - [ ] `GetConstantValue`
    - [ ] `AnalyzeDataFlow`
    - [ ] `ClassifyConversion`

# Type and members
- [ ] Attributes: positional and named parameters
- [ ] Parameters:
    - [ ] Default parameter values: `CancellationToken ct = default`
    - [ ] `params`:
        - [ ] `F(params T[] args)`: `F(default)` and `F(null, default)`
        - [ ] overload resolution with `params` and non-`params`, mixed value/reference types
- [ ] Constant values
- [ ] Enum (implicit and explicit underlying type)
- [ ] Expression trees
- [ ] Iterators: `yield return default;`
- [ ] Initializers (object, collection, dictionary)
- [ ] Array initializer
- [ ] Expression-bodied methods/properties
- [ ] String interpolation: `$"{default}"`
- [ ] Dynamic: `F(dynamic d)`: `F(default)`

# Code
- [ ] Statements:
    - `if (default) { }`
    - `switch (default) { }`
    - `switch (e) { case default: }`
    - `switch (e1) { case e2 when (default): }`
    - `while (default) { }`, `do { } while (default)`
    - `for (; default; ) { }`
    - `foreach (... in default) { }`
    - `throw default;` 
    - `return default;` 
    - `try  { } catch (e) when (default) { }`
    - `lock (default) { }` 
    - `using (default) { }`
    - `fixed (byte* p = default) { }`
    - `yield return default;`
    - `this = default;` in `struct` .ctor
- [ ] Expressions:
    - `default.F`
    - `default()`
    - `default[i]`
    - `a[default]`
    - `nameof(default)`
    - `checked(default)`, `unchecked(default)`, `checked(default + x)`
    - unary: `op default`
        - `!default`
    - binary: `e op default`, `default op e`
        - `default == false`
        - `new S() == default` for `struct S` with/without `operator==`, `default` has type `S` not `S?`
        - `default + x` with user-defined operator or conversion
    - `default ? e1 : e2`, `e1 ? default : e2`
    - `default ?? e`, `e ?? default`
    - `(T)default`
    - `x op= default`
        - `x += default` with user-defined operator or conversion
    - `*default`, `&default`, `default->F`
    - `default is e`, `e is default`
    - `await default`
    - `__refvalue(default, T)`, `_reftype(default)`, `__makeref(default)`
- [ ] Lambdas: `F(Func<T> f)`, `F(Action a)`: `F(() => default)`
- [ ] Target typing (var, lambdas, integrals)
- [ ] Conversions
    - [ ] boxing/unboxing
    - [ ] `DefaultOrNullLiteralConversion`
- [ ] Nullable (wrapping, unwrapping)
- [ ] Anonymous types
- [ ] Tuples
- [ ] LINQ: `select default`
- [ ] Ref return values
- [ ] Overload resolution:
    - [ ] `F(int i)`, `F(string s)`: `F(default)`
    - [ ] `F(Task<T> t)`, `F(ValueTask<T> t)`: `F(async () => default)`
- [ ] Type inference
- [ ] `WRN_IsAlwaysTrue`, `WRN_IsAlwaysFalse`, `WRN_AlwaysNull`
- [ ] `__arglist`: `F(object o, __arglist)`: `F(null, default)`

# IDE
- [ ] Colorization
- [ ] Intellisense: `default(`, `default.`, `default[`
- [ ] Squiggles
- [ ] More: https://github.com/dotnet/roslyn/issues/8389

# Debugger / EE
- [ ] Compiling expressions in Immediate/Watch windows or hovering over an expression
- [ ] Compiling expressions in `[DebuggerDisplay("...")]`
- [ ] Assigning values in Locals/Autos/Watch windows
  
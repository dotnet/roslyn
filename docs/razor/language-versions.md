# Razor language versions

This table lists the features added in each Razor language version.
Later versions include the features from earlier versions.

| Razor version | Features added |
|---------------|------------------|
| **1.0** | Baseline Razor markup/C# transitions, expressions, code and control-flow blocks, templates, comments and Tag Helpers. |
| **1.1** | None |
| **2.0** | None |
| **2.1** | Minimized boolean Tag Helper attributes, such as `<some-tag enabled />`. HTML comments, transitions and code are permitted inside Tag Helpers that restrict their allowed children. |
| **3.0** | Component (`.razor`) files, including component directives such as `@code`, `@layout`, `@page`, `@inject` and `@typeparam`, and directive attributes for binding, events, `@ref`, `@key` and `@attributes`. The `@attribute` and `@implements` directives. Razor markup inside C# methods, including those in `@functions`. `using var` declarations and nullable-forgiving `!` in implicit expressions. |
| **5.0** | `@preservewhitespace` for components. |
| **6.0** | Generic constraints on component type parameters, such as `@typeparam T where T : IDisposable`. |
| **7.0** | `@bind:get` and `@bind:set`, including their `@bind-...` forms. |
| **8.0** | `@rendermode`, both as a component directive and a directive attribute, and `@formname`. |
| **9.0** | None |
| **10.0** | None |
| **11.0** | [Tilde path expansion](tilde-path-expansion.md) for `~/`-prefixed string literal values in HTML attributes and component parameters that accept asset paths. |
| **12.0** | [`@documentation { ... }`](documentation-directive.md) for XML documentation on components and views. |

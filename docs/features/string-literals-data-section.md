# String literals in data section

This opt-in Roslyn feature allows changing how string literals in C# programs are emitted into PE files (`.dll`/`.exe`).
By default, string literals are emitted to the UserString heap which is limited to [2^24 bytes](https://github.com/dotnet/roslyn/issues/9852).
When the limit is reached, the following compiler error is reported by Roslyn:

```
error CS8103: Combined length of user strings used by the program exceeds allowed limit. Try to decrease use of string literals.
```

By turning on the feature flag `utf8-string-literal-encoding`, string literals (where possible) are instead emitted as UTF-8 data into a different section of the PE file
which does not have the same limit. The emit format is similar to [explicit u8 string literals](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/utf8-string-literals).

The feature flag can take a non-negative integer threshold. Only string literals whose length is over the threshold are emitted in the new way described above.
By default, the threshold is 100. Specifying 0 means all string literals are considered for the feature. Specifying `off` turns off the feature (this is the default).

The feature flag can be specified on the command line like `/features:utf8-string-literal-encoding` or `/features:utf8-string-literal-encoding=20`,
or in a project file in a `<PropertyGroup>` like `<Features>$(Features);utf8-string-literal-encoding</Features>` or `<Features>$(Features);utf8-string-literal-encoding=20</Features>`.

# Microsoft.CodeAnalysis.BannedApiAnalyzers

`Microsoft.CodeAnalysis.BannedApiAnalyzers` is a Roslyn analyzer package that detects uses of banned symbols and APIs in C# and Visual Basic projects, enabling teams to enforce architectural boundaries and deprecated API restrictions.

## How to Use

To enable banned API analysis, add one or more `BannedSymbols.txt` (or `BannedSymbols.*.txt`) files as additional files to your project:

```xml
<ItemGroup>
  <AdditionalFiles Include="BannedSymbols.txt" />
</ItemGroup>
```

Each line in `BannedSymbols.txt` specifies a documentation comment ID string for the prohibited symbol, with an optional custom diagnostic explanation:

```txt
{Documentation Comment ID string for the symbol}[;Description Text]
```

### Examples of `BannedSymbols.txt` Entries

```txt
// Ban specific type
T:System.Collections.ArrayList;Use generic List<T> instead

// Ban specific constructor or method
M:System.DateTime.#ctor(System.Int64);Use DateTimeOffset instead
M:System.Console.WriteLine(System.String);Use ILogger instead

// Ban property or field
P:System.Environment.TickCount;Use Stopwatch instead
```

Lines beginning with `//` are treated as comments.

## Diagnostic Rules Included

- **RS0030**: The symbol has been banned (banned symbol was used in source)
- **RS0031**: The list of banned symbols contains a duplicate
- **RS0035**: The list of banned symbols contains an entry that could not be resolved

For detailed configuration options (including excluding generated code via `.globalconfig`), see the [BannedApiAnalyzers Documentation](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md).

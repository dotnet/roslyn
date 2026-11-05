# Format documentation provenance

The hover's format explanations come from direct extracts of Microsoft documentation.
The extractor does not paraphrase or generate explanatory prose.
The snapshots retain the exact bytes of their upstream Git blobs.

```sh
python3 eng/format-string-documentation/generate.py --check
python3 -m unittest discover -s eng/format-string-documentation -p 'test_*.py'
```

To regenerate the resources and lookup, run the extractor without `--check`.
Then run the C# Features project's `UpdateXlf` target.

`sources.json` records each repository, commit, path, and Git blob hash.
`extractions.json` records each resource's exact text and its source selector.
Selectors identify table rows and fields, XML overloads, named Markdown sections, or sentences with an exact prefix.
The check verifies source hashes and compares the generated files byte for byte.
It also rejects manual format resources and the earlier Roslyn completion-label fallback.
These checks run when Python optimization is enabled.
They verify generated assets, not every possible change to the C# consumer.
The C# tests verify that representative hover descriptions use the extracted text; code review must check new description paths.

The extractor removes presentation markup, resolves cross-reference targets as identifiers, decodes HTML entities, and collapses whitespace.
It excludes table cross-links and example introductions from description cells.
It selects the GUID description before the table's static example pattern.
It does not replace words with summaries.

The resource and lookup output contain 136 extracts and 184 lookup entries.
The inputs contain ten documentation files and two license files.
The pinned documentation commits are:

- `dotnet/docs`: `81cd030da804aa06cfbd8f23474cbbacba597c72`
- `dotnet/dotnet-api-docs`: `b048785279367d02531d04e1f61dc7dad6ed7096`

The source snapshots and extracted documentation retain their CC-BY-4.0 terms.
The two license files are in `sources/`.
Each generated resource comment links to its pinned source.

The format heading, Preview and Example headings, documentation-link label, and preview-omission message are application UI text.
They do not describe .NET format semantics.
The application computes examples with framework formatters, separately from the extracted documentation.
Floating-point `X`/`x` uses the .NET 11 release-note sentence.
It requires .NET 11 library references and support from the host formatter.

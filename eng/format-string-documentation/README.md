# Format documentation

Format descriptions are extracted from pinned Microsoft documentation, without paraphrasing.
`sources.json` records the commits, paths, and Git blob hashes. Each generated resource
comment records its source URL and selector. The extracted text retains the CC-BY-4.0
terms in [LICENSE](LICENSE). The generator code uses the repository's MIT license.

```sh
python3 eng/format-string-documentation/generate.py --check
python3 -m unittest discover -s eng/format-string-documentation -p 'test_*.py'
```

The first run downloads the pinned sources into the ignored `.cache` directory.
Every run verifies their hashes before extraction and checks the generated resources
and lookup byte for byte. Later runs can use the cache offline.

Run the generator without `--check` to regenerate, then run the C# Features project's
`UpdateXlf` target. The extractor removes presentation markup and collapses whitespace
without rewriting source words. Output examples come from framework formatters,
separately from these descriptions.

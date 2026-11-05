# Format hover

C# Quick Info explains explicit formats in interpolation, recognized framework
format arguments, and composite items. Descriptions are direct Microsoft Learn
extracts. [Regeneration and verification](../../eng/format-string-documentation/README.md)
use pinned source hashes and selectors in the generated resource comments.

Supported types include Guid, numbers, enums, DateTime, DateTimeOffset, DateOnly,
TimeOnly, TimeSpan, BigInteger, and Complex. Unsupported types and formats receive
no feature hover.

## Preview and Example

Preview uses a compiler constant or an inferred value. The evaluator follows
supported operations and unchanged local initializers. Roslyn data-flow analysis
and structural checks reject mutation, capture, reference escape, and jumps past
initializers. Reviewed framework operations never execute project code.

A resolved supported culture takes precedence. Otherwise, the editor uses standard
en-US data. Preview describes one value in this editor context. It does not predict
ambient runtime culture, the complete string, or alignment padding.

Unknown values use fixed Example samples. Invalid or excessive output can omit the
output block. The evaluator bounds values, recursion, request work, and data-flow
scope. Cancellation propagates normally.

## Preview inlays

Inlays use the hover parser and evaluator. They are off by default:

```editorconfig
[*.cs]
dotnet_format_string_preview_hints = true
```

Only proven values receive hints. Labels are single-line, with at most 80 output
characters. Tooltips resolve on demand. The option and hint category require product
review. The current LSP path classifies non-parameter hints as Type. Visual Studio
presentation needs separate validation.

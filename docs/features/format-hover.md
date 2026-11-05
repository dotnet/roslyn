# Format hover

This draft implementation adds C# Quick Info for explicit format components.
It supports interpolation, recognized framework `ToString` and `TryFormat` calls,
and recognized composite-format arguments. Composite hover and preview hints use
the same parser and argument mapping.

## Documentation and output

Format descriptions are direct extracts from pinned Microsoft documentation.
The [extraction instructions](../../eng/format-string-documentation/README.md)
describe the source manifests, licenses, generated resources, and verification.
Application headings and labels are UI text. Framework formatters produce output.

Supported types include `Guid`, integral and floating-point numeric types,
`decimal`, `BigInteger`, `Complex`, `DateTime`, `DateTimeOffset`, `DateOnly`,
`TimeOnly`, `TimeSpan`, and enums. A type, format, or target that the implementation
cannot support does not receive an approximation of unsupported behavior.

`Preview` shows a proven source value in the editor's selected format context.
An explicit supported culture takes precedence. Otherwise, the editor uses
read-only `en-US` culture data. This is an editor policy, not an inference about
the application's ambient culture or deployment environment.

`Example` uses a fixed sample when the value is unknown or Preview is ineligible.
The implementation does not use Faker or random samples. Invalid formats receive
no feature hover. Excessive output can leave valid documentation without an
output block. A custom formatter or interpolated-string handler cannot receive
an unsupported claim about built-in format semantics.

The output describes one value. It excludes composite alignment padding and does
not establish the complete enclosing string, successful execution, or destination
buffer capacity for `TryFormat`.

## Value inference

The evaluator uses compiler constants first. For an eligible nonconstant local,
it follows the initializer and supported operations in the current semantic model.
Roslyn data-flow analysis and conservative structural checks reject other writes,
captures, managed-reference escapes, unsafe address use, and jumps that can bypass
the initializer. A write after the use can also reject the value.

Reviewed framework operations can produce known values from known inputs. Examples
include canonical `Guid.Parse` text, component-based date constructors, and exact
tick arithmetic. The evaluator never loads project assemblies or invokes project
constructors, methods, properties, conversions, or operators. It does not infer
clock values or an implicit local time zone.

Target and value restrictions remain conservative. Examples include unknown native
integer width, ambiguous enum aliases, and format support absent from the target
libraries. A matching framework signature alone does not prove identical behavior
across runtime versions.

## Optional preview hints

The draft experiment reads this opt-in EditorConfig key:

```editorconfig
[*.cs]
dotnet_format_string_preview_hints = true
```

The default is off. Only eligible Preview values receive hints. Unknown values
retain Example hover without a preview hint. Hint labels are single-line and at
most 80 output characters. Tooltips remain deferred until resolve.

The option name, product controls, visual density, and hint category require
maintainer review. The experiment uses the existing non-parameter hint path,
which the LSP handler currently classifies as Type. This does not establish the
final semantic category for value hints. Visual Studio presentation requires
separate client validation.

## Work limits

The evaluator and parser have request-local budgets. These limits do not provide
a hard time bound for compiler APIs.

| Work | Limit |
| --- | --- |
| Recursive operations per value | 64 |
| Evaluated operations per request | 4,096 |
| Cached locals per request | 128 |
| Data-flow calls per request | 16 |
| Analysis scope | 2,048 syntax nodes |
| Candidate items per range request | 2,048 |
| Literal scans | 32,768 characters per literal, 131,072 per request |
| Format and output text | 256 characters each |

Value-inference exhaustion uses the ordinary unknown-value fallback. Literal and
format limits can suppress feature hover. Request limits stop further hint discovery.
The evaluator checks cancellation during feature-owned walks and around compiler
analysis. It uses request-scoped pooled state and no persistent document cache.

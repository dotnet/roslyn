# Razor documentation

Razor 12 and later support XML documentation on components and views with
`@documentation`:

```razor
@documentation {
    <summary>
    Shows the current time.
    </summary>
    <remarks>Uses <see cref="System.DateTime"/>.</remarks>
}

<p>@DateTime.Now</p>
```

The documentation is attached to the generated component or view class. The XML
is emitted as a C# `/** ... */` comment, with source mappings back to the Razor file.
C# handles XML documentation validation and symbol references, including `cref`
and `typeparamref`.

Braces are required. `@documentation foo` reports `RZ1017` because it expects an
opening `{`; it isn't shorthand for a summary.

Plain text such as `@documentation { This is the summary }` produces Razor warning
`RZ1049`: "Documentation should start with an XML tag, such as '<summary>'."
The warning highlights the first non-whitespace character (`T` in this example).
It remains plain text in the generated documentation; Razor does not add a
`<summary>` element. Write the tags explicitly when you want a summary.

The check only requires a leading `<`, ignoring whitespace. Empty bodies don't
warn, and XML comments, CDATA, processing instructions and incomplete tags satisfy
the prefix check. C# still handles XML validation. The warning is always on
(warning level 0); bodies already rejected for a literal `*/` only report `RZ1047`.

Before Razor 12, `@documentation` still starts an implicit expression. For example,
`@documentation foo` renders the value of `documentation` followed by the literal
text ` foo`. Warning `RZ1048` flags this syntax when `RazorWarningLevel` is 11 or
higher because it changes meaning in Razor 12. Projects using older language
versions can opt in by raising their warning level. Use an explicit expression such as
`@(documentation) foo` or `@(documentation.Length)` to preserve the expression.

The body is documentation text, not Razor markup or C# code. Razor transitions,
braces inside XML elements, XML comments, and CDATA are preserved. The body is
excluded from rendered HTML.

Parsing uses Razor's HTML parser in an XML-only mode. Razor transitions remain
literal in text, attributes, comments, CDATA and processing instructions.
Tag names are case-sensitive, and HTML void-element and script rules don't apply.
Only top-level text can contain the directive's closing brace. The original body
is parsed directly, stored, mapped and emitted unchanged. This works with either
Razor tokenizer without changing how surrounding Razor is parsed.

Write plain XML rather than C# comment decoration. Leading `*` characters are
ordinary text for boundary parsing; Razor does not strip or normalise them or
issue a decoration-specific diagnostic. C# can still ignore them when processing
the emitted comment.

Recovery uses closing braces and following Razor directives to avoid consuming
unrelated markup or code. Incomplete C# examples inside complete XML remain
documentation text. A missing documentation brace reports `RZ1006`. Recovery is
best-effort for malformed XML. A literal `*/` does not end documentation parsing:
Razor finds the body boundary first, then reports `RZ1047` on the terminator and
does not emit the invalid documentation.

Only one `@documentation` block is allowed per file. Additional blocks produce
error `RZ2001`. Put all documentation tags in the same block. For error recovery,
only the first block is used, even if it is empty or malformed. C# decides whether
those tags are valid; Razor doesn't merge or validate them.

A literal `*/` in the parsed documentation body would end the generated C#
comment, so it produces error `RZ1047` and the block isn't emitted. In XML text,
use a character reference such as `*&#47;` instead.

The directive is file-local. It works in `.razor` and `.cshtml` files, but can't be
imported from `_Imports.razor` or `_ViewImports.cshtml`.

Compiler baselines are under
`src/Razor/src/Compiler/Microsoft.AspNetCore.Razor.Language/test/TestFiles/IntegrationTests/DocumentationDirectiveCodeGenerationTest`
for components and the neighbouring `DocumentationDirectiveCodeGenerationTest_Legacy`
directory for views. They include syntax trees (`.stree.txt`), IR (`.ir.txt`),
generated C# (`.codegen.cs`), diagnostics and source mappings. Component
declarations are recorded separately in the `.decl.*` baselines.
The `IdentifierInRazor11` and `MissingOpeningBrace` cases show the version-dependent
meaning of `@documentation foo`.
Recovery baselines cover closing-tag strings in following code, same-line markup
after malformed XML, and missing closing braces before subsequent code.

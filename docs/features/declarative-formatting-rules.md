# Declarative Formatting Rules (Code Style Sheets)

## TL;DR

**Goal:** Preserve the existing Roslyn formatting engine and all `.editorconfig` compatibility while adding a declarative, CSS-like authoring layer on top. Authors write human-readable selector rules; a source generator compiles them into efficient, pre-validated runtime structures.

**How to think about it:** Like CSS for syntax trees. Instead of imperatively coding spacing and newline decisions in C#, you write:

```text
MethodDeclaration::body-open {
  line-break-before: 1;
}

BinaryExpression::operator {
  space-before: 1;
  space-after: 1;
}
```

And the tooling takes care of the rest.

---

## Key Components

- **Option definitions stay.** All existing `OptionKey`-based formatting options remain in place. The new layer sits alongside them and compiles down to the same runtime types.
- **CSS-like selector language.** Rules target syntax nodes and their named token parts using a `Node::part-name { property: value; }` syntax inspired by CSS pseudo-elements. Raw token-kind selectors (`token[kind=OpenParenToken]`) remain available as an advanced escape hatch.
- **Source generator.** A Roslyn source generator parses `.style` rule blocks at compile time, validates selectors and property values against the known syntax model, and emits pre-built rule tables. No runtime parsing, no reflection.
- **`.editorconfig` compatibility wrapper.** Existing options can be declared with `@editorconfig name = value;` which expands into ordinary selector rules at a known priority tier, so existing `.editorconfig` files continue to work without any changes.
- **Specificity and ordering.** Conflict resolution follows CSS-style specificity (more-specific selector wins) with source order as a tiebreaker. User-authored rules always have higher priority than expanded compatibility rules.

---

## Scope

- **Phase 1 (initial):** Token-level spacing, newline placement, and open/close brace options. These cover the majority of C# formatting options (`csharp_space_*`, `csharp_new_line_*`, `csharp_preserve_*`).
- **Incremental migration.** Existing formatter rule methods are replaced one option group at a time. Each migrated group is validated against the existing behavior through snapshot tests before the imperative code is removed.
- **C# first.** The selector model is language-agnostic in principle, but the initial part-name catalog and compatibility mappings target C#.

---

## Benefits

| Benefit | Detail |
|---|---|
| Maintainability | Formatting policy lives in declarative rules, not scattered across C# methods. Adding or changing a rule requires editing one block, not tracing call chains. |
| Compile-time validation | The source generator catches unknown node names, unknown part names, unknown properties, and invalid values before the code ships. |
| Performance | Rule tables are generated at compile time. The runtime formatter does a single lookup per token boundary with no parsing overhead. |
| `.editorconfig` compatibility | `@editorconfig` directives expand to selector rules, preserving full backward compatibility. Users can override any expanded rule with a more specific selector. |
| Readability | `MethodDeclaration::parameter-open { space-before: 0; }` is immediately understandable to anyone familiar with either CSS or Roslyn syntax node names. |

---

## Future Work

- **Embedded language tooling.** Because rule blocks have a well-defined grammar, editors can provide syntax highlighting, completions, and diagnostics inside `.style` files or embedded rule strings. This is the primary motivation for the CSS-like surface (rather than a JSON or XML format).
- **Part-name catalog expansion.** Additional node types and token parts can be registered incrementally, covering VB.NET and Razor syntax nodes.
- **User-defined macros.** A `@define` directive could let teams define named shorthand rules that expand to multiple selectors, similar to CSS custom properties.
- **Live preview.** Because the rule compiler is fast, IDE tooling can show a real-time diff of how a rule change affects a sample code document.

---

## Risks and Open Questions

- **Part-name catalog maintenance.** Every new syntax node that needs formatting coverage requires new named parts. Keeping the catalog complete and stable across Roslyn versions is ongoing work.
- **Specificity edge cases.** CSS specificity rules can be surprising. We need a clear, documented model for how node-level rules, part-level rules, and compatibility rules rank against each other.
- **Source generator integration point.** Deciding where in the build the generator runs (analyzer layer vs. dedicated tool) affects how easily external consumers (e.g., Roslyn forks, MAUI Hot Reload) can adopt the system.
- **`.editorconfig` round-trip.** When a user has both a `.editorconfig` file and selector rules, the merged effective configuration should be inspectable. A diagnostic or tooling command to show the resolved rule set is needed but not yet designed.
- **VB.NET and Razor parity.** The initial design focuses on C#. Extending to VB.NET and Razor without creating per-language forks of the selector grammar is an open design question.
- **Testing coverage.** The space of possible selector combinations is large. A property-based or combinatorial test strategy is likely required in addition to handwritten snapshot tests (see [Testing Strategy](#testing-strategy) below).

---

## Testing Strategy

Formatting correctness is inherently example-driven, but the declarative model enables several complementary approaches.

### 1. Snapshot tests for each migrated option

For every `.editorconfig` option migrated to selector rules, add a test that formats a representative source file with the option set to each of its legal values and asserts the output matches a checked-in snapshot. This is the same pattern used today for `FormattingTestBase`-derived tests.

### 2. Compatibility regression suite

Run the full existing formatting test suite against the new selector-compiled rule tables and assert zero behavioral differences. This acts as a safety net during incremental migration.

### 3. Source generator validation tests

Unit-test the source generator in isolation: feed it known-good and known-bad rule blocks and assert that it emits the expected C# or the expected diagnostics.

### 4. Rule conflict resolution tests

For each specificity scenario (two rules targeting the same token, one more specific than the other), assert which rule wins. These are small, targeted unit tests against the rule-resolution engine.

### 5. Property-based / combinatorial tests

Generate random but syntactically valid C# and apply random but valid rule sets. Assert that the formatter terminates, produces valid C#, and is idempotent (formatting the output again produces no further changes). Idempotency is a particularly strong invariant for a token-boundary model.

### 6. `.editorconfig` round-trip tests

For each `@editorconfig` compatibility directive, assert that the expanded selector rules produce output identical to what the original `.editorconfig` option produces through the current code path.

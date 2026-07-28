---
coverage: Compiler-layer (src/{Compilers,Dependencies,ExpressionEvaluator,Tools}) known issues, quirks & workarounds
---

# Compiler — Known Issues

Layer-specific quirks for the compiler. Load when working under
`src/{Compilers,Dependencies,ExpressionEvaluator,Tools}`. Cross-cutting issues
(generated code, CI marker gating, environmental test failures) live in
`.github/memory/KNOWN_ISSUES.md`.

## VB grammar generator pairs node-kinds with child-kinds by position

- **Affected area:** `src/Tools/CompilerGeneratorTools/Source/VisualBasicSyntaxGenerator/Grammar/GrammarGenerator.vb`,
  `src/Compilers/VisualBasic/Portable/Syntax/Syntax.xml`
- **Description:** When a `node-structure` declares several `node-kind`s and a child
  declares the same number of kinds, the generator emits one grammar rule per node-kind
  and pairs kind *i* of the node with kind *i* of the child. Unless the child spells the
  pairing out with `<kind name="..." node-kind="..."/>` elements, the two declaration
  orders in `Syntax.xml` must agree or the generated grammar is silently wrong.
  Adding the explicit `<kind>` elements is not a free fix: it also makes the child
  auto-creatable, which changes the generated `SyntaxFactory` overloads (a public API
  break).
- **Workaround:** Keep the child's `kind="A|B"` list in the same order as the
  structure's `node-kind` elements.

## VB grammar rule names come from both structures and node-kinds

- **Affected area:** same generator.
- **Description:** Rule books are keyed by raw name (`ResumeStatementSyntax` vs
  `ResumeStatement`) but emitted names drop the `Syntax` suffix, so a node-kind named
  after its own structure collapses onto the structure's rule. The generator now folds
  such a specialization into the structure's rule rather than emitting it twice.
- **Workaround:** None needed; be aware when adding a node-kind whose name matches its
  structure minus `Syntax`.

## Plain-list multiplicity in the VB grammar comes from `min-count`

- **Affected area:** same generator.
- **Description:** `min-count` on a `<child>` is consumed only by the grammar generator
  in VB (nothing else in codegen reads it), so it is the safe way to mark a list as
  required. `optional="true"` on a list child does feed codegen — it makes the factory
  parameter defaultable — so do not flip it just to influence the grammar.
